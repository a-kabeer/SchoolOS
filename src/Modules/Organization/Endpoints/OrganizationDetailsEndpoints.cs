using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Modules.Organization.Domain;
using SchoolOS.Modules.Organization.Persistence;

namespace SchoolOS.Modules.Organization.Endpoints;

public sealed record UpsertCampusDetailsBody(
    string AddressLine1,
    string City,
    string PostalCode,
    string? Email,
    string? WebsiteUrl,
    decimal? Latitude,
    decimal? Longitude,
    string TimezoneId = "UTC");

public sealed record AddCampusPhoneBody(string Label, string PhoneNumber, bool IsPrimary = false);
public sealed record UpsertSchoolBrandingBody(string PrimaryColorHex, string SecondaryColorHex, string AccentColorHex);
public sealed record UploadBrandingImageBody(string Base64Data);
public sealed record UpsertSchoolProfileBody(
    string? Tagline,
    string? PrincipalName,
    int? EstablishedYear,
    string? ContactEmail,
    string? WebsiteUrl,
    string DefaultLanguage = "en");

public sealed class OrganizationSchoolsDetailsEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/organization/schools";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapNamedGet(GetSchoolBranding, "{schoolId:guid}/branding")
            .RequireModulePermission(ModuleCode.School, PermissionAction.View);
        groupBuilder.MapNamedPut(UpsertSchoolBranding, "{schoolId:guid}/branding")
            .RequireModulePermission(ModuleCode.School, PermissionAction.Edit);

        groupBuilder.MapNamedGet(DownloadBrandingImage, "{schoolId:guid}/branding/images/{slot}")
            .RequireModulePermission(ModuleCode.School, PermissionAction.View);
        groupBuilder.MapNamedPut(UploadBrandingImage, "{schoolId:guid}/branding/images/{slot}")
            .RequireModulePermission(ModuleCode.School, PermissionAction.Edit);

        groupBuilder.MapNamedGet(GetSchoolProfile, "{schoolId:guid}/profile")
            .RequireModulePermission(ModuleCode.School, PermissionAction.View);
        groupBuilder.MapNamedPut(UpsertSchoolProfile, "{schoolId:guid}/profile")
            .RequireModulePermission(ModuleCode.School, PermissionAction.Edit);
    }

    private static async Task<IResult> GetSchoolBranding(
        Guid schoolId,
        OrganizationDbContext db,
        CancellationToken cancellationToken)
    {
        var branding = await db.SchoolBrandings.AsNoTracking()
            .FirstOrDefaultAsync(b => b.SchoolId == schoolId, cancellationToken);
        if (branding is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            branding.Id,
            branding.SchoolId,
            branding.PrimaryColorHex,
            branding.SecondaryColorHex,
            branding.AccentColorHex
        });
    }

    private static async Task<IResult> UpsertSchoolBranding(
        Guid schoolId,
        UpsertSchoolBrandingBody request,
        OrganizationDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        var schoolExists = await db.Schools.AnyAsync(s => s.Id == schoolId, cancellationToken);
        if (!schoolExists)
        {
            return Results.NotFound();
        }

        var existing = await db.SchoolBrandings.FirstOrDefaultAsync(b => b.SchoolId == schoolId, cancellationToken);
        if (existing is null)
        {
            existing = SchoolBranding.Create(
                tenantContext.TenantId.Value,
                schoolId,
                request.PrimaryColorHex,
                request.SecondaryColorHex,
                request.AccentColorHex);
            db.SchoolBrandings.Add(existing);
        }
        else
        {
            existing.UpdateColors(request.PrimaryColorHex, request.SecondaryColorHex, request.AccentColorHex);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new
        {
            existing.Id,
            existing.SchoolId,
            existing.PrimaryColorHex,
            existing.SecondaryColorHex,
            existing.AccentColorHex
        });
    }

    private static async Task<IResult> DownloadBrandingImage(
        Guid schoolId,
        BrandingImageSlot slot,
        OrganizationDbContext db,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var img = await db.BrandingImages.AsNoTracking()
            .FirstOrDefaultAsync(i => i.SchoolId == schoolId && i.Slot == slot, cancellationToken);
        if (img is null)
        {
            return Results.NotFound();
        }

        httpContext.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        return Results.File(img.Data, img.ContentType, fileDownloadName: $"{slot.ToString().ToLowerInvariant()}");
    }

    private static async Task<IResult> UploadBrandingImage(
        Guid schoolId,
        BrandingImageSlot slot,
        UploadBrandingImageBody request,
        OrganizationDbContext db,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!await db.Schools.AnyAsync(s => s.Id == schoolId, cancellationToken))
        {
            return Results.NotFound();
        }

        byte[] rawBytes;
        try
        {
            rawBytes = Convert.FromBase64String(request.Base64Data);
        }
        catch (FormatException)
        {
            return Results.BadRequest(new { code = "INVALID_BASE64_IMAGE" });
        }

        var now = timeProvider.GetUtcNow();
        var existing = await db.BrandingImages.FirstOrDefaultAsync(i => i.SchoolId == schoolId && i.Slot == slot, cancellationToken);
        if (existing is null)
        {
            existing = BrandingImage.Create(tenantContext.TenantId.Value, schoolId, slot, rawBytes, now);
            db.BrandingImages.Add(existing);
        }
        else
        {
            existing.ReplaceContent(rawBytes, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { existing.Id, existing.Slot, existing.ContentType, existing.Sha256Hash });
    }

    private static async Task<IResult> GetSchoolProfile(
        Guid schoolId,
        OrganizationDbContext db,
        CancellationToken cancellationToken)
    {
        var profile = await db.SchoolProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.SchoolId == schoolId, cancellationToken);
        if (profile is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            profile.Id,
            profile.SchoolId,
            profile.Tagline,
            profile.PrincipalName,
            profile.EstablishedYear,
            profile.ContactEmail,
            profile.WebsiteUrl,
            profile.DefaultLanguage
        });
    }

    private static async Task<IResult> UpsertSchoolProfile(
        Guid schoolId,
        UpsertSchoolProfileBody request,
        OrganizationDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!await db.Schools.AnyAsync(s => s.Id == schoolId, cancellationToken))
        {
            return Results.NotFound();
        }

        var existing = await db.SchoolProfiles.FirstOrDefaultAsync(p => p.SchoolId == schoolId, cancellationToken);
        if (existing is null)
        {
            existing = SchoolProfile.Create(
                tenantContext.TenantId.Value,
                schoolId,
                request.Tagline,
                request.PrincipalName,
                request.EstablishedYear,
                request.ContactEmail,
                request.WebsiteUrl,
                request.DefaultLanguage);
            db.SchoolProfiles.Add(existing);
        }
        else
        {
            existing.Update(
                request.Tagline,
                request.PrincipalName,
                request.EstablishedYear,
                request.ContactEmail,
                request.WebsiteUrl,
                request.DefaultLanguage);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new
        {
            existing.Id,
            existing.SchoolId,
            existing.Tagline,
            existing.PrincipalName,
            existing.EstablishedYear,
            existing.ContactEmail,
            existing.WebsiteUrl,
            existing.DefaultLanguage
        });
    }
}

public sealed class OrganizationCampusesDetailsEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/organization/campuses";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapNamedGet(GetCampusDetails, "{campusId:guid}/details")
            .RequireModulePermission(ModuleCode.Campus, PermissionAction.View);
        groupBuilder.MapNamedPut(UpsertCampusDetails, "{campusId:guid}/details")
            .RequireModulePermission(ModuleCode.Campus, PermissionAction.Edit);
        groupBuilder.MapNamedPost(AddCampusPhone, "{campusId:guid}/phones")
            .RequireModulePermission(ModuleCode.Campus, PermissionAction.Edit);
    }

    private static async Task<IResult> GetCampusDetails(
        Guid campusId,
        OrganizationDbContext db,
        CancellationToken cancellationToken)
    {
        var details = await db.CampusDetails.AsNoTracking()
            .FirstOrDefaultAsync(d => d.CampusId == campusId, cancellationToken);
        var phones = await db.CampusPhones.AsNoTracking()
            .Where(p => p.CampusId == campusId)
            .Select(p => new { p.Id, p.Label, p.PhoneNumber, p.IsPrimary })
            .ToListAsync(cancellationToken);

        if (details is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            details.Id,
            details.CampusId,
            details.AddressLine1,
            details.City,
            details.PostalCode,
            details.Email,
            details.WebsiteUrl,
            details.Latitude,
            details.Longitude,
            details.TimezoneId,
            Phones = phones
        });
    }

    private static async Task<IResult> UpsertCampusDetails(
        Guid campusId,
        UpsertCampusDetailsBody request,
        OrganizationDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!await db.Campuses.AnyAsync(c => c.Id == campusId, cancellationToken))
        {
            return Results.NotFound();
        }

        var existing = await db.CampusDetails.FirstOrDefaultAsync(d => d.CampusId == campusId, cancellationToken);
        if (existing is null)
        {
            existing = CampusDetails.Create(
                tenantContext.TenantId.Value,
                campusId,
                request.AddressLine1,
                request.City,
                request.PostalCode,
                request.Email,
                request.WebsiteUrl,
                request.Latitude,
                request.Longitude,
                request.TimezoneId);
            db.CampusDetails.Add(existing);
        }
        else
        {
            existing.Update(
                request.AddressLine1,
                request.City,
                request.PostalCode,
                request.Email,
                request.WebsiteUrl,
                request.Latitude,
                request.Longitude,
                request.TimezoneId);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { existing.Id, existing.CampusId, existing.City, existing.PostalCode, existing.TimezoneId });
    }

    private static async Task<IResult> AddCampusPhone(
        Guid campusId,
        AddCampusPhoneBody request,
        OrganizationDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!await db.Campuses.AnyAsync(c => c.Id == campusId, cancellationToken))
        {
            return Results.NotFound();
        }

        var phone = CampusPhone.Create(tenantContext.TenantId.Value, campusId, request.Label, request.PhoneNumber, request.IsPrimary);
        db.CampusPhones.Add(phone);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/organization/campuses/{campusId}/phones/{phone.Id}", new { phone.Id, phone.Label, phone.PhoneNumber, phone.IsPrimary });
    }
}
