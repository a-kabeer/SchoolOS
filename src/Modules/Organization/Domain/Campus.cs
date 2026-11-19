using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Organization.Domain;

/// <summary>
/// Represents a physical branch or campus of a School.
/// A Campus strictly belongs to one School and carries the same TenantId.
/// </summary>
public class Campus : Entity<Guid>, ICampusScopedEntity
{
    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation property
    public School? School { get; private set; }

    public Guid CampusId => Id;

    protected Campus() { }

    private Campus(Guid id, Guid tenantId, Guid schoolId, string name, string code, string? address)
        : base(id)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        if (schoolId == Guid.Empty)
        {
            throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        TenantId = tenantId;
        SchoolId = schoolId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Address = address?.Trim();
        IsActive = true;
    }

    /// <summary>
    /// Factory method generating a new Campus with sequential UUIDv7 identifier.
    /// </summary>
    public static Campus Create(Guid tenantId, Guid schoolId, string name, string code, string? address = null)
    {
        return new Campus(Guid.CreateVersion7(), tenantId, schoolId, name, code, address);
    }

    /// <summary>
    /// Factory method validating that Campus tenant strictly aligns with the parent School's tenant.
    /// </summary>
    public static Campus CreateForSchool(School school, string name, string code, string? address = null)
    {
        ArgumentNullException.ThrowIfNull(school);
        return new Campus(Guid.CreateVersion7(), school.TenantId, school.Id, name, code, address);
    }

    public void Update(string name, string code, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Address = address?.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
