using CrewService.Domain.Constants;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Application.Models.UserAccount;
using CrewService.Application.Modules.UserAccount;
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
    IUserParentAssignmentRepository assignmentRepository,
    IInvitationRepository invitationRepository,
    ICurrentUserService currentUserService) : AuthSrvc.AuthSrvcBase
{
    private readonly IUserAccountService _userAccountService = userAccountService;
    private readonly IConfiguration _configuration = configuration;
    private readonly IUserParentAssignmentRepository _assignmentRepository = assignmentRepository;
    private readonly IInvitationRepository _invitationRepository = invitationRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public override async Task<AcceptInvitationResponse> AcceptInvitation(AcceptInvitationRequest request, ServerCallContext context)
    {
        AcceptInvitationResponse response = new();

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

        // Set audit context for this unauthenticated endpoint
        _currentUserService.SetAuditOverride(invitation.Email);

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
        var existingUser = await _userAccountService.FindByEmailAsync(invitation.Email);

        if (existingUser is null)
        {
            if (string.IsNullOrEmpty(request.Password))
            {
                response.Success = false;
                response.Message.Add("Password is required.");
                return response;
            }

            var createResult = await _userAccountService.CreateAsync(new CreateUserRequest
            {
                UserName = invitation.Email,
                Email = invitation.Email,
                Password = request.Password
            });

            if (!createResult.Result.Succeeded)
            {
                response.Success = false;
                foreach (var error in createResult.Result.Errors)
                    response.Message.Add(error);
                return response;
            }

            existingUser = await _userAccountService.FindByIdAsync(createResult.UserId);
        }

        // Accept the invitation
        invitation.Accept();
        await _invitationRepository.UpdateAsync(invitation);

        // Global SystemAdmin invitation (no parent) — set PrimaryRoleId
        if (invitation.ParentCtrlNbr is null)
        {
            await _userAccountService.UpdatePrimaryRoleAsync(existingUser!.Id, Roles.SystemAdmin);

            // Supersede prior SystemAdmin invitations for this email
            var oldInvitations = await _invitationRepository.GetAcceptedByEmailAndParentAsync(invitation.Email, null);
            foreach (var oldInv in oldInvitations.Where(i => i.CtrlNbr != invitation.CtrlNbr))
            {
                oldInv.MarkSuperseded();
                await _invitationRepository.UpdateAsync(oldInv);
            }

            response.Success = true;
            response.Message.Add("Invitation accepted successfully.");
            return response;
        }

        // Create or update the UserParentAssignment from the invitation
        var existingAssignments = await _assignmentRepository.GetByUserAndParentAsync(existingUser!.Id, invitation.ParentCtrlNbr!);
        var isParentScoped = !Roles.RequiresRailroad(invitation.Role);

        if (existingAssignments.Count > 0)
        {
            var hasRailroadScoped = existingAssignments.Any(a => Roles.RequiresRailroad(a.Role));
            var hasParentScoped = existingAssignments.Any(a => !Roles.RequiresRailroad(a.Role));

            if (isParentScoped && hasRailroadScoped)
            {
                // Upgrading from railroad-scoped to parent-scoped: replace all railroad assignments
                foreach (var old in existingAssignments)
                    await _assignmentRepository.DeleteAsync(old.CtrlNbr);

                var newAssignment = UserParentAssignment.Create(
                    existingUser.Id,
                    invitation.ParentCtrlNbr.Value,
                    invitation.Role);
                await _assignmentRepository.AddAsync(newAssignment);
            }
            else if (!isParentScoped && hasParentScoped)
            {
                // Downgrading from parent-scoped to railroad-scoped: replace parent assignment
                foreach (var old in existingAssignments)
                    await _assignmentRepository.DeleteAsync(old.CtrlNbr);

                var newAssignment = UserParentAssignment.Create(
                    existingUser.Id,
                    invitation.ParentCtrlNbr.Value,
                    invitation.Role,
                    invitation.RailroadCtrlNbr);
                await _assignmentRepository.AddAsync(newAssignment);
            }
            else
            {
                // Same scope type: update matching assignment or add new railroad
                var matchingAssignment = existingAssignments.FirstOrDefault(a => a.RailroadCtrlNbr == invitation.RailroadCtrlNbr);
                if (matchingAssignment is not null)
                {
                    matchingAssignment.UpdateRole(invitation.Role, invitation.RailroadCtrlNbr);
                    await _assignmentRepository.UpdateAsync(matchingAssignment);
                }
                else
                {
                    var newAssignment = UserParentAssignment.Create(
                        existingUser.Id,
                        invitation.ParentCtrlNbr.Value,
                        invitation.Role,
                        invitation.RailroadCtrlNbr);
                    await _assignmentRepository.AddAsync(newAssignment);
                }
            }

            // Mark all prior accepted invitations for this parent as superseded
            var oldInvitations = await _invitationRepository.GetAcceptedByEmailAndParentAsync(invitation.Email, invitation.ParentCtrlNbr);
            foreach (var oldInv in oldInvitations.Where(i => i.CtrlNbr != invitation.CtrlNbr))
            {
                oldInv.MarkSuperseded();
                await _invitationRepository.UpdateAsync(oldInv);
            }
        }
        else
        {
            // No existing assignments - create new
            var assignment = UserParentAssignment.Create(
                existingUser.Id,
                invitation.ParentCtrlNbr.Value,
                invitation.Role,
                invitation.RailroadCtrlNbr);
            await _assignmentRepository.AddAsync(assignment);
        }

        response.Success = true;
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
            var assignments = await _assignmentRepository.GetByUserIdAsync(user.Id);

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
