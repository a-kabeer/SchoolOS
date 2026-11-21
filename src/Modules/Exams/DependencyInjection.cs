using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.Modules.Exams.Application;
using SchoolOS.Modules.Exams.Persistence;

namespace SchoolOS.Modules.Exams;

public static class DependencyInjection
{
    public static IServiceCollection AddExamsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "LocalDatabase")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? configuration.GetConnectionString("SchoolOSDatabase")
            ?? configuration["Database:ConnectionString"];

        services.AddDbContext<ExamsDbContext>((sp, options) =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "exams");
                });
                options.UseSnakeCaseNamingConvention();
            }

            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<ChangeAuditInterceptor>(),
                sp.GetRequiredService<PostgresTenantSessionInterceptor>(),
                sp.GetRequiredService<PostgresTenantTransactionInterceptor>());
        });

        services.AddScoped<ExamsService>();

        return services;
    }
}
