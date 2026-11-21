using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Attendance.Domain;

public enum StaffRoleType
{
    Teacher = 1,
    HomeroomTeacher = 2,
    Coordinator = 3,
    CampusStaff = 4,
    Principal = 5
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    Excused = 4,
    HalfDay = 5
}

/// <summary>
/// Campus-scoped staff / teacher record owned by the Attendance &amp; Staff slice (<c>attendance.staff_members</c>).
/// Optionally links to an Identity <see cref="UserId"/> for teacher self-scoping on section registers.
/// </summary>
public sealed class StaffMember : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private StaffMember()
    {
        EmployeeCode = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    private StaffMember(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        string employeeCode,
        string firstName,
        string lastName,
        StaffRoleType roleType,
        Guid? userId,
        string? email,
        string? phone) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        EmployeeCode = employeeCode.Trim().ToUpperInvariant();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        RoleType = roleType;
        UserId = userId;
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid? UserId { get; private set; }
    public string EmployeeCode { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public StaffRoleType RoleType { get; private set; }
    public bool IsActive { get; private set; }

    public static StaffMember Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        string employeeCode,
        string firstName,
        string lastName,
        StaffRoleType roleType,
        Guid? userId = null,
        string? email = null,
        string? phone = null)
        => new(tenantId, schoolId, campusId, employeeCode, firstName, lastName, roleType, userId, email, phone);

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

/// <summary>
/// Assigns a <see cref="StaffMember"/> to an Organization <c>Section</c> for a specific <c>AcademicYear</c>.
/// Used to enforce teacher-scoped daily roll-call authorization.
/// </summary>
public sealed class TeacherSectionAssignment : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private TeacherSectionAssignment()
    {
    }

    private TeacherSectionAssignment(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid staffMemberId,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid sectionId,
        bool isHomeroomTeacher)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (staffMemberId == Guid.Empty) throw new ArgumentException("Staff member ID cannot be empty.", nameof(staffMemberId));
        if (academicYearId == Guid.Empty) throw new ArgumentException("Academic year ID cannot be empty.", nameof(academicYearId));
        if (gradeLevelId == Guid.Empty) throw new ArgumentException("Grade level ID cannot be empty.", nameof(gradeLevelId));
        if (sectionId == Guid.Empty) throw new ArgumentException("Section ID cannot be empty.", nameof(sectionId));

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        StaffMemberId = staffMemberId;
        AcademicYearId = academicYearId;
        GradeLevelId = gradeLevelId;
        SectionId = sectionId;
        IsHomeroomTeacher = isHomeroomTeacher;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid StaffMemberId { get; private set; }
    public Guid AcademicYearId { get; private set; }
    public Guid GradeLevelId { get; private set; }
    public Guid SectionId { get; private set; }
    public bool IsHomeroomTeacher { get; private set; }
    public bool IsActive { get; private set; }

    public static TeacherSectionAssignment Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid staffMemberId,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid sectionId,
        bool isHomeroomTeacher)
        => new(tenantId, schoolId, campusId, staffMemberId, academicYearId, gradeLevelId, sectionId, isHomeroomTeacher);

    public void UpdateHomeroom(bool isHomeroomTeacher) => IsHomeroomTeacher = isHomeroomTeacher;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

/// <summary>
/// Aggregate root for a section's daily attendance register (<c>attendance.daily_attendance_registers</c>).
/// Unique per <c>(tenant_id, campus_id, academic_year_id, section_id, attendance_date)</c>.
/// </summary>
public sealed class DailyAttendanceRegister : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private readonly List<AttendanceEntry> _entries = [];

    private DailyAttendanceRegister()
    {
        MarkedBy = string.Empty;
    }

    private DailyAttendanceRegister(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid sectionId,
        DateOnly attendanceDate,
        string markedBy,
        DateTimeOffset markedAtUtc,
        string? notes) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (academicYearId == Guid.Empty) throw new ArgumentException("Academic year ID cannot be empty.", nameof(academicYearId));
        if (gradeLevelId == Guid.Empty) throw new ArgumentException("Grade level ID cannot be empty.", nameof(gradeLevelId));
        if (sectionId == Guid.Empty) throw new ArgumentException("Section ID cannot be empty.", nameof(sectionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(markedBy);

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        AcademicYearId = academicYearId;
        GradeLevelId = gradeLevelId;
        SectionId = sectionId;
        AttendanceDate = attendanceDate;
        MarkedBy = markedBy.Trim();
        MarkedAtUtc = markedAtUtc;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        IsLocked = false;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid AcademicYearId { get; private set; }
    public Guid GradeLevelId { get; private set; }
    public Guid SectionId { get; private set; }
    public DateOnly AttendanceDate { get; private set; }
    public string MarkedBy { get; private set; }
    public DateTimeOffset MarkedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public bool IsLocked { get; private set; }

    public IReadOnlyCollection<AttendanceEntry> Entries => _entries.AsReadOnly();

    public static DailyAttendanceRegister Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid sectionId,
        DateOnly attendanceDate,
        string markedBy,
        DateTimeOffset markedAtUtc,
        string? notes = null)
        => new(tenantId, schoolId, campusId, academicYearId, gradeLevelId, sectionId, attendanceDate, markedBy, markedAtUtc, notes);

    public void UpdateNotes(string? notes, string actor, DateTimeOffset utcNow)
    {
        if (IsLocked) throw new InvalidOperationException("Attendance register is locked and cannot be modified.");
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        MarkedBy = actor;
        MarkedAtUtc = utcNow;
    }

    public void Lock() => IsLocked = true;
}

