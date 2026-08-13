namespace CoreServices.Application;

/// <summary>
/// Identifies the non-sensitive category of a provider operation failure.
/// </summary>
public enum ProviderFailureKind
{
    /// <summary>
    /// Indicates that the request was invalid before a provider call was made.
    /// </summary>
    InvalidRequest,

    /// <summary>
    /// Indicates that the requested resource was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Indicates that provider authentication or subscription validation failed.
    /// </summary>
    Authentication,

    /// <summary>
    /// Indicates that a local or upstream quota was exceeded.
    /// </summary>
    RateLimited,

    /// <summary>
    /// Indicates that the provider did not respond before the configured timeout.
    /// </summary>
    Timeout,

    /// <summary>
    /// Indicates that the provider is unavailable.
    /// </summary>
    Unavailable,

    /// <summary>
    /// Indicates that the provider returned an invalid payload.
    /// </summary>
    InvalidPayload,

    /// <summary>
    /// Indicates an unexpected provider failure.
    /// </summary>
    Unexpected
}
