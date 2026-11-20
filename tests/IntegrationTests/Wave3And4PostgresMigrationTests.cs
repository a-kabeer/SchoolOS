using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Admissions.Persistence;
using SchoolOS.Modules.Organization.Persistence;
using SchoolOS.PlatformModules.Subscriptions.Persistence;
using SchoolOS.PlatformModules.Tenants.Persistence;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
public class Wave3And4PostgresMigrationTests
{
    private static readonly string[] Wave3SubscriptionTenantTables =
    [
        "subscriptions",
        "tenant_modules",
        "invoices",
        "invoice_lines",
        "payments",
        "add_on_requests",
        "renewal_reminders"
    ];

    private static readonly string[] Wave4OrganizationTenantTables =
    [
        "schools",
        "campuses",
        "campus_details",
        "campus_phones",
        "branding_images",
        "school_brandings",
        "school_profiles"
    ];

    private static readonly string[] Wave4AdmissionsTenantTables =
    [
        "admission_enquiries",
        "enquiry_follow_ups",
        "campus_visits",
        "student_profiles"
    ];

    [Fact]
    public async Task PlatformAndSubscriptions_CleanDatabase_Up_Down_Up_Enforces_ForceRls_And_Blocks_AppRole_Bypass()
    {
        const string tempDbName = "schoolos_wave3_migration_verify";
        var adminConnString = $"Host=127.0.0.1;Port=5432;Database={tempDbName};Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";
        var appConnString = $"Host=127.0.0.1;Port=5432;Database={tempDbName};Username=schoolos_app;Password=schoolos_app_password;Include Error Detail=true";
        var platformAdminConnString = $"Host=127.0.0.1;Port=5432;Database={tempDbName};Username=schoolos_platform_admin;Password=schoolos_platform_admin_password;Include Error Detail=true";

        await using var masterConn = new NpgsqlConnection(PostgresDatabaseFixture.MasterConnectionString);
        await masterConn.OpenAsync();

        try
        {
            await RecreateDatabaseAsync(masterConn, tempDbName);

            var platOptions = new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql(adminConnString)
                .UseSnakeCaseNamingConvention()
                .Options;

            var subOptions = new DbContextOptionsBuilder<SubscriptionsDbContext>()
                .UseNpgsql(adminConnString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "subscriptions");
                })
                .UseSnakeCaseNamingConvention()
                .Options;

            var systemCtx = new TenantContext(null, null, "wave3-migration-tester");
            await using var platDb = new PlatformDbContext(platOptions);
            await using var subDb = new SubscriptionsDbContext(subOptions, systemCtx);

            // 1. Migrate Up on a completely empty database
            await platDb.Database.MigrateAsync();
            await subDb.Database.MigrateAsync();

            (await platDb.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
            (await subDb.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
            (await platDb.Database.GetAppliedMigrationsAsync()).ShouldContain(m => m.Contains("Initial_Platform_Create"));
            (await platDb.Database.GetAppliedMigrationsAsync()).ShouldContain(m => m.Contains("Add_SchoolRequests_And_GeoReference"));
            (await subDb.Database.GetAppliedMigrationsAsync()).ShouldContain(m => m.Contains("Initial_Subscriptions_Create"));

            // 2. Verify ENABLE + FORCE RLS and policies via PostgreSQL system catalogs
            await using (var adminConn = new NpgsqlConnection(adminConnString))
            {
                await adminConn.OpenAsync();

                // Verify all 7 tenant-scoped subscriptions tables enforce RLS
                await using (var rlsCmd = adminConn.CreateCommand())
                {
                    rlsCmd.CommandText = """
                        SELECT c.relname
                        FROM pg_class c
                        JOIN pg_namespace n ON n.oid = c.relnamespace
                        WHERE n.nspname = 'subscriptions'
                          AND c.relkind = 'r'
                          AND c.relrowsecurity = true
                          AND c.relforcerowsecurity = true
                        ORDER BY c.relname;
                        """;
                    await using var reader = await rlsCmd.ExecuteReaderAsync();
                    var enforcedTables = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        enforcedTables.Add(reader.GetString(0));
                    }

                    enforcedTables.ShouldBe(Wave3SubscriptionTenantTables, ignoreOrder: true);
                }

                // Verify role flags for schoolos_platform_admin (NOINHERIT NOSUPERUSER NOBYPASSRLS)
                await using (var roleCmd = adminConn.CreateCommand())
                {
                    roleCmd.CommandText = """
                        SELECT rolsuper, rolbypassrls, rolinherit
                        FROM pg_roles
                        WHERE rolname = 'schoolos_platform_admin';
                        """;
                    await using var reader = await roleCmd.ExecuteReaderAsync();
                    (await reader.ReadAsync()).ShouldBeTrue("schoolos_platform_admin role must exist after Subscriptions migration");
                    reader.GetBoolean(0).ShouldBeFalse("schoolos_platform_admin must be NOSUPERUSER");
                    reader.GetBoolean(1).ShouldBeFalse("schoolos_platform_admin must be NOBYPASSRLS");
                    reader.GetBoolean(2).ShouldBeFalse("schoolos_platform_admin must be NOINHERIT");
                }

                // Verify policies: role-scoped platform_admin_access + tenant isolation on all 7 tables, zero is_platform_admin GUC refs
                await using (var polCmd = adminConn.CreateCommand())
                {
                    polCmd.CommandText = """
                        SELECT schemaname, tablename, policyname, COALESCE(array_to_string(roles, ','), ''), COALESCE(qual, ''), COALESCE(with_check, '')
                        FROM pg_policies
                        WHERE schemaname IN ('subscriptions', 'platform')
                        ORDER BY tablename, policyname;
                        """;
                    await using var reader = await polCmd.ExecuteReaderAsync();
                    var policies = new List<(string Schema, string Table, string Policy, string Roles, string Qual, string WithCheck)>();
                    while (await reader.ReadAsync())
                    {
                        policies.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5)));
                    }

