using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class CraftService(ICraftRepository craftRepository) : CraftSrvc.CraftSrvcBase
{
    private readonly ICraftRepository _craftRepository = craftRepository;

    public override async Task<GetAllCraftResponse> GetAllAsync(GetAllCraftRequest request, ServerCallContext context)
    {
        var response = new GetAllCraftResponse();

        var crafts = request.ParentCtrlNbr > 0
            ? await _craftRepository.GetByParentAndRailroadAsync(
                ControlNumber.Create(request.ParentCtrlNbr),
                request.DynamicGroupCtrlNbr > 0 ? ControlNumber.Create(request.DynamicGroupCtrlNbr) : null)
            : await _craftRepository.GetAllAsync();

        foreach (var craft in crafts)
        {
            response.Crafts.Add(new CraftResponse
            {
                CtrlNbr = craft.CtrlNbr.Value,
                ParentCtrlNbr = craft.ParentCtrlNbr?.Value ?? 0,
                DynamicGroupCtrlNbr = craft.DynamicGroupCtrlNbr?.Value ?? 0,
                CraftName = craft.CraftName,
                CraftPluralName = craft.CraftPluralName,
                CraftNumber = craft.CraftNumber,
                AutoMarkUp = craft.AutoMarkUp,
                ApproveAllMarkOffs = craft.ApproveAllMarkOffs,
                MarkOffHours = craft.MarkOffHours,
                MarkUpHours = craft.MarkUpHours,
                RequiredRestHours = craft.RequiredRestHours,
                MaximumVacationDayTime = craft.MaximumVacationDayTime,
                UnpaidMealPeriodMinutes = craft.UnpaidMealPeriodMinutes,
                HoursofService = craft.HoursofService,
                ProcessPayroll = craft.ProcessPayroll,
                ShowNotifications = craft.ShowNotifications,
                VacationAssignmentType = craft.VacationAssignmentType,
                DepartmentCtrlNbr = craft.DepartmentCtrlNbr?.Value ?? 0
            });
        }

        return await Task.FromResult(response);
    }

    public override async Task<CraftResponse> GetAsync(GetCraftRequest request, ServerCallContext context)
    {
        var craft = await _craftRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Craft, with control number {request.CtrlNbr}, was not found."));

        return await Task.FromResult(new CraftResponse
        {
            CtrlNbr = craft.CtrlNbr.Value,
            ParentCtrlNbr = craft.ParentCtrlNbr?.Value ?? 0,
                DynamicGroupCtrlNbr = craft.DynamicGroupCtrlNbr?.Value ?? 0,
            CraftName = craft.CraftName,
            CraftPluralName = craft.CraftPluralName,
            CraftNumber = craft.CraftNumber,
            AutoMarkUp = craft.AutoMarkUp,
            ApproveAllMarkOffs = craft.ApproveAllMarkOffs,
            MarkOffHours = craft.MarkOffHours,
            MarkUpHours = craft.MarkUpHours,
            RequiredRestHours = craft.RequiredRestHours,
            MaximumVacationDayTime = craft.MaximumVacationDayTime,
            UnpaidMealPeriodMinutes = craft.UnpaidMealPeriodMinutes,
            HoursofService = craft.HoursofService,
            ProcessPayroll = craft.ProcessPayroll,
            ShowNotifications = craft.ShowNotifications,
            VacationAssignmentType = craft.VacationAssignmentType,
            DepartmentCtrlNbr = craft.DepartmentCtrlNbr?.Value ?? 0
        });
    }

    public override async Task<CraftResponse> CreateAsync(CreateCraftRequest request, ServerCallContext context)
    {
        var craft = Craft.Create(
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            request.DynamicGroupCtrlNbr > 0 ? ControlNumber.Create(request.DynamicGroupCtrlNbr) : null,
            request.CraftName,
            request.CraftPluralName,
            request.CraftNumber,
            request.AutoMarkUp,
            request.ApproveAllMarkOffs,
            request.MarkOffHours,
            request.MarkUpHours,
            request.RequiredRestHours,
            request.MaximumVacationDayTime,
            request.UnpaidMealPeriodMinutes,
            request.HoursofService,
            request.ProcessPayroll,
            request.ShowNotifications,
            request.VacationAssignmentType,
            request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null);

        await _craftRepository.AddAsync(craft);

        return await Task.FromResult(new CraftResponse
        {
            CtrlNbr = craft.CtrlNbr.Value,
            ParentCtrlNbr = craft.ParentCtrlNbr?.Value ?? 0,
                DynamicGroupCtrlNbr = craft.DynamicGroupCtrlNbr?.Value ?? 0,
            CraftName = craft.CraftName,
            CraftPluralName = craft.CraftPluralName,
            CraftNumber = craft.CraftNumber,
            AutoMarkUp = craft.AutoMarkUp,
            ApproveAllMarkOffs = craft.ApproveAllMarkOffs,
            MarkOffHours = craft.MarkOffHours,
            MarkUpHours = craft.MarkUpHours,
            RequiredRestHours = craft.RequiredRestHours,
            MaximumVacationDayTime = craft.MaximumVacationDayTime,
            UnpaidMealPeriodMinutes = craft.UnpaidMealPeriodMinutes,
            HoursofService = craft.HoursofService,
            ProcessPayroll = craft.ProcessPayroll,
            ShowNotifications = craft.ShowNotifications,
            VacationAssignmentType = craft.VacationAssignmentType,
            DepartmentCtrlNbr = craft.DepartmentCtrlNbr?.Value ?? 0
        });
    }

    public override async Task<CraftResponse> UpdateAsync(UpdateCraftRequest request, ServerCallContext context)
    {
        var craft = await _craftRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Craft, with control number {request.CtrlNbr}, was not found."));

        craft.Update(
            request.CraftName,
            request.CraftPluralName,
            request.CraftNumber,
            request.AutoMarkUp,
            request.ApproveAllMarkOffs,
            request.MarkOffHours,
            request.MarkUpHours,
            request.RequiredRestHours,
            request.MaximumVacationDayTime,
            request.UnpaidMealPeriodMinutes,
            request.HoursofService,
            request.ProcessPayroll,
            request.ShowNotifications,
            request.VacationAssignmentType,
            departmentCtrlNbr: request.DepartmentCtrlNbr);

        await _craftRepository.UpdateAsync(craft);

        return await Task.FromResult(new CraftResponse
        {
            CtrlNbr = craft.CtrlNbr.Value,
            ParentCtrlNbr = craft.ParentCtrlNbr?.Value ?? 0,
                DynamicGroupCtrlNbr = craft.DynamicGroupCtrlNbr?.Value ?? 0,
            CraftName = craft.CraftName,
            CraftPluralName = craft.CraftPluralName,
            CraftNumber = craft.CraftNumber,
            AutoMarkUp = craft.AutoMarkUp,
            ApproveAllMarkOffs = craft.ApproveAllMarkOffs,
            MarkOffHours = craft.MarkOffHours,
            MarkUpHours = craft.MarkUpHours,
            RequiredRestHours = craft.RequiredRestHours,
            MaximumVacationDayTime = craft.MaximumVacationDayTime,
            UnpaidMealPeriodMinutes = craft.UnpaidMealPeriodMinutes,
            HoursofService = craft.HoursofService,
            ProcessPayroll = craft.ProcessPayroll,
            ShowNotifications = craft.ShowNotifications,
            VacationAssignmentType = craft.VacationAssignmentType,
            DepartmentCtrlNbr = craft.DepartmentCtrlNbr?.Value ?? 0
        });
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteCraftRequest request, ServerCallContext context)
    {
        var craft = await _craftRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Craft, with control number {request.CtrlNbr}, was not found."));

        await _craftRepository.DeleteAsync(craft.CtrlNbr);

        return await Task.FromResult(new DeleteResponse
        {
            Success = true,
            Messages = { $"Craft {craft.CtrlNbr.Value} deleted." }
        });
    }
}