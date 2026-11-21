using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.Modules.Attendance.Application;
using SchoolOS.Modules.Attendance.Persistence;

namespace SchoolOS.Modules.Attendance;

public static class DependencyInjection
{
    public static IServiceCollection AddAttendanceModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "LocalDatabase")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? configuration.GetConnectionString("SchoolOSDatabase")
            ?? configuration["Database:ConnectionString"];

        services.AddDbContext<AttendanceDbContext>((sp, options) =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "attendance");
                });
                options.UseSnakeCaseNamingConvention();
            }

            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<ChangeAuditInterceptor>(),
                sp.GetRequiredService<PostgresTenantSessionInterceptor>(),
                sp.GetRequiredService<PostgresTenantTransactionInterceptor>());
        });

        services.AddScoped<AttendanceService>();

        return services;
    }
}
