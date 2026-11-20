using System.Net;
using System.Text.Json;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Token;

namespace SchoolOS.BuildingBlocks.Secrets;

public sealed class HashiCorpVaultSecretProvider(IVaultClient client, string mountPoint) : ISecretProvider
{
    public const string ValueField = "value";

    public static IVaultClient CreateClient(HashiCorpVaultOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        IAuthMethodInfo auth = options.AuthMethod switch
        {
            HashiCorpAuthMethod.AppRole => new AppRoleAuthMethodInfo(options.AppRoleMountPoint, options.RoleId, options.SecretId),
            _ => new TokenAuthMethodInfo(options.Token),
        };

        return new VaultClient(new VaultClientSettings(options.Address, auth));
    }

    public static string ToSecretPath(SecretKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key.Path;
    }

    public Task<string?> GetSecretAsync(SecretKey key, CancellationToken cancellationToken = default) =>
        SecretCall.RunAsync<string?>("get", key, async () =>
        {
            try
            {
                var secret = await client.V1.Secrets.KeyValue.V2
                    .ReadSecretAsync(ToSecretPath(key), mountPoint: mountPoint)
                    .WaitAsync(cancellationToken);

                return secret?.Data?.Data is { } data && data.TryGetValue(ValueField, out var value)
                    ? ReadString(value)
                    : null;
            }
            catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }, IsTransient, cancellationToken);

    public Task SetSecretAsync(SecretKey key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        return SecretCall.RunAsync("set", key, () => client.V1.Secrets.KeyValue.V2
            .WriteSecretAsync(ToSecretPath(key), new Dictionary<string, object> { [ValueField] = value }, mountPoint: mountPoint)
            .WaitAsync(cancellationToken), IsTransient, cancellationToken);
    }

    public Task DeleteSecretAsync(SecretKey key, CancellationToken cancellationToken = default) =>
        SecretCall.RunAsync("delete", key, async () =>
        {
            try
            {
                await client.V1.Secrets.KeyValue.V2
                    .DeleteMetadataAsync(ToSecretPath(key), mountPoint)
                    .WaitAsync(cancellationToken);
            }
            catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                // Already gone.
            }
        }, IsTransient, cancellationToken);

    private static string? ReadString(object? value) => value switch
    {
        null => null,
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        _ => value.ToString(),
    };

    private static bool IsTransient(Exception ex) => ex switch
    {
        VaultApiException api => SecretCall.IsTransientStatus((int)api.HttpStatusCode),
        HttpRequestException or TimeoutException or TaskCanceledException => true,
        _ => false,
    };
}
