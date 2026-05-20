using System.Net;
using Asp.Versioning;
using CoreServices.Model;
using CoreServices.Model.Qrz;
using CoreServices.Services;
using MaidenheadLib;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ContactController: ControllerBase
{
    private readonly QrzDataService _qrzDataService;
    private readonly ILogger<ContactController> _logger;
    
    public ContactController(QrzDataService qrzDataService, ILogger<ContactController> logger)
    {
        _qrzDataService = qrzDataService;
        _logger = logger;
    }
    
    /// <summary>
    /// This operation tries to enhance the existing contact info by doing a lookup of the DxCall and if the DxGrid is
    /// missing, or the first 4 chars of the DxGrid match the lookup, the lookup grid is used. A new bearing will be
    /// calculated. If the the contact info cannot be enhanced, the original information is returned.
    /// </summary>
    /// <param name="contactInfo"></param>
    /// <returns></returns>
    [HttpPost("EnhanceBearing")]
    [MapToApiVersion("1.0")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ContactInfo), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> EnhanceBearing([FromBody] ContactInfo contactInfo)
    {
        if (string.IsNullOrEmpty(contactInfo.DxCall))
        {
            //Nothing to do since we don't have a call to lookup
            return Ok(contactInfo);
        }
        //Try to do a lookup on the call and see if we can improve the grid accuracy to 6 chars.
        var callInfo = await _qrzDataService.GetCallDataAsync(contactInfo.DxCall);
        if (callInfo.Session != null && callInfo.Session.Length > 0)
        {
            var subExp = callInfo.Session[0].SubExp;
            if (!string.IsNullOrEmpty(subExp) &&
                DateTime.TryParseExact(subExp, "ddd MMM d HH:mm:ss yyyy", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var expDate) &&
                expDate < DateTime.UtcNow)
            {
                foreach (var qrzDatabaseSession in callInfo.Session)
                {
                    _logger.LogError("Session: {SessionMessage}, {SessionError}, {Remark}, {SubExp}", 
                        qrzDatabaseSession.Message, qrzDatabaseSession.Error,qrzDatabaseSession.Remark, qrzDatabaseSession.SubExp );
                }
                return Problem($"QRZ subscription is expired as of {expDate:yyyy-MM-dd}.", statusCode: (int)HttpStatusCode.Forbidden);
            }
            
            if (callInfo.Session.Any(x => !string.IsNullOrEmpty(x.Error)))
            {
                foreach (var error in callInfo.Session.Where(x => !string.IsNullOrEmpty(x.Error)))
                {
                    _logger.LogError("Session error: {Error}", error.Error);
                }
                return Problem(callInfo.Session.Select(x => x.Error).ToString());
            }
            
            if (callInfo.Callsign.Length > 0 && callInfo.Callsign[0] != null && callInfo.Callsign[0].grid != null)
            {
                _logger.LogDebug("QRZ call information: {CallInfo}", callInfo.Callsign.ToString());
                if (string.IsNullOrEmpty(contactInfo.DxGrid) || 
                    callInfo.Callsign[0].grid.StartsWith(contactInfo.DxGrid))
                {
                    //Use the lookup grid as it will be more accurate
                    contactInfo.DxGrid = callInfo.Callsign[0].grid;
                }
            }
            else
            {
                _logger.LogError("Qrz returned no error, but Callsign info is missing or grid is null");
            }
            
            //If the call fails, just use the input grids
        }

        if (contactInfo.DeGrid == string.Empty || contactInfo.DxGrid == string.Empty)
        {
            //Nothing to work with here since one or both grids are empty, so just return the input.
            return Ok(contactInfo);
        }
        
        var start = MaidenheadLocator.LocatorToLatLng(contactInfo.DeGrid);
        var end = MaidenheadLocator.LocatorToLatLng(contactInfo.DxGrid);
        
        //Update our bearing
        contactInfo.Bearing = (Int32)Math.Round(MaidenheadLocator.Azimuth(start, end), 0, MidpointRounding.AwayFromZero);

        return Ok(contactInfo);

    }
}