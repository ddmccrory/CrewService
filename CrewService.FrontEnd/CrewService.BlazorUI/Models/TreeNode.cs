namespace CrewService.BlazorUI.Models;

/// <summary>
/// A node in a hierarchical tree structure for display in the TreeView component.
/// </summary>
public sealed class TreeNode
{
    public string Label { get; set; } = string.Empty;
    public string? Href { get; set; }
    public string? Badge { get; set; }
    public string? BadgeClass { get; set; }
    public List<BadgeItem> Badges { get; set; } = [];
    public string? Subtitle { get; set; }
    public bool IsHighlighted { get; set; }
    public List<TreeNode> Children { get; set; } = [];
}

public sealed record BadgeItem(string Text, string CssClass = "bg-secondary");
