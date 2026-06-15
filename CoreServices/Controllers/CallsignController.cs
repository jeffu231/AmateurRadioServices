using System.Net;
using Asp.Versioning;
using CoreServices.Model.Qrz;
using CoreServices.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class CallsignController: ControllerBase
{
    private const string CallsignQueryEndpoint = "/api/ars/v{version}/Callsign?call={call}";
    private readonly QrzDataService _qrzDataService;
    
    public CallsignController(QrzDataService qrzDataService)
    {
        _qrzDataService = qrzDataService;
    }

    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(QRZDatabase), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [Produces("application/json")]
    public Task<IActionResult> GetCallDataByCallsignFromQuery([FromQuery] string call)
    {
        return GetCallDataByCallsignValue(call);
    }
    
    [HttpGet("{*id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(QRZDatabase), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [Produces("application/json")]
    [Obsolete("Use GET /api/ars/v{version}/Callsign?call={call}. The path-based endpoint cannot reliably carry encoded slashes.", false)]
    public Task<IActionResult> GetCallDataByCallsign(string id)
    {
        AddLegacyPathDeprecationHeaders();
        return GetCallDataByCallsignValue(id);
    }

    private void AddLegacyPathDeprecationHeaders()
    {
        Response.Headers["Deprecation"] = "true";
        Response.Headers["Link"] = $"<{CallsignQueryEndpoint}>; rel=\"alternate\"";
    }

    private async Task<IActionResult> GetCallDataByCallsignValue(string call)
    {
        var decodedCall = WebUtility.UrlDecode(call);
        if (!string.IsNullOrEmpty(decodedCall))
        {
            var callInfo = await _qrzDataService.GetCallDataAsync(decodedCall);
            return Ok(callInfo);
        }
        
        return BadRequest("Missing call sign.");
    }
}
