using System.Net;
using System.Text.Json;
using CoreServices.Tests.Infrastructure;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies the public v2 routes and their stable JSON contracts.
/// </summary>
public sealed class V2ApiIntegrationTests(V2ApiWebApplicationFactory factory) : IClassFixture<V2ApiWebApplicationFactory>
{
    /// <summary>
    /// Reports liveness without performing a provider lookup.
    /// </summary>
    [Fact]
    public async Task GetLiveness_ReturnsSuccess()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Reports readiness from local configuration without performing a provider lookup.
    /// </summary>
    [Fact]
    public async Task GetReadiness_ReturnsSuccess()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Serves the v2 configuration contract alongside v1.
    /// </summary>
    [Fact]
    public async Task GetVersion_ReturnsV2Contract()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/ars/v2/configuration/version");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("applicationVersion", out _));
    }

    /// <summary>
    /// Accepts a slash-bearing APRS callsign as a query value.
    /// </summary>
    [Fact]
    public async Task GetAprsLocations_WhenCallsignContainsSlash_ReturnsSuccess()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/ars/v2/aprs/locations?callsigns=N9NOC%2FP");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("N9NOC/P", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// Produces a v2 document without a QRZ session contract.
    /// </summary>
    [Fact]
    public async Task GetV2SwaggerDocument_DoesNotExposeSessionContract()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/ars/swagger/v2/swagger.json");
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("QRZDatabase", json, StringComparison.Ordinal);
        Assert.DoesNotContain("session", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/ars/v2/aprs/locations", json, StringComparison.Ordinal);
    }
}
