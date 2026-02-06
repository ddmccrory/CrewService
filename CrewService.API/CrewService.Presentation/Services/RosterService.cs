using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class RosterService(IRosterRepository rosterRepository) : RosterSrvc.RosterSrvcBase
{
    private readonly IRosterRepository _rosterRepository = rosterRepository;

    public override async Task<GetAllRosterResponse> GetAllAsync(GetAllRosterRequest request, ServerCallContext context)
    {
        var response = new GetAllRosterResponse();
        var rosters = await _rosterRepository.GetAllAsync();

        foreach (var roster in rosters)
        {
            response.Rosters.Add(new RosterResponse
            {
                CtrlNbr = roster.CtrlNbr?.Value ?? 0,
                CraftCtrlNbr = roster.CraftCtrlNbr?.Value ?? 0,
                RailroadPayrollDepartmentCtrlNbr = roster.RailroadPayrollDepartmentCtrlNbr?.Value ?? 0,
                RosterName = roster.RosterName,
                RosterPluralName = roster.RosterPluralName,
                RosterNumber = roster.RosterNumber,
                Training = roster.Training,
                ExtraBoard = roster.ExtraBoard,
                OvertimeBoard = roster.OvertimeBoard
            });
        }

        return await Task.FromResult(response);
    }

    public override async Task<RosterResponse> GetAsync(GetRosterRequest request, ServerCallContext context)
    {
        var roster = await _rosterRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr));

        return await Task.FromResult(new RosterResponse
        {
            CtrlNbr = roster?.CtrlNbr?.Value ?? 0,
            CraftCtrlNbr = roster?.CraftCtrlNbr?.Value ?? 0,
            RailroadPayrollDepartmentCtrlNbr = roster?.RailroadPayrollDepartmentCtrlNbr?.Value ?? 0,
            RosterName = roster?.RosterName ?? string.Empty,
            RosterPluralName = roster?.RosterPluralName ?? string.Empty,
            RosterNumber = roster?.RosterNumber ?? 0,
            Training = roster?.Training ?? false,
            ExtraBoard = roster?.ExtraBoard ?? false,
            OvertimeBoard = roster?.OvertimeBoard ?? false
        });
    }

    public override async Task<RosterResponse> CreateAsync(CreateRosterRequest request, ServerCallContext context)
    {
        var roster = Roster.Create(
            request.CraftCtrlNbr,
            request.RailroadPayrollDepartmentCtrlNbr,
            request.RosterName,
            request.RosterPluralName,
            request.RosterNumber,
            request.Training,
            request.ExtraBoard,
            request.OvertimeBoard);

        await _rosterRepository.AddAsync(roster);

        return await Task.FromResult(new RosterResponse
        {
            CtrlNbr = roster.CtrlNbr?.Value ?? 0,
            CraftCtrlNbr = roster.CraftCtrlNbr?.Value ?? 0,
            RailroadPayrollDepartmentCtrlNbr = roster.RailroadPayrollDepartmentCtrlNbr?.Value ?? 0,
            RosterName = roster.RosterName,
            RosterPluralName = roster.RosterPluralName,
            RosterNumber = roster.RosterNumber,
            Training = roster.Training,
            ExtraBoard = roster.ExtraBoard,
            OvertimeBoard = roster.OvertimeBoard
        });
    }

    public override async Task<RosterResponse> UpdateAsync(UpdateRosterRequest request, ServerCallContext context)
    {
        var roster = await _rosterRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Roster, with control number {request.CtrlNbr}, was not found."));

        roster.Update(
            request.RosterName,
            request.RosterPluralName,
            request.RosterNumber,
            request.Training,
            request.ExtraBoard,
            request.OvertimeBoard);

        await _rosterRepository.UpdateAsync(roster);

        return await Task.FromResult(new RosterResponse
        {
            CtrlNbr = roster.CtrlNbr?.Value ?? 0,
            CraftCtrlNbr = roster.CraftCtrlNbr?.Value ?? 0,
            RailroadPayrollDepartmentCtrlNbr = roster.RailroadPayrollDepartmentCtrlNbr?.Value ?? 0,
            RosterName = roster.RosterName,
            RosterPluralName = roster.RosterPluralName,
            RosterNumber = roster.RosterNumber,
            Training = roster.Training,
            ExtraBoard = roster.ExtraBoard,
            OvertimeBoard = roster.OvertimeBoard
        });
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteRosterRequest request, ServerCallContext context)
    {
        var roster = await _rosterRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Roster, with control number {request.CtrlNbr}, was not found."));

        await _rosterRepository.DeleteAsync(roster.CtrlNbr);

        return await Task.FromResult(new DeleteResponse
        {
            Success = true,
            Messages = { $"Roster {roster.CtrlNbr?.Value ?? 0} deleted." }
        });
    }
}