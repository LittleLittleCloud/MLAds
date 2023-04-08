using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PredictAPI;

public sealed class AdsMLHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}