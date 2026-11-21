using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Attendance.Domain;

namespace SchoolOS.Modules.Attendance.Persistence;

public class AttendanceDbContext : SchoolOSDbContextBase
{
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<TeacherSectionAssignment> TeacherSectionAssignments => Set<TeacherSectionAssignment>();
    public DbSet<DailyAttendanceRegister> DailyAttendanceRegisters => Set<DailyAttendanceRegister>();
    public DbSet<AttendanceEntry> AttendanceEntries => Set<AttendanceEntry>();
    public DbSet<AttendanceCorrectionAudit> AttendanceCorrectionAudits => Set<AttendanceCorrectionAudit>();
    public DbSet<ChangeAuditEntry> ChangeAuditEntries => Set<ChangeAuditEntry>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("attendance");

        modelBuilder.Entity<StaffMember>(b =>
        {
            b.ToTable("staff_members");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).ValueGeneratedNever();
            b.HasAlternateKey(s => new { s.TenantId, s.Id });
            b.Property(s => s.EmployeeCode).HasMaxLength(50).IsRequired();
            b.Property(s => s.FirstName).HasMaxLength(100).IsRequired();
            b.Property(s => s.LastName).HasMaxLength(100).IsRequired();
            b.Property(s => s.Email).HasMaxLength(254);
            b.Property(s => s.Phone).HasMaxLength(30);
            b.Property(s => s.RoleType).HasConversion<int>().IsRequired();
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.UpdatedBy).HasMaxLength(100);
            b.Ignore(s => s.DomainEvents);
            b.HasIndex(s => new { s.TenantId, s.SchoolId, s.EmployeeCode }).IsUnique();
            b.HasIndex(s => new { s.TenantId, s.UserId })
                .IsUnique()
                .HasFilter("user_id IS NOT NULL")
                .HasDatabaseName("ix_staff_members_unique_user");
        });

        modelBuilder.Entity<TeacherSectionAssignment>(b =>
        {
            b.ToTable("teacher_section_assignments");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).ValueGeneratedNever();
            b.Property(a => a.CreatedBy).HasMaxLength(100);
            b.Property(a => a.UpdatedBy).HasMaxLength(100);
            b.Ignore(a => a.DomainEvents);
            b.HasIndex(a => new { a.TenantId, a.StaffMemberId, a.AcademicYearId, a.SectionId }).IsUnique();
            b.HasIndex(a => new { a.TenantId, a.SectionId, a.AcademicYearId });
            b.HasOne<StaffMember>()
                .WithMany()
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .HasForeignKey(a => new { a.TenantId, a.StaffMemberId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DailyAttendanceRegister>(b =>
        {
            b.ToTable("daily_attendance_registers");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).ValueGeneratedNever();
            b.HasAlternateKey(r => new { r.TenantId, r.Id });
            b.Property(r => r.MarkedBy).HasMaxLength(100).IsRequired();
            b.Property(r => r.Notes).HasMaxLength(1000);
            b.Property(r => r.CreatedBy).HasMaxLength(100);
            b.Property(r => r.UpdatedBy).HasMaxLength(100);
            b.Ignore(r => r.DomainEvents);
            b.HasIndex(r => new { r.TenantId, r.CampusId, r.AcademicYearId, r.SectionId, r.AttendanceDate })
                .IsUnique()
                .HasDatabaseName("ix_daily_attendance_registers_unique_section_date");
            b.HasMany(r => r.Entries)
                .WithOne()
                .HasPrincipalKey(r => new { r.TenantId, r.Id })
                .HasForeignKey(e => new { e.TenantId, e.RegisterId })
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(r => r.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<AttendanceEntry>(b =>
        {
            b.ToTable("attendance_entries");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            b.HasAlternateKey(e => new { e.TenantId, e.Id });
            b.Property(e => e.Status).HasConversion<int>().IsRequired();
            b.Property(e => e.Remarks).HasMaxLength(500);
            b.Property(e => e.LastCorrectionReason).HasMaxLength(500);
            b.Property(e => e.LastUpdatedBy).HasMaxLength(100).IsRequired();
            b.Property(e => e.CreatedBy).HasMaxLength(100);
            b.Property(e => e.UpdatedBy).HasMaxLength(100);
            b.Ignore(e => e.DomainEvents);
            b.HasIndex(e => new { e.TenantId, e.RegisterId, e.StudentId }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.StudentId });
        });

        modelBuilder.Entity<AttendanceCorrectionAudit>(b =>
        {
            b.ToTable("attendance_correction_audits");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).ValueGeneratedNever();
            b.Property(c => c.PreviousStatus).HasConversion<int>().IsRequired();
            b.Property(c => c.NewStatus).HasConversion<int>().IsRequired();
            b.Property(c => c.Reason).HasMaxLength(500).IsRequired();
            b.Property(c => c.CorrectedBy).HasMaxLength(100).IsRequired();
            b.Property(c => c.CreatedBy).HasMaxLength(100);
            b.Property(c => c.UpdatedBy).HasMaxLength(100);
            b.Ignore(c => c.DomainEvents);
            b.HasIndex(c => new { c.TenantId, c.EntryId });
            b.HasIndex(c => new { c.TenantId, c.StudentId, c.AttendanceDate });
            b.HasOne<AttendanceEntry>()
                .WithMany()
                .HasPrincipalKey(e => new { e.TenantId, e.Id })
                .HasForeignKey(c => new { c.TenantId, c.EntryId })
                .OnDelete(DeleteBehavior.Restrict);
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

public sealed class AttendanceDbContextFactory : IDesignTimeDbContextFactory<AttendanceDbContext>
{
    public AttendanceDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("SCHOOLOS_LOCAL_DB")
            ?? "Host=127.0.0.1;Port=5432;Database=schoolos_local;Username=schoolos_dev;Password=schoolos_dev_password";

        var opts = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "attendance"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AttendanceDbContext(opts, new TenantContext(null, null, "design-time"));
    }
}
