using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.AbsenceVacancy;

public sealed class AbsenceCode : Entity
{
    public ControlNumber RailroadCtrlNbr { get; private set; } = null!;
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsExcused { get; private set; }
    public bool IsCompensated { get; private set; }
    public bool RequiresApproval { get; private set; }
    public bool IsSystemOnly { get; private set; }
    public bool IsHolidayExempt { get; private set; }
    public decimal? DefaultAutoMarkUpHours { get; private set; }
    public bool IsActive { get; private set; }

    private AbsenceCode() { }

    public static AbsenceCode Create(
        long railroadCtrlNbr,
        string code, string description, bool isExcused, bool isCompensated,
        bool requiresApproval, bool isSystemOnly, bool isHolidayExempt,
        decimal? defaultAutoMarkUpHours, bool isActive)
    {
        return new AbsenceCode
        {
            RailroadCtrlNbr = ControlNumber.Create(railroadCtrlNbr),
            Code = NormalizeCode(code),
            Description = description,
            IsExcused = isExcused,
            IsCompensated = isCompensated,
            RequiresApproval = requiresApproval,
            IsSystemOnly = isSystemOnly,
            IsHolidayExempt = isHolidayExempt,
            DefaultAutoMarkUpHours = defaultAutoMarkUpHours,
            IsActive = isActive
        };
    }

    public void Update(
        string? code = null, string? description = null, bool? isExcused = null, bool? isCompensated = null,
        bool? requiresApproval = null, bool? isSystemOnly = null, bool? isHolidayExempt = null,
        decimal? defaultAutoMarkUpHours = null, bool? isActive = null)
    {
        if (code is not null) Code = NormalizeCode(code);
        if (description is not null) Description = description;
        if (isExcused.HasValue) IsExcused = isExcused.Value;
        if (isCompensated.HasValue) IsCompensated = isCompensated.Value;
        if (requiresApproval.HasValue) RequiresApproval = requiresApproval.Value;
        if (isSystemOnly.HasValue) IsSystemOnly = isSystemOnly.Value;
        if (isHolidayExempt.HasValue) IsHolidayExempt = isHolidayExempt.Value;
        if (defaultAutoMarkUpHours.HasValue) DefaultAutoMarkUpHours = defaultAutoMarkUpHours;
        if (isActive.HasValue) IsActive = isActive.Value;
    }

    private static string NormalizeCode(string code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}
