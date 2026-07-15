using System.Security.Claims;
using CrewService.BlazorUI.Client;
using CrewService.BlazorUI.Models.Auth;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Scoped (per-circuit) service that resolves and caches the current user's
/// identity and bootstrap-seeded employee linkage from <see cref="ClaimsPrincipal"/>.
/// Avoids post-login profile lookups so auth and role gating do not depend on
/// a second gRPC round-trip.
/// </summary>
public class CurrentUserService
{
    private bool _initialized;

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
    public Task InitializeAsync(ClaimsPrincipal user)
    {
        if (_initialized) return Task.CompletedTask;
        User = user;

        Username = user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        IsAdmin = user.IsInRole(Roles.SystemAdmin)
            || user.IsInRole(Roles.ParentAdmin)
            || user.IsInRole(Roles.RailroadAdmin);

        EmployeeNumber = user.FindFirst(CustomClaimTypes.EmployeeNumber)?.Value;
        // Employee identity for gating is claim/bootstrap-based.
        // Detailed employee profile is loaded by feature pages as needed.
        IsEmployee = user.IsInRole(Roles.Employee) || !string.IsNullOrWhiteSpace(EmployeeNumber) || Employee is not null;

        _initialized = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Seeds this service from bootstrap data, avoiding the separate gRPC call
    /// to the Employee service.
    /// </summary>
    public void SeedFromBootstrap(ClaimsPrincipal user, GetEmployeeResponse? employee)
    {
        User ??= user;

        Username ??= user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        IsAdmin = IsAdmin
            || user.IsInRole(Roles.SystemAdmin)
            || user.IsInRole(Roles.ParentAdmin)
            || user.IsInRole(Roles.RailroadAdmin);

        EmployeeNumber ??= user.FindFirst(CustomClaimTypes.EmployeeNumber)?.Value;

        if (employee is not null)
        {
            Employee = employee;
            IsEmployee = true;
        }
        else if (!IsEmployee)
        {
            IsEmployee = user.IsInRole(Roles.Employee) || !string.IsNullOrWhiteSpace(EmployeeNumber);
        }

        _initialized = true;
    }

    /// <summary>
    /// No-op placeholder retained for compatibility with existing call sites.
    /// </summary>
    public Task ReloadEmployeeAsync()
    {
        return Task.CompletedTask;
    }
}
