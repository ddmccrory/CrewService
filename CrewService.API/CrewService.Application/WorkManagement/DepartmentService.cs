using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.WorkManagement;

public sealed class DepartmentService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    private const int DefaultCallLeadMinutes = 90;
    private const int DefaultCallDurationMinutes = 30;
    private const int DefaultGlobalPreCreateOffsetMinutes = -720;

    public async Task<List<Department>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? dynamicGroupCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.Departments.GetByParentAndRailroadAsync(parentCtrlNbr, dynamicGroupCtrlNbr);
    }

    public async Task<Department> CreateAsync(ControlNumber? parentCtrlNbr, ControlNumber? dynamicGroupCtrlNbr, string name, string defaultCallSheetView)
    {
        await using var uow = await uowFactory.CreateAsync();
        var department = Department.Create(parentCtrlNbr, dynamicGroupCtrlNbr, name, defaultCallSheetView);
        uow.Departments.Add(department);

        var defaultRule = CallSheetRule.Create(
            department.CtrlNbr,
            DefaultCallLeadMinutes,
            DefaultCallDurationMinutes,
            CallSheetHolidayAdjustmentType.None,
            holidayCustomOffsetMinutes: null,
            DefaultGlobalPreCreateOffsetMinutes,
            isEnabled: true);

        await uow.CallSheetRules.AddAsync(defaultRule);
        await uow.CommitAsync();
        return department;
    }

    public async Task<Department> UpdateAsync(ControlNumber ctrlNbr, string name, string defaultCallSheetView)
    {
        await using var uow = await uowFactory.CreateAsync();
        var department = await uow.Departments.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Department {ctrlNbr} not found.");
        department.Update(name, defaultCallSheetView);
        uow.Departments.Update(department);
        await uow.CommitAsync();
        return department;
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var department = await uow.Departments.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Department {ctrlNbr} not found.");

        var callSheetRule = await uow.CallSheetRules.GetByDepartmentAsync(ctrlNbr);
        if (callSheetRule is not null)
            uow.CallSheetRules.Remove(callSheetRule);

        uow.Departments.Remove(department);
        await uow.CommitAsync();
    }
}
