namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// Constants for security policies, roles, and authorization requirements across SchoolOS.
/// </summary>
public static class SecurityPolicies
{
    // Authorization Policies
    public const string PlatformAdminPolicy = "PlatformAdminPolicy";
    public const string TenantScopedPolicy = "TenantScopedPolicy";
    public const string OrganizationAdminPolicy = "OrganizationAdminPolicy";

    // System and Tenant Roles
    public const string PlatformAdminRole = "PlatformAdmin";
    public const string TenantAdminRole = "TenantAdmin";
    public const string SchoolAdminRole = "SchoolAdmin";
    public const string PrincipalRole = "Principal";
    public const string TeacherRole = "Teacher";
    public const string StudentRole = "Student";

    // Scopes & Functional Permissions
    public const string PlatformAdminScope = "platform:admin";
    public const string OrganizationManageScope = "organization:manage";
    public const string SchoolsWriteScope = "schools:write";
    public const string CampusesWriteScope = "campuses:write";
}
