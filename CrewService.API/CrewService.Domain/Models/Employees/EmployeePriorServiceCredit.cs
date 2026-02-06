using CrewService.Domain.DomainEvents.Employees;
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
        var entity = new EmployeePriorServiceCredit(
            ControlNumber.Create(employeeCtrlNbr),
            serviceYears,
            serviceMonths,
            serviceDays);
        entity.Raise(new EmployeePriorServiceCreditCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public void Update(int serviceYears, int serviceMonths, int serviceDays)
    {
        var changes = new Dictionary<string, object?>();

        if (ServiceYears != serviceYears) { ServiceYears = serviceYears; changes["serviceYears"] = serviceYears; }
        if (ServiceMonths != serviceMonths) { ServiceMonths = serviceMonths; changes["serviceMonths"] = serviceMonths; }
        if (ServiceDays != serviceDays) { ServiceDays = serviceDays; changes["serviceDays"] = serviceDays; }

        if (changes.Count > 0)
        {
            Raise(new EmployeePriorServiceCreditUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }
    }

    public void Delete()
    {
        Raise(new EmployeePriorServiceCreditDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}