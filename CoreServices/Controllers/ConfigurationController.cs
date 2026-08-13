using System.Net;
using System.Reflection;
using Asp.Versioning;
using CoreServices.Contracts.V1;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

/// <summary>
/// Provides application configuration metadata.
/// </summary>
[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class ConfigurationController : ControllerBase
{
    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    /// <returns>The application version response.</returns>
    [HttpGet("version")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ConfigurationVersionResponse), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public IActionResult GetVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        return Ok(new ConfigurationVersionResponse { ApplicationVersion = version });
    }
}
