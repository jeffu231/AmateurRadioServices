using Asp.Versioning;
using CoreServices.Application;
using CoreServices.Contracts.V2;
using CoreServices.Validation;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers.V2;

/// <summary>
/// Provides stable v2 contact enhancement operations.
/// </summary>
[ApiController]
[Route("api/ars/v{version:apiVersion}/contacts")]
[ApiVersion("2.0")]
[Produces("application/json")]
public sealed class ContactController(ContactEnhancer contactEnhancer) : ControllerBase
{
    /// <summary>
    /// Enhances a contact with a QRZ grid and calculated bearing when available.
    /// </summary>
    /// <remarks>Blank grids retain a successful response with no bearing. Non-empty grids must use a four-, six-, or eight-character Maidenhead locator.</remarks>
    /// <param name="request">The contact enhancement request.</param>
    /// <param name="cancellationToken">The token that can cancel the request.</param>
    /// <returns>An immutable contact response or Problem Details.</returns>
    [HttpPost("enhance-bearing")]
    [ProducesResponseType(typeof(ContactEnhancementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ContactEnhancementResponse>> EnhanceBearingAsync(
        [FromBody] ContactEnhancementRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsValidRequest(request, out var errors))
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        var result = await contactEnhancer.EnhanceAsync(request, cancellationToken).ConfigureAwait(false);
        return ProviderResultMapper.ToActionResult(this, result);
    }

    private static bool IsValidRequest(ContactEnhancementRequest request, out Dictionary<string, string[]> errors)
    {
        errors = [];
        AddGridError(errors, "deGrid", request.DeGrid);
        AddGridError(errors, "dxGrid", request.DxGrid);
        if (request.DxCall is { } dxCall && (string.IsNullOrWhiteSpace(dxCall) || dxCall.Trim().Length > 16))
        {
            errors["dxCall"] = ["DxCall must be a non-empty callsign no longer than 16 characters when provided."];
        }

        return errors.Count == 0;
    }

    private static void AddGridError(Dictionary<string, string[]> errors, string name, string? grid)
    {
        if (!string.IsNullOrWhiteSpace(grid) && !MaidenheadGridValidator.IsValid(grid.Trim().ToUpperInvariant()))
        {
            errors[name] = ["Grid locators must be valid four-, six-, or eight-character Maidenhead values."];
        }
    }
}
