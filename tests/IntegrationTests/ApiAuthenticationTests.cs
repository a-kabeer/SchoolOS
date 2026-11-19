using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Persistence;
using SchoolOS.PlatformModules.Tenants.Persistence;
using Xunit;

namespace SchoolOS.IntegrationTests;

public class CloudApiFactory : WebApplicationFactory<SchoolOS.Cloud.Api.Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PlatformDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<PlatformDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            });
        });
    }
}

public class LocalApiFactory : WebApplicationFactory<SchoolOS.Local.Api.Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrganizationDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<OrganizationDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            });
        });
    }
}

public class ApiAuthenticationTests : IClassFixture<CloudApiFactory>, IClassFixture<LocalApiFactory>
{
    private readonly CloudApiFactory _cloudFactory;
    private readonly LocalApiFactory _localFactory;
    private readonly JwtOptions _jwtOptions = new();

    public ApiAuthenticationTests(CloudApiFactory cloudFactory, LocalApiFactory localFactory)
    {
        _cloudFactory = cloudFactory;
        _localFactory = localFactory;
    }

    private string CreateToken(
        string userId,
        Guid? tenantId = null,
        Guid? campusId = null,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? scopes = null,
        TimeSpan? lifetime = null,
        string? customSigningKey = null)
    {
        var options = new JwtOptions
        {
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningKey = customSigningKey ?? _jwtOptions.SigningKey,
            ExpiryMinutes = _jwtOptions.ExpiryMinutes
        };

        return JwtTokenHelper.GenerateToken(options, userId, tenantId, campusId, roles, scopes, lifetime);
    }

