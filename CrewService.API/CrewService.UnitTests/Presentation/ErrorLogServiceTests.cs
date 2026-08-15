using CrewService.Domain.Constants;
using CrewService.Domain.Diagnostics;
using CrewService.Domain.Models.UserAccess;
using CrewService.Presentation;
using CrewService.Presentation.Services.Modules;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CrewService.UnitTests.Presentation;

public sealed class ErrorLogServiceTests
{
    [Fact]
    public async Task GetAllErrorLogsAsync_WithoutSelectedRailroad_ReturnsEmptyAndSkipsQuery()
    {
        var query = new CapturingErrorLogQuery();
        var command = new CapturingErrorLogCommand();
        var sut = new ErrorLogService(query, command);
        var context = TestServerCallContextFactory.Create(roles: [Roles.SystemAdmin]);

        var response = await sut.GetAllErrorLogsAsync(new GetAllErrorLogsRequest(), context);

        Assert.Empty(response.Entries);
        Assert.Equal(0, response.TotalCount);
        Assert.Equal(0, query.CallCount);
    }

    [Fact]
    public async Task GetAllErrorLogsAsync_WithFilters_PassesFilterAndMapsResponse()
    {
        var occurredAtUtc = new DateTime(2026, 8, 15, 12, 30, 0, DateTimeKind.Utc);
        var seeded = ErrorLog.Create(
            occurredAtUtc,
            ErrorLogKinds.UnhandledException,
            "BackendApi",
            "HTTP",
            "Critical",
            "fingerprint-test",
            "HTTP_500",
            "System.InvalidOperationException",
            "boom",
            "trace-1",
            "/v1/test",
            "GET",
            "admin.user",
            200,
            300,
            "{\"message\":\"boom\"}");

        var query = new CapturingErrorLogQuery
        {
            ResultEntries = [seeded],
            ResultTotalCount = 1
        };

        var command = new CapturingErrorLogCommand();
        var sut = new ErrorLogService(query, command);
        var fromUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Utc);

        var request = new GetAllErrorLogsRequest
        {
            PageNumber = 2,
            PageSize = 25,
            SearchText = "boom",
            DateFrom = fromUtc.ToString("O"),
            DateTo = toUtc.ToString("O"),
            Severity = "Critical",
            SourceApp = "BackendApi",
            ErrorKind = ErrorLogKinds.UnhandledException,
            Status = ErrorLogStatuses.New,
            FingerprintHash = "fingerprint-test"
        };

        var context = TestServerCallContextFactory.Create(
            selectedRailroadCtrlNbr: 300,
            parentCtrlNbrHeader: 200,
            roles: [Roles.RailroadAdmin]);

        var response = await sut.GetAllErrorLogsAsync(request, context);

        Assert.Equal(1, query.CallCount);
        Assert.Equal(2, query.CapturedPageNumber);
        Assert.Equal(25, query.CapturedPageSize);
        Assert.NotNull(query.CapturedFilter);
        Assert.Equal("boom", query.CapturedFilter!.SearchText);
        Assert.Equal("Critical", query.CapturedFilter.Severity);
        Assert.Equal("BackendApi", query.CapturedFilter.SourceApp);
        Assert.Equal(ErrorLogKinds.UnhandledException, query.CapturedFilter.ErrorKind);
        Assert.Equal(ErrorLogStatuses.New, query.CapturedFilter.Status);
        Assert.Equal("fingerprint-test", query.CapturedFilter.FingerprintHash);
        Assert.Equal(200, query.CapturedFilter.ParentCtrlNbr);
        Assert.Equal(300, query.CapturedFilter.RailroadCtrlNbr);
        Assert.Equal(fromUtc, query.CapturedFilter.DateFromUtc?.ToUniversalTime());
        Assert.Equal(toUtc, query.CapturedFilter.DateToUtc?.ToUniversalTime());

