namespace CoreServices.Tests.Infrastructure;

/// <summary>
/// Handles outbound HTTP requests with a test-supplied asynchronous delegate.
/// </summary>
internal sealed class DelegatingHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) => sendAsync(request, cancellationToken);
}
