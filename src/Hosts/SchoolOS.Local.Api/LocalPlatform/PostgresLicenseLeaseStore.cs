using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.Local.Api.LocalPlatform;

/// <summary>
/// Durable PostgreSQL-backed <see cref="ILicenseLeaseStore"/> for Local Data Plane nodes (ADR-012 / DOC-15).
/// Persists signed leases, monotonic sequence numbers, and last trusted timestamps in <c>platform_local.license_leases</c>.
/// </summary>
public sealed class PostgresLicenseLeaseStore : ILicenseLeaseStore
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly LocalPlatformDbContext _db;
    private readonly TimeProvider _timeProvider;

    public PostgresLicenseLeaseStore(LocalPlatformDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<SignedLicenseLease?> GetLeaseAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var record = await _db.LicenseLeases.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        return record?.ToSignedLease();
    }

    public async ValueTask<StoredLicenseLeaseState> GetStateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var record = await _db.LicenseLeases.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        return record is null
            ? new StoredLicenseLeaseState(null, 0, null)
            : new StoredLicenseLeaseState(record.ToSignedLease(), record.SequenceNumber, record.LastTrustedUtc);
    }

    public async ValueTask SetLeaseAsync(Guid tenantId, SignedLicenseLease lease, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        var payload = JsonSerializer.Deserialize<LicenseLeasePayload>(lease.PayloadJson, PayloadOptions)
            ?? throw new InvalidOperationException("Cannot persist license lease with invalid payload JSON.");

        var now = _timeProvider.GetUtcNow();
        var existing = await _db.LicenseLeases
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (existing is null)
        {
            _db.LicenseLeases.Add(LocalLicenseLeaseRecord.Create(tenantId, payload, lease, now));
        }
        else
        {
            existing.UpdateLease(payload, lease, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask ClearLeaseAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.LicenseLeases
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (existing is not null)
        {
            _db.LicenseLeases.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async ValueTask RecordTrustedTimeAsync(Guid tenantId, DateTimeOffset utcNow, CancellationToken cancellationToken = default)
    {
        var existing = await _db.LicenseLeases
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (existing is not null && utcNow > existing.LastTrustedUtc)
        {
            existing.AdvanceTrustedTime(utcNow);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
