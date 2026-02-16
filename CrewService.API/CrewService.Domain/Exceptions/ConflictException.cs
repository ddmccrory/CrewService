namespace CrewService.Domain.Exceptions;

public sealed class ConflictException : DomainException
{
    public string EntityName { get; }

    public ConflictException(string entityName, string message)
        : base("ENTITY_CONFLICT", message)
    {
        EntityName = entityName;
    }

    public ConflictException(string entityName)
        : base("ENTITY_CONFLICT", $"{entityName} already exists.")
    {
        EntityName = entityName;
    }
}