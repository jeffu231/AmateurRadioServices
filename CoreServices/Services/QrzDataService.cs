using System.Xml.Serialization;
using CoreServices.Integrations.Qrz;
using CoreServices.Model.Qrz;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
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
    /// <summary>
    /// Initializes a new instance of the <see cref="QrzDataService"/> class.
    /// </summary>
    /// <param name="httpClient">The client used to call the QRZ provider.</param>
    /// <param name="logger">The logger used to record sanitized provider events.</param>
    /// <param name="options">The validated QRZ provider configuration.</param>
    public QrzDataService(HttpClient httpClient, ILogger<QrzDataService> logger, IOptions<QrzOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _username = options.Value.Username;
        _password = options.Value.Password;
        _agent = options.Value.AgentIdentifier;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseAddress);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, options.Value.UserAgent);
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
        _logger.LogInformation("Creating QRZ session for configured agent {AgentIdentifier}", _agent);
        try
        {
            HttpContent content = new FormUrlEncodedContent(query);
            var response = await _httpClient.PostAsync("/xml/current", content);

            var xml = await response.Content.ReadAsStringAsync();
            
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
            
            _logger.LogError("QRZ session response could not be deserialized");
            return (false, CreateError("Error deserializing session", string.Empty));
        }
        catch (Exception)
        {
            _logger.LogError("QRZ session creation failed");
            return (false, CreateError("Error creating session", string.Empty));
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

        var normalizedCall = NormalizeCallsign(call);
        var database = await GetCallDataWithSessionRetryAsync(normalizedCall);
        if (IsLookupSuccessful(database))
        {
            return database;
        }

        var fallbackCall = GetFallbackCallsign(normalizedCall);
        return fallbackCall == null ? database : await GetCallDataWithSessionRetryAsync(fallbackCall);
    }

    private static string NormalizeCallsign(string call)
    {
        return call.Trim().ToUpperInvariant();
    }

    private static string? GetFallbackCallsign(string call)
    {
        if (call.EndsWith("/R") || call.EndsWith("/P"))
        {
            return call[..^2];
        }

        return null;
    }

    private async Task<QRZDatabase> GetCallDataWithSessionRetryAsync(string call)
    {
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var database = await QueryCallDataAsync(call);
            if (!ShouldRefreshSession(database) || attempt == maxAttempts)
            {
                return database;
            }

            _sessionToken = String.Empty;
            await CreateSessionAsync();
        }

        return CreateError("Call lookup failed.", "Failed to call QRZ call search");
    }

    private async Task<QRZDatabase> QueryCallDataAsync(string call)
    {
        var keys = new Dictionary<string, string?>
        {
            ["s"] = _sessionToken,
            ["callsign"] = call
        };

        try
        {
            var response = await _httpClient.GetAsync(QueryHelpers.AddQueryString("/xml/current", keys));
            var xml = await response.Content.ReadAsStringAsync();

            using StringReader reader = new StringReader(xml);
            if (QrzDatabaseSerializer.Deserialize(reader) is QRZDatabase qrzDatabase)
            {
                return qrzDatabase;
            }

            _logger.LogError(@"Failed to deserialize response from QRZ call search");
            return CreateError("Call lookup failed.", @"Failed to deserialize response from QRZ call search");
        }
        catch (Exception)
        {
            _logger.LogError("QRZ callsign lookup failed");
            return CreateError("Error calling QRZ call search", string.Empty);
        }
    }

    private static bool IsLookupSuccessful(QRZDatabase database)
    {
        return database.Callsign is { Length: > 0 };
    }

    private static bool ShouldRefreshSession(QRZDatabase database)
    {
        if (database.Session is not { Length: > 0 })
        {
            return false;
        }

        var session = database.Session[0];
        return string.IsNullOrEmpty(session.Key) && string.IsNullOrEmpty(session.Message);
    }

    private QRZDatabase CreateError(string error, string message)
    {
        return new QRZDatabase
        {
            Session = new[] { new QRZDatabaseSession { Error = error, Message = message} }
        };
    }
}
