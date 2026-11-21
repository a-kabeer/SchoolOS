using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Attendance.Domain;
using SchoolOS.Modules.Attendance.Persistence;
using SchoolOS.Modules.Organization.Contracts;
using SchoolOS.Modules.Students.Contracts;

namespace SchoolOS.Modules.Attendance.Application;

public sealed record CreateStaffMemberCommand(
    Guid CampusId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    StaffRoleType RoleType,
    Guid? UserId = null,
    string? Email = null,
    string? Phone = null);

public sealed record AssignTeacherSectionCommand(
    Guid AcademicYearId,
    Guid GradeLevelId,
    Guid SectionId,
    bool IsHomeroomTeacher = false);

public sealed record MarkAttendanceEntryItem(
    Guid StudentId,
    AttendanceStatus Status,
    string? Remarks = null,
    string? CorrectionReason = null);

public sealed record MarkSectionAttendanceCommand(
    DateOnly AttendanceDate,
    IReadOnlyList<MarkAttendanceEntryItem> Entries,
    string? Notes = null);

public sealed record RosterAttendanceEntryDto(
    Guid StudentId,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    string? RollNumber,
    AttendanceStatus? Status,
    string? Remarks,
    string? LastCorrectionReason,
    DateTimeOffset? LastUpdatedAtUtc);

public sealed record SectionRegisterViewDto(
    Guid? RegisterId,
    Guid CampusId,
    Guid AcademicYearId,
    Guid GradeLevelId,
    Guid SectionId,
    string SectionName,
    DateOnly AttendanceDate,
    bool IsMarked,
    bool IsLocked,
    string? Notes,
    IReadOnlyList<RosterAttendanceEntryDto> Students);

public sealed record MarkAttendanceResultDto(
    Guid RegisterId,
    DateOnly AttendanceDate,
    int CreatedCount,
    int UpdatedCount,
    int CorrectionsRecorded);

public sealed record AttendanceSummaryDto(
    int TotalMarks,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    int HalfDayCount,
    decimal AttendancePercentage);

