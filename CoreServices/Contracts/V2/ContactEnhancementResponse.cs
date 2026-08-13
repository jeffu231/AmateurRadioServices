namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents an immutable v2 contact-bearing enhancement result.
/// </summary>
public sealed record ContactEnhancementResponse
{
    /// <summary>Gets the operator's callsign.</summary>
    public string? DeCall { get; init; }

    /// <summary>Gets the normalized operator grid locator.</summary>
    public string? DeGrid { get; init; }

    /// <summary>Gets the distant station's callsign.</summary>
    public string? DxCall { get; init; }

    /// <summary>Gets the normalized distant station grid locator.</summary>
    public string? DxGrid { get; init; }

    /// <summary>Gets the calculated bearing in degrees, when both grids are available.</summary>
    public int? Bearing { get; init; }
}
