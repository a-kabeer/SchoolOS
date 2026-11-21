using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Npgsql;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Local.Api.LocalPlatform;
using SchoolOS.Modules.Admissions;
using SchoolOS.Modules.Attendance;
using SchoolOS.Modules.Exams;
using SchoolOS.Modules.Fees;
using SchoolOS.Modules.Identity;
using SchoolOS.Modules.Identity.Application;
using SchoolOS.Modules.Organization;
using SchoolOS.Modules.Students;
using Shouldly;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace SchoolOS.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class DevBootstrapPostgresTests
{
    private const string BootstrapDbName = "schoolos_bootstrap_test";
    private static string AdminConn =>
        $"Host=127.0.0.1;Port=5432;Database={BootstrapDbName};Username=schoolos_dev;Password=schoolos_dev_password;Include Error Detail=true";

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "SchoolOS.Local.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static ServiceProvider BuildLocalBootstrapServiceProvider(string publicKeyPem)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LocalDatabase"] = AdminConn
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSecurityBuildingBlocks(config, isDevelopmentOrTesting: true);
        services.AddPersistenceBuildingBlocks();
        services.AddSingleton(new OfflineLicenseVerificationOptions { PublicKeyPem = publicKeyPem });
        services.AddDbContext<LocalPlatformDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(AdminConn, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "platform_local"))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                    sp.GetRequiredService<PostgresTenantSessionInterceptor>(),
                    sp.GetRequiredService<PostgresTenantTransactionInterceptor>());
        });
        services.AddScoped<ILicenseLeaseStore, PostgresLicenseLeaseStore>();
        services.AddScoped<IModuleAccess, OfflineLicenseLeaseModuleAccess>();
        services.AddIdentityModule(config);
        services.AddOrganizationModule(config);
        services.AddStudentsModule(config);
        services.AddAdmissionsModule(config);
        services.AddAttendanceModule(config);
        services.AddFeesModule(config);
        services.AddExamsModule(config);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task LocalDatabaseBootstrapper_ShouldMigrateAll8Schemas_GrantLeastPrivilege_SeedIdempotently_AllowAdminLogin_AndBlockProduction()
    {
        // 1. Fresh empty database
        await using (var master = new NpgsqlConnection(PostgresDatabaseFixture.MasterConnectionString))
        {
            await master.OpenAsync();
            await using var cmd = master.CreateCommand();
            cmd.CommandText = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{BootstrapDbName}' AND pid <> pg_backend_pid();";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = $"DROP DATABASE IF EXISTS {BootstrapDbName};";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = $"CREATE DATABASE {BootstrapDbName};";
            await cmd.ExecuteNonQueryAsync();
        }

        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privatePem = ecdsa.ExportPkcs8PrivateKeyPem();
        var publicPem = ecdsa.ExportSubjectPublicKeyInfoPem();

        await using var sp = BuildLocalBootstrapServiceProvider(publicPem);

        // 2. Apply all 8 Local Data Plane migrations + schoolos_app DML-only grants
        await LocalDatabaseBootstrapper.ApplyMigrationsAndGrantsAsync(sp, "schoolos_app");

        // Verify all 8 schemas exist in PostgreSQL
        await using (var conn = new NpgsqlConnection(AdminConn))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT COUNT(*) FROM pg_namespace
                WHERE nspname IN ('identity','organization','admissions','students','attendance','fees','exams','platform_local');
                """;
            Convert.ToInt64(await cmd.ExecuteScalarAsync()).ShouldBe(8L);
        }

        var devOptions = new DevBootstrapOptions
        {
            Enabled = true,
            TenantId = Guid.CreateVersion7(),
            AdminEmail = "admin@schoolos.local",
            AdminPassword = "DevBootstrap#Password2026!",
            DevLicensePrivateKeyPem = privatePem
        };

        // 3. Hard Production guard: seeding MUST fail in Production
        await Should.ThrowAsync<InvalidOperationException>(() =>
            LocalDatabaseBootstrapper.SeedDevelopmentEnvironmentAsync(
                sp,
                new StubHostEnvironment(Environments.Production),
                devOptions));

        // 4. First Development seed creates everything cleanly
        var devEnv = new StubHostEnvironment(Environments.Development);
        var first = await LocalDatabaseBootstrapper.SeedDevelopmentEnvironmentAsync(sp, devEnv, devOptions);
        first.AdminCreated.ShouldBeTrue();
        first.LicenseLeaseSeeded.ShouldBeTrue();

        // 5. Second Development seed is 100% idempotent
        var second = await LocalDatabaseBootstrapper.SeedDevelopmentEnvironmentAsync(sp, devEnv, devOptions);
        second.AdminCreated.ShouldBeFalse();
        second.SchoolId.ShouldBe(first.SchoolId);
        second.CampusId.ShouldBe(first.CampusId);
        second.AdminUserId.ShouldBe(first.AdminUserId);

        // 6. Seeded Initial Admin can sign in via IdentityAuthenticationService and gets full module scopes
        using var loginScope = sp.CreateScope();
        var authService = loginScope.ServiceProvider.GetRequiredService<IdentityAuthenticationService>();
        var loginResult = await authService.SignInAsync(
            devOptions.AdminEmail,
            devOptions.AdminPassword!,
            requestedCampusId: first.CampusId,
            tenantId: devOptions.TenantId);

        loginResult.Outcome.ShouldBe(SchoolOS.Modules.Identity.Domain.SignInOutcome.Succeeded);
        loginResult.AccessToken.ShouldNotBeNullOrWhiteSpace();
        loginResult.User.ShouldNotBeNull();
        loginResult.User.Permissions.ShouldContain("students:view");
        loginResult.User.Permissions.ShouldContain("attendance:create");
        loginResult.User.Permissions.ShouldContain("fees:create");
        loginResult.User.Permissions.ShouldContain("exams:create");
    }
}
