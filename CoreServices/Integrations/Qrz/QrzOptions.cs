using System.ComponentModel.DataAnnotations;

namespace CoreServices.Integrations.Qrz;

/// <summary>
/// Defines the validated configuration required to call the QRZ provider.
/// </summary>
public sealed class QrzOptions
{
    /// <summary>
    /// Gets or sets the QRZ provider base address.
    /// </summary>
    [Required]
    [Url]
    public string BaseAddress { get; set; } = "https://xmldata.qrz.com";

    /// <summary>
    /// Gets or sets the QRZ account user name.
    /// </summary>
    [Required]
    [RegularExpression(@".*\S.*")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the QRZ account password.
    /// </summary>
    [Required]
    [RegularExpression(@".*\S.*")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the QRZ client identifier.
    /// </summary>
    [Required]
    [RegularExpression(@".*\S.*")]
    public string AgentIdentifier { get; set; } = "ARUv1.0";

    /// <summary>
    /// Gets or sets the user agent sent to the QRZ provider.
    /// </summary>
    [Required]
    [RegularExpression(@".*\S.*")]
    public string UserAgent { get; set; } = "Mozilla/5.0";

    /// <summary>
    /// Gets or sets the maximum duration in seconds allowed for a QRZ request.
    /// </summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the maximum upstream response size accepted from QRZ.
    /// </summary>
    [Range(1_024, 10_485_760)]
    public int ResponseSizeLimitBytes { get; set; } = 1_048_576;
}
