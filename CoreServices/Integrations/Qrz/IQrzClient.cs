using CoreServices.Model.Qrz;

namespace CoreServices.Integrations.Qrz;

/// <summary>
/// Defines QRZ callsign lookups performed by the application.
/// </summary>
public interface IQrzClient
{
    /// <summary>
    /// Gets QRZ data for a callsign.
    /// </summary>
    /// <param name="call">The callsign to look up.</param>
    /// <param name="cancellationToken">The token that can cancel the operation.</param>
    /// <returns>The QRZ response model used by the existing v1 contract.</returns>
    Task<QRZDatabase> GetCallDataAsync(string call, CancellationToken cancellationToken);
}
