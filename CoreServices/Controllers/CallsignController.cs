using System.Net;
using Asp.Versioning;
using CoreServices.Model.Qrz;
using CoreServices.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class CallsignController: ControllerBase
{
    private readonly QrzDataService _qrzDataService;
    
    public CallsignController(QrzDataService qrzDataService)
    {
        _qrzDataService = qrzDataService;
    }
    
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(QRZDatabase), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetCallDataByCallsign(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            var callInfo = await _qrzDataService.GetCallDataAsync(id);
            return Ok(callInfo);
        }
        
        return BadRequest("Missing call sign.");
    }
}