using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class RailroadInfoService(
    IRailroadInformationRepository infoRepo,
    IRailroadInformationReadReceiptRepository receiptRepo) : RailroadInfoSrvc.RailroadInfoSrvcBase
{
    public override async Task<RailroadInformationResponse> CreateInformation(CreateInformationRequest request, ServerCallContext context)
    {
        var info = RailroadInformation.Create(
            request.WorkAreaGroupCtrlNbr, request.InformationType, request.Subject, request.Body);
        await infoRepo.AddAsync(info);
        return MapInfo(info);
    }

    public override async Task<RailroadInformationResponse> GetInformation(GetInformationRequest request, ServerCallContext context)
    {
        var info = await infoRepo.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Railroad information {request.CtrlNbr} not found."));
        return MapInfo(info);
    }

    public override async Task<GetInformationListResponse> GetByWorkArea(GetByWorkAreaRequest request, ServerCallContext context)
    {
        var workArea = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var items = request.PublishedOnly
            ? await infoRepo.GetPublishedByWorkAreaAsync(workArea, context.CancellationToken)
            : await infoRepo.GetByWorkAreaAsync(workArea, context.CancellationToken);

        var response = new GetInformationListResponse { TotalCount = items.Count };
        foreach (var item in items) response.Items.Add(MapInfo(item));
        return response;
    }

    public override async Task<RailroadInformationResponse> PublishInformation(PublishInformationRequest request, ServerCallContext context)
    {
        var info = await GetRequiredAsync(request.CtrlNbr);
        info.Publish();
        await infoRepo.UpdateAsync(info);
        return MapInfo(info);
    }

    public override async Task<RailroadInformationResponse> CloseInformation(CloseInformationRequest request, ServerCallContext context)
    {
        var info = await GetRequiredAsync(request.CtrlNbr);
        info.Close();
        await infoRepo.UpdateAsync(info);
        return MapInfo(info);
    }

    public override async Task<RailroadInformationResponse> CancelInformation(CancelInformationRequest request, ServerCallContext context)
    {
        var info = await GetRequiredAsync(request.CtrlNbr);
        info.Cancel();
        await infoRepo.UpdateAsync(info);
        return MapInfo(info);
    }

    public override async Task<ReadReceiptResponse> AcknowledgeRead(AcknowledgeReadRequest request, ServerCallContext context)
    {
        var infoCtrl = ControlNumber.Create(request.InformationCtrlNbr);
        var empCtrl = ControlNumber.Create(request.EmployeeCtrlNbr);

        var existing = await receiptRepo.GetByInformationAndEmployeeAsync(infoCtrl, empCtrl, context.CancellationToken);
        if (existing is not null)
            return MapReceipt(existing);

        var receipt = RailroadInformationReadReceipt.Create(infoCtrl, empCtrl);
        await receiptRepo.AddAsync(receipt, context.CancellationToken);
        return MapReceipt(receipt);
    }

    public override async Task<GetReadReceiptsResponse> GetReadReceipts(GetReadReceiptsRequest request, ServerCallContext context)
    {
        var receipts = await receiptRepo.GetByInformationAsync(
            ControlNumber.Create(request.InformationCtrlNbr), context.CancellationToken);

        var response = new GetReadReceiptsResponse { TotalCount = receipts.Count };
        foreach (var r in receipts) response.Receipts.Add(MapReceipt(r));
        return response;
    }

    private async Task<RailroadInformation> GetRequiredAsync(long ctrlNbr) =>
        await infoRepo.GetByCtrlNbrAsync(ControlNumber.Create(ctrlNbr))
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"Railroad information {ctrlNbr} not found."));

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
