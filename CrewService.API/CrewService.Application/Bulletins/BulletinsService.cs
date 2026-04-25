using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Bulletins;

public sealed class BulletinsService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<IReadOnlyList<PositionVacancy>> GetOpenVacanciesAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionVacancies.GetOpenAsync();
    }

    public async Task<IReadOnlyList<PositionVacancy>> GetVacanciesByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionVacancies.GetByCraftAsync(craftCtrlNbr);
    }

    public async Task<PositionVacancy> GetVacancyAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionVacancies.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Vacancy {ctrlNbr} not found.");
    }

    public async Task<PositionVacancy> AbolishVacancyAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Vacancy {ctrlNbr} not found.");
        vacancy.Abolish();
        await uow.PositionVacancies.UpdateAsync(vacancy, ct);
        await uow.CommitAsync(ct);
        return vacancy;
    }

    public async Task<IReadOnlyList<Bulletin>> GetPostedBulletinsAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Bulletins.GetPostedAsync();
    }

    public async Task<IReadOnlyList<Bulletin>> GetPostedBulletinsByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Bulletins.GetPostedByCraftAsync(craftCtrlNbr);
    }

    public async Task<Bulletin> GetBulletinAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Bulletins.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bulletin {ctrlNbr} not found.");
    }

    public async Task<BulletinBid> SubmitBidAsync(long bulletinCtrlNbr, long employeeCtrlNbr, int priority, int seniorityRank, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bid = BulletinBid.Create(bulletinCtrlNbr, employeeCtrlNbr, priority, seniorityRank);
        await uow.BulletinBids.AddAsync(bid, ct);
        await uow.CommitAsync(ct);
        return bid;
    }

    public async Task<BulletinBid> WithdrawBidAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var bid = await uow.BulletinBids.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Bid {ctrlNbr} not found.");
        bid.Withdraw();
        await uow.BulletinBids.UpdateAsync(bid, ct);
        await uow.CommitAsync(ct);
        return bid;
    }

    public async Task<IReadOnlyList<BulletinBid>> GetBidsByBulletinAsync(ControlNumber bulletinCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.BulletinBids.GetByBulletinAsync(bulletinCtrlNbr);
    }

    public async Task<IReadOnlyList<BulletinBid>> GetBidsByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.BulletinBids.GetByEmployeeAsync(employeeCtrlNbr);
    }
}
