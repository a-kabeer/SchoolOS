using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Modules.Identity.Application;
using SchoolOS.Modules.Identity.Domain;
using SchoolOS.Modules.Identity.Persistence;

namespace SchoolOS.Modules.Identity.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        authGroup.MapPost("/login", async (
            [FromBody] LoginRequest request,
            HttpContext httpContext,
            [FromServices] IdentityAuthenticationService authService,
            CancellationToken ct) =>
        {
            var result = await authService.SignInAsync(
                request.Email,
                request.Password,
                request.CampusId,
                httpContext.Connection.RemoteIpAddress?.ToString(),
                httpContext.Request.Headers.UserAgent.ToString(),
                ct,
                request.TenantId);

            if (result.Outcome != SignInOutcome.Succeeded || result.AccessToken is null || result.User is null)
            {
                var status = result.Outcome == SignInOutcome.CampusAssignmentRequired
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status401Unauthorized;

                return Results.Problem(
                    title: "Authentication failed",
                    detail: $"Sign-in could not be completed ({result.ErrorCode}).",
                    statusCode: status,
                    extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode ?? "AUTH_FAILED" });
            }

            if (!string.IsNullOrEmpty(result.RawRefreshToken))
            {
                AppendRefreshCookie(httpContext, result.RawRefreshToken);
            }

            return Results.Ok(new LoginResponse(result.AccessToken, result.ExpiresInSeconds, result.User));
        }).RequireRateLimiting(RateLimitPolicies.SignIn);

        authGroup.MapPost("/refresh", async (
            HttpContext httpContext,
            [FromBody] RefreshTokenRequest? bodyRequest,
            [FromServices] IdentityAuthenticationService authService,
            CancellationToken ct) =>
        {
            var rawToken = httpContext.Request.Cookies[IdentityAuthenticationService.RefreshCookieName]
                           ?? bodyRequest?.RefreshToken
                           ?? string.Empty;

            var result = await authService.RefreshSessionAsync(rawToken, ct);
            if (result.Outcome != SignInOutcome.Succeeded || result.AccessToken is null || result.User is null)
            {
                httpContext.Response.Cookies.Delete(IdentityAuthenticationService.RefreshCookieName);
                return Results.Problem(
                    title: "Session refresh failed",
                    detail: $"Refresh token rejected ({result.ErrorCode}).",
                    statusCode: StatusCodes.Status401Unauthorized,
                    extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode ?? "REFRESH_FAILED" });
            }

            if (!string.IsNullOrEmpty(result.RawRefreshToken))
            {
                AppendRefreshCookie(httpContext, result.RawRefreshToken);
            }

            return Results.Ok(new LoginResponse(result.AccessToken, result.ExpiresInSeconds, result.User));
        });

        authGroup.MapPost("/logout", async (
            HttpContext httpContext,
            [FromServices] ITenantContext tenantContext,
            [FromServices] IdentityAuthenticationService authService,
            CancellationToken ct) =>
        {
            if (!tenantContext.IsAuthenticated || !Guid.TryParse(tenantContext.UserId, out var userId))
            {
                return Results.Unauthorized();
            }

            var rawToken = httpContext.Request.Cookies[IdentityAuthenticationService.RefreshCookieName];
            await authService.SignOutAsync(userId, tenantContext.SessionId, rawToken, ct);
            httpContext.Response.Cookies.Delete(IdentityAuthenticationService.RefreshCookieName);
            return Results.NoContent();
        }).RequireAuthorization(SecurityPolicies.TenantScopedPolicy);

        authGroup.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest request,
            [FromServices] ITenantContext tenantContext,
            [FromServices] IdentityAuthenticationService authService,
            CancellationToken ct) =>
        {
            if (!tenantContext.IsAuthenticated || !Guid.TryParse(tenantContext.UserId, out var userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                await authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);
                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { code = "PASSWORD_POLICY_VIOLATION", message = ex.Message });
            }
        }).RequireAuthorization(SecurityPolicies.TenantScopedPolicy);

        authGroup.MapPost("/switch-context", async (
            [FromBody] SwitchCampusContextRequest request,
            [FromServices] ITenantContext tenantContext,
            [FromServices] IdentityAuthenticationService authService,
            CancellationToken ct) =>
        {
            if (!tenantContext.IsAuthenticated || !Guid.TryParse(tenantContext.UserId, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await authService.SwitchCampusContextAsync(
                userId,
                tenantContext.SessionId,
                request.TargetCampusId,
                ct);

            if (result.Outcome != SignInOutcome.Succeeded || result.AccessToken is null || result.User is null)
            {
                return Results.Problem(
                    title: "Campus switch denied",
                    detail: "User does not hold an active assignment for the target campus.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode ?? "CAMPUS_SWITCH_DENIED" });
            }

            return Results.Ok(new SwitchCampusContextResponse(
                result.AccessToken,
                result.ExpiresInSeconds,
                request.TargetCampusId,
                result.User.Roles,
                result.User.Permissions));
        }).RequireAuthorization(SecurityPolicies.TenantScopedPolicy);

        authGroup.MapPost("/offline-reset-voucher", async (
            [FromBody] OfflineResetVoucherRequest request,
            [FromServices] ITenantContext tenantContext,
            [FromServices] IdentityAuthenticationService authService,
            CancellationToken ct) =>
        {
            if (!tenantContext.IsAuthenticated || !Guid.TryParse(tenantContext.UserId, out var actorId))
            {
                return Results.Unauthorized();
            }

            var voucher = await authService.IssueOfflineResetVoucherAsync(actorId, request.TargetUserId, ct);
            return Results.Ok(voucher);
        }).RequireAuthorization(SecurityPolicies.OrganizationAdminPolicy);

        // =====================================================================
        // Scoped RBAC Roles & Users Endpoints (/api/v1/identity)
        // =====================================================================
        var identityGroup = app.MapGroup("/api/v1/identity")
            .RequireAuthorization(SecurityPolicies.TenantScopedPolicy)
            .WithTags("Identity & Scoped RBAC");

        identityGroup.MapGet("/roles", async (
            [FromServices] IdentityDbContext db,
            CancellationToken ct) =>
        {
            var roles = await db.SchoolRoles
                .AsNoTracking()
                .Include(r => r.Permissions)
                .OrderBy(r => r.Name)
                .Select(r => new SchoolRoleResponse(
                    r.Id,
                    r.TenantId,
                    r.SchoolId,
                    r.Name,
                    r.Code,
                    r.IsSystemRole,
                    r.Permissions.Select(p => new RolePermissionDto(
                        p.Module,
                        p.CanView,
                        p.CanCreate,
                        p.CanEdit,
                        p.CanDelete)).ToList()))
                .ToListAsync(ct);

            return Results.Ok(roles);
        }).RequireModulePermission(ModuleCode.UsersRoles, PermissionAction.View);

        identityGroup.MapPost("/roles", async (
            [FromBody] CreateSchoolRoleRequest request,
            [FromServices] IdentityDbContext db,
            [FromServices] ITenantContext tenantContext,
            [FromServices] IModuleAccess moduleAccess,
            [FromServices] ISecurityAuditLog auditLog,
            CancellationToken ct) =>
        {
            var tenantId = tenantContext.TenantId!.Value;
            var trimmedName = request.Name.Trim();
            var exists = await db.SchoolRoles.AnyAsync(
                r => r.SchoolId == request.SchoolId && r.Name == trimmedName,
                ct);

            if (exists)
            {
                return Results.Conflict(new { code = "ROLE_NAME_TAKEN", message = $"Role '{trimmedName}' already exists." });
            }

            var enabledModules = await moduleAccess.GetEnabledModulesAsync(tenantId, ct);
            var role = SchoolRole.Create(tenantId, request.SchoolId, trimmedName);

            if (request.Permissions is not null)
            {
                foreach (var perm in request.Permissions)
                {
                    if (!enabledModules.Contains(perm.Module))
                    {
                        return Results.Problem(
                            title: "Module not enabled",
                            detail: $"Cannot assign permissions for disabled module '{perm.Module}'.",
                            statusCode: StatusCodes.Status403Forbidden,
                            extensions: new Dictionary<string, object?> { ["code"] = "MODULE_NOT_ENABLED" });
                    }

                    role.SetModulePermission(perm.Module, perm.CanView, perm.CanCreate, perm.CanEdit, perm.CanDelete);
                }
            }

            db.SchoolRoles.Add(role);
            await db.SaveChangesAsync(ct);

            await auditLog.WriteAsync(
                SecurityAuditEventType.RoleCreated,
                SecurityAuditOutcome.Success,
                targetType: nameof(SchoolRole),
                targetId: role.Id.ToString(),
                cancellationToken: ct);

            var response = new SchoolRoleResponse(
                role.Id,
                role.TenantId,
                role.SchoolId,
                role.Name,
                role.Code,
                role.IsSystemRole,
                role.Permissions.Select(p => new RolePermissionDto(
                    p.Module,
                    p.CanView,
                    p.CanCreate,
                    p.CanEdit,
                    p.CanDelete)).ToList());

            return Results.Created($"/api/v1/identity/roles/{role.Id}", response);
        }).RequireModulePermission(ModuleCode.UsersRoles, PermissionAction.Create);

        identityGroup.MapGet("/users", async (
            [FromQuery] Guid? campusId,
            [FromServices] IdentityDbContext db,
            [FromServices] ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            var query = db.Users.AsNoTracking().Where(u => !u.IsDeleted);
            var effectiveCampusId = tenantContext.CampusId ?? campusId;

            if (effectiveCampusId.HasValue)
            {
                var assignedUserIds = db.UserCampusAssignments
                    .Where(a => a.CampusId == effectiveCampusId.Value)
                    .Select(a => a.UserId);
                query = query.Where(u => assignedUserIds.Contains(u.Id));
            }

            var users = await query
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new UserDirectoryItemResponse(
                    u.Id,
                    u.TenantId,
                    u.SchoolId,
                    u.Email,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.UserType,
                    u.PhoneNumber,
                    u.Status,
                    u.PreferredLanguage))
                .ToListAsync(ct);

            return Results.Ok(users);
        }).RequireModulePermission(ModuleCode.UsersRoles, PermissionAction.View);

        identityGroup.MapPost("/users", async (
            [FromBody] ProvisionUserRequest request,
            [FromServices] IdentityDbContext db,
            [FromServices] ITenantContext tenantContext,
            [FromServices] IdentityAuthenticationService authService,
            CancellationToken ct) =>
        {
            var tenantId = tenantContext.TenantId!.Value;
            try
            {
                var (user, _) = await authService.ProvisionUserAsync(
                    tenantId,
                    request.SchoolId,
                    request.Email,
                    request.FirstName,
                    request.MiddleName,
                    request.LastName,
                    request.UserType,
                    request.InitialPassword,
                    request.MustChangePasswordOnNextLogin,
                    request.PhoneNumber,
                    request.PreferredLanguage ?? "en",
                    ct);

                if (request.CampusIds is { Count: > 0 } && request.SchoolId.HasValue)
                {
                    var isFirst = true;
                    foreach (var cid in request.CampusIds.Distinct())
                    {
                        db.UserCampusAssignments.Add(
                            UserCampusAssignment.Create(tenantId, request.SchoolId.Value, user.Id, cid, isFirst));
                        isFirst = false;
                    }
                    await db.SaveChangesAsync(ct);
                }

                if (request.RoleIds is { Count: > 0 })
                {
                    Guid? scopedCampusId = request.CampusIds is { Count: 1 } ? request.CampusIds[0] : null;
                    foreach (var roleId in request.RoleIds.Distinct())
                    {
                        db.UserRoleAssignments.Add(
                            UserRoleAssignment.Create(tenantId, user.Id, roleId, scopedCampusId));
                    }
                    await db.SaveChangesAsync(ct);
                }

                var dto = new UserDirectoryItemResponse(
                    user.Id,
                    user.TenantId,
                    user.SchoolId,
                    user.Email,
                    user.FirstName,
                    user.MiddleName,
                    user.LastName,
                    user.UserType,
                    user.PhoneNumber,
                    user.Status,
                    user.PreferredLanguage);

                return Results.Created($"/api/v1/identity/users/{user.Id}", dto);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { code = "INVALID_USER_INPUT", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "USER_EMAIL_CONFLICT", message = ex.Message });
            }
        }).RequireModulePermission(ModuleCode.UsersRoles, PermissionAction.Create);

        return app;
    }

    private static void AppendRefreshCookie(HttpContext httpContext, string rawRefreshToken)
    {
        httpContext.Response.Cookies.Append(
            IdentityAuthenticationService.RefreshCookieName,
            rawRefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/auth/refresh",
                MaxAge = IdentityAuthenticationService.LocalRefreshTokenLifetime
            });
    }
}

