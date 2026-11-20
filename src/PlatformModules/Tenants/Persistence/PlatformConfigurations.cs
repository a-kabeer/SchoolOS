using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolOS.PlatformModules.Tenants.Domain;

namespace SchoolOS.PlatformModules.Tenants.Persistence;

public sealed class SchoolRequestConfiguration : IEntityTypeConfiguration<SchoolRequest>
{
    public void Configure(EntityTypeBuilder<SchoolRequest> builder)
    {
        builder.ToTable("school_requests", "platform");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SchoolName).HasMaxLength(SchoolRequest.SchoolNameMaxLength).IsRequired();
        builder.Property(r => r.ContactName).HasMaxLength(SchoolRequest.ContactNameMaxLength).IsRequired();
        builder.Property(r => r.ContactEmail).HasMaxLength(SchoolRequest.ContactEmailMaxLength).IsRequired();
        builder.Property(r => r.Status).HasConversion<int>().IsRequired();
        builder.Property(r => r.ReviewedBy).HasMaxLength(100);
        builder.Property(r => r.DecisionNotes).HasMaxLength(SchoolRequest.DecisionNotesMaxLength);
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);
        builder.Ignore(r => r.DomainEvents);
        builder.HasIndex(r => r.Status);
    }
}

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries", "platform");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(Country.CodeLength).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(Country.NameMaxLength).IsRequired();
        builder.Property(c => c.LanguagesCsv).HasMaxLength(50).IsRequired();
        builder.Property(c => c.DefaultLanguage).HasMaxLength(10).IsRequired();
        builder.Property(c => c.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(c => c.CurrencyCode).HasMaxLength(Country.CurrencyCodeLength).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);
        builder.Ignore(c => c.DomainEvents);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasMany(c => c.States)
            .WithOne()
            .HasForeignKey(s => s.CountryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ToTable("states", "platform");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(State.CodeMaxLength).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(State.NameMaxLength).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);
        builder.Ignore(s => s.DomainEvents);
        builder.HasIndex(s => new { s.CountryId, s.Code }).IsUnique();
        builder.HasMany(s => s.Cities)
            .WithOne()
            .HasForeignKey(c => c.StateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities", "platform");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(City.NameMaxLength).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);
        builder.Ignore(c => c.DomainEvents);
        builder.HasIndex(c => new { c.StateId, c.Name }).IsUnique();
    }
}
