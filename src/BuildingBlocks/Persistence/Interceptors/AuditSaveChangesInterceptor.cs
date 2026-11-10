using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.BuildingBlocks.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChangesInterceptor that automatically stamps audit metadata
/// (CreatedAtUtc, CreatedBy, UpdatedAtUtc, UpdatedBy) on all entities implementing IAuditableEntity.
/// The actor identity is derived strictly from trusted server-side ITenantContext.
/// If an authenticated user identifier is missing during a mutation, it fails fast to prevent unattributed audit records.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;

    public AuditSaveChangesInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;
        var userId = _tenantContext.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new InvalidOperationException(
                        $"Audit integrity failure: Cannot create entity of type '{entry.Entity.GetType().Name}' " +
                        "without an authenticated user identifier in the ambient TenantContext.");
                }

                entry.Entity.SetCreated(utcNow, userId);
            }
            else if (entry.State == EntityState.Modified)
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new InvalidOperationException(
                        $"Audit integrity failure: Cannot modify entity of type '{entry.Entity.GetType().Name}' " +
                        "without an authenticated user identifier in the ambient TenantContext.");
                }

                entry.Entity.SetUpdated(utcNow, userId);
            }
        }
    }
}
