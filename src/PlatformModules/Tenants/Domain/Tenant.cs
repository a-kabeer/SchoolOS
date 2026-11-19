using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.PlatformModules.Tenants.Domain;

/// <summary>
/// Represents a customer organization registered in the SchoolOS Cloud Control Plane platform registry.
/// Owns subscriptions, nodes, and licensing boundaries.
/// </summary>
public class Tenant : Entity<Guid>
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public TenantStatus Status { get; private set; } = TenantStatus.Active;
    public bool IsActive => Status == TenantStatus.Active;

    protected Tenant() { }

    private Tenant(Guid id, string name, string slug)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Status = TenantStatus.Active;
    }

    /// <summary>
    /// Factory method generating a new Tenant with sequential UUIDv7 identifier.
    /// </summary>
    public static Tenant Create(string name, string slug)
    {
        return new Tenant(Guid.CreateVersion7(), name, slug);
    }

    public void UpdateDetails(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void Suspend()
    {
        Status = TenantStatus.Suspended;
    }

    public void Activate()
    {
        Status = TenantStatus.Active;
    }

    public void Deactivate()
    {
        Status = TenantStatus.Deactivated;
    }
}
