using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Organization.Domain;

/// <summary>
/// School academic year owned by the Organization module (<c>organization.academic_years</c>, DOC-43 §5.3).
/// Enforces <c>EndDate >= StartDate</c> and single current academic year per school.
/// </summary>
public sealed class AcademicYear : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    private AcademicYear()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private AcademicYear(
        Guid tenantId,
        Guid schoolId,
        string code,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        bool isCurrent) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (endDate < startDate)
        {
            throw new ArgumentException("Academic year end date must be on or after start date.", nameof(endDate));
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        IsCurrent = isCurrent;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsCurrent { get; private set; }
    public bool IsActive { get; private set; }

    public static AcademicYear Create(
        Guid tenantId,
        Guid schoolId,
        string code,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        bool isCurrent = false)
        => new(tenantId, schoolId, code, name, startDate, endDate, isCurrent);

    public void SetCurrent(bool isCurrent) => IsCurrent = isCurrent;

    public void Deactivate()
    {
        IsActive = false;
        IsCurrent = false;
    }

    public void Activate() => IsActive = true;
}

/// <summary>
/// Grade / class level owned by a school in the Organization module (<c>organization.grade_levels</c>, DOC-43 §5.4).
/// </summary>
public sealed class GradeLevel : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    private GradeLevel()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private GradeLevel(
        Guid tenantId,
        Guid schoolId,
        string code,
        string name,
        int sequenceOrder) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (sequenceOrder < 0) throw new ArgumentOutOfRangeException(nameof(sequenceOrder), "Sequence order must be non-negative.");

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        SequenceOrder = sequenceOrder;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public int SequenceOrder { get; private set; }
    public bool IsActive { get; private set; }

    public static GradeLevel Create(Guid tenantId, Guid schoolId, string code, string name, int sequenceOrder)
        => new(tenantId, schoolId, code, name, sequenceOrder);

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

/// <summary>
/// Configurable campus section for a specific academic year and grade level (<c>organization.sections</c>, DOC-43 §5.4).
/// </summary>
public sealed class Section : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private Section()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private Section(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid academicYearId,
        Guid gradeLevelId,
        string code,
        string name,
        int? capacity) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (academicYearId == Guid.Empty) throw new ArgumentException("Academic year ID cannot be empty.", nameof(academicYearId));
        if (gradeLevelId == Guid.Empty) throw new ArgumentException("Grade level ID cannot be empty.", nameof(gradeLevelId));
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (capacity is <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Section capacity must be positive when specified.");

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        AcademicYearId = academicYearId;
        GradeLevelId = gradeLevelId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Capacity = capacity;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid AcademicYearId { get; private set; }
    public Guid GradeLevelId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public int? Capacity { get; private set; }
    public bool IsActive { get; private set; }

    public static Section Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid academicYearId,
        Guid gradeLevelId,
        string code,
        string name,
        int? capacity = null)
        => new(tenantId, schoolId, campusId, academicYearId, gradeLevelId, code, name, capacity);

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
