using CoreServices.Controllers;
using CoreServices.Integrations.Qrz;
using CoreServices.Model.Qrz;
using CoreServices.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies v2 callsign error and response mapping without provider transport details.
/// </summary>
public sealed class V2ControllerTests
{
    /// <summary>
    /// Returns a stable not-found problem for a QRZ lookup with no callsign data.
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenCallsignIsUnknown_ReturnsNotFoundProblem()
    {
        // Arrange
        IQrzClient qrzClient = new StubQrzClient(new QRZDatabase());
        var controller = new V2CallsignsController(qrzClient);

        // Act
        var result = await controller.GetAsync("UNKNOWN", CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(404, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }

}
