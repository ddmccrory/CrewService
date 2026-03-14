using CrewService.Domain.DomainEvents.MarkOff;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.AbsenceVacancy;

public sealed class CompensationBalance : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string CompensationType { get; private set; } = string.Empty;
    public decimal BalanceHours { get; private set; }
    public DateTime AsOfUtc { get; private set; }

    private CompensationBalance() { EmployeeCtrlNbr = null!; }

    public static CompensationBalance Create(
        ControlNumber employeeCtrlNbr, string compensationType, decimal initialHours)
    {
        return new CompensationBalance
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            CompensationType = compensationType,
            BalanceHours = initialHours,
            AsOfUtc = DateTime.UtcNow
        };
    }

    public bool Debit(decimal hours)
    {
        if (BalanceHours < hours) return false;
        BalanceHours -= hours;
        AsOfUtc = DateTime.UtcNow;
        Raise(new CompensationBalanceDebitedDomainEvent(CtrlNbr, EmployeeCtrlNbr, hours));
        return true;
    }

    public void Credit(decimal hours)
    {
        BalanceHours += hours;
        AsOfUtc = DateTime.UtcNow;
    }
}
