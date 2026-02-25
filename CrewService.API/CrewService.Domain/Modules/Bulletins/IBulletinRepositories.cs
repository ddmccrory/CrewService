using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Bulletins;

public interface IPositionVacancyRepository : IRepository<PositionVacancy>
{
    Task<List<PositionVacancy>> GetOpenAsync();
    Task<List<PositionVacancy>> GetByTargetAsync(string targetType, ControlNumber targetCtrlNbr);
    Task<List<PositionVacancy>> GetByCraftAsync(ControlNumber craftCtrlNbr);
}

public interface IBulletinRepository : IRepository<Bulletin>
{
    Task<Bulletin?> GetByVacancyAsync(ControlNumber positionVacancyCtrlNbr);
    Task<List<Bulletin>> GetPostedAsync();
    Task<List<Bulletin>> GetPostedByCraftAsync(ControlNumber craftCtrlNbr);
    Task<List<Bulletin>> GetByStatusAsync(string status);
}

public interface IBulletinBidRepository : IRepository<BulletinBid>
{
    Task<List<BulletinBid>> GetByBulletinAsync(ControlNumber bulletinCtrlNbr);
    Task<List<BulletinBid>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr);
    Task<List<BulletinBid>> GetActiveByEmployeeAsync(ControlNumber employeeCtrlNbr);
}
