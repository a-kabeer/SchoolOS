using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.Modules.Identity.Persistence;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(
                "Host=127.0.0.1;Database=schoolos_local_design;Username=postgres;Password=dummy",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
            .UseSnakeCaseNamingConvention();

        return new IdentityDbContext(
            optionsBuilder.Options,
            new TenantContext(null, null, "design-time"));
    }
}
