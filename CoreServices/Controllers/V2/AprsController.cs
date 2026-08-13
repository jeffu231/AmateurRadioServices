using Asp.Versioning;
using CoreServices.Application;
using CoreServices.Contracts.V2;
using CoreServices.Integrations.Aprs;
using CoreServices.Model.Aprs;
using CoreServices.Validation;
using MaidenheadLib;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers.V2;

/// <summary>
/// Provides stable v2 APRS location resources.
/// </summary>
[ApiController]
[Route("api/ars/v{version:apiVersion}/aprs/locations")]
[ApiVersion("2.0")]
[Produces("application/json")]
public sealed class AprsController(IAprsClient aprsClient) : ControllerBase
{
    /// <summary>
    /// Gets APRS locations for a query-string comma-separated callsign list.
    /// </summary>
    /// <remarks>Each callsign is 1-16 characters; at most 25 unique callsigns are accepted. Slash-bearing callsigns, such as <c>N9NOC/P</c>, are supported as query values.</remarks>
    /// <param name="callsigns">The comma-separated APRS callsign list.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>Stable APRS location responses or Problem Details.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AprsLocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public Task<ActionResult<IReadOnlyList<AprsLocationResponse>>> GetLocationsAsync(
        [FromQuery] string callsigns,
        CancellationToken cancellationToken) =>
        GetResponseAsync(callsigns, cancellationToken, entries => entries.Select(entry => new AprsLocationResponse
        {
            Name = entry.Name,
            SourceCallsign = entry.SrcCall,
            DestinationCallsign = entry.DstCall,
            Latitude = entry.Lat,
            Longitude = entry.Lng,
            Comment = entry.Comment,
            Path = entry.Path,
            Type = entry.Type,
            Time = entry.Time,
            LastTime = entry.LastTime,
            Class = entry.Class,
            Symbol = entry.Symbol
        }).ToArray());

    /// <summary>
    /// Gets APRS coordinates for a query-string comma-separated callsign list.
    /// </summary>
    /// <param name="callsigns">The comma-separated APRS callsign list.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>Stable APRS coordinate responses or Problem Details.</returns>
    [HttpGet("coordinates")]
    [ProducesResponseType(typeof(IReadOnlyList<AprsCoordinateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public Task<ActionResult<IReadOnlyList<AprsCoordinateResponse>>> GetCoordinatesAsync(
        [FromQuery] string callsigns,
        CancellationToken cancellationToken) =>
        GetResponseAsync(callsigns, cancellationToken, entries => entries.Select(entry => new AprsCoordinateResponse
        {
            Name = entry.Name,
            Latitude = entry.Lat,
            Longitude = entry.Lng
        }).ToArray());

    /// <summary>
    /// Gets Maidenhead grids for a query-string comma-separated callsign list.
    /// </summary>
    /// <param name="callsigns">The comma-separated APRS callsign list.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>Stable APRS grid responses or Problem Details.</returns>
    [HttpGet("grids")]
    [ProducesResponseType(typeof(IReadOnlyList<AprsGridResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public Task<ActionResult<IReadOnlyList<AprsGridResponse>>> GetGridsAsync(
        [FromQuery] string callsigns,
        CancellationToken cancellationToken) =>
        GetResponseAsync(callsigns, cancellationToken, entries => entries.Select(entry => new AprsGridResponse
        {
            Name = entry.Name,
            Grid = MaidenheadLocator.LatLngToLocator(entry.Lat, entry.Lng)
        }).ToArray());

    private async Task<ActionResult<IReadOnlyList<TResponse>>> GetResponseAsync<TResponse>(
        string callsigns,
        CancellationToken cancellationToken,
        Func<IEnumerable<AprsEntry>, IReadOnlyList<TResponse>> map)
    {
        if (!AprsIdentifierValidator.IsValid(callsigns))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["callsigns"] = ["Provide between one and 25 unique APRS callsigns, each 1-16 characters."]
            }));
        }

        var record = await aprsClient.GetAprsLocRecordAsync(callsigns, cancellationToken).ConfigureAwait(false);
        var failure = ProviderFailureClassifier.FromAprs(record);
        if (failure is not null)
        {
            return ProviderResultMapper.ToActionResult<IReadOnlyList<TResponse>>(this,
                ProviderResult<IReadOnlyList<TResponse>>.Failure(failure.Value));
        }

        if (record!.Entries.Any(entry => !IsValidCoordinate(entry.Lat, entry.Lng)))
        {
            return ProviderResultMapper.ToActionResult<IReadOnlyList<TResponse>>(this,
                ProviderResult<IReadOnlyList<TResponse>>.Failure(ProviderFailureKind.InvalidPayload));
        }

        return Ok(map(record.Entries));
    }

    private static bool IsValidCoordinate(double latitude, double longitude) =>
        double.IsFinite(latitude) && double.IsFinite(longitude) &&
        latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
}
