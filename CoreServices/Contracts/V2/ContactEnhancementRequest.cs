namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the input for a v2 contact-bearing enhancement.
/// </summary>
public sealed record ContactEnhancementRequest
{
    /// <summary>Gets the operator's callsign.</summary>
    public string? DeCall { get; init; }

    /// <summary>Gets the operator's Maidenhead grid locator.</summary>
    public string? DeGrid { get; init; }

    /// <summary>Gets the distant station's callsign.</summary>
    public string? DxCall { get; init; }

    /// <summary>Gets the distant station's Maidenhead grid locator.</summary>
    public string? DxGrid { get; init; }
}
