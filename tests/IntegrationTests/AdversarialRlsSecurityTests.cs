using Shouldly;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Domain;
using SchoolOS.Modules.Organization.Persistence;
using SchoolOS.PlatformModules.Tenants.Persistence;
using Xunit;

namespace SchoolOS.IntegrationTests;

/// <summary>
/// Adversarial PostgreSQL 17 RLS security test suite.
/// Verifies empirically that:
/// 1. A NOSUPERUSER NOBYPASSRLS tenant runtime role CAN set custom app.* GUCs, proving custom GUC bypasses are unsafe.
/// 2. SchoolOS RLS policies do NOT honor app.is_platform_admin or any custom GUC bypass.
/// 3. Role escalation (SET ROLE), DDL tampering (DISABLE RLS / DROP POLICY / CREATE TABLE), and TRUNCATE are denied (42501).
/// 4. EF Core migrations alone enforce ENABLE + FORCE ROW LEVEL SECURITY, non-owner runtime role, and least-privilege grants.
/// 5. Missing tenant context rejects writes at both PostgreSQL RLS and EF Core DbContext layers (even when IsPlatformAdmin = true).
/// 6. Platform-wide cross-tenant access on allowlisted Cloud tables works ONLY via an independently authenticated PostgreSQL role,
///    never via custom GUC and never on Local Data Plane organization.* tables.
/// 7. Connection pool reuse (same pg_backend_pid()) and transaction rollbacks/aborts leave zero stale tenant context.
/// 8. Clean-database migration Up -> Down -> Up lifecycle works cleanly and idempotently.
/// </summary>
[Collection(PostgresDatabaseCollection.Name)]
public class AdversarialRlsSecurityTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public AdversarialRlsSecurityTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CustomGuc_CanBeSetByNoBypassRlsRole_And_CannotBypassSchoolOsRls()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();

        // Seed a protected row under Tenant A
        await using (var dbA = _fixture.CreateDbContext(new TenantContext(tenantA, null, "user-a")))
        {
            dbA.Schools.Add(School.Create(tenantA, "Adversarial Victim School", "ADV-VICTIM"));
            await dbA.SaveChangesAsync();
        }

        // Act as an attacker with SQL execution on the normal NOSUPERUSER NOBYPASSRLS runtime role (schoolos_app)
        await using var attackerConn = new NpgsqlConnection(PostgresDatabaseFixture.AppConnectionString);
        await attackerConn.OpenAsync();

        // 1. Empirically prove NOBYPASSRLS does NOT block setting arbitrary custom app.* GUCs
        await using (var gucCmd = attackerConn.CreateCommand())
        {
            gucCmd.CommandText = "SELECT set_config('app.is_platform_admin', 'true', false);";
            var gucValue = (string?)await gucCmd.ExecuteScalarAsync();
            gucValue.ShouldBe("true",
                "PostgreSQL allows any role (including NOSUPERUSER NOBYPASSRLS) to set custom app.* placeholder GUCs");
        }

        // 2. With app.is_platform_admin = 'true' and NO tenant_id, Tenant A's rows must remain invisible
        await using (var clearTenantCmd = attackerConn.CreateCommand())
        {
            clearTenantCmd.CommandText = "SELECT set_config('app.current_tenant_id', '', false);";
            await clearTenantCmd.ExecuteNonQueryAsync();
        }
        await using (var readCmd = attackerConn.CreateCommand())
        {
            readCmd.CommandText = "SELECT COUNT(*) FROM organization.schools WHERE code = 'ADV-VICTIM';";
            var count = Convert.ToInt64(await readCmd.ExecuteScalarAsync());
            count.ShouldBe(0, "Custom GUC app.is_platform_admin must NEVER bypass RLS");
        }

        // 3. With app.is_platform_admin = 'true' AND tenant_id = Tenant B, Tenant A's rows must remain invisible and immutable
        await using (var setTenantBCmd = attackerConn.CreateCommand())
        {
            setTenantBCmd.CommandText = "SELECT set_config('app.current_tenant_id', @b, false);";
            setTenantBCmd.Parameters.AddWithValue("@b", tenantB.ToString());
            await setTenantBCmd.ExecuteNonQueryAsync();
        }
        await using (var crossTenantCmd = attackerConn.CreateCommand())
        {
            crossTenantCmd.CommandText = "SELECT COUNT(*) FROM organization.schools WHERE code = 'ADV-VICTIM';";
            var count = Convert.ToInt64(await crossTenantCmd.ExecuteScalarAsync());
            count.ShouldBe(0);
        }

        await using (var updateCmd = attackerConn.CreateCommand())
        {
            updateCmd.CommandText = "UPDATE organization.schools SET name = 'Pwned' WHERE code = 'ADV-VICTIM';";
            var updated = await updateCmd.ExecuteNonQueryAsync();
            updated.ShouldBe(0);
        }

        await using (var deleteCmd = attackerConn.CreateCommand())
        {
            deleteCmd.CommandText = "DELETE FROM organization.schools WHERE code = 'ADV-VICTIM';";
            var deleted = await deleteCmd.ExecuteNonQueryAsync();
            deleted.ShouldBe(0);
        }

        // 4. Attempting to forge an INSERT into Tenant A while app.is_platform_admin = 'true' must fail with 42501
        await using (var insertCmd = attackerConn.CreateCommand())
        {
            insertCmd.CommandText = """
                INSERT INTO organization.schools (id, tenant_id, name, code, is_active, created_at_utc, created_by)
                VALUES (@id, @tenantA, 'Forged School', 'FORGE-1', true, now(), 'attacker');
                """;
            insertCmd.Parameters.AddWithValue("@id", Guid.CreateVersion7());
            insertCmd.Parameters.AddWithValue("@tenantA", tenantA);

            var ex = await Assert.ThrowsAsync<PostgresException>(() => insertCmd.ExecuteNonQueryAsync());
            ex.SqlState.ShouldBe("42501");
        }
    }

    [Fact]
    public async Task RuntimeRole_CannotEscalateRole_TamperRls_Truncate_Or_CreateObjects()
    {
        await using var conn = new NpgsqlConnection(PostgresDatabaseFixture.AppConnectionString);
        await conn.OpenAsync();

        string[] forbiddenStatements =
        [
            "SET ROLE schoolos_platform_admin;",
            "SET ROLE schoolos_dev;",
            "ALTER TABLE organization.schools DISABLE ROW LEVEL SECURITY;",
            "ALTER TABLE organization.schools NO FORCE ROW LEVEL SECURITY;",
            "DROP POLICY schools_tenant_isolation ON organization.schools;",
            "TRUNCATE TABLE organization.schools;",
            "CREATE TABLE organization.rogue_table (id int);",
            "CREATE TABLE public.rogue_table (id int);"
        ];

        foreach (var sql in forbiddenStatements)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
            ex.SqlState.ShouldBe("42501", $"Statement '{sql}' must be denied with insufficient_privilege (42501)");
        }
    }

    [Fact]
    public async Task SystemCatalogs_Verify_MigrationForceRls_Ownership_RoleFlags_Grants_And_PolicyPredicates()
    {
        await using var adminConn = new NpgsqlConnection(PostgresDatabaseFixture.AdminConnectionString);
        await adminConn.OpenAsync();

        // 1. Verify ENABLE + FORCE ROW LEVEL SECURITY directly from MigrateAsync() on all organization tables
        foreach (var table in new[] { "schools", "campuses", "campus_details", "campus_phones", "branding_images", "school_brandings", "school_profiles", "academic_years", "grade_levels", "sections" })
        {
            await using var rlsCmd = adminConn.CreateCommand();
            rlsCmd.CommandText = """
                SELECT c.relrowsecurity, c.relforcerowsecurity, pg_get_userbyid(c.relowner)
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'organization' AND c.relname = @table;
                """;
            rlsCmd.Parameters.AddWithValue("@table", table);

            await using var reader = await rlsCmd.ExecuteReaderAsync();
            (await reader.ReadAsync()).ShouldBeTrue($"Table organization.{table} must exist");
            reader.GetBoolean(0).ShouldBeTrue($"organization.{table} must have ENABLE ROW LEVEL SECURITY");
            reader.GetBoolean(1).ShouldBeTrue($"organization.{table} must have FORCE ROW LEVEL SECURITY");
            reader.GetString(2).ShouldBe("schoolos_dev", $"schoolos_app must NOT own organization.{table}");
        }

        // 2. Verify pg_policies predicates strictly check app.current_tenant_id and NEVER reference is_platform_admin
        await using (var polCmd = adminConn.CreateCommand())
        {
            polCmd.CommandText = """
                SELECT tablename, policyname, COALESCE(qual, ''), COALESCE(with_check, '')
                FROM pg_policies
                WHERE schemaname = 'organization'
                ORDER BY tablename, policyname;
                """;
            await using var reader = await polCmd.ExecuteReaderAsync();
            var policies = new List<(string Table, string Policy, string Qual, string WithCheck)>();
            while (await reader.ReadAsync())
            {
                policies.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
            }

            policies.Count.ShouldBe(10);
            policies.Select(p => p.Table).ShouldBe(["academic_years", "branding_images", "campus_details", "campus_phones", "campuses", "grade_levels", "school_brandings", "school_profiles", "schools", "sections"], ignoreOrder: true);
            foreach (var p in policies)
            {
                p.Qual.ShouldContain("app.current_tenant_id");
                p.WithCheck.ShouldContain("app.current_tenant_id");
                p.Qual.ShouldNotContain("is_platform_admin");
                p.WithCheck.ShouldNotContain("is_platform_admin");
            }
        }

        // 3. Verify role attributes and non-membership for schoolos_app and schoolos_platform_admin
        foreach (var roleName in new[] { "schoolos_app", "schoolos_platform_admin" })
        {
            await using var roleCmd = adminConn.CreateCommand();
            roleCmd.CommandText = """
                SELECT rolsuper, rolbypassrls, rolcreatedb, rolcreaterole, rolinherit
                FROM pg_roles
                WHERE rolname = @role;
                """;
            roleCmd.Parameters.AddWithValue("@role", roleName);
            await using var reader = await roleCmd.ExecuteReaderAsync();
            (await reader.ReadAsync()).ShouldBeTrue($"Role {roleName} must exist");
            reader.GetBoolean(0).ShouldBeFalse($"{roleName} must be NOSUPERUSER");
            reader.GetBoolean(1).ShouldBeFalse($"{roleName} must be NOBYPASSRLS");
            reader.GetBoolean(2).ShouldBeFalse($"{roleName} must be NOCREATEDB");
            reader.GetBoolean(3).ShouldBeFalse($"{roleName} must be NOCREATEROLE");
            reader.GetBoolean(4).ShouldBeFalse($"{roleName} must be NOINHERIT");
        }

        await using (var privCmd = adminConn.CreateCommand())
        {
            privCmd.CommandText = """
                SELECT
                    pg_has_role('schoolos_app', 'schoolos_platform_admin', 'MEMBER'),
                    pg_has_role('schoolos_app', 'schoolos_dev', 'MEMBER'),
                    has_schema_privilege('schoolos_app', 'organization', 'USAGE'),
                    has_schema_privilege('schoolos_app', 'organization', 'CREATE'),
                    has_table_privilege('schoolos_app', 'organization.schools', 'SELECT'),
                    has_table_privilege('schoolos_app', 'organization.schools', 'INSERT'),
                    has_table_privilege('schoolos_app', 'organization.schools', 'UPDATE'),
                    has_table_privilege('schoolos_app', 'organization.schools', 'DELETE'),
                    has_table_privilege('schoolos_app', 'organization.schools', 'TRUNCATE'),
                    has_table_privilege('schoolos_app', 'organization.schools', 'TRIGGER'),
                    has_schema_privilege('schoolos_platform_admin', 'organization', 'USAGE');
                """;
            await using var reader = await privCmd.ExecuteReaderAsync();
            (await reader.ReadAsync()).ShouldBeTrue();
            reader.GetBoolean(0).ShouldBeFalse("schoolos_app must NOT be a member of schoolos_platform_admin");
            reader.GetBoolean(1).ShouldBeFalse("schoolos_app must NOT be a member of schoolos_dev");
            reader.GetBoolean(2).ShouldBeTrue("schoolos_app needs USAGE on organization");
            reader.GetBoolean(3).ShouldBeFalse("schoolos_app must NOT have CREATE on organization");
            reader.GetBoolean(4).ShouldBeTrue();
            reader.GetBoolean(5).ShouldBeTrue();
            reader.GetBoolean(6).ShouldBeTrue();
            reader.GetBoolean(7).ShouldBeTrue();
            reader.GetBoolean(8).ShouldBeFalse("schoolos_app must NOT have TRUNCATE");
            reader.GetBoolean(9).ShouldBeFalse("schoolos_app must NOT have TRIGGER");
            reader.GetBoolean(10).ShouldBeFalse("schoolos_platform_admin must NOT have privileges on Local organization schema");
        }
    }

    [Fact]
    public async Task MissingTenantContext_RejectsWrites_At_Both_DatabaseRls_And_EfCoreLayers()
    {
        // 1. Raw SQL INSERT with missing app.current_tenant_id fails at PostgreSQL RLS layer (42501)
        await using (var rawConn = new NpgsqlConnection(PostgresDatabaseFixture.AppConnectionString))
        {
            await rawConn.OpenAsync();
            await using var cmd = rawConn.CreateCommand();
            cmd.CommandText = """
                SELECT set_config('app.current_tenant_id', '', false);
                INSERT INTO organization.schools (id, tenant_id, name, code, is_active, created_at_utc, created_by)
                VALUES (@id, @tenant, 'No Context Raw School', 'NOCONTEXT-RAW', true, now(), 'user-x');
                """;
            cmd.Parameters.AddWithValue("@id", Guid.CreateVersion7());
            cmd.Parameters.AddWithValue("@tenant", Guid.CreateVersion7());

            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
            ex.SqlState.ShouldBe("42501");
        }

        // 2. EF Core SaveChangesAsync with null TenantId fails closed even if IsPlatformAdmin = true
        var platformAdminWithoutTenant = new TenantContext(tenantId: null, campusId: null, userId: "platform-admin", isPlatformAdmin: true);
        await using (var db = _fixture.CreateDbContext(platformAdminWithoutTenant))
        {
            db.Schools.Add(School.Create(Guid.CreateVersion7(), "No Context EF School", "NOCONTEXT-EF"));
            var ex = await Should.ThrowAsync<InvalidOperationException>(() => db.SaveChangesAsync());
            ex.Message.ShouldContain("Tenant isolation violation");
        }
    }

    [Fact]
    public async Task PlatformAdminRoleScopedPolicy_AllowsOnlyIndependentRole_OnAllowlistedCloudTable_AndDeniesLocalOrganization()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();

        await using var adminConn = new NpgsqlConnection(PostgresDatabaseFixture.AdminConnectionString);
        await adminConn.OpenAsync();

        try
        {
            // Simulate an allowlisted Cloud Control Plane table with role-scoped platform admin policy
            await using (var setupCmd = adminConn.CreateCommand())
            {
                setupCmd.CommandText = $"""
                    DROP SCHEMA IF EXISTS cloud_rls_verify CASCADE;
                    CREATE SCHEMA cloud_rls_verify;
                    CREATE TABLE cloud_rls_verify.commercial_records (
                        id uuid PRIMARY KEY,
                        tenant_id uuid NOT NULL,
                        plan_code text NOT NULL
                    );
                    {PostgresRlsHelper.GenerateEnableRlsSql("cloud_rls_verify", "commercial_records")}
                    {PostgresRlsHelper.GeneratePlatformAdminRolePolicySql("cloud_rls_verify", "commercial_records", "schoolos_platform_admin")}
                    {PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql("cloud_rls_verify", "schoolos_app")}
                    {PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql("cloud_rls_verify", "schoolos_platform_admin")}
                    """;
                await setupCmd.ExecuteNonQueryAsync();
            }

            // Seed two tenants' commercial records as platform admin role
            await using (var platConn = new NpgsqlConnection(PostgresDatabaseFixture.PlatformAdminConnectionString))
            {
                await platConn.OpenAsync();
                await using var seedCmd = platConn.CreateCommand();
                seedCmd.CommandText = """
                    INSERT INTO cloud_rls_verify.commercial_records (id, tenant_id, plan_code)
                    VALUES (@idA, @tenantA, 'PRO'), (@idB, @tenantB, 'ENTERPRISE');
                    """;
                seedCmd.Parameters.AddWithValue("@idA", Guid.CreateVersion7());
                seedCmd.Parameters.AddWithValue("@tenantA", tenantA);
                seedCmd.Parameters.AddWithValue("@idB", Guid.CreateVersion7());
                seedCmd.Parameters.AddWithValue("@tenantB", tenantB);
                (await seedCmd.ExecuteNonQueryAsync()).ShouldBe(2);

                // Platform admin role sees BOTH tenants WITHOUT any custom GUC
                await using var readAllCmd = platConn.CreateCommand();
                readAllCmd.CommandText = "SELECT COUNT(*) FROM cloud_rls_verify.commercial_records;";
                Convert.ToInt64(await readAllCmd.ExecuteScalarAsync()).ShouldBe(2);

                // Platform admin role is still strictly denied access to Local Data Plane organization.schools
                await using var deniedLocalCmd = platConn.CreateCommand();
                deniedLocalCmd.CommandText = "SELECT COUNT(*) FROM organization.schools;";
                var ex = await Assert.ThrowsAsync<PostgresException>(() => deniedLocalCmd.ExecuteScalarAsync());
                ex.SqlState.ShouldBe("42501");
            }

            // Normal tenant runtime role (schoolos_app) still CANNOT bypass RLS even with app.is_platform_admin = 'true'
            await using (var appConn = new NpgsqlConnection(PostgresDatabaseFixture.AppConnectionString))
            {
                await appConn.OpenAsync();
                await using (var setGucCmd = appConn.CreateCommand())
                {
                    setGucCmd.CommandText = "SELECT set_config('app.is_platform_admin', 'true', false), set_config('app.current_tenant_id', '', false);";
                    await setGucCmd.ExecuteNonQueryAsync();
                }
                await using (var attemptCmd = appConn.CreateCommand())
                {
                    attemptCmd.CommandText = "SELECT COUNT(*) FROM cloud_rls_verify.commercial_records;";
                    Convert.ToInt64(await attemptCmd.ExecuteScalarAsync()).ShouldBe(0);
                }

                // Scoped to Tenant A, schoolos_app sees ONLY Tenant A's record
                await using (var setTenantACmd = appConn.CreateCommand())
                {
                    setTenantACmd.CommandText = "SELECT set_config('app.current_tenant_id', @tenantA, false);";
                    setTenantACmd.Parameters.AddWithValue("@tenantA", tenantA.ToString());
                    await setTenantACmd.ExecuteNonQueryAsync();
                }
                await using (var scopedCmd = appConn.CreateCommand())
                {
                    scopedCmd.CommandText = "SELECT COUNT(*) FROM cloud_rls_verify.commercial_records;";
                    Convert.ToInt64(await scopedCmd.ExecuteScalarAsync()).ShouldBe(1);
                }
            }
        }
        finally
        {
            await using var cleanupCmd = adminConn.CreateCommand();
            cleanupCmd.CommandText = "DROP SCHEMA IF EXISTS cloud_rls_verify CASCADE;";
            await cleanupCmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task SingleSlotConnectionPool_Reuse_And_TransactionAbortRollback_Leave_Zero_Stale_TenantContext()
    {
        var tenantA = Guid.CreateVersion7();

        // Dedicated 1-connection pool string so we deterministically force physical backend PID reuse
        var singleSlotConnString = $"{PostgresDatabaseFixture.AppConnectionString};MaxPoolSize=1;MinPoolSize=1;Application Name=schoolos_pool_audit";

        try
        {
            // Seed a school for Tenant A
            await using (var seedDb = _fixture.CreateDbContext(new TenantContext(tenantA, null, "user-pool-a")))
            {
                seedDb.Schools.Add(School.Create(tenantA, "Pool Audit School A", "POOL-AUD-A"));
                await seedDb.SaveChangesAsync();
            }

            // 1. Open EF Core DbContext with interceptors on the single-slot pool under Tenant A, capture physical PID
            int pidFromTenantA;
            var contextA = new TenantContext(tenantA, null, "user-pool-a");
            var options = new DbContextOptionsBuilder<OrganizationDbContext>()
                .UseNpgsql(singleSlotConnString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    new AuditSaveChangesInterceptor(contextA),
                    new PostgresTenantSessionInterceptor(contextA),
                    new PostgresTenantTransactionInterceptor(contextA))
                .Options;

            await using (var dbA = new OrganizationDbContext(options, contextA))
            {
                await dbA.Database.OpenConnectionAsync();
                var schools = await dbA.Schools.Where(s => s.Code == "POOL-AUD-A").ToListAsync();
                schools.ShouldHaveSingleItem();

                var conn = dbA.Database.GetDbConnection();
                await using var pidCmd = conn.CreateCommand();
                pidCmd.CommandText = "SELECT pg_backend_pid();";
                pidFromTenantA = Convert.ToInt32(await pidCmd.ExecuteScalarAsync());
            }

            // 2. Borrow the EXACT SAME pooled physical connection WITHOUT interceptors or setting any tenant GUC
            await using (var reusedConn = new NpgsqlConnection(singleSlotConnString))
            {
                await reusedConn.OpenAsync();

                await using var checkCmd = reusedConn.CreateCommand();
                checkCmd.CommandText = "SELECT pg_backend_pid(), COALESCE(current_setting('app.current_tenant_id', true), ''), (SELECT COUNT(*) FROM organization.schools WHERE code = 'POOL-AUD-A');";
                await using var reader = await checkCmd.ExecuteReaderAsync();
                (await reader.ReadAsync()).ShouldBeTrue();

                reader.GetInt32(0).ShouldBe(pidFromTenantA, "Single-slot pool must reuse the same physical backend connection");
                reader.GetString(1).ShouldBeEmpty("PostgresTenantSessionInterceptor must reset app.current_tenant_id on close");
                reader.GetInt64(2).ShouldBe(0, "Reused pooled connection without tenant context must fail closed and see 0 rows");
            }

            // 3. Verify transaction-local set_config (is_local = true) automatically clears on explicit ROLLBACK and error abort
            await using (var txConn = new NpgsqlConnection(singleSlotConnString))
            {
                await txConn.OpenAsync();

                // 3a. Aborted transaction (division by zero) + rollback
                await using (var tx = await txConn.BeginTransactionAsync())
                {
                    await using (var setCmd = txConn.CreateCommand())
                    {
                        setCmd.Transaction = tx;
                        setCmd.CommandText = "SELECT set_config('app.current_tenant_id', @t, true);";
                        setCmd.Parameters.AddWithValue("@t", tenantA.ToString());
                        await setCmd.ExecuteNonQueryAsync();
                    }

                    await using (var boomCmd = txConn.CreateCommand())
                    {
                        boomCmd.Transaction = tx;
                        boomCmd.CommandText = "SELECT 1 / 0;";
                        await Assert.ThrowsAsync<PostgresException>(() => boomCmd.ExecuteScalarAsync());
                    }

                    await tx.RollbackAsync();
                }

                // After rollback of aborted tx, app.current_tenant_id must be empty and 0 rows visible
                await using var postAbortCmd = txConn.CreateCommand();
                postAbortCmd.CommandText = "SELECT COALESCE(current_setting('app.current_tenant_id', true), ''), (SELECT COUNT(*) FROM organization.schools WHERE code = 'POOL-AUD-A');";
                await using var reader = await postAbortCmd.ExecuteReaderAsync();
                (await reader.ReadAsync()).ShouldBeTrue();
                reader.GetString(0).ShouldBeEmpty("Transaction-local tenant GUC must be discarded by PostgreSQL on rollback/abort");
                reader.GetInt64(1).ShouldBe(0);
            }
        }
        finally
        {
            await using var clearConn = new NpgsqlConnection(singleSlotConnString);
            NpgsqlConnection.ClearPool(clearConn);
        }
    }

    [Fact]
    public async Task CleanDatabase_Migration_Up_Down_Up_Lifecycle_Enforces_And_CleansUp_Rls()
    {
        const string tempDbName = "schoolos_migration_verify";
        var tempAdminConnString = $"Host=127.0.0.1;Port=5432;Database={tempDbName};Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";

        await using var masterConn = new NpgsqlConnection(PostgresDatabaseFixture.MasterConnectionString);
        await masterConn.OpenAsync();

        try
        {
            await RecreateDatabaseAsync(masterConn, tempDbName);

            var options = new DbContextOptionsBuilder<OrganizationDbContext>()
                .UseNpgsql(tempAdminConnString)
                .UseSnakeCaseNamingConvention()
                .Options;

            var ctx = new TenantContext(null, null, "migration-lifecycle-tester");
            await using var db = new OrganizationDbContext(options, ctx);

            // 1. Migrate Up on a completely empty database
            await db.Database.MigrateAsync();
            (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();

            await using (var checkConn = new NpgsqlConnection(tempAdminConnString))
            {
                await checkConn.OpenAsync();
                await using var cmd = checkConn.CreateCommand();
                cmd.CommandText = """
                    SELECT COUNT(*)
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = 'organization'
                      AND c.relname IN ('schools', 'campuses')
                      AND c.relrowsecurity = true
                      AND c.relforcerowsecurity = true;
                    """;
                Convert.ToInt64(await cmd.ExecuteScalarAsync()).ShouldBe(2, "Clean-DB Up migration must enable & force RLS on both tables");
            }

            // 2. Rollback Down to initial state ("0")
            var migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();
            await migrator.MigrateAsync("0");

            await using (var checkDownConn = new NpgsqlConnection(tempAdminConnString))
            {
                await checkDownConn.OpenAsync();
                await using var cmd = checkDownConn.CreateCommand();
                cmd.CommandText = """
                    SELECT COUNT(*)
                    FROM pg_tables
                    WHERE schemaname = 'organization' AND tablename IN ('schools', 'campuses');
                    """;
                Convert.ToInt64(await cmd.ExecuteScalarAsync()).ShouldBe(0, "Down migration must cleanly drop tables and policies");
            }

            // 3. Re-migrate Up cleanly
            await db.Database.MigrateAsync();
            (await db.Database.GetAppliedMigrationsAsync()).ShouldContain(m => m.Contains("Initial_Organization_Create"));

            // 4. Verify Cloud Control Plane PlatformDbContext clean migration Up -> Down -> Up on a separate clean database
            const string platformTempDb = "schoolos_platform_migration_verify";
            var platformConnString = $"Host=127.0.0.1;Port=5432;Database={platformTempDb};Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";
            try
            {
                await RecreateDatabaseAsync(masterConn, platformTempDb);

                var platOptions = new DbContextOptionsBuilder<PlatformDbContext>()
                    .UseNpgsql(platformConnString)
                    .UseSnakeCaseNamingConvention()
                    .Options;

                await using var platDb = new PlatformDbContext(platOptions);
                await platDb.Database.MigrateAsync();
                (await platDb.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
                (await platDb.Database.GetAppliedMigrationsAsync()).ShouldContain(m => m.Contains("Initial_Platform_Create"));

                var platMigrator = platDb.GetInfrastructure().GetRequiredService<IMigrator>();
                await platMigrator.MigrateAsync("0");
                await platDb.Database.MigrateAsync();
                (await platDb.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
            }
            finally
            {
                await using var clearPlatConn = new NpgsqlConnection(platformConnString);
                NpgsqlConnection.ClearPool(clearPlatConn);
                await DropDatabaseIfExistsAsync(masterConn, platformTempDb);
            }
        }
        finally
        {
            await using var clearConn = new NpgsqlConnection(tempAdminConnString);
            NpgsqlConnection.ClearPool(clearConn);
            await DropDatabaseIfExistsAsync(masterConn, tempDbName);
        }
    }

    private static async Task RecreateDatabaseAsync(NpgsqlConnection masterConn, string dbName)
    {
        await DropDatabaseIfExistsAsync(masterConn, dbName);
        await using var createCmd = masterConn.CreateCommand();
        createCmd.CommandText = $"CREATE DATABASE {dbName};";
        await createCmd.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseIfExistsAsync(NpgsqlConnection masterConn, string dbName)
    {
        await using (var termCmd = masterConn.CreateCommand())
        {
            termCmd.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @db AND pid <> pg_backend_pid();";
            termCmd.Parameters.AddWithValue("@db", dbName);
            await termCmd.ExecuteNonQueryAsync();
        }

        await using var dropCmd = masterConn.CreateCommand();
        dropCmd.CommandText = $"DROP DATABASE IF EXISTS {dbName};";
        await dropCmd.ExecuteNonQueryAsync();
    }
}
