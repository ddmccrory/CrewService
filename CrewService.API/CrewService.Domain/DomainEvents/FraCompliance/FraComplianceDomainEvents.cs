using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.FraCompliance;

public sealed record FraDutyTourOpenedDomainEvent : DomainEvent
{
    public FraDutyTourOpenedDomainEvent(ControlNumber tourCtrlNbr, ControlNumber employeeCtrlNbr)
        : base("FraDutyTour", tourCtrlNbr.Value,
            payload: new { TourCtrlNbr = tourCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value }) { }
}

public sealed record FraDutyTourClosedDomainEvent : DomainEvent
{
    public FraDutyTourClosedDomainEvent(ControlNumber tourCtrlNbr, ControlNumber employeeCtrlNbr, int totalTimeOnDutyMinutes)
        : base("FraDutyTour", tourCtrlNbr.Value,
            payload: new { TourCtrlNbr = tourCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value, TotalTimeOnDutyMinutes = totalTimeOnDutyMinutes }) { }
}

public sealed record FraExcessServiceDetectedDomainEvent : DomainEvent
{
    public FraExcessServiceDetectedDomainEvent(ControlNumber reportCtrlNbr, ControlNumber employeeCtrlNbr, string violationType)
        : base("FraExcessServiceReport", reportCtrlNbr.Value,
            payload: new { ReportCtrlNbr = reportCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value, ViolationType = violationType }) { }
}

public sealed record FraConsecutiveDayLimitReachedDomainEvent : DomainEvent
{
    public FraConsecutiveDayLimitReachedDomainEvent(ControlNumber employeeCtrlNbr, int consecutiveDays, int tier)
        : base("FraDutyTour", employeeCtrlNbr.Value,
            payload: new { EmployeeCtrlNbr = employeeCtrlNbr.Value, ConsecutiveDays = consecutiveDays, Tier = tier }) { }
}
