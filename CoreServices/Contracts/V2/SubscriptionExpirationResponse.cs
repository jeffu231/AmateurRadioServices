namespace CoreServices.Contracts.V2;

/// <summary>
/// Represents the QRZ subscription expiration available to API consumers.
/// </summary>
public sealed record SubscriptionExpirationResponse
{
    /// <summary>Gets the QRZ subscription expiration in UTC.</summary>
    public required DateTimeOffset SubscriptionExpiration { get; init; }
}
