using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.WorkManagement;

public sealed class DepartmentService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<List<Department>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? dynamicGroupCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.Departments.GetByParentAndRailroadAsync(parentCtrlNbr, dynamicGroupCtrlNbr);
    }

    public async Task<Department> CreateAsync(ControlNumber? parentCtrlNbr, ControlNumber? dynamicGroupCtrlNbr, string name, string defaultCallSheetView)
    {
        var department = Department.Create(parentCtrlNbr, dynamicGroupCtrlNbr, name, defaultCallSheetView);
        await using var uow = await uowFactory.CreateAsync();
        uow.Departments.Add(department);
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
