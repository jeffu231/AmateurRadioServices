using System.Text.Json;
using CoreServices.Integrations.Aprs;
using CoreServices.Model.Aprs;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace CoreServices.Services;

/// <summary>
/// Performs APRS location lookups while preserving the existing v1 response model.
/// </summary>
public sealed class AprsService(
    HttpClient httpClient,
    IOptions<AprsOptions> options,
    ILogger<AprsService> logger) : IAprsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public async Task<AprsLocRecord?> GetAprsLocRecordAsync(string id, CancellationToken cancellationToken)
    {
        var query = QueryHelpers.AddQueryString("/api/get", new Dictionary<string, string?>
        {
            ["what"] = "loc",
            ["format"] = "json",
            ["name"] = id,
            ["apikey"] = options.Value.ApiKey
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, query);

        try
        {
            using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("APRS location lookup returned HTTP status {StatusCode}", (int)response.StatusCode);
                return CreateError("APRS location lookup is unavailable.");
            }

            await response.Content.LoadIntoBufferAsync(options.Value.ResponseSizeLimitBytes, cancellationToken)
                .ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var record = await JsonSerializer.DeserializeAsync<AprsLocRecord>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (record is not null)
            {
                record.Description = "Success";
                return record;
            }

            logger.LogWarning("APRS location lookup returned an invalid payload");
            return CreateError("APRS location lookup returned an invalid payload.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "APRS location lookup failed");
            return CreateError("APRS location lookup is unavailable.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("APRS location lookup timed out");
            return CreateError("APRS location lookup timed out.");
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "APRS location lookup response was invalid or too large");
            return CreateError("APRS location lookup returned an invalid payload.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "APRS location lookup returned an invalid payload");
            return CreateError("APRS location lookup returned an invalid payload.");
        }
    }

    private static AprsLocRecord CreateError(string message) => new()
    {
        Found = 0,
        Command = "loc",
        Result = "fail",
        Description = message
    };
}
