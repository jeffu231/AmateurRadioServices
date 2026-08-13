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

    /// <summary>Gets the APRS packet path.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the APRS station type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the APRS packet timestamp as Unix time.</summary>
    public long Time { get; init; }

    /// <summary>Gets the prior APRS packet timestamp as Unix time.</summary>
    public long LastTime { get; init; }

    /// <summary>Gets the APRS station class.</summary>
    public required string Class { get; init; }

    /// <summary>Gets the APRS map symbol.</summary>
    public required string Symbol { get; init; }
}
