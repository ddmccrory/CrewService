using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class QualificationsClient(
    GrpcChannelProvider channelProvider,
    CircuitTokenProvider tokenProvider,
    AppContextService appContext,
    ILogger<QualificationsClient> logger)
    : BaseGrpcClient<QualificationsSrvc.QualificationsSrvcClient>(
        channelProvider,
        tokenProvider,
        appContext,
        callInvoker => new QualificationsSrvc.QualificationsSrvcClient(callInvoker),
        logger)
{
    public async Task<GetQualificationTypesResponse> GetQualificationTypesAsync(long parentCtrlNbr, bool activeOnly = false)
    {
        try
        {
            return await _client.GetQualificationTypesAsync(new GetQualificationTypesRequest
            {
                ParentCtrlNbr = parentCtrlNbr,
                ActiveOnly = activeOnly
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetRegulatoryQualificationsResponse> GetRegulatoryQualificationsAsync()
    {
        try
        {
            return await _client.GetRegulatoryQualificationsAsync(new GetRegulatoryQualificationsRequest());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<QualificationTypeResponse> CreateQualificationTypeAsync(CreateQualificationTypeRequest request)
    {
        try
        {
            return await _client.CreateQualificationTypeAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<QualificationTypeResponse> UpdateQualificationTypeAsync(UpdateQualificationTypeRequest request)
    {
        try
        {
            return await _client.UpdateQualificationTypeAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> DeleteQualificationTypeAsync(long qualificationTypeCtrlNbr)
    {
        try
        {
            return await _client.DeleteQualificationTypeAsync(new DeleteQualificationTypeRequest
            {
                QualificationTypeCtrlNbr = qualificationTypeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetQualificationRequirementsResponse> GetQualificationRequirementsAsync(long qualificationTypeCtrlNbr)
    {
        try
        {
            return await _client.GetQualificationRequirementsAsync(new GetQualificationRequirementsRequest
            {
                QualificationTypeCtrlNbr = qualificationTypeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<QualificationRequirementResponse> AddQualificationRequirementAsync(AddQualificationRequirementRequest request)
    {
        try
        {
            return await _client.AddQualificationRequirementAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<QualificationRequirementResponse> UpdateQualificationRequirementAsync(UpdateQualificationRequirementRequest request)
    {
        try
        {
            return await _client.UpdateQualificationRequirementAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> RemoveQualificationRequirementAsync(long RequirementCtrlNbr)
    {
        try
        {
            return await _client.RemoveQualificationRequirementAsync(new RemoveQualificationRequirementRequest
            {
                RequirementCtrlNbr = RequirementCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<QualificationTypeResponse> SetQualificationTypeActiveAsync(long qualificationTypeCtrlNbr, bool isActive)
    {
        try
        {
            return await _client.SetQualificationTypeActiveAsync(new SetQualificationTypeActiveRequest
            {
                QualificationTypeCtrlNbr = qualificationTypeCtrlNbr,
                IsActive = isActive
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetEmployeeQualificationsResponse> GetEmployeeQualificationsAsync(long employeeCtrlNbr)
    {
        try
        {
            return await _client.GetEmployeeQualificationsAsync(new GetEmployeeQualificationsRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<EmployeeQualificationResponse> GrantEmployeeQualificationAsync(GrantEmployeeQualificationRequest request)
    {
        try
        {
            return await _client.GrantEmployeeQualificationAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<EmployeeQualificationResponse> RevokeEmployeeQualificationAsync(long employeeQualificationCtrlNbr, string reason)
    {
        try
        {
            return await _client.RevokeEmployeeQualificationAsync(new RevokeEmployeeQualificationRequest
            {
                EmployeeQualificationCtrlNbr = employeeQualificationCtrlNbr,
                Reason = reason
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CheckEligibilityResponse> CheckEligibilityAsync(long employeeCtrlNbr, long positionSlotCtrlNbr)
    {
        try
        {
            return await _client.CheckEligibilityAsync(new CheckEligibilityRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr,
                PositionSlotCtrlNbr = positionSlotCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetEligibleEmployeesForCraftRoleResponse> GetEligibleEmployeesForCraftRoleAsync(long craftRoleCtrlNbr, long clientCtrlNbr)
    {
        try
        {
            return await _client.GetEligibleEmployeesForCraftRoleAsync(new GetEligibleEmployeesForCraftRoleRequest
            {
                CraftRoleCtrlNbr = craftRoleCtrlNbr,
                ClientCtrlNbr = clientCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
