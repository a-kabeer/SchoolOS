using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.PlatformModules.Subscriptions.Application;
using SchoolOS.PlatformModules.Subscriptions.Persistence;
using SchoolOS.PlatformModules.Tenants.Persistence;

namespace SchoolOS.Cloud.Api;

/// <summary>
/// Orchestrates Cloud Control Plane EF Core 10 migrations, least-privilege runtime role grants,
/// and idempotent subscription catalog seeding.
/// </summary>
public static class CloudDatabaseBootstrapper
{
    public static readonly string[] CloudSchemas = ["platform", "subscriptions"];

    public static async Task ApplyMigrationsAndGrantsAsync(
        IServiceProvider services,
        string runtimeRoleName = "schoolos_app",
        string? runtimeRolePassword = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetService<ILoggerFactory>()?.CreateLogger(nameof(CloudDatabaseBootstrapper));

        var platformDb = sp.GetRequiredService<PlatformDbContext>();
        var subscriptionsDb = sp.GetRequiredService<SubscriptionsDbContext>();

        await platformDb.Database.MigrateAsync(cancellationToken);
        await subscriptionsDb.Database.MigrateAsync(cancellationToken);

        if (platformDb.Database.IsRelational())
        {
            if (!string.IsNullOrWhiteSpace(runtimeRolePassword))
            {
                await platformDb.Database.ExecuteSqlRawAsync(
                    PostgresRlsHelper.GenerateEnsureLeastPrivilegeRoleSql(runtimeRoleName, runtimeRolePassword),
                    cancellationToken);
            }

            foreach (var schema in CloudSchemas)
            {
                await platformDb.Database.ExecuteSqlRawAsync(
                    PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql(schema, runtimeRoleName),
                    cancellationToken);
            }
        }

        logger?.LogInformation("Applied Cloud Control Plane migrations and least-privilege grants for role {Role}.", runtimeRoleName);
    }

    public static async Task SeedCatalogAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();
        await SubscriptionCatalogSeeder.SeedAsync(db, cancellationToken);
    }
}
