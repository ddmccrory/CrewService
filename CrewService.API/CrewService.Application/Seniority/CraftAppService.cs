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
            : await uow.Crafts.GetAllAsync();
    }

    public async Task<Craft> GetCraftAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Crafts.GetByCtrlNbrAsync(ctrlNbr)
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

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.Crafts.Add(craft);

        Roster? roster = null;
        var boards = new List<RosterBoard>();

        if (workAreaCtrlNbr is not null)
        {
            (roster, boards) = CreateStandardRosterAndBoards(uow, craft, workAreaCtrlNbr);
        }
        else if (dynamicGroupCtrlNbr is not null)
        {
            // Auto-create for every work area already under this railroad
            var workAreas = await uow.DynamicGroups.GetWorkAreasAsync(dynamicGroupCtrlNbr);
            foreach (var wa in workAreas)
            {
                var (r, b) = CreateStandardRosterAndBoards(uow, craft, wa.CtrlNbr);
                roster ??= r;
                boards.AddRange(b);
            }
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
        var craft = await uow.Crafts.GetByCtrlNbrAsync(ctrlNbr)
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
        var craft = await uow.Crafts.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Craft {ctrlNbr.Value} not found.");
        uow.Crafts.Remove(craft);
        await uow.CommitAsync(ct);
        return craft.CtrlNbr;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (Roster Roster, List<RosterBoard> Boards) CreateStandardRosterAndBoards(
        IOrchestrationUnitOfWork uow, Craft craft, ControlNumber workAreaCtrlNbr)
    {
        var roster = Roster.Create(
            craft.CtrlNbr, workAreaCtrlNbr,
            railroadPayrollDepartmentCtrlNbr: null,
            craft.CraftName, craft.CraftPluralName, rosterNumber: 1);
        uow.Rosters.Add(roster);

        var boards = new List<RosterBoard>
        {
            RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr,
                $"{craft.CraftName} Extra Board", BoardType.ExtraBoard, RotationType.FirstInFirstOut),
            RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr,
                $"{craft.CraftName} Hangout", BoardType.Hangout),
            RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr,
                $"{craft.CraftName} Extended Absence", BoardType.ExtendedAbsence),
        };

        var trainingRoster = Roster.Create(
            craft.CtrlNbr, workAreaCtrlNbr,
            railroadPayrollDepartmentCtrlNbr: null,
            $"{craft.CraftName} Trainees", $"{craft.CraftPluralName} Trainees", rosterNumber: 99,
            RosterType.Training);
        uow.Rosters.Add(trainingRoster);

        boards.Add(RosterBoard.Create(craft.CtrlNbr, trainingRoster.CtrlNbr,
            $"{craft.CraftName} New Hires", BoardType.NewHire));

        foreach (var board in boards)
            uow.RosterBoards.Add(board);

        return (roster, boards);
    }
}
