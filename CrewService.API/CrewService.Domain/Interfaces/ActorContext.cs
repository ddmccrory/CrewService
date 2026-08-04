namespace CrewService.Domain.Interfaces;

public sealed record ActorContext(
    string? UserIdentifier,
    string? UserName,
    long? ParentCtrlNbr,
    long? RailroadCtrlNbr);
