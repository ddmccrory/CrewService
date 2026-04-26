using CrewService.Application.Employees;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class PriorServiceCreditService(PriorServiceCreditAppService priorServiceCreditAppService) : PriorServiceCreditSrvc.PriorServiceCreditSrvcBase
{
    public override async Task<PriorServiceCreditResponse> GetAsync(GetPriorServiceCreditRequest request, ServerCallContext context)
    {
        var credit = await priorServiceCreditAppService.GetByEmployeeAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        if (credit is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Prior service credit for employee {request.EmployeeCtrlNbr} was not found."));
        return MapToResponse(credit);
    }

    public override async Task<PriorServiceCreditResponse> CreateAsync(CreatePriorServiceCreditRequest request, ServerCallContext context)
    {
        try
        {
            var credit = await priorServiceCreditAppService.CreateAsync(
                ControlNumber.Create(request.EmployeeCtrlNbr), request.ServiceYears,
                request.ServiceMonths, request.ServiceDays, context.CancellationToken);
            return MapToResponse(credit, true, "Prior service credit created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
    }

    public override async Task<PriorServiceCreditResponse> UpdateAsync(UpdatePriorServiceCreditRequest request, ServerCallContext context)
    {
        var existing = await priorServiceCreditAppService.GetByEmployeeAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        if (existing is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Prior service credit for employee {request.EmployeeCtrlNbr} was not found."));
        try
        {
            var credit = await priorServiceCreditAppService.UpdateAsync(
                existing.CtrlNbr, request.ServiceYears, request.ServiceMonths, request.ServiceDays,
                context.CancellationToken);
            return MapToResponse(credit, true, "Prior service credit updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeletePriorServiceCreditRequest request, ServerCallContext context)
    {
        var existing = await priorServiceCreditAppService.GetByEmployeeAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        if (existing is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Prior service credit for employee {request.EmployeeCtrlNbr} was not found."));
        try
        {
            await priorServiceCreditAppService.DeleteAsync(existing.CtrlNbr, context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { "Prior service credit deleted successfully." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static PriorServiceCreditResponse MapToResponse(
        Domain.Models.Employees.EmployeePriorServiceCredit credit, bool success = false, string? message = null)
    {
        var response = new PriorServiceCreditResponse
        {
            CtrlNbr = credit.CtrlNbr.Value,
            EmployeeCtrlNbr = credit.EmployeeCtrlNbr.Value,
            ServiceYears = credit.ServiceYears,
            ServiceMonths = credit.ServiceMonths,
            ServiceDays = credit.ServiceDays,
            Success = success
        };
        if (message is not null) response.Messages.Add(message);
        return response;
    }
}