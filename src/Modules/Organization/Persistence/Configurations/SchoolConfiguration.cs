using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolOS.Modules.Organization.Domain;

namespace SchoolOS.Modules.Organization.Persistence.Configurations;

public class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("schools", "organization");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.CreatedBy)
            .HasMaxLength(100);

        builder.Property(s => s.UpdatedAtUtc);

        builder.Property(s => s.UpdatedBy)
            .HasMaxLength(100);

        // A school code must be unique within a tenant
        builder.HasIndex(s => new { s.TenantId, s.Code })
            .IsUnique()
            .HasDatabaseName("ix_schools_tenant_id_code");

        // Relationships
        builder.HasMany(s => s.Campuses)
            .WithOne(c => c.School)
            .HasForeignKey(c => c.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
