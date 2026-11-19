using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Observability;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Secrets;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Modules.Organization;
using SchoolOS.Modules.Organization.Domain;
using SchoolOS.Modules.Organization.Persistence;

var builder = WebApplication.CreateBuilder(args);
var isDevOrTesting = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");

// Load node/platform secrets from configured secret store (if enabled) before DB/Auth registration
await builder.AddPlatformSecretsAsync(SecretsDependencyInjection.LocalStartupSecrets);

// Building Blocks & Modules
builder.Services.AddSecretsBuildingBlocks(builder.Configuration);
builder.Services.AddWebHardeningBuildingBlocks(builder.Configuration);
builder.Services.AddSecurityBuildingBlocks(builder.Configuration, isDevelopmentOrTesting: isDevOrTesting);
builder.Services.AddPersistenceBuildingBlocks();
builder.Services.AddObservabilityBuildingBlocks(builder.Configuration, builder.Environment, "SchoolOS.Local.Api");
builder.Services.AddOrganizationModule(builder.Configuration);

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
app.MapGet("/health/ready", async ([FromServices] OrganizationDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "Ready", database = "Connected", timestamp = DateTime.UtcNow })
        : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Database connection failed");
});

// Legacy health endpoint for backwards compatibility
app.MapGet("/health", () => Results.Ok(new 
{ 
    service = "SchoolOS.Local.Api", 
    status = "Healthy", 
    plane = "CampusLocalDataPlane",
    timestamp = DateTime.UtcNow 
}));

// ==============================================================================
// Organization Endpoints (Local Data Plane — Multi-Tenant Scoped)
// ==============================================================================
var orgGroup = app.MapGroup("/api/v1/organization")
                  .RequireAuthorization(SecurityPolicies.TenantScopedPolicy)
                  .WithTags("Organization");

orgGroup.MapGet("/schools", async (
    [FromServices] OrganizationDbContext db,
    [FromServices] ITenantContext tenantContext) =>
{
    if (!tenantContext.IsAuthenticated || tenantContext.TenantId is null)
    {
        return Results.Unauthorized();
    }

    // Project directly to explicit DTOs (never expose EF Core entities over the API)
    var schools = await db.Schools
        .AsNoTracking()
        .Select(s => new SchoolResponse(
            s.Id,
            s.TenantId,
            s.Name,
            s.Code,
            s.IsActive,
            s.Campuses.Select(c => new CampusResponse(
                c.Id,
                c.TenantId,
                c.SchoolId,
                c.Name,
                c.Code,
                c.Address,
                c.IsActive)).ToList()))
        .ToListAsync();

    return Results.Ok(schools);
});

orgGroup.MapPost("/schools", async (
    [FromBody] CreateSchoolRequest request,
    [FromServices] OrganizationDbContext db,
    [FromServices] ITenantContext tenantContext) =>
{
    if (!tenantContext.IsAuthenticated || tenantContext.TenantId is null)
    {
        return Results.Unauthorized();
    }

    var school = School.Create(tenantContext.TenantId.Value, request.Name, request.Code);
    db.Schools.Add(school);
    await db.SaveChangesAsync();

    var response = new CreateSchoolResponse(school.Id, school.TenantId, school.Name, school.Code);
    return Results.Created($"/api/v1/organization/schools/{school.Id}", response);
}).RequireAuthorization(SecurityPolicies.OrganizationAdminPolicy);

orgGroup.MapGet("/campuses", async (
    [FromServices] OrganizationDbContext db,
    [FromServices] ITenantContext tenantContext) =>
{
    if (!tenantContext.IsAuthenticated || tenantContext.TenantId is null)
    {
        return Results.Unauthorized();
    }

    var query = db.Campuses.AsNoTracking();

    // If request has restricted campus scope, filter by campus as well
    if (tenantContext.CampusId.HasValue)
    {
        query = query.Where(c => c.Id == tenantContext.CampusId.Value);
    }

    var campuses = await query
        .Select(c => new CampusResponse(
            c.Id,
            c.TenantId,
            c.SchoolId,
            c.Name,
            c.Code,
            c.Address,
            c.IsActive))
        .ToListAsync();
    return Results.Ok(campuses);
});

orgGroup.MapPost("/schools/{schoolId:guid}/campuses", async (
    Guid schoolId,
    [FromBody] CreateCampusRequest request,
    [FromServices] OrganizationDbContext db,
    [FromServices] ITenantContext tenantContext) =>
{
    if (!tenantContext.IsAuthenticated || tenantContext.TenantId is null)
    {
        return Results.Unauthorized();
    }

    var school = await db.Schools.FirstOrDefaultAsync(s => s.Id == schoolId);
    if (school is null)
    {
        return Results.NotFound(new { message = $"School with ID '{schoolId}' was not found in active tenant scope." });
    }

    var campus = school.AddCampus(request.Name, request.Code, request.Address);
    db.Campuses.Add(campus);
    await db.SaveChangesAsync();

    var response = new CreateCampusResponse(campus.Id, campus.TenantId, campus.SchoolId, campus.Name, campus.Code, campus.Address);
    return Results.Created($"/api/v1/organization/campuses/{campus.Id}", response);
}).RequireAuthorization(SecurityPolicies.OrganizationAdminPolicy);

app.Run();

public record CreateSchoolRequest(string Name, string Code);
public record CreateSchoolResponse(Guid Id, Guid TenantId, string Name, string Code);
public record SchoolResponse(Guid Id, Guid TenantId, string Name, string Code, bool IsActive, IReadOnlyList<CampusResponse> Campuses);
public record CreateCampusRequest(string Name, string Code, string? Address);
public record CreateCampusResponse(Guid Id, Guid TenantId, Guid SchoolId, string Name, string Code, string? Address);
public record CampusResponse(Guid Id, Guid TenantId, Guid SchoolId, string Name, string Code, string? Address, bool IsActive);

namespace SchoolOS.Local.Api
{
    public partial class Program { }
}
