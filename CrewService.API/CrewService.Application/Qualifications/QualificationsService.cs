using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

/// <summary>
/// Represents the evaluated status of a non-Manual qualification type for a specific employee.
/// Computed on demand, never persisted.
/// </summary>
public sealed record ComputedQualificationStatus(
    QualificationType QualificationType,
    string Status,
    DateTime? AchievedAtUtc,
    DateTime? ExpiresAtUtc,
    IReadOnlyList<RequirementCheckResult> RequirementResults,
    bool IsSuspended = false,
    ControlNumber? SuspensionCtrlNbr = null,
    string? SuspensionReason = null,
    DateTime? AutoReinstateAtUtc = null,
    long? RelatedCertificationCtrlNbr = null);

public sealed class QualificationsService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    RequirementEvaluationService requirementEvaluationService)
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

    public async Task<(IReadOnlyList<ComputedQualificationStatus> ComputedStatuses, IReadOnlyList<EmployeeQualification> ManualQualifications)>
        GetEmployeeQualificationsAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr} not found.");

        var allTypes = await uow.QualificationTypes.GetActiveByParentCtrlNbrAsync(employee.ClientCtrlNbr);

        // Determine the crafts this employee is actively on so qual types scoped to other crafts are excluded
        var seniorities = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        var activeRosterCtrlNbrs = seniorities
            .Where(s => s.LastActiveRoster)
            .Select(s => s.RosterCtrlNbr)
            .ToList();
        var activeRosters = await uow.Rosters.GetByCtrlNbrsAsync(activeRosterCtrlNbrs, ct);
        var employeeCraftCtrlNbrs = activeRosters.Select(r => r.CraftCtrlNbr).ToHashSet();

        // Compute non-Manual qualification statuses fresh from ground-truth data
        var computedStatuses = new List<ComputedQualificationStatus>();
        foreach (var qualType in allTypes.Where(t =>
            !string.Equals(t.EvaluationStrategy, EvaluationStrategies.Manual, StringComparison.OrdinalIgnoreCase) &&
            (t.CraftCtrlNbr is null || employeeCraftCtrlNbrs.Contains(t.CraftCtrlNbr))))
        {
            var evalResult = await requirementEvaluationService.EvaluateAsync(employeeCtrlNbr, qualType, uow, ct);
            var status = evalResult.IsSuspended
                ? QualificationStatuses.Suspended
                : evalResult.AllSatisfied
                    ? (evalResult.ExpiresAtUtc.HasValue && evalResult.ExpiresAtUtc.Value <= DateTime.UtcNow.AddDays(EmployeeQualification.ExpiringSoonDays)
                        ? QualificationStatuses.ExpiringSoon
                        : (evalResult.SatisfiedStatus ?? QualificationStatuses.Active))
                    : (evalResult.OverallFailureKind ?? QualificationStatuses.Pending);
            computedStatuses.Add(new ComputedQualificationStatus(
                qualType,
                status,
                evalResult.AchievedAtUtc,
                evalResult.ExpiresAtUtc,
                evalResult.Results,
                evalResult.IsSuspended,
                evalResult.SuspensionCtrlNbr,
                evalResult.SuspensionReason,
                evalResult.AutoReinstateAtUtc,
                evalResult.RelatedCertificationCtrlNbr));
        }

        // Load only stored Manual qualification rows
        var manualQualifications = await uow.EmployeeQualifications.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);

        return (computedStatuses, manualQualifications);
    }

    public async Task<EmployeeQualification> GrantEmployeeQualificationAsync(
        ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr,
        string grantedBy, DateTime? expiresAtUtc, string? evidenceValue,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var qualType = await uow.QualificationTypes.GetByCtrlNbrAsync(qualificationTypeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"QualificationType {qualificationTypeCtrlNbr} not found.");

        if (!string.Equals(qualType.EvaluationStrategy, EvaluationStrategies.Manual, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Qualification '{qualType.Name}' uses strategy '{qualType.EvaluationStrategy}' and is computed automatically. Only Manual qualifications can be granted.");

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

    public async Task<EmployeeQualificationSuspension> SuspendComputedQualificationAsync(
        ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr,
        string suspendedBy, string reason, DateTime? autoReinstateAtUtc,
        DateTime? suspendedAtUtc = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var qualType = await uow.QualificationTypes.GetByCtrlNbrAsync(qualificationTypeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"QualificationType {qualificationTypeCtrlNbr} not found.");

        if (string.Equals(qualType.EvaluationStrategy, EvaluationStrategies.Manual, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Manual qualifications are revoked, not suspended. Use RevokeEmployeeQualificationAsync instead.");

        var existing = await uow.QualificationSuspensions
            .GetActiveByEmployeeAndTypeAsync(employeeCtrlNbr, qualificationTypeCtrlNbr, ct);
        if (existing is not null)
            throw new InvalidOperationException(
                $"An active suspension already exists for qualification '{qualType.Name}'. Lift it before creating a new one.");

        var suspension = EmployeeQualificationSuspension.Create(
            employeeCtrlNbr, qualificationTypeCtrlNbr, suspendedBy, reason, suspendedAtUtc, autoReinstateAtUtc);
        await uow.QualificationSuspensions.AddAsync(suspension, ct);
        await uow.CommitAsync(ct);
        return suspension;
    }

    public async Task<EmployeeQualificationSuspension> LiftQualificationSuspensionAsync(
        ControlNumber suspensionCtrlNbr, string reinstatedBy, string? note,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var suspension = await uow.QualificationSuspensions.GetByCtrlNbrAsync(suspensionCtrlNbr, ct)
            ?? throw new KeyNotFoundException("Qualification suspension not found.");
        suspension.Lift(reinstatedBy, note);
        await uow.QualificationSuspensions.UpdateAsync(suspension, ct);
        await uow.CommitAsync(ct);
        return suspension;
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
        HashSet<ControlNumber> EligibleEmployeeCtrlNbrs)>
        GetEligibleEmployeesDataAsync(
            ControlNumber craftRoleCtrlNbr, ControlNumber clientCtrlNbr,
            EmployeeEligibilityService eligibilityService, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var requiredQuals = await uow.CraftRoleQualifications.GetByCraftRoleAsync(craftRoleCtrlNbr);
        var employees = await uow.Employees.GetListByClientCtrlNbrAsync(clientCtrlNbr);
        var assignedCtrlNbrs = await uow.PositionAssignments.GetAssignedEmployeeCtrlNbrsAsync();
        var unassigned = assignedCtrlNbrs.Count == 0
            ? employees
            : employees.Where(e => !assignedCtrlNbrs.Contains(e.CtrlNbr.Value)).ToList();

        var eligibleCtrlNbrs = new HashSet<ControlNumber>();
        foreach (var emp in unassigned)
        {
            var result = await eligibilityService.CheckEligibilityByCraftRoleAsync(emp.CtrlNbr, craftRoleCtrlNbr, ct);
            if (result.IsEligible)
                eligibleCtrlNbrs.Add(emp.CtrlNbr);
        }

        return (unassigned, assignedCtrlNbrs, requiredQuals, eligibleCtrlNbrs);
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
