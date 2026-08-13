namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the Maidenhead grid locator for a geographic coordinate.
/// </summary>
public sealed record MaidenheadGridResponse
{
    /// <summary>Gets the Maidenhead grid locator.</summary>
    public required string Grid { get; init; }
}
