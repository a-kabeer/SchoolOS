using System.Text.RegularExpressions;

namespace SchoolOS.BuildingBlocks.SharedKernel;

/// <summary>
/// A CSS color value strictly validated in <c>#rrggbb</c> hexadecimal format (SEC-INPUT-01).
/// Ported from Education-Portal.
/// </summary>
public sealed partial record HexColor
{
    public const int Length = 7;

    private HexColor(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool IsValid(string? value) => value is not null && Pattern().IsMatch(value);

    public static HexColor From(string value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException("A color must have the form #rrggbb.", nameof(value));
        }

        return new HexColor(value.ToLowerInvariant());
    }

    public override string ToString() => Value;

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex Pattern();
}
