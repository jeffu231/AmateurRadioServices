using System.Xml.Serialization;
using CoreServices.Model.Qrz;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace CoreServices.Services;

public class QrzDataService
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _agent;
    private static string _sessionToken = String.Empty;
    private static DateTime _subExpirationTime = DateTime.MinValue;
    private readonly HttpClient _httpClient;
    private static readonly XmlSerializer QrzDatabaseSerializer = new(typeof(QRZDatabase));
    private readonly ILogger<QrzDataService> _logger;
    private readonly IConfiguration _config;

    public QrzDataService(HttpClient httpClient, ILogger<QrzDataService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        _username = _config["QrzUsername"] ?? String.Empty;
        _password = _config["QrzPassword"] ?? String.Empty;
        _agent = _config["AgentIdentifier"] ?? "ARUv1.0";
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://xmldata.qrz.com");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mozilla/5.0");
    }
    
    public DateTime SubscriptionExpirationTime => _subExpirationTime;
    
    public bool IsSessionActive => _sessionToken != String.Empty;

    internal async Task<(bool success, QRZDatabase database)> CreateSessionAsync()
    {
        _logger.LogDebug("Creating session token");
        var query = new Dictionary<string, string>
        {
            ["username"] = _username,
            ["password"] = _password,
            ["agent"] = _agent
        };
        _logger.LogInformation("Agent = {}", _config["AgentIdentifier"] ?? String.Empty);
        try
        {
            HttpContent content = new FormUrlEncodedContent(query);
            var response = await _httpClient.PostAsync("/xml/current", content);

            var xml = await response.Content.ReadAsStringAsync();
            _logger.LogDebug(xml);
            
            using StringReader reader = new StringReader(xml);
            if (QrzDatabaseSerializer.Deserialize(reader) is QRZDatabase qrzDatabase)
            {
                if(qrzDatabase.Session != null && qrzDatabase.Session[0].Key != null)
                {
                    _sessionToken = qrzDatabase.Session[0].Key;

                    if (DateTime.TryParseExact(qrzDatabase.Session[0].SubExp, "ddd MMM d HH:mm:ss yyyy",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out var expDate))
                    {
                        _subExpirationTime = expDate;
                        _logger.LogInformation("QRZ sub expires {SubExp}", _subExpirationTime);
                    }
                    else
                    {
                        _logger.LogError("Could not parse QRZ sub expired for {SubExp}", qrzDatabase.Session[0].SubExp);
                    }
                    
                    return (true, qrzDatabase);
                }
                _sessionToken = string.Empty;
                return (false, qrzDatabase);
            }
            
            _logger.LogError(@"Failed create session for QRZ");
            return (false, CreateError("Error deserializing session", string.Empty));
        }
        catch (Exception ex)
        {
            _logger.LogError(@"Failed create session for QRZ");
            return (false, CreateError("Error creating session", ex.Message));
        }
    }

    private async Task<(bool, QRZDatabase?)> ValidateSessionAsync()
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            var status = await CreateSessionAsync();
            if (status.Item1 == false)
            {
                _logger.LogError("Failed to create QRZ session");
                return status;
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Gets the Call data information from QRZ for the given call
    /// </summary>
    /// <param name="call">Valid callsign to lookup</param>
    /// <returns><see cref="QRZDatabase"/> record</returns>
    public async Task<QRZDatabase> GetCallDataAsync(string call)
    {
        var sessionStatus = await ValidateSessionAsync();
        if (sessionStatus.Item1 == false)
        {
            return sessionStatus.Item2 ?? CreateError("Error validating session", String.Empty);
        }
        
        //Strip any /R or /P qualifiers on the end of the call.
        call = call.ToUpper()
            .Replace("/R", string.Empty)
            .Replace("/P", string.Empty);
        
        var tryCount = 0;

        while (true)
        {
            tryCount++;
            var keys = new Dictionary<string, string?>
            {
                ["s"] = _sessionToken,
                ["callsign"] = call
            };
            try
            {
                var response = await _httpClient.GetAsync(QueryHelpers.AddQueryString("/xml/current", keys));
               
                var xml = await response.Content.ReadAsStringAsync();
                _logger.LogDebug(xml);
                
                using StringReader reader = new StringReader(xml);
                if (QrzDatabaseSerializer.Deserialize(reader) is QRZDatabase qrzDatabase)
                {
                    if (qrzDatabase.Session != null)
                    {
                        if (!string.IsNullOrEmpty(qrzDatabase.Session[0].Key))
                        {
                            //We got a good response since the key is populated.
                            return qrzDatabase;
                        }
                        //Session is probably expired, so recreate it and try once again.
                        _sessionToken = String.Empty;
                        if (tryCount < 2)
                        {
                            await CreateSessionAsync();
                            continue;
                        }
                        return qrzDatabase;
                    }
                }
               
                _logger.LogError(@"Failed to deserialize response from QRZ call search");
                return CreateError("Call lookup failed.", @"Failed to deserialize response from QRZ call search" );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,@"Error calling QRZ call search");
                return CreateError("Error calling QRZ call search", ex.Message);
            }
        }
    }

    private QRZDatabase CreateError(string error, string message)
    {
        return new QRZDatabase
        {
            Session = new[] { new QRZDatabaseSession { Error = error, Message = message} }
        };
    }
}