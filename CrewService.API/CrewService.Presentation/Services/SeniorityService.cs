using CrewService.Application.SeniorityOps;
using CrewService.Domain.Exceptions;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class SeniorityService(
    SeniorityAppService seniorityAppService,
    EmployeeNameService employeeNameService) : SenioritySrvc.SenioritySrvcBase
{
    public override async Task<GetAllSeniorityResponse> GetAllAsync(GetAllSeniorityRequest request, ServerCallContext context)
    {
        var response = new GetAllSeniorityResponse();

        ControlNumber? rosterCtrlNbr = request.RosterCtrlNbr > 0
            ? ControlNumber.Create(request.RosterCtrlNbr) : null;

        var items = await seniorityAppService.GetAllAsync(rosterCtrlNbr, context.CancellationToken);
        if (items.Count == 0)
        {
            response.TotalCount = 0;
            return response;
        }

        // Batch-resolve names for all unique userIds
        var userIds = items
            .Where(i => !string.IsNullOrEmpty(i.EmployeeUserId))
            .Select(i => i.EmployeeUserId)
            .Distinct()
            .ToList();
        var nameMap = await employeeNameService.GetFullNameLnfBatchAsync(userIds!);

        foreach (var item in items)
        {
            var fullName = !string.IsNullOrEmpty(item.EmployeeUserId) &&
                           nameMap.TryGetValue(item.EmployeeUserId!, out var n) ? n : string.Empty;
            var sr = new SeniorityResponse
            {
                CtrlNbr = item.Seniority.CtrlNbr.Value,
                RosterCtrlNbr = item.Seniority.RosterCtrlNbr.Value,
                EmployeeCtrlNbr = item.Seniority.EmployeeCtrlNbr.Value,
                LastActiveRoster = item.Seniority.LastActiveRoster,
                RosterDate = item.Seniority.RosterDate.ToString("yyyy-MM-dd"),
                Rank = item.Seniority.Rank,
                SeniorityStateCtrlNbr = item.Seniority.SeniorityStateCtrlNbr.Value,
                CanTrain = item.Seniority.CanTrain,
                EmployeeNumber = item.EmployeeNumber,
                EmployeeUserId = item.EmployeeUserId ?? string.Empty,
                SeniorityStateName = item.SeniorityStateName,
                EmployeeFullNameLnf = fullName
            };
            sr.RestrictionLabels.AddRange(item.RestrictionLabels);
            response.Seniority.Add(sr);
        }

        response.TotalCount = response.Seniority.Count;
        return response;
    }

    public override async Task<SeniorityResponse> GetAsync(GetSeniorityRequest request, ServerCallContext context)
    {
        try
        {
            var seniority = await seniorityAppService.GetAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapToResponse(seniority);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<SeniorityResponse> CreateAsync(CreateSeniorityRequest request, ServerCallContext context)
    {
        var seniority = await seniorityAppService.CreateAsync(
            ControlNumber.Create(request.RosterCtrlNbr),
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.LastActiveRoster,
            DateTime.Parse(request.RosterDate),
            request.Rank,
            ControlNumber.Create(request.SeniorityStateCtrlNbr),
            request.CanTrain,
            context.CancellationToken);
        return MapToResponse(seniority);
    }

    public override async Task<SeniorityResponse> UpdateAsync(UpdateSeniorityRequest request, ServerCallContext context)
    {
        try
        {
            var seniority = await seniorityAppService.UpdateAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.LastActiveRoster,
                DateTime.Parse(request.RosterDate),
                request.Rank,
                ControlNumber.Create(request.SeniorityStateCtrlNbr),
                request.CanTrain,
                context.CancellationToken);
            return MapToResponse(seniority);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteSeniorityRequest request, ServerCallContext context)
    {
        try
        {
            await seniorityAppService.DeleteAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { $"Seniority {request.CtrlNbr} deleted." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ActiveCraftResponse> GetActiveCraftForEmployee(GetActiveCraftRequest request, ServerCallContext context)
    {
        var (found, craftCtrlNbr, craftName) = await seniorityAppService.GetActiveCraftForEmployeeAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);

        if (!found) return new ActiveCraftResponse { Found = false };
        return new ActiveCraftResponse { CraftCtrlNbr = craftCtrlNbr, CraftName = craftName, Found = true };
    }

    private static SeniorityResponse MapToResponse(Domain.Models.Seniority.Seniority seniority)
    {
        return new SeniorityResponse
        {
            CtrlNbr = seniority.CtrlNbr.Value,
            RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
            EmployeeCtrlNbr = seniority.EmployeeCtrlNbr.Value,
            LastActiveRoster = seniority.LastActiveRoster,
            RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
            Rank = seniority.Rank,
            SeniorityStateCtrlNbr = seniority.SeniorityStateCtrlNbr.Value,
            CanTrain = seniority.CanTrain
        };
    }
}