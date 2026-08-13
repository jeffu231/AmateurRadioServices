using CoreServices.Integrations.Aprs;
using CoreServices.Model.Aprs;

namespace CoreServices.Tests.Infrastructure;

/// <summary>
/// Returns a configured APRS record for controller tests.
/// </summary>
internal sealed class StubAprsClient(AprsLocRecord? record) : IAprsClient
{
    /// <inheritdoc />
    public Task<AprsLocRecord?> GetAprsLocRecordAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(record);
}
