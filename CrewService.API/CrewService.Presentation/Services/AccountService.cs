using CrewService.Application.Modules.UserAccount;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace CrewService.Presentation.Services;

public sealed partial class AccountService(IUserAccountService userAccountService, ILogger<AccountService> logger) : AccountSrvc.AccountSrvcBase
{
    private readonly IUserAccountService _userAccountService = userAccountService;
    private readonly ILogger<AccountService> _logger = logger;

    public override async Task<GetProfileResponse> GetProfile(GetProfileRequest request, ServerCallContext context)
    {
        GetProfileResponse response = new();

        if (!string.IsNullOrEmpty(request.UserName))
        {
            var user = await _userAccountService.FindByEmailAsync(request.UserName);

            if (user is null)
            {
                response.Success = false;
                response.Message.Add("User could not be found.");
            }
            else
            {
                response.FirstName = user.FirstName ?? string.Empty;
                response.MiddleName = user.MiddleName ?? string.Empty;
                response.LastName = user.LastName ?? string.Empty;
                response.FullName = user.FullName ?? string.Empty;
                response.Success = true;
            }
        }
        else
        {
            response.Success = false;
            response.Message.Add("User Name is required.");
        }

        return response;
    }

    public override async Task<ThemeResponse> ModifyTheme(ThemeRequest request, ServerCallContext context)
    {
        ThemeResponse response = new();

        _logger.LogInformation("ModifyTheme called for user '{UserName}' with theme '{ThemeName}' mode '{ThemeMode}'",
            request.UserName, request.ThemeName, request.ThemeMode);

        if (!string.IsNullOrEmpty(request.UserName))
        {
            var user = await _userAccountService.FindByEmailAsync(request.UserName);

            if (user is null)
            {
                _logger.LogWarning("ModifyTheme: User '{UserName}' not found", request.UserName);
                response.Success = false;
                response.Message.Add("User could not be found.");
            }
            else
            {
                var result = await _userAccountService.UpdateThemeAsync(user.Id, request.ThemeName, request.ThemeMode);

                if (result.Succeeded)
                {
                    _logger.LogInformation("ModifyTheme: Successfully saved theme '{ThemeName}' mode '{ThemeMode}' for user '{UserName}'",
                        request.ThemeName, request.ThemeMode, request.UserName);
                    response.Success = true;
                    response.Message.Add($"User theme has successfully modified to {request.ThemeName} ({request.ThemeMode}).");
                }
                else
                {
                    _logger.LogError("ModifyTheme: UpdateAsync failed for user '{UserName}': {Errors}",
                        request.UserName, string.Join("; ", result.Errors));
                    response.Success = false;
                    foreach (var error in result.Errors)
                        response.Message.Add(error);
                }
            }
        }
        else
        {
            _logger.LogWarning("ModifyTheme: UserName is empty");
            response.Success = false;
            response.Message.Add("User Name is required.");
        }

        return response;
    }

    public override async Task<ProfileResponse> ModifyProfile(ProfileRequest request, ServerCallContext context)
    {
        ProfileResponse response = new();

        if (!string.IsNullOrEmpty(request.UserName))
        {
            var user = await _userAccountService.FindByEmailAsync(request.UserName);

            if (user is null)
            {
                response.Success = false;
                response.Message.Add("User could not be found.");
            }
            else
            {
                var fullName = EmployeeNameService.FormatFullName(request.FirstName, request.MiddleName, request.LastName);
                var fullNameLNF = EmployeeNameService.FormatFullNameLnf(request.FirstName, request.MiddleName, request.LastName);

                var result = await _userAccountService.UpdateProfileAsync(
                    user.Id, request.FirstName, request.MiddleName, request.LastName, fullName, fullNameLNF);

                if (result.Succeeded)
                {
                    response.Success = true;
                    response.FullName = fullName;
                    response.Message.Add("User profile name has been successfully updated.");
                }
                else
                {
                    response.Success = false;
                    foreach (var error in result.Errors)
                        response.Message.Add(error);
                }
            }
        }
        else
        {
            response.Success = false;
            response.Message.Add("User Name is required.");
        }

        return response;
    }

}
