using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class FraDutyTour : Entity
{
    private readonly List<FraDutyTourSegment> _segments = [];
    private readonly List<FraTransportationSegment> _transportationSegments = [];
    private readonly List<FraOtherServiceSegment> _otherServiceSegments = [];

    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber RegulatoryStandardCtrlNbr { get; private set; }
    public DateTime DutyTourStartUtc { get; private set; }
    public DateTime? DutyTourEndUtc { get; private set; }
    public int? TotalTimeOnDutyMinutes { get; private set; }
    public int? ExcessMinutes { get; private set; }
    public string? ExcessServiceReason { get; private set; }
    public int PriorTimeOffMinutes { get; private set; }
    public int? EmployeeReportedPriorTimeOffMinutes { get; private set; }
    public bool PriorTimeOffReconciled { get; private set; }
    public int ConsecutiveDays { get; private set; }
    public bool IsQuickTieUp { get; private set; }
    public bool IsCertified { get; private set; }

    public IReadOnlyList<FraDutyTourSegment> Segments => _segments.AsReadOnly();
    public IReadOnlyList<FraTransportationSegment> TransportationSegments => _transportationSegments.AsReadOnly();
    public IReadOnlyList<FraOtherServiceSegment> OtherServiceSegments => _otherServiceSegments.AsReadOnly();

    private FraDutyTour()
    {
        EmployeeCtrlNbr = null!;
        RegulatoryStandardCtrlNbr = null!;
    }

    public static FraDutyTour Create(
        ControlNumber employeeCtrlNbr,
        ControlNumber regulatoryStandardCtrlNbr,
        DateTime dutyTourStartUtc,
        int priorTimeOffMinutes,
        int consecutiveDays)
    {
        var tour = new FraDutyTour
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            RegulatoryStandardCtrlNbr = regulatoryStandardCtrlNbr,
            DutyTourStartUtc = dutyTourStartUtc,
            PriorTimeOffMinutes = priorTimeOffMinutes,
            ConsecutiveDays = consecutiveDays,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
        return tour;
    }

    public FraDutyTourSegment AddSegment(
        ControlNumber onDutyRecordCtrlNbr,
        string positionDescription,
        string startLocationCode,
        DateTime startUtc)
    {
        var segment = FraDutyTourSegment.Create(
            CtrlNbr, onDutyRecordCtrlNbr, positionDescription,
            startLocationCode, startUtc, _segments.Count + 1);
        _segments.Add(segment);
        return segment;
    }

    public FraTransportationSegment AddTransportationSegment(
        string startLocationCode, DateTime startUtc,
        string endLocationCode, DateTime endUtc,
        string transportMode, bool isToAssignment)
    {
        var segment = FraTransportationSegment.Create(
            CtrlNbr, startLocationCode, startUtc,
            endLocationCode, endUtc, transportMode, isToAssignment);
        _transportationSegments.Add(segment);
        return segment;
    }

    public FraOtherServiceSegment AddOtherServiceSegment(
        string serviceTypeCode, string startLocationCode, DateTime startUtc,
        string endLocationCode, DateTime endUtc, bool isCommingled)
    {
        var segment = FraOtherServiceSegment.Create(
            CtrlNbr, serviceTypeCode, startLocationCode, startUtc,
            endLocationCode, endUtc, isCommingled);
        _otherServiceSegments.Add(segment);
        return segment;
    }

    public void Close(
        DateTime dutyTourEndUtc,
        int totalTimeOnDutyMinutes,
        int? excessMinutes,
        string? excessServiceReason,
        bool isQuickTieUp)
    {
        DutyTourEndUtc = dutyTourEndUtc;
        TotalTimeOnDutyMinutes = totalTimeOnDutyMinutes;
        ExcessMinutes = excessMinutes;
        ExcessServiceReason = excessServiceReason;
        IsQuickTieUp = isQuickTieUp;
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void SetEmployeeReportedPriorTimeOff(int minutes)
    {
        EmployeeReportedPriorTimeOffMinutes = minutes;
        PriorTimeOffReconciled = minutes == PriorTimeOffMinutes;
    }

    public void ReconcilePriorTimeOff()
    {
        PriorTimeOffReconciled = true;
    }

    public void Certify()
    {
        IsCertified = true;
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }
}
