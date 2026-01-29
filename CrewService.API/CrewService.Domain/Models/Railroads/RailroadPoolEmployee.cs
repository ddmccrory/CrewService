using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Railroads;

/// <summary>
/// Represents an employee assigned to a railroad pool.
/// </summary>
public sealed class RailroadPoolEmployee : Entity
{
    public ControlNumber RailroadPoolCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public bool IsActive { get; private set; }

    private RailroadPoolEmployee()
    {
        RailroadPoolCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    private RailroadPoolEmployee(ControlNumber railroadPoolCtrlNbr, ControlNumber employeeCtrlNbr, bool isActive)
    {
        RailroadPoolCtrlNbr = railroadPoolCtrlNbr;
        EmployeeCtrlNbr = employeeCtrlNbr;
        IsActive = isActive;
    }

    public static RailroadPoolEmployee Create(long railroadPoolCtrlNbr, long employeeCtrlNbr, bool isActive = true)
    {
        return new RailroadPoolEmployee(
            ControlNumber.Create(railroadPoolCtrlNbr),
            ControlNumber.Create(employeeCtrlNbr),
            isActive);
    }

    public RailroadPoolEmployee Update(bool? isActive = null)
    {
        if (isActive.HasValue) IsActive = isActive.Value;
        return this;
    }
}