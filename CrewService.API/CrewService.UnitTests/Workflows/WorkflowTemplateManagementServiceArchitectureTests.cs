namespace CrewService.UnitTests.Workflows;

public sealed class WorkflowTemplateManagementServiceArchitectureTests
{
    [Fact]
    public void WorkflowTemplateManagementService_ResolvesMetadataAllowedValuesServerSide()
    {
        var source = File.ReadAllText(GetWorkflowTemplateManagementServicePath());

        Assert.Contains("BuildMetadataAllowedValuesByCodeAsync", source, StringComparison.Ordinal);
        Assert.Contains("departmentRepository.GetByParentAndRailroadAsync", source, StringComparison.Ordinal);
        Assert.Contains("craftRepository.GetByParentAndRailroadAsync", source, StringComparison.Ordinal);
        Assert.Contains("seniorityStateRepository.GetByParentCtrlNbrAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowTemplateManagementService_UsesDomainSourcesForMetadataOptions()
    {
        var source = File.ReadAllText(GetWorkflowTemplateManagementServicePath());

        Assert.Contains("NotificationCategories.BoardPlacement", source, StringComparison.Ordinal);
        Assert.Contains("Enum.GetNames<BoardType>()", source, StringComparison.Ordinal);
        Assert.Contains("WorkflowReferenceItemDto(", source, StringComparison.Ordinal);
        Assert.Contains("List<string> AllowedValues", source, StringComparison.Ordinal);
    }

    private static string GetWorkflowTemplateManagementServicePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var servicePath = Path.Combine(
                dir.FullName,
                "CrewService.Application",
                "Workflows",
                "WorkflowTemplateManagementService.cs");

            if (File.Exists(servicePath))
                return servicePath;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate WorkflowTemplateManagementService.cs from test output path.");
    }
}
