using System.Net;
using CoreServices.Model.Aprs;
using CoreServices.Services;
using MaidenheadLib;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class AprsController:ControllerBase
{
    private readonly ILogger<AprsController> _logger;
    private readonly AprsService _aprsService;
    
    public AprsController(ILogger<AprsController> logger, AprsService aprsService)
    {
        _logger = logger;
        _aprsService = aprsService;
    }
    
    [HttpGet("loc/{id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AprsLocRecord), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Get(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Invalid search criteria. Must be one or more calls seperated by a comma.");
        }

        AprsLocRecord? record = await _aprsService.GetAprsLocRecordAsync(id);

        if (record != null)
        {
            return Ok(record);
        }
        
        _logger.LogError("Returned record was null!");
        return NotFound("Record not found.");
    }

    [HttpGet("loc/{id}/coord")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(List<AprsCoordinate>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> GetCoord(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Invalid search criteria. Must be one or more calls seperated by a comma.");
        }

        AprsLocRecord? record = await _aprsService.GetAprsLocRecordAsync(id);

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
        
        _logger.LogError("Returned record was null!");
        return NotFound("Record not found.");
    }
    

    [HttpGet("loc/{id}/grid")]
    [MapToApiVersion("1.0")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(List<AprsGrid>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetGrid(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Invalid search criteria. Must be one or more calls seperated by a comma.");
        }

        AprsLocRecord? record = await _aprsService.GetAprsLocRecordAsync(id);

        if (record != null)
        {
            if (record.Found > 0)
            {
                var grids = new List<AprsGrid>();
                foreach (var recordEntry in record.Entries)
                {
                    var grid = MaidenheadLocator.LatLngToLocator(record.Entries[0].Lat, record.Entries[0].Lng);
                    grids.Add(new AprsGrid(recordEntry.Name, grid));
                }
       
                return Ok(grids);
            }
            return NotFound($"Call not found. Result: {record.Result} Message:{record.Description}");
        }
        
        _logger.LogError("Returned record was null!");
        return NotFound("Record not found.");
    }
}