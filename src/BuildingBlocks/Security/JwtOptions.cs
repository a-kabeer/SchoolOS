using System.Text;

namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// Configuration settings for cryptographic JWT Bearer authentication.
/// In non-development environments, <see cref="SigningKey"/> must be supplied via a secret store or environment variables.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    /// <summary>
    /// Minimum HMAC-SHA256 key size in bytes (256 bits).
    /// </summary>
    public const int MinimumSigningKeyBytes = 32;

    /// <summary>
    /// Default development fallback symmetric key (256-bit). Rejected at startup outside Development/Testing.
    /// </summary>
    public const string DefaultDevelopmentSigningKey = "SchoolOS_Super_Secure_Dev_Key_For_Jwt_HMAC_SHA256_Must_Be_At_Least_32_Bytes!";

    public string Issuer { get; set; } = "SchoolOS.Platform";
    public string Audience { get; set; } = "SchoolOS.Api";
    public string SigningKey { get; set; } = DefaultDevelopmentSigningKey;
    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Validates signing key strength and prevents the built-in development fallback key from running in production.
    /// </summary>
    public void Validate(bool allowDevelopmentDefaults)
    {
        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException(
                $"{SectionName}:SigningKey is missing. Supply a strong 256-bit key via secret store or environment variables.");
        }

        if (Encoding.UTF8.GetByteCount(SigningKey) < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"{SectionName}:SigningKey must be at least {MinimumSigningKeyBytes} bytes (256 bits).");
        }

        if (!allowDevelopmentDefaults &&
            string.Equals(SigningKey, DefaultDevelopmentSigningKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"CRITICAL SECURITY ERROR: {SectionName}:SigningKey cannot use the built-in development default key outside Development/Testing environments.");
        }
    }
}
