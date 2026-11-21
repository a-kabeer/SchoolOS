using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Attendance.Domain;
using SchoolOS.Modules.Attendance.Persistence;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class Phase1CAttendancePostgresTests
{
    private const string AttendanceDbName = "schoolos_attendance_test";
    private static string AdminConn =>
        $"Host=127.0.0.1;Port=5432;Database={AttendanceDbName};Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";
    private static string AppConn =>
        $"Host=127.0.0.1;Port=5432;Database={AttendanceDbName};Username=schoolos_app;Password=schoolos_app_password;Include Error Detail=true";

    private static AttendanceDbContext CreateContext(string connStr, ITenantContext tenantContext)
    {
        var opts = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseNpgsql(connStr)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new PostgresTenantSessionInterceptor(tenantContext),
                new PostgresTenantTransactionInterceptor(tenantContext))
            .Options;
        return new AttendanceDbContext(opts, tenantContext);
    }

    [Fact]
    public async Task Attendance_Migrations_ShouldSupportUpDownUp_ForceRls_UniqueSectionDate_And_NonOwnerTenantIsolation()
    {
        await using (var master = new NpgsqlConnection(PostgresDatabaseFixture.MasterConnectionString))
        {
            await master.OpenAsync();
            await using var cmd = master.CreateCommand();
            cmd.CommandText = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{AttendanceDbName}' AND pid <> pg_backend_pid();";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = $"DROP DATABASE IF EXISTS {AttendanceDbName};";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = $"CREATE DATABASE {AttendanceDbName};";
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
            grantCmd.CommandText = PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql("attendance", "schoolos_app");
            await grantCmd.ExecuteNonQueryAsync();

            var tables = new[]
            {
                "staff_members",
                "teacher_section_assignments",
                "daily_attendance_registers",
                "attendance_entries",
                "attendance_correction_audits"
            };

            foreach (var table in tables)
            {
                await using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = """
                    SELECT c.relrowsecurity, c.relforcerowsecurity, pg_get_userbyid(c.relowner)
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = 'attendance' AND c.relname = @t;
                    """;
                checkCmd.Parameters.AddWithValue("@t", table);
                await using var reader = await checkCmd.ExecuteReaderAsync();
                (await reader.ReadAsync()).ShouldBeTrue($"attendance.{table} must exist");
                reader.GetBoolean(0).ShouldBeTrue($"attendance.{table} must have ENABLE RLS");
                reader.GetBoolean(1).ShouldBeTrue($"attendance.{table} must have FORCE RLS");
                reader.GetString(2).ShouldBe("schoolos_dev");
            }

            // Zero platform_admin policies on Local attendance schema
            await using var polCmd = conn.CreateCommand();
            polCmd.CommandText = """
                SELECT COUNT(*) FROM pg_policies
                WHERE schemaname = 'attendance'
                  AND (policyname LIKE '%platform_admin%' OR COALESCE(qual, '') LIKE '%is_platform_admin%');
                """;
            Convert.ToInt64(await polCmd.ExecuteScalarAsync()).ShouldBe(0L);
        }

        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var schoolA = Guid.CreateVersion7();
        var campusA = Guid.CreateVersion7();
        var yearId = Guid.CreateVersion7();
        var gradeId = Guid.CreateVersion7();
        var sectionId = Guid.CreateVersion7();
        var date = new DateOnly(2026, 11, 21);

        var ctxA = new TenantContext(tenantA, campusA, "teacher-a");
        var ctxB = new TenantContext(tenantB, Guid.CreateVersion7(), "teacher-b");
        var ctxMissing = new TenantContext(null, null, "unscoped");

        Guid registerId;
        await using (var dbA = CreateContext(AppConn, ctxA))
        {
            var reg = DailyAttendanceRegister.Create(tenantA, schoolA, campusA, yearId, gradeId, sectionId, date, "teacher-a", DateTimeOffset.UtcNow);
            registerId = reg.Id;
            dbA.DailyAttendanceRegisters.Add(reg);
            await dbA.SaveChangesAsync();

            // Duplicate (section, date) register must fail with PostgreSQL 23505 unique violation
            var dup = DailyAttendanceRegister.Create(tenantA, schoolA, campusA, yearId, gradeId, sectionId, date, "teacher-a", DateTimeOffset.UtcNow);
            dbA.DailyAttendanceRegisters.Add(dup);
            var ex = await Should.ThrowAsync<DbUpdateException>(() => dbA.SaveChangesAsync());
            ex.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe("23505");
        }

        // Cross-tenant isolation under non-owner schoolos_app
        await using (var dbB = CreateContext(AppConn, ctxB))
        {
            var leaked = await dbB.DailyAttendanceRegisters.IgnoreQueryFilters().Where(r => r.Id == registerId).ToListAsync();
            leaked.ShouldBeEmpty();
        }

        // Missing tenant context fails closed (0 rows + raw SQL 42501)
        await using (var dbMissing = CreateContext(AppConn, ctxMissing))
        {
            (await dbMissing.DailyAttendanceRegisters.IgnoreQueryFilters().ToListAsync()).ShouldBeEmpty();
        }

        await using (var rawConn = new NpgsqlConnection(AppConn))
        {
            await rawConn.OpenAsync();
            await using var rawCmd = rawConn.CreateCommand();
            rawCmd.CommandText = """
                INSERT INTO attendance.staff_members (id, tenant_id, school_id, campus_id, employee_code, first_name, last_name, role_type, is_active, created_at_utc)
                VALUES (gen_random_uuid(), @t, @s, @c, 'RAW-1', 'Raw', 'Fail', 1, true, NOW());
                """;
            rawCmd.Parameters.AddWithValue("@t", tenantA);
            rawCmd.Parameters.AddWithValue("@s", schoolA);
            rawCmd.Parameters.AddWithValue("@c", campusA);
            var pgEx = await Should.ThrowAsync<PostgresException>(() => rawCmd.ExecuteNonQueryAsync());
            pgEx.SqlState.ShouldBe("42501");
        }
    }
}
