using CrewService.Application.Boards;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrewService.Presentation.Services.Modules;

public class BoardsService(IServiceProvider serviceProvider) : BoardsSrvc.BoardsSrvcBase
{
    private ILogger<BoardsService> Logger => serviceProvider.GetRequiredService<ILogger<BoardsService>>();
    public override async Task<CascadePolicyResponse> GetCascadePolicy(GetCascadePolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<BoardCascadePolicyService>();
        var policy = await svc.GetByWorkAreaAndCraftAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), ControlNumber.Create(request.CraftCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Cascade policy not found."));
        return MapCascade(policy);
    }

    public override async Task<CascadePolicyResponse> UpsertCascadePolicy(UpsertCascadePolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<BoardCascadePolicyService>();
        var policy = await svc.UpsertAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            ControlNumber.Create(request.CraftCtrlNbr),
            request.CascadeMode,
            request.MaxLevels > 0 ? request.MaxLevels : null,
            request.AuxEnabled,
            request.AuxMaxLevels > 0 ? request.AuxMaxLevels : null,
            string.IsNullOrEmpty(request.SelectionStrategy) ? null : request.SelectionStrategy);
        return MapCascade(policy);
    }

    // ── Required Positions Strategy ──────────────────────────────────────────

    public override async Task<GetAllRequiredPositionsStrategiesResponse> GetAllRequiredPositionsStrategies(
        GetAllRequiredPositionsStrategiesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RequiredPositionsStrategyAppService>();
        var items = await svc.GetAllAsync(context.CancellationToken);
        var response = new GetAllRequiredPositionsStrategiesResponse();
        response.Strategies.AddRange(items.Select(MapStrategy));
        return response;
    }

    public override async Task<RequiredPositionsStrategyResponse> GetRequiredPositionsStrategy(
        GetRequiredPositionsStrategyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RequiredPositionsStrategyAppService>();
        var strategy = await svc.GetAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
        return MapStrategy(strategy);
    }

    public override async Task<RequiredPositionsStrategyResponse> CreateRequiredPositionsStrategy(
        CreateRequiredPositionsStrategyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RequiredPositionsStrategyAppService>();
        var strategy = await svc.CreateAsync(
            request.Code, request.Name, request.Description,
            request.FormulaType, request.ParametersJson,
            context.CancellationToken);
        return MapStrategy(strategy);
    }

    public override async Task<RequiredPositionsStrategyResponse> UpdateRequiredPositionsStrategy(
        UpdateRequiredPositionsStrategyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RequiredPositionsStrategyAppService>();
        var strategy = await svc.UpdateAsync(
            ControlNumber.Create(request.CtrlNbr),
            request.Name, request.Description,
            request.FormulaType, request.ParametersJson,
            context.CancellationToken);
        return MapStrategy(strategy);
    }

    public override async Task<DeleteResponse> DeleteRequiredPositionsStrategy(
        DeleteRequiredPositionsStrategyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RequiredPositionsStrategyAppService>();
        await svc.DeleteAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
        return new DeleteResponse { Success = true };
    }

    public override async Task<CraftStrategyResponse> AssignStrategyToCraft(
        AssignStrategyToCraftRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RequiredPositionsStrategyAppService>();
        var assignment = await svc.AssignToCraftAsync(
            ControlNumber.Create(request.CraftCtrlNbr),
            ControlNumber.Create(request.StrategyCtrlNbr),
            string.IsNullOrWhiteSpace(request.ParametersJson) ? null : request.ParametersJson,
            context.CancellationToken);
        return await BuildCraftStrategyResponseAsync(svc, assignment, context.CancellationToken);
    }

    public override async Task<CraftStrategyResponse> GetCraftStrategy(
        GetCraftStrategyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RequiredPositionsStrategyAppService>();
        var assignment = await svc.GetCraftStrategyAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No strategy assigned to this craft."));
        return await BuildCraftStrategyResponseAsync(svc, assignment, context.CancellationToken);
    }

    public override Task<GetFormulaTypesResponse> GetFormulaTypes(
        GetFormulaTypesRequest request, ServerCallContext context)
    {
        var registry = serviceProvider.GetRequiredService<IEnumerable<IRequiredPositionsFormula>>();
        var response = new GetFormulaTypesResponse();
        response.FormulaTypes.AddRange(registry.Select(f => new FormulaTypeInfo
        {
            FormulaType           = f.FormulaType,
            DisplayName           = f.DisplayName,
            ParametersTemplate    = f.ParametersTemplate,
            ParametersDescription = f.ParametersDescription
        }));
        return Task.FromResult(response);
    }

    public override async Task<GetCraftAssignmentsResponse> GetCraftAssignments(
        GetCraftAssignmentsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RequiredPositionsStrategyAppService>();
        var railroadCtrlNbr = ControlNumber.Create(request.RailroadCtrlNbr);
        var assignments = await svc.GetCraftAssignmentsByRailroadAsync(railroadCtrlNbr, context.CancellationToken);
        var craftNames  = await svc.GetCraftNamesByCtrlNbrsAsync(
            assignments.Select(a => a.CraftCtrlNbr), context.CancellationToken);
        var strategies  = await svc.GetAllAsync(context.CancellationToken);
        var strategyMap = strategies.ToDictionary(s => s.CtrlNbr!.Value);
        var response = new GetCraftAssignmentsResponse();
        foreach (var a in assignments)
        {
            if (!strategyMap.TryGetValue(a.StrategyCtrlNbr.Value, out var strategy)) continue;
            response.Assignments.Add(new CraftStrategyResponse
            {
                CtrlNbr         = a.CtrlNbr!.Value,
                CraftCtrlNbr    = a.CraftCtrlNbr.Value,
                StrategyCtrlNbr = a.StrategyCtrlNbr.Value,
                StrategyCode    = strategy.Code,
                StrategyName    = strategy.Name,
                FormulaType     = strategy.FormulaType,
                CraftName       = craftNames.GetValueOrDefault(a.CraftCtrlNbr.Value, $"#{a.CraftCtrlNbr.Value}"),
                ParametersJson  = a.ParametersJson ?? string.Empty
            });
        }
        return response;
    }

    public override async Task<GetCraftsForAssignmentResponse> GetCraftsForAssignment(
        GetCraftsForAssignmentRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RequiredPositionsStrategyAppService>();
        var available = await svc.GetCraftsForAssignmentAsync(
            ControlNumber.Create(request.RailroadCtrlNbr),
            ControlNumber.Create(request.StrategyCtrlNbr),
            context.CancellationToken);
        var response = new GetCraftsForAssignmentResponse();
        response.Crafts.AddRange(available.Select(c => new AvailableCraftResponse
        {
            CtrlNbr   = c.CtrlNbr.Value,
            CraftName = c.CraftName
        }));
        return response;
    }

    // ── Mapping helpers ──────────────────────────────────────────────────────

    private static CascadePolicyResponse MapCascade(BoardCascadePolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        WorkAreaGroupCtrlNbr = p.WorkAreaGroupCtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        CascadeMode = p.CascadeMode,
        MaxLevels = p.MaxLevels ?? 0,
        AuxEnabled = p.AuxEnabled,
        AuxMaxLevels = p.AuxMaxLevels ?? 0,
        SelectionStrategy = p.SelectionStrategy ?? string.Empty
    };

    private static RequiredPositionsStrategyResponse MapStrategy(RequiredPositionsStrategy s) => new()
    {
        CtrlNbr        = s.CtrlNbr.Value,
        Code           = s.Code,
        Name           = s.Name,
        Description    = s.Description,
        FormulaType    = s.FormulaType,
        ParametersJson = s.ParametersJson
    };

    private async Task<CraftStrategyResponse> BuildCraftStrategyResponseAsync(
        RequiredPositionsStrategyAppService svc,
        CraftRequiredPositionsStrategy assignment,
        CancellationToken ct)
    {
        var strategy   = await svc.GetAsync(assignment.StrategyCtrlNbr, ct);
        var craftNames = await svc.GetCraftNamesByCtrlNbrsAsync([assignment.CraftCtrlNbr], ct);
        return new CraftStrategyResponse
        {
            CtrlNbr         = assignment.CtrlNbr.Value,
            CraftCtrlNbr    = assignment.CraftCtrlNbr.Value,
            StrategyCtrlNbr = assignment.StrategyCtrlNbr.Value,
            StrategyCode    = strategy.Code,
            StrategyName    = strategy.Name,
            FormulaType     = strategy.FormulaType,
            CraftName       = craftNames.GetValueOrDefault(assignment.CraftCtrlNbr.Value, $"#{assignment.CraftCtrlNbr.Value}"),
            ParametersJson  = assignment.ParametersJson ?? string.Empty
        };
    }
}
