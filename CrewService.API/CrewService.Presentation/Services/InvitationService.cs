using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Application.Modules.UserAccess;
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
    UserManager<User> userManager,
    IInvitationEmailService emailService,
    IConfiguration configuration)
    : InvitationSrvc.InvitationSrvcBase
{
    private readonly IInvitationRepository _invitationRepository = invitationRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IUserParentAssignmentRepository _assignmentRepository = assignmentRepository;
    private readonly IParentRepository _parentRepository = parentRepository;
    private readonly UserManager<User> _userManager = userManager;
    private readonly IInvitationEmailService _emailService = emailService;
    private readonly string _baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://localhost:7132";

    public override async Task<InvitationResponse> CreateInvitation(CreateInvitationRequest request, ServerCallContext context)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(request.Email))
            errors.Add("Email", ["Required"]);

        if (request.ParentCtrlNbr <= 0)
            errors.Add("ParentCtrlNbr", ["Must be greater than 0"]);

        if (string.IsNullOrEmpty(request.Role))
            errors.Add("Role", ["Required"]);
        else if (!Roles.AllPerParentRoles.Contains(request.Role))
            errors.Add("Role", [$"Unknown role '{request.Role}'. Valid roles: {string.Join(", ", Roles.AllPerParentRoles)}"]);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        // Enforce role authorization
        var callerRole = await GetCallerRoleForParentAsync(context, request.ParentCtrlNbr);
        EnsureCanCreateRole(callerRole, request.Role);

        var existing = await _invitationRepository.GetPendingByEmailAndParentAsync(request.Email, request.ParentCtrlNbr);
        if (existing is not null)
            throw new ConflictException(nameof(Invitation), $"A pending invitation already exists for {request.Email} at parent {request.ParentCtrlNbr}.");

        var expirationDays = request.ExpirationDays > 0 ? request.ExpirationDays : 7;

        var invitation = Invitation.Create(
            request.Email,
            request.ParentCtrlNbr,
            request.Role,
            _currentUserService.GetUserId().ToString(),
            expirationDays);

        await _invitationRepository.AddAsync(invitation);

        // Send invitation email
        var parent = await _parentRepository.GetByCtrlNbrAsync(invitation.ParentCtrlNbr);
        var parentName = parent?.Name.Value ?? $"Parent {invitation.ParentCtrlNbr.Value}";
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
        var callerRole = await GetCallerRoleForParentAsync(context, invitation.ParentCtrlNbr.Value);
        EnsureCanView(callerRole);

        return MapToResponse(invitation);
    }

    public override async Task<GetInvitationsResponse> GetInvitationsByParent(GetInvitationsByParentRequest request, ServerCallContext context)
    {
        if (request.ParentCtrlNbr <= 0)
            throw new ValidationException("ParentCtrlNbr", "Must be greater than 0");

        var callerRole = await GetCallerRoleForParentAsync(context, request.ParentCtrlNbr);
        EnsureCanView(callerRole);

        var invitations = await _invitationRepository.GetByParentCtrlNbrAsync(request.ParentCtrlNbr);

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

        var callerRole = await GetCallerRoleForParentAsync(context, invitation.ParentCtrlNbr.Value);
        EnsureCanView(callerRole);

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

        var callerRole = await GetCallerRoleForParentAsync(context, existing.ParentCtrlNbr.Value);
        EnsureCanView(callerRole);

        // Revoke the old invitation and create a new one
        existing.Revoke();
        await _invitationRepository.UpdateAsync(existing);

        var newInvitation = Invitation.Create(
            existing.Email,
            existing.ParentCtrlNbr.Value,
            existing.Role,
            _currentUserService.GetUserId().ToString());

        await _invitationRepository.AddAsync(newInvitation);

        // Send reminder email
        var parent = await _parentRepository.GetByCtrlNbrAsync(newInvitation.ParentCtrlNbr);
        var parentName = parent?.Name.Value ?? $"Parent {newInvitation.ParentCtrlNbr.Value}";
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

        var parent = await _parentRepository.GetByCtrlNbrAsync(invitation.ParentCtrlNbr);
        var parentName = parent?.Name.Value ?? $"Parent {invitation.ParentCtrlNbr.Value}";

        var existingUser = await _userManager.FindByEmailAsync(invitation.Email);

        return new ValidateInvitationTokenReply
        {
            IsValid = invitation.IsValid,
            Email = invitation.Email,
            Role = invitation.Role,
            ParentName = parentName,
            Status = invitation.Status.ToString(),
            UserAlreadyExists = existingUser is not null
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

        // Check parent_role claims: "{parentCtrlNbr}:{role}"
        var parentRoles = user.Claims
            .Where(c => c.Type == "parent_role")
            .Select(c => c.Value.Split(':'))
            .Where(parts => parts.Length == 2 && long.TryParse(parts[0], out var p) && p == parentCtrlNbr)
            .Select(parts => parts[1])
            .ToList();

        if (parentRoles.Count == 0)
            return Task.FromResult<string?>(null);

        // Return the highest-privilege role
        if (parentRoles.Contains(Roles.ParentAdmin)) return Task.FromResult<string?>(Roles.ParentAdmin);
        if (parentRoles.Contains(Roles.RailroadAdmin)) return Task.FromResult<string?>(Roles.RailroadAdmin);
        return Task.FromResult<string?>(parentRoles[0]);
    }

    private static readonly HashSet<string> _adminRoles =
        [Roles.SystemAdmin, Roles.ParentAdmin, Roles.RailroadAdmin];

    private static readonly HashSet<string> _nonAdminRoles =
        [Roles.CraftManager, Roles.CrewManager, Roles.Dispatcher, Roles.PayrollClerk, Roles.ReadOnly];

    private static void EnsureCanView(string? callerRole)
    {
        if (callerRole is null || !_adminRoles.Contains(callerRole))
            throw new RpcException(new Status(StatusCode.PermissionDenied,
                "You do not have permission to manage invitations for this parent."));
    }

    private static void EnsureCanCreateRole(string? callerRole, string targetRole)
    {
        EnsureCanView(callerRole);

        // SystemAdmin and ParentAdmin can create any per-parent role
        if (callerRole is Roles.SystemAdmin or Roles.ParentAdmin)
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
            ParentCtrlNbr = invitation.ParentCtrlNbr.Value,
            Role = invitation.Role,
            Status = invitation.Status.ToString(),
            ExpiresAt = invitation.ExpiresAt.Ticks
        };

        if (includeToken)
            response.Token = invitation.Token;

        if (invitation.AcceptedAt.HasValue)
            response.AcceptedAt = invitation.AcceptedAt.Value.Ticks;

        return response;
    }
}
