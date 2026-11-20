using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Admissions.Domain;
using SchoolOS.Modules.Organization.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

public sealed class Wave4LocalAdmissionsHttpTests : IClassFixture<LocalApiFactory>
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly LocalApiFactory _factory;
    private readonly JwtOptions _jwtOptions = new();

    public Wave4LocalAdmissionsHttpTests(LocalApiFactory factory)
    {
        _factory = factory;
    }

    private string CreateTenantAdminToken(Guid tenantId, Guid? campusId = null) =>
        JwtTokenHelper.GenerateToken(
            _jwtOptions,
            "01931a74-0000-7000-8000-000000000099",
            tenantId: tenantId,
            campusId: campusId,
            roles: [SecurityPolicies.TenantAdminRole]);

    [Fact]
    public async Task LocalApi_ShouldEnforceLicenseLease_AdmissionsWorkflow_SafeBrandingUpload_AndLocalizationHeaders()
    {
        var tenantId = Guid.CreateVersion7();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateTenantAdminToken(tenantId));

        // 1. Create School + Campus
        var schoolResp = await client.PostAsJsonAsync(
            "/api/v1/organization/schools",
            new { name = "Beaconhouse Wave4", code = $"BWH-{Guid.NewGuid():N}"[..10] },
            WebJson);
        schoolResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var school = await schoolResp.Content.ReadFromJsonAsync<CreatedIdResponse>(WebJson);
        school.ShouldNotBeNull();

        var campusResp = await client.PostAsJsonAsync(
            $"/api/v1/organization/schools/{school.Id}/campuses",
            new { name = "Main Campus", code = "MAIN", address = "Gulberg III" },
            WebJson);
        campusResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var campus = await campusResp.Content.ReadFromJsonAsync<CreatedIdResponse>(WebJson);
        campus.ShouldNotBeNull();

        // 2. Enrich Organization: CampusDetails, Phone, Branding, Safe PNG upload, Reject SVG, Localization headers
        var detailsResp = await client.PutAsJsonAsync(
            $"/api/v1/organization/campuses/{campus.Id}/details",
            new
            {
                addressLine1 = "12-B Gulberg",
                city = "Lahore",
                postalCode = "54660",
                email = "main@beacon.pk",
                websiteUrl = "https://beacon.pk",
                latitude = 31.51m,
                longitude = 74.35m,
                timezoneId = "Asia/Karachi"
            },
            WebJson);
        detailsResp.EnsureSuccessStatusCode();

        var brandColorResp = await client.PutAsJsonAsync(
            $"/api/v1/organization/schools/{school.Id}/branding",
            new { primaryColorHex = "#1A56DB", secondaryColorHex = "#0F172A", accentColorHex = "#0EA5E9" },
            WebJson);
        brandColorResp.EnsureSuccessStatusCode();

        byte[] validPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
        var pngUploadResp = await client.PutAsJsonAsync(
            $"/api/v1/organization/schools/{school.Id}/branding/images/{BrandingImageSlot.Logo}",
            new { base64Data = Convert.ToBase64String(validPng) },
            WebJson);
        pngUploadResp.EnsureSuccessStatusCode();

        var imgDownload = await client.GetAsync($"/api/v1/organization/schools/{school.Id}/branding/images/{BrandingImageSlot.Logo}");
        imgDownload.StatusCode.ShouldBe(HttpStatusCode.OK);
        imgDownload.Content.Headers.ContentType?.MediaType.ShouldBe("image/png");
        imgDownload.Headers.GetValues("Cross-Origin-Resource-Policy").ShouldContain("same-origin");

        var svgBytes = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");
        var svgResp = await client.PutAsJsonAsync(
            $"/api/v1/organization/schools/{school.Id}/branding/images/{BrandingImageSlot.Logo}",
            new { base64Data = Convert.ToBase64String(svgBytes) },
            WebJson);
        svgResp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify Accept-Language -> Content-Language middleware
        using var urReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/organization/schools/{school.Id}/branding");
        urReq.Headers.TryAddWithoutValidation("Accept-Language", "ur-PK");
        var urResp = await client.SendAsync(urReq);
        urResp.EnsureSuccessStatusCode();
        urResp.Content.Headers.ContentLanguage.ShouldContain("ur");

        // 3. Admissions WITHOUT a signed lease enabling Admissions -> 403 MODULE_NOT_ENABLED
        var blockedResp = await client.PostAsJsonAsync(
            "/api/v1/admissions/enquiries",
            new
            {
                campusId = campus.Id,
                applicantName = "Hamza Ali",
                guardianName = "Ali Raza",
                guardianPhone = "+923001234567",
                guardianEmail = "ali@example.pk",
                targetGrade = "Grade 3",
                academicYear = "2026-2027"
            },
            WebJson);
        blockedResp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await blockedResp.Content.ReadAsStringAsync()).ShouldContain("MODULE_NOT_ENABLED");

        // 4. Tampered ES256 license lease -> 400 BadRequest
        var now = DateTimeOffset.UtcNow;
        var validLease = OfflineLicenseLeaseVerifier.SignWithPrivateKey(
            new LicenseLeasePayload(tenantId, now, now.AddDays(14), "test-k1", "STARTER", [ModuleCode.School, ModuleCode.Admissions]),
            _factory.TestSigningPrivateKeyPem);

        var tamperedLease = validLease with { PayloadJson = validLease.PayloadJson.Replace("STARTER", "HACKED", StringComparison.Ordinal) };
        var badInstallResp = await client.PostAsJsonAsync("/api/v1/local/licensing/lease", tamperedLease, WebJson);
        badInstallResp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // 5. Install validly signed Cloud ES256 lease granting Admissions -> 200 OK
        var goodInstallResp = await client.PostAsJsonAsync("/api/v1/local/licensing/lease", validLease, WebJson);
        goodInstallResp.EnsureSuccessStatusCode();

        // 6. Full Admissions flow now unlocked!
        var enqResp = await client.PostAsJsonAsync(
            "/api/v1/admissions/enquiries",
            new
            {
                campusId = campus.Id,
                applicantName = "Hamza Ali",
                guardianName = "Ali Raza",
                guardianPhone = "+923001234567",
                guardianEmail = "ali@example.pk",
                targetGrade = "Grade 3",
                academicYear = "2026-2027",
                notes = "Sibling already enrolled"
            },
            WebJson);
        enqResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var enquiry = await enqResp.Content.ReadFromJsonAsync<CreatedIdResponse>(WebJson);
        enquiry.ShouldNotBeNull();

        var followUpResp = await client.PostAsJsonAsync(
            $"/api/v1/admissions/enquiries/{enquiry.Id}/follow-ups",
            new { followUpAtUtc = DateTimeOffset.UtcNow, channel = FollowUpChannel.Phone, notes = "Confirmed tour time" },
            WebJson);
        followUpResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var visitResp = await client.PostAsJsonAsync(
            $"/api/v1/admissions/enquiries/{enquiry.Id}/visits",
            new { scheduledAtUtc = DateTimeOffset.UtcNow.AddDays(1), visitorName = "Ali Raza", notes = "Primary block tour" },
            WebJson);
        visitResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var convertResp = await client.PostAsJsonAsync(
            $"/api/v1/admissions/enquiries/{enquiry.Id}/convert",
            new
            {
                admissionNumber = $"ADM-{Guid.NewGuid():N}"[..12],
                firstName = "Hamza",
                lastName = "Ali",
                dateOfBirth = new DateOnly(2018, 4, 15),
                admissionDate = new DateOnly(2026, 9, 1)
            },
            WebJson);
        convertResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var studentsResp = await client.GetAsync("/api/v1/admissions/students");
        studentsResp.EnsureSuccessStatusCode();
        (await studentsResp.Content.ReadAsStringAsync()).ShouldContain("Hamza");
    }

    private sealed record CreatedIdResponse(Guid Id);
}
