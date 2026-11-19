using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Persistence;
using Xunit;

namespace SchoolOS.IntegrationTests;

[CollectionDefinition(Name)]
public class PostgresDatabaseCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "PostgresDatabaseCollection";
}

public class PostgresDatabaseFixture : IAsyncLifetime
{
    public const string MasterConnectionString = "Host=127.0.0.1;Port=5432;Database=postgres;Username=schoolos_dev;Password=schoolos_dev_password";
    public const string AdminConnectionString = "Host=127.0.0.1;Port=5432;Database=schoolos_test;Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";
    public const string AppConnectionString = "Host=127.0.0.1;Port=5432;Database=schoolos_test;Username=schoolos_app;Password=schoolos_app_password;Include Error Detail=true";
    public const string PlatformAdminConnectionString = "Host=127.0.0.1;Port=5432;Database=schoolos_test;Username=schoolos_platform_admin;Password=schoolos_platform_admin_password;Include Error Detail=true";

    public async Task InitializeAsync()
    {
        // 1. Ensure clean, isolated schoolos_test database by connecting to default postgres administrative database
        await using (var masterConn = new NpgsqlConnection(MasterConnectionString))
        {
            await masterConn.OpenAsync();
            await using (var cmd = masterConn.CreateCommand())
            {
                cmd.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'schoolos_test' AND pid <> pg_backend_pid();";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = "DROP DATABASE IF EXISTS schoolos_test;";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = "CREATE DATABASE schoolos_test;";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // 2. Setup schema and initial structure using admin credentials on schoolos_test.
        // RLS enablement, FORCE RLS, and policies MUST come strictly from EF Core migrations (never patched by this fixture).
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(AdminConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        var systemContext = new TenantContext(null, null, "system-setup");
        await using var db = new OrganizationDbContext(options, systemContext);

        // Apply formal EF Core migrations (embeds ENABLE + FORCE ROW LEVEL SECURITY and tenant isolation policies)
        await db.Database.MigrateAsync();

        // Ensure least-privilege, non-superuser runtime roles exist:
        // - schoolos_app: tenant runtime role (NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS)
        // - schoolos_platform_admin: independent Cloud platform-admin role (never granted to schoolos_app, no grants on organization.*)
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'schoolos_app') THEN
                    CREATE ROLE schoolos_app WITH LOGIN PASSWORD 'schoolos_app_password' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS;
                ELSE
                    ALTER ROLE schoolos_app WITH LOGIN PASSWORD 'schoolos_app_password' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS;
                END IF;

                IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'schoolos_platform_admin') THEN
                    CREATE ROLE schoolos_platform_admin WITH LOGIN PASSWORD 'schoolos_platform_admin_password' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS;
                ELSE
                    ALTER ROLE schoolos_platform_admin WITH LOGIN PASSWORD 'schoolos_platform_admin_password' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS;
                END IF;
            END
            $$;
            REVOKE CREATE ON SCHEMA public FROM PUBLIC;
            REVOKE ALL ON SCHEMA organization FROM schoolos_platform_admin;
            """);

        // Apply strict least-privilege DML-only grants to schoolos_app (no CREATE, no TRUNCATE, no TRIGGER, no REFERENCES)
        await db.Database.ExecuteSqlRawAsync(
            PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql("organization", "schoolos_app"));
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public OrganizationDbContext CreateDbContext(ITenantContext tenantContext)
    {
        var auditInterceptor = new AuditSaveChangesInterceptor(tenantContext);
        var sessionInterceptor = new PostgresTenantSessionInterceptor(tenantContext);
        var txInterceptor = new PostgresTenantTransactionInterceptor(tenantContext);

        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(AppConnectionString)
            .UseSnakeCaseNamingConvention()
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .AddInterceptors(auditInterceptor, sessionInterceptor, txInterceptor)
            .Options;

        return new OrganizationDbContext(options, tenantContext);
    }

    public async Task<DbConnection> OpenRawConnectionAsync(string? tenantId = null)
    {
        var connection = new NpgsqlConnection(AppConnectionString);
        await connection.OpenAsync();

        if (tenantId is not null)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT set_config('app.current_tenant_id', @t, false);";
            cmd.Parameters.Add(new NpgsqlParameter("@t", tenantId));
            await cmd.ExecuteNonQueryAsync();
        }

        return connection;
    }
}
