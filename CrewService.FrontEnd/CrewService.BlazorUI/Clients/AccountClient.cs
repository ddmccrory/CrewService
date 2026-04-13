using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

internal sealed class AccountClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<AccountClient> logger) 
: BaseGrpcClient<AccountSrvc.AccountSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new AccountSrvc.AccountSrvcClient(callInvoker), logger)
{
    #region Methods

    public async Task<GetProfileResponse> GetProfileAsync(string userName)
    {
        try
        {
            return await _client.GetProfileAsync(new GetProfileRequest { UserName = userName ?? string.Empty });
        }
        catch (Exception ex)
        {
            base.LogException(ex);
            throw;
        }
    }

    public async Task<ProfileResponse> SaveProfileAsync(string userName, string firstName, string middleName, string lastName)
    {
        try
        {
            ProfileRequest request = new()
            {
                UserName = userName ?? string.Empty,
                FirstName = firstName ?? string.Empty,
                MiddleName = middleName ?? string.Empty,
                LastName = lastName ?? string.Empty
            };

            return await _client.ModifyProfileAsync(request);
        }
        catch (Exception ex)
        {
            base.LogException(ex);
            throw;
        }
    }

    public async Task<ThemeResponse> SaveThemeAsync(string userName, string mode, string theme)
    {
        try
        {
            ValidateThemeInputs(mode, theme);

            ThemeRequest request = new()
            {
                UserName = userName ?? string.Empty,
                ThemeName = theme,
                ThemeMode = mode
            };

            return await _client.ModifyThemeAsync(request);
        }
        catch (Exception ex)
        {
            base.LogException(ex);
            throw;
        }
    }

    private static void ValidateThemeInputs(string mode, string theme)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new ArgumentException("Theme mode cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(theme))
        {
            throw new ArgumentException("Theme name cannot be null or empty.");
        }
    }

    #endregion
}
