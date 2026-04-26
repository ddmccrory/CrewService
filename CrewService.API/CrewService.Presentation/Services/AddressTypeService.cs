using CrewService.Application.ContactTypes;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class AddressTypeService(ContactTypesAppService contactTypesAppService) : AddressTypeSrvc.AddressTypeSrvcBase
{
    public override async Task<GetAllAddressTypeResponse> GetAllAsync(GetAllAddressTypeRequest request, ServerCallContext context)
    {
        var types = await contactTypesAppService.GetAllAddressTypesAsync(
            ControlNumber.Create(request.ClientCtrlNbr), request.PageNumber, request.PageSize,
            context.CancellationToken);

        var response = new GetAllAddressTypeResponse();
        foreach (var type in types)
            response.AddressTypeList.Add(MapToResponse(type));
        response.TotalCount = response.AddressTypeList.Count;
        return response;
    }

    public override async Task<AddressTypeResponse> GetAsync(GetAddressTypeRequest request, ServerCallContext context)
    {
        try
        {
            var type = await contactTypesAppService.GetAddressTypeAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapToResponse(type);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<AddressTypeResponse> CreateAsync(CreateAddressTypeRequest request, ServerCallContext context)
    {
        var type = await contactTypesAppService.CreateAddressTypeAsync(
            ControlNumber.Create(request.ClientCtrlNbr), request.Name, request.Number, request.EmergencyType,
            context.CancellationToken);
        return MapToResponse(type, true, "Address type created successfully.");
    }

    public override async Task<AddressTypeResponse> UpdateAsync(UpdateAddressTypeRequest request, ServerCallContext context)
    {
        try
        {
            var type = await contactTypesAppService.UpdateAddressTypeAsync(
                ControlNumber.Create(request.CtrlNbr), request.Name, request.Number, request.EmergencyType,
                context.CancellationToken);
            return MapToResponse(type, true, "Address type updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteAddressTypeRequest request, ServerCallContext context)
    {
        try
        {
            await contactTypesAppService.DeleteAddressTypeAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { "Address type deleted successfully." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static AddressTypeResponse MapToResponse(
        Domain.Models.ContactTypes.AddressType type, bool success = false, string? message = null)
    {
        var response = new AddressTypeResponse
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