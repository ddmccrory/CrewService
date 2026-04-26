using CrewService.Application.Employment;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class EmploymentStatusHistoryService(EmploymentAppService employmentAppService) : EmploymentStatusHistorySrvc.EmploymentStatusHistorySrvcBase
{
    public override async Task<GetAllStatusHistoryResponse> GetAllByEmployeeAsync(GetAllStatusHistoryRequest request, ServerCallContext context)
    {
        var history = await employmentAppService.GetAllHistoryAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), request.PageNumber, request.PageSize,
            context.CancellationToken);

        var response = new GetAllStatusHistoryResponse { TotalCount = history.Count };
        foreach (var record in history)
            response.History.Add(MapToResponse(record));
        return response;
    }

    public override async Task<StatusHistoryResponse> GetAsync(GetStatusHistoryRequest request, ServerCallContext context)
    {
        try
        {
            var record = await employmentAppService.GetHistoryRecordAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapToResponse(record);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<StatusHistoryResponse> CreateAsync(CreateStatusHistoryRequest request, ServerCallContext context)
    {
        var record = await employmentAppService.CreateHistoryRecordAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.EmploymentStatusCtrlNbr),
            request.StatusChangeDate.ToDateTime(),
            context.CancellationToken);
        return MapToResponse(record, true, "Employment status history created successfully.");
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteStatusHistoryRequest request, ServerCallContext context)
    {
        try
        {
            await employmentAppService.DeleteHistoryRecordAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { "Employment status history deleted successfully." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static StatusHistoryResponse MapToResponse(
        Domain.Models.Employment.EmploymentStatusHistory record, bool success = false, string? message = null)
    {
        var response = new StatusHistoryResponse
        {
            CtrlNbr = record.CtrlNbr.Value,
            EmployeeCtrlNbr = record.EmployeeCtrlNbr.Value,
            EmploymentStatusCtrlNbr = record.EmploymentStatusCtrlNbr.Value,
            StatusChangeDate = Timestamp.FromDateTime(DateTime.SpecifyKind(record.StatusChangeDate, DateTimeKind.Utc)),
            Success = success
        };
        if (message is not null) response.Messages.Add(message);
        return response;
    }
}