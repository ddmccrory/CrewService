using CrewService.Domain.Diagnostics;
using CrewService.Domain.Exceptions;
using CrewService.Infrastructure.Exceptions;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace CrewService.UnitTests.Infrastructure;

public sealed class GrpcExceptionInterceptorTests
{
    [Fact]
    public async Task UnaryServerHandler_WithValidationException_MapsAndPersistsErrorLog()
    {
        var writer = new CapturingErrorLogWriter();
        var httpContext = BuildHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new GrpcExceptionInterceptor(
            NullLogger<GrpcExceptionInterceptor>.Instance,
            httpContextAccessor,
            writer);
        var context = new TestServerCallContext("/CrewService.Presentation.Tests/ThrowValidation");

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            sut.UnaryServerHandler<object, string>(
                new object(),
                context,
                (_, _) => Task.FromException<string>(new ValidationException("Name", "Required"))));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Equal("VALIDATION_FAILED", ex.Trailers.FirstOrDefault(t => t.Key == "code")?.Value);

        var request = Assert.Single(writer.Requests);
        Assert.Equal("BackendApi", request.SourceApp);
        Assert.Equal("gRPC", request.SourceLayer);
        Assert.Equal("Error", request.Severity);
        Assert.Equal("VALIDATION_FAILED", request.ErrorCode);
        Assert.Equal("gRPC", request.Method);
        Assert.Equal("/CrewService.Presentation.Tests/ThrowValidation", request.Route);
        Assert.Equal("grpc.user", request.PerformedBy);
        Assert.Equal(901, request.ParentCtrlNbr);
        Assert.Equal(902, request.RailroadCtrlNbr);
        Assert.Equal(ex.Trailers.FirstOrDefault(t => t.Key == "trace-id")?.Value, request.TraceId);
        Assert.Contains("\"grpcMethod\":\"/CrewService.Presentation.Tests/ThrowValidation\"", request.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnaryServerHandler_WithRpcException_PassesThroughAndWritesHandledFailureErrorLog()
    {
        var writer = new CapturingErrorLogWriter();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = BuildHttpContext() };
        var sut = new GrpcExceptionInterceptor(
            NullLogger<GrpcExceptionInterceptor>.Instance,
            httpContextAccessor,
            writer);
        var context = new TestServerCallContext("/CrewService.Presentation.Tests/PassThrough");

        var original = new RpcException(new Status(StatusCode.NotFound, "missing"));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            sut.UnaryServerHandler<object, string>(
                new object(),
                context,
                (_, _) => Task.FromException<string>(original)));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
        var request = Assert.Single(writer.Requests);
        Assert.Equal(ErrorLogKinds.HandledFailure, request.ErrorKind);
        Assert.Equal("NOTFOUND", request.ErrorCode);
        Assert.Equal("Warning", request.Severity);
        Assert.Equal("/CrewService.Presentation.Tests/PassThrough", request.Route);
    }

    [Fact]
    public async Task ClientStreamingServerHandler_WithRpcException_PassesThroughAndWritesHandledFailureErrorLog()
    {
        var writer = new CapturingErrorLogWriter();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = BuildHttpContext() };
        var sut = new GrpcExceptionInterceptor(
            NullLogger<GrpcExceptionInterceptor>.Instance,
            httpContextAccessor,
            writer);
        var context = new TestServerCallContext("/CrewService.Presentation.Tests/ClientStreamPassThrough");

        var original = new RpcException(new Status(StatusCode.Unavailable, "downstream unavailable"));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            sut.ClientStreamingServerHandler<string, string>(
                new TestAsyncStreamReader<string>(),
                context,
                (_, _) => Task.FromException<string>(original)));

        Assert.Equal(StatusCode.Unavailable, ex.StatusCode);
        var request = Assert.Single(writer.Requests);
        Assert.Equal(ErrorLogKinds.HandledFailure, request.ErrorKind);
        Assert.Equal("UNAVAILABLE", request.ErrorCode);
        Assert.Equal("Critical", request.Severity);
    }

    [Fact]
    public async Task ServerStreamingServerHandler_WithRpcException_PassesThroughAndWritesHandledFailureErrorLog()
    {
        var writer = new CapturingErrorLogWriter();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = BuildHttpContext() };
        var sut = new GrpcExceptionInterceptor(
            NullLogger<GrpcExceptionInterceptor>.Instance,
            httpContextAccessor,
            writer);
        var context = new TestServerCallContext("/CrewService.Presentation.Tests/ServerStreamPassThrough");

        var original = new RpcException(new Status(StatusCode.NotFound, "missing stream"));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            sut.ServerStreamingServerHandler<string, string>(
                "request",
                new TestServerStreamWriter<string>(),
                context,
                (_, _, _) => Task.FromException(original)));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
        var request = Assert.Single(writer.Requests);
        Assert.Equal(ErrorLogKinds.HandledFailure, request.ErrorKind);
        Assert.Equal("NOTFOUND", request.ErrorCode);
        Assert.Equal("Warning", request.Severity);
    }

    [Fact]
    public async Task DuplexStreamingServerHandler_WithRpcException_PassesThroughAndWritesHandledFailureErrorLog()
    {
        var writer = new CapturingErrorLogWriter();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = BuildHttpContext() };
        var sut = new GrpcExceptionInterceptor(
            NullLogger<GrpcExceptionInterceptor>.Instance,
            httpContextAccessor,
            writer);
        var context = new TestServerCallContext("/CrewService.Presentation.Tests/DuplexPassThrough");

        var original = new RpcException(new Status(StatusCode.InvalidArgument, "bad duplex request"));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            sut.DuplexStreamingServerHandler<string, string>(
                new TestAsyncStreamReader<string>(),
                new TestServerStreamWriter<string>(),
                context,
                (_, _, _) => Task.FromException(original)));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        var request = Assert.Single(writer.Requests);
        Assert.Equal(ErrorLogKinds.HandledFailure, request.ErrorKind);
        Assert.Equal("INVALIDARGUMENT", request.ErrorCode);
        Assert.Equal("Warning", request.Severity);
    }

    private static DefaultHttpContext BuildHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/grpc-test";
        httpContext.Request.Headers["x-parent-ctrl-nbr"] = "901";
        httpContext.Request.Headers["x-railroad-ctrl-nbr"] = "902";
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "grpc.user")],
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

    private sealed class TestAsyncStreamReader<T> : IAsyncStreamReader<T>
    {
        public T Current => default!;

        public Task<bool> MoveNext(CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class TestServerStreamWriter<T> : IServerStreamWriter<T>
    {
        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(T message) => Task.CompletedTask;
    }

    private sealed class TestServerCallContext(string method) : ServerCallContext
    {
        protected override string MethodCore => method;
        protected override string HostCore => "localhost";
        protected override string PeerCore => "peer";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore { get; } = [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore =>
            new("anonymous", new Dictionary<string, List<AuthProperty>>());

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
            => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
            => Task.CompletedTask;
    }
}
