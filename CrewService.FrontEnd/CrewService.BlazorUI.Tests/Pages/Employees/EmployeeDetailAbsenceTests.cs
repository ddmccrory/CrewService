using Xunit;

namespace CrewService.BlazorUI.Tests.Pages.Employees;

public class EmployeeDetailAbsenceTests
{
    [Fact]
    public void EmployeeDetail_UsesBackendEmployeeFilter_ForOpenAbsences()
    {
        var source = File.ReadAllText(GetEmployeeDetailRazorPath());

        Assert.Contains("AbsenceClient.GetOpenAbsencesAsync(", source, StringComparison.Ordinal);
        Assert.Contains("employeeCtrlNbr: CtrlNbr", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Where(r => r.EmployeeCtrlNbr == CtrlNbr)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EmployeeDetail_UsesBackendEmployeeAndMonthFilter_ForScheduledAbsences()
    {
        var source = File.ReadAllText(GetEmployeeDetailRazorPath());

        Assert.Contains("AbsenceClient.GetScheduledAbsencesAsync(", source, StringComparison.Ordinal);
        Assert.Contains("employeeCtrlNbr: CtrlNbr", source, StringComparison.Ordinal);
        Assert.Contains("currentMonthOnly: true", source, StringComparison.Ordinal);
        Assert.DoesNotContain("monthStart", source, StringComparison.Ordinal);
        Assert.DoesNotContain("monthEnd", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EmployeeDetail_UsesBackendEmployeeFilter_ForAbsenceHistory()
    {
        var source = File.ReadAllText(GetEmployeeDetailRazorPath());

        Assert.Contains("AbsenceClient.GetAbsenceHistoryAsync(", source, StringComparison.Ordinal);
        Assert.Contains("employeeCtrlNbr: CtrlNbr", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EmployeeDetail_PassesWorkAreaFilter_ForAbsenceLocalization()
    {
        var source = File.ReadAllText(GetEmployeeDetailRazorPath());

        Assert.Contains("workAreaGroupCtrlNbr: workAreaGroupCtrlNbr", source, StringComparison.Ordinal);
    }

    private static string GetEmployeeDetailRazorPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var blazorRoot = Path.Combine(dir.FullName, "CrewService.BlazorUI");
            if (Directory.Exists(blazorRoot))
            {
                return Path.Combine(blazorRoot, "Components", "Pages", "Employees", "EmployeeDetail.razor");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CrewService.BlazorUI project directory from test output path.");
    }
}
