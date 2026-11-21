using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Localization;
using SchoolOS.BuildingBlocks.Observability;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Secrets;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Local.Api.LocalPlatform;
using SchoolOS.Modules.Admissions;
using SchoolOS.Modules.Admissions.Endpoints;
using SchoolOS.Modules.Attendance;
using SchoolOS.Modules.Attendance.Endpoints;
using SchoolOS.Modules.Fees;
using SchoolOS.Modules.Fees.Endpoints;
using SchoolOS.Modules.Exams;
using SchoolOS.Modules.Exams.Endpoints;
using SchoolOS.Modules.Identity;
using SchoolOS.Modules.Identity.Endpoints;
using SchoolOS.Modules.Organization;
using SchoolOS.Modules.Organization.Domain;
using SchoolOS.Modules.Organization.Endpoints;
using SchoolOS.Modules.Organization.Persistence;
using SchoolOS.Modules.Students;
using SchoolOS.Modules.Students.Endpoints;

var builder = WebApplication.CreateBuilder(args);
var isDevOrTesting = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");

// Load node/platform secrets from configured secret store (if enabled) before DB/Auth registration
await builder.AddPlatformSecretsAsync(SecretsDependencyInjection.LocalStartupSecrets);

// Building Blocks & Modules
builder.Services.AddSecretsBuildingBlocks(builder.Configuration);
builder.Services.AddWebHardeningBuildingBlocks(builder.Configuration);
builder.Services.AddLocalizationBuildingBlocks();
builder.Services.AddSecurityBuildingBlocks(builder.Configuration, isDevelopmentOrTesting: isDevOrTesting);
var offlineLicenseOptions = new OfflineLicenseVerificationOptions();
builder.Configuration.GetSection("OfflineLicense").Bind(offlineLicenseOptions);
builder.Services.AddSingleton(offlineLicenseOptions);
builder.Services.AddPersistenceBuildingBlocks();
builder.Services.AddDbContext<LocalPlatformDbContext>((sp, options) =>
{
    var localConn = builder.Configuration.GetConnectionString("LocalDatabase")
                    ?? builder.Configuration.GetConnectionString("SchoolOSDatabase")
                    ?? builder.Configuration["Database:ConnectionString"];
    if (!string.IsNullOrEmpty(localConn))
    {
        options.UseNpgsql(localConn, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "platform_local"))
               .UseSnakeCaseNamingConvention();
    }

    options.AddInterceptors(
        sp.GetRequiredService<AuditSaveChangesInterceptor>(),
        sp.GetRequiredService<PostgresTenantSessionInterceptor>(),
        sp.GetRequiredService<PostgresTenantTransactionInterceptor>());
});
builder.Services.AddScoped<ILicenseLeaseStore, PostgresLicenseLeaseStore>();
builder.Services.AddScoped<IModuleAccess, OfflineLicenseLeaseModuleAccess>();
builder.Services.AddObservabilityBuildingBlocks(builder.Configuration, builder.Environment, "SchoolOS.Local.Api");
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddOrganizationModule(builder.Configuration);
builder.Services.AddStudentsModule(builder.Configuration);
builder.Services.AddAdmissionsModule(builder.Configuration);
builder.Services.AddAttendanceModule(builder.Configuration);
builder.Services.AddFeesModule(builder.Configuration);
builder.Services.AddExamsModule(builder.Configuration);

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
// Identity & Scoped RBAC Endpoints (Local Data Plane)
// ==============================================================================
app.MapIdentityEndpoints();

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

