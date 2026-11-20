using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// Evaluated local offline license lifecycle state (ADR-012 / DOC-15 §2).
/// </summary>
public enum LicenseLeaseStatus
{
    Missing = 0,
    Valid = 1,
    Grace = 2,
    Restricted = 3,
    Expired = 4,
    InvalidSignature = 5,
    ClockRollbackReview = 6
}

/// <summary>
/// Unsigned payload of a Cloud-issued, Local-verified school subscription lease.
/// </summary>
public sealed record LicenseLeasePayload(
    Guid TenantId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string KeyId,
    string? PlanCode,
    IReadOnlyList<ModuleCode> EnabledModules,
    long SequenceNumber = 1,
    Guid? NodeId = null,
    DateTimeOffset? PaidThroughUtc = null,
    DateTimeOffset? GraceUntilUtc = null);

/// <summary>
/// Signed offline license lease envelope synced from the Cloud Control Plane to Local Data Plane nodes.
/// </summary>
public sealed record SignedLicenseLease(
    string PayloadJson,
    string SignatureBase64Url,
    string Algorithm = "ES256");

/// <summary>
/// Verified result of inspecting a <see cref="SignedLicenseLease"/>.
/// </summary>
public sealed record LicenseLeaseValidationResult(
    bool IsValid,
    string? FailureReason,
    LicenseLeasePayload? Payload,
    IReadOnlySet<ModuleCode> EntitledModules,
    LicenseLeaseStatus Status = LicenseLeaseStatus.Valid);

/// <summary>
/// Persisted lease state including monotonic sequence and last trusted clock observation.
/// </summary>
public sealed record StoredLicenseLeaseState(
    SignedLicenseLease? Lease,
    long HighestSequenceNumber,
    DateTimeOffset? LastTrustedUtc);

/// <summary>
/// Stores and retrieves the active signed license lease per tenant on a Local node.
/// </summary>
public interface ILicenseLeaseStore
{
    ValueTask<SignedLicenseLease?> GetLeaseAsync(Guid tenantId, CancellationToken cancellationToken = default);
    ValueTask SetLeaseAsync(Guid tenantId, SignedLicenseLease lease, CancellationToken cancellationToken = default);
    ValueTask ClearLeaseAsync(Guid tenantId, CancellationToken cancellationToken = default);

    async ValueTask<StoredLicenseLeaseState> GetStateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var lease = await GetLeaseAsync(tenantId, cancellationToken);
        return new StoredLicenseLeaseState(lease, 0, null);
    }

    ValueTask RecordTrustedTimeAsync(Guid tenantId, DateTimeOffset utcNow, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}

/// <summary>
/// Thread-safe in-memory lease store for runtime caching, single-node deployments, and integration tests.
/// </summary>
public sealed class InMemoryLicenseLeaseStore : ILicenseLeaseStore
{
    private readonly ConcurrentDictionary<Guid, SignedLicenseLease> _leases = new();

