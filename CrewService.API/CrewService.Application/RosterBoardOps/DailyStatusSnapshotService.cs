using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.RosterBoardOps;

public interface IDailyEmployeeStatusRepository
{
    Task AddAsync(DailyEmployeeStatusRecord record, CancellationToken ct = default);
}

public sealed record EmployeeStatusSnapshot(
    ControlNumber EmployeeCtrlNbr,
    string StatusCode,
    string? SnapshotJson = null);

public sealed class DailyStatusSnapshotService(IDailyEmployeeStatusRepository statusRepo)
{
    public async Task GenerateAsync(
        ControlNumber workAreaGroupCtrlNbr,
        IReadOnlyList<EmployeeStatusSnapshot> snapshots,
        DateOnly recordDate,
        CancellationToken ct = default)
    {
        foreach (var snapshot in snapshots)
        {
            var record = DailyEmployeeStatusRecord.Create(
                snapshot.EmployeeCtrlNbr, workAreaGroupCtrlNbr,
                recordDate, snapshot.StatusCode, snapshot.SnapshotJson);
            await statusRepo.AddAsync(record, ct);
        }
    }
}
