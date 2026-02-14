using System.Text.Json;
using CoreServices.Model.Aprs;
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
    
    public AprsService(ILogger<AprsService> logger, IConfiguration config, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.aprs.fi");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "www.k9kld.org");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
        _aprsApiKey = config["AprsApiKey"] ?? String.Empty;
    }
    
    /// <summary>
    /// Makes a lookup to aprs.fi to find location info for the given call
    /// </summary>
    /// <param name="id">Call identifier used in APRS packets</param>
    /// <returns>A location record. <see cref="AprsLocRecord"/></returns>
    public async Task<AprsLocRecord?> GetAprsLocRecordAsync(string id)
    {
        var query = new Dictionary<string, string>
        { 
            ["what"] = "loc",
            ["format"] = "json",
            ["name"] = id,
            ["apikey"] = _aprsApiKey
        };

        try
        {
            HttpContent content = new FormUrlEncodedContent(query);
            var response = await _httpClient.PostAsync("/api/get", content);

            if (response.IsSuccessStatusCode)
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                
                AprsLocRecord? record = DeserializeFromStream<AprsLocRecord>(contentStream);
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