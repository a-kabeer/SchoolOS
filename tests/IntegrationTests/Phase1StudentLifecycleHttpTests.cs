using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Identity.Application;
using SchoolOS.Modules.Identity.Domain;
using SchoolOS.Modules.Students.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

public sealed class Phase1StudentLifecycleHttpTests : IClassFixture<LocalApiFactory>
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly LocalApiFactory _factory;
    private readonly JwtOptions _jwtOptions = new();

    public Phase1StudentLifecycleHttpTests(LocalApiFactory factory)
    {
        _factory = factory;
    }

    private string CreateToken(
        Guid tenantId,
        Guid? campusId = null,
        string[]? roles = null,
        string? userId = null) =>
        JwtTokenHelper.GenerateToken(
            _jwtOptions,
            userId ?? "01931a74-0000-7000-8000-000000000101",
            tenantId: tenantId,
            campusId: campusId,
            roles: roles ?? [SecurityPolicies.TenantAdminRole]);

    [Fact]
    public async Task StudentLifecycle_EndToEnd_ShouldEnforceAcademics_Guardians_Households_Enrollments_Transfers_And_AdmissionsConversion()
    {
        var tenantId = Guid.CreateVersion7();
        var client = _factory.CreateClient();

        // 0. Unauthenticated request must return 401
        var unauthResp = await client.GetAsync("/api/v1/students");
        unauthResp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // 1. Unauthorized role (no Students permission) must return 403
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(tenantId, roles: ["UnprivilegedGuest"]));
        var forbiddenResp = await client.GetAsync("/api/v1/students");
        forbiddenResp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // 2. Authenticate as TenantAdmin and import a signed lease enabling Students + Admissions
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(tenantId));

        var leasePayload = new LicenseLeasePayload(
            TenantId: tenantId,
            IssuedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddDays(30),
            KeyId: "test-key-v1",
            PlanCode: "ENTERPRISE",
            EnabledModules: [ModuleCode.Students, ModuleCode.Admissions],
            SequenceNumber: 2,
            PaidThroughUtc: DateTimeOffset.UtcNow.AddDays(20),
            GraceUntilUtc: DateTimeOffset.UtcNow.AddDays(25));

        var signedLease = OfflineLicenseLeaseVerifier.SignWithPrivateKey(leasePayload, _factory.TestSigningPrivateKeyPem);
        var importResp = await client.PostAsJsonAsync("/api/v1/local/licensing/lease", signedLease, WebJson);
        importResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify licensing status endpoint returns Valid + sequence 2
        var statusResp = await client.GetAsync("/api/v1/local/licensing/status");
        statusResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var statusDoc = await statusResp.Content.ReadFromJsonAsync<JsonElement>(WebJson);
        statusDoc.GetProperty("status").GetString().ShouldBe("Valid");
        statusDoc.GetProperty("sequenceNumber").GetInt64().ShouldBe(2);

        // Sequence rollback (seq 1 < stored seq 2) must be rejected
        var olderLease = OfflineLicenseLeaseVerifier.SignWithPrivateKey(
            leasePayload with { SequenceNumber = 1 },
            _factory.TestSigningPrivateKeyPem);
        var rollbackResp = await client.PostAsJsonAsync("/api/v1/local/licensing/lease", olderLease, WebJson);
        rollbackResp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // 3. Create School, Campus A, Campus B, Academic Year, Grade Level, Sections A & B
        var schoolResp = await client.PostAsJsonAsync(
            "/api/v1/organization/schools",
            new { name = "Crescent Model School", code = $"CMS-{Guid.NewGuid():N}"[..10] },
            WebJson);
        schoolResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var school = (await schoolResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var campusAResp = await client.PostAsJsonAsync(
            $"/api/v1/organization/schools/{school.Id}/campuses",
            new { name = "North Campus", code = "NORTH", address = "Model Town" },
            WebJson);
        campusAResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var campusA = (await campusAResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var campusBResp = await client.PostAsJsonAsync(
            $"/api/v1/organization/schools/{school.Id}/campuses",
            new { name = "South Campus", code = "SOUTH", address = "Johar Town" },
            WebJson);
        campusBResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var campusB = (await campusBResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var yearResp = await client.PostAsJsonAsync(
            "/api/v1/organization/academics/years",
            new
            {
                schoolId = school.Id,
                code = "2026-27",
                name = "Academic Year 2026-2027",
                startDate = "2026-04-01",
                endDate = "2027-03-31",
                isCurrent = true
            },
            WebJson);
        yearResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var year = (await yearResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var gradeResp = await client.PostAsJsonAsync(
            "/api/v1/organization/academics/grades",
            new { schoolId = school.Id, code = "G5", name = "Grade 5", sequenceOrder = 5 },
            WebJson);
        gradeResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var grade = (await gradeResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var sectionAResp = await client.PostAsJsonAsync(
            "/api/v1/organization/academics/sections",
            new
            {
                campusId = campusA.Id,
                academicYearId = year.Id,
                gradeLevelId = grade.Id,
                code = "5A",
                name = "Grade 5 - A",
                capacity = 30
            },
            WebJson);
        sectionAResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var sectionA = (await sectionAResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var sectionBResp = await client.PostAsJsonAsync(
            "/api/v1/organization/academics/sections",
            new
            {
                campusId = campusA.Id,
                academicYearId = year.Id,
                gradeLevelId = grade.Id,
                code = "5B",
                name = "Grade 5 - B",
                capacity = 30
            },
            WebJson);
        sectionBResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var sectionB = (await sectionBResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        // 4. Register Student 1 & verify duplicate admission number returns 409
        var student1Resp = await client.PostAsJsonAsync(
            "/api/v1/students",
            new
            {
                campusId = campusA.Id,
                admissionNumber = "CMS-2026-001",
                firstName = "Zara",
                lastName = "Malik",
                dateOfBirth = "2015-06-14",
                gender = "Female",
                admissionDate = "2026-04-02"
            },
            WebJson);
        student1Resp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var student1 = (await student1Resp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var dupAdmResp = await client.PostAsJsonAsync(
            "/api/v1/students",
            new
            {
                campusId = campusA.Id,
                admissionNumber = "cms-2026-001",
                firstName = "Duplicate",
                lastName = "Student",
                dateOfBirth = "2015-06-14",
                admissionDate = "2026-04-02"
            },
            WebJson);
        dupAdmResp.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // Register sibling (Student 2)
        var student2Resp = await client.PostAsJsonAsync(
            "/api/v1/students",
            new
            {
                campusId = campusA.Id,
                admissionNumber = "CMS-2026-002",
                firstName = "Omar",
                lastName = "Malik",
                dateOfBirth = "2017-02-11",
                gender = "Male",
                admissionDate = "2026-04-02"
            },
            WebJson);
        student2Resp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var student2 = (await student2Resp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        // 5. Create 2 Guardians and link both to Student 1 with distinct authority flags
        var fatherResp = await client.PostAsJsonAsync(
            "/api/v1/students/guardians",
            new { firstName = "Imran", lastName = "Malik", primaryPhone = "+923001110001", email = "imran@example.test", preferredChannel = "SMS" },
            WebJson);
        fatherResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var father = (await fatherResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var motherResp = await client.PostAsJsonAsync(
            "/api/v1/students/guardians",
            new { firstName = "Sadia", lastName = "Malik", primaryPhone = "+923001110002", email = "sadia@example.test", preferredChannel = "Email" },
            WebJson);
        motherResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var mother = (await motherResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var linkFather = await client.PostAsJsonAsync(
            $"/api/v1/students/{student1.Id}/guardians",
            new
            {
                guardianId = father.Id,
                relationshipType = GuardianRelationshipType.Father,
                isPrimaryContact = true,
                isEmergencyContact = true,
                hasLegalCustody = true,
                canCollectStudent = true,
                receivesAcademicNotices = true,
                receivesFinancialNotices = true
            },
            WebJson);
        linkFather.StatusCode.ShouldBe(HttpStatusCode.Created);

        var linkMother = await client.PostAsJsonAsync(
            $"/api/v1/students/{student1.Id}/guardians",
            new
            {
                guardianId = mother.Id,
                relationshipType = GuardianRelationshipType.Mother,
                isPrimaryContact = false,
                isEmergencyContact = true,
                hasLegalCustody = true,
                canCollectStudent = true,
                receivesAcademicNotices = true,
                receivesFinancialNotices = false
            },
            WebJson);
        linkMother.StatusCode.ShouldBe(HttpStatusCode.Created);

        // 6. Create Household & associate guardian + both siblings
        var hhResp = await client.PostAsJsonAsync(
            "/api/v1/students/households",
            new { householdCode = "HH-MALIK-01", displayName = "Malik Family", primaryGuardianId = father.Id },
            WebJson);
        hhResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var household = (await hhResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        // Primary guardian (father) is automatically enrolled as a member; re-adding returns 409 Conflict
        (await client.PostAsJsonAsync($"/api/v1/students/households/{household.Id}/members",
            new { guardianId = father.Id, roleLabel = "Primary Guardian", isBillingMember = true }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await client.PostAsJsonAsync($"/api/v1/students/households/{household.Id}/members",
            new { guardianId = mother.Id, roleLabel = "Co-Guardian", isBillingMember = false }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await client.PostAsJsonAsync($"/api/v1/students/households/{household.Id}/members",
            new { studentId = student1.Id, roleLabel = "Student", isBillingMember = false }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await client.PostAsJsonAsync($"/api/v1/students/households/{household.Id}/members",
            new { studentId = student2.Id, roleLabel = "Student", isBillingMember = false }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.Created);

        // 7. Enroll Student 1 in Section A, reject duplicate active enrollment, then transfer to Section B
        var enrollResp = await client.PostAsJsonAsync(
            $"/api/v1/students/{student1.Id}/enrollments",
            new
            {
                campusId = campusA.Id,
                academicYearId = year.Id,
                gradeLevelId = grade.Id,
                sectionId = sectionA.Id,
                rollNumber = "5A-01",
                startDate = "2026-04-05"
            },
            WebJson);
        enrollResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var dupEnrollResp = await client.PostAsJsonAsync(
            $"/api/v1/students/{student1.Id}/enrollments",
            new
            {
                campusId = campusA.Id,
                academicYearId = year.Id,
                gradeLevelId = grade.Id,
                sectionId = sectionB.Id,
                rollNumber = "5B-01",
                startDate = "2026-04-06"
            },
            WebJson);
        dupEnrollResp.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var transferResp = await client.PostAsJsonAsync(
            $"/api/v1/students/{student1.Id}/transfer",
            new
            {
                targetCampusId = campusA.Id,
                academicYearId = year.Id,
                targetGradeLevelId = grade.Id,
                targetSectionId = sectionB.Id,
                effectiveDate = "2026-08-15",
                rollNumber = "5B-12",
                reason = "Parent requested Section B"
            },
            WebJson);
        transferResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify enrollment history preserves both Transferred and Active records
        var enrollHistoryResp = await client.GetAsync($"/api/v1/students/{student1.Id}/enrollments");
        enrollHistoryResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var history = await enrollHistoryResp.Content.ReadFromJsonAsync<JsonElement>(WebJson);
        history.GetArrayLength().ShouldBe(2);
        var statuses = history.EnumerateArray().Select(e => e.GetProperty("status").GetString()).ToList();
        statuses.ShouldContain("Active");
        statuses.ShouldContain("Transferred");

        // 8. Status change + terminal Graduated guard
        var statusChangeResp = await client.PostAsJsonAsync(
            $"/api/v1/students/{student1.Id}/status",
            new { targetStatus = StudentLifecycleStatus.Graduated, reason = "Completed program", effectiveDate = "2027-03-31" },
            WebJson);
        statusChangeResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var invalidReactivateResp = await client.PostAsJsonAsync(
            $"/api/v1/students/{student1.Id}/status",
            new { targetStatus = StudentLifecycleStatus.Active, reason = "Should fail", effectiveDate = "2027-04-01" },
            WebJson);
        invalidReactivateResp.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // 9. Cross-campus isolation: Campus B scoped token cannot read or mutate Campus A student
        var campusBClient = _factory.CreateClient();
        campusBClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(tenantId, campusId: campusB.Id, roles: [SecurityPolicies.TenantAdminRole]));

        var crossCampusGet = await campusBClient.GetAsync($"/api/v1/students/{student1.Id}");
        crossCampusGet.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var crossCampusCreate = await campusBClient.PostAsJsonAsync(
            "/api/v1/students",
            new
            {
                campusId = campusA.Id, // forged campusId outside token's campus
                admissionNumber = "FORGED-001",
                firstName = "Forged",
                lastName = "Campus",
                dateOfBirth = "2015-05-01",
                admissionDate = "2026-04-02"
            },
            WebJson);
        crossCampusCreate.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // 10. Admissions enquiry conversion creates canonical Student in Students module idempotently
        var enqResp = await client.PostAsJsonAsync(
            "/api/v1/admissions/enquiries",
            new
            {
                campusId = campusA.Id,
                applicantName = "Hassan Raza",
                guardianName = "Raza Ali",
                guardianPhone = "+923009998877",
                guardianEmail = "raza@example.test",
                targetGrade = "Grade 5",
                academicYear = "2026-27"
            },
            WebJson);
        enqResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var enquiry = (await enqResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var convBody = new
        {
            admissionNumber = "CMS-2026-099",
            firstName = "Hassan",
            lastName = "Raza",
            dateOfBirth = "2015-08-20",
            admissionDate = "2026-04-02"
        };
        var convResp = await client.PostAsJsonAsync(
            $"/api/v1/admissions/enquiries/{enquiry.Id}/convert",
            convBody,
            WebJson);
        convResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var converted = await convResp.Content.ReadFromJsonAsync<JsonElement>(WebJson);
        var canonicalStudentId = converted.GetProperty("id").GetGuid();
        converted.GetProperty("wasExisting").GetBoolean().ShouldBeFalse();

        // Idempotent retry returns 200 OK with same canonical Student ID
        var convRetryResp = await client.PostAsJsonAsync(
            $"/api/v1/admissions/enquiries/{enquiry.Id}/convert",
            convBody,
            WebJson);
        convRetryResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var retryJson = await convRetryResp.Content.ReadFromJsonAsync<JsonElement>(WebJson);
        retryJson.GetProperty("id").GetGuid().ShouldBe(canonicalStudentId);
        retryJson.GetProperty("wasExisting").GetBoolean().ShouldBeTrue();

        // Converted student must exist in Students module detail endpoint
        var canonicalDetailResp = await client.GetAsync($"/api/v1/students/{canonicalStudentId}");
        canonicalDetailResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthLogout_ShouldRevokeUserSession_AndRejectSubsequentRefreshToken()
    {
        var tenantId = Guid.CreateVersion7();
        var email = $"logout-test-{Guid.NewGuid():N}@schoolos.test";
        const string password = "StrongPassword#2026!";

        using (var scope = _factory.Services.CreateScope())
        {
            var tenantContext = (TenantContext)scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.EnterPreAuthenticationTenantScope(tenantId, "system:test-seed");
            var authService = scope.ServiceProvider.GetRequiredService<IdentityAuthenticationService>();
            await authService.ProvisionUserAsync(
                tenantId,
                schoolId: null,
                email: email,
                firstName: "Logout",
                middleName: null,
                lastName: "Tester",
                userType: UserType.SchoolAdministrator,
                initialPassword: password,
                mustChangePasswordOnNextLogin: false);
        }

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var loginResp = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { tenantId, email, password },
            WebJson);
        loginResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>(WebJson);
        var accessToken = loginBody.GetProperty("accessToken").GetString()!;

        // Logout with Bearer token + refresh cookie
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var logoutResp = await client.PostAsync("/api/v1/auth/logout", null);
        logoutResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Attempting to refresh with the revoked session cookie must fail with 401
        client.DefaultRequestHeaders.Authorization = null;
        var refreshResp = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { }, WebJson);
        refreshResp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record IdDto(Guid Id);
}
