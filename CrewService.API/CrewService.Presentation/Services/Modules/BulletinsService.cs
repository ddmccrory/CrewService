using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class BulletinsService(
    IPositionVacancyRepository vacancyRepository,
    IBulletinRepository bulletinRepository,
    IBulletinBidRepository bidRepository) : BulletinsSrvc.BulletinsSrvcBase
{
    // Position Vacancies

    public override async Task<GetVacanciesResponse> GetOpenVacancies(GetOpenVacanciesRequest request, ServerCallContext context)
    {
        var vacancies = await vacancyRepository.GetOpenAsync();
        var response = new GetVacanciesResponse { TotalCount = vacancies.Count };
        foreach (var v in vacancies) response.Vacancies.Add(MapVacancy(v));
        return response;
    }

    public override async Task<GetVacanciesResponse> GetVacanciesByCraft(GetVacanciesByCraftRequest request, ServerCallContext context)
    {
        var vacancies = await vacancyRepository.GetByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr));
        var response = new GetVacanciesResponse { TotalCount = vacancies.Count };
        foreach (var v in vacancies) response.Vacancies.Add(MapVacancy(v));
        return response;
    }

    public override async Task<PositionVacancyResponse> GetVacancy(GetVacancyRequest request, ServerCallContext context)
    {
        var vacancy = await vacancyRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Vacancy {request.CtrlNbr} not found."));
        return MapVacancy(vacancy);
    }

    public override async Task<PositionVacancyResponse> AbolishVacancy(AbolishVacancyRequest request, ServerCallContext context)
    {
        var vacancy = await vacancyRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Vacancy {request.CtrlNbr} not found."));
        vacancy.Abolish();
        await vacancyRepository.UpdateAsync(vacancy);
        return MapVacancy(vacancy);
    }

    // Bulletins

    public override async Task<GetBulletinsResponse> GetPostedBulletins(GetPostedBulletinsRequest request, ServerCallContext context)
    {
        var bulletins = await bulletinRepository.GetPostedAsync();
        var response = new GetBulletinsResponse { TotalCount = bulletins.Count };
        foreach (var b in bulletins) response.Bulletins.Add(MapBulletin(b));
        return response;
    }

    public override async Task<GetBulletinsResponse> GetPostedBulletinsByCraft(GetPostedBulletinsByCraftRequest request, ServerCallContext context)
    {
        var bulletins = await bulletinRepository.GetPostedByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr));
        var response = new GetBulletinsResponse { TotalCount = bulletins.Count };
        foreach (var b in bulletins) response.Bulletins.Add(MapBulletin(b));
        return response;
    }

    public override async Task<BulletinResponse> GetBulletin(GetBulletinRequest request, ServerCallContext context)
    {
        var bulletin = await bulletinRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Bulletin {request.CtrlNbr} not found."));
        return MapBulletin(bulletin);
    }

    // Bids

    public override async Task<BulletinBidResponse> SubmitBid(SubmitBidRequest request, ServerCallContext context)
    {
        var bid = BulletinBid.Create(request.BulletinCtrlNbr, request.EmployeeCtrlNbr, request.Priority, request.SeniorityRank);
        await bidRepository.AddAsync(bid);
        return MapBid(bid);
    }

    public override async Task<BulletinBidResponse> WithdrawBid(WithdrawBidRequest request, ServerCallContext context)
    {
        var bid = await bidRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Bid {request.CtrlNbr} not found."));
        bid.Withdraw();
        await bidRepository.UpdateAsync(bid);
        return MapBid(bid);
    }

    public override async Task<GetBidsResponse> GetBidsByBulletin(GetBidsByBulletinRequest request, ServerCallContext context)
    {
        var bids = await bidRepository.GetByBulletinAsync(ControlNumber.Create(request.BulletinCtrlNbr));
        var response = new GetBidsResponse { TotalCount = bids.Count };
        foreach (var b in bids) response.Bids.Add(MapBid(b));
        return response;
    }

    public override async Task<GetBidsResponse> GetBidsByEmployee(GetBidsByEmployeeRequest request, ServerCallContext context)
    {
        var bids = await bidRepository.GetByEmployeeAsync(ControlNumber.Create(request.EmployeeCtrlNbr));
        var response = new GetBidsResponse { TotalCount = bids.Count };
        foreach (var b in bids) response.Bids.Add(MapBid(b));
        return response;
    }

    // Mappers

    private static PositionVacancyResponse MapVacancy(PositionVacancy v) => new()
    {
        CtrlNbr = v.CtrlNbr.Value,
        TargetType = v.TargetType,
        TargetCtrlNbr = v.TargetCtrlNbr.Value,
        CraftCtrlNbr = v.CraftCtrlNbr.Value,
        VacancyReasonCode = v.VacancyReasonCode,
        PreviousIncumbentCtrlNbr = v.PreviousIncumbentCtrlNbr?.Value ?? 0,
        Status = v.Status,
        OpenedUtc = v.OpenedUtc.ToString("O"),
        ClosedUtc = v.ClosedUtc?.ToString("O") ?? string.Empty
    };

    private static BulletinResponse MapBulletin(Bulletin b) => new()
    {
        CtrlNbr = b.CtrlNbr.Value,
        PositionVacancyCtrlNbr = b.PositionVacancyCtrlNbr.Value,
        CraftCtrlNbr = b.CraftCtrlNbr.Value,
        BidWindowOpensUtc = b.BidWindowOpensUtc.ToString("O"),
        BidWindowClosesUtc = b.BidWindowClosesUtc.ToString("O"),
        Status = b.Status,
        AwardedEmployeeCtrlNbr = b.AwardedEmployeeCtrlNbr?.Value ?? 0,
        AwardType = b.AwardType ?? string.Empty
    };

    private static BulletinBidResponse MapBid(BulletinBid b) => new()
    {
        CtrlNbr = b.CtrlNbr.Value,
        BulletinCtrlNbr = b.BulletinCtrlNbr.Value,
        EmployeeCtrlNbr = b.EmployeeCtrlNbr.Value,
        Priority = b.Priority,
        SubmittedUtc = b.SubmittedUtc.ToString("O"),
        SeniorityRank = b.SeniorityRank,
        Status = b.Status
    };
}
