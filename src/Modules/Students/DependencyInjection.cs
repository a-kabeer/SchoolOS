using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.Modules.Students.Application;
using SchoolOS.Modules.Students.Contracts;
using SchoolOS.Modules.Students.Persistence;

namespace SchoolOS.Modules.Students;

public static class DependencyInjection
{
    public static IServiceCollection AddStudentsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "LocalDatabase")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? configuration.GetConnectionString("SchoolOSDatabase")
            ?? configuration["Database:ConnectionString"];

        services.AddDbContext<StudentsDbContext>((sp, options) =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "students");
                });
                options.UseSnakeCaseNamingConvention();
            }

            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<ChangeAuditInterceptor>(),
                sp.GetRequiredService<PostgresTenantSessionInterceptor>(),
                sp.GetRequiredService<PostgresTenantTransactionInterceptor>());
        });

        services.AddScoped<StudentLifecycleService>();
        services.AddScoped<IStudentAdmissionBridge>(sp => sp.GetRequiredService<StudentLifecycleService>());

        return services;
    }
}
