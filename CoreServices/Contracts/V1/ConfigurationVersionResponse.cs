namespace CoreServices.Contracts.V1;

/// <summary>
/// Represents the version of the running API application.
/// </summary>
public sealed record ConfigurationVersionResponse
{
    /// <summary>
    /// Gets the version reported by the application entry assembly.
    /// </summary>
    public required string? ApplicationVersion { get; init; }
}
