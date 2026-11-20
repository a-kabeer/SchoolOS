namespace SchoolOS.BuildingBlocks.SharedKernel;

/// <summary>
/// Enforces multi-tenant ownership on an entity.
/// Evaluated by EF Core global query filters and PostgreSQL Row-Level Security.
/// </summary>
public interface ITenantScopedEntity
{
    Guid TenantId { get; }
}
