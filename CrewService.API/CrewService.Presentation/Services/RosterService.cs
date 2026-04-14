using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class RosterService(IRosterRepository rosterRepository, ICraftRepository craftRepository, IDynamicGroupRepository dynamicGroupRepository) : RosterSrvc.RosterSrvcBase
{
    private readonly IRosterRepository _rosterRepository = rosterRepository;
    private readonly ICraftRepository _craftRepository = craftRepository;
    private readonly IDynamicGroupRepository _dynamicGroupRepository = dynamicGroupRepository;

    public override async Task<GetAllRosterResponse> GetAllAsync(GetAllRosterRequest request, ServerCallContext context)
    {
        var response = new GetAllRosterResponse();

        List<Roster> rosters;

        if (request.CraftCtrlNbr > 0)
        {
            rosters = await _rosterRepository.GetByCraftCtrlNbrAsync(ControlNumber.Create(request.CraftCtrlNbr));
        }
        else if (request.ParentCtrlNbr > 0)
        {
            var crafts = await _craftRepository.GetByParentAndRailroadAsync(
                ControlNumber.Create(request.ParentCtrlNbr),
                request.DynamicGroupCtrlNbr > 0 ? ControlNumber.Create(request.DynamicGroupCtrlNbr) : null);

            var craftCtrlNbrs = crafts.Select(c => c.CtrlNbr).ToList();
            rosters = craftCtrlNbrs.Count > 0
                ? await _rosterRepository.GetByCraftCtrlNbrsAsync(craftCtrlNbrs)
                : [];
        }
        else
        {
            rosters = await _rosterRepository.GetAllAsync();
        }

        var workAreaCtrlNbrs = rosters.Select(r => r.WorkAreaGroupCtrlNbr).Distinct().ToList();
        var workAreas = await _dynamicGroupRepository.GetByCtrlNbrsAsync(workAreaCtrlNbrs);
        var workAreaNames = workAreas.ToDictionary(g => g.CtrlNbr, g => g.Name);

        var distinctCraftCtrlNbrs = rosters.Select(r => r.CraftCtrlNbr).Distinct().ToList();
        var craftNames = new Dictionary<ControlNumber, string>();
        foreach (var ctrlNbr in distinctCraftCtrlNbrs)
        {
            var craft = await _craftRepository.GetByCtrlNbrAsync(ctrlNbr);
            if (craft is not null) craftNames[ctrlNbr] = craft.CraftName;
        }

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
        var roster = await _rosterRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr));
        if (roster is null) return new RosterResponse();
        var waName = await ResolveWorkAreaNameAsync(roster.WorkAreaGroupCtrlNbr);
        var craftName = await ResolveCraftNameAsync(roster.CraftCtrlNbr);
        return MapToResponse(roster, waName, craftName);
    }

    public override async Task<RosterResponse> CreateAsync(CreateRosterRequest request, ServerCallContext context)
    {
        var roster = Roster.Create(
            request.CraftCtrlNbr,
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            request.RailroadPayrollDepartmentCtrlNbr > 0 ? ControlNumber.Create(request.RailroadPayrollDepartmentCtrlNbr) : null,
            request.RosterName,
            request.RosterPluralName,
            request.RosterNumber);

        await _rosterRepository.AddAsync(roster);
        var waName = await ResolveWorkAreaNameAsync(roster.WorkAreaGroupCtrlNbr);
        var craftName = await ResolveCraftNameAsync(roster.CraftCtrlNbr);
        return MapToResponse(roster, waName, craftName);
    }

    public override async Task<RosterResponse> UpdateAsync(UpdateRosterRequest request, ServerCallContext context)
    {
        var roster = await _rosterRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Roster, with control number {request.CtrlNbr}, was not found."));

        roster.Update(
            request.RosterName,
            request.RosterPluralName,
            request.RosterNumber);

        await _rosterRepository.UpdateAsync(roster);
        var waName = await ResolveWorkAreaNameAsync(roster.WorkAreaGroupCtrlNbr);
        var craftName = await ResolveCraftNameAsync(roster.CraftCtrlNbr);
        return MapToResponse(roster, waName, craftName);
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteRosterRequest request, ServerCallContext context)
    {
        var roster = await _rosterRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Roster, with control number {request.CtrlNbr}, was not found."));

        await _rosterRepository.DeleteAsync(roster.CtrlNbr);

        return new DeleteResponse
        {
            Success = true,
            Messages = { $"Roster {roster.CtrlNbr.Value} deleted." }
        };
    }

    private async Task<string> ResolveWorkAreaNameAsync(ControlNumber workAreaCtrlNbr)
    {
        var group = await _dynamicGroupRepository.GetByCtrlNbrAsync(workAreaCtrlNbr);
        return group?.Name ?? string.Empty;
    }

    private async Task<string> ResolveCraftNameAsync(ControlNumber craftCtrlNbr)
    {
        var craft = await _craftRepository.GetByCtrlNbrAsync(craftCtrlNbr);
        return craft?.CraftName ?? string.Empty;
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
        RosterNumber = roster.RosterNumber
    };
}
