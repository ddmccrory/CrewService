using Xunit;

namespace CrewService.BlazorUI.Tests.Pages.Administration;

public sealed class ErrorLogTests
{
    [Fact]
    public void ErrorLogPage_UsesAdminRouteAndFeatureKey()
    {
        var source = File.ReadAllText(GetErrorLogRazorPath());

        Assert.Contains("@page \"/admin/error-log\"", source, StringComparison.Ordinal);
        Assert.Contains("protected override string? FeatureKey => \"admin/error-log\";", source, StringComparison.Ordinal);
        Assert.Contains("<h1>Error Log</h1>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ErrorLogPage_RendersSummaryColumnsAndPayloadToggle()
    {
        var source = File.ReadAllText(GetErrorLogRazorPath());

        Assert.Contains("<th style=\"width:110px\">Severity</th>", source, StringComparison.Ordinal);
        Assert.Contains("<th style=\"width:120px\">Source</th>", source, StringComparison.Ordinal);
        Assert.Contains("<th style=\"width:120px\">Status</th>", source, StringComparison.Ordinal);
        Assert.Contains("<th style=\"width:90px\">Count</th>", source, StringComparison.Ordinal);
        Assert.Contains("<th style=\"width:80px\">Payload</th>", source, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"() => TogglePayload(entry.ErrorId)\"", source, StringComparison.Ordinal);
        Assert.Contains("IsPayloadVisible(entry.ErrorId)", source, StringComparison.Ordinal);
        Assert.Contains("@entry.PayloadJson", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ErrorLogPage_HasSeverityAndSourceFilters()
    {
        var source = File.ReadAllText(GetErrorLogRazorPath());

        Assert.Contains("<label class=\"form-label fw-semibold small mb-1\">Severity</label>", source, StringComparison.Ordinal);
        Assert.Contains("<option value=\"Critical\">Critical</option>", source, StringComparison.Ordinal);
        Assert.Contains("<label class=\"form-label fw-semibold small mb-1\">Status</label>", source, StringComparison.Ordinal);
        Assert.Contains("<label class=\"form-label fw-semibold small mb-1\">Kind</label>", source, StringComparison.Ordinal);
        Assert.Contains("<label class=\"form-label fw-semibold small mb-1\">Source</label>", source, StringComparison.Ordinal);
        Assert.Contains("<option value=\"BackendApi\">Backend API</option>", source, StringComparison.Ordinal);
        Assert.Contains("<label class=\"form-label fw-semibold small mb-1\">Fingerprint</label>", source, StringComparison.Ordinal);
        Assert.Contains("placeholder=\"Search code, message, trace, route, payload…\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ErrorLogPage_HasTriageActionButtons()
    {
        var source = File.ReadAllText(GetErrorLogRazorPath());

        Assert.Contains("Investigate", source, StringComparison.Ordinal);
        Assert.Contains("Resolve", source, StringComparison.Ordinal);
        Assert.Contains("Suppress", source, StringComparison.Ordinal);
        Assert.Contains("UpdateStatusAsync(entry.ErrorId, \"Investigating\")", source, StringComparison.Ordinal);
        Assert.Contains("UpdateStatusAsync(entry.ErrorId, \"Resolved\")", source, StringComparison.Ordinal);
        Assert.Contains("UpdateStatusAsync(entry.ErrorId, \"Suppressed\")", source, StringComparison.Ordinal);
    }

    private static string GetErrorLogRazorPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var blazorRoot = Path.Combine(dir.FullName, "CrewService.BlazorUI");
            if (Directory.Exists(blazorRoot))
            {
                return Path.Combine(
                    blazorRoot,
                    "Components",
                    "Pages",
                    "Administration",
                    "ErrorLog.razor");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CrewService.BlazorUI project directory from test output path.");
    }
}
