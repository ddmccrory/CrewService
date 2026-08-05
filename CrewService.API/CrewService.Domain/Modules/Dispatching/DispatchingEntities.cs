using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Dispatching;

public sealed class DispatchProjection : Entity
{
    public ControlNumber PositionSlotCtrlNbr { get; private set; }
    public DateTime AsOfUtc { get; private set; }
    public ControlNumber? ProjectedEmployeeCtrlNbr { get; private set; }
    public string? TraceJson { get; private set; }
    public DateTime ComputedUtc { get; private set; }

    private DispatchProjection() { PositionSlotCtrlNbr = null!; }

    public static DispatchProjection Create(ControlNumber positionSlotCtrlNbr, DateTime asOfUtc,
        ControlNumber? projectedEmployeeCtrlNbr, string? traceJson)
    {
        return new DispatchProjection
        {
            PositionSlotCtrlNbr = positionSlotCtrlNbr,
            AsOfUtc = asOfUtc,
            ProjectedEmployeeCtrlNbr = projectedEmployeeCtrlNbr,
            TraceJson = traceJson,
            ComputedUtc = DateTime.UtcNow
        };
    }
}

public sealed class DispatchDecisionLog : Entity
{
    public ControlNumber PositionSlotCtrlNbr { get; private set; }
    public DateTime AsOfUtc { get; private set; }
    public string Phase { get; private set; } = string.Empty;
    public ControlNumber? SelectedEmployeeCtrlNbr { get; private set; }
    public string? SelectionSource { get; private set; }
    public string? DecisionJson { get; private set; }

    private DispatchDecisionLog() { PositionSlotCtrlNbr = null!; }

    public static DispatchDecisionLog Create(ControlNumber positionSlotCtrlNbr, DateTime asOfUtc, string phase,
        ControlNumber? selectedEmployeeCtrlNbr, string? selectionSource, string? decisionJson)
    {
        var log = new DispatchDecisionLog
        {
            PositionSlotCtrlNbr = positionSlotCtrlNbr,
            AsOfUtc = asOfUtc,
            Phase = phase,
            SelectedEmployeeCtrlNbr = selectedEmployeeCtrlNbr,
            SelectionSource = selectionSource,
            DecisionJson = decisionJson
        };
        log.Raise(new DispatchDecisionLoggedDomainEvent(log));
        return log;
    }
}

public sealed class DispatchOverride : Entity
{
    public ControlNumber PositionSlotCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string OverrideType { get; private set; } = string.Empty;
    public string ReasonCode { get; private set; } = string.Empty;
    public string? ReasonText { get; private set; }
    public string Status { get; private set; } = "PENDING";
    public ControlNumber? ApprovedByCtrlNbr { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }

    private DispatchOverride() { PositionSlotCtrlNbr = null!; EmployeeCtrlNbr = null!; }

    public static DispatchOverride Create(ControlNumber positionSlotCtrlNbr, ControlNumber employeeCtrlNbr,
        string overrideType, string reasonCode, string? reasonText)
    {
        return new DispatchOverride
        {
            PositionSlotCtrlNbr = positionSlotCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            OverrideType = overrideType,
            ReasonCode = reasonCode,
            ReasonText = reasonText
        };
    }

    public void Approve(ControlNumber approvedByCtrlNbr)
    {
        Status = "APPROVED";
        ApprovedByCtrlNbr = approvedByCtrlNbr;
        ApprovedAtUtc = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = "REJECTED";
    }
}

public sealed class EmployeeBooking : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public ControlNumber? PositionSlotCtrlNbr { get; private set; }

    private EmployeeBooking() { EmployeeCtrlNbr = null!; }

    public static EmployeeBooking Create(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc, ControlNumber? positionSlotCtrlNbr = null)
    {
        return new EmployeeBooking
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            StartUtc = startUtc,
            EndUtc = endUtc,
            PositionSlotCtrlNbr = positionSlotCtrlNbr
        };
    }
}

public sealed class VacancyFillLog : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public ControlNumber ShiftInstanceCtrlNbr { get; private set; }
    public ControlNumber PositionSlotCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber? OnDutyRecordCtrlNbr { get; private set; }
    public string AssignmentCode { get; private set; } = string.Empty;
    public string CraftRoleName { get; private set; } = string.Empty;
    public bool ForceOverride { get; private set; }
    public string? ForceReason { get; private set; }
    public bool Accepted { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public bool IsLateCall { get; private set; }
    public string? LateCallNote { get; private set; }
    public string? ArrivalFollowUpNote { get; private set; }
    public string? DispatcherNote { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private VacancyFillLog()
    {
        WorkAreaGroupCtrlNbr = null!;
        ShiftInstanceCtrlNbr = null!;
        PositionSlotCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static VacancyFillLog Create(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber positionSlotCtrlNbr,
        ControlNumber employeeCtrlNbr,
        ControlNumber? onDutyRecordCtrlNbr,
        string assignmentCode,
        string craftRoleName,
        bool forceOverride,
        string? forceReason,
        bool accepted,
        DateTime? acceptedAtUtc,
        bool isLateCall,
        string? lateCallNote,
        string? arrivalFollowUpNote,
        string? dispatcherNote,
        string status)
    {
        return new VacancyFillLog
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            PositionSlotCtrlNbr = positionSlotCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            OnDutyRecordCtrlNbr = onDutyRecordCtrlNbr,
            AssignmentCode = assignmentCode,
            CraftRoleName = craftRoleName,
            ForceOverride = forceOverride,
            ForceReason = forceReason,
            Accepted = accepted,
            AcceptedAtUtc = acceptedAtUtc,
            IsLateCall = isLateCall,
            LateCallNote = lateCallNote,
            ArrivalFollowUpNote = arrivalFollowUpNote,
            DispatcherNote = dispatcherNote,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

// Domain Events
public sealed record DispatchDecisionLoggedDomainEvent : DomainEvent
{
    public DispatchDecisionLoggedDomainEvent(DispatchDecisionLog log)
        : base(nameof(DispatchDecisionLog), log.CtrlNbr.Value, new { log.Phase, SelectedEmployeeCtrlNbr = log.SelectedEmployeeCtrlNbr?.Value }) { }
}
