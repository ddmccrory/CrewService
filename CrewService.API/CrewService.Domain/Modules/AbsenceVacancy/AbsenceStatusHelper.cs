namespace CrewService.Domain.Modules.AbsenceVacancy;

public static class AbsenceStatusValues
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Open = "OPEN";
    public const string Complete = "COMPLETE";
    public const string Denied = "DENIED";
    public const string Cancelled = "CANCELLED";
}

public static class AbsenceStatusHelper
{
    public static string Derive(AbsenceRequest request) => request.DerivedStatus;

    public static bool IsPending(AbsenceRequest request) => IsPending(request.DerivedStatus);
    public static bool IsApproved(AbsenceRequest request) => IsApproved(request.DerivedStatus);
    public static bool IsOpen(AbsenceRequest request) => IsOpen(request.DerivedStatus);
    public static bool IsComplete(AbsenceRequest request) => IsComplete(request.DerivedStatus);
    public static bool IsDenied(AbsenceRequest request) => IsDenied(request.DerivedStatus);
    public static bool IsCancelled(AbsenceRequest request) => IsCancelled(request.DerivedStatus);

    public static bool IsPending(string? status) => Is(status, AbsenceStatusValues.Pending);
    public static bool IsApproved(string? status) => Is(status, AbsenceStatusValues.Approved);
    public static bool IsOpen(string? status) => Is(status, AbsenceStatusValues.Open);
    public static bool IsComplete(string? status) => Is(status, AbsenceStatusValues.Complete);
    public static bool IsDenied(string? status) => Is(status, AbsenceStatusValues.Denied);
    public static bool IsCancelled(string? status) => Is(status, AbsenceStatusValues.Cancelled);

    public static bool Is(string? status, string expected)
        => string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);
}
