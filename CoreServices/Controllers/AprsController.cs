using System.Net;
using Asp.Versioning;
using CoreServices.Model.Aprs;
using CoreServices.Integrations.Aprs;
using CoreServices.Validation;
using MaidenheadLib;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class AprsController(ILogger<AprsController> logger, IAprsClient aprsClient) : ControllerBase
{
    /// <summary>
    /// Gets APRS location records for a bounded comma-separated identifier list.
    /// </summary>
    /// <param name="id">The comma-separated APRS identifier list.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>An APRS location record or an HTTP error response.</returns>
    [HttpGet("loc/{id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AprsLocRecord), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        if (!AprsIdentifierValidator.IsValid(id))
        {
            return InvalidIdentifierList();
        }

        var record = await aprsClient.GetAprsLocRecordAsync(id, cancellationToken);

        if (record != null)
        {
            return Ok(record);
        }
        
        logger.LogError("APRS location lookup returned no record");
        return NotFound("Record not found.");
    }

    /// <summary>
    /// Gets APRS coordinates for a bounded comma-separated identifier list.
    /// </summary>
    /// <param name="id">The comma-separated APRS identifier list.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>APRS coordinate records or an HTTP error response.</returns>
    [HttpGet("loc/{id}/coord")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(List<AprsCoordinate>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetCoord(string id, CancellationToken cancellationToken)
    {
        if (!AprsIdentifierValidator.IsValid(id))
        {
            return InvalidIdentifierList();
        }

        var record = await aprsClient.GetAprsLocRecordAsync(id, cancellationToken);

        if (record != null)
        {
            if (record.Found > 0)
            {
                var coords = new List<AprsCoordinate>();

                foreach (var recordEntry in record.Entries)
                {
                    var point = new AprsCoordinate(recordEntry.Name, recordEntry.Lat, recordEntry.Lng);
                    coords.Add(point);
                }
            
                return Ok(coords);
            }
            
            return NotFound($"Call not found. Result: {record.Result} Message:{record.Description}");    
        }
        
        logger.LogError("APRS coordinate lookup returned no record");
        return NotFound("Record not found.");
    }
    

    /// <summary>
    /// Gets Maidenhead grids for a bounded comma-separated APRS identifier list.
    /// </summary>
    /// <param name="id">The comma-separated APRS identifier list.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>APRS grid records or an HTTP error response.</returns>
    [HttpGet("loc/{id}/grid")]
    [MapToApiVersion("1.0")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(List<AprsGrid>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadGateway)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetGrid(string id, CancellationToken cancellationToken)
    {
        if (!AprsIdentifierValidator.IsValid(id))
        {
            return InvalidIdentifierList();
        }

        var record = await aprsClient.GetAprsLocRecordAsync(id, cancellationToken);

        if (record != null)
        {
            if (record.Found > 0)
            {
                var grids = new List<AprsGrid>();
                foreach (var recordEntry in record.Entries)
                {
                    if (!IsValidCoordinate(recordEntry.Lat, recordEntry.Lng))
                    {
                        logger.LogWarning("APRS grid lookup returned invalid coordinates");
                        return Problem(
                            statusCode: (int)HttpStatusCode.BadGateway,
                            title: "APRS returned invalid coordinates.",
                            type: "https://httpstatuses.com/502");
                    }

                    var grid = MaidenheadLocator.LatLngToLocator(recordEntry.Lat, recordEntry.Lng);
                    grids.Add(new AprsGrid(recordEntry.Name, grid));
                }
       
                return Ok(grids);
            }
            return NotFound($"Call not found. Result: {record.Result} Message:{record.Description}");
        }
        
        logger.LogError("APRS grid lookup returned no record");
        return NotFound("Record not found.");
    }

    private IActionResult InvalidIdentifierList()
    {
        return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["id"] = ["Provide between one and 25 unique APRS identifiers, each no longer than 16 characters."]
        }));
    }

    private static bool IsValidCoordinate(double latitude, double longitude) =>
        double.IsFinite(latitude) && double.IsFinite(longitude) &&
        latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
}
