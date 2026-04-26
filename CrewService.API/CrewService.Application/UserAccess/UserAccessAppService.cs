using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.UserAccess;

public sealed class UserAccessAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<UserParentAssignment> GetAssignmentAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.UserParentAssignments.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Assignment {ctrlNbr.Value} not found.");
    }

    public async Task<List<UserParentAssignment>> GetByUserAsync(string userId, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.UserParentAssignments.GetByUserIdAsync(userId);
    }

    public async Task<List<UserParentAssignment>> GetByParentAsync(long parentCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.UserParentAssignments.GetByParentCtrlNbrAsync(ControlNumber.Create(parentCtrlNbr));
    }

    public async Task<UserParentAssignment> CreateAssignmentAsync(
        string userId, ControlNumber parentCtrlNbr, string role,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        if (role == Roles.SystemAdmin)
            throw new ValidationException("Role", "SystemAdmin cannot be assigned per-parent");

        var roleEntity = await uow.Roles.GetByNameAsync(role)
            ?? throw new ValidationException("Role", $"Unknown role '{role}'");

        var existingAssignments = await uow.UserParentAssignments.GetByUserAndParentAsync(userId, parentCtrlNbr);
        if (existingAssignments.Count > 0)
            throw new ConflictException(nameof(UserParentAssignment),
                $"User {userId} is already assigned to parent {parentCtrlNbr.Value}.");

        var assignment = UserParentAssignment.Create(userId, parentCtrlNbr, role);
        await uow.UserParentAssignments.AddAsync(assignment);
        await uow.CommitAsync(ct);
        return assignment;
    }

    public async Task<UserParentAssignment> UpdateAssignmentRoleAsync(
        ControlNumber ctrlNbr, string role, ControlNumber? railroadCtrlNbr = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        if (role == Roles.SystemAdmin)
            throw new ValidationException("Role", "SystemAdmin cannot be assigned per-parent");

        var roleEntity = await uow.Roles.GetByNameAsync(role)
            ?? throw new ValidationException("Role", $"Unknown role '{role}'");

        var assignment = await uow.UserParentAssignments.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Assignment {ctrlNbr.Value} not found.");

        assignment.UpdateRole(role, railroadCtrlNbr);
        await uow.UserParentAssignments.UpdateAsync(assignment);
        await uow.CommitAsync(ct);
        return assignment;
    }

    public async Task<UserParentAssignment> DeleteAssignmentAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var assignment = await uow.UserParentAssignments.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Assignment {ctrlNbr.Value} not found.");
        assignment.Delete();
        await uow.UserParentAssignments.DeleteAsync(assignment.CtrlNbr);
        await uow.CommitAsync(ct);
        return assignment;
    }
}
