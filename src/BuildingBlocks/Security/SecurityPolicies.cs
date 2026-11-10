namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// Constants for security policies, roles, and authorization requirements across SchoolOS.
/// </summary>
public static class SecurityPolicies
{
    public const string PlatformAdminPolicy = "PlatformAdminPolicy";
    public const string TenantScopedPolicy = "TenantScopedPolicy";

    public const string PlatformAdminRole = "PlatformAdmin";
    public const string PlatformAdminScope = "platform:admin";
}
