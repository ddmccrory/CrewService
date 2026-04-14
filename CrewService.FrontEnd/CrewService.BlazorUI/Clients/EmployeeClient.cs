using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class EmployeeClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<EmployeeClient> logger)
: BaseGrpcClient<EmployeeSrvc.EmployeeSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new EmployeeSrvc.EmployeeSrvcClient(callInvoker), logger)
{
    #region Employee

    public async Task<GetAllEmployeesResponse> GetAllAsync(long clientCtrlNbr = 0)
    {
        try
        {
            return await _client.GetAllEmployeesAsyncAsync(new GetAllEmployeesRequest
            {
                PageSize = 1000,
                ClientCtrlNbr = clientCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetEmployeeResponse> GetByNumberAsync(string employeeNumber)
    {
        try
        {
            return await _client.GetEmployeeByNumberAsyncAsync(new GetEmployeeByNumberRequest { EmployeeNumber = employeeNumber });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetEmployeeResponse> GetByCtrlNbrAsync(long ctrlNbr)
    {
        try
        {
            return await _client.GetEmployeeAsyncAsync(new GetEmployeeRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CreateEmployeeResponse> CreateAsync(CreateEmployeeRequest request)
    {
        try
        {
            return await _client.CreateEmployeeAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<UpdateEmployeeResponse> UpdateAsync(UpdateEmployeeRequest request)
    {
        try
        {
            return await _client.UpdateEmployeeAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteEmployeeResponse> DeleteAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteEmployeeAsyncAsync(new DeleteEmployeeRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    #endregion

    #region Addresses

    public async Task<AddressResponse> AddAddressAsync(AddAddressRequest request)
    {
        try
        {
            return await _client.AddAddressAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<AddressResponse> UpdateAddressAsync(UpdateAddressRequest request)
    {
        try
        {
            return await _client.UpdateAddressAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> DeleteAddressAsync(long employeeCtrlNbr, long ctrlNbr)
    {
        try
        {
            return await _client.DeleteAddressAsyncAsync(new DeleteAddressRequest { EmployeeCtrlNbr = employeeCtrlNbr, CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    #endregion

    #region Phone Numbers

    public async Task<PhoneNumberResponse> AddPhoneNumberAsync(AddPhoneNumberRequest request)
    {
        try
        {
            return await _client.AddPhoneNumberAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<PhoneNumberResponse> UpdatePhoneNumberAsync(UpdatePhoneNumberRequest request)
    {
        try
        {
            return await _client.UpdatePhoneNumberAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> DeletePhoneNumberAsync(long employeeCtrlNbr, long ctrlNbr)
    {
        try
        {
            return await _client.DeletePhoneNumberAsyncAsync(new DeletePhoneNumberRequest { EmployeeCtrlNbr = employeeCtrlNbr, CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    #endregion

    #region Email Addresses

    public async Task<EmailAddressResponse> AddEmailAddressAsync(AddEmailAddressRequest request)
    {
        try
        {
            return await _client.AddEmailAddressAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<EmailAddressResponse> UpdateEmailAddressAsync(UpdateEmailAddressRequest request)
    {
        try
        {
            return await _client.UpdateEmailAddressAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> DeleteEmailAddressAsync(long employeeCtrlNbr, long ctrlNbr)
    {
        try
        {
            return await _client.DeleteEmailAddressAsyncAsync(new DeleteEmailAddressRequest { EmployeeCtrlNbr = employeeCtrlNbr, CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    #endregion
}
