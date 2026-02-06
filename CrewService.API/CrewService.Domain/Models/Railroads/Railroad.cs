using CrewService.Domain.DomainEvents.Railroads;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Railroads;

public sealed class Railroad : Entity
{
    public ControlNumber ParentCtrlNbr { get; private set; }

    public string RailroadMark { get; private set; }

    public Name Name { get; private set; }

    private Railroad(ControlNumber parentCtrlNbr, string railroadMark, Name name)
    {
        ParentCtrlNbr = parentCtrlNbr;
        RailroadMark = railroadMark;
        Name = name;
    }

    public static Railroad Create(long parentCtrlNbr, string rrMark, string name)
    {
        var railroad = new Railroad(ControlNumber.Create(parentCtrlNbr), rrMark, Name.Create(name));

        railroad.Raise(new RailroadCreatedDomainEvent(railroad.CtrlNbr));

        return railroad;
    }

    public Railroad Update(long clntCtrlNbr, string rrMark, string name)
    {
        bool raise = false;
        var changes = new List<object>();

        if (clntCtrlNbr > 0)
        {
            ParentCtrlNbr = ControlNumber.Create(clntCtrlNbr);
            raise = true;
            changes.Add(new { Field = "ParentCtrlNbr", Value = clntCtrlNbr });
        }

        if (!string.IsNullOrEmpty(rrMark))
        {
            RailroadMark = rrMark;
            raise = true;
            changes.Add(new { Field = "RailroadMark", Value = rrMark });
        }

        if (!string.IsNullOrEmpty(name))
        {
            Name = Name.Create(name);
            raise = true;
            changes.Add(new { Field = "Name", Value = name });
        }

        if (raise)
            Raise(new RailroadUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));

        return this;
    }

    public void Delete()
    {
        Raise(new RailroadDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}