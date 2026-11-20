using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.PlatformModules.Tenants.Domain;
using SchoolOS.PlatformModules.Tenants.Persistence;

namespace SchoolOS.PlatformModules.Tenants.Endpoints;

public sealed record SubmitSchoolRequestBody(string SchoolName, string ContactName, string ContactEmail);

public sealed record ApproveSchoolRequestBody(
    string Slug,
    string CountryCode,
    DataRegion? DataRegion = null,
    string? DecisionNotes = null);

public sealed record RejectSchoolRequestBody(string? DecisionNotes);

public sealed record UpsertCountryBody(
    string Code,
    string Name,
    string LanguagesCsv,
    string DefaultLanguage,
    string TimeZoneId,
    string CurrencyCode,
    bool IsActive = true);

public sealed record CreateStateBody(string Code, string Name);
public sealed record CreateCityBody(string Name);

public sealed class PublicSchoolRequestsEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/public/school-requests";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.AllowAnonymous();
        groupBuilder.RequireRateLimiting(RateLimitPolicies.SchoolRequest);
        groupBuilder.MapNamedPost(SubmitSchoolRequest);
    }

    private static async Task<IResult> SubmitSchoolRequest(
        SubmitSchoolRequestBody request,
        PlatformDbContext db,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (tenantContext is TenantContext mutable &&
            !mutable.IsAuthenticated &&
            !mutable.TenantId.HasValue &&
            string.IsNullOrWhiteSpace(mutable.UserId))
        {
            mutable.EnterPublicActorScope("public:school-request");
        }

        var entity = SchoolRequest.Submit(
            request.SchoolName,
            request.ContactName,
            request.ContactEmail,
            timeProvider.GetUtcNow());

        db.SchoolRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/v1/platform/school-requests/{entity.Id}",
            new { entity.Id, entity.Status, entity.SubmittedAtUtc });
    }
}

public sealed class PlatformSchoolRequestsEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/platform/school-requests";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(SecurityPolicies.PlatformAdminPolicy);
        groupBuilder.MapNamedGet(ListSchoolRequests);
        groupBuilder.MapNamedPost(ApproveSchoolRequest, "{id:guid}/approve");
        groupBuilder.MapNamedPost(RejectSchoolRequest, "{id:guid}/reject");
    }

    private static async Task<IResult> ListSchoolRequests(
        PlatformDbContext db,
        SchoolRequestStatus? status,
        CancellationToken cancellationToken)
    {
        var query = db.SchoolRequests.AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var items = await query
            .OrderByDescending(r => r.SubmittedAtUtc)
            .Select(r => new
            {
                r.Id,
                r.SchoolName,
                r.ContactName,
                r.ContactEmail,
                r.Status,
                r.SubmittedAtUtc,
                r.ReviewedAtUtc,
                r.ApprovedTenantId
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> ApproveSchoolRequest(
        Guid id,
        ApproveSchoolRequestBody request,
        PlatformDbContext db,
        ITenantContext tenantContext,
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var schoolRequest = await db.SchoolRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (schoolRequest is null)
        {
            return Results.NotFound();
        }

        if (schoolRequest.Status != SchoolRequestStatus.Pending)
        {
            return Results.Conflict(new { code = "SCHOOL_REQUEST_NOT_PENDING" });
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (await db.Tenants.AnyAsync(t => t.Slug == normalizedSlug, cancellationToken))
        {
            return Results.Conflict(new { code = "TENANT_SLUG_TAKEN" });
        }

        var normalizedCountry = Country.NormalizeCode(request.CountryCode);
        if (!await db.Countries.AnyAsync(c => c.Code == normalizedCountry && c.IsActive, cancellationToken))
        {
            return Results.BadRequest(new { code = "UNKNOWN_COUNTRY" });
        }

        var now = timeProvider.GetUtcNow();
        var tenant = Tenant.Create(schoolRequest.SchoolName, normalizedSlug);
        tenant.Activate();

        schoolRequest.Approve(tenant.Id, tenantContext.UserId ?? "platform-admin", now, request.DecisionNotes);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        Guid? trialSubscriptionId = null;
        var provisioner = serviceProvider.GetService<ITenantTrialProvisioner>();
        if (provisioner is not null)
        {
            trialSubscriptionId = await provisioner.StartTrialAsync(tenant.Id, normalizedCountry, cancellationToken);
        }

        return Results.Ok(new
        {
            schoolRequest.Id,
            TenantId = tenant.Id,
            tenant.Slug,
            SubscriptionId = trialSubscriptionId,
            Status = schoolRequest.Status
        });
    }

    private static async Task<IResult> RejectSchoolRequest(
        Guid id,
        RejectSchoolRequestBody request,
        PlatformDbContext db,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var schoolRequest = await db.SchoolRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (schoolRequest is null)
        {
            return Results.NotFound();
        }

        if (schoolRequest.Status != SchoolRequestStatus.Pending)
        {
            return Results.Conflict(new { code = "SCHOOL_REQUEST_NOT_PENDING" });
        }

        schoolRequest.Reject(tenantContext.UserId ?? "platform-admin", timeProvider.GetUtcNow(), request.DecisionNotes);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { schoolRequest.Id, schoolRequest.Status });
    }
}

public sealed class PlatformCountriesEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/platform/countries";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(SecurityPolicies.PlatformAdminPolicy);
        groupBuilder.MapNamedGet(ListCountries);
        groupBuilder.MapNamedPost(CreateCountry);
        groupBuilder.MapNamedPut(UpdateCountry, "{code}");
        groupBuilder.MapNamedPost(CreateState, "{code}/states");
        groupBuilder.MapNamedPost(CreateCity, "states/{stateId:guid}/cities");
    }

    private static async Task<IResult> ListCountries(PlatformDbContext db, CancellationToken cancellationToken)
    {
        var items = await db.Countries.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.LanguagesCsv,
                c.DefaultLanguage,
                c.TimeZoneId,
                c.CurrencyCode,
                c.IsActive
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateCountry(
        UpsertCountryBody request,
        PlatformDbContext db,
        CancellationToken cancellationToken)
    {
        var normalizedCode = Country.NormalizeCode(request.Code);
        if (await db.Countries.AnyAsync(c => c.Code == normalizedCode, cancellationToken))
        {
            return Results.Conflict(new { code = "COUNTRY_CODE_TAKEN" });
        }

        var country = Country.Create(
            normalizedCode,
            request.Name,
            request.LanguagesCsv,
            request.DefaultLanguage,
            request.TimeZoneId,
            request.CurrencyCode);

        db.Countries.Add(country);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/platform/countries/{country.Code}", new { country.Id, country.Code, country.CurrencyCode });
    }

    private static async Task<IResult> UpdateCountry(
        string code,
        UpsertCountryBody request,
        PlatformDbContext db,
        CancellationToken cancellationToken)
    {
        var normalizedCode = Country.NormalizeCode(code);
        var country = await db.Countries.FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);
        if (country is null)
        {
            return Results.NotFound();
        }

        country.Update(
            request.Name,
            request.LanguagesCsv,
            request.DefaultLanguage,
            request.TimeZoneId,
            request.CurrencyCode,
            request.IsActive);

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { country.Id, country.Code, country.CurrencyCode, country.IsActive });
    }

    private static async Task<IResult> CreateState(
        string code,
        CreateStateBody request,
        PlatformDbContext db,
        CancellationToken cancellationToken)
    {
        var normalizedCode = Country.NormalizeCode(code);
        var country = await db.Countries.FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);
        if (country is null)
        {
            return Results.NotFound();
        }

        var state = State.Create(country.Id, request.Code, request.Name);
        db.States.Add(state);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/platform/countries/{country.Code}/states/{state.Id}", new { state.Id, state.Code, state.Name });
    }

    private static async Task<IResult> CreateCity(
        Guid stateId,
        CreateCityBody request,
        PlatformDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await db.States.AnyAsync(s => s.Id == stateId, cancellationToken))
        {
            return Results.NotFound();
        }

        var city = City.Create(stateId, request.Name);
        db.Cities.Add(city);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/platform/countries/states/{stateId}/cities/{city.Id}", new { city.Id, city.Name });
    }
}
