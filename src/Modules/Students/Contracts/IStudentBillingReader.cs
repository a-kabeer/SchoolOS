namespace SchoolOS.Modules.Students.Contracts;

public sealed record BillableStudentEnrollmentItem(
    Guid StudentId,
    Guid EnrollmentId,
    Guid CampusId,
    Guid AcademicYearId,
    Guid GradeLevelId,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    Guid? HouseholdId,
    int ActiveHouseholdSiblingCount);

/// <summary>
/// Published cross-module read contract allowing the Fees module to query active enrolled students
/// and household sibling counts without accessing <c>StudentsDbContext</c> directly.
/// </summary>
public interface IStudentBillingReader
{
    Task<IReadOnlyList<BillableStudentEnrollmentItem>> GetBillableStudentsAsync(
        Guid tenantId,
        Guid campusId,
        Guid academicYearId,
        Guid gradeLevelId,
        CancellationToken cancellationToken = default);
}
