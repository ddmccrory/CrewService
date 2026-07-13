using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Bulletins;

public interface IPositionVacancyRepository : IRepository<PositionVacancy>
{
    Task<List<PositionVacancy>> GetOpenAsync();
    Task<List<PositionVacancy>> GetOpenByRailroadAsync(ControlNumber railroadCtrlNbr);
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
    Task<List<Bulletin>> GetPostedByRailroadAsync(ControlNumber railroadCtrlNbr);
    Task<List<Bulletin>> GetPostedByCraftAsync(ControlNumber craftCtrlNbr);
    Task<List<Bulletin>> GetActiveAsync();
    Task<List<Bulletin>> GetActiveByRailroadAsync(ControlNumber railroadCtrlNbr);
    Task<List<Bulletin>> GetByStatusAsync(string status);
    Task<List<Bulletin>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr);
    /// <summary>
    /// Returns Posted bulletins whose BidWindowClosesUtc has passed and that have not yet
    /// been awarded (AwardedEmployeeCtrlNbr is null). These are candidates for auto-award
    /// or auto-no-bid processing by the BulletinProcessingWorker.
    /// </summary>
    Task<List<Bulletin>> GetClosedUnawardedAsync(CancellationToken ct = default);
    /// <summary>
    /// Returns NoBid bulletins on crew positions whose ForceAssignDeadlineUtc has passed
    /// and that have not yet been force-assigned (AwardedEmployeeCtrlNbr is null).
    /// </summary>
    Task<List<Bulletin>> GetNoBidPastDeadlineAsync(CancellationToken ct = default);
    /// <summary>
    /// Returns all bulletins whose BidWindowOpensUtc is on or after <paramref name="fromUtc"/>,
    /// optionally scoped to a railroad.
    /// </summary>
    Task<List<Bulletin>> GetInDateRangeAsync(DateTime fromUtc, ControlNumber? railroadCtrlNbr = null);

    /// <summary>
    /// Returns the earliest UTC datetime at which the bulletin worker needs to wake:
    /// the minimum of all future <c>BidWindowClosesUtc</c> on Posted bulletins and all future
    /// <c>ForceAssignDeadlineUtc</c> on NoBid bulletins. Returns <c>null</c> when there are
    /// no pending events.
    /// </summary>
    Task<DateTime?> GetNextPendingEventUtcAsync(CancellationToken ct = default);
    /// <summary>
    /// Returns the bulletin that drives the next pending event (the one whose
    /// <c>BidWindowClosesUtc</c> or <c>ForceAssignDeadlineUtc</c> is earliest in the future).
    /// Used to resolve the correct work-area timezone for display.
    /// </summary>
    Task<Bulletin?> GetNextPendingEventBulletinAsync(CancellationToken ct = default);
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

public interface IBulletinAccessAuditRepository : IRepository<BulletinAccessAudit>
{
    Task<bool> ExistsWithinWindowAsync(
        ControlNumber bulletinCtrlNbr,
        ControlNumber employeeCtrlNbr,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        CancellationToken ct = default);
}
