using CrewService.Application.DailyOperations;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.BackgroundWorkers;

public interface IDailyCallSheetManualOverrideStore
{
    void Schedule(DateTime scheduledUtc, DailyCallSheetDueWorkItem item);
    IReadOnlyList<DailyCallSheetDueWorkItem> DequeueDue(ControlNumber workAreaGroupCtrlNbr, DateTime nowUtc);
    DateTime? GetNextScheduledUtc(ControlNumber workAreaGroupCtrlNbr);
    (DateTime ScheduledUtc, DailyCallSheetDueWorkItem Item)? GetNextScheduled(ControlNumber workAreaGroupCtrlNbr);
}

public sealed class DailyCallSheetManualOverrideStore : IDailyCallSheetManualOverrideStore
{
    private readonly Lock _lock = new();
    private readonly List<(DateTime ScheduledUtc, DailyCallSheetDueWorkItem Item)> _scheduled = [];

    public void Schedule(DateTime scheduledUtc, DailyCallSheetDueWorkItem item)
    {
        var utc = DateTime.SpecifyKind(scheduledUtc, DateTimeKind.Utc);
        lock (_lock)
        {
            _scheduled.Add((utc, item));
        }
    }

    public IReadOnlyList<DailyCallSheetDueWorkItem> DequeueDue(ControlNumber workAreaGroupCtrlNbr, DateTime nowUtc)
    {
        var utcNow = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        lock (_lock)
        {
            var due = _scheduled
                .Where(x => x.Item.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && x.ScheduledUtc <= utcNow)
                .ToList();

            if (due.Count == 0)
                return [];

            _scheduled.RemoveAll(x => x.Item.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && x.ScheduledUtc <= utcNow);
            return due.Select(x => x.Item).ToList();
        }
    }

    public DateTime? GetNextScheduledUtc(ControlNumber workAreaGroupCtrlNbr)
    {
        lock (_lock)
        {
            return _scheduled
                .Where(x => x.Item.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr)
                .Select(x => (DateTime?)x.ScheduledUtc)
                .OrderBy(x => x)
                .FirstOrDefault();
        }
    }

    public (DateTime ScheduledUtc, DailyCallSheetDueWorkItem Item)? GetNextScheduled(ControlNumber workAreaGroupCtrlNbr)
    {
        lock (_lock)
        {
            return _scheduled
                .Where(x => x.Item.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr)
                .OrderBy(x => x.ScheduledUtc)
                .Select(x => ((DateTime ScheduledUtc, DailyCallSheetDueWorkItem Item)?)x)
                .FirstOrDefault();
        }
    }
}
