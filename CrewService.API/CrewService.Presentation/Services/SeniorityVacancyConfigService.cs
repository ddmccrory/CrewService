using CrewService.Application.SeniorityOps;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class SeniorityVacancyConfigService(SeniorityStateVacancyConfigService vacancyConfigService)
    : SeniorityStateVacancyConfigSrvc.SeniorityStateVacancyConfigSrvcBase
{
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

    public override async Task<GetVacancyConfigsResponse> GetByRailroadAsync(
        GetVacancyConfigsByRailroadRequest request, ServerCallContext context)
    {
        var configs = await vacancyConfigService.GetByRailroadAsync(
            ControlNumber.Create(request.RailroadCtrlNbr), context.CancellationToken);

        var response = new GetVacancyConfigsResponse();
        foreach (var c in configs)
            response.Configs.Add(MapToResponse(c));
        return response;
    }

    public override async Task<VacancyConfigResponse> UpsertAsync(
        UpsertVacancyConfigRequest request, ServerCallContext context)
    {
        BoardType? targetBoardType = !string.IsNullOrEmpty(request.TargetBoardType)
            ? Enum.Parse<BoardType>(request.TargetBoardType)
            : null;

        var config = await vacancyConfigService.UpsertAsync(
            ControlNumber.Create(request.ParentCtrlNbr),
            ControlNumber.Create(request.RailroadCtrlNbr),
            ControlNumber.Create(request.SeniorityStateCtrlNbr),
            Enum.Parse<VacancyAction>(request.VacancyAction),
            targetBoardType,
            context.CancellationToken);

        return MapToResponse(config);
    }

    public override async Task<DeleteResponse> DeleteAsync(
        DeleteVacancyConfigRequest request, ServerCallContext context)
    {
        try
        {
            await vacancyConfigService.DeleteAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { "Vacancy config deleted." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetVacancyStateTypeDefaultsResponse> GetStateTypeDefaultsByRailroadAsync(
        GetVacancyStateTypeDefaultsByRailroadRequest request, ServerCallContext context)
    {
        var defaults = await vacancyConfigService.GetStateTypeDefaultsByRailroadAsync(
            ControlNumber.Create(request.RailroadCtrlNbr), context.CancellationToken);

        var response = new GetVacancyStateTypeDefaultsResponse();
        foreach (var d in defaults)
            response.Defaults.Add(MapToResponse(d));
        return response;
    }

    public override async Task<VacancyStateTypeDefaultResponse> UpsertStateTypeDefaultAsync(
        UpsertVacancyStateTypeDefaultRequest request, ServerCallContext context)
    {
        var config = await vacancyConfigService.UpsertStateTypeDefaultAsync(
            ControlNumber.Create(request.ParentCtrlNbr),
            ControlNumber.Create(request.RailroadCtrlNbr),
            ToDomain(request.StateType),
            Enum.Parse<VacancyAction>(request.DefaultVacancyAction),
            context.CancellationToken);

        return MapToResponse(config);
    }

    private static VacancyConfigResponse MapToResponse(SeniorityStateVacancyConfig c) => new()
    {
        CtrlNbr = c.CtrlNbr.Value,
        ParentCtrlNbr = c.ParentCtrlNbr.Value,
        RailroadCtrlNbr = c.RailroadCtrlNbr.Value,
        SeniorityStateCtrlNbr = c.SeniorityStateCtrlNbr.Value,
        VacancyAction = c.VacancyAction.ToString(),
        TargetBoardType = c.TargetBoardType?.ToString() ?? string.Empty
    };

    private static VacancyStateTypeDefaultResponse MapToResponse(SeniorityStateTypeVacancyDefault c) => new()
    {
        CtrlNbr = c.CtrlNbr.Value,
        ParentCtrlNbr = c.ParentCtrlNbr.Value,
        RailroadCtrlNbr = c.RailroadCtrlNbr.Value,
        StateType = ToProto(c.StateType),
        DefaultVacancyAction = c.DefaultVacancyAction.ToString()
    };
}
