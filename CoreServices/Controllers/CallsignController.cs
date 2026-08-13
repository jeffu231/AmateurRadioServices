using System.Net;
using Asp.Versioning;
using CoreServices.Model.Qrz;
using CoreServices.Integrations.Qrz;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class CallsignController: ControllerBase
{
    private const string CallsignQueryEndpoint = "/api/ars/v{version}/Callsign?call={call}";
    private readonly IQrzClient _qrzClient;
    
    public CallsignController(IQrzClient qrzClient)
    {
        _qrzClient = qrzClient;
    }

    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(QRZDatabase), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [Produces("application/json")]
    public Task<IActionResult> GetCallDataByCallsignFromQuery([FromQuery] string call, CancellationToken cancellationToken)
    {
        return GetCallDataByCallsignValue(call, cancellationToken);
    }
    
    [HttpGet("{*id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(QRZDatabase), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [Produces("application/json")]
    [Obsolete("Use GET /api/ars/v{version}/Callsign?call={call}. The path-based endpoint cannot reliably carry encoded slashes.", false)]
    public Task<IActionResult> GetCallDataByCallsign(string id, CancellationToken cancellationToken)
    {
        AddLegacyPathDeprecationHeaders();
        return GetCallDataByCallsignValue(id, cancellationToken);
    }

    private void AddLegacyPathDeprecationHeaders()
    {
        Response.Headers["Deprecation"] = "true";
        Response.Headers["Link"] = $"<{CallsignQueryEndpoint}>; rel=\"alternate\"";
    }

    private async Task<IActionResult> GetCallDataByCallsignValue(string call, CancellationToken cancellationToken)
    {
        var decodedCall = WebUtility.UrlDecode(call);
        if (!string.IsNullOrEmpty(decodedCall))
        {
            var callInfo = await _qrzClient.GetCallDataAsync(decodedCall, cancellationToken);
            return Ok(callInfo);
        }
        
        return BadRequest("Missing call sign.");
    }
}
