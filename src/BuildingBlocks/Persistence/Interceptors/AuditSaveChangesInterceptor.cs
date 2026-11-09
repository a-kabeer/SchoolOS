using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.BuildingBlocks.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChangesInterceptor that automatically stamps audit metadata
/// (CreatedAtUtc, CreatedBy, UpdatedAtUtc, UpdatedBy) on all entities implementing IAuditableEntity.
/// The actor identity is derived strictly from trusted server-side ITenantContext.
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
                entry.Entity.SetCreated(utcNow, userId);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetUpdated(utcNow, userId);
            }
        }
    }
}
