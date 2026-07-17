using CrewService.BlazorUI.Components.Shared;
using Xunit;

namespace CrewService.BlazorUI.Tests.Components.Shared;

public class DataTableColumnTests
{
    [Fact]
    public void ActionsFactory_CreatesNonSortableActionsColumn()
    {
        var column = DataTableColumn<int>.Actions("140px");

        Assert.Equal("Actions", column.Title);
        Assert.Equal("140px", column.Width);
        Assert.False(column.IsSortable);
    }

    [Fact]
    public void ColumnWithoutSortValue_IsNotSortable()
    {
        var column = new DataTableColumn<int>("Status", width: "140px");

        Assert.Equal("Status", column.Title);
        Assert.False(column.IsSortable);
    }

    [Fact]
    public void ColumnWithSortValue_IsSortable()
    {
        var column = new DataTableColumn<int>("Department", x => x);

        Assert.True(column.IsSortable);
    }
}