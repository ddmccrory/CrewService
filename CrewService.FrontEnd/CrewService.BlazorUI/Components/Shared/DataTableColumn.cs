namespace CrewService.BlazorUI.Components.Shared;

public sealed class DataTableColumn<TItem>(string title, Func<TItem, object?>? sortValue = null, string? sortKey = null, string? width = null)
{
    public string Title { get; } = title;
    public string SortKey { get; } = sortKey ?? title;
    public Func<TItem, object?>? SortValue { get; } = sortValue;
    public string? Width { get; } = width;
    public bool IsSortable => SortValue is not null;

    public static DataTableColumn<TItem> Actions(string width = "180px")
        => new("", width: width);
}
