using CrewService.Application.Notifications;
using CrewService.Infrastructure.Exceptions;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CrewService.UnitTests.Notifications;

public sealed class NotificationAcknowledgementInterceptorTests
{
    [Fact]
    public async Task UnaryServerHandler_AllowsRequest_WhenNoOpenNotices()
    {
        var interceptor = new NotificationAcknowledgementInterceptor(
            new StubEnforcer(0),
            NullLogger<NotificationAcknowledgementInterceptor>.Instance);
        var context = new TestServerCallContext("/CrewService.Presentation.EmployeeSrvc/GetEmployee");

        var continuationCalled = false;
        var response = await interceptor.UnaryServerHandler<object, string>(
            new object(),
            context,
            (_, _) =>
            {
                continuationCalled = true;
                return Task.FromResult("ok");
            });

        Assert.True(continuationCalled);
        Assert.Equal("ok", response);
    }

    [Fact]
    public async Task UnaryServerHandler_BlocksRequest_WhenOpenNoticesExist()
    {
        var interceptor = new NotificationAcknowledgementInterceptor(
            new StubEnforcer(2),
            NullLogger<NotificationAcknowledgementInterceptor>.Instance);
        var context = new TestServerCallContext("/CrewService.Presentation.EmployeeSrvc/GetEmployee");

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler<object, string>(
                new object(),
                context,
                (_, _) => Task.FromResult("ok")));

        Assert.Equal(StatusCode.FailedPrecondition, ex.Status.StatusCode);
        Assert.Equal("NOTIFICATION_ACKNOWLEDGEMENT_REQUIRED", ex.Trailers.FirstOrDefault(t => t.Key == "code")?.Value);
        Assert.Equal("2", ex.Trailers.FirstOrDefault(t => t.Key == "open-count")?.Value);
    }

    private sealed class StubEnforcer(int count) : INotificationAcknowledgementEnforcer
    {
        public Task<int> GetBlockingOpenCountAsync(string grpcMethod, CancellationToken ct = default)
            => Task.FromResult(count);
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
