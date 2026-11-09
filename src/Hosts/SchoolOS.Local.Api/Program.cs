using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Observability;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization;
using SchoolOS.Modules.Organization.Domain;
using SchoolOS.Modules.Organization.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Building Blocks & Modules
builder.Services.AddSecurityBuildingBlocks();
builder.Services.AddPersistenceBuildingBlocks();
builder.Services.AddObservabilityBuildingBlocks();
builder.Services.AddOrganizationModule(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseObservabilityBuildingBlocks();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
                  .WithTags("Organization");

orgGroup.MapGet("/schools", async (
    [FromServices] OrganizationDbContext db,
    [FromServices] ITenantContext tenantContext) =>
{
    if (!tenantContext.IsAuthenticated || tenantContext.TenantId is null)
    {
        return Results.Unauthorized();
    }

    // Global query filter automatically restricts results to tenantContext.TenantId
    var schools = await db.Schools
                          .Include(s => s.Campuses)
                          .AsNoTracking()
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

    return Results.Created($"/api/v1/organization/schools/{school.Id}", school);
});

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

    var campuses = await query.ToListAsync();
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
    await db.SaveChangesAsync();

    return Results.Created($"/api/v1/organization/campuses/{campus.Id}", campus);
});

app.Run();

public record CreateSchoolRequest(string Name, string Code);
public record CreateCampusRequest(string Name, string Code, string? Address);
