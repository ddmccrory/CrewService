using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

public interface IFraCertificationChecker
{
    Task<bool> HasActiveCertificationAsync(ControlNumber employeeCtrlNbr, ControlNumber regulatoryQualificationCtrlNbr, CancellationToken ct = default);
}

public sealed class EmployeeEligibilityService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IFraCertificationChecker? fraCertificationChecker = null)
{
    public async Task<EligibilityResult> CheckEligibilityAsync(
        ControlNumber employeeCtrlNbr,
        ControlNumber positionSlotCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var blockingReasons = new List<BlockingReason>();

        // ── 1. Slot-level requirements (explicit overrides) ──────────────────
        var slotRequirements = await uow.SlotRequirements.GetByPositionSlotAsync(positionSlotCtrlNbr);

        foreach (var requirement in slotRequirements)
        {
            if (requirement.CraftRoleCtrlNbr is not null)
            {
                var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(requirement.CraftRoleCtrlNbr, ct);
                if (craftRole is null)
                {
                    blockingReasons.Add(new BlockingReason(
                        "CRAFT_ROLE_NOT_FOUND",
                        "Required craft role configuration was not found"));
                }
                else
                {
                    var employeeSeniority = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
                    var activeRosterCtrlNbrs = employeeSeniority
                        .Where(s => s.LastActiveRoster)
                        .Select(s => s.RosterCtrlNbr)
                        .ToHashSet();

                    var craftRosters = await uow.Rosters.GetByCraftCtrlNbrAsync(craftRole.CraftCtrlNbr);
                    var hasActiveCraftMembership = craftRosters.Any(r => activeRosterCtrlNbrs.Contains(r.CtrlNbr));

                    if (!hasActiveCraftMembership)
                    {
                        blockingReasons.Add(new BlockingReason(
                            "CRAFT_MEMBERSHIP_MISSING",
                            $"Missing active craft membership for required role: {craftRole.Name}"));
                    }
                }
            }

            if (requirement.QualificationTypeCtrlNbr is not null)
                await EvaluateQualificationTypeAsync(uow, employeeCtrlNbr, requirement.QualificationTypeCtrlNbr, blockingReasons, ct);
        }

        // ── 2. Role-level required qualifications (B2: template-based) ───────
        var slot = await uow.PositionSlots.GetByCtrlNbrAsync(positionSlotCtrlNbr, ct);
        if (slot is not null)
        {
            var roleQualifications = await uow.CraftRoleQualifications.GetByCraftRoleAsync(slot.CraftRoleCtrlNbr);
            foreach (var roleQual in roleQualifications)
                await EvaluateQualificationTypeAsync(uow, employeeCtrlNbr, roleQual.QualificationTypeCtrlNbr, blockingReasons, ct);
        }

        return new EligibilityResult(
            IsEligible: blockingReasons.Count == 0,
            BlockingReasons: blockingReasons);
    }

    public async Task<EligibilityResult> CheckEligibilityByCraftRoleForPositionAsync(
        ControlNumber employeeCtrlNbr,
        ControlNumber crewPositionCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var position = await uow.CrewPositions.GetByCtrlNbrAsync(crewPositionCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Crew position {crewPositionCtrlNbr.Value} not found.");
        return await CheckEligibilityByCraftRoleAsync(employeeCtrlNbr, position.CraftRoleCtrlNbr, ct);
    }

    public async Task<EligibilityResult> CheckEligibilityByCraftRoleAsync(
        ControlNumber employeeCtrlNbr,
        ControlNumber craftRoleCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await CheckEligibilityByCraftRoleAsync(uow, employeeCtrlNbr, craftRoleCtrlNbr, ct);
    }

    internal async Task<EligibilityResult> CheckEligibilityByCraftRoleAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        ControlNumber craftRoleCtrlNbr,
        CancellationToken ct = default)
    {
        var blockingReasons = new List<BlockingReason>();

        var roleQualifications = await uow.CraftRoleQualifications.GetByCraftRoleAsync(craftRoleCtrlNbr);
        foreach (var roleQual in roleQualifications)
            await EvaluateQualificationTypeAsync(uow, employeeCtrlNbr, roleQual.QualificationTypeCtrlNbr, blockingReasons, ct);

        return new EligibilityResult(
            IsEligible: blockingReasons.Count == 0,
            BlockingReasons: blockingReasons);
    }

    private async Task EvaluateQualificationTypeAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        ControlNumber qualificationTypeCtrlNbr,
        List<BlockingReason> blockingReasons,
        CancellationToken ct)
    {
        var qualType = await uow.QualificationTypes.GetByCtrlNbrAsync(qualificationTypeCtrlNbr, ct);

        if (qualType is null || !qualType.IsActive)
            return;

        // Both "FraCertification" and "QualificationHeld" backed by an FRA cert are evaluated
        // on demand via IFraCertificationChecker. QualificationReactiveService is a no-op, so
        // no materialized EmployeeQualification rows exist for these strategies.
        var isFraBacked = qualType.RegulatoryQualificationCtrlNbr is not null
            && (qualType.EvaluationStrategy == "FraCertification"
                || qualType.EvaluationStrategy == "QualificationHeld");

        if (isFraBacked)
        {
            if (fraCertificationChecker is null)
                return; // checker not available — skip rather than incorrectly block

            var hasCert = await fraCertificationChecker
                .HasActiveCertificationAsync(employeeCtrlNbr, qualType.RegulatoryQualificationCtrlNbr!, ct);

            if (!hasCert)
            {
                blockingReasons.Add(new BlockingReason(
                    "FRA_CERT_MISSING",
                    $"Missing or inactive FRA certification for {qualType.Name}"));
            }
        }
        else
        {
            var qualification = await uow.EmployeeQualifications
                .GetByEmployeeAndTypeAsync(employeeCtrlNbr, qualType.CtrlNbr);

            if (qualification is null || qualification.Status is not ("Active" or "ExpiringSoon"))
            {
                if (qualType.IsBlocking)
                {
                    var status = qualification?.Status ?? "None";
                    blockingReasons.Add(new BlockingReason(
                        "NOT_QUALIFIED",
                        $"Missing or {status.ToLowerInvariant()} qualification: {qualType.Name}"));
                }
            }
        }
    }
}

public sealed record EligibilityResult(
    bool IsEligible,
    IReadOnlyList<BlockingReason> BlockingReasons);

public sealed record BlockingReason(string RuleCode, string Description);
