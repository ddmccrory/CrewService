using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using CrewService.Application.Absence;

namespace CrewService.Application.AbsenceVacancy;

public sealed class AbsenceRequestService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IAbsenceCodeRepository absenceCodeRepository,
    IAbsenceApprovalPolicyResolver approvalPolicyResolver,
    BackgroundWorkers.IAbsenceMarkOffSignal absenceMarkOffSignal)
{
    public const long SystemApprovalOfficerCtrlNbr = 1;

    public async Task<AbsenceRequest> SubmitAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc, string reasonCode, string? notes)
    {
        var absence = AbsenceRequest.Create(employeeCtrlNbr, startUtc, endUtc, reasonCode, notes);
        await using var uow = await uowFactory.CreateAsync();
        uow.AbsenceRequests.Add(absence);
        await uow.CommitAsync();
        return absence;
    }

    public async Task<AbsenceRequest> SubmitWithCodeAsync(
        ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc,
        ControlNumber absenceCodeCtrlNbr, string reasonCode,
        bool isSystemGenerated = false, string? notes = null,
        ControlNumber? approvedByCtrlNbr = null,
        bool autoMarkOffOnApproval = false,
        DateTime? markOffStartUtc = null)
    {
        var absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(absenceCodeCtrlNbr)
            ?? throw new KeyNotFoundException($"Absence code {absenceCodeCtrlNbr.Value} not found.");
        var approvalPolicy = await approvalPolicyResolver.ResolveAsync(absenceCode);

        var absence = AbsenceRequest.CreateWithCode(
            employeeCtrlNbr, startUtc, endUtc, absenceCodeCtrlNbr, reasonCode,
            isSystemGenerated, notes, autoMarkOffOnApproval);

        var markOffReferenceUtc = DateTime.SpecifyKind(markOffStartUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        if (approvalPolicy.Level == AbsenceApprovalLevel.Automatic)
        {
            var systemOfficerCtrlNbr = ControlNumber.Create(SystemApprovalOfficerCtrlNbr);
            absence.AddApproval(systemOfficerCtrlNbr).Approve("Automatically approved by system policy.");
            absence.Approve(systemOfficerCtrlNbr);
        }
        else if (approvedByCtrlNbr is not null)
        {
            absence.AddApproval(approvedByCtrlNbr).Approve("Approved during request creation.");
            absence.Approve(approvedByCtrlNbr);
        }

        if (autoMarkOffOnApproval
            && string.Equals(absence.Status, "APPROVED", StringComparison.OrdinalIgnoreCase)
            && ShouldAutoMarkOffImmediately(absence.ScheduledStartUtc, markOffReferenceUtc, approvalPolicy))
        {
            absence.Exercise(markOffReferenceUtc);
        }

        await using var uow = await uowFactory.CreateAsync();
        uow.AbsenceRequests.Add(absence);
        await uow.CommitAsync();

        if (autoMarkOffOnApproval
            && string.Equals(absence.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            absenceMarkOffSignal.Notify(absence.ScheduledStartUtc);
        }

        return absence;
    }

    public async Task<AbsenceApprovalPolicy> ResolveApprovalPolicyAsync(ControlNumber absenceCodeCtrlNbr, CancellationToken ct = default)
    {
        var absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(absenceCodeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Absence code {absenceCodeCtrlNbr.Value} not found.");

        return await approvalPolicyResolver.ResolveAsync(absenceCode, ct);
    }

    public async Task<List<AbsenceApprovalOfficer>> GetApprovalOfficersAsync(
        ControlNumber parentCtrlNbr,
        ControlNumber railroadCtrlNbr,
        AbsenceApprovalLevel level,
        CancellationToken ct = default)
    {
        if (level == AbsenceApprovalLevel.Automatic)
        {
            return
            [
                new AbsenceApprovalOfficer(
                    SystemApprovalOfficerCtrlNbr,
                    "SYSTEM",
                    "SYSTEM",
                    "SYSTEM",
                    null)
            ];
        }

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var assignments = await uow.UserParentAssignments.GetByParentCtrlNbrAsync(parentCtrlNbr);

        var scopedAssignments = assignments
            .Where(a => a.RailroadCtrlNbr is null || a.RailroadCtrlNbr == railroadCtrlNbr)
            .ToList();

        var distinctRoleNames = scopedAssignments
            .Select(a => a.Role)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var roleMap = new Dictionary<string, CrewService.Domain.Modules.Authorization.Role>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in distinctRoleNames)
        {
            var role = await uow.Roles.GetByNameAsync(roleName, ct);
            if (role is not null)
                roleMap[roleName] = role;
        }

        scopedAssignments = scopedAssignments
            .Where(a => IsEligibleApproverRole(a.Role, level, roleMap))
            .ToList();

        if (scopedAssignments.Count == 0)
            return [];

        var userIds = scopedAssignments
            .Select(a => a.UserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (userIds.Count == 0)
            return [];

        var officers = new List<AbsenceApprovalOfficer>();
        foreach (var userId in userIds)
        {
            var employee = await uow.Employees.GetByUserIdAsync(userId, ct);
            if (employee is null)
                continue;

            var assignment = scopedAssignments.FirstOrDefault(a => string.Equals(a.UserId, userId, StringComparison.Ordinal));
            var role = assignment?.Role;

            officers.Add(new AbsenceApprovalOfficer(
                employee.CtrlNbr.Value,
                employee.EmployeeNumber,
                employee.EmployeeNumber,
                employee.EmployeeNumber,
                role));
        }

        return officers
            .OrderBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(o => o.OfficerCtrlNbr)
            .ToList();
    }

    public async Task<List<AbsenceRequest>> GetPendingAsync()
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetPendingAsync();
    }

    public async Task<List<AbsenceRequest>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetByEmployeeAsync(employeeCtrlNbr);
    }

    public async Task<List<AbsenceRequest>> GetByDateAsync(
        ControlNumber railroadCtrlNbr,
        DateTime requestDateUtc,
        bool includeAllStatuses,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetByDateAsync(railroadCtrlNbr, requestDateUtc, includeAllStatuses, ct);
    }

    public async Task<List<AbsenceRequest>> GetByDateRangeAsync(
        ControlNumber railroadCtrlNbr,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        bool includeAllStatuses,
        ControlNumber? craftCtrlNbr = null,
        ControlNumber? departmentCtrlNbr = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetByDateRangeAsync(railroadCtrlNbr, rangeStartUtc, rangeEndUtc, includeAllStatuses, craftCtrlNbr, departmentCtrlNbr, ct);
    }

    public async Task<List<AbsenceRequest>> GetOpenAbsencesByRangeAsync(
        ControlNumber railroadCtrlNbr,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        ControlNumber? craftCtrlNbr = null,
        ControlNumber? departmentCtrlNbr = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetOpenAbsencesByRangeAsync(railroadCtrlNbr, rangeStartUtc, rangeEndUtc, craftCtrlNbr, departmentCtrlNbr, ct);
    }

    public async Task<AbsenceRequest> ApproveAsync(ControlNumber ctrlNbr, ControlNumber approvedByCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");

        Domain.Modules.AbsenceVacancy.AbsenceCode? absenceCode = null;
        if (absence.AbsenceCodeCtrlNbr is not null)
        {
            absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(absence.AbsenceCodeCtrlNbr)
                ?? throw new KeyNotFoundException($"Absence code {absence.AbsenceCodeCtrlNbr.Value} not found.");
        }

        var approvalPolicy = absenceCode is null
            ? Application.AbsenceVacancy.AbsenceApprovalPolicy.ForLevel(AbsenceApprovalLevel.CallerManager)
            : await approvalPolicyResolver.ResolveAsync(absenceCode);

        absence.Approve(approvedByCtrlNbr);

        if (absence.AutoMarkOffOnApproval
            && ShouldAutoMarkOffImmediately(absence.ScheduledStartUtc, DateTime.UtcNow, approvalPolicy))
        {
            absence.Exercise(DateTime.UtcNow);
        }

        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();

        if (absence.AutoMarkOffOnApproval
            && string.Equals(absence.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            absenceMarkOffSignal.Notify(absence.ScheduledStartUtc);
        }

        return absence;
    }

    public async Task<AbsenceRequest> DenyAsync(ControlNumber ctrlNbr, ControlNumber deniedByCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");
        absence.Deny(deniedByCtrlNbr);
        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();
        return absence;
    }

    public async Task<int> ExecuteDueAutoMarkOffAsync(DateTime asOfUtc, CancellationToken ct = default)
    {
        var asOf = DateTime.SpecifyKind(asOfUtc, DateTimeKind.Utc);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var due = await uow.AbsenceRequests.GetApprovedAutoMarkOffDueAsync(asOf, ct);
        if (due.Count == 0)
            return 0;

        foreach (var request in due)
        {
            request.Exercise(asOf);
            uow.AbsenceRequests.Update(request);
        }

        await uow.CommitAsync(ct);
        return due.Count;
    }

    public async Task<DateTime?> GetNextApprovedAutoMarkOffStartUtcAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.AbsenceRequests.GetNextApprovedAutoMarkOffStartUtcAsync(ct);
    }

    private static bool ShouldAutoMarkOffImmediately(
        DateTime scheduledStartUtc,
        DateTime nowUtc,
        Application.AbsenceVacancy.AbsenceApprovalPolicy approvalPolicy)
    {
        var start = DateTime.SpecifyKind(scheduledStartUtc, DateTimeKind.Utc);
        var now = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

        if (start <= now)
            return true;

        if (!approvalPolicy.AutoMarkOffIfWithinHoursEnabled)
            return false;

        if (approvalPolicy.AutoMarkOffIfWithinHours <= 0)
            return false;

        var threshold = TimeSpan.FromHours(Math.Max(0, approvalPolicy.AutoMarkOffIfWithinHours));
        return start - now <= threshold;
    }

    public async Task<AbsenceRequest> CancelAsync(ControlNumber ctrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");
        absence.Cancel();
        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();
        return absence;
    }

    public async Task<AbsenceRequest> MarkOffAsync(ControlNumber ctrlNbr, DateTime exercisedUtc)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");

        absence.Exercise(DateTime.SpecifyKind(exercisedUtc, DateTimeKind.Utc));
        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();
        return absence;
    }

    private static bool IsEligibleApproverRole(
        string? roleName,
        AbsenceApprovalLevel level,
        IReadOnlyDictionary<string, CrewService.Domain.Modules.Authorization.Role> roleMap)
    {
        if (string.IsNullOrWhiteSpace(roleName)
            || !roleMap.TryGetValue(roleName, out var role))
            return false;

        if (string.Equals(role.Name, Roles.SystemAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role.Name, Roles.ParentAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role.Name, Roles.RailroadAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (role.Level <= 20)
            return false;

        var isManagerRole = !string.IsNullOrWhiteSpace(role.Description)
            && role.Description.Contains("manage", StringComparison.OrdinalIgnoreCase);

        return level switch
        {
            AbsenceApprovalLevel.CallerManager => true,
            AbsenceApprovalLevel.ManagerOnly => isManagerRole,
            _ => false
        };
    }
}

public sealed record AbsenceApprovalOfficer(
    long OfficerCtrlNbr,
    string DisplayName,
    string FullName,
    string EmployeeNumber,
    string? Role);
