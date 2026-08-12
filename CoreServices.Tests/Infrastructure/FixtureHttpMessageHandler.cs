using System.Net;
using System.Net.Http.Headers;

namespace CoreServices.Tests.Infrastructure;

/// <summary>
/// Returns a fixed HTTP response without contacting an external service.
/// </summary>
internal sealed class FixtureHttpMessageHandler(string responseBody) : HttpMessageHandler
{
    /// <summary>
    /// Creates the configured successful response.
    /// </summary>
    /// <param name="request">The outbound request intercepted by the handler.</param>
    /// <param name="cancellationToken">The token that can cancel the operation.</param>
    /// <returns>A completed task containing the configured response.</returns>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            }
        });
}
