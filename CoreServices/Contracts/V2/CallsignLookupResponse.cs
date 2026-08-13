namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the intentionally supported public details for a callsign lookup.
/// </summary>
public sealed record CallsignLookupResponse
{
    /// <summary>Gets the callsign returned by QRZ.</summary>
    public required string Callsign { get; init; }

    /// <summary>Gets the operator's first name when published by QRZ.</summary>
    public string? FirstName { get; init; }

    /// <summary>Gets the operator's name when published by QRZ.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the published state or region.</summary>
    public string? State { get; init; }

    /// <summary>Gets the published country.</summary>
    public string? Country { get; init; }

    /// <summary>Gets the Maidenhead grid locator.</summary>
    public string? Grid { get; init; }
}
