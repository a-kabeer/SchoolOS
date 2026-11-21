using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.Modules.Fees.Application;
using SchoolOS.Modules.Fees.Persistence;

namespace SchoolOS.Modules.Fees;

public static class DependencyInjection
{
    public static IServiceCollection AddFeesModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "LocalDatabase")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? configuration.GetConnectionString("SchoolOSDatabase")
            ?? configuration["Database:ConnectionString"];

        services.AddDbContext<FeesDbContext>((sp, options) =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "fees");
                });
                options.UseSnakeCaseNamingConvention();
            }

            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<ChangeAuditInterceptor>(),
                sp.GetRequiredService<PostgresTenantSessionInterceptor>(),
                sp.GetRequiredService<PostgresTenantTransactionInterceptor>());
        });

        services.AddScoped<FeesService>();

        return services;
    }
}
