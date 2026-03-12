using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class CrewOffDay : Entity
{
    public ControlNumber CrewPositionCtrlNbr { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }

    private CrewOffDay() { CrewPositionCtrlNbr = null!; }

    public static CrewOffDay Create(ControlNumber crewPositionCtrlNbr, DayOfWeek dayOfWeek)
    {
        return new CrewOffDay
        {
            CrewPositionCtrlNbr = crewPositionCtrlNbr,
            DayOfWeek = dayOfWeek,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }
}
