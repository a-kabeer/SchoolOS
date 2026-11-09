using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Domain;

namespace SchoolOS.Modules.Organization.Persistence;

/// <summary>
/// Operational DbContext for the Organization module on the Local Data Plane.
/// Maps to the 'organization' logical schema in PostgreSQL.
/// Enforces multi-tenant query filters and PostgreSQL Row-Level Security.
/// </summary>
public class OrganizationDbContext : SchoolOSDbContextBase
{
    public DbSet<School> Schools => Set<School>();
    public DbSet<Campus> Campuses => Set<Campus>();

    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("organization");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
