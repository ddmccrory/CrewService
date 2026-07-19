using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.WorkManagement;

public sealed class DepartmentService(IOrchestrationUnitOfWorkFactory uowFactory, ICurrentUserService currentUserService)
{
    private const int DefaultCallLeadMinutes = 90;
    private const int DefaultCallDurationMinutes = 30;
    private const int DefaultGlobalPreCreateOffsetMinutes = -720;

    public async Task<List<Department>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? dynamicGroupCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();

        var departments = await uow.Departments.GetByParentAndRailroadAsync(parentCtrlNbr, dynamicGroupCtrlNbr);

        if (!currentUserService.IsInRole(Roles.Employee))
            return departments;

        var userId = currentUserService.GetUserIdentifier();
        if (string.IsNullOrWhiteSpace(userId))
            return [];

        var employee = await uow.Employees.GetByUserIdAsync(userId);
        if (employee is null)
            return [];

        var employeeSeniority = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employee.CtrlNbr);
        var rosterCtrlNbrs = employeeSeniority
            .Select(s => s.RosterCtrlNbr)
            .Distinct()
            .ToList();

        if (rosterCtrlNbrs.Count == 0)
            return [];

        var rosters = await uow.Rosters.GetByCtrlNbrsAsync(rosterCtrlNbrs);

        var craftCtrlNbrs = rosters
            .Select(r => r.CraftCtrlNbr)
            .Distinct()
            .ToList();

        var crafts = await uow.Crafts.GetByCtrlNbrsAsync(craftCtrlNbrs);

        var allowedDepartmentCtrlNbrs = crafts
            .Where(c => c.DepartmentCtrlNbr is not null)
            .Select(c => c.DepartmentCtrlNbr!)
            .ToHashSet();

        return departments
            .Where(d => allowedDepartmentCtrlNbrs.Contains(d.CtrlNbr))
            .ToList();
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

        var defaultReassignmentRule = DepartmentReassignmentRule.Create(
            department.CtrlNbr,
            BoardType.Hangout,
            isRequired: true);

        await uow.DepartmentReassignmentRules.AddAsync(defaultReassignmentRule);
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

        uow.Departments.Remove(department);
        await uow.CommitAsync();
    }
}
