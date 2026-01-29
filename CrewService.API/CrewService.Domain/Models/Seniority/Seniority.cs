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
        return new Seniority(
            ControlNumber.Create(rosterCtrlNbr),
            ControlNumber.Create(railroadPoolEmployeeCtrlNbr),
            lastActiveRoster,
            rosterDate,
            rank,
            stateID,
            canTrain);
    }

    public Seniority Update(
        bool? lastActiveRoster = null,
        DateTime? rosterDate = null,
        int? rank = null,
        int? stateID = null,
        bool? canTrain = null)
    {
        if (lastActiveRoster is not null) LastActiveRoster = lastActiveRoster.Value;
        if (rosterDate is not null) RosterDate = rosterDate.Value;
        if (rank is not null) Rank = rank.Value;
        if (stateID is not null) StateID = stateID.Value;
        if (canTrain is not null) CanTrain = canTrain.Value;

        return this;
    }
}