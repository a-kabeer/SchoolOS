using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Fees.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

public sealed class Phase2AFeesHttpTests : IClassFixture<LocalApiFactory>
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly LocalApiFactory _factory;
    private readonly JwtOptions _jwtOptions = new();

    public Phase2AFeesHttpTests(LocalApiFactory factory)
    {
        _factory = factory;
    }

    private string CreateToken(Guid tenantId, Guid? campusId = null) =>
        JwtTokenHelper.GenerateToken(
            _jwtOptions,
            "01931a74-0000-7000-8000-000000000301",
            tenantId: tenantId,
            campusId: campusId,
            roles: [SecurityPolicies.TenantAdminRole]);

    [Fact]
    public async Task Fees_EndToEnd_ShouldEnforceLicenseLease_SiblingDiscounts_IdempotentChallans_Receipts_Reversal_CampusIsolation_AndAging()
    {
        var tenantId = Guid.CreateVersion7();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(tenantId));

        // 1. Setup School, Campus A, Campus B, Academic Year, Grade 5
        var school = (await (await client.PostAsJsonAsync("/api/v1/organization/schools",
            new { name = "Beacon Fees School", code = $"BFS-{Guid.NewGuid():N}"[..10] }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var campusA = (await (await client.PostAsJsonAsync($"/api/v1/organization/schools/{school.Id}/campuses",
            new { name = "Main Campus", code = "MAIN", address = "Gulberg" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var campusB = (await (await client.PostAsJsonAsync($"/api/v1/organization/schools/{school.Id}/campuses",
            new { name = "West Campus", code = "WEST", address = "Model Town" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var year = (await (await client.PostAsJsonAsync("/api/v1/organization/academics/years",
            new { schoolId = school.Id, code = "2026-27", name = "AY 2026-27", startDate = "2026-04-01", endDate = "2027-03-31", isCurrent = true }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var grade = (await (await client.PostAsJsonAsync("/api/v1/organization/academics/grades",
            new { schoolId = school.Id, code = "G5", name = "Grade 5", sequenceOrder = 5 }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        // 2. Paid Fees module WITHOUT signed lease -> 403 Forbidden
        (await client.GetAsync("/api/v1/fees/heads")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // 3. Import ES256 signed lease enabling Students + Fees
        var lease = OfflineLicenseLeaseVerifier.SignWithPrivateKey(
            new LicenseLeasePayload(tenantId, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30), "k1", "PRO", [ModuleCode.Students, ModuleCode.Fees], SequenceNumber: 1),
            _factory.TestSigningPrivateKeyPem);
        (await client.PostAsJsonAsync("/api/v1/local/licensing/lease", lease, WebJson)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // 4. Register 3 students in Grade 5: 2 siblings in HH-KHAN + 1 single student
        var sib1 = (await (await client.PostAsJsonAsync("/api/v1/students",
            new { campusId = campusA.Id, admissionNumber = "BFS-001", firstName = "Zain", lastName = "Khan", dateOfBirth = "2015-05-01", admissionDate = "2026-04-01" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var sib2 = (await (await client.PostAsJsonAsync("/api/v1/students",
            new { campusId = campusA.Id, admissionNumber = "BFS-002", firstName = "Hira", lastName = "Khan", dateOfBirth = "2016-05-01", admissionDate = "2026-04-01" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        var solo = (await (await client.PostAsJsonAsync("/api/v1/students",
            new { campusId = campusA.Id, admissionNumber = "BFS-003", firstName = "Saad", lastName = "Tariq", dateOfBirth = "2015-08-01", admissionDate = "2026-04-01" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var hh = (await (await client.PostAsJsonAsync("/api/v1/students/households",
            new { householdCode = "HH-KHAN", displayName = "Khan Family" }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;
        (await client.PostAsJsonAsync($"/api/v1/students/households/{hh.Id}/members", new { studentId = sib1.Id, roleLabel = "Sibling" }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await client.PostAsJsonAsync($"/api/v1/students/households/{hh.Id}/members", new { studentId = sib2.Id, roleLabel = "Sibling" }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.Created);

        foreach (var sid in new[] { sib1.Id, sib2.Id, solo.Id })
        {
            (await client.PostAsJsonAsync($"/api/v1/students/{sid}/enrollments",
                new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, startDate = "2026-04-02" }, WebJson)).StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        // 5. Create FeeHead + FeeSchedule (10,000 PKR, 10% sibling discount)
        var head = (await (await client.PostAsJsonAsync("/api/v1/fees/heads",
            new { schoolId = school.Id, code = "TUITION", name = "Monthly Tuition", frequency = FeeFrequency.Monthly, isOptional = false }, WebJson)).Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        var schedResp = await client.PostAsJsonAsync("/api/v1/fees/schedules",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, feeHeadId = head.Id, amount = 10000m, currencyCode = "PKR", siblingDiscountPercent = 10m, lateFeePerDay = 50m }, WebJson);
        schedResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // 6. Generate 2026-11 monthly challans -> 9000 + 9000 + 10000 = 28000 PKR
        var genResp = await client.PostAsJsonAsync("/api/v1/fees/challans/generate",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, billingPeriod = "2026-11", issueDate = "2026-11-01", dueDate = "2026-11-10" }, WebJson);
        genResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var genJson = await genResp.Content.ReadFromJsonAsync<JsonElement>(WebJson);
        genJson.GetProperty("generatedCount").GetInt32().ShouldBe(3);
        genJson.GetProperty("skippedExistingCount").GetInt32().ShouldBe(0);
        genJson.GetProperty("totalNetAmount").GetDecimal().ShouldBe(28000m);

        // Idempotent re-run skips all 3
        var regenJson = await (await client.PostAsJsonAsync("/api/v1/fees/challans/generate",
            new { campusId = campusA.Id, academicYearId = year.Id, gradeLevelId = grade.Id, billingPeriod = "2026-11", issueDate = "2026-11-01", dueDate = "2026-11-10" }, WebJson))
            .Content.ReadFromJsonAsync<JsonElement>(WebJson);
        regenJson.GetProperty("generatedCount").GetInt32().ShouldBe(0);
        regenJson.GetProperty("skippedExistingCount").GetInt32().ShouldBe(3);

        var challans = await (await client.GetAsync("/api/v1/fees/challans?billingPeriod=2026-11")).Content.ReadFromJsonAsync<JsonElement>(WebJson);
        var sib1Challan = challans.EnumerateArray().First(c => c.GetProperty("studentId").GetGuid() == sib1.Id);
        var soloChallan = challans.EnumerateArray().First(c => c.GetProperty("studentId").GetGuid() == solo.Id);
        sib1Challan.GetProperty("netAmount").GetDecimal().ShouldBe(9000m);
        soloChallan.GetProperty("netAmount").GetDecimal().ShouldBe(10000m);

        var sib1ChallanId = sib1Challan.GetProperty("id").GetGuid();
        var soloChallanId = soloChallan.GetProperty("id").GetGuid();

        // 7. Post partial receipt (4000 Cash), reject overpayment (6000), post remaining (5000 BankTransfer)
        var r1Resp = await client.PostAsJsonAsync("/api/v1/fees/receipts",
            new { challanId = sib1ChallanId, amount = 4000m, paymentDate = "2026-11-05", method = PaymentMethod.Cash }, WebJson);
        r1Resp.StatusCode.ShouldBe(HttpStatusCode.Created);

        (await client.PostAsJsonAsync("/api/v1/fees/receipts",
            new { challanId = sib1ChallanId, amount = 6000m, paymentDate = "2026-11-06", method = PaymentMethod.Cash }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var r2Resp = await client.PostAsJsonAsync("/api/v1/fees/receipts",
            new { challanId = sib1ChallanId, amount = 5000m, paymentDate = "2026-11-06", method = PaymentMethod.BankTransfer, externalReference = "HBL-9981" }, WebJson);
        r2Resp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var r2 = (await r2Resp.Content.ReadFromJsonAsync<IdDto>(WebJson))!;

        // Paid challan cannot be voided -> 409 Conflict
        (await client.PostAsJsonAsync($"/api/v1/fees/challans/{sib1ChallanId}/void", new { reason = "Test" }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // 8. Reverse receipt r2 -> restores balance 5000 on sib1Challan
        var revResp = await client.PostAsJsonAsync($"/api/v1/fees/receipts/{r2.Id}/reverse", new { reason = "Duplicate bank entry" }, WebJson);
        revResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 9. Void unpaid soloChallan -> 200 OK
        (await client.PostAsJsonAsync($"/api/v1/fees/challans/{soloChallanId}/void", new { reason = "Student scholarship waiver" }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        // 10. Cross-campus isolation: Campus B token cannot collect payment on Campus A challan
        var campusBClient = _factory.CreateClient();
        campusBClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(tenantId, campusId: campusB.Id));
        (await campusBClient.PostAsJsonAsync("/api/v1/fees/receipts",
            new { challanId = sib1ChallanId, amount = 1000m, paymentDate = "2026-11-07", method = PaymentMethod.Cash }, WebJson))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // 11. Defaulter aging as of 2026-12-20 (40 days overdue -> 31-60 bucket):
        // sib1 balance = 5000, sib2 balance = 9000, solo = voided (0) -> Total = 14000
        var aging = await (await client.GetAsync("/api/v1/fees/defaulters?asOfDate=2026-12-20")).Content.ReadFromJsonAsync<JsonElement>(WebJson);
        aging.GetProperty("outstandingChallanCount").GetInt32().ShouldBe(2);
        aging.GetProperty("totalOutstanding").GetDecimal().ShouldBe(14000m);
        aging.GetProperty("days31To60Amount").GetDecimal().ShouldBe(14000m);
    }

    private sealed record IdDto(Guid Id);
}
