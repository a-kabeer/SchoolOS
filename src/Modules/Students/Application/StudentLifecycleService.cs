using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Contracts;
using SchoolOS.Modules.Students.Contracts;
using SchoolOS.Modules.Students.Domain;
using SchoolOS.Modules.Students.Persistence;

namespace SchoolOS.Modules.Students.Application;

public sealed record RegisterStudentCommand(
    Guid? CampusId,
    string? AdmissionNumber,
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly DateOfBirth,
    string? Gender,
    DateOnly? AdmissionDate,
    string? Notes,
    Guid? AcademicYearId = null,
    Guid? GradeLevelId = null,
    Guid? SectionId = null,
    string? RollNumber = null);

public sealed record UpdateStudentCommand(
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly DateOfBirth,
    string? Gender,
    string? Notes);

public sealed record ChangeStudentStatusCommand(
    StudentLifecycleStatus TargetStatus,
    DateOnly? EffectiveDate,
    string Reason);

public sealed record EnrollStudentCommand(
    Guid? CampusId,
    Guid AcademicYearId,
    Guid GradeLevelId,
    Guid? SectionId,
    string? RollNumber,
    DateOnly? StartDate,
    string? Reason);

public sealed record TransferStudentCommand(
    Guid TargetCampusId,
    Guid AcademicYearId,
    Guid TargetGradeLevelId,
    Guid? TargetSectionId,
    string? RollNumber,
    DateOnly? EffectiveDate,
    string Reason);

public sealed record CreateGuardianCommand(
    string FirstName,
    string LastName,
    string PrimaryPhone,
    string? SecondaryPhone,
    string? Email,
    string? Occupation,
    string? PreferredChannel);

public sealed record LinkGuardianCommand(
    Guid GuardianId,
    GuardianRelationshipType RelationshipType,
    bool IsPrimaryContact,
    bool IsEmergencyContact,
    bool HasLegalCustody,
    bool CanCollectStudent,
    bool ReceivesAcademicNotices,
    bool ReceivesFinancialNotices);

public sealed class StudentLifecycleService : IStudentAdmissionBridge, IStudentRosterReader, IStudentBillingReader
{
    private readonly StudentsDbContext _db;
    private readonly IOrganizationAcademicReader _organization;
    private readonly ITenantContext _tenantContext;
    private readonly ISecurityAuditLog _auditLog;
    private readonly TimeProvider _timeProvider;

