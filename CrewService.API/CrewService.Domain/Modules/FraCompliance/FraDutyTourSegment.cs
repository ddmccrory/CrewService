using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class FraDutyTourSegment : Entity
{
    public ControlNumber DutyTourCtrlNbr { get; private set; }
    public ControlNumber OnDutyRecordCtrlNbr { get; private set; }
    public string PositionDescription { get; private set; } = string.Empty;
    public string StartLocationCode { get; private set; } = string.Empty;
    public DateTime StartUtc { get; private set; }
    public string? EndLocationCode { get; private set; }
    public DateTime? EndUtc { get; private set; }
    public int SegmentOrder { get; private set; }

    private FraDutyTourSegment()
    {
        DutyTourCtrlNbr = null!;
        OnDutyRecordCtrlNbr = null!;
    }

    internal static FraDutyTourSegment Create(
        ControlNumber dutyTourCtrlNbr,
        ControlNumber onDutyRecordCtrlNbr,
        string positionDescription,
        string startLocationCode,
        DateTime startUtc,
        int segmentOrder)
    {
        return new FraDutyTourSegment
        {
            DutyTourCtrlNbr = dutyTourCtrlNbr,
            OnDutyRecordCtrlNbr = onDutyRecordCtrlNbr,
            PositionDescription = positionDescription,
            StartLocationCode = startLocationCode,
            StartUtc = startUtc,
            SegmentOrder = segmentOrder
        };
    }

    public void Complete(string endLocationCode, DateTime endUtc)
    {
        EndLocationCode = endLocationCode;
        EndUtc = endUtc;
    }
}
