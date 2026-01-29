using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Employees;

public sealed class EmailAddress : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber EmailTypeCtrlNbr { get; private set; }
    public string Email { get; private set; } = string.Empty;

    private EmailAddress()
    {
        EmployeeCtrlNbr = null!;
        EmailTypeCtrlNbr = null!;
    }

    private EmailAddress(
        ControlNumber employeeCtrlNbr,
        ControlNumber emailTypeCtrlNbr,
        string email)
    {
        EmployeeCtrlNbr = employeeCtrlNbr;
        EmailTypeCtrlNbr = emailTypeCtrlNbr;
        Email = email;
    }

    internal static EmailAddress Create(
        ControlNumber employeeCtrlNbr,
        long emailTypeCtrlNbr,
        string email)
    {
        return new EmailAddress(
            employeeCtrlNbr,
            ControlNumber.Create(emailTypeCtrlNbr),
            email);
    }

    public EmailAddress Update(string? email = null)
    {
        if (email is not null) Email = email;
        return this;
    }
}