    public StudentLifecycleService(
        StudentsDbContext db,
        IOrganizationAcademicReader organization,
        ITenantContext tenantContext,
        ISecurityAuditLog auditLog,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _organization = organization;
        _tenantContext = tenantContext;
        _auditLog = auditLog;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<(Student Student, Enrollment? InitialEnrollment)> RegisterStudentAsync(
        RegisterStudentCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var campusId = ResolveAuthorizedCampusId(command.CampusId);

        var campus = await _organization.GetCampusAsync(tenantId, campusId, cancellationToken)
            ?? throw new KeyNotFoundException("Campus does not exist in the authenticated tenant.");

        if (!campus.IsActive)
        {
            throw new InvalidOperationException("Cannot register a student into an inactive campus.");
        }

        var admissionDate = command.AdmissionDate ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var admissionNumber = string.IsNullOrWhiteSpace(command.AdmissionNumber)
            ? await GenerateUniqueAdmissionNumberAsync(tenantId, campus.SchoolId, admissionDate.Year, cancellationToken)
            : command.AdmissionNumber.Trim().ToUpperInvariant();

        var duplicateAdmission = await _db.Students.AnyAsync(
            s => s.TenantId == tenantId && s.SchoolId == campus.SchoolId && s.AdmissionNumber == admissionNumber,
            cancellationToken);
        if (duplicateAdmission)
        {
            throw new InvalidOperationException($"Admission number '{admissionNumber}' already exists in this school.");
        }

        PlacementValidationResult? placement = null;
        if (command.AcademicYearId.HasValue || command.GradeLevelId.HasValue)
        {
            if (!command.AcademicYearId.HasValue || !command.GradeLevelId.HasValue)
            {
                throw new ArgumentException("Both AcademicYearId and GradeLevelId are required when registering with initial placement.");
            }

            placement = await _organization.ValidatePlacementAsync(
                tenantId,
                campusId,
                command.AcademicYearId.Value,
                command.GradeLevelId.Value,
                command.SectionId,
                cancellationToken);
            if (!placement.IsValid)
            {
                throw new InvalidOperationException(placement.ErrorMessage ?? "Invalid academic placement.");
            }
        }

        var student = Student.Create(
            tenantId,
            campus.SchoolId,
            campusId,
            admissionNumber,
            command.FirstName,
            command.MiddleName,
            command.LastName,
            command.DateOfBirth,
            command.Gender,
            admissionDate,
            sourceEnquiryId: null,
            notes: command.Notes);

        _db.Students.Add(student);

        Enrollment? initialEnrollment = null;
        if (placement is not null && command.AcademicYearId.HasValue && command.GradeLevelId.HasValue)
        {
            initialEnrollment = Enrollment.Create(
                tenantId,
                campus.SchoolId,
                campusId,
                student.Id,
                command.AcademicYearId.Value,
                command.GradeLevelId.Value,
                command.SectionId,
                command.RollNumber,
                admissionDate,
                previousEnrollmentId: null,
                reason: "InitialAdmission");
            _db.Enrollments.Add(initialEnrollment);
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.UserDetailsCreated,
            SecurityAuditOutcome.Success,
            actorUserId: _tenantContext.UserId,
            targetType: nameof(Student),
            targetId: student.Id.ToString(),
            cancellationToken: cancellationToken);

        return (student, initialEnrollment);
    }

    public async Task<AdmittedStudentResult> AdmitFromEnquiryAsync(
        AdmitStudentFromEnquiryCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        if (command.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("Cross-tenant admission conversion is prohibited.");
        }

        var campusId = ResolveAuthorizedCampusId(command.CampusId);

        // Idempotency guard: prevent duplicate student creation when admission conversion is retried
        var existingByEnquiry = await _db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SourceEnquiryId == command.SourceEnquiryId, cancellationToken);
        if (existingByEnquiry is not null)
        {
            return new AdmittedStudentResult(
                existingByEnquiry.Id,
                existingByEnquiry.AdmissionNumber,
                existingByEnquiry.FirstName,
                existingByEnquiry.LastName,
                WasExisting: true);
        }

        var campus = await _organization.GetCampusAsync(tenantId, campusId, cancellationToken)
            ?? throw new KeyNotFoundException("Campus does not exist in the authenticated tenant.");

        var normalizedAdmNo = command.AdmissionNumber.Trim().ToUpperInvariant();
        var duplicateAdm = await _db.Students.AnyAsync(
            s => s.TenantId == tenantId && s.SchoolId == campus.SchoolId && s.AdmissionNumber == normalizedAdmNo,
            cancellationToken);
        if (duplicateAdm)
        {
            throw new InvalidOperationException($"Admission number '{normalizedAdmNo}' already exists in this school.");
        }

        var student = Student.Create(
            tenantId,
            campus.SchoolId,
            campusId,
            normalizedAdmNo,
            command.FirstName,
            null,
            command.LastName,
            command.DateOfBirth,
            null,
            command.AdmissionDate,
            sourceEnquiryId: command.SourceEnquiryId,
            notes: $"Admitted from enquiry for {command.TargetGrade}");

        var guardianNameParts = command.GuardianName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var gFirst = guardianNameParts.Length > 0 ? guardianNameParts[0] : command.GuardianName.Trim();
        var gLast = guardianNameParts.Length > 1 ? guardianNameParts[1] : "Guardian";

        var guardian = Guardian.Create(
            tenantId,
            gFirst,
            gLast,
            command.GuardianPhone,
            secondaryPhone: null,
            email: command.GuardianEmail);

        var link = StudentGuardian.Create(
            tenantId,
            student.Id,
            guardian.Id,
            GuardianRelationshipType.LegalGuardian,
            isPrimaryContact: true,
            isEmergencyContact: true,
            hasLegalCustody: true,
            canCollectStudent: true,
            receivesAcademicNotices: true,
            receivesFinancialNotices: true);

        _db.Students.Add(student);
        _db.Guardians.Add(guardian);
        _db.StudentGuardians.Add(link);
        await _db.SaveChangesAsync(cancellationToken);

        return new AdmittedStudentResult(student.Id, student.AdmissionNumber, student.FirstName, student.LastName, WasExisting: false);
    }

