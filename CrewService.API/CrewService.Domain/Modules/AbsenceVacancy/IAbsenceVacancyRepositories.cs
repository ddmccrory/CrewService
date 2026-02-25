using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.AbsenceVacancy;

public interface IAbsenceRequestRepository : IRepository<AbsenceRequest>
{
    Task<List<AbsenceRequest>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr);
    Task<List<AbsenceRequest>> GetPendingAsync();
    Task<List<AbsenceRequest>> GetActiveMarkupBoundAsync(ControlNumber employeeCtrlNbr);
}

public interface IVacancyImpactRepository : IRepository<VacancyImpact>
{
    Task<List<VacancyImpact>> GetByAbsenceRequestAsync(ControlNumber absenceRequestCtrlNbr);
    Task<List<VacancyImpact>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr);
}
