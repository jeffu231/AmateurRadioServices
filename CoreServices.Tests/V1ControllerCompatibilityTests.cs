using CoreServices.Controllers;
using CoreServices.Model;
using CoreServices.Model.Aprs;
using CoreServices.Model.Qrz;
using CoreServices.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies compatibility and safe validation behavior for the v1 controllers.
/// </summary>
public sealed class V1ControllerCompatibilityTests
{
    /// <summary>
    /// Rejects repeated APRS identifiers before an upstream request is made.
    /// </summary>
    [Fact]
    public async Task GetGrid_WhenIdentifiersRepeat_ReturnsValidationProblem()
    {
        // Arrange
        var controller = new AprsController(NullLogger<AprsController>.Instance, new StubAprsClient(null));

        // Act
        var result = await controller.GetGrid("K1ABC,k1abc", CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(badRequest.Value);
    }

    /// <summary>
    /// Rejects provider coordinates outside the valid latitude and longitude ranges.
    /// </summary>
    [Fact]
    public async Task GetGrid_WhenProviderCoordinatesAreInvalid_ReturnsBadGateway()
    {
        // Arrange
        var record = new AprsLocRecord
        {
            Found = 1,
            Entries = [new AprsEntry { Name = "K1ABC", Lat = 91, Lng = 0 }]
        };
        var controller = new AprsController(NullLogger<AprsController>.Instance, new StubAprsClient(record));

        // Act
        var result = await controller.GetGrid("K1ABC", CancellationToken.None);

        // Assert
        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
    }

    /// <summary>
    /// Returns a copied v1 contact value and normalizes valid grid values.
    /// </summary>
    [Fact]
    public async Task EnhanceBearing_WhenLookupIsNotNeeded_ReturnsCopiedNormalizedContact()
    {
        // Arrange
        var input = new ContactInfo { DeGrid = "fn31", DxGrid = "dm04" };
        var controller = new ContactController(
            new StubQrzClient(new QRZDatabase()),
            NullLogger<ContactController>.Instance);

        // Act
        var result = await controller.EnhanceBearing(input, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ContactInfo>(ok.Value);
        Assert.NotSame(input, response);
        Assert.Equal("fn31", input.DeGrid);
        Assert.Equal("FN31", response.DeGrid);
        Assert.Equal("DM04", response.DxGrid);
    }

    /// <summary>
    /// Rejects malformed non-empty contact grid values.
    /// </summary>
    [Fact]
    public async Task EnhanceBearing_WhenGridIsMalformed_ReturnsValidationProblem()
    {
        // Arrange
        var controller = new ContactController(
            new StubQrzClient(new QRZDatabase()),
            NullLogger<ContactController>.Instance);

        // Act
        var result = await controller.EnhanceBearing(
            new ContactInfo { DeGrid = "not-a-grid" },
            CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(badRequest.Value);
    }

    /// <summary>
    /// Adds deprecation and alternate-route headers to the legacy path route.
    /// </summary>
    [Fact]
    public async Task GetCallDataByCallsign_WhenUsingLegacyPath_AddsDeprecationHeaders()
    {
        // Arrange
        var controller = new CallsignController(new StubQrzClient(new QRZDatabase()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        // Act
#pragma warning disable CS0618 // The legacy route is intentionally tested for its compatibility headers.
        await controller.GetCallDataByCallsign("K1ABC", CancellationToken.None);
#pragma warning restore CS0618

        // Assert
        Assert.Equal("true", controller.Response.Headers["Deprecation"].ToString());
        Assert.Contains("rel=\"alternate\"", controller.Response.Headers["Link"].ToString(), StringComparison.Ordinal);
    }
}
