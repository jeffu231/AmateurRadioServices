namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the intentionally supported public details for a callsign lookup.
/// </summary>
public sealed record CallsignLookupResponse
{
    /// <summary>Gets the callsign returned by QRZ.</summary>
    public required string Callsign { get; init; }

    /// <summary>Gets alternate callsigns published by QRZ.</summary>
    public string? Aliases { get; init; }

    /// <summary>Gets the DXCC entity identifier.</summary>
    public string? Dxcc { get; init; }

    /// <summary>Gets the operator's first name when published by QRZ.</summary>
    public string? FirstName { get; init; }

    /// <summary>Gets the operator's name when published by QRZ.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the first published address line.</summary>
    public string? AddressLine1 { get; init; }

    /// <summary>Gets the second published address line.</summary>
    public string? AddressLine2 { get; init; }

    /// <summary>Gets the published state or region.</summary>
    public string? State { get; init; }

    /// <summary>Gets the published postal code.</summary>
    public string? PostalCode { get; init; }

    /// <summary>Gets the published country.</summary>
    public string? Country { get; init; }

    /// <summary>Gets the published latitude.</summary>
    public string? Latitude { get; init; }

    /// <summary>Gets the published longitude.</summary>
    public string? Longitude { get; init; }

    /// <summary>Gets the Maidenhead grid locator.</summary>
    public string? Grid { get; init; }

    /// <summary>Gets the published county.</summary>
    public string? County { get; init; }

    /// <summary>Gets the published country code.</summary>
    public string? CountryCode { get; init; }

    /// <summary>Gets the published FIPS code.</summary>
    public string? FipsCode { get; init; }

    /// <summary>Gets the published land designation.</summary>
    public string? Land { get; init; }

    /// <summary>Gets the effective date published by QRZ.</summary>
    public string? EffectiveDate { get; init; }

    /// <summary>Gets the expiration date published by QRZ.</summary>
    public string? ExpirationDate { get; init; }

    /// <summary>Gets the published license class.</summary>
    public string? LicenseClass { get; init; }

    /// <summary>Gets the published callsign codes.</summary>
    public string? Codes { get; init; }

    /// <summary>Gets the published QSL manager.</summary>
    public string? QslManager { get; init; }

    /// <summary>Gets the published email address.</summary>
    public string? Email { get; init; }

    /// <summary>Gets the published profile view count.</summary>
    public string? ViewCount { get; init; }

    /// <summary>Gets the published biography.</summary>
    public string? Biography { get; init; }

    /// <summary>Gets the published biography date.</summary>
    public string? BiographyDate { get; init; }

    /// <summary>Gets the published modification date.</summary>
    public string? ModifiedDate { get; init; }

    /// <summary>Gets the published metropolitan statistical area.</summary>
    public string? MetropolitanStatisticalArea { get; init; }

    /// <summary>Gets the published area code.</summary>
    public string? AreaCode { get; init; }

    /// <summary>Gets the published time zone.</summary>
    public string? TimeZone { get; init; }

    /// <summary>Gets the published GMT offset.</summary>
    public string? GmtOffset { get; init; }

    /// <summary>Gets the published daylight-saving-time designation.</summary>
    public string? DaylightSavingTime { get; init; }

    /// <summary>Gets the published eQSL status.</summary>
    public string? Eqsl { get; init; }

    /// <summary>Gets the published Mail QSL status.</summary>
    public string? Mqsl { get; init; }

    /// <summary>Gets the published CQ zone.</summary>
    public string? CqZone { get; init; }

    /// <summary>Gets the published ITU zone.</summary>
    public string? ItuZone { get; init; }

    /// <summary>Gets the published Logbook of the World status.</summary>
    public string? Lotw { get; init; }

    /// <summary>Gets the published geographic-location descriptor.</summary>
    public string? Geolocation { get; init; }

    /// <summary>Gets the published name format.</summary>
    public string? NameFormat { get; init; }
}
