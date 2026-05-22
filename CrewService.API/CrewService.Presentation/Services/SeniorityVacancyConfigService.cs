using CrewService.Application.SeniorityOps;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class SeniorityVacancyConfigService(SeniorityStateVacancyConfigService vacancyConfigService)
    : SeniorityStateVacancyConfigSrvc.SeniorityStateVacancyConfigSrvcBase
{
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

    private static VacancyConfigResponse MapToResponse(SeniorityStateVacancyConfig c) => new()
    {
        CtrlNbr = c.CtrlNbr.Value,
        ParentCtrlNbr = c.ParentCtrlNbr.Value,
        RailroadCtrlNbr = c.RailroadCtrlNbr.Value,
        SeniorityStateCtrlNbr = c.SeniorityStateCtrlNbr.Value,
        VacancyAction = c.VacancyAction.ToString(),
        TargetBoardType = c.TargetBoardType?.ToString() ?? string.Empty
    };
}
