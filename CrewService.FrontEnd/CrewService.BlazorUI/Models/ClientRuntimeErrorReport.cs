namespace CrewService.BlazorUI.Models;

public sealed class ClientRuntimeErrorReport
{
    public string SourceApp { get; set; } = "BlazorWasm";
    public string SourceLayer { get; set; } = "BrowserRuntime";
    public string Severity { get; set; } = "Error";
    public string ErrorCode { get; set; } = "CLIENT_RUNTIME_ERROR";
    public string ErrorKind { get; set; } = "ClientRuntime";
    public string Message { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? Url { get; set; }
    public string? Method { get; set; } = "Browser";
    public string? UserAgent { get; set; }
    public string? TraceId { get; set; }
    public long? ParentCtrlNbr { get; set; }
    public long? RailroadCtrlNbr { get; set; }
    public string? PayloadJson { get; set; }
    public Dictionary<string, string?>? Metadata { get; set; }
}
