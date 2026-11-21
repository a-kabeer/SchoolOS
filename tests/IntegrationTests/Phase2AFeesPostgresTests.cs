using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Fees.Domain;
using SchoolOS.Modules.Fees.Persistence;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class Phase2AFeesPostgresTests
{
    private const string FeesDbName = "schoolos_fees_test";
    private static string AdminConn =>
        $"Host=127.0.0.1;Port=5432;Database={FeesDbName};Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";
    private static string AppConn =>
        $"Host=127.0.0.1;Port=5432;Database={FeesDbName};Username=schoolos_app;Password=schoolos_app_password;Include Error Detail=true";

    private static FeesDbContext CreateContext(string connStr, ITenantContext tenantContext)
    {
        var opts = new DbContextOptionsBuilder<FeesDbContext>()
            .UseNpgsql(connStr)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new PostgresTenantSessionInterceptor(tenantContext),
                new PostgresTenantTransactionInterceptor(tenantContext))
            .Options;
        return new FeesDbContext(opts, tenantContext);
    }

    [Fact]
    public async Task Fees_Migrations_ShouldSupportUpDownUp_ForceRls_PartialUniqueChallan_And_NonOwnerTenantIsolation()
    {
        await using (var master = new NpgsqlConnection(PostgresDatabaseFixture.MasterConnectionString))
        {
            await master.OpenAsync();
            await using var cmd = master.CreateCommand();
            cmd.CommandText = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{FeesDbName}' AND pid <> pg_backend_pid();";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = $"DROP DATABASE IF EXISTS {FeesDbName};";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = $"CREATE DATABASE {FeesDbName};";
            await cmd.ExecuteNonQueryAsync();
        }

        var sysCtx = new TenantContext(null, null, "migration-runner");
        await using (var db = CreateContext(AdminConn, sysCtx))
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync();
            await migrator.MigrateAsync("0");
            await migrator.MigrateAsync();
        }

        await using (var conn = new NpgsqlConnection(AdminConn))
        {
            await conn.OpenAsync();
            await using var grantCmd = conn.CreateCommand();
            grantCmd.CommandText = PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql("fees", "schoolos_app");
            await grantCmd.ExecuteNonQueryAsync();

            var tables = new[]
            {
                "fee_heads",
                "fee_schedules",
                "fee_challans",
                "fee_challan_lines",
                "fee_receipts",
                "fee_posting_outbox"
            };

            foreach (var table in tables)
            {
                await using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = """
                    SELECT c.relrowsecurity, c.relforcerowsecurity, pg_get_userbyid(c.relowner)
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = 'fees' AND c.relname = @t;
                    """;
                checkCmd.Parameters.AddWithValue("@t", table);
                await using var reader = await checkCmd.ExecuteReaderAsync();
                (await reader.ReadAsync()).ShouldBeTrue($"fees.{table} must exist");
                reader.GetBoolean(0).ShouldBeTrue($"fees.{table} must have ENABLE RLS");
                reader.GetBoolean(1).ShouldBeTrue($"fees.{table} must have FORCE RLS");
                reader.GetString(2).ShouldBe("schoolos_dev");
            }

            await using var polCmd = conn.CreateCommand();
            polCmd.CommandText = """
                SELECT COUNT(*) FROM pg_policies
                WHERE schemaname = 'fees'
                  AND (policyname LIKE '%platform_admin%' OR COALESCE(qual, '') LIKE '%is_platform_admin%');
                """;
            Convert.ToInt64(await polCmd.ExecuteScalarAsync()).ShouldBe(0L);
        }

        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var schoolA = Guid.CreateVersion7();
        var campusA = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();
        var yearId = Guid.CreateVersion7();
        var gradeId = Guid.CreateVersion7();
        var headId = Guid.CreateVersion7();

        var ctxA = new TenantContext(tenantA, campusA, "cashier-a");
        var ctxB = new TenantContext(tenantB, Guid.CreateVersion7(), "cashier-b");

        Guid challanId;
        await using (var dbA = CreateContext(AppConn, ctxA))
        {
            var c1 = FeeChallan.Create(
                tenantA, schoolA, campusA, studentId, null, yearId, gradeId,
                "2026-11", "CH-PG-001", "PKR", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 10),
                [FeeChallanLine.Create(tenantA, headId, "Tuition", 10000m)]);
            challanId = c1.Id;
            dbA.FeeChallans.Add(c1);
            await dbA.SaveChangesAsync();

            // Duplicate active challan for same student & billing period must fail with PostgreSQL 23505
            var dup = FeeChallan.Create(
                tenantA, schoolA, campusA, studentId, null, yearId, gradeId,
                "2026-11", "CH-PG-002", "PKR", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 10),
                [FeeChallanLine.Create(tenantA, headId, "Tuition", 10000m)]);
            dbA.FeeChallans.Add(dup);
            var ex = await Should.ThrowAsync<DbUpdateException>(() => dbA.SaveChangesAsync());
            ex.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe("23505");
        }

        // Cross-tenant isolation under non-owner schoolos_app
        await using (var dbB = CreateContext(AppConn, ctxB))
        {
            (await dbB.FeeChallans.IgnoreQueryFilters().Where(c => c.Id == challanId).ToListAsync()).ShouldBeEmpty();
        }

        // Missing tenant context fails closed on raw SQL write (42501)
        await using (var rawConn = new NpgsqlConnection(AppConn))
        {
            await rawConn.OpenAsync();
            await using var rawCmd = rawConn.CreateCommand();
            rawCmd.CommandText = """
                INSERT INTO fees.fee_heads (id, tenant_id, school_id, code, name, frequency, is_optional, is_active, created_at_utc)
                VALUES (gen_random_uuid(), @t, @s, 'RAW', 'Raw Head', 1, false, true, NOW());
                """;
            rawCmd.Parameters.AddWithValue("@t", tenantA);
            rawCmd.Parameters.AddWithValue("@s", schoolA);
            var pgEx = await Should.ThrowAsync<PostgresException>(() => rawCmd.ExecuteNonQueryAsync());
            pgEx.SqlState.ShouldBe("42501");
        }
    }
}
