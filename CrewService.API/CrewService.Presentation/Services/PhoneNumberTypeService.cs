using CrewService.Application.ContactTypes;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class PhoneNumberTypeService(ContactTypesAppService contactTypesAppService) : PhoneNumberTypeSrvc.PhoneNumberTypeSrvcBase
{
    public override async Task<GetAllPhoneNumberTypeResponse> GetAllAsync(GetAllPhoneNumberTypeRequest request, ServerCallContext context)
    {
        var types = await contactTypesAppService.GetAllPhoneNumberTypesAsync(
            ControlNumber.Create(request.ClientCtrlNbr), request.PageNumber, request.PageSize,
            context.CancellationToken);

        var response = new GetAllPhoneNumberTypeResponse();
        foreach (var type in types)
            response.PhoneNumberTypeList.Add(MapToResponse(type));
        response.TotalCount = response.PhoneNumberTypeList.Count;
        return response;
    }

    public override async Task<PhoneNumberTypeResponse> GetAsync(GetPhoneNumberTypeRequest request, ServerCallContext context)
    {
        try
        {
            var type = await contactTypesAppService.GetPhoneNumberTypeAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapToResponse(type);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<PhoneNumberTypeResponse> CreateAsync(CreatePhoneNumberTypeRequest request, ServerCallContext context)
    {
        var type = await contactTypesAppService.CreatePhoneNumberTypeAsync(
            ControlNumber.Create(request.ClientCtrlNbr), request.Name, request.Number, request.EmergencyType,
            context.CancellationToken);
        return MapToResponse(type, true, "Phone number type created successfully.");
    }

    public override async Task<PhoneNumberTypeResponse> UpdateAsync(UpdatePhoneNumberTypeRequest request, ServerCallContext context)
    {
        try
        {
            var type = await contactTypesAppService.UpdatePhoneNumberTypeAsync(
                ControlNumber.Create(request.CtrlNbr), request.Name, request.Number, request.EmergencyType,
                context.CancellationToken);
            return MapToResponse(type, true, "Phone number type updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeletePhoneNumberTypeRequest request, ServerCallContext context)
    {
        try
        {
            await contactTypesAppService.DeletePhoneNumberTypeAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { "Phone number type deleted successfully." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static PhoneNumberTypeResponse MapToResponse(
        Domain.Models.ContactTypes.PhoneNumberType type, bool success = false, string? message = null)
    {
        var response = new PhoneNumberTypeResponse
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