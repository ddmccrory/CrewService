using CrewService.Domain.DomainEvents.Employees;
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
        var entity = new EmailAddress(
            employeeCtrlNbr,
            ControlNumber.Create(emailTypeCtrlNbr),
            email);
        entity.Raise(new EmailAddressCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public EmailAddress Update(string? email = null)
    {
        var changes = new Dictionary<string, object?>();

        if (email is not null)
        {
            Email = email;
            changes["email"] = email;
        }

        if (changes.Count > 0)
        {
            Raise(new EmailAddressUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }

        return this;
    }

    public void Delete()
    {
        Raise(new EmailAddressDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}