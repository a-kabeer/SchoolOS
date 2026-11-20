using System.Security.Claims;
using SchoolOS.BuildingBlocks.Security;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public class TenantContextTests
{
    [Fact]
    public void TenantContext_WithValidClaims_ShouldResolveProperties()
    {
        var tenantId = Guid.CreateVersion7();
        var campusId = Guid.CreateVersion7();
        var userId = "user-123";

        var claims = new[]
        {
            new Claim("sub", userId),
            new Claim(TenantContext.TenantIdClaimType, tenantId.ToString()),
            new Claim(TenantContext.CampusIdClaimType, campusId.ToString()),
            new Claim(TenantContext.ScopeClaimType, "schools:read"),
            new Claim(ClaimTypes.Role, "Principal")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new TenantContext(principal);

        context.IsAuthenticated.ShouldBeTrue();
        context.TenantId.ShouldBe(tenantId);
        context.CampusId.ShouldBe(campusId);
        context.UserId.ShouldBe(userId);
        context.HasScope("schools:read").ShouldBeTrue();
        context.HasScope("role:Principal").ShouldBeTrue();
        context.HasScope("finance:post").ShouldBeFalse();
    }

    [Fact]
    public void TenantContext_Unauthenticated_ShouldFailClosed()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new TenantContext(principal);

        context.IsAuthenticated.ShouldBeFalse();
        context.TenantId.ShouldBeNull();
        context.CampusId.ShouldBeNull();
        context.UserId.ShouldBeNull();
    }

    [Fact]
    public void TenantContext_AuthenticatedWithoutTenantId_ShouldFailClosed()
    {
        var claims = new[]
        {
            new Claim("sub", "global-user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new TenantContext(principal);

        context.IsAuthenticated.ShouldBeTrue();
        context.TenantId.ShouldBeNull();
        context.UserId.ShouldBe("global-user");
    }

    [Fact]
    public void TenantContext_WithMalformedTenantId_ShouldFailClosed()
    {
        var claims = new[]
        {
            new Claim("sub", "user-456"),
            new Claim(TenantContext.TenantIdClaimType, "not-a-valid-guid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new TenantContext(principal);

        context.IsAuthenticated.ShouldBeTrue();
        context.TenantId.ShouldBeNull();
    }
}
