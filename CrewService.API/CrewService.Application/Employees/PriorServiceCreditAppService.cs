using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Employees;

public sealed class PriorServiceCreditAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<EmployeePriorServiceCredit?> GetByEmployeeAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeePriorServiceCredits.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
    }

    public async Task<EmployeePriorServiceCredit> GetAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeePriorServiceCredits.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Prior service credit {ctrlNbr.Value} not found.");
    }

    public async Task<EmployeePriorServiceCredit> CreateAsync(
        ControlNumber employeeCtrlNbr, int serviceYears, int serviceMonths, int serviceDays,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.EmployeePriorServiceCredits.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        if (existing is not null)
            throw new InvalidOperationException("A prior service credit record already exists for this employee.");
        var credit = EmployeePriorServiceCredit.Create(employeeCtrlNbr, serviceYears, serviceMonths, serviceDays);
        uow.EmployeePriorServiceCredits.Add(credit);
        await uow.CommitAsync(ct);
        return credit;
    }

    public async Task<EmployeePriorServiceCredit> UpdateAsync(
        ControlNumber ctrlNbr, int serviceYears, int serviceMonths, int serviceDays,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var credit = await uow.EmployeePriorServiceCredits.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Prior service credit {ctrlNbr.Value} not found.");
        credit.Update(serviceYears, serviceMonths, serviceDays);
        uow.EmployeePriorServiceCredits.Update(credit);
        await uow.CommitAsync(ct);
        return credit;
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var credit = await uow.EmployeePriorServiceCredits.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Prior service credit {ctrlNbr.Value} not found.");
        uow.EmployeePriorServiceCredits.Remove(credit);
        await uow.CommitAsync(ct);
    }
}
