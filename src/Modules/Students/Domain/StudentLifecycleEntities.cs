using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Students.Domain;

/// <summary>
/// Canonical student lifecycle status (distinct from <see cref="EnrollmentStatus"/>).
/// </summary>
public enum StudentLifecycleStatus
{
    Active = 1,
    Inactive = 2,
    Withdrawn = 3,
    Graduated = 4
}

/// <summary>
/// Canonical academic placement / enrollment status (distinct from <see cref="StudentLifecycleStatus"/>).
/// </summary>
public enum EnrollmentStatus
{
    Active = 1,
    Completed = 2,
    Transferred = 3,
    Withdrawn = 4
}

/// <summary>
/// Controlled guardian-to-student relationship categories (DOC-55).
/// </summary>
public enum GuardianRelationshipType
{
    Father = 1,
    Mother = 2,
    LegalGuardian = 3,
    EmergencyContact = 4,
    AuthorizedPickup = 5,
    Payer = 6,
    Other = 7
}

/// <summary>
/// Canonical student aggregate owned by the Students module (<c>students.students</c>).
/// Enforces age bounds (3 to 30 years), unique admission numbers per school, and controlled status transitions.
/// </summary>
public sealed class Student : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    public const int MinAgeYears = 3;
    public const int MaxAgeYears = 30;

    private readonly List<StudentStatusHistory> _statusHistory = [];

    private Student()
    {
        AdmissionNumber = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    private Student(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        string admissionNumber,
        string firstName,
        string? middleName,
        string lastName,
        DateOnly dateOfBirth,
        string? gender,
        DateOnly admissionDate,
        Guid? sourceEnquiryId,
        string? notes) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        ArgumentException.ThrowIfNullOrWhiteSpace(admissionNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var age = CalculateCompletedYears(dateOfBirth, admissionDate);
        if (age < MinAgeYears || age > MaxAgeYears)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dateOfBirth),
                $"Student age at admission must be between {MinAgeYears} and {MaxAgeYears} years.");
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        AdmissionNumber = admissionNumber.Trim().ToUpperInvariant();
        FirstName = firstName.Trim();
        MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = string.IsNullOrWhiteSpace(gender) ? null : gender.Trim();
        AdmissionDate = admissionDate;
        Status = StudentLifecycleStatus.Active;
        SourceEnquiryId = sourceEnquiryId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public string AdmissionNumber { get; private set; }
    public string FirstName { get; private set; }
    public string? MiddleName { get; private set; }
    public string LastName { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string? Gender { get; private set; }
    public DateOnly AdmissionDate { get; private set; }
    public StudentLifecycleStatus Status { get; private set; }
    public Guid? SourceEnquiryId { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<StudentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";

    public static Student Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        string admissionNumber,
        string firstName,
        string? middleName,
        string lastName,
        DateOnly dateOfBirth,
        string? gender,
        DateOnly admissionDate,
        Guid? sourceEnquiryId = null,
        string? notes = null)
        => new(tenantId, schoolId, campusId, admissionNumber, firstName, middleName, lastName, dateOfBirth, gender, admissionDate, sourceEnquiryId, notes);

    public void UpdateProfile(
        string firstName,
        string? middleName,
        string lastName,
        DateOnly dateOfBirth,
        string? gender,
        string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var age = CalculateCompletedYears(dateOfBirth, AdmissionDate);
        if (age < MinAgeYears || age > MaxAgeYears)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dateOfBirth),
                $"Student age at admission must be between {MinAgeYears} and {MaxAgeYears} years.");
        }

        FirstName = firstName.Trim();
        MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = string.IsNullOrWhiteSpace(gender) ? null : gender.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public StudentStatusHistory ChangeStatus(
        StudentLifecycleStatus targetStatus,
        DateOnly effectiveDate,
        string reason,
        string actorUserId,
        DateTimeOffset changedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorUserId);

        if (Status == targetStatus)
        {
            throw new InvalidOperationException($"Student is already in status '{targetStatus}'.");
        }

        if (Status == StudentLifecycleStatus.Graduated)
        {
            throw new InvalidOperationException("Graduated student status is terminal and cannot transition to another status.");
        }

        var entry = StudentStatusHistory.Create(
            TenantId,
            Id,
            Status,
            targetStatus,
            effectiveDate,
            reason,
            actorUserId,
            changedAtUtc);

        Status = targetStatus;
        _statusHistory.Add(entry);
        return entry;
    }

    public void AssignCampus(Guid newCampusId)
    {
        if (newCampusId == Guid.Empty)
        {
            throw new ArgumentException("Campus ID cannot be empty.", nameof(newCampusId));
        }

        CampusId = newCampusId;
    }

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

