namespace SchoolOS.Modules.Organization.Contracts;

public sealed record CampusLookup(Guid Id, Guid TenantId, Guid SchoolId, string Name, string Code, bool IsActive);
public sealed record AcademicYearLookup(Guid Id, Guid TenantId, Guid SchoolId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent, bool IsActive);
public sealed record GradeLevelLookup(Guid Id, Guid TenantId, Guid SchoolId, string Code, string Name, int SequenceOrder, bool IsActive);
public sealed record SectionLookup(Guid Id, Guid TenantId, Guid SchoolId, Guid CampusId, Guid AcademicYearId, Guid GradeLevelId, string Code, string Name, int? Capacity, bool IsActive);

public sealed record PlacementValidationResult(
    bool IsValid,
    string? ErrorCode,
    string? ErrorMessage,
    CampusLookup? Campus = null,
    AcademicYearLookup? AcademicYear = null,
    GradeLevelLookup? GradeLevel = null,
    SectionLookup? Section = null);

/// <summary>
/// Published cross-module contract allowing Students and Admissions to validate Organization-owned
/// campuses, academic years, grade levels, and sections without accessing <c>OrganizationDbContext</c> directly.
/// </summary>
public interface IOrganizationAcademicReader
{
    Task<CampusLookup?> GetCampusAsync(Guid tenantId, Guid campusId, CancellationToken cancellationToken = default);
    Task<AcademicYearLookup?> GetAcademicYearAsync(Guid tenantId, Guid academicYearId, CancellationToken cancellationToken = default);
    Task<GradeLevelLookup?> GetGradeLevelAsync(Guid tenantId, Guid gradeLevelId, CancellationToken cancellationToken = default);
    Task<SectionLookup?> GetSectionAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default);

    Task<PlacementValidationResult> ValidatePlacementAsync(
        Guid tenantId,
        Guid campusId,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid? sectionId,
        CancellationToken cancellationToken = default);
}
