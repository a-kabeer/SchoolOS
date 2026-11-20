using System.Security.Cryptography;
using System.Text;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Identity.Domain;

/// <summary>
/// Local Data Plane user identity aggregate (DOC-63 §3.1 + Education-Portal UserDetails).
/// Credentials and sessions are stored in separate aggregates (<see cref="UserCredential"/>, <see cref="UserSession"/>)
/// so user profile projections never load password hashes or session secrets.
/// </summary>
public class User : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    public const int MaxFailedAccessAttempts = 3;
    public static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(30);
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;

    public Guid TenantId { get; private set; }
    public Guid? SchoolId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string? MiddleName { get; private set; }
    public string LastName { get; private set; } = string.Empty;
    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";
    public string? PhoneNumber { get; private set; }
    public UserType UserType { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public int AccessFailedCount { get; private set; }
    public DateTimeOffset? LockoutEndUtc { get; private set; }
    public int CredentialVersion { get; private set; } = 1;
    public Guid SecurityStamp { get; private set; }
    public string PreferredLanguage { get; private set; } = "en";
    public bool IsDeleted { get; private set; }

    protected User() { }

    private User(
        Guid id,
        Guid tenantId,
        Guid? schoolId,
        string email,
        string firstName,
        string? middleName,
        string lastName,
        UserType userType,
        string? phoneNumber,
        string preferredLanguage)
        : base(id)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        if (schoolId.HasValue && schoolId.Value == Guid.Empty)
        {
            throw new ArgumentException("School ID cannot be an empty GUID.", nameof(schoolId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (!ContactRules.IsValidEmail(normalizedEmail))
        {
            throw new ArgumentException("Invalid email format.", nameof(email));
        }

        var validated = ValidateNameAndContact(firstName, middleName, lastName, userType, phoneNumber, preferredLanguage);

        TenantId = tenantId;
        SchoolId = schoolId;
        Email = normalizedEmail;
        FirstName = validated.FirstName;
        MiddleName = validated.MiddleName;
        LastName = validated.LastName;
        UserType = validated.UserType;
        PhoneNumber = validated.Phone;
        PreferredLanguage = validated.Language;
        Status = UserStatus.Active;
        CredentialVersion = 1;
        SecurityStamp = Guid.CreateVersion7();
    }

    public static User Create(
        Guid tenantId,
        Guid? schoolId,
        string email,
        string firstName,
        string? middleName,
        string lastName,
        UserType userType,
        string? phoneNumber = null,
        string preferredLanguage = "en")
    {
        return new User(
            Guid.CreateVersion7(),
            tenantId,
            schoolId,
            email,
            firstName,
            middleName,
            lastName,
            userType,
            phoneNumber,
            preferredLanguage);
    }

    public void UpdateProfile(
        string firstName,
        string? middleName,
        string lastName,
        UserType userType,
        string? phoneNumber,
        string preferredLanguage = "en")
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("A deleted person cannot be changed.");
        }

        var validated = ValidateNameAndContact(firstName, middleName, lastName, userType, phoneNumber, preferredLanguage);
        FirstName = validated.FirstName;
        MiddleName = validated.MiddleName;
        LastName = validated.LastName;
        UserType = validated.UserType;
        PhoneNumber = validated.Phone;
        PreferredLanguage = validated.Language;
    }

    public bool IsLockedOut(DateTimeOffset nowUtc) =>
        LockoutEndUtc.HasValue && LockoutEndUtc.Value > nowUtc;

    /// <summary>
    /// Increments failed login count and locks the account for 30 minutes after 3 consecutive failures (SEC-AUTHN-05).
    /// </summary>
    public bool RecordFailedSignIn(DateTimeOffset nowUtc)
    {
        AccessFailedCount++;
        if (AccessFailedCount >= MaxFailedAccessAttempts)
        {
            LockoutEndUtc = nowUtc.Add(DefaultLockoutDuration);
            Status = UserStatus.LockedOut;
            return true;
        }

        return false;
    }

    public void RecordSuccessfulSignIn()
    {
        AccessFailedCount = 0;
        LockoutEndUtc = null;
        if (Status == UserStatus.LockedOut)
        {
            Status = UserStatus.Active;
        }
    }

    public void IncrementCredentialVersion()
    {
        CredentialVersion++;
        SecurityStamp = Guid.CreateVersion7();
    }

    public void Suspend()
    {
        Status = UserStatus.Suspended;
        IncrementCredentialVersion();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        Status = UserStatus.Suspended;
        IncrementCredentialVersion();
    }

    private static (string FirstName, string? MiddleName, string LastName, UserType UserType, string? Phone, string Language)
        ValidateNameAndContact(
            string firstName,
            string? middleName,
            string lastName,
            UserType userType,
            string? phoneNumber,
            string preferredLanguage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(firstName.Trim().Length, NameMaxLength, nameof(firstName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lastName.Trim().Length, NameMaxLength, nameof(lastName));

        var trimmedMiddle = middleName?.Trim();
        if (!string.IsNullOrEmpty(trimmedMiddle))
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmedMiddle.Length, NameMaxLength, nameof(middleName));
        }

        if (!Enum.IsDefined(userType))
        {
            throw new ArgumentOutOfRangeException(nameof(userType));
        }

        var trimmedPhone = phoneNumber?.Trim();
        if (!string.IsNullOrEmpty(trimmedPhone) && !ContactRules.IsValidPhone(trimmedPhone))
        {
            throw new ArgumentException("A phone number has 7 to 15 digits and an optional leading +.", nameof(phoneNumber));
        }

        var lang = string.IsNullOrWhiteSpace(preferredLanguage) ? "en" : preferredLanguage.Trim().ToLowerInvariant();
        if (lang is not ("en" or "ur" or "ar"))
        {
            throw new ArgumentException("Supported preferred languages are 'en', 'ur', and 'ar'.", nameof(preferredLanguage));
        }

        return (
            firstName.Trim(),
            string.IsNullOrEmpty(trimmedMiddle) ? null : trimmedMiddle,
            lastName.Trim(),
            userType,
            string.IsNullOrEmpty(trimmedPhone) ? null : trimmedPhone,
            lang);
    }
}

