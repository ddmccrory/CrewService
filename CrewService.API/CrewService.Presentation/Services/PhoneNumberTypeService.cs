using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.Repositories;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class PhoneNumberTypeService(IPhoneNumberTypeRepository repository) : PhoneNumberTypeSrvc.PhoneNumberTypeSrvcBase
{
    private readonly IPhoneNumberTypeRepository _repository = repository;

    public override async Task<GetAllPhoneNumberTypeResponse> GetAllAsync(GetAllPhoneNumberTypeRequest request, ServerCallContext context)
    {
        var types = request.PageSize > 0
            ? await _repository.GetAllAsync(request.ClientCtrlNbr, request.PageNumber, request.PageSize)
            : await _repository.GetAllAsync(request.ClientCtrlNbr);

        var response = new GetAllPhoneNumberTypeResponse { TotalCount = types.Count };

        foreach (var type in types)
        {
            response.PhoneNumberTypeList.Add(MapToResponse(type));
        }

        response.TotalCount = response.PhoneNumberTypeList.Count;

        return response;
    }

    public override async Task<PhoneNumberTypeResponse> GetAsync(GetPhoneNumberTypeRequest request, ServerCallContext context)
    {
        var type = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Phone number type with control number {request.CtrlNbr} was not found."));

        return MapToResponse(type);
    }

    public override async Task<PhoneNumberTypeResponse> CreateAsync(CreatePhoneNumberTypeRequest request, ServerCallContext context)
    {
        var type = PhoneNumberType.Create(
            request.ClientCtrlNbr,
            request.Name,
            request.Number,
            request.EmergencyType);

        _repository.Add(type);

        return MapToResponse(type, true, "Phone number type created successfully.");
    }

    public override async Task<PhoneNumberTypeResponse> UpdateAsync(UpdatePhoneNumberTypeRequest request, ServerCallContext context)
    {
        var type = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Phone number type with control number {request.CtrlNbr} was not found."));

        type.Update(request.Name, request.Number, request.EmergencyType);

        _repository.Update(type);

        return MapToResponse(type, true, "Phone number type updated successfully.");
    }

    public override async Task<DeleteResponse> DeleteAsync(DeletePhoneNumberTypeRequest request, ServerCallContext context)
    {
        var type = await _repository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Phone number type with control number {request.CtrlNbr} was not found."));

        _repository.Remove(type);

        return new DeleteResponse { Success = true, Messages = { "Phone number type deleted successfully." } };
    }

    private static PhoneNumberTypeResponse MapToResponse(PhoneNumberType type, bool success = false, string? message = null)
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