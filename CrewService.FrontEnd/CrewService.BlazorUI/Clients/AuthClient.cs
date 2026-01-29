using CrewService.BlazorUI.Models.Account;
using Grpc.Core;


namespace CrewService.BlazorUI.Clients;

internal sealed class AuthClient(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<AccountClient> logger) 
    : BaseGrpcClient<AuthSrvc.AuthSrvcClient>(configuration, httpContextAccessor, callInvoker => new AuthSrvc.AuthSrvcClient(callInvoker), logger, addAuthHeader: false)
{
    #region Methods

    public async Task<RegisterResponse> RegisterUserAsync(RegisterInputModel registerModel)
    {
        try
        { 
        RegisterRequest request = new()
        {
            Email = registerModel.Email,
            Password = registerModel.Password
        };

        return await _client.RegisterUserAsync(request);
        }
        catch (Exception ex)
        {
            base.LogException(ex);
            throw;
        }
    }

    public async Task<AuthResponse> AuthenticateUserAsync(LoginInputModel loginModel)
    {
        try
        { 
        AuthRequest request = new()
        {
            UserName = loginModel.Email,
            Password = loginModel.Password
        };

        return await _client.AuthenticateUserAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex); // Use base class logging
            throw;
        }
    }

    #endregion
}
