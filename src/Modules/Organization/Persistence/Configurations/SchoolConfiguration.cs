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

        // Composite alternate key to enforce database-level composite foreign key from campuses
        builder.HasAlternateKey(s => new { s.TenantId, s.Id })
            .HasName("ak_schools_tenant_id_id");

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

        // Relationships: Enforce composite FK (tenant_id, school_id) -> schools(tenant_id, id)
        builder.HasMany(s => s.Campuses)
            .WithOne(c => c.School)
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .HasForeignKey(c => new { c.TenantId, c.SchoolId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_campuses_schools_tenant_id_school_id");
    }
}
