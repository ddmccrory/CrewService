using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Payroll;

public sealed class EarningCodeRule : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public int Priority { get; private set; }
    public string ConditionsJson { get; private set; } = string.Empty;
    public string ResultCode { get; private set; } = string.Empty;
    public bool RequiresApproval { get; private set; }
    public bool IsActive { get; private set; }

    private EarningCodeRule() { WorkAreaGroupCtrlNbr = null!; }

    public static EarningCodeRule Create(
        ControlNumber workAreaGroupCtrlNbr, int priority,
        string conditionsJson, string resultCode,
        bool requiresApproval, bool isActive)
    {
        return new EarningCodeRule
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            Priority = priority,
            ConditionsJson = conditionsJson,
            ResultCode = resultCode,
            RequiresApproval = requiresApproval,
            IsActive = isActive,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }
}
