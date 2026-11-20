using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;
using SchoolOS.Modules.Admissions.Persistence;
using SchoolOS.Modules.Identity.Persistence;
using SchoolOS.Modules.Organization.Persistence;
using SchoolOS.PlatformModules.Subscriptions.Persistence;
using Shouldly;
using Xunit;

namespace SchoolOS.ArchitectureTests;

public class TenantModelInvariantTests
{
    private static DbContextOptions<TContext> InMemoryOptions<TContext>()
        where TContext : DbContext
        => new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase($"arch-invariant-{typeof(TContext).Name}-{Guid.NewGuid():N}")
            .Options;

    [Fact]
    public void All_Entities_With_NonNullable_TenantId_Must_Implement_ITenantScopedEntity_And_Have_QueryFilter()
    {
        var ctx = new TenantContext(Guid.NewGuid(), null, "arch-test");

        using var identityDb = new IdentityDbContext(InMemoryOptions<IdentityDbContext>(), ctx);
        using var orgDb = new OrganizationDbContext(InMemoryOptions<OrganizationDbContext>(), ctx);
        using var admDb = new AdmissionsDbContext(InMemoryOptions<AdmissionsDbContext>(), ctx);
        using var subDb = new SubscriptionsDbContext(InMemoryOptions<SubscriptionsDbContext>(), ctx);

        DbContext[] contexts = [identityDb, orgDb, admDb, subDb];

        foreach (var db in contexts)
        {
            foreach (var entityType in db.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                var tenantProp = clrType.GetProperty(nameof(ITenantScopedEntity.TenantId));
                if (tenantProp is not null && tenantProp.PropertyType == typeof(Guid))
                {
                    typeof(ITenantScopedEntity).IsAssignableFrom(clrType).ShouldBeTrue(
                        $"Entity {clrType.FullName} in {db.GetType().Name} declares non-nullable TenantId and must implement ITenantScopedEntity.");
                }

                if (typeof(ITenantScopedEntity).IsAssignableFrom(clrType))
                {
                    entityType.GetDeclaredQueryFilters().ShouldNotBeEmpty(
                        $"Tenant-scoped entity {clrType.FullName} in {db.GetType().Name} must have an active query filter.");
                }
            }
        }
    }
}
