using CoreServices.Model.Aprs;
using CoreServices.Model.Qrz;

namespace CoreServices.Application;

/// <summary>
/// Classifies sanitized v1 provider failure models for v2 HTTP responses.
/// </summary>
internal static class ProviderFailureClassifier
{
    /// <summary>
    /// Gets the failure category represented by an APRS response.
    /// </summary>
    /// <param name="record">The APRS response.</param>
    /// <returns>The failure category, or <see langword="null"/> for a successful response.</returns>
    public static ProviderFailureKind? FromAprs(AprsLocRecord? record)
    {
        if (record is null)
        {
            return ProviderFailureKind.Unavailable;
        }

        if (record.Found > 0)
        {
            return null;
        }

        return record.Description switch
        {
            var description when description.Contains("timed out", StringComparison.OrdinalIgnoreCase) => ProviderFailureKind.Timeout,
            var description when description.Contains("invalid payload", StringComparison.OrdinalIgnoreCase) => ProviderFailureKind.InvalidPayload,
            var description when description.Contains("unavailable", StringComparison.OrdinalIgnoreCase) => ProviderFailureKind.Unavailable,
            _ => ProviderFailureKind.NotFound
        };
    }

    /// <summary>
    /// Gets the failure category represented by a QRZ response.
    /// </summary>
    /// <param name="database">The QRZ response.</param>
    /// <returns>The failure category, or <see langword="null"/> for a successful response.</returns>
    public static ProviderFailureKind? FromQrz(QRZDatabase database)
    {
        if (database.Callsign is { Length: > 0 })
        {
            return null;
        }

        var error = database.Session?.FirstOrDefault()?.Error ?? string.Empty;
        return error switch
        {
            var value when string.IsNullOrWhiteSpace(value) => ProviderFailureKind.NotFound,
            var value when value.Contains("timed out", StringComparison.OrdinalIgnoreCase) => ProviderFailureKind.Timeout,
            var value when value.Contains("invalid payload", StringComparison.OrdinalIgnoreCase) => ProviderFailureKind.InvalidPayload,
            var value when value.Contains("unavailable", StringComparison.OrdinalIgnoreCase) => ProviderFailureKind.Unavailable,
            _ => ProviderFailureKind.Unavailable
        };
    }
}
