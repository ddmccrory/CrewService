using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class FraOtherServiceSegment : Entity
{
    public ControlNumber DutyTourCtrlNbr { get; private set; }
    public string ServiceTypeCode { get; private set; } = string.Empty;
    public string StartLocationCode { get; private set; } = string.Empty;
    public DateTime StartUtc { get; private set; }
    public string EndLocationCode { get; private set; } = string.Empty;
    public DateTime EndUtc { get; private set; }
    public bool IsCommingled { get; private set; }

    private FraOtherServiceSegment()
    {
        DutyTourCtrlNbr = null!;
    }

    internal static FraOtherServiceSegment Create(
        ControlNumber dutyTourCtrlNbr,
        string serviceTypeCode,
        string startLocationCode, DateTime startUtc,
        string endLocationCode, DateTime endUtc,
        bool isCommingled)
    {
        return new FraOtherServiceSegment
        {
            DutyTourCtrlNbr = dutyTourCtrlNbr,
            ServiceTypeCode = serviceTypeCode,
            StartLocationCode = startLocationCode,
            StartUtc = startUtc,
            EndLocationCode = endLocationCode,
            EndUtc = endUtc,
            IsCommingled = isCommingled
        };
    }
}
