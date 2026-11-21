using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Attendance.Contracts;
using SchoolOS.Modules.Exams.Domain;
using SchoolOS.Modules.Exams.Persistence;
using SchoolOS.Modules.Organization.Contracts;
using SchoolOS.Modules.Students.Contracts;

namespace SchoolOS.Modules.Exams.Application;

public sealed record CreateSubjectCommand(Guid SchoolId, Guid GradeLevelId, string Code, string Name);

public sealed record CreateExamTermCommand(
    Guid CampusId,
    Guid AcademicYearId,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record CreateExamPaperCommand(
    Guid ExamTermId,
    Guid GradeLevelId,
    Guid SubjectId,
    decimal MaxMarks,
    decimal PassingMarks,
    decimal WeightagePercent = 100m,
    DateOnly? ExamDate = null);

public sealed record StudentMarkEntryItem(Guid StudentId, decimal MarksObtained, bool IsAbsent = false, string? Remarks = null);

public sealed record SaveSectionMarksCommand(IReadOnlyList<StudentMarkEntryItem> Entries);

public sealed record ReviewMarkSheetCommand(bool Approve, string? ReviewNotes = null);

public sealed record MarkSheetStudentRowDto(
    Guid StudentId,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    string? RollNumber,
    decimal? MarksObtained,
    bool IsAbsent,
    string? Remarks,
    decimal? Percentage,
    string? LetterGrade,
    bool? IsPassed);

public sealed record SectionMarkSheetViewDto(
    Guid? MarkSheetId,
    Guid ExamPaperId,
    Guid SectionId,
    string SectionName,
    string SubjectName,
    decimal MaxMarks,
    decimal PassingMarks,
    string Status,
    string? ReviewNotes,
    IReadOnlyList<MarkSheetStudentRowDto> Students);

public sealed record ReportCardSubjectDto(
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    decimal MaxMarks,
    decimal PassingMarks,
    decimal MarksObtained,
    bool IsAbsent,
    decimal Percentage,
    string LetterGrade,
    bool IsPassed);

public sealed record StudentReportCardDto(
    Guid ExamTermId,
    string ExamTermName,
    Guid StudentId,
    decimal TotalMaxMarks,
    decimal TotalObtainedMarks,
    decimal OverallPercentage,
    string OverallGrade,
    bool IsOverallPassed,
    IReadOnlyList<ReportCardSubjectDto> Subjects);

public sealed class ExamsService
{
    private readonly ExamsDbContext _db;
    private readonly IOrganizationAcademicReader _organization;
    private readonly IStudentRosterReader _rosterReader;
    private readonly ITeacherAssignmentReader _teacherAssignments;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public ExamsService(
        ExamsDbContext db,
        IOrganizationAcademicReader organization,
        IStudentRosterReader rosterReader,
        ITeacherAssignmentReader teacherAssignments,
        ITenantContext tenantContext,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _organization = organization;
        _rosterReader = rosterReader;
        _teacherAssignments = teacherAssignments;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Subject> CreateSubjectAsync(CreateSubjectCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var grade = await _organization.GetGradeLevelAsync(tenantId, command.GradeLevelId, cancellationToken)
            ?? throw new KeyNotFoundException("Grade level not found.");

        if (grade.SchoolId != command.SchoolId)
        {
            throw new InvalidOperationException("Grade level does not belong to the specified school.");
        }

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        var duplicate = await _db.Subjects.AnyAsync(
            s => s.SchoolId == command.SchoolId && s.GradeLevelId == command.GradeLevelId && s.Code == normalizedCode,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException($"Subject '{normalizedCode}' already exists for this grade level.");
        }

        var subject = Subject.Create(tenantId, command.SchoolId, command.GradeLevelId, command.Code, command.Name);
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync(cancellationToken);
        return subject;
    }

    public async Task<ExamTerm> CreateExamTermAsync(CreateExamTermCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        EnsureCampusAuthorized(command.CampusId);

        var campus = await _organization.GetCampusAsync(tenantId, command.CampusId, cancellationToken)
            ?? throw new KeyNotFoundException("Campus not found.");
        var year = await _organization.GetAcademicYearAsync(tenantId, command.AcademicYearId, cancellationToken)
            ?? throw new KeyNotFoundException("Academic year not found.");

        if (campus.SchoolId != year.SchoolId)
        {
            throw new InvalidOperationException("Academic year does not belong to the campus's school.");
        }

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        var duplicate = await _db.ExamTerms.AnyAsync(
            t => t.CampusId == command.CampusId && t.AcademicYearId == command.AcademicYearId && t.Code == normalizedCode,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException($"Exam term '{normalizedCode}' already exists for this campus and academic year.");
        }

        var term = ExamTerm.Create(
            tenantId,
            campus.SchoolId,
            campus.Id,
            year.Id,
            command.Code,
            command.Name,
            command.StartDate,
            command.EndDate);

        _db.ExamTerms.Add(term);
        await _db.SaveChangesAsync(cancellationToken);
        return term;
    }

    public async Task<ExamPaper> CreateExamPaperAsync(CreateExamPaperCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var term = await _db.ExamTerms.FirstOrDefaultAsync(t => t.Id == command.ExamTermId, cancellationToken)
            ?? throw new KeyNotFoundException("Exam term not found.");

        EnsureCampusAuthorized(term.CampusId);

        var subject = await _db.Subjects.FirstOrDefaultAsync(
            s => s.Id == command.SubjectId && s.SchoolId == term.SchoolId && s.GradeLevelId == command.GradeLevelId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Subject not found for this school and grade level.");

        var duplicate = await _db.ExamPapers.AnyAsync(
            p => p.ExamTermId == term.Id && p.GradeLevelId == command.GradeLevelId && p.SubjectId == subject.Id,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("An exam paper already exists for this subject in the exam term.");
        }

        var paper = ExamPaper.Create(
            tenantId,
            term.SchoolId,
            term.CampusId,
            term.Id,
            command.GradeLevelId,
            subject.Id,
            command.MaxMarks,
            command.PassingMarks,
            command.WeightagePercent,
            command.ExamDate);

        _db.ExamPapers.Add(paper);
        await _db.SaveChangesAsync(cancellationToken);
        return paper;
    }

    public async Task<SectionMarkSheetViewDto> GetSectionMarkSheetAsync(
        Guid examPaperId,
        Guid sectionId,
        ClaimsPrincipal? caller,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var paperInfo = await (
            from p in _db.ExamPapers.AsNoTracking()
            join t in _db.ExamTerms.AsNoTracking() on p.ExamTermId equals t.Id
            join s in _db.Subjects.AsNoTracking() on p.SubjectId equals s.Id
            where p.Id == examPaperId
            select new { Paper = p, Term = t, SubjectName = s.Name }
        ).FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Exam paper not found.");

        var section = await _organization.GetSectionAsync(tenantId, sectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Section not found.");

        if (section.CampusId != paperInfo.Paper.CampusId
            || section.AcademicYearId != paperInfo.Term.AcademicYearId
            || section.GradeLevelId != paperInfo.Paper.GradeLevelId)
        {
            throw new InvalidOperationException("Section does not match the exam paper's campus, academic year, and grade level.");
        }

        EnsureCampusAuthorized(section.CampusId);
        await EnsureTeacherSectionAuthorizedAsync(tenantId, section, caller, cancellationToken);

        var roster = await _rosterReader.GetActiveSectionRosterAsync(
            tenantId,
            section.CampusId,
            section.AcademicYearId,
            section.Id,
            cancellationToken);

        var sheet = await _db.MarkSheets
            .Include(m => m.Marks)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ExamPaperId == examPaperId && m.SectionId == sectionId, cancellationToken);

        var markByStudent = sheet?.Marks.ToDictionary(m => m.StudentId)
            ?? new Dictionary<Guid, StudentMark>();

        var rows = roster.Select(student =>
        {
            if (markByStudent.TryGetValue(student.StudentId, out var mark))
            {
                var pct = ExamGrading.CalculatePercentage(mark.MarksObtained, paperInfo.Paper.MaxMarks);
                var passed = !mark.IsAbsent && mark.MarksObtained >= paperInfo.Paper.PassingMarks;
                var letter = ExamGrading.CalculateLetterGrade(pct, passed);
                return new MarkSheetStudentRowDto(
                    student.StudentId,
                    student.AdmissionNumber,
                    student.FirstName,
                    student.LastName,
                    student.RollNumber,
                    mark.MarksObtained,
                    mark.IsAbsent,
                    mark.Remarks,
                    pct,
                    letter,
                    passed);
            }

            return new MarkSheetStudentRowDto(
                student.StudentId,
                student.AdmissionNumber,
                student.FirstName,
                student.LastName,
                student.RollNumber,
                null,
                false,
                null,
                null,
                null,
                null);
        }).ToList();

        return new SectionMarkSheetViewDto(
            sheet?.Id,
            paperInfo.Paper.Id,
            section.Id,
            section.Name,
            paperInfo.SubjectName,
            paperInfo.Paper.MaxMarks,
            paperInfo.Paper.PassingMarks,
            (sheet?.Status ?? MarkSheetStatus.Draft).ToString(),
            sheet?.ReviewNotes,
            rows);
    }

    public async Task<MarkSheet> SaveSectionMarksAsync(
        Guid examPaperId,
        Guid sectionId,
        SaveSectionMarksCommand command,
        ClaimsPrincipal? caller,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        if (command.Entries is null || command.Entries.Count == 0)
        {
            throw new ArgumentException("At least one student mark entry is required.", nameof(command));
        }

        if (command.Entries.GroupBy(e => e.StudentId).Any(g => g.Count() > 1))
        {
            throw new ArgumentException("Duplicate student IDs in a single mark submission are not allowed.");
        }

        var paperInfo = await (
            from p in _db.ExamPapers
            join t in _db.ExamTerms on p.ExamTermId equals t.Id
            where p.Id == examPaperId
            select new { Paper = p, Term = t }
        ).FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Exam paper not found.");

        var section = await _organization.GetSectionAsync(tenantId, sectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Section not found.");

        if (section.CampusId != paperInfo.Paper.CampusId
            || section.AcademicYearId != paperInfo.Term.AcademicYearId
            || section.GradeLevelId != paperInfo.Paper.GradeLevelId)
        {
            throw new InvalidOperationException("Section does not match the exam paper's campus, academic year, and grade level.");
        }

        EnsureCampusAuthorized(section.CampusId);
        await EnsureTeacherSectionAuthorizedAsync(tenantId, section, caller, cancellationToken);

        var roster = await _rosterReader.GetActiveSectionRosterAsync(
            tenantId,
            section.CampusId,
            section.AcademicYearId,
            section.Id,
            cancellationToken);
        var validStudentIds = roster.Select(r => r.StudentId).ToHashSet();

        foreach (var entry in command.Entries)
        {
            if (!validStudentIds.Contains(entry.StudentId))
            {
                throw new InvalidOperationException($"Student '{entry.StudentId}' is not actively enrolled in section '{section.Code}'.");
            }
        }

        var sheet = await _db.MarkSheets
            .Include(m => m.Marks)
            .FirstOrDefaultAsync(m => m.ExamPaperId == examPaperId && m.SectionId == sectionId, cancellationToken);

        if (sheet is null)
        {
            sheet = MarkSheet.Create(tenantId, paperInfo.Paper.SchoolId, paperInfo.Paper.CampusId, examPaperId, sectionId);
            _db.MarkSheets.Add(sheet);
        }
        else
        {
            sheet.EnsureEditable();
        }

        var existingMarks = sheet.Marks.ToDictionary(m => m.StudentId);
        foreach (var item in command.Entries)
        {
            if (existingMarks.TryGetValue(item.StudentId, out var existing))
            {
                existing.UpdateScore(item.MarksObtained, paperInfo.Paper.MaxMarks, item.IsAbsent, item.Remarks);
            }
            else
            {
                var mark = StudentMark.Create(
                    tenantId,
                    section.CampusId,
                    sheet.Id,
                    item.StudentId,
                    item.MarksObtained,
                    paperInfo.Paper.MaxMarks,
                    item.IsAbsent,
                    item.Remarks);
                _db.StudentMarks.Add(mark);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return sheet;
    }

    public async Task<MarkSheet> SubmitMarkSheetAsync(Guid markSheetId, ClaimsPrincipal? caller, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var sheet = await _db.MarkSheets
            .Include(m => m.Marks)
            .FirstOrDefaultAsync(m => m.Id == markSheetId, cancellationToken)
            ?? throw new KeyNotFoundException("Mark sheet not found.");

        EnsureCampusAuthorized(sheet.CampusId);

        var section = await _organization.GetSectionAsync(tenantId, sheet.SectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Section not found.");
        await EnsureTeacherSectionAuthorizedAsync(tenantId, section, caller, cancellationToken);

        if (sheet.Marks.Count == 0)
        {
            throw new InvalidOperationException("Cannot submit an empty mark sheet.");
        }

        var actor = _tenantContext.UserId ?? "system";
        sheet.Submit(actor, _timeProvider.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);
        return sheet;
    }

    public async Task<MarkSheet> ReviewMarkSheetAsync(
        Guid markSheetId,
        ReviewMarkSheetCommand command,
        ClaimsPrincipal? caller,
        CancellationToken cancellationToken = default)
    {
        _ = RequireTenantId();
        EnsureApproverAuthorized(caller);

        var sheet = await _db.MarkSheets.FirstOrDefaultAsync(m => m.Id == markSheetId, cancellationToken)
            ?? throw new KeyNotFoundException("Mark sheet not found.");

        EnsureCampusAuthorized(sheet.CampusId);

        var actor = _tenantContext.UserId ?? "system";
        var now = _timeProvider.GetUtcNow();

        if (command.Approve)
        {
            sheet.Approve(actor, command.ReviewNotes, now);
        }
        else
        {
            sheet.ReturnForCorrection(actor, command.ReviewNotes ?? string.Empty, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return sheet;
    }

    public async Task<StudentReportCardDto> GetStudentReportCardAsync(
        Guid examTermId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        _ = RequireTenantId();
        var term = await _db.ExamTerms.AsNoTracking().FirstOrDefaultAsync(t => t.Id == examTermId, cancellationToken)
            ?? throw new KeyNotFoundException("Exam term not found.");

        EnsureCampusAuthorized(term.CampusId);

        var approvedMarks = await (
            from sm in _db.StudentMarks.AsNoTracking()
            join ms in _db.MarkSheets.AsNoTracking() on sm.MarkSheetId equals ms.Id
            join ep in _db.ExamPapers.AsNoTracking() on ms.ExamPaperId equals ep.Id
            join sub in _db.Subjects.AsNoTracking() on ep.SubjectId equals sub.Id
            where ep.ExamTermId == examTermId
                && sm.StudentId == studentId
                && ms.Status == MarkSheetStatus.Approved
            orderby sub.Code
            select new
            {
                sub.Id,
                sub.Code,
                sub.Name,
                ep.MaxMarks,
                ep.PassingMarks,
                sm.MarksObtained,
                sm.IsAbsent
            }
        ).ToListAsync(cancellationToken);

        var subjects = approvedMarks.Select(x =>
        {
            var pct = ExamGrading.CalculatePercentage(x.MarksObtained, x.MaxMarks);
            var passed = !x.IsAbsent && x.MarksObtained >= x.PassingMarks;
            var grade = ExamGrading.CalculateLetterGrade(pct, passed);
            return new ReportCardSubjectDto(
                x.Id,
                x.Code,
                x.Name,
                x.MaxMarks,
                x.PassingMarks,
                x.MarksObtained,
                x.IsAbsent,
                pct,
                grade,
                passed);
        }).ToList();

        var totalMax = subjects.Sum(s => s.MaxMarks);
        var totalObtained = subjects.Sum(s => s.MarksObtained);
        var overallPct = ExamGrading.CalculatePercentage(totalObtained, totalMax);
        var overallPassed = subjects.Count > 0 && subjects.All(s => s.IsPassed);
        var overallGrade = subjects.Count == 0 ? "N/A" : ExamGrading.CalculateLetterGrade(overallPct, overallPassed);

        return new StudentReportCardDto(
            term.Id,
            term.Name,
            studentId,
            totalMax,
            totalObtained,
            overallPct,
            overallGrade,
            overallPassed,
            subjects);
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

    private static void EnsureApproverAuthorized(ClaimsPrincipal? caller)
    {
        if (caller is null) return;

        var isApprover = caller.IsInRole(SecurityPolicies.PrincipalRole)
            || caller.IsInRole(SecurityPolicies.SchoolAdminRole)
            || caller.IsInRole(SecurityPolicies.TenantAdminRole)
            || caller.IsInRole(SecurityPolicies.PlatformAdminRole);

        if (!isApprover)
        {
            throw new UnauthorizedAccessException("Only a Principal or Administrator can approve or return mark sheets.");
        }
    }

    private async Task EnsureTeacherSectionAuthorizedAsync(
        Guid tenantId,
        SectionLookup section,
        ClaimsPrincipal? caller,
        CancellationToken cancellationToken)
    {
        if (caller is null) return;

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

            var assigned = await _teacherAssignments.IsTeacherAssignedToSectionAsync(
                tenantId,
                userId,
                section.AcademicYearId,
                section.Id,
                cancellationToken);

            if (!assigned)
            {
                throw new UnauthorizedAccessException("Teacher is not assigned to this section for the active academic year.");
            }
        }
    }
}
