using CrewService.Application.Modules.UserAccount;
using CrewService.Application.Staffing;
using CrewService.Application.Time;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Employees;

public sealed class EmployeeAppService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IUserAccountService userAccountService,
    ICurrentUserService currentUserService,
    IWorkAreaClock workAreaClock,
    IEmployeeOnDutyQueryService onDutyQueryService)
{
    // ── Employee CRUD ────────────────────────────────────────────────────────

    public async Task<List<Employee>> GetAllAsync(
        ControlNumber? clientCtrlNbr = null, int pageNumber = 0, int pageSize = 0,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        if (clientCtrlNbr is not null)
            return await uow.Employees.GetListByClientCtrlNbrAsync(clientCtrlNbr);
        if (pageSize > 0)
            return await uow.Employees.GetAllAsync(pageNumber, pageSize, ct);
        return await uow.Employees.GetAllAsync(ct);
    }

    public async Task<Employee> GetAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Employees.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {ctrlNbr.Value} not found.");
    }

    public async Task<Employee?> GetByNumberAsync(string employeeNumber, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Employees.GetByEmployeeNumberAsync(employeeNumber);
    }

    public async Task<Employee> CreateAsync(
        ControlNumber clientCtrlNbr, string email, string employeeNumber,
        string socialSecurityNumber, Gender gender, Race race,
        DateTime birthDate, DateTime employmentDate, ControlNumber employmentStatusCtrlNbr,
        string? driversLicenseNumber = null, string? issuingState = null, MaritalStatus? maritalStatus = null,
        string? firstName = null, string? middleName = null, string? lastName = null,
        bool sendInvitation = true,
        CancellationToken ct = default)
    {
        // Step 1 — Create Identity user (idempotent: reuse if already exists)
        var existingUser = await userAccountService.FindByEmailAsync(email);
        string userId;
        if (existingUser is not null)
        {
            userId = existingUser.Id;
        }
        else
        {
            var (createResult, newUserId) = await userAccountService.CreateWithoutPasswordAsync(email);
            if (!createResult.Succeeded)
                throw new InvalidOperationException($"Failed to create user account: {string.Join("; ", createResult.Errors)}");
            userId = newUserId;
        }

        // Step 2 — Build employee aggregate (after UoW so parentName is available)
        var invitedByUserId = currentUserService.GetUserId().ToString();
        var invitedByUserName = currentUserService.GetUserName();

        // Step 3 — Persist employee and update name atomically in one transaction
        await using var uow = await uowFactory.CreateAsync(
            new OrchestrationUnitOfWorkOptions { SuppressReactor = !sendInvitation },
            ct);

        var parent = await uow.Parents.GetByCtrlNbrAsync(clientCtrlNbr)
            ?? throw new InvalidOperationException($"Client {clientCtrlNbr.Value} not found.");
        var parentName = parent.Name.Value;

        var employee = Employee.Create(
            clientCtrlNbr, userId, employeeNumber, socialSecurityNumber,
            gender, race, birthDate, employmentDate, employmentStatusCtrlNbr,
            email, invitedByUserId, invitedByUserName, parentName);

        if (!string.IsNullOrEmpty(driversLicenseNumber))
            employee.Update(driversLicenseNumber: driversLicenseNumber, issuingState: issuingState, maritalStatus: maritalStatus);

        var emailTypes = await uow.EmailAddressTypes.GetByClientCtrlNbrAsync(clientCtrlNbr);
        var emailType = emailTypes.FirstOrDefault()
            ?? throw new InvalidOperationException($"No email address types configured for client {clientCtrlNbr.Value}.");
        employee.AddEmailAddress(email, emailType.CtrlNbr);

        uow.Employees.Add(employee);

        // Step 4 — Update Identity user name in the same transaction (shared connection, no lock contention)
        if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
        {
            var fn = firstName?.Trim() ?? string.Empty;
            var mn = middleName?.Trim();
            var ln = lastName?.Trim() ?? string.Empty;
            var fullName = string.Join(" ", new[] { fn, ln }.Where(s => !string.IsNullOrEmpty(s)));
            var fullNameLnf = string.IsNullOrEmpty(ln) ? fn : $"{ln}, {fn}";
            await uow.UpdateUserProfileAsync(userId, fn, mn, ln, fullName, fullNameLnf, employeeNumber, ct);
        }

        await uow.CommitAsync(ct);

        return employee;
    }

    public async Task<Employee> UpdateAsync(
        ControlNumber ctrlNbr,
        string? driversLicenseNumber = null, string? issuingState = null, MaritalStatus? maritalStatus = null,
        bool? allowFmlaMarkOff = null, bool? callForOvertime = null, bool? processPayroll = null,
        bool? tieUpOffProperty = null, Gender? gender = null, Race? race = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {ctrlNbr.Value} not found.");
        employee.Update(driversLicenseNumber, issuingState, maritalStatus, allowFmlaMarkOff, callForOvertime, processPayroll, tieUpOffProperty, gender, race);
        uow.Employees.Update(employee);
        await uow.CommitAsync(ct);
        return employee;
    }

    public async Task<Employee> DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {ctrlNbr.Value} not found.");
        uow.Employees.Remove(employee);
        await uow.CommitAsync(ct);
        return employee;
    }

    // ── Address Operations ───────────────────────────────────────────────────

    public async Task<(Employee Employee, Address Address)> AddAddressAsync(
        ControlNumber employeeCtrlNbr, string address1, string city, string state, string zipCode,
        long addressTypeCtrlNbr, string? address2 = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr.Value} not found.");
        var address = employee.AddAddress(address1, city, state, zipCode, addressTypeCtrlNbr, address2);
        uow.Employees.Update(employee);
        await uow.CommitAsync(ct);
        return (employee, address);
    }

    public async Task<(Employee Employee, Address Address)> UpdateAddressAsync(
        ControlNumber employeeCtrlNbr, ControlNumber addressCtrlNbr,
        string? address1 = null, string? address2 = null, string? city = null,
        string? state = null, string? zipCode = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr.Value} not found.");
        var address = employee.Addresses.FirstOrDefault(a => a.CtrlNbr == addressCtrlNbr)
            ?? throw new KeyNotFoundException($"Address {addressCtrlNbr.Value} not found.");
        address.Update(address1, address2, city, state, zipCode);
        uow.Employees.Update(employee);
        await uow.CommitAsync(ct);
        return (employee, address);
    }

    public async Task DeleteAddressAsync(
        ControlNumber employeeCtrlNbr, ControlNumber addressCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr.Value} not found.");
        employee.RemoveAddress(addressCtrlNbr);
        uow.Employees.Update(employee);
        await uow.CommitAsync(ct);
    }

    // ── Phone Number Operations ──────────────────────────────────────────────

    public async Task<(Employee Employee, PhoneNumber Phone)> AddPhoneNumberAsync(
        ControlNumber employeeCtrlNbr, string number, int callingOrder, bool dialOne,
        long phoneTypeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr.Value} not found.");
        var phone = employee.AddPhoneNumber(number, callingOrder, dialOne, phoneTypeCtrlNbr);
        uow.Employees.Update(employee);
        await uow.CommitAsync(ct);
        return (employee, phone);
    }

    public async Task<(Employee Employee, PhoneNumber Phone)> UpdatePhoneNumberAsync(
        ControlNumber employeeCtrlNbr, ControlNumber phoneCtrlNbr,
        string? number = null, int? callingOrder = null, bool? dialOne = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr.Value} not found.");
        var phone = employee.PhoneNumbers.FirstOrDefault(p => p.CtrlNbr == phoneCtrlNbr)
            ?? throw new KeyNotFoundException($"Phone number {phoneCtrlNbr.Value} not found.");
        phone.Update(number, callingOrder, dialOne);
        uow.Employees.Update(employee);
        await uow.CommitAsync(ct);
        return (employee, phone);
    }

    public async Task DeletePhoneNumberAsync(
        ControlNumber employeeCtrlNbr, ControlNumber phoneCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr.Value} not found.");
        employee.RemovePhoneNumber(phoneCtrlNbr);
        uow.Employees.Update(employee);
        await uow.CommitAsync(ct);
    }

    // ── Email Address Operations ─────────────────────────────────────────────

    public async Task<(Employee Employee, EmailAddress Email)> AddEmailAddressAsync(
        ControlNumber employeeCtrlNbr, string email, long emailTypeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr.Value} not found.");
        var emailAddress = employee.AddEmailAddress(email, emailTypeCtrlNbr);
        uow.Employees.Update(employee);
        await uow.CommitAsync(ct);
        return (employee, emailAddress);
    }

    public async Task<(Employee Employee, EmailAddress Email)> UpdateEmailAddressAsync(
        ControlNumber employeeCtrlNbr, ControlNumber emailCtrlNbr,
        string? email = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr.Value} not found.");
        var emailAddress = employee.EmailAddresses.FirstOrDefault(e => e.CtrlNbr == emailCtrlNbr)
            ?? throw new KeyNotFoundException($"Email address {emailCtrlNbr.Value} not found.");
        emailAddress.Update(email);
        uow.Employees.Update(employee);
        await uow.CommitAsync(ct);
        return (employee, emailAddress);
    }

    public async Task DeleteEmailAddressAsync(
        ControlNumber employeeCtrlNbr, ControlNumber emailCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr.Value} not found.");
        employee.RemoveEmailAddress(emailCtrlNbr);
        uow.Employees.Update(employee);
        await uow.CommitAsync(ct);
    }

    // ── Batch Lookup ─────────────────────────────────────────────────────────

    public async Task<List<Employee>> GetByCtrlNbrsAsync(
        IEnumerable<ControlNumber> ctrlNbrs, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Employees.GetByCtrlNbrsAsync(ctrlNbrs, ct);
    }
    // ── Work Profile ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all data needed by the EmployeeDetail work-profile panel in a single UoW.
    /// </summary>
    public async Task<EmployeeWorkProfileResult> GetEmployeeWorkProfileAsync(
        ControlNumber employeeCtrlNbr,
        ControlNumber? parentCtrlNbr,
        ControlNumber? railroadCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Employee {employeeCtrlNbr.Value} not found.");

        // Role — find the matching UserParentAssignment
        var userAssignments = await uow.UserParentAssignments.GetByUserIdAsync(employee.UserId);
        var assignment = railroadCtrlNbr is not null
            ? userAssignments.FirstOrDefault(a => a.RailroadCtrlNbr == railroadCtrlNbr)
              ?? userAssignments.FirstOrDefault(a => parentCtrlNbr is not null && a.ParentCtrlNbr == parentCtrlNbr)
            : userAssignments.FirstOrDefault(a => parentCtrlNbr is not null && a.ParentCtrlNbr == parentCtrlNbr);
        var role = assignment?.Role ?? string.Empty;

        // Employment status name
        var empStatus = await uow.EmploymentStatuses.GetByCtrlNbrAsync(employee.EmploymentStatusCtrlNbr, ct);

        // Seniority entries with roster names
        var seniorityEntries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        var rosterIds = seniorityEntries.Select(s => s.RosterCtrlNbr).Distinct().ToList();
        var rosters = await uow.Rosters.GetByCtrlNbrsAsync(rosterIds, ct);
        var rosterMap = rosters.ToDictionary(r => r.CtrlNbr);

        var stateIds = seniorityEntries.Select(s => s.SeniorityStateCtrlNbr).Distinct().ToList();
        var states = new Dictionary<ControlNumber, string>();
        foreach (var id in stateIds)
        {
            var state = await uow.SeniorityStates.GetByCtrlNbrAsync(id, ct);
            if (state is not null) states[id] = state.StateDescription;
        }

        // Current position assignments for this employee — keyed by StaffablePositionCtrlNbr
        var positionAssignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        var positionMap = new Dictionary<ControlNumber, (string Name, string Type, DateTime AssignedDateUtc, bool AllowBulletinBidding)>();
        foreach (var pa in positionAssignments)
        {
            var pos = await uow.StaffablePositions.GetByCtrlNbrAsync(pa.StaffablePositionCtrlNbr, ct);
            if (pos is null) continue;

            string posName;
            bool allowBidding = true; // crew positions always allow bidding
            if (pos.PositionType == StaffablePositionType.Board)
            {
                // Board position: resolve display name from the RosterBoard that owns this position
                RosterBoard? board = null;
                if (pa.AssignmentSourceCtrlNbr is not null)
                    board = await uow.RosterBoards.GetByPositionCtrlNbrAsync(pa.AssignmentSourceCtrlNbr, ct);
                posName = board?.Name ?? pos.PositionType;
                allowBidding = board?.AllowBulletinBidding ?? true;
            }
            else
            {
                // Crew position (Direct, BulletinAssignment, SeniorityMove, etc.):
                // resolve Crew.Name / CraftRole.Name
                CrewPosition? crewPos = null;
                if (pa.AssignmentSourceCtrlNbr is not null)
                    crewPos = await uow.CrewPositions.GetByCtrlNbrAsync(pa.AssignmentSourceCtrlNbr, ct);
                crewPos ??= await uow.CrewPositions.GetByStaffablePositionAsync(pa.StaffablePositionCtrlNbr);

                if (crewPos is not null)
                {
                    var crew      = await uow.Crews.GetByCtrlNbrAsync(crewPos.CrewCtrlNbr, ct);
                    var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPos.CraftRoleCtrlNbr, ct);
                    var crewName  = crew?.Name ?? string.Empty;
                    var roleName  = craftRole?.Name ?? string.Empty;
                    posName = (crewName, roleName) switch
                    {
                        ({ Length: > 0 }, { Length: > 0 }) => $"{crewName} / {roleName}",
                        ({ Length: > 0 }, _)               => crewName,
                        (_, { Length: > 0 })               => roleName,
                        _                                  => pos.PositionType
                    };
                }
                else
                {
                    posName = pos.PositionType;
                }
            }

            positionMap[pa.StaffablePositionCtrlNbr] = (posName, pos.PositionType, pa.AssignedDateUtc, allowBidding);
        }

        // Map seniority entries to the result
        var seniorityResults = seniorityEntries.Select(s =>
        {
            rosterMap.TryGetValue(s.RosterCtrlNbr, out var roster);
            states.TryGetValue(s.SeniorityStateCtrlNbr, out var stateName);
            // Use the first position assignment found for the employee on this roster
            string posName = string.Empty, posType = string.Empty, posDate = string.Empty;
            var firstPos = positionMap.Values.FirstOrDefault();
            if (firstPos != default)
            {
                posName = firstPos.Name;
                posType = firstPos.Type;
                posDate = firstPos.AssignedDateUtc.ToString("o");
            }
            return new WorkProfileSeniorityEntry(
                s.CtrlNbr, s.RosterCtrlNbr,
                roster?.RosterName ?? string.Empty,
                s.RosterDate.ToString("yyyy-MM-dd"),
                s.Rank, s.SeniorityStateCtrlNbr,
                stateName ?? string.Empty,
                s.LastActiveRoster,
                posName, posType, posDate,
                roster?.CraftCtrlNbr ?? ControlNumber.Create(0));
        }).ToList();

        // Moves and bids
        var moves = await uow.SeniorityMoves.GetByEmployeeAsync(employeeCtrlNbr, ct);
        var bids  = await uow.BulletinBids.GetActiveByEmployeeAsync(employeeCtrlNbr);

        // Project moves, enriching each with the server-computed CanCancel flag so the UI
        // never has to replicate cancel-window business logic, and the resolved target
        // position name so the UI can show where the employee is moving to.
        var policyCache = new Dictionary<ControlNumber, SeniorityMovePolicy?>();
        var targetNameCache = new Dictionary<ControlNumber, string>();
        var moveItems = new List<WorkProfileSeniorityMoveItem>();
        foreach (var m in moves)
        {
            if (!policyCache.TryGetValue(m.CraftCtrlNbr, out var policy))
            {
                policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(m.RailroadCtrlNbr, m.CraftCtrlNbr);
                policyCache[m.CraftCtrlNbr] = policy;
            }

            if (!targetNameCache.TryGetValue(m.TargetPositionCtrlNbr, out var targetName))
            {
                targetName = await StaffablePositionNameResolver.ResolveAsync(uow, m.TargetPositionCtrlNbr, ct);
                targetNameCache[m.TargetPositionCtrlNbr] = targetName;
            }

            moveItems.Add(new WorkProfileSeniorityMoveItem(
                m.CtrlNbr,
                m.CraftCtrlNbr,
                m.TargetPositionCtrlNbr,
                m.DisplacedEmployeeCtrlNbr,
                m.RequestedUtc,
                m.EffectiveUtc,
                m.DaysOnCurrentPosition,
                m.MoveType,
                m.Status,
                m.RejectionReason,
                m.CancellationReason,
                CanCancelMove(m, policy),
                targetName));
        }

        // Enrich bids with bulletin/position info
        var enrichedBids = new List<WorkProfileBulletinBid>();
        foreach (var bid in bids)
        {
            var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(bid.BulletinCtrlNbr, ct);
            string bulletinCode = string.Empty, positionName = string.Empty;
            if (bulletin is not null)
            {
                bulletinCode = bulletin.CtrlNbr.Value.ToString();
                var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
                positionName = vacancy?.TargetName ?? string.Empty;
            }
            enrichedBids.Add(new WorkProfileBulletinBid(
                bid.CtrlNbr, bid.BulletinCtrlNbr,
                bid.Priority, bid.SubmittedUtc,
                bid.Status, bulletinCode, positionName));
        }

        return new EmployeeWorkProfileResult(
            role,
            employee.EmploymentDate.ToString("yyyy-MM-dd"),
            empStatus?.StatusName ?? string.Empty,
            // An employee can bid if they have no board position, or if all their board positions allow bidding.
            // Crew positions never restrict bidding.
            positionMap.Values.All(p => p.AllowBulletinBidding),
            seniorityResults,
            moveItems,
            enrichedBids);
    }

    // ── Employee On-Duty (Work & Staffing open records + On-Duty History) ────

    /// <summary>
    /// Open on-duty records for an employee — those not yet tied up (Scheduled, Called, or OnDuty).
    /// Surfaced on the employee-detail Work &amp; Staffing tab. Mirrors the legacy "Open On Duty
    /// Records" pay-period slice. Times are returned both as UTC and as work-area-localized ISO-8601
    /// strings so the front-end renders wall-clock times without any timezone logic of its own.
    /// </summary>
    public async Task<IReadOnlyList<EmployeeOnDutyRecordItem>> GetOpenOnDutyRecordsAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var records = await uow.OnDutyRecords.GetOpenForEmployeeAsync(employeeCtrlNbr, ct);
        return await EnrichAsync(uow, records, ct);
    }

    /// <summary>
    /// Completed on-duty history for an employee within the requested pay-period window. Surfaced on
    /// the employee-detail On-Duty History tab. Window bounds for the work-period options are derived
    /// from the railroad's configured <see cref="WorkPeriodMode"/> (defaulting to
    /// <see cref="WorkPeriodMode.HalfMonth"/>, the legacy behavior).
    /// </summary>
    public async Task<IReadOnlyList<EmployeeOnDutyRecordItem>> GetOnDutyHistoryAsync(
        ControlNumber employeeCtrlNbr, OnDutyHistoryPeriod period,
        ControlNumber? railroadCtrlNbr = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var mode = await ResolveWorkPeriodModeAsync(uow, railroadCtrlNbr, ct);
        var (startUtc, endUtc) = ResolveHistoryWindow(period, mode, workAreaClock.UtcNow.UtcDateTime);

        var records = await uow.OnDutyRecords.GetForEmployeeInRangeAsync(employeeCtrlNbr, startUtc, endUtc, ct);
        return await EnrichAsync(uow, records, ct);
    }

    /// <summary>
    /// Enriches raw on-duty records with slot display data (assignment, crew, craft, location),
    /// tie-up (off-duty) data, and work-area-localized ISO on/off-duty timestamps.
    /// </summary>
    private async Task<IReadOnlyList<EmployeeOnDutyRecordItem>> EnrichAsync(
        IOrchestrationUnitOfWork uow, IReadOnlyList<OnDutyRecord> records, CancellationToken ct)
    {
        if (records.Count == 0) return [];

        var slotIds = records.Select(r => r.PositionSlotCtrlNbr).Distinct().ToList();
        var slotDisplay = await onDutyQueryService.GetSlotDisplayAsync(slotIds, ct);

        var recordIds = records.Select(r => r.CtrlNbr).ToList();
        var offDuty = await uow.OffDutyRecords.GetByOnDutyRecordsAsync(recordIds, ct);
        var offDutyMap = offDuty
            .GroupBy(o => o.OnDutyRecordCtrlNbr)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.OffDutyTimeUtc).First());

        // Cache resolved timezones so records sharing a work area only resolve once.
        var tzCache = new Dictionary<string, TimeZoneInfo?>(StringComparer.OrdinalIgnoreCase);

        var items = new List<EmployeeOnDutyRecordItem>(records.Count);
        foreach (var r in records)
        {
            slotDisplay.TryGetValue(r.PositionSlotCtrlNbr, out var display);

            var tz = ResolveTimeZone(display?.TimeZoneId, tzCache);

            offDutyMap.TryGetValue(r.CtrlNbr, out var off);
            DateTime? offUtc = off?.OffDutyTimeUtc;

            items.Add(new EmployeeOnDutyRecordItem(
                r.CtrlNbr,
                r.PreviousRestHours,
                display?.AssignmentName ?? string.Empty,
                display?.AssignmentCode ?? string.Empty,
                display?.CrewName ?? string.Empty,
                display?.CraftRoleName ?? string.Empty,
                display?.Location ?? string.Empty,
                r.OnDutyTimeUtc,
                workAreaClock.FormatLocalIso(r.OnDutyTimeUtc, tz),
                offUtc,
                offUtc is null ? string.Empty : workAreaClock.FormatLocalIso(offUtc.Value, tz),
                off?.TotalTimeOnDutyMinutes,
                r.ConsecutiveDays,
                r.IsAssigned,
                r.IsLateCall,
                r.Status.Value));
        }

        return items;
    }

    private TimeZoneInfo? ResolveTimeZone(string? timeZoneId, Dictionary<string, TimeZoneInfo?> cache)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return null;
        if (cache.TryGetValue(timeZoneId, out var cached)) return cached;
        var tz = workAreaClock.ResolveTimeZone(timeZoneId);
        cache[timeZoneId] = tz;
        return tz;
    }

    /// <summary>
    /// Resolves the railroad's configured <see cref="WorkPeriodMode"/>, falling back to
    /// <see cref="WorkPeriodMode.HalfMonth"/> (legacy behavior) when the railroad is unknown or
    /// unconfigured.
    /// </summary>
    private static async Task<WorkPeriodMode> ResolveWorkPeriodModeAsync(
        IOrchestrationUnitOfWork uow, ControlNumber? railroadCtrlNbr, CancellationToken ct)
    {
        if (railroadCtrlNbr is null) return WorkPeriodMode.HalfMonth;
        var railroad = await uow.DynamicGroups.GetByCtrlNbrAsync(railroadCtrlNbr, ct);
        return railroad?.WorkPeriodMode ?? WorkPeriodMode.HalfMonth;
    }

    /// <summary>
    /// Computes the [start, end) UTC bounds for a completed on-duty history window, given the
    /// railroad's <see cref="WorkPeriodMode"/>. Mirrors the legacy pay-period slices: current and
    /// previous work period, current and previous calendar month, and year-to-date.
    /// </summary>
    private static (DateTime StartUtc, DateTime EndUtc) ResolveHistoryWindow(
        OnDutyHistoryPeriod period, WorkPeriodMode mode, DateTime nowUtc)
    {
        var today = nowUtc.Date;

        switch (period)
        {
            case OnDutyHistoryPeriod.CurrentWorkPeriod:
            {
                var (start, end) = CurrentWorkPeriod(mode, today);
                return (start, end);
            }
            case OnDutyHistoryPeriod.PreviousWorkPeriod:
            {
                var (currentStart, _) = CurrentWorkPeriod(mode, today);
                var (prevStart, _) = CurrentWorkPeriod(mode, currentStart.AddDays(-1));
                return (prevStart, currentStart);
            }
            case OnDutyHistoryPeriod.CurrentMonth:
            {
                var start = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                return (start, start.AddMonths(1));
            }
            case OnDutyHistoryPeriod.PreviousMonth:
            {
                var currentMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                return (currentMonthStart.AddMonths(-1), currentMonthStart);
            }
            case OnDutyHistoryPeriod.YearToDate:
            {
                var start = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return (start, nowUtc.AddDays(1).Date);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown on-duty history period.");
        }
    }

    /// <summary>
    /// Computes the [start, end) UTC bounds of the work period that contains <paramref name="onDate"/>,
    /// honoring the railroad's <see cref="WorkPeriodMode"/>.
    /// </summary>
    private static (DateTime StartUtc, DateTime EndUtc) CurrentWorkPeriod(WorkPeriodMode mode, DateTime onDate)
    {
        var day = new DateTime(onDate.Year, onDate.Month, onDate.Day, 0, 0, 0, DateTimeKind.Utc);

        if (mode == WorkPeriodMode.Monthly)
        {
            var start = new DateTime(day.Year, day.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return (start, start.AddMonths(1));
        }

        if (mode == WorkPeriodMode.Weekly)
        {
            var start = day.AddDays(-(int)day.DayOfWeek);
            return (start, start.AddDays(7));
        }

        if (mode == WorkPeriodMode.BiWeekly)
        {
            // Anchor bi-weekly periods to the start of the calendar year for determinism.
            var yearStart = new DateTime(day.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodIndex = (int)((day - yearStart).TotalDays / 14);
            var start = yearStart.AddDays(periodIndex * 14);
            return (start, start.AddDays(14));
        }

        // Default: HalfMonth — 1st–15th and 16th–end-of-month (legacy behavior).
        if (day.Day <= 15)
        {
            var start = new DateTime(day.Year, day.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return (start, new DateTime(day.Year, day.Month, 16, 0, 0, 0, DateTimeKind.Utc));
        }
        else
        {
            var start = new DateTime(day.Year, day.Month, 16, 0, 0, 0, DateTimeKind.Utc);
            return (start, start.AddDays(-15).AddMonths(1));
        }
    }

    /// <summary>
    /// Mirrors the cancel rules enforced by <c>PoliciesService.CancelSeniorityMoveAsync</c>:
    /// completed/cancelled moves can never be cancelled; an Approved move with an effective
    /// time cannot be cancelled once inside the policy's cancel window.
    /// </summary>
    private static bool CanCancelMove(SeniorityMove move, SeniorityMovePolicy? policy)
    {
        if (move.Status == SeniorityMoveStatus.Completed || move.Status == SeniorityMoveStatus.Cancelled
            || move.Status == SeniorityMoveStatus.Rejected)
            return false;

        if (move.Status == SeniorityMoveStatus.Approved && move.EffectiveUtc.HasValue
            && policy is not null && policy.CancelHours > 0)
        {
            var cancelDeadline = move.EffectiveUtc.Value.AddHours(-policy.CancelHours);
            if (DateTime.UtcNow > cancelDeadline)
                return false;
        }

        return true;
    }
}