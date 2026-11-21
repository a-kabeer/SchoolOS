using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Fees.Domain;

namespace SchoolOS.Modules.Fees.Persistence;

public class FeesDbContext : SchoolOSDbContextBase
{
    public DbSet<FeeHead> FeeHeads => Set<FeeHead>();
    public DbSet<FeeSchedule> FeeSchedules => Set<FeeSchedule>();
    public DbSet<FeeChallan> FeeChallans => Set<FeeChallan>();
    public DbSet<FeeChallanLine> FeeChallanLines => Set<FeeChallanLine>();
    public DbSet<FeeReceipt> FeeReceipts => Set<FeeReceipt>();
    public DbSet<FeePostingOutboxEntry> FeePostingOutbox => Set<FeePostingOutboxEntry>();
    public DbSet<ChangeAuditEntry> ChangeAuditEntries => Set<ChangeAuditEntry>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    public FeesDbContext(DbContextOptions<FeesDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("fees");

        modelBuilder.Entity<FeeHead>(b =>
        {
            b.ToTable("fee_heads");
            b.HasKey(h => h.Id);
            b.Property(h => h.Id).ValueGeneratedNever();
            b.HasAlternateKey(h => new { h.TenantId, h.Id });
            b.Property(h => h.Code).HasMaxLength(40).IsRequired();
            b.Property(h => h.Name).HasMaxLength(120).IsRequired();
            b.Property(h => h.Frequency).HasConversion<int>().IsRequired();
            b.Property(h => h.CreatedBy).HasMaxLength(100);
            b.Property(h => h.UpdatedBy).HasMaxLength(100);
            b.Ignore(h => h.DomainEvents);
            b.HasIndex(h => new { h.TenantId, h.SchoolId, h.Code }).IsUnique();
        });

        modelBuilder.Entity<FeeSchedule>(b =>
        {
            b.ToTable("fee_schedules");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).ValueGeneratedNever();
            b.Property(s => s.Amount).HasPrecision(18, 2).IsRequired();
            b.Property(s => s.CurrencyCode).HasMaxLength(10).IsRequired();
            b.Property(s => s.SiblingDiscountPercent).HasPrecision(5, 2).IsRequired();
            b.Property(s => s.LateFeePerDay).HasPrecision(18, 2).IsRequired();
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.UpdatedBy).HasMaxLength(100);
            b.Ignore(s => s.DomainEvents);
            b.HasIndex(s => new { s.TenantId, s.CampusId, s.AcademicYearId, s.GradeLevelId, s.FeeHeadId })
                .IsUnique()
                .HasDatabaseName("ix_fee_schedules_unique_head_per_grade");
            b.HasOne<FeeHead>()
                .WithMany()
                .HasPrincipalKey(h => new { h.TenantId, h.Id })
                .HasForeignKey(s => new { s.TenantId, s.FeeHeadId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FeeChallan>(b =>
        {
            b.ToTable("fee_challans");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).ValueGeneratedNever();
            b.HasAlternateKey(c => new { c.TenantId, c.Id });
            b.Property(c => c.BillingPeriod).HasMaxLength(10).IsRequired();
            b.Property(c => c.ChallanNumber).HasMaxLength(60).IsRequired();
            b.Property(c => c.CurrencyCode).HasMaxLength(10).IsRequired();
            b.Property(c => c.GrossAmount).HasPrecision(18, 2).IsRequired();
            b.Property(c => c.DiscountAmount).HasPrecision(18, 2).IsRequired();
            b.Property(c => c.LateFeeAmount).HasPrecision(18, 2).IsRequired();
            b.Property(c => c.NetAmount).HasPrecision(18, 2).IsRequired();
            b.Property(c => c.PaidAmount).HasPrecision(18, 2).IsRequired();
            b.Property(c => c.BalanceAmount).HasPrecision(18, 2).IsRequired();
            b.Property(c => c.Status).HasConversion<int>().IsRequired();
            b.Property(c => c.VoidReason).HasMaxLength(500);
            b.Property(c => c.CreatedBy).HasMaxLength(100);
            b.Property(c => c.UpdatedBy).HasMaxLength(100);
            b.Ignore(c => c.DomainEvents);
            b.HasIndex(c => new { c.TenantId, c.SchoolId, c.ChallanNumber }).IsUnique();
            // Prevent duplicate active challans per student & billing period (status = 5 => Voided)
            b.HasIndex(c => new { c.TenantId, c.StudentId, c.AcademicYearId, c.BillingPeriod })
                .IsUnique()
                .HasFilter("status <> 5")
                .HasDatabaseName("ix_fee_challans_unique_active_student_period");
            b.HasIndex(c => new { c.TenantId, c.CampusId, c.Status, c.DueDate });
            b.HasMany(c => c.Lines)
                .WithOne()
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .HasForeignKey(l => new { l.TenantId, l.ChallanId })
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(c => c.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<FeeChallanLine>(b =>
        {
            b.ToTable("fee_challan_lines");
            b.HasKey(l => l.Id);
            b.Property(l => l.Id).ValueGeneratedNever();
            b.Property(l => l.Description).HasMaxLength(200).IsRequired();
            b.Property(l => l.GrossAmount).HasPrecision(18, 2).IsRequired();
            b.Property(l => l.DiscountAmount).HasPrecision(18, 2).IsRequired();
            b.Property(l => l.NetAmount).HasPrecision(18, 2).IsRequired();
            b.Property(l => l.CreatedBy).HasMaxLength(100);
            b.Property(l => l.UpdatedBy).HasMaxLength(100);
            b.Ignore(l => l.DomainEvents);
            b.HasIndex(l => new { l.TenantId, l.ChallanId });
        });

        modelBuilder.Entity<FeeReceipt>(b =>
        {
            b.ToTable("fee_receipts");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).ValueGeneratedNever();
            b.Property(r => r.ReceiptNumber).HasMaxLength(60).IsRequired();
            b.Property(r => r.Amount).HasPrecision(18, 2).IsRequired();
            b.Property(r => r.CurrencyCode).HasMaxLength(10).IsRequired();
            b.Property(r => r.Method).HasConversion<int>().IsRequired();
            b.Property(r => r.ExternalReference).HasMaxLength(120);
            b.Property(r => r.CollectedBy).HasMaxLength(100).IsRequired();
            b.Property(r => r.Status).HasConversion<int>().IsRequired();
            b.Property(r => r.ReversalReason).HasMaxLength(500);
            b.Property(r => r.ReversedBy).HasMaxLength(100);
            b.Property(r => r.CreatedBy).HasMaxLength(100);
            b.Property(r => r.UpdatedBy).HasMaxLength(100);
            b.Ignore(r => r.DomainEvents);
            b.HasIndex(r => new { r.TenantId, r.SchoolId, r.ReceiptNumber }).IsUnique();
            b.HasIndex(r => new { r.TenantId, r.ChallanId });
            b.HasOne<FeeChallan>()
                .WithMany()
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .HasForeignKey(r => new { r.TenantId, r.ChallanId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FeePostingOutboxEntry>(b =>
        {
            b.ToTable("fee_posting_outbox");
            b.HasKey(o => o.Id);
            b.Property(o => o.Id).ValueGeneratedNever();
            b.Property(o => o.EventType).HasMaxLength(60).IsRequired();
            b.Property(o => o.Amount).HasPrecision(18, 2).IsRequired();
            b.Property(o => o.CurrencyCode).HasMaxLength(10).IsRequired();
            b.Property(o => o.PayloadJson).IsRequired();
            b.Property(o => o.CreatedBy).HasMaxLength(100);
            b.Property(o => o.UpdatedBy).HasMaxLength(100);
            b.Ignore(o => o.DomainEvents);
            b.HasIndex(o => new { o.TenantId, o.OccurredAtUtc });
        });

        modelBuilder.Entity<ChangeAuditEntry>(b =>
        {
            b.ToTable("change_audit_entries");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.ActorUserId).HasMaxLength(100);
            b.Property(e => e.EntityType).HasMaxLength(64).IsRequired();
            b.Property(e => e.Action).HasConversion<int>().IsRequired();
            b.Property(e => e.ChangesJson).IsRequired();
            b.Property(e => e.TraceId).HasMaxLength(64);
        });

        modelBuilder.Entity<SecurityAuditEvent>(b =>
        {
            b.ToTable("security_audit_events");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.EventType).HasConversion<int>().IsRequired();
            b.Property(e => e.Outcome).HasConversion<int>().IsRequired();
            b.Property(e => e.ActorUserId).HasMaxLength(100);
            b.Property(e => e.TargetType).HasMaxLength(64);
            b.Property(e => e.TargetId).HasMaxLength(100);
            b.Property(e => e.SourceIp).HasMaxLength(64);
            b.Property(e => e.UserAgent).HasMaxLength(512);
            b.Property(e => e.TraceId).HasMaxLength(64);
        });

        base.OnModelCreating(modelBuilder);
    }
}

public sealed class FeesDbContextFactory : IDesignTimeDbContextFactory<FeesDbContext>
{
    public FeesDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("SCHOOLOS_LOCAL_DB")
            ?? "Host=127.0.0.1;Port=5432;Database=schoolos_local;Username=schoolos_dev;Password=schoolos_dev_password";

        var opts = new DbContextOptionsBuilder<FeesDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "fees"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FeesDbContext(opts, new TenantContext(null, null, "design-time"));
    }
}
