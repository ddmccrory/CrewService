using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class RosterService(IRosterRepository rosterRepository) : RosterSrvc.RosterSrvcBase
{
    private readonly IRosterRepository _rosterRepository = rosterRepository;

    public override async Task<GetAllRosterResponse> GetAllAsync(GetAllRosterRequest request, ServerCallContext context)
    {
        var response = new GetAllRosterResponse();

        var rosters = request.CraftCtrlNbr > 0
            ? await _rosterRepository.GetByCraftCtrlNbrAsync(ControlNumber.Create(request.CraftCtrlNbr))
            : await _rosterRepository.GetAllAsync();

        foreach (var roster in rosters)
        {
            response.Rosters.Add(MapToResponse(roster));
        }

        return response;
    }

    public override async Task<RosterResponse> GetAsync(GetRosterRequest request, ServerCallContext context)
    {
        var roster = await _rosterRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr));
        if (roster is null) return new RosterResponse();
        return MapToResponse(roster);
    }

    public override async Task<RosterResponse> CreateAsync(CreateRosterRequest request, ServerCallContext context)
    {
        var roster = Roster.Create(
            request.CraftCtrlNbr,
            request.RailroadPayrollDepartmentCtrlNbr > 0 ? ControlNumber.Create(request.RailroadPayrollDepartmentCtrlNbr) : null,
            request.RosterName,
            request.RosterPluralName,
            request.RosterNumber);

        await _rosterRepository.AddAsync(roster);
        return MapToResponse(roster);
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
        return MapToResponse(roster);
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

    private static RosterResponse MapToResponse(Roster roster) => new()
    {
        CtrlNbr = roster.CtrlNbr.Value,
        CraftCtrlNbr = roster.CraftCtrlNbr.Value,
        RailroadPayrollDepartmentCtrlNbr = roster.RailroadPayrollDepartmentCtrlNbr?.Value ?? 0,
        RosterName = roster.RosterName,
        RosterPluralName = roster.RosterPluralName,
        RosterNumber = roster.RosterNumber
    };
}
