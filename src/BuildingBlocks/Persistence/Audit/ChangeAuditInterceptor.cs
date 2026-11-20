using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.BuildingBlocks.Persistence.Audit;

/// <summary>
/// EF Core SaveChangesInterceptor (ported from Education-Portal ADR-016) that captures property-level old/new diffs
/// into <see cref="ChangeAuditEntry"/> inside the same database transaction for any entity implementing
/// <see cref="IChangeAuditedEntity"/>.
/// </summary>
public sealed class ChangeAuditInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.Ordinal)
    {
        nameof(Entity<Guid>.Id),
        nameof(IAuditableEntity.CreatedAtUtc),
        nameof(IAuditableEntity.CreatedBy),
        nameof(IAuditableEntity.UpdatedAtUtc),
        nameof(IAuditableEntity.UpdatedBy),
    };

    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public ChangeAuditInterceptor(ITenantContext tenantContext, TimeProvider? timeProvider = null)
    {
        _tenantContext = tenantContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        AddEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        AddEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddEntries(DbContext? context)
    {
        if (context is null || context.Model.FindEntityType(typeof(ChangeAuditEntry)) is null)
        {
            return;
        }

        var changed = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                     && e.Entity is IChangeAuditedEntity)
            .ToList();

        if (changed.Count == 0)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        var actor = _tenantContext.UserId;
        var traceId = Activity.Current?.TraceId.ToString();

        foreach (var entry in changed)
        {
            var changes = Describe(entry);
            if (changes.Count == 0)
            {
                continue;
            }

            Guid? tenantId = entry.Entity is ITenantScopedEntity scoped
                ? scoped.TenantId
                : _tenantContext.TenantId;

            Guid entityId = entry.Entity is Entity<Guid> guidEntity
                ? guidEntity.Id
                : Guid.Empty;

            context.Add(new ChangeAuditEntry
            {
                OccurredAtUtc = now,
                ActorUserId = actor,
                TenantId = tenantId,
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = entityId,
                Action = entry.State switch
                {
                    EntityState.Added => ChangeAuditAction.Added,
                    EntityState.Deleted => ChangeAuditAction.Removed,
                    _ => ChangeAuditAction.Modified,
                },
                ChangesJson = JsonSerializer.Serialize(changes),
                TraceId = traceId,
            });
        }
    }

    public static List<PropertyChange> Describe(EntityEntry entry)
    {
        var changes = new List<PropertyChange>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (IgnoredProperties.Contains(name) || property.Metadata.IsShadowProperty())
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    changes.Add(new PropertyChange(name, null, Format(property.CurrentValue)));
                    break;
                case EntityState.Deleted:
                    changes.Add(new PropertyChange(name, Format(property.OriginalValue), null));
                    break;
                case EntityState.Modified when property.IsModified && !Equals(property.OriginalValue, property.CurrentValue):
                    changes.Add(new PropertyChange(name, Format(property.OriginalValue), Format(property.CurrentValue)));
                    break;
            }
        }

        return changes;
    }

    public static string? Format(object? value) => value switch
    {
        null => null,
        DateTimeOffset date => date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        DateTime date => date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        decimal amount => amount.ToString("0.############################", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    public sealed record PropertyChange(
        [property: JsonPropertyName("property")] string Property,
        [property: JsonPropertyName("old")] string? Old,
        [property: JsonPropertyName("new")] string? New);
}
