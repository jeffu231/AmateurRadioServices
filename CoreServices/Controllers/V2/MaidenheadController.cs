using Asp.Versioning;
using CoreServices.Contracts.V2;
using CoreServices.Validation;
using MaidenheadLib;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers.V2;

/// <summary>
/// Provides stable v2 Maidenhead calculation resources.
/// </summary>
[ApiController]
[Route("api/ars/v{version:apiVersion}/maidenhead")]
[ApiVersion("2.0")]
[Produces("application/json")]
public sealed class MaidenheadController : ControllerBase
{
    /// <summary>
    /// Gets the rounded bearing between two Maidenhead grid locators.
    /// </summary>
    /// <param name="srcGrid">The source grid locator.</param>
    /// <param name="destGrid">The destination grid locator.</param>
    /// <returns>A bearing response or a validation problem.</returns>
    [HttpGet("bearing")]
    [ProducesResponseType(typeof(MaidenheadBearingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<MaidenheadBearingResponse> GetBearing([FromQuery] string srcGrid, [FromQuery] string destGrid)
    {
        if (!IsValidGridPair(srcGrid, destGrid))
        {
            return InvalidGridPair();
        }

        var start = MaidenheadLocator.LocatorToLatLng(srcGrid.Trim().ToUpperInvariant());
        var end = MaidenheadLocator.LocatorToLatLng(destGrid.Trim().ToUpperInvariant());
        return Ok(new MaidenheadBearingResponse
        {
            Bearing = (int)Math.Round(MaidenheadLocator.Azimuth(start, end), 0, MidpointRounding.AwayFromZero)
        });
    }

    /// <summary>
    /// Gets the rounded distance between two Maidenhead grid locators.
    /// </summary>
    /// <param name="srcGrid">The source grid locator.</param>
    /// <param name="destGrid">The destination grid locator.</param>
    /// <returns>A distance response or a validation problem.</returns>
    [HttpGet("distance")]
    [ProducesResponseType(typeof(MaidenheadDistanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<MaidenheadDistanceResponse> GetDistance([FromQuery] string srcGrid, [FromQuery] string destGrid)
    {
        if (!IsValidGridPair(srcGrid, destGrid))
        {
            return InvalidGridPair();
        }

        var start = MaidenheadLocator.LocatorToLatLng(srcGrid.Trim().ToUpperInvariant());
        var end = MaidenheadLocator.LocatorToLatLng(destGrid.Trim().ToUpperInvariant());
        var kilometers = MaidenheadLocator.Distance(start, end);
        return Ok(new MaidenheadDistanceResponse
        {
            Miles = (int)Math.Round(kilometers * .6214, 0, MidpointRounding.AwayFromZero),
            Kilometers = (int)Math.Round(kilometers, 0, MidpointRounding.AwayFromZero)
        });
    }

    /// <summary>
    /// Gets the Maidenhead grid locator for a geographic coordinate.
    /// </summary>
    /// <param name="lat">The latitude in decimal degrees.</param>
    /// <param name="lon">The longitude in decimal degrees.</param>
    /// <returns>A grid response or a validation problem.</returns>
    [HttpGet("grid")]
    [ProducesResponseType(typeof(MaidenheadGridResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<MaidenheadGridResponse> GetGrid([FromQuery] double lat, [FromQuery] double lon)
    {
        if (!double.IsFinite(lat) || !double.IsFinite(lon) || lat is < -90 or > 90 || lon is < -180 or > 180)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["coordinates"] = ["Latitude must be between -90 and 90 and longitude must be between -180 and 180."]
            }));
        }

        return Ok(new MaidenheadGridResponse { Grid = MaidenheadLocator.LatLngToLocator(lat, lon) });
    }

    private static bool IsValidGridPair(string? srcGrid, string? destGrid) =>
        !string.IsNullOrWhiteSpace(srcGrid) && !string.IsNullOrWhiteSpace(destGrid) &&
        MaidenheadGridValidator.IsValid(srcGrid.Trim().ToUpperInvariant()) &&
        MaidenheadGridValidator.IsValid(destGrid.Trim().ToUpperInvariant());

    private ActionResult InvalidGridPair() => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
    {
        ["grid"] = ["Source and destination grids must be valid four-, six-, or eight-character Maidenhead values."]
    }));
}
