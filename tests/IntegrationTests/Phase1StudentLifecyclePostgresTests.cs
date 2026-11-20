using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Local.Api.LocalPlatform;
using SchoolOS.Modules.Admissions.Domain;
using SchoolOS.Modules.Admissions.Persistence;
using SchoolOS.Modules.Organization.Domain;
using SchoolOS.Modules.Organization.Persistence;
using SchoolOS.Modules.Students.Domain;
using SchoolOS.Modules.Students.Persistence;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class Phase1StudentLifecyclePostgresTests
{
    private const string Phase1DbName = "schoolos_phase1_test";
    private static string AdminConn(string dbName) =>
        $"Host=127.0.0.1;Port=5432;Database={dbName};Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";
    private static string AppConn(string dbName) =>
        $"Host=127.0.0.1;Port=5432;Database={dbName};Username=schoolos_app;Password=schoolos_app_password;Include Error Detail=true";

    private static async Task RecreateDatabaseAsync(string dbName)
    {
        await using var master = new NpgsqlConnection(PostgresDatabaseFixture.MasterConnectionString);
        await master.OpenAsync();
        await using var cmd = master.CreateCommand();
        cmd.CommandText = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{dbName}' AND pid <> pg_backend_pid();";
        await cmd.ExecuteNonQueryAsync();
        cmd.CommandText = $"DROP DATABASE IF EXISTS {dbName};";
        await cmd.ExecuteNonQueryAsync();
        cmd.CommandText = $"CREATE DATABASE {dbName};";
        await cmd.ExecuteNonQueryAsync();
    }

    private static StudentsDbContext CreateStudentsContext(string connStr, ITenantContext tenantContext)
    {
        var opts = new DbContextOptionsBuilder<StudentsDbContext>()
            .UseNpgsql(connStr)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new PostgresTenantSessionInterceptor(tenantContext),
                new PostgresTenantTransactionInterceptor(tenantContext))
            .Options;
        return new StudentsDbContext(opts, tenantContext);
    }

    private static LocalPlatformDbContext CreateLocalPlatformContext(string connStr, ITenantContext tenantContext)
    {
        var opts = new DbContextOptionsBuilder<LocalPlatformDbContext>()
            .UseNpgsql(connStr)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new PostgresTenantSessionInterceptor(tenantContext),
                new PostgresTenantTransactionInterceptor(tenantContext))
            .Options;
        return new LocalPlatformDbContext(opts, tenantContext);
    }

    [Fact]
    public async Task Students_And_LocalPlatform_Migrations_ShouldSupportUpDownUp_ForceRls_PartialUniqueIndex_And_NonOwnerTenantIsolation()
    {
        await RecreateDatabaseAsync(Phase1DbName);
        var adminConn = AdminConn(Phase1DbName);
        var appConn = AppConn(Phase1DbName);
        var sysCtx = new TenantContext(null, null, "migration-runner");

        // 1. Clean-DB Up -> Down -> Up on StudentsDbContext and LocalPlatformDbContext
        await using (var studentsDb = CreateStudentsContext(adminConn, sysCtx))
        {
            var migrator = studentsDb.GetService<IMigrator>();
            await migrator.MigrateAsync();
            await migrator.MigrateAsync("0");
            await migrator.MigrateAsync();
        }

        await using (var localDb = CreateLocalPlatformContext(adminConn, sysCtx))
        {
            var migrator = localDb.GetService<IMigrator>();
            await migrator.MigrateAsync();
            await migrator.MigrateAsync("0");
            await migrator.MigrateAsync();
        }

        // Grant least-privilege DML-only permissions to schoolos_app
        await using (var conn = new NpgsqlConnection(adminConn))
        {
            await conn.OpenAsync();
            await using var grantCmd = conn.CreateCommand();
            grantCmd.CommandText =
                PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql("students", "schoolos_app") + "\n" +
                PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql("platform_local", "schoolos_app");
            await grantCmd.ExecuteNonQueryAsync();

            // Verify ENABLE + FORCE RLS on all 7 students tables + platform_local.license_leases
            var rlsTargets = new (string Schema, string Table)[]
            {
                ("students", "students"),
                ("students", "student_status_history"),
                ("students", "guardians"),
                ("students", "student_guardians"),
                ("students", "households"),
                ("students", "household_members"),
                ("students", "enrollments"),
                ("platform_local", "license_leases")
            };

            foreach (var (schema, table) in rlsTargets)
            {
                await using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = """
                    SELECT c.relrowsecurity, c.relforcerowsecurity, pg_get_userbyid(c.relowner)
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = @s AND c.relname = @t;
                    """;
                checkCmd.Parameters.AddWithValue("@s", schema);
                checkCmd.Parameters.AddWithValue("@t", table);
                await using var reader = await checkCmd.ExecuteReaderAsync();
                (await reader.ReadAsync()).ShouldBeTrue($"{schema}.{table} must exist");
                reader.GetBoolean(0).ShouldBeTrue($"{schema}.{table} must have ENABLE RLS");
                reader.GetBoolean(1).ShouldBeTrue($"{schema}.{table} must have FORCE RLS");
                reader.GetString(2).ShouldBe("schoolos_dev");
            }

            // Verify zero platform_admin policies exist on Local schemas
            await using var polCmd = conn.CreateCommand();
            polCmd.CommandText = """
                SELECT COUNT(*) FROM pg_policies
                WHERE schemaname IN ('students', 'platform_local')
                  AND (policyname LIKE '%platform_admin%' OR COALESCE(qual, '') LIKE '%is_platform_admin%');
                """;
            Convert.ToInt64(await polCmd.ExecuteScalarAsync()).ShouldBe(0L);
        }

        // 2. Non-owner schoolos_app cross-tenant RLS isolation + partial unique active enrollment index
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var schoolA = Guid.CreateVersion7();
        var campusA = Guid.CreateVersion7();
        var yearId = Guid.CreateVersion7();
        var gradeId = Guid.CreateVersion7();

        var ctxA = new TenantContext(tenantA, campusA, "user-a");
        var ctxB = new TenantContext(tenantB, Guid.CreateVersion7(), "user-b");
        var ctxMissing = new TenantContext(null, null, "unscoped");

        Guid studentAId;
        await using (var dbA = CreateStudentsContext(appConn, ctxA))
        {
            var studentA = Student.Create(tenantA, schoolA, campusA, "PG-001", "Aisha", null, "Noor", new DateOnly(2015, 5, 1), "Female", new DateOnly(2026, 4, 1));
            studentAId = studentA.Id;
            dbA.Students.Add(studentA);

            var enrollment1 = Enrollment.Create(tenantA, schoolA, campusA, studentA.Id, yearId, gradeId, null, "R1", new DateOnly(2026, 4, 5));
            dbA.Enrollments.Add(enrollment1);
            await dbA.SaveChangesAsync();

            // Direct DB-layer enforcement: second ACTIVE enrollment for same student & academic year must fail with 23505
            var duplicateActive = Enrollment.Create(tenantA, schoolA, campusA, studentA.Id, yearId, gradeId, null, "R2", new DateOnly(2026, 4, 6));
            dbA.Enrollments.Add(duplicateActive);
            var dupEx = await Should.ThrowAsync<DbUpdateException>(() => dbA.SaveChangesAsync());
            var pgEx = dupEx.InnerException.ShouldBeOfType<PostgresException>();
            pgEx.SqlState.ShouldBe("23505");
        }

        // Tenant B cannot see Tenant A's student
        await using (var dbB = CreateStudentsContext(appConn, ctxB))
        {
            var visibleToB = await dbB.Students.IgnoreQueryFilters().Where(s => s.Id == studentAId).ToListAsync();
            visibleToB.ShouldBeEmpty();
        }

        // Missing tenant context fails closed (0 rows visible, EF write rejected by DbContext guard, raw SQL write rejected by RLS WITH CHECK 42501)
        await using (var dbMissing = CreateStudentsContext(appConn, ctxMissing))
        {
            var visibleWithoutTenant = await dbMissing.Students.IgnoreQueryFilters().ToListAsync();
            visibleWithoutTenant.ShouldBeEmpty();

            var orphan = Student.Create(tenantA, schoolA, campusA, "ORPHAN-1", "Fail", null, "Closed", new DateOnly(2015, 5, 1), null, new DateOnly(2026, 4, 1));
            dbMissing.Students.Add(orphan);
            await Should.ThrowAsync<InvalidOperationException>(() => dbMissing.SaveChangesAsync());
        }

        await using (var rawConn = new NpgsqlConnection(appConn))
        {
            await rawConn.OpenAsync();
            await using var rawCmd = rawConn.CreateCommand();
            rawCmd.CommandText = """
                INSERT INTO students.students (id, tenant_id, school_id, campus_id, admission_number, first_name, last_name, date_of_birth, admission_date, status, created_at_utc)
                VALUES (gen_random_uuid(), @t, @s, @c, 'RAW-FAIL', 'Raw', 'Fail', DATE '2015-05-01', DATE '2026-04-01', 1, NOW());
                """;
            rawCmd.Parameters.AddWithValue("@t", tenantA);
            rawCmd.Parameters.AddWithValue("@s", schoolA);
            rawCmd.Parameters.AddWithValue("@c", campusA);
            var pgEx = await Should.ThrowAsync<PostgresException>(() => rawCmd.ExecuteNonQueryAsync());
            pgEx.SqlState.ShouldBe("42501");
        }

        // 3. Durable PostgreSQL license lease persistence across DbContext instances + cross-tenant RLS isolation
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privatePem = ecdsa.ExportPkcs8PrivateKeyPem();
        var now = DateTimeOffset.UtcNow;
        var lease = OfflineLicenseLeaseVerifier.SignWithPrivateKey(
            new LicenseLeasePayload(tenantA, now.AddMinutes(-1), now.AddDays(30), "k1", "PRO", [ModuleCode.Students], SequenceNumber: 7),
            privatePem);

        await using (var lpWrite = CreateLocalPlatformContext(appConn, ctxA))
        {
            var store = new PostgresLicenseLeaseStore(lpWrite);
            await store.SetLeaseAsync(tenantA, lease);
        }

        // New DbContext instance (simulating process restart) still reads persisted lease & sequence 7
        await using (var lpRead = CreateLocalPlatformContext(appConn, ctxA))
        {
            var store = new PostgresLicenseLeaseStore(lpRead);
            var state = await store.GetStateAsync(tenantA);
            state.Lease.ShouldNotBeNull();
            state.HighestSequenceNumber.ShouldBe(7);
        }

        // Tenant B cannot see Tenant A's license lease under FORCE RLS
        await using (var lpOther = CreateLocalPlatformContext(appConn, ctxB))
        {
            var leaked = await lpOther.LicenseLeases.IgnoreQueryFilters().Where(l => l.TenantId == tenantA).ToListAsync();
            leaked.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task UpgradePath_ApplyingStudentsAndOrganizationMigrations_ShouldPreserveLegacyAdmissionsStudentProfilesUntouched()
    {
        const string upgradeDbName = "schoolos_phase1_upgrade_test";
        await RecreateDatabaseAsync(upgradeDbName);
        var adminConn = AdminConn(upgradeDbName);
        var sysCtx = new TenantContext(null, null, "upgrade-runner");

        var tenantId = Guid.CreateVersion7();
        var tenantCtx = new TenantContext(tenantId, null, "upgrade-seed");

        // 1. Apply Organization + Admissions migrations and seed a legacy admissions.student_profiles row
        var orgOpts = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(adminConn).UseSnakeCaseNamingConvention()
            .AddInterceptors(new PostgresTenantSessionInterceptor(tenantCtx), new PostgresTenantTransactionInterceptor(tenantCtx)).Options;
        var admOpts = new DbContextOptionsBuilder<AdmissionsDbContext>()
            .UseNpgsql(adminConn).UseSnakeCaseNamingConvention()
            .AddInterceptors(new PostgresTenantSessionInterceptor(tenantCtx), new PostgresTenantTransactionInterceptor(tenantCtx)).Options;

        Guid legacyProfileId;
        await using (var orgDb = new OrganizationDbContext(orgOpts, sysCtx))
        {
            await orgDb.Database.MigrateAsync();
        }
        await using (var admDb = new AdmissionsDbContext(admOpts, sysCtx))
        {
            await admDb.Database.MigrateAsync();
        }

        await using (var orgSeed = new OrganizationDbContext(orgOpts, tenantCtx))
        {
            var school = School.Create(tenantId, "Legacy School", "LEG");
            var campus = school.AddCampus("Legacy Campus", "LEG-C1", "Main Rd");
            orgSeed.Schools.Add(school);
            await orgSeed.SaveChangesAsync();

            await using var admSeed = new AdmissionsDbContext(admOpts, tenantCtx);
            var enquiry = AdmissionEnquiry.Create(tenantId, campus.Id, "Legacy Student", "Legacy Guardian", "+923001234567", null, "Grade 3", "2025-26", null);
            admSeed.AdmissionEnquiries.Add(enquiry);
            var legacyProfile = enquiry.ConvertToStudent("LEG-2025-001", "Legacy", "Student", new DateOnly(2016, 1, 15), new DateOnly(2025, 4, 1));
            legacyProfileId = legacyProfile.Id;
            admSeed.StudentProfiles.Add(legacyProfile);
            await admSeed.SaveChangesAsync();
        }

        // 2. Apply new Students + LocalPlatform migrations on top of the existing database
        await using (var studentsDb = CreateStudentsContext(adminConn, sysCtx))
        {
            await studentsDb.Database.MigrateAsync();
        }
        await using (var localDb = CreateLocalPlatformContext(adminConn, sysCtx))
        {
            await localDb.Database.MigrateAsync();
        }

        // 3. Verify legacy admissions.student_profiles row and ID remain 100% preserved
        await using (var admVerify = new AdmissionsDbContext(admOpts, tenantCtx))
        {
            var preserved = await admVerify.StudentProfiles.FirstOrDefaultAsync(p => p.Id == legacyProfileId);
            preserved.ShouldNotBeNull();
            preserved.AdmissionNumber.ShouldBe("LEG-2025-001");
            preserved.FirstName.ShouldBe("Legacy");
            preserved.LastName.ShouldBe("Student");
        }
    }
}
