using CrewService.Application.SeniorityOps;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class CraftService(CraftAppService craftAppService) : CraftSrvc.CraftSrvcBase
{
    public override async Task<GetAllCraftResponse> GetAllAsync(GetAllCraftRequest request, ServerCallContext context)
    {
        var response = new GetAllCraftResponse();

        var crafts = await craftAppService.GetAllCraftsAsync(
            request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null,
            request.DynamicGroupCtrlNbr > 0 ? ControlNumber.Create(request.DynamicGroupCtrlNbr) : null);

        foreach (var craft in crafts)
            response.Crafts.Add(MapToResponse(craft));

        return response;
    }

    public override async Task<CraftResponse> GetAsync(GetCraftRequest request, ServerCallContext context)
    {
        try
        {
            var craft = await craftAppService.GetCraftAsync(ControlNumber.Create(request.CtrlNbr));
            return MapToResponse(craft);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<CraftResponse> CreateAsync(CreateCraftRequest request, ServerCallContext context)
    {
        var (craft, _, _) = await craftAppService.CreateCraftAsync(
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
            request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null,
            workAreaCtrlNbr: null,
            createStandardRoster: request.HasCreateStandardRoster ? request.CreateStandardRoster : null,
            createExtraBoard: request.HasCreateExtraBoard ? request.CreateExtraBoard : null,
            createHangoutBoard: request.HasCreateHangoutBoard ? request.CreateHangoutBoard : null,
            createExtendedAbsenceBoard: request.HasCreateExtendedAbsenceBoard ? request.CreateExtendedAbsenceBoard : null,
            createTrainingRoster: request.HasCreateTrainingRoster ? request.CreateTrainingRoster : null,
            createNewHiresBoard: request.HasCreateNewHiresBoard ? request.CreateNewHiresBoard : null,
            standardRosterName: request.StandardRosterName,
            standardRosterPluralName: request.StandardRosterPluralName,
            trainingRosterName: request.TrainingRosterName,
            trainingRosterPluralName: request.TrainingRosterPluralName,
            extraBoardName: request.ExtraBoardName,
            hangoutBoardName: request.HangoutBoardName,
            extendedAbsenceBoardName: request.ExtendedAbsenceBoardName,
            newHiresBoardName: request.NewHiresBoardName);

        return MapToResponse(craft);
    }

    public override async Task<CraftResponse> UpdateAsync(UpdateCraftRequest request, ServerCallContext context)
    {
        try
        {
            var craft = await craftAppService.UpdateCraftAsync(
                ControlNumber.Create(request.CtrlNbr),
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
                request.DepartmentCtrlNbr);

            return MapToResponse(craft);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteCraftRequest request, ServerCallContext context)
    {
        try
        {
            var ctrlNbr = await craftAppService.DeleteCraftAsync(ControlNumber.Create(request.CtrlNbr));
            return new DeleteResponse { Success = true, Messages = { $"Craft {ctrlNbr.Value} deleted." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static CraftResponse MapToResponse(Craft craft) => new()
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
    };
}
