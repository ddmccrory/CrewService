using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Dispatching;

public sealed class VacancyResolutionRun : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public ControlNumber ShiftInstanceCtrlNbr { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int SlotsEvaluated { get; private set; }
    public int SlotsFilled { get; private set; }
    public string Status { get; private set; } = "Running";

    private VacancyResolutionRun()
    {
        WorkAreaGroupCtrlNbr = null!;
        ShiftInstanceCtrlNbr = null!;
    }

    public static VacancyResolutionRun Start(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftInstanceCtrlNbr)
    {
        return new VacancyResolutionRun
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            StartedAtUtc = DateTime.UtcNow,
            Status = "Running",
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public void Complete(int slotsEvaluated, int slotsFilled)
    {
        SlotsEvaluated = slotsEvaluated;
        SlotsFilled = slotsFilled;
        CompletedAtUtc = DateTime.UtcNow;
        Status = "Completed";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void Fail()
    {
        CompletedAtUtc = DateTime.UtcNow;
        Status = "Failed";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }
}
