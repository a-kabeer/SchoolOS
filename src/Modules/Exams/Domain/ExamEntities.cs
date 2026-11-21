using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Exams.Domain;

public enum ExamTermStatus
{
    Draft = 1,
    Active = 2,
    Published = 3,
    Closed = 4
}

public enum MarkSheetStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    ReturnedForCorrection = 4
}

public static class ExamGrading
{
    public static decimal CalculatePercentage(decimal obtained, decimal maxMarks)
    {
        if (maxMarks <= 0m) return 0m;
        return Math.Round((obtained / maxMarks) * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static string CalculateLetterGrade(decimal percentage, bool isPassed)
    {
        if (!isPassed) return "F";
        return percentage switch
        {
            >= 90m => "A+",
            >= 80m => "A",
            >= 70m => "B",
            >= 60m => "C",
            >= 50m => "D",
            _ => "F"
        };
    }
}

/// <summary>
/// School + GradeLevel subject definition (<c>exams.subjects</c>).
/// </summary>
public sealed class Subject : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    private Subject()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private Subject(Guid tenantId, Guid schoolId, Guid gradeLevelId, string code, string name) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (gradeLevelId == Guid.Empty) throw new ArgumentException("Grade level ID cannot be empty.", nameof(gradeLevelId));
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        GradeLevelId = gradeLevelId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid GradeLevelId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public static Subject Create(Guid tenantId, Guid schoolId, Guid gradeLevelId, string code, string name)
        => new(tenantId, schoolId, gradeLevelId, code, name);
}

/// <summary>
/// Campus + AcademicYear examination term (e.g., Midterm 2026, Final 2027) owned by <c>exams.exam_terms</c>.
/// </summary>
public sealed class ExamTerm : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private ExamTerm()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private ExamTerm(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid academicYearId,
        string code,
        string name,
        DateOnly startDate,
        DateOnly endDate) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (academicYearId == Guid.Empty) throw new ArgumentException("Academic year ID cannot be empty.", nameof(academicYearId));
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (endDate < startDate) throw new ArgumentException("Exam term end date cannot precede start date.", nameof(endDate));

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        AcademicYearId = academicYearId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        Status = ExamTermStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid AcademicYearId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public ExamTermStatus Status { get; private set; }

    public static ExamTerm Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid academicYearId,
        string code,
        string name,
        DateOnly startDate,
        DateOnly endDate)
        => new(tenantId, schoolId, campusId, academicYearId, code, name, startDate, endDate);
}

/// <summary>
/// Subject exam paper within an <see cref="ExamTerm"/> (<c>exams.exam_papers</c>).
/// </summary>
public sealed class ExamPaper : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private ExamPaper()
    {
    }

    private ExamPaper(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid examTermId,
        Guid gradeLevelId,
        Guid subjectId,
        decimal maxMarks,
        decimal passingMarks,
        decimal weightagePercent,
        DateOnly? examDate)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (examTermId == Guid.Empty) throw new ArgumentException("Exam term ID cannot be empty.", nameof(examTermId));
        if (gradeLevelId == Guid.Empty) throw new ArgumentException("Grade level ID cannot be empty.", nameof(gradeLevelId));
        if (subjectId == Guid.Empty) throw new ArgumentException("Subject ID cannot be empty.", nameof(subjectId));
        if (maxMarks <= 0m) throw new ArgumentOutOfRangeException(nameof(maxMarks), "Max marks must be greater than zero.");
        if (passingMarks < 0m || passingMarks > maxMarks)
        {
            throw new ArgumentOutOfRangeException(nameof(passingMarks), "Passing marks must be between 0 and max marks.");
        }
        if (weightagePercent is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(weightagePercent), "Weightage percent must be between 0 and 100.");
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        ExamTermId = examTermId;
        GradeLevelId = gradeLevelId;
        SubjectId = subjectId;
        MaxMarks = Math.Round(maxMarks, 2, MidpointRounding.AwayFromZero);
        PassingMarks = Math.Round(passingMarks, 2, MidpointRounding.AwayFromZero);
        WeightagePercent = Math.Round(weightagePercent, 2, MidpointRounding.AwayFromZero);
        ExamDate = examDate;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid ExamTermId { get; private set; }
    public Guid GradeLevelId { get; private set; }
    public Guid SubjectId { get; private set; }
    public decimal MaxMarks { get; private set; }
    public decimal PassingMarks { get; private set; }
    public decimal WeightagePercent { get; private set; }
    public DateOnly? ExamDate { get; private set; }

    public static ExamPaper Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid examTermId,
        Guid gradeLevelId,
        Guid subjectId,
        decimal maxMarks,
        decimal passingMarks,
        decimal weightagePercent = 100m,
        DateOnly? examDate = null)
        => new(tenantId, schoolId, campusId, examTermId, gradeLevelId, subjectId, maxMarks, passingMarks, weightagePercent, examDate);
}

