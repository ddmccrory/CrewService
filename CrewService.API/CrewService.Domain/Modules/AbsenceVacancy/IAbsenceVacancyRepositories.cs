using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.AbsenceVacancy;

public interface IAbsenceRequestRepository : IRepository<AbsenceRequest>
{
    Task<List<AbsenceRequest>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr);
    Task<List<AbsenceRequest>> GetPendingAsync();
    Task<List<AbsenceRequest>> GetByDateAsync(
        ControlNumber railroadCtrlNbr,
        DateTime requestDateUtc,
        bool includeAllStatuses,
        CancellationToken ct = default);
    Task<List<AbsenceRequest>> GetByDateRangeAsync(
        ControlNumber railroadCtrlNbr,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        bool includeAllStatuses,
        ControlNumber? craftCtrlNbr = null,
        ControlNumber? departmentCtrlNbr = null,
        CancellationToken ct = default);
    Task<List<AbsenceRequest>> GetOpenAbsencesByRangeAsync(
        ControlNumber railroadCtrlNbr,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        ControlNumber? craftCtrlNbr = null,
        ControlNumber? departmentCtrlNbr = null,
        CancellationToken ct = default);
    Task<List<AbsenceRequest>> GetActiveMarkupBoundAsync(ControlNumber employeeCtrlNbr);
}

public interface IVacancyImpactRepository : IRepository<VacancyImpact>
{
    Task<List<VacancyImpact>> GetByAbsenceRequestAsync(ControlNumber absenceRequestCtrlNbr);
    Task<List<VacancyImpact>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr);
}
