namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the rounded distance between two Maidenhead grid locators.
/// </summary>
public sealed record MaidenheadDistanceResponse
{
    /// <summary>Gets the distance in miles.</summary>
    public int Miles { get; init; }

    /// <summary>Gets the distance in kilometers.</summary>
    public int Kilometers { get; init; }
}
