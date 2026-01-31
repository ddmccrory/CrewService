using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Railroads
{
    public sealed class RailroadEmployee : Entity
    {
        public ControlNumber EmployeeCtrlNbr { get; private set; }
        public ControlNumber RailroadCtrlNbr { get; private set; }
        public bool AssignedPoolsOnly { get; private set; }

        private RailroadEmployee()
        {
            EmployeeCtrlNbr = ControlNumber.Create();
            RailroadCtrlNbr = ControlNumber.Create();
        }

        private RailroadEmployee(ControlNumber employeeCtrlNbr, ControlNumber railroadCtrlNbr, bool assignedPoolsOnly)
        {
            EmployeeCtrlNbr = employeeCtrlNbr;
            RailroadCtrlNbr = railroadCtrlNbr;
            AssignedPoolsOnly = assignedPoolsOnly;
        }

        public static RailroadEmployee Create(long employeeCtrlNbr, long railroadCtrlNbr, bool assignedPoolsOnly = false)
        {
            return new RailroadEmployee(ControlNumber.Create(employeeCtrlNbr), ControlNumber.Create(railroadCtrlNbr), assignedPoolsOnly);
        }

        public RailroadEmployee Update(bool? assignedPoolsOnly = null)
        {
            if (assignedPoolsOnly.HasValue)
                AssignedPoolsOnly = assignedPoolsOnly.Value;

            return this;
        }
    }
}
