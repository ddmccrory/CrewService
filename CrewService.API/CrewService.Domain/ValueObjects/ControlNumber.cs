namespace CrewService.Domain.ValueObjects;

public sealed record ControlNumber
{
    public long Value { get; }

    private ControlNumber(long value)
    {
        Value = value;
    }

    public static ControlNumber Create()
    {
        Thread.Sleep(1);

        return new ControlNumber(Convert.ToInt64(DateTime.UtcNow.ToString("yyMMddHHmmssfff")));
    }

    public static ControlNumber Create(long value)
    {
        return new ControlNumber(value);
    }
}
