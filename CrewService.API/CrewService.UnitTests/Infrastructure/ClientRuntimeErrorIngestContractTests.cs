using Xunit;

namespace CrewService.UnitTests.Infrastructure;

public sealed class ClientRuntimeErrorIngestContractTests
{
    [Fact]
    public void GrpcHost_MapsClientRuntimeErrorIngestEndpoint()
    {
        var source = File.ReadAllText(GetGrpcProgramPath());

        Assert.Contains("app.MapPost(\"/v1/error-logs/client\"", source, StringComparison.Ordinal);
        Assert.Contains("ErrorKind: string.IsNullOrWhiteSpace(request.ErrorKind) ? ErrorLogKinds.ClientRuntime : request.ErrorKind", source, StringComparison.Ordinal);
        Assert.Contains("SourceLayer: string.IsNullOrWhiteSpace(request.SourceLayer) ? \"BrowserRuntime\" : request.SourceLayer", source, StringComparison.Ordinal);
    }

    private static string GetGrpcProgramPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var apiRoot = Path.Combine(dir.FullName, "CrewService.GrpcService");
            if (Directory.Exists(apiRoot))
            {
                return Path.Combine(apiRoot, "Program.cs");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CrewService.GrpcService project directory from test output path.");
    }
}
