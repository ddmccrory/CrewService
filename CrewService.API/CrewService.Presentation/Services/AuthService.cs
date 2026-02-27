using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.UserAccess;
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

public sealed class AuthService(
    IConfiguration configuration,
    UserManager<User> userManager,
    IUserParentAssignmentRepository assignmentRepository,
    IInvitationRepository invitationRepository) : AuthSrvc.AuthSrvcBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IConfiguration _configuration = configuration;
    private readonly IUserParentAssignmentRepository _assignmentRepository = assignmentRepository;
    private readonly IInvitationRepository _invitationRepository = invitationRepository;

    public override async Task<RegisterResponse> RegisterUser(RegisterRequest request, ServerCallContext context)
    {
        RegisterResponse response = new();

        if (string.IsNullOrEmpty(request.InvitationToken))
        {
            response.Success = false;
            response.Message.Add("Invitation token is required.");
            return response;
        }

        var invitation = await _invitationRepository.GetByTokenAsync(request.InvitationToken);

        if (invitation is null)
        {
            response.Success = false;
            response.Message.Add("Invalid invitation token.");
            return response;
        }

        if (!invitation.IsValid)
        {
            response.Success = false;
            response.Message.Add($"Invitation is no longer valid (status: {invitation.Status}).");

            // Persist expired status if detected
            if (invitation.Status == InvitationStatus.Pending && DateTime.UtcNow > invitation.ExpiresAt)
            {
                invitation.MarkExpired();
                await _invitationRepository.UpdateAsync(invitation);
            }

            return response;
        }

        // Check if user already exists (e.g., invited to a second parent)
        var existingUser = await _userManager.FindByEmailAsync(invitation.Email);

        if (existingUser is null)
        {
            if (string.IsNullOrEmpty(request.Password))
            {
                response.Success = false;
                response.Message.Add("Password is required.");
                return response;
            }

            existingUser = new User
            {
                UserName = invitation.Email,
                Email = invitation.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(existingUser, request.Password);

            if (!result.Succeeded)
            {
                response.Success = false;
                foreach (var error in result.Errors)
                    response.Message.Add(error.Description);
                return response;
            }
        }

        // Accept the invitation
        invitation.Accept();
        await _invitationRepository.UpdateAsync(invitation);

        // Create the UserParentAssignment from the invitation
        var assignment = UserParentAssignment.Create(
            existingUser.Id,
            invitation.ParentCtrlNbr.Value,
            invitation.Role);
        await _assignmentRepository.AddAsync(assignment);

        response.Success = true;
        response.Message.Add("User has successfully registered.");
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

        var token = await GenerateJwtTokenAsync(user, expireDate);
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

    private async Task<string> GenerateJwtTokenAsync(User user, DateTime expirationDate)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.NameIdentifier, user.Id)
        };

        // Global role: SystemAdmin bypasses parent scoping
        if (string.Equals(user.PrimaryRoleId, Roles.SystemAdmin, StringComparison.Ordinal))
        {
            claims.Add(new Claim(ClaimTypes.Role, Roles.SystemAdmin));
        }
        else
        {
            // Per-parent roles from UserParentAssignment
            var assignments = await _assignmentRepository.GetByUserIdAsync(user.Id);

            if (assignments.Count > 0)
            {
                foreach (var role in assignments.Select(a => a.Role).Distinct())
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                foreach (var assignment in assignments)
                {
                    claims.Add(new Claim("parent_role", $"{assignment.ParentCtrlNbr.Value}:{assignment.Role}"));
                }
            }
            else
            {
                // No assignments — default to ReadOnly (will be blocked by policies requiring parent context)
                claims.Add(new Claim(ClaimTypes.Role, Roles.ReadOnly));
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