        var mapped = Assert.Single(response.Entries);
        Assert.Equal(seeded.ErrorId.ToString(), mapped.ErrorId);
        Assert.Equal("Critical", mapped.Severity);
        Assert.Equal("BackendApi", mapped.SourceApp);
        Assert.Equal("HTTP", mapped.SourceLayer);
        Assert.Equal("HTTP_500", mapped.ErrorCode);
        Assert.Equal("System.InvalidOperationException", mapped.ExceptionType);
        Assert.Equal("boom", mapped.Message);
        Assert.Equal("trace-1", mapped.TraceId);
        Assert.Equal("/v1/test", mapped.Route);
        Assert.Equal("GET", mapped.Method);
        Assert.Equal("admin.user", mapped.PerformedBy);
        Assert.Equal("{\"message\":\"boom\"}", mapped.PayloadJson);
        Assert.Equal(ErrorLogKinds.UnhandledException, mapped.ErrorKind);
        Assert.Equal(ErrorLogStatuses.New, mapped.Status);
        Assert.Equal("fingerprint-test", mapped.FingerprintHash);
        Assert.Equal(1, mapped.OccurrenceCount);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task UpdateErrorLogStatusAsync_WithValidInput_InvokesCommand()
    {
        var query = new CapturingErrorLogQuery();
        var command = new CapturingErrorLogCommand { Result = true };
        var sut = new ErrorLogService(query, command);
        var errorId = Guid.NewGuid();

        var context = TestServerCallContextFactory.Create(
            selectedRailroadCtrlNbr: 300,
            parentCtrlNbrHeader: 200,
            roles: [Roles.RailroadAdmin],
            userName: "triage.user");

        var response = await sut.UpdateErrorLogStatusAsync(
            new UpdateErrorLogStatusRequest
            {
                ErrorId = errorId.ToString(),
                Status = ErrorLogStatuses.Resolved,
                SuppressionReason = ""
            },
            context);

        Assert.True(response.Updated);
        Assert.Equal(1, command.CallCount);
        Assert.Equal(errorId, command.CapturedErrorId);
        Assert.Equal(ErrorLogStatuses.Resolved, command.CapturedStatus);
        Assert.Equal("triage.user", command.CapturedActedBy);
        Assert.Null(command.CapturedSuppressionReason);
    }

    [Fact]
    public async Task UpdateErrorLogStatusAsync_WithInvalidGuid_ThrowsInvalidArgument()
    {
        var query = new CapturingErrorLogQuery();
        var command = new CapturingErrorLogCommand();
        var sut = new ErrorLogService(query, command);
        var context = TestServerCallContextFactory.Create(selectedRailroadCtrlNbr: 300, roles: [Roles.RailroadAdmin]);

        var ex = await Assert.ThrowsAsync<RpcException>(() => sut.UpdateErrorLogStatusAsync(
            new UpdateErrorLogStatusRequest
            {
                ErrorId = "not-a-guid",
                Status = ErrorLogStatuses.Investigating
            },
            context));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Equal(0, command.CallCount);
    }

    [Fact]
    public async Task UpdateErrorLogStatusAsync_WithMissingStatus_ThrowsInvalidArgument()
    {
        var query = new CapturingErrorLogQuery();
        var command = new CapturingErrorLogCommand();
        var sut = new ErrorLogService(query, command);
        var context = TestServerCallContextFactory.Create(selectedRailroadCtrlNbr: 300, roles: [Roles.RailroadAdmin]);

        var ex = await Assert.ThrowsAsync<RpcException>(() => sut.UpdateErrorLogStatusAsync(
            new UpdateErrorLogStatusRequest
            {
                ErrorId = Guid.NewGuid().ToString(),
                Status = ""
            },
            context));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Equal(0, command.CallCount);
    }

