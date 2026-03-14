using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Payroll;

public sealed class PayRate : Entity
{
    public ControlNumber? PositionRoleCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public decimal HourlyRate { get; private set; }
    public decimal OvertimeMultiplier { get; private set; }

    private PayRate() { CraftCtrlNbr = null!; }

    public static PayRate Create(
        ControlNumber craftCtrlNbr, DateTime effectiveDate,
        decimal hourlyRate, decimal overtimeMultiplier = 1.5m,
        ControlNumber? positionRoleCtrlNbr = null)
    {
        return new PayRate
        {
            CraftCtrlNbr = craftCtrlNbr,
            PositionRoleCtrlNbr = positionRoleCtrlNbr,
            EffectiveDate = effectiveDate,
            HourlyRate = hourlyRate,
            OvertimeMultiplier = overtimeMultiplier
        };
    }

    public decimal CalculatePay(decimal hours, bool isOvertime)
    {
        var rate = isOvertime ? HourlyRate * OvertimeMultiplier : HourlyRate;
        return Math.Round(rate * hours, 2);
    }
}
