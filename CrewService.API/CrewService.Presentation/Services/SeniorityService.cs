using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class SeniorityService(
    ISeniorityRepository seniorityRepository,
    IRosterRepository rosterRepository,
    ICraftRepository craftRepository) : SenioritySrvc.SenioritySrvcBase
{
    private readonly ISeniorityRepository _seniorityRepository = seniorityRepository;
    private readonly IRosterRepository _rosterRepository = rosterRepository;
    private readonly ICraftRepository _craftRepository = craftRepository;

    public override async Task<GetAllSeniorityResponse> GetAllAsync(GetAllSeniorityRequest request, ServerCallContext context)
    {
        var response = new GetAllSeniorityResponse();
        var seniorities = await _seniorityRepository.GetAllAsync();

        foreach (var seniority in seniorities)
        {
            response.Seniority.Add(new SeniorityResponse
            {
                CtrlNbr = seniority.CtrlNbr.Value,
                RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
                EmployeeCtrlNbr = seniority.EmployeeCtrlNbr.Value,
                LastActiveRoster = seniority.LastActiveRoster,
                RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
                Rank = seniority.Rank,
                SeniorityStateCtrlNbr = seniority.SeniorityStateCtrlNbr.Value,
                CanTrain = seniority.CanTrain
            });
        }

        return await Task.FromResult(response);
    }

    public override async Task<SeniorityResponse> GetAsync(GetSeniorityRequest request, ServerCallContext context)
    {
        var seniority = await _seniorityRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority, with control number {request.CtrlNbr}, was not found."));

        return await Task.FromResult(new SeniorityResponse
        {
            CtrlNbr = seniority.CtrlNbr.Value,
            RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
            EmployeeCtrlNbr = seniority.EmployeeCtrlNbr.Value,
            LastActiveRoster = seniority.LastActiveRoster,
            RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
            Rank = seniority.Rank,
            SeniorityStateCtrlNbr = seniority.SeniorityStateCtrlNbr.Value,
            CanTrain = seniority.CanTrain
        });
    }

    public override async Task<SeniorityResponse> CreateAsync(CreateSeniorityRequest request, ServerCallContext context)
    {
        var seniority = Seniority.Create(
            request.RosterCtrlNbr,
            request.EmployeeCtrlNbr,
            request.LastActiveRoster,
            DateTime.Parse(request.RosterDate),
            request.Rank,
            ControlNumber.Create(request.SeniorityStateCtrlNbr),
            request.CanTrain);

        await _seniorityRepository.AddAsync(seniority);

        return await Task.FromResult(new SeniorityResponse
        {
            CtrlNbr = seniority.CtrlNbr.Value,
            RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
            EmployeeCtrlNbr = seniority.EmployeeCtrlNbr.Value,
            LastActiveRoster = seniority.LastActiveRoster,
            RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
            Rank = seniority.Rank,
            SeniorityStateCtrlNbr = seniority.SeniorityStateCtrlNbr.Value,
            CanTrain = seniority.CanTrain
        });
    }

    public override async Task<SeniorityResponse> UpdateAsync(UpdateSeniorityRequest request, ServerCallContext context)
    {
        var seniority = await _seniorityRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority, with control number {request.CtrlNbr}, was not found."));

        seniority.Update(
            request.LastActiveRoster,
            DateTime.Parse(request.RosterDate),
            request.Rank,
            ControlNumber.Create(request.SeniorityStateCtrlNbr),
            request.CanTrain);

        await _seniorityRepository.UpdateAsync(seniority);

        return await Task.FromResult(new SeniorityResponse
        {
            CtrlNbr = seniority.CtrlNbr.Value,
            RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
            EmployeeCtrlNbr = seniority.EmployeeCtrlNbr.Value,
            LastActiveRoster = seniority.LastActiveRoster,
            RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
            Rank = seniority.Rank,
            SeniorityStateCtrlNbr = seniority.SeniorityStateCtrlNbr.Value,
            CanTrain = seniority.CanTrain
        });
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteSeniorityRequest request, ServerCallContext context)
    {
        var seniority = await _seniorityRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority, with control number {request.CtrlNbr}, was not found."));

        await _seniorityRepository.DeleteAsync(seniority.CtrlNbr);

        return await Task.FromResult(new DeleteResponse
        {
            Success = true,
            Messages = { $"Seniority {seniority.CtrlNbr.Value} deleted." }
        });
    }

    public override async Task<ActiveCraftResponse> GetActiveCraftForEmployee(GetActiveCraftRequest request, ServerCallContext context)
    {
        var seniorityRecords = await _seniorityRepository.GetByEmployeeCtrlNbrAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr));

        var activeRecord = seniorityRecords.FirstOrDefault(s => s.LastActiveRoster);
        if (activeRecord is null)
            return new ActiveCraftResponse { Found = false };

        var roster = await _rosterRepository.GetByCtrlNbrAsync(activeRecord.RosterCtrlNbr);
        if (roster is null)
            return new ActiveCraftResponse { Found = false };

        var craft = await _craftRepository.GetByCtrlNbrAsync(roster.CraftCtrlNbr);
        if (craft is null)
            return new ActiveCraftResponse { Found = false };

        return new ActiveCraftResponse
        {
            CraftCtrlNbr = craft.CtrlNbr.Value,
            CraftName = craft.CraftName,
            Found = true
        };
    }
}