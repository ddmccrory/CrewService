namespace CrewService.Application.Models.UserAccount;

public sealed class UserAccountDto
{
    public string Id { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? MiddleName { get; init; }
    public string? LastName { get; init; }
    public string? FullName { get; init; }
    public string? FullNameLNF { get; init; }
    public string? ThemeName { get; init; }
    public string? ThemeMode { get; init; }
    public string? EmployeeNumber { get; init; }
    public string? PrimaryRoleId { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? RefreshTokenExpiration { get; init; }
}
