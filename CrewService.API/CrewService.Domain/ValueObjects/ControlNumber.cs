namespace CrewService.Domain.ValueObjects;

public sealed record ControlNumber
{
    private static readonly object Sync = new();
    private static long _lastGeneratedValue;

    public long Value { get; }

    public ControlNumber(long value)
    {
        Value = value;
    }

    public static ControlNumber Create()
    {
        lock (Sync)
        {
            var candidate = Convert.ToInt64(DateTime.UtcNow.ToString("yyMMddHHmmssfff"));
            if (candidate <= _lastGeneratedValue)
                candidate = _lastGeneratedValue + 1;

            _lastGeneratedValue = candidate;
            return new ControlNumber(candidate);
        }
    }

    public static ControlNumber Create(long value)
    {
        return new ControlNumber(value);
    }

    public static implicit operator ControlNumber(long value) => new(value);
}