public sealed record LoginRequest(string Email, string Password, Guid? CampusId = null, Guid? TenantId = null);
public sealed record RefreshTokenRequest(string? RefreshToken = null);
public sealed record LoginResponse(string AccessToken, int ExpiresIn, AuthenticatedUserSummary User);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record SwitchCampusContextRequest(Guid TargetCampusId);
public sealed record SwitchCampusContextResponse(
    string AccessToken,
    int ExpiresIn,
    Guid ActiveCampusId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
public sealed record OfflineResetVoucherRequest(Guid TargetUserId);
public sealed record RolePermissionDto(
    ModuleCode Module,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete);
public sealed record CreateSchoolRoleRequest(
    Guid SchoolId,
    string Name,
    IReadOnlyList<RolePermissionDto>? Permissions = null);
public sealed record SchoolRoleResponse(
    Guid Id,
    Guid TenantId,
    Guid SchoolId,
    string Name,
    string Code,
    bool IsSystemRole,
    IReadOnlyList<RolePermissionDto> Permissions);
public sealed record ProvisionUserRequest(
    Guid? SchoolId,
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    UserType UserType,
    string InitialPassword,
    bool MustChangePasswordOnNextLogin = true,
    string? PhoneNumber = null,
    string? PreferredLanguage = "en",
    IReadOnlyList<Guid>? CampusIds = null,
    IReadOnlyList<Guid>? RoleIds = null);
public sealed record UserDirectoryItemResponse(
    Guid Id,
    Guid TenantId,
    Guid? SchoolId,
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    UserType UserType,
    string? PhoneNumber,
    UserStatus Status,
    string PreferredLanguage);
