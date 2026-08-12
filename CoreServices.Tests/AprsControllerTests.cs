using CoreServices.Controllers;
using CoreServices.Model.Aprs;
using CoreServices.Services;
using CoreServices.Tests.Infrastructure;
using MaidenheadLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies APRS controller behavior using deterministic provider responses.
/// </summary>
public sealed class AprsControllerTests
{
    /// <summary>
    /// Returns a distinct Maidenhead grid for every APRS record in a multi-record lookup.
    /// </summary>
    [Fact]
    public async Task GetGrid_ReturnsGridCalculatedFromEachEntry()
    {
        // Arrange
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "aprs-multiple-locations.json");
        var fixture = await File.ReadAllTextAsync(fixturePath);
        using var client = new HttpClient(new FixtureHttpMessageHandler(fixture));
        var service = new AprsService(
            NullLogger<AprsService>.Instance,
            new ConfigurationBuilder().Build(),
            client);
        var controller = new AprsController(NullLogger<AprsController>.Instance, service);

        // Act
        var result = await controller.GetGrid("K1AAA,K2BBB");

        // Assert
        var objectResult = Assert.IsType<OkObjectResult>(result);
        var grids = Assert.IsType<List<AprsGrid>>(objectResult.Value);
        Assert.Collection(
            grids,
            first =>
            {
                Assert.Equal("K1AAA", first.Name);
                Assert.Equal(MaidenheadLocator.LatLngToLocator(42.3601, -71.0589), first.Grid);
            },
            second =>
            {
                Assert.Equal("K2BBB", second.Name);
                Assert.Equal(MaidenheadLocator.LatLngToLocator(34.0522, -118.2437), second.Grid);
            });
    }
}
