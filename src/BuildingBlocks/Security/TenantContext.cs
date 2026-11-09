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

    private readonly HashSet<string> _scopes = new(StringComparer.OrdinalIgnoreCase);

    public Guid? TenantId { get; }
    public Guid? CampusId { get; }
    public string? UserId { get; }
    public bool IsAuthenticated { get; }

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

        // Cache scopes and roles for authorization evaluation
        foreach (var claim in principal.FindAll(ScopeClaimType))
        {
            _scopes.Add(claim.Value);
        }

        foreach (var claim in principal.FindAll(ClaimTypes.Role))
        {
            _scopes.Add($"role:{claim.Value}");
        }
    }

    /// <summary>
    /// Constructor for explicit, verified server-side background contexts (e.g. migrations, sync daemon).
    /// </summary>
    public TenantContext(Guid? tenantId, Guid? campusId = null, string? userId = null, IEnumerable<string>? scopes = null)
    {
        TenantId = tenantId;
        CampusId = campusId;
        UserId = userId;
        IsAuthenticated = userId is not null || tenantId is not null;

        if (scopes is not null)
        {
            foreach (var s in scopes)
            {
                _scopes.Add(s);
            }
        }
    }

    public bool HasScope(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        return _scopes.Contains(scope);
    }
}
