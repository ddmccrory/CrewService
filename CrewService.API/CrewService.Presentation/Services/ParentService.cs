using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Parents;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class ParentService(IParentRepository parentRepository) : ParentSrvc.ParentSrvcBase
{
    private readonly IParentRepository _parentRepository = parentRepository;

    public override async Task<GetAllParentsResponse> GetAllParentsAsync(GetAllParentsRequest request, ServerCallContext context)
    {
        var response = new GetAllParentsResponse();
        var parents = await _parentRepository.GetAllAsync();

        foreach (var parent in parents)
        {
            var parentResponse = new GetParentResponse
            {
                CtrlNbr = parent.CtrlNbr.Value,
                Name = parent.Name.Value
            };

            foreach (var railroad in parent.Railroads)
            {
                parentResponse.Railroads.Add(new GetRailroadResponse
                {
                    CtrlNbr = railroad.CtrlNbr.Value,
                    RrMark = railroad.RailroadMark,
                    Name = railroad.Name.Value
                });
            }

            response.Parent.Add(parentResponse);
        }

        return await Task.FromResult(response);
    }

    public override async Task<GetParentResponse> GetParentAsync(GetParentRequest request, ServerCallContext context)
    {
        var parent = await _parentRepository.GetByCtrlNbrAsync(request.CtrlNbr) ?? 
            throw new RpcException(new Status(StatusCode.NotFound, $"Parent, with control number {request.CtrlNbr}, was not found."));

        var response = new GetParentResponse
        {
            CtrlNbr = parent.CtrlNbr.Value,
            Name = parent.Name.Value
        };

        foreach (var railroad in parent.Railroads)
        {
            response.Railroads.Add(new GetRailroadResponse
            {
                CtrlNbr = railroad.CtrlNbr.Value,
                RrMark = railroad.RailroadMark,
                Name = railroad.Name.Value
            });
        }

        return await Task.FromResult(response);
    }

    public override async Task<CreateParentResponse> CreateParentAsync(CreateParentRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid parent name."));

        var parent = Parent.Create(request.Name);

        _parentRepository.Add(parent);

        return await Task.FromResult(new CreateParentResponse
        {
            CtrlNbr = parent.CtrlNbr.Value,
            Name = parent.Name.Value,
        });
    }

    public override async Task<UpdateParentResponse> UpdateParentAsync(UpdateParentRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid parent control number."));

        if (string.IsNullOrEmpty(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid parent name."));

        var parent = await _parentRepository.GetByCtrlNbrAsync(request.CtrlNbr) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Parent, with control number {request.CtrlNbr}, was not found."));

        parent.Update(request.Name);

        _parentRepository.Update(parent);

        return await Task.FromResult(new UpdateParentResponse
        {
            CtrlNbr = parent.CtrlNbr.Value,
            Name = parent.Name.Value,
        });
    }

    public override async Task<DeleteParentResponse> DeleteParentAsync(DeleteParentRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid parent control number."));

        var parent = await _parentRepository.GetByCtrlNbrAsync(request.CtrlNbr) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Parent, with control number {request.CtrlNbr}, was not found."));

        _parentRepository.Remove(parent);

        return await Task.FromResult(new DeleteParentResponse
        {
            CtrlNbr = parent.CtrlNbr.Value,
            Name = parent.Name.Value,
        });
    }
}
