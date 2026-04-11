using CrewService.BlazorUI.Clients;
using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Microsoft.AspNetCore.Components;

namespace CrewService.BlazorUI.Components.Pages.Staffing;

public partial class CrewSetupWizard
{
    private sealed record CrewTypeOption(string Value, string Label);
    private static readonly IReadOnlyList<CrewTypeOption> crewTypeOptions =
    [
        new("REGULAR", "Regular"),
        new("EXTRA", "Extra"),
        new("RELIEF", "Relief")
    ];

    [Inject] private CrewClient CrewClient { get; set; } = default!;
    [Inject] private AssignmentClient AssignmentClient { get; set; } = default!;
    [Inject] private WorkManagementClient WorkManagementClient { get; set; } = default!;
    [Inject] private RailroadReferenceDataService ReferenceData { get; set; } = default!;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnComplete { get; set; }

    [Parameter] public IReadOnlyList<DepartmentResponse>? Departments { get; set; }
    [Parameter] public IReadOnlyList<CrewResponse>? ExistingCrews { get; set; }

    [Parameter] public long? SelectedRailroadCtrlNbr { get; set; }

    private IReadOnlyList<GroupResponse>? allGroups;
    private IReadOnlyList<GroupResponse>? allGroupsForAssignments;

    private int currentStep = 1;
    private bool isSaving;
    private bool isLoading;
    private string? errorMessage;
    private string? successSummary;

    // ── Step 1: Crew ──
    private bool useExistingCrew;
    private long selectedCrewCtrlNbr;
    private string? existingCrewType;
    private string? newCrewType;
    private long newWorkAreaCtrlNbr;
    private long newCrewDeptCtrlNbr;
    private string newCrewName = "";
    private DateOnly effectiveDate = DateOnly.FromDateTime(DateTime.Today);
    private DateOnly? abolishedDate;

    // ── Step 2 & 3: Assignments ──
    private List<CrewAssignmentEntry> assignmentEntries = [];
    private IReadOnlyList<StaffingAssignmentResponse>? existingAssignments;
    private IReadOnlyList<ShiftDefinitionResponse>? shiftDefinitions;

    // ── Step 1b: Positions ──
    private List<WizardPositionEntry> positionEntries = [];
    private IReadOnlyList<CraftRoleResponse>? craftRoles;

    // ── Step navigation ──
    private string StepTitle => currentStep switch
    {
        1 => "Crew Setup Wizard — Step 1: Crew",
        2 => "Crew Setup Wizard — Step 2: Assignments",
        3 => "Crew Setup Wizard — Step 3: Crew Assignments",
        4 => "Crew Setup Wizard — Step 4: Review & Submit",
        _ => "Crew Setup Wizard"
    };

    private bool CanGoNext => currentStep switch
    {
        1 => useExistingCrew
            ? selectedCrewCtrlNbr > 0
            : newWorkAreaCtrlNbr > 0
              && !string.IsNullOrWhiteSpace(newCrewName)
              && positionEntries.Any(p => p.CraftRoleCtrlNbr > 0),
        2 => assignmentEntries.Count > 0 && assignmentEntries.All(a => a.IsAssignmentValid),
        3 => assignmentEntries.Count > 0 && assignmentEntries.All(a => a.IsValid),
        _ => false
    };

    protected override async Task OnParametersSetAsync()
    {
        if (Visible && allGroups is null)
        {
            allGroups = await ReferenceData.GetWorkAreasAsync();
            allGroupsForAssignments = await ReferenceData.GetGroupsAsync();
            craftRoles = await ReferenceData.GetCraftRolesAsync();
            shiftDefinitions = await ReferenceData.GetShiftDefinitionsAsync();
        }
    }

