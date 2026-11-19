using Microsoft.EntityFrameworkCore;
using SchoolOS.PlatformModules.Tenants.Domain;

namespace SchoolOS.PlatformModules.Tenants.Persistence;

/// <summary>
/// Cloud Control Plane database context for managing the global customer tenant registry.
/// Distinct from the Local Data Plane operational SchoolOS database.
/// </summary>
public class PlatformDbContext : DbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("platform");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
