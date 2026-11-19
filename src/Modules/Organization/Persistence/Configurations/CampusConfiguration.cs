using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolOS.Modules.Organization.Domain;

namespace SchoolOS.Modules.Organization.Persistence.Configurations;

public class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    public void Configure(EntityTypeBuilder<Campus> builder)
    {
        builder.ToTable("campuses", "organization");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.SchoolId)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100);

        builder.Property(c => c.UpdatedAtUtc);

        builder.Property(c => c.UpdatedBy)
            .HasMaxLength(100);

        // A campus code must be unique within a school
        builder.HasIndex(c => new { c.SchoolId, c.Code })
            .IsUnique()
            .HasDatabaseName("ix_campuses_school_id_code");

        // Composite index for fast tenant/school queries
        builder.HasIndex(c => new { c.TenantId, c.SchoolId })
            .HasDatabaseName("ix_campuses_tenant_id_school_id");
    }
}
