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
        return new AddressType(
            ControlNumber.Create(clientCtrlNbr),
            name,
            number,
            emergencyType);
    }

    public void Update(string name, int number, bool emergencyType)
    {
        Name = name;
        Number = number;
        EmergencyType = emergencyType;
    }
}