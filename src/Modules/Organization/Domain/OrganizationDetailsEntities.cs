using System.Security.Cryptography;
using System.Text;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Organization.Domain;

public enum BrandingImageSlot
{
    Logo = 1,
    Favicon = 2
}

/// <summary>
/// Rich address, contact, geo-coordinate, and timezone profile for a <see cref="Campus"/>.
/// </summary>
public sealed class CampusDetails : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private CampusDetails()
    {
        AddressLine1 = string.Empty;
        City = string.Empty;
        PostalCode = string.Empty;
        TimezoneId = "UTC";
    }

    private CampusDetails(
        Guid tenantId,
        Guid campusId,
        string addressLine1,
        string city,
        string postalCode,
        string? email,
        string? websiteUrl,
        decimal? latitude,
        decimal? longitude,
        string timezoneId) : this()
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        if (campusId == Guid.Empty)
        {
            throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        CampusId = campusId;
        Update(addressLine1, city, postalCode, email, websiteUrl, latitude, longitude, timezoneId);
    }

    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public string AddressLine1 { get; private set; }
    public string City { get; private set; }
    public string PostalCode { get; private set; }
    public string? Email { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string TimezoneId { get; private set; }

    public static CampusDetails Create(
        Guid tenantId,
        Guid campusId,
        string addressLine1,
        string city,
        string postalCode,
        string? email = null,
        string? websiteUrl = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string timezoneId = "UTC")
        => new(tenantId, campusId, addressLine1, city, postalCode, email, websiteUrl, latitude, longitude, timezoneId);

    public void Update(
        string addressLine1,
        string city,
        string postalCode,
        string? email,
        string? websiteUrl,
        decimal? latitude,
        decimal? longitude,
        string timezoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(addressLine1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(timezoneId);

        var trimmedPostal = postalCode.Trim();
        if (!ContactRules.IsValidPostalCode(trimmedPostal))
        {
            throw new ArgumentException("Invalid postal code format.", nameof(postalCode));
        }

        var trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        if (trimmedEmail is not null && !ContactRules.IsValidEmail(trimmedEmail))
        {
            throw new ArgumentException("Invalid campus email address.", nameof(email));
        }

        var trimmedUrl = string.IsNullOrWhiteSpace(websiteUrl) ? null : websiteUrl.Trim();
        if (trimmedUrl is not null && !ContactRules.IsValidWebsite(trimmedUrl))
        {
            throw new ArgumentException("Website URL must be an absolute http/https URI.", nameof(websiteUrl));
        }

        if (latitude is < -90m or > 90m)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude));
        }

        if (longitude is < -180m or > 180m)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude));
        }

        AddressLine1 = addressLine1.Trim();
        City = city.Trim();
        PostalCode = trimmedPostal;
        Email = trimmedEmail;
        WebsiteUrl = trimmedUrl;
        Latitude = latitude;
        Longitude = longitude;
        TimezoneId = timezoneId.Trim();
    }
}

/// <summary>
/// Validated E.164 campus telephone number.
/// </summary>
public sealed class CampusPhone : Entity<Guid>, ICampusScopedEntity
{
    private CampusPhone()
    {
        Label = string.Empty;
        PhoneNumber = string.Empty;
    }

