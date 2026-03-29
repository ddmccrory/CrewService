using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class CallSheetGenerationService(
    IAssignmentQueryService templateQuery,
    IShiftDefinitionRepository shiftDefRepo,
    IShiftInstanceRepository shiftInstanceRepo)
{
    public async Task<IReadOnlyList<ShiftInstance>> GenerateAsync(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber workInstanceCtrlNbr,
        DateOnly targetDate,
        CancellationToken ct = default)
    {
        var templates = await templateQuery.GetTemplatesForDateAsync(workAreaGroupCtrlNbr, targetDate, ct);
        var shiftDefs = await shiftDefRepo.GetByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
        var activeShifts = shiftDefs.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToList();

        var createdShifts = new List<ShiftInstance>();

        foreach (var shiftDef in activeShifts)
        {
            var shiftStart = targetDate.ToDateTime(shiftDef.DefaultStartTime, DateTimeKind.Utc);
            var shiftEnd = targetDate.ToDateTime(shiftDef.DefaultEndTime, DateTimeKind.Utc);

            if (shiftEnd <= shiftStart)
                shiftEnd = shiftEnd.AddDays(1);

            var shiftInstance = ShiftInstance.Create(
                workInstanceCtrlNbr,
                shiftDef.ShiftCode,
                shiftStart,
                shiftEnd);

            foreach (var template in templates)
            {
                foreach (var position in template.Positions)
                {
                    shiftInstance.AddPositionSlot(
                        position.PositionCtrlNbr,
                        position.IncumbentEmployeeCtrlNbr,
                        position.DisplayOrder);
                }
            }

            await shiftInstanceRepo.AddAsync(shiftInstance, ct);
            createdShifts.Add(shiftInstance);
        }

        return createdShifts;
    }
}
