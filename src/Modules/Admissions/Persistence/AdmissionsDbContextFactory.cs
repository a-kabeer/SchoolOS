using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.Modules.Admissions.Persistence;

public sealed class AdmissionsDbContextFactory : IDesignTimeDbContextFactory<AdmissionsDbContext>
{
    public AdmissionsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AdmissionsDbContext>();
        optionsBuilder.UseNpgsql(
                "Host=127.0.0.1;Database=schoolos_local_design;Username=postgres;Password=dummy",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "admissions"))
            .UseSnakeCaseNamingConvention();

        return new AdmissionsDbContext(
            optionsBuilder.Options,
            new TenantContext(null, null, "design-time"));
    }
}
