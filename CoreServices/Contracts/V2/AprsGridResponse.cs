namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the Maidenhead grid of one APRS station.
/// </summary>
public sealed record AprsGridResponse
{
    /// <summary>Gets the station name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the Maidenhead grid locator.</summary>
    public required string Grid { get; init; }
}
