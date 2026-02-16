namespace CrewService.Domain.Exceptions;

public sealed class NotFoundException(string entityName, object entityId) : DomainException("ENTITY_NOT_FOUND", $"{entityName} with identifier '{entityId}' was not found.")
{
    public string EntityName { get; } = entityName;
    public object EntityId { get; } = entityId;
}