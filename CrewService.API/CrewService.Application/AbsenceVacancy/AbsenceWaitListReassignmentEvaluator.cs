using CrewService.Application.Absence;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.AbsenceVacancy;

public sealed class AbsenceWaitListReassignmentEvaluator(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IAbsenceCodeRepository absenceCodeRepository,
    IAbsenceRequestWaitListRecordRepository waitListRecordRepository,
    IDepartmentAbsenceWaitListPolicyRepository departmentWaitListPolicyRepository,
    IAbsenceWaitListAllowancePolicyRepository waitListAllowancePolicyRepository,
    ILogger<AbsenceWaitListReassignmentEvaluator> logger)
{
    public async Task<IReadOnlyList<AbsenceRequestWaitListRecord>> EvaluateCompensableDayAsync(
        DateTime requestDateUtc,
        CancellationToken ct = default)
    {
        var targetDate = DateTime.SpecifyKind(requestDateUtc, DateTimeKind.Utc).Date;

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var pending = await waitListRecordRepository.GetPendingByDateAsync(
            targetDate,
            AbsenceRequestWaitListType.CompensableDay,
            ct);

        if (pending.Count == 0)
            return [];

        var eligible = new List<AbsenceRequestWaitListRecord>(pending.Count);

        foreach (var waitListRecord in pending)
        {
            if (waitListRecord.CraftCtrlNbr is null)
            {
                logger.LogWarning(
                    "Comp-day waitlist record {WaitListCtrlNbr} has no craft context; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value);
                continue;
            }

            if (waitListRecord.DepartmentCtrlNbr is null)
            {
                logger.LogWarning(
                    "Comp-day waitlist record {WaitListCtrlNbr} has no department scope; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value);
                continue;
            }

            var departmentPolicy = await departmentWaitListPolicyRepository.GetByDepartmentAsync(waitListRecord.DepartmentCtrlNbr);
            if (departmentPolicy is null || !departmentPolicy.IsEnabled)
            {
                logger.LogWarning(
                    "Comp-day waitlist record {WaitListCtrlNbr} has no enabled department waitlist policy for department {DepartmentCtrlNbr}; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value,
                    waitListRecord.DepartmentCtrlNbr.Value);
                continue;
            }

            var craft = await uow.Crafts.GetByCtrlNbrAsync(waitListRecord.CraftCtrlNbr, ct);
            if (craft?.DynamicGroupCtrlNbr is null)
            {
                logger.LogWarning(
                    "Comp-day waitlist record {WaitListCtrlNbr} missing authoritative railroad context for craft {CraftCtrlNbr}; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value,
                    waitListRecord.CraftCtrlNbr.Value);
                continue;
            }

            var absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(waitListRecord.AbsenceCodeCtrlNbr, ct);
            if (absenceCode is null)
            {
                logger.LogWarning(
                    "Comp-day waitlist record {WaitListCtrlNbr} references missing absence code {AbsenceCodeCtrlNbr}; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value,
                    waitListRecord.AbsenceCodeCtrlNbr.Value);
                continue;
            }

            var allowanceCode = absenceCode.Code.Trim().ToUpperInvariant();
            var allowance = await waitListAllowancePolicyRepository.GetByCraftTypeCodeYearAsync(
                waitListRecord.CraftCtrlNbr,
                AbsenceRequestWaitListType.CompensableDay,
                allowanceCode,
                targetDate.Year);

            if (allowance is null || !allowance.IsEnabled)
            {
                logger.LogWarning(
                    "Comp-day waitlist record {WaitListCtrlNbr} has no enabled allowance policy for craft {CraftCtrlNbr} code {AllowanceCode} year {Year}; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value,
                    waitListRecord.CraftCtrlNbr.Value,
                    allowanceCode,
                    targetDate.Year);
                continue;
            }

            var existingRequests = await uow.AbsenceRequests.GetByDateRangeAsync(
                craft.DynamicGroupCtrlNbr,
                targetDate,
                targetDate.AddDays(1),
                includeAllStatuses: true,
                craftCtrlNbr: waitListRecord.CraftCtrlNbr,
                departmentCtrlNbr: waitListRecord.DepartmentCtrlNbr,
                ct: ct);

            var currentAssigned = 0;
            foreach (var existingRequest in existingRequests)
            {
                if (existingRequest.DeniedAtUtc.HasValue || existingRequest.CancelledAtUtc.HasValue)
                    continue;

                if (existingRequest.AbsenceCodeCtrlNbr is null)
                    continue;

                var existingCode = await absenceCodeRepository.GetByCtrlNbrAsync(existingRequest.AbsenceCodeCtrlNbr, ct);
                if (existingCode is null)
                    continue;

                if (!string.Equals(existingCode.Code, allowanceCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                currentAssigned++;
            }

            var maxAssignments = Math.Min(allowance.MaxAssignments, departmentPolicy.CompensableDayMaxAssignments);
            if (currentAssigned < maxAssignments)
                eligible.Add(waitListRecord);
        }

        return eligible
            .OrderBy(r => r.EntryUtc)
            .ThenBy(r => r.CtrlNbr)
            .ToList();
    }

    public async Task<IReadOnlyList<AbsenceRequestWaitListRecord>> EvaluateVacationWeekAsync(
        DateTime requestDateUtc,
        CancellationToken ct = default)
    {
        var targetDate = DateTime.SpecifyKind(requestDateUtc, DateTimeKind.Utc).Date;

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var pending = await waitListRecordRepository.GetPendingByDateAsync(
            targetDate,
            AbsenceRequestWaitListType.VacationWeek,
            ct);

        if (pending.Count == 0)
            return [];

        var eligible = new List<AbsenceRequestWaitListRecord>(pending.Count);

        foreach (var waitListRecord in pending)
        {
            if (waitListRecord.CraftCtrlNbr is null)
            {
                logger.LogWarning(
                    "Vacation-week waitlist record {WaitListCtrlNbr} has no craft context; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value);
                continue;
            }

            if (waitListRecord.DepartmentCtrlNbr is null)
            {
                logger.LogWarning(
                    "Vacation-week waitlist record {WaitListCtrlNbr} has no department scope; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value);
                continue;
            }

            var departmentPolicy = await departmentWaitListPolicyRepository.GetByDepartmentAsync(waitListRecord.DepartmentCtrlNbr);
            if (departmentPolicy is null || !departmentPolicy.IsEnabled)
            {
                logger.LogWarning(
                    "Vacation-week waitlist record {WaitListCtrlNbr} has no enabled department waitlist policy for department {DepartmentCtrlNbr}; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value,
                    waitListRecord.DepartmentCtrlNbr.Value);
                continue;
            }

            var craft = await uow.Crafts.GetByCtrlNbrAsync(waitListRecord.CraftCtrlNbr, ct);
            if (craft?.DynamicGroupCtrlNbr is null)
            {
                logger.LogWarning(
                    "Vacation-week waitlist record {WaitListCtrlNbr} missing authoritative railroad context for craft {CraftCtrlNbr}; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value,
                    waitListRecord.CraftCtrlNbr.Value);
                continue;
            }

            var absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(waitListRecord.AbsenceCodeCtrlNbr, ct);
            if (absenceCode is null)
            {
                logger.LogWarning(
                    "Vacation-week waitlist record {WaitListCtrlNbr} references missing absence code {AbsenceCodeCtrlNbr}; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value,
                    waitListRecord.AbsenceCodeCtrlNbr.Value);
                continue;
            }

            var vacationWeeks = ResolveVacationWeeks(absenceCode.Code);
            if (vacationWeeks <= 0)
            {
                logger.LogWarning(
                    "Vacation-week waitlist record {WaitListCtrlNbr} has non-vacation absence code {AbsenceCode}; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value,
                    absenceCode.Code);
                continue;
            }

            var allowance = await waitListAllowancePolicyRepository.GetByCraftTypeCodeYearAsync(
                waitListRecord.CraftCtrlNbr,
                AbsenceRequestWaitListType.VacationWeek,
                "VW",
                targetDate.Year);

            if (allowance is null || !allowance.IsEnabled)
            {
                logger.LogWarning(
                    "Vacation-week waitlist record {WaitListCtrlNbr} has no enabled allowance policy for craft {CraftCtrlNbr} year {Year}; leaving on waitlist.",
                    waitListRecord.CtrlNbr.Value,
                    waitListRecord.CraftCtrlNbr.Value,
                    targetDate.Year);
                continue;
            }

            var rangeStartUtc = targetDate.AddDays(-35);
            var rangeEndUtc = targetDate.AddDays((vacationWeeks * 7) + 1);
            var existingRequests = await uow.AbsenceRequests.GetByDateRangeAsync(
                craft.DynamicGroupCtrlNbr,
                rangeStartUtc,
                rangeEndUtc,
                includeAllStatuses: true,
                craftCtrlNbr: waitListRecord.CraftCtrlNbr,
                departmentCtrlNbr: waitListRecord.DepartmentCtrlNbr,
                ct: ct);

            var codeCache = new Dictionary<ControlNumber, string>();
            var maxAssignments = Math.Min(allowance.MaxAssignments, departmentPolicy.VacationWeekMaxAssignments);
            var capacityAvailable = await IsVacationWeekCapacityAvailableAsync(
                existingRequests,
                targetDate,
                vacationWeeks,
                maxAssignments,
                codeCache,
                ct);

            if (capacityAvailable)
                eligible.Add(waitListRecord);
        }

        return eligible
            .OrderBy(r => r.EntryUtc)
            .ThenBy(r => r.CtrlNbr)
            .ToList();
    }

    private async Task<bool> IsVacationWeekCapacityAvailableAsync(
        IReadOnlyCollection<AbsenceRequest> existingRequests,
        DateTime requestedStartDateUtc,
        int requestedWeeks,
        int maxAssignments,
        Dictionary<ControlNumber, string> codeCache,
        CancellationToken ct)
    {
        for (var i = 0; i < requestedWeeks; i++)
        {
            var weekDate = requestedStartDateUtc.AddDays(i * 7).Date;
            var assignedCount = 0;

            foreach (var request in existingRequests)
            {
                if (request.DeniedAtUtc.HasValue || request.CancelledAtUtc.HasValue)
                    continue;

                if (request.AbsenceCodeCtrlNbr is null)
                    continue;

                if (!await CoversVacationWeekDateAsync(request, weekDate, codeCache, ct))
                    continue;

                assignedCount++;
            }

            if (assignedCount >= maxAssignments)
                return false;
        }

        return true;
    }

    private async Task<bool> CoversVacationWeekDateAsync(
        AbsenceRequest request,
        DateTime weekDateUtc,
        IDictionary<ControlNumber, string> codeCache,
        CancellationToken ct)
    {
        if (request.AbsenceCodeCtrlNbr is null)
            return false;

        var codeCtrlNbr = request.AbsenceCodeCtrlNbr;
        if (!codeCache.TryGetValue(codeCtrlNbr, out var code))
        {
            var absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(codeCtrlNbr, ct);
            if (absenceCode is null)
                return false;

            code = absenceCode.Code;
            codeCache[codeCtrlNbr] = code;
        }

        var weeks = ResolveVacationWeeks(code);
        if (weeks <= 0)
            return false;

        var requestDate = DateTime.SpecifyKind(request.ScheduledStartUtc, DateTimeKind.Utc).Date;
        for (var i = 0; i < weeks; i++)
        {
            if (requestDate.AddDays(i * 7) == weekDateUtc)
                return true;
        }

        return false;
    }

    private static int ResolveVacationWeeks(string absenceCode)
    {
        if (string.IsNullOrWhiteSpace(absenceCode))
            return 0;

        var normalized = absenceCode.Trim().ToUpperInvariant();
        if (!normalized.StartsWith('V') || normalized == "VD")
            return 0;

        if (normalized.Length < 2)
            return 0;

        return int.TryParse(normalized[1].ToString(), out var weeks)
            ? Math.Max(weeks, 0)
            : 0;
    }
}
