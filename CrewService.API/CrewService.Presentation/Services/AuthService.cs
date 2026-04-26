using CrewService.Domain.Constants;
using CrewService.Domain.Models.UserAccess;
using CrewService.Application.Models.UserAccount;
using CrewService.Application.Modules.UserAccount;
using CrewService.Application.UserAccess;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CrewService.Presentation.Services;

public sealed class AuthService(
    IConfiguration configuration,
    IUserAccountService userAccountService,
    AuthAppService authAppService,
    UserAccessAppService userAccessAppService) : AuthSrvc.AuthSrvcBase
{
    private readonly IUserAccountService _userAccountService = userAccountService;
    private readonly IConfiguration _configuration = configuration;
    private readonly AuthAppService _authAppService = authAppService;
    private readonly UserAccessAppService _userAccessAppService = userAccessAppService;

    public override async Task<AcceptInvitationResponse> AcceptInvitation(AcceptInvitationRequest request, ServerCallContext context)
    {
        AcceptInvitationResponse response = new();

        if (string.IsNullOrEmpty(request.InvitationToken))
        {
            response.Success = false;
            response.Message.Add("Invitation token is required.");
            return response;
        }

        var (success, errorMessage) = await _authAppService.AcceptInvitationAsync(
            request.InvitationToken, request.Password, context.CancellationToken);

        response.Success = success;
        if (!success && errorMessage is not null)
            response.Message.Add(errorMessage);
        else if (success)
            response.Message.Add("Invitation accepted successfully.");

        return response;
    }

    public override async Task<AuthResponse> AuthenticateUser(AuthRequest request, ServerCallContext context)
    {
        AuthResponse response = new();

        if (!string.IsNullOrEmpty(request.UserName) && !string.IsNullOrEmpty(request.Password))
        {
            var user = await _userAccountService.FindByEmailAsync(request.UserName);

            if (user is null)
            {
                response.Success = false;
                response.Message.Add("User could not be found.");
            }
            else
            {
                var validated = await _userAccountService.CheckPasswordAsync(user.Id, request.Password);

                if (validated)
                {
                    response = await GenerateJwtAccessTokensAsync(user);

                    if (string.IsNullOrEmpty(user.FullName))
                        response.FullName = user.Email;
                    else
                        response.FullName = user.FullName ?? string.Empty;

                    response.ThemeName = user.ThemeName ?? string.Empty;
                    response.ThemeMode = user.ThemeMode ?? string.Empty;
                }
                else
                {
                    response.Success = false;
                    response.Message.Add("Password is not valid.");
                }
            }
        }
        else
        {
            response.Success = false;

            if (string.IsNullOrEmpty(request.UserName))
                response.Message.Add("User Name is required.");

            if (string.IsNullOrEmpty(request.Password))
                response.Message.Add("Password is required");                
        }

        return response;
    }

    public override async Task<AuthResponse> RefreshJwtToken(RefreshRequest request, ServerCallContext context)
    {
        AuthResponse response = new();

        var principal = GetPrincipalFromExpiredToken(request.JwtToken);

        if (principal?.Identity?.Name is null)
            return response;

        var user = await _userAccountService.FindByEmailAsync(principal.Identity.Name);

        if (user is null)
        {
            response.Success = false;
            response.Message.Add("User could not be found.");
        }
        else if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiration < DateTime.UtcNow)
        {
            response.Success = false;
            response.Message.Add("Refresh token does not match or expired");
        }
        else
        {
            response = await GenerateJwtAccessTokensAsync(user);
        }

        return response;
    }

    private async Task<AuthResponse> GenerateJwtAccessTokensAsync(UserAccountDto user)
    {
        AuthResponse response = new();

        var expireDate = DateTime.UtcNow.AddHours(1);

        var token = await GenerateJwtTokenAsync(user, expireDate);
        var refreshToken = GenerateRefreshToken();

        await _userAccountService.UpdateRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddHours(12));

        response.Token = token;
        response.Success = true;
        response.TokenExpired = expireDate.Ticks;
        response.Message.Add("Successful Login");
        response.RefreshToken = refreshToken;

        return response;
    }

    private async Task<string> GenerateJwtTokenAsync(UserAccountDto user, DateTime expirationDate)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, user.UserName!),
            new(JwtRegisteredClaimNames.Sub, user.Id)
        };

        if (!string.IsNullOrWhiteSpace(user.EmployeeNumber))
        {
            claims.Add(new Claim(CustomClaimTypes.EmployeeNumber, user.EmployeeNumber));
        }

        // Global role: SystemAdmin bypasses parent scoping
        if (string.Equals(user.PrimaryRoleId, Roles.SystemAdmin, StringComparison.Ordinal))
        {
            claims.Add(new Claim("role", Roles.SystemAdmin));
        }
        else
        {
            // Per-parent roles from UserParentAssignment
                var assignments = await _userAccessAppService.GetByUserAsync(user.Id);

            if (assignments.Count > 0)
            {
                foreach (var role in assignments.Select(a => a.Role).Distinct())
                {
                    claims.Add(new Claim("role", role));
                }

                foreach (var assignment in assignments)
                {
                    var claimValue = assignment.RailroadCtrlNbr is not null
                        ? $"{assignment.ParentCtrlNbr.Value}:{assignment.Role}:{assignment.RailroadCtrlNbr.Value}"
                        : $"{assignment.ParentCtrlNbr.Value}:{assignment.Role}";
                    claims.Add(new Claim(CustomClaimTypes.ParentRole, claimValue));
                }
            }
            else
            {
                // No assignments — default to ReadOnly (will be blocked by policies requiring parent context)
                claims.Add(new Claim("role", Roles.Employee));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecretKey()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
                issuer: "CrewService.GrpcService",
                audience: "CrewService.BlazorUI",
                claims: claims,
                expires: expirationDate,
                signingCredentials: creds
            );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];

        using (var numberGenerator = RandomNumberGenerator.Create())
        {
            numberGenerator.GetBytes(randomNumber);
        }

        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecretKey())),
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = "role"
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }

    private string GetJwtSecretKey()
    {
        return _configuration.GetValue<string>("Jwt:Key") ??
            throw new Exception("Jwt Key is not defined.");
    }
}
