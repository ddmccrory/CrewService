using CrewService.Domain.DomainEvents.Railroads;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using System.Collections.ObjectModel;

namespace CrewService.Domain.Models.Railroads;

public sealed class RailroadPool : Entity
{
    private readonly Collection<RailroadPoolPayrollTier> _payrollTiers = [];
    private readonly Collection<RailroadPoolEmployee> _poolEmployees = [];

    public ControlNumber RailroadCtrlNbr { get; private set; }
    public string PoolName { get; private set; } = string.Empty;
    public int PoolNumber { get; private set; }

    public IReadOnlyList<RailroadPoolPayrollTier> PayrollTiers => [.. _payrollTiers];
    public IReadOnlyList<RailroadPoolEmployee> PoolEmployees => [.. _poolEmployees];

    private RailroadPool()
    {
        RailroadCtrlNbr = null!;
    }

    private RailroadPool(ControlNumber railroadCtrlNbr, string poolName, int poolNumber)
    {
        RailroadCtrlNbr = railroadCtrlNbr;
        PoolName = poolName;
        PoolNumber = poolNumber;
    }

    public static RailroadPool Create(long railroadCtrlNbr, string poolName, int poolNumber)
    {
        var entity = new RailroadPool(ControlNumber.Create(railroadCtrlNbr), poolName, poolNumber);
        entity.Raise(new RailroadPoolCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public RailroadPool Update(string? poolName = null, int? poolNumber = null)
    {
        var changes = new Dictionary<string, object?>();

        if (poolName is not null)
        {
            PoolName = poolName;
            changes["poolName"] = poolName;
        }

        if (poolNumber.HasValue)
        {
            PoolNumber = poolNumber.Value;
            changes["poolNumber"] = poolNumber.Value;
        }

        if (changes.Count > 0)
        {
            Raise(new RailroadPoolUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }

        return this;
    }

    public void AddPayrollTier(RailroadPoolPayrollTier tier)
    {
        _payrollTiers.Add(tier);
        Raise(new RailroadPoolUpdatedDomainEvent(CtrlNbr, payload: new { Action = "AddPayrollTier", TierCtrlNbr = tier.CtrlNbr.Value }));
    }

    public void RemovePayrollTier(ControlNumber tierCtrlNbr)
    {
        var tier = _payrollTiers.FirstOrDefault(t => t.CtrlNbr == tierCtrlNbr);
        if (tier is not null)
        {
            _payrollTiers.Remove(tier);
            Raise(new RailroadPoolUpdatedDomainEvent(CtrlNbr, payload: new { Action = "RemovePayrollTier", TierCtrlNbr = tierCtrlNbr.Value }));
        }
    }

    public void AddPoolEmployee(RailroadPoolEmployee employee)
    {
        _poolEmployees.Add(employee);
        Raise(new RailroadPoolUpdatedDomainEvent(CtrlNbr, payload: new { Action = "AddPoolEmployee", EmployeeCtrlNbr = employee.CtrlNbr.Value }));
    }

    public void RemovePoolEmployee(ControlNumber employeeCtrlNbr)
    {
        var emp = _poolEmployees.FirstOrDefault(e => e.CtrlNbr == employeeCtrlNbr);
        if (emp is not null)
        {
            _poolEmployees.Remove(emp);
            Raise(new RailroadPoolUpdatedDomainEvent(CtrlNbr, payload: new { Action = "RemovePoolEmployee", EmployeeCtrlNbr = employeeCtrlNbr.Value }));
        }
    }

    public void Delete()
    {
        Raise(new RailroadPoolDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}