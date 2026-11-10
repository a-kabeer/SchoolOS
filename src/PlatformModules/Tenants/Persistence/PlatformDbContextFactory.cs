using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.PlatformModules.Tenants.Persistence;

public class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        optionsBuilder.UseNpgsql("Host=127.0.0.1;Database=schoolos_platform_design;Username=postgres;Password=dummy")
                      .UseSnakeCaseNamingConvention();

        return new PlatformDbContext(optionsBuilder.Options);
    }
}
