using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.BuildingBlocks.Persistence;

/// <summary>
/// Multi-tenant base DbContext for SchoolOS modules.
/// Enforces global tenant query filters and index conventions across all ITenantScopedEntity types.
/// </summary>
public abstract class SchoolOSDbContextBase : DbContext
{
    public ITenantContext TenantContext { get; }

    /// <summary>
    /// Current tenant ID evaluated dynamically on this DbContext instance for global query filters.
    /// </summary>
    public Guid? CurrentTenantId => TenantContext.TenantId;

    protected SchoolOSDbContextBase(DbContextOptions options, ITenantContext tenantContext)
        : base(options)
    {
        TenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // Apply multi-tenant global query filter and B-tree index
            if (typeof(ITenantScopedEntity).IsAssignableFrom(clrType))
            {
                ConfigureTenantFilter(modelBuilder, clrType);

                var tenantProperty = entityType.FindProperty(nameof(ITenantScopedEntity.TenantId));
                if (tenantProperty is not null && entityType.FindIndex(tenantProperty) is null)
                {
                    entityType.AddIndex(tenantProperty);
                }
            }

            // Apply index on CampusId for campus-scoped entities
            if (typeof(ICampusScopedEntity).IsAssignableFrom(clrType))
            {
                var campusProperty = entityType.FindProperty(nameof(ICampusScopedEntity.CampusId));
                if (campusProperty is not null && entityType.FindIndex(campusProperty) is null)
                {
                    entityType.AddIndex(campusProperty);
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidateTenantIntegrity();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ValidateTenantIntegrity();
        return base.SaveChanges();
    }

    /// <summary>
    /// Fails fast if any entity is modified or added with a tenant ID differing from the ambient TenantContext.
    /// </summary>
    protected virtual void ValidateTenantIntegrity()
    {
        if (TenantContext.TenantId is null)
        {
            return;
        }

        var activeTenantId = TenantContext.TenantId.Value;

        foreach (var entry in ChangeTracker.Entries<ITenantScopedEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                if (entry.Entity.TenantId != Guid.Empty && entry.Entity.TenantId != activeTenantId)
                {
                    throw new InvalidOperationException(
                        $"Cross-tenant operation detected: Entity {entry.Entity.GetType().Name} carries tenant '{entry.Entity.TenantId}', " +
                        $"which does not match active session tenant '{activeTenantId}'.");
                }
            }
        }
    }

    private void ConfigureTenantFilter(ModelBuilder modelBuilder, Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");
        var propertyAccess = Expression.Property(parameter, nameof(ITenantScopedEntity.TenantId));
        var tenantContextProperty = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));

        // Expression: CurrentTenantId != null && (Guid?)e.TenantId == CurrentTenantId
        var nullCheck = Expression.NotEqual(tenantContextProperty, Expression.Constant(null, typeof(Guid?)));
        var castProperty = Expression.Convert(propertyAccess, typeof(Guid?));
        var equalCheck = Expression.Equal(castProperty, tenantContextProperty);
        var filterExpression = Expression.AndAlso(nullCheck, equalCheck);

        var lambda = Expression.Lambda(filterExpression, parameter);
        modelBuilder.Entity(clrType).HasQueryFilter(lambda);
    }
}
