using CrewService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CrewService.Persistance.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string? _auditOverride;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var claim = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (claim is null)
                return Guid.Empty;

            return Guid.Parse(claim);
        }

        public string GetUserName()
        {
            if (!string.IsNullOrWhiteSpace(_auditOverride))
                return _auditOverride;

            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.Name)
                ?? user?.FindFirstValue(JwtRegisteredClaimNames.Name)
                ?? string.Empty;
        }

        public bool IsInRole(string roleName)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.IsInRole(roleName) ?? false;
        }

        public long? GetParentCtrlNbr()
        {
            var header = _httpContextAccessor.HttpContext?.Request.Headers["x-parent-ctrl-nbr"].FirstOrDefault();
            if (long.TryParse(header, out var parentCtrlNbr) && parentCtrlNbr > 0)
                return parentCtrlNbr;
            return null;
        }

        public void SetAuditOverride(string name)
        {
            _auditOverride = name;
        }
    }
}
