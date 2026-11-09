using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Observability;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.PlatformModules.Tenants;
using SchoolOS.PlatformModules.Tenants.Domain;
using SchoolOS.PlatformModules.Tenants.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Building Blocks & Modules
builder.Services.AddSecurityBuildingBlocks();
builder.Services.AddPersistenceBuildingBlocks();
builder.Services.AddObservabilityBuildingBlocks();
builder.Services.AddPlatformTenantsModule(builder.Configuration);

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
                     .WithTags("Platform Tenants");

tenantGroup.MapGet("/", async ([FromServices] PlatformDbContext db) =>
{
    var tenants = await db.Tenants.AsNoTracking().ToListAsync();
    return Results.Ok(tenants);
});

tenantGroup.MapPost("/", async (
    [FromBody] CreateTenantRequest request,
    [FromServices] PlatformDbContext db) =>
{
    var existingSlug = await db.Tenants.AnyAsync(t => t.Slug == request.Slug.ToLowerInvariant());
    if (existingSlug)
    {
        return Results.Conflict(new { message = $"Tenant with slug '{request.Slug}' already exists." });
    }

    var tenant = Tenant.Create(request.Name, request.Slug);
    db.Tenants.Add(tenant);
    await db.SaveChangesAsync();

    return Results.Created($"/api/v1/platform/tenants/{tenant.Id}", tenant);
});

app.Run();

public record CreateTenantRequest(string Name, string Slug);
