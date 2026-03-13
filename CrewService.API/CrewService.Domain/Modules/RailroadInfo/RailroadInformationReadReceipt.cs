using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.RailroadInfo;

public sealed class RailroadInformationReadReceipt : Entity
{
    public ControlNumber InformationCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public DateTime ReadAtUtc { get; private set; }

    private RailroadInformationReadReceipt()
    {
        InformationCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static RailroadInformationReadReceipt Create(
        ControlNumber informationCtrlNbr, ControlNumber employeeCtrlNbr)
    {
        return new RailroadInformationReadReceipt
        {
            InformationCtrlNbr = informationCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            ReadAtUtc = DateTime.UtcNow
        };
    }
}
