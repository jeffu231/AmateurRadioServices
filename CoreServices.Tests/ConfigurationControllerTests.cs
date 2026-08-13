using CoreServices.Contracts.V1;
using CoreServices.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies the configuration endpoint's declared v1 response shape.
/// </summary>
public sealed class ConfigurationControllerTests
{
    /// <summary>
    /// Returns the documented configuration version response.
    /// </summary>
    [Fact]
    public void GetVersion_ReturnsConfigurationVersionResponse()
    {
        // Arrange
        var controller = new ConfigurationController();

        // Act
        var result = controller.GetVersion();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ConfigurationVersionResponse>(ok.Value);
    }
}
