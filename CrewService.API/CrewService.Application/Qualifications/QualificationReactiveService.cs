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
    private readonly byte _instanceSentinel = 0;

    public Task HandleAddedToRosterAsync(ControlNumber employeeCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        _ = _instanceSentinel;
        _ = employeeCtrlNbr;
        _ = craftCtrlNbr;
        _ = ct;
        return Task.CompletedTask;
    }

    public Task HandleOnDutyRecordCreatedAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        _ = _instanceSentinel;
        _ = employeeCtrlNbr;
        _ = ct;
        return Task.CompletedTask;
    }
}
