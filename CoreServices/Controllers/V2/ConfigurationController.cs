using System.Reflection;
using Asp.Versioning;
using CoreServices.Contracts.V2;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers.V2;

/// <summary>
/// Provides v2 configuration metadata.
/// </summary>
[ApiController]
[Route("api/ars/v{version:apiVersion}/configuration")]
[ApiVersion("2.0")]
public sealed class ConfigurationController : ControllerBase
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
}
