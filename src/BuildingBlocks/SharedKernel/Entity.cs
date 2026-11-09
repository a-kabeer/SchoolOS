namespace SchoolOS.BuildingBlocks.SharedKernel;

/// <summary>
/// Base class for domain entities with strongly-typed identifiers.
/// </summary>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public string? CreatedBy { get; protected set; }
    public DateTime? UpdatedAtUtc { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }
}