    private async Task GoNext()
    {
        try
        {
            isLoading = true;
            if (currentStep == 1)
            {
                if (!useExistingCrew && string.IsNullOrWhiteSpace(newCrewType))
                {
                    errorMessage = "Please select a crew type.";
                    return;
                }

                if (existingAssignments is null)
                    await LoadExistingAssignments();

                // Seed new assignment defaults from crew department
                var (_, dept) = GetCrewGroupAndDepartment();

                // Ensure at least one assignment card is open
                if (assignmentEntries.Count == 0)
                    assignmentEntries.Add(new CrewAssignmentEntry
                    {
                        NewAssignment = new NewAssignmentInfo { DepartmentCtrlNbr = dept },
                        StartDate = effectiveDate
                    });

                foreach (var entry in assignmentEntries)
                {
                    if (!entry.UseExisting && entry.NewAssignment.DepartmentCtrlNbr == 0)
                        entry.NewAssignment.DepartmentCtrlNbr = dept;
                    if (!entry.UseExisting && string.IsNullOrWhiteSpace(entry.NewAssignment.Code) && newCrewType == "REGULAR")
                        entry.NewAssignment.Code = newCrewName;
                }
            }
            else if (currentStep == 2)
            {
                // Default crew assignment dates from crew effective date
                foreach (var entry in assignmentEntries)
                {
                    if (entry.StartDate == DateOnly.FromDateTime(DateTime.Today) && effectiveDate != DateOnly.FromDateTime(DateTime.Today))
                        entry.StartDate = effectiveDate;
                }
            }
            currentStep++;
        }
        finally { isLoading = false; }
    }

    private async Task OnExistingCrewSelected(long crewCtrlNbr)
    {
        selectedCrewCtrlNbr = crewCtrlNbr;
        positionEntries.Clear();
        assignmentEntries.Clear();
        effectiveDate = DateOnly.FromDateTime(DateTime.Today);
        abolishedDate = null;
        existingCrewType = null;

        if (crewCtrlNbr <= 0) return;

        // Pre-populate lifecycle dates and crew type from the selected crew
        var selectedCrew = ExistingCrews?.FirstOrDefault(c => c.CtrlNbr == crewCtrlNbr);
        if (selectedCrew is not null)
        {
            existingCrewType = selectedCrew.CrewType;
            if (DateTime.TryParse(selectedCrew.EffectiveDate, out var eff))
                effectiveDate = DateOnly.FromDateTime(eff);
            if (!string.IsNullOrWhiteSpace(selectedCrew.AbolishedDate) && DateTime.TryParse(selectedCrew.AbolishedDate, out var abol))
                abolishedDate = DateOnly.FromDateTime(abol);
        }

        isLoading = true;
        StateHasChanged();

        if (existingAssignments is null)
            await LoadExistingAssignments();

        try
        {
            // Load crew assignments into unified entries
            var crewAssignmentsResponse = await CrewClient.GetCrewAssignmentsAsync(crewCtrlNbr);
            if (crewAssignmentsResponse.Assignments.Count > 0)
            {
                foreach (var ca in crewAssignmentsResponse.Assignments)
                {
                    var entry = new CrewAssignmentEntry { UseExisting = true };
                    await PopulateExistingAssignmentInfo(entry, ca.AssignmentCtrlNbr);

                    // Pre-populate crew work days from existing crew assignment
                    entry.CrewWorkDaysMask = ca.DaysOfWeekMask;

                    // Pre-populate dates from existing crew assignment
                    if (DateTime.TryParse(ca.StartUtc, out var caStart))
                        entry.StartDate = DateOnly.FromDateTime(caStart);
                    if (!string.IsNullOrWhiteSpace(ca.EndUtc) && DateTime.TryParse(ca.EndUtc, out var caEnd))
                        entry.EndDate = DateOnly.FromDateTime(caEnd);

                    assignmentEntries.Add(entry);
                }
            }

            // Load crew positions
            var positionsResponse = await CrewClient.GetCrewPositionsAsync(crewCtrlNbr);
            foreach (var p in positionsResponse.Positions)
            {
                positionEntries.Add(new WizardPositionEntry
                {
                    CraftRoleCtrlNbr = p.CraftRoleCtrlNbr,
                    DisplayOrder = p.DisplayOrder
                });
            }
        }
        catch { /* best-effort loading */ }
        finally { isLoading = false; }
    }

