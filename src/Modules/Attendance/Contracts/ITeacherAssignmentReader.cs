namespace SchoolOS.Modules.Attendance.Contracts;

/// <summary>
/// Published cross-module read contract allowing Exams and other academic modules
/// to verify teacher-to-section assignments without accessing <c>AttendanceDbContext</c> directly.
/// </summary>
public interface ITeacherAssignmentReader
{
    Task<bool> IsTeacherAssignedToSectionAsync(
        Guid tenantId,
        Guid userId,
        Guid academicYearId,
        Guid sectionId,
        CancellationToken cancellationToken = default);
}
