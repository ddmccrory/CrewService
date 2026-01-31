using CrewService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CrewService.Persistance.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

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
}