    private (long groupCtrlNbr, long departmentCtrlNbr) GetCrewGroupAndDepartment()
    {
        if (useExistingCrew)
        {
            var crew = ExistingCrews?.FirstOrDefault(c => c.CtrlNbr == selectedCrewCtrlNbr);
            return (crew?.WorkAreaCtrlNbr ?? 0, crew?.DepartmentCtrlNbr ?? 0);
        }
        return (newWorkAreaCtrlNbr, newCrewDeptCtrlNbr);
    }

    private void GoBack() => currentStep = Math.Max(1, currentStep - 1);

    private async Task LoadExistingAssignments()
    {
        try
        {
            var response = await AssignmentClient.GetAllAsync(railroadCtrlNbr: SelectedRailroadCtrlNbr ?? 0);
            existingAssignments = [.. response.Assignments];
        }
        catch { existingAssignments = []; }
    }

    // ── Step 2 helpers ──
    private void AddAssignmentEntry()
    {
        var (_, dept) = GetCrewGroupAndDepartment();
        assignmentEntries.Add(new CrewAssignmentEntry
        {
            NewAssignment = new NewAssignmentInfo { DepartmentCtrlNbr = dept },
            StartDate = effectiveDate
        });
    }

    private void RemoveAssignmentEntry(CrewAssignmentEntry entry)
    {
        if (assignmentEntries.Count > 1)
            assignmentEntries.Remove(entry);
    }

    private async Task OnExistingAssignmentSelected(CrewAssignmentEntry entry, long assignmentCtrlNbr)
    {
        await PopulateExistingAssignmentInfo(entry, assignmentCtrlNbr);
    }

    private async Task PopulateExistingAssignmentInfo(CrewAssignmentEntry entry, long assignmentCtrlNbr)
    {
        entry.ExistingAssignmentCtrlNbr = assignmentCtrlNbr;
        if (assignmentCtrlNbr <= 0) return;

        var assignment = existingAssignments?.FirstOrDefault(a => a.CtrlNbr == assignmentCtrlNbr);
        if (assignment is null) return;

        entry.ExistingCode = assignment.Code;
        entry.ExistingName = assignment.Name;
        entry.ExistingIsExtra = assignment.IsExtra;
        entry.ExistingGroupName = assignment.GroupName;
        entry.ExistingIsActive = assignment.IsActive;

        try
        {
            var schedResponse = await AssignmentClient.GetSchedulesAsync(assignmentCtrlNbr);
            var schedule = schedResponse.Schedules.FirstOrDefault();
            if (schedule is not null)
            {
                var shift = shiftDefinitions?.FirstOrDefault(s => s.CtrlNbr == schedule.ShiftDefinitionCtrlNbr);
                entry.ExistingShiftDisplay = shift?.DisplayName ?? "—";
                entry.ExistingDutyTimeDisplay = schedule.OnDutyTime;
                entry.ExistingOperatingDaysMask = schedule.OperatingDaysMask;
            }
        }
        catch { /* schedule load is best-effort */ }
    }