    private CampusPhone(Guid tenantId, Guid campusId, string label, string phoneNumber, bool isPrimary) : this()
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        if (campusId == Guid.Empty)
        {
            throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        var trimmedPhone = phoneNumber.Trim();
        if (!ContactRules.IsValidPhone(trimmedPhone))
        {
            throw new ArgumentException("Phone number must contain 7 to 15 digits.", nameof(phoneNumber));
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        CampusId = campusId;
        Label = label.Trim();
        PhoneNumber = trimmedPhone;
        IsPrimary = isPrimary;
    }

    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public string Label { get; private set; }
    public string PhoneNumber { get; private set; }
    public bool IsPrimary { get; private set; }

    public static CampusPhone Create(Guid tenantId, Guid campusId, string label, string phoneNumber, bool isPrimary = false) =>
        new(tenantId, campusId, label, phoneNumber, isPrimary);
}

/// <summary>
/// Magic-byte content inspection for uploaded school branding images.
/// Rejects SVG, XML, HTML, or script payloads cleanly to prevent stored XSS (SEC-INPUT-04).
/// </summary>
public static class BrandingImageRules
{
    public const int MaxLogoBytes = 512 * 1024;
    public const int MaxFaviconBytes = 100 * 1024;
    public const int AbsoluteMaxBytes = 2 * 1024 * 1024;

