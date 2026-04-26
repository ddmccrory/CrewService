using CrewService.Application.Qualifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.SeniorityOps;

public sealed class SeniorityAppService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    QualificationReactiveService qualificationReactiveService)
{
    public sealed record SeniorityListItem(
        Domain.Models.Seniority.Seniority Seniority,
        string EmployeeNumber,
        string? EmployeeUserId,
        string FullNameLnf,
        string SeniorityStateName,
        List<string> RestrictionLabels);

    public async Task<List<SeniorityListItem>> GetAllAsync(
        ControlNumber? rosterCtrlNbr = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var seniorities = rosterCtrlNbr is not null
            ? await uow.Seniority.GetByRosterCtrlNbrAsync(rosterCtrlNbr)
            : await uow.Seniority.GetAllAsync();

        if (seniorities.Count == 0) return [];

        var uniqueEmpCtrlNbrs = seniorities.Select(s => s.EmployeeCtrlNbr).Distinct().ToList();
        var employees = await uow.Employees.GetByCtrlNbrsAsync(uniqueEmpCtrlNbrs, ct);
        var employeeMap = employees.ToDictionary(e => e.CtrlNbr.Value);

        var uniqueStateCtrlNbrs = seniorities.Select(s => s.SeniorityStateCtrlNbr).Distinct().ToList();
        var stateMap = new Dictionary<long, string>();
        foreach (var stateCtrlNbr in uniqueStateCtrlNbrs)
        {
            var state = await uow.SeniorityStates.GetByCtrlNbrAsync(stateCtrlNbr);
            stateMap[stateCtrlNbr.Value] = state?.StateDescription ?? string.Empty;
        }

        var empRestrictionLabels = new Dictionary<ControlNumber, List<string>>();
        var firstRosterCtrlNbr = seniorities.Select(s => s.RosterCtrlNbr).FirstOrDefault();
        if (firstRosterCtrlNbr is not null)
        {
            var roster = await uow.Rosters.GetByCtrlNbrAsync(firstRosterCtrlNbr);
            if (roster is not null)
            {
                var restrictingQualTypes = (await uow.QualificationTypes.GetActiveByCraftCtrlNbrAsync(roster.CraftCtrlNbr))
                    .Where(qt => qt.RestrictionLabel is not null).ToList();

                if (restrictingQualTypes.Count > 0)
                {
                    var empQuals = await uow.EmployeeQualifications.GetActiveByEmployeeCtrlNbrsAsync(uniqueEmpCtrlNbrs);
                    var empActiveQualTypes = empQuals
                        .GroupBy(eq => eq.EmployeeCtrlNbr)
                        .ToDictionary(g => g.Key, g => g.Select(eq => eq.QualificationTypeCtrlNbr!).ToHashSet());

                    foreach (var empCtrlNbr in uniqueEmpCtrlNbrs)
                    {
                        empActiveQualTypes.TryGetValue(empCtrlNbr, out var heldQuals);
                        heldQuals ??= [];
                        foreach (var qt in restrictingQualTypes)
                        {
                            if (!heldQuals.Contains(qt.CtrlNbr))
                            {
                                if (!empRestrictionLabels.TryGetValue(empCtrlNbr, out var labels))
                                {
                                    labels = [];
                                    empRestrictionLabels[empCtrlNbr] = labels;
                                }
                                labels.Add(qt.RestrictionLabel!);
                            }
                        }
                    }
                }
            }
        }

        return seniorities.Select(s =>
        {
            employeeMap.TryGetValue(s.EmployeeCtrlNbr.Value, out var emp);
            var stateName = stateMap.GetValueOrDefault(s.SeniorityStateCtrlNbr.Value, string.Empty);
            empRestrictionLabels.TryGetValue(s.EmployeeCtrlNbr, out var restrictionLabels);
            return new SeniorityListItem(
                s,
                emp?.EmployeeNumber ?? string.Empty,
                emp?.UserId,
                string.Empty, // name resolved in presentation via EmployeeNameService
                stateName,
                restrictionLabels ?? []);
        }).ToList();
    }

    public async Task<Domain.Models.Seniority.Seniority> GetAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Seniority.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Seniority {ctrlNbr.Value} not found.");
    }

    public async Task<Domain.Models.Seniority.Seniority> CreateAsync(
        ControlNumber rosterCtrlNbr, ControlNumber employeeCtrlNbr,
        bool lastActiveRoster, DateTime rosterDate, int rank,
        ControlNumber seniorityStateCtrlNbr, bool canTrain,
        CancellationToken ct = default)
    {
        var seniority = Domain.Models.Seniority.Seniority.Create(
            rosterCtrlNbr, employeeCtrlNbr, lastActiveRoster, rosterDate, rank, seniorityStateCtrlNbr, canTrain);

        ControlNumber? craftCtrlNbr = null;
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.Seniority.Add(seniority);
        var roster = await uow.Rosters.GetByCtrlNbrAsync(rosterCtrlNbr, ct);
        if (roster is not null)
            craftCtrlNbr = roster.CraftCtrlNbr;
        await uow.CommitAsync(ct);

        if (craftCtrlNbr is not null)
            await qualificationReactiveService.HandleAddedToRosterAsync(seniority.EmployeeCtrlNbr, craftCtrlNbr);

        return seniority;
    }

    public async Task<Domain.Models.Seniority.Seniority> UpdateAsync(
        ControlNumber ctrlNbr, bool lastActiveRoster, DateTime rosterDate, int rank,
        ControlNumber seniorityStateCtrlNbr, bool canTrain, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var seniority = await uow.Seniority.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Seniority {ctrlNbr.Value} not found.");
        seniority.Update(lastActiveRoster, rosterDate, rank, seniorityStateCtrlNbr, canTrain);
        uow.Seniority.Update(seniority);
        await uow.CommitAsync(ct);
        return seniority;
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var seniority = await uow.Seniority.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Seniority {ctrlNbr.Value} not found.");
        await uow.Seniority.DeleteAsync(seniority.CtrlNbr);
        await uow.CommitAsync(ct);
    }

    public async Task<(bool Found, long CraftCtrlNbr, string CraftName)> GetActiveCraftForEmployeeAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var seniorityRecords = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        var activeRecord = seniorityRecords.FirstOrDefault(s => s.LastActiveRoster);
        if (activeRecord is null) return (false, 0, string.Empty);

        var roster = await uow.Rosters.GetByCtrlNbrAsync(activeRecord.RosterCtrlNbr);
        if (roster is null) return (false, 0, string.Empty);

        var craft = await uow.Crafts.GetByCtrlNbrAsync(roster.CraftCtrlNbr);
        if (craft is null) return (false, 0, string.Empty);

        return (true, craft.CtrlNbr.Value, craft.CraftName);
    }
}