public sealed class AttendanceService
{
    private readonly AttendanceDbContext _db;
    private readonly IOrganizationAcademicReader _organization;
    private readonly IStudentRosterReader _rosterReader;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public AttendanceService(
        AttendanceDbContext db,
        IOrganizationAcademicReader organization,
        IStudentRosterReader rosterReader,
        ITenantContext tenantContext,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _organization = organization;
        _rosterReader = rosterReader;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<StaffMember> CreateStaffMemberAsync(
        CreateStaffMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        EnsureCampusAuthorized(command.CampusId);

        var campus = await _organization.GetCampusAsync(tenantId, command.CampusId, cancellationToken)
            ?? throw new InvalidOperationException("Campus does not exist in this tenant.");

        var normalizedCode = command.EmployeeCode.Trim().ToUpperInvariant();
        var duplicateCode = await _db.StaffMembers.AnyAsync(
            s => s.SchoolId == campus.SchoolId && s.EmployeeCode == normalizedCode,
            cancellationToken);
        if (duplicateCode)
        {
            throw new InvalidOperationException($"Employee code '{normalizedCode}' already exists in this school.");
        }

        if (command.UserId.HasValue)
        {
            var duplicateUser = await _db.StaffMembers.AnyAsync(
                s => s.UserId == command.UserId.Value,
                cancellationToken);
            if (duplicateUser)
            {
                throw new InvalidOperationException("This user account is already linked to another staff profile.");
            }
        }

        var staff = StaffMember.Create(
            tenantId,
            campus.SchoolId,
            campus.Id,
            command.EmployeeCode,
            command.FirstName,
            command.LastName,
            command.RoleType,
            command.UserId,
            command.Email,
            command.Phone);

        _db.StaffMembers.Add(staff);
        await _db.SaveChangesAsync(cancellationToken);
        return staff;
    }

    public async Task<TeacherSectionAssignment> AssignTeacherToSectionAsync(
        Guid staffMemberId,
        AssignTeacherSectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var staff = await _db.StaffMembers.FirstOrDefaultAsync(s => s.Id == staffMemberId, cancellationToken)
            ?? throw new KeyNotFoundException("Staff member not found.");

        EnsureCampusAuthorized(staff.CampusId);

        var placement = await _organization.ValidatePlacementAsync(
            tenantId,
            staff.CampusId,
            command.AcademicYearId,
            command.GradeLevelId,
            command.SectionId,
            cancellationToken);

        if (!placement.IsValid || placement.Section is null)
        {
            throw new InvalidOperationException(placement.ErrorMessage ?? "Invalid section placement for teacher assignment.");
        }

        var existing = await _db.TeacherSectionAssignments.FirstOrDefaultAsync(
            a => a.StaffMemberId == staff.Id
                && a.AcademicYearId == command.AcademicYearId
                && a.SectionId == command.SectionId,
            cancellationToken);

        if (existing is not null)
        {
            existing.Activate();
            existing.UpdateHomeroom(command.IsHomeroomTeacher);
            await _db.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var assignment = TeacherSectionAssignment.Create(
            tenantId,
            staff.SchoolId,
            staff.CampusId,
            staff.Id,
            command.AcademicYearId,
            command.GradeLevelId,
            command.SectionId,
            command.IsHomeroomTeacher);

        _db.TeacherSectionAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task<SectionRegisterViewDto> GetSectionRegisterAsync(
        Guid sectionId,
        DateOnly attendanceDate,
        ClaimsPrincipal? caller,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var section = await _organization.GetSectionAsync(tenantId, sectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Section not found.");

        EnsureCampusAuthorized(section.CampusId);
        await EnsureTeacherSectionAuthorizedAsync(section, caller, cancellationToken);

        var roster = await _rosterReader.GetActiveSectionRosterAsync(
            tenantId,
            section.CampusId,
            section.AcademicYearId,
            section.Id,
            cancellationToken);

        var register = await _db.DailyAttendanceRegisters
            .Include(r => r.Entries)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.CampusId == section.CampusId
                    && r.AcademicYearId == section.AcademicYearId
                    && r.SectionId == section.Id
                    && r.AttendanceDate == attendanceDate,
                cancellationToken);

        var entryByStudent = register?.Entries.ToDictionary(e => e.StudentId)
            ?? new Dictionary<Guid, AttendanceEntry>();

        var rows = roster.Select(s =>
        {
            entryByStudent.TryGetValue(s.StudentId, out var entry);
            return new RosterAttendanceEntryDto(
                s.StudentId,
                s.AdmissionNumber,
                s.FirstName,
                s.LastName,
                s.RollNumber,
                entry?.Status,
                entry?.Remarks,
                entry?.LastCorrectionReason,
                entry?.LastUpdatedAtUtc);
        }).ToList();

        return new SectionRegisterViewDto(
            register?.Id,
            section.CampusId,
            section.AcademicYearId,
            section.GradeLevelId,
            section.Id,
            section.Name,
            attendanceDate,
            IsMarked: register is not null,
            IsLocked: register?.IsLocked ?? false,
            Notes: register?.Notes,
            Students: rows);
    }

    public async Task<MarkAttendanceResultDto> MarkSectionAttendanceAsync(
        Guid sectionId,
        MarkSectionAttendanceCommand command,
        ClaimsPrincipal? caller,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        if (command.Entries is null || command.Entries.Count == 0)
        {
            throw new ArgumentException("At least one student attendance entry is required.", nameof(command));
        }

        var duplicateStudentIds = command.Entries
            .GroupBy(e => e.StudentId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateStudentIds.Count > 0)
        {
            throw new ArgumentException("Duplicate student IDs in a single attendance submission are not allowed.");
        }

        var section = await _organization.GetSectionAsync(tenantId, sectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Section not found.");

        EnsureCampusAuthorized(section.CampusId);
        await EnsureTeacherSectionAuthorizedAsync(section, caller, cancellationToken);

        var roster = await _rosterReader.GetActiveSectionRosterAsync(
            tenantId,
            section.CampusId,
            section.AcademicYearId,
            section.Id,
            cancellationToken);
        var validStudentIds = roster.Select(r => r.StudentId).ToHashSet();

        foreach (var item in command.Entries)
        {
            if (!validStudentIds.Contains(item.StudentId))
            {
                throw new InvalidOperationException(
                    $"Student '{item.StudentId}' is not actively enrolled in section '{section.Code}'.");
            }
        }

        var now = _timeProvider.GetUtcNow();
        var actor = _tenantContext.UserId ?? "system";

        var register = await _db.DailyAttendanceRegisters
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(
                r => r.CampusId == section.CampusId
                    && r.AcademicYearId == section.AcademicYearId
                    && r.SectionId == section.Id
                    && r.AttendanceDate == command.AttendanceDate,
                cancellationToken);

        if (register is null)
        {
            register = DailyAttendanceRegister.Create(
                tenantId,
                section.SchoolId,
                section.CampusId,
                section.AcademicYearId,
                section.GradeLevelId,
                section.Id,
                command.AttendanceDate,
                actor,
                now,
                command.Notes);
            _db.DailyAttendanceRegisters.Add(register);
        }
        else
        {
            if (register.IsLocked)
            {
                throw new InvalidOperationException("Attendance register is locked and cannot be modified.");
            }
            register.UpdateNotes(command.Notes, actor, now);
        }

        var existingEntries = register.Entries.ToDictionary(e => e.StudentId);
        int created = 0, updated = 0, corrections = 0;

        foreach (var item in command.Entries)
        {
            if (existingEntries.TryGetValue(item.StudentId, out var existing))
            {
                var audit = existing.Correct(
                    command.AttendanceDate,
                    item.Status,
                    item.Remarks,
                    item.CorrectionReason,
                    actor,
                    now);
                if (audit is not null)
                {
                    _db.AttendanceCorrectionAudits.Add(audit);
                    corrections++;
                    updated++;
                }
            }
            else
            {
                var entry = AttendanceEntry.Create(
                    tenantId,
                    section.CampusId,
                    register.Id,
                    item.StudentId,
                    item.Status,
                    item.Remarks,
                    actor,
                    now);
                _db.AttendanceEntries.Add(entry);
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new MarkAttendanceResultDto(register.Id, command.AttendanceDate, created, updated, corrections);
    }

    public async Task<AttendanceSummaryDto> GetSectionSummaryAsync(
        Guid sectionId,
        DateOnly? fromDate,
        DateOnly? toDate,
        ClaimsPrincipal? caller,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var section = await _organization.GetSectionAsync(tenantId, sectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Section not found.");

        EnsureCampusAuthorized(section.CampusId);
        await EnsureTeacherSectionAuthorizedAsync(section, caller, cancellationToken);

        var query = from e in _db.AttendanceEntries.AsNoTracking()
                    join r in _db.DailyAttendanceRegisters.AsNoTracking() on e.RegisterId equals r.Id
                    where r.SectionId == section.Id
                    select new { e.Status, r.AttendanceDate };

        if (fromDate.HasValue) query = query.Where(x => x.AttendanceDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(x => x.AttendanceDate <= toDate.Value);

        var statuses = await query.Select(x => x.Status).ToListAsync(cancellationToken);
        return ComputeSummary(statuses);
    }

    public async Task<AttendanceSummaryDto> GetStudentSummaryAsync(
        Guid studentId,
        Guid? academicYearId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        _ = RequireTenantId();

        var query = from e in _db.AttendanceEntries.AsNoTracking()
                    join r in _db.DailyAttendanceRegisters.AsNoTracking() on e.RegisterId equals r.Id
                    where e.StudentId == studentId
                    select new { e.Status, e.CampusId, r.AcademicYearId, r.AttendanceDate };

        if (_tenantContext.CampusId.HasValue)
        {
            query = query.Where(x => x.CampusId == _tenantContext.CampusId.Value);
        }

        if (academicYearId.HasValue) query = query.Where(x => x.AcademicYearId == academicYearId.Value);
        if (fromDate.HasValue) query = query.Where(x => x.AttendanceDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(x => x.AttendanceDate <= toDate.Value);

        var statuses = await query.Select(x => x.Status).ToListAsync(cancellationToken);
        return ComputeSummary(statuses);
    }

    public static AttendanceSummaryDto ComputeSummary(IReadOnlyCollection<AttendanceStatus> statuses)
    {
        var total = statuses.Count;
        var present = statuses.Count(s => s == AttendanceStatus.Present);
        var absent = statuses.Count(s => s == AttendanceStatus.Absent);
        var late = statuses.Count(s => s == AttendanceStatus.Late);
        var excused = statuses.Count(s => s == AttendanceStatus.Excused);
        var halfDay = statuses.Count(s => s == AttendanceStatus.HalfDay);

        // Attended credit: Present + Late = 1.0, HalfDay = 0.5
        decimal percentage = total == 0
            ? 0m
            : Math.Round(((present + late + (halfDay * 0.5m)) / total) * 100m, 2, MidpointRounding.AwayFromZero);

        return new AttendanceSummaryDto(total, present, absent, late, excused, halfDay, percentage);
    }

    private Guid RequireTenantId()
    {
        if (!_tenantContext.TenantId.HasValue || _tenantContext.TenantId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        return _tenantContext.TenantId.Value;
    }

    private void EnsureCampusAuthorized(Guid targetCampusId)
    {
        if (_tenantContext.CampusId.HasValue && _tenantContext.CampusId.Value != targetCampusId)
        {
            throw new UnauthorizedAccessException("Caller is not authorized to access another campus.");
        }
    }

    private async Task EnsureTeacherSectionAuthorizedAsync(
        SectionLookup section,
        ClaimsPrincipal? caller,
        CancellationToken cancellationToken)
    {
        if (caller is null)
        {
            return;
        }

        // Tenant/School admins and Principals may access all sections within their authorized campus scope
        if (caller.IsInRole(SecurityPolicies.TenantAdminRole)
            || caller.IsInRole(SecurityPolicies.SchoolAdminRole)
            || caller.IsInRole(SecurityPolicies.PrincipalRole)
            || caller.IsInRole(SecurityPolicies.PlatformAdminRole))
        {
            return;
        }

        if (caller.IsInRole(SecurityPolicies.TeacherRole))
        {
            if (!Guid.TryParse(_tenantContext.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Teacher account lacks a valid user identifier.");
            }

            var isAssigned = await (
                from a in _db.TeacherSectionAssignments.AsNoTracking()
                join s in _db.StaffMembers.AsNoTracking() on a.StaffMemberId equals s.Id
                where s.UserId == userId
                    && s.IsActive
                    && a.IsActive
                    && a.SectionId == section.Id
                    && a.AcademicYearId == section.AcademicYearId
                select a.Id
            ).AnyAsync(cancellationToken);

            if (!isAssigned)
            {
                throw new UnauthorizedAccessException(
                    "Teacher is not assigned to this section for the active academic year.");
            }
        }
    }
}