    [Fact]
    public async Task CloudApi_Anonymous_Get_Tenants_Returns_401_Unauthorized()
    {
        var client = _cloudFactory.CreateClient();
        var response = await client.GetAsync("/api/v1/platform/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CloudApi_Anonymous_Post_Tenant_Returns_401_Unauthorized()
    {
        var client = _cloudFactory.CreateClient();
        var payload = new { Name = "Test School System", Slug = "test-system" };
        var response = await client.PostAsJsonAsync("/api/v1/platform/tenants", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CloudApi_Authenticated_NonAdmin_To_Tenants_Returns_403_Forbidden()
    {
        var client = _cloudFactory.CreateClient();
        var token = CreateToken("standard-user", roles: new[] { "Teacher" });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/platform/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CloudApi_Tampered_Signature_Token_Returns_401_Unauthorized()
    {
        var client = _cloudFactory.CreateClient();
        // Signed with a different key than the API host expects
        var invalidToken = CreateToken("attacker", roles: new[] { SecurityPolicies.PlatformAdminRole }, customSigningKey: "Completely_Different_Invalid_Signing_Key_For_Attack_Testing!!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

        var response = await client.GetAsync("/api/v1/platform/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CloudApi_Expired_Token_Returns_401_Unauthorized()
    {
        var client = _cloudFactory.CreateClient();
        // Negative lifetime means token is already expired
        var expiredToken = CreateToken("platform-admin", roles: new[] { SecurityPolicies.PlatformAdminRole }, lifetime: TimeSpan.FromMinutes(-10));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/api/v1/platform/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CloudApi_Authenticated_PlatformAdmin_Can_Create_And_List_Tenants()
    {
        var client = _cloudFactory.CreateClient();
        var token = CreateToken("platform-superadmin", roles: new[] { SecurityPolicies.PlatformAdminRole });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var slug = $"slug-{Guid.NewGuid():N}";
        var payload = new { Name = "Al-Hikmah Educational Trust", Slug = slug };
        var postResponse = await client.PostAsJsonAsync("/api/v1/platform/tenants", payload);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await client.GetAsync("/api/v1/platform/tenants");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LocalApi_Anonymous_Get_Schools_Returns_401_Unauthorized()
    {
        var client = _localFactory.CreateClient();
        var response = await client.GetAsync("/api/v1/organization/schools");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LocalApi_Anonymous_Post_School_Returns_401_Unauthorized()
    {
        var client = _localFactory.CreateClient();
        var payload = new { Name = "City Campus", Code = "CC-01" };
        var response = await client.PostAsJsonAsync("/api/v1/organization/schools", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LocalApi_Authenticated_User_Without_Tenant_Claim_Returns_403_Forbidden()
    {
        var client = _localFactory.CreateClient();
        // User is authenticated, but carries no tenant_id claim
        var token = CreateToken("user-no-tenant");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/organization/schools");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LocalApi_Tampered_Signature_Token_Returns_401_Unauthorized()
    {
        var client = _localFactory.CreateClient();
        var invalidToken = CreateToken("attacker", tenantId: Guid.NewGuid(), customSigningKey: "Completely_Different_Invalid_Signing_Key_For_Attack_Testing!!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

        var response = await client.GetAsync("/api/v1/organization/schools");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LocalApi_Expired_Token_Returns_401_Unauthorized()
    {
        var client = _localFactory.CreateClient();
        var expiredToken = CreateToken("local-admin", tenantId: Guid.NewGuid(), lifetime: TimeSpan.FromMinutes(-5));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/api/v1/organization/schools");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LocalApi_Authenticated_Tenant_User_Without_Admin_Role_Gets_403_When_Creating_School()
    {
        var client = _localFactory.CreateClient();
        var tenantId = Guid.NewGuid();
        // User has verified tenant_id, but is only a Teacher (insufficient permissions for school creation)
        var token = CreateToken("teacher-user", tenantId: tenantId, roles: new[] { SecurityPolicies.TeacherRole });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Can read schools within tenant scope
        var getResponse = await client.GetAsync("/api/v1/organization/schools");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cannot create new school (requires OrganizationAdminPolicy)
        var payload = new { Name = "Unauthorized School Attempt", Code = "FAIL-01" };
        var postResponse = await client.PostAsJsonAsync("/api/v1/organization/schools", payload);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LocalApi_Authenticated_TenantAdmin_Can_Create_And_List_Schools()
    {
        var client = _localFactory.CreateClient();
        var tenantId = Guid.NewGuid();
        var token = CreateToken("admin-user", tenantId: tenantId, roles: new[] { SecurityPolicies.TenantAdminRole });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var code = $"SCH-{Guid.NewGuid():N}"[..10];
        var payload = new { Name = "Beaconhouse Main Branch", Code = code };
        var postResponse = await client.PostAsJsonAsync("/api/v1/organization/schools", payload);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await client.GetAsync("/api/v1/organization/schools");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LocalApi_Authenticated_SchoolAdmin_Can_Create_Campus()
    {
        var client = _localFactory.CreateClient();
        var tenantId = Guid.NewGuid();
        var token = CreateToken("school-admin-user", tenantId: tenantId, roles: new[] { SecurityPolicies.SchoolAdminRole });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // First create school as school admin
        var schoolCode = $"SCH-{Guid.NewGuid():N}"[..10];
        var schoolPayload = new { Name = "City School Model Town", Code = schoolCode };
        var schoolPost = await client.PostAsJsonAsync("/api/v1/organization/schools", schoolPayload);
        schoolPost.StatusCode.Should().Be(HttpStatusCode.Created);

        var school = await schoolPost.Content.ReadFromJsonAsync<CreatedSchoolResponse>();
        school.Should().NotBeNull();

        // Create campus
        var campusPayload = new { Name = "Junior Branch", Code = "JB-01", Address = "123 Main St" };
        var campusPost = await client.PostAsJsonAsync($"/api/v1/organization/schools/{school!.Id}/campuses", campusPayload);
        campusPost.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task LocalApi_Authenticated_User_With_Functional_Scope_Can_Create_School()
    {
        var client = _localFactory.CreateClient();
        var tenantId = Guid.NewGuid();
        // Functional scope instead of explicit role
        var token = CreateToken("scoped-operator", tenantId: tenantId, scopes: new[] { SecurityPolicies.SchoolsWriteScope });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var code = $"SCH-{Guid.NewGuid():N}"[..10];
        var payload = new { Name = "Scope-Authorized Academy", Code = code };
        var postResponse = await client.PostAsJsonAsync("/api/v1/organization/schools", payload);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CloudApi_And_LocalApi_Health_Probes_Are_Public()
    {
        var cloudClient = _cloudFactory.CreateClient();
        var localClient = _localFactory.CreateClient();

        var cloudHealth = await cloudClient.GetAsync("/healthz");
        cloudHealth.StatusCode.Should().Be(HttpStatusCode.OK);

        var localHealth = await localClient.GetAsync("/healthz");
        localHealth.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public record CreatedSchoolResponse(Guid Id, Guid TenantId, string Name, string Code);
