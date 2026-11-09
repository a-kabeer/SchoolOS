namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// Server-derived tenant and campus context interface.
/// Client-supplied tenant IDs must never be trusted directly.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    Guid? CampusId { get; }
    string? UserId { get; }
    bool IsAuthenticated { get; }
    bool HasScope(string scope);
}