/// <summary>
/// Individual student daily attendance mark within a <see cref="DailyAttendanceRegister"/> (<c>attendance.attendance_entries</c>).
/// </summary>
public sealed class AttendanceEntry : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private AttendanceEntry()
    {
        LastUpdatedBy = string.Empty;
    }

    private AttendanceEntry(
        Guid tenantId,
        Guid campusId,
        Guid registerId,
        Guid studentId,
        AttendanceStatus status,
        string? remarks,
        string updatedBy,
        DateTimeOffset updatedAtUtc) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (registerId == Guid.Empty) throw new ArgumentException("Register ID cannot be empty.", nameof(registerId));
        if (studentId == Guid.Empty) throw new ArgumentException("Student ID cannot be empty.", nameof(studentId));
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        CampusId = campusId;
        RegisterId = registerId;
        StudentId = studentId;
        Status = status;
        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
        LastUpdatedBy = updatedBy.Trim();
        LastUpdatedAtUtc = updatedAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid RegisterId { get; private set; }
    public Guid StudentId { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public string? Remarks { get; private set; }
    public string? LastCorrectionReason { get; private set; }
    public string LastUpdatedBy { get; private set; }
    public DateTimeOffset LastUpdatedAtUtc { get; private set; }

    public static AttendanceEntry Create(
        Guid tenantId,
        Guid campusId,
        Guid registerId,
        Guid studentId,
        AttendanceStatus status,
        string? remarks,
        string updatedBy,
        DateTimeOffset updatedAtUtc)
        => new(tenantId, campusId, registerId, studentId, status, remarks, updatedBy, updatedAtUtc);

    /// <summary>
    /// Updates an existing attendance mark. Changing <see cref="Status"/> requires a non-empty <paramref name="correctionReason"/>
    /// and emits an immutable <see cref="AttendanceCorrectionAudit"/> record.
    /// </summary>
    public AttendanceCorrectionAudit? Correct(
        DateOnly attendanceDate,
        AttendanceStatus newStatus,
        string? remarks,
        string? correctionReason,
        string actor,
        DateTimeOffset utcNow)
    {
        var normalizedRemarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();

        if (Status == newStatus)
        {
            if (!string.Equals(Remarks, normalizedRemarks, StringComparison.Ordinal))
            {
                Remarks = normalizedRemarks;
                LastUpdatedBy = actor;
                LastUpdatedAtUtc = utcNow;
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(correctionReason))
        {
            throw new ArgumentException(
                "Changing an already recorded attendance status requires a correction reason.",
                nameof(correctionReason));
        }

        var previousStatus = Status;
        var trimmedReason = correctionReason.Trim();

        Status = newStatus;
        Remarks = normalizedRemarks;
        LastCorrectionReason = trimmedReason;
        LastUpdatedBy = actor;
        LastUpdatedAtUtc = utcNow;

        return AttendanceCorrectionAudit.Create(
            TenantId,
            CampusId,
            Id,
            RegisterId,
            StudentId,
            attendanceDate,
            previousStatus,
            newStatus,
            trimmedReason,
            actor,
            utcNow);
    }
}

/// <summary>
/// Append-only audit trail for post-marking attendance corrections (<c>attendance.attendance_correction_audits</c>).
/// </summary>
public sealed class AttendanceCorrectionAudit : Entity<Guid>, ICampusScopedEntity
{
    private AttendanceCorrectionAudit()
    {
        Reason = string.Empty;
        CorrectedBy = string.Empty;
    }

    private AttendanceCorrectionAudit(
        Guid tenantId,
        Guid campusId,
        Guid entryId,
        Guid registerId,
        Guid studentId,
        DateOnly attendanceDate,
        AttendanceStatus previousStatus,
        AttendanceStatus newStatus,
        string reason,
        string correctedBy,
        DateTimeOffset correctedAtUtc) : this()
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        CampusId = campusId;
        EntryId = entryId;
        RegisterId = registerId;
        StudentId = studentId;
        AttendanceDate = attendanceDate;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Reason = reason.Trim();
        CorrectedBy = correctedBy.Trim();
        CorrectedAtUtc = correctedAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid EntryId { get; private set; }
    public Guid RegisterId { get; private set; }
    public Guid StudentId { get; private set; }
    public DateOnly AttendanceDate { get; private set; }
    public AttendanceStatus PreviousStatus { get; private set; }
    public AttendanceStatus NewStatus { get; private set; }
    public string Reason { get; private set; }
    public string CorrectedBy { get; private set; }
    public DateTimeOffset CorrectedAtUtc { get; private set; }

    public static AttendanceCorrectionAudit Create(
        Guid tenantId,
        Guid campusId,
        Guid entryId,
        Guid registerId,
        Guid studentId,
        DateOnly attendanceDate,
        AttendanceStatus previousStatus,
        AttendanceStatus newStatus,
        string reason,
        string correctedBy,
        DateTimeOffset correctedAtUtc)
        => new(tenantId, campusId, entryId, registerId, studentId, attendanceDate, previousStatus, newStatus, reason, correctedBy, correctedAtUtc);
}
