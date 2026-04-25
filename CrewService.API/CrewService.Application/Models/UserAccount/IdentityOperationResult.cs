namespace CrewService.Application.Models.UserAccount;

public sealed class IdentityOperationResult
{
    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static IdentityOperationResult Success { get; } = new() { Succeeded = true };

    public static IdentityOperationResult Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = [.. errors] };

    public static IdentityOperationResult Failure(string error) =>
        new() { Succeeded = false, Errors = [error] };
}
