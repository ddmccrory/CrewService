using CrewService.Application;
using CrewService.Infrastructure;
using CrewService.Persistance;
using CrewService.Presentation;
using CrewService.Presentation.Services;
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

builder.Services.AddGrpc().AddJsonTranscoding();
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
app.MapGrpcService<RailroadEmployeeService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<RailroadPoolService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<RailroadPoolEmployeeService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<RailroadPoolPayrollTierService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<RosterService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<SeniorityService>().EnableGrpcWeb().RequireAuthorization();
app.MapGrpcService<SeniorityStateService>().EnableGrpcWeb().RequireAuthorization();

app.Run();
