using System.Net;
using CoreServices.Integrations.Qrz;
using CoreServices.Services;
using CoreServices.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies QRZ session coordination and safe invalid-session retry behavior.
/// </summary>
public sealed class QrzSessionProviderTests
{
    /// <summary>
    /// Creates one QRZ session when multiple callers concurrently need a cold session.
    /// </summary>
    [Fact]
    public async Task GetSessionAsync_WhenConcurrentCallersNeedASession_AuthenticatesOnce()
    {
        // Arrange
        var authenticationCount = 0;
        using var client = CreateClient(async (_, cancellationToken) =>
        {
            Interlocked.Increment(ref authenticationCount);
            await Task.Delay(25, cancellationToken);
            return XmlResponse(CreateSessionXml("session-one"));
        });
        using var provider = CreateSessionProvider(client);

        // Act
        var sessions = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => provider.GetSessionAsync(CancellationToken.None)));

        // Assert
        Assert.Equal(1, authenticationCount);
        Assert.All(sessions, session => Assert.Equal("session-one", session?.Token));
    }

    /// <summary>
    /// Refreshes the QRZ session once and retries a safe lookup when QRZ rejects the old token.
    /// </summary>
    [Fact]
    public async Task GetCallDataAsync_WhenQrzRejectsSession_RefreshesAndRetriesWithoutEmptyToken()
    {
        // Arrange
        var authenticationCount = 0;
        var lookupTokens = new List<string?>();
        using var client = CreateClient((request, _) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                var token = Interlocked.Increment(ref authenticationCount) == 1 ? "session-one" : "session-two";
                return Task.FromResult(XmlResponse(CreateSessionXml(token)));
            }

            lookupTokens.Add(request.RequestUri?.Query.Split('&')
                .SingleOrDefault(value => value.TrimStart('?').StartsWith("s=", StringComparison.Ordinal))?
                .TrimStart('?')[2..]);
            return Task.FromResult(lookupTokens.Count == 1
                ? XmlResponse("<QRZDatabase xmlns=\"http://xmldata.qrz.com\"><Session><Error>Invalid session</Error></Session></QRZDatabase>")
                : XmlResponse("<QRZDatabase xmlns=\"http://xmldata.qrz.com\"><Callsign><call>K1ABC</call></Callsign></QRZDatabase>"));
        });
        using var provider = CreateSessionProvider(client);
        var service = new QrzDataService(
            client,
            provider,
            Options.Create(new QrzOptions()),
            NullLogger<QrzDataService>.Instance);

        // Act
        var response = await service.GetCallDataAsync("K1ABC", CancellationToken.None);

        // Assert
        Assert.Equal(2, authenticationCount);
        Assert.Equal(["session-one", "session-two"], lookupTokens);
        Assert.NotNull(response.Callsign);
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) => new(
        new DelegatingHttpMessageHandler(sendAsync))
    {
        BaseAddress = new Uri("https://qrz.test")
    };

    private static QrzSessionProvider CreateSessionProvider(HttpClient client) => new(
        new TestHttpClientFactory(client),
        Options.Create(new QrzOptions()),
        NullLogger<QrzSessionProvider>.Instance);

    private static HttpResponseMessage XmlResponse(string xml) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(xml)
    };

    private static string CreateSessionXml(string token) =>
        $"<QRZDatabase xmlns=\"http://xmldata.qrz.com\"><Session><Key>{token}</Key><SubExp>{DateTime.UtcNow.AddDays(30):ddd MMM d HH:mm:ss yyyy}</SubExp></Session></QRZDatabase>";
}
