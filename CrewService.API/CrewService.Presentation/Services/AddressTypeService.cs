using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class AddressTypeService(IAddressTypeRepository repository) : AddressTypeSrvc.AddressTypeSrvcBase
{
    private readonly IAddressTypeRepository _repository = repository;

    public override async Task<GetAllAddressTypeResponse> GetAllAsync(GetAllAddressTypeRequest request, ServerCallContext context)
    {
        var types = request.PageSize > 0
            ? await _repository.GetByClientCtrlNbrAsync(ControlNumber.Create(request.ClientCtrlNbr), request.PageNumber, request.PageSize)
            : await _repository.GetByClientCtrlNbrAsync(ControlNumber.Create(request.ClientCtrlNbr));

        var response = new GetAllAddressTypeResponse();

        foreach (var type in types)
        {
            response.AddressTypeList.Add(MapToResponse(type));
        }

        response.TotalCount = response.AddressTypeList.Count;

        return response;
    }

    public override async Task<AddressTypeResponse> GetAsync(GetAddressTypeRequest request, ServerCallContext context)
    {
        var type = await _repository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Address type with control number {request.CtrlNbr} was not found."));

        return MapToResponse(type);
    }

    public override async Task<AddressTypeResponse> CreateAsync(CreateAddressTypeRequest request, ServerCallContext context)
    {
        var type = AddressType.Create(
            request.ClientCtrlNbr,
            request.Name,
            request.Number,
            request.EmergencyType);

        _repository.Add(type);

        return MapToResponse(type, true, "Address type created successfully.");
    }

    public override async Task<AddressTypeResponse> UpdateAsync(UpdateAddressTypeRequest request, ServerCallContext context)
    {
        var type = await _repository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Address type with control number {request.CtrlNbr} was not found."));

        type.Update(request.Name, request.Number, request.EmergencyType);

        _repository.Update(type);

        return MapToResponse(type, true, "Address type updated successfully.");
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteAddressTypeRequest request, ServerCallContext context)
    {
        var type = await _repository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Address type with control number {request.CtrlNbr} was not found."));

        _repository.Remove(type);

        return new DeleteResponse { Success = true, Messages = { "Address type deleted successfully." } };
    }

    private static AddressTypeResponse MapToResponse(AddressType type, bool success = false, string? message = null)
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