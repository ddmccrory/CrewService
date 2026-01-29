using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Repositories;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class SeniorityStateService(ISeniorityStateRepository repository) : SeniorityStateSrvc.SeniorityStateSrvcBase
{
    private readonly ISeniorityStateRepository _repository = repository;

    public override async Task<GetAllSeniorityStateResponse> GetAllAsync(GetAllSeniorityStateRequest request, ServerCallContext context)
    {
        var states = request.PageSize > 0
            ? await _repository.GetAllAsync(request.PageNumber, request.PageSize)
            : await _repository.GetAllAsync();

        var response = new GetAllSeniorityStateResponse { TotalCount = states.Count };

        foreach (var state in states)
        {
            response.States.Add(MapToResponse(state));
        }

        return response;
    }

    public override async Task<SeniorityStateResponse> GetAsync(GetSeniorityStateRequest request, ServerCallContext context)
    {
        var state = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority state with control number {request.CtrlNbr} was not found."));

        return MapToResponse(state);
    }

    public override async Task<SeniorityStateResponse> CreateAsync(CreateSeniorityStateRequest request, ServerCallContext context)
    {
        var state = SeniorityState.Create(
            request.StateDescription,
            request.Active,
            request.CutBack,
            request.Inactive);

        _repository.Add(state);

        return MapToResponse(state, true, "Seniority state created successfully.");
    }

    public override async Task<SeniorityStateResponse> UpdateAsync(UpdateSeniorityStateRequest request, ServerCallContext context)
    {
        var state = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority state with control number {request.CtrlNbr} was not found."));

        state.Update(request.StateDescription, request.Active, request.CutBack, request.Inactive);

        _repository.Update(state);

        return MapToResponse(state, true, "Seniority state updated successfully.");
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteSeniorityStateRequest request, ServerCallContext context)
    {
        var state = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority state with control number {request.CtrlNbr} was not found."));

        _repository.Remove(state);

        return new DeleteResponse { Success = true, Messages = { "Seniority state deleted successfully." } };
    }

    private static SeniorityStateResponse MapToResponse(SeniorityState state, bool success = false, string? message = null)
    {
        var response = new SeniorityStateResponse
        {
            CtrlNbr = state.CtrlNbr.Value,
            StateDescription = state.StateDescription,
            Active = state.Active,
            CutBack = state.CutBack,
            Inactive = state.Inactive,
            Success = success
        };

        if (message is not null) response.Messages.Add(message);

        return response;
    }
}