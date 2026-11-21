using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Admissions.Persistence;
using SchoolOS.Modules.Attendance.Persistence;
using SchoolOS.Modules.Exams.Persistence;
using SchoolOS.Modules.Fees.Persistence;
using SchoolOS.Modules.Identity.Application;
using SchoolOS.Modules.Identity.Domain;
using SchoolOS.Modules.Identity.Persistence;
using SchoolOS.Modules.Organization.Domain;
using SchoolOS.Modules.Organization.Persistence;
using SchoolOS.Modules.Students.Persistence;

namespace SchoolOS.Local.Api.LocalPlatform;

public sealed class DevBootstrapOptions
{
    public const string SectionName = "DevBootstrap";

    public bool Enabled { get; set; }
    public Guid TenantId { get; set; } = Guid.Parse("01931a74-0000-7000-8000-000000000001");
    public string SchoolName { get; set; } = "SchoolOS Demo Academy";
    public string SchoolCode { get; set; } = "SDA";
    public string CampusName { get; set; } = "Main Campus";
    public string CampusCode { get; set; } = "MAIN";
    public string AdminEmail { get; set; } = "admin@schoolos.local";
    public string AdminFirstName { get; set; } = "School";
    public string AdminLastName { get; set; } = "Administrator";
    public string? AdminPassword { get; set; }
    public string? DevLicensePrivateKeyPem { get; set; }
}

public sealed record DevBootstrapResult(
    Guid TenantId,
    Guid SchoolId,
    Guid CampusId,
    Guid AcademicYearId,
    Guid GradeLevelId,
    Guid SectionId,
    Guid AdminUserId,
    bool AdminCreated,
    bool LicenseLeaseSeeded);

/// <summary>
/// Orchestrates Local Data Plane EF Core 10 migrations, least-privilege PostgreSQL runtime role grants,
/// and strictly Development-only initial tenant/school/campus/admin seeding.
/// </summary>
public static class LocalDatabaseBootstrapper
{
    public static readonly string[] LocalSchemas =
    [
        "identity",
        "organization",
        "admissions",
        "students",
        "attendance",
        "fees",
        "exams",
        "platform_local"
    ];

    public static async Task ApplyMigrationsAndGrantsAsync(
        IServiceProvider services,
        string runtimeRoleName = "schoolos_app",
        string? runtimeRolePassword = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetService<ILoggerFactory>()?.CreateLogger(nameof(LocalDatabaseBootstrapper));

        await sp.GetRequiredService<IdentityDbContext>().Database.MigrateAsync(cancellationToken);
        await sp.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync(cancellationToken);
        await sp.GetRequiredService<AdmissionsDbContext>().Database.MigrateAsync(cancellationToken);
        await sp.GetRequiredService<StudentsDbContext>().Database.MigrateAsync(cancellationToken);
        await sp.GetRequiredService<AttendanceDbContext>().Database.MigrateAsync(cancellationToken);
        await sp.GetRequiredService<FeesDbContext>().Database.MigrateAsync(cancellationToken);
        await sp.GetRequiredService<ExamsDbContext>().Database.MigrateAsync(cancellationToken);
        await sp.GetRequiredService<LocalPlatformDbContext>().Database.MigrateAsync(cancellationToken);

        var orgDb = sp.GetRequiredService<OrganizationDbContext>();
        if (orgDb.Database.IsRelational())
        {
            if (!string.IsNullOrWhiteSpace(runtimeRolePassword))
            {
                await orgDb.Database.ExecuteSqlRawAsync(
                    PostgresRlsHelper.GenerateEnsureLeastPrivilegeRoleSql(runtimeRoleName, runtimeRolePassword),
                    cancellationToken);
            }

            foreach (var schema in LocalSchemas)
            {
                await orgDb.Database.ExecuteSqlRawAsync(
                    PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql(schema, runtimeRoleName),
                    cancellationToken);
            }
        }

        logger?.LogInformation("Applied all 8 Local Data Plane migrations and least-privilege grants for role {Role}.", runtimeRoleName);
    }

    public static async Task<DevBootstrapResult> SeedDevelopmentEnvironmentAsync(
        IServiceProvider services,
        IHostEnvironment environment,
        DevBootstrapOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            throw new InvalidOperationException(
                "Development bootstrap seeding is strictly prohibited outside Development/Testing environments.");
        }

        ArgumentNullException.ThrowIfNull(options);
        if (options.TenantId == Guid.Empty)
        {
            throw new ArgumentException("DevBootstrap TenantId cannot be empty.", nameof(options));
        }

