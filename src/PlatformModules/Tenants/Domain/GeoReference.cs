using System.Text.RegularExpressions;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.PlatformModules.Tenants.Domain;

/// <summary>
/// ISO 3166-1 alpha-2 country reference with supported UI languages, timezone, and ISO 4217 currency.
/// </summary>
public sealed partial class Country : Entity<Guid>
{
    public const int CodeLength = 2;
    public const int NameMaxLength = 100;
    public const int CurrencyCodeLength = 3;

    private readonly List<State> _states = [];

    private Country()
    {
        Code = string.Empty;
        Name = string.Empty;
        LanguagesCsv = "en";
        DefaultLanguage = "en";
        TimeZoneId = "UTC";
        CurrencyCode = "USD";
    }

    private Country(
        string code,
        string name,
        string languagesCsv,
        string defaultLanguage,
        string timeZoneId,
        string currencyCode) : this()
    {
        var normalizedCode = NormalizeCode(code);
        if (!IsValidCode(normalizedCode))
        {
            throw new ArgumentException("Country code must be a two-letter ISO 3166-1 alpha-2 code.", nameof(code));
        }

        Id = Guid.CreateVersion7();
        Code = normalizedCode;
        Update(name, languagesCsv, defaultLanguage, timeZoneId, currencyCode, isActive: true);
    }

    public string Code { get; private set; }
    public string Name { get; private set; }
    public string LanguagesCsv { get; private set; }
    public string DefaultLanguage { get; private set; }
    public string TimeZoneId { get; private set; }
    public string CurrencyCode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<State> States => _states.AsReadOnly();

    public static Country Create(
        string code,
        string name,
        string languagesCsv,
        string defaultLanguage,
        string timeZoneId,
        string currencyCode)
        => new(code, name, languagesCsv, defaultLanguage, timeZoneId, currencyCode);

    public void Update(
        string name,
        string languagesCsv,
        string defaultLanguage,
        string timeZoneId,
        string currencyCode,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(languagesCsv);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultLanguage);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);

        var trimmedName = name.Trim();
        if (trimmedName.Length > NameMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(name));
        }

        var normalizedCurrency = currencyCode.Trim().ToUpperInvariant();
        if (!CurrencyCodeRegex().IsMatch(normalizedCurrency))
        {
            throw new ArgumentException("Currency code must be a three-letter ISO 4217 code.", nameof(currencyCode));
        }

        var normalizedLanguages = string.Join(
            ',',
            languagesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(l => l.ToLowerInvariant())
                .Distinct(StringComparer.Ordinal));

        if (string.IsNullOrWhiteSpace(normalizedLanguages))
        {
            throw new ArgumentException("At least one supported language code is required.", nameof(languagesCsv));
        }

        var normalizedDefaultLang = defaultLanguage.Trim().ToLowerInvariant();
        if (!normalizedLanguages.Split(',').Contains(normalizedDefaultLang, StringComparer.Ordinal))
        {
            throw new ArgumentException("Default language must be one of the country's supported languages.", nameof(defaultLanguage));
        }

        Name = trimmedName;
        LanguagesCsv = normalizedLanguages;
        DefaultLanguage = normalizedDefaultLang;
        TimeZoneId = timeZoneId.Trim();
        CurrencyCode = normalizedCurrency;
        IsActive = isActive;
    }

    public static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    public static bool IsValidCode(string? code) =>
        code is not null && CountryCodeRegex().IsMatch(NormalizeCode(code));

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex CountryCodeRegex();

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCodeRegex();
}

/// <summary>
/// Province or state belonging to a <see cref="Country"/>.
/// </summary>
public sealed class State : Entity<Guid>
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 100;

    private readonly List<City> _cities = [];

    private State()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private State(Guid countryId, string code, string name) : this()
    {
        if (countryId == Guid.Empty)
        {
            throw new ArgumentException("Country ID cannot be empty.", nameof(countryId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedCode = code.Trim().ToUpperInvariant();
        var trimmedName = name.Trim();
        if (normalizedCode.Length > CodeMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(code));
        }

        if (trimmedName.Length > NameMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(name));
        }

        Id = Guid.CreateVersion7();
        CountryId = countryId;
        Code = normalizedCode;
        Name = trimmedName;
        IsActive = true;
    }

    public Guid CountryId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<City> Cities => _cities.AsReadOnly();

    public static State Create(Guid countryId, string code, string name) => new(countryId, code, name);
}

/// <summary>
/// City belonging to a <see cref="State"/>.
/// </summary>
public sealed class City : Entity<Guid>
{
    public const int NameMaxLength = 100;

    private City()
    {
        Name = string.Empty;
    }

    private City(Guid stateId, string name) : this()
    {
        if (stateId == Guid.Empty)
        {
            throw new ArgumentException("State ID cannot be empty.", nameof(stateId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmedName = name.Trim();
        if (trimmedName.Length > NameMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(name));
        }

        Id = Guid.CreateVersion7();
        StateId = stateId;
        Name = trimmedName;
        IsActive = true;
    }

    public Guid StateId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public static City Create(Guid stateId, string name) => new(stateId, name);
}
