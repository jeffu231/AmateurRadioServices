using System.Net;
using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ConfigurationController:ControllerBase
{
    /// <summary>
    /// Get the version of the application
    /// </summary>
    /// <returns>Application Version</returns>
    [HttpGet("version")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public IActionResult GetVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        return Ok(new { ApplicationVersion = version });
    }
}