using Xunit;

namespace CrewService.BlazorUI.Tests.Pages.DailyOperations;

public class VacancyResolutionTests
{
    [Fact]
    public void VacancyResolutionPage_FillVacancyButton_IsEnabledAndInvokesModal()
    {
        var source = File.ReadAllText(GetVacancyResolutionRazorPath());

        Assert.Contains("@onclick=\"() => OpenFillVacancyModal(shift, slot)\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("title=\"Fill Vacancy is not available yet\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VacancyResolutionPage_FillModal_ContainsCandidateContactAndLateCallFields()
    {
        var source = File.ReadAllText(GetVacancyResolutionRazorPath());

        Assert.Contains("GetVacancyFillCandidatesAsync", source, StringComparison.Ordinal);
        Assert.Contains("Force Override (Daily)", source, StringComparison.Ordinal);
        Assert.Contains("Late Call", source, StringComparison.Ordinal);
        Assert.Contains("Late Call Note", source, StringComparison.Ordinal);
        Assert.Contains("Arrival Follow-Up Note", source, StringComparison.Ordinal);
        Assert.Contains("Dispatcher Note", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VacancyResolutionPage_FillAuditReport_IsRendered()
    {
        var source = File.ReadAllText(GetVacancyResolutionRazorPath());

        Assert.Contains("Vacancy Fill Audit", source, StringComparison.Ordinal);
        Assert.Contains("GetVacancyFillAuditReportAsync", source, StringComparison.Ordinal);
        Assert.Contains("Created", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Created (UTC)", source, StringComparison.Ordinal);
        Assert.Contains("@record.CreatedAtLocal", source, StringComparison.Ordinal);
    }

    private static string GetVacancyResolutionRazorPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var blazorRoot = Path.Combine(dir.FullName, "CrewService.BlazorUI");
            if (Directory.Exists(blazorRoot))
            {
                return Path.Combine(blazorRoot, "Components", "Pages", "DailyOperations", "VacancyResolution.razor");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CrewService.BlazorUI project directory from test output path.");
    }
}