/// <summary>
/// Section mark sheet for an <see cref="ExamPaper"/> enforcing Draft -> Submitted -> Approved workflow (<c>exams.mark_sheets</c>).
/// </summary>
public sealed class MarkSheet : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private readonly List<StudentMark> _marks = [];

    private MarkSheet()
    {
    }

    private MarkSheet(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid examPaperId,
        Guid sectionId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (examPaperId == Guid.Empty) throw new ArgumentException("Exam paper ID cannot be empty.", nameof(examPaperId));
        if (sectionId == Guid.Empty) throw new ArgumentException("Section ID cannot be empty.", nameof(sectionId));

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        ExamPaperId = examPaperId;
        SectionId = sectionId;
        Status = MarkSheetStatus.Draft;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid ExamPaperId { get; private set; }
    public Guid SectionId { get; private set; }
    public MarkSheetStatus Status { get; private set; }
    public string? SubmittedBy { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? ReviewNotes { get; private set; }

    public IReadOnlyCollection<StudentMark> Marks => _marks.AsReadOnly();

    public bool IsEditable => Status is MarkSheetStatus.Draft or MarkSheetStatus.ReturnedForCorrection;

    public static MarkSheet Create(Guid tenantId, Guid schoolId, Guid campusId, Guid examPaperId, Guid sectionId)
        => new(tenantId, schoolId, campusId, examPaperId, sectionId);

    public void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new InvalidOperationException($"Mark sheet in status '{Status}' cannot be modified.");
        }
    }

    public void Submit(string actor, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        EnsureEditable();
        Status = MarkSheetStatus.Submitted;
        SubmittedBy = actor.Trim();
        SubmittedAtUtc = utcNow;
    }

    public void Approve(string actor, string? notes, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        if (Status != MarkSheetStatus.Submitted)
        {
            throw new InvalidOperationException($"Only Submitted mark sheets can be approved (current status: '{Status}').");
        }

        Status = MarkSheetStatus.Approved;
        ApprovedBy = actor.Trim();
        ApprovedAtUtc = utcNow;
        ReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void ReturnForCorrection(string actor, string reviewNotes, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewNotes);
        if (Status != MarkSheetStatus.Submitted)
        {
            throw new InvalidOperationException($"Only Submitted mark sheets can be returned for correction (current status: '{Status}').");
        }

        Status = MarkSheetStatus.ReturnedForCorrection;
        ReviewNotes = reviewNotes.Trim();
        ApprovedBy = null;
        ApprovedAtUtc = null;
        UpdatedAtUtc = utcNow.UtcDateTime;
        UpdatedBy = actor.Trim();
    }
}

/// <summary>
/// Individual student score within a <see cref="MarkSheet"/> (<c>exams.student_marks</c>).
/// </summary>
public sealed class StudentMark : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private StudentMark()
    {
    }

    private StudentMark(
        Guid tenantId,
        Guid campusId,
        Guid markSheetId,
        Guid studentId,
        decimal marksObtained,
        decimal maxMarks,
        bool isAbsent,
        string? remarks)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (markSheetId == Guid.Empty) throw new ArgumentException("MarkSheet ID cannot be empty.", nameof(markSheetId));
        if (studentId == Guid.Empty) throw new ArgumentException("Student ID cannot be empty.", nameof(studentId));

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        CampusId = campusId;
        MarkSheetId = markSheetId;
        StudentId = studentId;
        UpdateScore(marksObtained, maxMarks, isAbsent, remarks);
    }

    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid MarkSheetId { get; private set; }
    public Guid StudentId { get; private set; }
    public decimal MarksObtained { get; private set; }
    public bool IsAbsent { get; private set; }
    public string? Remarks { get; private set; }

    public static StudentMark Create(
        Guid tenantId,
        Guid campusId,
        Guid markSheetId,
        Guid studentId,
        decimal marksObtained,
        decimal maxMarks,
        bool isAbsent,
        string? remarks = null)
        => new(tenantId, campusId, markSheetId, studentId, marksObtained, maxMarks, isAbsent, remarks);

    public void UpdateScore(decimal marksObtained, decimal maxMarks, bool isAbsent, string? remarks)
    {
        var effectiveMarks = isAbsent ? 0m : Math.Round(marksObtained, 2, MidpointRounding.AwayFromZero);
        if (effectiveMarks < 0m || effectiveMarks > maxMarks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(marksObtained),
                $"Marks obtained ({effectiveMarks:F2}) must be between 0 and max marks ({maxMarks:F2}).");
        }

        MarksObtained = effectiveMarks;
        IsAbsent = isAbsent;
        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
    }
}
