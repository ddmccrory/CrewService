using CrewService.BlazorUI.Components.Pages.DailyOperations;
using Xunit;

namespace CrewService.BlazorUI.Tests.Pages.DailyOperations;

public class CallBoardTests
{
    [Fact]
    public void CallBoard_CurrentTable_UsesBackendAuthoritativeDefaultSortByRowNumber()
    {
        var source = File.ReadAllText(GetCallBoardRazorPath());

        Assert.Contains("DefaultSortColumn=\"RowNumber\"", source, StringComparison.Ordinal);
        Assert.Contains("DefaultSortAscending=\"true\"", source, StringComparison.Ordinal);
        Assert.Contains("new(\"#\", r => r.RowNumber, sortKey: \"RowNumber\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VacancyAssignmentService_CurrentBoard_OrdersByBoardNameThenRowNumber_AssignsRowNumber_AndUsesCanonicalEmployeeName()
    {
        var source = File.ReadAllText(GetVacancyAssignmentServicePath());

        Assert.Contains(".OrderBy(r => r.BoardName)", source, StringComparison.Ordinal);
        Assert.Contains(".ThenBy(r => r.RowNumber)", source, StringComparison.Ordinal);
        Assert.Contains("rows[i].RowNumber = i + 1;", source, StringComparison.Ordinal);
        Assert.Contains("EmployeeName = authoritativeEmployeeName", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CallBoard_MarkedOffEmployee_UsesRedStrikeThroughAndRedCode()
    {
        var source = File.ReadAllText(GetCallBoardRazorPath());

        Assert.Contains("text-decoration-line-through text-danger", source, StringComparison.Ordinal);
        Assert.Contains("<span class=\"ms-1 text-danger\">(@row.MarkOffCodeDisplay)</span>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CallBoard_HistoryTimestamps_UseBackendDisplayFields_NotClientLocalConversion()
    {
        var source = File.ReadAllText(GetCallBoardRazorPath());

        Assert.Contains("@snapshot.CapturedAtDisplay", source, StringComparison.Ordinal);
        Assert.Contains("@selectedDecision.OccurredAtDisplay", source, StringComparison.Ordinal);
        Assert.Contains("@row.TieUpAtDisplay", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ToLocalDisplay(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VacancyAssignmentService_MapsBackendHistoryDisplayFields()
    {
        var source = File.ReadAllText(GetVacancyAssignmentServicePath());

        Assert.Contains("CapturedAtDisplay = ResolveLocalizedDateTimeDisplay", source, StringComparison.Ordinal);
        Assert.Contains("OccurredAtDisplay = ResolveLocalizedDateTimeDisplay", source, StringComparison.Ordinal);
        Assert.Contains("TieUpAtDisplay = ResolveLocalizedDateTimeDisplay", source, StringComparison.Ordinal);
    }

    private static string GetCallBoardRazorPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var blazorRoot = Path.Combine(dir.FullName, "CrewService.BlazorUI");
            if (Directory.Exists(blazorRoot))
            {
                return Path.Combine(blazorRoot, "Components", "Pages", "DailyOperations", "CallBoard.razor");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CrewService.BlazorUI project directory from test output path.");
    }

    private static string GetVacancyAssignmentServicePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var repoRoot = dir.FullName;
            var servicePath = Path.Combine(repoRoot, "CrewService.API", "CrewService.Presentation", "Services", "Modules", "VacancyAssignmentService.cs");
            if (File.Exists(servicePath))
            {
                return servicePath;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not locate VacancyAssignmentService.cs from test output path.");
    }
}
