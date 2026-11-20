using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolOS.Modules.Organization.Domain;

namespace SchoolOS.Modules.Organization.Persistence.Configurations;

public sealed class CampusDetailsConfiguration : IEntityTypeConfiguration<CampusDetails>
{
    public void Configure(EntityTypeBuilder<CampusDetails> builder)
    {
        builder.ToTable("campus_details", "organization");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.AddressLine1).HasMaxLength(300).IsRequired();
        builder.Property(d => d.City).HasMaxLength(100).IsRequired();
        builder.Property(d => d.PostalCode).HasMaxLength(10).IsRequired();
        builder.Property(d => d.Email).HasMaxLength(254);
        builder.Property(d => d.WebsiteUrl).HasMaxLength(2048);
        builder.Property(d => d.Latitude).HasPrecision(9, 6);
        builder.Property(d => d.Longitude).HasPrecision(9, 6);
        builder.Property(d => d.TimezoneId).HasMaxLength(100).IsRequired();
        builder.Property(d => d.CreatedBy).HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);
        builder.Ignore(d => d.DomainEvents);
        builder.HasIndex(d => new { d.TenantId, d.CampusId }).IsUnique();
        builder.HasOne<Campus>()
            .WithMany()
            .HasPrincipalKey(c => new { c.TenantId, c.Id })
            .HasForeignKey(d => new { d.TenantId, d.CampusId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CampusPhoneConfiguration : IEntityTypeConfiguration<CampusPhone>
{
    public void Configure(EntityTypeBuilder<CampusPhone> builder)
    {
        builder.ToTable("campus_phones", "organization");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Label).HasMaxLength(60).IsRequired();
        builder.Property(p => p.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);
        builder.Ignore(p => p.DomainEvents);
        builder.HasIndex(p => new { p.TenantId, p.CampusId });
        builder.HasOne<Campus>()
            .WithMany()
            .HasPrincipalKey(c => new { c.TenantId, c.Id })
            .HasForeignKey(p => new { p.TenantId, p.CampusId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BrandingImageConfiguration : IEntityTypeConfiguration<BrandingImage>
{
    public void Configure(EntityTypeBuilder<BrandingImage> builder)
    {
        builder.ToTable("branding_images", "organization");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.Slot).HasConversion<int>().IsRequired();
        builder.Property(i => i.ContentType).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Data).IsRequired();
        builder.Property(i => i.Sha256Hash).HasMaxLength(64).IsRequired();
        builder.Property(i => i.CreatedBy).HasMaxLength(100);
        builder.Property(i => i.UpdatedBy).HasMaxLength(100);
        builder.Ignore(i => i.DomainEvents);
        builder.HasIndex(i => new { i.TenantId, i.SchoolId, i.Slot }).IsUnique();
        builder.HasOne<School>()
            .WithMany()
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .HasForeignKey(i => new { i.TenantId, i.SchoolId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SchoolBrandingConfiguration : IEntityTypeConfiguration<SchoolBranding>
{
    public void Configure(EntityTypeBuilder<SchoolBranding> builder)
    {
        builder.ToTable("school_brandings", "organization");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.PrimaryColorHex).HasMaxLength(7).IsRequired();
        builder.Property(b => b.SecondaryColorHex).HasMaxLength(7).IsRequired();
        builder.Property(b => b.AccentColorHex).HasMaxLength(7).IsRequired();
        builder.Property(b => b.CreatedBy).HasMaxLength(100);
        builder.Property(b => b.UpdatedBy).HasMaxLength(100);
        builder.Ignore(b => b.DomainEvents);
        builder.HasIndex(b => new { b.TenantId, b.SchoolId }).IsUnique();
        builder.HasOne<School>()
            .WithMany()
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .HasForeignKey(b => new { b.TenantId, b.SchoolId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SchoolProfileConfiguration : IEntityTypeConfiguration<SchoolProfile>
{
    public void Configure(EntityTypeBuilder<SchoolProfile> builder)
    {
        builder.ToTable("school_profiles", "organization");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Tagline).HasMaxLength(200);
        builder.Property(p => p.PrincipalName).HasMaxLength(150);
        builder.Property(p => p.ContactEmail).HasMaxLength(254);
        builder.Property(p => p.WebsiteUrl).HasMaxLength(2048);
        builder.Property(p => p.DefaultLanguage).HasMaxLength(10).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);
        builder.Ignore(p => p.DomainEvents);
        builder.HasIndex(p => new { p.TenantId, p.SchoolId }).IsUnique();
        builder.HasOne<School>()
            .WithMany()
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .HasForeignKey(p => new { p.TenantId, p.SchoolId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
