using System.Text;
using CoreServices.Infrastructure;
using CoreServices.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies that failed requests include their complete replayable input in logs.
/// </summary>
public sealed class FailedRequestLoggingMiddlewareTests
{
    /// <summary>
    /// Logs method, path, query, content type, and body for a failed response.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenResponseFails_LogsCompleteRequestInput()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/ars/v2/contacts/enhance-bearing";
        context.Request.QueryString = new QueryString("?source=test-client");
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"dxCall\":\"N9NOC/P\"}"));
        var logger = new CollectingLogger<FailedRequestLoggingMiddleware>();
        var middleware = new FailedRequestLoggingMiddleware(async requestContext =>
        {
            requestContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await Task.CompletedTask;
        }, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("POST", entry.Message, StringComparison.Ordinal);
        Assert.Contains("source=test-client", entry.Message, StringComparison.Ordinal);
        Assert.Contains("N9NOC/P", entry.Message, StringComparison.Ordinal);
        Assert.Equal(0, context.Request.Body.Position);
    }
}
