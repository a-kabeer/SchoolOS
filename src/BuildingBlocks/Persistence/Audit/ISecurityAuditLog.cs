using System.Collections.Concurrent;
using System.Diagnostics;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.BuildingBlocks.Persistence.Audit;

/// <summary>
/// Writes append-only security audit events containing only technical IDs and metadata (never PII or secrets).
/// Ported from Education-Portal SEC-LOG-03 / SEC-LOG-04.
/// </summary>
public interface ISecurityAuditLog
{
    Task WriteAsync(
        SecurityAuditEventType eventType,
        SecurityAuditOutcome outcome,
        string? actorUserId = null,
        string? targetType = null,
        string? targetId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Thread-safe default security audit log implementation that captures structured security events
/// and can be queried in tests or backed by a module DbContext sink.
/// </summary>
public sealed class StructuredSecurityAuditLog : ISecurityAuditLog
{
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentQueue<SecurityAuditEvent> _events = new();

    public StructuredSecurityAuditLog(ITenantContext tenantContext, TimeProvider? timeProvider = null)
    {
        _tenantContext = tenantContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IReadOnlyCollection<SecurityAuditEvent> Events => _events.ToArray();

    public Task WriteAsync(
        SecurityAuditEventType eventType,
        SecurityAuditOutcome outcome,
        string? actorUserId = null,
        string? targetType = null,
        string? targetId = null,
        CancellationToken cancellationToken = default)
    {
        var evt = new SecurityAuditEvent
        {
            OccurredAtUtc = _timeProvider.GetUtcNow(),
            EventType = eventType,
            Outcome = outcome,
            ActorUserId = actorUserId ?? _tenantContext.UserId,
            ActorTenantId = _tenantContext.TenantId,
            ActorCampusId = _tenantContext.CampusId,
            TargetType = targetType,
            TargetId = targetId,
            TraceId = Activity.Current?.TraceId.ToString(),
        };

        _events.Enqueue(evt);
        return Task.CompletedTask;
    }
}
