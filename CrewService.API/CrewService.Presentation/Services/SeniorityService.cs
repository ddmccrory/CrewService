using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Infrastructure.Models.UserAccount;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;

namespace CrewService.Presentation.Services;

public class SeniorityService(
    ISeniorityRepository seniorityRepository,
    IRosterRepository rosterRepository,
    ICraftRepository craftRepository,
    IEmployeeRepository employeeRepository,
    ISeniorityStateRepository seniorityStateRepository,
    UserManager<User> userManager) : SenioritySrvc.SenioritySrvcBase
{
    private readonly ISeniorityRepository _seniorityRepository = seniorityRepository;
    private readonly IRosterRepository _rosterRepository = rosterRepository;
    private readonly ICraftRepository _craftRepository = craftRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly ISeniorityStateRepository _seniorityStateRepository = seniorityStateRepository;
    private readonly UserManager<User> _userManager = userManager;

    public override async Task<GetAllSeniorityResponse> GetAllAsync(GetAllSeniorityRequest request, ServerCallContext context)
    {
        var response = new GetAllSeniorityResponse();

        var seniorities = request.RosterCtrlNbr > 0
            ? await _seniorityRepository.GetByRosterCtrlNbrAsync(ControlNumber.Create(request.RosterCtrlNbr))
            : await _seniorityRepository.GetAllAsync();

        if (seniorities.Count == 0)
        {
            response.TotalCount = 0;
            return response;
        }

        // Batch-load all referenced employees in a single query (no nav-property includes needed)
        var uniqueEmpCtrlNbrs = seniorities.Select(s => s.EmployeeCtrlNbr).Distinct().ToList();
        var employees = await _employeeRepository.GetByCtrlNbrsAsync(uniqueEmpCtrlNbrs);
        var employeeMap = employees.ToDictionary(e => e.CtrlNbr.Value);

        // Batch-load all referenced users in a single query
        var userIds = employees.Where(e => !string.IsNullOrEmpty(e.UserId)).Select(e => e.UserId).Distinct().ToList();
        var users = new Dictionary<string, User>();
        if (userIds.Count > 0)
        {
            foreach (var uid in userIds)
            {
                var user = await _userManager.FindByIdAsync(uid);
                if (user is not null)
                    users[uid] = user;
            }
        }

        // Batch-load all referenced seniority states
        var uniqueStateCtrlNbrs = seniorities.Select(s => s.SeniorityStateCtrlNbr).Distinct().ToList();
        var stateMap = new Dictionary<long, string>();
        foreach (var stateCtrlNbr in uniqueStateCtrlNbrs)
        {
            var state = await _seniorityStateRepository.GetByCtrlNbrAsync(stateCtrlNbr);
            stateMap[stateCtrlNbr.Value] = state?.StateDescription ?? string.Empty;
        }

        foreach (var seniority in seniorities)
        {
            var empNumber = string.Empty;
            var empUserId = string.Empty;
            var fullNameLnf = string.Empty;

            if (employeeMap.TryGetValue(seniority.EmployeeCtrlNbr.Value, out var employee))
            {
                empNumber = employee.EmployeeNumber;
                empUserId = employee.UserId;
                if (!string.IsNullOrEmpty(employee.UserId) && users.TryGetValue(employee.UserId, out var user))
                    fullNameLnf = user.FullNameLNF ?? string.Empty;
            }

            var stateName = stateMap.GetValueOrDefault(seniority.SeniorityStateCtrlNbr.Value, string.Empty);

            response.Seniority.Add(new SeniorityResponse
            {
                CtrlNbr = seniority.CtrlNbr.Value,
                RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
                EmployeeCtrlNbr = seniority.EmployeeCtrlNbr.Value,
                LastActiveRoster = seniority.LastActiveRoster,
                RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
                Rank = seniority.Rank,
                SeniorityStateCtrlNbr = seniority.SeniorityStateCtrlNbr.Value,
                CanTrain = seniority.CanTrain,
                EmployeeNumber = empNumber,
                EmployeeUserId = empUserId,
                SeniorityStateName = stateName,
                EmployeeFullNameLnf = fullNameLnf
            });
        }

        response.TotalCount = response.Seniority.Count;
        return response;
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