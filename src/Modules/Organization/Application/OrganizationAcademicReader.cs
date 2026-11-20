using Microsoft.EntityFrameworkCore;
using SchoolOS.Modules.Organization.Contracts;
using SchoolOS.Modules.Organization.Persistence;

namespace SchoolOS.Modules.Organization.Application;

public sealed class OrganizationAcademicReader : IOrganizationAcademicReader
{
    private readonly OrganizationDbContext _db;

    public OrganizationAcademicReader(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<CampusLookup?> GetCampusAsync(Guid tenantId, Guid campusId, CancellationToken cancellationToken = default)
    {
        return await _db.Campuses.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Id == campusId)
            .Select(c => new CampusLookup(c.Id, c.TenantId, c.SchoolId, c.Name, c.Code, c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AcademicYearLookup?> GetAcademicYearAsync(Guid tenantId, Guid academicYearId, CancellationToken cancellationToken = default)
    {
        return await _db.AcademicYears.AsNoTracking()
            .Where(y => y.TenantId == tenantId && y.Id == academicYearId)
            .Select(y => new AcademicYearLookup(y.Id, y.TenantId, y.SchoolId, y.Code, y.Name, y.StartDate, y.EndDate, y.IsCurrent, y.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<GradeLevelLookup?> GetGradeLevelAsync(Guid tenantId, Guid gradeLevelId, CancellationToken cancellationToken = default)
    {
        return await _db.GradeLevels.AsNoTracking()
            .Where(g => g.TenantId == tenantId && g.Id == gradeLevelId)
            .Select(g => new GradeLevelLookup(g.Id, g.TenantId, g.SchoolId, g.Code, g.Name, g.SequenceOrder, g.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SectionLookup?> GetSectionAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default)
    {
        return await _db.Sections.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Id == sectionId)
            .Select(s => new SectionLookup(s.Id, s.TenantId, s.SchoolId, s.CampusId, s.AcademicYearId, s.GradeLevelId, s.Code, s.Name, s.Capacity, s.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PlacementValidationResult> ValidatePlacementAsync(
        Guid tenantId,
        Guid campusId,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid? sectionId,
        CancellationToken cancellationToken = default)
    {
        var campus = await GetCampusAsync(tenantId, campusId, cancellationToken);
        if (campus is null)
        {
            return new PlacementValidationResult(false, "CAMPUS_NOT_FOUND", "Campus does not exist in the authenticated tenant.");
        }

        if (!campus.IsActive)
        {
            return new PlacementValidationResult(false, "CAMPUS_INACTIVE", "Cannot enroll or place a student into an inactive campus.");
        }

        var academicYear = await GetAcademicYearAsync(tenantId, academicYearId, cancellationToken);
        if (academicYear is null || academicYear.SchoolId != campus.SchoolId)
        {
            return new PlacementValidationResult(false, "ACADEMIC_YEAR_INVALID", "Academic year does not belong to the campus's school.");
        }

        if (!academicYear.IsActive)
        {
            return new PlacementValidationResult(false, "ACADEMIC_YEAR_INACTIVE", "Cannot enroll a student into an inactive academic year.");
        }

        var gradeLevel = await GetGradeLevelAsync(tenantId, gradeLevelId, cancellationToken);
        if (gradeLevel is null || gradeLevel.SchoolId != campus.SchoolId)
        {
            return new PlacementValidationResult(false, "GRADE_LEVEL_INVALID", "Grade level does not belong to the campus's school.");
        }

        if (!gradeLevel.IsActive)
        {
            return new PlacementValidationResult(false, "GRADE_LEVEL_INACTIVE", "Cannot enroll a student into an inactive grade level.");
        }

        SectionLookup? section = null;
        if (sectionId.HasValue)
        {
            section = await GetSectionAsync(tenantId, sectionId.Value, cancellationToken);
            if (section is null
                || section.CampusId != campusId
                || section.AcademicYearId != academicYearId
                || section.GradeLevelId != gradeLevelId)
            {
                return new PlacementValidationResult(false, "SECTION_INVALID", "Section does not match the selected campus, academic year, and grade level.");
            }

            if (!section.IsActive)
            {
                return new PlacementValidationResult(false, "SECTION_INACTIVE", "Cannot enroll a student into an inactive section.");
            }
        }

        return new PlacementValidationResult(true, null, null, campus, academicYear, gradeLevel, section);
    }
}
