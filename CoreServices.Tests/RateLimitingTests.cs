using System.Net;
using CoreServices.Tests.Infrastructure;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies that public endpoints enforce the configured quota protection.
/// </summary>
public sealed class RateLimitingTests
{
    /// <summary>
    /// Does not apply a request limit when a private deployment disables rate limiting.
    /// </summary>
    [Fact]
    public async Task GetVersion_WhenRateLimitingIsDisabled_RemainsAvailable()
    {
        // Arrange
        using var factory = new PrivateApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var firstResponse = await client.GetAsync("/api/ars/v1/configuration/version");
        var secondResponse = await client.GetAsync("/api/ars/v1/configuration/version");

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    /// <summary>
    /// Returns a retryable response when a direct client exceeds its configured request limit.
    /// </summary>
    [Fact]
    public async Task GetVersion_WhenClientExceedsLimit_ReturnsRetryableTooManyRequests()
    {
        // Arrange
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var firstResponse = await client.GetAsync("/api/ars/v1/configuration/version");
        var limitedResponse = await client.GetAsync("/api/ars/v1/configuration/version");

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
        Assert.Equal("60", limitedResponse.Headers.RetryAfter?.Delta?.TotalSeconds.ToString("0"));
        Assert.Equal("application/problem+json", limitedResponse.Content.Headers.ContentType?.MediaType);
    }
}
