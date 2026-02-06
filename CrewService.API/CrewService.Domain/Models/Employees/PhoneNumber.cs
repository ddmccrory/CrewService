using CrewService.Domain.DomainEvents.Employees;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Employees;

public sealed class PhoneNumber : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber PhoneTypeCtrlNbr { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public int CallingOrder { get; private set; }
    public bool DialOne { get; private set; }

    private PhoneNumber()
    {
        EmployeeCtrlNbr = null!;
        PhoneTypeCtrlNbr = null!;
    }

    private PhoneNumber(
        ControlNumber employeeCtrlNbr,
        ControlNumber phoneTypeCtrlNbr,
        string number,
        int callingOrder,
        bool dialOne)
    {
        EmployeeCtrlNbr = employeeCtrlNbr;
        PhoneTypeCtrlNbr = phoneTypeCtrlNbr;
        Number = number;
        CallingOrder = callingOrder;
        DialOne = dialOne;
    }

    internal static PhoneNumber Create(
        ControlNumber employeeCtrlNbr,
        long phoneTypeCtrlNbr,
        string number,
        int callingOrder,
        bool dialOne)
    {
        var entity = new PhoneNumber(
            employeeCtrlNbr,
            ControlNumber.Create(phoneTypeCtrlNbr),
            number,
            callingOrder,
            dialOne);
        entity.Raise(new PhoneNumberCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public PhoneNumber Update(string? number = null, int? callingOrder = null, bool? dialOne = null)
    {
        var changes = new Dictionary<string, object?>();

        if (number is not null)
        {
            Number = number;
            changes["number"] = number;
        }

        if (callingOrder.HasValue)
        {
            CallingOrder = callingOrder.Value;
            changes["callingOrder"] = callingOrder.Value;
        }

        if (dialOne.HasValue)
        {
            DialOne = dialOne.Value;
            changes["dialOne"] = dialOne.Value;
        }

        if (changes.Count > 0)
        {
            Raise(new PhoneNumberUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }

        return this;
    }

    public void Delete()
    {
        Raise(new PhoneNumberDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}