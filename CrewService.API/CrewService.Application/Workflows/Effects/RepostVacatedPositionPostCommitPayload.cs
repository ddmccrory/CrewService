using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Workflows.Effects;

public sealed record RepostVacatedPositionPostCommitPayload(
    ControlNumber StaffablePositionCtrlNbr,
    ControlNumber? PreviousIncumbentCtrlNbr);