    private async Task Submit()
    {
        try
        {
            isSaving = true;
            isLoading = true;
            errorMessage = null;

            var request = new CrewSetupWizardRequest
            {
                ExistingCrewCtrlNbr = useExistingCrew ? selectedCrewCtrlNbr : 0,
                CrewType = useExistingCrew ? existingCrewType ?? "" : newCrewType ?? "",
                WorkAreaCtrlNbr = useExistingCrew ? 0 : newWorkAreaCtrlNbr,
                CrewDepartmentCtrlNbr = useExistingCrew ? 0 : newCrewDeptCtrlNbr,
                CrewName = useExistingCrew ? "" : newCrewName,
                EffectiveDate = effectiveDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O"),
                AbolishedDate = abolishedDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O") ?? ""
            };

            foreach (var entry in assignmentEntries)
            {
                var protoEntry = new CrewSetupWizardAssignmentEntry
                {
                    CrewWorkDaysMask = entry.CrewWorkDaysMask,
                    StartDate = entry.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O"),
                    EndDate = entry.EndDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O") ?? ""
                };

                if (entry.UseExisting)
                {
                    protoEntry.ExistingAssignmentCtrlNbr = entry.ExistingAssignmentCtrlNbr;
                }
                else
                {
                    protoEntry.GroupCtrlNbr = entry.NewAssignment.GroupCtrlNbr;
                    protoEntry.DepartmentCtrlNbr = entry.NewAssignment.DepartmentCtrlNbr;
                    protoEntry.Code = entry.NewAssignment.Code;
                    protoEntry.Name = entry.NewAssignment.Name;
                    protoEntry.IsExtra = entry.NewAssignment.IsExtra;
                    protoEntry.ShiftDefinitionCtrlNbr = entry.NewAssignment.ShiftDefinitionCtrlNbr;
                    protoEntry.AssignmentOperatingDaysMask = entry.NewAssignment.OperatingDaysMask;
                    protoEntry.OnDutyTime = entry.NewAssignment.OnDutyTime.ToString("HH:mm");
                    protoEntry.OffDutyTime = entry.NewAssignment.OffDutyTime.ToString("HH:mm");
                }

                request.Assignments.Add(protoEntry);
            }

            foreach (var pos in positionEntries)
            {
                if (pos.CraftRoleCtrlNbr > 0)
                {
                    request.Positions.Add(new CrewSetupWizardPositionEntry
                    {
                        CraftRoleCtrlNbr = pos.CraftRoleCtrlNbr,
                        DisplayOrder = pos.DisplayOrder
                    });
                }
            }

            var result = await CrewClient.CrewSetupWizardAsync(request);
            successSummary = BuildSuccessSummary(result);
            currentStep = 5; // show success
        }
        catch (Exception ex)
        {
            errorMessage = $"Wizard failed: {ex.Message}";
        }
        finally { isSaving = false; isLoading = false; }
    }

    private async Task CloseWizard()
    {
        if (currentStep == 5) await OnComplete.InvokeAsync();
        currentStep = 1;
        errorMessage = null;
        successSummary = null;
        useExistingCrew = false;
        selectedCrewCtrlNbr = 0;
        existingCrewType = null;
        newCrewType = null;
        newWorkAreaCtrlNbr = 0;
        newCrewDeptCtrlNbr = 0;
        newCrewName = "";
        effectiveDate = DateOnly.FromDateTime(DateTime.Today);
        abolishedDate = null;
        assignmentEntries = [];
        positionEntries = [];
        craftRoles = null;
        allGroups = null;
        await OnClose.InvokeAsync();
    }

