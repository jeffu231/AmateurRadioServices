using System.Net;
using Asp.Versioning;
using CoreServices.Model;
using CoreServices.Integrations.Qrz;
using CoreServices.Validation;
using MaidenheadLib;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/ars/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class ContactController(IQrzClient qrzClient, ILogger<ContactController> logger) : ControllerBase
{
    /// <summary>
    /// This operation tries to enhance the existing contact info by doing a lookup of the DxCall and if the DxGrid is
    /// missing, or the first 4 chars of the DxGrid match the lookup, the lookup grid is used. A new bearing will be
    /// calculated. If the contact info cannot be enhanced, the original information is returned.
    /// </summary>
    /// <param name="contactInfo">The contact information to enhance.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>The enhanced contact information or a validation response.</returns>
    [HttpPost("EnhanceBearing")]
    [MapToApiVersion("1.0")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ContactInfo), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<IActionResult> EnhanceBearing([FromBody] ContactInfo contactInfo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contactInfo);

        if (!TryCopyAndNormalize(contactInfo, out var response))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["grid"] = ["Grid locators must be valid four-, six-, or eight-character Maidenhead values."]
            }));
        }

        if (string.IsNullOrWhiteSpace(response.DxCall))
        {
            //Nothing to do since we don't have a call to lookup
            return Ok(response);
        }
        //Try to do a lookup on the call and see if we can improve the grid accuracy to 6 chars.
        var callInfo = await qrzClient.GetCallDataAsync(response.DxCall, cancellationToken);
        if (callInfo.Session != null && callInfo.Session.Length > 0)
        {
            var subExp = callInfo.Session[0].SubExp;
            if (!string.IsNullOrEmpty(subExp) &&
                DateTime.TryParseExact(subExp, "ddd MMM d HH:mm:ss yyyy", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var expDate) &&
                expDate < DateTime.UtcNow)
            {
                logger.LogError("QRZ subscription has expired as of {ExpDate:yyyy-MM-dd}", expDate);
                return Problem($"QRZ subscription is expired as of {expDate:yyyy-MM-dd}.", statusCode: (int)HttpStatusCode.Forbidden);
            }
            
            if (callInfo.Session.Any(x => !string.IsNullOrEmpty(x.Error)))
            {
                logger.LogError("QRZ session returned an error");
                return Problem(title: "QRZ callsign lookup is unavailable.", statusCode: (int)HttpStatusCode.ServiceUnavailable);
            }
            
            if (callInfo.Callsign.Length > 0 && callInfo.Callsign[0] != null && callInfo.Callsign[0].grid != null)
            {
                var lookupGrid = callInfo.Callsign[0].grid.ToUpperInvariant();
                if (MaidenheadGridValidator.IsValid(lookupGrid) &&
                    (string.IsNullOrEmpty(response.DxGrid) || MatchesFirstFourCharacters(lookupGrid, response.DxGrid)))
                {
                    //Use the lookup grid as it will be more accurate
                    response.DxGrid = lookupGrid;
                }
            }
            else
            {
                logger.LogError("QRZ callsign lookup returned no usable grid");
            }
            
            //If the call fails, just use the input grids
        }

        if (response.DeGrid == string.Empty || response.DxGrid == string.Empty)
        {
            //Nothing to work with here since one or both grids are empty, so just return the input.
            return Ok(response);
        }
        
        var start = MaidenheadLocator.LocatorToLatLng(response.DeGrid);
        var end = MaidenheadLocator.LocatorToLatLng(response.DxGrid);
        
        //Update our bearing
        response.Bearing = (int)Math.Round(MaidenheadLocator.Azimuth(start, end), 0, MidpointRounding.AwayFromZero);

        return Ok(response);

    }

    private static bool TryCopyAndNormalize(ContactInfo contactInfo, out ContactInfo response)
    {
        var deGrid = contactInfo.DeGrid?.Trim().ToUpperInvariant() ?? string.Empty;
        var dxGrid = contactInfo.DxGrid?.Trim().ToUpperInvariant() ?? string.Empty;
        response = new ContactInfo
        {
            DeCall = contactInfo.DeCall,
            DeGrid = deGrid,
            DxCall = contactInfo.DxCall,
            DxGrid = dxGrid,
            Bearing = contactInfo.Bearing
        };

        return (deGrid.Length == 0 || MaidenheadGridValidator.IsValid(deGrid)) &&
               (dxGrid.Length == 0 || MaidenheadGridValidator.IsValid(dxGrid));
    }

    private static bool MatchesFirstFourCharacters(string lookupGrid, string suppliedGrid) =>
        lookupGrid.Length >= 4 && suppliedGrid.Length >= 4 &&
        lookupGrid[..4].Equals(suppliedGrid[..4], StringComparison.Ordinal);
}
