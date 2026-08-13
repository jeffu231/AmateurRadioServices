using System.Globalization;
using System.Xml.Serialization;
using CoreServices.Model.Qrz;
using Microsoft.Extensions.Options;

namespace CoreServices.Integrations.Qrz;

/// <summary>
/// Stores and refreshes the QRZ session for one application process.
/// </summary>
public sealed class QrzSessionProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<QrzOptions> options,
    ILogger<QrzSessionProvider> logger) : IQrzSessionProvider, IDisposable
{
    private static readonly XmlSerializer Serializer = new(typeof(QRZDatabase));
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private QrzSession? _session;
    private bool _disposed;

    /// <inheritdoc />
    public async Task<QrzSession?> GetSessionAsync(CancellationToken cancellationToken)
    {
        var currentSession = Volatile.Read(ref _session);
        if (IsUsable(currentSession))
        {
            return currentSession;
        }

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            currentSession = Volatile.Read(ref _session);
            if (IsUsable(currentSession))
            {
                return currentSession;
            }

            var refreshedSession = await CreateSessionAsync(cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref _session, refreshedSession);
            return refreshedSession;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc />
    public void InvalidateSession() => Interlocked.Exchange(ref _session, null);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _refreshLock.Dispose();
        _disposed = true;
    }

    private async Task<QrzSession?> CreateSessionAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("qrz-session");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = options.Value.Username,
            ["password"] = options.Value.Password,
            ["agent"] = options.Value.AgentIdentifier
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/xml/current") { Content = content };
        using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("QRZ session request returned HTTP status {StatusCode}", (int)response.StatusCode);
            return null;
        }

        try
        {
            await response.Content.LoadIntoBufferAsync(options.Value.ResponseSizeLimitBytes, cancellationToken)
                .ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            if (Serializer.Deserialize(stream) is not QRZDatabase { Session: { Length: > 0 } sessions })
            {
                logger.LogWarning("QRZ session response had an invalid payload");
                return null;
            }

            var session = sessions[0];
            if (string.IsNullOrWhiteSpace(session.Key) ||
                !DateTimeOffset.TryParseExact(
                    session.SubExp,
                    "ddd MMM d HH:mm:ss yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var subscriptionExpiration))
            {
                logger.LogWarning("QRZ session response did not include a valid session or expiration");
                return null;
            }

            logger.LogInformation("QRZ session was refreshed successfully");
            return new QrzSession(session.Key, subscriptionExpiration);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "QRZ session request failed");
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("QRZ session request timed out");
            return null;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "QRZ session response was invalid or too large");
            return null;
        }
    }

    private static bool IsUsable(QrzSession? session) =>
        session is not null && session.SubscriptionExpiration > DateTimeOffset.UtcNow.Add(RefreshSkew);
}
