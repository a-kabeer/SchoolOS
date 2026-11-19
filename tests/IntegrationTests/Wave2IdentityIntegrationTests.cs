using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Identity.Domain;
using SchoolOS.Modules.Identity.Persistence;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

public class Wave2IdentityHttpTests : IClassFixture<LocalApiFactory>
{
    private readonly LocalApiFactory _localFactory;
    private readonly JwtOptions _jwtOptions = new();
    private static readonly JsonSerializerOptions NamedEnumJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Wave2IdentityHttpTests(LocalApiFactory localFactory)
    {
        _localFactory = localFactory;
    }

    private string CreateAdminToken(Guid tenantId, string userId = "01931a74-0000-7000-8000-000000000001")
    {
        return JwtTokenHelper.GenerateToken(
            _jwtOptions,
            userId,
            tenantId: tenantId,
            roles: [SecurityPolicies.TenantAdminRole]);
    }

    [Fact]
    public async Task Provision_Login_OtpChangePassword_Rbac403_And_RefreshReuseDetection_Work_EndToEnd()
    {
        var tenantId = Guid.CreateVersion7();
        var schoolId = Guid.CreateVersion7();
        var campusId = Guid.CreateVersion7();

        var client = _localFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAdminToken(tenantId));

        // 1. Provision an OTP teacher user (only has usersroles:view via custom role, NOT usersroles:create)
        var roleResp = await client.PostAsJsonAsync("/api/v1/identity/roles", new
        {
            SchoolId = schoolId,
            Name = "Directory Reader",
            Permissions = new[]
            {
                new { Module = ModuleCode.UsersRoles, CanView = true, CanCreate = false, CanEdit = false, CanDelete = false }
            }
        }, NamedEnumJson);
        var roleBody = await roleResp.Content.ReadAsStringAsync();
        roleResp.StatusCode.ShouldBe(HttpStatusCode.Created, roleBody);
        using var roleDoc = JsonDocument.Parse(roleBody);
        var roleId = roleDoc.RootElement.GetProperty("id").GetGuid();

        const string email = "amna.tariq@beacon.pk";
        const string otpPassword = "Otp-Temporary-Passphrase-2026!";
        const string finalPassword = "Final-User-Passphrase-2026!";

        var createResp = await client.PostAsJsonAsync("/api/v1/identity/users", new
        {
            SchoolId = schoolId,
            Email = email,
            FirstName = "Amna",
            LastName = "Tariq",
            UserType = UserType.Staff,
            InitialPassword = otpPassword,
            MustChangePasswordOnNextLogin = true,
            PreferredLanguage = "ur",
            CampusIds = new[] { campusId },
            RoleIds = new[] { roleId }
        }, NamedEnumJson);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // 2. Login anonymously as OTP user -> returns access token + HttpOnly schoolos_rt cookie
        var anonClient = _localFactory.CreateClient();
        var loginResp = await anonClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = otpPassword,
            CampusId = campusId,
            TenantId = tenantId
        }, NamedEnumJson);
        loginResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        loginResp.Headers.TryGetValues("Set-Cookie", out var setCookies).ShouldBeTrue();
        var cookieHeader = setCookies!.First(c => c.StartsWith("schoolos_rt=", StringComparison.Ordinal));
        cookieHeader.ShouldContain("httponly", Case.Insensitive);
        cookieHeader.ShouldContain("samesite=strict", Case.Insensitive);
        var rawRefresh1 = Uri.UnescapeDataString(cookieHeader.Split(';')[0]["schoolos_rt=".Length..]);

        using var loginDoc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        var otpAccessToken = loginDoc.RootElement.GetProperty("accessToken").GetString()!;
        loginDoc.RootElement.GetProperty("user").GetProperty("mustChangePassword").GetBoolean().ShouldBeTrue();

        // 3. Using OTP token against operational /api/v1/identity/users MUST return 403 PASSWORD_CHANGE_REQUIRED
        var otpClient = _localFactory.CreateClient();
        otpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otpAccessToken);
        var blockedByOtpResp = await otpClient.GetAsync("/api/v1/identity/users");
        blockedByOtpResp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var blockedBody = await blockedByOtpResp.Content.ReadAsStringAsync();
        blockedBody.ShouldContain("PASSWORD_CHANGE_REQUIRED");

        // 4. Change password (permitted while pwd_change_required=true)
        var changePwdResp = await otpClient.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            CurrentPassword = otpPassword,
            NewPassword = finalPassword
        }, NamedEnumJson);
        changePwdResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // 5. Re-login with new password (MustChangePassword is now false) -> 200 OK on GET /users, 403 PERMISSION_DENIED on POST /users
        var login2Resp = await anonClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = finalPassword,
            CampusId = campusId,
            TenantId = tenantId
        }, NamedEnumJson);
        login2Resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        login2Resp.Headers.TryGetValues("Set-Cookie", out var setCookies2).ShouldBeTrue();
        var rawRefresh2 = Uri.UnescapeDataString(setCookies2!.First(c => c.StartsWith("schoolos_rt=")).Split(';')[0]["schoolos_rt=".Length..]);

        using var login2Doc = JsonDocument.Parse(await login2Resp.Content.ReadAsStringAsync());
        var activeToken = login2Doc.RootElement.GetProperty("accessToken").GetString()!;
        login2Doc.RootElement.GetProperty("user").GetProperty("mustChangePassword").GetBoolean().ShouldBeFalse();

        var activeClient = _localFactory.CreateClient();
        activeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", activeToken);

        var getUsersResp = await activeClient.GetAsync("/api/v1/identity/users");
        getUsersResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var deniedCreateResp = await activeClient.PostAsJsonAsync("/api/v1/identity/users", new
        {
            SchoolId = schoolId,
            Email = "unauthorized@beacon.pk",
            FirstName = "No",
            LastName = "Access",
            UserType = UserType.Staff,
            InitialPassword = "Unrelated-Passphrase-2026!"
        }, NamedEnumJson);
        deniedCreateResp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await deniedCreateResp.Content.ReadAsStringAsync()).ShouldContain("PERMISSION_DENIED");

        // 6. Refresh token rotation and replay detection (old token1 was revoked on ChangePassword, and token2 rotates to token3)
        var refreshResp = await anonClient.PostAsJsonAsync("/api/v1/auth/refresh", new { RefreshToken = rawRefresh2 });
        refreshResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Replaying rawRefresh2 after rotation MUST fail with 401 SESSION_COMPROMISED
        var replayResp = await anonClient.PostAsJsonAsync("/api/v1/auth/refresh", new { RefreshToken = rawRefresh2 });
        replayResp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await replayResp.Content.ReadAsStringAsync()).ShouldContain("SESSION_COMPROMISED");

        // Old pre-change-password token also rejected
        var oldReplay = await anonClient.PostAsJsonAsync("/api/v1/auth/refresh", new { RefreshToken = rawRefresh1 });
        oldReplay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

