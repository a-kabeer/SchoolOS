using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Domain;
using SchoolOS.Modules.Organization.Persistence;
using SchoolOS.Modules.Organization.Persistence.Migrations;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public class Wave0SecurityHardeningTests
{
    [Fact]
    public void JwtOptions_Validate_Throws_In_Production_When_Using_Default_Development_Key()
    {
        var options = new JwtOptions();

        var ex = Should.Throw<InvalidOperationException>(() => options.Validate(allowDevelopmentDefaults: false));
        ex.Message.ShouldContain("CRITICAL SECURITY ERROR");
    }

    [Fact]
    public void JwtOptions_Validate_Allows_Default_Key_In_Development_Or_Testing()
    {
        var options = new JwtOptions();
        Should.NotThrow(() => options.Validate(allowDevelopmentDefaults: true));
    }

    [Fact]
    public void JwtOptions_Validate_Rejects_Short_Signing_Key_Even_In_Development()
    {
        var options = new JwtOptions { SigningKey = "too-short-key" };
        Should.Throw<InvalidOperationException>(() => options.Validate(allowDevelopmentDefaults: true));
    }

    [Fact]
    public void JwtOptions_Validate_Accepts_Strong_Key_In_Production()
    {
        var options = new JwtOptions { SigningKey = "A_Very_Strong_Random_Production_Key_With_At_Least_32_Bytes_Length!" };
        Should.NotThrow(() => options.Validate(allowDevelopmentDefaults: false));
    }

    [Fact]
    public void PostgresRlsHelper_Generates_Force_Rls_And_Optional_PlatformAdmin_Predicate()
    {
        var strictSql = PostgresRlsHelper.GenerateEnableRlsSql("organization", "schools");
        strictSql.ShouldContain("ENABLE ROW LEVEL SECURITY");
        strictSql.ShouldContain("FORCE ROW LEVEL SECURITY");
        strictSql.ShouldContain("app.current_tenant_id");
        strictSql.ShouldNotContain("app.is_platform_admin");

        var cloudSql = PostgresRlsHelper.GenerateEnableRlsSql("subscriptions", "subscriptions", allowPlatformAdminBypass: true);
        cloudSql.ShouldContain("app.is_platform_admin");
        cloudSql.ShouldContain("app.current_tenant_id");

        var disableSql = PostgresRlsHelper.GenerateDisableRlsSql("organization", "schools");
        disableSql.ShouldContain("DROP POLICY IF EXISTS schools_tenant_isolation");
        disableSql.ShouldContain("DISABLE ROW LEVEL SECURITY");
    }

    [Fact]
    public void Initial_Organization_Migration_Embeds_Rls_Sql_In_Up_And_Down()
    {
        var migration = new Initial_Organization_Create();
        var upMethod = typeof(Initial_Organization_Create).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var downMethod = typeof(Initial_Organization_Create).GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var upBuilder = new MigrationBuilder(activeProvider: "Npgsql.EntityFrameworkCore.PostgreSQL");
        upMethod.Invoke(migration, [upBuilder]);
        var upSql = upBuilder.Operations.OfType<SqlOperation>().Select(o => o.Sql).ToList();
        upSql.ShouldContain(sql => sql.Contains("organization.schools") && sql.Contains("FORCE ROW LEVEL SECURITY"));
        upSql.ShouldContain(sql => sql.Contains("organization.campuses") && sql.Contains("FORCE ROW LEVEL SECURITY"));

        var downBuilder = new MigrationBuilder(activeProvider: "Npgsql.EntityFrameworkCore.PostgreSQL");
        downMethod.Invoke(migration, [downBuilder]);
        var downSql = downBuilder.Operations.OfType<SqlOperation>().Select(o => o.Sql).ToList();
        downSql.ShouldContain(sql => sql.Contains("DISABLE ROW LEVEL SECURITY") && sql.Contains("organization.schools"));
        downSql.ShouldContain(sql => sql.Contains("DISABLE ROW LEVEL SECURITY") && sql.Contains("organization.campuses"));
    }

    [Fact]
    public async Task DbContext_SaveChanges_FailsClosed_When_TenantId_Is_Null_And_Not_PlatformAdmin()
    {
        var unauthenticated = new TenantContext(tenantId: null, campusId: null, userId: "user-without-tenant");
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase($"wave0-failclosed-{Guid.NewGuid()}")
            .Options;

        await using var db = new OrganizationDbContext(options, unauthenticated);
        db.Schools.Add(School.Create(Guid.CreateVersion7(), "Rogue School", "ROG-1"));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        ex.Message.ShouldContain("Tenant isolation violation");
    }

    [Fact]
    public async Task DbContext_SaveChanges_Allows_Verified_PlatformAdmin_Without_Single_TenantId()
    {
        var platformAdmin = new TenantContext(tenantId: null, campusId: null, userId: "platform-admin", isPlatformAdmin: true);
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase($"wave0-platform-{Guid.NewGuid()}")
            .Options;

        await using var db = new OrganizationDbContext(options, platformAdmin);
        db.Schools.Add(School.Create(Guid.CreateVersion7(), "Provisioned School", "PRV-1"));

        var saved = await db.SaveChangesAsync();
        saved.ShouldBe(1);
    }
}
