using CrewService.Application;
using CrewService.GrpcService;
using CrewService.Infrastructure;
using CrewService.Infrastructure.Exceptions;
using CrewService.Persistance;
using CrewService.Presentation;
using CrewService.Presentation.Services;
using CrewService.Presentation.Services.Modules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Filters;
using System.Text;

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
    options.Interceptors.Add<GrpcExceptionInterceptor>();
}).AddJsonTranscoding();
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

    await app.Services.MigrateDatabasesAsync();
    await DevDataSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<AccountService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<AddressTypeService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<AuthService>().EnableGrpcWeb();
app.MapGrpcService<CraftService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<EmailAddressTypeService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<EmployeeService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<EmploymentStatusService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<EmploymentStatusHistoryService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<ParentService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PhoneNumberTypeService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PriorServiceCreditService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<RailroadService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PayrollTierService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<RosterService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<SeniorityService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<SeniorityStateService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<UserParentAssignmentService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<InvitationService>().EnableGrpcWeb().RequireAuthorization();

// Module services
app.MapGrpcService<TenantConfigService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<WorkManagementService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<CrewsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<BoardsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PoliciesService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<BulletinsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<DispatchingService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<AbsenceVacancyService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PayrollService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<FraComplianceService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<DailyOperationsService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<MarkOffService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<VacancyAssignmentService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<PayrollEngineService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<ElectronicCallingService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<BackgroundServicesService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<RosterBoardService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<HolidayPayrollService>().EnableGrpcWeb().RequireAuthorization();

await app.RunAsync();
