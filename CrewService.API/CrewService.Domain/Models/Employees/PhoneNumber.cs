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
        return new PhoneNumber(
            employeeCtrlNbr,
            ControlNumber.Create(phoneTypeCtrlNbr),
            number,
            callingOrder,
            dialOne);
    }

    public PhoneNumber Update(string? number = null, int? callingOrder = null, bool? dialOne = null)
    {
        if (number is not null) Number = number;
        if (callingOrder.HasValue) CallingOrder = callingOrder.Value;
        if (dialOne.HasValue) DialOne = dialOne.Value;

        return this;
    }
}