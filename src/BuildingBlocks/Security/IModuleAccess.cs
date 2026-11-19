using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// Evaluates whether a tenant is currently entitled to use a given <see cref="ModuleCode"/>.
/// On the Cloud Control Plane, backed by active subscription/trial state; on the Local Data Plane,
/// backed by offline-verified cryptographic license lease or default core entitlements.
/// </summary>
public interface IModuleAccess
{
    ValueTask<bool> HasModuleAsync(Guid tenantId, ModuleCode module, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlySet<ModuleCode>> GetEnabledModulesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default module access implementation enabling Core modules (and Paid modules unless restricted by an active lease store).
/// Replaced or decorated by the Subscription / Licensing engine in Wave 3.
/// </summary>
public sealed class DefaultCoreModuleAccess : IModuleAccess
{
    private static readonly IReadOnlySet<ModuleCode> AllModules =
        Enum.GetValues<ModuleCode>().Where(m => m != ModuleCode.None).ToHashSet();

    public ValueTask<bool> HasModuleAsync(Guid tenantId, ModuleCode module, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(module == ModuleCode.None || AllModules.Contains(module));
    }

    public ValueTask<IReadOnlySet<ModuleCode>> GetEnabledModulesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return ValueTask.FromResult<IReadOnlySet<ModuleCode>>(new HashSet<ModuleCode>());
        }

        return ValueTask.FromResult(AllModules);
    }
}

/// <summary>
/// Minimal API endpoint filter enforcing:
/// 1. Authenticated tenant scope (<c>401</c> / <c>403</c>)
/// 2. Mandatory one-time password change enforcement (<c>403 PASSWORD_CHANGE_REQUIRED</c>)
/// 3. Tenant module subscription/license entitlement (<c>403 MODULE_NOT_ENABLED</c>)
/// 4. Scoped RBAC module CRUD permission (<c>403 PERMISSION_DENIED</c>)
/// </summary>
public sealed class RequireModulePermissionEndpointFilter : IEndpointFilter
{
    private readonly ModuleCode _module;
    private readonly PermissionAction _action;
    private readonly bool _allowWhilePasswordChangeRequired;

    public RequireModulePermissionEndpointFilter(
        ModuleCode module,
        PermissionAction action,
        bool allowWhilePasswordChangeRequired = false)
    {
        _module = module;
        _action = action;
        _allowWhilePasswordChangeRequired = allowWhilePasswordChangeRequired;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var tenantContext = httpContext.RequestServices.GetRequiredService<ITenantContext>();

        if (!tenantContext.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        if (!_allowWhilePasswordChangeRequired && tenantContext.MustChangePassword)
        {
            return Results.Problem(
                title: "Password change required",
                detail: "You must change your one-time password before accessing operational endpoints.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["code"] = "PASSWORD_CHANGE_REQUIRED" });
        }

        if (tenantContext.TenantId is null || tenantContext.TenantId == Guid.Empty)
        {
            return Results.Problem(
                title: "Tenant context required",
                detail: "An authenticated tenant context is required for this endpoint.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["code"] = "TENANT_REQUIRED" });
        }

        if (_module != ModuleCode.None)
        {
            var moduleAccess = httpContext.RequestServices.GetRequiredService<IModuleAccess>();
            var hasModule = await moduleAccess.HasModuleAsync(
                tenantContext.TenantId.Value,
                _module,
                httpContext.RequestAborted);

            if (!hasModule)
            {
                return Results.Problem(
                    title: "Module not enabled",
                    detail: $"Module '{_module}' is not enabled for the current tenant subscription.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "MODULE_NOT_ENABLED",
                        ["module"] = _module.ToString()
                    });
            }
        }

        if (!HasPermission(tenantContext, _module, _action))
        {
            return Results.Problem(
                title: "Permission denied",
                detail: $"Missing required permission '{_module.ToPermissionScope(_action)}'.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "PERMISSION_DENIED",
                    ["permission"] = _module.ToPermissionScope(_action)
                });
        }

        return await next(context);
    }

    private static bool HasPermission(ITenantContext tenantContext, ModuleCode module, PermissionAction action)
    {
        if (module == ModuleCode.None)
        {
            return true;
        }

        // TenantAdmin, SchoolAdmin, and Principal carry full administrative access within their tenant/school scope.
        if (tenantContext.HasScope($"role:{SecurityPolicies.TenantAdminRole}") ||
            tenantContext.HasScope($"role:{SecurityPolicies.SchoolAdminRole}") ||
            tenantContext.HasScope($"role:{SecurityPolicies.PrincipalRole}"))
        {
            return true;
        }

        var requiredScope = module.ToPermissionScope(action);
        if (tenantContext.HasScope(requiredScope))
        {
            return true;
        }

        // View permission is implicitly satisfied if the user holds Create, Edit, or Delete on the same module.
        if (action == PermissionAction.View)
        {
            return tenantContext.HasScope(module.ToPermissionScope(PermissionAction.Create)) ||
                   tenantContext.HasScope(module.ToPermissionScope(PermissionAction.Edit)) ||
                   tenantContext.HasScope(module.ToPermissionScope(PermissionAction.Delete));
        }

        return false;
    }
}

public static class ModulePermissionEndpointExtensions
{
    public static TBuilder RequireModulePermission<TBuilder>(
        this TBuilder builder,
        ModuleCode module,
        PermissionAction action = PermissionAction.View,
        bool allowWhilePasswordChangeRequired = false)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter(
            new RequireModulePermissionEndpointFilter(module, action, allowWhilePasswordChangeRequired));
    }
}
