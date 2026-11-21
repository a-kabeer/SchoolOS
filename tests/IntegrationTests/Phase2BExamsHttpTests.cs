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

public sealed class Phase2BExamsHttpTests : IClassFixture<LocalApiFactory>
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly LocalApiFactory _factory;
    private readonly JwtOptions _jwtOptions = new();

    public Phase2BExamsHttpTests(LocalApiFactory factory)
    {
        _factory = factory;
    }

    private string CreateToken(
        Guid tenantId,
        Guid? campusId = null,
        string[]? roles = null,
        string? userId = null,
        string[]? scopes = null) =>
        JwtTokenHelper.GenerateToken(
            _jwtOptions,
            userId ?? "01931a74-0000-7000-8000-000000000401",
            tenantId: tenantId,
            campusId: campusId,
            roles: roles ?? [SecurityPolicies.TenantAdminRole],
            scopes: scopes);

    [Fact]
    public async Task Exams_EndToEnd_ShouldEnforceLicenseLease_TeacherScope_ApprovalWorkflow_Immutability_AndReportCard()
    {
        var tenantId = Guid.CreateVersion7();
        var teacherUserId = Guid.CreateVersion7();
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(tenantId));

        // 1. Setup School, Campus A, Campus B, Academic Year, Grade 5, Sections A & B
        var school = (await (await adminClient.PostAsJsonAsync("/api/v1/organization/schools",
            new { name = "City Grammar", code = $"CGS-{Guid.NewGuid():N}"[..10] }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var campusA = (await (await adminClient.PostAsJsonAsync($"/api/v1/organization/schools/{school.Id}/campuses",
            new { name = "North Campus", code = "NORTH", address = "Gulberg" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var campusB = (await (await adminClient.PostAsJsonAsync($"/api/v1/organization/schools/{school.Id}/campuses",
            new { name = "South Campus", code = "SOUTH", address = "DHA" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var year = (await (await adminClient.PostAsJsonAsync("/api/v1/organization/academics/years",
            new { schoolId = school.Id, code = "2026-27", name = "AY 2026-27", startDate = "2026-04-01", endDate = "2027-03-31", isCurrent = true }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var grade = (await (await adminClient.PostAsJsonAsync("/api/v1/organization/academics/grades",
            new { schoolId = school.Id, code = "G5", name = "Grade 5", sequenceOrder = 5 }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var sectionA = (await (await adminClient.PostAsJsonAsync("/api/v1/organization/academics/sections",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, code = "5A", name = "Grade 5 - A", capacity = 30 }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var sectionB = (await (await adminClient.PostAsJsonAsync("/api/v1/organization/academics/sections",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, code = "5B", name = "Grade 5 - B", capacity = 30 }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        // 2. Paid Exams module WITHOUT signed lease -> 403 Forbidden
        (await adminClient.GetAsync("/api/v1/exams/subjects")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // 3. Import ES256 signed lease enabling Students + Exams
        var lease = OfflineLicenseLeaseVerifier.SignWithPrivateKey(
            new LicenseLeasePayload(tenantId, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30), "k1", "PRO", [ModuleCode.Students, ModuleCode.Exams], SequenceNumber: 1),
            _factory.TestSigningPrivateKeyPem);
        (await adminClient.PostAsJsonAsync("/api/v1/local/licensing/lease", lease, WebJson)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // 4. Create Teacher staff & assign ONLY to Section A
        var staff = (await (await adminClient.PostAsJsonAsync("/api/v1/attendance/staff",
            new { campusId = campusA.Id, employeeCode = "T-501", firstName = "Nadia", lastName = "Kamran", roleType = StaffRoleType.Teacher, userId = teacherUserId }, WebJson))
            .Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        (await adminClient.PostAsJsonAsync($"/api/v1/attendance/staff/{staff.Id}/assignments",
            new { academicYearId = year.Id, gradeLevelId = grade.Id, sectionId = sectionA.Id, isHomeroomTeacher = true }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        // 5. Enroll 2 students in Section A
        var s1 = (await (await adminClient.PostAsJsonAsync("/api/v1/students",
            new { campusId = campusA.Id, admissionNumber = "CGS-001", firstName = "Bilal", lastName = "Aslam", dateOfBirth = "2015-05-01", admissionDate = "2026-04-01" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var s2 = (await (await adminClient.PostAsJsonAsync("/api/v1/students",
            new { campusId = campusA.Id, admissionNumber = "CGS-002", firstName = "Maryam", lastName = "Tariq", dateOfBirth = "2015-06-01", admissionDate = "2026-04-01" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        (await adminClient.PostAsJsonAsync($"/api/v1/students/{s1.Id}/enrollments",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, sectionId = sectionA.Id, rollNumber = "01", startDate = "2026-04-02" }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await adminClient.PostAsJsonAsync($"/api/v1/students/{s2.Id}/enrollments",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, sectionId = sectionA.Id, rollNumber = "02", startDate = "2026-04-02" }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.Created);

        // 6. Setup Subject (MATH), ExamTerm (MID-2026), ExamPaper (100 max / 40 passing)
        var math = (await (await adminClient.PostAsJsonAsync("/api/v1/exams/subjects",
            new { schoolId = school.Id, gradeLevelId = grade.Id, code = "MATH", name = "Mathematics" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var term = (await (await adminClient.PostAsJsonAsync("/api/v1/exams/terms",
            new { campusId = campusA.Id, academicYearId = year.Id, code = "MID-2026", name = "Midterm Examination 2026", startDate = "2026-11-10", endDate = "2026-11-25" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var paper = (await (await adminClient.PostAsJsonAsync("/api/v1/exams/papers",
            new { examTermId = term.Id, gradeLevelId = grade.Id, subjectId = math.Id, maxMarks = 100m, passingMarks = 40m, weightagePercent = 100m, examDate = "2026-11-15" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        // 7. Teacher-scoped client (assigned ONLY to Section A)
        var teacherClient = _factory.CreateClient();
        teacherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(
                tenantId,
                campusId: campusA.Id,
                roles: [SecurityPolicies.TeacherRole],
                userId: teacherUserId.ToString(),
                scopes: ["exams:view", "exams:create", "exams:edit"]));

        // Teacher CANNOT access unassigned Section B -> 403 Forbidden
        (await teacherClient.GetAsync($"/api/v1/exams/papers/{paper.Id}/sections/{sectionB.Id}/marksheet"))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Marks > MaxMarks -> 400 BadRequest
        (await teacherClient.PostAsJsonAsync($"/api/v1/exams/papers/{paper.Id}/sections/{sectionA.Id}/marks",
            new { entries = new[] { new { studentId = s1.Id, marksObtained = 105m, isAbsent = false } } }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Save valid Draft marks
        var saveResp = await teacherClient.PostAsJsonAsync($"/api/v1/exams/papers/{paper.Id}/sections/{sectionA.Id}/marks",
            new
            {
                entries = new[]
                {
                    new { studentId = s1.Id, marksObtained = 92m, isAbsent = false, remarks = "Excellent" },
                    new { studentId = s2.Id, marksObtained = 35m, isAbsent = false, remarks = "Needs review" }
                }
            }, WebJson);
        saveResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var markSheetId = (await saveResp.Content.ReadFromJsonAsync<JsonElement>(WebJson)).GetProperty("markSheetId").GetGuid();

        // Submit mark sheet
        (await teacherClient.PostAsync($"/api/v1/exams/marksheets/{markSheetId}/submit", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Teacher CANNOT self-approve -> 403 Forbidden
        (await teacherClient.PostAsJsonAsync($"/api/v1/exams/marksheets/{markSheetId}/review",
            new { approve = true, reviewNotes = "Self-approve attempt" }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // 8. Principal returns for correction -> Teacher fixes s2 to 68 -> Resubmits -> Principal approves
        var principalClient = _factory.CreateClient();
        principalClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(tenantId, campusId: campusA.Id, roles: [SecurityPolicies.PrincipalRole], scopes: ["exams:view", "exams:edit"]));

        (await principalClient.PostAsJsonAsync($"/api/v1/exams/marksheets/{markSheetId}/review",
            new { approve = false, reviewNotes = "Recheck Maryam's paper" }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await teacherClient.PostAsJsonAsync($"/api/v1/exams/papers/{paper.Id}/sections/{sectionA.Id}/marks",
            new { entries = new[] { new { studentId = s2.Id, marksObtained = 68m, isAbsent = false, remarks = "Corrected" } } }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        (await teacherClient.PostAsync($"/api/v1/exams/marksheets/{markSheetId}/submit", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await principalClient.PostAsJsonAsync($"/api/v1/exams/marksheets/{markSheetId}/review",
            new { approve = true, reviewNotes = "Approved for publication" }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Approved mark sheet is immutable -> 409 Conflict
        (await teacherClient.PostAsJsonAsync($"/api/v1/exams/papers/{paper.Id}/sections/{sectionA.Id}/marks",
            new { entries = new[] { new { studentId = s1.Id, marksObtained = 99m, isAbsent = false } } }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // 9. Cross-campus isolation (Campus B cannot read Campus A report card -> 403)
        var campusBClient = _factory.CreateClient();
        campusBClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(tenantId, campusId: campusB.Id));
        (await campusBClient.GetAsync($"/api/v1/exams/terms/{term.Id}/students/{s1.Id}/report-card"))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // 10. Approved Student Report Card returns exact grade & percentage (92/100 -> 92%, A+, Passed)
        var card = await (await adminClient.GetAsync($"/api/v1/exams/terms/{term.Id}/students/{s1.Id}/report-card"))
            .Content.ReadFromJsonAsync<JsonElement>(WebJson);
        card.GetProperty("totalObtainedMarks").GetDecimal().ShouldBe(92m);
        card.GetProperty("totalMaxMarks").GetDecimal().ShouldBe(100m);
        card.GetProperty("overallPercentage").GetDecimal().ShouldBe(92m);
        card.GetProperty("overallGrade").GetString().ShouldBe("A+");
        card.GetProperty("isOverallPassed").GetBoolean().ShouldBeTrue();
    }

    private sealed record IdDto(Guid Id);
}
