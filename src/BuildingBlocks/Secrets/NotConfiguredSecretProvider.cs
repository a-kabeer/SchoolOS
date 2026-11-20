namespace SchoolOS.BuildingBlocks.Secrets;

public sealed class NotConfiguredSecretProvider : ISecretProvider
{
    public const string Message =
        "No secret store is configured. Set Secrets:Provider to HashiCorp, AzureKeyVault, Aws or Gcp.";

    public Task<string?> GetSecretAsync(SecretKey key, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task SetSecretAsync(SecretKey key, string value, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task DeleteSecretAsync(SecretKey key, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);
}
