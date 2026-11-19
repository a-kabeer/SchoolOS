namespace SchoolOS.BuildingBlocks.Persistence.Audit;

/// <summary>
/// Marker interface for commercial or governance entities whose property-level diffs are recorded atomically
/// by <see cref="ChangeAuditInterceptor"/> in the same database transaction.
/// </summary>
public interface IChangeAuditedEntity
{
}

/// <summary>
/// Append-only change audit entry recording old and new values as JSON alongside OpenTelemetry TraceId.
/// </summary>
public sealed class ChangeAuditEntry
{
    public const int EntityTypeMaxLength = 64;

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required DateTimeOffset OccurredAtUtc { get; init; }
    public string? ActorUserId { get; init; }
    public Guid? TenantId { get; init; }
    public required string EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public required ChangeAuditAction Action { get; init; }
    public required string ChangesJson { get; init; }
    public string? TraceId { get; init; }
}

/// <summary>
/// Append-only security audit event (never stores PII, emails, names, or secrets — IDs only).
/// </summary>
public sealed class SecurityAuditEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required DateTimeOffset OccurredAtUtc { get; init; }
    public required SecurityAuditEventType EventType { get; init; }
    public required SecurityAuditOutcome Outcome { get; init; }
    public string? ActorUserId { get; init; }
    public Guid? ActorTenantId { get; init; }
    public Guid? ActorCampusId { get; init; }
    public string? TargetType { get; init; }
    public string? TargetId { get; init; }
    public string? SourceIp { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
