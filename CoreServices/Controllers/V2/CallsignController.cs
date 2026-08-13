using Asp.Versioning;
using CoreServices.Application;
using CoreServices.Contracts.V2;
using CoreServices.Integrations.Qrz;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers.V2;

/// <summary>
/// Provides stable v2 callsign lookup responses.
/// </summary>
[ApiController]
[Route("api/ars/v{version:apiVersion}/callsign")]
[ApiVersion("2.0")]
public sealed class CallsignController(IQrzClient qrzClient) : ControllerBase
{
    /// <summary>
    /// Gets the supported public details for one callsign.
    /// </summary>
    /// <param name="callsign">The callsign to look up.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>A stable callsign lookup response or Problem Details.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(CallsignLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    [Produces("application/json")]
    public async Task<ActionResult<CallsignLookupResponse>> GetAsync(
        [FromQuery] string callsign,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(callsign))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["callsign"] = ["A callsign is required."]
            }));
        }

        var callData = await qrzClient.GetCallDataAsync(callsign.Trim(), cancellationToken).ConfigureAwait(false);
        var failure = ProviderFailureClassifier.FromQrz(callData);
        if (failure is not null)
        {
            return ProviderResultMapper.ToActionResult<CallsignLookupResponse>(this,
                ProviderResult<CallsignLookupResponse>.Failure(failure.Value));
        }

        var callsignData = callData.Callsign[0];
        return Ok(new CallsignLookupResponse
        {
            Callsign = callsignData.call,
            FirstName = callsignData.fname,
            Name = callsignData.name,
            State = callsignData.state,
            Country = callsignData.country,
            Grid = callsignData.grid
        });
    }
}