                    policies.Count.ShouldBe(14);
                    foreach (var p in policies)
                    {
                        p.Schema.ShouldBe("subscriptions");
                        p.Qual.ShouldNotContain("is_platform_admin");
                        p.WithCheck.ShouldNotContain("is_platform_admin");
                    }

                    var platformPolicies = policies.Where(p => p.Policy == $"{p.Table}_platform_admin_access").ToList();
                    platformPolicies.Select(p => p.Table).ShouldBe(Wave3SubscriptionTenantTables, ignoreOrder: true);
                    platformPolicies.ShouldAllBe(p => p.Roles == "schoolos_platform_admin");

                    var tenantPolicies = policies.Where(p => p.Policy == $"{p.Table}_tenant_isolation").ToList();
                    tenantPolicies.Select(p => p.Table).ShouldBe(Wave3SubscriptionTenantTables, ignoreOrder: true);
                }

                // 3. Grant least-privilege runtime DML grants to schoolos_app and schoolos_platform_admin, and verify access boundaries
                await using (var grantCmd = adminConn.CreateCommand())
                {
                    grantCmd.CommandText =
                        PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql("subscriptions", "schoolos_app") +
                        "\n" +
                        PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql("subscriptions", "schoolos_platform_admin");
                    await grantCmd.ExecuteNonQueryAsync();
                }

                var tenantA = Guid.CreateVersion7();
                var tenantB = Guid.CreateVersion7();
                await using (var seedCmd = adminConn.CreateCommand())
                {
                    seedCmd.CommandText = """
                        INSERT INTO subscriptions.subscriptions
                            (id, tenant_id, country_code, price_book_id, billing_cycle, currency_code, amount, status, start_date, end_date, created_at_utc, created_by)
                        VALUES
                            (@idA, @tenantA, 'PK', @pb, 0, 'PKR', 10000.00, 0, now(), now() + interval '14 days', now(), 'seed'),
                            (@idB, @tenantB, 'PK', @pb, 0, 'PKR', 10000.00, 0, now(), now() + interval '14 days', now(), 'seed');
                        """;
                    seedCmd.Parameters.AddWithValue("@idA", Guid.CreateVersion7());
                    seedCmd.Parameters.AddWithValue("@idB", Guid.CreateVersion7());
                    seedCmd.Parameters.AddWithValue("@tenantA", tenantA);
                    seedCmd.Parameters.AddWithValue("@tenantB", tenantB);
                    seedCmd.Parameters.AddWithValue("@pb", Guid.CreateVersion7());
                    (await seedCmd.ExecuteNonQueryAsync()).ShouldBe(2);
                }

                await using (var platConn = new NpgsqlConnection(platformAdminConnString))
                {
                    await platConn.OpenAsync();
                    await using var platReadCmd = platConn.CreateCommand();
                    platReadCmd.CommandText = "SELECT COUNT(*) FROM subscriptions.subscriptions;";
                    Convert.ToInt64(await platReadCmd.ExecuteScalarAsync()).ShouldBe(2,
                        "Independent schoolos_platform_admin role must have cross-tenant access on subscriptions without any custom GUC");
                }

                await using var appConn = new NpgsqlConnection(appConnString);
                await appConn.OpenAsync();

                await using (var attackerCmd = appConn.CreateCommand())
                {
                    attackerCmd.CommandText = "SELECT set_config('app.is_platform_admin', 'true', false), set_config('app.current_tenant_id', '', false);";
                    await attackerCmd.ExecuteNonQueryAsync();
                }

                await using (var readZeroCmd = appConn.CreateCommand())
                {
                    readZeroCmd.CommandText = "SELECT COUNT(*) FROM subscriptions.subscriptions;";
                    Convert.ToInt64(await readZeroCmd.ExecuteScalarAsync()).ShouldBe(0,
                        "NOSUPERUSER NOBYPASSRLS app role setting app.is_platform_admin = 'true' with empty tenant must see 0 rows");
                }

                await using (var setTenantACmd = appConn.CreateCommand())
                {
                    setTenantACmd.CommandText = "SELECT set_config('app.current_tenant_id', @tenantA, false);";
                    setTenantACmd.Parameters.AddWithValue("@tenantA", tenantA.ToString());
                    await setTenantACmd.ExecuteNonQueryAsync();
                }

                await using (var readScopedCmd = appConn.CreateCommand())
                {
                    readScopedCmd.CommandText = "SELECT COUNT(*) FROM subscriptions.subscriptions;";
                    Convert.ToInt64(await readScopedCmd.ExecuteScalarAsync()).ShouldBe(1,
                        "Scoped to Tenant A while app.is_platform_admin = 'true', schoolos_app must see only Tenant A");
                }
            }

            // Clear app/platform-admin pools before running Down migration
            await using (var clearAppConn = new NpgsqlConnection(appConnString))
            {
                NpgsqlConnection.ClearPool(clearAppConn);
            }
            await using (var clearPlatConn = new NpgsqlConnection(platformAdminConnString))
            {
                NpgsqlConnection.ClearPool(clearPlatConn);
            }

            // 4. Down -> verify 0 remaining tables -> Up again
            var subMigrator = subDb.GetInfrastructure().GetRequiredService<IMigrator>();
            await subMigrator.MigrateAsync("0");

            var platMigrator = platDb.GetInfrastructure().GetRequiredService<IMigrator>();
            await platMigrator.MigrateAsync("0");

            await using (var checkDownConn = new NpgsqlConnection(adminConnString))
            {
                await checkDownConn.OpenAsync();
                await using var cmd = checkDownConn.CreateCommand();
                cmd.CommandText = """
                    SELECT COUNT(*)
                    FROM pg_tables
                    WHERE schemaname IN ('subscriptions', 'platform')
                      AND tablename <> '__ef_migrations_history';
                    """;
                Convert.ToInt64(await cmd.ExecuteScalarAsync()).ShouldBe(0);
            }

            await platDb.Database.MigrateAsync();
            await subDb.Database.MigrateAsync();
            (await platDb.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
            (await subDb.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        }
        finally
        {
            await using var clearPlat = new NpgsqlConnection(platformAdminConnString);
            NpgsqlConnection.ClearPool(clearPlat);
            await using var clearApp = new NpgsqlConnection(appConnString);
            NpgsqlConnection.ClearPool(clearApp);
            await using var clearAdmin = new NpgsqlConnection(adminConnString);
            NpgsqlConnection.ClearPool(clearAdmin);
            await DropDatabaseIfExistsAsync(masterConn, tempDbName);
        }
    }

    [Fact]
    public async Task OrganizationAndAdmissions_CleanDatabase_Up_Down_Up_Enforces_ForceRls_With_No_PlatformAdmin_Policy()
    {
        const string tempDbName = "schoolos_wave4_migration_verify";
        var adminConnString = $"Host=127.0.0.1;Port=5432;Database={tempDbName};Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";

        await using var masterConn = new NpgsqlConnection(PostgresDatabaseFixture.MasterConnectionString);
        await masterConn.OpenAsync();

        try
        {
            await RecreateDatabaseAsync(masterConn, tempDbName);

            var orgOptions = new DbContextOptionsBuilder<OrganizationDbContext>()
                .UseNpgsql(adminConnString)
                .UseSnakeCaseNamingConvention()
                .Options;

            var admOptions = new DbContextOptionsBuilder<AdmissionsDbContext>()
                .UseNpgsql(adminConnString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "admissions");
                })
                .UseSnakeCaseNamingConvention()
                .Options;

            var ctx = new TenantContext(null, null, "wave4-migration-tester");
            await using var orgDb = new OrganizationDbContext(orgOptions, ctx);
            await using var admDb = new AdmissionsDbContext(admOptions, ctx);

            // 1. Migrate Up on clean database
            await orgDb.Database.MigrateAsync();
            await admDb.Database.MigrateAsync();

            (await orgDb.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
            (await admDb.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();

            await using (var checkConn = new NpgsqlConnection(adminConnString))
            {
                await checkConn.OpenAsync();

                // Verify FORCE RLS on all 7 organization + 4 admissions tenant tables
                await using (var rlsCmd = checkConn.CreateCommand())
                {
                    rlsCmd.CommandText = """
                        SELECT n.nspname, c.relname
                        FROM pg_class c
                        JOIN pg_namespace n ON n.oid = c.relnamespace
                        WHERE n.nspname IN ('organization', 'admissions')
                          AND c.relkind = 'r'
                          AND c.relrowsecurity = true
                          AND c.relforcerowsecurity = true
                        ORDER BY n.nspname, c.relname;
                        """;
                    await using var reader = await rlsCmd.ExecuteReaderAsync();
                    var orgTables = new List<string>();
                    var admTables = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        var schema = reader.GetString(0);
                        var table = reader.GetString(1);
                        if (schema == "organization") orgTables.Add(table);
                        else admTables.Add(table);
                    }

                    orgTables.ShouldBe(Wave4OrganizationTenantTables, ignoreOrder: true);
                    admTables.ShouldBe(Wave4AdmissionsTenantTables, ignoreOrder: true);
                }

                // Verify ZERO pg_policies in organization/admissions mention is_platform_admin or platform_admin_access
                await using (var polCmd = checkConn.CreateCommand())
                {
                    polCmd.CommandText = """
                        SELECT schemaname, tablename, policyname, COALESCE(array_to_string(roles, ','), ''), COALESCE(qual, ''), COALESCE(with_check, '')
                        FROM pg_policies
                        WHERE schemaname IN ('organization', 'admissions')
                        ORDER BY schemaname, tablename, policyname;
                        """;
                    await using var reader = await polCmd.ExecuteReaderAsync();
                    var policies = new List<(string Schema, string Table, string Policy, string Roles, string Qual, string WithCheck)>();
                    while (await reader.ReadAsync())
                    {
                        policies.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5)));
                    }

                    policies.Count.ShouldBe(Wave4OrganizationTenantTables.Length + Wave4AdmissionsTenantTables.Length);
                    foreach (var p in policies)
                    {
                        p.Policy.ShouldNotContain("platform_admin");
                        p.Roles.ShouldNotContain("schoolos_platform_admin");
                        p.Qual.ShouldContain("app.current_tenant_id");
                        p.WithCheck.ShouldContain("app.current_tenant_id");
                        p.Qual.ShouldNotContain("is_platform_admin");
                        p.WithCheck.ShouldNotContain("is_platform_admin");
                    }
                }
            }

            // 2. Rollback Down -> verify 0 tables -> Re-migrate Up cleanly
            var admMigrator = admDb.GetInfrastructure().GetRequiredService<IMigrator>();
            await admMigrator.MigrateAsync("0");

            var orgMigrator = orgDb.GetInfrastructure().GetRequiredService<IMigrator>();
            await orgMigrator.MigrateAsync("0");

            await using (var downCmd = new NpgsqlConnection(adminConnString))
            {
                await downCmd.OpenAsync();
                await using var cmd = downCmd.CreateCommand();
                cmd.CommandText = """
                    SELECT COUNT(*)
                    FROM pg_tables
                    WHERE schemaname IN ('organization', 'admissions')
                      AND tablename <> '__ef_migrations_history';
                    """;
                Convert.ToInt64(await cmd.ExecuteScalarAsync()).ShouldBe(0);
            }

            await orgDb.Database.MigrateAsync();
            await admDb.Database.MigrateAsync();
            (await orgDb.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
            (await admDb.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        }
        finally
        {
            await using var clearConn = new NpgsqlConnection(adminConnString);
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
