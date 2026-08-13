using CoreServices.Contracts.V2;
using CoreServices.Integrations.Qrz;
using CoreServices.Model.Qrz;
using CoreServices.Validation;
using MaidenheadLib;

namespace CoreServices.Application;

/// <summary>
/// Enhances validated v2 contact data using QRZ grid information.
/// </summary>
public sealed class ContactEnhancer(IQrzClient qrzClient)
{
    /// <summary>
    /// Enhances a validated v2 contact request.
    /// </summary>
    /// <param name="request">The validated contact request.</param>
    /// <param name="cancellationToken">The token that can cancel the operation.</param>
    /// <returns>A provider result containing the immutable enhanced contact response.</returns>
    public async Task<ProviderResult<ContactEnhancementResponse>> EnhanceAsync(
        ContactEnhancementRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var deGrid = NormalizeGrid(request.DeGrid);
        var dxGrid = NormalizeGrid(request.DxGrid);
        if (!string.IsNullOrWhiteSpace(request.DxCall))
        {
            var callData = await qrzClient.GetCallDataAsync(request.DxCall.Trim(), cancellationToken).ConfigureAwait(false);
            var failure = ProviderFailureClassifier.FromQrz(callData);
            if (failure is not null)
            {
                return ProviderResult<ContactEnhancementResponse>.Failure(failure.Value);
            }

            dxGrid = SelectLookupGrid(callData, dxGrid) ?? dxGrid;
        }

        int? bearing = deGrid is not null && dxGrid is not null
            ? (int)Math.Round(MaidenheadLocator.Azimuth(
                MaidenheadLocator.LocatorToLatLng(deGrid),
                MaidenheadLocator.LocatorToLatLng(dxGrid)), 0, MidpointRounding.AwayFromZero)
            : null;

        return ProviderResult<ContactEnhancementResponse>.Success(new ContactEnhancementResponse
        {
            DeCall = request.DeCall,
            DeGrid = deGrid,
            DxCall = request.DxCall,
            DxGrid = dxGrid,
            Bearing = bearing
        });
    }

    private static string? NormalizeGrid(string? grid) => string.IsNullOrWhiteSpace(grid)
        ? null
        : grid.Trim().ToUpperInvariant();

    private static string? SelectLookupGrid(QRZDatabase callData, string? suppliedGrid)
    {
        var lookupGrid = callData.Callsign?.FirstOrDefault()?.grid?.Trim().ToUpperInvariant();
        return lookupGrid is not null && MaidenheadGridValidator.IsValid(lookupGrid) &&
               (suppliedGrid is null || SharesFirstFourCharacters(lookupGrid, suppliedGrid))
            ? lookupGrid
            : null;
    }

    private static bool SharesFirstFourCharacters(string first, string second) =>
        first.Length >= 4 && second.Length >= 4 && first[..4].Equals(second[..4], StringComparison.Ordinal);
}
