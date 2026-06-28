using CrewService.Application.SeniorityOps;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class RosterService(RosterAppService rosterAppService) : RosterSrvc.RosterSrvcBase
{
    public override async Task<GetAllRosterResponse> GetAllAsync(GetAllRosterRequest request, ServerCallContext context)
    {
        var (rosters, workAreaNames, craftNames) = await rosterAppService.GetRostersAsync(
            request.CraftCtrlNbr, request.ParentCtrlNbr, request.DynamicGroupCtrlNbr, context.CancellationToken);

        var response = new GetAllRosterResponse();
        foreach (var roster in rosters)
        {
            workAreaNames.TryGetValue(roster.WorkAreaGroupCtrlNbr, out var waName);
            craftNames.TryGetValue(roster.CraftCtrlNbr, out var craftName);
            response.Rosters.Add(MapToResponse(roster, waName ?? string.Empty, craftName ?? string.Empty));
        }
        return response;
    }

    public override async Task<RosterResponse> GetAsync(GetRosterRequest request, ServerCallContext context)
    {
        var (roster, waName, craftName) = await rosterAppService.GetRosterAsync(
            ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
        return roster is null ? new RosterResponse() : MapToResponse(roster, waName, craftName);
    }

    public override async Task<RosterResponse> CreateAsync(CreateRosterRequest request, ServerCallContext context)
    {
        var roster = await rosterAppService.CreateRosterAsync(
            ControlNumber.Create(request.CraftCtrlNbr),
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            request.RailroadPayrollDepartmentCtrlNbr > 0 ? ControlNumber.Create(request.RailroadPayrollDepartmentCtrlNbr) : null,
            request.RosterName,
            request.RosterPluralName,
            request.RosterNumber,
            ct: context.CancellationToken);

        var (_, waName, craftName) = await rosterAppService.GetRosterAsync(roster.CtrlNbr, context.CancellationToken);
        return MapToResponse(roster, waName, craftName);
    }

    public override async Task<RosterResponse> UpdateAsync(UpdateRosterRequest request, ServerCallContext context)
    {
        try
        {
            var roster = await rosterAppService.UpdateRosterAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.RosterName,
                request.RosterPluralName,
                request.RosterNumber,
                context.CancellationToken);

            var (_, waName, craftName) = await rosterAppService.GetRosterAsync(roster.CtrlNbr, context.CancellationToken);
            return MapToResponse(roster, waName, craftName);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteRosterRequest request, ServerCallContext context)
    {
        try
        {
            var ctrlNbr = await rosterAppService.DeleteRosterAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { $"Roster {ctrlNbr.Value} deleted." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static RosterResponse MapToResponse(Roster roster, string workAreaName = "", string craftName = "") => new()
    {
        CtrlNbr = roster.CtrlNbr.Value,
        CraftCtrlNbr = roster.CraftCtrlNbr.Value,
        WorkAreaGroupCtrlNbr = roster.WorkAreaGroupCtrlNbr.Value,
        WorkAreaName = workAreaName,
        CraftName = craftName,
        RailroadPayrollDepartmentCtrlNbr = roster.RailroadPayrollDepartmentCtrlNbr?.Value ?? 0,
        RosterName = roster.RosterName,
        RosterPluralName = roster.RosterPluralName,
        RosterNumber = roster.RosterNumber,
        RosterType = roster.RosterType.ToString()
    };
}
