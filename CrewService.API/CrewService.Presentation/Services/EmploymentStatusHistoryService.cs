using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employment;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class EmploymentStatusHistoryService(IEmploymentStatusHistoryRepository repository) : EmploymentStatusHistorySrvc.EmploymentStatusHistorySrvcBase
{
    private readonly IEmploymentStatusHistoryRepository _repository = repository;

    public override async Task<GetAllStatusHistoryResponse> GetAllByEmployeeAsync(GetAllStatusHistoryRequest request, ServerCallContext context)
    {
        var history = request.PageSize > 0
            ? await _repository.GetAllByEmployeeAsync(request.EmployeeCtrlNbr, request.PageNumber, request.PageSize)
            : await _repository.GetAllByEmployeeAsync(request.EmployeeCtrlNbr);

        var response = new GetAllStatusHistoryResponse { TotalCount = history.Count };

        foreach (var record in history)
        {
            response.History.Add(MapToResponse(record));
        }

        return response;
    }

    public override async Task<StatusHistoryResponse> GetAsync(GetStatusHistoryRequest request, ServerCallContext context)
    {
        var record = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employment status history with control number {request.CtrlNbr} was not found."));

        return MapToResponse(record);
    }

    public override async Task<StatusHistoryResponse> CreateAsync(CreateStatusHistoryRequest request, ServerCallContext context)
    {
        var record = EmploymentStatusHistory.Create(
            request.EmployeeCtrlNbr,
            request.EmploymentStatusCtrlNbr,
            request.StatusChangeDate.ToDateTime());

        _repository.Add(record);

        return MapToResponse(record, true, "Employment status history created successfully.");
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteStatusHistoryRequest request, ServerCallContext context)
    {
        var record = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employment status history with control number {request.CtrlNbr} was not found."));

        _repository.Remove(record);

        return new DeleteResponse { Success = true, Messages = { "Employment status history deleted successfully." } };
    }

    private static StatusHistoryResponse MapToResponse(EmploymentStatusHistory record, bool success = false, string? message = null)
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