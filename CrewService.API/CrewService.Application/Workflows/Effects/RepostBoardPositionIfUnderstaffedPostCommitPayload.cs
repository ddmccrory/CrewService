using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Workflows.Effects;

public sealed record RepostBoardPositionIfUnderstaffedPostCommitPayload(
    ControlNumber BoardCtrlNbr,
    ControlNumber VacatedStaffablePositionCtrlNbr,
    ControlNumber? PreviousIncumbentCtrlNbr,
    bool EnforceUnderstaffedPolicy = true);
