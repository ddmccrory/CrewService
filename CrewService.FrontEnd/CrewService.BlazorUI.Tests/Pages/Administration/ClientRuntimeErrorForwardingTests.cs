using Xunit;

namespace CrewService.BlazorUI.Tests.Pages.Administration;

public sealed class ClientRuntimeErrorForwardingTests
{
    [Fact]
    public void BlazorHost_MapsInternalClientErrorForwardingEndpoint()
    {
        var source = File.ReadAllText(GetBlazorProgramPath());

        Assert.Contains("app.MapPost(\"/internal/client-errors\"", source, StringComparison.Ordinal);
        Assert.Contains("PostAsJsonAsync(\"/v1/error-logs/client\"", source, StringComparison.Ordinal);
        Assert.Contains("CrewServiceApiUrl", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SiteJs_CapturesWindowErrorAndUnhandledRejection()
    {
        var source = File.ReadAllText(GetSiteJsPath());

        Assert.Contains("window.addEventListener(\"error\"", source, StringComparison.Ordinal);
        Assert.Contains("window.addEventListener(\"unhandledrejection\"", source, StringComparison.Ordinal);
        Assert.Contains("fetch(\"/internal/client-errors\"", source, StringComparison.Ordinal);
        Assert.Contains("errorKind: \"ClientRuntime\"", source, StringComparison.Ordinal);
    }

    private static string GetBlazorProgramPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var blazorRoot = Path.Combine(dir.FullName, "CrewService.BlazorUI");
            if (Directory.Exists(blazorRoot))
            {
                return Path.Combine(blazorRoot, "Program.cs");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CrewService.BlazorUI project directory from test output path.");
    }

    private static string GetSiteJsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var blazorRoot = Path.Combine(dir.FullName, "CrewService.BlazorUI");
            if (Directory.Exists(blazorRoot))
            {
                return Path.Combine(blazorRoot, "wwwroot", "js", "site.js");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CrewService.BlazorUI project directory from test output path.");
    }
}
