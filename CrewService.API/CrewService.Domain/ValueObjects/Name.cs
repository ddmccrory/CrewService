namespace CrewService.Domain.ValueObjects;

public sealed record Name
{
    public string Value { get; }

    private Name(string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        Value = value;
    }

    public static Name Create(string? value)
    {
        return new Name(value);
    }
}
