using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

public interface IFraCertificationChecker
{
    Task<bool> HasActiveCertificationAsync(ControlNumber employeeCtrlNbr, ControlNumber regulatoryQualificationCtrlNbr, CancellationToken ct = default);
}

public sealed class EmployeeEligibilityService(
    ISlotRequirementRepository slotRequirementRepository,
    IPositionSlotRepository positionSlotRepository,
    IQualificationTypeRepository qualificationTypeRepository,
    IEmployeeQualificationRepository employeeQualificationRepository,
    ICraftRoleRepository craftRoleRepository,
    ICraftRoleQualificationRepository craftRoleQualificationRepository,
    ISeniorityRepository seniorityRepository,
    IRosterRepository rosterRepository,
    IFraCertificationChecker? fraCertificationChecker = null)
{
    public async Task<EligibilityResult> CheckEligibilityAsync(
        ControlNumber employeeCtrlNbr,
        ControlNumber positionSlotCtrlNbr,
        CancellationToken ct = default)
    {
        var blockingReasons = new List<BlockingReason>();

        // ── 1. Slot-level requirements (explicit overrides) ──────────────────
        var slotRequirements = await slotRequirementRepository.GetByPositionSlotAsync(positionSlotCtrlNbr);

        foreach (var requirement in slotRequirements)
        {
            if (requirement.CraftRoleCtrlNbr is not null)
            {
                var craftRole = await craftRoleRepository.GetByCtrlNbrAsync(requirement.CraftRoleCtrlNbr, ct);
                if (craftRole is null)
                {
                    blockingReasons.Add(new BlockingReason(
                        "CRAFT_ROLE_NOT_FOUND",
                        "Required craft role configuration was not found"));
                }
                else
                {
                    var employeeSeniority = await seniorityRepository.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
                    var activeRosterCtrlNbrs = employeeSeniority
                        .Where(s => s.LastActiveRoster)
                        .Select(s => s.RosterCtrlNbr)
                        .ToHashSet();

                    var craftRosters = await rosterRepository.GetByCraftCtrlNbrAsync(craftRole.CraftCtrlNbr);
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
                await EvaluateQualificationTypeAsync(employeeCtrlNbr, requirement.QualificationTypeCtrlNbr, blockingReasons, ct);
        }

        // ── 2. Role-level required qualifications (B2: template-based) ───────
        var slot = await positionSlotRepository.GetByCtrlNbrAsync(positionSlotCtrlNbr, ct);
        if (slot is not null)
        {
            var roleQualifications = await craftRoleQualificationRepository.GetByCraftRoleAsync(slot.CraftRoleCtrlNbr);
            foreach (var roleQual in roleQualifications)
                await EvaluateQualificationTypeAsync(employeeCtrlNbr, roleQual.QualificationTypeCtrlNbr, blockingReasons, ct);
        }

        return new EligibilityResult(
            IsEligible: blockingReasons.Count == 0,
            BlockingReasons: blockingReasons);
    }

    private async Task EvaluateQualificationTypeAsync(
        ControlNumber employeeCtrlNbr,
        ControlNumber qualificationTypeCtrlNbr,
        List<BlockingReason> blockingReasons,
        CancellationToken ct)
    {
        var qualType = await qualificationTypeRepository.GetByCtrlNbrAsync(qualificationTypeCtrlNbr, ct);

        if (qualType is null || !qualType.IsActive)
            return;

        if (qualType.EvaluationStrategy == "FraCertification")
        {
            if (qualType.RegulatoryQualificationCtrlNbr is null)
            {
                blockingReasons.Add(new BlockingReason(
                    "FRA_CERT_REQUIREMENT_INVALID",
                    $"FRA certification requirement is not configured for {qualType.Name}"));
            }
            else if (fraCertificationChecker is not null)
            {
                var hasCert = await fraCertificationChecker
                    .HasActiveCertificationAsync(employeeCtrlNbr, qualType.RegulatoryQualificationCtrlNbr, ct);

                if (!hasCert)
                {
                    blockingReasons.Add(new BlockingReason(
                        "FRA_CERT_MISSING",
                        $"Missing or inactive FRA certification for {qualType.Name}"));
                }
            }
        }
        else
        {
            var qualification = await employeeQualificationRepository
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
