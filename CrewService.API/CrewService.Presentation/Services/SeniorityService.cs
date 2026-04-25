using CrewService.Application.Qualifications;
using CrewService.Domain.Exceptions;
using CrewService.Presentation.Services;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class SeniorityService(
    ISeniorityRepository seniorityRepository,
    IRosterRepository rosterRepository,
    ICraftRepository craftRepository,
    IEmployeeRepository employeeRepository,
    ISeniorityStateRepository seniorityStateRepository,
    IQualificationTypeRepository qualificationTypeRepository,
    IEmployeeQualificationRepository employeeQualificationRepository,
    EmployeeNameService employeeNameService,
    IOrchestrationUnitOfWorkFactory uowFactory,
    QualificationReactiveService qualificationReactiveService) : SenioritySrvc.SenioritySrvcBase
{
    private readonly ISeniorityRepository _seniorityRepository = seniorityRepository;
    private readonly IRosterRepository _rosterRepository = rosterRepository;
    private readonly ICraftRepository _craftRepository = craftRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly ISeniorityStateRepository _seniorityStateRepository = seniorityStateRepository;
    private readonly IQualificationTypeRepository _qualificationTypeRepository = qualificationTypeRepository;
    private readonly IEmployeeQualificationRepository _employeeQualificationRepository = employeeQualificationRepository;
    private readonly IOrchestrationUnitOfWorkFactory _uowFactory = uowFactory;
        private readonly QualificationReactiveService _qualificationReactiveService = qualificationReactiveService;
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

        // Batch-load all referenced seniority states
        var uniqueStateCtrlNbrs = seniorities.Select(s => s.SeniorityStateCtrlNbr).Distinct().ToList();
        var stateMap = new Dictionary<long, string>();
        foreach (var stateCtrlNbr in uniqueStateCtrlNbrs)
        {
            var state = await _seniorityStateRepository.GetByCtrlNbrAsync(stateCtrlNbr);
            stateMap[stateCtrlNbr.Value] = state?.StateDescription ?? string.Empty;
        }

        // Batch-compute RestrictionLabels: for each QualificationType with a RestrictionLabel
        // scoped to this craft, check independently per employee whether they hold an active qual.
        var empRestrictionLabels = new Dictionary<ControlNumber, List<string>>();
        var rosterCtrlNbr = seniorities.Select(s => s.RosterCtrlNbr).FirstOrDefault();
        if (rosterCtrlNbr is not null)
        {
            var roster = await _rosterRepository.GetByCtrlNbrAsync(rosterCtrlNbr);
            if (roster is not null)
            {
                var restrictingQualTypes = (await _qualificationTypeRepository.GetActiveByCraftCtrlNbrAsync(roster.CraftCtrlNbr))
                    .Where(qt => qt.RestrictionLabel is not null)
                    .ToList();

                if (restrictingQualTypes.Count > 0)
                {
                    var empQuals = await _employeeQualificationRepository
                        .GetActiveByEmployeeCtrlNbrsAsync(uniqueEmpCtrlNbrs);

                    // Index active quals by employee → set of QualType ctrl nbrs
                    var empActiveQualTypes = empQuals
                        .GroupBy(eq => eq.EmployeeCtrlNbr)
                        .ToDictionary(g => g.Key, g => g.Select(eq => eq.QualificationTypeCtrlNbr!).ToHashSet());

                    foreach (var empCtrlNbr in uniqueEmpCtrlNbrs)
                    {
                        empActiveQualTypes.TryGetValue(empCtrlNbr, out var heldQuals);
                        heldQuals ??= [];

                        foreach (var qt in restrictingQualTypes)
                        {
                            if (!heldQuals.Contains(qt.CtrlNbr))
                            {
                                if (!empRestrictionLabels.TryGetValue(empCtrlNbr, out var labels))
                                {
                                    labels = [];
                                    empRestrictionLabels[empCtrlNbr] = labels;
                                }
                                labels.Add(qt.RestrictionLabel!);
                            }
                        }
                    }
                }
            }
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
                if (!string.IsNullOrEmpty(employee.UserId))
                    fullNameLnf = await employeeNameService.GetFullNameLnfAsync(employee.UserId);
            }

            var stateName = stateMap.GetValueOrDefault(seniority.SeniorityStateCtrlNbr.Value, string.Empty);

            var sr = new SeniorityResponse
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
            };
            if (empRestrictionLabels.TryGetValue(seniority.EmployeeCtrlNbr, out var restrictionLabels))
                sr.RestrictionLabels.AddRange(restrictionLabels);
            response.Seniority.Add(sr);
        }

        response.TotalCount = response.Seniority.Count;
        return response;
    }

    public override async Task<SeniorityResponse> GetAsync(GetSeniorityRequest request, ServerCallContext context)
    {
        var seniority = await _seniorityRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority, with control number {request.CtrlNbr}, was not found."));

        return new SeniorityResponse
        {
            CtrlNbr = seniority.CtrlNbr.Value,
            RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
            EmployeeCtrlNbr = seniority.EmployeeCtrlNbr.Value,
            LastActiveRoster = seniority.LastActiveRoster,
            RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
            Rank = seniority.Rank,
            SeniorityStateCtrlNbr = seniority.SeniorityStateCtrlNbr.Value,
            CanTrain = seniority.CanTrain
        };
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

        // Auto-assign required qualifications scoped to this craft
        var roster = await _rosterRepository.GetByCtrlNbrAsync(seniority.RosterCtrlNbr);
        if (roster is not null)
            await _qualificationReactiveService.HandleAddedToRosterAsync(
                seniority.EmployeeCtrlNbr, roster.CraftCtrlNbr);

        return new SeniorityResponse
        {
            CtrlNbr = seniority.CtrlNbr.Value,
            RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
            EmployeeCtrlNbr = seniority.EmployeeCtrlNbr.Value,
            LastActiveRoster = seniority.LastActiveRoster,
            RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
            Rank = seniority.Rank,
            SeniorityStateCtrlNbr = seniority.SeniorityStateCtrlNbr.Value,
            CanTrain = seniority.CanTrain
        };
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

        return new SeniorityResponse
        {
            CtrlNbr = seniority.CtrlNbr.Value,
            RosterCtrlNbr = seniority.RosterCtrlNbr.Value,
            EmployeeCtrlNbr = seniority.EmployeeCtrlNbr.Value,
            LastActiveRoster = seniority.LastActiveRoster,
            RosterDate = seniority.RosterDate.ToString("yyyy-MM-dd"),
            Rank = seniority.Rank,
            SeniorityStateCtrlNbr = seniority.SeniorityStateCtrlNbr.Value,
            CanTrain = seniority.CanTrain
        };
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