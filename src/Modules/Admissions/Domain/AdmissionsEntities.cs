using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Admissions.Domain;

public enum AdmissionEnquiryStatus
{
    New = 1,
    Contacted = 2,
    VisitScheduled = 3,
    Applied = 4,
    Admitted = 5,
    Closed = 6
}

public enum FollowUpChannel
{
    Phone = 1,
    Email = 2,
    InPerson = 3,
    WhatsApp = 4
}

public enum StudentStatus
{
    Active = 1,
    Inactive = 2,
    Graduated = 3,
    Withdrawn = 4
}

/// <summary>
/// Prospective student enquiry scoped to a tenant and campus (<c>admissions</c> schema).
/// </summary>
public sealed class AdmissionEnquiry : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private readonly List<EnquiryFollowUp> _followUps = [];
    private readonly List<CampusVisit> _visits = [];

    private AdmissionEnquiry()
    {
        ApplicantName = string.Empty;
        GuardianName = string.Empty;
        GuardianPhone = string.Empty;
        TargetGrade = string.Empty;
        AcademicYear = string.Empty;
    }

    private AdmissionEnquiry(
        Guid tenantId,
        Guid campusId,
        string applicantName,
        string guardianName,
        string guardianPhone,
        string? guardianEmail,
        string targetGrade,
        string academicYear,
        string? notes) : this()
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        if (campusId == Guid.Empty)
        {
            throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(applicantName);
        ArgumentException.ThrowIfNullOrWhiteSpace(guardianName);
        ArgumentException.ThrowIfNullOrWhiteSpace(guardianPhone);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetGrade);
        ArgumentException.ThrowIfNullOrWhiteSpace(academicYear);

        var phone = guardianPhone.Trim();
        if (!ContactRules.IsValidPhone(phone))
        {
            throw new ArgumentException("Guardian phone number must contain 7 to 15 digits.", nameof(guardianPhone));
        }

        var email = string.IsNullOrWhiteSpace(guardianEmail) ? null : guardianEmail.Trim().ToLowerInvariant();
        if (email is not null && !ContactRules.IsValidEmail(email))
        {
            throw new ArgumentException("Invalid guardian email address.", nameof(guardianEmail));
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        CampusId = campusId;
        ApplicantName = applicantName.Trim();
        GuardianName = guardianName.Trim();
        GuardianPhone = phone;
        GuardianEmail = email;
        TargetGrade = targetGrade.Trim();
        AcademicYear = academicYear.Trim();
        Status = AdmissionEnquiryStatus.New;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public string ApplicantName { get; private set; }
    public string GuardianName { get; private set; }
    public string GuardianPhone { get; private set; }
    public string? GuardianEmail { get; private set; }
    public string TargetGrade { get; private set; }
    public string AcademicYear { get; private set; }
    public AdmissionEnquiryStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid? ConvertedStudentProfileId { get; private set; }

    public IReadOnlyCollection<EnquiryFollowUp> FollowUps => _followUps.AsReadOnly();
    public IReadOnlyCollection<CampusVisit> Visits => _visits.AsReadOnly();

    public static AdmissionEnquiry Create(
        Guid tenantId,
        Guid campusId,
        string applicantName,
        string guardianName,
        string guardianPhone,
        string? guardianEmail,
        string targetGrade,
        string academicYear,
        string? notes = null)
        => new(tenantId, campusId, applicantName, guardianName, guardianPhone, guardianEmail, targetGrade, academicYear, notes);

    public EnquiryFollowUp AddFollowUp(DateTimeOffset followUpAtUtc, FollowUpChannel channel, string notes)
    {
        if (Status == AdmissionEnquiryStatus.Admitted)
        {
            throw new InvalidOperationException("Cannot add follow-ups to an already admitted enquiry.");
        }

        var followUp = EnquiryFollowUp.Create(TenantId, CampusId, Id, followUpAtUtc, channel, notes);
        _followUps.Add(followUp);
        if (Status == AdmissionEnquiryStatus.New)
        {
            Status = AdmissionEnquiryStatus.Contacted;
        }

        return followUp;
    }

    public CampusVisit ScheduleVisit(DateTimeOffset scheduledAtUtc, string visitorName, string? notes = null)
    {
        if (Status == AdmissionEnquiryStatus.Admitted)
        {
            throw new InvalidOperationException("Cannot schedule a campus visit for an already admitted enquiry.");
        }

        var visit = CampusVisit.Create(TenantId, CampusId, Id, scheduledAtUtc, visitorName, notes);
        _visits.Add(visit);
        Status = AdmissionEnquiryStatus.VisitScheduled;
        return visit;
    }

    public StudentProfile ConvertToStudent(
        string admissionNumber,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        DateOnly admissionDate)
    {
        if (Status == AdmissionEnquiryStatus.Admitted || ConvertedStudentProfileId.HasValue)
        {
            throw new InvalidOperationException("Enquiry has already been converted to an admitted student.");
        }

        if (Status == AdmissionEnquiryStatus.Closed)
        {
            throw new InvalidOperationException("Closed enquiries cannot be converted to students.");
        }

        var student = StudentProfile.Create(
            TenantId,
            CampusId,
            admissionNumber,
            firstName,
            lastName,
            dateOfBirth,
            admissionDate,
            GuardianName,
            GuardianPhone,
            GuardianEmail,
            TargetGrade,
            Id);

        Status = AdmissionEnquiryStatus.Admitted;
        ConvertedStudentProfileId = student.Id;
        return student;
    }

    /// <summary>
    /// Marks the enquiry as admitted and links it to the canonical Student record owned by the Students module.
    /// Idempotent when called with the same canonical <paramref name="canonicalStudentId"/>.
    /// </summary>
    public void MarkAdmitted(Guid canonicalStudentId)
    {
        if (canonicalStudentId == Guid.Empty)
        {
            throw new ArgumentException("Student ID cannot be empty.", nameof(canonicalStudentId));
        }

        if (Status == AdmissionEnquiryStatus.Closed)
        {
            throw new InvalidOperationException("Closed enquiries cannot be converted to students.");
        }

        if (ConvertedStudentProfileId.HasValue && ConvertedStudentProfileId.Value != canonicalStudentId)
        {
            throw new InvalidOperationException("Enquiry has already been converted to another student record.");
        }

        Status = AdmissionEnquiryStatus.Admitted;
        ConvertedStudentProfileId = canonicalStudentId;
    }

    public void Close()
    {
        if (Status == AdmissionEnquiryStatus.Admitted)
        {
            throw new InvalidOperationException("Admitted enquiries cannot be closed.");
        }

        Status = AdmissionEnquiryStatus.Closed;
    }
}

