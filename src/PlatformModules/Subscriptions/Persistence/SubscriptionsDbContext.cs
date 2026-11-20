using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.PlatformModules.Subscriptions.Domain;

namespace SchoolOS.PlatformModules.Subscriptions.Persistence;

/// <summary>
/// Cloud Control Plane database context for commercial plans, price books, tenant subscriptions,
/// invoices, manual/gateway payments, add-on requests, and renewal reminders (<c>subscriptions</c> schema).
/// </summary>
public class SubscriptionsDbContext : SchoolOSDbContextBase
{
    public DbSet<PortalModule> PortalModules => Set<PortalModule>();
    public DbSet<ModuleDependency> ModuleDependencies => Set<ModuleDependency>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanModule> PlanModules => Set<PlanModule>();
    public DbSet<PriceBook> PriceBooks => Set<PriceBook>();
    public DbSet<PriceBookItem> PriceBookItems => Set<PriceBookItem>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AddOnRequest> AddOnRequests => Set<AddOnRequest>();
    public DbSet<RenewalReminder> RenewalReminders => Set<RenewalReminder>();
    public DbSet<ChangeAuditEntry> ChangeAuditEntries => Set<ChangeAuditEntry>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    public SubscriptionsDbContext(DbContextOptions<SubscriptionsDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("subscriptions");
        modelBuilder.HasSequence<long>("invoice_number_seq", "subscriptions")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.Entity<PortalModule>(b =>
        {
            b.ToTable("portal_modules");
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).ValueGeneratedNever();
            b.Property(m => m.Code).HasConversion<int>().IsRequired();
            b.Property(m => m.Name).HasMaxLength(PortalModule.NameMaxLength).IsRequired();
            b.Property(m => m.Description).HasMaxLength(PortalModule.DescriptionMaxLength).IsRequired();
            b.Property(m => m.CreatedBy).HasMaxLength(100);
            b.Property(m => m.UpdatedBy).HasMaxLength(100);
            b.Ignore(m => m.DomainEvents);
            b.HasIndex(m => m.Code).IsUnique();
        });

        modelBuilder.Entity<ModuleDependency>(b =>
        {
            b.ToTable("module_dependencies");
            b.HasKey(d => d.Id);
            b.Property(d => d.Id).ValueGeneratedNever();
            b.Property(d => d.CreatedBy).HasMaxLength(100);
            b.Property(d => d.UpdatedBy).HasMaxLength(100);
            b.Ignore(d => d.DomainEvents);
            b.HasIndex(d => new { d.ModuleId, d.RequiresModuleId }).IsUnique();
        });

