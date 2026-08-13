using System.Net;
using CoreServices.Integrations.Aprs;
using CoreServices.Model.Aprs;
using CoreServices.Services;
using CoreServices.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies cancellation behavior for APRS provider calls.
/// </summary>
public sealed class AprsServiceReliabilityTests
{
    /// <summary>
    /// Converts provider HTTP failures to a non-sensitive v1 failure record.
    /// </summary>
    /// <param name="statusCode">The upstream status code to simulate.</param>
    [Theory]
    [InlineData(401)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task GetAprsLocRecordAsync_WhenProviderReturnsFailure_ReturnsSanitizedFailureRecord(int statusCode)
    {
        // Arrange
        using var client = new HttpClient(new DelegatingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage((HttpStatusCode)statusCode))))
        {
            BaseAddress = new Uri("https://aprs.test")
        };
        var service = new AprsService(
            client,
            Options.Create(new AprsOptions { ApiKey = "test-aprs-key" }),
            NullLogger<AprsService>.Instance);

        // Act
        var response = await service.GetAprsLocRecordAsync("K1ABC", CancellationToken.None);

        // Assert
        var record = Assert.IsType<AprsLocRecord>(response);
        Assert.Equal("fail", record.Result);
        Assert.Equal("APRS location lookup is unavailable.", record.Description);
    }

    /// <summary>
    /// Propagates cancellation rather than converting a cancelled caller request into a provider response.
    /// </summary>
    [Fact]
    public async Task GetAprsLocRecordAsync_WhenCallerCancels_PropagatesCancellation()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        using var client = new HttpClient(new DelegatingHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage();
        }))
        {
            BaseAddress = new Uri("https://aprs.test")
        };
        var service = new AprsService(
            client,
            Options.Create(new AprsOptions { ApiKey = "test-aprs-key" }),
            NullLogger<AprsService>.Instance);

        // Act
        var operation = service.GetAprsLocRecordAsync("K1ABC", cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
    }
}
