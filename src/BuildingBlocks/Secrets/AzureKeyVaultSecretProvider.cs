using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace SchoolOS.BuildingBlocks.Secrets;

public sealed class AzureKeyVaultSecretProvider(SecretClient client) : ISecretProvider
{
    public static SecretClient CreateClient(AzureKeyVaultOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new SecretClient(new Uri(options.VaultUri!), new DefaultAzureCredential());
    }

    public static string ToSecretName(SecretKey key) => DashedSecretName.From(key);

    public Task<string?> GetSecretAsync(SecretKey key, CancellationToken cancellationToken = default) =>
        SecretCall.RunAsync<string?>("get", key, async () =>
        {
            try
            {
                var response = await client.GetSecretAsync(ToSecretName(key), cancellationToken: cancellationToken);
                return response.Value.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }, IsTransient, cancellationToken);

    public Task SetSecretAsync(SecretKey key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        return SecretCall.RunAsync("set", key,
            () => client.SetSecretAsync(ToSecretName(key), value, cancellationToken),
            IsTransient, cancellationToken);
    }

    public Task DeleteSecretAsync(SecretKey key, CancellationToken cancellationToken = default) =>
        SecretCall.RunAsync("delete", key, async () =>
        {
            try
            {
                var operation = await client.StartDeleteSecretAsync(ToSecretName(key), cancellationToken);
                await operation.WaitForCompletionAsync(cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Already gone.
            }
        }, IsTransient, cancellationToken);

    private static bool IsTransient(Exception ex) => ex switch
    {
        RequestFailedException failed => SecretCall.IsTransientStatus(failed.Status),
        HttpRequestException or TimeoutException => true,
        _ => false,
    };
}

internal static class DashedSecretName
{
    public static string From(SecretKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Compact 32-hex GUID keeps Azure Key Vault / GCP Secret Manager names compact (<127 chars).
        return key.TenantId is { } tenantId
            ? $"tenant-{tenantId:N}-{key.Name}"
            : $"platform-{key.Name}";
    }
}
