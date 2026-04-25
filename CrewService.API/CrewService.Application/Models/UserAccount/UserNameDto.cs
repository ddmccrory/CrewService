namespace CrewService.Application.Models.UserAccount;

public sealed class UserNameDto
{
    public string Id { get; init; } = string.Empty;
    public string? FullNameLNF { get; init; }
    public string? FirstName { get; init; }
    public string? MiddleName { get; init; }
    public string? LastName { get; init; }
}
