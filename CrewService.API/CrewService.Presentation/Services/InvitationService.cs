using CrewService.Domain.Constants;
using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Application.UserAccess;
using CrewService.Application.Modules.UserAccount;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CrewService.Presentation.Services;

public class InvitationService(
    InvitationAppService invitationAppService,
    ICurrentUserService currentUserService,
    IUserAccountService userAccountService)
    : InvitationSrvc.InvitationSrvcBase
{
    private readonly InvitationAppService _invitationAppService = invitationAppService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IUserAccountService _userAccountService = userAccountService;

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

        if (Roles.RequiresRailroad(request.Role) && request.RailroadCtrlNbr <= 0)
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

            if (callerRole == Roles.RailroadAdmin && request.RailroadCtrlNbr > 0)
            {
                var callerRailroads = GetCallerRailroadsForParent(context, request.ParentCtrlNbr);
                if (!callerRailroads.Contains(request.RailroadCtrlNbr))
                    throw new RpcException(new Status(StatusCode.PermissionDenied,
                        "You can only create invitations for your assigned railroad."));
            }
        }

        // Validate railroad belongs to parent
        if (request.RailroadCtrlNbr > 0)
        {
            var valid = await _invitationAppService.ValidateRailroadBelongsToParentAsync(
                request.ParentCtrlNbr, request.RailroadCtrlNbr, context.CancellationToken);
            if (!valid)
                throw new ValidationException("RailroadCtrlNbr", "Railroad does not belong to the selected parent");
        }

        ControlNumber? parentCtrlNbr = request.ParentCtrlNbr > 0
            ? ControlNumber.Create(request.ParentCtrlNbr) : null;
        ControlNumber? railroadCtrlNbr = request.RailroadCtrlNbr > 0
            ? ControlNumber.Create(request.RailroadCtrlNbr) : null;

        var expirationDays = request.ExpirationDays > 0 ? request.ExpirationDays : 7;
        var parentName = await _invitationAppService.GetParentNameAsync(parentCtrlNbr, context.CancellationToken);

        var invitation = await _invitationAppService.CreateAsync(
            request.Email, parentCtrlNbr, request.Role, railroadCtrlNbr,
            expirationDays, parentName, context.CancellationToken);

        return MapToResponse(invitation);
    }

    public override async Task<InvitationResponse> GetInvitation(GetInvitationRequest request, ServerCallContext context)
    {
        var invitation = await _invitationAppService.GetAsync(
            ControlNumber.Create(request.CtrlNbr), context.CancellationToken);

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
        if (request.ParentCtrlNbr == 0)
        {
            EnsureSystemAdmin(context);
            var sysAdminInvitations = await _invitationAppService.GetByRoleAsync(Roles.SystemAdmin, context.CancellationToken);
            var globalResponse = new GetInvitationsResponse();
            foreach (var inv in sysAdminInvitations)
                globalResponse.Invitations.Add(MapToResponse(inv, includeToken: false));
            return globalResponse;
        }

        var callerRole = await GetCallerRoleForParentAsync(context, request.ParentCtrlNbr);
        EnsureCanView(callerRole);

        var invitations = await _invitationAppService.GetByParentAsync(request.ParentCtrlNbr, context.CancellationToken);

        if (callerRole == Roles.SystemAdmin)
        {
            var sysAdminInvitations = await _invitationAppService.GetByRoleAsync(Roles.SystemAdmin, context.CancellationToken);
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

        EnsureSystemAdmin(context);

        var invitations = await _invitationAppService.GetByEmailAsync(request.Email, context.CancellationToken);

        var response = new GetInvitationsResponse();
        foreach (var invitation in invitations)
            response.Invitations.Add(MapToResponse(invitation, includeToken: false));
        return response;
    }

    public override async Task<InvitationResponse> RevokeInvitation(RevokeInvitationRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        var invitation = await _invitationAppService.GetAsync(
            ControlNumber.Create(request.CtrlNbr), context.CancellationToken);

        if (invitation.ParentCtrlNbr is null)
            EnsureSystemAdmin(context);
        else
        {
            var callerRole = await GetCallerRoleForParentAsync(context, invitation.ParentCtrlNbr.Value);
            EnsureCanView(callerRole);
        }

        var revoked = await _invitationAppService.RevokeAsync(
            ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
        return MapToResponse(revoked);
    }

    public override async Task<InvitationResponse> ResendInvitation(ResendInvitationRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        var existing = await _invitationAppService.GetAsync(
            ControlNumber.Create(request.CtrlNbr), context.CancellationToken);

        if (existing.Status != InvitationStatus.Pending)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Cannot resend invitation with status '{existing.Status}'."));

        if (existing.ParentCtrlNbr is null)
            EnsureSystemAdmin(context);
        else
        {
            var callerRole = await GetCallerRoleForParentAsync(context, existing.ParentCtrlNbr.Value);
            EnsureCanView(callerRole);
        }

        var parentName = await _invitationAppService.GetParentNameAsync(existing.ParentCtrlNbr, context.CancellationToken);
        var newInvitation = await _invitationAppService.ResendAsync(
            ControlNumber.Create(request.CtrlNbr), parentName, context.CancellationToken);
        return MapToResponse(newInvitation);
    }

    [AllowAnonymous]
    public override async Task<ValidateInvitationTokenReply> ValidateInvitationToken(
        ValidateInvitationTokenRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.Token))
            return new ValidateInvitationTokenReply { IsValid = false };

        var info = await _invitationAppService.ValidateTokenAsync(request.Token, context.CancellationToken);
        if (!info.IsValid && string.IsNullOrEmpty(info.Email))
            return new ValidateInvitationTokenReply { IsValid = false };

        var parentName = await _invitationAppService.GetParentNameAsync(info.ParentCtrlNbr, context.CancellationToken);
        var railroadName = await _invitationAppService.GetRailroadNameAsync(
            info.ParentCtrlNbr, null, context.CancellationToken);

        var existingUser = await _userAccountService.FindByEmailAsync(info.Email);

        return new ValidateInvitationTokenReply
        {
            IsValid = info.IsValid,
            Email = info.Email,
            Role = info.Role,
            ParentName = parentName,
            Status = info.Status,
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
        if (callerRole == Roles.RailroadAdmin && !_adminRoles.Contains(targetRole))
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