public sealed class EnquiryFollowUp : Entity<Guid>, ICampusScopedEntity
{
    private EnquiryFollowUp()
    {
        Notes = string.Empty;
    }

    private EnquiryFollowUp(
        Guid tenantId,
        Guid campusId,
        Guid enquiryId,
        DateTimeOffset followUpAtUtc,
        FollowUpChannel channel,
        string notes) : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notes);
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        CampusId = campusId;
        EnquiryId = enquiryId;
        FollowUpAtUtc = followUpAtUtc;
        Channel = channel;
        Notes = notes.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid EnquiryId { get; private set; }
    public DateTimeOffset FollowUpAtUtc { get; private set; }
    public FollowUpChannel Channel { get; private set; }
    public string Notes { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static EnquiryFollowUp Create(
        Guid tenantId,
        Guid campusId,
        Guid enquiryId,
        DateTimeOffset followUpAtUtc,
        FollowUpChannel channel,
        string notes)
        => new(tenantId, campusId, enquiryId, followUpAtUtc, channel, notes);

    public void Complete(DateTimeOffset completedAtUtc) => CompletedAtUtc ??= completedAtUtc;
}

public sealed class CampusVisit : Entity<Guid>, ICampusScopedEntity
{
    private CampusVisit()
    {
        VisitorName = string.Empty;
    }

