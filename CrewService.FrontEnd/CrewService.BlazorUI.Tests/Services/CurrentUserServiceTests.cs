using System.Security.Claims;
using CrewService.BlazorUI.Client;
using CrewService.BlazorUI.Models.Auth;
using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Xunit;

namespace CrewService.BlazorUI.Tests.Services;

public sealed class CurrentUserServiceTests
{
    private static ClaimsPrincipal CreateUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task InitializeAsync_WithEmployeeRole_SetsIsEmployeeTrue()
    {
        var service = new CurrentUserService();
        var user = CreateUser(
            new Claim(ClaimTypes.Role, Roles.Employee),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        await service.InitializeAsync(user);

        Assert.True(service.IsEmployee);
    }

    [Fact]
    public async Task InitializeAsync_WithEmployeeNumberClaim_SetsIsEmployeeTrue()
    {
        var service = new CurrentUserService();
        var user = CreateUser(
            new Claim(CustomClaimTypes.EmployeeNumber, "PTRA0099"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        await service.InitializeAsync(user);

        Assert.True(service.IsEmployee);
        Assert.Equal("PTRA0099", service.EmployeeNumber);
    }

    [Fact]
    public async Task InitializeAsync_WithAdminRole_SetsIsAdminTrue()
    {
        var service = new CurrentUserService();
        var user = CreateUser(
            new Claim(ClaimTypes.Role, Roles.SystemAdmin),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        await service.InitializeAsync(user);

        Assert.True(service.IsAdmin);
    }

    [Fact]
    public void SeedFromBootstrap_WithEmployee_PopulatesEmployeeAndIsEmployee()
    {
        var service = new CurrentUserService();
        var user = CreateUser(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        service.SeedFromBootstrap(user, new GetEmployeeResponse
        {
            CtrlNbr = 123,
            EmployeeNumber = "PTRA0123"
        });

        Assert.True(service.IsEmployee);
        Assert.NotNull(service.Employee);
        Assert.Equal(123, service.Employee!.CtrlNbr);
        Assert.Equal("PTRA0123", service.Employee.EmployeeNumber);
    }
}
