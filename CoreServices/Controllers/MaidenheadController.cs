using System.Net;
using Asp.Versioning;
using CoreServices.Model;
using CoreServices.Validation;
using MaidenheadLib;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class MaidenheadController(ILogger<MaidenheadController> logger) : ControllerBase
{
    [HttpGet]
    [Route("bearing")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [Produces("application/json", "text/plain")]
    public IActionResult GetBearing([FromQuery] string srcGrid, [FromQuery] string destGrid)
    {
        if (IsValidGridPair(srcGrid, destGrid))
        {
            var start = MaidenheadLocator.LocatorToLatLng(srcGrid.Trim().ToUpperInvariant());
            var end = MaidenheadLocator.LocatorToLatLng(destGrid.Trim().ToUpperInvariant());

            return Ok((Int32)Math.Round(MaidenheadLocator.Azimuth(start, end), 0, MidpointRounding.AwayFromZero));
        }
        
        return InvalidGridPair();
    }

    [HttpGet]
    [Route("distance")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(Distance), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [Produces("application/json")]
    public IActionResult GetDistance([FromQuery] string srcGrid, [FromQuery] string destGrid)
    {
        if (IsValidGridPair(srcGrid, destGrid))
        {
            var start = MaidenheadLocator.LocatorToLatLng(srcGrid.Trim().ToUpperInvariant());
            var end = MaidenheadLocator.LocatorToLatLng(destGrid.Trim().ToUpperInvariant());
            var distance = new Distance(MaidenheadLocator.Distance(start, end));
            return Ok(distance);
        }
        
        logger.LogWarning("Invalid grid pair on request. src:{Lat} dest:{Lon}", srcGrid, destGrid);
        return InvalidGridPair();
    }

    [HttpGet]
    [Route("grid")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [Produces("application/json", "text/plain")]
    public IActionResult GetGrid([FromQuery] double lat, [FromQuery] double lon)
    {
        if (!double.IsFinite(lat) || !double.IsFinite(lon) || lat is < -90 or > 90 || lon is < -180 or > 180)
        {
            logger.LogWarning("Invalid coordinates on request. Lat:{Lat} Long:{Lon}", lat, lon);
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["coordinates"] = ["Latitude must be between -90 and 90 and longitude must be between -180 and 180."]
            }));
        }

        var grid = MaidenheadLocator.LatLngToLocator(lat, lon);
        return Ok(grid);
    }

    private static bool IsValidGridPair(string? sourceGrid, string? destinationGrid) =>
        !string.IsNullOrWhiteSpace(sourceGrid) && !string.IsNullOrWhiteSpace(destinationGrid) &&
        MaidenheadGridValidator.IsValid(sourceGrid.Trim().ToUpperInvariant()) &&
        MaidenheadGridValidator.IsValid(destinationGrid.Trim().ToUpperInvariant());

    private IActionResult InvalidGridPair() => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
    {
        ["grid"] = ["Source and destination grids must be valid four-, six-, or eight-character Maidenhead values."]
    }));
}
