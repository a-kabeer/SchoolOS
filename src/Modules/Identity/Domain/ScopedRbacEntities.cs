using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Identity.Domain;

/// <summary>
/// Custom or standard school role defining per-module CRUD rights (<see cref="RolePermission"/>).
/// Ported from Education-Portal <c>SchoolRole</c> + DOC-63 §4.1.
/// </summary>
public class SchoolRole : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    public const int NameMaxLength = 100;
    private readonly List<RolePermission> _permissions = [];

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public bool IsSystemRole { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    protected SchoolRole() { }

    private SchoolRole(Guid id, Guid tenantId, Guid schoolId, string name, bool isSystemRole)
        : base(id)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));

        TenantId = tenantId;
        SchoolId = schoolId;
        IsSystemRole = isSystemRole;
        Rename(name);
    }

    public static SchoolRole Create(Guid tenantId, Guid schoolId, string name, bool isSystemRole = false) =>
        new(Guid.CreateVersion7(), tenantId, schoolId, name, isSystemRole);

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, NameMaxLength, nameof(name));

        Name = trimmed;
        Code = trimmed.ToUpperInvariant().Replace(' ', '_');
    }

    /// <summary>
    /// Sets or removes CRUD permissions for a module on this role.
    /// Enforces Education-Portal's invariant: creating, editing, and deleting require viewing.
    /// If all flags are false (<see cref="RolePermission.IsEmpty"/>), the row is omitted.
    /// </summary>
    public RolePermission? SetModulePermission(
        ModuleCode module,
        bool canView,
        bool canCreate,
        bool canEdit,
        bool canDelete)
    {
        if (module == ModuleCode.None)
        {
            throw new ArgumentException("ModuleCode.None cannot be granted permissions.", nameof(module));
        }

        var existing = _permissions.FirstOrDefault(p => p.Module == module);
        if (!canView && !canCreate && !canEdit && !canDelete)
        {
            if (existing is not null)
            {
                _permissions.Remove(existing);
            }
            return null;
        }

        if (existing is not null)
        {
            existing.Set(canView, canCreate, canEdit, canDelete);
            return existing;
        }

        var created = RolePermission.Create(this, module, canView, canCreate, canEdit, canDelete);
        _permissions.Add(created);
        return created;
    }

    public IReadOnlyList<string> GetEffectivePermissionScopes(IReadOnlySet<ModuleCode>? enabledModules = null)
    {
        var scopes = new List<string>();
        foreach (var perm in _permissions)
        {
            if (enabledModules is not null && !enabledModules.Contains(perm.Module))
            {
                continue;
            }

            scopes.AddRange(perm.ToScopeStrings());
        }
        return scopes;
    }
}

/// <summary>
/// What a <see cref="SchoolRole"/> may do in one <see cref="ModuleCode"/>: view, create, edit, and delete.
/// Creating, editing, and deleting require the right to view (ported from Education-Portal RolePermission).
/// </summary>
public class RolePermission : Entity<Guid>, ITenantScopedEntity
{
    public Guid TenantId { get; private set; }
    public Guid SchoolRoleId { get; private set; }
    public ModuleCode Module { get; private set; }
    public bool CanView { get; private set; }
    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanDelete { get; private set; }

    public bool IsEmpty => !CanView;

    protected RolePermission() { }

    private RolePermission(
        Guid id,
        Guid tenantId,
        Guid schoolRoleId,
        ModuleCode module,
        bool canView,
        bool canCreate,
        bool canEdit,
        bool canDelete)
        : base(id)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolRoleId == Guid.Empty) throw new ArgumentException("SchoolRole ID cannot be empty.", nameof(schoolRoleId));
        if (module == ModuleCode.None || !Enum.IsDefined(module))
        {
            throw new ArgumentOutOfRangeException(nameof(module));
        }

        TenantId = tenantId;
        SchoolRoleId = schoolRoleId;
        Module = module;
        Set(canView, canCreate, canEdit, canDelete);
    }

    public static RolePermission Create(
        SchoolRole role,
        ModuleCode module,
        bool canView,
        bool canCreate,
        bool canEdit,
        bool canDelete)
    {
        ArgumentNullException.ThrowIfNull(role);
        return new RolePermission(
            Guid.CreateVersion7(),
            role.TenantId,
            role.Id,
            module,
            canView,
            canCreate,
            canEdit,
            canDelete);
    }

    public void Set(bool canView, bool canCreate, bool canEdit, bool canDelete)
    {
        if (!canView && (canCreate || canEdit || canDelete))
        {
            throw new ArgumentException("Creating, editing and deleting need the right to view.");
        }

        CanView = canView;
        CanCreate = canCreate;
        CanEdit = canEdit;
        CanDelete = canDelete;
    }

    public IEnumerable<string> ToScopeStrings()
    {
        if (CanView) yield return Module.ToPermissionScope(PermissionAction.View);
        if (CanCreate) yield return Module.ToPermissionScope(PermissionAction.Create);
        if (CanEdit) yield return Module.ToPermissionScope(PermissionAction.Edit);
        if (CanDelete) yield return Module.ToPermissionScope(PermissionAction.Delete);
    }
}

/// <summary>
/// Links a user to an authorized campus within the same tenant and school (ported from Education-Portal UserCampus + DOC-63 §3.1).
/// </summary>
public class UserCampusAssignment : Entity<Guid>, ICampusScopedEntity
{
    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CampusId { get; private set; }
    public bool IsPrimary { get; private set; }

    protected UserCampusAssignment() { }

    private UserCampusAssignment(Guid id, Guid tenantId, Guid schoolId, Guid userId, Guid campusId, bool isPrimary)
        : base(id)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));

        TenantId = tenantId;
        SchoolId = schoolId;
        UserId = userId;
        CampusId = campusId;
        IsPrimary = isPrimary;
    }

    public static UserCampusAssignment Create(
        Guid tenantId,
        Guid schoolId,
        Guid userId,
        Guid campusId,
        bool isPrimary = false) =>
        new(Guid.CreateVersion7(), tenantId, schoolId, userId, campusId, isPrimary);
}

/// <summary>
/// Grants a <see cref="SchoolRole"/> to a <see cref="User"/> either tenant/school-wide (<see cref="CampusId"/> is null)
/// or scoped to a single campus (<see cref="CampusId"/> is non-null).
/// </summary>
public class UserRoleAssignment : Entity<Guid>, ITenantScopedEntity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SchoolRoleId { get; private set; }
    public Guid? CampusId { get; private set; }

    protected UserRoleAssignment() { }

    private UserRoleAssignment(Guid id, Guid tenantId, Guid userId, Guid schoolRoleId, Guid? campusId)
        : base(id)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (schoolRoleId == Guid.Empty) throw new ArgumentException("SchoolRole ID cannot be empty.", nameof(schoolRoleId));
        if (campusId.HasValue && campusId.Value == Guid.Empty)
        {
            throw new ArgumentException("Campus ID cannot be an empty GUID.", nameof(campusId));
        }

        TenantId = tenantId;
        UserId = userId;
        SchoolRoleId = schoolRoleId;
        CampusId = campusId;
    }

    public static UserRoleAssignment Create(
        Guid tenantId,
        Guid userId,
        Guid schoolRoleId,
        Guid? campusId = null) =>
        new(Guid.CreateVersion7(), tenantId, userId, schoolRoleId, campusId);
}
