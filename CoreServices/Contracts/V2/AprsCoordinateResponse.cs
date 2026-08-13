namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the coordinates of one APRS station.
/// </summary>
public sealed record AprsCoordinateResponse
{
    /// <summary>Gets the station name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the latitude in decimal degrees.</summary>
    public double Latitude { get; init; }

    /// <summary>Gets the longitude in decimal degrees.</summary>
    public double Longitude { get; init; }
}
