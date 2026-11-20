namespace SchoolOS.Modules.Students.Contracts;

public sealed record AdmitStudentFromEnquiryCommand(
    Guid TenantId,
    Guid CampusId,
    Guid SourceEnquiryId,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    DateOnly AdmissionDate,
    string GuardianName,
    string GuardianPhone,
    string? GuardianEmail,
    string TargetGrade);

public sealed record AdmittedStudentResult(
    Guid StudentId,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    bool WasExisting);

public sealed record AdmittedStudentSummary(
    Guid Id,
    Guid CampusId,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    string Status,
    Guid? SourceEnquiryId);

/// <summary>
/// Published contract implemented by the Students module and consumed by Admissions
/// so admitted enquiries create canonical Students records idempotently without duplicating entities.
/// </summary>
public interface IStudentAdmissionBridge
{
    Task<AdmittedStudentResult> AdmitFromEnquiryAsync(
        AdmitStudentFromEnquiryCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdmittedStudentSummary>> ListAdmittedStudentsAsync(
        Guid? campusId,
        CancellationToken cancellationToken = default);
}
