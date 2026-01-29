using CrewService.Infrastructure.Models.UserAccount;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CrewService.Presentation.Services;

public sealed class AuthService(IConfiguration configuration, UserManager<User> userManager) : AuthSrvc.AuthSrvcBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IConfiguration _configuration = configuration;

    public override async Task<RegisterResponse> RegisterUser(RegisterRequest request, ServerCallContext context)
    {
        RegisterResponse response = new();

        if (!string.IsNullOrEmpty(request.Email) && !string.IsNullOrEmpty(request.Password))
        {
            User user = new()
            {
                UserName = request.Email,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                response.Success = true;
                response.Message.Add("User has successfully registered.");
            }
            else
            {
                response.Success = false;
                foreach (var erorr in result.Errors)
                    response.Message.Add(erorr.Description);
            }
        }
        else
        {
            response.Success = false;

            if (string.IsNullOrEmpty(request.Email))
                response.Message.Add("UserName is required.");

            if (string.IsNullOrEmpty(request.Password))
                response.Message.Add("Password is required");
        }
        
        return response;
    }

    public override async Task<AuthResponse> AuthenticateUser(AuthRequest request, ServerCallContext context)
    {
        AuthResponse response = new();

        if (!string.IsNullOrEmpty(request.UserName) && !string.IsNullOrEmpty(request.Password))
        {
            var user = await _userManager.FindByEmailAsync(request.UserName);

            if (user is null)
            {
                response.Success = false;
                response.Message.Add("User could not be found.");
            }
            else
            {
                var validated = await _userManager.CheckPasswordAsync(user, request.Password);

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

        var user = await _userManager.FindByEmailAsync(principal.Identity.Name);

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

    private async Task<AuthResponse> GenerateJwtAccessTokensAsync(User user)
    {
        AuthResponse response = new();

        var expireDate = DateTime.UtcNow.AddHours(1);

        var token = GenerateJwtToken(user.UserName!, expireDate);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiration = DateTime.UtcNow.AddHours(12);

        await _userManager.UpdateAsync(user);

        response.Token = token;
        response.Success = true;
        response.TokenExpired = expireDate.Ticks;
        response.Message.Add("Successful Login");
        response.RefreshToken = refreshToken;

        return response;
    }

    private string GenerateJwtToken(string username, DateTime experationDate)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecretKey()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
                issuer: "CrewService.GrpcService",
                audience: "CrewService.BlazorUI",
                claims: claims,
                expires: experationDate,
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecretKey()))
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