    public ValueTask<SignedLicenseLease?> GetLeaseAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_leases.TryGetValue(tenantId, out var lease) ? lease : null);

    public ValueTask SetLeaseAsync(Guid tenantId, SignedLicenseLease lease, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        _leases[tenantId] = lease;
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearLeaseAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _leases.TryRemove(tenantId, out _);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Cryptographically verifies Cloud-signed ES256 (ECDSA P-256 SHA-256) license leases offline using the Cloud public key.
/// </summary>
public static class OfflineLicenseLeaseVerifier
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    public static string SerializePayload(LicenseLeasePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    public static SignedLicenseLease SignWithPrivateKey(LicenseLeasePayload payload, string ecPrivateKeyPem)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(ecPrivateKeyPem);

        var payloadJson = SerializePayload(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(ecPrivateKeyPem);
        var sigBytes = ecdsa.SignData(payloadBytes, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return new SignedLicenseLease(payloadJson, Base64UrlEncode(sigBytes), "ES256");
    }

    public static LicenseLeaseValidationResult Verify(
        SignedLicenseLease? lease,
        Guid expectedTenantId,
        string ecPublicKeyPem,
        DateTimeOffset utcNow,
        Guid? expectedNodeId = null,
        long? minimumSequenceNumber = null,
        DateTimeOffset? lastTrustedUtc = null)
    {
        if (lastTrustedUtc.HasValue && utcNow < lastTrustedUtc.Value.Subtract(MaxClockSkew))
        {
            return Fail("CLOCK_ROLLBACK_DETECTED", LicenseLeaseStatus.ClockRollbackReview);
        }

        if (lease is null)
        {
            return Fail("LEASE_MISSING", LicenseLeaseStatus.Missing);
        }

        if (!string.Equals(lease.Algorithm, "ES256", StringComparison.Ordinal))
        {
            return Fail("UNSUPPORTED_ALGORITHM", LicenseLeaseStatus.InvalidSignature);
        }

        if (string.IsNullOrWhiteSpace(lease.PayloadJson) || string.IsNullOrWhiteSpace(lease.SignatureBase64Url))
        {
            return Fail("MALFORMED_LEASE", LicenseLeaseStatus.InvalidSignature);
        }

        if (string.IsNullOrWhiteSpace(ecPublicKeyPem))
        {
            return Fail("PUBLIC_KEY_MISSING", LicenseLeaseStatus.InvalidSignature);
        }

        byte[] signatureBytes;
        try
        {
            signatureBytes = Base64UrlDecode(lease.SignatureBase64Url);
        }
        catch (FormatException)
        {
            return Fail("INVALID_SIGNATURE_ENCODING", LicenseLeaseStatus.InvalidSignature);
        }

        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(ecPublicKeyPem);
            var payloadBytes = Encoding.UTF8.GetBytes(lease.PayloadJson);
            if (!ecdsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            {
                return Fail("INVALID_SIGNATURE", LicenseLeaseStatus.InvalidSignature);
            }
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            return Fail("INVALID_SIGNATURE_KEY", LicenseLeaseStatus.InvalidSignature);
        }

        LicenseLeasePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<LicenseLeasePayload>(lease.PayloadJson, SerializerOptions);
        }
        catch (JsonException)
        {
            return Fail("INVALID_PAYLOAD_JSON", LicenseLeaseStatus.InvalidSignature);
        }

        if (payload is null || payload.TenantId == Guid.Empty || payload.TenantId != expectedTenantId)
        {
            return Fail("TENANT_MISMATCH", LicenseLeaseStatus.InvalidSignature);
        }

        if (expectedNodeId.HasValue && payload.NodeId != expectedNodeId.Value)
        {
            return Fail("NODE_MISMATCH", LicenseLeaseStatus.InvalidSignature);
        }

        if (minimumSequenceNumber.HasValue && payload.SequenceNumber < minimumSequenceNumber.Value)
        {
            return Fail("SEQUENCE_ROLLBACK", LicenseLeaseStatus.InvalidSignature);
        }

        if (payload.IssuedAtUtc > utcNow.Add(MaxClockSkew))
        {
            return Fail("LEASE_NOT_YET_VALID", LicenseLeaseStatus.InvalidSignature);
        }

        var paidThrough = payload.PaidThroughUtc ?? payload.ExpiresAtUtc;
        var graceUntil = payload.GraceUntilUtc ?? payload.ExpiresAtUtc;

        if (payload.ExpiresAtUtc <= utcNow || graceUntil <= utcNow)
        {
            return Fail("LEASE_EXPIRED", LicenseLeaseStatus.Restricted);
        }

        var status = utcNow > paidThrough ? LicenseLeaseStatus.Grace : LicenseLeaseStatus.Valid;

        var modules = new HashSet<ModuleCode>(CoreModules);
        if (payload.EnabledModules is not null)
        {
            foreach (var module in payload.EnabledModules)
            {
                if (module != ModuleCode.None)
                {
                    modules.Add(module);
                }
            }
        }

        return new LicenseLeaseValidationResult(true, null, payload, modules, status);
    }

    public static IReadOnlySet<ModuleCode> CoreModules { get; } =
        Enum.GetValues<ModuleCode>().Where(m => m.IsCoreModule()).ToHashSet();

    private static LicenseLeaseValidationResult Fail(string reason, LicenseLeaseStatus status) =>
        new(false, reason, null, CoreModules, status);

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded
        };
        return Convert.FromBase64String(padded);
    }
}

/// <summary>
/// Configuration holder for offline license lease verification on the Local Data Plane.
/// </summary>
public sealed class OfflineLicenseVerificationOptions
{
    public string PublicKeyPem { get; set; } = string.Empty;
    public Guid? ExpectedNodeId { get; set; }
}

/// <summary>
/// Offline-capable <see cref="IModuleAccess"/> enforcing ECDSA P-256 signed leases for paid modules
/// while keeping Core modules (<c>Organization</c>, <c>Security</c>, <c>AcademicYear</c>, <c>Students</c>, <c>Staff</c>)
/// available even when offline without an add-on lease.
/// </summary>
public sealed class OfflineLicenseLeaseModuleAccess : IModuleAccess
{
    private readonly ILicenseLeaseStore _store;
    private readonly OfflineLicenseVerificationOptions _options;
    private readonly TimeProvider _timeProvider;

    public OfflineLicenseLeaseModuleAccess(
        ILicenseLeaseStore store,
        OfflineLicenseVerificationOptions options,
        TimeProvider? timeProvider = null)
    {
        _store = store;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<bool> HasModuleAsync(Guid tenantId, ModuleCode module, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return false;
        }

        if (module == ModuleCode.None || module.IsCoreModule())
        {
            return true;
        }

        var enabled = await GetEnabledModulesAsync(tenantId, cancellationToken);
        return enabled.Contains(module);
    }

    public async ValueTask<IReadOnlySet<ModuleCode>> GetEnabledModulesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return new HashSet<ModuleCode>();
        }

        var state = await _store.GetStateAsync(tenantId, cancellationToken);
        if (state.Lease is null)
        {
            return OfflineLicenseLeaseVerifier.CoreModules;
        }

        var now = _timeProvider.GetUtcNow();
        var result = OfflineLicenseLeaseVerifier.Verify(
            state.Lease,
            tenantId,
            _options.PublicKeyPem,
            now,
            _options.ExpectedNodeId,
            minimumSequenceNumber: state.HighestSequenceNumber > 0 ? state.HighestSequenceNumber : null,
            lastTrustedUtc: state.LastTrustedUtc);

        if (result.IsValid)
        {
            await _store.RecordTrustedTimeAsync(tenantId, now, cancellationToken);
        }

        return result.EntitledModules;
    }
}
