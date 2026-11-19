namespace SchoolOS.BuildingBlocks.SharedKernel;

/// <summary>
/// Marker interface for domain events dispatched within the modular monolith.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAtUtc { get; }
}
