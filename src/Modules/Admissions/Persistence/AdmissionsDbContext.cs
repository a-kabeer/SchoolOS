using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Admissions.Domain;

namespace SchoolOS.Modules.Admissions.Persistence;

public class AdmissionsDbContext : SchoolOSDbContextBase
{
    public DbSet<AdmissionEnquiry> AdmissionEnquiries => Set<AdmissionEnquiry>();
    public DbSet<EnquiryFollowUp> EnquiryFollowUps => Set<EnquiryFollowUp>();
    public DbSet<CampusVisit> CampusVisits => Set<CampusVisit>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<ChangeAuditEntry> ChangeAuditEntries => Set<ChangeAuditEntry>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    public AdmissionsDbContext(DbContextOptions<AdmissionsDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("admissions");

        modelBuilder.Entity<AdmissionEnquiry>(b =>
        {
            b.ToTable("admission_enquiries");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            b.HasAlternateKey(e => new { e.TenantId, e.Id });
            b.Property(e => e.ApplicantName).HasMaxLength(150).IsRequired();
            b.Property(e => e.GuardianName).HasMaxLength(150).IsRequired();
            b.Property(e => e.GuardianPhone).HasMaxLength(30).IsRequired();
            b.Property(e => e.GuardianEmail).HasMaxLength(254);
            b.Property(e => e.TargetGrade).HasMaxLength(50).IsRequired();
            b.Property(e => e.AcademicYear).HasMaxLength(20).IsRequired();
            b.Property(e => e.Status).HasConversion<int>().IsRequired();
            b.Property(e => e.Notes).HasMaxLength(1000);
            b.Property(e => e.CreatedBy).HasMaxLength(100);
            b.Property(e => e.UpdatedBy).HasMaxLength(100);
            b.Ignore(e => e.DomainEvents);
            b.HasIndex(e => new { e.TenantId, e.CampusId, e.Status });
            b.HasMany(e => e.FollowUps)
                .WithOne()
                .HasPrincipalKey(e => new { e.TenantId, e.Id })
                .HasForeignKey(f => new { f.TenantId, f.EnquiryId })
                .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(e => e.Visits)
                .WithOne()
                .HasPrincipalKey(e => new { e.TenantId, e.Id })
                .HasForeignKey(v => new { v.TenantId, v.EnquiryId })
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(e => e.FollowUps).UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(e => e.Visits).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<EnquiryFollowUp>(b =>
        {
            b.ToTable("enquiry_follow_ups");
            b.HasKey(f => f.Id);
            b.Property(f => f.Id).ValueGeneratedNever();
            b.Property(f => f.Channel).HasConversion<int>().IsRequired();
            b.Property(f => f.Notes).HasMaxLength(1000).IsRequired();
            b.Property(f => f.CreatedBy).HasMaxLength(100);
            b.Property(f => f.UpdatedBy).HasMaxLength(100);
            b.Ignore(f => f.DomainEvents);
        });

        modelBuilder.Entity<CampusVisit>(b =>
        {
            b.ToTable("campus_visits");
            b.HasKey(v => v.Id);
            b.Property(v => v.Id).ValueGeneratedNever();
            b.Property(v => v.VisitorName).HasMaxLength(150).IsRequired();
            b.Property(v => v.Notes).HasMaxLength(1000);
            b.Property(v => v.CreatedBy).HasMaxLength(100);
            b.Property(v => v.UpdatedBy).HasMaxLength(100);
            b.Ignore(v => v.DomainEvents);
        });

        modelBuilder.Entity<StudentProfile>(b =>
        {
            b.ToTable("student_profiles");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).ValueGeneratedNever();
            b.Property(s => s.AdmissionNumber).HasMaxLength(50).IsRequired();
            b.Property(s => s.FirstName).HasMaxLength(100).IsRequired();
            b.Property(s => s.LastName).HasMaxLength(100).IsRequired();
            b.Property(s => s.GuardianName).HasMaxLength(150).IsRequired();
            b.Property(s => s.GuardianPhone).HasMaxLength(30).IsRequired();
            b.Property(s => s.GuardianEmail).HasMaxLength(254);
            b.Property(s => s.TargetGrade).HasMaxLength(50).IsRequired();
            b.Property(s => s.Status).HasConversion<int>().IsRequired();
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.UpdatedBy).HasMaxLength(100);
            b.Ignore(s => s.DomainEvents);
            b.HasIndex(s => new { s.TenantId, s.AdmissionNumber }).IsUnique();
        });

        modelBuilder.Entity<ChangeAuditEntry>(b =>
        {
            b.ToTable("change_audit_entries");
            b.HasKey(e => e.Id);
            b.Property(e => e.EntityType).HasMaxLength(ChangeAuditEntry.EntityTypeMaxLength).IsRequired();
            b.Property(e => e.ActorUserId).HasMaxLength(100);
            b.Property(e => e.TraceId).HasMaxLength(64);
        });

        modelBuilder.Entity<SecurityAuditEvent>(b =>
        {
            b.ToTable("security_audit_events");
            b.HasKey(e => e.Id);
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