        var adminPassword = options.AdminPassword;
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "DevBootstrap:AdminPassword must be configured via environment variable or user-secrets before seeding the initial Development Admin.");
        }

        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var tenantContext = (TenantContext)sp.GetRequiredService<ITenantContext>();
        tenantContext.EnterPreAuthenticationTenantScope(options.TenantId, "system:dev-bootstrap");

        var orgDb = sp.GetRequiredService<OrganizationDbContext>();
        var idDb = sp.GetRequiredService<IdentityDbContext>();
        var authService = sp.GetRequiredService<IdentityAuthenticationService>();
        var leaseStore = sp.GetRequiredService<ILicenseLeaseStore>();

        // 1. Idempotent School + Campus
        var school = await orgDb.Schools
            .Include(s => s.Campuses)
            .FirstOrDefaultAsync(s => s.Code == options.SchoolCode.ToUpperInvariant(), cancellationToken);
        if (school is null)
        {
            school = School.Create(options.TenantId, options.SchoolName, options.SchoolCode);
            orgDb.Schools.Add(school);
            await orgDb.SaveChangesAsync(cancellationToken);
        }

        var campus = school.Campuses.FirstOrDefault(c => c.Code == options.CampusCode.ToUpperInvariant());
        if (campus is null)
        {
            campus = school.AddCampus(options.CampusName, options.CampusCode, "Development Campus");
            orgDb.Campuses.Add(campus);
            await orgDb.SaveChangesAsync(cancellationToken);
        }

        // 2. Idempotent AcademicYear + GradeLevel + Section
        var academicYear = await orgDb.AcademicYears
            .FirstOrDefaultAsync(y => y.SchoolId == school.Id && y.Code == "2026-27", cancellationToken);
        if (academicYear is null)
        {
            academicYear = AcademicYear.Create(
                options.TenantId,
                school.Id,
                "2026-27",
                "Academic Year 2026-2027",
                new DateOnly(2026, 4, 1),
                new DateOnly(2027, 3, 31),
                isCurrent: true);
            orgDb.AcademicYears.Add(academicYear);
            await orgDb.SaveChangesAsync(cancellationToken);
        }

        var gradeLevel = await orgDb.GradeLevels
            .FirstOrDefaultAsync(g => g.SchoolId == school.Id && g.Code == "G5", cancellationToken);
        if (gradeLevel is null)
        {
            gradeLevel = GradeLevel.Create(options.TenantId, school.Id, "G5", "Grade 5", sequenceOrder: 5);
            orgDb.GradeLevels.Add(gradeLevel);
            await orgDb.SaveChangesAsync(cancellationToken);
        }

        var section = await orgDb.Sections
            .FirstOrDefaultAsync(
                s => s.CampusId == campus.Id && s.AcademicYearId == academicYear.Id && s.GradeLevelId == gradeLevel.Id && s.Code == "5A",
                cancellationToken);
        if (section is null)
        {
            section = Section.Create(options.TenantId, school.Id, campus.Id, academicYear.Id, gradeLevel.Id, "5A", "Grade 5 - A", capacity: 35);
            orgDb.Sections.Add(section);
            await orgDb.SaveChangesAsync(cancellationToken);
        }

        // 3. Idempotent TenantAdmin Role with full module permissions
        var adminRole = await idDb.SchoolRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.SchoolId == school.Id && r.Name == SecurityPolicies.TenantAdminRole, cancellationToken);
        if (adminRole is null)
        {
            adminRole = SchoolRole.Create(options.TenantId, school.Id, SecurityPolicies.TenantAdminRole, isSystemRole: true);
            foreach (var module in new[]
            {
                ModuleCode.School, ModuleCode.Campus, ModuleCode.AcademicSetup, ModuleCode.UsersRoles,
                ModuleCode.Students, ModuleCode.Staff, ModuleCode.Admissions, ModuleCode.Attendance,
                ModuleCode.Fees, ModuleCode.Exams
            })
            {
                adminRole.SetModulePermission(module, canView: true, canCreate: true, canEdit: true, canDelete: true);
            }

            idDb.SchoolRoles.Add(adminRole);
            await idDb.SaveChangesAsync(cancellationToken);
        }

        // 4. Idempotent Initial Development Admin User
        var normalizedEmail = options.AdminEmail.Trim().ToLowerInvariant();
        var existingAdmin = await idDb.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        bool adminCreated = false;
        Guid adminUserId;

        if (existingAdmin is null)
        {
            var (user, _) = await authService.ProvisionUserAsync(
                options.TenantId,
                school.Id,
                normalizedEmail,
                options.AdminFirstName,
                middleName: null,
                options.AdminLastName,
                UserType.SchoolAdministrator,
                adminPassword,
                mustChangePasswordOnNextLogin: false,
                cancellationToken: cancellationToken);

            idDb.UserCampusAssignments.Add(UserCampusAssignment.Create(options.TenantId, school.Id, user.Id, campus.Id, isPrimary: true));
            idDb.UserRoleAssignments.Add(UserRoleAssignment.Create(options.TenantId, user.Id, adminRole.Id, campusId: null));
            await idDb.SaveChangesAsync(cancellationToken);

            adminUserId = user.Id;
            adminCreated = true;
        }
        else
        {
            adminUserId = existingAdmin.Id;
        }

        // 5. Optional Dev-Signed Offline License Lease enabling all implemented modules
        bool leaseSeeded = false;
        if (!string.IsNullOrWhiteSpace(options.DevLicensePrivateKeyPem))
        {
            var now = DateTimeOffset.UtcNow;
            var payload = new LicenseLeasePayload(
                options.TenantId,
                now.AddMinutes(-1),
                now.AddDays(90),
                "dev-bootstrap-k1",
                "DEV-ALL-MODULES",
                [ModuleCode.School, ModuleCode.Campus, ModuleCode.AcademicSetup, ModuleCode.UsersRoles, ModuleCode.Students, ModuleCode.Staff, ModuleCode.Admissions, ModuleCode.Attendance, ModuleCode.Fees, ModuleCode.Exams],
                SequenceNumber: 1,
                PaidThroughUtc: now.AddDays(60),
                GraceUntilUtc: now.AddDays(75));

            var signed = OfflineLicenseLeaseVerifier.SignWithPrivateKey(payload, options.DevLicensePrivateKeyPem);
            await leaseStore.SetLeaseAsync(options.TenantId, signed, cancellationToken);
            leaseSeeded = true;
        }

        return new DevBootstrapResult(
            options.TenantId,
            school.Id,
            campus.Id,
            academicYear.Id,
            gradeLevel.Id,
            section.Id,
            adminUserId,
            adminCreated,
            leaseSeeded);
    }
}
