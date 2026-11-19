using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.PlatformModules.Tenants.Persistence;

namespace SchoolOS.PlatformModules.Tenants;

public static class DependencyInjection
{
    public static IServiceCollection AddPlatformTenantsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "PlatformDatabase")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? configuration["Database:ConnectionString"];

        services.AddDbContext<PlatformDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();

            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
            }

            options.AddInterceptors(auditInterceptor);
        });

        return services;
    }
}
