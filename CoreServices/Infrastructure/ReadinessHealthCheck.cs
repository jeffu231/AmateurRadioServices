using CoreServices.Integrations.Aprs;
using CoreServices.Integrations.Qrz;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CoreServices.Infrastructure;

/// <summary>
/// Verifies that the configuration required to accept API traffic is available.
/// </summary>
internal sealed class ReadinessHealthCheck(
    IOptions<AprsOptions> aprsOptions,
    IOptions<QrzOptions> qrzOptions) : IHealthCheck
{
    /// <summary>
    /// Checks the loaded local configuration without contacting an upstream provider.
    /// </summary>
    /// <param name="context">The health-check execution context.</param>
    /// <param name="cancellationToken">A token that observes cancellation requests.</param>
    /// <returns>A task that represents the completed readiness check.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _ = aprsOptions.Value;
        _ = qrzOptions.Value;
        return Task.FromResult(HealthCheckResult.Healthy("Required local configuration is loaded."));
    }
}
