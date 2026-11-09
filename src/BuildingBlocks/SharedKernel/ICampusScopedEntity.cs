namespace SchoolOS.BuildingBlocks.SharedKernel;

/// <summary>
/// Enforces campus-level ownership on an entity within a tenant.
/// Campus ownership must never cross tenant boundaries.
/// </summary>
public interface ICampusScopedEntity : ITenantScopedEntity
{
    Guid CampusId { get; }
}
