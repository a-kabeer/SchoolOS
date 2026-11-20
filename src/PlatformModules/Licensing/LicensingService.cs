using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.PlatformModules.Subscriptions.Domain;
using SchoolOS.PlatformModules.Subscriptions.Persistence;

namespace SchoolOS.PlatformModules.Licensing.Application;

public sealed class LicensingSigningOptions
{
    public string KeyId { get; set; } = "schoolos-cloud-es256-v1";
    public string PrivateKeyPem { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;
    public int DefaultLeaseDays { get; set; } = 14;

    public static LicensingSigningOptions CreateEphemeral()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new LicensingSigningOptions
        {
            KeyId = "schoolos-dev-es256-v1",
            PrivateKeyPem = ecdsa.ExportECPrivateKeyPem(),
            PublicKeyPem = ecdsa.ExportSubjectPublicKeyInfoPem(),
            DefaultLeaseDays = 14
        };
    }

    public void EnsureKeysConfigured(bool allowEphemeralGeneration)
    {
        if (!string.IsNullOrWhiteSpace(PrivateKeyPem) && !string.IsNullOrWhiteSpace(PublicKeyPem))
        {
            return;
        }

        if (!allowEphemeralGeneration)
        {
            throw new InvalidOperationException(
                "Licensing ES256 signing keys must be explicitly configured outside Development/Testing.");
        }

        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        PrivateKeyPem = ecdsa.ExportECPrivateKeyPem();
        PublicKeyPem = ecdsa.ExportSubjectPublicKeyInfoPem();
    }
}

public sealed class LicenseLeaseIssuer
{
    private readonly SubscriptionsDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly LicensingSigningOptions _options;
    private readonly ISecurityAuditLog _auditLog;
    private readonly TimeProvider _timeProvider;

    public LicenseLeaseIssuer(
        SubscriptionsDbContext db,
        ITenantContext tenantContext,
        LicensingSigningOptions options,
        ISecurityAuditLog auditLog,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _tenantContext = tenantContext;
        _options = options;
        _auditLog = auditLog;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<SignedLicenseLease> IssueLeaseAsync(
        Guid tenantId,
        int? leaseDays = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        var days = leaseDays ?? _options.DefaultLeaseDays;
        if (days is < 1 or > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDays), "Lease duration must be between 1 and 30 days.");
        }

        using var _ = BeginTargetTenantScope(tenantId);

        var subscription = await _db.Subscriptions.AsNoTracking()
            .Include(s => s.Modules)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status != SubscriptionStatus.Cancelled, cancellationToken)
            ?? throw new KeyNotFoundException($"No active subscription found for tenant '{tenantId}'.");

        if (subscription.Status == SubscriptionStatus.Suspended)
        {
            throw new InvalidOperationException("Suspended subscriptions cannot be issued an active add-on license lease.");
        }

        var now = _timeProvider.GetUtcNow();
        var activePaid = subscription.Modules
            .Where(m => m.Status == TenantModuleStatus.Active && m.ValidTo > now && !m.ModuleCode.IsCoreModule())
            .Select(m => m.ModuleCode);

        var enabledModules = OfflineLicenseLeaseVerifier.CoreModules
            .Concat(activePaid)
            .Distinct()
            .OrderBy(m => (int)m)
            .ToList();

        string? planCode = null;
        if (subscription.PlanId.HasValue)
        {
            planCode = await _db.Plans.AsNoTracking()
                .Where(p => p.Id == subscription.PlanId.Value)
                .Select(p => p.Code)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var maxExpiry = now.AddDays(days);
        var effectiveExpiry = subscription.EndDate < maxExpiry && subscription.EndDate > now
            ? subscription.EndDate
            : maxExpiry;

        var payload = new LicenseLeasePayload(
            tenantId,
            now,
            effectiveExpiry,
            _options.KeyId,
            planCode,
            enabledModules);

        var signed = OfflineLicenseLeaseVerifier.SignWithPrivateKey(payload, _options.PrivateKeyPem);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.LicenseLeaseIssued,
            SecurityAuditOutcome.Success,
            targetType: nameof(Subscription),
            targetId: subscription.Id.ToString(),
            cancellationToken: cancellationToken);

        return signed;
    }

    private IDisposable BeginTargetTenantScope(Guid targetTenantId)
    {
        if (_tenantContext.TenantId == targetTenantId)
        {
            return NoOpDisposable.Instance;
        }

        if (_tenantContext is TenantContext mutable && mutable.IsPlatformAdmin)
        {
            return mutable.BeginPlatformTenantScope(targetTenantId);
        }

        throw new UnauthorizedAccessException("Caller is not authorized to issue a lease for this tenant.");
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public static readonly NoOpDisposable Instance = new();
        public void Dispose() { }
    }
}
