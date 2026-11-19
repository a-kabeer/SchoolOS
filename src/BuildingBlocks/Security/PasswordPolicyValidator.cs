using System.IO.Compression;
using System.Text;

namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// NIST SP 800-63B password length policy (ported from Education-Portal SEC-AUTHN-03).
/// Requires 15 to 128 characters instead of arbitrary character-composition rules.
/// </summary>
public static class PasswordRules
{
    public const int MinimumLength = 15;
    public const int MaximumLength = 128;
}

public sealed record PasswordPolicyError(string Code, string Description);

public sealed record PasswordPolicyValidationResult(bool IsValid, IReadOnlyList<PasswordPolicyError> Errors)
{
    public static readonly PasswordPolicyValidationResult Success = new(true, []);

    public static PasswordPolicyValidationResult Failed(params PasswordPolicyError[] errors) =>
        new(false, errors);
}

/// <summary>
/// NIST SP 800-63B password policy validator enforcing length bounds (15–128), top-10,000 breached/common
/// password rejection via embedded gzipped corpus, and rejection of account/product words.
/// </summary>
public static class PasswordPolicyValidator
{
    private const string CommonPasswordsResource = "SchoolOS.BuildingBlocks.Security.CommonPasswords.txt.gz";
    private const string ProductWord = "schoolos";
    private const string LegacyProductWord = "educationportal";
    private const int MinimumPersonalWordLength = 4;

    private static readonly Lazy<HashSet<string>> CommonPasswords = new(LoadCommonPasswords);

    public static PasswordPolicyValidationResult Validate(
        string? password,
        string? email = null,
        string? userName = null)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return PasswordPolicyValidationResult.Failed(
                new PasswordPolicyError("PasswordRequired", "A password is required."));
        }

        if (password.Length < PasswordRules.MinimumLength)
        {
            return PasswordPolicyValidationResult.Failed(
                new PasswordPolicyError(
                    "PasswordTooShort",
                    $"The password must be at least {PasswordRules.MinimumLength} characters."));
        }

        if (password.Length > PasswordRules.MaximumLength)
        {
            return PasswordPolicyValidationResult.Failed(
                new PasswordPolicyError(
                    "PasswordTooLong",
                    $"The password can be at most {PasswordRules.MaximumLength} characters."));
        }

        var normalized = password.ToLowerInvariant();

        if (CommonPasswords.Value.Contains(normalized))
        {
            return PasswordPolicyValidationResult.Failed(
                new PasswordPolicyError(
                    "CommonPassword",
                    "This password is too common. Choose a longer, less predictable one."));
        }

        if (ContainsPersonalOrProductWord(normalized, email, userName))
        {
            return PasswordPolicyValidationResult.Failed(
                new PasswordPolicyError(
                    "PersonalWord",
                    "The password must not contain the account name or the product name."));
        }

        return PasswordPolicyValidationResult.Success;
    }

    private static bool ContainsPersonalOrProductWord(string normalizedPassword, string? email, string? userName)
    {
        if (normalizedPassword.Contains(ProductWord, StringComparison.Ordinal) ||
            normalizedPassword.Contains(LegacyProductWord, StringComparison.Ordinal))
        {
            return true;
        }

        var localPart = email?.Split('@')[0].ToLowerInvariant();
        var normalizedUser = userName?.ToLowerInvariant();

        foreach (var word in new[] { localPart, normalizedUser })
        {
            if (word is { Length: >= MinimumPersonalWordLength } &&
                normalizedPassword.Contains(word, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> LoadCommonPasswords()
    {
        using var resource = typeof(PasswordPolicyValidator).Assembly.GetManifestResourceStream(CommonPasswordsResource)
            ?? throw new InvalidOperationException($"The embedded resource '{CommonPasswordsResource}' is missing.");
        using var gzip = new GZipStream(resource, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);

        var passwords = new HashSet<string>(StringComparer.Ordinal);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length > 0)
            {
                passwords.Add(line);
            }
        }

        return passwords;
    }
}
