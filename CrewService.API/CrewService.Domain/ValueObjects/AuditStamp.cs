namespace CrewService.Domain.ValueObjects;

public sealed record AuditStamp
{
    public Name AuditName { get; private set; }

    public DateTime AuditDateTime { get; private set; }

    internal AuditStamp(Name auditName, DateTime auditDateTime)
    {
        AuditName = auditName;

        AuditDateTime = auditDateTime;
    }

    public static AuditStamp Create(string auditName)
    {
        return new AuditStamp(Name.Create(auditName), DateTime.UtcNow);
    }
}