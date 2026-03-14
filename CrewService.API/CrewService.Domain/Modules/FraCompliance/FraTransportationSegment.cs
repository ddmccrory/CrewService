using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class FraTransportationSegment : Entity
{
    public ControlNumber DutyTourCtrlNbr { get; private set; }
    public string StartLocationCode { get; private set; } = string.Empty;
    public DateTime StartUtc { get; private set; }
    public string EndLocationCode { get; private set; } = string.Empty;
    public DateTime EndUtc { get; private set; }
    public string TransportMode { get; private set; } = string.Empty;
    public bool IsToAssignment { get; private set; }

    private FraTransportationSegment()
    {
        DutyTourCtrlNbr = null!;
    }

    internal static FraTransportationSegment Create(
        ControlNumber dutyTourCtrlNbr,
        string startLocationCode, DateTime startUtc,
        string endLocationCode, DateTime endUtc,
        string transportMode, bool isToAssignment)
    {
        return new FraTransportationSegment
        {
            DutyTourCtrlNbr = dutyTourCtrlNbr,
            StartLocationCode = startLocationCode,
            StartUtc = startUtc,
            EndLocationCode = endLocationCode,
            EndUtc = endUtc,
            TransportMode = transportMode,
            IsToAssignment = isToAssignment
        };
    }
}
