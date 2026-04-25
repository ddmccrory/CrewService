using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

public sealed class QualificationsService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<IReadOnlyList<QualificationType>> GetQualificationTypesAsync(
        ControlNumber parentCtrlNbr, bool activeOnly, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return activeOnly
            ? await uow.QualificationTypes.GetActiveByParentCtrlNbrAsync(parentCtrlNbr)
            : await uow.QualificationTypes.GetByParentCtrlNbrAsync(parentCtrlNbr);
    }

    public async Task<IReadOnlyList<QualificationRequirement>> GetQualificationRequirementsAsync(
        ControlNumber qualificationTypeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.QualificationRequirements.GetByQualificationTypeCtrlNbrAsync(qualificationTypeCtrlNbr);
    }

    public async Task<IReadOnlyList<EmployeeQualification>> GetEmployeeQualificationsAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeeQualifications.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
    }

    public async Task<EmployeeQualification> GrantEmployeeQualificationAsync(
        ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr,
        string grantedBy, DateTime? expiresAtUtc, string? evidenceValue,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var qualType = await uow.QualificationTypes.GetByCtrlNbrAsync(qualificationTypeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"QualificationType {qualificationTypeCtrlNbr} not found.");

        if (qualType.CraftCtrlNbr is not null)
        {
            var employeeSeniority = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
            var activeRosterCtrlNbrs = employeeSeniority
                .Where(s => s.LastActiveRoster).Select(s => s.RosterCtrlNbr).ToHashSet();
            var craftRosters = await uow.Rosters.GetByCraftCtrlNbrAsync(qualType.CraftCtrlNbr);
            if (!craftRosters.Any(r => activeRosterCtrlNbrs.Contains(r.CtrlNbr)))
                throw new InvalidOperationException(
                    $"Employee does not have active membership in the craft required for qualification '{qualType.Name}'.");
        }

        var existing = await uow.EmployeeQualifications.GetByEmployeeAndTypeAsync(employeeCtrlNbr, qualificationTypeCtrlNbr);

        if (existing is null)
        {
            var created = EmployeeQualification.Create(employeeCtrlNbr, qualificationTypeCtrlNbr, grantedBy, expiresAtUtc);
            if (!string.IsNullOrWhiteSpace(evidenceValue))
                created.AddEvidence(EvidenceTypes.ManualCompletion, evidenceValue, grantedBy);
            await uow.EmployeeQualifications.AddAsync(created, ct);
            await uow.CommitAsync(ct);
            return created;
        }

        existing.Reinstate(expiresAtUtc);
        if (!string.IsNullOrWhiteSpace(evidenceValue))
            existing.AddEvidence(EvidenceTypes.ManualCompletion, evidenceValue, grantedBy);
        await uow.EmployeeQualifications.UpdateAsync(existing, ct);
        await uow.CommitAsync(ct);
        return existing;
    }

    public async Task<EmployeeQualification> RevokeEmployeeQualificationAsync(
        ControlNumber employeeQualificationCtrlNbr, string reason, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var qualification = await uow.EmployeeQualifications.GetByCtrlNbrAsync(employeeQualificationCtrlNbr, ct)
            ?? throw new KeyNotFoundException("Employee qualification not found.");
        qualification.Revoke(reason);
        await uow.EmployeeQualifications.UpdateAsync(qualification, ct);
        await uow.CommitAsync(ct);
        return qualification;
    }

    public async Task<(IReadOnlyList<Employee> Employees, HashSet<long> AssignedCtrlNbrs,
        IReadOnlyList<CraftRoleQualification> RequiredQuals,
        Dictionary<ControlNumber, HashSet<ControlNumber>> QualsByEmployee)>
        GetEligibleEmployeesDataAsync(
            ControlNumber craftRoleCtrlNbr, ControlNumber clientCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var requiredQuals = await uow.CraftRoleQualifications.GetByCraftRoleAsync(craftRoleCtrlNbr);
        var employees = await uow.Employees.GetListByClientCtrlNbrAsync(clientCtrlNbr);
        var assignedCtrlNbrs = await uow.PositionAssignments.GetAssignedEmployeeCtrlNbrsAsync();
        var unassigned = assignedCtrlNbrs.Count == 0
            ? employees
            : employees.Where(e => !assignedCtrlNbrs.Contains(e.CtrlNbr.Value)).ToList();

        var allEmpQuals = await uow.EmployeeQualifications.GetActiveByEmployeeCtrlNbrsAsync(unassigned.Select(e => e.CtrlNbr));
        var qualsByEmployee = allEmpQuals
            .GroupBy(q => q.EmployeeCtrlNbr)
            .ToDictionary(g => g.Key, g => g.Select(q => q.QualificationTypeCtrlNbr).ToHashSet());

        return (unassigned, assignedCtrlNbrs, requiredQuals, qualsByEmployee);
    }

    public async Task<QualificationType> CreateQualificationTypeAsync(
        ControlNumber parentCtrlNbr, string code, string name, string evaluationStrategy,
        ControlNumber? scopeGroupCtrlNbr, ControlNumber? craftCtrlNbr,
        ControlNumber? regulatoryQualificationCtrlNbr, string? description,
        int? expirationMonths, bool calendarYearExpiry, int graceDays, int renewalLeadDays,
        bool isBlocking, CancellationToken ct = default)
    {
        var qualificationType = QualificationType.Create(
            parentCtrlNbr, code, name, evaluationStrategy, scopeGroupCtrlNbr, craftCtrlNbr,
            regulatoryQualificationCtrlNbr, description, expirationMonths, calendarYearExpiry,
            graceDays, renewalLeadDays, isBlocking);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.QualificationTypes.Add(qualificationType);
        await uow.CommitAsync(ct);
        return qualificationType;
    }

    public async Task<QualificationType> UpdateQualificationTypeAsync(
        ControlNumber ctrlNbr, string name, string? description, string evaluationStrategy,
        ControlNumber? scopeGroupCtrlNbr, ControlNumber? craftCtrlNbr,
        int? expirationMonths, bool calendarYearExpiry, int graceDays, int renewalLeadDays,
        bool isBlocking, string? restrictionLabel, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var qualificationType = await uow.QualificationTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException("Qualification type not found.");
        if (qualificationType.IsSystemSeeded || qualificationType.EvaluationStrategy == EvaluationStrategies.FraCertification)
            throw new InvalidOperationException("FRA-managed qualification types cannot be modified from this menu.");
        qualificationType.Update(name, description, evaluationStrategy, scopeGroupCtrlNbr, craftCtrlNbr,
            expirationMonths, calendarYearExpiry, graceDays, renewalLeadDays, isBlocking, restrictionLabel);
        uow.QualificationTypes.Update(qualificationType);
        await uow.CommitAsync(ct);
        return qualificationType;
    }

    public async Task DeleteQualificationTypeAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var qualificationType = await uow.QualificationTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException("Qualification type not found.");
        if (qualificationType.IsSystemSeeded || qualificationType.EvaluationStrategy == EvaluationStrategies.FraCertification)
            throw new InvalidOperationException("FRA-managed qualification types cannot be deleted.");
        uow.QualificationTypes.Remove(qualificationType);
        await uow.CommitAsync(ct);
    }

    public async Task<QualificationType> SetQualificationTypeActiveAsync(
        ControlNumber ctrlNbr, bool isActive, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var qualificationType = await uow.QualificationTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException("Qualification type not found.");
        if (qualificationType.IsSystemSeeded || qualificationType.EvaluationStrategy == EvaluationStrategies.FraCertification)
            throw new InvalidOperationException("FRA-managed qualification types cannot be modified from this menu.");
        if (isActive) qualificationType.Activate(); else qualificationType.Deactivate();
        uow.QualificationTypes.Update(qualificationType);
        await uow.CommitAsync(ct);
        return qualificationType;
    }

    public async Task<QualificationRequirement> AddQualificationRequirementAsync(
        ControlNumber qualificationTypeCtrlNbr, string requirementKind, int threshold,
        string thresholdUnit, string description, string? eventSource, string? activityFilter,
        ControlNumber? requiredQualTypeCtrlNbr, ControlNumber? requiredRegulatoryQualCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var qualificationType = await uow.QualificationTypes.GetByCtrlNbrAsync(qualificationTypeCtrlNbr, ct)
            ?? throw new KeyNotFoundException("Qualification type not found.");
        var requirement = qualificationType.AddRequirement(requirementKind, threshold, thresholdUnit,
            description, eventSource, activityFilter, requiredQualTypeCtrlNbr, requiredRegulatoryQualCtrlNbr);
        uow.QualificationTypes.Update(qualificationType);
        await uow.CommitAsync(ct);
        return requirement;
    }

    public async Task<QualificationRequirement> UpdateQualificationRequirementAsync(
        ControlNumber reqCtrlNbr, int threshold, string thresholdUnit, string description,
        string? eventSource, string? activityFilter,
        ControlNumber? requiredQualTypeCtrlNbr, ControlNumber? requiredRegulatoryQualCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var requirement = await uow.QualificationRequirements.GetByCtrlNbrAsync(reqCtrlNbr, ct)
            ?? throw new KeyNotFoundException("Requirement not found.");
        requirement.Update(threshold, thresholdUnit, description, eventSource, activityFilter,
            requiredQualTypeCtrlNbr, requiredRegulatoryQualCtrlNbr);
        uow.QualificationRequirements.Update(requirement);
        await uow.CommitAsync(ct);
        return requirement;
    }

    public async Task RemoveQualificationRequirementAsync(ControlNumber reqCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var requirement = await uow.QualificationRequirements.GetByCtrlNbrAsync(reqCtrlNbr, ct)
            ?? throw new KeyNotFoundException("Requirement not found.");
        var qualificationType = await uow.QualificationTypes.GetByCtrlNbrAsync(requirement.QualificationTypeCtrlNbr, ct)
            ?? throw new KeyNotFoundException("Qualification type not found.");
        qualificationType.RemoveRequirement(reqCtrlNbr);
        uow.QualificationRequirements.Remove(requirement);
        uow.QualificationTypes.Update(qualificationType);
        await uow.CommitAsync(ct);
    }
}
