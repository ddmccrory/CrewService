using CrewService.Application.UserAccess;
using CrewService.Domain.Exceptions;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class UserParentAssignmentService(UserAccessAppService userAccessAppService)
    : UserParentAssignmentSrvc.UserParentAssignmentSrvcBase
{
    public override async Task<GetAssignmentResponse> GetAssignmentAsync(GetAssignmentRequest request, ServerCallContext context)
    {
        try
        {
            var assignment = await userAccessAppService.GetAssignmentAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapToResponse(assignment);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetAssignmentsByUserResponse> GetAssignmentsByUserAsync(GetAssignmentsByUserRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.UserId))
            throw new ValidationException("UserId", "Required");

        var assignments = await userAccessAppService.GetByUserAsync(request.UserId, context.CancellationToken);
        var response = new GetAssignmentsByUserResponse();
        foreach (var assignment in assignments)
            response.Assignments.Add(MapToResponse(assignment));
        return response;
    }

    public override async Task<GetAssignmentsByParentResponse> GetAssignmentsByParentAsync(GetAssignmentsByParentRequest request, ServerCallContext context)
    {
        if (request.ParentCtrlNbr <= 0)
            throw new ValidationException("ParentCtrlNbr", "Must be greater than 0");

        var assignments = await userAccessAppService.GetByParentAsync(request.ParentCtrlNbr, context.CancellationToken);
        var response = new GetAssignmentsByParentResponse();
        foreach (var assignment in assignments)
            response.Assignments.Add(MapToResponse(assignment));
        return response;
    }

    public override async Task<CreateAssignmentResponse> CreateAssignmentAsync(CreateAssignmentRequest request, ServerCallContext context)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(request.UserId)) errors.Add("UserId", ["Required"]);
        if (request.ParentCtrlNbr <= 0) errors.Add("ParentCtrlNbr", ["Must be greater than 0"]);
        if (string.IsNullOrEmpty(request.Role)) errors.Add("Role", ["Required"]);
        if (errors.Count > 0) throw new ValidationException(errors);

        try
        {
            var assignment = await userAccessAppService.CreateAssignmentAsync(
                request.UserId, ControlNumber.Create(request.ParentCtrlNbr), request.Role,
                context.CancellationToken);
            return new CreateAssignmentResponse
            {
                CtrlNbr = assignment.CtrlNbr.Value,
                UserId = assignment.UserId,
                ParentCtrlNbr = assignment.ParentCtrlNbr.Value,
                Role = assignment.Role
            };
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Domain.Exceptions.ConflictException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
    }

    public override async Task<UpdateAssignmentRoleResponse> UpdateAssignmentRoleAsync(UpdateAssignmentRoleRequest request, ServerCallContext context)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.CtrlNbr <= 0) errors.Add("CtrlNbr", ["Must be greater than 0"]);
        if (string.IsNullOrEmpty(request.Role)) errors.Add("Role", ["Required"]);
        if (errors.Count > 0) throw new ValidationException(errors);

        try
        {
            var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
            var assignment = await userAccessAppService.UpdateAssignmentRoleAsync(
                ControlNumber.Create(request.CtrlNbr), request.Role, railroadCtrlNbr, context.CancellationToken);
            return new UpdateAssignmentRoleResponse
            {
                CtrlNbr = assignment.CtrlNbr.Value,
                UserId = assignment.UserId,
                ParentCtrlNbr = assignment.ParentCtrlNbr.Value,
                Role = assignment.Role,
                RailroadCtrlNbr = assignment.RailroadCtrlNbr?.Value ?? 0
            };
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteAssignmentResponse> DeleteAssignmentAsync(DeleteAssignmentRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        try
        {
            var assignment = await userAccessAppService.DeleteAssignmentAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteAssignmentResponse
            {
                CtrlNbr = assignment.CtrlNbr.Value,
                UserId = assignment.UserId,
                ParentCtrlNbr = assignment.ParentCtrlNbr.Value,
                Role = assignment.Role,
                RailroadCtrlNbr = assignment.RailroadCtrlNbr?.Value ?? 0
            };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static GetAssignmentResponse MapToResponse(Domain.Models.UserAccess.UserParentAssignment assignment)
    {
        return new GetAssignmentResponse
        {
            CtrlNbr = assignment.CtrlNbr.Value,
            UserId = assignment.UserId,
            ParentCtrlNbr = assignment.ParentCtrlNbr.Value,
            Role = assignment.Role,
            RailroadCtrlNbr = assignment.RailroadCtrlNbr?.Value ?? 0
        };
    }
}
