namespace SchoolOS.BuildingBlocks.SharedKernel;

/// <summary>
/// Defines audit tracking metadata for entities.
/// Automatically stamped by EF Core SaveChanges interceptors from verified server context.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; }
    string? CreatedBy { get; }
    DateTime? UpdatedAtUtc { get; }
    string? UpdatedBy { get; }

    void SetCreated(DateTime createdAtUtc, string? createdBy);
    void SetUpdated(DateTime updatedAtUtc, string? updatedBy);
}
