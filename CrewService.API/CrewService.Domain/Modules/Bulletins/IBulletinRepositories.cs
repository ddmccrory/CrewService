using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Bulletins;

public interface IPositionVacancyRepository : IRepository<PositionVacancy>
{
    Task<List<PositionVacancy>> GetOpenAsync();
    Task<List<PositionVacancy>> GetByTargetAsync(string targetType, ControlNumber targetCtrlNbr);
    Task<List<PositionVacancy>> GetByCraftAsync(ControlNumber craftCtrlNbr);
    Task<List<PositionVacancy>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr);
    /// <summary>
    /// Returns the average number of Board-type vacancies opened per day over the last 30 days
    /// for the given work area and craft - the input to the legacy NbrOfRequiredExtraBoardPositions formula.
    /// </summary>
    Task<double> GetAverageDailyBoardVacanciesAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default);
}

public interface IBulletinRepository : IRepository<Bulletin>
{
    Task<Bulletin?> GetByVacancyAsync(ControlNumber positionVacancyCtrlNbr);
    Task<List<Bulletin>> GetPostedAsync();
    Task<List<Bulletin>> GetPostedByCraftAsync(ControlNumber craftCtrlNbr);
    Task<List<Bulletin>> GetByStatusAsync(string status);
    Task<List<Bulletin>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr);
    /// <summary>
    /// Returns NoBid bulletins on crew positions whose ForceAssignDeadlineUtc has passed
    /// and that have not yet been force-assigned (AwardedEmployeeCtrlNbr is null).
    /// </summary>
    Task<List<Bulletin>> GetNoBidPastDeadlineAsync(CancellationToken ct = default);
}

public interface IBulletinBidRepository : IRepository<BulletinBid>
{
    Task<List<BulletinBid>> GetByBulletinAsync(ControlNumber bulletinCtrlNbr);
    Task<List<BulletinBid>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr);
    Task<List<BulletinBid>> GetActiveByEmployeeAsync(ControlNumber employeeCtrlNbr);
}

public interface IBulletinRuleRepository : IRepository<BulletinRule>
{
    Task<BulletinRule?> GetByCraftAsync(ControlNumber craftCtrlNbr);
}
