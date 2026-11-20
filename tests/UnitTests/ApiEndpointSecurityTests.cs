using SchoolOS.BuildingBlocks.Security;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public class ApiEndpointSecurityTests
{
    [Fact]
    public void Unauthenticated_TenantContext_Has_Null_TenantId_And_FailsClosed()
    {
        var context = new TenantContext(null, null, null);
        context.IsAuthenticated.ShouldBeFalse();
        context.TenantId.ShouldBeNull();
        context.UserId.ShouldBeNull();
    }

    [Fact]
    public void Missing_TenantClaim_Leaves_TenantId_Null_Even_When_User_Is_Authenticated()
    {
        var context = new TenantContext(null, null, "user-without-tenant");
        context.IsAuthenticated.ShouldBeTrue();
        context.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Authenticated_TenantContext_Exposes_Verified_TenantId()
    {
        var tenantId = Guid.CreateVersion7();
        var context = new TenantContext(tenantId, null, "authorized-user");
        context.IsAuthenticated.ShouldBeTrue();
        context.TenantId.ShouldBe(tenantId);
        context.UserId.ShouldBe("authorized-user");
    }
}
