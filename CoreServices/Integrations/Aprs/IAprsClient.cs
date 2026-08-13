using CoreServices.Model.Aprs;

namespace CoreServices.Integrations.Aprs;

/// <summary>
/// Defines APRS location lookups performed by the application.
/// </summary>
public interface IAprsClient
{
    /// <summary>
    /// Gets APRS location data for one or more callsigns.
    /// </summary>
    /// <param name="id">The comma-separated APRS callsigns.</param>
    /// <param name="cancellationToken">The token that can cancel the operation.</param>
    /// <returns>A provider response record, or <see langword="null"/> when no record is produced.</returns>
    Task<AprsLocRecord?> GetAprsLocRecordAsync(string id, CancellationToken cancellationToken);
}
