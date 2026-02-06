using CrewService.Domain.DomainEvents.Seniority;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Seniority;

public sealed class Seniority : Entity
{
    public ControlNumber RosterCtrlNbr { get; private set; }
    public ControlNumber RailroadPoolEmployeeCtrlNbr { get; private set; }
    public bool LastActiveRoster { get; private set; }
    public DateTime RosterDate { get; private set; }
    public int Rank { get; private set; }
    public int StateID { get; private set; }
    public bool CanTrain { get; private set; }

    private Seniority()
    {
        RosterCtrlNbr = null!;
        RailroadPoolEmployeeCtrlNbr = null!;
    }

    private Seniority(
        ControlNumber rosterCtrlNbr,
        ControlNumber railroadPoolEmployeeCtrlNbr,
        bool lastActiveRoster,
        DateTime rosterDate,
        int rank,
        int stateID,
        bool canTrain)
    {
        RosterCtrlNbr = rosterCtrlNbr;
        RailroadPoolEmployeeCtrlNbr = railroadPoolEmployeeCtrlNbr;
        LastActiveRoster = lastActiveRoster;
        RosterDate = rosterDate;
        Rank = rank;
        StateID = stateID;
        CanTrain = canTrain;
    }

    public static Seniority Create(
        long rosterCtrlNbr,
        long railroadPoolEmployeeCtrlNbr,
        bool lastActiveRoster,
        DateTime rosterDate,
        int rank,
        int stateID,
        bool canTrain)
    {
        var entity = new Seniority(
            ControlNumber.Create(rosterCtrlNbr),
            ControlNumber.Create(railroadPoolEmployeeCtrlNbr),
            lastActiveRoster,
            rosterDate,
            rank,
            stateID,
            canTrain);
        entity.Raise(new SeniorityCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public Seniority Update(
        bool? lastActiveRoster = null,
        DateTime? rosterDate = null,
        int? rank = null,
        int? stateID = null,
        bool? canTrain = null)
    {
        var changes = new Dictionary<string, object?>();

        if (lastActiveRoster is not null) { LastActiveRoster = lastActiveRoster.Value; changes["lastActiveRoster"] = lastActiveRoster.Value; }
        if (rosterDate is not null) { RosterDate = rosterDate.Value; changes["rosterDate"] = rosterDate.Value; }
        if (rank is not null) { Rank = rank.Value; changes["rank"] = rank.Value; }
        if (stateID is not null) { StateID = stateID.Value; changes["stateID"] = stateID.Value; }
        if (canTrain is not null) { CanTrain = canTrain.Value; changes["canTrain"] = canTrain.Value; }

        if (changes.Count > 0)
        {
            Raise(new SeniorityUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }

        return this;
    }

    public void Delete()
    {
        Raise(new SeniorityDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}