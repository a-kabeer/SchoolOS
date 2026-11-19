using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.BuildingBlocks.Secrets;

/// <summary>
/// Resolves secrets strictly for the authenticated tenant under <c>tenants/{tenantGuid}/{name}</c>.
/// Fails closed if no tenant is resolved in <see cref="ITenantContext"/>.
/// </summary>
public sealed class TenantSecretProvider(ITenantContext tenantContext, ISecretProvider secrets) : ITenantSecretProvider
{
    public const string NoTenantMessage = "No tenant is resolved for this request, so tenant secrets cannot be used.";

    public Task<string?> GetAsync(string name, CancellationToken cancellationToken = default) =>
        secrets.GetSecretAsync(KeyFor(name), cancellationToken);

    public Task SetAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        return secrets.SetSecretAsync(KeyFor(name), value, cancellationToken);
    }

    public Task DeleteAsync(string name, CancellationToken cancellationToken = default) =>
        secrets.DeleteSecretAsync(KeyFor(name), cancellationToken);

    private SecretKey KeyFor(string name) => tenantContext.TenantId is { } tenantId && tenantId != Guid.Empty
        ? SecretKey.ForTenant(tenantId, name)
        : throw new InvalidOperationException(NoTenantMessage);
}
