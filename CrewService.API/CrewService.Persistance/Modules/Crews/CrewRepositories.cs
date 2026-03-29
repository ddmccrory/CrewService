using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Crews;

internal sealed class CrewRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Crew>(dbContext, currentUserService), ICrewRepository
{
    public async Task<List<Crew>> GetByHomeGroupAsync(ControlNumber homeGroupCtrlNbr) =>
        await DbContext.Set<Crew>().Where(c => c.HomeGroupCtrlNbr == homeGroupCtrlNbr).OrderBy(c => c.Name).ToListAsync();

    public async Task<List<Crew>> GetByTypeAsync(string crewType) =>
        await DbContext.Set<Crew>().Where(c => c.CrewType == crewType).OrderBy(c => c.Name).ToListAsync();
}

internal sealed class CrewPositionRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CrewPosition>(dbContext, currentUserService), ICrewPositionRepository
{
    public async Task<List<CrewPosition>> GetByCrewAsync(ControlNumber crewCtrlNbr) =>
        await DbContext.Set<CrewPosition>().Where(p => p.CrewCtrlNbr == crewCtrlNbr).OrderBy(p => p.DisplayOrder).ToListAsync();
}

internal sealed class CrewIncumbencyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CrewIncumbency>(dbContext, currentUserService), ICrewIncumbencyRepository
{
    public async Task<List<CrewIncumbency>> GetByCrewPositionAsync(ControlNumber crewPositionCtrlNbr) =>
        await DbContext.Set<CrewIncumbency>().Where(i => i.CrewPositionCtrlNbr == crewPositionCtrlNbr).ToListAsync();

    public async Task<List<CrewIncumbency>> GetActiveByEmployeeAsync(ControlNumber employeeCtrlNbr, DateTime asOfUtc) =>
        await DbContext.Set<CrewIncumbency>()
            .Where(i => i.EmployeeCtrlNbr == employeeCtrlNbr && i.StartUtc <= asOfUtc && (i.EndUtc == null || i.EndUtc > asOfUtc))
            .ToListAsync();
}

internal sealed class CrewAssignmentRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CrewAssignment>(dbContext, currentUserService), ICrewAssignmentRepository
{
    public async Task<List<CrewAssignment>> GetByCrewAsync(ControlNumber crewCtrlNbr) =>
        await DbContext.Set<CrewAssignment>().Where(a => a.CrewCtrlNbr == crewCtrlNbr).ToListAsync();

    public async Task<List<CrewAssignment>> GetByAssignmentGroupAsync(ControlNumber assignmentGroupCtrlNbr) =>
        await DbContext.Set<CrewAssignment>().Where(a => a.AssignmentGroupCtrlNbr == assignmentGroupCtrlNbr).ToListAsync();
}

internal sealed class CrewAttachmentInstanceRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CrewAttachmentInstance>(dbContext, currentUserService), ICrewAttachmentInstanceRepository
{
    public async Task<List<CrewAttachmentInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr) =>
        await DbContext.Set<CrewAttachmentInstance>().Where(a => a.WorkInstanceCtrlNbr == workInstanceCtrlNbr).ToListAsync();
}