using System.ComponentModel.DataAnnotations;

namespace CoreServices.Infrastructure;

/// <summary>
/// Defines the public API limits that protect upstream subscription quotas.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>
    /// Gets or sets a value that indicates whether API rate limiting is enforced.
    /// </summary>
    /// <value><see langword="true"/> to enforce the configured limits; otherwise, <see langword="false"/>.</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of requests permitted from one direct client in a window.
    /// </summary>
    [Range(1, 10_000)]
    public int PublicPermitLimit { get; set; } = 60;

    /// <summary>
    /// Gets or sets the fixed-window duration in seconds.
    /// </summary>
    [Range(1, 3_600)]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the number of requests allowed to wait for a public permit.
    /// </summary>
    [Range(0, 1_000)]
    public int QueueLimit { get; set; }

    /// <summary>
    /// Gets or sets the maximum concurrent requests allowed across the application.
    /// </summary>
    [Range(1, 1_000)]
    public int GlobalConcurrencyPermitLimit { get; set; } = 8;
}
