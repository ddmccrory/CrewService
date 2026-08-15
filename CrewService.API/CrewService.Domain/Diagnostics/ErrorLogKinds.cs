namespace CrewService.Domain.Diagnostics;

public static class ErrorLogKinds
{
    public const string UnhandledException = "UnhandledException";
    public const string HandledFailure = "HandledFailure";
    public const string ClientRuntime = "ClientRuntime";
    public const string Dependency = "Dependency";
    public const string Validation = "Validation";
}