app.MapGet("/api/v1/local/licensing/status", async (
    [FromServices] ITenantContext tenantContext,
    [FromServices] ILicenseLeaseStore store,
    [FromServices] OfflineLicenseVerificationOptions verificationOptions,
    TimeProvider timeProvider,
    CancellationToken ct) =>
{
    if (!tenantContext.IsAuthenticated || !tenantContext.TenantId.HasValue)
    {
        return Results.Unauthorized();
    }

    var state = await store.GetStateAsync(tenantContext.TenantId.Value, ct);
    var now = timeProvider.GetUtcNow();
    var verified = OfflineLicenseLeaseVerifier.Verify(
        state.Lease,
        tenantContext.TenantId.Value,
        verificationOptions.PublicKeyPem,
        now,
        verificationOptions.ExpectedNodeId,
        minimumSequenceNumber: state.HighestSequenceNumber > 0 ? state.HighestSequenceNumber : null,
        lastTrustedUtc: state.LastTrustedUtc);

    return Results.Ok(new
    {
        status = verified.Status.ToString(),
        isValid = verified.IsValid,
        failureReason = verified.FailureReason,
        tenantId = tenantContext.TenantId.Value,
        sequenceNumber = verified.Payload?.SequenceNumber ?? state.HighestSequenceNumber,
        expiresAtUtc = verified.Payload?.ExpiresAtUtc,
        paidThroughUtc = verified.Payload?.PaidThroughUtc,
        graceUntilUtc = verified.Payload?.GraceUntilUtc,
        enabledModules = verified.EntitledModules.Select(m => m.ToString()).ToList()
    });
}).RequireAuthorization(SecurityPolicies.TenantScopedPolicy);

app.MapPost("/api/v1/local/licensing/lease", async (
    [FromBody] SignedLicenseLease lease,
    [FromServices] ITenantContext tenantContext,
    [FromServices] ILicenseLeaseStore store,
    [FromServices] OfflineLicenseVerificationOptions verificationOptions,
    [FromServices] ISecurityAuditLog auditLog,
    TimeProvider timeProvider,
    CancellationToken ct) =>
{
    if (!tenantContext.IsAuthenticated || !tenantContext.TenantId.HasValue)
    {
        return Results.Unauthorized();
    }

    var tenantId = tenantContext.TenantId.Value;
    var existingState = await store.GetStateAsync(tenantId, ct);
    var verified = OfflineLicenseLeaseVerifier.Verify(
        lease,
        tenantId,
        verificationOptions.PublicKeyPem,
        timeProvider.GetUtcNow(),
        verificationOptions.ExpectedNodeId,
        minimumSequenceNumber: existingState.HighestSequenceNumber > 0 ? existingState.HighestSequenceNumber : null,
        lastTrustedUtc: existingState.LastTrustedUtc);

    if (!verified.IsValid)
    {
        await auditLog.WriteAsync(
            SecurityAuditEventType.AccessDenied,
            SecurityAuditOutcome.Failure,
            actorUserId: tenantContext.UserId,
            targetType: "OfflineLicenseLease",
            targetId: verified.FailureReason ?? "INVALID_LICENSE_LEASE",
            cancellationToken: ct);
        return Results.BadRequest(new { code = verified.FailureReason ?? "INVALID_LICENSE_LEASE", status = verified.Status.ToString() });
    }

    await store.SetLeaseAsync(tenantId, lease, ct);
    await auditLog.WriteAsync(
        SecurityAuditEventType.LicenseLeaseIssued,
        SecurityAuditOutcome.Success,
        actorUserId: tenantContext.UserId,
        targetType: "OfflineLicenseLease",
        targetId: verified.Payload?.KeyId,
        cancellationToken: ct);

    return Results.Ok(new
    {
        verified = true,
        status = verified.Status.ToString(),
        tenantId,
        sequenceNumber = verified.Payload?.SequenceNumber,
        expiresAtUtc = verified.Payload?.ExpiresAtUtc,
        enabledModules = verified.EntitledModules.ToList()
    });
}).RequireAuthorization(SecurityPolicies.OrganizationAdminPolicy);

app.MapEndpointGroups(typeof(OrganizationSchoolsDetailsEndpointGroup).Assembly);
app.MapEndpointGroups(typeof(StudentsEndpointGroup).Assembly);
app.MapEndpointGroups(typeof(AdmissionsEndpointGroup).Assembly);
app.MapEndpointGroups(typeof(AttendanceEndpointGroup).Assembly);
app.MapEndpointGroups(typeof(FeesEndpointGroup).Assembly);
app.MapEndpointGroups(typeof(ExamsEndpointGroup).Assembly);

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
