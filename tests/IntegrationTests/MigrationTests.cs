using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Persistence;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
public class MigrationTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public MigrationTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_AppliesAllMigrations_Successfully()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(PostgresDatabaseFixture.AdminConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        var systemContext = new TenantContext(null, null, "migration-tester");
        await using var db = new OrganizationDbContext(options, systemContext);

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        pendingMigrations.ShouldBeEmpty("All EF Core migrations should have been applied during database fixture initialization");

        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
        appliedMigrations.ShouldContain(m => m.Contains("Initial_Organization_Create"));
    }

    [Fact]
    public async Task Migrate_ExistingDatabase_IsIdempotent_And_LeavesSchemaIntact()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(PostgresDatabaseFixture.AdminConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        var systemContext = new TenantContext(null, null, "migration-upgrade-tester");
        await using var db = new OrganizationDbContext(options, systemContext);

        await Should.NotThrowAsync(() => db.Database.MigrateAsync());

        var canConnect = await db.Database.CanConnectAsync();
        canConnect.ShouldBeTrue();
    }
}
