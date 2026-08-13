namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the rounded bearing between two Maidenhead grid locators.
/// </summary>
public sealed record MaidenheadBearingResponse
{
    /// <summary>Gets the bearing in degrees.</summary>
    public int Bearing { get; init; }
}
