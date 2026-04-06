using CrewService.Infrastructure.Models.UserAccount;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CrewService.Presentation.Services;

public sealed partial class AccountService(UserManager<User> userManager, ILogger<AccountService> logger) : AccountSrvc.AccountSrvcBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ILogger<AccountService> _logger = logger;

    public override async Task<GetProfileResponse> GetProfile(GetProfileRequest request, ServerCallContext context)
    {
        GetProfileResponse response = new();

        if (!string.IsNullOrEmpty(request.UserName))
        {
            var user = await _userManager.FindByEmailAsync(request.UserName);

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
            var user = await _userManager.FindByEmailAsync(request.UserName);


            if (user is null)
            {
                _logger.LogWarning("ModifyTheme: User '{UserName}' not found", request.UserName);
                response.Success = false;
                response.Message.Add("User could not be found.");
            }
            else
            {
                user.ThemeName = request.ThemeName;
                user.ThemeMode = request.ThemeMode;

                var result = await _userManager.UpdateAsync(user);

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
                        request.UserName, string.Join("; ", result.Errors.Select(e => e.Description)));
                    response.Success = false;
                    foreach (var erorr in result.Errors)
                        response.Message.Add(erorr.Description);
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
            var user = await _userManager.FindByEmailAsync(request.UserName);

            if (user is null)
            {
                response.Success = false;
                response.Message.Add("User could not be found.");
            }
            else
            {
                user.FirstName = request.FirstName;
                user.MiddleName = request.MiddleName;
                user.LastName = request.LastName;
                user.FullName = FormatFullName(request.FirstName, request.MiddleName, request.LastName, false);
                user.FullNameLNF = FormatFullName(request.FirstName, request.MiddleName, request.LastName, true);

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    response.Success = true;
                    response.FullName = user.FullName ?? string.Empty;
                    response.Message.Add("User profile name has been successfully updated.");
                }
                else
                {
                    response.Success = false;
                    foreach (var erorr in result.Errors)
                        response.Message.Add(erorr.Description);
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

    private static string FormatFullName(string fname, string mname, string lname, bool lnf)
    {
        if (lnf)
            return $"{lname}, {FormatFirstName(fname)} {FormatMiddleName(mname)}";

        return $"{FormatFirstName(fname)} {FormatMiddleName(mname)} {lname}";
    }

    private static string FormatFirstName(string fname)
    {
        fname = fname.Trim('.');

        if (!string.IsNullOrEmpty(fname) && fname.Length is 1)
            fname = $"{fname}.";

        return fname;
    }

    private static string FormatMiddleName(string mname)
    {
        mname = mname.Trim('.');

        if (!string.IsNullOrEmpty(mname))
            mname = $"{mname[..1]}.";

        return mname;
    }
}
