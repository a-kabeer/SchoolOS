namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// Canonical sellable and core module catalogue codes across Cloud Control Plane and Local Data Plane.
/// Ported from Education-Portal ADR-009 / ENG-ENUM-01..03 (never renumber or reuse an integer value).
/// </summary>
public enum ModuleCode
{
    None = 0,

    // Core modules: always included in every subscription, never sold separately.
    School = 1,
    Campus = 2,
    AcademicSetup = 3,
    UsersRoles = 4,
    Students = 5,
    Staff = 6,

    // Paid / sellable modules: included in plans or purchased as add-ons.
    Admissions = 7,
    Attendance = 8,
    Fees = 9,
    ParentCommunication = 10,
    Exams = 11,
    Timetable = 12,
    HrPayroll = 13,
    Transport = 14,
    Library = 15,
    Lms = 16,
    Hostel = 17,
}

/// <summary>
/// Fine-grained CRUD actions evaluated per <see cref="ModuleCode"/> by scoped RBAC policies.
/// </summary>
public enum PermissionAction
{
    View = 1,
    Create = 2,
    Edit = 3,
    Delete = 4,
}

public static class ModuleCodeExtensions
{
    public static bool IsCoreModule(this ModuleCode code) =>
        code is ModuleCode.School
            or ModuleCode.Campus
            or ModuleCode.AcademicSetup
            or ModuleCode.UsersRoles
            or ModuleCode.Students
            or ModuleCode.Staff;

    public static bool IsPaidModule(this ModuleCode code) =>
        code != ModuleCode.None && !code.IsCoreModule();

    public static string ToPermissionScope(this ModuleCode module, PermissionAction action) =>
        $"{module.ToString().ToLowerInvariant()}:{action.ToString().ToLowerInvariant()}";
}
