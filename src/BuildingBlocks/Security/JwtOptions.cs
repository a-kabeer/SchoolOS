namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// Configuration settings for cryptographic JWT Bearer authentication.
/// In production, these values must be supplied via secure environment variables or vault.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    /// <summary>
    /// Default development fallback symmetric key (256-bit). Never use this in production.
    /// </summary>
    public const string DefaultDevelopmentSigningKey = "SchoolOS_Super_Secure_Dev_Key_For_Jwt_HMAC_SHA256_Must_Be_At_Least_32_Bytes!";

    public string Issuer { get; set; } = "SchoolOS.Platform";
    public string Audience { get; set; } = "SchoolOS.Api";
    public string SigningKey { get; set; } = DefaultDevelopmentSigningKey;
    public int ExpiryMinutes { get; set; } = 60;
}
