using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolOS.Modules.Organization.Domain;

namespace SchoolOS.Modules.Organization.Persistence.Configurations;

public sealed class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> builder)
    {
        builder.ToTable("academic_years", "organization", t =>
            t.HasCheckConstraint("ck_academic_years_date_range", "end_date >= start_date"));

        builder.HasKey(y => y.Id);
        builder.Property(y => y.Id).ValueGeneratedNever();
        builder.HasAlternateKey(y => new { y.TenantId, y.Id });

        builder.Property(y => y.Code).HasMaxLength(40).IsRequired();
        builder.Property(y => y.Name).HasMaxLength(120).IsRequired();
        builder.Property(y => y.CreatedBy).HasMaxLength(100);
        builder.Property(y => y.UpdatedBy).HasMaxLength(100);
        builder.Ignore(y => y.DomainEvents);

        builder.HasIndex(y => new { y.TenantId, y.SchoolId, y.Code }).IsUnique();
        builder.HasIndex(y => new { y.TenantId, y.SchoolId })
            .HasDatabaseName("ix_academic_years_single_current_per_school")
            .IsUnique()
            .HasFilter("is_current = true");

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(y => new { y.TenantId, y.SchoolId })
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GradeLevelConfiguration : IEntityTypeConfiguration<GradeLevel>
{
    public void Configure(EntityTypeBuilder<GradeLevel> builder)
    {
        builder.ToTable("grade_levels", "organization");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.HasAlternateKey(g => new { g.TenantId, g.Id });

        builder.Property(g => g.Code).HasMaxLength(40).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(120).IsRequired();
        builder.Property(g => g.CreatedBy).HasMaxLength(100);
        builder.Property(g => g.UpdatedBy).HasMaxLength(100);
        builder.Ignore(g => g.DomainEvents);

        builder.HasIndex(g => new { g.TenantId, g.SchoolId, g.Code }).IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(g => new { g.TenantId, g.SchoolId })
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("sections", "organization");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.HasAlternateKey(s => new { s.TenantId, s.Id });

        builder.Property(s => s.Code).HasMaxLength(40).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(120).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);
        builder.Ignore(s => s.DomainEvents);

        builder.HasIndex(s => new { s.TenantId, s.CampusId, s.AcademicYearId, s.GradeLevelId, s.Code }).IsUnique();

        builder.HasOne<Campus>()
            .WithMany()
            .HasForeignKey(s => new { s.TenantId, s.CampusId })
            .HasPrincipalKey(c => new { c.TenantId, c.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicYear>()
            .WithMany()
            .HasForeignKey(s => new { s.TenantId, s.AcademicYearId })
            .HasPrincipalKey(y => new { y.TenantId, y.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<GradeLevel>()
            .WithMany()
            .HasForeignKey(s => new { s.TenantId, s.GradeLevelId })
            .HasPrincipalKey(g => new { g.TenantId, g.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
