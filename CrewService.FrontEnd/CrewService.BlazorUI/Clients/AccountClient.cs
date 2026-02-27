using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

internal sealed class AccountClient(GrpcChannelProvider channelProvider, IHttpContextAccessor httpContextAccessor, ILogger<AccountClient> logger) 
: BaseGrpcClient<AccountSrvc.AccountSrvcClient>(channelProvider, httpContextAccessor, callInvoker => new AccountSrvc.AccountSrvcClient(callInvoker), logger)
{
    #region Methods

    public async Task<ThemeResponse> SaveThemeAsync(string mode, string theme)
    {
        try
        {
            ValidateThemeInputs(mode, theme);

            ThemeRequest request = new()
            {
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
