using System.Text.Json;
using CoreServices.Integrations.Aprs;
using CoreServices.Model.Aprs;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace CoreServices.Services;

public class AprsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    private readonly string _aprsApiKey;
    private readonly ILogger<AprsService> _logger;
    private readonly HttpClient _httpClient;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AprsService"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record sanitized provider events.</param>
    /// <param name="options">The validated APRS provider configuration.</param>
    /// <param name="httpClient">The client used to call the APRS provider.</param>
    public AprsService(ILogger<AprsService> logger, IOptions<AprsOptions> options, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);

        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseAddress);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, options.Value.UserAgent);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
        _aprsApiKey = options.Value.ApiKey;
    }
    
    /// <summary>
    /// Makes a lookup to aprs.fi to find location info for the given call
    /// </summary>
    /// <param name="id">Call identifier used in APRS packets</param>
    /// <returns>A location record. <see cref="AprsLocRecord"/></returns>
    public async Task<AprsLocRecord?> GetAprsLocRecordAsync(string id)
    {
        var query = new Dictionary<string, string?>
        { 
            ["what"] = "loc",
            ["format"] = "json",
            ["name"] = id,
            ["apikey"] = _aprsApiKey
        };

        try
        {
            string fullUrl = QueryHelpers.AddQueryString("/api/get", query);
            var response = await _httpClient.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                
                AprsLocRecord? record = DeserializeFromStream<AprsLocRecord>(contentStream);
                if (record != null)
                {
                    record.Description = "Success";
                }
                return record;
            }
            
            _logger.LogError("Response code {Code}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed trying APRS");
            return CreateError(ex.Message);
        }

        return CreateError("Unknown Error");
    }
    
    private AprsLocRecord CreateError(string message)
    {
        return new AprsLocRecord()
        {
            Found = 0,
            Command = "loc",
            Result = "fail",
            Description = message
        };
    }
    
    private static T? DeserializeFromStream<T>(Stream stream)
    {
        return JsonSerializer.Deserialize<T>(stream, JsonOptions);
    }
}
