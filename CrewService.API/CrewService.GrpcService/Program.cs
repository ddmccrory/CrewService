using CrewService.Application;
using CrewService.GrpcService;
using CrewService.Infrastructure;
using CrewService.Infrastructure.Exceptions;
using CrewService.Persistance;
using CrewService.Presentation;
using CrewService.Presentation.Services;
using CrewService.Presentation.Services.Modules;
using CrewService.Domain.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ??
    throw new Exception("Jwt Key is not defined."));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "gRPC Transcoding", Version = "v1" });
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddApplication()
                .AddInfrastructure(builder.Configuration)
                .AddPersistance(builder.Configuration)
                .AddPresentation();

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<NotificationAcknowledgementInterceptor>();
    options.Interceptors.Add<GrpcExceptionInterceptor>();
}).AddJsonTranscoding();
builder.Services.AddTransient<ParentService>();
builder.Services.AddScoped<EmployeeNameService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseStatusCodePages();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

await app.Services.MigrateDatabasesAsync();

// Baseline data required in all environments (idempotent)
await BaselineSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    await DevDataSeeder.SeedAsync(app.Services);

    // Dev data seeding creates tenant/railroad groups on a fresh database.
    // Re-run baseline seeding so railroad-scoped baseline workflows are created
    // in the same startup cycle.
    await BaselineSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/v1/error-logs/client", async (
    ClientRuntimeErrorIngestRequest request,
    HttpContext httpContext,
    IErrorLogWriter errorLogWriter,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest("Message is required.");

    var parentCtrlNbr = request.ParentCtrlNbr ?? TryParseHeaderAsLong(httpContext.Request.Headers["x-parent-ctrl-nbr"].FirstOrDefault());
    var railroadCtrlNbr = request.RailroadCtrlNbr ?? TryParseHeaderAsLong(httpContext.Request.Headers["x-railroad-ctrl-nbr"].FirstOrDefault());

    var traceId = request.TraceId
        ?? System.Diagnostics.Activity.Current?.Id
        ?? httpContext.TraceIdentifier;

    var payloadJson = JsonSerializer.Serialize(new
    {
        schemaVersion = "1.0",
        pipeline = "client-runtime-ingest",
        sourceApp = request.SourceApp,
        sourceLayer = request.SourceLayer,
        errorCode = request.ErrorCode,
        message = request.Message,
        exceptionType = request.ExceptionType,
        stackTrace = request.StackTrace,
        url = request.Url,
        method = request.Method,
        userAgent = request.UserAgent,
        metadata = request.Metadata,
        payloadJson = request.PayloadJson,
        timestampUtc = DateTime.UtcNow
    });

    await errorLogWriter.WriteAsync(new ErrorLogWriteRequest(
        OccurredAtUtc: DateTime.UtcNow,
        ErrorKind: string.IsNullOrWhiteSpace(request.ErrorKind) ? ErrorLogKinds.ClientRuntime : request.ErrorKind,
        SourceApp: string.IsNullOrWhiteSpace(request.SourceApp) ? "BlazorWasm" : request.SourceApp,
        SourceLayer: string.IsNullOrWhiteSpace(request.SourceLayer) ? "BrowserRuntime" : request.SourceLayer,
        Severity: string.IsNullOrWhiteSpace(request.Severity) ? "Error" : request.Severity,
        ErrorCode: string.IsNullOrWhiteSpace(request.ErrorCode) ? "CLIENT_RUNTIME_ERROR" : request.ErrorCode,
        ExceptionType: string.IsNullOrWhiteSpace(request.ExceptionType) ? "ClientRuntimeError" : request.ExceptionType,
        Message: request.Message,
        TraceId: traceId,
        Route: request.Url,
        Method: string.IsNullOrWhiteSpace(request.Method) ? "Browser" : request.Method,
        PerformedBy: httpContext.User.Identity?.Name ?? string.Empty,
        ParentCtrlNbr: parentCtrlNbr,
        RailroadCtrlNbr: railroadCtrlNbr,
        PayloadJson: payloadJson),
        cancellationToken);

    return Results.Accepted();
}).RequireAuthorization();

app.MapGrpcService<AccountService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<AddressTypeService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<AuthService>().EnableGrpcWeb();
app.MapGrpcService<BootstrapService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<CraftService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<EmailAddressTypeService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<EmployeeService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<EmploymentStatusService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<EmploymentStatusHistoryService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<ParentService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PhoneNumberTypeService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PriorServiceCreditService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PayrollTierService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<RosterService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<SeniorityService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<SeniorityStateService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<UserParentAssignmentService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<InvitationService>().EnableGrpcWeb().RequireAuthorization();

// Module services
app.MapGrpcService<AuthorizationService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<TenantConfigService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<WorkManagementService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<DepartmentService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<CrewsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<AssignmentsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<BoardsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PoliciesService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<BulletinsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<DispatchingService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<AbsenceVacancyService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PayrollService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<FraComplianceService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<DailyOperationsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<AbsenceService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<VacancyAssignmentService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PayrollEngineService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<ElectronicCallingService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<BackgroundServicesService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<RosterBoardService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<AuditLogService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<ErrorLogService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<HolidayPayrollService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<HolidayManagementService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<ReportingExportsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<RailroadInfoService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<SafetyService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<QualificationsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<NotificationsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<WorkflowTemplatesService>().EnableGrpcWeb().RequireAuthorization();

static long? TryParseHeaderAsLong(string? value)
{
    return long.TryParse(value, out var parsed) && parsed > 0
        ? parsed
        : null;
}

await app.RunAsync();
