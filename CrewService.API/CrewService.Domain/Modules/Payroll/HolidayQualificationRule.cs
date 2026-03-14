using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Payroll;

public sealed class HolidayQualificationRule : Entity
{
    public ControlNumber HolidayCtrlNbr { get; private set; }
    public ControlNumber? CraftCtrlNbr { get; private set; }
    public bool RequireWorkDayBefore { get; private set; }
    public bool RequireWorkDayAfter { get; private set; }
    public string? ExemptAbsenceCodes { get; private set; }

    private HolidayQualificationRule() { HolidayCtrlNbr = null!; }

    public static HolidayQualificationRule Create(
        ControlNumber holidayCtrlNbr, bool requireWorkDayBefore, bool requireWorkDayAfter,
        ControlNumber? craftCtrlNbr = null, string? exemptAbsenceCodes = null)
    {
        return new HolidayQualificationRule
        {
            HolidayCtrlNbr = holidayCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            RequireWorkDayBefore = requireWorkDayBefore,
            RequireWorkDayAfter = requireWorkDayAfter,
            ExemptAbsenceCodes = exemptAbsenceCodes
        };
    }
}
