using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace SchoolOS.BuildingBlocks.Secrets;

public sealed class AwsSecretsManagerSecretProvider(IAmazonSecretsManager client) : ISecretProvider, IDisposable
{
    public const int RecoveryWindowInDays = 7;

    public static IAmazonSecretsManager CreateClient(AwsSecretsManagerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(options.Region));
    }

    public static string ToSecretName(SecretKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key.Path;
    }

    public Task<string?> GetSecretAsync(SecretKey key, CancellationToken cancellationToken = default) =>
        SecretCall.RunAsync<string?>("get", key, async () =>
        {
            try
            {
                var response = await client.GetSecretValueAsync(
                    new GetSecretValueRequest { SecretId = ToSecretName(key) }, cancellationToken);
                return response.SecretString;
            }
            catch (ResourceNotFoundException)
            {
                return null;
            }
            catch (InvalidRequestException)
            {
                if (await IsScheduledForDeletionAsync(key, cancellationToken))
                {
                    return null;
                }

                throw;
            }
        }, IsTransient, cancellationToken);

    public Task SetSecretAsync(SecretKey key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        return SecretCall.RunAsync("set", key, async () =>
        {
            var name = ToSecretName(key);

            try
            {
                await PutAsync(name, value, cancellationToken);
            }
            catch (ResourceNotFoundException)
            {
                await client.CreateSecretAsync(new CreateSecretRequest { Name = name, SecretString = value }, cancellationToken);
            }
            catch (InvalidRequestException)
            {
                if (!await IsScheduledForDeletionAsync(key, cancellationToken))
                {
                    throw;
                }

                await client.RestoreSecretAsync(new RestoreSecretRequest { SecretId = name }, cancellationToken);
                await PutAsync(name, value, cancellationToken);
            }
        }, IsTransient, cancellationToken);
    }

    public Task DeleteSecretAsync(SecretKey key, CancellationToken cancellationToken = default) =>
        SecretCall.RunAsync("delete", key, async () =>
        {
            try
            {
                await client.DeleteSecretAsync(
                    new DeleteSecretRequest { SecretId = ToSecretName(key), RecoveryWindowInDays = RecoveryWindowInDays },
                    cancellationToken);
            }
            catch (ResourceNotFoundException)
            {
                // Already gone.
            }
            catch (InvalidRequestException)
            {
                if (!await IsScheduledForDeletionAsync(key, cancellationToken))
                {
                    throw;
                }
            }
        }, IsTransient, cancellationToken);

    public void Dispose() => client.Dispose();

    private Task PutAsync(string name, string value, CancellationToken cancellationToken) =>
        client.PutSecretValueAsync(new PutSecretValueRequest { SecretId = name, SecretString = value }, cancellationToken);

    private async Task<bool> IsScheduledForDeletionAsync(SecretKey key, CancellationToken cancellationToken)
    {
        var description = await client.DescribeSecretAsync(
            new DescribeSecretRequest { SecretId = ToSecretName(key) }, cancellationToken);
        return description.DeletedDate is not null;
    }

    private static bool IsTransient(Exception ex) => ex switch
    {
        AmazonServiceException service => SecretCall.IsTransientStatus((int)service.StatusCode),
        HttpRequestException or TimeoutException => true,
        _ => false,
    };
}
