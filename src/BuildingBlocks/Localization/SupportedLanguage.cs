using System.Globalization;

namespace SchoolOS.BuildingBlocks.Localization;

public sealed record LanguageDescriptor(string Code, string EnglishName, string NativeName, bool IsRtl);

/// <summary>
/// Canonical UI and validation languages supported across SchoolOS (<c>en</c>, <c>ur</c>, <c>ar</c>).
/// </summary>
public static class SupportedLanguage
{
    public const string DefaultCode = "en";

    public static readonly LanguageDescriptor English = new("en", "English", "English", false);
    public static readonly LanguageDescriptor Urdu = new("ur", "Urdu", "اردو", true);
    public static readonly LanguageDescriptor Arabic = new("ar", "Arabic", "العربية", true);

    public static IReadOnlyList<LanguageDescriptor> All { get; } = [English, Urdu, Arabic];

    public static IReadOnlySet<string> SupportedCodes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { English.Code, Urdu.Code, Arabic.Code };

    public static bool IsSupported(string? code)
    {
        var normalized = Normalize(code, fallbackToDefault: false);
        return normalized is not null && SupportedCodes.Contains(normalized);
    }

    public static bool IsRtl(string? code) =>
        string.Equals(Normalize(code), Urdu.Code, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Normalize(code), Arabic.Code, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? code, bool fallbackToDefault = true)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return fallbackToDefault ? DefaultCode : string.Empty;
        }

        var trimmed = code.Trim().ToLowerInvariant();
        var primary = trimmed.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        if (SupportedCodes.Contains(primary))
        {
            return primary;
        }

        return fallbackToDefault ? DefaultCode : string.Empty;
    }

    public static CultureInfo ToCultureInfo(string? code)
    {
        var normalized = Normalize(code);
        return CultureInfo.GetCultureInfo(normalized);
    }
}
