using CrewService.Application.Models.UserAccount;

namespace CrewService.Application.Modules.UserAccount;

public interface IUserAccountService
{
    Task<UserAccountDto?> FindByEmailAsync(string email);
    Task<UserAccountDto?> FindByIdAsync(string id);
    Task<(IdentityOperationResult Result, string UserId)> CreateAsync(CreateUserRequest request);
    Task<IdentityOperationResult> UpdateProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLNF);
    Task<IdentityOperationResult> UpdateThemeAsync(string userId, string themeName, string themeMode);
    Task<IdentityOperationResult> UpdateRefreshTokenAsync(string userId, string refreshToken, DateTime expiration);
    Task<IdentityOperationResult> UpdatePrimaryRoleAsync(string userId, string roleId);
    Task<IdentityOperationResult> UpdateEmployeeInfoAsync(string userId, string? employeeNumber, string? onProperty);
    Task<bool> CheckPasswordAsync(string userId, string password);
    Task<(IdentityOperationResult Result, string UserId)> CreateWithoutPasswordAsync(string email);
    Task<bool> HasPasswordAsync(string userId);
    Task<IdentityOperationResult> SetPasswordAsync(string userId, string password);
    Task<IReadOnlyList<UserNameDto>> GetNamesByIdsAsync(IEnumerable<string> userIds);
}
