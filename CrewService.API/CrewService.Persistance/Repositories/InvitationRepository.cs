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

    public async Task<List<Invitation>> GetByParentCtrlNbrAsync(long parentCtrlNbr)
    {
        var ctrlNbr = ControlNumber.Create(parentCtrlNbr);
        return await DbContext.Set<Invitation>()
            .Where(i => i.ParentCtrlNbr == ctrlNbr)
            .ToListAsync();
    }

    public async Task<Invitation?> GetPendingByEmailAndParentAsync(string email, long parentCtrlNbr)
    {
        var normalized = email.ToLowerInvariant();
        var ctrlNbr = ControlNumber.Create(parentCtrlNbr);
        return await DbContext.Set<Invitation>()
            .SingleOrDefaultAsync(i =>
                i.Email == normalized &&
                i.ParentCtrlNbr == ctrlNbr &&
                i.Status == InvitationStatus.Pending);
    }
}
