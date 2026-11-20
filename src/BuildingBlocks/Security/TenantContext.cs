using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// Scoped TenantContext implementation resolving multi-tenant and campus identity
/// strictly from authenticated ClaimsPrincipal claims.
/// Client-supplied headers, query parameters, or body values are never trusted.
/// </summary>
public class TenantContext : ITenantContext
{
    public const string TenantIdClaimType = "tenant_id";
    public const string CampusIdClaimType = "campus_id";
    public const string ScopeClaimType = "scope";
    public const string PermissionClaimType = "perms";
    public const string SessionIdClaimType = "sid";
    public const string CredentialVersionClaimType = "cv";
    public const string MustChangePasswordClaimType = "pwd_change_required";

    private readonly HashSet<string> _scopes = new(StringComparer.OrdinalIgnoreCase);

    public Guid? TenantId { get; private set; }
    public Guid? CampusId { get; }
    public string? UserId { get; private set; }
    public Guid? SessionId { get; }
    public int CredentialVersion { get; } = 1;
    public bool MustChangePassword { get; }
    public bool IsAuthenticated { get; private set; }
    public bool IsPlatformAdmin { get; private set; }

    /// <summary>
    /// Resolves TenantContext from ambient IHttpContextAccessor.
    /// Fails closed if HttpContext, User, or Identity is missing or unauthenticated.
    /// </summary>
    public TenantContext(IHttpContextAccessor httpContextAccessor)
        : this(httpContextAccessor.HttpContext?.User)
    {
    }

    /// <summary>
    /// Resolves TenantContext from an explicit ClaimsPrincipal.
    /// Used for testing, middleware, and background queue workers.
    /// </summary>
    public TenantContext(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not { IsAuthenticated: true })
        {
            IsAuthenticated = false;
            TenantId = null;
            CampusId = null;
            UserId = null;
            return;
        }

        IsAuthenticated = true;

        // Resolve User ID from standard OIDC 'sub' or WS-Federation NameIdentifier
        UserId = principal.FindFirst("sub")?.Value 
                 ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Resolve Tenant ID: must be a syntactically valid Guid
        var tenantClaim = principal.FindFirst(TenantIdClaimType)?.Value;
        if (Guid.TryParse(tenantClaim, out var tenantId))
        {
            TenantId = tenantId;
        }
        else
        {
            // Fail closed: authenticated account without tenant scope has null TenantId
            TenantId = null;
        }

        // Resolve Campus ID: optional authorized campus scope
        var campusClaim = principal.FindFirst(CampusIdClaimType)?.Value;
        if (Guid.TryParse(campusClaim, out var campusId))
        {
            CampusId = campusId;
        }

        // Cache scopes, permissions, and roles for authorization evaluation
        foreach (var claim in principal.FindAll(ScopeClaimType))
        {
            _scopes.Add(claim.Value);
        }

        foreach (var claim in principal.FindAll(PermissionClaimType))
        {
            _scopes.Add(claim.Value);
        }

        foreach (var claim in principal.FindAll(ClaimTypes.Role))
        {
            _scopes.Add($"role:{claim.Value}");
        }

        foreach (var claim in principal.FindAll("role"))
        {
            _scopes.Add($"role:{claim.Value}");
        }

        var sidClaim = principal.FindFirst(SessionIdClaimType)?.Value;
        if (Guid.TryParse(sidClaim, out var sessionId))
        {
            SessionId = sessionId;
        }

        var cvClaim = principal.FindFirst(CredentialVersionClaimType)?.Value;
        if (int.TryParse(cvClaim, out var cv) && cv > 0)
        {
            CredentialVersion = cv;
        }

        MustChangePassword = string.Equals(
            principal.FindFirst(MustChangePasswordClaimType)?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase);