/// <summary>
/// Separate credential aggregate storing PBKDF2-HMAC-SHA512 password hashes (DOC-63 §3.1 &amp; §3.3).
/// </summary>
public class UserCredential : Entity<Guid>, ITenantScopedEntity
{
    public const string DefaultAlgorithm = "PBKDF2-HMAC-SHA512";
    public const int DefaultIterations = 100_000;

    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public string HashAlgorithm { get; private set; } = DefaultAlgorithm;
    public int Iterations { get; private set; } = DefaultIterations;
    public bool MustChangePasswordOnNextLogin { get; private set; }
    public DateTimeOffset LastChangedAtUtc { get; private set; }

    protected UserCredential() { }

    private UserCredential(
        Guid id,
        Guid tenantId,
        Guid userId,
        string passwordHash,
        bool mustChangePasswordOnNextLogin,
        DateTimeOffset nowUtc)
        : base(id)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        TenantId = tenantId;
        UserId = userId;
        PasswordHash = passwordHash;
        HashAlgorithm = DefaultAlgorithm;
        Iterations = DefaultIterations;
        MustChangePasswordOnNextLogin = mustChangePasswordOnNextLogin;
        LastChangedAtUtc = nowUtc;
    }

    public static UserCredential Create(
        Guid tenantId,
        Guid userId,
        string passwordHash,
        bool mustChangePasswordOnNextLogin,
        DateTimeOffset nowUtc) =>
        new(Guid.CreateVersion7(), tenantId, userId, passwordHash, mustChangePasswordOnNextLogin, nowUtc);

    public void UpdatePasswordHash(string newPasswordHash, bool mustChangePasswordOnNextLogin, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        PasswordHash = newPasswordHash;
        MustChangePasswordOnNextLogin = mustChangePasswordOnNextLogin;
        LastChangedAtUtc = nowUtc;
    }
}

