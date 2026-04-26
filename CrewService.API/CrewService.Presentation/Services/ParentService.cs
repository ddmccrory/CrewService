using CrewService.Application.Parents;
using CrewService.Domain.Exceptions;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace CrewService.Presentation.Services;

public class ParentService(ParentAppService parentAppService, IHttpContextAccessor httpContextAccessor) : ParentSrvc.ParentSrvcBase
{
    public override async Task<GetAllParentsResponse> GetAllParentsAsync(GetAllParentsRequest request, ServerCallContext context)
    {
        var response = new GetAllParentsResponse();
        var parents = await parentAppService.GetAllAsync(context.CancellationToken);

        foreach (var parent in parents)
        {
            var parentResponse = new GetParentResponse
            {
                CtrlNbr = parent.CtrlNbr.Value,
                Name = parent.Name.Value
            };
            var railroadGroups = await parentAppService.GetRailroadsAsync(parent.CtrlNbr.Value, context.CancellationToken);
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
        try
        {
            var parent = await parentAppService.GetAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            var response = new GetParentResponse
            {
                CtrlNbr = parent.CtrlNbr.Value,
                Name = parent.Name.Value
            };
            var railroadGroups = await parentAppService.GetRailroadsAsync(parent.CtrlNbr.Value, context.CancellationToken);
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
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<CreateParentResponse> CreateParentAsync(CreateParentRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.Name))
            throw new ValidationException("Name", "Required");

        var parent = await parentAppService.CreateAsync(request.Name, context.CancellationToken);

        httpContextAccessor.HttpContext?.Request.Headers["x-parent-ctrl-nbr"] =
            parent.CtrlNbr.Value.ToString();

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

        try
        {
            var parent = await parentAppService.UpdateAsync(
                ControlNumber.Create(request.CtrlNbr), request.Name, context.CancellationToken);
            return new UpdateParentResponse
            {
                CtrlNbr = parent.CtrlNbr.Value,
                Name = parent.Name.Value,
            };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteParentResponse> DeleteParentAsync(DeleteParentRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        try
        {
            var parent = await parentAppService.DeleteAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteParentResponse
            {
                CtrlNbr = parent.CtrlNbr.Value,
                Name = parent.Name.Value,
            };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }
}