/// <summary>
/// Append-only history of student status transitions (<c>students.student_status_history</c>).
/// </summary>
public sealed class StudentStatusHistory : Entity<Guid>, ITenantScopedEntity
{
    private StudentStatusHistory()
    {
        Reason = string.Empty;
        ChangedBy = string.Empty;
    }

    private StudentStatusHistory(
        Guid tenantId,
        Guid studentId,
        StudentLifecycleStatus fromStatus,
        StudentLifecycleStatus toStatus,
        DateOnly effectiveDate,
        string reason,
        string changedBy,
        DateTimeOffset changedAtUtc) : this()
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        StudentId = studentId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        EffectiveDate = effectiveDate;
        Reason = reason.Trim();
        ChangedBy = changedBy.Trim();
        ChangedAtUtc = changedAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public StudentLifecycleStatus FromStatus { get; private set; }
    public StudentLifecycleStatus ToStatus { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public string Reason { get; private set; }
    public string ChangedBy { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }

    public static StudentStatusHistory Create(
        Guid tenantId,
        Guid studentId,
        StudentLifecycleStatus fromStatus,
        StudentLifecycleStatus toStatus,
        DateOnly effectiveDate,
        string reason,
        string changedBy,
        DateTimeOffset changedAtUtc)
        => new(tenantId, studentId, fromStatus, toStatus, effectiveDate, reason, changedBy, changedAtUtc);
}

/// <summary>
/// Guardian natural-person record (<c>students.guardians</c>, DOC-55).
/// One guardian may be linked to multiple students across a household.
/// </summary>
public sealed class Guardian : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    private Guardian()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        PrimaryPhone = string.Empty;
        PreferredChannel = "Phone";
    }

    private Guardian(
        Guid tenantId,
        string firstName,
        string lastName,
        string primaryPhone,
        string? secondaryPhone,
        string? email,
        string? occupation,
        string? preferredChannel) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryPhone);

        var p1 = primaryPhone.Trim();
        if (!ContactRules.IsValidPhone(p1))
        {
            throw new ArgumentException("Primary phone number must contain 7 to 15 digits.", nameof(primaryPhone));
        }

        var p2 = string.IsNullOrWhiteSpace(secondaryPhone) ? null : secondaryPhone.Trim();
        if (p2 is not null && !ContactRules.IsValidPhone(p2))
        {
            throw new ArgumentException("Secondary phone number must contain 7 to 15 digits.", nameof(secondaryPhone));
        }

        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        if (normalizedEmail is not null && !ContactRules.IsValidEmail(normalizedEmail))
        {
            throw new ArgumentException("Invalid guardian email address.", nameof(email));
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PrimaryPhone = p1;
        SecondaryPhone = p2;
        Email = normalizedEmail;
        Occupation = string.IsNullOrWhiteSpace(occupation) ? null : occupation.Trim();
        PreferredChannel = string.IsNullOrWhiteSpace(preferredChannel) ? "Phone" : preferredChannel.Trim();
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PrimaryPhone { get; private set; }
    public string? SecondaryPhone { get; private set; }
    public string? Email { get; private set; }
    public string? Occupation { get; private set; }
    public string PreferredChannel { get; private set; }
    public bool IsActive { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public static Guardian Create(
        Guid tenantId,
        string firstName,
        string lastName,
        string primaryPhone,
        string? secondaryPhone = null,
        string? email = null,
        string? occupation = null,
        string? preferredChannel = null)
        => new(tenantId, firstName, lastName, primaryPhone, secondaryPhone, email, occupation, preferredChannel);

    public void UpdateContact(
        string firstName,
        string lastName,
        string primaryPhone,
        string? secondaryPhone,
        string? email,
        string? occupation,
        string? preferredChannel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var p1 = primaryPhone.Trim();
        if (!ContactRules.IsValidPhone(p1))
        {
            throw new ArgumentException("Primary phone number must contain 7 to 15 digits.", nameof(primaryPhone));
        }

        var p2 = string.IsNullOrWhiteSpace(secondaryPhone) ? null : secondaryPhone.Trim();
        if (p2 is not null && !ContactRules.IsValidPhone(p2))
        {
            throw new ArgumentException("Secondary phone number must contain 7 to 15 digits.", nameof(secondaryPhone));
        }

        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        if (normalizedEmail is not null && !ContactRules.IsValidEmail(normalizedEmail))
        {
            throw new ArgumentException("Invalid guardian email address.", nameof(email));
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PrimaryPhone = p1;
        SecondaryPhone = p2;
        Email = normalizedEmail;
        Occupation = string.IsNullOrWhiteSpace(occupation) ? null : occupation.Trim();
        PreferredChannel = string.IsNullOrWhiteSpace(preferredChannel) ? PreferredChannel : preferredChannel.Trim();
    }
}

/// <summary>
/// Many-to-many link between a <see cref="Student"/> and a <see cref="Guardian"/> with explicit authority flags (DOC-55 / DOC-46 §6).
/// </summary>
public sealed class StudentGuardian : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    private StudentGuardian()
    {
    }

    private StudentGuardian(
        Guid tenantId,
        Guid studentId,
        Guid guardianId,
        GuardianRelationshipType relationshipType,
        bool isPrimaryContact,
        bool isEmergencyContact,
        bool hasLegalCustody,
        bool canCollectStudent,
        bool receivesAcademicNotices,
        bool receivesFinancialNotices)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (studentId == Guid.Empty) throw new ArgumentException("Student ID cannot be empty.", nameof(studentId));
        if (guardianId == Guid.Empty) throw new ArgumentException("Guardian ID cannot be empty.", nameof(guardianId));

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        StudentId = studentId;
        GuardianId = guardianId;
        RelationshipType = relationshipType;
        IsPrimaryContact = isPrimaryContact;
        IsEmergencyContact = isEmergencyContact;
        HasLegalCustody = hasLegalCustody;
        CanCollectStudent = canCollectStudent;
        ReceivesAcademicNotices = receivesAcademicNotices;
        ReceivesFinancialNotices = receivesFinancialNotices;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid GuardianId { get; private set; }
    public GuardianRelationshipType RelationshipType { get; private set; }
    public bool IsPrimaryContact { get; private set; }
    public bool IsEmergencyContact { get; private set; }
    public bool HasLegalCustody { get; private set; }
    public bool CanCollectStudent { get; private set; }
    public bool ReceivesAcademicNotices { get; private set; }
    public bool ReceivesFinancialNotices { get; private set; }
    public bool IsActive { get; private set; }

    public static StudentGuardian Create(
        Guid tenantId,
        Guid studentId,
        Guid guardianId,
        GuardianRelationshipType relationshipType,
        bool isPrimaryContact = false,
        bool isEmergencyContact = false,
        bool hasLegalCustody = false,
        bool canCollectStudent = false,
        bool receivesAcademicNotices = false,
        bool receivesFinancialNotices = false)
        => new(
            tenantId,
            studentId,
            guardianId,
            relationshipType,
            isPrimaryContact,
            isEmergencyContact,
            hasLegalCustody,
            canCollectStudent,
            receivesAcademicNotices,
            receivesFinancialNotices);

    public void SetPrimary(bool isPrimary) => IsPrimaryContact = isPrimary;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

/// <summary>
/// Shared family / household aggregate associating multiple siblings and guardians (<c>students.households</c>, DOC-55).
/// </summary>
public sealed class Household : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    private Household()
    {
        HouseholdCode = string.Empty;
        DisplayName = string.Empty;
    }

    private Household(Guid tenantId, string householdCode, string displayName, Guid? primaryGuardianId) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(householdCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        HouseholdCode = householdCode.Trim().ToUpperInvariant();
        DisplayName = displayName.Trim();
        PrimaryGuardianId = primaryGuardianId;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public string HouseholdCode { get; private set; }
    public string DisplayName { get; private set; }
    public Guid? PrimaryGuardianId { get; private set; }
    public bool IsActive { get; private set; }

    public static Household Create(Guid tenantId, string householdCode, string displayName, Guid? primaryGuardianId = null)
        => new(tenantId, householdCode, displayName, primaryGuardianId);
}

/// <summary>
/// Explicit membership linking either a <see cref="Student"/> or a <see cref="Guardian"/> to a <see cref="Household"/> (<c>students.household_members</c>).
/// </summary>
public sealed class HouseholdMember : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    private HouseholdMember()
    {
        RoleLabel = string.Empty;
    }

    private HouseholdMember(
        Guid tenantId,
        Guid householdId,
        Guid? studentId,
        Guid? guardianId,
        string roleLabel,
        bool isBillingMember) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (householdId == Guid.Empty) throw new ArgumentException("Household ID cannot be empty.", nameof(householdId));
        if ((studentId.HasValue && guardianId.HasValue) || (!studentId.HasValue && !guardianId.HasValue))
        {
            throw new ArgumentException("A household member must reference either a student OR a guardian, not both or neither.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(roleLabel);

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        HouseholdId = householdId;
        StudentId = studentId;
        GuardianId = guardianId;
        RoleLabel = roleLabel.Trim();
        IsBillingMember = isBillingMember;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid HouseholdId { get; private set; }
    public Guid? StudentId { get; private set; }
    public Guid? GuardianId { get; private set; }
    public string RoleLabel { get; private set; }
    public bool IsBillingMember { get; private set; }
    public bool IsActive { get; private set; }

    public static HouseholdMember ForStudent(Guid tenantId, Guid householdId, Guid studentId, string roleLabel = "Student")
        => new(tenantId, householdId, studentId, null, roleLabel, isBillingMember: false);

    public static HouseholdMember ForGuardian(Guid tenantId, Guid householdId, Guid guardianId, string roleLabel = "Guardian", bool isBillingMember = false)
        => new(tenantId, householdId, null, guardianId, roleLabel, isBillingMember);
}

/// <summary>
/// Historical academic placement record (<c>students.enrollments</c>, DOC-43 §6.7).
/// Transfers and promotions create new enrollment rows referencing <see cref="PreviousEnrollmentId"/> rather than overwriting history.
/// </summary>
public sealed class Enrollment : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private Enrollment()
    {
    }

    private Enrollment(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid studentId,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid? sectionId,
        string? rollNumber,
        DateOnly startDate,
        Guid? previousEnrollmentId,
        string? reason)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (studentId == Guid.Empty) throw new ArgumentException("Student ID cannot be empty.", nameof(studentId));
        if (academicYearId == Guid.Empty) throw new ArgumentException("Academic year ID cannot be empty.", nameof(academicYearId));
        if (gradeLevelId == Guid.Empty) throw new ArgumentException("Grade level ID cannot be empty.", nameof(gradeLevelId));

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        StudentId = studentId;
        AcademicYearId = academicYearId;
        GradeLevelId = gradeLevelId;
        SectionId = sectionId;
        RollNumber = string.IsNullOrWhiteSpace(rollNumber) ? null : rollNumber.Trim();
        StartDate = startDate;
        Status = EnrollmentStatus.Active;
        PreviousEnrollmentId = previousEnrollmentId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid AcademicYearId { get; private set; }
    public Guid GradeLevelId { get; private set; }
    public Guid? SectionId { get; private set; }
    public string? RollNumber { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public Guid? PreviousEnrollmentId { get; private set; }
    public string? Reason { get; private set; }

    public static Enrollment Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid studentId,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid? sectionId,
        string? rollNumber,
        DateOnly startDate,
        Guid? previousEnrollmentId = null,
        string? reason = null)
        => new(tenantId, schoolId, campusId, studentId, academicYearId, gradeLevelId, sectionId, rollNumber, startDate, previousEnrollmentId, reason);

    public void MarkTransferred(DateOnly endDate, string? reason = null)
    {
        if (Status != EnrollmentStatus.Active)
        {
            throw new InvalidOperationException("Only an active enrollment can be transferred.");
        }

        if (endDate < StartDate)
        {
            throw new ArgumentException("Transfer end date cannot precede enrollment start date.", nameof(endDate));
        }

        Status = EnrollmentStatus.Transferred;
        EndDate = endDate;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Reason = reason.Trim();
        }
    }

    public void MarkWithdrawn(DateOnly endDate, string? reason = null)
    {
        if (Status != EnrollmentStatus.Active)
        {
            throw new InvalidOperationException("Only an active enrollment can be withdrawn.");
        }

        Status = EnrollmentStatus.Withdrawn;
        EndDate = endDate;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Reason = reason.Trim();
        }
    }

    public void MarkCompleted(DateOnly endDate)
    {
        if (Status != EnrollmentStatus.Active)
        {
            throw new InvalidOperationException("Only an active enrollment can be completed.");
        }

        Status = EnrollmentStatus.Completed;
        EndDate = endDate;
    }
}
