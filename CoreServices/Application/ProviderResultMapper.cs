using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Application;

/// <summary>
/// Maps provider results to non-sensitive HTTP responses for versioned API contracts.
/// </summary>
public static class ProviderResultMapper
{
    /// <summary>
    /// Maps a provider result to a successful value or a standard Problem Details response.
    /// </summary>
    /// <typeparam name="TValue">The successful response type.</typeparam>
    /// <param name="controller">The controller producing the response.</param>
    /// <param name="result">The provider operation result.</param>
    /// <returns>An HTTP action result.</returns>
    public static ActionResult<TValue> ToActionResult<TValue>(ControllerBase controller, ProviderResult<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess && result.Value is not null)
        {
            return controller.Ok(result.Value);
        }

        var (statusCode, title) = result.FailureKind switch
        {
            ProviderFailureKind.InvalidRequest => (StatusCodes.Status400BadRequest, "The request is invalid."),
            ProviderFailureKind.NotFound => (StatusCodes.Status404NotFound, "The requested resource was not found."),
            ProviderFailureKind.RateLimited => (StatusCodes.Status429TooManyRequests, "The provider quota is temporarily exhausted."),
            ProviderFailureKind.InvalidPayload => (StatusCodes.Status502BadGateway, "The provider returned an invalid response."),
            ProviderFailureKind.Timeout => (StatusCodes.Status504GatewayTimeout, "The provider request timed out."),
            ProviderFailureKind.Authentication or ProviderFailureKind.Unavailable =>
                (StatusCodes.Status503ServiceUnavailable, "The provider is temporarily unavailable."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title
        };
        problem.Extensions["traceId"] = controller.ControllerContext?.HttpContext?.TraceIdentifier
            ?? System.Diagnostics.Activity.Current?.Id
            ?? string.Empty;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }
}
