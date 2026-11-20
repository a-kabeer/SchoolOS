using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Students.Domain;

namespace SchoolOS.Modules.Students.Persistence;

public sealed class StudentsDbContext : SchoolOSDbContextBase
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentStatusHistory> StudentStatusHistory => Set<StudentStatusHistory>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ChangeAuditEntry> ChangeAuditEntries => Set<ChangeAuditEntry>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    public StudentsDbContext(DbContextOptions<StudentsDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("students");

        modelBuilder.Entity<Student>(b =>
        {
            b.ToTable("students");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).ValueGeneratedNever();
            b.HasAlternateKey(s => new { s.TenantId, s.Id });

            b.Property(s => s.AdmissionNumber).HasMaxLength(50).IsRequired();
            b.Property(s => s.FirstName).HasMaxLength(100).IsRequired();
            b.Property(s => s.MiddleName).HasMaxLength(100);
            b.Property(s => s.LastName).HasMaxLength(100).IsRequired();
            b.Property(s => s.Gender).HasMaxLength(30);
            b.Property(s => s.Status).HasConversion<int>().IsRequired();
            b.Property(s => s.Notes).HasMaxLength(1000);
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.UpdatedBy).HasMaxLength(100);
            b.Ignore(s => s.FullName);
            b.Ignore(s => s.DomainEvents);

            b.HasIndex(s => new { s.TenantId, s.SchoolId, s.AdmissionNumber }).IsUnique();
            b.HasIndex(s => new { s.TenantId, s.SourceEnquiryId })
                .HasDatabaseName("ix_students_unique_source_enquiry")
                .IsUnique()
                .HasFilter("source_enquiry_id IS NOT NULL");
            b.HasIndex(s => new { s.TenantId, s.CampusId, s.Status });

            b.HasMany(s => s.StatusHistory)
                .WithOne()
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .HasForeignKey(h => new { h.TenantId, h.StudentId })
                .OnDelete(DeleteBehavior.Restrict);
            b.Navigation(s => s.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<StudentStatusHistory>(b =>
        {
            b.ToTable("student_status_history");
            b.HasKey(h => h.Id);
            b.Property(h => h.Id).ValueGeneratedNever();
            b.Property(h => h.FromStatus).HasConversion<int>().IsRequired();
            b.Property(h => h.ToStatus).HasConversion<int>().IsRequired();
            b.Property(h => h.Reason).HasMaxLength(500).IsRequired();
            b.Property(h => h.ChangedBy).HasMaxLength(100).IsRequired();
            b.Property(h => h.CreatedBy).HasMaxLength(100);
            b.Property(h => h.UpdatedBy).HasMaxLength(100);
            b.Ignore(h => h.DomainEvents);
            b.HasIndex(h => new { h.TenantId, h.StudentId, h.ChangedAtUtc });
        });

        modelBuilder.Entity<Guardian>(b =>
        {
            b.ToTable("guardians");
            b.HasKey(g => g.Id);
            b.Property(g => g.Id).ValueGeneratedNever();
            b.HasAlternateKey(g => new { g.TenantId, g.Id });

            b.Property(g => g.FirstName).HasMaxLength(100).IsRequired();
            b.Property(g => g.LastName).HasMaxLength(100).IsRequired();
            b.Property(g => g.PrimaryPhone).HasMaxLength(30).IsRequired();
            b.Property(g => g.SecondaryPhone).HasMaxLength(30);
            b.Property(g => g.Email).HasMaxLength(254);
            b.Property(g => g.Occupation).HasMaxLength(120);
            b.Property(g => g.PreferredChannel).HasMaxLength(30).IsRequired();
            b.Property(g => g.CreatedBy).HasMaxLength(100);
            b.Property(g => g.UpdatedBy).HasMaxLength(100);
            b.Ignore(g => g.FullName);
            b.Ignore(g => g.DomainEvents);
            b.HasIndex(g => new { g.TenantId, g.PrimaryPhone });
        });

        modelBuilder.Entity<StudentGuardian>(b =>
        {
            b.ToTable("student_guardians");
            b.HasKey(sg => sg.Id);
            b.Property(sg => sg.Id).ValueGeneratedNever();
            b.Property(sg => sg.RelationshipType).HasConversion<int>().IsRequired();
            b.Property(sg => sg.CreatedBy).HasMaxLength(100);
            b.Property(sg => sg.UpdatedBy).HasMaxLength(100);
            b.Ignore(sg => sg.DomainEvents);

            b.HasIndex(sg => new { sg.TenantId, sg.StudentId, sg.GuardianId }).IsUnique();

            b.HasOne<Student>()
                .WithMany()
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .HasForeignKey(sg => new { sg.TenantId, sg.StudentId })
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Guardian>()
                .WithMany()
                .HasPrincipalKey(g => new { g.TenantId, g.Id })
                .HasForeignKey(sg => new { sg.TenantId, sg.GuardianId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Household>(b =>
        {
            b.ToTable("households");
            b.HasKey(h => h.Id);
            b.Property(h => h.Id).ValueGeneratedNever();
            b.HasAlternateKey(h => new { h.TenantId, h.Id });

            b.Property(h => h.HouseholdCode).HasMaxLength(40).IsRequired();
            b.Property(h => h.DisplayName).HasMaxLength(150).IsRequired();
            b.Property(h => h.CreatedBy).HasMaxLength(100);
            b.Property(h => h.UpdatedBy).HasMaxLength(100);
            b.Ignore(h => h.DomainEvents);
            b.HasIndex(h => new { h.TenantId, h.HouseholdCode }).IsUnique();
        });

        modelBuilder.Entity<HouseholdMember>(b =>
        {
            b.ToTable("household_members", t =>
                t.HasCheckConstraint(
                    "ck_household_members_single_ref",
                    "(student_id IS NOT NULL AND guardian_id IS NULL) OR (student_id IS NULL AND guardian_id IS NOT NULL)"));

            b.HasKey(m => m.Id);
            b.Property(m => m.Id).ValueGeneratedNever();
            b.Property(m => m.RoleLabel).HasMaxLength(60).IsRequired();
            b.Property(m => m.CreatedBy).HasMaxLength(100);
            b.Property(m => m.UpdatedBy).HasMaxLength(100);
            b.Ignore(m => m.DomainEvents);

            b.HasIndex(m => new { m.TenantId, m.HouseholdId, m.StudentId })
                .HasDatabaseName("ix_household_members_unique_student")
                .IsUnique()
                .HasFilter("student_id IS NOT NULL");

            b.HasIndex(m => new { m.TenantId, m.HouseholdId, m.GuardianId })
                .HasDatabaseName("ix_household_members_unique_guardian")
                .IsUnique()
                .HasFilter("guardian_id IS NOT NULL");

            b.HasOne<Household>()
                .WithMany()
                .HasPrincipalKey(h => new { h.TenantId, h.Id })
                .HasForeignKey(m => new { m.TenantId, m.HouseholdId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrollment>(b =>
        {
            b.ToTable("enrollments");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.RollNumber).HasMaxLength(40);
            b.Property(e => e.Status).HasConversion<int>().IsRequired();
            b.Property(e => e.Reason).HasMaxLength(500);
            b.Property(e => e.CreatedBy).HasMaxLength(100);
            b.Property(e => e.UpdatedBy).HasMaxLength(100);
            b.Ignore(e => e.DomainEvents);

            // Prevent concurrent duplicate active enrollments for the same student and academic year (status = 1 => Active)
            b.HasIndex(e => new { e.TenantId, e.StudentId, e.AcademicYearId })
                .HasDatabaseName("ix_enrollments_single_active_per_student_year")
                .IsUnique()
                .HasFilter("status = 1");

            b.HasIndex(e => new { e.TenantId, e.CampusId, e.AcademicYearId, e.GradeLevelId, e.SectionId });

            b.HasOne<Student>()
                .WithMany()
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .HasForeignKey(e => new { e.TenantId, e.StudentId })
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

public sealed class StudentsDbContextFactory : IDesignTimeDbContextFactory<StudentsDbContext>
{
    public StudentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StudentsDbContext>();
        optionsBuilder.UseNpgsql(
                "Host=127.0.0.1;Database=schoolos_local_design;Username=postgres;Password=dummy",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "students"))
            .UseSnakeCaseNamingConvention();

        return new StudentsDbContext(
            optionsBuilder.Options,
            new TenantContext(null, null, "design-time"));
    }
}
