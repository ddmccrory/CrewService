namespace CrewService.BlazorUI.Components.Shared;

public sealed class DataTableColumn<TItem>(
    string title,
    Func<TItem, object?>? sortValue = null,
    string? sortKey = null,
    string? width = null,
    string? headerTooltip = null)
{
    public string Title { get; } = title;
    public string SortKey { get; } = sortKey ?? title;
    public Func<TItem, object?>? SortValue { get; } = sortValue;
    public string? Width { get; } = width;
    public string? HeaderTooltip { get; } = headerTooltip;
    public bool IsSortable => SortValue is not null;

    public static DataTableColumn<TItem> Actions(string width = "200px")
        => new("Actions", width: width);
}
