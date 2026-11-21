namespace SchoolOS.Modules.Students.Contracts;

public sealed record EnrolledStudentRosterItem(
    Guid StudentId,
    Guid EnrollmentId,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    string? RollNumber);

/// <summary>
/// Published cross-module read contract allowing Attendance and other downstream modules
/// to query active enrolled student rosters without accessing <c>StudentsDbContext</c> directly.
/// </summary>
public interface IStudentRosterReader
{
    Task<IReadOnlyList<EnrolledStudentRosterItem>> GetActiveSectionRosterAsync(
        Guid tenantId,
        Guid campusId,
        Guid academicYearId,
        Guid sectionId,
        CancellationToken cancellationToken = default);
}
