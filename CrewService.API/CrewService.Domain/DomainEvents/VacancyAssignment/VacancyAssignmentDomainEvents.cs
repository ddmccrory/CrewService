using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.VacancyAssignment;

public sealed record VacancyResolutionCompletedDomainEvent : DomainEvent
{
    public VacancyResolutionCompletedDomainEvent(ControlNumber runCtrlNbr, int slotsEvaluated, int slotsFilled)
        : base("VacancyResolutionRun", runCtrlNbr.Value,
            payload: new { RunCtrlNbr = runCtrlNbr.Value, SlotsEvaluated = slotsEvaluated, SlotsFilled = slotsFilled }) { }
}

public sealed record PositionSlotFilledByVacancyDomainEvent : DomainEvent
{
    public PositionSlotFilledByVacancyDomainEvent(ControlNumber slotCtrlNbr, ControlNumber employeeCtrlNbr)
        : base("PositionSlotInstance", slotCtrlNbr.Value,
            payload: new { SlotCtrlNbr = slotCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value }) { }
}
