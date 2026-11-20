namespace SchoolOS.BuildingBlocks.Secrets;

/// <summary>
/// Cloud-agnostic secret store abstraction (HashiCorp Vault, Azure Key Vault, AWS Secrets Manager, GCP Secret Manager).
/// Secret values must never be stored in the database, logged, or put in exception messages.
/// </summary>
public interface ISecretProvider
{
    Task<string?> GetSecretAsync(SecretKey key, CancellationToken cancellationToken = default);
    Task SetSecretAsync(SecretKey key, string value, CancellationToken cancellationToken = default);
    Task DeleteSecretAsync(SecretKey key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Secrets scoped to the current tenant under <c>tenants/{tenantId}/{name}</c>.
/// Tenant identity comes strictly from authenticated <see cref="Security.ITenantContext"/>.
/// </summary>
public interface ITenantSecretProvider
{
    Task<string?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task SetAsync(string name, string value, CancellationToken cancellationToken = default);
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Raised when a secret store operation fails. Names the operation and logical key, never the secret value.
/// </summary>
public sealed class SecretProviderException : Exception
{
    public SecretProviderException(string operation, SecretKey key, bool isTransient, Exception? innerException = null)
        : base($"Secret store operation '{operation}' failed for '{key.Path}'.", innerException)
    {
        Operation = operation;
        Key = key;
        IsTransient = isTransient;
    }

    public string Operation { get; }
    public SecretKey Key { get; }
    public bool IsTransient { get; }
}
