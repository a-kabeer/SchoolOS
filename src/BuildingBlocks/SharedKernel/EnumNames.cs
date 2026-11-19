namespace SchoolOS.BuildingBlocks.SharedKernel;

/// <summary>
/// Parses an enum from its member name only. Standard <see cref="Enum.TryParse{TEnum}(string?, bool, out TEnum)"/>
/// also accepts numeric strings such as <c>"2"</c> or <c>"99"</c>; this helper refuses them.
/// </summary>
public static class EnumNames
{
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        var name = value?.Trim();

        if (!string.IsNullOrEmpty(name)
            && !char.IsAsciiDigit(name[0]) && name[0] is not ('-' or '+')
            && Enum.TryParse(name, ignoreCase: true, out result)
            && Enum.IsDefined(result))
        {
            return true;
        }

        result = default;
        return false;
    }
}
