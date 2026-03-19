using CrewService.Domain.Exceptions;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class UserParentAssignmentService(IUserParentAssignmentRepository assignmentRepository)
    : UserParentAssignmentSrvc.UserParentAssignmentSrvcBase
{
    private readonly IUserParentAssignmentRepository _assignmentRepository = assignmentRepository;

    public override async Task<GetAssignmentResponse> GetAssignmentAsync(GetAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await _assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr)) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Assignment with control number {request.CtrlNbr} was not found."));

        return MapToResponse(assignment);
    }

    public override async Task<GetAssignmentsByUserResponse> GetAssignmentsByUserAsync(GetAssignmentsByUserRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.UserId))
            throw new ValidationException("UserId", "Required");

        var assignments = await _assignmentRepository.GetByUserIdAsync(request.UserId);

        var response = new GetAssignmentsByUserResponse();
        foreach (var assignment in assignments)
            response.Assignments.Add(MapToResponse(assignment));

        return response;
    }

    public override async Task<GetAssignmentsByParentResponse> GetAssignmentsByParentAsync(GetAssignmentsByParentRequest request, ServerCallContext context)
    {
        if (request.ParentCtrlNbr <= 0)
            throw new ValidationException("ParentCtrlNbr", "Must be greater than 0");

        var assignments = await _assignmentRepository.GetByParentCtrlNbrAsync(request.ParentCtrlNbr);

        var response = new GetAssignmentsByParentResponse();
        foreach (var assignment in assignments)
            response.Assignments.Add(MapToResponse(assignment));

        return response;
    }

    public override async Task<CreateAssignmentResponse> CreateAssignmentAsync(CreateAssignmentRequest request, ServerCallContext context)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(request.UserId))
            errors.Add("UserId", ["Required"]);

        if (request.ParentCtrlNbr <= 0)
            errors.Add("ParentCtrlNbr", ["Must be greater than 0"]);

        if (string.IsNullOrEmpty(request.Role))
            errors.Add("Role", ["Required"]);
        else if (!Roles.AllPerParentRoles.Contains(request.Role))
            errors.Add("Role", [$"Unknown role '{request.Role}'. Valid roles: {string.Join(", ", Roles.AllPerParentRoles)}"]);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var existing = await _assignmentRepository.GetByUserAndParentAsync(request.UserId, request.ParentCtrlNbr);
        if (existing is not null)
            throw new ConflictException(nameof(UserParentAssignment), $"User {request.UserId} is already assigned to parent {request.ParentCtrlNbr}.");

        var assignment = UserParentAssignment.Create(request.UserId, request.ParentCtrlNbr, request.Role);

        await _assignmentRepository.AddAsync(assignment);

        return new CreateAssignmentResponse
        {
            CtrlNbr = assignment.CtrlNbr.Value,
            UserId = assignment.UserId,
            ParentCtrlNbr = assignment.ParentCtrlNbr.Value,
            Role = assignment.Role
        };
    }

    public override async Task<UpdateAssignmentRoleResponse> UpdateAssignmentRoleAsync(UpdateAssignmentRoleRequest request, ServerCallContext context)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CtrlNbr <= 0)
            errors.Add("CtrlNbr", ["Must be greater than 0"]);

        if (string.IsNullOrEmpty(request.Role))
            errors.Add("Role", ["Required"]);
        else if (!Roles.AllPerParentRoles.Contains(request.Role))
            errors.Add("Role", [$"Unknown role '{request.Role}'. Valid roles: {string.Join(", ", Roles.AllPerParentRoles)}"]);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var assignment = await _assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr)) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Assignment with control number {request.CtrlNbr} was not found."));

        var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        assignment.UpdateRole(request.Role, railroadCtrlNbr);

        await _assignmentRepository.UpdateAsync(assignment);

        return new UpdateAssignmentRoleResponse
        {
            CtrlNbr = assignment.CtrlNbr.Value,
            UserId = assignment.UserId,
            ParentCtrlNbr = assignment.ParentCtrlNbr.Value,
            Role = assignment.Role,
            RailroadCtrlNbr = assignment.RailroadCtrlNbr?.Value ?? 0
        };
    }

    public override async Task<DeleteAssignmentResponse> DeleteAssignmentAsync(DeleteAssignmentRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        var assignment = await _assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr)) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Assignment with control number {request.CtrlNbr} was not found."));

        assignment.Delete();
        await _assignmentRepository.DeleteAsync(assignment.CtrlNbr);

        return new DeleteAssignmentResponse
        {
            CtrlNbr = assignment.CtrlNbr.Value,
            UserId = assignment.UserId,
            ParentCtrlNbr = assignment.ParentCtrlNbr.Value,
            Role = assignment.Role,
            RailroadCtrlNbr = assignment.RailroadCtrlNbr?.Value ?? 0
        };
    }

    private static GetAssignmentResponse MapToResponse(UserParentAssignment assignment)
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
