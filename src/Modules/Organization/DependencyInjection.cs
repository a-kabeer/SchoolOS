using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.Modules.Organization.Application;
using SchoolOS.Modules.Organization.Contracts;
using SchoolOS.Modules.Organization.Persistence;

namespace SchoolOS.Modules.Organization;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "LocalDatabase")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? configuration.GetConnectionString("SchoolOSDatabase")
            ?? configuration["Database:ConnectionString"];

        services.AddDbContext<OrganizationDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();
            var sessionInterceptor = sp.GetRequiredService<PostgresTenantSessionInterceptor>();
            var txInterceptor = sp.GetRequiredService<PostgresTenantTransactionInterceptor>();

            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
            }

            options.AddInterceptors(auditInterceptor, sessionInterceptor, txInterceptor);
        });

        services.AddScoped<IOrganizationAcademicReader, OrganizationAcademicReader>();

        return services;
    }
}
