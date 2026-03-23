using CrewService.Domain.Constants;
using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Application.Modules.UserAccess;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.ValueObjects;
using CrewService.Infrastructure.Models.UserAccount;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CrewService.Presentation.Services;

public class InvitationService(
    IInvitationRepository invitationRepository,
    ICurrentUserService currentUserService,
    IUserParentAssignmentRepository assignmentRepository,
    IParentRepository parentRepository,
    IDynamicGroupRepository dynamicGroupRepository,
    UserManager<User> userManager,
    IInvitationEmailService emailService,
    IConfiguration configuration)
    : InvitationSrvc.InvitationSrvcBase
{
    private readonly IInvitationRepository _invitationRepository = invitationRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IUserParentAssignmentRepository _assignmentRepository = assignmentRepository;
    private readonly IParentRepository _parentRepository = parentRepository;
    private readonly IDynamicGroupRepository _dynamicGroupRepository = dynamicGroupRepository;
    private readonly UserManager<User> _userManager = userManager;
    private readonly IInvitationEmailService _emailService = emailService;
    private readonly string _baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://localhost:7132";

    public override async Task<InvitationResponse> CreateInvitation(CreateInvitationRequest request, ServerCallContext context)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(request.Email))
            errors.Add("Email", ["Required"]);

        var isSystemAdminInvite = request.Role == Roles.SystemAdmin;

        if (!isSystemAdminInvite && request.ParentCtrlNbr <= 0)
            errors.Add("ParentCtrlNbr", ["Must be greater than 0"]);

        if (string.IsNullOrEmpty(request.Role))
            errors.Add("Role", ["Required"]);
        else if (!Roles.AllInvitableRoles.Contains(request.Role))
            errors.Add("Role", [$"Unknown role '{request.Role}'. Valid roles: {string.Join(", ", Roles.AllInvitableRoles)}"]);

        if (Roles.RolesRequiringRailroad.Contains(request.Role) && request.RailroadCtrlNbr <= 0)
            errors.Add("RailroadCtrlNbr", ["Required for the selected role"]);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        // Enforce role authorization
        if (isSystemAdminInvite)
        {
            EnsureSystemAdmin(context);
        }
        else
        {
            var callerRole = await GetCallerRoleForParentAsync(context, request.ParentCtrlNbr);
            EnsureCanCreateRole(callerRole, request.Role);

            // RailroadAdmin can only invite for their assigned railroad(s)
            if (callerRole == Roles.RailroadAdmin && request.RailroadCtrlNbr > 0)
            {
                var callerRailroads = GetCallerRailroadsForParent(context, request.ParentCtrlNbr);
                if (!callerRailroads.Contains(request.RailroadCtrlNbr))
                    throw new RpcException(new Status(StatusCode.PermissionDenied,
                        "You can only create invitations for your assigned railroad."));
            }
        }

        // Validate railroad belongs to the parent when required
        if (request.RailroadCtrlNbr > 0)
        {
            var railroads = await _dynamicGroupRepository.GetByGroupTypeNameAsync("Railroad", request.ParentCtrlNbr);
            if (!railroads.Any(rr => rr.CtrlNbr.Value == request.RailroadCtrlNbr))
                throw new ValidationException("RailroadCtrlNbr", "Railroad does not belong to the selected parent");
        }

        ControlNumber? parentCtrlNbr = request.ParentCtrlNbr > 0
            ? ControlNumber.Create(request.ParentCtrlNbr)
            : null;

        var existing = await _invitationRepository.GetPendingByEmailAndParentAsync(request.Email, parentCtrlNbr);
        if (existing is not null)
            throw new ConflictException(nameof(Invitation), $"A pending invitation already exists for {request.Email}.");

        var expirationDays = request.ExpirationDays > 0 ? request.ExpirationDays : 7;

        var railroadCtrlNbr = request.RailroadCtrlNbr > 0
            ? ControlNumber.Create(request.RailroadCtrlNbr)
            : null;

        var invitation = Invitation.Create(
            request.Email,
            parentCtrlNbr,
            request.Role,
            _currentUserService.GetUserId().ToString(),
            expirationDays,
            railroadCtrlNbr);

        await _invitationRepository.AddAsync(invitation);

        // Send invitation email
        string parentName = "CrewService";
        if (invitation.ParentCtrlNbr is not null)
        {
            var parent = await _parentRepository.GetByCtrlNbrAsync(invitation.ParentCtrlNbr);
            parentName = parent?.Name.Value ?? $"Parent {invitation.ParentCtrlNbr.Value}";
        }
        var acceptUrl = $"{_baseUrl}/Account/AcceptInvitation?token={Uri.EscapeDataString(invitation.Token)}";

        await _emailService.SendInvitationAsync(
            invitation.Email, invitation.Role, parentName, acceptUrl, invitation.ExpiresAt);

        return MapToResponse(invitation);
    }

    public override async Task<InvitationResponse> GetInvitation(GetInvitationRequest request, ServerCallContext context)
    {
        var invitation = await _invitationRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Invitation with control number {request.CtrlNbr} was not found."));

        // SystemAdmin can view any; others must have a role for the invitation's parent
        if (invitation.ParentCtrlNbr is null)
            EnsureSystemAdmin(context);
        else
        {
            var callerRole = await GetCallerRoleForParentAsync(context, invitation.ParentCtrlNbr.Value);
            EnsureCanView(callerRole);
        }

        return MapToResponse(invitation);
    }

    public override async Task<GetInvitationsResponse> GetInvitationsByParent(GetInvitationsByParentRequest request, ServerCallContext context)
    {
        // ParentCtrlNbr == 0 means "global SystemAdmin invitations only"
        if (request.ParentCtrlNbr == 0)
        {
            EnsureSystemAdmin(context);
            var sysAdminInvitations = await _invitationRepository.GetByRoleAsync(Roles.SystemAdmin);
            var globalResponse = new GetInvitationsResponse();
            foreach (var inv in sysAdminInvitations)
                globalResponse.Invitations.Add(MapToResponse(inv, includeToken: false));
            return globalResponse;
        }

        var callerRole = await GetCallerRoleForParentAsync(context, request.ParentCtrlNbr);
        EnsureCanView(callerRole);

        var invitations = await _invitationRepository.GetByParentCtrlNbrAsync(request.ParentCtrlNbr);

        // SystemAdmin invitations are global — show them regardless of parent context
        if (callerRole == Roles.SystemAdmin)
        {
            var sysAdminInvitations = await _invitationRepository.GetByRoleAsync(Roles.SystemAdmin);
            var existingCtrlNbrs = invitations.Select(i => i.CtrlNbr).ToHashSet();
            invitations.AddRange(sysAdminInvitations.Where(i => !existingCtrlNbrs.Contains(i.CtrlNbr)));
        }

        var response = new GetInvitationsResponse();
        foreach (var invitation in invitations)
            response.Invitations.Add(MapToResponse(invitation, includeToken: false));

        return response;
    }

    public override async Task<GetInvitationsResponse> GetInvitationsByEmail(GetInvitationsByEmailRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.Email))
            throw new ValidationException("Email", "Required");

        // Only SystemAdmin can query across all parents by email
        EnsureSystemAdmin(context);

        var invitations = await _invitationRepository.GetByEmailAsync(request.Email);

        var response = new GetInvitationsResponse();
        foreach (var invitation in invitations)
            response.Invitations.Add(MapToResponse(invitation, includeToken: false));

        return response;
    }

    public override async Task<InvitationResponse> RevokeInvitation(RevokeInvitationRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        var invitation = await _invitationRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Invitation with control number {request.CtrlNbr} was not found."));

        if (invitation.ParentCtrlNbr is null)
            EnsureSystemAdmin(context);
        else
        {
            var callerRole = await GetCallerRoleForParentAsync(context, invitation.ParentCtrlNbr.Value);
            EnsureCanView(callerRole);
        }

        invitation.Revoke();
        await _invitationRepository.UpdateAsync(invitation);

        return MapToResponse(invitation);
    }

    public override async Task<InvitationResponse> ResendInvitation(ResendInvitationRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        var existing = await _invitationRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Invitation with control number {request.CtrlNbr} was not found."));

        if (existing.Status != InvitationStatus.Pending)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Cannot resend invitation with status '{existing.Status}'."));

        if (existing.ParentCtrlNbr is null)
            EnsureSystemAdmin(context);
        else
        {
            var callerRole = await GetCallerRoleForParentAsync(context, existing.ParentCtrlNbr.Value);
            EnsureCanView(callerRole);
        }

        // Revoke the old invitation and create a new one
        existing.Revoke();
        await _invitationRepository.UpdateAsync(existing);

        var newInvitation = Invitation.Create(
            existing.Email,
            existing.ParentCtrlNbr,
            existing.Role,
            _currentUserService.GetUserId().ToString(),
            railroadCtrlNbr: existing.RailroadCtrlNbr);

        await _invitationRepository.AddAsync(newInvitation);

        // Send reminder email
        string parentName = "CrewService";
        if (newInvitation.ParentCtrlNbr is not null)
        {
            var parent = await _parentRepository.GetByCtrlNbrAsync(newInvitation.ParentCtrlNbr);
            parentName = parent?.Name.Value ?? $"Parent {newInvitation.ParentCtrlNbr.Value}";
        }
        var acceptUrl = $"{_baseUrl}/Account/AcceptInvitation?token={Uri.EscapeDataString(newInvitation.Token)}";

        await _emailService.SendReminderAsync(
            newInvitation.Email, newInvitation.Role, parentName, acceptUrl, newInvitation.ExpiresAt);

        return MapToResponse(newInvitation);
    }

    [AllowAnonymous]
    public override async Task<ValidateInvitationTokenReply> ValidateInvitationToken(
        ValidateInvitationTokenRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.Token))
            return new ValidateInvitationTokenReply { IsValid = false };

        var invitation = await _invitationRepository.GetByTokenAsync(request.Token);

        if (invitation is null)
            return new ValidateInvitationTokenReply { IsValid = false };

        // Persist expired status if detected
        if (invitation.Status == InvitationStatus.Pending && DateTime.UtcNow > invitation.ExpiresAt)
        {
            invitation.MarkExpired();
            await _invitationRepository.UpdateAsync(invitation);
        }

        var parentName = "CrewService";
        if (invitation.ParentCtrlNbr is not null)
        {
            var parent = await _parentRepository.GetByCtrlNbrAsync(invitation.ParentCtrlNbr);
            parentName = parent?.Name.Value ?? $"Parent {invitation.ParentCtrlNbr.Value}";
        }

        var existingUser = await _userManager.FindByEmailAsync(invitation.Email);

        var railroadName = string.Empty;
        if (invitation.RailroadCtrlNbr is not null && invitation.ParentCtrlNbr is not null)
        {
            var railroads = await _dynamicGroupRepository.GetByGroupTypeNameAsync("Railroad", invitation.ParentCtrlNbr.Value);
            railroadName = railroads.FirstOrDefault(rr => rr.CtrlNbr == invitation.RailroadCtrlNbr)?.Name ?? string.Empty;
        }

        return new ValidateInvitationTokenReply
        {
            IsValid = invitation.IsValid,
            Email = invitation.Email,
            Role = invitation.Role,
            ParentName = parentName,
            Status = invitation.Status.ToString(),
            UserAlreadyExists = existingUser is not null,
            RailroadName = railroadName
        };
    }

    #region Authorization helpers

    /// <summary>
    /// Returns the caller's highest role for the given parent, or null if no access.
    /// SystemAdmin bypasses parent scoping entirely.
    /// </summary>
    private Task<string?> GetCallerRoleForParentAsync(ServerCallContext context, long parentCtrlNbr)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        // SystemAdmin has full access
        if (user.IsInRole(Roles.SystemAdmin))
            return Task.FromResult<string?>(Roles.SystemAdmin);

        // Check parent_role claims: "{parentCtrlNbr}:{role}" or "{parentCtrlNbr}:{role}:{railroadCtrlNbr}"
        var parentRoles = user.Claims
            .Where(c => c.Type == CustomClaimTypes.ParentRole)
            .Select(c => c.Value.Split(':'))
            .Where(parts => parts.Length >= 2 && long.TryParse(parts[0], out var p) && p == parentCtrlNbr)
            .Select(parts => parts[1])
            .ToList();

        if (parentRoles.Count == 0)
            return Task.FromResult<string?>(null);

        // Return the highest-privilege role
        if (parentRoles.Contains(Roles.ParentAdmin)) return Task.FromResult<string?>(Roles.ParentAdmin);
        if (parentRoles.Contains(Roles.RailroadAdmin)) return Task.FromResult<string?>(Roles.RailroadAdmin);
        return Task.FromResult<string?>(parentRoles[0]);
    }

    /// <summary>
    /// Returns the railroad CtrlNbrs the caller is assigned to for the given parent.
    /// </summary>
    private static HashSet<long> GetCallerRailroadsForParent(ServerCallContext context, long parentCtrlNbr)
    {
        var user = context.GetHttpContext().User;
        return user.Claims
            .Where(c => c.Type == CustomClaimTypes.ParentRole)
            .Select(c => c.Value.Split(':'))
            .Where(parts => parts.Length >= 3
                && long.TryParse(parts[0], out var p) && p == parentCtrlNbr
                && long.TryParse(parts[2], out _))
            .Select(parts => long.Parse(parts[2]))
            .ToHashSet();
    }

    private static readonly HashSet<string> _adminRoles =
        [Roles.SystemAdmin, Roles.ParentAdmin, Roles.RailroadAdmin];

    private static readonly HashSet<string> _nonAdminRoles =
        [Roles.CraftManager, Roles.CrewManager, Roles.Dispatcher, Roles.PayrollClerk, Roles.Employee];

    private static void EnsureCanView(string? callerRole)
    {
        if (callerRole is null || !_adminRoles.Contains(callerRole))
            throw new RpcException(new Status(StatusCode.PermissionDenied,
                "You do not have permission to manage invitations for this parent."));
    }

    private static void EnsureCanCreateRole(string? callerRole, string targetRole)
    {
        EnsureCanView(callerRole);

        // SystemAdmin can create any role, including other SystemAdmins
        if (callerRole == Roles.SystemAdmin)
            return;

        // ParentAdmin can create any per-parent role (not SystemAdmin)
        if (callerRole == Roles.ParentAdmin && targetRole != Roles.SystemAdmin)
            return;

        // RailroadAdmin can only create non-admin roles
        if (callerRole == Roles.RailroadAdmin && _nonAdminRoles.Contains(targetRole))
            return;

        throw new RpcException(new Status(StatusCode.PermissionDenied,
            $"Your role ({callerRole}) cannot create invitations for the '{targetRole}' role."));
    }

    private static void EnsureSystemAdmin(ServerCallContext context)
    {
        var user = context.GetHttpContext().User;
        if (!user.IsInRole(Roles.SystemAdmin))
            throw new RpcException(new Status(StatusCode.PermissionDenied,
                "Only SystemAdmin can query invitations by email across all parents."));
    }

    #endregion

    private static InvitationResponse MapToResponse(Invitation invitation, bool includeToken = true)
    {
        var response = new InvitationResponse
        {
            CtrlNbr = invitation.CtrlNbr.Value,
            Email = invitation.Email,
            ParentCtrlNbr = invitation.ParentCtrlNbr?.Value ?? 0,
            Role = invitation.Role,
            Status = invitation.Status.ToString(),
            ExpiresAt = invitation.ExpiresAt.Ticks
        };

        if (includeToken)
            response.Token = invitation.Token;

        if (invitation.AcceptedAt.HasValue)
            response.AcceptedAt = invitation.AcceptedAt.Value.Ticks;

        if (invitation.RevokedAt.HasValue)
            response.RevokedAt = invitation.RevokedAt.Value.Ticks;

        if (invitation.SupersededAt.HasValue)
            response.SupersededAt = invitation.SupersededAt.Value.Ticks;

        if (invitation.RailroadCtrlNbr is not null)
            response.RailroadCtrlNbr = invitation.RailroadCtrlNbr.Value;

        return response;
    }
}
