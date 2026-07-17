namespace CrewService.Application.Authorization;

public interface IRequestActorContextPolicy
{
    /// <summary>
    /// Authoritative employee behavior check: linked employee + self match.
    /// </summary>
    bool ShouldUseEmployeeBehavior(RequestActorContext context);

    /// <summary>
    /// Subject access check for operations targeting an employee.
    /// Allows self by default, and optionally allows on-behalf access.
    /// </summary>
    bool CanAccessRequestedEmployee(RequestActorContext context, bool allowOnBehalf);
}
