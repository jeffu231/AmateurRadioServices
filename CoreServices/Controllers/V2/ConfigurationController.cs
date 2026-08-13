using System.Reflection;
using Asp.Versioning;
using CoreServices.Contracts.V2;
using CoreServices.Integrations.Qrz;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers.V2;

/// <summary>
/// Provides v2 configuration metadata.
/// </summary>
[ApiController]
[Route("api/ars/v{version:apiVersion}/configuration")]
[ApiVersion("2.0")]
[Produces("application/json")]
public sealed class ConfigurationController(IQrzSessionProvider qrzSessionProvider) : ControllerBase
{
    /// <summary>
    /// Gets the running application version.
    /// </summary>
    /// <returns>A stable application-version response.</returns>
    [HttpGet("version")]
    [ProducesResponseType(typeof(VersionResponse), StatusCodes.Status200OK)]
    public ActionResult<VersionResponse> GetVersion() => Ok(new VersionResponse
    {
        ApplicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
    });

    /// <summary>
    /// Gets the active QRZ subscription expiration.
    /// </summary>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>The subscription expiration or Problem Details when it is unavailable.</returns>
    [HttpGet("qrz/subscription-expiration")]
    [ProducesResponseType(typeof(SubscriptionExpirationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SubscriptionExpirationResponse>> GetSubscriptionExpirationAsync(
        CancellationToken cancellationToken)
    {
        var session = await qrzSessionProvider.GetSessionAsync(cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QRZ subscription information is unavailable.",
                type: "https://httpstatuses.com/503");
        }

        return Ok(new SubscriptionExpirationResponse
        {
            SubscriptionExpiration = session.SubscriptionExpiration
        });
    }
}
