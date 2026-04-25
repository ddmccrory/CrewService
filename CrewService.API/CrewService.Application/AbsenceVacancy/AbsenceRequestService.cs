using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.AbsenceVacancy;

public sealed class AbsenceRequestService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<AbsenceRequest> SubmitAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc, string reasonCode, string? notes)
    {
        var absence = AbsenceRequest.Create(employeeCtrlNbr, startUtc, endUtc, reasonCode, notes);
        await using var uow = await uowFactory.CreateAsync();
        uow.AbsenceRequests.Add(absence);
        await uow.CommitAsync();
        return absence;
    }

    public async Task<AbsenceRequest> SubmitWithCodeAsync(
        ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc,
        ControlNumber absenceCodeCtrlNbr, string reasonCode,
        ControlNumber? positionSlotCtrlNbr = null,
        bool isSystemGenerated = false, string? notes = null)
    {
        var absence = AbsenceRequest.CreateWithCode(
            employeeCtrlNbr, startUtc, endUtc, absenceCodeCtrlNbr, reasonCode,
            positionSlotCtrlNbr, isSystemGenerated, notes);
        await using var uow = await uowFactory.CreateAsync();
        uow.AbsenceRequests.Add(absence);
        await uow.CommitAsync();
        return absence;
    }

    public async Task<List<AbsenceRequest>> GetPendingAsync()
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetPendingAsync();
    }

    public async Task<List<AbsenceRequest>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetByEmployeeAsync(employeeCtrlNbr);
    }

    public async Task<AbsenceRequest> ApproveAsync(ControlNumber ctrlNbr, ControlNumber approvedByCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");
        absence.Approve(approvedByCtrlNbr);
        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();
        return absence;
    }

    public async Task<AbsenceRequest> DenyAsync(ControlNumber ctrlNbr, ControlNumber deniedByCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");
        absence.Deny(deniedByCtrlNbr);
        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();
        return absence;
    }

    public async Task<AbsenceRequest> CancelAsync(ControlNumber ctrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");
        absence.Cancel();
        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();
        return absence;
    }
}
