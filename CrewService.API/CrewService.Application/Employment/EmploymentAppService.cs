using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Employment;

public sealed class EmploymentAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    // ── Employment Status ────────────────────────────────────────────────────

    public async Task<List<EmploymentStatus>> GetAllStatusesAsync(
        ControlNumber clientCtrlNbr, int pageNumber = 0, int pageSize = 0, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return pageSize > 0
            ? await uow.EmploymentStatuses.GetByClientCtrlNbrAsync(clientCtrlNbr, pageNumber, pageSize)
            : await uow.EmploymentStatuses.GetByClientCtrlNbrAsync(clientCtrlNbr);
    }

    public async Task<EmploymentStatus> GetStatusAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmploymentStatuses.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Employment status {ctrlNbr.Value} not found.");
    }

    public async Task<EmploymentStatus> CreateStatusAsync(
        ControlNumber clientCtrlNbr, string statusCode, string statusName, int statusNumber, string employmentCode,
        CancellationToken ct = default)
    {
        var status = EmploymentStatus.Create(clientCtrlNbr, statusCode, statusName, statusNumber, employmentCode);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.EmploymentStatuses.Add(status);
        await uow.CommitAsync(ct);
        return status;
    }

    public async Task<EmploymentStatus> UpdateStatusAsync(
        ControlNumber ctrlNbr, string statusCode, string statusName, int statusNumber, string employmentCode,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var status = await uow.EmploymentStatuses.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Employment status {ctrlNbr.Value} not found.");
        status.Update(statusCode, statusName, statusNumber, employmentCode);
        uow.EmploymentStatuses.Update(status);
        await uow.CommitAsync(ct);
        return status;
    }

    public async Task DeleteStatusAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var status = await uow.EmploymentStatuses.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Employment status {ctrlNbr.Value} not found.");
        uow.EmploymentStatuses.Remove(status);
        await uow.CommitAsync(ct);
    }

    // ── Employment Status History ────────────────────────────────────────────

    public async Task<List<EmploymentStatusHistory>> GetAllHistoryAsync(
        ControlNumber employeeCtrlNbr, int pageNumber = 0, int pageSize = 0, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return pageSize > 0
            ? await uow.EmploymentStatusHistory.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr, pageNumber, pageSize)
            : await uow.EmploymentStatusHistory.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
    }

    public async Task<EmploymentStatusHistory> GetHistoryRecordAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmploymentStatusHistory.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Employment status history {ctrlNbr.Value} not found.");
    }

    public async Task<EmploymentStatusHistory> CreateHistoryRecordAsync(
        ControlNumber employeeCtrlNbr, ControlNumber employmentStatusCtrlNbr, DateTime statusChangeDate,
        CancellationToken ct = default)
    {
        var record = EmploymentStatusHistory.Create(employeeCtrlNbr, employmentStatusCtrlNbr, statusChangeDate);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.EmploymentStatusHistory.Add(record);
        await uow.CommitAsync(ct);
        return record;
    }

    public async Task DeleteHistoryRecordAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var record = await uow.EmploymentStatusHistory.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Employment status history {ctrlNbr.Value} not found.");
        uow.EmploymentStatusHistory.Remove(record);
        await uow.CommitAsync(ct);
    }
}
