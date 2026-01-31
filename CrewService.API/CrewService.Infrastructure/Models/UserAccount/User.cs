using Microsoft.AspNetCore.Identity;

namespace CrewService.Infrastructure.Models.UserAccount;

public sealed class User : IdentityUser
{
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? FullNameLNF { get; set; }
    public string? ThemeName { get; set; }
    public string? ThemeMode { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
    public string? EmployeeNumber { get; set; }
    public DateTime LastLogin { get; set; }
    public string? IPAddress { get; set; }
    public bool OnProperty { get; set; }
    public string? PrimaryRoleId { get; set; }
}
