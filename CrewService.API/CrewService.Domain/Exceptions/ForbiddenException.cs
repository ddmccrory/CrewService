namespace CrewService.Domain.Exceptions;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message)
        : base("ACCESS_FORBIDDEN", message)
    {
    }

    public ForbiddenException()
        : base("ACCESS_FORBIDDEN", "You do not have permission to perform this action.")
    {
    }
}