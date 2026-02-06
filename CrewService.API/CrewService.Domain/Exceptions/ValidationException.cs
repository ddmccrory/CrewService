namespace CrewService.Domain.Exceptions;

public sealed class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("VALIDATION_FAILED", "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : base("VALIDATION_FAILED", "One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, [message] }
        };
    }
}