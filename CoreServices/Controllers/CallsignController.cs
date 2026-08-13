using System.Net;
using Asp.Versioning;
using CoreServices.Model.Qrz;
using CoreServices.Integrations.Qrz;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class CallsignController(IQrzClient qrzClient) : ControllerBase
{
    private const string CallsignQueryEndpoint = "/api/ars/v{version}/Callsign?call={call}";

    /// <summary>
    /// Gets QRZ callsign data using the query-string route.
    /// </summary>
    /// <param name="call">The callsign to look up.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>A QRZ callsign record or a validation response.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(QRZDatabase), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [Produces("application/json")]
    public Task<IActionResult> GetCallDataByCallsignFromQuery([FromQuery] string call, CancellationToken cancellationToken)
    {
        return GetCallDataByCallsignValue(call, cancellationToken);
    }
    
    /// <summary>
    /// Gets QRZ callsign data using the deprecated path route.
    /// </summary>
    /// <param name="id">The callsign to look up.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>A QRZ callsign record or a validation response.</returns>
    [HttpGet("{*id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(QRZDatabase), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
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
        var decodedCall = WebUtility.UrlDecode(call)?.Trim();
        if (!string.IsNullOrWhiteSpace(decodedCall))
        {
            var callInfo = await qrzClient.GetCallDataAsync(decodedCall, cancellationToken);
            return Ok(callInfo);
        }
        
        return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["call"] = ["A callsign is required."]
        }));
    }
}
