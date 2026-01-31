using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employment;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class EmploymentStatusService(IEmploymentStatusRepository repository) : EmploymentStatusSrvc.EmploymentStatusSrvcBase
{
    private readonly IEmploymentStatusRepository _repository = repository;

    public override async Task<GetAllEmploymentStatusResponse> GetAllAsync(GetAllEmploymentStatusRequest request, ServerCallContext context)
    {
        var statuses = request.PageSize > 0
            ? await _repository.GetAllAsync(request.ClientCtrlNbr, request.PageNumber, request.PageSize)
            : await _repository.GetAllAsync(request.ClientCtrlNbr);

        var response = new GetAllEmploymentStatusResponse { TotalCount = statuses.Count };

        foreach (var status in statuses)
        {
            response.Statuses.Add(MapToResponse(status));
        }

        return response;
    }

    public override async Task<EmploymentStatusResponse> GetAsync(GetEmploymentStatusRequest request, ServerCallContext context)
    {
        var status = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employment status with control number {request.CtrlNbr} was not found."));

        return MapToResponse(status);
    }

    public override async Task<EmploymentStatusResponse> CreateAsync(CreateEmploymentStatusRequest request, ServerCallContext context)
    {
        var status = EmploymentStatus.Create(
            request.ClientCtrlNbr,
            request.StatusCode,
            request.StatusName,
            request.StatusNumber,
            request.EmploymentCode);

        _repository.Add(status);

        return MapToResponse(status, true, "Employment status created successfully.");
    }

    public override async Task<EmploymentStatusResponse> UpdateAsync(UpdateEmploymentStatusRequest request, ServerCallContext context)
    {
        var status = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employment status with control number {request.CtrlNbr} was not found."));

        status.Update(request.StatusCode, request.StatusName, request.StatusNumber, request.EmploymentCode);

        _repository.Update(status);

        return MapToResponse(status, true, "Employment status updated successfully.");
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteEmploymentStatusRequest request, ServerCallContext context)
    {
        var status = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employment status with control number {request.CtrlNbr} was not found."));

        _repository.Remove(status);

        return new DeleteResponse { Success = true, Messages = { "Employment status deleted successfully." } };
    }

    private static EmploymentStatusResponse MapToResponse(EmploymentStatus status, bool success = false, string? message = null)
    {
        var response = new EmploymentStatusResponse
        {
            CtrlNbr = status.CtrlNbr.Value,
            ClientCtrlNbr = status.ClientCtrlNbr.Value,
            StatusCode = status.StatusCode,
            StatusName = status.StatusName,
            StatusNumber = status.StatusNumber,
            EmploymentCode = status.EmploymentCode,
            Success = success
        };

        if (message is not null) response.Messages.Add(message);

        return response;
    }
}