        IsPlatformAdmin =
            _scopes.Contains($"role:{SecurityPolicies.PlatformAdminRole}") ||
            _scopes.Contains(SecurityPolicies.PlatformAdminScope);
    }

    /// <summary>
    /// Constructor for explicit, verified server-side background contexts (e.g. migrations, sync daemon).
    /// </summary>
    public TenantContext(
        Guid? tenantId,
        Guid? campusId = null,
        string? userId = null,
        IEnumerable<string>? scopes = null,
        bool isPlatformAdmin = false,
        int credentialVersion = 1,
        Guid? sessionId = null,
        bool mustChangePassword = false)
    {
        TenantId = tenantId;
        CampusId = campusId;
        UserId = userId;
        SessionId = sessionId;
        CredentialVersion = credentialVersion > 0 ? credentialVersion : 1;
        MustChangePassword = mustChangePassword;
        IsAuthenticated = userId is not null || tenantId is not null || isPlatformAdmin;

        if (scopes is not null)
        {
            foreach (var s in scopes)
            {
                _scopes.Add(s);
            }
        }

        IsPlatformAdmin = isPlatformAdmin ||
                          _scopes.Contains($"role:{SecurityPolicies.PlatformAdminRole}") ||
                          _scopes.Contains(SecurityPolicies.PlatformAdminScope);
    }

    public bool HasScope(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        return _scopes.Contains(scope);
    }

    /// <summary>
    /// Binds an unauthenticated, tenant-less request context to a specific target tenant during
    /// pre-authentication workflows (<c>/auth/login</c>, <c>/auth/refresh</c>) so EF Core query filters,
    /// PostgreSQL RLS session interceptors, and audit attribution stay strictly scoped and fail-closed.
    /// Refuses to overwrite an already authenticated or tenant-bound context.
    /// </summary>
    public void EnterPreAuthenticationTenantScope(Guid tenantId, string systemActorId)
    {
        if (IsAuthenticated || TenantId.HasValue)
        {
            throw new InvalidOperationException("Cannot override an already-authenticated or tenant-bound TenantContext.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(systemActorId);
        if (!systemActorId.StartsWith("system:", StringComparison.Ordinal))
        {
            throw new ArgumentException("Pre-authentication scope requires an explicit 'system:' actor identifier.", nameof(systemActorId));
        }

        TenantId = tenantId;
        UserId = systemActorId;
    }

    /// <summary>
    /// Attaches an explicit anonymous/system attribution identifier (e.g. <c>public:school-request</c>) on
    /// unauthenticated endpoints without forging authentication or tenant scope.
    /// </summary>
    public void EnterPublicActorScope(string actorId)
    {
        if (IsAuthenticated || TenantId.HasValue || !string.IsNullOrWhiteSpace(UserId))
        {
            throw new InvalidOperationException("Cannot set public actor scope on an initialized TenantContext.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        if (!actorId.StartsWith("public:", StringComparison.Ordinal) &&
            !actorId.StartsWith("system:", StringComparison.Ordinal))
        {
            throw new ArgumentException("Actor identifier must start with 'public:' or 'system:'.", nameof(actorId));
        }

        UserId = actorId;
    }

    /// <summary>
    /// Initializes an internal background-worker context for automated Control Plane jobs (such as subscription
    /// lifecycle sweeps) where no HTTP user exists, permitting subsequent <see cref="BeginPlatformTenantScope"/> calls.
    /// </summary>
    public void EnterSystemJobScope(string systemActorId, bool isPlatformWorker = false)
    {
        if (IsAuthenticated || TenantId.HasValue || !string.IsNullOrWhiteSpace(UserId))
        {
            throw new InvalidOperationException("Cannot initialize a system job scope on an already initialized TenantContext.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(systemActorId);
        if (!systemActorId.StartsWith("system:", StringComparison.Ordinal))
        {
            throw new ArgumentException("System job scope requires an explicit 'system:' actor identifier.", nameof(systemActorId));
        }

        UserId = systemActorId;
        IsAuthenticated = true;
        IsPlatformAdmin = isPlatformWorker;
    }

    /// <summary>
    /// Temporarily scopes an authenticated Cloud <c>PlatformAdmin</c> context (with no ambient tenant)
    /// to operate on a specific target tenant database record while keeping EF query filters and PostgreSQL RLS intact.
    /// </summary>
    public IDisposable BeginPlatformTenantScope(Guid targetTenantId)
    {
        if (!IsPlatformAdmin)
        {
            throw new UnauthorizedAccessException("Only PlatformAdmin contexts may enter a platform tenant operation scope.");
        }

        if (targetTenantId == Guid.Empty)
        {
            throw new ArgumentException("Target tenant ID cannot be empty.", nameof(targetTenantId));
        }

        if (TenantId.HasValue && TenantId.Value != targetTenantId)
        {
            throw new InvalidOperationException("Cannot nest a different tenant scope inside an already tenant-bound context.");
        }

        var previousTenantId = TenantId;
        TenantId = targetTenantId;
        return new ScopeReset(this, previousTenantId);
    }

    private sealed class ScopeReset(TenantContext owner, Guid? previousTenantId) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            owner.TenantId = previousTenantId;
            _disposed = true;
        }
    }
}
