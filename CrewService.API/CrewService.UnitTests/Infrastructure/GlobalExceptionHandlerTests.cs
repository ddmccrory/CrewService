using CrewService.Domain.Diagnostics;
using CrewService.Domain.Exceptions;
using CrewService.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace CrewService.UnitTests.Infrastructure;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WithValidationException_WritesErrorLogWithRequestContext()
    {
        var writer = new CapturingErrorLogWriter();
        var sut = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, writer);
        var httpContext = BuildHttpContext(traceId: "trace-validation");

        var handled = await sut.TryHandleAsync(
            httpContext,
            new ValidationException("Name", "Required"),
            CancellationToken.None);

        Assert.True(handled);
        var request = Assert.Single(writer.Requests);
        Assert.Equal("BackendApi", request.SourceApp);
        Assert.Equal("HTTP", request.SourceLayer);
        Assert.Equal("Warning", request.Severity);
        Assert.Equal("VALIDATION_FAILED", request.ErrorCode);
        Assert.Equal(typeof(ValidationException).FullName, request.ExceptionType);
        Assert.Equal("trace-validation", request.TraceId);
        Assert.Equal("GET", request.Method);
        Assert.Equal("/v1/test-errors", request.Route);
        Assert.Equal("test.user", request.PerformedBy);
        Assert.Equal(123, request.ParentCtrlNbr);
        Assert.Equal(456, request.RailroadCtrlNbr);
        Assert.Contains("\"statusCode\":400", request.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryHandleAsync_WithUnhandledException_WritesCriticalErrorWithHttp500Code()
    {
        var writer = new CapturingErrorLogWriter();
        var sut = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, writer);
        var httpContext = BuildHttpContext(traceId: "trace-unhandled");

        var handled = await sut.TryHandleAsync(
            httpContext,
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.True(handled);
        var request = Assert.Single(writer.Requests);
        Assert.Equal("Critical", request.Severity);
        Assert.Equal("HTTP_500", request.ErrorCode);
        Assert.Equal(typeof(InvalidOperationException).FullName, request.ExceptionType);
        Assert.Equal("boom", request.Message);
        Assert.Equal("trace-unhandled", request.TraceId);
    }

    private static DefaultHttpContext BuildHttpContext(string traceId)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = traceId
        };

        httpContext.Request.Scheme = "https";
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/v1/test-errors";
        httpContext.Request.QueryString = new QueryString("?q=1");
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Headers["x-parent-ctrl-nbr"] = "123";
        httpContext.Request.Headers["x-railroad-ctrl-nbr"] = "456";
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "test.user")],
                authenticationType: "Test"));

        return httpContext;
    }

    private sealed class CapturingErrorLogWriter : IErrorLogWriter
    {
        public List<ErrorLogWriteRequest> Requests { get; } = [];

        public Task WriteAsync(ErrorLogWriteRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }
}
