using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RailroadInformationRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RailroadInformation>(dbContext, currentUserService), IRailroadInformationRepository
{
    public async Task<IReadOnlyList<RailroadInformation>> GetByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<RailroadInformation>()
            .Where(r => r.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr)
            .OrderByDescending(r => r.PublishedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RailroadInformation>> GetPublishedByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<RailroadInformation>()
            .Where(r => r.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && r.Status == "Published")
            .OrderByDescending(r => r.PublishedAtUtc)
            .ToListAsync(ct);
}

internal sealed class RailroadInformationReadReceiptRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RailroadInformationReadReceipt>(dbContext, currentUserService), IRailroadInformationReadReceiptRepository
{
    public async Task<IReadOnlyList<RailroadInformationReadReceipt>> GetByInformationAsync(
        ControlNumber informationCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<RailroadInformationReadReceipt>()
            .Where(r => r.InformationCtrlNbr == informationCtrlNbr)
            .OrderBy(r => r.ReadAtUtc)
            .ToListAsync(ct);

    public async Task<RailroadInformationReadReceipt?> GetByInformationAndEmployeeAsync(
        ControlNumber informationCtrlNbr, ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<RailroadInformationReadReceipt>()
            .SingleOrDefaultAsync(r => r.InformationCtrlNbr == informationCtrlNbr
                                       && r.EmployeeCtrlNbr == employeeCtrlNbr, ct);
}
