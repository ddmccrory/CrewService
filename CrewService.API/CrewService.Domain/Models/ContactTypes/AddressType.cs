using CrewService.Domain.DomainEvents.ContactTypes;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.ContactTypes;

public sealed class AddressType : Entity
{
    public ControlNumber ClientCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Number { get; private set; }
    public bool EmergencyType { get; private set; }

    private AddressType()
    {
        ClientCtrlNbr = null!;
    }

    private AddressType(
        ControlNumber clientCtrlNbr,
        string name,
        int number,
        bool emergencyType)
    {
        ClientCtrlNbr = clientCtrlNbr;
        Name = name;
        Number = number;
        EmergencyType = emergencyType;
    }

    public static AddressType Create(
        long clientCtrlNbr,
        string name,
        int number,
        bool emergencyType)
    {
        var entity = new AddressType(
            ControlNumber.Create(clientCtrlNbr),
            name,
            number,
            emergencyType);
        entity.Raise(new AddressTypeCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public void Update(string name, int number, bool emergencyType)
    {
        var changes = new Dictionary<string, object?>();

        if (Name != name)
        {
            Name = name;
            changes["name"] = name;
        }

        if (Number != number)
        {
            Number = number;
            changes["number"] = number;
        }

        if (EmergencyType != emergencyType)
        {
            EmergencyType = emergencyType;
            changes["emergencyType"] = emergencyType;
        }

        if (changes.Count > 0)
        {
            Raise(new AddressTypeUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }
    }

    public void Delete()
    {
        Raise(new AddressTypeDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}