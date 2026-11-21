using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Attendance.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

public sealed class Phase1CAttendanceHttpTests : IClassFixture<LocalApiFactory>
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly LocalApiFactory _factory;
    private readonly JwtOptions _jwtOptions = new();

    public Phase1CAttendanceHttpTests(LocalApiFactory factory)
    {
        _factory = factory;
    }

    private string CreateToken(
        Guid tenantId,
        Guid? campusId = null,
        string[]? roles = null,
        string? userId = null,
        string[]? permissions = null) =>
        JwtTokenHelper.GenerateToken(
            _jwtOptions,
            userId ?? "01931a74-0000-7000-8000-000000000201",
            tenantId: tenantId,
            campusId: campusId,
            roles: roles ?? [SecurityPolicies.TenantAdminRole],
            scopes: permissions);

    [Fact]
    public async Task Attendance_EndToEnd_ShouldEnforceLicenseLease_TeacherSectionScope_CampusIsolation_IdempotentBulkMark_AndCorrectionAudit()
    {
        var tenantId = Guid.CreateVersion7();
        var teacherUserId = Guid.CreateVersion7();
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(tenantId));

        // 1. Setup School, Campus A, Campus B, Academic Year, Grade 5, Section A & Section B
        var school = (await (await adminClient.PostAsJsonAsync(
            "/api/v1/organization/schools",
            new { name = "Al-Noor Academy", code = $"ANA-{Guid.NewGuid():N}"[..10] },
            WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var campusA = (await (await adminClient.PostAsJsonAsync(
            $"/api/v1/organization/schools/{school.Id}/campuses",
            new { name = "City Campus", code = "CITY", address = "Mall Rd" },
            WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var campusB = (await (await adminClient.PostAsJsonAsync(
            $"/api/v1/organization/schools/{school.Id}/campuses",
            new { name = "Cantt Campus", code = "CANTT", address = "Cantt" },
            WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var year = (await (await adminClient.PostAsJsonAsync(
            "/api/v1/organization/academics/years",
            new { schoolId = school.Id, code = "2026-27", name = "AY 2026-27", startDate = "2026-04-01", endDate = "2027-03-31", isCurrent = true },
            WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var grade = (await (await adminClient.PostAsJsonAsync(
            "/api/v1/organization/academics/grades",
            new { schoolId = school.Id, code = "G5", name = "Grade 5", sequenceOrder = 5 },
            WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var sectionA = (await (await adminClient.PostAsJsonAsync(
            "/api/v1/organization/academics/sections",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, code = "5A", name = "Grade 5 - A", capacity = 30 },
            WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var sectionB = (await (await adminClient.PostAsJsonAsync(
            "/api/v1/organization/academics/sections",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, code = "5B", name = "Grade 5 - B", capacity = 30 },
            WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        // 2. Paid Attendance module WITHOUT signed lease -> 403 MODULE_NOT_ENABLED
        var blockedAttendance = await adminClient.GetAsync($"/api/v1/attendance/sections/{sectionA.Id}/register?date=2026-11-21");
        blockedAttendance.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // 3. Core Staff module works without paid lease -> create Teacher & assign ONLY to Section A
        var staffResp = await adminClient.PostAsJsonAsync(
            "/api/v1/attendance/staff",
            new
            {
                campusId = campusA.Id,
                employeeCode = "TCH-001",
                firstName = "Ayesha",
                lastName = "Siddiqui",
                roleType = StaffRoleType.HomeroomTeacher,
                userId = teacherUserId,
                email = "ayesha@alnoor.edu.pk"
            },
            WebJson);
        staffResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var teacherStaff = (await staffResp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var assignResp = await adminClient.PostAsJsonAsync(
            $"/api/v1/attendance/staff/{teacherStaff.Id}/assignments",
            new { academicYearId = year.Id, gradeLevelId = grade.Id, sectionId = sectionA.Id, isHomeroomTeacher = true },
            WebJson);
        assignResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // 4. Import signed ES256 lease enabling Students + Attendance
        var lease = OfflineLicenseLeaseVerifier.SignWithPrivateKey(
            new LicenseLeasePayload(
                tenantId,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddDays(30),
                "k1",
                "PRO",
                [ModuleCode.Students, ModuleCode.Attendance],
                SequenceNumber: 1),
            _factory.TestSigningPrivateKeyPem);
        (await adminClient.PostAsJsonAsync("/api/v1/local/licensing/lease", lease, WebJson)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // 5. Register 2 students and enroll them in Section A (plus 1 unenrolled student)
        var s1 = (await (await adminClient.PostAsJsonAsync("/api/v1/students",
            new { campusId = campusA.Id, admissionNumber = "ANA-001", firstName = "Ali", lastName = "Hassan", dateOfBirth = "2015-05-01", admissionDate = "2026-04-01" }, WebJson))
            .Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var s2 = (await (await adminClient.PostAsJsonAsync("/api/v1/students",
            new { campusId = campusA.Id, admissionNumber = "ANA-002", firstName = "Fatima", lastName = "Noor", dateOfBirth = "2015-06-01", admissionDate = "2026-04-01" }, WebJson))
            .Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var unenrolled = (await (await adminClient.PostAsJsonAsync("/api/v1/students",
            new { campusId = campusA.Id, admissionNumber = "ANA-003", firstName = "Umar", lastName = "Farooq", dateOfBirth = "2015-07-01", admissionDate = "2026-04-01" }, WebJson))
            .Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        (await adminClient.PostAsJsonAsync($"/api/v1/students/{s1.Id}/enrollments",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, sectionId = sectionA.Id, rollNumber = "01", startDate = "2026-04-02" }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.Created);
        (await adminClient.PostAsJsonAsync($"/api/v1/students/{s2.Id}/enrollments",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, sectionId = sectionA.Id, rollNumber = "02", startDate = "2026-04-02" }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        // 6. Teacher-scoped client (assigned ONLY to Section A)
        var teacherClient = _factory.CreateClient();
        teacherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(
                tenantId,
                campusId: campusA.Id,
                roles: [SecurityPolicies.TeacherRole],
                userId: teacherUserId.ToString(),
                permissions: ["attendance:view", "attendance:create"]));

        // Teacher can view Section A register with both enrolled students
        var regGet = await teacherClient.GetAsync($"/api/v1/attendance/sections/{sectionA.Id}/register?date=2026-11-21");
        regGet.StatusCode.ShouldBe(HttpStatusCode.OK);
        var regDoc = await regGet.Content.ReadFromJsonAsync<JsonElement>(WebJson);
        regDoc.GetProperty("students").GetArrayLength().ShouldBe(2);
        regDoc.GetProperty("isMarked").GetBoolean().ShouldBeFalse();

        // Teacher CANNOT view or mark Section B (not assigned!) -> 403 Forbidden
        (await teacherClient.GetAsync($"/api/v1/attendance/sections/{sectionB.Id}/register?date=2026-11-21"))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Cross-campus token (Campus B) CANNOT access Campus A section -> 403 Forbidden
        var campusBClient = _factory.CreateClient();
        campusBClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(tenantId, campusId: campusB.Id));
        (await campusBClient.GetAsync($"/api/v1/attendance/sections/{sectionA.Id}/register?date=2026-11-21"))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // 7. Marking an unenrolled student returns 409 Conflict
        var badMark = await teacherClient.PostAsJsonAsync(
            $"/api/v1/attendance/sections/{sectionA.Id}/register",
            new
            {
                attendanceDate = "2026-11-21",
                entries = new[] { new { studentId = unenrolled.Id, status = AttendanceStatus.Present } }
            },
            WebJson);
        badMark.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // 8. Bulk mark Section A (s1 Present, s2 Absent) -> 200 OK, createdCount = 2
        var markResp = await teacherClient.PostAsJsonAsync(
            $"/api/v1/attendance/sections/{sectionA.Id}/register",
            new
            {
                attendanceDate = "2026-11-21",
                notes = "Morning roll call",
                entries = new[]
                {
                    new { studentId = s1.Id, status = AttendanceStatus.Present, remarks = (string?)null, correctionReason = (string?)null },
                    new { studentId = s2.Id, status = AttendanceStatus.Absent, remarks = (string?)"Not present at 8am", correctionReason = (string?)null }
                }
            },
            WebJson);
        markResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var markJson = await markResp.Content.ReadFromJsonAsync<JsonElement>(WebJson);
        markJson.GetProperty("createdCount").GetInt32().ShouldBe(2);
        markJson.GetProperty("correctionsRecorded").GetInt32().ShouldBe(0);

        // 9. Changing s2 Absent -> Late WITHOUT correctionReason returns 400 BadRequest
        var missingReason = await teacherClient.PostAsJsonAsync(
            $"/api/v1/attendance/sections/{sectionA.Id}/register",
            new
            {
                attendanceDate = "2026-11-21",
                entries = new[]
                {
                    new { studentId = s2.Id, status = AttendanceStatus.Late, remarks = "Arrived 8:20", correctionReason = "" }
                }
            },
            WebJson);
        missingReason.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // 10. Changing s2 Absent -> Late WITH correctionReason succeeds and writes audit trail
        var corrected = await teacherClient.PostAsJsonAsync(
            $"/api/v1/attendance/sections/{sectionA.Id}/register",
            new
            {
                attendanceDate = "2026-11-21",
                entries = new[]
                {
                    new { studentId = s2.Id, status = AttendanceStatus.Late, remarks = "Arrived 8:20", correctionReason = "Late school van" }
                }
            },
            WebJson);
        corrected.StatusCode.ShouldBe(HttpStatusCode.OK);
        var correctedJson = await corrected.Content.ReadFromJsonAsync<JsonElement>(WebJson);
        correctedJson.GetProperty("correctionsRecorded").GetInt32().ShouldBe(1);

        var correctionsResp = await adminClient.GetAsync($"/api/v1/attendance/corrections?studentId={s2.Id}");
        correctionsResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corrections = await correctionsResp.Content.ReadFromJsonAsync<JsonElement>(WebJson);
        corrections.GetArrayLength().ShouldBe(1);
        corrections[0].GetProperty("previousStatus").GetString().ShouldBe("Absent");
        corrections[0].GetProperty("newStatus").GetString().ShouldBe("Late");
        corrections[0].GetProperty("reason").GetString().ShouldBe("Late school van");

        // 11. Section & Student Summary endpoints return exact counts (1 Present, 1 Late -> 100%)
        var secSummary = await (await teacherClient.GetAsync($"/api/v1/attendance/sections/{sectionA.Id}/summary?from=2026-11-01&to=2026-11-30"))
            .Content.ReadFromJsonAsync<JsonElement>(WebJson);
        secSummary.GetProperty("totalMarks").GetInt32().ShouldBe(2);
        secSummary.GetProperty("presentCount").GetInt32().ShouldBe(1);
        secSummary.GetProperty("lateCount").GetInt32().ShouldBe(1);
        secSummary.GetProperty("attendancePercentage").GetDecimal().ShouldBe(100m);

        var stdSummary = await (await adminClient.GetAsync($"/api/v1/attendance/students/{s1.Id}/summary?academicYearId={year.Id}"))
            .Content.ReadFromJsonAsync<JsonElement>(WebJson);
        stdSummary.GetProperty("presentCount").GetInt32().ShouldBe(1);
    }

    private sealed record IdDto(Guid Id);
}
