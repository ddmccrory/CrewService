using CrewService.Domain.Models.Employees;
using CrewService.Domain.Repositories;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class PriorServiceCreditService(IEmployeePriorServiceCreditRepository repository) : PriorServiceCreditSrvc.PriorServiceCreditSrvcBase
{
    private readonly IEmployeePriorServiceCreditRepository _repository = repository;

    public override async Task<PriorServiceCreditResponse> GetAsync(GetPriorServiceCreditRequest request, ServerCallContext context)
    {
        var credit = await _repository.GetByEmployeeCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Prior service credit for employee {request.EmployeeCtrlNbr} was not found."));

        return MapToResponse(credit);
    }

    public override async Task<PriorServiceCreditResponse> CreateAsync(CreatePriorServiceCreditRequest request, ServerCallContext context)
    {
        var existing = await _repository.GetByEmployeeCtrlNbrAsync(request.EmployeeCtrlNbr);
        if (existing is not null)
            throw new RpcException(new Status(StatusCode.AlreadyExists, $"Prior service credit for employee {request.EmployeeCtrlNbr} already exists."));

        var credit = EmployeePriorServiceCredit.Create(
            request.EmployeeCtrlNbr,
            request.ServiceYears,
            request.ServiceMonths,
            request.ServiceDays);

        _repository.Add(credit);

        return MapToResponse(credit, true, "Prior service credit created successfully.");
    }

    public override async Task<PriorServiceCreditResponse> UpdateAsync(UpdatePriorServiceCreditRequest request, ServerCallContext context)
    {
        var credit = await _repository.GetByEmployeeCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Prior service credit for employee {request.EmployeeCtrlNbr} was not found."));

        credit.Update(request.ServiceYears, request.ServiceMonths, request.ServiceDays);

        _repository.Update(credit);

        return MapToResponse(credit, true, "Prior service credit updated successfully.");
    }

    public override async Task<DeleteResponse> DeleteAsync(DeletePriorServiceCreditRequest request, ServerCallContext context)
    {
        var credit = await _repository.GetByEmployeeCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Prior service credit for employee {request.EmployeeCtrlNbr} was not found."));

        _repository.Remove(credit);

        return new DeleteResponse { Success = true, Messages = { "Prior service credit deleted successfully." } };
    }

    private static PriorServiceCreditResponse MapToResponse(EmployeePriorServiceCredit credit, bool success = false, string? message = null)
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