using CrewService.Application.ContactTypes;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class EmailAddressTypeService(ContactTypesAppService contactTypesAppService) : EmailAddressTypeSrvc.EmailAddressTypeSrvcBase
{
    public override async Task<GetAllEmailAddressTypeResponse> GetAllAsync(GetAllEmailAddressTypeRequest request, ServerCallContext context)
    {
        var types = await contactTypesAppService.GetAllEmailAddressTypesAsync(
            ControlNumber.Create(request.ClientCtrlNbr), request.PageNumber, request.PageSize,
            context.CancellationToken);

        var response = new GetAllEmailAddressTypeResponse();
        foreach (var type in types)
            response.EmaiAddressTypeList.Add(MapToResponse(type));
        response.TotalCount = response.EmaiAddressTypeList.Count;
        return response;
    }

    public override async Task<EmailAddressTypeResponse> GetAsync(GetEmailAddressTypeRequest request, ServerCallContext context)
    {
        try
        {
            var type = await contactTypesAppService.GetEmailAddressTypeAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapToResponse(type);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<EmailAddressTypeResponse> CreateAsync(CreateEmailAddressTypeRequest request, ServerCallContext context)
    {
        var type = await contactTypesAppService.CreateEmailAddressTypeAsync(
            ControlNumber.Create(request.ClientCtrlNbr), request.Name, request.Number, request.EmergencyType,
            context.CancellationToken);
        return MapToResponse(type, true, "Email address type created successfully.");
    }

    public override async Task<EmailAddressTypeResponse> UpdateAsync(UpdateEmailAddressTypeRequest request, ServerCallContext context)
    {
        try
        {
            var type = await contactTypesAppService.UpdateEmailAddressTypeAsync(
                ControlNumber.Create(request.CtrlNbr), request.Name, request.Number, request.EmergencyType,
                context.CancellationToken);
            return MapToResponse(type, true, "Email address type updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteEmailAddressTypeRequest request, ServerCallContext context)
    {
        try
        {
            await contactTypesAppService.DeleteEmailAddressTypeAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { "Email address type deleted successfully." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static EmailAddressTypeResponse MapToResponse(
        Domain.Models.ContactTypes.EmailAddressType type, bool success = false, string? message = null)
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