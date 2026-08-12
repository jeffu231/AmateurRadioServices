using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies that the application produces its versioned OpenAPI document.
/// </summary>
public sealed class SwaggerDocumentTests(WebApplicationFactory<ApiEntryPoint> factory) : IClassFixture<WebApplicationFactory<ApiEntryPoint>>
{
    /// <summary>
    /// Serves the v1 OpenAPI document from the configured Swagger route.
    /// </summary>
    [Fact]
    public async Task GetV1SwaggerDocument_ReturnsSuccess()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/ars/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
