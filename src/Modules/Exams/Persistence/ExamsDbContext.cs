using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Exams.Domain;

namespace SchoolOS.Modules.Exams.Persistence;

public class ExamsDbContext : SchoolOSDbContextBase
{
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ExamTerm> ExamTerms => Set<ExamTerm>();
    public DbSet<ExamPaper> ExamPapers => Set<ExamPaper>();
    public DbSet<MarkSheet> MarkSheets => Set<MarkSheet>();
    public DbSet<StudentMark> StudentMarks => Set<StudentMark>();
    public DbSet<ChangeAuditEntry> ChangeAuditEntries => Set<ChangeAuditEntry>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    public ExamsDbContext(DbContextOptions<ExamsDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("exams");

        modelBuilder.Entity<Subject>(b =>
        {
            b.ToTable("subjects");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).ValueGeneratedNever();
            b.HasAlternateKey(s => new { s.TenantId, s.Id });
            b.Property(s => s.Code).HasMaxLength(40).IsRequired();
            b.Property(s => s.Name).HasMaxLength(120).IsRequired();
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.UpdatedBy).HasMaxLength(100);
            b.Ignore(s => s.DomainEvents);
            b.HasIndex(s => new { s.TenantId, s.SchoolId, s.GradeLevelId, s.Code }).IsUnique();
        });

        modelBuilder.Entity<ExamTerm>(b =>
        {
            b.ToTable("exam_terms");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).ValueGeneratedNever();
            b.HasAlternateKey(t => new { t.TenantId, t.Id });
            b.Property(t => t.Code).HasMaxLength(40).IsRequired();
            b.Property(t => t.Name).HasMaxLength(150).IsRequired();
            b.Property(t => t.Status).HasConversion<int>().IsRequired();
            b.Property(t => t.CreatedBy).HasMaxLength(100);
            b.Property(t => t.UpdatedBy).HasMaxLength(100);
            b.Ignore(t => t.DomainEvents);
            b.HasIndex(t => new { t.TenantId, t.CampusId, t.AcademicYearId, t.Code }).IsUnique();
        });

        modelBuilder.Entity<ExamPaper>(b =>
        {
            b.ToTable("exam_papers");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).ValueGeneratedNever();
            b.HasAlternateKey(p => new { p.TenantId, p.Id });
            b.Property(p => p.MaxMarks).HasPrecision(8, 2).IsRequired();
            b.Property(p => p.PassingMarks).HasPrecision(8, 2).IsRequired();
            b.Property(p => p.WeightagePercent).HasPrecision(5, 2).IsRequired();
            b.Property(p => p.CreatedBy).HasMaxLength(100);
            b.Property(p => p.UpdatedBy).HasMaxLength(100);
            b.Ignore(p => p.DomainEvents);
            b.HasIndex(p => new { p.TenantId, p.ExamTermId, p.GradeLevelId, p.SubjectId }).IsUnique();
            b.HasOne<ExamTerm>()
                .WithMany()
                .HasPrincipalKey(t => new { t.TenantId, t.Id })
                .HasForeignKey(p => new { p.TenantId, p.ExamTermId })
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Subject>()
                .WithMany()
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .HasForeignKey(p => new { p.TenantId, p.SubjectId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MarkSheet>(b =>
        {
            b.ToTable("mark_sheets");
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).ValueGeneratedNever();
            b.HasAlternateKey(m => new { m.TenantId, m.Id });
            b.Property(m => m.Status).HasConversion<int>().IsRequired();
            b.Property(m => m.SubmittedBy).HasMaxLength(100);
            b.Property(m => m.ApprovedBy).HasMaxLength(100);
            b.Property(m => m.ReviewNotes).HasMaxLength(1000);
            b.Property(m => m.CreatedBy).HasMaxLength(100);
            b.Property(m => m.UpdatedBy).HasMaxLength(100);
            b.Ignore(m => m.DomainEvents);
            b.HasIndex(m => new { m.TenantId, m.ExamPaperId, m.SectionId })
                .IsUnique()
                .HasDatabaseName("ix_mark_sheets_unique_paper_section");
            b.HasOne<ExamPaper>()
                .WithMany()
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(m => new { m.TenantId, m.ExamPaperId })
                .OnDelete(DeleteBehavior.Restrict);
            b.HasMany(m => m.Marks)
                .WithOne()
                .HasPrincipalKey(m => new { m.TenantId, m.Id })
                .HasForeignKey(sm => new { sm.TenantId, sm.MarkSheetId })
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(m => m.Marks).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<StudentMark>(b =>
        {
            b.ToTable("student_marks");
            b.HasKey(sm => sm.Id);
            b.Property(sm => sm.Id).ValueGeneratedNever();
            b.Property(sm => sm.MarksObtained).HasPrecision(8, 2).IsRequired();
            b.Property(sm => sm.Remarks).HasMaxLength(500);
            b.Property(sm => sm.CreatedBy).HasMaxLength(100);
            b.Property(sm => sm.UpdatedBy).HasMaxLength(100);
            b.Ignore(sm => sm.DomainEvents);
            b.HasIndex(sm => new { sm.TenantId, sm.MarkSheetId, sm.StudentId }).IsUnique();
            b.HasIndex(sm => new { sm.TenantId, sm.StudentId });
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

public sealed class ExamsDbContextFactory : IDesignTimeDbContextFactory<ExamsDbContext>
{
    public ExamsDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("SCHOOLOS_LOCAL_DB")
            ?? "Host=127.0.0.1;Port=5432;Database=schoolos_local;Username=schoolos_dev;Password=schoolos_dev_password";

        var opts = new DbContextOptionsBuilder<ExamsDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "exams"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ExamsDbContext(opts, new TenantContext(null, null, "design-time"));
    }
}