    private static string BuildSuccessSummary(CrewSetupWizardResponse r)
    {
        var parts = new List<string>();

        if (r.IsExistingCrew)
            parts.Add($"Crew \"{r.CrewName}\" verified");
        else
            parts.Add($"Crew \"{r.CrewName}\" created");

        // Positions
        var posParts = new List<string>();
        if (r.PositionsCreated > 0) posParts.Add($"{r.PositionsCreated} added");
        if (r.PositionsDeleted > 0) posParts.Add($"{r.PositionsDeleted} removed");
        if (r.PositionsExisting > 0) posParts.Add($"{r.PositionsExisting} unchanged");
        if (posParts.Count > 0)
            parts.Add($"position(s): {string.Join(", ", posParts)}");

        // Assignments
        if (r.AssignmentsCreated > 0)
            parts.Add($"{r.AssignmentsCreated} assignment(s) created");
        if (r.AssignmentsUpdated > 0)
            parts.Add($"{r.AssignmentsUpdated} assignment(s) updated");

        // Schedules
        var schedParts = new List<string>();
        if (r.SchedulesCreated > 0) schedParts.Add($"{r.SchedulesCreated} added");
        if (r.SchedulesUpdated > 0) schedParts.Add($"{r.SchedulesUpdated} updated");
        if (r.SchedulesExisting > 0) schedParts.Add($"{r.SchedulesExisting} unchanged");
        if (schedParts.Count > 0)
            parts.Add($"schedule(s): {string.Join(", ", schedParts)}");

        // Crew assignments linked
        var caParts = new List<string>();
        if (r.CrewAssignmentsCreated > 0) caParts.Add($"{r.CrewAssignmentsCreated} linked");
        if (r.CrewAssignmentsUpdated > 0) caParts.Add($"{r.CrewAssignmentsUpdated} updated");
        if (r.CrewAssignmentsDeleted > 0) caParts.Add($"{r.CrewAssignmentsDeleted} removed");
        if (r.CrewAssignmentsExisting > 0) caParts.Add($"{r.CrewAssignmentsExisting} unchanged");
        if (caParts.Count > 0)
            parts.Add($"crew assignment(s): {string.Join(", ", caParts)}");

        return string.Join(" · ", parts) + ".";
    }

    // ── Position helpers ──
    private void AddPositionEntry() => positionEntries.Add(new() { DisplayOrder = positionEntries.Count + 1 });

    private void RemovePositionEntry(WizardPositionEntry entry)
    {
        positionEntries.Remove(entry);
        // Re-number display order
        for (var i = 0; i < positionEntries.Count; i++)
            positionEntries[i].DisplayOrder = i + 1;
    }

    // ── Entry models ──
    public sealed class WizardPositionEntry
    {
        public long CraftRoleCtrlNbr { get; set; }
        public int DisplayOrder { get; set; } = 1;
    }

    /// <summary>Fields needed to create a brand-new Assignment entity + its schedule.</summary>
    public sealed class NewAssignmentInfo
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public long GroupCtrlNbr { get; set; }
        public long DepartmentCtrlNbr { get; set; }
        public bool IsExtra { get; set; }

        // Schedule
        public long ShiftDefinitionCtrlNbr { get; set; }
        public int OperatingDaysMask { get; set; }
        public TimeOnly OnDutyTime { get; set; } = new(7, 0);
        public TimeOnly OffDutyTime { get; set; } = new(15, 0);

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Code)
            && !string.IsNullOrWhiteSpace(Name)
            && GroupCtrlNbr > 0
            && ShiftDefinitionCtrlNbr > 0;
    }

    /// <summary>Unified entry: assignment selection/creation (Step 2) + crew link details (Step 3).</summary>
    public sealed class CrewAssignmentEntry
    {
        // ── Step 2: Assignment selection ──
        public bool UseExisting { get; set; }
        public long ExistingAssignmentCtrlNbr { get; set; }
        public NewAssignmentInfo NewAssignment { get; set; } = new();

        // ── Display info for existing assignments ──
        public string ExistingCode { get; set; } = "";
        public string ExistingName { get; set; } = "";
        public string ExistingGroupName { get; set; } = "";
        public bool ExistingIsActive { get; set; }
        public bool ExistingIsExtra { get; set; }
        public string ExistingShiftDisplay { get; set; } = "";
        public string ExistingDutyTimeDisplay { get; set; } = "";
        public int ExistingOperatingDaysMask { get; set; }

        // ── Step 3: Crew assignment link fields ──
        public int CrewWorkDaysMask { get; set; }
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly? EndDate { get; set; }

        /// <summary>Step 2 validity: is the assignment itself properly defined?</summary>
        public bool IsAssignmentValid => UseExisting
            ? ExistingAssignmentCtrlNbr > 0
            : NewAssignment.IsValid;

        /// <summary>Full validity: assignment + crew link fields (for step 3 onwards).</summary>
        public bool IsValid => IsAssignmentValid;
    }
}
