namespace CrewService.Domain.Modules.Dispatching;

/// <summary>
/// Completion lifecycle for an on-duty record. Kept separate from <see cref="OnDutyStatus"/> so
/// a record can be tied up (off-duty started) but still require later employee completion.
/// </summary>
public sealed record OnDutyCompletionStatus
{
    public static readonly OnDutyCompletionStatus NotStarted = new("NotStarted");
    public static readonly OnDutyCompletionStatus PendingEmployeeCompletion = new("PendingEmployeeCompletion");
    public static readonly OnDutyCompletionStatus Completed = new("Completed");

    public static readonly IReadOnlyList<OnDutyCompletionStatus> All =
        [NotStarted, PendingEmployeeCompletion, Completed];

    public string Value { get; }

    private OnDutyCompletionStatus(string value) => Value = value;

    public static OnDutyCompletionStatus FromValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return NotStarted;

        return All.FirstOrDefault(s => string.Equals(s.Value, value, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown {nameof(OnDutyCompletionStatus)} value.");
    }

    public override string ToString() => Value;
}
