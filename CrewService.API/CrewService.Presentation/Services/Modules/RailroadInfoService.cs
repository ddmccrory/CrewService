using CrewService.Application.RailroadInfo;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class RailroadInfoService(IServiceProvider serviceProvider) : RailroadInfoSrvc.RailroadInfoSrvcBase
{
    public override async Task<RailroadInformationResponse> CreateInformation(CreateInformationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.RailroadInfo.RailroadInfoService>();
        var info = await svc.CreateAsync(
            request.WorkAreaGroupCtrlNbr, request.InformationType, request.Subject, request.Body);
        return MapInfo(info);
    }

    public override async Task<RailroadInformationResponse> GetInformation(GetInformationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.RailroadInfo.RailroadInfoService>();
        try
        {
            return MapInfo(await svc.GetAsync(ControlNumber.Create(request.CtrlNbr)));
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetInformationListResponse> GetByWorkArea(GetByWorkAreaRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.RailroadInfo.RailroadInfoService>();
        var items = await svc.GetByWorkAreaAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), request.PublishedOnly, context.CancellationToken);
        var response = new GetInformationListResponse { TotalCount = items.Count };
        foreach (var item in items) response.Items.Add(MapInfo(item));
        return response;
    }

    public override async Task<RailroadInformationResponse> PublishInformation(PublishInformationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.RailroadInfo.RailroadInfoService>();
        try { return MapInfo(await svc.PublishAsync(ControlNumber.Create(request.CtrlNbr))); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<RailroadInformationResponse> CloseInformation(CloseInformationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.RailroadInfo.RailroadInfoService>();
        try { return MapInfo(await svc.CloseAsync(ControlNumber.Create(request.CtrlNbr))); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<RailroadInformationResponse> CancelInformation(CancelInformationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.RailroadInfo.RailroadInfoService>();
        try { return MapInfo(await svc.CancelAsync(ControlNumber.Create(request.CtrlNbr))); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<ReadReceiptResponse> AcknowledgeRead(AcknowledgeReadRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.RailroadInfo.RailroadInfoService>();
        var receipt = await svc.AcknowledgeReadAsync(
            ControlNumber.Create(request.InformationCtrlNbr),
            ControlNumber.Create(request.EmployeeCtrlNbr),
            context.CancellationToken);
        return MapReceipt(receipt);
    }

    public override async Task<GetReadReceiptsResponse> GetReadReceipts(GetReadReceiptsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.RailroadInfo.RailroadInfoService>();
        var receipts = await svc.GetReadReceiptsAsync(
            ControlNumber.Create(request.InformationCtrlNbr), context.CancellationToken);
        var response = new GetReadReceiptsResponse { TotalCount = receipts.Count };
        foreach (var r in receipts) response.Receipts.Add(MapReceipt(r));
        return response;
    }

    private static RailroadInformationResponse MapInfo(RailroadInformation info) => new()
    {
        CtrlNbr = info.CtrlNbr.Value,
        WorkAreaGroupCtrlNbr = info.WorkAreaGroupCtrlNbr.Value,
        InformationType = info.InformationType,
        Subject = info.Subject,
        Body = info.Body,
        Status = info.Status,
        PublishedAtUtc = info.PublishedAtUtc?.ToString("O") ?? string.Empty,
        ClosedAtUtc = info.ClosedAtUtc?.ToString("O") ?? string.Empty
    };

    private static ReadReceiptResponse MapReceipt(RailroadInformationReadReceipt r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        InformationCtrlNbr = r.InformationCtrlNbr.Value,
        EmployeeCtrlNbr = r.EmployeeCtrlNbr.Value,
        ReadAtUtc = r.ReadAtUtc.ToString("O")
    };
}

