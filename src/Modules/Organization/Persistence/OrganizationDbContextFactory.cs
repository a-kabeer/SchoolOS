using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.Modules.Organization.Persistence;

public class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrganizationDbContext>();
        optionsBuilder.UseNpgsql("Host=127.0.0.1;Database=schoolos_design;Username=postgres;Password=dummy")
                      .UseSnakeCaseNamingConvention();

        var designTenantContext = new TenantContext(null, null, "design-time");
        return new OrganizationDbContext(optionsBuilder.Options, designTenantContext);
    }
}
