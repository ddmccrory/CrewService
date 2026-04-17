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
    IQualificationTypeRepository qualificationTypeRepository,
    IEmployeeQualificationRepository employeeQualificationRepository,
    ICraftRoleRepository craftRoleRepository,
    ISeniorityRepository seniorityRepository,
    IRosterRepository rosterRepository,
    IFraCertificationChecker? fraCertificationChecker = null)
{
    public async Task<EligibilityResult> CheckEligibilityAsync(
        ControlNumber employeeCtrlNbr,
        ControlNumber positionSlotCtrlNbr,
        CancellationToken ct = default)
    {
        var slotRequirements = await slotRequirementRepository.GetByPositionSlotAsync(positionSlotCtrlNbr);
        var blockingReasons = new List<BlockingReason>();

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

            if (requirement.QualificationTypeCtrlNbr is null)
                continue;

            var qualType = await qualificationTypeRepository
                .GetByCtrlNbrAsync(requirement.QualificationTypeCtrlNbr, ct);

            if (qualType is null || !qualType.IsActive)
                continue;

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

        return new EligibilityResult(
            IsEligible: blockingReasons.Count == 0,
            BlockingReasons: blockingReasons);
    }
}

public sealed record EligibilityResult(
    bool IsEligible,
    IReadOnlyList<BlockingReason> BlockingReasons);

public sealed record BlockingReason(string RuleCode, string Description);
