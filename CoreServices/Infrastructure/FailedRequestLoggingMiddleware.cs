using System.Text;

namespace CoreServices.Infrastructure;

/// <summary>
/// Logs the complete client input for every failed API request.
/// </summary>
public sealed class FailedRequestLoggingMiddleware(RequestDelegate next, ILogger<FailedRequestLoggingMiddleware> logger)
{
    /// <summary>
    /// Executes the next middleware and logs client input when the response is unsuccessful.
    /// </summary>
    /// <param name="context">The HTTP request context.</param>
    /// <returns>A task that represents the middleware operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestBody = await ReadRequestBodyAsync(context.Request, context.RequestAborted).ConfigureAwait(false);
        await next(context).ConfigureAwait(false);

        if (context.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            logger.LogWarning(
                "Failed API request {Method} {Path}{QueryString} with content type {ContentType} and body {RequestBody} returned {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                context.Request.ContentType ?? string.Empty,
                requestBody,
                context.Response.StatusCode);
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is 0 || request.Body == Stream.Null)
        {
            return string.Empty;
        }

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        request.Body.Position = 0;
        return body;
    }
}