        modelBuilder.Entity<Plan>(b =>
        {
            b.ToTable("plans");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).ValueGeneratedNever();
            b.Property(p => p.Code).HasMaxLength(Plan.CodeMaxLength).IsRequired();
            b.Property(p => p.Name).HasMaxLength(Plan.NameMaxLength).IsRequired();
            b.Property(p => p.Description).HasMaxLength(Plan.DescriptionMaxLength).IsRequired();
            b.Property(p => p.CreatedBy).HasMaxLength(100);
            b.Property(p => p.UpdatedBy).HasMaxLength(100);
            b.Ignore(p => p.DomainEvents);
            b.HasIndex(p => p.Code).IsUnique();
            b.HasMany(p => p.Modules)
                .WithOne()
                .HasForeignKey(pm => pm.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(p => p.Modules).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PlanModule>(b =>
        {
            b.ToTable("plan_modules");
            b.HasKey(pm => pm.Id);
            b.Property(pm => pm.Id).ValueGeneratedNever();
            b.Property(pm => pm.CreatedBy).HasMaxLength(100);
            b.Property(pm => pm.UpdatedBy).HasMaxLength(100);
            b.Ignore(pm => pm.DomainEvents);
            b.HasIndex(pm => new { pm.PlanId, pm.ModuleId }).IsUnique();
            b.HasOne<PortalModule>()
                .WithMany()
                .HasForeignKey(pm => pm.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PriceBook>(b =>
        {
            b.ToTable("price_books");
            b.HasKey(pb => pb.Id);
            b.Property(pb => pb.Id).ValueGeneratedNever();
            b.Property(pb => pb.CountryCode).HasMaxLength(2).IsRequired();
            b.Property(pb => pb.CurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(pb => pb.TaxRate).HasPrecision(5, 2);
            b.Property(pb => pb.CreatedBy).HasMaxLength(100);
            b.Property(pb => pb.UpdatedBy).HasMaxLength(100);
            b.Ignore(pb => pb.DomainEvents);
            b.HasIndex(pb => new { pb.CountryCode, pb.IsActive });
            b.HasMany(pb => pb.Items)
                .WithOne()
                .HasForeignKey(i => i.PriceBookId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(pb => pb.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PriceBookItem>(b =>
        {
            b.ToTable("price_book_items");
            b.HasKey(i => i.Id);
            b.Property(i => i.Id).ValueGeneratedNever();
            b.Property(i => i.BillingCycle).HasConversion<int>().IsRequired();
            b.Property(i => i.Price).HasPrecision(18, 2);
            b.Property(i => i.CreatedBy).HasMaxLength(100);
            b.Property(i => i.UpdatedBy).HasMaxLength(100);
            b.Ignore(i => i.DomainEvents);
            b.HasIndex(i => new { i.PriceBookId, i.PlanId, i.ModuleId, i.BillingCycle }).IsUnique();
        });

        modelBuilder.Entity<Subscription>(b =>
        {
            b.ToTable("subscriptions");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).ValueGeneratedNever();
            b.HasAlternateKey(s => new { s.TenantId, s.Id });
            b.Property(s => s.CountryCode).HasMaxLength(2).IsRequired();
            b.Property(s => s.CurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(s => s.BillingCycle).HasConversion<int>().IsRequired();
            b.Property(s => s.Status).HasConversion<int>().IsRequired();
            b.Property(s => s.Amount).HasPrecision(18, 2);
            b.Property(s => s.TaxRateOverride).HasPrecision(5, 2);
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.UpdatedBy).HasMaxLength(100);
            b.Ignore(s => s.DomainEvents);
            b.HasIndex(s => new { s.TenantId, s.Status });
            b.HasMany(s => s.Modules)
                .WithOne()
                .HasForeignKey(m => new { m.TenantId, m.SubscriptionId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(s => s.Modules).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TenantModule>(b =>
        {
            b.ToTable("tenant_modules");
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).ValueGeneratedNever();
            b.Property(m => m.ModuleCode).HasConversion<int>().IsRequired();
            b.Property(m => m.Status).HasConversion<int>().IsRequired();
            b.Property(m => m.CreatedBy).HasMaxLength(100);
            b.Property(m => m.UpdatedBy).HasMaxLength(100);
            b.Ignore(m => m.DomainEvents);
            b.HasIndex(m => new { m.TenantId, m.SubscriptionId, m.ModuleId }).IsUnique();
        });

        modelBuilder.Entity<Invoice>(b =>
        {
            b.ToTable("invoices");
            b.HasKey(i => i.Id);
            b.Property(i => i.Id).ValueGeneratedNever();
            b.HasAlternateKey(i => new { i.TenantId, i.Id });
            b.Property(i => i.InvoiceNumber).HasMaxLength(Invoice.NumberMaxLength).IsRequired();
            b.Property(i => i.CurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(i => i.Subtotal).HasPrecision(18, 2);
            b.Property(i => i.TaxRate).HasPrecision(5, 2);
            b.Property(i => i.TaxAmount).HasPrecision(18, 2);
            b.Property(i => i.Total).HasPrecision(18, 2);
            b.Property(i => i.Status).HasConversion<int>().IsRequired();
            b.Property(i => i.CreatedBy).HasMaxLength(100);
            b.Property(i => i.UpdatedBy).HasMaxLength(100);
            b.Ignore(i => i.DomainEvents);
            b.HasIndex(i => i.InvoiceNumber).IsUnique();
            b.HasIndex(i => new { i.TenantId, i.SubscriptionId, i.Status });
            b.HasOne<Subscription>()
                .WithMany()
                .HasForeignKey(i => new { i.TenantId, i.SubscriptionId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(i => i.Lines)
                .WithOne()
                .HasForeignKey(l => new { l.TenantId, l.InvoiceId })
                .HasPrincipalKey(i => new { i.TenantId, i.Id })
                .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(i => i.Payments)
                .WithOne()
                .HasForeignKey(p => new { p.TenantId, p.InvoiceId })
                .HasPrincipalKey(i => new { i.TenantId, i.Id })
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(i => i.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(i => i.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<InvoiceLine>(b =>
        {
            b.ToTable("invoice_lines");
            b.HasKey(l => l.Id);
            b.Property(l => l.Id).ValueGeneratedNever();
            b.Property(l => l.Description).HasMaxLength(InvoiceLine.DescriptionMaxLength).IsRequired();
            b.Property(l => l.Amount).HasPrecision(18, 2);
            b.Property(l => l.CreatedBy).HasMaxLength(100);
            b.Property(l => l.UpdatedBy).HasMaxLength(100);
            b.Ignore(l => l.DomainEvents);
        });

        modelBuilder.Entity<Payment>(b =>
        {
            b.ToTable("payments");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).ValueGeneratedNever();
            b.Property(p => p.Amount).HasPrecision(18, 2);
            b.Property(p => p.Method).HasConversion<int>().IsRequired();
            b.Property(p => p.Reference).HasMaxLength(Payment.ReferenceMaxLength).IsRequired();
            b.Property(p => p.ProviderName).HasMaxLength(Payment.ProviderMaxLength).IsRequired();
            b.Property(p => p.CreatedBy).HasMaxLength(100);
            b.Property(p => p.UpdatedBy).HasMaxLength(100);
            b.Ignore(p => p.DomainEvents);
        });

        modelBuilder.Entity<AddOnRequest>(b =>
        {
            b.ToTable("add_on_requests");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).ValueGeneratedNever();
            b.Property(r => r.ModuleCode).HasConversion<int>().IsRequired();
            b.Property(r => r.RequestedByUserId).HasMaxLength(100).IsRequired();
            b.Property(r => r.Notes).HasMaxLength(AddOnRequest.NotesMaxLength);
            b.Property(r => r.Status).HasConversion<int>().IsRequired();
            b.Property(r => r.DecidedByUserId).HasMaxLength(100);
            b.Property(r => r.CreatedBy).HasMaxLength(100);
            b.Property(r => r.UpdatedBy).HasMaxLength(100);
            b.Ignore(r => r.DomainEvents);
            b.HasIndex(r => new { r.TenantId, r.Status });
        });

        modelBuilder.Entity<RenewalReminder>(b =>
        {
            b.ToTable("renewal_reminders");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).ValueGeneratedNever();
            b.Property(r => r.CreatedBy).HasMaxLength(100);
            b.Property(r => r.UpdatedBy).HasMaxLength(100);
            b.Ignore(r => r.DomainEvents);
            b.HasIndex(r => new { r.TenantId, r.SubscriptionId, r.PeriodEnd, r.DaysBefore }).IsUnique();
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
