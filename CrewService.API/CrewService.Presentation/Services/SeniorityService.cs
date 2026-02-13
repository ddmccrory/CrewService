using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class SeniorityService(ISeniorityRepository seniorityRepository) : SenioritySrvc.SenioritySrvcBase
{
    private readonly ISeniorityRepository _seniorityRepository = seniorityRepository;

    public override async Task<GetAllSeniorityResponse> GetAllAsync(GetAllSeniorityRequest request, ServerCallContext context)
    {
        var response = new GetAllSeniorityResponse();
        var seniorities = await _seniorityRepository.GetAllAsync();

        foreach (var seniority in seniorities)
        {
            response.Seniority.Add(new SeniorityResponse
            {
                CtrlNbr = seniority.CtrlNbr.Value,
                RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
                RailroadPoolEmployeeCtrlNbr = seniority.RailroadPoolEmployeeCtrlNbr.Value,
                LastActiveRoster = seniority.LastActiveRoster,
                RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
                Rank = seniority.Rank,
                StateId = seniority.StateID,
                CanTrain = seniority.CanTrain
            });
        }

        return await Task.FromResult(response);
    }

    public override async Task<SeniorityResponse> GetAsync(GetSeniorityRequest request, ServerCallContext context)
    {
        var seniority = await _seniorityRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr));

        return seniority is null
            ? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority, with control number {request.CtrlNbr}, was not found."))
            : await Task.FromResult(new SeniorityResponse
            {
                CtrlNbr = seniority.CtrlNbr.Value,
                RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
                RailroadPoolEmployeeCtrlNbr = seniority.RailroadPoolEmployeeCtrlNbr.Value,
                LastActiveRoster = seniority.LastActiveRoster,
                RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
                Rank = seniority.Rank,
                StateId = seniority.StateID,
                CanTrain = seniority.CanTrain
            });
    }

    public override async Task<SeniorityResponse> CreateAsync(CreateSeniorityRequest request, ServerCallContext context)
    {
        var seniority = Seniority.Create(
            request.RosterCtrlNbr,
            request.RailroadPoolEmployeeCtrlNbr,
            request.LastActiveRoster,
            DateTime.Parse(request.RosterDate),
            request.Rank,
            request.StateId,
            request.CanTrain);

        await _seniorityRepository.AddAsync(seniority);

        return await Task.FromResult(new SeniorityResponse
        {
            CtrlNbr = seniority.CtrlNbr.Value,
            RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
            RailroadPoolEmployeeCtrlNbr = seniority.RailroadPoolEmployeeCtrlNbr.Value,
            LastActiveRoster = seniority.LastActiveRoster,
            RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
            Rank = seniority.Rank,
            StateId = seniority.StateID,
            CanTrain = seniority.CanTrain
        });
    }

    public override async Task<SeniorityResponse> UpdateAsync(UpdateSeniorityRequest request, ServerCallContext context)
    {
        var seniority = await _seniorityRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority, with control number {request.CtrlNbr}, was not found."));

        seniority.Update(
            request.LastActiveRoster,
            DateTime.Parse(request.RosterDate),
            request.Rank,
            request.StateId,
            request.CanTrain);

        await _seniorityRepository.UpdateAsync(seniority);

        return await Task.FromResult(new SeniorityResponse
        {
            CtrlNbr = seniority.CtrlNbr.Value,
            RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
            RailroadPoolEmployeeCtrlNbr = seniority.RailroadPoolEmployeeCtrlNbr.Value,
            LastActiveRoster = seniority.LastActiveRoster,
            RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
            Rank = seniority.Rank,
            StateId = seniority.StateID,
            CanTrain = seniority.CanTrain
        });
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteSeniorityRequest request, ServerCallContext context)
    {
        var seniority = await _seniorityRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority, with control number {request.CtrlNbr}, was not found."));

        await _seniorityRepository.DeleteAsync(seniority.CtrlNbr);

        return await Task.FromResult(new DeleteResponse
        {
            Success = true,
            Messages = { $"Seniority {seniority.CtrlNbr.Value} deleted." }
        });
    }
}