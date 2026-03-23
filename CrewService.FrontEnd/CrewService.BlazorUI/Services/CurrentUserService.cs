using System.Security.Claims;
using CrewService.BlazorUI.Client;
using CrewService.BlazorUI.Clients;
using CrewService.BlazorUI.Models.Auth;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Scoped (per-circuit) service that resolves and caches the current user's
/// identity, admin status, and employee record from <see cref="ClaimsPrincipal"/>.
/// Eliminates duplicated auth/employee resolution logic across pages.
/// </summary>
public class CurrentUserService
{
    private readonly EmployeeClient _employeeClient;
    private bool _initialized;

    public CurrentUserService(EmployeeClient employeeClient)
    {
        _employeeClient = employeeClient;
    }

    /// <summary>The user's email or name identifier claim.</summary>
    public string? Username { get; private set; }

    /// <summary>The employee number from the user's claims, if present.</summary>
    public string? EmployeeNumber { get; private set; }

    /// <summary><c>true</c> when the user has a linked employee record.</summary>
    public bool IsEmployee { get; private set; }

    /// <summary><c>true</c> when the user holds any admin-level role.</summary>
    public bool IsAdmin { get; private set; }

    /// <summary>The full employee record, or <c>null</c> if the user is not an employee.</summary>
    public GetEmployeeResponse? Employee { get; private set; }

    /// <summary>The raw <see cref="ClaimsPrincipal"/> from the last initialization.</summary>
    public ClaimsPrincipal? User { get; private set; }

    /// <summary>
    /// Resolves the current user's identity and employee record from claims.
    /// Idempotent — subsequent calls within the same circuit are no-ops.
    /// </summary>
    public async Task InitializeAsync(ClaimsPrincipal user)
    {
        if (_initialized) return;
        _initialized = true;
        User = user;

        Username = user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        IsAdmin = user.IsInRole(Roles.SystemAdmin)
            || user.IsInRole(Roles.ParentAdmin)
            || user.IsInRole(Roles.RailroadAdmin);

        EmployeeNumber = user.FindFirst(CustomClaimTypes.EmployeeNumber)?.Value;
        if (!string.IsNullOrWhiteSpace(EmployeeNumber))
        {
            try
            {
                Employee = await _employeeClient.GetByNumberAsync(EmployeeNumber);
                IsEmployee = true;
            }
            catch
            {
                IsEmployee = false;
            }
        }
    }

    /// <summary>
    /// Reloads the employee record from the API. Use after CRUD operations
    /// that modify the employee's sub-collections (addresses, phones, etc.).
    /// </summary>
    public async Task ReloadEmployeeAsync()
    {
        if (Employee is null || string.IsNullOrWhiteSpace(EmployeeNumber)) return;
        try
        {
            Employee = await _employeeClient.GetByNumberAsync(Employee.EmployeeNumber);
        }
        catch
        {
        }
    }
}
