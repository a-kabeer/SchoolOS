using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Identity.Application;
using SchoolOS.Modules.Identity.Domain;
using SchoolOS.Modules.Identity.Persistence;
using SchoolOS.Modules.Identity.Persistence.Migrations;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public class Wave2IdentityAndRbacTests
{
    private static IdentityDbContext CreateInMemoryIdentityDb(ITenantContext tenantContext, string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(dbName ?? $"wave2-identity-{Guid.NewGuid()}")
            .Options;

        return new IdentityDbContext(options, tenantContext);
    }

    [Theory]
    [InlineData("short-pass-12", "PasswordTooShort")]
    [InlineData("000000000000000", "CommonPassword")]
    [InlineData("123456789012345", "CommonPassword")]
    [InlineData("MySchoolOsAdminPassphrase2026!", "PersonalWord")]
    [InlineData("Sarah.Ahmed.SecretPassphrase!", "PersonalWord")]
    public void PasswordPolicyValidator_Rejects_Weak_Common_And_Personal_Passwords(string badPassword, string expectedErrorCode)
    {
        var result = PasswordPolicyValidator.Validate(badPassword, email: "sarah.ahmed@school.pk", userName: "sarah");
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == expectedErrorCode);
    }

    [Fact]
    public void PasswordPolicyValidator_Accepts_Strong_15Plus_Char_Passphrase()
    {
        var result = PasswordPolicyValidator.Validate(
            "Correct-Horse-Battery-Staple-2026!",
            email: "sarah.ahmed@school.pk",
            userName: "sarah");

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void SchoolRole_And_RolePermission_Enforce_View_Prerequisite_And_Omit_Empty_Rows()
    {
        var tenantId = Guid.CreateVersion7();
        var schoolId = Guid.CreateVersion7();
        var role = SchoolRole.Create(tenantId, schoolId, "Admissions Coordinator");

        // Creating, editing, or deleting without viewing must fail (Education-Portal invariant)
        Should.Throw<ArgumentException>(() =>
            role.SetModulePermission(ModuleCode.Admissions, canView: false, canCreate: true, canEdit: false, canDelete: false));

        // Granting view + create + edit succeeds
        var perm = role.SetModulePermission(ModuleCode.Admissions, canView: true, canCreate: true, canEdit: true, canDelete: false);
        perm.ShouldNotBeNull();
        role.Permissions.Count.ShouldBe(1);

        var scopes = role.GetEffectivePermissionScopes(new HashSet<ModuleCode> { ModuleCode.Admissions });
        scopes.ShouldContain("admissions:view");
        scopes.ShouldContain("admissions:create");
        scopes.ShouldContain("admissions:edit");
        scopes.ShouldNotContain("admissions:delete");

        // When module is disabled in subscription, effective permission scopes exclude it
        var disabledScopes = role.GetEffectivePermissionScopes(new HashSet<ModuleCode> { ModuleCode.School });
        disabledScopes.ShouldBeEmpty();

        // Clearing all permissions removes the row
        role.SetModulePermission(ModuleCode.Admissions, canView: false, canCreate: false, canEdit: false, canDelete: false);
        role.Permissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task SignIn_LocksOut_After_Three_Failed_Attempts_And_Audits_Lockout()
    {
        var tenantId = Guid.CreateVersion7();
        var schoolId = Guid.CreateVersion7();
        var ctx = new TenantContext(tenantId, null, "system:seed");
        var dbName = $"wave2-lockout-{Guid.NewGuid()}";
        await using var db = CreateInMemoryIdentityDb(ctx, dbName);
        var audit = new StructuredSecurityAuditLog(ctx);
        var service = new IdentityAuthenticationService(db, new JwtOptions(), new DefaultCoreModuleAccess(), audit);

        await service.ProvisionUserAsync(
            tenantId,
            schoolId,
            "principal@beacon.pk",
            "Ayesha",
            null,
            "Khan",
            UserType.Principal,
            "Strong-Unique-Passphrase-2026!",
            mustChangePasswordOnNextLogin: false);

        // Attempt 1 & 2 -> InvalidCredentials
        (await service.SignInAsync("principal@beacon.pk", "Wrong-Passphrase-Attempt-1!")).Outcome
            .ShouldBe(SignInOutcome.InvalidCredentials);
        (await service.SignInAsync("principal@beacon.pk", "Wrong-Passphrase-Attempt-2!")).Outcome
            .ShouldBe(SignInOutcome.InvalidCredentials);

        // Attempt 3 -> LockedOut (30 minutes)
        var third = await service.SignInAsync("principal@beacon.pk", "Wrong-Passphrase-Attempt-3!");
        third.Outcome.ShouldBe(SignInOutcome.LockedOut);
        third.ErrorCode.ShouldBe("ACCOUNT_LOCKED_OUT");

        // Even with the right password during active lockout, sign-in must be rejected
        var blocked = await service.SignInAsync("principal@beacon.pk", "Strong-Unique-Passphrase-2026!");
        blocked.Outcome.ShouldBe(SignInOutcome.LockedOut);

        audit.Events.ShouldContain(e =>
            e.EventType == SecurityAuditEventType.SignInLockedOut &&
            e.Outcome == SecurityAuditOutcome.Failure);
    }

    [Fact]
    public async Task RefreshSession_Rotates_Token_And_Detects_Replay_Compromise()
    {
        var tenantId = Guid.CreateVersion7();
        var schoolId = Guid.CreateVersion7();
        var ctx = new TenantContext(tenantId, null, "system:seed");
        await using var db = CreateInMemoryIdentityDb(ctx);
        var audit = new StructuredSecurityAuditLog(ctx);
        var service = new IdentityAuthenticationService(db, new JwtOptions(), new DefaultCoreModuleAccess(), audit);

        var (user, _) = await service.ProvisionUserAsync(
            tenantId,
            schoolId,
            "teacher@beacon.pk",
            "Bilal",
            null,
            "Raza",
            UserType.Teacher,
            "Amber-Cedar-River-Harbor-2026!",
            mustChangePasswordOnNextLogin: false);

        var login = await service.SignInAsync("teacher@beacon.pk", "Amber-Cedar-River-Harbor-2026!");
        login.Outcome.ShouldBe(SignInOutcome.Succeeded);
        var token1 = login.RawRefreshToken!;

        // 1. Legitimate refresh rotates token1 -> token2
        var rotated = await service.RefreshSessionAsync(token1);
        rotated.Outcome.ShouldBe(SignInOutcome.Succeeded);
        var token2 = rotated.RawRefreshToken!;
        token2.ShouldNotBe(token1);

        // 2. Attacker replays token1 -> triggers breach detection, revokes session & increments CredentialVersion
        var replay = await service.RefreshSessionAsync(token1);
        replay.Outcome.ShouldBe(SignInOutcome.InvalidCredentials);
        replay.ErrorCode.ShouldBe("SESSION_COMPROMISED");

        // 3. Even token2 is now revoked because the session was compromised!
        var afterBreach = await service.RefreshSessionAsync(token2);
        afterBreach.Outcome.ShouldBe(SignInOutcome.InvalidCredentials);
        afterBreach.ErrorCode.ShouldBe("SESSION_COMPROMISED");

        var reloadedUser = await db.Users.FirstAsync(u => u.Id == user.Id);
        reloadedUser.CredentialVersion.ShouldBe(2);
        audit.Events.ShouldContain(e => e.EventType == SecurityAuditEventType.TokenReuseDetected);
    }

    [Fact]
    public async Task SwitchCampusContext_Verifies_ServerSide_Assignment_And_Resolves_Scoped_Role_Permissions()
    {
        var tenantId = Guid.CreateVersion7();
        var schoolId = Guid.CreateVersion7();
        var campusNorth = Guid.CreateVersion7();
        var campusSouth = Guid.CreateVersion7();
        var ctx = new TenantContext(tenantId, null, "system:seed");
        await using var db = CreateInMemoryIdentityDb(ctx);
        var audit = new StructuredSecurityAuditLog(ctx);
        var service = new IdentityAuthenticationService(db, new JwtOptions(), new DefaultCoreModuleAccess(), audit);

        var (user, _) = await service.ProvisionUserAsync(
            tenantId,
            schoolId,
            "coordinator@beacon.pk",
            "Zainab",
            null,
            "Malik",
            UserType.Staff,
            "Granite-Falcon-Meadow-2026!",
            mustChangePasswordOnNextLogin: false);

        // Assign user ONLY to campusNorth and give a campus-scoped role on campusNorth
        db.UserCampusAssignments.Add(UserCampusAssignment.Create(tenantId, schoolId, user.Id, campusNorth, isPrimary: true));
        var role = SchoolRole.Create(tenantId, schoolId, "North Admissions Lead");
        role.SetModulePermission(ModuleCode.Admissions, canView: true, canCreate: true, canEdit: false, canDelete: false);
        db.SchoolRoles.Add(role);
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(tenantId, user.Id, role.Id, campusNorth));
        await db.SaveChangesAsync();

        // Switching to unassigned campusSouth must fail closed
        var denied = await service.SwitchCampusContextAsync(user.Id, null, campusSouth);
        denied.Outcome.ShouldBe(SignInOutcome.CampusAssignmentRequired);

        // Switching to assigned campusNorth succeeds and carries campus-scoped permissions
        var allowed = await service.SwitchCampusContextAsync(user.Id, null, campusNorth);
        allowed.Outcome.ShouldBe(SignInOutcome.Succeeded);
        allowed.User!.ActiveCampusId.ShouldBe(campusNorth);
        allowed.User.Permissions.ShouldContain("admissions:view");
        allowed.User.Permissions.ShouldContain("admissions:create");
    }

    [Fact]
    public void Initial_Identity_Migration_Embeds_Force_Rls_For_All_Seven_Tenant_Tables()
    {
        var migration = new Initial_Identity_Create();
        var upMethod = typeof(Initial_Identity_Create).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var downMethod = typeof(Initial_Identity_Create).GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var upBuilder = new MigrationBuilder(activeProvider: "Npgsql.EntityFrameworkCore.PostgreSQL");
        upMethod.Invoke(migration, [upBuilder]);
        var upSql = upBuilder.Operations.OfType<SqlOperation>().Select(o => o.Sql).ToList();

        string[] tables =
        [
            "users",
            "user_credentials",
            "user_sessions",
            "school_roles",
            "role_permissions",
            "user_campus_assignments",
            "user_role_assignments"
        ];

        foreach (var table in tables)
        {
            upSql.ShouldContain(sql => sql.Contains($"identity.{table}") && sql.Contains("FORCE ROW LEVEL SECURITY"));
        }
        upSql.ShouldAllBe(sql => !sql.Contains("is_platform_admin", StringComparison.OrdinalIgnoreCase));

        var downBuilder = new MigrationBuilder(activeProvider: "Npgsql.EntityFrameworkCore.PostgreSQL");
        downMethod.Invoke(migration, [downBuilder]);
        var downSql = downBuilder.Operations.OfType<SqlOperation>().Select(o => o.Sql).ToList();
        downSql.Count.ShouldBe(7);
    }
}
