using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class BulletinsService(IServiceProvider serviceProvider) : BulletinsSrvc.BulletinsSrvcBase
{
    public override async Task<GetVacanciesResponse> GetOpenVacancies(GetOpenVacanciesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var vacancies = await svc.GetOpenVacanciesAsync(context.CancellationToken);
        var response = new GetVacanciesResponse { TotalCount = vacancies.Count };
        foreach (var v in vacancies) response.Vacancies.Add(MapVacancy(v));
        return response;
    }

    public override async Task<GetVacanciesResponse> GetVacanciesByCraft(GetVacanciesByCraftRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var vacancies = await svc.GetVacanciesByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken);
        var response = new GetVacanciesResponse { TotalCount = vacancies.Count };
        foreach (var v in vacancies) response.Vacancies.Add(MapVacancy(v));
        return response;
    }

    public override async Task<PositionVacancyResponse> GetVacancy(GetVacancyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try { return MapVacancy(await svc.GetVacancyAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<PositionVacancyResponse> AbolishVacancy(AbolishVacancyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try { return MapVacancy(await svc.AbolishVacancyAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetBulletinsResponse> GetPostedBulletins(GetPostedBulletinsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bulletins = await svc.GetPostedBulletinsAsync(context.CancellationToken);
        var response = new GetBulletinsResponse { TotalCount = bulletins.Count };
        foreach (var b in bulletins) response.Bulletins.Add(MapBulletin(b));
        return response;
    }

    public override async Task<GetBulletinsResponse> GetPostedBulletinsByCraft(GetPostedBulletinsByCraftRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bulletins = await svc.GetPostedBulletinsByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken);
        var response = new GetBulletinsResponse { TotalCount = bulletins.Count };
        foreach (var b in bulletins) response.Bulletins.Add(MapBulletin(b));
        return response;
    }

    public override async Task<BulletinResponse> GetBulletin(GetBulletinRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try { return MapBulletin(await svc.GetBulletinAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinBidResponse> SubmitBid(SubmitBidRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bid = await svc.SubmitBidAsync(
            request.BulletinCtrlNbr, request.EmployeeCtrlNbr, request.Priority, request.SeniorityRank, context.CancellationToken);
        return MapBid(bid);
    }

    public override async Task<BulletinBidResponse> WithdrawBid(WithdrawBidRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try { return MapBid(await svc.WithdrawBidAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetBidsResponse> GetBidsByBulletin(GetBidsByBulletinRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bids = await svc.GetBidsByBulletinAsync(ControlNumber.Create(request.BulletinCtrlNbr), context.CancellationToken);
        var response = new GetBidsResponse { TotalCount = bids.Count };
        foreach (var b in bids) response.Bids.Add(MapBid(b));
        return response;
    }

    public override async Task<GetBidsResponse> GetBidsByEmployee(GetBidsByEmployeeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bids = await svc.GetBidsByEmployeeAsync(ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        var response = new GetBidsResponse { TotalCount = bids.Count };
        foreach (var b in bids) response.Bids.Add(MapBid(b));
        return response;
    }

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
