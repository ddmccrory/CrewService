using CrewService.Domain.Modules.Crews;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class CrewsService(
    ICrewRepository crewRepository,
    ICrewPositionRepository crewPositionRepository) : CrewsSrvc.CrewsSrvcBase
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
}
