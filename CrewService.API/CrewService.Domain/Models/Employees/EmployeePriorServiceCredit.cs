using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Employees;

public sealed class EmployeePriorServiceCredit : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public int ServiceYears { get; private set; }
    public int ServiceMonths { get; private set; }
    public int ServiceDays { get; private set; }

    private EmployeePriorServiceCredit()
    {
        EmployeeCtrlNbr = null!;
    }

    private EmployeePriorServiceCredit(
        ControlNumber employeeCtrlNbr,
        int serviceYears,
        int serviceMonths,
        int serviceDays)
    {
        EmployeeCtrlNbr = employeeCtrlNbr;
        ServiceYears = serviceYears;
        ServiceMonths = serviceMonths;
        ServiceDays = serviceDays;
    }

    public static EmployeePriorServiceCredit Create(
        long employeeCtrlNbr,
        int serviceYears,
        int serviceMonths,
        int serviceDays)
    {
        return new EmployeePriorServiceCredit(
            ControlNumber.Create(employeeCtrlNbr),
            serviceYears,
            serviceMonths,
            serviceDays);
    }

    public void Update(int serviceYears, int serviceMonths, int serviceDays)
    {
        ServiceYears = serviceYears;
        ServiceMonths = serviceMonths;
        ServiceDays = serviceDays;
    }
}