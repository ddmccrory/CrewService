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
        var schedules = await workerScheduleRepository.GetEnabledByTypeAsync("WaitListReassignment", ct);
        if (schedules.Count == 0)
            return 0;

        var assignedCount = 0;

        var compCandidates = await evaluator.EvaluateCompensableDayAsync(targetDate, ct);
        foreach (var candidate in compCandidates)
        {
            if (await AssignFromWaitListAsync(candidate.CtrlNbr, targetDate, ct))
                assignedCount++;
        }

        var vacationCandidates = await evaluator.EvaluateVacationWeekAsync(targetDate, ct);
        foreach (var candidate in vacationCandidates)
        {
            if (await AssignFromWaitListAsync(candidate.CtrlNbr, targetDate, ct))
                assignedCount++;
        }

        return assignedCount;
    }

    private async Task<bool> AssignFromWaitListAsync(
        ControlNumber waitListCtrlNbr,
        DateTime targetDateUtc,
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
            targetDateUtc.AddMinutes(1),
            endUtc: null,
            waitListRecord.AbsenceCodeCtrlNbr,
            reasonCode: "MARKOFF",
            isSystemGenerated: true,
            notes: notes,
            approvedByCtrlNbr: ControlNumber.Create(AbsenceRequestService.SystemApprovalOfficerCtrlNbr),
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
