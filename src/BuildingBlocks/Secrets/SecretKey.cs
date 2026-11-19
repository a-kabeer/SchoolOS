using System.Text.RegularExpressions;

namespace SchoolOS.BuildingBlocks.Secrets;

/// <summary>
/// A logical secret key. Application code names secrets only through this type, and each secret provider
/// translates the key to its own naming rules:
/// <list type="bullet">
/// <item><c>platform/{name}</c>: platform secrets, for example <c>platform/db-connection</c>.</item>
/// <item><c>tenants/{tenantGuid}/{name}</c>: secrets of one tenant, for example <c>tenants/0195.../sms-api-key</c>.</item>
/// </list>
/// A name is 1 to 63 lowercase letters, digits and dashes starting with a letter, ruling out path traversal such as <c>../</c>.
/// </summary>
public sealed partial record SecretKey
{
    public const string PlatformPrefix = "platform";
    public const string TenantPrefix = "tenants";
    public const int MaxNameLength = 63;

    private SecretKey(Guid? tenantId, string name)
    {
        TenantId = tenantId;
        Name = name;
        Path = tenantId is null
            ? $"{PlatformPrefix}/{name}"
            : $"{TenantPrefix}/{tenantId.Value:D}/{name}";
    }

    public Guid? TenantId { get; }
    public string Name { get; }
    public string Path { get; }
    public bool IsPlatform => TenantId is null;

    public static SecretKey Platform(string name) => new(null, ValidName(name));

    /// <summary>
    /// A tenant secret. Application code uses <see cref="ITenantSecretProvider"/>, which derives TenantId
    /// strictly from authenticated server-side <see cref="Security.ITenantContext"/>.
    /// </summary>
    public static SecretKey ForTenant(Guid tenantId, string name)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant id cannot be empty.", nameof(tenantId));
        }

        return new SecretKey(tenantId, ValidName(name));
    }

    public static SecretKey Parse(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var parts = path.Split('/');

        if (parts is [PlatformPrefix, var platformName])
        {
            return Platform(platformName);
        }

        if (parts is [TenantPrefix, var tenant, var tenantName]
            && Guid.TryParseExact(tenant, "D", out var tenantId)
            && tenantId != Guid.Empty)
        {
            return ForTenant(tenantId, tenantName);
        }

        throw new ArgumentException(
            "A secret key is 'platform/{name}' or 'tenants/{tenantGuid}/{name}'.", nameof(path));
    }

    public static bool IsValidName(string? name) => name is not null && NamePattern().IsMatch(name);

    public override string ToString() => Path;

    private static string ValidName(string name)
    {
        if (!IsValidName(name))
        {
            throw new ArgumentException(
                $"A secret name is 1 to {MaxNameLength} lowercase letters, digits and dashes, starting with a letter.",
                nameof(name));
        }

        return name;
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{0,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex NamePattern();
}
