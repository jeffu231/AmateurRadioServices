using System.Net;
using Asp.Versioning;
using CoreServices.Integrations.Qrz;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class DataServiceController(IQrzSessionProvider qrzSessionProvider, ILogger<DataServiceController> logger)
    : ControllerBase
{
    /// <summary>
    /// This operation returns the subscription expiration date time when there is a valid session.
    /// </summary>
    /// <returns><see cref="DateTime"/> Expiration Date Time</returns>
    [HttpGet("SubscriptionExpirationTime")]
    [MapToApiVersion("1.0")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(DateTime), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> SubscriptionExpirationTime(CancellationToken cancellationToken)
    {
        var session = await qrzSessionProvider.GetSessionAsync(cancellationToken);
        if (session is null)
        {
            logger.LogError("QRZ session could not be established");
            return Problem("The session was not valid.");
        }
        
        return Ok(session.SubscriptionExpiration.UtcDateTime);
    }
}
