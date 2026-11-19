using System.Diagnostics;
using SchoolOS.BuildingBlocks.Observability;

namespace SchoolOS.BuildingBlocks.Secrets;

public sealed class InstrumentedSecretProvider(ISecretProvider inner, string providerName) : ISecretProvider
{
    public ISecretProvider Inner => inner;

    public async Task<string?> GetSecretAsync(SecretKey key, CancellationToken cancellationToken = default)
    {
        using var activity = Start("get", key);
        try
        {
            return await inner.GetSecretAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            Fail(activity, ex);
            throw;
        }
    }

    public async Task SetSecretAsync(SecretKey key, string value, CancellationToken cancellationToken = default)
    {
        using var activity = Start("set", key);
        try
        {
            await inner.SetSecretAsync(key, value, cancellationToken);
        }
        catch (Exception ex)
        {
            Fail(activity, ex);
            throw;
        }
    }

    public async Task DeleteSecretAsync(SecretKey key, CancellationToken cancellationToken = default)
    {
        using var activity = Start("delete", key);
        try
        {
            await inner.DeleteSecretAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            Fail(activity, ex);
            throw;
        }
    }

    private Activity? Start(string operation, SecretKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var activity = SecretsTelemetry.ActivitySource.StartActivity($"secrets {operation}", ActivityKind.Internal);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag(SecretsTelemetry.ProviderTag, providerName);
        activity.SetTag(SecretsTelemetry.OperationTag, operation);
        activity.SetTag(SecretsTelemetry.ScopeTag, key.IsPlatform ? "platform" : "tenant");
        activity.SetTag(SecretsTelemetry.NameTag, key.Name);

        if (key.TenantId is { } tenantId)
        {
            activity.SetTag(TelemetryNames.Tags.TenantId, tenantId.ToString("D"));
        }

        return activity;
    }

    private static void Fail(Activity? activity, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.SetTag("error.type", ex.GetType().FullName);
    }
}
