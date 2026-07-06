using CrewService.Application.SeniorityOps;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class SeniorityStateService(SeniorityStateAppService seniorityStateAppService) : SeniorityStateSrvc.SeniorityStateSrvcBase
{
    public override async Task<GetAllSeniorityStateResponse> GetAllAsync(GetAllSeniorityStateRequest request, ServerCallContext context)
    {
        ControlNumber? parentCtrlNbr = request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null;
        var states = await seniorityStateAppService.GetAllAsync(
            parentCtrlNbr, request.PageNumber, request.PageSize, context.CancellationToken);

        var response = new GetAllSeniorityStateResponse { TotalCount = states.Count };
        foreach (var state in states)
            response.States.Add(MapToResponse(state));
        return response;
    }

    public override async Task<SeniorityStateResponse> GetAsync(GetSeniorityStateRequest request, ServerCallContext context)
    {
        try
        {
            var state = await seniorityStateAppService.GetAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapToResponse(state);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<SeniorityStateResponse> CreateAsync(CreateSeniorityStateRequest request, ServerCallContext context)
    {
        var state = await seniorityStateAppService.CreateAsync(
            request.StateDescription, ToDomain(request.StateType), request.ParentCtrlNbr,
            context.CancellationToken);
        return MapToResponse(state, true, "Seniority state created successfully.");
    }

    public override async Task<SeniorityStateResponse> UpdateAsync(UpdateSeniorityStateRequest request, ServerCallContext context)
    {
        try
        {
            var state = await seniorityStateAppService.UpdateAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.StateDescription, ToDomain(request.StateType),
                context.CancellationToken);
            return MapToResponse(state, true, "Seniority state updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteSeniorityStateRequest request, ServerCallContext context)
    {
        try
        {
            await seniorityStateAppService.DeleteAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { "Seniority state deleted successfully." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static SeniorityStateResponse MapToResponse(SeniorityState state, bool success = false, string? message = null)
    {
        var response = new SeniorityStateResponse
        {
            CtrlNbr = state.CtrlNbr.Value,
            StateDescription = state.StateDescription,
            StateType = ToProto(state.StateType),
            Success = success
        };
        if (message is not null) response.Messages.Add(message);
        return response;
    }

    private static StateType ToDomain(SeniorityStateTypeEnum stateType) => stateType switch
    {
        SeniorityStateTypeEnum.SeniorityStateTypeActive => StateType.Active,
        SeniorityStateTypeEnum.SeniorityStateTypeCutBack => StateType.CutBack,
        SeniorityStateTypeEnum.SeniorityStateTypeInactive => StateType.Inactive,
        SeniorityStateTypeEnum.SeniorityStateTypeOffProperty => StateType.OffProperty,
        _ => throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unsupported seniority state type: {stateType}."))
    };

    private static SeniorityStateTypeEnum ToProto(StateType stateType) => stateType switch
    {
        StateType.Active => SeniorityStateTypeEnum.SeniorityStateTypeActive,
        StateType.CutBack => SeniorityStateTypeEnum.SeniorityStateTypeCutBack,
        StateType.Inactive => SeniorityStateTypeEnum.SeniorityStateTypeInactive,
        StateType.OffProperty => SeniorityStateTypeEnum.SeniorityStateTypeOffProperty,
        _ => SeniorityStateTypeEnum.SeniorityStateTypeUnspecified
    };
}