    [Fact]
    public async Task GetAllErrorLogsAsync_WithoutParentHeader_ForNonSystemAdmin_UsesFirstParentClaim()
    {
        var query = new CapturingErrorLogQuery();
        var command = new CapturingErrorLogCommand();
        var sut = new ErrorLogService(query, command);

        var context = TestServerCallContextFactory.Create(
            selectedRailroadCtrlNbr: 501,
            roles: [Roles.RailroadAdmin],
            additionalClaims:
            [
                new Claim(CustomClaimTypes.ParentRole, "777:RailroadAdmin:501")
            ]);

        await sut.GetAllErrorLogsAsync(new GetAllErrorLogsRequest(), context);

        Assert.Equal(1, query.CallCount);
        Assert.NotNull(query.CapturedFilter);
        Assert.Equal(777, query.CapturedFilter!.ParentCtrlNbr);
        Assert.Equal(501, query.CapturedFilter.RailroadCtrlNbr);
    }

    private sealed class CapturingErrorLogCommand : IErrorLogCommand
    {
        public int CallCount { get; private set; }
        public Guid CapturedErrorId { get; private set; }
        public string? CapturedStatus { get; private set; }
        public string? CapturedActedBy { get; private set; }
        public string? CapturedSuppressionReason { get; private set; }
        public bool Result { get; init; }

        public Task<bool> UpdateStatusAsync(
            Guid errorId,
            string status,
            string actedBy,
            string? suppressionReason = null,
            CancellationToken ct = default)
        {
            CallCount++;
            CapturedErrorId = errorId;
            CapturedStatus = status;
            CapturedActedBy = actedBy;
            CapturedSuppressionReason = suppressionReason;
            return Task.FromResult(Result);
        }
    }

    private sealed class CapturingErrorLogQuery : IErrorLogQuery
    {
        public int CallCount { get; private set; }
        public int CapturedPageNumber { get; private set; }
        public int CapturedPageSize { get; private set; }
        public ErrorLogFilter? CapturedFilter { get; private set; }
        public IReadOnlyList<ErrorLog> ResultEntries { get; init; } = [];
        public int ResultTotalCount { get; init; }

        public Task<(IReadOnlyList<ErrorLog> Entries, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            ErrorLogFilter? filter = null,
            CancellationToken ct = default)
        {
            CallCount++;
            CapturedPageNumber = pageNumber;
            CapturedPageSize = pageSize;
            CapturedFilter = filter;
            return Task.FromResult((ResultEntries, ResultTotalCount));
        }
    }

    private static class TestServerCallContextFactory
    {
        public static ServerCallContext Create(
            long? selectedRailroadCtrlNbr = null,
            long? parentCtrlNbrHeader = null,
            string[]? roles = null,
            IEnumerable<Claim>? additionalClaims = null,
            string? userName = null)
        {
            var claims = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(userName))
                claims.Add(new Claim(ClaimTypes.Name, userName));

            if (roles is not null)
            {
                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            }

            if (additionalClaims is not null)
            {
                claims.AddRange(additionalClaims);
            }

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            };

            if (selectedRailroadCtrlNbr.HasValue)
                httpContext.Request.Headers["x-railroad-ctrl-nbr"] = selectedRailroadCtrlNbr.Value.ToString();

            if (parentCtrlNbrHeader.HasValue)
                httpContext.Request.Headers["x-parent-ctrl-nbr"] = parentCtrlNbrHeader.Value.ToString();

            return TestServerCallContext.Create("/CrewService.Presentation.ErrorLogSrvc/Test", httpContext);
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly Dictionary<object, object> _userState = new();

        private TestServerCallContext(string method, HttpContext httpContext)
        {
            MethodCore = method;
            _userState["__HttpContext"] = httpContext;
        }

        public static ServerCallContext Create(string method, HttpContext httpContext) =>
            new TestServerCallContext(method, httpContext);

        protected override string MethodCore { get; }
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

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
            throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;

        protected override IDictionary<object, object> UserStateCore => _userState;
    }
}