    private CampusVisit(
        Guid tenantId,
        Guid campusId,
        Guid enquiryId,
        DateTimeOffset scheduledAtUtc,
        string visitorName,
        string? notes) : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(visitorName);
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        CampusId = campusId;
        EnquiryId = enquiryId;
        ScheduledAtUtc = scheduledAtUtc;
        VisitorName = visitorName.Trim();
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid EnquiryId { get; private set; }
    public DateTimeOffset ScheduledAtUtc { get; private set; }
    public string VisitorName { get; private set; }
    public bool Completed { get; private set; }
    public string? Notes { get; private set; }

    public static CampusVisit Create(
        Guid tenantId,
        Guid campusId,
        Guid enquiryId,
        DateTimeOffset scheduledAtUtc,
        string visitorName,
        string? notes)
        => new(tenantId, campusId, enquiryId, scheduledAtUtc, visitorName, notes);

    public void MarkCompleted(string? notes = null)
    {
        Completed = true;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes.Trim();
        }
    }
}

/// <summary>
/// Admitted student record owned by a tenant campus.
/// Enforces student age bounds (3 to 30 years on admission date).
/// </summary>
public sealed class StudentProfile : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    public const int MinAgeYears = 3;
    public const int MaxAgeYears = 30;

    private StudentProfile()
    {
        AdmissionNumber = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        GuardianName = string.Empty;
        GuardianPhone = string.Empty;
        TargetGrade = string.Empty;
    }

    private StudentProfile(
        Guid tenantId,
        Guid campusId,
        string admissionNumber,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        DateOnly admissionDate,
        string guardianName,
        string guardianPhone,
        string? guardianEmail,
        string targetGrade,
        Guid? sourceEnquiryId) : this()
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        if (campusId == Guid.Empty)
        {
            throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(admissionNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(guardianName);
        ArgumentException.ThrowIfNullOrWhiteSpace(guardianPhone);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetGrade);

        var phone = guardianPhone.Trim();
        if (!ContactRules.IsValidPhone(phone))
        {
            throw new ArgumentException("Guardian phone number must contain 7 to 15 digits.", nameof(guardianPhone));
        }

        var email = string.IsNullOrWhiteSpace(guardianEmail) ? null : guardianEmail.Trim().ToLowerInvariant();
        if (email is not null && !ContactRules.IsValidEmail(email))
        {
            throw new ArgumentException("Invalid guardian email address.", nameof(guardianEmail));
        }

        var age = CalculateCompletedYears(dateOfBirth, admissionDate);
        if (age < MinAgeYears || age > MaxAgeYears)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dateOfBirth),
                $"Student age at admission must be between {MinAgeYears} and {MaxAgeYears} years.");
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        CampusId = campusId;
        AdmissionNumber = admissionNumber.Trim().ToUpperInvariant();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        GuardianName = guardianName.Trim();
        GuardianPhone = phone;
        GuardianEmail = email;
        TargetGrade = targetGrade.Trim();
        Status = StudentStatus.Active;
        SourceEnquiryId = sourceEnquiryId;
    }

    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public string AdmissionNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string GuardianName { get; private set; }
    public string GuardianPhone { get; private set; }
    public string? GuardianEmail { get; private set; }
    public string TargetGrade { get; private set; }
    public StudentStatus Status { get; private set; }
    public Guid? SourceEnquiryId { get; private set; }

    public static StudentProfile Create(
        Guid tenantId,
        Guid campusId,
        string admissionNumber,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        DateOnly admissionDate,
        string guardianName,
        string guardianPhone,
        string? guardianEmail,
        string targetGrade,
        Guid? sourceEnquiryId = null)
        => new(
            tenantId,
            campusId,
            admissionNumber,
            firstName,
            lastName,
            dateOfBirth,
            admissionDate,
            guardianName,
            guardianPhone,
            guardianEmail,
            targetGrade,
            sourceEnquiryId);

    private static int CalculateCompletedYears(DateOnly dob, DateOnly asOf)
    {
        var age = asOf.Year - dob.Year;
        if (dob > asOf.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
