using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using CrewService.Application.BackgroundWorkers;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.AbsenceVacancy;

public sealed class AbsenceWaitListReassignmentProcessor(
    AbsenceWaitListReassignmentEvaluator evaluator,
    AbsenceRequestService absenceRequestService,
    IWorkerScheduleRepository workerScheduleRepository,
    IAbsenceRequestWaitListRecordRepository waitListRecordRepository,
    IAbsenceRequestWaitListLinkRepository waitListLinkRepository,
    ILogger<AbsenceWaitListReassignmentProcessor> logger)
{
    public async Task<int> ProcessAsync(DateTime requestDateUtc, CancellationToken ct = default)
    {
        var targetDate = DateTime.SpecifyKind(requestDateUtc, DateTimeKind.Utc).Date;
        var promotedStartUtc = targetDate.AddMinutes(1);
        return await ProcessInternalAsync(targetDate, promotedStartUtc, requireEnabledSchedule: true, ct);
    }

    public async Task<int> ProcessImmediateAsync(DateTime promotedStartUtc, CancellationToken ct = default)
    {
        var promotedStart = DateTime.SpecifyKind(promotedStartUtc, DateTimeKind.Utc);
        var targetDate = promotedStart.Date;
        return await ProcessInternalAsync(targetDate, promotedStart, requireEnabledSchedule: false, ct);
    }

    private async Task<int> ProcessInternalAsync(
        DateTime targetDate,
        DateTime promotedStartUtc,
        bool requireEnabledSchedule,
        CancellationToken ct)
    {
        if (requireEnabledSchedule)
        {
            var schedules = await workerScheduleRepository.GetEnabledByTypeAsync("WaitListReassignment", ct);
            if (schedules.Count == 0)
                return 0;
        }

        var assignedCount = 0;

        var compCandidates = await evaluator.EvaluateCompensableDayAsync(targetDate, ct);
        foreach (var candidate in compCandidates)
        {
            if (await AssignFromWaitListAsync(candidate.CtrlNbr, targetDate, promotedStartUtc, ct))
                assignedCount++;
        }

        var vacationCandidates = await evaluator.EvaluateVacationWeekAsync(targetDate, ct);
        foreach (var candidate in vacationCandidates)
        {
            if (await AssignFromWaitListAsync(candidate.CtrlNbr, targetDate, promotedStartUtc, ct))
                assignedCount++;
        }

        return assignedCount;
    }

    private async Task<bool> AssignFromWaitListAsync(
        ControlNumber waitListCtrlNbr,
        DateTime targetDateUtc,
        DateTime promotedStartUtc,
        CancellationToken ct)
    {
        var waitListRecord = await waitListRecordRepository.GetByCtrlNbrAsync(waitListCtrlNbr, ct);
        if (waitListRecord is null)
            return false;

        if (waitListRecord.AssignedAtUtc.HasValue)
            return false;

        var notes = string.Format(
            "Waitlist request was assigned for {0}.",
            targetDateUtc.ToString("MM/dd/yyyy"));

        var submitResult = await absenceRequestService.SubmitWithCodeAsync(
            waitListRecord.EmployeeCtrlNbr,
            promotedStartUtc,
            endUtc: null,
            waitListRecord.AbsenceCodeCtrlNbr,
            reasonCode: "MARKOFF",
            isSystemGenerated: true,
            notes: notes,
            approvedByCtrlNbr: waitListRecord.EmployeeCtrlNbr,
            autoMarkOffOnApproval: false,
            markOffStartUtc: DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            bypassWaitList: true);

        var createdRequest = submitResult.AbsenceRequest
            ?? throw new InvalidOperationException("Waitlist reassignment expected an absence request but none was created.");

        waitListRecord.MarkAssigned(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc), notes);
        await waitListRecordRepository.UpdateAsync(waitListRecord, ct);

        await absenceRequestService.NotifyWaitListPromotionAsync(createdRequest, waitListRecord, ct);

        var link = AbsenceRequestWaitListLink.Create(createdRequest.CtrlNbr, waitListRecord.CtrlNbr);
        await waitListLinkRepository.AddAsync(link, ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Assigned waitlist record {WaitListCtrlNbr} to absence request {AbsenceRequestCtrlNbr}.",
                waitListCtrlNbr.Value,
                createdRequest.CtrlNbr.Value);
        }

        return true;
    }
}
