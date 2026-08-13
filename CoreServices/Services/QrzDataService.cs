using System.Xml.Serialization;
using CoreServices.Integrations.Qrz;
using CoreServices.Model.Qrz;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace CoreServices.Services;

/// <summary>
/// Performs QRZ callsign lookups while preserving the existing v1 response model.
/// </summary>
public sealed class QrzDataService(
    HttpClient httpClient,
    IQrzSessionProvider sessionProvider,
    IOptions<QrzOptions> options,
    ILogger<QrzDataService> logger) : IQrzClient
{
    private static readonly XmlSerializer Serializer = new(typeof(QRZDatabase));

    /// <inheritdoc />
    public async Task<QRZDatabase> GetCallDataAsync(string call, CancellationToken cancellationToken)
    {
        var normalizedCall = call.Trim().ToUpperInvariant();
        var database = await GetCallDataWithSessionRetryAsync(normalizedCall, cancellationToken).ConfigureAwait(false);
        if (IsLookupSuccessful(database))
        {
            return database;
        }

        var fallbackCall = GetFallbackCallsign(normalizedCall);
        return fallbackCall is null
            ? database
            : await GetCallDataWithSessionRetryAsync(fallbackCall, cancellationToken).ConfigureAwait(false);
    }

    private async Task<QRZDatabase> GetCallDataWithSessionRetryAsync(string call, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var session = await sessionProvider.GetSessionAsync(cancellationToken).ConfigureAwait(false);
            if (session is null)
            {
                return CreateError("QRZ session is unavailable.");
            }

            var database = await QueryCallDataAsync(call, session.Token, cancellationToken).ConfigureAwait(false);
            if (!RequiresSessionRefresh(database) || attempt == 1)
            {
                return database;
            }

            sessionProvider.InvalidateSession();
        }

        return CreateError("QRZ callsign lookup failed.");
    }

    private async Task<QRZDatabase> QueryCallDataAsync(string call, string sessionToken, CancellationToken cancellationToken)
    {
        var query = QueryHelpers.AddQueryString("/xml/current", new Dictionary<string, string?>
        {
            ["s"] = sessionToken,
            ["callsign"] = call
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, query);

        try
        {
            using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("QRZ callsign lookup returned HTTP status {StatusCode}", (int)response.StatusCode);
                return CreateError("QRZ callsign lookup is unavailable.");
            }

            await response.Content.LoadIntoBufferAsync(options.Value.ResponseSizeLimitBytes, cancellationToken)
                .ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            if (Serializer.Deserialize(stream) is QRZDatabase database)
            {
                return database;
            }

            logger.LogWarning("QRZ callsign lookup returned an invalid payload");
            return CreateError("QRZ callsign lookup returned an invalid payload.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "QRZ callsign lookup failed");
            return CreateError("QRZ callsign lookup is unavailable.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("QRZ callsign lookup timed out");
            return CreateError("QRZ callsign lookup timed out.");
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "QRZ callsign lookup response was invalid or too large");
            return CreateError("QRZ callsign lookup returned an invalid payload.");
        }
    }

    private static bool IsLookupSuccessful(QRZDatabase database) => database.Callsign is { Length: > 0 };

    private static string? GetFallbackCallsign(string call) =>
        call.EndsWith("/R", StringComparison.Ordinal) || call.EndsWith("/P", StringComparison.Ordinal)
            ? call[..^2]
            : null;

    private static bool RequiresSessionRefresh(QRZDatabase database) =>
        database.Session is { Length: > 0 } sessions &&
        string.IsNullOrWhiteSpace(sessions[0].Key) &&
        string.IsNullOrWhiteSpace(sessions[0].Message);

    private static QRZDatabase CreateError(string error) => new()
    {
        Session = [new QRZDatabaseSession { Error = error }]
    };
}
