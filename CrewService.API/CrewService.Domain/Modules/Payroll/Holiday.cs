using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Payroll;

public sealed class Holiday : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly ObservedDate { get; private set; }
    public bool IsActive { get; private set; }

    private Holiday() { WorkAreaGroupCtrlNbr = null!; }

    public static Holiday Create(
        ControlNumber workAreaGroupCtrlNbr, string name, DateOnly observedDate, bool isActive = true)
    {
        return new Holiday
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            Name = name,
            ObservedDate = observedDate,
            IsActive = isActive,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }
}