[Collection(PostgresDatabaseCollection.Name)]
public class Wave2IdentityPostgresMigrationTests
{
    public Wave2IdentityPostgresMigrationTests(PostgresDatabaseFixture _) { }

    [Fact]
    public async Task IdentityDbContext_CleanDatabase_Migration_Up_Down_Up_Enforces_ForceRls_On_All_Tenant_Tables()
    {
        const string tempDb = "schoolos_identity_migration_verify";
        var tempAdminConnString = $"Host=127.0.0.1;Port=5432;Database={tempDb};Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";

        await using var masterConn = new NpgsqlConnection(PostgresDatabaseFixture.MasterConnectionString);
        await masterConn.OpenAsync();

        try
        {
            await RecreateDatabaseAsync(masterConn, tempDb);

            var options = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(tempAdminConnString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
                .UseSnakeCaseNamingConvention()
                .Options;

            var ctx = new TenantContext(null, null, "identity-migration-tester");
            await using var db = new IdentityDbContext(options, ctx);

            await db.Database.MigrateAsync();
            (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
            (await db.Database.GetAppliedMigrationsAsync()).ShouldContain(m => m.Contains("Initial_Identity_Create"));

            await using (var checkConn = new NpgsqlConnection(tempAdminConnString))
            {
                await checkConn.OpenAsync();
                await using var rlsCmd = checkConn.CreateCommand();
                rlsCmd.CommandText = """
                    SELECT COUNT(*)
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = 'identity'
                      AND c.relname IN (
                          'users', 'user_credentials', 'user_sessions',
                          'school_roles', 'role_permissions',
                          'user_campus_assignments', 'user_role_assignments'
                      )
                      AND c.relrowsecurity = true
                      AND c.relforcerowsecurity = true;
                    """;
                Convert.ToInt64(await rlsCmd.ExecuteScalarAsync()).ShouldBe(7);

                await using var polCmd = checkConn.CreateCommand();
                polCmd.CommandText = """
                    SELECT COUNT(*)
                    FROM pg_policies
                    WHERE schemaname = 'identity'
                      AND (COALESCE(qual, '') ILIKE '%is_platform_admin%' OR COALESCE(with_check, '') ILIKE '%is_platform_admin%');
                    """;
                Convert.ToInt64(await polCmd.ExecuteScalarAsync()).ShouldBe(0);
            }

            var migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();
            await migrator.MigrateAsync("0");

            await using (var downConn = new NpgsqlConnection(tempAdminConnString))
            {
                await downConn.OpenAsync();
                await using var downCmd = downConn.CreateCommand();
                downCmd.CommandText = "SELECT COUNT(*) FROM pg_tables WHERE schemaname = 'identity';";
                Convert.ToInt64(await downCmd.ExecuteScalarAsync()).ShouldBe(0);
            }

            await db.Database.MigrateAsync();
            (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        }
        finally
        {
            await using var clearConn = new NpgsqlConnection(tempAdminConnString);
            NpgsqlConnection.ClearPool(clearConn);
            await DropDatabaseIfExistsAsync(masterConn, tempDb);
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
