using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CrewService.Persistance.Services;

public class CurrentUserService()
{
    private readonly HttpContextAccessor _httpContextAccessor = new();

    public Guid GetUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (claim is null)
            return Guid.Empty;

        return Guid.Parse(claim);
    }

    public string GetUserName()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    }
}
