using CoreServices.Application;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies stable HTTP mappings for provider failure categories.
/// </summary>
public sealed class ProviderResultMapperTests
{
    /// <summary>
    /// Maps an unavailable provider to a non-sensitive service-unavailable response.
    /// </summary>
    [Fact]
    public void ToActionResult_WhenProviderIsUnavailable_ReturnsServiceUnavailable()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = ProviderResultMapper.ToActionResult(controller, ProviderResult<string>.Failure(ProviderFailureKind.Unavailable));

        // Assert
        var response = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, response.StatusCode);
        Assert.Equal("The provider is temporarily unavailable.", Assert.IsType<ProblemDetails>(response.Value).Title);
    }

    private sealed class TestController : ControllerBase;
}
