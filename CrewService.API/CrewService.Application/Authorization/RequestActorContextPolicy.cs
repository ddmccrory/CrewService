namespace CrewService.Application.Authorization;

public sealed class RequestActorContextPolicy : IRequestActorContextPolicy
{
    public bool ShouldUseEmployeeBehavior(RequestActorContext context)
        => context.IsLinkedEmployee && context.IsSelfEmployeeContext;

    public bool CanAccessRequestedEmployee(RequestActorContext context, bool allowOnBehalf)
    {
        if (!context.RequestedEmployeeCtrlNbr.HasValue)
            return false;

        if (ShouldUseEmployeeBehavior(context))
            return true;

        if (context.IsActingOnBehalfOfEmployee)
            return allowOnBehalf;

        return false;
    }
}
