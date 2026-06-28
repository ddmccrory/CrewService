namespace CrewService.BlazorUI.Components.Shared;

/// <summary>
/// Generic named item used by <see cref="RequestSeniorityMoveModal"/> for craft,
/// employee, and move-type option lists.
/// </summary>
public sealed class SeniorityMoveNamedItem
{
    public long   CtrlNbr    { get; init; }
    public string Name       { get; init; }
    /// <summary>String-keyed value for non-numeric option lists (e.g. move types).</summary>
    public string CtrlNbrStr { get; set; } = string.Empty;
    /// <summary>Optional secondary ctrl nbr (e.g. employee ctrl nbr when this item represents a junior position).</summary>
    public long SecondaryCtrlNbr { get; init; }
    /// <summary>Optional secondary display name (e.g. the holder's name when this item represents a junior position).</summary>
    public string SecondaryName { get; init; } = string.Empty;

    public SeniorityMoveNamedItem(long ctrlNbr, string name)
    {
        CtrlNbr = ctrlNbr;
        Name    = name;
    }
}
