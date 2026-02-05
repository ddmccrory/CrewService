using CrewService.Domain.DomainEvents.Employment;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Employment;

public sealed class EmploymentStatus : Entity
{
    public ControlNumber ClientCtrlNbr { get; private set; }
    public string StatusCode { get; private set; } = string.Empty;
    public string StatusName { get; private set; } = string.Empty;
    public int StatusNumber { get; private set; }
    public string EmploymentCode { get; private set; } = string.Empty;

    private EmploymentStatus()
    {
        ClientCtrlNbr = null!;
    }

    private EmploymentStatus(
        ControlNumber clientCtrlNbr,
        string statusCode,
        string statusName,
        int statusNumber,
        string employmentCode)
    {
        ClientCtrlNbr = clientCtrlNbr;
        StatusCode = statusCode;
        StatusName = statusName;
        StatusNumber = statusNumber;
        EmploymentCode = employmentCode;
    }

    public static EmploymentStatus Create(
        long clientCtrlNbr,
        string statusCode,
        string statusName,
        int statusNumber,
        string employmentCode)
    {
        var entity = new EmploymentStatus(
            ControlNumber.Create(clientCtrlNbr),
            statusCode,
            statusName,
            statusNumber,
            employmentCode);
        entity.Raise(new EmploymentStatusCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public void Update(string statusCode, string statusName, int statusNumber, string employmentCode)
    {
        var changes = new Dictionary<string, object?>();

        if (StatusCode != statusCode) { StatusCode = statusCode; changes["statusCode"] = statusCode; }
        if (StatusName != statusName) { StatusName = statusName; changes["statusName"] = statusName; }
        if (StatusNumber != statusNumber) { StatusNumber = statusNumber; changes["statusNumber"] = statusNumber; }
        if (EmploymentCode != employmentCode) { EmploymentCode = employmentCode; changes["employmentCode"] = employmentCode; }

        if (changes.Count > 0)
        {
            Raise(new EmploymentStatusUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }
    }

    public void Delete()
    {
        Raise(new EmploymentStatusDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}