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

    // ── Reference data caches (loaded once, persist across wizard sessions) ──
    private IReadOnlyList<GroupResponse>? workAreas;
    private IReadOnlyList<GroupResponse>? allGroups;
    private IReadOnlyList<StaffingAssignmentResponse>? existingAssignments;
    private IReadOnlyList<ShiftDefinitionResponse>? shiftDefinitions;
    private IReadOnlyList<CraftRoleResponse>? craftRoles;

    // ── Wizard form state (reset wholesale on close) ──
    private WizardState state = new();

    // ── Step navigation ──
    private string StepTitle => state.CurrentStep switch
    {
        1 => "Crew Setup Wizard — Step 1: Crew",
        2 => "Crew Setup Wizard — Step 2: Assignments",
        3 => "Crew Setup Wizard — Step 3: Crew Assignments",
        4 => "Crew Setup Wizard — Step 4: Review & Submit",
        _ => "Crew Setup Wizard"
    };

    private bool CanGoNext => state.CurrentStep switch
    {
        1 => state.UseExistingCrew
            ? state.SelectedCrewCtrlNbr > 0
            : state.NewWorkAreaCtrlNbr > 0
              && !string.IsNullOrWhiteSpace(state.NewCrewType)
              && !string.IsNullOrWhiteSpace(state.NewCrewName)
              && state.PositionEntries.Any(p => p.CraftRoleCtrlNbr > 0),
        2 => state.AssignmentEntries.Count > 0 && state.AssignmentEntries.All(a => a.IsAssignmentValid),
        3 => state.AssignmentEntries.Count > 0 && state.AssignmentEntries.All(a => a.IsValid),
        _ => false
    };

    protected override async Task OnParametersSetAsync()
    {
        if (Visible && workAreas is null)
        {
            workAreas = await ReferenceData.GetWorkAreasAsync();
            allGroups = await ReferenceData.GetGroupsAsync();
            craftRoles = await ReferenceData.GetCraftRolesAsync();
            shiftDefinitions = await ReferenceData.GetShiftDefinitionsAsync();
        }
    }

    private async Task GoNext()
    {
        try
        {
            state.IsLoading = true;
            if (state.CurrentStep == 1)
                await PrepareStep2();
            else if (state.CurrentStep == 2)
                PrepareStep3();
            state.CurrentStep++;
        }
        finally { state.IsLoading = false; }
    }

    private async Task PrepareStep2()
    {
        if (existingAssignments is null)
            await LoadExistingAssignments();

        var (_, dept) = GetCrewGroupAndDepartment();

        // Ensure at least one assignment card is open
        if (state.AssignmentEntries.Count == 0)
            state.AssignmentEntries.Add(new CrewAssignmentEntry
            {
                NewAssignment = new NewAssignmentInfo { DepartmentCtrlNbr = dept },
                StartDate = state.EffectiveDate
            });

        foreach (var entry in state.AssignmentEntries)
        {
            if (!entry.UseExisting && entry.NewAssignment.DepartmentCtrlNbr == 0)
                entry.NewAssignment.DepartmentCtrlNbr = dept;
            if (!entry.UseExisting && string.IsNullOrWhiteSpace(entry.NewAssignment.Code) && state.NewCrewType == "REGULAR")
                entry.NewAssignment.Code = state.NewCrewName;
        }
    }

    private void PrepareStep3()
    {
        // Default crew assignment dates from crew effective date (only for entries the user hasn't manually edited)
        foreach (var entry in state.AssignmentEntries)
        {
            if (!entry.HasUserSetStartDate && state.EffectiveDate != DateOnly.FromDateTime(DateTime.Today))
                entry.StartDate = state.EffectiveDate;
        }
    }

    private async Task OnExistingCrewSelected(long crewCtrlNbr)
    {
        state.SelectedCrewCtrlNbr = crewCtrlNbr;
        state.PositionEntries.Clear();
        state.AssignmentEntries.Clear();
        state.EffectiveDate = DateOnly.FromDateTime(DateTime.Today);
        state.AbolishedDate = null;
        state.ExistingCrewType = null;

        if (crewCtrlNbr <= 0) return;

        LoadCrewMetadata(crewCtrlNbr);

        state.IsLoading = true;
        StateHasChanged();

        if (existingAssignments is null)
            await LoadExistingAssignments();

        try { await LoadCrewAssignmentsAsync(crewCtrlNbr); }
        catch { /* best-effort */ }

        try { await LoadCrewPositionsAsync(crewCtrlNbr); }
        catch { /* best-effort */ }

        state.IsLoading = false;
    }

    private void LoadCrewMetadata(long crewCtrlNbr)
    {
        var selectedCrew = ExistingCrews?.FirstOrDefault(c => c.CtrlNbr == crewCtrlNbr);
        if (selectedCrew is null) return;

        state.ExistingCrewType = selectedCrew.CrewType;
        if (DateTime.TryParse(selectedCrew.EffectiveDate, out var eff))
            state.EffectiveDate = DateOnly.FromDateTime(eff);
        if (!string.IsNullOrWhiteSpace(selectedCrew.AbolishedDate) && DateTime.TryParse(selectedCrew.AbolishedDate, out var abol))
            state.AbolishedDate = DateOnly.FromDateTime(abol);
    }

    private async Task LoadCrewAssignmentsAsync(long crewCtrlNbr)
    {
        var crewAssignmentsResponse = await CrewClient.GetCrewAssignmentsAsync(crewCtrlNbr);
        foreach (var ca in crewAssignmentsResponse.Assignments)
        {
            var entry = new CrewAssignmentEntry { UseExisting = true };
            await PopulateExistingAssignmentInfo(entry, ca.AssignmentCtrlNbr);

            entry.CrewWorkDaysMask = ca.DaysOfWeekMask;

            if (DateTime.TryParse(ca.StartUtc, out var caStart))
            {
                entry.StartDate = DateOnly.FromDateTime(caStart);
                entry.HasUserSetStartDate = true;
            }
            if (!string.IsNullOrWhiteSpace(ca.EndUtc) && DateTime.TryParse(ca.EndUtc, out var caEnd))
                entry.EndDate = DateOnly.FromDateTime(caEnd);

            state.AssignmentEntries.Add(entry);
        }
    }

    private async Task LoadCrewPositionsAsync(long crewCtrlNbr)
    {
        var positionsResponse = await CrewClient.GetCrewPositionsAsync(crewCtrlNbr);
        foreach (var p in positionsResponse.Positions)
        {
            state.PositionEntries.Add(new WizardPositionEntry
            {
                CraftRoleCtrlNbr = p.CraftRoleCtrlNbr,
                DisplayOrder = p.DisplayOrder
            });
        }
    }

    private (long groupCtrlNbr, long departmentCtrlNbr) GetCrewGroupAndDepartment()
    {
        if (state.UseExistingCrew)
        {
            var crew = ExistingCrews?.FirstOrDefault(c => c.CtrlNbr == state.SelectedCrewCtrlNbr);
            return (crew?.WorkAreaCtrlNbr ?? 0, crew?.DepartmentCtrlNbr ?? 0);
        }
        return (state.NewWorkAreaCtrlNbr, state.NewCrewDeptCtrlNbr);
    }

    private void GoBack() => state.CurrentStep = Math.Max(1, state.CurrentStep - 1);

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
        state.AssignmentEntries.Add(new CrewAssignmentEntry
        {
            NewAssignment = new NewAssignmentInfo { DepartmentCtrlNbr = dept },
            StartDate = state.EffectiveDate
        });
    }

    private void RemoveAssignmentEntry(CrewAssignmentEntry entry)
    {
        if (state.AssignmentEntries.Count > 1)
            state.AssignmentEntries.Remove(entry);
    }

    private async Task OnExistingAssignmentSelected(CrewAssignmentEntry entry, long assignmentCtrlNbr)
    {
        await PopulateExistingAssignmentInfo(entry, assignmentCtrlNbr);
    }

    private async Task PopulateExistingAssignmentInfo(CrewAssignmentEntry entry, long assignmentCtrlNbr)
    {
        entry.ExistingAssignmentCtrlNbr = assignmentCtrlNbr;
        entry.ExistingSummary = null;
        if (assignmentCtrlNbr <= 0)
        {
            entry.NewAssignment = new();
            return;
        }

        var assignment = existingAssignments?.FirstOrDefault(a => a.CtrlNbr == assignmentCtrlNbr);
        if (assignment is null) return;

        // Populate the editable NewAssignment fields from the existing assignment
        entry.NewAssignment = new NewAssignmentInfo
        {
            Code = assignment.Code,
            Name = assignment.Name,
            GroupCtrlNbr = assignment.GroupCtrlNbr,
            DepartmentCtrlNbr = assignment.DepartmentCtrlNbr,
            IsExtra = assignment.IsExtra
        };

        var summary = new ExistingAssignmentSummary
        {
            Code = assignment.Code,
            Name = assignment.Name,
            IsExtra = assignment.IsExtra,
            GroupName = assignment.GroupName,
            IsActive = assignment.IsActive
        };

        try
        {
            var schedResponse = await AssignmentClient.GetSchedulesAsync(assignmentCtrlNbr);
            var schedule = schedResponse.Schedules.FirstOrDefault();
            if (schedule is not null)
            {
                var shift = shiftDefinitions?.FirstOrDefault(s => s.CtrlNbr == schedule.ShiftDefinitionCtrlNbr);
                summary.ShiftDisplay = shift?.DisplayName ?? "—";
                summary.DutyTimeDisplay = schedule.OnDutyTime;
                summary.OperatingDaysMask = schedule.OperatingDaysMask;

                entry.NewAssignment.ShiftDefinitionCtrlNbr = schedule.ShiftDefinitionCtrlNbr;
                entry.NewAssignment.OperatingDaysMask = schedule.OperatingDaysMask;
                if (TimeOnly.TryParse(schedule.OnDutyTime, out var onDuty))
                    entry.NewAssignment.OnDutyTime = onDuty;
                if (TimeOnly.TryParse(schedule.OffDutyTime, out var offDuty))
                    entry.NewAssignment.OffDutyTime = offDuty;
            }
        }
        catch { /* schedule load is best-effort */ }

        entry.ExistingSummary = summary;
    }

    private async Task Submit()
    {
        try
        {
            state.IsSaving = true;
            state.IsLoading = true;
            state.ErrorMessage = null;

            var request = BuildWizardRequest();
            var result = await CrewClient.CrewSetupWizardAsync(request);
            state.SuccessSummary = BuildSuccessSummary(result);
            state.CurrentStep = 5;
        }
        catch (Exception ex)
        {
            state.ErrorMessage = $"Wizard failed: {ex.Message}";
        }
        finally { state.IsSaving = false; state.IsLoading = false; }
    }

    private CrewSetupWizardRequest BuildWizardRequest()
    {
        var request = new CrewSetupWizardRequest
        {
            ExistingCrewCtrlNbr = state.UseExistingCrew ? state.SelectedCrewCtrlNbr : 0,
            CrewType = state.UseExistingCrew ? state.ExistingCrewType ?? "" : state.NewCrewType ?? "",
            WorkAreaCtrlNbr = state.UseExistingCrew ? 0 : state.NewWorkAreaCtrlNbr,
            CrewDepartmentCtrlNbr = state.UseExistingCrew ? 0 : state.NewCrewDeptCtrlNbr,
            CrewName = state.UseExistingCrew ? "" : state.NewCrewName,
            EffectiveDate = state.EffectiveDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O"),
            AbolishedDate = state.AbolishedDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O") ?? ""
        };

        foreach (var entry in state.AssignmentEntries)
        {
            var protoEntry = new CrewSetupWizardAssignmentEntry
            {
                ExistingAssignmentCtrlNbr = entry.UseExisting ? entry.ExistingAssignmentCtrlNbr : 0,
                GroupCtrlNbr = entry.NewAssignment.GroupCtrlNbr,
                DepartmentCtrlNbr = entry.NewAssignment.DepartmentCtrlNbr,
                Code = entry.NewAssignment.Code,
                Name = entry.NewAssignment.Name,
                IsExtra = entry.NewAssignment.IsExtra,
                ShiftDefinitionCtrlNbr = entry.NewAssignment.ShiftDefinitionCtrlNbr,
                AssignmentOperatingDaysMask = entry.NewAssignment.OperatingDaysMask,
                OnDutyTime = entry.NewAssignment.OnDutyTime.ToString("HH:mm"),
                OffDutyTime = entry.NewAssignment.OffDutyTime.ToString("HH:mm"),
                CrewWorkDaysMask = entry.CrewWorkDaysMask,
                StartDate = entry.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O"),
                EndDate = entry.EndDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O") ?? ""
            };

            request.Assignments.Add(protoEntry);
        }

        foreach (var pos in state.PositionEntries)
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

        return request;
    }

    private async Task CloseWizard()
    {
        if (state.CurrentStep == 5) await OnComplete.InvokeAsync();
        state = new WizardState();
        craftRoles = null;
        workAreas = null;
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
    private void AddPositionEntry() => state.PositionEntries.Add(new() { DisplayOrder = state.PositionEntries.Count + 1 });

    // ── Display helpers (keep LINQ out of .razor markup) ──
    private string GetCraftRoleDisplay(long ctrlNbr)
    {
        var role = craftRoles?.FirstOrDefault(r => r.CtrlNbr == ctrlNbr);
        return role is not null ? $"{role.Code.ToUpperInvariant()} — {role.Name}" : "—";
    }

    private string GetGroupName(long ctrlNbr)
    {
        return allGroups?.FirstOrDefault(g => g.CtrlNbr == ctrlNbr)?.Name ?? "—";
    }

    private string GetShiftDisplay(long ctrlNbr)
    {
        return shiftDefinitions?.FirstOrDefault(s => s.CtrlNbr == ctrlNbr)?.DisplayName ?? "—";
    }

    private void RemovePositionEntry(WizardPositionEntry entry)
    {
        state.PositionEntries.Remove(entry);
        // Re-number display order
        for (var i = 0; i < state.PositionEntries.Count; i++)
            state.PositionEntries[i].DisplayOrder = i + 1;
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

    /// <summary>Cached display info for an existing assignment selected in the wizard.</summary>
    public sealed class ExistingAssignmentSummary
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string GroupName { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsExtra { get; set; }
        public string ShiftDisplay { get; set; } = "";
        public string DutyTimeDisplay { get; set; } = "";
        public int OperatingDaysMask { get; set; }
    }

    /// <summary>Unified entry: assignment selection/creation (Step 2) + crew link details (Step 3).</summary>
    public sealed class CrewAssignmentEntry
    {
        // ── Step 2: Assignment selection ──
        public bool UseExisting { get; set; }
        public long ExistingAssignmentCtrlNbr { get; set; }
        public NewAssignmentInfo NewAssignment { get; set; } = new();

        /// <summary>Display cache populated when an existing assignment is selected.</summary>
        public ExistingAssignmentSummary? ExistingSummary { get; set; }

        // ── Step 3: Crew assignment link fields ──
        public int CrewWorkDaysMask { get; set; }
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly? EndDate { get; set; }

        /// <summary>Tracks whether the user explicitly changed the start date (prevents auto-defaulting).</summary>
        public bool HasUserSetStartDate { get; set; }

        /// <summary>Human-readable label for this entry regardless of UseExisting.</summary>
        public string DisplayLabel => UseExisting && ExistingAssignmentCtrlNbr <= 0
            ? "—"
            : $"{NewAssignment.Code.ToUpperInvariant()} — {NewAssignment.Name}";

        /// <summary>Step 2 validity: is the assignment itself properly defined?</summary>
        public bool IsAssignmentValid => UseExisting
            ? ExistingAssignmentCtrlNbr > 0 && NewAssignment.IsValid
            : NewAssignment.IsValid;

        /// <summary>Full validity: assignment + crew link fields (for step 3 onwards).</summary>
        public bool IsValid => IsAssignmentValid;
    }

    /// <summary>All mutable wizard form state, resettable as a single unit.</summary>
    public sealed class WizardState
    {
        public int CurrentStep { get; set; } = 1;
        public bool IsSaving { get; set; }
        public bool IsLoading { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessSummary { get; set; }

        // Step 1: Crew
        public bool UseExistingCrew { get; set; }
        public long SelectedCrewCtrlNbr { get; set; }
        public string? ExistingCrewType { get; set; }
        public string? NewCrewType { get; set; }
        public long NewWorkAreaCtrlNbr { get; set; }
        public long NewCrewDeptCtrlNbr { get; set; }
        public string NewCrewName { get; set; } = "";
        public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly? AbolishedDate { get; set; }

        // Steps 2 & 3: Assignments
        public List<CrewAssignmentEntry> AssignmentEntries { get; set; } = [];

        // Step 1b: Positions
        public List<WizardPositionEntry> PositionEntries { get; set; } = [];
    }
}
