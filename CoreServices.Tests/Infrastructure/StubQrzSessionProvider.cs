using CoreServices.Integrations.Qrz;

namespace CoreServices.Tests.Infrastructure;

/// <summary>
/// Provides a deterministic QRZ session for endpoint tests.
/// </summary>
internal sealed class StubQrzSessionProvider(QrzSession? session) : IQrzSessionProvider
{
    /// <inheritdoc />
    public Task<QrzSession?> GetSessionAsync(CancellationToken cancellationToken) => Task.FromResult(session);

    /// <inheritdoc />
    public void InvalidateSession()
    {
    }
}
