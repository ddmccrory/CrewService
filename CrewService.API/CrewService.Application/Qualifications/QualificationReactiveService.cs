using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

/// <summary>
/// Previously materialized EmployeeQualification rows reactively.
/// Qualification status is now fully computed on demand -- no rows are written for
/// non-Manual qualification types. These methods are retained so callers continue to
/// compile but they are intentional no-ops.
/// </summary>
public sealed class QualificationReactiveService
{
    public Task HandleAddedToRosterAsync(ControlNumber employeeCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task HandleOnDutyRecordCreatedAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
        => Task.CompletedTask;
}
