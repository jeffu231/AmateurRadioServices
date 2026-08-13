namespace CoreServices.Tests.Infrastructure;

/// <summary>
/// Starts the API as a private deployment with rate limiting disabled.
/// </summary>
public sealed class PrivateApiWebApplicationFactory : ApiWebApplicationFactory
{
    /// <inheritdoc />
    protected override string EnvironmentName => "PrivateTesting";
}
