using CrewService.Application.Employment;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class EmploymentStatusService(EmploymentAppService employmentAppService) : EmploymentStatusSrvc.EmploymentStatusSrvcBase
{
    public override async Task<GetAllEmploymentStatusResponse> GetAllAsync(GetAllEmploymentStatusRequest request, ServerCallContext context)
    {
        var statuses = await employmentAppService.GetAllStatusesAsync(
            ControlNumber.Create(request.ClientCtrlNbr), request.PageNumber, request.PageSize,
            context.CancellationToken);

        var response = new GetAllEmploymentStatusResponse { TotalCount = statuses.Count };
        foreach (var status in statuses)
            response.Statuses.Add(MapToResponse(status));
        return response;
    }

    public override async Task<EmploymentStatusResponse> GetAsync(GetEmploymentStatusRequest request, ServerCallContext context)
    {
        try
        {
            var status = await employmentAppService.GetStatusAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapToResponse(status);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<EmploymentStatusResponse> CreateAsync(CreateEmploymentStatusRequest request, ServerCallContext context)
    {
        var status = await employmentAppService.CreateStatusAsync(
            ControlNumber.Create(request.ClientCtrlNbr), request.StatusCode, request.StatusName,
            request.StatusNumber, request.EmploymentCode, context.CancellationToken);
        return MapToResponse(status, true, "Employment status created successfully.");
    }

    public override async Task<EmploymentStatusResponse> UpdateAsync(UpdateEmploymentStatusRequest request, ServerCallContext context)
    {
        try
        {
            var status = await employmentAppService.UpdateStatusAsync(
                ControlNumber.Create(request.CtrlNbr), request.StatusCode, request.StatusName,
                request.StatusNumber, request.EmploymentCode, context.CancellationToken);
            return MapToResponse(status, true, "Employment status updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteEmploymentStatusRequest request, ServerCallContext context)
    {
        try
        {
            await employmentAppService.DeleteStatusAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { "Employment status deleted successfully." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static EmploymentStatusResponse MapToResponse(
        Domain.Models.Employment.EmploymentStatus status, bool success = false, string? message = null)
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