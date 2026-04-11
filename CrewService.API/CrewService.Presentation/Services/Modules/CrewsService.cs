using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class CrewsService(
    ICrewRepository crewRepository,
    ICrewPositionRepository crewPositionRepository,
    ICrewIncumbencyRepository incumbencyRepository,
    ICrewAssignmentRepository assignmentRepository,
    IAssignmentRepository staffingAssignmentRepository,
    IAssignmentScheduleRepository assignmentScheduleRepository,
    IOrchestrationUnitOfWorkFactory uowFactory) : CrewsSrvc.CrewsSrvcBase
{
    public override async Task<GetAllCrewsResponse> GetAllCrews(GetAllCrewsRequest request, ServerCallContext context)
    {
        List<Domain.Modules.Crews.Crew> crews;
        if (!string.IsNullOrEmpty(request.CrewType))
            crews = await crewRepository.GetByTypeAsync(request.CrewType);
        else if (request.WorkAreaCtrlNbr > 0)
            crews = await crewRepository.GetByWorkAreaAsync(ControlNumber.Create(request.WorkAreaCtrlNbr));
        else if (request.RailroadCtrlNbr > 0)
            crews = await crewRepository.GetByRailroadAsync(ControlNumber.Create(request.RailroadCtrlNbr));
        else
            crews = await crewRepository.GetAllAsync();
        var crewIds = crews.Select(c => c.CtrlNbr).ToList();
        var allPositions = await crewPositionRepository.GetByCrewsAsync(crewIds);
        var allAssignments = await assignmentRepository.GetByCrewsAsync(crewIds);
        var positionCounts = allPositions.GroupBy(p => p.CrewCtrlNbr).ToDictionary(g => g.Key, g => g.Count());
        var daysMasks = allAssignments.GroupBy(a => a.CrewCtrlNbr).ToDictionary(g => g.Key, g => g.Aggregate(0, (mask, a) => mask | a.DaysOfWeekMask));

        var response = new GetAllCrewsResponse { TotalCount = crews.Count };
        foreach (var c in crews)
        {
            var mapped = MapCrew(c);
            positionCounts.TryGetValue(c.CtrlNbr, out var posCount);
            daysMasks.TryGetValue(c.CtrlNbr, out var daysMask);
            mapped.PositionCount = posCount;
            mapped.WorkDaysMask = daysMask;
            response.Crews.Add(mapped);
        }
        return response;
    }

    public override async Task<CrewResponse> GetCrew(GetCrewRequest request, ServerCallContext context)
    {
        var crew = await crewRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Crew {request.CtrlNbr} not found."));
        return MapCrew(crew);
    }

    public override async Task<CrewResponse> CreateCrew(CreateCrewRequest request, ServerCallContext context)
    {
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var effectiveDate = !string.IsNullOrWhiteSpace(request.EffectiveDate)
            ? DateTime.Parse(request.EffectiveDate).ToUniversalTime()
            : (DateTime?)null;
        var abolishedDate = !string.IsNullOrWhiteSpace(request.AbolishedDate)
            ? DateTime.Parse(request.AbolishedDate).ToUniversalTime()
            : (DateTime?)null;
        var crew = Crew.Create(request.CrewType, request.WorkAreaCtrlNbr, request.Name, request.IsActive, departmentCtrlNbr, effectiveDate, abolishedDate);

        await using var uow = await uowFactory.CreateAsync();
        uow.Crews.Add(crew);
        await uow.CommitAsync();

        return MapCrew(crew);
    }

    public override async Task<CrewResponse> UpdateCrew(UpdateCrewRequest request, ServerCallContext context)
    {
        var crew = await crewRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Crew {request.CtrlNbr} not found."));
        var departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;
        var effectiveDate = !string.IsNullOrWhiteSpace(request.EffectiveDate)
            ? DateTime.Parse(request.EffectiveDate).ToUniversalTime()
            : (DateTime?)null;
        var abolishedDate = !string.IsNullOrWhiteSpace(request.AbolishedDate)
            ? DateTime.Parse(request.AbolishedDate).ToUniversalTime()
            : (DateTime?)null;
        var crewType = !string.IsNullOrWhiteSpace(request.CrewType) ? request.CrewType : null;
        crew.Update(request.Name, request.IsActive, departmentCtrlNbr, effectiveDate, abolishedDate, crewType);

        await using var uow = await uowFactory.CreateAsync();
        uow.Crews.Update(crew);
        await uow.CommitAsync();

        return MapCrew(crew);
    }

    public override async Task<DeleteResponse> DeleteCrew(DeleteCrewRequest request, ServerCallContext context)
    {
        var crew = await crewRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Crew {request.CtrlNbr} not found."));

        await using var uow = await uowFactory.CreateAsync();
        uow.Crews.Remove(crew);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    public override async Task<GetCrewPositionsResponse> GetCrewPositions(GetCrewPositionsRequest request, ServerCallContext context)
    {
        var positions = await crewPositionRepository.GetByCrewAsync(ControlNumber.Create(request.CrewCtrlNbr));
        var response = new GetCrewPositionsResponse { TotalCount = positions.Count };
        foreach (var p in positions)
            response.Positions.Add(new CrewPositionResponse
            {
                CtrlNbr = p.CtrlNbr.Value,
                CrewCtrlNbr = p.CrewCtrlNbr.Value,
                CraftRoleCtrlNbr = p.CraftRoleCtrlNbr.Value,
                DisplayOrder = p.DisplayOrder
            });
        return response;
    }

    public override async Task<CrewPositionResponse> CreateCrewPosition(CreateCrewPositionRequest request, ServerCallContext context)
    {
        var staffablePosition = StaffablePosition.Create("Crew");
        var position = CrewPosition.Create(request.CrewCtrlNbr, request.CraftRoleCtrlNbr, request.DisplayOrder, staffablePosition.CtrlNbr);

        await using var uow = await uowFactory.CreateAsync();
        uow.StaffablePositions.Add(staffablePosition);
        uow.CrewPositions.Add(position);
        await uow.CommitAsync();

        return new CrewPositionResponse
        {
            CtrlNbr = position.CtrlNbr.Value,
            CrewCtrlNbr = position.CrewCtrlNbr.Value,
            CraftRoleCtrlNbr = position.CraftRoleCtrlNbr.Value,
            DisplayOrder = position.DisplayOrder
        };
    }

    public override async Task<DeleteResponse> DeleteCrewPosition(DeleteCrewPositionRequest request, ServerCallContext context)
    {
        var position = await crewPositionRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"CrewPosition {request.CtrlNbr} not found."));

        await using var uow = await uowFactory.CreateAsync();
        uow.CrewPositions.Remove(position);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    private static CrewResponse MapCrew(Crew c) => new()
    {
        CtrlNbr = c.CtrlNbr.Value,
        CrewType = c.CrewType,
        WorkAreaCtrlNbr = c.WorkAreaCtrlNbr.Value,
        DepartmentCtrlNbr = c.DepartmentCtrlNbr?.Value ?? 0,
        Name = c.Name,
        IsActive = c.IsActive,
        EffectiveDate = c.EffectiveDate.ToString("O"),
        AbolishedDate = c.AbolishedDate?.ToString("O") ?? string.Empty
    };

    // Incumbencies
    public override async Task<GetCrewIncumbenciesResponse> GetCrewIncumbencies(GetCrewIncumbenciesRequest request, ServerCallContext context)
    {
        var items = await incumbencyRepository.GetByCrewPositionAsync(ControlNumber.Create(request.CrewPositionCtrlNbr));
        var response = new GetCrewIncumbenciesResponse { TotalCount = items.Count };
        foreach (var i in items) response.Incumbencies.Add(MapIncumbency(i));
        return response;
    }

    public override async Task<CrewIncumbencyResponse> CreateCrewIncumbency(CreateCrewIncumbencyRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var incumbency = CrewIncumbency.Create(request.CrewPositionCtrlNbr, request.EmployeeCtrlNbr, startUtc, endUtc);

        await using var uow = await uowFactory.CreateAsync();
        uow.CrewIncumbencies.Add(incumbency);
        await uow.CommitAsync();

        return MapIncumbency(incumbency);
    }

    private static CrewIncumbencyResponse MapIncumbency(CrewIncumbency i) => new()
    {
        CtrlNbr = i.CtrlNbr.Value,
        CrewPositionCtrlNbr = i.CrewPositionCtrlNbr.Value,
        EmployeeCtrlNbr = i.EmployeeCtrlNbr.Value,
        StartUtc = i.StartUtc.ToString("O"),
        EndUtc = i.EndUtc?.ToString("O") ?? string.Empty
    };

    // Crew Assignments
    public override async Task<GetCrewAssignmentsResponse> GetCrewAssignments(GetCrewAssignmentsRequest request, ServerCallContext context)
    {
        var items = await assignmentRepository.GetByCrewAsync(ControlNumber.Create(request.CrewCtrlNbr));
        return await BuildCrewAssignmentsResponse(items);
    }

    public override async Task<GetCrewAssignmentsResponse> GetCrewAssignmentsByAssignment(GetCrewAssignmentsByAssignmentRequest request, ServerCallContext context)
    {
        var items = await assignmentRepository.GetByAssignmentAsync(ControlNumber.Create(request.AssignmentCtrlNbr));
        return await BuildCrewAssignmentsResponse(items);
    }

    private async Task<GetCrewAssignmentsResponse> BuildCrewAssignmentsResponse(List<CrewAssignment> items)
    {
        var crewCtrlNbrs = items.Select(a => a.CrewCtrlNbr).Distinct().ToList();
        var crewNames = new Dictionary<long, string>();
        foreach (var ctrlNbr in crewCtrlNbrs)
        {
            var crew = await crewRepository.GetByCtrlNbrAsync(ctrlNbr);
            if (crew is not null) crewNames[ctrlNbr.Value] = crew.Name;
        }
        var response = new GetCrewAssignmentsResponse { TotalCount = items.Count };
        foreach (var a in items) response.Assignments.Add(MapAssignment(a, crewNames));
        return response;
    }

    public override async Task<CrewAssignmentResponse> CreateCrewAssignment(CreateCrewAssignmentRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var assignment = CrewAssignment.Create(request.CrewCtrlNbr, request.AssignmentCtrlNbr, request.DaysOfWeekMask, startUtc, endUtc);

        await using var uow = await uowFactory.CreateAsync();
        uow.CrewAssignments.Add(assignment);
        await uow.CommitAsync();

        return MapAssignment(assignment);
    }

    public override async Task<CrewAssignmentResponse> UpdateCrewAssignment(UpdateCrewAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"CrewAssignment {request.CtrlNbr} not found."));
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        assignment.Update(request.DaysOfWeekMask, startUtc, endUtc);

        await using var uow = await uowFactory.CreateAsync();
        uow.CrewAssignments.Update(assignment);
        await uow.CommitAsync();

        return MapAssignment(assignment);
    }

    public override async Task<DeleteResponse> DeleteCrewAssignment(DeleteCrewAssignmentRequest request, ServerCallContext context)
    {
        var assignment = await assignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"CrewAssignment {request.CtrlNbr} not found."));

        await using var uow = await uowFactory.CreateAsync();
        uow.CrewAssignments.Remove(assignment);
        await uow.CommitAsync();

        return new DeleteResponse { Success = true };
    }

    private static CrewAssignmentResponse MapAssignment(CrewAssignment a, Dictionary<long, string>? crewNames = null) => new()
    {
        CtrlNbr = a.CtrlNbr.Value,
        CrewCtrlNbr = a.CrewCtrlNbr.Value,
        AssignmentCtrlNbr = a.AssignmentCtrlNbr.Value,
        DaysOfWeekMask = a.DaysOfWeekMask,
        StartUtc = a.StartUtc.ToString("O"),
        EndUtc = a.EndUtc?.ToString("O") ?? string.Empty,
        CrewName = crewNames is not null && crewNames.TryGetValue(a.CrewCtrlNbr.Value, out var name) ? name : string.Empty
    };

    // ── Crew Setup Wizard ──

    public override async Task<CrewSetupWizardResponse> CrewSetupWizard(CrewSetupWizardRequest request, ServerCallContext context)
    {
        if (request.Assignments.Count == 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "At least one assignment entry is required."));

        await using var uow = await uowFactory.CreateAsync();

        // ── Step 1: Crew ──
        Crew crew;
        if (request.ExistingCrewCtrlNbr > 0)
        {
            crew = await crewRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.ExistingCrewCtrlNbr))
                ?? throw new RpcException(new Status(StatusCode.NotFound, $"Crew {request.ExistingCrewCtrlNbr} not found."));

            // Update lifecycle dates and crew type if they changed
            var newEffective = !string.IsNullOrWhiteSpace(request.EffectiveDate)
                ? DateTime.Parse(request.EffectiveDate).ToUniversalTime()
                : crew.EffectiveDate;
            var newAbolished = !string.IsNullOrWhiteSpace(request.AbolishedDate)
                ? DateTime.Parse(request.AbolishedDate).ToUniversalTime()
                : (DateTime?)null;
            var newCrewType = !string.IsNullOrWhiteSpace(request.CrewType) ? request.CrewType : null;

            if (crew.EffectiveDate != newEffective || crew.AbolishedDate != newAbolished || (newCrewType is not null && crew.CrewType != newCrewType))
            {
                crew.Update(crew.Name, crew.IsActive, crew.DepartmentCtrlNbr, newEffective, newAbolished, newCrewType);
                uow.Crews.Update(crew);
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.CrewName))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Crew name is required when creating a new crew."));

            var deptCtrlNbr = request.CrewDepartmentCtrlNbr > 0 ? ControlNumber.Create(request.CrewDepartmentCtrlNbr) : null;
            var effectiveDate = !string.IsNullOrWhiteSpace(request.EffectiveDate)
                ? DateTime.Parse(request.EffectiveDate).ToUniversalTime()
                : (DateTime?)null;
            var abolishedDate = !string.IsNullOrWhiteSpace(request.AbolishedDate)
                ? DateTime.Parse(request.AbolishedDate).ToUniversalTime()
                : (DateTime?)null;
            crew = Crew.Create(
                request.CrewType,
                request.WorkAreaCtrlNbr,
                request.CrewName,
                isActive: true,
                departmentCtrlNbr: deptCtrlNbr,
                effectiveDate: effectiveDate,
                abolishedDate: abolishedDate);
            uow.Crews.Add(crew);
        }

        int assignmentsCreated = 0, assignmentsUpdated = 0, schedulesCreated = 0, schedulesUpdated = 0, crewAssignmentsCreated = 0, crewAssignmentsUpdated = 0, crewAssignmentsDeleted = 0, positionsCreated = 0, positionsDeleted = 0;
        int positionsExisting = 0, schedulesExisting = 0, crewAssignmentsExisting = 0;
        var consumedCrewAssignmentKeys = new HashSet<long>();

        // Load existing crew-assignment links for update/skip logic
        var existingCrewAssignmentMap = request.ExistingCrewCtrlNbr > 0
            ? (await assignmentRepository.GetByCrewAsync(crew.CtrlNbr))
                .ToDictionary(ca => ca.AssignmentCtrlNbr.Value)
            : new Dictionary<long, CrewAssignment>();

        // Load existing crew positions to avoid re-creating ones already in the DB.
        // Multiple positions with the same craft role are allowed (e.g. 2 Engineers),
        // so we use count-based matching: consume one existing match per requested entry.
        var unmatchedPositions = request.ExistingCrewCtrlNbr > 0
            ? (await crewPositionRepository.GetByCrewAsync(crew.CtrlNbr)).ToList()
            : new List<CrewPosition>();

        // ── Positions ──
        foreach (var pos in request.Positions)
        {
            if (pos.CraftRoleCtrlNbr <= 0) continue;
            var craftRoleCtrlNbr = ControlNumber.Create(pos.CraftRoleCtrlNbr);

            // Try to match against an existing position (same role + order); consume it if found
            var match = unmatchedPositions.FindIndex(ep => ep.CraftRoleCtrlNbr == craftRoleCtrlNbr && ep.DisplayOrder == pos.DisplayOrder);
            if (match >= 0)
            {
                unmatchedPositions.RemoveAt(match);
                positionsExisting++;
                continue;
            }

            var staffablePosition = StaffablePosition.Create("Crew");
            var position = CrewPosition.Create(crew.CtrlNbr, craftRoleCtrlNbr, pos.DisplayOrder, staffablePosition.CtrlNbr);
            uow.StaffablePositions.Add(staffablePosition);
            uow.CrewPositions.Add(position);
            positionsCreated++;
        }

        // Delete positions that were removed from the wizard
        foreach (var removed in unmatchedPositions)
        {
            uow.CrewPositions.Remove(removed);
            positionsDeleted++;
        }

        // ── Step 2: Assignments + Schedules + CrewAssignments ──
        foreach (var entry in request.Assignments)
        {
            Assignment assignment;
            if (entry.ExistingAssignmentCtrlNbr > 0)
            {
                assignment = await staffingAssignmentRepository.GetByCtrlNbrAsync(ControlNumber.Create(entry.ExistingAssignmentCtrlNbr))
                        ?? throw new RpcException(new Status(StatusCode.NotFound, $"Assignment {entry.ExistingAssignmentCtrlNbr} not found."));

                    // Apply any edits from the wizard to the existing assignment
                    var deptCtrlNbr = entry.DepartmentCtrlNbr > 0 ? ControlNumber.Create(entry.DepartmentCtrlNbr) : null;
                    assignment.Update(
                        code: !string.IsNullOrWhiteSpace(entry.Code) ? entry.Code : null,
                        name: !string.IsNullOrWhiteSpace(entry.Name) ? entry.Name : null,
                        isExtra: entry.IsExtra,
                        isActive: true,
                        departmentCtrlNbr: deptCtrlNbr,
                        groupCtrlNbr: entry.GroupCtrlNbr > 0 ? ControlNumber.Create(entry.GroupCtrlNbr) : null);
                    uow.Assignments.Update(assignment);
                    assignmentsUpdated++;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(entry.Code) || string.IsNullOrWhiteSpace(entry.Name))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Assignment code and name are required for new assignments."));

                var deptCtrlNbr = entry.DepartmentCtrlNbr > 0 ? ControlNumber.Create(entry.DepartmentCtrlNbr) : null;
                assignment = Assignment.Create(
                    ControlNumber.Create(entry.GroupCtrlNbr),
                    entry.Code,
                    entry.Name,
                    isExtra: entry.IsExtra,
                    isActive: true,
                    departmentCtrlNbr: deptCtrlNbr);
                uow.Assignments.Add(assignment);
                assignmentsCreated++;
            }

            // Create or update schedule
            if (entry.ShiftDefinitionCtrlNbr > 0 && !string.IsNullOrWhiteSpace(entry.OnDutyTime))
            {
                var onDuty = TimeOnly.Parse(entry.OnDutyTime);
                var offDuty = !string.IsNullOrWhiteSpace(entry.OffDutyTime) ? TimeOnly.Parse(entry.OffDutyTime) : onDuty.AddHours(8);
                var shiftCtrlNbr = ControlNumber.Create(entry.ShiftDefinitionCtrlNbr);

                if (entry.ExistingAssignmentCtrlNbr > 0)
                {
                    var existingSchedules = await assignmentScheduleRepository.GetByAssignmentAsync(assignment.CtrlNbr);
                    var existingSchedule = existingSchedules.FirstOrDefault();

                    if (existingSchedule is not null)
                    {
                        if (existingSchedule.ShiftDefinitionCtrlNbr != shiftCtrlNbr)
                        {
                            uow.AssignmentSchedules.Remove(existingSchedule);
                            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(
                                assignment.CtrlNbr, shiftCtrlNbr, entry.AssignmentOperatingDaysMask, onDuty, offDuty));
                        }
                        else
                        {
                            existingSchedule.Update(entry.AssignmentOperatingDaysMask, onDuty, offDuty);
                            uow.AssignmentSchedules.Update(existingSchedule);
                        }
                        schedulesUpdated++;
                    }
                    else
                    {
                        uow.AssignmentSchedules.Add(AssignmentSchedule.Create(
                            assignment.CtrlNbr, shiftCtrlNbr, entry.AssignmentOperatingDaysMask, onDuty, offDuty));
                        schedulesCreated++;
                    }
                }
                else
                {
                    uow.AssignmentSchedules.Add(AssignmentSchedule.Create(
                        assignment.CtrlNbr, shiftCtrlNbr, entry.AssignmentOperatingDaysMask, onDuty, offDuty));
                    schedulesCreated++;
                }
            }

            // Create or update crew assignment
            if (existingCrewAssignmentMap.TryGetValue(assignment.CtrlNbr.Value, out var existingCa))
            {
                consumedCrewAssignmentKeys.Add(assignment.CtrlNbr.Value);
                var startUtc = !string.IsNullOrWhiteSpace(entry.StartDate)
                    ? DateTime.Parse(entry.StartDate).ToUniversalTime()
                    : existingCa.StartUtc;
                DateTime? endUtc = !string.IsNullOrWhiteSpace(entry.EndDate)
                    ? DateTime.Parse(entry.EndDate).ToUniversalTime()
                    : null;

                if (existingCa.DaysOfWeekMask != entry.CrewWorkDaysMask || existingCa.StartUtc != startUtc || existingCa.EndUtc != endUtc)
                {
                    existingCa.Update(entry.CrewWorkDaysMask, startUtc, endUtc);
                    uow.CrewAssignments.Update(existingCa);
                    crewAssignmentsUpdated++;
                }
                else
                {
                    crewAssignmentsExisting++;
                }
            }
            else
            {
                var startUtc = !string.IsNullOrWhiteSpace(entry.StartDate)
                    ? DateTime.Parse(entry.StartDate).ToUniversalTime()
                    : DateTime.UtcNow;
                DateTime? endUtc = !string.IsNullOrWhiteSpace(entry.EndDate)
                    ? DateTime.Parse(entry.EndDate).ToUniversalTime()
                    : null;

                var crewAssignment = CrewAssignment.Create(
                    crew.CtrlNbr,
                    assignment.CtrlNbr,
                    entry.CrewWorkDaysMask,
                    startUtc,
                    endUtc);
                uow.CrewAssignments.Add(crewAssignment);
                crewAssignmentsCreated++;
            }
        }

        // Delete crew assignments that were removed from the wizard
        foreach (var (key, removedCa) in existingCrewAssignmentMap)
        {
            if (!consumedCrewAssignmentKeys.Contains(key))
            {
                uow.CrewAssignments.Remove(removedCa);
                crewAssignmentsDeleted++;
            }
        }

        await uow.CommitAsync();

        return new CrewSetupWizardResponse
        {
            CrewCtrlNbr = crew.CtrlNbr.Value,
            CrewName = crew.Name,
            AssignmentsCreated = assignmentsCreated,
            AssignmentsUpdated = assignmentsUpdated,
            SchedulesCreated = schedulesCreated,
            CrewAssignmentsCreated = crewAssignmentsCreated,
            PositionsCreated = positionsCreated,
            PositionsExisting = positionsExisting,
            PositionsDeleted = positionsDeleted,
            SchedulesUpdated = schedulesUpdated,
            SchedulesExisting = schedulesExisting,
            CrewAssignmentsExisting = crewAssignmentsExisting,
            CrewAssignmentsUpdated = crewAssignmentsUpdated,
            CrewAssignmentsDeleted = crewAssignmentsDeleted,
            IsExistingCrew = request.ExistingCrewCtrlNbr > 0
        };
    }
}