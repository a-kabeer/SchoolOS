using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SchoolOS.BuildingBlocks.Secrets;

public sealed class SecretProviderHealthCheck(ISecretProvider uncachedProvider) : IHealthCheck
{
    public const string Name = "secrets";
    public static readonly SecretKey ProbeKey = SecretKey.Platform("health-check");

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await uncachedProvider.GetSecretAsync(ProbeKey, cancellationToken);
            return HealthCheckResult.Healthy("The secret store answered.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "The secret store did not answer.", ex);
        }
    }
}
