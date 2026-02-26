using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class InvitationService(
    IInvitationRepository invitationRepository,
    ICurrentUserService currentUserService)
    : InvitationSrvc.InvitationSrvcBase
{
    private readonly IInvitationRepository _invitationRepository = invitationRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

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

        var existing = await _invitationRepository.GetPendingByEmailAndParentAsync(request.Email, request.ParentCtrlNbr);
        if (existing is not null)
            throw new ConflictException(nameof(Invitation), $"A pending invitation already exists for {request.Email} at parent {request.ParentCtrlNbr}.");

        var invitation = Invitation.Create(
            request.Email,
            request.ParentCtrlNbr,
            request.Role,
            _currentUserService.GetUserId().ToString());

        await _invitationRepository.AddAsync(invitation);

        return MapToResponse(invitation);
    }

    public override async Task<InvitationResponse> GetInvitation(GetInvitationRequest request, ServerCallContext context)
    {
        var invitation = await _invitationRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Invitation with control number {request.CtrlNbr} was not found."));

        return MapToResponse(invitation);
    }

    public override async Task<GetInvitationsResponse> GetInvitationsByParent(GetInvitationsByParentRequest request, ServerCallContext context)
    {
        if (request.ParentCtrlNbr <= 0)
            throw new ValidationException("ParentCtrlNbr", "Must be greater than 0");

        var invitations = await _invitationRepository.GetByParentCtrlNbrAsync(request.ParentCtrlNbr);

        var response = new GetInvitationsResponse();
        foreach (var invitation in invitations)
            response.Invitations.Add(MapToResponse(invitation));

        return response;
    }

    public override async Task<GetInvitationsResponse> GetInvitationsByEmail(GetInvitationsByEmailRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.Email))
            throw new ValidationException("Email", "Required");

        var invitations = await _invitationRepository.GetByEmailAsync(request.Email);

        var response = new GetInvitationsResponse();
        foreach (var invitation in invitations)
            response.Invitations.Add(MapToResponse(invitation));

        return response;
    }

    public override async Task<InvitationResponse> RevokeInvitation(RevokeInvitationRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new ValidationException("CtrlNbr", "Must be greater than 0");

        var invitation = await _invitationRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Invitation with control number {request.CtrlNbr} was not found."));

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

        // Revoke the old invitation and create a new one
        existing.Revoke();
        await _invitationRepository.UpdateAsync(existing);

        var newInvitation = Invitation.Create(
            existing.Email,
            existing.ParentCtrlNbr.Value,
            existing.Role,
            _currentUserService.GetUserId().ToString());

        await _invitationRepository.AddAsync(newInvitation);

        return MapToResponse(newInvitation);
    }

    private static InvitationResponse MapToResponse(Invitation invitation)
    {
        var response = new InvitationResponse
        {
            CtrlNbr = invitation.CtrlNbr.Value,
            Email = invitation.Email,
            ParentCtrlNbr = invitation.ParentCtrlNbr.Value,
            Role = invitation.Role,
            Status = invitation.Status.ToString(),
            Token = invitation.Token,
            ExpiresAt = invitation.ExpiresAt.Ticks
        };

        if (invitation.AcceptedAt.HasValue)
            response.AcceptedAt = invitation.AcceptedAt.Value.Ticks;

        return response;
    }
}
