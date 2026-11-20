using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Identity.Domain;
using SchoolOS.Modules.Identity.Persistence;

namespace SchoolOS.Modules.Identity.Application;

public sealed record AuthenticatedSessionResult(
    SignInOutcome Outcome,
    string? AccessToken = null,
    string? RawRefreshToken = null,
    int ExpiresInSeconds = 900,
    AuthenticatedUserSummary? User = null,
    string? ErrorCode = null);

public sealed record AuthenticatedUserSummary(
    Guid Id,
    string Email,
    string FullName,
    Guid TenantId,
    Guid? SchoolId,
    Guid? ActiveCampusId,
    UserType UserType,
    bool MustChangePassword,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record OfflineResetVoucherResult(
    Guid TargetUserId,
    string TemporaryPassword,
    int NewCredentialVersion,
    int RevokedSessionCount);

/// <summary>
/// Application service orchestrating Local Data Plane authentication, NIST SP 800-63B password validation,
/// PBKDF2-HMAC-SHA512 hashing (100,000 iterations), 3-attempt account lockout, SHA-256 refresh token rotation
/// with token-reuse breach detection, and campus-scoped RBAC resolution (DOC-63 + Education-Portal).
/// </summary>
public sealed class IdentityAuthenticationService
{
    public const string RefreshCookieName = "schoolos_rt";
    public static readonly TimeSpan LocalRefreshTokenLifetime = TimeSpan.FromHours(24);
    private const string VoucherAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%*";

    private static readonly PasswordHasher<User> Hasher = new(
        Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = UserCredential.DefaultIterations
        }));

    private static readonly Lazy<string> DummyPasswordHash = new(() =>
    {
        var dummyUser = User.Create(Guid.CreateVersion7(), null, "dummy@schoolos.local", "Dummy", null, "User", UserType.Staff);
        return Hasher.HashPassword(dummyUser, "DummyConstantTimeComparisonPassword12345!");
    });

    private readonly IdentityDbContext _db;
    private readonly JwtOptions _jwtOptions;
    private readonly IModuleAccess _moduleAccess;
    private readonly ISecurityAuditLog _auditLog;
    private readonly TimeProvider _timeProvider;

    public IdentityAuthenticationService(
        IdentityDbContext db,
        JwtOptions jwtOptions,
        IModuleAccess moduleAccess,
        ISecurityAuditLog auditLog,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _jwtOptions = jwtOptions;
        _moduleAccess = moduleAccess;
        _auditLog = auditLog;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public static string HashPassword(User user, string password) =>
        Hasher.HashPassword(user, password);

    public static bool VerifyPassword(User user, string passwordHash, string providedPassword) =>
        Hasher.VerifyHashedPassword(user, passwordHash, providedPassword)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;

    public async Task<(User User, UserCredential Credential)> ProvisionUserAsync(
        Guid tenantId,
        Guid? schoolId,
        string email,
        string firstName,
        string? middleName,
        string lastName,
        UserType userType,
        string initialPassword,
        bool mustChangePasswordOnNextLogin,
        string? phoneNumber = null,
        string preferredLanguage = "en",
        CancellationToken cancellationToken = default)
    {
        var policyResult = PasswordPolicyValidator.Validate(initialPassword, email, firstName);
        if (!policyResult.IsValid)
        {
            throw new ArgumentException(
                string.Join(" ", policyResult.Errors.Select(e => e.Description)),
                nameof(initialPassword));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"A user with email '{normalizedEmail}' already exists in this tenant.");
        }

        var now = _timeProvider.GetUtcNow();
        var user = User.Create(tenantId, schoolId, normalizedEmail, firstName, middleName, lastName, userType, phoneNumber, preferredLanguage);
        var hash = HashPassword(user, initialPassword);
        var credential = UserCredential.Create(tenantId, user.Id, hash, mustChangePasswordOnNextLogin, now);

        _db.Users.Add(user);
        _db.UserCredentials.Add(credential);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.UserCreated,
            SecurityAuditOutcome.Success,
            targetType: nameof(User),
            targetId: user.Id.ToString(),
            cancellationToken: cancellationToken);

        return (user, credential);
    }

    public async Task<AuthenticatedSessionResult> SignInAsync(
        string email,
        string password,
        Guid? requestedCampusId = null,
        string? sourceIp = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default,
        Guid? tenantId = null)
    {
        if (!_db.TenantContext.TenantId.HasValue && tenantId.HasValue && tenantId.Value != Guid.Empty &&
            _db.TenantContext is TenantContext preAuthContext && !preAuthContext.IsAuthenticated)
        {
            preAuthContext.EnterPreAuthenticationTenantScope(tenantId.Value, "system:auth-signin");
        }

        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var now = _timeProvider.GetUtcNow();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            // Constant-time dummy verification to prevent account enumeration timing side-channels.
            var dummy = User.Create(Guid.CreateVersion7(), null, "nobody@schoolos.local", "No", null, "Body", UserType.Staff);
            _ = VerifyPassword(dummy, DummyPasswordHash.Value, password ?? string.Empty);

            await _auditLog.WriteAsync(
                SecurityAuditEventType.SignInFailed,
                SecurityAuditOutcome.Failure,
                targetType: nameof(User),
                cancellationToken: cancellationToken);

            return new AuthenticatedSessionResult(SignInOutcome.InvalidCredentials, ErrorCode: "INVALID_CREDENTIALS");
        }

        if (user.Status == UserStatus.Suspended)
        {
            await _auditLog.WriteAsync(
                SecurityAuditEventType.SignInFailed,
                SecurityAuditOutcome.Failure,
                actorUserId: user.Id.ToString(),
                targetType: nameof(User),
                targetId: user.Id.ToString(),
                cancellationToken: cancellationToken);

            return new AuthenticatedSessionResult(SignInOutcome.Suspended, ErrorCode: "ACCOUNT_SUSPENDED");
        }

        if (user.IsLockedOut(now))
        {
            await _auditLog.WriteAsync(
                SecurityAuditEventType.SignInLockedOut,
                SecurityAuditOutcome.Failure,
                actorUserId: user.Id.ToString(),
                targetType: nameof(User),
                targetId: user.Id.ToString(),
                cancellationToken: cancellationToken);

            return new AuthenticatedSessionResult(SignInOutcome.LockedOut, ErrorCode: "ACCOUNT_LOCKED_OUT");
        }

        var credential = await _db.UserCredentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);

        if (credential is null || !VerifyPassword(user, credential.PasswordHash, password ?? string.Empty))
        {
            var lockedNow = user.RecordFailedSignIn(now);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLog.WriteAsync(
                lockedNow ? SecurityAuditEventType.SignInLockedOut : SecurityAuditEventType.SignInFailed,
                SecurityAuditOutcome.Failure,
                actorUserId: user.Id.ToString(),
                targetType: nameof(User),
                targetId: user.Id.ToString(),
                cancellationToken: cancellationToken);

            return new AuthenticatedSessionResult(
                lockedNow ? SignInOutcome.LockedOut : SignInOutcome.InvalidCredentials,
                ErrorCode: lockedNow ? "ACCOUNT_LOCKED_OUT" : "INVALID_CREDENTIALS");
        }

        // Resolve and verify campus assignment if requested
        var campusAssignments = await _db.UserCampusAssignments
            .Where(a => a.UserId == user.Id)
            .ToListAsync(cancellationToken);

        Guid? activeCampusId = requestedCampusId;
        if (requestedCampusId.HasValue)
        {
            if (campusAssignments.Count > 0 && campusAssignments.All(a => a.CampusId != requestedCampusId.Value))
            {
                await _auditLog.WriteAsync(
                    SecurityAuditEventType.AccessDenied,
                    SecurityAuditOutcome.Failure,
                    actorUserId: user.Id.ToString(),
                    targetType: "Campus",
                    targetId: requestedCampusId.Value.ToString(),
                    cancellationToken: cancellationToken);

                return new AuthenticatedSessionResult(
                    SignInOutcome.CampusAssignmentRequired,
                    ErrorCode: "CAMPUS_ASSIGNMENT_REQUIRED");
            }
        }
        else
        {
            activeCampusId = campusAssignments.FirstOrDefault(a => a.IsPrimary)?.CampusId
                             ?? campusAssignments.FirstOrDefault()?.CampusId;
        }

        user.RecordSuccessfulSignIn();

        var rawRefreshToken = UserSession.GenerateRawRefreshToken(user.TenantId);
        var session = UserSession.Create(
            user.TenantId,
            user.Id,
            activeCampusId,
            rawRefreshToken,
            now,
            LocalRefreshTokenLifetime,
            sourceIp,
            userAgent);

        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        var (roles, permissions) = await ResolveRolesAndPermissionsAsync(user, activeCampusId, cancellationToken);
        var accessToken = JwtTokenHelper.GenerateToken(
            _jwtOptions,
            user.Id.ToString(),
            user.TenantId,
            activeCampusId,
            roles: roles,
            scopes: permissions,
            credentialVersion: user.CredentialVersion,
            sessionId: session.Id,
            mustChangePassword: credential.MustChangePasswordOnNextLogin,
            permissions: permissions);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.SignInSucceeded,
            SecurityAuditOutcome.Success,
            actorUserId: user.Id.ToString(),
            targetType: nameof(UserSession),
            targetId: session.Id.ToString(),
            cancellationToken: cancellationToken);

        var summary = new AuthenticatedUserSummary(
            user.Id,
            user.Email,
            user.FullName,
            user.TenantId,
            user.SchoolId,
            activeCampusId,
            user.UserType,
            credential.MustChangePasswordOnNextLogin,
            roles,
            permissions);

        return new AuthenticatedSessionResult(
            SignInOutcome.Succeeded,
            accessToken,
            rawRefreshToken,
            _jwtOptions.ExpiryMinutes * 60,
            summary);
    }

    /// <summary>
    /// Rotates a refresh token or triggers immediate breach/reuse revocation if an already-rotated token is replayed (DOC-63 §5.2).
    /// </summary>
    public async Task<AuthenticatedSessionResult> RefreshSessionAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return new AuthenticatedSessionResult(SignInOutcome.InvalidCredentials, ErrorCode: "INVALID_REFRESH_TOKEN");
        }

        if (!_db.TenantContext.TenantId.HasValue &&
            UserSession.TryExtractTenantIdFromRefreshToken(rawRefreshToken, out var tokenTenantId) &&
            _db.TenantContext is TenantContext preAuthContext && !preAuthContext.IsAuthenticated)
        {
            preAuthContext.EnterPreAuthenticationTenantScope(tokenTenantId, "system:auth-refresh");
        }

        var tokenHash = UserSession.HashRefreshToken(rawRefreshToken);
        var now = _timeProvider.GetUtcNow();

        var session = await _db.UserSessions
            .FirstOrDefaultAsync(
                s => s.RefreshTokenHash == tokenHash || s.PreviousRefreshTokenHash == tokenHash,
                cancellationToken);

        if (session is null)
        {
            return new AuthenticatedSessionResult(SignInOutcome.InvalidCredentials, ErrorCode: "INVALID_REFRESH_TOKEN");
        }

        // Breach / Token Reuse Detection: client presented a previously rotated or revoked token!
        if (session.IsRevoked || session.PreviousRefreshTokenHash == tokenHash)
        {
            var wasAlreadyRevoked = session.IsRevoked;
            var allUserSessions = await _db.UserSessions
                .Where(s => s.UserId == session.UserId && !s.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var activeSession in allUserSessions)
            {
                activeSession.Revoke("TokenReuseDetected", now);
            }

            if (!wasAlreadyRevoked || allUserSessions.Count > 0)
            {
                var compromisedUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken);
                compromisedUser?.IncrementCredentialVersion();
                await _db.SaveChangesAsync(cancellationToken);
            }

            await _auditLog.WriteAsync(
                SecurityAuditEventType.TokenReuseDetected,
                SecurityAuditOutcome.Failure,
                actorUserId: session.UserId.ToString(),
                targetType: nameof(UserSession),
                targetId: session.Id.ToString(),
                cancellationToken: cancellationToken);

            return new AuthenticatedSessionResult(SignInOutcome.InvalidCredentials, ErrorCode: "SESSION_COMPROMISED");
        }

        if (session.RefreshTokenExpiresAtUtc <= now)
        {
            session.Revoke("Expired", now);
            await _db.SaveChangesAsync(cancellationToken);
            return new AuthenticatedSessionResult(SignInOutcome.InvalidCredentials, ErrorCode: "REFRESH_TOKEN_EXPIRED");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == session.UserId && !u.IsDeleted, cancellationToken);
        var credential = await _db.UserCredentials.FirstOrDefaultAsync(c => c.UserId == session.UserId, cancellationToken);
        if (user is null || credential is null || user.Status != UserStatus.Active)
        {
            return new AuthenticatedSessionResult(SignInOutcome.InvalidCredentials, ErrorCode: "USER_INACTIVE");
        }

        var newRawRefreshToken = UserSession.GenerateRawRefreshToken(session.TenantId);
        session.RotateRefreshToken(newRawRefreshToken, now, LocalRefreshTokenLifetime);
        await _db.SaveChangesAsync(cancellationToken);

        var (roles, permissions) = await ResolveRolesAndPermissionsAsync(user, session.ActiveCampusId, cancellationToken);
        var accessToken = JwtTokenHelper.GenerateToken(
            _jwtOptions,
            user.Id.ToString(),
            user.TenantId,
            session.ActiveCampusId,
            roles: roles,
            scopes: permissions,
            credentialVersion: user.CredentialVersion,
            sessionId: session.Id,
            mustChangePassword: credential.MustChangePasswordOnNextLogin,
            permissions: permissions);

        var summary = new AuthenticatedUserSummary(
            user.Id,
            user.Email,
            user.FullName,
            user.TenantId,
            user.SchoolId,
            session.ActiveCampusId,
            user.UserType,
            credential.MustChangePasswordOnNextLogin,
            roles,
            permissions);

        return new AuthenticatedSessionResult(
            SignInOutcome.Succeeded,
            accessToken,
            newRawRefreshToken,
            _jwtOptions.ExpiryMinutes * 60,
            summary);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
        {
            throw new ArgumentException("The new password must be different from the current password.", nameof(newPassword));
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken)
                   ?? throw new InvalidOperationException("The user does not exist.");

        var credential = await _db.UserCredentials.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken)
                         ?? throw new InvalidOperationException("User credentials were not found.");

        if (!VerifyPassword(user, credential.PasswordHash, currentPassword))
        {
            await _auditLog.WriteAsync(
                SecurityAuditEventType.PasswordChanged,
                SecurityAuditOutcome.Failure,
                actorUserId: userId.ToString(),
                targetType: nameof(User),
                targetId: userId.ToString(),
                cancellationToken: cancellationToken);

            throw new ArgumentException("The current password is incorrect.", nameof(currentPassword));
        }

        var policy = PasswordPolicyValidator.Validate(newPassword, user.Email, user.FirstName);
        if (!policy.IsValid)
        {
            await _auditLog.WriteAsync(
                SecurityAuditEventType.PasswordChanged,
                SecurityAuditOutcome.Failure,
                actorUserId: userId.ToString(),
                targetType: nameof(User),
                targetId: userId.ToString(),
                cancellationToken: cancellationToken);

            throw new ArgumentException(
                string.Join(" ", policy.Errors.Select(e => e.Description)),
                nameof(newPassword));
        }

        var now = _timeProvider.GetUtcNow();
        var newHash = HashPassword(user, newPassword);
        credential.UpdatePasswordHash(newHash, mustChangePasswordOnNextLogin: false, now);
        user.IncrementCredentialVersion();

        // Revoke all other sessions of this user (SEC-AUTHN-07)
        var sessions = await _db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var s in sessions)
        {
            s.Revoke("PasswordChanged", now);
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.PasswordChanged,
            SecurityAuditOutcome.Success,
            actorUserId: userId.ToString(),
            targetType: nameof(User),
            targetId: userId.ToString(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Offline supervised password reset voucher issued by an authorized CampusAdmin/Principal during WAN outages (DOC-63 §3.5).
    /// </summary>
    public async Task<OfflineResetVoucherResult> IssueOfflineResetVoucherAsync(
        Guid actorUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId && !u.IsDeleted, cancellationToken)
                         ?? throw new InvalidOperationException("Target user was not found in active tenant scope.");

        var credential = await _db.UserCredentials.FirstOrDefaultAsync(c => c.UserId == targetUserId, cancellationToken)
                         ?? throw new InvalidOperationException("Target user credential record was not found.");

        var voucherPassword = GenerateStrongTemporaryPassword();
        var now = _timeProvider.GetUtcNow();
        var hash = HashPassword(targetUser, voucherPassword);

        credential.UpdatePasswordHash(hash, mustChangePasswordOnNextLogin: true, now);
        targetUser.RecordSuccessfulSignIn();
        targetUser.IncrementCredentialVersion();

        var activeSessions = await _db.UserSessions
            .Where(s => s.UserId == targetUserId && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Revoke("OfflineSupervisedResetVoucher", now);
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.EmergencyCredentialReset,
            SecurityAuditOutcome.Success,
            actorUserId: actorUserId.ToString(),
            targetType: nameof(User),
            targetId: targetUserId.ToString(),
            cancellationToken: cancellationToken);

        return new OfflineResetVoucherResult(
            targetUser.Id,
            voucherPassword,
            targetUser.CredentialVersion,
            activeSessions.Count);
    }

    /// <summary>
    /// Revokes the active session on logout and records a <see cref="SecurityAuditEventType.SignedOut"/> audit event (ADR-023 / DOC-63).
    /// </summary>
    public async Task<bool> SignOutAsync(
        Guid userId,
        Guid? sessionId,
        string? rawRefreshToken,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        UserSession? session = null;

        if (sessionId.HasValue)
        {
            session = await _db.UserSessions.FirstOrDefaultAsync(
                s => s.Id == sessionId.Value && s.UserId == userId && !s.IsRevoked,
                cancellationToken);
        }

        if (session is null && !string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            var tokenHash = UserSession.HashRefreshToken(rawRefreshToken);
            session = await _db.UserSessions.FirstOrDefaultAsync(
                s => s.UserId == userId && !s.IsRevoked && s.RefreshTokenHash == tokenHash,
                cancellationToken);
        }

        if (session is not null)
        {
            session.Revoke("UserLogout", now);
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _auditLog.WriteAsync(
            SecurityAuditEventType.SignedOut,
            SecurityAuditOutcome.Success,
            actorUserId: userId.ToString(),
            targetType: nameof(UserSession),
            targetId: session?.Id.ToString() ?? sessionId?.ToString(),
            cancellationToken: cancellationToken);

        return session is not null;
    }

    /// <summary>
    /// Verifies server-side <see cref="UserCampusAssignment"/> and re-issues a campus-scoped JWT (DOC-63 §5.3).
    /// </summary>
    public async Task<AuthenticatedSessionResult> SwitchCampusContextAsync(
        Guid userId,
        Guid? sessionId,
        Guid targetCampusId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        var credential = await _db.UserCredentials.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (user is null || credential is null)
        {
            return new AuthenticatedSessionResult(SignInOutcome.InvalidCredentials, ErrorCode: "USER_NOT_FOUND");
        }

        var isAssigned = await _db.UserCampusAssignments
            .AnyAsync(a => a.UserId == userId && a.CampusId == targetCampusId, cancellationToken);

        if (!isAssigned)
        {
            await _auditLog.WriteAsync(
                SecurityAuditEventType.AccessDenied,
                SecurityAuditOutcome.Failure,
                actorUserId: userId.ToString(),
                targetType: "Campus",
                targetId: targetCampusId.ToString(),
                cancellationToken: cancellationToken);

            return new AuthenticatedSessionResult(
                SignInOutcome.CampusAssignmentRequired,
                ErrorCode: "CAMPUS_ASSIGNMENT_REQUIRED");
        }

        var now = _timeProvider.GetUtcNow();
        if (sessionId.HasValue)
        {
            var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId.Value && !s.IsRevoked, cancellationToken);
            session?.SwitchActiveCampus(targetCampusId, now);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var (roles, permissions) = await ResolveRolesAndPermissionsAsync(user, targetCampusId, cancellationToken);
        var accessToken = JwtTokenHelper.GenerateToken(
            _jwtOptions,
            user.Id.ToString(),
            user.TenantId,
            targetCampusId,
            roles: roles,
            scopes: permissions,
            credentialVersion: user.CredentialVersion,
            sessionId: sessionId,
            mustChangePassword: credential.MustChangePasswordOnNextLogin,
            permissions: permissions);

        var summary = new AuthenticatedUserSummary(
            user.Id,
            user.Email,
            user.FullName,
            user.TenantId,
            user.SchoolId,
            targetCampusId,
            user.UserType,
            credential.MustChangePasswordOnNextLogin,
            roles,
            permissions);

        return new AuthenticatedSessionResult(
            SignInOutcome.Succeeded,
            accessToken,
             null,
            _jwtOptions.ExpiryMinutes * 60,
            summary);
    }

    private async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> ResolveRolesAndPermissionsAsync(
        User user,
        Guid? activeCampusId,
        CancellationToken cancellationToken)
    {
        var roleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var permissionScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Map base UserType to standard system role name
        var systemRole = user.UserType switch
        {
            UserType.SchoolAdministrator => SecurityPolicies.SchoolAdminRole,
            UserType.Principal => SecurityPolicies.PrincipalRole,
            UserType.Teacher => SecurityPolicies.TeacherRole,
            UserType.Student => SecurityPolicies.StudentRole,
            _ => user.UserType.ToString()
        };
        roleNames.Add(systemRole);

        var assignedRoleIds = await _db.UserRoleAssignments
            .Where(a => a.UserId == user.Id && (a.CampusId == null || a.CampusId == activeCampusId))
            .Select(a => a.SchoolRoleId)
            .ToListAsync(cancellationToken);

        if (assignedRoleIds.Count > 0)
        {
            var enabledModules = await _moduleAccess.GetEnabledModulesAsync(user.TenantId, cancellationToken);
            var roles = await _db.SchoolRoles
                .Include(r => r.Permissions)
                .Where(r => assignedRoleIds.Contains(r.Id))
                .ToListAsync(cancellationToken);

            foreach (var role in roles)
            {
                roleNames.Add(role.Name);
                foreach (var scope in role.GetEffectivePermissionScopes(enabledModules))
                {
                    permissionScopes.Add(scope);
                }
            }
        }

        return (roleNames.OrderBy(r => r).ToList(), permissionScopes.OrderBy(p => p).ToList());
    }

    private static string GenerateStrongTemporaryPassword()
    {
        // 20-char high-entropy voucher guaranteed >= 15 chars and never in CommonPasswords
        return RandomNumberGenerator.GetString(VoucherAlphabet, 20);
    }
}
