using CrewService.Application.Modules.UserAccount;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Employees;

public sealed class EmployeeAppService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IUserAccountService userAccountService,
    ICurrentUserService currentUserService)
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
}
