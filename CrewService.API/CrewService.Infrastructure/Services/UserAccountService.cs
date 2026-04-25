using CrewService.Application.Models.UserAccount;
using CrewService.Application.Modules.UserAccount;
using CrewService.Infrastructure.Models.UserAccount;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Infrastructure.Services;

internal sealed class UserAccountService(UserManager<User> userManager) : IUserAccountService
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<UserAccountDto?> FindByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserAccountDto?> FindByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user is null ? null : MapToDto(user);
    }

    public async Task<(IdentityOperationResult Result, string UserId)> CreateAsync(CreateUserRequest request)
    {
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        return result.Succeeded
            ? (IdentityOperationResult.Success, user.Id)
            : (IdentityOperationResult.Failure(result.Errors.Select(e => e.Description)), string.Empty);
    }

    public async Task<IdentityOperationResult> UpdateProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLNF)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return IdentityOperationResult.Failure("User not found.");

        user.FirstName = firstName;
        user.MiddleName = middleName;
        user.LastName = lastName;
        user.FullName = fullName;
        user.FullNameLNF = fullNameLNF;

        return ToResult(await _userManager.UpdateAsync(user));
    }

    public async Task<IdentityOperationResult> UpdateThemeAsync(string userId, string themeName, string themeMode)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return IdentityOperationResult.Failure("User not found.");

        user.ThemeName = themeName;
        user.ThemeMode = themeMode;

        return ToResult(await _userManager.UpdateAsync(user));
    }

    public async Task<IdentityOperationResult> UpdateRefreshTokenAsync(string userId, string refreshToken, DateTime expiration)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return IdentityOperationResult.Failure("User not found.");

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiration = expiration;

        return ToResult(await _userManager.UpdateAsync(user));
    }

    public async Task<IdentityOperationResult> UpdatePrimaryRoleAsync(string userId, string roleId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return IdentityOperationResult.Failure("User not found.");

        user.PrimaryRoleId = roleId;

        return ToResult(await _userManager.UpdateAsync(user));
    }

    public async Task<IdentityOperationResult> UpdateEmployeeInfoAsync(string userId, string? employeeNumber, string? onProperty)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return IdentityOperationResult.Failure("User not found.");

        user.EmployeeNumber = employeeNumber;

        return ToResult(await _userManager.UpdateAsync(user));
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IReadOnlyList<UserNameDto>> GetNamesByIdsAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0) return [];

        return await _userManager.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserNameDto
            {
                Id = u.Id,
                FullNameLNF = u.FullNameLNF,
                FirstName = u.FirstName,
                MiddleName = u.MiddleName,
                LastName = u.LastName
            })
            .ToListAsync();
    }

    private static UserAccountDto MapToDto(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        FirstName = user.FirstName,
        MiddleName = user.MiddleName,
        LastName = user.LastName,
        FullName = user.FullName,
        FullNameLNF = user.FullNameLNF,
        ThemeName = user.ThemeName,
        ThemeMode = user.ThemeMode,
        EmployeeNumber = user.EmployeeNumber,
        PrimaryRoleId = user.PrimaryRoleId,
        RefreshToken = user.RefreshToken,
        RefreshTokenExpiration = user.RefreshTokenExpiration
    };

    private static IdentityOperationResult ToResult(IdentityResult result) =>
        result.Succeeded
            ? IdentityOperationResult.Success
            : IdentityOperationResult.Failure(result.Errors.Select(e => e.Description));
}
