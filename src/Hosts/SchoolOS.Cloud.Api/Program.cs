using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Localization;
using SchoolOS.BuildingBlocks.Observability;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Secrets;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.PlatformModules.Licensing;
using SchoolOS.PlatformModules.Licensing.Endpoints;
using SchoolOS.PlatformModules.Subscriptions;
using SchoolOS.PlatformModules.Subscriptions.Endpoints;
using SchoolOS.PlatformModules.Tenants;
using SchoolOS.PlatformModules.Tenants.Domain;
using SchoolOS.PlatformModules.Tenants.Endpoints;
using SchoolOS.PlatformModules.Tenants.Persistence;

var builder = WebApplication.CreateBuilder(args);
var isDevOrTesting = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");

// Load platform secrets from configured secret store (if enabled) before DB/Auth registration
await builder.AddPlatformSecretsAsync(SecretsDependencyInjection.CloudStartupSecrets);

// Building Blocks & Modules
builder.Services.AddSecretsBuildingBlocks(builder.Configuration);
builder.Services.AddWebHardeningBuildingBlocks(builder.Configuration);
builder.Services.AddLocalizationBuildingBlocks();
builder.Services.AddSecurityBuildingBlocks(builder.Configuration, isDevelopmentOrTesting: isDevOrTesting);
builder.Services.AddPersistenceBuildingBlocks();
builder.Services.AddObservabilityBuildingBlocks(builder.Configuration, builder.Environment, "SchoolOS.Cloud.Api");
builder.Services.AddPlatformTenantsModule(builder.Configuration);
builder.Services.AddSubscriptionsModule(builder.Configuration, enableBackgroundWorker: !isDevOrTesting);
builder.Services.AddLicensingModule(builder.Configuration, isDevelopmentOrTesting: isDevOrTesting);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (!isDevOrTesting)
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSchoolOSSecurityHeaders(isDevOrTesting);
app.UseObservabilityBuildingBlocks();
app.UseRequestLocalizationContext();
app.UseRateLimiter();
app.UseAuthentication();
app.UseTenantTelemetry();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Liveness & Readiness Probes
app.MapGet("/healthz", () => Results.Ok(new { status = "Live", timestamp = DateTime.UtcNow }));
app.MapGet("/health/ready", async ([FromServices] PlatformDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "Ready", database = "Connected", timestamp = DateTime.UtcNow })
        : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Platform database connection failed");
});

// Legacy health endpoint for backwards compatibility
app.MapGet("/health", () => Results.Ok(new 
{ 
    service = "SchoolOS.Cloud.Api", 
    status = "Healthy", 
    plane = "CloudControlPlane",
    timestamp = DateTime.UtcNow 
}));

// ==============================================================================
// Platform Tenant Registry Endpoints (Cloud Control Plane)
// ==============================================================================
var tenantGroup = app.MapGroup("/api/v1/platform/tenants")
                     .RequireAuthorization(SecurityPolicies.PlatformAdminPolicy)
                     .WithTags("Platform Tenants");

tenantGroup.MapGet("/", async (
    [FromServices] PlatformDbContext db,
    [FromServices] ITenantContext tenantContext) =>
{
    if (!tenantContext.IsAuthenticated)
    {
        return Results.Unauthorized();
    }

    var tenants = await db.Tenants
        .AsNoTracking()
        .Select(t => new TenantResponse(t.Id, t.Name, t.Slug, t.Status, t.IsActive, t.CreatedAtUtc))
        .ToListAsync();
    return Results.Ok(tenants);
});

tenantGroup.MapPost("/", async (
    [FromBody] CreateTenantRequest request,
    [FromServices] PlatformDbContext db,
    [FromServices] ITenantContext tenantContext) =>
{
    if (!tenantContext.IsAuthenticated)
    {
        return Results.Unauthorized();
    }

    var existingSlug = await db.Tenants.AnyAsync(t => t.Slug == request.Slug.ToLowerInvariant());
    if (existingSlug)
    {
        return Results.Conflict(new { message = $"Tenant with slug '{request.Slug}' already exists." });
    }

    var tenant = Tenant.Create(request.Name, request.Slug);
    db.Tenants.Add(tenant);
    await db.SaveChangesAsync();

    var response = new TenantResponse(tenant.Id, tenant.Name, tenant.Slug, tenant.Status, tenant.IsActive, tenant.CreatedAtUtc);
    return Results.Created($"/api/v1/platform/tenants/{tenant.Id}", response);
});

app.MapEndpointGroups(typeof(PublicSchoolRequestsEndpointGroup).Assembly);
app.MapEndpointGroups(typeof(PlatformSubscriptionsEndpointGroup).Assembly);
app.MapEndpointGroups(typeof(PlatformLicensingEndpointGroup).Assembly);

var runMigrateOnly = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);
var runSeedCatalogOnly = args.Contains("--seed-catalog", StringComparer.OrdinalIgnoreCase);
var autoMigrate = builder.Configuration.GetValue<bool>("Bootstrap:AutoMigrateOnStartup");
var runtimeRoleName = builder.Configuration["Bootstrap:RuntimeRoleName"] ?? "schoolos_app";
var runtimeRolePassword = builder.Configuration["Bootstrap:RuntimeRolePassword"];

if (runMigrateOnly || runSeedCatalogOnly || autoMigrate)
{
    await SchoolOS.Cloud.Api.CloudDatabaseBootstrapper.ApplyMigrationsAndGrantsAsync(app.Services, runtimeRoleName, runtimeRolePassword);
}

if (runSeedCatalogOnly || (builder.Configuration.GetValue<bool>("Bootstrap:SeedCatalogOnStartup") && app.Environment.IsDevelopment()))
{
    await SchoolOS.Cloud.Api.CloudDatabaseBootstrapper.SeedCatalogAsync(app.Services);
}

if (runMigrateOnly || runSeedCatalogOnly)
{
    return;
}

app.Run();

public record CreateTenantRequest(string Name, string Slug);
public record TenantResponse(Guid Id, string Name, string Slug, TenantStatus Status, bool IsActive, DateTime CreatedAtUtc);

namespace SchoolOS.Cloud.Api
{
    public partial class Program { }
}
