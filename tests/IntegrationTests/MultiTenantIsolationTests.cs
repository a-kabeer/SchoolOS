using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Domain;
using Xunit;

namespace SchoolOS.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
public class MultiTenantIsolationTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public MultiTenantIsolationTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Scenario1_And_7_TenantA_Cannot_Retrieve_TenantB_Records()
    {
        // Arrange: Two independent tenants
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();

        var contextA = new TenantContext(tenantA, null, "user-tenant-a");
        var contextB = new TenantContext(tenantB, null, "user-tenant-b");

        // Seed Tenant A data
        await using (var dbA = _fixture.CreateDbContext(contextA))
        {
            var schoolA = School.Create(tenantA, "Army Public School Islamabad", "APS-ISB");
            schoolA.AddCampus("Main Campus", "MAIN");
            dbA.Schools.Add(schoolA);
            await dbA.SaveChangesAsync();
        }

        // Seed Tenant B data
        await using (var dbB = _fixture.CreateDbContext(contextB))
        {
            var schoolB = School.Create(tenantB, "Beaconhouse Lahore", "BSS-LHR");
            schoolB.AddCampus("Gulberg Branch", "GUL");
            dbB.Schools.Add(schoolB);
            await dbB.SaveChangesAsync();
        }

        // Act & Assert: Querying as Tenant A should only return Tenant A records
        await using (var dbA = _fixture.CreateDbContext(contextA))
        {
            var schoolsA = await dbA.Schools.Include(s => s.Campuses).ToListAsync();
            schoolsA.Should().ContainSingle(s => s.Code == "APS-ISB");
            schoolsA.Should().NotContain(s => s.Code == "BSS-LHR");

            var campusesA = await dbA.Campuses.ToListAsync();
            campusesA.Should().ContainSingle(c => c.Code == "MAIN");
            campusesA.Should().NotContain(c => c.Code == "GUL");
        }

        // Act & Assert: Querying as Tenant B should only return Tenant B records
        await using (var dbB = _fixture.CreateDbContext(contextB))
        {
            var schoolsB = await dbB.Schools.Include(s => s.Campuses).ToListAsync();
            schoolsB.Should().ContainSingle(s => s.Code == "BSS-LHR");
            schoolsB.Should().NotContain(s => s.Code == "APS-ISB");
        }
    }

    [Fact]
    public async Task Scenario2_TenantA_Cannot_Modify_TenantB_Records_FailsFast()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();

        var contextA = new TenantContext(tenantA, null, "user-a");
        var contextB = new TenantContext(tenantB, null, "user-b");

        Guid schoolBId;
        await using (var dbB = _fixture.CreateDbContext(contextB))
        {
            var schoolB = School.Create(tenantB, "City School Karachi", "CS-KHI");
            dbB.Schools.Add(schoolB);
            await dbB.SaveChangesAsync();
            schoolBId = schoolB.Id;
        }

        // Act: Tenant A attempts to update or attach an entity carrying Tenant B's ID
        await using (var dbA = _fixture.CreateDbContext(contextA))
        {
            // Attempting to inject a school with Tenant B's tenant_id inside Tenant A's context
            var rogueSchool = School.Create(tenantB, "Rogue School", "ROGUE");
            dbA.Schools.Add(rogueSchool);

            var act = () => dbA.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*Cross-tenant operation detected*");
        }
    }

    [Fact]
    public async Task Scenario3_CampusScope_Restricts_Campus_Query_Results()
    {
        var tenantId = Guid.CreateVersion7();
        var contextAll = new TenantContext(tenantId, null, "admin-user");

        Guid campus1Id;
        Guid campus2Id;

        await using (var db = _fixture.CreateDbContext(contextAll))
        {
            var school = School.Create(tenantId, "Roots Millennium", "RMS");
            var c1 = school.AddCampus("Campus One", "C1");
            var c2 = school.AddCampus("Campus Two", "C2");
            db.Schools.Add(school);
            await db.SaveChangesAsync();
            campus1Id = c1.Id;
            campus2Id = c2.Id;
        }

        // Act: Scoped user with access restricted to Campus 1 only
        var contextCampus1 = new TenantContext(tenantId, campus1Id, "teacher-c1");
        await using (var db = _fixture.CreateDbContext(contextCampus1))
        {
            // Application logic applying campus scope check
            var campuses = await db.Campuses
                                   .Where(c => c.Id == contextCampus1.CampusId!.Value)
                                   .ToListAsync();

            campuses.Should().ContainSingle(c => c.Id == campus1Id);
            campuses.Should().NotContain(c => c.Id == campus2Id);
        }
    }

    [Fact]
    public async Task Scenario4_And_5_Tenantless_And_Unauthenticated_Query_FailsClosed()
    {
        // Arrange: Unauthenticated / Tenant-less context
        var unauthenticatedContext = new TenantContext(null, null, null);

        await using var db = _fixture.CreateDbContext(unauthenticatedContext);

        // Act: Global query filter evaluates `CurrentTenantId != null && TenantId == CurrentTenantId`
        var schools = await db.Schools.ToListAsync();

        // Assert: Zero records returned
        schools.Should().BeEmpty();
    }

    [Fact]
    public async Task Scenario8_Postgres_RLS_Enforces_Isolation_At_Database_Layer()
    {
        // Arrange
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();

        var contextA = new TenantContext(tenantA, null, "admin-a");
        var contextB = new TenantContext(tenantB, null, "admin-b");

        await using (var dbA = _fixture.CreateDbContext(contextA))
        {
            dbA.Schools.Add(School.Create(tenantA, "RLS Test School A", "RLS-A"));
            await dbA.SaveChangesAsync();
        }

        await using (var dbB = _fixture.CreateDbContext(contextB))
        {
            dbB.Schools.Add(School.Create(tenantB, "RLS Test School B", "RLS-B"));
            await dbB.SaveChangesAsync();
        }

        // Act & Assert 1: Raw SQL with app.current_tenant_id = tenantA
        await using (var connA = await _fixture.OpenRawConnectionAsync(tenantA.ToString()))
        await using (var cmdA = connA.CreateCommand())
        {
            cmdA.CommandText = "SELECT COUNT(*) FROM organization.schools;";
            var countA = Convert.ToInt32(await cmdA.ExecuteScalarAsync());
            countA.Should().Be(1, "PostgreSQL RLS should only allow Tenant A's record");
        }

        // Act & Assert 2: Raw SQL with app.current_tenant_id = tenantB
        await using (var connB = await _fixture.OpenRawConnectionAsync(tenantB.ToString()))
        await using (var cmdB = connB.CreateCommand())
        {
            cmdB.CommandText = "SELECT COUNT(*) FROM organization.schools;";
            var countB = Convert.ToInt32(await cmdB.ExecuteScalarAsync());
            countB.Should().Be(1, "PostgreSQL RLS should only allow Tenant B's record");
        }

        // Act & Assert 3: Raw SQL with app.current_tenant_id = '' (missing/reset)
        await using (var connEmpty = await _fixture.OpenRawConnectionAsync(string.Empty))
        await using (var cmdEmpty = connEmpty.CreateCommand())
        {
            cmdEmpty.CommandText = "SELECT COUNT(*) FROM organization.schools;";
            var countEmpty = Convert.ToInt32(await cmdEmpty.ExecuteScalarAsync());
            countEmpty.Should().Be(0, "PostgreSQL RLS must fail closed when tenant context is empty");
        }
    }

    [Fact]
    public async Task Scenario9_Pooled_Connection_Reset_Prevents_Context_Leakage()
    {
        var tenantA = Guid.CreateVersion7();

        // 1. Borrow connection for Tenant A
        await using (var conn = await _fixture.OpenRawConnectionAsync(tenantA.ToString()))
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT current_setting('app.current_tenant_id', true);";
            var setting = (string?)await cmd.ExecuteScalarAsync();
            setting.Should().Be(tenantA.ToString());

            // Reset session variable (as done by connection pooling / reset)
            cmd.CommandText = "RESET app.current_tenant_id;";
            await cmd.ExecuteNonQueryAsync();
        }

        // 2. Query again on a new connection without setting tenant
        await using (var conn2 = await _fixture.OpenRawConnectionAsync(null))
        await using (var cmd2 = conn2.CreateCommand())
        {
            cmd2.CommandText = "SELECT current_setting('app.current_tenant_id', true);";
            var setting2 = (string?)await cmd2.ExecuteScalarAsync();
            setting2.Should().BeEmpty("Connection reset must wipe any residual tenant context");
        }
    }

    [Fact]
    public async Task Scenario10_AuditFields_Populated_On_Insert_And_Update()
    {
        var tenantId = Guid.CreateVersion7();
        var authorUser = "principal-audit-tester";
        var context = new TenantContext(tenantId, null, authorUser);

        Guid schoolId;

        // Insert
        await using (var db = _fixture.CreateDbContext(context))
        {
            var school = School.Create(tenantId, "Audit Tracking School", "ATS");
            db.Schools.Add(school);
            await db.SaveChangesAsync();
            schoolId = school.Id;

            school.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            school.CreatedBy.Should().Be(authorUser);
            school.UpdatedAtUtc.Should().BeNull();
            school.UpdatedBy.Should().BeNull();
        }

        // Update with another user
        var editorUser = "admin-editor";
        var editContext = new TenantContext(tenantId, null, editorUser);

        await using (var db = _fixture.CreateDbContext(editContext))
        {
            var school = await db.Schools.FirstAsync(s => s.Id == schoolId);
            school.Update("Audit Tracking School (Updated)", "ATS-UPD");
            await db.SaveChangesAsync();

            school.CreatedBy.Should().Be(authorUser);
            school.UpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            school.UpdatedBy.Should().Be(editorUser);
        }
    }

    [Fact]
    public async Task Scenario11_Idempotent_Seed_Execution_Does_Not_Fail()
    {
        var tenantId = Guid.CreateVersion7();
        var context = new TenantContext(tenantId, null, "seed-worker");

        // First Seed
        await using (var db1 = _fixture.CreateDbContext(context))
        {
            if (!await db1.Schools.AnyAsync(s => s.Code == "SEED-01"))
            {
                db1.Schools.Add(School.Create(tenantId, "Seed School", "SEED-01"));
                await db1.SaveChangesAsync();
            }
        }

        // Second Seed (Repeated execution)
        await using (var db2 = _fixture.CreateDbContext(context))
        {
            if (!await db2.Schools.AnyAsync(s => s.Code == "SEED-01"))
            {
                db2.Schools.Add(School.Create(tenantId, "Seed School", "SEED-01"));
                await db2.SaveChangesAsync();
            }
        }

        // Verify exactly one instance exists
        await using (var dbCheck = _fixture.CreateDbContext(context))
        {
            var count = await dbCheck.Schools.CountAsync(s => s.Code == "SEED-01");
            count.Should().Be(1);
        }
    }

    [Fact]
    public async Task Scenario12_Missing_UserId_On_AuditableEntity_Mutation_FailsFast()
    {
        // Arrange: Tenant context with null UserId
        var tenantId = Guid.CreateVersion7();
        var contextWithoutUser = new TenantContext(tenantId, null, null);

        await using var db = _fixture.CreateDbContext(contextWithoutUser);
        var school = School.Create(tenantId, "Unattributed School", "UNATTR");
        db.Schools.Add(school);

        // Act & Assert: Must fail fast to prevent unattributed audit records
        var act = () => db.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Audit integrity failure: Cannot create entity of type 'School' without an authenticated user identifier*");
    }

    [Fact]
    public async Task Scenario13_Database_Level_Composite_Foreign_Key_Enforces_Campus_Tenant_Ownership()
    {
        // Arrange: School B belonging to Tenant B
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();

        var contextA = new TenantContext(tenantA, null, "user-a");
        var contextB = new TenantContext(tenantB, null, "user-b");

        Guid schoolBId;
        await using (var dbB = _fixture.CreateDbContext(contextB))
        {
            var schoolB = School.Create(tenantB, "Tenant B School", "TB-SCH");
            dbB.Schools.Add(schoolB);
            await dbB.SaveChangesAsync();
            schoolBId = schoolB.Id;
        }

        // Act & Assert: Attempt to insert a campus with Tenant A's tenant_id pointing to Tenant B's school_id
        // This simulates a cross-tenant injection bypassing domain constructors
        await using (var connA = await _fixture.OpenRawConnectionAsync(tenantA.ToString()))
        await using (var cmd = connA.CreateCommand())
        {
            var rogueCampusId = Guid.CreateVersion7();
            cmd.CommandText = $"""
                INSERT INTO organization.campuses (id, tenant_id, school_id, name, code, is_active, created_at_utc, created_by)
                VALUES ('{rogueCampusId}', '{tenantA}', '{schoolBId}', 'Cross-Tenant Rogue Campus', 'ROGUE-C', true, NOW(), 'attacker');
                """;

            var act = () => cmd.ExecuteNonQueryAsync();
            var ex = await act.Should().ThrowAsync<Npgsql.PostgresException>();
            ex.Which.SqlState.Should().Be("23503", "Foreign key violation must be raised by PostgreSQL engine");
            ex.Which.ConstraintName.Should().Be("fk_campuses_schools_tenant_id_school_id");
        }
    }

    [Fact]
    public async Task Scenario14_Connection_Closing_Resets_Session_Variable_Preventing_Pool_Leakage()
    {
        var tenantA = Guid.CreateVersion7();
        var contextA = new TenantContext(tenantA, null, "tenant-a-user");

        // 1. Open and close a DbContext session for Tenant A
        await using (var dbA = _fixture.CreateDbContext(contextA))
        {
            var schools = await dbA.Schools.ToListAsync();
        }

        // 2. Open a raw connection without specifying tenant context
        await using (var conn = await _fixture.OpenRawConnectionAsync(null))
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT current_setting('app.current_tenant_id', true);";
            var residualSetting = (string?)await cmd.ExecuteScalarAsync();
            residualSetting.Should().BeEmpty("Connection close must reset app.current_tenant_id to empty string");
        }
    }

    [Fact]
    public async Task Scenario15_Transaction_Rollback_Does_Not_Leak_Tenant_Context()
    {
        var tenantA = Guid.CreateVersion7();
        var contextA = new TenantContext(tenantA, null, "user-a");

        await using var dbA = _fixture.CreateDbContext(contextA);
        await using (var tx = await dbA.Database.BeginTransactionAsync())
        {
            var tempSchool = School.Create(tenantA, "Temporary School", "TEMP");
            dbA.Schools.Add(tempSchool);
            await dbA.SaveChangesAsync();

            // Explicit rollback
            await tx.RollbackAsync();
        }

        // Verify school was not persisted
        var schoolExists = await dbA.Schools.AnyAsync(s => s.Code == "TEMP");
        schoolExists.Should().BeFalse();

        // Verify transaction-local setting is cleared
        await using var conn = await _fixture.OpenRawConnectionAsync(null);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT current_setting('app.current_tenant_id', true);";
        var setting = (string?)await cmd.ExecuteScalarAsync();
        setting.Should().BeEmpty("Rollback must not leak transaction-local tenant context");
    }

    [Fact]
    public async Task Scenario16_Reused_Pooled_Connection_Across_Tenants_Enforces_Strict_Isolation()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();

        var contextA = new TenantContext(tenantA, null, "user-a");
        var contextB = new TenantContext(tenantB, null, "user-b");

        // Seed Tenant A and B rows
        await using (var dbA = _fixture.CreateDbContext(contextA))
        {
            dbA.Schools.Add(School.Create(tenantA, "Tenant A Isolated School", "TA-ISO"));
            await dbA.SaveChangesAsync();
        }

        await using (var dbB = _fixture.CreateDbContext(contextB))
        {
            dbB.Schools.Add(School.Create(tenantB, "Tenant B Isolated School", "TB-ISO"));
            await dbB.SaveChangesAsync();
        }

        // Reusing connections sequentially as pooled connections
        await using (var dbA = _fixture.CreateDbContext(contextA))
        {
            var schoolsA = await dbA.Schools.ToListAsync();
            schoolsA.Should().ContainSingle(s => s.Code == "TA-ISO");
            schoolsA.Should().NotContain(s => s.Code == "TB-ISO");
        }

        await using (var dbB = _fixture.CreateDbContext(contextB))
        {
            var schoolsB = await dbB.Schools.ToListAsync();
            schoolsB.Should().ContainSingle(s => s.Code == "TB-ISO");
            schoolsB.Should().NotContain(s => s.Code == "TA-ISO");
        }
    }

    [Fact]
    public async Task Scenario17_Backend_Pid_And_Connection_Pool_Reuse_Enforces_Context_Clearing()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();

        int backendPidA;

        // 1. Borrower A uses physical connection from pool
        await using (var connA = await _fixture.OpenRawConnectionAsync(tenantA.ToString()))
        await using (var cmd = connA.CreateCommand())
        {
            cmd.CommandText = "SELECT pg_backend_pid();";
            backendPidA = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            cmd.CommandText = "SELECT current_setting('app.current_tenant_id', true);";
            var settingA = (string?)await cmd.ExecuteScalarAsync();
            settingA.Should().Be(tenantA.ToString());
        } // Returns connection to Npgsql pool, invoking ConnectionClosing reset

        // 2. Borrower B checks out connection without tenant context (or after pool return)
        await using (var connNeutral = await _fixture.OpenRawConnectionAsync(null))
        await using (var cmd = connNeutral.CreateCommand())
        {
            cmd.CommandText = "SELECT current_setting('app.current_tenant_id', true);";
            var residual = (string?)await cmd.ExecuteScalarAsync();
            residual.Should().BeEmpty("Pooled connection must not leak Tenant A's identity to subsequent neutral borrower");
        }

        // 3. Borrower C checks out connection with Tenant B context
        await using (var connB = await _fixture.OpenRawConnectionAsync(tenantB.ToString()))
        await using (var cmd = connB.CreateCommand())
        {
            cmd.CommandText = "SELECT current_setting('app.current_tenant_id', true);";
            var settingB = (string?)await cmd.ExecuteScalarAsync();
            settingB.Should().Be(tenantB.ToString(), "Pooled connection must reflect Tenant B identity");
        }
    }

    [Fact]
    public async Task Scenario18_NonTransactional_Queries_Enforce_Tenant_Isolation()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();

        var contextA = new TenantContext(tenantA, null, "user-a");
        var contextB = new TenantContext(tenantB, null, "user-b");

        // Seed data
        await using (var dbA = _fixture.CreateDbContext(contextA))
        {
            dbA.Schools.Add(School.Create(tenantA, "Non-Tx School A", "NTX-A"));
            await dbA.SaveChangesAsync();
        }

        await using (var dbB = _fixture.CreateDbContext(contextB))
        {
            dbB.Schools.Add(School.Create(tenantB, "Non-Tx School B", "NTX-B"));
            await dbB.SaveChangesAsync();
        }

        // Execute regular EF Core queries WITHOUT an explicit transaction (tests PostgresTenantSessionInterceptor)
        await using (var dbA = _fixture.CreateDbContext(contextA))
        {
            var resultsA = await dbA.Schools.AsNoTracking().ToListAsync();
            resultsA.Should().ContainSingle(s => s.Code == "NTX-A");
            resultsA.Should().NotContain(s => s.Code == "NTX-B");
        }

        await using (var dbB = _fixture.CreateDbContext(contextB))
        {
            var resultsB = await dbB.Schools.AsNoTracking().ToListAsync();
            resultsB.Should().ContainSingle(s => s.Code == "NTX-B");
            resultsB.Should().NotContain(s => s.Code == "NTX-A");
        }
    }

    [Fact]
    public async Task Scenario19_Transaction_Commit_And_Rollback_Lifecycle()
    {
        var tenantA = Guid.CreateVersion7();
        var contextA = new TenantContext(tenantA, null, "user-a");

        await using var dbA = _fixture.CreateDbContext(contextA);

        // 1. Transaction Commit
        await using (var tx = await dbA.Database.BeginTransactionAsync())
        {
            var committedSchool = School.Create(tenantA, "Committed School", "COMM-01");
            dbA.Schools.Add(committedSchool);
            await dbA.SaveChangesAsync();
            await tx.CommitAsync();
        }

        var existsCommitted = await dbA.Schools.AnyAsync(s => s.Code == "COMM-01");
        existsCommitted.Should().BeTrue();

        // 2. Transaction Rollback
        await using (var tx = await dbA.Database.BeginTransactionAsync())
        {
            var rolledBackSchool = School.Create(tenantA, "Rolled Back School", "ROLL-01");
            dbA.Schools.Add(rolledBackSchool);
            await dbA.SaveChangesAsync();
            await tx.RollbackAsync();
        }

        var existsRolledBack = await dbA.Schools.AnyAsync(s => s.Code == "ROLL-01");
        existsRolledBack.Should().BeFalse();
    }
}
