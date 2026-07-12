using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.SeniorityOps;

public sealed class CraftAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<Craft>> GetAllCraftsAsync(
        ControlNumber? parentCtrlNbr = null, ControlNumber? railroadCtrlNbr = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return parentCtrlNbr is not null || railroadCtrlNbr is not null
            ? await uow.Crafts.GetByParentAndRailroadAsync(parentCtrlNbr, railroadCtrlNbr)
            : await uow.Crafts.GetAllAsync(ct);
    }

    public async Task<Craft> GetCraftAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Crafts.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Craft {ctrlNbr.Value} not found.");
    }

    // ── Create ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a craft. When <paramref name="workAreaCtrlNbr"/> is provided the standard boards
    /// (Extra Board, Hangout, Extended Absence) plus a Training roster and New Hires board are
    /// created in the same transaction. When omitted the craft is created alone and the caller is
    /// responsible for supplying work areas (e.g. the gRPC handler discovers them from the railroad).
    /// </summary>
    public async Task<(Craft Craft, Roster? Roster, List<RosterBoard> Boards)> CreateCraftAsync(
        ControlNumber? parentCtrlNbr,
        ControlNumber? dynamicGroupCtrlNbr,
        string craftName,
        string craftPluralName,
        int craftNumber,
        bool autoMarkUp,
        bool approveAllMarkOffs,
        int markOffHours,
        int markUpHours,
        int requiredRestHours,
        int maximumVacationDayTime,
        int unpaidMealPeriodMinutes,
        bool hoursofService,
        bool processPayroll,
        bool showNotifications,
        int vacationAssignmentType,
        ControlNumber? departmentCtrlNbr = null,
        ControlNumber? workAreaCtrlNbr = null,
        bool? createStandardRoster = null,
        bool? createExtraBoard = null,
        bool? createHangoutBoard = null,
        bool? createExtendedAbsenceBoard = null,
        bool? createTrainingRoster = null,
        bool? createNewHiresBoard = null,
        string? standardRosterName = null,
        string? standardRosterPluralName = null,
        string? trainingRosterName = null,
        string? trainingRosterPluralName = null,
        string? extraBoardName = null,
        string? hangoutBoardName = null,
        string? extendedAbsenceBoardName = null,
        string? newHiresBoardName = null,
        CancellationToken ct = default)
    {
        var craft = Craft.Create(
            parentCtrlNbr, dynamicGroupCtrlNbr,
            craftName, craftPluralName, craftNumber,
            autoMarkUp, approveAllMarkOffs,
            markOffHours, markUpHours, requiredRestHours,
            maximumVacationDayTime, unpaidMealPeriodMinutes,
            hoursofService, processPayroll, showNotifications,
            vacationAssignmentType, departmentCtrlNbr);

        var provisioningOptions = CraftProvisioningOptions.Create(
            craft,
            createStandardRoster,
            createExtraBoard,
            createHangoutBoard,
            createExtendedAbsenceBoard,
            createTrainingRoster,
            createNewHiresBoard,
            standardRosterName,
            standardRosterPluralName,
            trainingRosterName,
            trainingRosterPluralName,
            extraBoardName,
            hangoutBoardName,
            extendedAbsenceBoardName,
            newHiresBoardName);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.Crafts.Add(craft);

        Roster? roster = null;
        var boards = new List<RosterBoard>();

        if (workAreaCtrlNbr is not null)
        {
            (roster, boards) = CreateRostersAndBoards(uow, craft, workAreaCtrlNbr, provisioningOptions);
        }
        else if (dynamicGroupCtrlNbr is not null)
        {
            // Auto-create for every work area already under this railroad
            var workAreas = await uow.DynamicGroups.GetWorkAreasAsync(dynamicGroupCtrlNbr);
            foreach (var wa in workAreas)
            {
                var (r, b) = CreateRostersAndBoards(uow, craft, wa.CtrlNbr, provisioningOptions);
                roster ??= r;
                boards.AddRange(b);
            }
        }

        // Assign the system-wide Static strategy as the default for the new craft
        var staticStrategy = await uow.RequiredPositionsStrategies.GetStaticAsync(ct);
        if (staticStrategy is not null)
        {
            var existingAssignment = await uow.CraftRequiredPositionsStrategies.GetByCraftAsync(craft.CtrlNbr!, ct);
            if (existingAssignment is null)
                uow.CraftRequiredPositionsStrategies.Add(CraftRequiredPositionsStrategy.Create(craft.CtrlNbr!, staticStrategy.CtrlNbr!));
        }

        await uow.CommitAsync(ct);
        return (craft, roster, boards);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task<Craft> UpdateCraftAsync(
        ControlNumber ctrlNbr,
        string craftName,
        string craftPluralName,
        int craftNumber,
        bool autoMarkUp,
        bool approveAllMarkOffs,
        int markOffHours,
        int markUpHours,
        int requiredRestHours,
        int maximumVacationDayTime,
        int unpaidMealPeriodMinutes,
        bool hoursofService,
        bool processPayroll,
        bool showNotifications,
        int vacationAssignmentType,
        long departmentCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var craft = await uow.Crafts.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Craft {ctrlNbr.Value} not found.");

        craft.Update(
            craftName, craftPluralName, craftNumber,
            autoMarkUp, approveAllMarkOffs,
            markOffHours, markUpHours, requiredRestHours,
            maximumVacationDayTime, unpaidMealPeriodMinutes,
            hoursofService, processPayroll, showNotifications,
            vacationAssignmentType, departmentCtrlNbr);

        uow.Crafts.Update(craft);
        await uow.CommitAsync(ct);
        return craft;
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task<ControlNumber> DeleteCraftAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var craft = await uow.Crafts.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Craft {ctrlNbr.Value} not found.");

        uow.Crafts.Remove(craft);
        await uow.CommitAsync(ct);
        return craft.CtrlNbr;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (Roster? Roster, List<RosterBoard> Boards) CreateRostersAndBoards(
        IOrchestrationUnitOfWork uow, Craft craft, ControlNumber workAreaCtrlNbr, CraftProvisioningOptions options)
    {
        Roster? firstRoster = null;
        var boards = new List<RosterBoard>();

        if (options.CreateStandardRoster)
        {
            var roster = Roster.Create(
                craft.CtrlNbr, workAreaCtrlNbr,
                railroadPayrollDepartmentCtrlNbr: null,
                options.StandardRosterName, options.StandardRosterPluralName, rosterNumber: 1);
            uow.Rosters.Add(roster);
            firstRoster ??= roster;

            if (options.CreateExtraBoard)
            {
                boards.Add(RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr,
                    options.ExtraBoardName, BoardType.ExtraBoard, RotationType.FirstInFirstOut));
            }

            if (options.CreateHangoutBoard)
            {
                boards.Add(RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr,
                    options.HangoutBoardName, BoardType.Hangout));
            }

            if (options.CreateExtendedAbsenceBoard)
            {
                boards.Add(RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr,
                    options.ExtendedAbsenceBoardName, BoardType.ExtendedAbsence));
            }
        }

        if (options.CreateTrainingRoster)
        {
            var trainingRoster = Roster.Create(
                craft.CtrlNbr, workAreaCtrlNbr,
                railroadPayrollDepartmentCtrlNbr: null,
                options.TrainingRosterName, options.TrainingRosterPluralName, rosterNumber: 99,
                RosterType.Training);
            uow.Rosters.Add(trainingRoster);
            firstRoster ??= trainingRoster;

            if (options.CreateNewHiresBoard)
            {
                boards.Add(RosterBoard.Create(craft.CtrlNbr, trainingRoster.CtrlNbr,
                    options.NewHiresBoardName, BoardType.NewHire));
            }
        }

        foreach (var board in boards)
            uow.RosterBoards.Add(board);

        return (firstRoster, boards);
    }

    private sealed record CraftProvisioningOptions(
        bool CreateStandardRoster,
        bool CreateExtraBoard,
        bool CreateHangoutBoard,
        bool CreateExtendedAbsenceBoard,
        bool CreateTrainingRoster,
        bool CreateNewHiresBoard,
        string StandardRosterName,
        string StandardRosterPluralName,
        string TrainingRosterName,
        string TrainingRosterPluralName,
        string ExtraBoardName,
        string HangoutBoardName,
        string ExtendedAbsenceBoardName,
        string NewHiresBoardName)
    {
        public static CraftProvisioningOptions Create(
            Craft craft,
            bool? createStandardRoster,
            bool? createExtraBoard,
            bool? createHangoutBoard,
            bool? createExtendedAbsenceBoard,
            bool? createTrainingRoster,
            bool? createNewHiresBoard,
            string? standardRosterName,
            string? standardRosterPluralName,
            string? trainingRosterName,
            string? trainingRosterPluralName,
            string? extraBoardName,
            string? hangoutBoardName,
            string? extendedAbsenceBoardName,
            string? newHiresBoardName)
        {
            var hasExplicitStandardSelection = createStandardRoster.HasValue
                || createExtraBoard.HasValue
                || createHangoutBoard.HasValue
                || createExtendedAbsenceBoard.HasValue;

            var hasExplicitTrainingSelection = createTrainingRoster.HasValue
                || createNewHiresBoard.HasValue;

            var wantsStandardRoster = createStandardRoster ?? true;
            var wantsExtraBoard = createExtraBoard ?? (hasExplicitStandardSelection ? false : true);
            var wantsHangoutBoard = createHangoutBoard ?? (hasExplicitStandardSelection ? false : true);
            var wantsExtendedAbsenceBoard = createExtendedAbsenceBoard ?? (hasExplicitStandardSelection ? false : true);
            var wantsTrainingRoster = createTrainingRoster ?? true;
            var wantsNewHiresBoard = createNewHiresBoard ?? (hasExplicitTrainingSelection ? false : true);

            if (!wantsStandardRoster && (wantsExtraBoard || wantsHangoutBoard || wantsExtendedAbsenceBoard))
            {
                throw new ArgumentException("Standard roster must be enabled to create standard roster boards.");
            }

            if (!wantsTrainingRoster && wantsNewHiresBoard)
            {
                throw new ArgumentException("Training roster must be enabled to create the New Hires board.");
            }

            return new CraftProvisioningOptions(
                wantsStandardRoster,
                wantsStandardRoster && wantsExtraBoard,
                wantsStandardRoster && wantsHangoutBoard,
                wantsStandardRoster && wantsExtendedAbsenceBoard,
                wantsTrainingRoster,
                wantsTrainingRoster && wantsNewHiresBoard,
                ResolveName(standardRosterName, craft.CraftName, wantsStandardRoster, nameof(standardRosterName)),
                ResolveName(standardRosterPluralName, craft.CraftPluralName, wantsStandardRoster, nameof(standardRosterPluralName)),
                ResolveName(trainingRosterName, $"{craft.CraftName} Trainees", wantsTrainingRoster, nameof(trainingRosterName)),
                ResolveName(trainingRosterPluralName, $"{craft.CraftPluralName} Trainees", wantsTrainingRoster, nameof(trainingRosterPluralName)),
                ResolveName(extraBoardName, $"{craft.CraftName} Extra Board", wantsStandardRoster && wantsExtraBoard, nameof(extraBoardName)),
                ResolveName(hangoutBoardName, $"{craft.CraftName} Hangout", wantsStandardRoster && wantsHangoutBoard, nameof(hangoutBoardName)),
                ResolveName(extendedAbsenceBoardName, $"{craft.CraftName} Extended Absence", wantsStandardRoster && wantsExtendedAbsenceBoard, nameof(extendedAbsenceBoardName)),
                ResolveName(newHiresBoardName, $"{craft.CraftName} New Hires", wantsTrainingRoster && wantsNewHiresBoard, nameof(newHiresBoardName)));
        }

        private static string ResolveName(string? value, string fallback, bool isEnabled, string fieldName)
        {
            if (!isEnabled)
            {
                return fallback;
            }

            if (value is null)
            {
                return fallback;
            }

            var trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException($"{fieldName} is required when its roster/board is enabled.");
            }

            return trimmed;
        }
    }
}
