using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Seniority;

public sealed class Craft : Entity
{
    public ControlNumber RailroadPoolCtrlNbr { get; private set; }
    public string CraftName { get; private set; } = string.Empty;
    public string CraftPluralName { get; private set; } = string.Empty;
    public int CraftNumber { get; private set; }
    public bool AutoMarkUp { get; private set; }
    public bool ApproveAllMarkOffs { get; private set; }
    public int MarkOffHours { get; private set; }
    public int MarkUpHours { get; private set; }
    public int RequiredRestHours { get; private set; }
    public int MaximumVacationDayTime { get; private set; }
    public int UnpaidMealPeriodMinutes { get; private set; }
    public bool HoursofService { get; private set; }
    public bool ProcessPayroll { get; private set; }
    public bool ShowNotifications { get; private set; }
    public int VacationAssignmentType { get; private set; }

    private Craft()
    {
        RailroadPoolCtrlNbr = null!;
    }

    private Craft(
        ControlNumber railroadPoolCtrlNbr,
        string craftName,
        string craftPluralName,
        int craftNumber,
        bool autoMarkUp,
        bool approveAllMarkOffs,
        int markOffHours,
        int markUpHours,
        int requiredRestHours,
        int maximumVacationDayTime,
        int unpaidMealPeriodMinutes,
        bool hoursofService,
        bool processPayroll,
        bool showNotifications,
        int vacationAssignmentType)
    {
        RailroadPoolCtrlNbr = railroadPoolCtrlNbr;
        CraftName = craftName;
        CraftPluralName = craftPluralName;
        CraftNumber = craftNumber;
        AutoMarkUp = autoMarkUp;
        ApproveAllMarkOffs = approveAllMarkOffs;
        MarkOffHours = markOffHours;
        MarkUpHours = markUpHours;
        RequiredRestHours = requiredRestHours;
        MaximumVacationDayTime = maximumVacationDayTime;
        UnpaidMealPeriodMinutes = unpaidMealPeriodMinutes;
        HoursofService = hoursofService;
        ProcessPayroll = processPayroll;
        ShowNotifications = showNotifications;
        VacationAssignmentType = vacationAssignmentType;
    }

    public static Craft Create(
        long railroadPoolCtrlNbr,
        string craftName,
        string craftPluralName,
        int craftNumber,
        bool autoMarkUp,
        bool approveAllMarkOffs,
        int markOffHours,
        int markUpHours,
        int requiredRestHours,
        int maximumVacationDayTime,
        int unpaidMealPeriodMinutes,
        bool hoursofService,
        bool processPayroll,
        bool showNotifications,
        int vacationAssignmentType)
    {
        return new Craft(
            ControlNumber.Create(railroadPoolCtrlNbr),
            craftName,
            craftPluralName,
            craftNumber,
            autoMarkUp,
            approveAllMarkOffs,
            markOffHours,
            markUpHours,
            requiredRestHours,
            maximumVacationDayTime,
            unpaidMealPeriodMinutes,
            hoursofService,
            processPayroll,
            showNotifications,
            vacationAssignmentType);
    }

    public Craft Update(
        string? craftName = null,
        string? craftPluralName = null,
        int? craftNumber = null,
        bool? autoMarkUp = null,
        bool? approveAllMarkOffs = null,
        int? markOffHours = null,
        int? markUpHours = null,
        int? requiredRestHours = null,
        int? maximumVacationDayTime = null,
        int? unpaidMealPeriodMinutes = null,
        bool? hoursofService = null,
        bool? processPayroll = null,
        bool? showNotifications = null,
        int? vacationAssignmentType = null)
    {
        if (craftName is not null) CraftName = craftName;
        if (craftPluralName is not null) CraftPluralName = craftPluralName;
        if (craftNumber is not null) CraftNumber = craftNumber.Value;
        if (autoMarkUp is not null) AutoMarkUp = autoMarkUp.Value;
        if (approveAllMarkOffs is not null) ApproveAllMarkOffs = approveAllMarkOffs.Value;
        if (markOffHours is not null) MarkOffHours = markOffHours.Value;
        if (markUpHours is not null) MarkUpHours = markUpHours.Value;
        if (requiredRestHours is not null) RequiredRestHours = requiredRestHours.Value;
        if (maximumVacationDayTime is not null) MaximumVacationDayTime = maximumVacationDayTime.Value;
        if (unpaidMealPeriodMinutes is not null) UnpaidMealPeriodMinutes = unpaidMealPeriodMinutes.Value;
        if (hoursofService is not null) HoursofService = hoursofService.Value;
        if (processPayroll is not null) ProcessPayroll = processPayroll.Value;
        if (showNotifications is not null) ShowNotifications = showNotifications.Value;
        if (vacationAssignmentType is not null) VacationAssignmentType = vacationAssignmentType.Value;

        return this;
    }
}