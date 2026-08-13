namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the running application version exposed by v2.
/// </summary>
public sealed record VersionResponse
{
    /// <summary>Gets the application version.</summary>
    public required string? ApplicationVersion { get; init; }
}
