using System.ComponentModel.DataAnnotations;

namespace CoreServices.Integrations.Aprs;

/// <summary>
/// Defines the validated configuration required to call the APRS provider.
/// </summary>
public sealed class AprsOptions
{
    /// <summary>
    /// Gets or sets the APRS provider base address.
    /// </summary>
    [Required]
    [Url]
    public string BaseAddress { get; set; } = "https://api.aprs.fi";

    /// <summary>
    /// Gets or sets the API key used to authenticate with the APRS provider.
    /// </summary>
    [Required]
    [RegularExpression(@".*\S.*")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user agent sent to the APRS provider.
    /// </summary>
    [Required]
    [RegularExpression(@".*\S.*")]
    public string UserAgent { get; set; } = "www.k9kld.org";

    /// <summary>
    /// Gets or sets the maximum duration in seconds allowed for an APRS request.
    /// </summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the maximum upstream response size accepted from APRS.
    /// </summary>
    [Range(1_024, 10_485_760)]
    public int ResponseSizeLimitBytes { get; set; } = 1_048_576;
}
