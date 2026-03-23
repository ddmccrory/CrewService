using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class ParentService(IParentRepository parentRepository, IDynamicGroupRepository dynamicGroupRepository, IOrchestrationUnitOfWorkFactory uowFactory) : ParentSrvc.ParentSrvcBase
{
    private readonly IParentRepository _parentRepository = parentRepository;
    private readonly IDynamicGroupRepository _dynamicGroupRepository = dynamicGroupRepository;
    private readonly IOrchestrationUnitOfWorkFactory _uowFactory = uowFactory;

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

            var railroadGroups = await _dynamicGroupRepository.GetByGroupTypeNameAsync("Railroad", parent.CtrlNbr.Value);

            foreach (var rr in railroadGroups)
            {
                parentResponse.Railroads.Add(new ParentRailroadInfo
                {
                    CtrlNbr = rr.CtrlNbr.Value,
                    RrMark = rr.Code ?? string.Empty,
                    Name = rr.Name
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

        var railroadGroups = await _dynamicGroupRepository.GetByGroupTypeNameAsync("Railroad", parent.CtrlNbr.Value);

        foreach (var rr in railroadGroups)
        {
            response.Railroads.Add(new ParentRailroadInfo
            {
                CtrlNbr = rr.CtrlNbr.Value,
                RrMark = rr.Code ?? string.Empty,
                Name = rr.Name
            });
        }

        return response;
    }

    public override async Task<CreateParentResponse> CreateParentAsync(CreateParentRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.Name))
            throw new ValidationException("Name", "Required");

        var parent = Parent.Create(request.Name);

        await using var uow = await _uowFactory.CreateAsync();
        uow.Parents.Add(parent);

        await uow.CommitAsync();

        return new CreateParentResponse
        {
            CtrlNbr = parent.CtrlNbr.Value,
            Name = parent.Name.Value,
        };
    }

    public override async Task<UpdateParentResponse> UpdateParentAsync(UpdateParentRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        if (string.IsNullOrEmpty(request.Name))
            throw new ValidationException("Name", "Required");

        await using var uow = await _uowFactory.CreateAsync();

        var parent = await uow.Parents.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr)) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Parent, with control number {request.CtrlNbr}, was not found."));

        parent.Update(request.Name);

        uow.Parents.Update(parent);
        await uow.CommitAsync();

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

        await using var uow = await _uowFactory.CreateAsync();

        var parent = await uow.Parents.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr)) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Parent, with control number {request.CtrlNbr}, was not found."));

        uow.Parents.Remove(parent);
        await uow.CommitAsync();

        return new DeleteParentResponse
        {
            CtrlNbr = parent.CtrlNbr.Value,
            Name = parent.Name.Value,
        };
    }
}
