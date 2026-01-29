using CrewService.Domain.DomainEvents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using System.Collections.ObjectModel;

namespace CrewService.Domain.Models.Parents;

public sealed class Parent : Entity
{
    private readonly Collection<Railroad> _railroads = [];

    public Name Name { get; private set; }

    public IReadOnlyList<Railroad> Railroads => [.. _railroads];

    private Parent(Name name)
    {
        Name = name;
    }

    public static Parent Create(string name)
    {
        var parent = new Parent(Name.Create(name));

        parent.Raise(new ParentCreatedDomainEvent(parent.CtrlNbr));

        return parent;
    }

    public Parent Update(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            Name = Name.Create(name);

            Raise(new ParentCreatedDomainEvent(CtrlNbr));
        }

        return this;
    }
}