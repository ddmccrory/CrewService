using CrewService.Application.Qualifications;
using CrewService.Application.Policies;
using CrewService.Application.DailyOperations;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.RosterBoardOps;

/// <summary>
/// Encapsulates the new hire onboarding process:
///   1. Creates a seniority entry on the craft Training roster
///   2. Creates a Pending FRA certification for the craft regulatory qualification
///   3. Places the employee on the New Hires board (associated with the Training roster)
///   4. Auto-assigns all required qualifications for the craft (Pending -- cert not yet active)
///
/// Steps 1-3 execute in a single atomic transaction via IOrchestrationUnitOfWork.
/// Step 4 runs post-commit via QualificationReactiveService.
/// </summary>
public sealed class NewHireService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    QualificationReactiveService qualificationReactiveService,
    CallSheetVacancyProjectionSyncService vacancyProjectionSyncService,
    IncumbentAssignmentPath? incumbentAssignmentPath = null)
{
    private readonly IncumbentAssignmentPath _incumbentAssignmentPath = incumbentAssignmentPath ?? new(new(), vacancyProjectionSyncService);

    public async Task OnboardAsync(
        ControlNumber employeeCtrlNbr,
        ControlNumber craftCtrlNbr,
        ControlNumber trainingRosterCtrlNbr,
        ControlNumber seniorityStateCtrlNbr,
        DateTime hireDate,
        ControlNumber? regulatoryQualificationCtrlNbr,
        string certificationType = "Yard",
        int recertificationIntervalMonths = 36,
        int rank = 1,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        // 1. Seniority entry on the Training roster
        var seniority = Seniority.Create(
            trainingRosterCtrlNbr,
            employeeCtrlNbr,
            lastActiveRoster: false,
            rosterDate: hireDate.Date,
            rank: rank,
            seniorityStateCtrlNbr: seniorityStateCtrlNbr,
            canTrain: false);
        uow.Seniority.Add(seniority);

        // 2. Pending FRA certification -- activated when training is complete
        if (regulatoryQualificationCtrlNbr is not null)
        {
            var cert = EmployeeCertification.Create(
                employeeCtrlNbr,
                regulatoryQualificationCtrlNbr,
                certificationType,
                DateOnly.FromDateTime(hireDate.Date),
                recertificationIntervalMonths);
            uow.EmployeeCertifications.Add(cert);
        }

        // 3. Place on the New Hires board (associated with the Training roster)
        var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(craftCtrlNbr, ct);
        var newHireBoard = boards.FirstOrDefault(b => b.BoardType == BoardType.NewHire);
        if (newHireBoard is not null)
        {
            var staffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
            uow.StaffablePositions.Add(staffablePosition);
            var nextOrder = newHireBoard.Positions.Count + 1;
            var position = newHireBoard.AddPosition(employeeCtrlNbr, nextOrder, staffablePosition.CtrlNbr);
            await _incumbentAssignmentPath.AssignAsync(
                uow,
                staffablePosition.CtrlNbr,
                employeeCtrlNbr,
                PositionAssignmentType.Board,
                assignmentSourceCtrlNbr: position.CtrlNbr,
                assignedDateUtc: null,
                cancellationReason: IncumbentAssignmentPath.DefaultCancellationReason,
                excludeMoveCtrlNbr: null,
                ct);
            uow.RosterBoards.Update(newHireBoard);
        }

        await uow.CommitAsync(ct);

        // 4. Auto-assign required qualifications post-commit (all Pending -- cert not active yet)
        await qualificationReactiveService.HandleAddedToRosterAsync(employeeCtrlNbr, craftCtrlNbr, ct);
    }
}