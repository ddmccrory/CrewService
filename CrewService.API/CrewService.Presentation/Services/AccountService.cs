using CrewService.Infrastructure.Models.UserAccount;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;

namespace CrewService.Presentation.Services;

public sealed class AccountService(UserManager<User> userManager) : AccountSrvc.AccountSrvcBase
{
    private readonly UserManager<User> _userManager = userManager;

    public override async Task<ThemeResponse> ModifyTheme(ThemeRequest request, ServerCallContext context)
    {
        ThemeResponse response = new();

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
                user.ThemeName = request.ThemeName;
                user.ThemeMode = request.ThemeMode;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    response.Success = true;
                    response.Message.Add($"User theme has successfully modified to {request.ThemeName} ({request.ThemeMode}).");
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
