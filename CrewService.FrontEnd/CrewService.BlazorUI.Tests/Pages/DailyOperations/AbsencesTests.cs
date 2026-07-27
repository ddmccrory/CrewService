using System.Reflection;
using CrewService.BlazorUI.Components.Pages.DailyOperations;
using CrewService.Presentation;
using Xunit;

namespace CrewService.BlazorUI.Tests.Pages.DailyOperations;

public class AbsencesTests
{
    [Fact]
    public void BuildAbsenceCodeDisplay_ReturnsCombinedCodeAndDescription()
    {
        var code = new MarkOffCodeResponse
        {
            Code = "VAC",
            Description = "Vacation"
        };

        var result = InvokePrivateStatic<string>(nameof(BuildAbsenceCodeDisplay_ReturnsCombinedCodeAndDescription), "BuildAbsenceCodeDisplay", code);

        Assert.Equal("VAC — Vacation", result);
    }

    [Fact]
    public void GetReasonDisplay_ReturnsLookupDisplay_WhenConfigured()
    {
        var component = new Absences();
        var lookup = new Dictionary<long, string> { [77] = "VAC — Vacation" };
        SetPrivateField(component, "absenceCodeDisplayLookup", lookup);

        var request = new MarkOffAbsenceRequestListItem
        {
            AbsenceCodeCtrlNbr = 77
        };

        var result = InvokePrivateInstance<string>(component, "GetReasonDisplay", request);

        Assert.Equal("VAC — Vacation", result);
    }

    [Fact]
    public void GetEmployeeDisplay_ReturnsUnknownEmployee_WhenMissing()
    {
        var component = new Absences();
        SetPrivateField(component, "employeeDisplayLookup", new Dictionary<long, string>());

        var result = InvokePrivateInstance<string>(component, "GetEmployeeDisplay", 123L);

        Assert.Equal("Unknown employee (123)", result);
    }

    [Fact]
    public void AbsencesPage_NotesColumn_UsesNotesIconMarkup()
    {
        var source = File.ReadAllText(GetAbsencesRazorPath());

        Assert.Contains("bi bi-chat-left-text", source, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Notes\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AbsencesPage_CreateRequestEmployees_UsesBackendEligibleAbsenceEmployeesQuery()
    {
        var source = File.ReadAllText(GetAbsencesRazorPath());

        Assert.Contains("<CreateAbsenceRequestModal", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RosterClient.GetAllAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SeniorityClient.GetAllByRosterAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("item.LastActiveRoster", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AbsencesPage_CreateRequestEmployees_DoesNotUnionParentAndRailroadEmployeeLists()
    {
        var source = File.ReadAllText(GetAbsencesRazorPath());

        Assert.DoesNotContain("EmployeeClient.GetAllAsync(SelectedRailroadCtrlNbr.Value)", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Union(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AbsencesPage_DecisionModal_ShowsApprovalLevelDescriptionAndApproverDropdown()
    {
        var source = File.ReadAllText(GetAbsencesRazorPath());

        Assert.Contains("Approval Level", source, StringComparison.Ordinal);
        Assert.Contains("decisionApprovalLevelDescription", source, StringComparison.Ordinal);
        Assert.Contains("Approver", source, StringComparison.Ordinal);
        Assert.Contains("decisionApprovers", source, StringComparison.Ordinal);
        Assert.Contains("InputSelect @bind-Value=\"decisionOfficerCtrlNbr\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AbsencesPage_DecisionFlow_UsesApprovalContextAndSelectedOfficer()
    {
        var source = File.ReadAllText(GetAbsencesRazorPath());

        Assert.Contains("AbsenceClient.GetAbsenceApprovalContextAsync", source, StringComparison.Ordinal);
        Assert.Contains("decisionOfficerCtrlNbr", source, StringComparison.Ordinal);
        Assert.Contains("ApproveAbsenceRequestAsync(decisionRequestCtrlNbr, decisionOfficerCtrlNbr", source, StringComparison.Ordinal);
        Assert.Contains("DeclineAbsenceRequestAsync(decisionRequestCtrlNbr, decisionOfficerCtrlNbr", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AbsencesPage_RequestActions_IncludeCancelForPendingRequests()
    {
        var source = File.ReadAllText(GetAbsencesRazorPath());

        Assert.Contains("OpenCancelModal(req.CtrlNbr)", source, StringComparison.Ordinal);
        Assert.Contains("ConfirmCancelRequestAsync", source, StringComparison.Ordinal);
        Assert.Contains(">Cancel</button>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AbsencesPage_WaitlistActions_IncludeCancelForWaitlistedRequests()
    {
        var source = File.ReadAllText(GetAbsencesRazorPath());

        Assert.Contains("ShowWaitListActionsColumn", source, StringComparison.Ordinal);
        Assert.Contains("IsStatus(r.Status, \"WAITLISTED\")", source, StringComparison.Ordinal);
        Assert.Contains("IsStatus(req.Status, \"WAITLISTED\")", source, StringComparison.Ordinal);
        Assert.Contains("OpenCancelModal(req.CtrlNbr)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AbsencesPage_MonthCounts_UsesRangeEndpointInsteadOfPerDayRequests()
    {
        var source = File.ReadAllText(GetAbsencesRazorPath());

        Assert.Contains("GetAbsenceRequestCountsByDayAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetAbsenceRequestsAsync(\n                            date", source, StringComparison.Ordinal);
    }

    private static string GetAbsencesRazorPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var blazorRoot = Path.Combine(dir.FullName, "CrewService.BlazorUI");
            if (Directory.Exists(blazorRoot))
            {
                return Path.Combine(blazorRoot, "Components", "Pages", "DailyOperations", "Absences.razor");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CrewService.BlazorUI project directory from test output path.");
    }

    private static T InvokePrivateStatic<T>(string testName, string methodName, params object?[] args)
    {
        var method = typeof(Absences).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{testName}: could not find static method '{methodName}'.");

        var result = method.Invoke(null, args);
        return result is T typed
            ? typed
            : throw new InvalidOperationException($"{testName}: method '{methodName}' returned an unexpected type.");
    }

    private static T InvokePrivateInstance<T>(Absences component, string methodName, params object?[] args)
    {
        var method = typeof(Absences).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"could not find instance method '{methodName}'.");

        var result = method.Invoke(component, args);
        return result is T typed
            ? typed
            : throw new InvalidOperationException($"method '{methodName}' returned an unexpected type.");
    }

    private static void SetPrivateField<TValue>(Absences component, string fieldName, TValue value)
    {
        var field = typeof(Absences).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"could not find field '{fieldName}'.");

        field.SetValue(component, value);
    }
}
