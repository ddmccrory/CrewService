using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Dispatching;

public sealed class DispatchingService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<IReadOnlyList<DispatchProjection>> GetProjectionsAsync(
        IEnumerable<ControlNumber> positionSlotCtrlNbrs, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var results = new List<DispatchProjection>();
        foreach (var slotCtrlNbr in positionSlotCtrlNbrs)
            results.AddRange(await uow.DispatchProjections.GetByPositionSlotAsync(slotCtrlNbr));
        return results;
    }

    public async Task<IReadOnlyList<DispatchDecisionLog>> GetDecisionLogsAsync(
        ControlNumber positionSlotCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DispatchDecisionLogs.GetByPositionSlotAsync(positionSlotCtrlNbr);
    }

    public async Task<DispatchOverride> RequestOverrideAsync(
        long positionSlotCtrlNbr, long employeeCtrlNbr,
        string overrideType, string reasonCode, string reasonText,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var dispatch = DispatchOverride.Create(positionSlotCtrlNbr, employeeCtrlNbr, overrideType, reasonCode, reasonText);
        await uow.DispatchOverrides.AddAsync(dispatch, ct);
        await uow.CommitAsync(ct);
        return dispatch;
    }

    public async Task<DispatchOverride> ApproveOverrideAsync(
        ControlNumber ctrlNbr, long approvedByCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var dispatch = await uow.DispatchOverrides.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Override {ctrlNbr} not found.");
        dispatch.Approve(approvedByCtrlNbr);
        await uow.DispatchOverrides.UpdateAsync(dispatch, ct);
        await uow.CommitAsync(ct);
        return dispatch;
    }

    public async Task<IReadOnlyList<EmployeeBooking>> GetEmployeeBookingsAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeeBookings.GetByEmployeeAsync(
            employeeCtrlNbr, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(30));
    }

    public async Task<EmployeeBooking> CreateEmployeeBookingAsync(
        long employeeCtrlNbr, DateTime startUtc, DateTime endUtc,
        ControlNumber? positionSlotCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var booking = EmployeeBooking.Create(employeeCtrlNbr, startUtc, endUtc, positionSlotCtrlNbr);
        await uow.EmployeeBookings.AddAsync(booking, ct);
        await uow.CommitAsync(ct);
        return booking;
    }
}
