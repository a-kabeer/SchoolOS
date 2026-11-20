using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Organization.Domain;

/// <summary>
/// Represents an educational institution or legal entity within a customer tenant.
/// A Tenant can operate multiple Schools (e.g. Grammar School, High School, College).
/// </summary>
public class School : Entity<Guid>, ITenantScopedEntity
{
    private readonly List<Campus> _campuses = [];

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<Campus> Campuses => _campuses.AsReadOnly();

    protected School() { }

    private School(Guid id, Guid tenantId, string name, string code)
        : base(id)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        TenantId = tenantId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        IsActive = true;
    }

    /// <summary>
    /// Factory method generating a new School with sequential UUIDv7 identifier.
    /// </summary>
    public static School Create(Guid tenantId, string name, string code)
    {
        return new School(Guid.CreateVersion7(), tenantId, name, code);
    }

    public void Update(string name, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Factory helper to attach a new Campus ensuring strict tenant boundary alignment.
    /// </summary>
    public Campus AddCampus(string name, string code, string? address = null)
    {
        var campus = Campus.Create(TenantId, Id, name, code, address);
        _campuses.Add(campus);
        return campus;
    }
}
