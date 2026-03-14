using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.AbsenceVacancy;

public sealed class AbsenceCodeCraftOverride : Entity
{
    public ControlNumber AbsenceCodeCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public decimal OverrideAutoMarkUpHours { get; private set; }

    private AbsenceCodeCraftOverride()
    {
        AbsenceCodeCtrlNbr = null!;
        CraftCtrlNbr = null!;
    }

    public static AbsenceCodeCraftOverride Create(
        ControlNumber absenceCodeCtrlNbr, ControlNumber craftCtrlNbr, decimal overrideAutoMarkUpHours)
    {
        return new AbsenceCodeCraftOverride
        {
            AbsenceCodeCtrlNbr = absenceCodeCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            OverrideAutoMarkUpHours = overrideAutoMarkUpHours
        };
    }
}
