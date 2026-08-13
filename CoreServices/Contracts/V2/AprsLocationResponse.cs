namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the public APRS location details for one station.
/// </summary>
public sealed record AprsLocationResponse
{
    /// <summary>Gets the station name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the source callsign.</summary>
    public required string SourceCallsign { get; init; }

    /// <summary>Gets the destination callsign.</summary>
    public required string DestinationCallsign { get; init; }

    /// <summary>Gets the latitude in decimal degrees.</summary>
    public double Latitude { get; init; }

    /// <summary>Gets the longitude in decimal degrees.</summary>
    public double Longitude { get; init; }

    /// <summary>Gets the APRS comment.</summary>
    public required string Comment { get; init; }
}
