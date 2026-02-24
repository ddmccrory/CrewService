using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
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

        return response;
    }

    public override async Task<GetParentResponse> GetParentAsync(GetParentRequest request, ServerCallContext context)
    {
        var parent = await _parentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr)) ??
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

        return response;
    }

    public override async Task<CreateParentResponse> CreateParentAsync(CreateParentRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.Name))
            throw new ValidationException("Name", "Required");

        var parent = Parent.Create(request.Name);

        _parentRepository.Add(parent);

        return new CreateParentResponse
        {
            CtrlNbr = parent.CtrlNbr.Value,
            Name = parent.Name.Value,
        };
    }

    public override async Task<UpdateParentResponse> UpdateParentAsync(UpdateParentRequest request, ServerCallContext context)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CtrlNbr <= 0)
            errors.Add("CtrlNbr", ["Must be greater than 0"]);

        if (string.IsNullOrEmpty(request.Name))
            errors.Add("Name", ["Required"]);

        var parent = await _parentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr)) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Parent, with control number {request.CtrlNbr}, was not found."));

        parent.Update(request.Name);

        _parentRepository.Update(parent);

        return new UpdateParentResponse
        {
            CtrlNbr = parent.CtrlNbr.Value,
            Name = parent.Name.Value,
        };
    }

    public override async Task<DeleteParentResponse> DeleteParentAsync(DeleteParentRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        var parent = await _parentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr)) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Parent, with control number {request.CtrlNbr}, was not found."));

        _parentRepository.Remove(parent);

        return new DeleteParentResponse
        {
            CtrlNbr = parent.CtrlNbr.Value,
            Name = parent.Name.Value,
        };
    }
}
