using System.Text;
using System.Text.RegularExpressions;
using SchoolOS.BuildingBlocks.Localization;
using SchoolOS.Modules.Admissions.Domain;
using SchoolOS.Modules.Organization.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public partial class Wave4OrganizationAdmissionsLocalizationTests
{
    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderRegex();

    [Fact]
    public void Localization_ShouldHaveCompleteKeyAndPlaceholderParity_AcrossEnUrAr()
    {
        SupportedLanguage.IsRtl("en").ShouldBeFalse();
        SupportedLanguage.IsRtl("ur").ShouldBeTrue();
        SupportedLanguage.IsRtl("ur-PK").ShouldBeTrue();
        SupportedLanguage.IsRtl("ar-SA").ShouldBeTrue();
        SupportedLanguage.Normalize("ur-PK").ShouldBe("ur");
        SupportedLanguage.Normalize("fr-FR").ShouldBe("en");

        var enCatalog = LocalizedValidationMessages.Catalogs["en"];
        var urCatalog = LocalizedValidationMessages.Catalogs["ur"];
        var arCatalog = LocalizedValidationMessages.Catalogs["ar"];

        enCatalog.Keys.OrderBy(k => k).ShouldBe(urCatalog.Keys.OrderBy(k => k));
        enCatalog.Keys.OrderBy(k => k).ShouldBe(arCatalog.Keys.OrderBy(k => k));

        foreach (var key in enCatalog.Keys)
        {
            var enVal = enCatalog[key];
            var urVal = urCatalog[key];
            var arVal = arCatalog[key];

            string.IsNullOrWhiteSpace(enVal).ShouldBeFalse();
            string.IsNullOrWhiteSpace(urVal).ShouldBeFalse();
            string.IsNullOrWhiteSpace(arVal).ShouldBeFalse();

            var enPlaceholders = PlaceholderRegex().Matches(enVal).Select(m => m.Value).OrderBy(x => x).ToArray();
            var urPlaceholders = PlaceholderRegex().Matches(urVal).Select(m => m.Value).OrderBy(x => x).ToArray();
            var arPlaceholders = PlaceholderRegex().Matches(arVal).Select(m => m.Value).OrderBy(x => x).ToArray();

            urPlaceholders.ShouldBe(enPlaceholders, $"Placeholder mismatch in UR for key '{key}'.");
            arPlaceholders.ShouldBe(enPlaceholders, $"Placeholder mismatch in AR for key '{key}'.");
        }
    }

    [Fact]
    public void BrandingImageRules_ShouldAcceptValidSignatures_AndRejectSvgHtmlAndOversized()
    {
        byte[] validPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
        byte[] validJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        byte[] validWebp = "RIFF\x24\x00\x00\x00WEBPVP8 "u8.ToArray();
        byte[] validIco = [0x00, 0x00, 0x01, 0x00, 0x01, 0x00];

        BrandingImageRules.ValidateAndDetectContentType(validPng, BrandingImageSlot.Logo).ShouldBe("image/png");
        BrandingImageRules.ValidateAndDetectContentType(validJpeg, BrandingImageSlot.Logo).ShouldBe("image/jpeg");
        BrandingImageRules.ValidateAndDetectContentType(validWebp, BrandingImageSlot.Logo).ShouldBe("image/webp");
        BrandingImageRules.ValidateAndDetectContentType(validPng, BrandingImageSlot.Favicon).ShouldBe("image/png");
        BrandingImageRules.ValidateAndDetectContentType(validIco, BrandingImageSlot.Favicon).ShouldBe("image/x-icon");

        var svgPayload = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");
        var xmlSvg = Encoding.UTF8.GetBytes("  <?xml version=\"1.0\"?><svg></svg>");
        var htmlPayload = Encoding.UTF8.GetBytes("<!doctype html><html><script>alert(1)</script></html>");
        var oversizedLogo = new byte[BrandingImageRules.MaxLogoBytes + 1];
        Array.Copy(validPng, oversizedLogo, validPng.Length);

        Should.Throw<ArgumentException>(() => BrandingImageRules.ValidateAndDetectContentType(svgPayload, BrandingImageSlot.Logo));
        Should.Throw<ArgumentException>(() => BrandingImageRules.ValidateAndDetectContentType(xmlSvg, BrandingImageSlot.Favicon));
        Should.Throw<ArgumentException>(() => BrandingImageRules.ValidateAndDetectContentType(htmlPayload, BrandingImageSlot.Logo));
        Should.Throw<ArgumentException>(() => BrandingImageRules.ValidateAndDetectContentType(oversizedLogo, BrandingImageSlot.Logo));
    }

    [Fact]
    public void OrganizationDetails_ShouldValidateHexColorsAndContactRules()
    {
        var tenantId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var campusId = Guid.NewGuid();

        Should.Throw<ArgumentException>(() => SchoolBranding.Create(tenantId, schoolId, "blue", "#0f172a", "#0ea5e9"));
        var branding = SchoolBranding.Create(tenantId, schoolId, "#1A56DB", "#0F172A", "#0EA5E9");
        branding.PrimaryColorHex.ShouldBe("#1a56db");

        Should.Throw<ArgumentException>(() => CampusPhone.Create(tenantId, campusId, "Front Desk", "123", true));
        var phone = CampusPhone.Create(tenantId, campusId, "Front Desk", "+92 300 1234567", true);
        phone.PhoneNumber.ShouldBe("+92 300 1234567");

        Should.Throw<ArgumentException>(() => CampusDetails.Create(tenantId, campusId, "Street 1", "Lahore", "INVALID-POSTAL-CODE"));
        var details = CampusDetails.Create(tenantId, campusId, "Main Boulevard", "Lahore", "54000", "info@school.pk", "https://school.pk", 31.5204m, 74.3587m, "Asia/Karachi");
        details.City.ShouldBe("Lahore");

        var profile = SchoolProfile.Create(tenantId, schoolId, "Excellence in Learning", "Dr. Nadia", 2001, "principal@school.pk", "https://school.pk", "ur-PK");
        profile.DefaultLanguage.ShouldBe("ur");
    }

    [Fact]
    public void AdmissionEnquiry_ShouldTrackWorkflow_EnforceStudentAgeBounds_AndRejectDoubleConversion()
    {
        var tenantId = Guid.NewGuid();
        var campusId = Guid.NewGuid();
        var admissionDate = new DateOnly(2026, 9, 1);

        var enquiry = AdmissionEnquiry.Create(
            tenantId,
            campusId,
            "Zainab Fatima",
            "Imran Khan",
            "+92 321 9876543",
            "imran@example.pk",
            "Grade 5",
            "2026-2027");

        enquiry.Status.ShouldBe(AdmissionEnquiryStatus.New);

        enquiry.AddFollowUp(DateTimeOffset.UtcNow, FollowUpChannel.Phone, "Spoke with parent");
        enquiry.Status.ShouldBe(AdmissionEnquiryStatus.Contacted);

        enquiry.ScheduleVisit(DateTimeOffset.UtcNow.AddDays(2), "Imran Khan", "Science wing tour");
        enquiry.Status.ShouldBe(AdmissionEnquiryStatus.VisitScheduled);

        // Too young (age 2)
        Should.Throw<ArgumentOutOfRangeException>(() =>
            enquiry.ConvertToStudent("ADM-001", "Zainab", "Fatima", new DateOnly(2024, 6, 1), admissionDate));

        // Too old (age 31)
        Should.Throw<ArgumentOutOfRangeException>(() =>
            enquiry.ConvertToStudent("ADM-001", "Zainab", "Fatima", new DateOnly(1995, 1, 1), admissionDate));

        var student = enquiry.ConvertToStudent("ADM-2026-001", "Zainab", "Fatima", new DateOnly(2016, 5, 10), admissionDate);
        student.Status.ShouldBe(StudentStatus.Active);
        enquiry.Status.ShouldBe(AdmissionEnquiryStatus.Admitted);
        enquiry.ConvertedStudentProfileId.ShouldBe(student.Id);

        Should.Throw<InvalidOperationException>(() =>
            enquiry.ConvertToStudent("ADM-2026-002", "Zainab", "Fatima", new DateOnly(2016, 5, 10), admissionDate));
    }
}
