using CoreServices.Integrations.Qrz;
using CoreServices.Model.Qrz;

namespace CoreServices.Tests.Infrastructure;

/// <summary>
/// Returns a configured QRZ response for controller tests.
/// </summary>
internal sealed class StubQrzClient(QRZDatabase response) : IQrzClient
{
    /// <inheritdoc />
    public Task<QRZDatabase> GetCallDataAsync(string call, CancellationToken cancellationToken) =>
        Task.FromResult(response);
}
