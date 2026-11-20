using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Local.Api.LocalPlatform;

/// <summary>
/// Durable signed offline license lease snapshot persisted in the Local Data Plane (<c>platform_local.license_leases</c>)
/// per ADR-012 and DOC-44 §2. Enforces monotonic sequence and clock-rollback detection across node restarts.
/// </summary>
public sealed class LocalLicenseLeaseRecord : Entity<Guid>, ITenantScopedEntity
{
    private LocalLicenseLeaseRecord()
    {
        KeyId = string.Empty;
        Algorithm = "ES256";
        PayloadJson = string.Empty;
        SignatureBase64Url = string.Empty;
    }

    private LocalLicenseLeaseRecord(
        Guid tenantId,
        Guid? nodeId,
        long sequenceNumber,
        string keyId,
        string algorithm,
        string payloadJson,
        string signatureBase64Url,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? paidThroughUtc,
        DateTimeOffset? graceUntilUtc,
        DateTimeOffset trustedNowUtc) : this()
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        NodeId = nodeId;
        SequenceNumber = sequenceNumber;
        KeyId = keyId;
        Algorithm = algorithm;
        PayloadJson = payloadJson;
        SignatureBase64Url = signatureBase64Url;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        PaidThroughUtc = paidThroughUtc;
        GraceUntilUtc = graceUntilUtc;
        LastTrustedUtc = trustedNowUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid? NodeId { get; private set; }
    public long SequenceNumber { get; private set; }
    public string KeyId { get; private set; }
    public string Algorithm { get; private set; }
    public string PayloadJson { get; private set; }
    public string SignatureBase64Url { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? PaidThroughUtc { get; private set; }
    public DateTimeOffset? GraceUntilUtc { get; private set; }
    public DateTimeOffset LastTrustedUtc { get; private set; }

    public static LocalLicenseLeaseRecord Create(
        Guid tenantId,
        LicenseLeasePayload payload,
        SignedLicenseLease lease,
        DateTimeOffset trustedNowUtc)
        => new(
            tenantId,
            payload.NodeId,
            payload.SequenceNumber,
            payload.KeyId,
            lease.Algorithm,
            lease.PayloadJson,
            lease.SignatureBase64Url,
            payload.IssuedAtUtc,
            payload.ExpiresAtUtc,
            payload.PaidThroughUtc,
            payload.GraceUntilUtc,
            trustedNowUtc);

    public void UpdateLease(LicenseLeasePayload payload, SignedLicenseLease lease, DateTimeOffset trustedNowUtc)
    {
        if (payload.SequenceNumber < SequenceNumber)
        {
            throw new InvalidOperationException("Cannot downgrade offline license lease to an older sequence number.");
        }

        NodeId = payload.NodeId;
        SequenceNumber = payload.SequenceNumber;
        KeyId = payload.KeyId;
        Algorithm = lease.Algorithm;
        PayloadJson = lease.PayloadJson;
        SignatureBase64Url = lease.SignatureBase64Url;
        IssuedAtUtc = payload.IssuedAtUtc;
        ExpiresAtUtc = payload.ExpiresAtUtc;
        PaidThroughUtc = payload.PaidThroughUtc;
        GraceUntilUtc = payload.GraceUntilUtc;
        AdvanceTrustedTime(trustedNowUtc);
    }

    public void AdvanceTrustedTime(DateTimeOffset utcNow)
    {
        if (utcNow > LastTrustedUtc)
        {
            LastTrustedUtc = utcNow;
        }
    }

    public SignedLicenseLease ToSignedLease() => new(PayloadJson, SignatureBase64Url, Algorithm);
}

public sealed class LocalPlatformDbContext : SchoolOSDbContextBase
{
    public DbSet<LocalLicenseLeaseRecord> LicenseLeases => Set<LocalLicenseLeaseRecord>();

    public LocalPlatformDbContext(DbContextOptions<LocalPlatformDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("platform_local");

        modelBuilder.Entity<LocalLicenseLeaseRecord>(b =>
        {
            b.ToTable("license_leases");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.KeyId).HasMaxLength(100).IsRequired();
            b.Property(x => x.Algorithm).HasMaxLength(20).IsRequired();
            b.Property(x => x.PayloadJson).IsRequired();
            b.Property(x => x.SignatureBase64Url).HasMaxLength(512).IsRequired();
            b.Property(x => x.CreatedBy).HasMaxLength(100);
            b.Property(x => x.UpdatedBy).HasMaxLength(100);
            b.Ignore(x => x.DomainEvents);
            b.HasIndex(x => x.TenantId).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}

public sealed class LocalPlatformDbContextFactory : IDesignTimeDbContextFactory<LocalPlatformDbContext>
{
    public LocalPlatformDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LocalPlatformDbContext>();
        optionsBuilder.UseNpgsql(
                "Host=127.0.0.1;Database=schoolos_local_design;Username=postgres;Password=dummy",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "platform_local"))
            .UseSnakeCaseNamingConvention();

        return new LocalPlatformDbContext(
            optionsBuilder.Options,
            new TenantContext(null, null, "design-time"));
    }
}
