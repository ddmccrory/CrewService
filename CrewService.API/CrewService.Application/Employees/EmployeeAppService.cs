using CrewService.Application.Modules.UserAccount;
using CrewService.Application.DailyOperations;
using CrewService.Application.Authorization;
using CrewService.Application.Staffing;
using CrewService.Application.Time;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
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
    IRequestActorContextResolver actorContextResolver,
    IRequestActorContextPolicy actorContextPolicy,
    IWorkAreaClock workAreaClock,
    IEmployeeOnDutyQueryService onDutyQueryService,
    TieUpService tieUpService)
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

    public async Task<Employee?> GetBySocialSecurityNumberAsync(string socialSecurityNumber, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Employees.GetBySocialSecurityNumberAsync(socialSecurityNumber, ct);
    }

    public async Task<Employee> CreateAsync(
        ControlNumber clientCtrlNbr, string email, string employeeNumber,
        string socialSecurityNumber, Gender gender, Race race,
        DateTime birthDate, DateTime employmentDate, ControlNumber employmentStatusCtrlNbr,
        string? driversLicenseNumber = null, string? issuingState = null, MaritalStatus? maritalStatus = null,
        string? firstName = null, string? middleName = null, string? lastName = null,
        bool sendInvitation = true,
        ControlNumber? railroadCtrlNbr = null,
        CancellationToken ct = default)
    {
        var normalizedFirstName = firstName?.Trim() ?? string.Empty;
        var normalizedLastName = lastName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedFirstName))
            throw new InvalidOperationException("First name is required.");
        if (string.IsNullOrWhiteSpace(normalizedLastName))
            throw new InvalidOperationException("Last name is required.");
        if (string.IsNullOrWhiteSpace(socialSecurityNumber) || !IsValidSocialSecurityNumber(socialSecurityNumber))
            throw new InvalidOperationException("Social Security Number must be in XXX-XX-XXXX format.");
        if (birthDate <= new DateTime(1900, 1, 1))
            throw new InvalidOperationException("Birth date is required.");

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

        var parent = await uow.Parents.GetByCtrlNbrAsync(clientCtrlNbr, ct)
            ?? throw new InvalidOperationException($"Client {clientCtrlNbr.Value} not found.");
        var parentName = parent.Name.Value;

        var employee = Employee.Create(
            clientCtrlNbr, railroadCtrlNbr, userId, employeeNumber, socialSecurityNumber,
            gender, race, birthDate, employmentDate, employmentStatusCtrlNbr,
            email, invitedByUserId, invitedByUserName, parentName);

        if (!string.IsNullOrEmpty(driversLicenseNumber))
            employee.Update(driversLicenseNumber: driversLicenseNumber, issuingState: issuingState, maritalStatus: maritalStatus);

        var emailTypes = await uow.EmailAddressTypes.GetByClientCtrlNbrAsync(clientCtrlNbr);
        var emailType = emailTypes.FirstOrDefault()
            ?? throw new InvalidOperationException($"No email address types configured for client {clientCtrlNbr.Value}.");
        employee.AddEmailAddress(email, emailType.CtrlNbr, isPrimary: true);

        uow.Employees.Add(employee);

        // Step 4 — Update Identity user name in the same transaction (shared connection, no lock contention)
        var mn = middleName?.Trim();
        var fullName = string.Join(" ", new[] { normalizedFirstName, normalizedLastName }.Where(s => !string.IsNullOrEmpty(s)));
        var fullNameLnf = $"{normalizedLastName}, {normalizedFirstName}";
        await uow.UpdateUserProfileAsync(userId, normalizedFirstName, mn, normalizedLastName, fullName, fullNameLnf, employeeNumber, ct);

        await uow.CommitAsync(ct);

        return employee;
    }

    private static bool IsValidSocialSecurityNumber(string value)
    {
        return value.Length == 11
            && char.IsDigit(value[0])
            && char.IsDigit(value[1])
            && char.IsDigit(value[2])
            && value[3] == '-'
            && char.IsDigit(value[4])
            && char.IsDigit(value[5])
            && value[6] == '-'
            && char.IsDigit(value[7])
            && char.IsDigit(value[8])
            && char.IsDigit(value[9])
            && char.IsDigit(value[10]);
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

    public async Task<List<Employee>> GetEligibleAbsenceEmployeesAsync(
        ControlNumber parentCtrlNbr,
        ControlNumber railroadCtrlNbr,
        ControlNumber? craftCtrlNbr = null,
        ControlNumber? departmentCtrlNbr = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var crafts = await uow.Crafts.GetByParentAndRailroadAsync(parentCtrlNbr, railroadCtrlNbr);
        if (crafts.Count == 0)
            return [];

        var scopedCrafts = crafts
            .Where(c => (craftCtrlNbr is null || c.CtrlNbr == craftCtrlNbr)
                        && (departmentCtrlNbr is null || c.DepartmentCtrlNbr == departmentCtrlNbr))
            .ToList();
        if (scopedCrafts.Count == 0)
            return [];

        var rosters = await uow.Rosters.GetByCraftCtrlNbrsAsync(scopedCrafts.Select(c => c.CtrlNbr));
        if (rosters.Count == 0)
            return [];

        var seniorityStateNameByCtrlNbr = new Dictionary<ControlNumber, string>(ControlNumberComparer.Instance);
        var hasInactiveSeniorityByEmployee = new Dictionary<ControlNumber, bool>(ControlNumberComparer.Instance);
        foreach (var roster in rosters)
        {
            var seniorityRows = await uow.Seniority.GetByRosterCtrlNbrAsync(roster.CtrlNbr);
            foreach (var seniority in seniorityRows)
            {
                if (!seniorityStateNameByCtrlNbr.TryGetValue(seniority.SeniorityStateCtrlNbr, out var stateName))
                {
                    var state = await uow.SeniorityStates.GetByCtrlNbrAsync(seniority.SeniorityStateCtrlNbr, ct);
                    if (state is null)
                        continue;

                    stateName = state.StateDescription;
                    seniorityStateNameByCtrlNbr[seniority.SeniorityStateCtrlNbr] = stateName;
                }

                var isInactive = string.Equals(stateName, "Inactive", StringComparison.OrdinalIgnoreCase);
                if (!hasInactiveSeniorityByEmployee.TryGetValue(seniority.EmployeeCtrlNbr, out var existingInactive))
                    hasInactiveSeniorityByEmployee[seniority.EmployeeCtrlNbr] = isInactive;
                else if (!existingInactive && isInactive)
                    hasInactiveSeniorityByEmployee[seniority.EmployeeCtrlNbr] = true;
            }
        }

        var eligibleEmployeeCtrlNbrs = hasInactiveSeniorityByEmployee
            .Where(kvp => !kvp.Value)
            .Select(kvp => kvp.Key)
            .ToHashSet(ControlNumberComparer.Instance);

        if (eligibleEmployeeCtrlNbrs.Count == 0)
            return [];

        var employees = await uow.Employees.GetByCtrlNbrsAsync(eligibleEmployeeCtrlNbrs, ct);
        if (employees.Count == 0)
            return [];

        return employees
            .OrderBy(e => e.EmployeeNumber)
            .ThenBy(e => e.CtrlNbr.Value)
            .ToList();
    }

    private sealed class ControlNumberComparer : IEqualityComparer<ControlNumber>
    {
        public static readonly ControlNumberComparer Instance = new();

        public bool Equals(ControlNumber? x, ControlNumber? y)
            => x?.Value == y?.Value;

        public int GetHashCode(ControlNumber obj)
            => obj.Value.GetHashCode();
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
        var actorContext = await actorContextResolver.ResolveAsync(
            requestedEmployeeCtrlNbr: employeeCtrlNbr.Value,
            parentCtrlNbr: parentCtrlNbr?.Value,
            railroadCtrlNbr: railroadCtrlNbr?.Value,
            ct: ct);
        var canCancelHangoutAsManager = actorContext.IsActingOnBehalfOfEmployee
            && actorContextPolicy.CanAccessRequestedEmployee(actorContext, allowOnBehalf: IsAdminRole());
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
                CanCancelMove(m, policy, canCancelHangoutAsManager),
                targetName));
        }

        // Enrich bids with position name and work-area-localized bulletin window/effective times.
        // Times are localized to the vacancy's work-area zone (mirroring the on-duty enrichment)
        // so the front-end renders wall-clock values without any timezone logic of its own.
        var bidTzCache = new Dictionary<string, TimeZoneInfo?>(StringComparer.OrdinalIgnoreCase);
        var groupTzIdCache = new Dictionary<ControlNumber, string?>();
        var enrichedBids = new List<WorkProfileBulletinBid>();
        foreach (var bid in bids)
        {
            var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(bid.BulletinCtrlNbr, ct);
            string positionName = string.Empty, closesLocalIso = string.Empty, effectiveLocalIso = string.Empty;
            if (bulletin is not null)
            {
                var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
                positionName = vacancy?.TargetName ?? string.Empty;

                TimeZoneInfo? tz = null;
                if (vacancy is not null)
                {
                    if (!groupTzIdCache.TryGetValue(vacancy.WorkAreaGroupCtrlNbr, out var tzId))
                    {
                        var group = await uow.DynamicGroups.GetByCtrlNbrAsync(vacancy.WorkAreaGroupCtrlNbr, ct);
                        tzId = group?.TimeZoneId;
                        groupTzIdCache[vacancy.WorkAreaGroupCtrlNbr] = tzId;
                    }
                    tz = ResolveTimeZone(tzId, bidTzCache);
                }

                closesLocalIso = workAreaClock.FormatLocalIso(bulletin.BidWindowClosesUtc, tz);
                effectiveLocalIso = workAreaClock.FormatLocalIso(bulletin.EffectiveUtc, tz);
            }
            enrichedBids.Add(new WorkProfileBulletinBid(
                bid.CtrlNbr, bid.BulletinCtrlNbr,
                bid.Priority, bid.SubmittedUtc,
                bid.Status, positionName,
                closesLocalIso, effectiveLocalIso));
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
        var records = await uow.OnDutyRecords.GetIncompleteForEmployeeAsync(employeeCtrlNbr, ct);
        return await EnrichAsync(uow, records, ct);
    }

    public async Task<IReadOnlyList<EmployeeOnDutyRecordItem>> GetDutyStatusNotStartedAsync(
        ControlNumber railroadCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var records = await uow.OnDutyRecords.GetNotStartedForRailroadAsync(railroadCtrlNbr, ct);
        var enriched = await EnrichAsync(uow, records, ct);

        return enriched
            .OrderBy(r => r.WorkAreaName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => ParseIsoOrMin(r.OnDutyLocalIso))
            .ToList();
    }

    public async Task CompleteDeferredOnDutyRecordAsync(
        ControlNumber onDutyRecordCtrlNbr,
        ControlNumber requestedEmployeeCtrlNbr,
        DateTime? offDutyTimeUtc = null,
        CancellationToken ct = default)
    {
        var actorContext = await actorContextResolver.ResolveAsync(
            requestedEmployeeCtrlNbr: requestedEmployeeCtrlNbr.Value,
            ct: ct);

        if (!actorContextPolicy.ShouldUseEmployeeBehavior(actorContext))
            throw new InvalidOperationException("Only the employee may complete deferred on-duty records.");

        ControlNumber positionSlotCtrlNbr;
        OnDutyStatus onDutyStatus;

        await using (var precheckUow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            var onDuty = await precheckUow.OnDutyRecords.GetByCtrlNbrAsync(onDutyRecordCtrlNbr, ct)
                ?? throw new KeyNotFoundException("On-duty record not found.");

            if (onDuty.EmployeeCtrlNbr != requestedEmployeeCtrlNbr)
                throw new InvalidOperationException("Employee mismatch for on-duty completion.");

            positionSlotCtrlNbr = onDuty.PositionSlotCtrlNbr;
            onDutyStatus = onDuty.Status;
        }

        if (onDutyStatus != OnDutyStatus.TiedUp)
        {
            var slotDisplayMap = await onDutyQueryService.GetSlotDisplayAsync([positionSlotCtrlNbr], ct);
            if (!slotDisplayMap.TryGetValue(positionSlotCtrlNbr, out var slotDisplay)
                || slotDisplay.CraftCtrlNbr <= 0)
            {
                throw new InvalidOperationException("Unable to resolve craft for employee tie-up.");
            }

            await tieUpService.ExecuteAsync(
                onDutyRecordCtrlNbr,
                DateTime.SpecifyKind(offDutyTimeUtc ?? workAreaClock.UtcNow.UtcDateTime, DateTimeKind.Utc),
                string.Empty,
                ControlNumber.Create(slotDisplay.CraftCtrlNbr),
                offDutyTimeConfirmed: true,
                ct);

            ControlNumber? postTieUpShiftInstanceCtrlNbr;
            await using (var postTieUpUow = await uowFactory.CreateAsync(cancellationToken: ct))
            {
                postTieUpShiftInstanceCtrlNbr = await CompleteIfRestedAsync(postTieUpUow, onDutyRecordCtrlNbr, requestedEmployeeCtrlNbr, ct);
            }

            if (postTieUpShiftInstanceCtrlNbr is not null)
            {
                await tieUpService.AutoCloseShiftIfAllOnDutyStartedAsync(postTieUpShiftInstanceCtrlNbr, ct);
            }

            return;
        }

        ControlNumber? tiedUpShiftInstanceCtrlNbr;
        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            var tiedUpOnDuty = await uow.OnDutyRecords.GetByCtrlNbrAsync(onDutyRecordCtrlNbr, ct)
                ?? throw new KeyNotFoundException("On-duty record not found.");

            if (tiedUpOnDuty.EmployeeCtrlNbr != requestedEmployeeCtrlNbr)
                throw new InvalidOperationException("Employee mismatch for on-duty completion.");

            if (offDutyTimeUtc.HasValue)
            {
                await ConfirmExistingTieUpAsync(
                    uow,
                    tiedUpOnDuty,
                    DateTime.SpecifyKind(offDutyTimeUtc.Value, DateTimeKind.Utc),
                    ct);
            }

            tiedUpShiftInstanceCtrlNbr = await CompleteIfRestedAsync(uow, onDutyRecordCtrlNbr, requestedEmployeeCtrlNbr, ct);
        }

        if (tiedUpShiftInstanceCtrlNbr is not null)
        {
            await tieUpService.AutoCloseShiftIfAllOnDutyStartedAsync(tiedUpShiftInstanceCtrlNbr, ct);
        }
    }

    private async Task ConfirmExistingTieUpAsync(
        IOrchestrationUnitOfWork uow,
        OnDutyRecord onDuty,
        DateTime actualOffDutyTimeUtc,
        CancellationToken ct)
    {
        if (actualOffDutyTimeUtc < onDuty.OnDutyTimeUtc)
            throw new InvalidOperationException("Off-duty time cannot be earlier than on-duty time.");

        var offDuty = (await uow.OffDutyRecords.GetByOnDutyRecordsAsync([onDuty.CtrlNbr], ct)).FirstOrDefault()
            ?? throw new InvalidOperationException("Off-duty record not found for on-duty completion.");

        var slotDisplayMap = await onDutyQueryService.GetSlotDisplayAsync([onDuty.PositionSlotCtrlNbr], ct);
        if (!slotDisplayMap.TryGetValue(onDuty.PositionSlotCtrlNbr, out var slotDisplay)
            || slotDisplay.CraftCtrlNbr <= 0)
        {
            throw new InvalidOperationException("Unable to resolve craft for employee tie-up confirmation.");
        }

        var policy = await uow.CraftOperationsPolicies.GetByCraftAsync(ControlNumber.Create(slotDisplay.CraftCtrlNbr), ct);
        var totalMinutes = Math.Max(0, (int)(actualOffDutyTimeUtc - onDuty.OnDutyTimeUtc).TotalMinutes);
        var restHours = CalculateRestHours(policy, totalMinutes);
        var consecutiveDayResetHours = policy?.ConsecutiveDayResetHours ?? 24m;

        offDuty.ConfirmOffDutyTime(
            actualOffDutyTimeUtc,
            totalMinutes,
            restHours,
            consecutiveDayResetHours,
            releaseReason: offDuty.ReleaseReason,
            confirmedAtUtc: workAreaClock.UtcNow.UtcDateTime,
            confirmedBy: currentUserService.GetUserName());

        uow.OffDutyRecords.Update(offDuty);
    }

    private static decimal CalculateRestHours(CraftOperationsPolicy? policy, int totalMinutes)
    {
        if (policy is null) return 10m;

        return policy.RestCalculationStrategy switch
        {
            "FixedHours" => policy.FixedRestHours ?? 10m,
            "CraftConfigured" => CalculateCraftConfiguredRest(totalMinutes),
            _ => 10m
        };
    }

    private static decimal CalculateCraftConfiguredRest(int totalMinutes)
    {
        var baseRest = 10m;
        var excessMinutes = Math.Max(0, totalMinutes - 720);
        var penalty = excessMinutes > 0 ? Math.Ceiling(excessMinutes / 60m) : 0;
        return baseRest + penalty;
    }

    private async Task<ControlNumber?> CompleteIfRestedAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber onDutyRecordCtrlNbr,
        ControlNumber requestedEmployeeCtrlNbr,
        CancellationToken ct)
    {
        var onDuty = await uow.OnDutyRecords.GetByCtrlNbrAsync(onDutyRecordCtrlNbr, ct)
            ?? throw new KeyNotFoundException("On-duty record not found.");

        if (onDuty.EmployeeCtrlNbr != requestedEmployeeCtrlNbr)
            throw new InvalidOperationException("Employee mismatch for on-duty completion.");

        if (onDuty.Status != OnDutyStatus.TiedUp)
            throw new InvalidOperationException("On-duty record cannot be completed until tied up.");

        var offDuty = (await uow.OffDutyRecords.GetByOnDutyRecordsAsync([onDuty.CtrlNbr], ct)).FirstOrDefault()
            ?? throw new InvalidOperationException("Off-duty record not found for on-duty completion.");

        var tieUpContext = await uow.OnDutyRecords.GetTieUpContextAsync(onDuty.CtrlNbr, ct);

        if (offDuty.RestedAtUtc > workAreaClock.UtcNow.UtcDateTime)
            throw new InvalidOperationException("Employee is not yet rested for deferred completion.");

        onDuty.CompleteByEmployee();
        await uow.CommitAsync(ct);

        return tieUpContext?.ShiftInstanceCtrlNbr;
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

    private static DateTimeOffset ParseIsoOrMin(string? iso)
        => DateTimeOffset.TryParse(iso, out var dto) ? dto : DateTimeOffset.MinValue;

    /// <summary>
    /// Enriches raw on-duty records with slot display data (assignment, crew, craft, location),
    /// tie-up (off-duty) data, and work-area-localized ISO on/off-duty timestamps.
    /// </summary>
    private async Task<IReadOnlyList<EmployeeOnDutyRecordItem>> EnrichAsync(
        IOrchestrationUnitOfWork uow, IReadOnlyList<OnDutyRecord> records, CancellationToken ct)
    {
        if (records.Count == 0) return [];

        var employeeIds = records.Select(r => r.EmployeeCtrlNbr).Distinct().ToList();
        var employees = await uow.Employees.GetByCtrlNbrsAsync(employeeIds, ct);
        var employeeMap = employees.ToDictionary(e => e.CtrlNbr, e => e);

        var userIds = employees
            .Select(e => e.UserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var userNames = userIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : (await userAccountService.GetNamesByIdsAsync(userIds))
                .Where(u => !string.IsNullOrWhiteSpace(u.FullNameLNF))
                .ToDictionary(u => u.Id, u => u.FullNameLNF!, StringComparer.Ordinal);

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
                display?.WorkAreaCtrlNbr,
                display?.WorkAreaName ?? string.Empty,
                r.OnDutyTimeUtc,
                workAreaClock.FormatLocalIso(r.OnDutyTimeUtc, tz),
                offUtc,
                offUtc is null ? string.Empty : workAreaClock.FormatLocalIso(offUtc.Value, tz),
                off?.TotalTimeOnDutyMinutes,
                r.ConsecutiveDays,
                r.IsAssigned,
                r.IsLateCall,
                r.Status.Value,
                r.CompletionStatus.Value,
                r.CompletionStatus == OnDutyCompletionStatus.PendingEmployeeCompletion,
                off?.RestedAtUtc,
                off?.OffDutyTimeConfirmed ?? false,
                off?.OffDutyTimeConfirmedAtUtc,
                off?.OffDutyTimeConfirmedBy ?? string.Empty,
                display?.WorkAreaCode ?? string.Empty,
                ResolveEmployeeName(r.EmployeeCtrlNbr, employeeMap, userNames),
                ResolveEmployeeNumber(r.EmployeeCtrlNbr, employeeMap),
                r.EmployeeCtrlNbr.Value,
                display?.CraftCtrlNbr ?? 0,
                ResolveAssignmentOffDutyLocalIso(r.OnDutyTimeUtc, display, tz)));
        }

        return items;
    }

    private static string ResolveEmployeeName(
        ControlNumber employeeCtrlNbr,
        IReadOnlyDictionary<ControlNumber, Employee> employeeMap,
        IReadOnlyDictionary<string, string> userNames)
    {
        if (!employeeMap.TryGetValue(employeeCtrlNbr, out var employee))
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(employee.UserId)
            && userNames.TryGetValue(employee.UserId, out var fullNameLnf)
            && !string.IsNullOrWhiteSpace(fullNameLnf))
        {
            return fullNameLnf;
        }

        return string.Empty;
    }

    private static string ResolveEmployeeNumber(
        ControlNumber employeeCtrlNbr,
        IReadOnlyDictionary<ControlNumber, Employee> employeeMap)
        => employeeMap.TryGetValue(employeeCtrlNbr, out var employee)
            ? employee.EmployeeNumber ?? string.Empty
            : string.Empty;

    private string ResolveAssignmentOffDutyLocalIso(
        DateTime onDutyUtc,
        EmployeeOnDutySlotDisplay? display,
        TimeZoneInfo? tz)
    {
        if (display is null)
            return string.Empty;

        var onDutyLocal = tz is null
            ? DateTime.SpecifyKind(onDutyUtc, DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(onDutyUtc, DateTimeKind.Utc), tz);

        var offDutyLocal = new DateTime(
            onDutyLocal.Year,
            onDutyLocal.Month,
            onDutyLocal.Day,
            display.AssignmentOffDutyTime.Hour,
            display.AssignmentOffDutyTime.Minute,
            display.AssignmentOffDutyTime.Second,
            DateTimeKind.Unspecified);

        if (display.AssignmentOffDutyTime <= TimeOnly.FromDateTime(onDutyLocal))
            offDutyLocal = offDutyLocal.AddDays(1);

        return tz is null
            ? DateTime.SpecifyKind(offDutyLocal, DateTimeKind.Utc).ToString("o")
            : new DateTimeOffset(offDutyLocal, tz.GetUtcOffset(offDutyLocal)).ToString("o");
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
    private bool IsAdminRole()
        => currentUserService.IsInRole(Roles.SystemAdmin)
            || currentUserService.IsInRole(Roles.ParentAdmin)
            || currentUserService.IsInRole(Roles.RailroadAdmin);

    private static bool CanCancelMove(SeniorityMove move, SeniorityMovePolicy? policy, bool canCancelHangoutAsManager)
    {
        if (move.MoveType == SeniorityMoveType.Hangout && !canCancelHangoutAsManager)
            return false;

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