using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Exams.Domain;
using SchoolOS.Modules.Exams.Persistence;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class Phase2BExamsPostgresTests
{
    private const string ExamsDbName = "schoolos_exams_test";
    private static string AdminConn =>
        $"Host=127.0.0.1;Port=5432;Database={ExamsDbName};Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";
    private static string AppConn =>
        $"Host=127.0.0.1;Port=5432;Database={ExamsDbName};Username=schoolos_app;Password=schoolos_app_password;Include Error Detail=true";

    private static ExamsDbContext CreateContext(string connStr, ITenantContext tenantContext)
    {
        var opts = new DbContextOptionsBuilder<ExamsDbContext>()
            .UseNpgsql(connStr)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new PostgresTenantSessionInterceptor(tenantContext),
                new PostgresTenantTransactionInterceptor(tenantContext))
            .Options;
        return new ExamsDbContext(opts, tenantContext);
    }

    [Fact]
    public async Task Exams_Migrations_ShouldSupportUpDownUp_ForceRls_UniqueMarkSheet_And_NonOwnerTenantIsolation()
    {
        await using (var master = new NpgsqlConnection(PostgresDatabaseFixture.MasterConnectionString))
        {
            await master.OpenAsync();
            await using var cmd = master.CreateCommand();
            cmd.CommandText = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{ExamsDbName}' AND pid <> pg_backend_pid();";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = $"DROP DATABASE IF EXISTS {ExamsDbName};";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = $"CREATE DATABASE {ExamsDbName};";
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
            grantCmd.CommandText = PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql("exams", "schoolos_app");
            await grantCmd.ExecuteNonQueryAsync();

            var tables = new[] { "subjects", "exam_terms", "exam_papers", "mark_sheets", "student_marks" };
            foreach (var table in tables)
            {
                await using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = """
                    SELECT c.relrowsecurity, c.relforcerowsecurity, pg_get_userbyid(c.relowner)
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = 'exams' AND c.relname = @t;
                    """;
                checkCmd.Parameters.AddWithValue("@t", table);
                await using var reader = await checkCmd.ExecuteReaderAsync();
                (await reader.ReadAsync()).ShouldBeTrue($"exams.{table} must exist");
                reader.GetBoolean(0).ShouldBeTrue($"exams.{table} must have ENABLE RLS");
                reader.GetBoolean(1).ShouldBeTrue($"exams.{table} must have FORCE RLS");
                reader.GetString(2).ShouldBe("schoolos_dev");
            }

            await using var polCmd = conn.CreateCommand();
            polCmd.CommandText = """
                SELECT COUNT(*) FROM pg_policies
                WHERE schemaname = 'exams'
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

        var ctxA = new TenantContext(tenantA, campusA, "teacher-a");
        var ctxB = new TenantContext(tenantB, Guid.CreateVersion7(), "teacher-b");

        Guid sheetId;
        await using (var dbA = CreateContext(AppConn, ctxA))
        {
            var sub = Subject.Create(tenantA, schoolA, gradeId, "MATH", "Math");
            var term = ExamTerm.Create(tenantA, schoolA, campusA, yearId, "MID", "Midterm", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 15));
            var paper = ExamPaper.Create(tenantA, schoolA, campusA, term.Id, gradeId, sub.Id, 100m, 40m);
            var ms = MarkSheet.Create(tenantA, schoolA, campusA, paper.Id, sectionId);
            sheetId = ms.Id;
            dbA.Subjects.Add(sub);
            dbA.ExamTerms.Add(term);
            dbA.ExamPapers.Add(paper);
            dbA.MarkSheets.Add(ms);
            await dbA.SaveChangesAsync();

            // Duplicate (exam_paper_id, section_id) mark sheet must fail with PostgreSQL 23505
            dbA.MarkSheets.Add(MarkSheet.Create(tenantA, schoolA, campusA, paper.Id, sectionId));
            var ex = await Should.ThrowAsync<DbUpdateException>(() => dbA.SaveChangesAsync());
            ex.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe("23505");
        }

        // Cross-tenant isolation under non-owner schoolos_app
        await using (var dbB = CreateContext(AppConn, ctxB))
        {
            (await dbB.MarkSheets.IgnoreQueryFilters().Where(m => m.Id == sheetId).ToListAsync()).ShouldBeEmpty();
        }

        // Missing tenant context fails closed on raw SQL write (42501)
        await using (var rawConn = new NpgsqlConnection(AppConn))
        {
            await rawConn.OpenAsync();
            await using var rawCmd = rawConn.CreateCommand();
            rawCmd.CommandText = """
                INSERT INTO exams.subjects (id, tenant_id, school_id, grade_level_id, code, name, is_active, created_at_utc)
                VALUES (gen_random_uuid(), @t, @s, @g, 'RAW', 'Raw Subject', true, NOW());
                """;
            rawCmd.Parameters.AddWithValue("@t", tenantA);
            rawCmd.Parameters.AddWithValue("@s", schoolA);
            rawCmd.Parameters.AddWithValue("@g", gradeId);
            var pgEx = await Should.ThrowAsync<PostgresException>(() => rawCmd.ExecuteNonQueryAsync());
            pgEx.SqlState.ShouldBe("42501");
        }
    }
}
