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
        return new RailroadPool(ControlNumber.Create(railroadCtrlNbr), poolName, poolNumber);
    }

    public RailroadPool Update(string? poolName = null, int? poolNumber = null)
    {
        if (poolName is not null) PoolName = poolName;
        if (poolNumber.HasValue) PoolNumber = poolNumber.Value;
        return this;
    }

    public void AddPayrollTier(RailroadPoolPayrollTier tier)
    {
        _payrollTiers.Add(tier);
    }

    public void RemovePayrollTier(ControlNumber tierCtrlNbr)
    {
        var tier = _payrollTiers.FirstOrDefault(t => t.CtrlNbr == tierCtrlNbr);
        if (tier is not null) _payrollTiers.Remove(tier);
    }

    public void AddPoolEmployee(RailroadPoolEmployee employee)
    {
        _poolEmployees.Add(employee);
    }

    public void RemovePoolEmployee(ControlNumber employeeCtrlNbr)
    {
        var emp = _poolEmployees.FirstOrDefault(e => e.CtrlNbr == employeeCtrlNbr);
        if (emp is not null) _poolEmployees.Remove(emp);
    }
}