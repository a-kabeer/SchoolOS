using FluentAssertions;
using SchoolOS.BuildingBlocks.Security;
using Xunit;

namespace SchoolOS.UnitTests;

public class ApiEndpointSecurityTests
{
    [Fact]
    public void Unauthenticated_TenantContext_Has_Null_TenantId_And_FailsClosed()
    {
        var context = new TenantContext(null, null, null);
        context.IsAuthenticated.Should().BeFalse();
        context.TenantId.Should().BeNull();
        context.UserId.Should().BeNull();
    }

    [Fact]
    public void Missing_TenantClaim_Leaves_TenantId_Null_Even_When_User_Is_Authenticated()
    {
        // An authenticated user who is not assigned to any tenant has UserId but null TenantId
        var context = new TenantContext(null, null, "user-without-tenant");
        context.IsAuthenticated.Should().BeTrue();
        context.TenantId.Should().BeNull();
    }

    [Fact]
    public void Authenticated_TenantContext_Exposes_Verified_TenantId()
    {
        var tenantId = Guid.CreateVersion7();
        var context = new TenantContext(tenantId, null, "authorized-user");
        context.IsAuthenticated.Should().BeTrue();
        context.TenantId.Should().Be(tenantId);
        context.UserId.Should().Be("authorized-user");
    }
}
