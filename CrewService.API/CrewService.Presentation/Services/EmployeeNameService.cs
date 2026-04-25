using CrewService.Domain.Modules.Employees;
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
    IEmployeeRepository employeeRepository)
{
    /// <summary>
    /// Returns <c>LastName, FirstName M.</c> for the given ASP.NET Identity user ID.
    /// Returns <see cref="string.Empty"/> if the user is not found.
    /// </summary>
    public async Task<string> GetFullNameLnfAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return string.Empty;
        var user = await userAccountService.FindByIdAsync(userId);
        return user?.FullNameLNF ?? string.Empty;
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

        return users
            .Where(u => u.FullNameLNF is not null)
            .ToDictionary(u => u.Id, u => u.FullNameLNF!, StringComparer.Ordinal);
    }

    /// <summary>
    /// Resolves the employee by <paramref name="employeeCtrlNbr"/> then returns their
    /// <c>LastName, FirstName M.</c> display name.
    /// Returns <see cref="string.Empty"/> if the employee or user is not found.
    /// </summary>
    public async Task<string> GetFullNameLnfAsync(ControlNumber employeeCtrlNbr)
    {
        var employee = await employeeRepository.GetByCtrlNbrAsync(employeeCtrlNbr);
        if (employee is null) return string.Empty;
        return await GetFullNameLnfAsync(employee.UserId);
    }

    /// <summary>
    /// Resolves the employee by <paramref name="employeeCtrlNbr"/> and returns both their
    /// <c>LastName, FirstName M.</c> display name and their employee number.
    /// </summary>
    public async Task<(string FullNameLnf, string EmployeeNumber)> GetEmployeeInfoAsync(ControlNumber employeeCtrlNbr)
    {
        var employee = await employeeRepository.GetByCtrlNbrAsync(employeeCtrlNbr);
        if (employee is null) return (string.Empty, string.Empty);
        var fullNameLnf = await GetFullNameLnfAsync(employee.UserId);
        return (fullNameLnf, employee.EmployeeNumber ?? string.Empty);
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

        var employees = await employeeRepository.GetByCtrlNbrsAsync(distinct);

        var userIds = employees.Select(e => e.UserId).Where(id => !string.IsNullOrEmpty(id)).Distinct();
        var nameMap = await GetFullNameLnfBatchAsync(userIds!);

        return employees.ToDictionary(
            e => e.CtrlNbr,
            e => (
                FullNameLnf: e.UserId is not null && nameMap.TryGetValue(e.UserId, out var n) ? n : string.Empty,
                EmployeeNumber: e.EmployeeNumber ?? string.Empty
            ));
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
