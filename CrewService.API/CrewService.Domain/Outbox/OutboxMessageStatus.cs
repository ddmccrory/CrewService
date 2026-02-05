namespace CrewService.Domain.Outbox;

public enum OutboxMessageStatus
{
    Pending = 0,
    Published = 1,
    Failed = 2
}