/// <summary>
/// Active or revoked authentication session tracking SHA-256 hashed rotating refresh tokens
/// and previous token hashes for breach/reuse detection (DOC-63 §3.1 &amp; §5.2).
/// </summary>
public class UserSession : Entity<Guid>, ITenantScopedEntity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? ActiveCampusId { get; private set; }
    public string RefreshTokenHash { get; private set; } = string.Empty;
    public string? PreviousRefreshTokenHash { get; private set; }
    public DateTimeOffset RefreshTokenExpiresAtUtc { get; private set; }
    public string? CreatedFromIp { get; private set; }
    public string? UserAgent { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedReason { get; private set; }
    public DateTimeOffset LastActivityAtUtc { get; private set; }

    protected UserSession() { }

    private UserSession(
        Guid id,
        Guid tenantId,
        Guid userId,
        Guid? activeCampusId,
        string refreshTokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc,
        string? createdFromIp,
        string? userAgent)
        : base(id)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenHash);

        TenantId = tenantId;
        UserId = userId;
        ActiveCampusId = activeCampusId;
        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiresAtUtc = expiresAtUtc;
        CreatedFromIp = createdFromIp;
        UserAgent = userAgent;
        LastActivityAtUtc = nowUtc;
    }

    public static UserSession Create(
        Guid tenantId,
        Guid userId,
        Guid? activeCampusId,
        string rawRefreshToken,
        DateTimeOffset nowUtc,
        TimeSpan lifetime,
        string? createdFromIp = null,
        string? userAgent = null)
    {
        var hash = HashRefreshToken(rawRefreshToken);
        return new UserSession(
            Guid.CreateVersion7(),
            tenantId,
            userId,
            activeCampusId,
            hash,
            nowUtc.Add(lifetime),
            nowUtc,
            createdFromIp,
            userAgent);
    }

    public static string GenerateRawRefreshToken(Guid? tenantId = null)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var randomPart = Convert.ToBase64String(bytes);
        return tenantId.HasValue && tenantId.Value != Guid.Empty
            ? $"{tenantId.Value:N}.{randomPart}"
            : randomPart;
    }

    public static bool TryExtractTenantIdFromRefreshToken(string rawRefreshToken, out Guid tenantId)
    {
        tenantId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return false;
        }

        var dotIndex = rawRefreshToken.IndexOf('.');
        if (dotIndex != 32)
        {
            return false;
        }

        return Guid.TryParseExact(rawRefreshToken.AsSpan(0, 32), "N", out tenantId) && tenantId != Guid.Empty;
    }

    public static string HashRefreshToken(string rawRefreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawRefreshToken);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken));
        return Convert.ToHexStringLower(hashBytes);
    }

    public void RotateRefreshToken(string newRawRefreshToken, DateTimeOffset nowUtc, TimeSpan lifetime)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException("Cannot rotate a revoked session.");
        }

        PreviousRefreshTokenHash = RefreshTokenHash;
        RefreshTokenHash = HashRefreshToken(newRawRefreshToken);
        RefreshTokenExpiresAtUtc = nowUtc.Add(lifetime);
        LastActivityAtUtc = nowUtc;
    }

    public void SwitchActiveCampus(Guid targetCampusId, DateTimeOffset nowUtc)
    {
        if (targetCampusId == Guid.Empty)
        {
            throw new ArgumentException("Target campus ID cannot be empty.", nameof(targetCampusId));
        }

        ActiveCampusId = targetCampusId;
        LastActivityAtUtc = nowUtc;
    }

    public void Revoke(string reason, DateTimeOffset nowUtc)
    {
        IsRevoked = true;
        RevokedAtUtc = nowUtc;
        RevokedReason = reason;
    }
}
