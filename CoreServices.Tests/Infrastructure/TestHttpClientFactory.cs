namespace CoreServices.Tests.Infrastructure;

/// <summary>
/// Supplies a deterministic HTTP client to provider tests.
/// </summary>
internal sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    /// <inheritdoc />
    public HttpClient CreateClient(string name) => client;
}
