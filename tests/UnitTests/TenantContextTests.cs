using System.Security.Claims;
using FluentAssertions;
using SchoolOS.BuildingBlocks.Security;
using Xunit;

namespace SchoolOS.UnitTests;

public class TenantContextTests
{
    [Fact]
    public void TenantContext_WithValidClaims_ShouldResolveProperties()
    {
        // Arrange
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

        // Act
        var context = new TenantContext(principal);

        // Assert
        context.IsAuthenticated.Should().BeTrue();
        context.TenantId.Should().Be(tenantId);
        context.CampusId.Should().Be(campusId);
        context.UserId.Should().Be(userId);
        context.HasScope("schools:read").Should().BeTrue();
        context.HasScope("role:Principal").Should().BeTrue();
        context.HasScope("finance:post").Should().BeFalse();
    }

    [Fact]
    public void TenantContext_Unauthenticated_ShouldFailClosed()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated

        // Act
        var context = new TenantContext(principal);

        // Assert
        context.IsAuthenticated.Should().BeFalse();
        context.TenantId.Should().BeNull();
        context.CampusId.Should().BeNull();
        context.UserId.Should().BeNull();
    }

    [Fact]
    public void TenantContext_AuthenticatedWithoutTenantId_ShouldFailClosed()
    {
        // Arrange: Authenticated user with sub but no tenant_id claim
        var claims = new[]
        {
            new Claim("sub", "global-user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var context = new TenantContext(principal);

        // Assert: User is authenticated, but TenantId must be null (failing closed for tenant operations)
        context.IsAuthenticated.Should().BeTrue();
        context.TenantId.Should().BeNull();
        context.UserId.Should().Be("global-user");
    }

    [Fact]
    public void TenantContext_WithMalformedTenantId_ShouldFailClosed()
    {
        // Arrange: Tenant ID claim is not a valid GUID
        var claims = new[]
        {
            new Claim("sub", "user-456"),
            new Claim(TenantContext.TenantIdClaimType, "not-a-valid-guid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var context = new TenantContext(principal);

        // Assert
        context.IsAuthenticated.Should().BeTrue();
        context.TenantId.Should().BeNull();
    }
}
