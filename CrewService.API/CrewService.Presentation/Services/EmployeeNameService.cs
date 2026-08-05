using CrewService.Application.Employees;
using CrewService.Application.Models.UserAccount;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;
using CrewService.Application.Modules.UserAccount;

namespace CrewService.Presentation.Services;

/// <summary>
/// Centralized service for resolving and formatting employee display names.
/// Eliminates the repeated pattern of: FindByIdAsync → user?.FullNameLNF
/// scattered across gRPC service classes.
/// </summary>
public sealed class EmployeeNameService(
    IUserAccountService userAccountService,
    EmployeeAppService employeeAppService)
{
    /// <summary>
    /// Returns <c>LastName, FirstName M.</c> for the given ASP.NET Identity user ID.
    /// Returns <see cref="string.Empty"/> if the user is not found.
    /// </summary>
    public async Task<string> GetFullNameLnfAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return string.Empty;
        var user = await userAccountService.FindByIdAsync(userId);
        if (user is null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
            return FormatFullNameLnf(user.FirstName, user.MiddleName ?? string.Empty, user.LastName);

        return string.Empty;
    }

    /// <summary>
    /// Resolves names for multiple user IDs in a single database query.
    /// Returns a dictionary keyed by userId → <c>LastName, FirstName M.</c>.
    /// Missing users are omitted from the result.
    /// </summary>
    public async Task<Dictionary<string, string>> GetFullNameLnfBatchAsync(IEnumerable<string?> userIds)
    {
        var ids = userIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().Cast<string>().ToList();
        if (ids.Count == 0) return [];

        var users = await userAccountService.GetNamesByIdsAsync(ids);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var user in users)
        {
            var display = !string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName)
                ? FormatFullNameLnf(user.FirstName, user.MiddleName ?? string.Empty, user.LastName)
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(display))
                result[user.Id] = display;
        }

        return result;
    }

    /// <summary>
    /// Resolves the employee by <paramref name="employeeCtrlNbr"/> then returns their
    /// <c>LastName, FirstName M.</c> display name.
    /// Returns <see cref="string.Empty"/> if the employee or user is not found.
    /// </summary>
    public async Task<string> GetFullNameLnfAsync(ControlNumber employeeCtrlNbr)
    {
        try
        {
            var employee = await employeeAppService.GetAsync(employeeCtrlNbr);
            return await GetFullNameLnfAsync(employee.UserId);
        }
        catch (KeyNotFoundException)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Resolves the employee by <paramref name="employeeCtrlNbr"/> and returns both their
    /// <c>LastName, FirstName M.</c> display name and their employee number.
    /// </summary>
    public async Task<(string FullNameLnf, string EmployeeNumber)> GetEmployeeInfoAsync(ControlNumber employeeCtrlNbr)
    {
        try
        {
            var employee = await employeeAppService.GetAsync(employeeCtrlNbr);
            var fullNameLnf = await GetFullNameLnfAsync(employee.UserId);
            return (fullNameLnf, employee.EmployeeNumber ?? string.Empty);
        }
        catch (KeyNotFoundException)
        {
            return (string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// Resolves employee number and display name for multiple employees in bulk.
    /// Returns a dictionary keyed by <see cref="ControlNumber"/> →
    /// <c>(FullNameLnf, EmployeeNumber)</c>.
    /// </summary>
    public async Task<Dictionary<ControlNumber, (string FullNameLnf, string EmployeeNumber)>> GetEmployeeInfoBatchAsync(
        IEnumerable<ControlNumber> ctrlNbrs)
    {
        var distinct = ctrlNbrs.Distinct().ToList();
        if (distinct.Count == 0) return [];

        var employees = await employeeAppService.GetByCtrlNbrsAsync(distinct);
        var userIds = employees.Select(e => e.UserId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var users = await userAccountService.GetNamesByIdsAsync(userIds!);
        var usersById = users.ToDictionary(u => u.Id, StringComparer.Ordinal);

        return BuildDeterministicEmployeeInfoMap(employees, usersById);
    }

    public async Task<Dictionary<ControlNumber, (string FullNameLnf, string EmployeeNumber)>> GetEmployeeInfoBatchAsync(
        IOrchestrationUnitOfWork uow,
        IEnumerable<ControlNumber> ctrlNbrs,
        CancellationToken ct = default)
    {
        var distinct = ctrlNbrs.Distinct().ToList();
        if (distinct.Count == 0) return [];

        var employees = await uow.Employees.GetByCtrlNbrsAsync(distinct, ct);

        var userIds = employees.Select(e => e.UserId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var users = await userAccountService.GetNamesByIdsAsync(userIds!);
        var usersById = users.ToDictionary(u => u.Id, StringComparer.Ordinal);

        return BuildDeterministicEmployeeInfoMap(employees, usersById);
    }

    private static Dictionary<ControlNumber, (string FullNameLnf, string EmployeeNumber)> BuildDeterministicEmployeeInfoMap(
        IEnumerable<Employee> employees,
        IReadOnlyDictionary<string, UserNameDto> usersById)
    {
        var result = new Dictionary<ControlNumber, (string FullNameLnf, string EmployeeNumber)>();

        foreach (var employee in employees)
        {
            if (string.IsNullOrWhiteSpace(employee.UserId))
                throw new InvalidOperationException($"Employee {employee.CtrlNbr.Value} has no UserId.");

            if (!usersById.TryGetValue(employee.UserId, out var user))
                throw new InvalidOperationException($"Employee {employee.CtrlNbr.Value} references missing user '{employee.UserId}'.");

            if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
                throw new InvalidOperationException(
                    $"User '{employee.UserId}' for employee {employee.CtrlNbr.Value} is missing required first/last name.");

            result[employee.CtrlNbr] = (
                FormatFullNameLnf(user.FirstName, user.MiddleName ?? string.Empty, user.LastName),
                employee.EmployeeNumber ?? string.Empty);
        }

        return result;
    }

    /// <summary>
    /// Formats a name as <c>FirstName M. LastName</c>.
    /// </summary>
    public static string FormatFullName(string firstName, string middleName, string lastName)
        => $"{FormatFirstName(firstName)} {FormatMiddleName(middleName)} {lastName}".Trim();

    /// <summary>
    /// Formats a name as <c>LastName, FirstName M.</c>
    /// </summary>
    public static string FormatFullNameLnf(string firstName, string middleName, string lastName)
        => $"{lastName}, {FormatFirstName(firstName)} {FormatMiddleName(middleName)}".Trim(',', ' ');

    private static string FormatFirstName(string fname)
    {
        fname = fname.Trim('.');
        if (!string.IsNullOrEmpty(fname) && fname.Length is 1)
            fname = $"{fname}.";
        return fname;
    }

    private static string FormatMiddleName(string mname)
    {
        mname = mname.Trim('.');
        if (!string.IsNullOrEmpty(mname))
            mname = $"{mname[..1]}.";
        return mname;
    }
}
