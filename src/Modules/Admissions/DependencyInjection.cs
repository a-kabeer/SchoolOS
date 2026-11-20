using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.Modules.Admissions.Persistence;

namespace SchoolOS.Modules.Admissions;

public static class DependencyInjection
{
    public static IServiceCollection AddAdmissionsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "LocalDatabase")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? configuration["Database:ConnectionString"];

        services.AddDbContext<AdmissionsDbContext>((sp, options) =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "admissions");
                });
                options.UseSnakeCaseNamingConvention();
            }

            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<ChangeAuditInterceptor>(),
                sp.GetRequiredService<PostgresTenantSessionInterceptor>(),
                sp.GetRequiredService<PostgresTenantTransactionInterceptor>());
        });

        return services;
    }
}
