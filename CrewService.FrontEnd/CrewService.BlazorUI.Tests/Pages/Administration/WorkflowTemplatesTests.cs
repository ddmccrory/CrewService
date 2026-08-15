using Xunit;

namespace CrewService.BlazorUI.Tests.Pages.Administration;

public sealed class WorkflowTemplatesTests
{
    [Fact]
    public void WorkflowTemplatesPage_DoesNotInjectDomainOptionClients()
    {
        var source = File.ReadAllText(GetWorkflowTemplatesRazorPath());

        Assert.DoesNotContain("@inject DepartmentClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@inject CraftClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@inject SeniorityStateClient", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowTemplatesPage_UsesBackendMetadataAllowedValues()
    {
        var source = File.ReadAllText(GetWorkflowTemplatesRazorPath());

        Assert.Contains("AllowedValues = metadataField.AllowedValues.ToList()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadTriggerFilterValueOptionsAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowTemplatesPage_DoesNotEncodeEffectOptionBusinessKeysInUi()
    {
        var source = File.ReadAllText(GetWorkflowTemplatesRazorPath());

        Assert.DoesNotContain("Key = \"roleCtrlNbr\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Key = \"expirationDays\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Key = \"usePrimaryEmail\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Key = \"autoMoveDelayHours\"", source, StringComparison.Ordinal);
        Assert.Contains("effectPayload.ExpirationDays", source, StringComparison.Ordinal);
        Assert.Contains("effectPayload.EmailSource", source, StringComparison.Ordinal);
        Assert.Contains("effectPayload.AutoMoveDelayHours", source, StringComparison.Ordinal);
    }

    private static string GetWorkflowTemplatesRazorPath()
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
                    "WorkflowTemplates.razor");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CrewService.BlazorUI project directory from test output path.");
    }
}
