using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Domain;
using Xunit;

namespace SchoolOS.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
public class AuditActorSafetyTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public AuditActorSafetyTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AuthenticatedUser_Actor_Stamps_CreatedBy_And_CreatedAt()
    {
        var tenantId = Guid.CreateVersion7();
        var context = new TenantContext(tenantId, null, "user-alice");
        await using var db = _fixture.CreateDbContext(context);

        var school = School.Create(tenantId, "Alice School", "ALICE");
        db.Schools.Add(school);
        await db.SaveChangesAsync();

        school.CreatedBy.Should().Be("user-alice");
        school.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        school.UpdatedBy.Should().BeNull();
        school.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task MissingUser_Actor_FailsClosed_With_InvalidOperationException()
    {
        var tenantId = Guid.CreateVersion7();
        var context = new TenantContext(tenantId, null, null); // Missing user ID
        await using var db = _fixture.CreateDbContext(context);

        var school = School.Create(tenantId, "No User School", "NOUSER");
        db.Schools.Add(school);

        var act = () => db.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Audit integrity failure*without an authenticated user identifier*");
    }

    [Fact]
    public async Task ExplicitSystem_Actor_Allowed_For_Authorized_Background_Operations()
    {
        var tenantId = Guid.CreateVersion7();
        var context = new TenantContext(tenantId, null, "system:migration-daemon");
        await using var db = _fixture.CreateDbContext(context);

        var school = School.Create(tenantId, "System School", "SYS");
        db.Schools.Add(school);
        await db.SaveChangesAsync();

        school.CreatedBy.Should().Be("system:migration-daemon");
    }

    [Fact]
    public async Task Update_Preserves_CreationMetadata_And_Updates_UpdatedMetadata()
    {
        var tenantId = Guid.CreateVersion7();
        var authorUser = "author-bob";
        var initialContext = new TenantContext(tenantId, null, authorUser);

        Guid schoolId;
        DateTime originalCreatedAt;

        await using (var db1 = _fixture.CreateDbContext(initialContext))
        {
            var school = School.Create(tenantId, "Original Name", "ORIG");
            db1.Schools.Add(school);
            await db1.SaveChangesAsync();
            schoolId = school.Id;
            originalCreatedAt = school.CreatedAtUtc;
        }

        // Now modify as a different user
        var modifierUser = "modifier-charlie";
        var updateContext = new TenantContext(tenantId, null, modifierUser);

        await using (var db2 = _fixture.CreateDbContext(updateContext))
        {
            var school = await db2.Schools.FirstAsync(s => s.Id == schoolId);
            school.Update("Updated Name", "ORIG-UPD");
            await db2.SaveChangesAsync();

            school.CreatedBy.Should().Be("author-bob", "Original author must be preserved across updates");
            school.CreatedAtUtc.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(1), "Original creation timestamp must remain unmodified");
            school.UpdatedBy.Should().Be("modifier-charlie");
            school.UpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}
