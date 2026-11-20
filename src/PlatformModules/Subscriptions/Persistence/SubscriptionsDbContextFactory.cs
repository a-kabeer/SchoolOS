using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.PlatformModules.Subscriptions.Persistence;

public sealed class SubscriptionsDbContextFactory : IDesignTimeDbContextFactory<SubscriptionsDbContext>
{
    public SubscriptionsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SubscriptionsDbContext>();
        optionsBuilder.UseNpgsql(
                "Host=127.0.0.1;Database=schoolos_platform_design;Username=postgres;Password=dummy",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "subscriptions"))
            .UseSnakeCaseNamingConvention();

        return new SubscriptionsDbContext(
            optionsBuilder.Options,
            new TenantContext(null, null, "design-time", isPlatformAdmin: true));
    }
}