    public static string ValidateAndDetectContentType(ReadOnlySpan<byte> bytes, BrandingImageSlot slot)
    {
        if (bytes.IsEmpty)
        {
            throw new ArgumentException("Image payload cannot be empty.", nameof(bytes));
        }

        var maxBytes = slot == BrandingImageSlot.Favicon ? MaxFaviconBytes : MaxLogoBytes;
        if (bytes.Length > maxBytes || bytes.Length > AbsoluteMaxBytes)
        {
            throw new ArgumentException($"Image exceeds maximum allowed size of {maxBytes / 1024} KB for {slot}.", nameof(bytes));
        }

        if (LooksLikeMarkupOrSvg(bytes))
        {
            throw new ArgumentException("SVG and active HTML/XML markup files are rejected for security reasons.", nameof(bytes));
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return "image/png";
        }

        // JPEG: FF D8 FF
        if (slot == BrandingImageSlot.Logo && bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        // WebP: RIFF....WEBP
        if (slot == BrandingImageSlot.Logo && bytes.Length >= 12 &&
            bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F' &&
            bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P')
        {
            return "image/webp";
        }

        // GIF: GIF87a / GIF89a
        if (slot == BrandingImageSlot.Logo && bytes.Length >= 6 &&
            bytes[0] == 'G' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == '8' &&
            (bytes[4] == '7' || bytes[4] == '9') && bytes[5] == 'a')
        {
            return "image/gif";
        }

        // ICO: 00 00 01 00
        if (slot == BrandingImageSlot.Favicon && bytes.Length >= 4 &&
            bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x01 && bytes[3] == 0x00)
        {
            return "image/x-icon";
        }

        throw new ArgumentException("Unsupported or unrecognized raster image signature.", nameof(bytes));
    }

    private static bool LooksLikeMarkupOrSvg(ReadOnlySpan<byte> bytes)
    {
        var headLen = Math.Min(bytes.Length, 512);
        var text = Encoding.UTF8.GetString(bytes[..headLen]).TrimStart();
        return text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<script", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Binary branding image (logo or favicon) verified via <see cref="BrandingImageRules"/>.
/// </summary>
public sealed class BrandingImage : Entity<Guid>, ITenantScopedEntity
{
    private BrandingImage()
    {
        ContentType = string.Empty;
        Data = [];
        Sha256Hash = string.Empty;
    }

    private BrandingImage(Guid tenantId, Guid schoolId, BrandingImageSlot slot, byte[] data, DateTimeOffset now) : this()
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        if (schoolId == Guid.Empty)
        {
            throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        Slot = slot;
        ReplaceContent(data, now);
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public BrandingImageSlot Slot { get; private set; }
    public string ContentType { get; private set; }
    public byte[] Data { get; private set; }
    public string Sha256Hash { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static BrandingImage Create(Guid tenantId, Guid schoolId, BrandingImageSlot slot, byte[] data, DateTimeOffset now) =>
        new(tenantId, schoolId, slot, data, now);

    public void ReplaceContent(byte[] data, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(data);
        ContentType = BrandingImageRules.ValidateAndDetectContentType(data, Slot);
        Data = (byte[])data.Clone();
        Sha256Hash = Convert.ToHexStringLower(SHA256.HashData(Data));
        UpdatedAt = now;
    }
}

/// <summary>
/// Theme hex palette configured per <see cref="School"/>.
/// </summary>
public sealed class SchoolBranding : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    private SchoolBranding()
    {
        PrimaryColorHex = "#1a56db";
        SecondaryColorHex = "#0f172a";
        AccentColorHex = "#0ea5e9";
    }

    private SchoolBranding(
        Guid tenantId,
        Guid schoolId,
        string primaryColorHex,
        string secondaryColorHex,
        string accentColorHex) : this()
    {
        if (tenantId == Guid.Empty || schoolId == Guid.Empty)
        {
            throw new ArgumentException("TenantId and SchoolId cannot be empty.");
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        UpdateColors(primaryColorHex, secondaryColorHex, accentColorHex);
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string PrimaryColorHex { get; private set; }
    public string SecondaryColorHex { get; private set; }
    public string AccentColorHex { get; private set; }

    public static SchoolBranding Create(
        Guid tenantId,
        Guid schoolId,
        string primaryColorHex,
        string secondaryColorHex,
        string accentColorHex)
        => new(tenantId, schoolId, primaryColorHex, secondaryColorHex, accentColorHex);

    public void UpdateColors(string primaryColorHex, string secondaryColorHex, string accentColorHex)
    {
        PrimaryColorHex = HexColor.From(primaryColorHex).Value;
        SecondaryColorHex = HexColor.From(secondaryColorHex).Value;
        AccentColorHex = HexColor.From(accentColorHex).Value;
    }
}

/// <summary>
/// Institutional metadata profile for a <see cref="School"/>.
/// </summary>
public sealed class SchoolProfile : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    private SchoolProfile()
    {
        DefaultLanguage = "en";
    }

    private SchoolProfile(
        Guid tenantId,
        Guid schoolId,
        string? tagline,
        string? principalName,
        int? establishedYear,
        string? contactEmail,
        string? websiteUrl,
        string defaultLanguage) : this()
    {
        if (tenantId == Guid.Empty || schoolId == Guid.Empty)
        {
            throw new ArgumentException("TenantId and SchoolId cannot be empty.");
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        Update(tagline, principalName, establishedYear, contactEmail, websiteUrl, defaultLanguage);
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string? Tagline { get; private set; }
    public string? PrincipalName { get; private set; }
    public int? EstablishedYear { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string DefaultLanguage { get; private set; }

    public static SchoolProfile Create(
        Guid tenantId,
        Guid schoolId,
        string? tagline,
        string? principalName,
        int? establishedYear,
        string? contactEmail,
        string? websiteUrl,
        string defaultLanguage)
        => new(tenantId, schoolId, tagline, principalName, establishedYear, contactEmail, websiteUrl, defaultLanguage);

    public void Update(
        string? tagline,
        string? principalName,
        int? establishedYear,
        string? contactEmail,
        string? websiteUrl,
        string defaultLanguage)
    {
        var email = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim().ToLowerInvariant();
        if (email is not null && !ContactRules.IsValidEmail(email))
        {
            throw new ArgumentException("Invalid contact email.", nameof(contactEmail));
        }

        var url = string.IsNullOrWhiteSpace(websiteUrl) ? null : websiteUrl.Trim();
        if (url is not null && !ContactRules.IsValidWebsite(url))
        {
            throw new ArgumentException("Website URL must be an absolute http/https URI.", nameof(websiteUrl));
        }

        if (establishedYear is < 1800 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(establishedYear));
        }

        Tagline = tagline?.Trim();
        PrincipalName = principalName?.Trim();
        EstablishedYear = establishedYear;
        ContactEmail = email;
        WebsiteUrl = url;
        DefaultLanguage = BuildingBlocks.Localization.SupportedLanguage.Normalize(defaultLanguage);
    }
}
