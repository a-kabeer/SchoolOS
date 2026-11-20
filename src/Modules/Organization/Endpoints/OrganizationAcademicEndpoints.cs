using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Modules.Organization.Domain;
using SchoolOS.Modules.Organization.Persistence;

namespace SchoolOS.Modules.Organization.Endpoints;

public sealed record CreateAcademicYearBody(Guid SchoolId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent = false);
public sealed record CreateGradeLevelBody(Guid SchoolId, string Code, string Name, int SequenceOrder);
public sealed record CreateSectionBody(Guid CampusId, Guid AcademicYearId, Guid GradeLevelId, string Code, string Name, int? Capacity = null);

public sealed class OrganizationAcademicEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/organization/academics";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapNamedGet(ListAcademicYears, "years")
            .RequireAuthorization(SecurityPolicies.TenantScopedPolicy);
        groupBuilder.MapNamedPost(CreateAcademicYear, "years")
            .RequireAuthorization(SecurityPolicies.OrganizationAdminPolicy);

        groupBuilder.MapNamedGet(ListGradeLevels, "grades")
            .RequireAuthorization(SecurityPolicies.TenantScopedPolicy);
        groupBuilder.MapNamedPost(CreateGradeLevel, "grades")
            .RequireAuthorization(SecurityPolicies.OrganizationAdminPolicy);

        groupBuilder.MapNamedGet(ListSections, "sections")
            .RequireAuthorization(SecurityPolicies.TenantScopedPolicy);
        groupBuilder.MapNamedPost(CreateSection, "sections")
            .RequireAuthorization(SecurityPolicies.OrganizationAdminPolicy);
    }

    private static async Task<IResult> ListAcademicYears(
        OrganizationDbContext db,
        Guid? schoolId,
        CancellationToken ct)
    {
        var query = db.AcademicYears.AsNoTracking();
        if (schoolId.HasValue)
        {
            query = query.Where(y => y.SchoolId == schoolId.Value);
        }

        var years = await query
            .OrderByDescending(y => y.IsCurrent)
            .ThenByDescending(y => y.StartDate)
            .Select(y => new { y.Id, y.SchoolId, y.Code, y.Name, y.StartDate, y.EndDate, y.IsCurrent, y.IsActive })
            .ToListAsync(ct);

        return Results.Ok(years);
    }

    private static async Task<IResult> CreateAcademicYear(
        CreateAcademicYearBody request,
        OrganizationDbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        var schoolExists = await db.Schools.AnyAsync(s => s.Id == request.SchoolId, ct);
        if (!schoolExists)
        {
            return Results.NotFound(new { code = "SCHOOL_NOT_FOUND" });
        }

        if (request.IsCurrent)
        {
            var currentYears = await db.AcademicYears
                .Where(y => y.SchoolId == request.SchoolId && y.IsCurrent)
                .ToListAsync(ct);
            foreach (var existing in currentYears)
            {
                existing.SetCurrent(false);
            }
        }

        var year = AcademicYear.Create(
            tenantContext.TenantId.Value,
            request.SchoolId,
            request.Code,
            request.Name,
            request.StartDate,
            request.EndDate,
            request.IsCurrent);

        db.AcademicYears.Add(year);
        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/v1/organization/academics/years/{year.Id}",
            new { year.Id, year.SchoolId, year.Code, year.Name, year.StartDate, year.EndDate, year.IsCurrent, year.IsActive });
    }

    private static async Task<IResult> ListGradeLevels(
        OrganizationDbContext db,
        Guid? schoolId,
        CancellationToken ct)
    {
        var query = db.GradeLevels.AsNoTracking();
        if (schoolId.HasValue)
        {
            query = query.Where(g => g.SchoolId == schoolId.Value);
        }

        var grades = await query
            .OrderBy(g => g.SequenceOrder)
            .ThenBy(g => g.Name)
            .Select(g => new { g.Id, g.SchoolId, g.Code, g.Name, g.SequenceOrder, g.IsActive })
            .ToListAsync(ct);

        return Results.Ok(grades);
    }

    private static async Task<IResult> CreateGradeLevel(
        CreateGradeLevelBody request,
        OrganizationDbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        var schoolExists = await db.Schools.AnyAsync(s => s.Id == request.SchoolId, ct);
        if (!schoolExists)
        {
            return Results.NotFound(new { code = "SCHOOL_NOT_FOUND" });
        }

        var grade = GradeLevel.Create(
            tenantContext.TenantId.Value,
            request.SchoolId,
            request.Code,
            request.Name,
            request.SequenceOrder);

        db.GradeLevels.Add(grade);
        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/v1/organization/academics/grades/{grade.Id}",
            new { grade.Id, grade.SchoolId, grade.Code, grade.Name, grade.SequenceOrder, grade.IsActive });
    }

    private static async Task<IResult> ListSections(
        OrganizationDbContext db,
        ITenantContext tenantContext,
        Guid? campusId,
        Guid? academicYearId,
        Guid? gradeLevelId,
        CancellationToken ct)
    {
        var query = db.Sections.AsNoTracking();

        if (tenantContext.CampusId.HasValue)
        {
            query = query.Where(s => s.CampusId == tenantContext.CampusId.Value);
        }
        else if (campusId.HasValue)
        {
            query = query.Where(s => s.CampusId == campusId.Value);
        }

        if (academicYearId.HasValue)
        {
            query = query.Where(s => s.AcademicYearId == academicYearId.Value);
        }

        if (gradeLevelId.HasValue)
        {
            query = query.Where(s => s.GradeLevelId == gradeLevelId.Value);
        }

        var sections = await query
            .OrderBy(s => s.Code)
            .Select(s => new { s.Id, s.SchoolId, s.CampusId, s.AcademicYearId, s.GradeLevelId, s.Code, s.Name, s.Capacity, s.IsActive })
            .ToListAsync(ct);

        return Results.Ok(sections);
    }

    private static async Task<IResult> CreateSection(
        CreateSectionBody request,
        OrganizationDbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (tenantContext.CampusId.HasValue && tenantContext.CampusId.Value != request.CampusId)
        {
            return Results.Forbid();
        }

        var campus = await db.Campuses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CampusId, ct);
        if (campus is null)
        {
            return Results.NotFound(new { code = "CAMPUS_NOT_FOUND" });
        }

        var year = await db.AcademicYears.AsNoTracking().FirstOrDefaultAsync(y => y.Id == request.AcademicYearId && y.SchoolId == campus.SchoolId, ct);
        if (year is null)
        {
            return Results.BadRequest(new { code = "ACADEMIC_YEAR_INVALID" });
        }

        var grade = await db.GradeLevels.AsNoTracking().FirstOrDefaultAsync(g => g.Id == request.GradeLevelId && g.SchoolId == campus.SchoolId, ct);
        if (grade is null)
        {
            return Results.BadRequest(new { code = "GRADE_LEVEL_INVALID" });
        }

        var section = Section.Create(
            tenantContext.TenantId.Value,
            campus.SchoolId,
            campus.Id,
            year.Id,
            grade.Id,
            request.Code,
            request.Name,
            request.Capacity);

        db.Sections.Add(section);
        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/v1/organization/academics/sections/{section.Id}",
            new { section.Id, section.SchoolId, section.CampusId, section.AcademicYearId, section.GradeLevelId, section.Code, section.Name, section.Capacity, section.IsActive });
    }
}
