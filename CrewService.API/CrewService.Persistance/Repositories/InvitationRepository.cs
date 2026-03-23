using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class InvitationRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Invitation>(dbContext, currentUserService), IInvitationRepository
{
    public async Task<Invitation?> GetByTokenAsync(string token)
    {
        return await DbContext.Set<Invitation>()
            .SingleOrDefaultAsync(i => i.Token == token);
    }

    public async Task<List<Invitation>> GetByEmailAsync(string email)
    {
        var normalized = email.ToLowerInvariant();
        return await DbContext.Set<Invitation>()
            .Where(i => i.Email == normalized)
            .ToListAsync();
    }

    public async Task<List<Invitation>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr)
    {
        return await DbContext.Set<Invitation>()
            .Where(i => i.ParentCtrlNbr == parentCtrlNbr)
            .ToListAsync();
    }

    public async Task<Invitation?> GetPendingByEmailAndParentAsync(string email, ControlNumber? parentCtrlNbr)
    {
        var normalized = email.ToLowerInvariant();
        return await DbContext.Set<Invitation>()
            .SingleOrDefaultAsync(i =>
                i.Email == normalized &&
                i.ParentCtrlNbr == parentCtrlNbr &&
                i.Status == InvitationStatus.Pending);
    }

    public async Task<List<Invitation>> GetAcceptedByEmailAndParentAsync(string email, ControlNumber? parentCtrlNbr)
    {
        var normalized = email.ToLowerInvariant();
        return await DbContext.Set<Invitation>()
            .Where(i =>
                i.Email == normalized &&
                i.ParentCtrlNbr == parentCtrlNbr &&
                i.Status == InvitationStatus.Accepted)
            .ToListAsync();
    }

    public async Task<List<Invitation>> GetByRoleAsync(string role)
    {
        return await DbContext.Set<Invitation>()
            .Where(i => i.Role == role)
            .ToListAsync();
    }
}
