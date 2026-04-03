using CrewService.BlazorUI.Clients;
using CrewService.BlazorUI.Components;
using CrewService.BlazorUI.Components.Account;
using CrewService.BlazorUI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSingleton<GrpcChannelProvider>();
builder.Services.AddScoped<CircuitTokenProvider>();
builder.Services.AddScoped<AccountClient>();
builder.Services.AddScoped<AddressTypeClient>();
builder.Services.AddScoped<AuthClient>();
builder.Services.AddScoped<AuthorizationClient>();
builder.Services.AddScoped<CraftClient>();
builder.Services.AddScoped<EmailAddressTypeClient>();
builder.Services.AddScoped<EmployeeClient>();
builder.Services.AddScoped<InvitationsClient>();
builder.Services.AddScoped<InvitationTokenClient>();
builder.Services.AddScoped<ParentsClient>();
builder.Services.AddScoped<PhoneNumberTypeClient>();
builder.Services.AddScoped<SeniorityClient>();
builder.Services.AddScoped<TenantConfigClient>();
builder.Services.AddScoped<DepartmentClient>();
builder.Services.AddScoped<CrewClient>();
builder.Services.AddScoped<AssignmentClient>();
builder.Services.AddScoped<WorkManagementClient>();

builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "auth_token";
                    options.LoginPath = "/Account/Login";
                    options.Cookie.MaxAge = TimeSpan.FromMinutes(30);
                    options.AccessDeniedPath = "/Account/AccessDenied";
                });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<AppThemeService>();
builder.Services.AddSingleton<PermissionCatalogCache>();
builder.Services.AddScoped<AppContextService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<UserPermissionService>();
builder.Services.AddScoped<BootstrapClient>();
builder.Services.AddScoped<ContextOptionsService>();
builder.Services.AddScoped<CircuitBootstrapService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapPost("/Account/Logout", async (HttpContext context) =>
{
    var themeService = context.RequestServices.GetRequiredService<AppThemeService>();
    themeService.ResetThemeValues();

    await context.SignOutAsync();

    return Results.Redirect("/Account/Login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CrewService.BlazorUI.Client._Imports).Assembly);

app.Run();