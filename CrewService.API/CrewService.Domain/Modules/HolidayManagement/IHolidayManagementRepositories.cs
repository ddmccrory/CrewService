using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.HolidayManagement;

public interface IRailroadHolidaySelectionRepository : IRepository<RailroadHolidaySelection>
{
    Task<IReadOnlyList<RailroadHolidaySelection>> GetActiveByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);

    Task<bool> HasOwnSelectionsAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}
