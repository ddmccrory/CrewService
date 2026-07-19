using CrewService.Application.Authorization;
using CrewService.Domain.Constants;
using CrewService.Domain.Modules.Employees;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CrewService.Persistance.Services;

public sealed class RequestActorContextResolver(
    IHttpContextAccessor httpContextAccessor,
    IEmployeeRepository employeeRepository) : IRequestActorContextResolver
{
    public async Task<RequestActorContext> ResolveAsync(
        long? requestedEmployeeCtrlNbr = null,
        long? parentCtrlNbr = null,
        long? railroadCtrlNbr = null,
        long? workAreaCtrlNbr = null,
        CancellationToken ct = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var currentUserId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub);

        var resolvedParentCtrlNbr = parentCtrlNbr ?? TryGetHeaderLong("x-parent-ctrl-nbr");
        var resolvedRailroadCtrlNbr = railroadCtrlNbr ?? TryGetHeaderLong("x-railroad-ctrl-nbr");

        long? currentEmployeeCtrlNbr = null;
        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            var employee = await employeeRepository.GetByUserIdAsync(currentUserId, ct);

            if (employee is null)
            {
                var employeeNumber = user?.FindFirstValue(CustomClaimTypes.EmployeeNumber);
                if (!string.IsNullOrWhiteSpace(employeeNumber))
                    employee = await employeeRepository.GetByEmployeeNumberAsync(employeeNumber);
            }

            currentEmployeeCtrlNbr = employee?.CtrlNbr.Value;
        }

        var isLinkedEmployee = currentEmployeeCtrlNbr.HasValue;
        var isSelfEmployeeContext = isLinkedEmployee
            && requestedEmployeeCtrlNbr.HasValue
            && currentEmployeeCtrlNbr.HasValue
            && currentEmployeeCtrlNbr.Value == requestedEmployeeCtrlNbr.Value;

        var isActingOnBehalfOfEmployee = requestedEmployeeCtrlNbr.HasValue
            && requestedEmployeeCtrlNbr != currentEmployeeCtrlNbr;

        return new RequestActorContext(
            CurrentUserId: currentUserId,
            CurrentEmployeeCtrlNbr: currentEmployeeCtrlNbr,
            RequestedEmployeeCtrlNbr: requestedEmployeeCtrlNbr,
            IsLinkedEmployee: isLinkedEmployee,
            IsSelfEmployeeContext: isSelfEmployeeContext,
            IsActingOnBehalfOfEmployee: isActingOnBehalfOfEmployee,
            ParentCtrlNbr: resolvedParentCtrlNbr,
            RailroadCtrlNbr: resolvedRailroadCtrlNbr,
            WorkAreaCtrlNbr: workAreaCtrlNbr);
    }

    private long? TryGetHeaderLong(string headerName)
    {
        var raw = httpContextAccessor.HttpContext?.Request.Headers[headerName].FirstOrDefault();
        return long.TryParse(raw, out var value) && value > 0 ? value : null;
    }
}
