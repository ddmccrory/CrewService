using CrewService.Domain.Modules.Crews;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class CrewsService(
    ICrewRepository crewRepository,
    ICrewPositionRepository crewPositionRepository,
    ICrewIncumbencyRepository incumbencyRepository,
    ICrewAttachmentTemplateRepository attachmentRepo,
    IReliefCoverageRuleRepository reliefRepo) : CrewsSrvc.CrewsSrvcBase
{
    public override async Task<GetAllCrewsResponse> GetAllCrews(GetAllCrewsRequest request, ServerCallContext context)
    {
        var crews = string.IsNullOrEmpty(request.CrewType)
            ? await crewRepository.GetByHomeGroupAsync(ControlNumber.Create(request.HomeGroupCtrlNbr))
            : await crewRepository.GetByTypeAsync(request.CrewType);
        var response = new GetAllCrewsResponse { TotalCount = crews.Count };
        foreach (var c in crews)
            response.Crews.Add(MapCrew(c));
        return response;
    }

    public override async Task<CrewResponse> GetCrew(GetCrewRequest request, ServerCallContext context)
    {
        var crew = await crewRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Crew {request.CtrlNbr} not found."));
        return MapCrew(crew);
    }

    public override async Task<CrewResponse> CreateCrew(CreateCrewRequest request, ServerCallContext context)
    {
        var crew = Crew.Create(request.CrewType, request.HomeGroupCtrlNbr, request.Name, request.IsActive);
        await crewRepository.AddAsync(crew);
        return MapCrew(crew);
    }

    public override async Task<CrewResponse> UpdateCrew(UpdateCrewRequest request, ServerCallContext context)
    {
        var crew = await crewRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Crew {request.CtrlNbr} not found."));
        crew.Update(request.Name, request.IsActive);
        await crewRepository.UpdateAsync(crew);
        return MapCrew(crew);
    }

    public override async Task<DeleteResponse> DeleteCrew(DeleteCrewRequest request, ServerCallContext context)
    {
        await crewRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    public override async Task<GetCrewPositionsResponse> GetCrewPositions(GetCrewPositionsRequest request, ServerCallContext context)
    {
        var positions = await crewPositionRepository.GetByCrewAsync(ControlNumber.Create(request.CrewCtrlNbr));
        var response = new GetCrewPositionsResponse { TotalCount = positions.Count };
        foreach (var p in positions)
            response.Positions.Add(new CrewPositionResponse
            {
                CtrlNbr = p.CtrlNbr.Value,
                CrewCtrlNbr = p.CrewCtrlNbr.Value,
                PositionRoleCtrlNbr = p.PositionRoleCtrlNbr.Value,
                DisplayOrder = p.DisplayOrder
            });
        return response;
    }

    public override async Task<CrewPositionResponse> CreateCrewPosition(CreateCrewPositionRequest request, ServerCallContext context)
    {
        var position = CrewPosition.Create(request.CrewCtrlNbr, request.PositionRoleCtrlNbr, request.DisplayOrder);
        await crewPositionRepository.AddAsync(position);
        return new CrewPositionResponse
        {
            CtrlNbr = position.CtrlNbr.Value,
            CrewCtrlNbr = position.CrewCtrlNbr.Value,
            PositionRoleCtrlNbr = position.PositionRoleCtrlNbr.Value,
            DisplayOrder = position.DisplayOrder
        };
    }

    private static CrewResponse MapCrew(Crew c) => new()
    {
        CtrlNbr = c.CtrlNbr.Value,
        CrewType = c.CrewType,
        HomeGroupCtrlNbr = c.HomeGroupCtrlNbr.Value,
        Name = c.Name,
        IsActive = c.IsActive
    };

    // Incumbencies
    public override async Task<GetCrewIncumbenciesResponse> GetCrewIncumbencies(GetCrewIncumbenciesRequest request, ServerCallContext context)
    {
        var items = await incumbencyRepository.GetByCrewPositionAsync(ControlNumber.Create(request.CrewPositionCtrlNbr));
        var response = new GetCrewIncumbenciesResponse { TotalCount = items.Count };
        foreach (var i in items) response.Incumbencies.Add(MapIncumbency(i));
        return response;
    }

    public override async Task<CrewIncumbencyResponse> CreateCrewIncumbency(CreateCrewIncumbencyRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var incumbency = CrewIncumbency.Create(request.CrewPositionCtrlNbr, request.EmployeeCtrlNbr, startUtc, endUtc);
        await incumbencyRepository.AddAsync(incumbency);
        return MapIncumbency(incumbency);
    }

    private static CrewIncumbencyResponse MapIncumbency(CrewIncumbency i) => new()
    {
        CtrlNbr = i.CtrlNbr.Value,
        CrewPositionCtrlNbr = i.CrewPositionCtrlNbr.Value,
        EmployeeCtrlNbr = i.EmployeeCtrlNbr.Value,
        StartUtc = i.StartUtc.ToString("O"),
        EndUtc = i.EndUtc?.ToString("O") ?? string.Empty
    };

    // Attachment Templates
    public override async Task<GetCrewAttachmentTemplatesResponse> GetCrewAttachmentTemplates(GetCrewAttachmentTemplatesRequest request, ServerCallContext context)
    {
        var items = await attachmentRepo.GetByAssignmentGroupAsync(ControlNumber.Create(request.CrewCtrlNbr));
        var response = new GetCrewAttachmentTemplatesResponse { TotalCount = items.Count };
        foreach (var t in items) response.Templates.Add(MapAttachmentTemplate(t));
        return response;
    }

    public override async Task<CrewAttachmentTemplateResponse> CreateCrewAttachmentTemplate(CreateCrewAttachmentTemplateRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var attachment = CrewAttachmentTemplate.Create(request.AssignmentGroupCtrlNbr, request.CrewCtrlNbr, startUtc, endUtc);
        await attachmentRepo.AddAsync(attachment);
        return MapAttachmentTemplate(attachment);
    }

    private static CrewAttachmentTemplateResponse MapAttachmentTemplate(CrewAttachmentTemplate t) => new()
    {
        CtrlNbr = t.CtrlNbr.Value,
        AssignmentGroupCtrlNbr = t.AssignmentGroupCtrlNbr.Value,
        CrewCtrlNbr = t.CrewCtrlNbr.Value,
        StartUtc = t.StartUtc.ToString("O"),
        EndUtc = t.EndUtc?.ToString("O") ?? string.Empty
    };

    // Relief Coverage Rules
    public override async Task<GetReliefCoverageRulesResponse> GetReliefCoverageRules(GetReliefCoverageRulesRequest request, ServerCallContext context)
    {
        var items = await reliefRepo.GetByReliefCrewAsync(ControlNumber.Create(request.ReliefCrewCtrlNbr));
        var response = new GetReliefCoverageRulesResponse { TotalCount = items.Count };
        foreach (var r in items) response.Rules.Add(MapReliefRule(r));
        return response;
    }

    public override async Task<ReliefCoverageRuleResponse> CreateReliefCoverageRule(CreateReliefCoverageRuleRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var rule = ReliefCoverageRule.Create(request.ReliefCrewCtrlNbr, request.AssignmentGroupCtrlNbr, request.DaysOfWeekMask, startUtc, endUtc);
        await reliefRepo.AddAsync(rule);
        return MapReliefRule(rule);
    }

    private static ReliefCoverageRuleResponse MapReliefRule(ReliefCoverageRule r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        ReliefCrewCtrlNbr = r.ReliefCrewCtrlNbr.Value,
        AssignmentGroupCtrlNbr = r.AssignmentGroupCtrlNbr.Value,
        DaysOfWeekMask = r.DaysOfWeekMask,
        StartUtc = r.StartUtc.ToString("O"),
        EndUtc = r.EndUtc?.ToString("O") ?? string.Empty
    };
}
