using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Persistence;
using Xunit;

namespace SchoolOS.IntegrationTests;

public class PostgresDatabaseFixture : IAsyncLifetime
{
    public const string AdminConnectionString = "Host=127.0.0.1;Port=5432;Database=schoolos_test;Username=schoolos_dev;Password=schoolos_dev_password";
    public const string AppConnectionString = "Host=127.0.0.1;Port=5432;Database=schoolos_test;Username=schoolos_app;Password=schoolos_app_password";

    public async Task InitializeAsync()
    {
        // Setup schema and initial structure using admin credentials
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(AdminConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        var systemContext = new TenantContext(null, null, "system-setup");
        await using var db = new OrganizationDbContext(options, systemContext);

        // Ensure clean test database and apply formal EF Core migrations
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        // Ensure non-superuser application role exists (superusers bypass PostgreSQL RLS by engine design)
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'schoolos_app') THEN
                    CREATE ROLE schoolos_app WITH LOGIN PASSWORD 'schoolos_app_password' NOSUPERUSER NOBYPASSRLS;
                ELSE
                    ALTER ROLE schoolos_app WITH PASSWORD 'schoolos_app_password' NOSUPERUSER NOBYPASSRLS;
                END IF;
            END
            $$;
            GRANT USAGE, CREATE ON SCHEMA organization TO schoolos_app;
            GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA organization TO schoolos_app;
            GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA organization TO schoolos_app;
            ALTER DEFAULT PRIVILEGES IN SCHEMA organization GRANT ALL PRIVILEGES ON TABLES TO schoolos_app;
            ALTER DEFAULT PRIVILEGES IN SCHEMA organization GRANT ALL PRIVILEGES ON SEQUENCES TO schoolos_app;
            """);

        // Enable PostgreSQL Row-Level Security
        var tables = new List<(string Schema, string Table)>
        {
            ("organization", "schools"),
            ("organization", "campuses")
        };
        await PostgresRlsHelper.ApplyRlsPoliciesAsync(db, tables);
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
