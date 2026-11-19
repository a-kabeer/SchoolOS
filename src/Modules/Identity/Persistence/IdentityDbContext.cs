using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Persistence;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Identity.Domain;

namespace SchoolOS.Modules.Identity.Persistence;

/// <summary>
/// Operational DbContext for the Identity &amp; Scoped RBAC module on the Local Data Plane.
/// Maps to the <c>identity</c> logical schema in PostgreSQL with composite tenant foreign keys and RLS.
/// </summary>
public class IdentityDbContext : SchoolOSDbContextBase
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<SchoolRole> SchoolRoles => Set<SchoolRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserCampusAssignment> UserCampusAssignments => Set<UserCampusAssignment>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<ChangeAuditEntry> ChangeAuditEntries => Set<ChangeAuditEntry>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.HasAlternateKey(u => new { u.TenantId, u.Id });
            b.Property(u => u.Email).HasMaxLength(User.EmailMaxLength).IsRequired();
            b.Property(u => u.FirstName).HasMaxLength(User.NameMaxLength).IsRequired();
            b.Property(u => u.MiddleName).HasMaxLength(User.NameMaxLength);
            b.Property(u => u.LastName).HasMaxLength(User.NameMaxLength).IsRequired();
            b.Property(u => u.PhoneNumber).HasMaxLength(30);
            b.Property(u => u.PreferredLanguage).HasMaxLength(10).IsRequired();
            b.Property(u => u.CreatedBy).HasMaxLength(100);
            b.Property(u => u.UpdatedBy).HasMaxLength(100);
            b.Ignore(u => u.FullName);
            b.Ignore(u => u.DomainEvents);
            b.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        });

        modelBuilder.Entity<UserCredential>(b =>
        {
            b.ToTable("user_credentials");
            b.HasKey(c => c.Id);
            b.Property(c => c.PasswordHash).HasMaxLength(512).IsRequired();
            b.Property(c => c.HashAlgorithm).HasMaxLength(50).IsRequired();
            b.Property(c => c.CreatedBy).HasMaxLength(100);
            b.Property(c => c.UpdatedBy).HasMaxLength(100);
            b.Ignore(c => c.DomainEvents);
            b.HasIndex(c => new { c.TenantId, c.UserId }).IsUnique();
            b.HasOne<User>()
                .WithOne()
                .HasForeignKey<UserCredential>(c => new { c.TenantId, c.UserId })
                .HasPrincipalKey<User>(u => new { u.TenantId, u.Id })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserSession>(b =>
        {
            b.ToTable("user_sessions");
            b.HasKey(s => s.Id);
            b.Property(s => s.RefreshTokenHash).HasMaxLength(128).IsRequired();
            b.Property(s => s.PreviousRefreshTokenHash).HasMaxLength(128);
            b.Property(s => s.CreatedFromIp).HasMaxLength(64);
            b.Property(s => s.UserAgent).HasMaxLength(512);
            b.Property(s => s.RevokedReason).HasMaxLength(100);
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.UpdatedBy).HasMaxLength(100);
            b.Ignore(s => s.DomainEvents);
            b.HasIndex(s => s.RefreshTokenHash).IsUnique();
            b.HasIndex(s => s.PreviousRefreshTokenHash);
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => new { s.TenantId, s.UserId })
                .HasPrincipalKey(u => new { u.TenantId, u.Id })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SchoolRole>(b =>
        {
            b.ToTable("school_roles");
            b.HasKey(r => r.Id);
            b.HasAlternateKey(r => new { r.TenantId, r.Id });
            b.Property(r => r.Name).HasMaxLength(SchoolRole.NameMaxLength).IsRequired();
            b.Property(r => r.Code).HasMaxLength(SchoolRole.NameMaxLength).IsRequired();
            b.Property(r => r.CreatedBy).HasMaxLength(100);
            b.Property(r => r.UpdatedBy).HasMaxLength(100);
            b.Ignore(r => r.DomainEvents);
            b.HasIndex(r => new { r.TenantId, r.SchoolId, r.Name }).IsUnique();
            b.HasMany(r => r.Permissions)
                .WithOne()
                .HasForeignKey(p => new { p.TenantId, p.SchoolRoleId })
                .HasPrincipalKey(r => new { r.TenantId, r.Id })
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(r => r.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("role_permissions");
            b.HasKey(p => p.Id);
            b.Property(p => p.CreatedBy).HasMaxLength(100);
            b.Property(p => p.UpdatedBy).HasMaxLength(100);
            b.Ignore(p => p.IsEmpty);
            b.Ignore(p => p.DomainEvents);
            b.HasIndex(p => new { p.TenantId, p.SchoolRoleId, p.Module }).IsUnique();
        });

        modelBuilder.Entity<UserCampusAssignment>(b =>
        {
            b.ToTable("user_campus_assignments");
            b.HasKey(a => a.Id);
            b.Property(a => a.CreatedBy).HasMaxLength(100);
            b.Property(a => a.UpdatedBy).HasMaxLength(100);
            b.Ignore(a => a.DomainEvents);
            b.HasIndex(a => new { a.TenantId, a.UserId, a.CampusId }).IsUnique();
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => new { a.TenantId, a.UserId })
                .HasPrincipalKey(u => new { u.TenantId, u.Id })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRoleAssignment>(b =>
        {
            b.ToTable("user_role_assignments");
            b.HasKey(a => a.Id);
            b.Property(a => a.CreatedBy).HasMaxLength(100);
            b.Property(a => a.UpdatedBy).HasMaxLength(100);
            b.Ignore(a => a.DomainEvents);
            b.HasIndex(a => new { a.TenantId, a.UserId, a.SchoolRoleId, a.CampusId }).IsUnique();
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => new { a.TenantId, a.UserId })
                .HasPrincipalKey(u => new { u.TenantId, u.Id })
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<SchoolRole>()
                .WithMany()
                .HasForeignKey(a => new { a.TenantId, a.SchoolRoleId })
                .HasPrincipalKey(r => new { r.TenantId, r.Id })
                .OnDelete(DeleteBehavior.Cascade);
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
