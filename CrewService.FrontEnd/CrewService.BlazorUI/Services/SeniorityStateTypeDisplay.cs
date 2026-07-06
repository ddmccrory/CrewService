using System.Text.RegularExpressions;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Centralized presentation for the shared <see cref="SeniorityStateTypeEnum"/> proto enum.
/// Option lists and labels are derived from the enum itself so new state types (e.g.
/// <see cref="SeniorityStateTypeEnum.SeniorityStateTypeOffProperty"/>) surface automatically
/// without editing each page.
/// </summary>
public static partial class SeniorityStateTypeDisplay
{
    private const string NamePrefix = "SeniorityStateType";

    /// <summary>
    /// The selectable state types (every enum value except the <c>Unspecified</c> zero sentinel).
    /// </summary>
    public static IReadOnlyList<SeniorityStateTypeEnum> SelectableValues { get; } =
        Enum.GetValues<SeniorityStateTypeEnum>()
            .Where(v => v != SeniorityStateTypeEnum.SeniorityStateTypeUnspecified)
            .ToList();

    /// <summary>
    /// Returns a human-friendly label for a state type (e.g. <c>OffProperty</c> =&gt; "Off Property").
    /// </summary>
    public static string Label(SeniorityStateTypeEnum value)
    {
        if (value == SeniorityStateTypeEnum.SeniorityStateTypeUnspecified)
            return string.Empty;

        var name = value.ToString();
        if (name.StartsWith(NamePrefix, StringComparison.Ordinal))
            name = name[NamePrefix.Length..];

        return CamelCaseBoundary().Replace(name, "$1 $2");
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex CamelCaseBoundary();
}
