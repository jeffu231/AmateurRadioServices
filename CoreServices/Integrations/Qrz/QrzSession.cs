namespace CoreServices.Integrations.Qrz;

/// <summary>
/// Represents a QRZ session held by this application process.
/// </summary>
public sealed record QrzSession(string Token, DateTimeOffset SubscriptionExpiration);
