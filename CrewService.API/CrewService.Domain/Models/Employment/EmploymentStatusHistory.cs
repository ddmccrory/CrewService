using CrewService.Domain.DomainEvents.Employment;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Employment;

public sealed class EmploymentStatusHistory : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber EmploymentStatusCtrlNbr { get; private set; }
    public DateTime StatusChangeDate { get; private set; }

    private EmploymentStatusHistory()
    {
        EmployeeCtrlNbr = null!;
        EmploymentStatusCtrlNbr = null!;
    }

    private EmploymentStatusHistory(
        ControlNumber employeeCtrlNbr,
        ControlNumber employmentStatusCtrlNbr,
        DateTime statusChangeDate)
    {
        EmployeeCtrlNbr = employeeCtrlNbr;
        EmploymentStatusCtrlNbr = employmentStatusCtrlNbr;
        StatusChangeDate = statusChangeDate;
    }

    public static EmploymentStatusHistory Create(
        long employeeCtrlNbr,
        long employmentStatusCtrlNbr,
        DateTime statusChangeDate)
    {
        var entity = new EmploymentStatusHistory(
            ControlNumber.Create(employeeCtrlNbr),
            ControlNumber.Create(employmentStatusCtrlNbr),
            statusChangeDate);
        entity.Raise(new EmploymentStatusHistoryCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public void Delete()
    {
        Raise(new EmploymentStatusHistoryDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}