    public async Task<IReadOnlyList<AdmittedStudentSummary>> ListAdmittedStudentsAsync(
        Guid? campusId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Students.AsNoTracking();
        if (campusId.HasValue)
        {
            query = query.Where(s => s.CampusId == campusId.Value);
        }

        return await query
            .OrderBy(s => s.AdmissionNumber)
            .Select(s => new AdmittedStudentSummary(
                s.Id,
                s.CampusId,
                s.AdmissionNumber,
                s.FirstName,
                s.LastName,
                s.Status.ToString(),
                s.SourceEnquiryId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EnrolledStudentRosterItem>> GetActiveSectionRosterAsync(
        Guid tenantId,
        Guid campusId,
        Guid academicYearId,
        Guid sectionId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from e in _db.Enrollments.AsNoTracking()
            join s in _db.Students.AsNoTracking() on e.StudentId equals s.Id
            where e.TenantId == tenantId
                && e.CampusId == campusId
                && e.AcademicYearId == academicYearId
                && e.SectionId == sectionId
                && e.Status == EnrollmentStatus.Active
                && s.Status == StudentLifecycleStatus.Active
            orderby e.RollNumber, s.AdmissionNumber
            select new EnrolledStudentRosterItem(
                s.Id,
                e.Id,
                s.AdmissionNumber,
                s.FirstName,
                s.LastName,
                e.RollNumber)
        ).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BillableStudentEnrollmentItem>> GetBillableStudentsAsync(
        Guid tenantId,
        Guid campusId,
        Guid academicYearId,
        Guid gradeLevelId,
        CancellationToken cancellationToken = default)
    {
        var enrolled = await (
            from e in _db.Enrollments.AsNoTracking()
            join s in _db.Students.AsNoTracking() on e.StudentId equals s.Id
            where e.TenantId == tenantId
                && e.CampusId == campusId
                && e.AcademicYearId == academicYearId
                && e.GradeLevelId == gradeLevelId
                && e.Status == EnrollmentStatus.Active
                && s.Status == StudentLifecycleStatus.Active
            orderby s.AdmissionNumber
            select new
            {
                s.Id,
                EnrollmentId = e.Id,
                e.CampusId,
                e.AcademicYearId,
                e.GradeLevelId,
                s.AdmissionNumber,
                s.FirstName,
                s.LastName
            }
        ).ToListAsync(cancellationToken);

        if (enrolled.Count == 0)
        {
            return [];
        }

        var studentIds = enrolled.Select(x => x.Id).ToList();
        var memberships = await _db.HouseholdMembers.AsNoTracking()
            .Where(m => m.IsActive && m.StudentId.HasValue && studentIds.Contains(m.StudentId.Value))
            .Select(m => new { StudentId = m.StudentId!.Value, m.HouseholdId })
            .ToListAsync(cancellationToken);

        var householdByStudent = memberships
            .GroupBy(m => m.StudentId)
            .ToDictionary(g => g.Key, g => g.First().HouseholdId);

        var householdIds = householdByStudent.Values.Distinct().ToList();
        var siblingCountsByHousehold = householdIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await (
                from m in _db.HouseholdMembers.AsNoTracking()
                join s in _db.Students.AsNoTracking() on m.StudentId equals s.Id
                where m.IsActive
                    && m.StudentId.HasValue
                    && householdIds.Contains(m.HouseholdId)
                    && s.Status == StudentLifecycleStatus.Active
                group m by m.HouseholdId into g
                select new { HouseholdId = g.Key, Count = g.Count() }
            ).ToDictionaryAsync(x => x.HouseholdId, x => x.Count, cancellationToken);

        return enrolled.Select(x =>
        {
            Guid? hhId = householdByStudent.TryGetValue(x.Id, out var h) ? h : null;
            var siblingCount = hhId.HasValue && siblingCountsByHousehold.TryGetValue(hhId.Value, out var c) ? c : 1;
            return new BillableStudentEnrollmentItem(
                x.Id,
                x.EnrollmentId,
                x.CampusId,
                x.AcademicYearId,
                x.GradeLevelId,
                x.AdmissionNumber,
                x.FirstName,
                x.LastName,
                hhId,
                siblingCount);
        }).ToList();
    }

    public async Task<Student> UpdateStudentAsync(
        Guid studentId,
        UpdateStudentCommand command,
        CancellationToken cancellationToken = default)
    {
        var student = await LoadAuthorizedStudentAsync(studentId, cancellationToken);
        student.UpdateProfile(command.FirstName, command.MiddleName, command.LastName, command.DateOfBirth, command.Gender, command.Notes);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.UserDetailsUpdated,
            SecurityAuditOutcome.Success,
            actorUserId: _tenantContext.UserId,
            targetType: nameof(Student),
            targetId: student.Id.ToString(),
            cancellationToken: cancellationToken);

        return student;
    }

    public async Task<(Student Student, StudentStatusHistory HistoryEntry)> ChangeStudentStatusAsync(
        Guid studentId,
        ChangeStudentStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var student = await LoadAuthorizedStudentAsync(studentId, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var effectiveDate = command.EffectiveDate ?? DateOnly.FromDateTime(now.UtcDateTime);
        var actor = _tenantContext.UserId ?? "system";

        var entry = student.ChangeStatus(command.TargetStatus, effectiveDate, command.Reason, actor, now);
        _db.StudentStatusHistory.Add(entry);

        if (command.TargetStatus is StudentLifecycleStatus.Withdrawn or StudentLifecycleStatus.Graduated)
        {
            var activeEnrollments = await _db.Enrollments
                .Where(e => e.StudentId == student.Id && e.Status == EnrollmentStatus.Active)
                .ToListAsync(cancellationToken);
            foreach (var enrollment in activeEnrollments)
            {
                if (command.TargetStatus == StudentLifecycleStatus.Graduated)
                {
                    enrollment.MarkCompleted(effectiveDate);
                }
                else
                {
                    enrollment.MarkWithdrawn(effectiveDate, command.Reason);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.UserDetailsUpdated,
            SecurityAuditOutcome.Success,
            actorUserId: _tenantContext.UserId,
            targetType: nameof(Student),
            targetId: student.Id.ToString(),
            cancellationToken: cancellationToken);

        return (student, entry);
    }

    public async Task<Enrollment> EnrollStudentAsync(
        Guid studentId,
        EnrollStudentCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var student = await LoadAuthorizedStudentAsync(studentId, cancellationToken);
        if (student.Status != StudentLifecycleStatus.Active)
        {
            throw new InvalidOperationException($"Cannot enroll student in status '{student.Status}'. Student must be Active.");
        }

        var targetCampusId = ResolveAuthorizedCampusId(command.CampusId ?? student.CampusId);
        var placement = await _organization.ValidatePlacementAsync(
            tenantId,
            targetCampusId,
            command.AcademicYearId,
            command.GradeLevelId,
            command.SectionId,
            cancellationToken);

        if (!placement.IsValid || placement.Campus is null)
        {
            throw new InvalidOperationException(placement.ErrorMessage ?? "Invalid academic placement.");
        }

        if (placement.Campus.SchoolId != student.SchoolId)
        {
            throw new InvalidOperationException("Target campus belongs to a different school than the student.");
        }

        var existingActive = await _db.Enrollments.AnyAsync(
            e => e.StudentId == student.Id && e.AcademicYearId == command.AcademicYearId && e.Status == EnrollmentStatus.Active,
            cancellationToken);
        if (existingActive)
        {
            throw new InvalidOperationException("Student already has an active enrollment for the selected academic year. Use transfer to move class/section/campus.");
        }

        var startDate = command.StartDate ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var enrollment = Enrollment.Create(
            tenantId,
            student.SchoolId,
            targetCampusId,
            student.Id,
            command.AcademicYearId,
            command.GradeLevelId,
            command.SectionId,
            command.RollNumber,
            startDate,
            previousEnrollmentId: null,
            reason: command.Reason);

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(cancellationToken);
        return enrollment;
    }

    public async Task<(Enrollment PreviousEnrollment, Enrollment NewEnrollment)> TransferStudentAsync(
        Guid studentId,
        TransferStudentCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Reason);

        var student = await LoadAuthorizedStudentAsync(studentId, cancellationToken);
        if (student.Status != StudentLifecycleStatus.Active)
        {
            throw new InvalidOperationException($"Cannot transfer student in status '{student.Status}'.");
        }

        // Cross-campus transfer is denied if caller is restricted to a different single campus
        if (_tenantContext.CampusId.HasValue && _tenantContext.CampusId.Value != command.TargetCampusId)
        {
            throw new UnauthorizedAccessException("Campus-scoped user cannot transfer a student to a different campus.");
        }

        var placement = await _organization.ValidatePlacementAsync(
            tenantId,
            command.TargetCampusId,
            command.AcademicYearId,
            command.TargetGradeLevelId,
            command.TargetSectionId,
            cancellationToken);

        if (!placement.IsValid || placement.Campus is null)
        {
            throw new InvalidOperationException(placement.ErrorMessage ?? "Invalid target placement for transfer.");
        }

        if (placement.Campus.SchoolId != student.SchoolId)
        {
            throw new InvalidOperationException("Cannot transfer a student to a campus belonging to another school.");
        }

        var activeEnrollment = await _db.Enrollments
            .FirstOrDefaultAsync(
                e => e.StudentId == student.Id && e.AcademicYearId == command.AcademicYearId && e.Status == EnrollmentStatus.Active,
                cancellationToken)
            ?? throw new InvalidOperationException("Student has no active enrollment in the specified academic year to transfer from.");

        var effectiveDate = command.EffectiveDate ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        activeEnrollment.MarkTransferred(effectiveDate, command.Reason);
        await _db.SaveChangesAsync(cancellationToken);

        var nextEnrollment = Enrollment.Create(
            tenantId,
            student.SchoolId,
            command.TargetCampusId,
            student.Id,
            command.AcademicYearId,
            command.TargetGradeLevelId,
            command.TargetSectionId,
            command.RollNumber ?? activeEnrollment.RollNumber,
            effectiveDate,
            previousEnrollmentId: activeEnrollment.Id,
            reason: command.Reason);

        if (student.CampusId != command.TargetCampusId)
        {
            student.AssignCampus(command.TargetCampusId);
        }

        _db.Enrollments.Add(nextEnrollment);
        await _db.SaveChangesAsync(cancellationToken);

        if (tx is not null)
        {
            await tx.CommitAsync(cancellationToken);
        }

        await _auditLog.WriteAsync(
            SecurityAuditEventType.UserDetailsUpdated,
            SecurityAuditOutcome.Success,
            actorUserId: _tenantContext.UserId,
            targetType: nameof(Enrollment),
            targetId: nextEnrollment.Id.ToString(),
            cancellationToken: cancellationToken);

        return (activeEnrollment, nextEnrollment);
    }

    public async Task<Guardian> CreateGuardianAsync(CreateGuardianCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var guardian = Guardian.Create(
            tenantId,
            command.FirstName,
            command.LastName,
            command.PrimaryPhone,
            command.SecondaryPhone,
            command.Email,
            command.Occupation,
            command.PreferredChannel);

        _db.Guardians.Add(guardian);
        await _db.SaveChangesAsync(cancellationToken);
        return guardian;
    }

    public async Task<StudentGuardian> LinkGuardianAsync(
        Guid studentId,
        LinkGuardianCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var student = await LoadAuthorizedStudentAsync(studentId, cancellationToken);

        var guardianExists = await _db.Guardians.AnyAsync(g => g.Id == command.GuardianId && g.IsActive, cancellationToken);
        if (!guardianExists)
        {
            throw new KeyNotFoundException("Guardian was not found in active tenant scope.");
        }

        var alreadyLinked = await _db.StudentGuardians.AnyAsync(
            sg => sg.StudentId == student.Id && sg.GuardianId == command.GuardianId,
            cancellationToken);
        if (alreadyLinked)
        {
            throw new InvalidOperationException("Guardian is already linked to this student.");
        }

        if (command.IsPrimaryContact)
        {
            var existingPrimaries = await _db.StudentGuardians
                .Where(sg => sg.StudentId == student.Id && sg.IsPrimaryContact)
                .ToListAsync(cancellationToken);
            foreach (var p in existingPrimaries)
            {
                p.SetPrimary(false);
            }
        }

        var link = StudentGuardian.Create(
            tenantId,
            student.Id,
            command.GuardianId,
            command.RelationshipType,
            command.IsPrimaryContact,
            command.IsEmergencyContact,
            command.HasLegalCustody,
            command.CanCollectStudent,
            command.ReceivesAcademicNotices,
            command.ReceivesFinancialNotices);

        _db.StudentGuardians.Add(link);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.UserDetailsUpdated,
            SecurityAuditOutcome.Success,
            actorUserId: _tenantContext.UserId,
            targetType: nameof(StudentGuardian),
            targetId: link.Id.ToString(),
            cancellationToken: cancellationToken);

        return link;
    }

    public async Task<Household> CreateHouseholdAsync(
        string householdCode,
        string displayName,
        Guid? primaryGuardianId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var normalizedCode = householdCode.Trim().ToUpperInvariant();
        if (await _db.Households.AnyAsync(h => h.HouseholdCode == normalizedCode, cancellationToken))
        {
            throw new InvalidOperationException($"Household code '{normalizedCode}' already exists.");
        }

        if (primaryGuardianId.HasValue && !await _db.Guardians.AnyAsync(g => g.Id == primaryGuardianId.Value, cancellationToken))
        {
            throw new KeyNotFoundException("Primary guardian was not found in active tenant scope.");
        }

        var household = Household.Create(tenantId, normalizedCode, displayName, primaryGuardianId);
        _db.Households.Add(household);

        if (primaryGuardianId.HasValue)
        {
            _db.HouseholdMembers.Add(HouseholdMember.ForGuardian(tenantId, household.Id, primaryGuardianId.Value, "PrimaryGuardian", isBillingMember: true));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return household;
    }

    public async Task<HouseholdMember> AddHouseholdMemberAsync(
        Guid householdId,
        Guid? studentId,
        Guid? guardianId,
        string roleLabel,
        bool isBillingMember,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var householdExists = await _db.Households.AnyAsync(h => h.Id == householdId && h.IsActive, cancellationToken);
        if (!householdExists)
        {
            throw new KeyNotFoundException("Household was not found in active tenant scope.");
        }

        HouseholdMember member;
        if (studentId.HasValue)
        {
            var student = await LoadAuthorizedStudentAsync(studentId.Value, cancellationToken);
            if (await _db.HouseholdMembers.AnyAsync(m => m.HouseholdId == householdId && m.StudentId == student.Id, cancellationToken))
            {
                throw new InvalidOperationException("Student is already a member of this household.");
            }

            member = HouseholdMember.ForStudent(tenantId, householdId, student.Id, string.IsNullOrWhiteSpace(roleLabel) ? "Student" : roleLabel);
        }
        else if (guardianId.HasValue)
        {
            if (!await _db.Guardians.AnyAsync(g => g.Id == guardianId.Value && g.IsActive, cancellationToken))
            {
                throw new KeyNotFoundException("Guardian was not found in active tenant scope.");
            }

            if (await _db.HouseholdMembers.AnyAsync(m => m.HouseholdId == householdId && m.GuardianId == guardianId.Value, cancellationToken))
            {
                throw new InvalidOperationException("Guardian is already a member of this household.");
            }

            member = HouseholdMember.ForGuardian(tenantId, householdId, guardianId.Value, string.IsNullOrWhiteSpace(roleLabel) ? "Guardian" : roleLabel, isBillingMember);
        }
        else
        {
            throw new ArgumentException("Either studentId or guardianId must be provided.");
        }

        _db.HouseholdMembers.Add(member);
        await _db.SaveChangesAsync(cancellationToken);
        return member;
    }

    public async Task<Student> LoadAuthorizedStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _db.Students
            .Include(s => s.StatusHistory)
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Student '{studentId}' was not found in active tenant scope.");

        if (_tenantContext.CampusId.HasValue && student.CampusId != _tenantContext.CampusId.Value)
        {
            throw new UnauthorizedAccessException("Campus-scoped user cannot access a student belonging to another campus.");
        }

        return student;
    }

    private Guid RequireTenantId()
    {
        if (!_tenantContext.IsAuthenticated || !_tenantContext.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException("Authenticated tenant context is required.");
        }

        return _tenantContext.TenantId.Value;
    }

    private Guid ResolveAuthorizedCampusId(Guid? requestedCampusId)
    {
        if (_tenantContext.CampusId.HasValue)
        {
            if (requestedCampusId.HasValue && requestedCampusId.Value != _tenantContext.CampusId.Value)
            {
                throw new UnauthorizedAccessException("Campus-scoped user cannot act on another campus.");
            }

            return _tenantContext.CampusId.Value;
        }

        if (!requestedCampusId.HasValue || requestedCampusId.Value == Guid.Empty)
        {
            throw new ArgumentException("CampusId is required when the user session is not bound to a single campus.");
        }

        return requestedCampusId.Value;
    }

    private async Task<string> GenerateUniqueAdmissionNumberAsync(
        Guid tenantId,
        Guid schoolId,
        int year,
        CancellationToken cancellationToken)
    {
        var prefix = $"ADM-{year}-";
        var count = await _db.Students.CountAsync(
            s => s.TenantId == tenantId && s.SchoolId == schoolId && s.AdmissionNumber.StartsWith(prefix),
            cancellationToken);

        for (var seq = count + 1; seq <= count + 100; seq++)
        {
            var candidate = $"{prefix}{seq:D4}";
            var exists = await _db.Students.AnyAsync(
                s => s.TenantId == tenantId && s.SchoolId == schoolId && s.AdmissionNumber == candidate,
                cancellationToken);
            if (!exists)
            {
                return candidate;
            }
        }

        return $"{prefix}{Guid.CreateVersion7().ToString("N")[..6].ToUpperInvariant()}";
    }
}
