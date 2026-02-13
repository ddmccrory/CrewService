using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class EmailAddressTypeService(IEmailAddressTypeRepository repository) : EmailAddressTypeSrvc.EmailAddressTypeSrvcBase
{
    private readonly IEmailAddressTypeRepository _repository = repository;

    public override async Task<GetAllEmailAddressTypeResponse> GetAllAsync(GetAllEmailAddressTypeRequest request, ServerCallContext context)
    {
        var types = request.PageSize > 0
            ? await _repository.GetByClientCtrlNbrAsync(ControlNumber.Create(request.ClientCtrlNbr), request.PageNumber, request.PageSize)
            : await _repository.GetByClientCtrlNbrAsync(ControlNumber.Create(request.ClientCtrlNbr));

        var response = new GetAllEmailAddressTypeResponse { TotalCount = types.Count };

        foreach (var type in types)
        {
            response.EmaiAddressTypeList.Add(MapToResponse(type));
        }

        response.TotalCount = response.EmaiAddressTypeList.Count;

        return response;
    }

    public override async Task<EmailAddressTypeResponse> GetAsync(GetEmailAddressTypeRequest request, ServerCallContext context)
    {
        var type = await _repository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Email address type with control number {request.CtrlNbr} was not found."));

        return MapToResponse(type);
    }

    public override async Task<EmailAddressTypeResponse> CreateAsync(CreateEmailAddressTypeRequest request, ServerCallContext context)
    {
        var type = EmailAddressType.Create(
            request.ClientCtrlNbr,
            request.Name,
            request.Number,
            request.EmergencyType);

        _repository.Add(type);

        return MapToResponse(type, true, "Email address type created successfully.");
    }

    public override async Task<EmailAddressTypeResponse> UpdateAsync(UpdateEmailAddressTypeRequest request, ServerCallContext context)
    {
        var type = await _repository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Email address type with control number {request.CtrlNbr} was not found."));

        type.Update(request.Name, request.Number, request.EmergencyType);

        _repository.Update(type);

        return MapToResponse(type, true, "Email address type updated successfully.");
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteEmailAddressTypeRequest request, ServerCallContext context)
    {
        var type = await _repository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Email address type with control number {request.CtrlNbr} was not found."));

        _repository.Remove(type);

        return new DeleteResponse { Success = true, Messages = { "Email address type deleted successfully." } };
    }

    private static EmailAddressTypeResponse MapToResponse(EmailAddressType type, bool success = false, string? message = null)
    {
        var response = new EmailAddressTypeResponse
        {
            CtrlNbr = type.CtrlNbr.Value,
            ClientCtrlNbr = type.ClientCtrlNbr.Value,
            Name = type.Name,
            Number = type.Number,
            EmergencyType = type.EmergencyType,
            Success = success
        };

        if (message is not null) response.Messages.Add(message);

        return response;
    }
}