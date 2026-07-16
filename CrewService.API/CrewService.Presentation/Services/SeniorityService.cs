using CrewService.Application.SeniorityOps;
using CrewService.Application.Time;
using CrewService.Domain.Exceptions;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace CrewService.Presentation.Services;

public class SeniorityService(
    SeniorityAppService seniorityAppService,
    EmployeeNameService employeeNameService,
    IWorkAreaClock workAreaClock,
    IServiceProvider serviceProvider) : SenioritySrvc.SenioritySrvcBase
{
    public override async Task<GetAllSeniorityResponse> GetAllAsync(GetAllSeniorityRequest request, ServerCallContext context)
    {
        var response = new GetAllSeniorityResponse();

        ControlNumber? rosterCtrlNbr = request.RosterCtrlNbr > 0
            ? ControlNumber.Create(request.RosterCtrlNbr) : null;

        ControlNumber? railroadCtrlNbr = request.RailroadCtrlNbr > 0
            ? ControlNumber.Create(request.RailroadCtrlNbr) : null;

        var items = await seniorityAppService.GetAllAsync(rosterCtrlNbr, railroadCtrlNbr, context.CancellationToken);
        if (items.Count == 0)
        {
            response.TotalCount = 0;
            return response;
        }

        // Batch-resolve names for all unique userIds
        var userIds = items
            .Where(i => !string.IsNullOrEmpty(i.EmployeeUserId))
            .Select(i => i.EmployeeUserId)
            .Distinct()
            .ToList();
        var nameMap = await employeeNameService.GetFullNameLnfBatchAsync(userIds!);

        foreach (var item in items)
        {
            var fullName = !string.IsNullOrEmpty(item.EmployeeUserId) &&
                           nameMap.TryGetValue(item.EmployeeUserId!, out var n) ? n : string.Empty;
            var sr = new SeniorityResponse
            {
                CtrlNbr = item.Seniority.CtrlNbr.Value,
                RosterCtrlNbr = item.Seniority.RosterCtrlNbr.Value,
                EmployeeCtrlNbr = item.Seniority.EmployeeCtrlNbr.Value,
                LastActiveRoster = item.Seniority.LastActiveRoster,
                RosterDate = item.Seniority.RosterDate.ToString("yyyy-MM-dd"),
                Rank = item.Seniority.Rank,
                SeniorityStateCtrlNbr = item.Seniority.SeniorityStateCtrlNbr.Value,
                CanTrain = item.Seniority.CanTrain,
                EmployeeNumber = item.EmployeeNumber,
                EmployeeUserId = item.EmployeeUserId ?? string.Empty,
                SeniorityStateName = item.SeniorityStateName,
                EmployeeFullNameLnf = fullName,
                PositionName = item.PositionName,
                PositionType = item.PositionType,
                StaffablePositionCtrlNbr = item.StaffablePositionCtrlNbr,
                CanExerciseSeniority = item.CanExerciseSeniority
            };
            sr.RestrictionLabels.AddRange(item.RestrictionLabels);
            response.Seniority.Add(sr);
        }

        response.TotalCount = response.Seniority.Count;
        return response;
    }

    public override async Task<SeniorityResponse> GetAsync(GetSeniorityRequest request, ServerCallContext context)
    {
        try
        {
            var seniority = await seniorityAppService.GetAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapToResponse(seniority);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<SeniorityResponse> CreateAsync(CreateSeniorityRequest request, ServerCallContext context)
    {
        var seniority = await seniorityAppService.CreateAsync(
            ControlNumber.Create(request.RosterCtrlNbr),
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.LastActiveRoster,
            DateTime.Parse(request.RosterDate),
            request.Rank,
            ControlNumber.Create(request.SeniorityStateCtrlNbr),
            request.CanTrain,
            context.CancellationToken);
        return MapToResponse(seniority);
    }

    public override async Task<SeniorityResponse> UpdateAsync(UpdateSeniorityRequest request, ServerCallContext context)
    {
        try
        {
            var seniority = await seniorityAppService.UpdateAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.LastActiveRoster,
                DateTime.Parse(request.RosterDate),
                request.Rank,
                ControlNumber.Create(request.SeniorityStateCtrlNbr),
                request.CanTrain,
                context.CancellationToken);
            return MapToResponse(seniority);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteAsync(DeleteSeniorityRequest request, ServerCallContext context)
    {
        try
        {
            await seniorityAppService.DeleteAsync(
                ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { $"Seniority {request.CtrlNbr} deleted." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ActiveCraftResponse> GetActiveCraftForEmployee(GetActiveCraftRequest request, ServerCallContext context)
    {
        var (found, craftCtrlNbr, craftName) = await seniorityAppService.GetActiveCraftForEmployeeAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);

        if (!found) return new ActiveCraftResponse { Found = false };
        return new ActiveCraftResponse { CraftCtrlNbr = craftCtrlNbr, CraftName = craftName, Found = true };
    }

    public override async Task<PendingStateChangeResponse> ScheduleStateChangeAsync(ScheduleStateChangeRequest request, ServerCallContext context)
    {
        var userId = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "No authenticated user."));

        if (!DateTime.TryParse(request.EffectiveDateUtc, out var effectiveDateUtc))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid effective_date_utc format. Use ISO 8601."));

        try
        {
            var pending = await seniorityAppService.ScheduleStateChangeAsync(
                ControlNumber.Create(request.SeniorityCtrlNbr),
                ControlNumber.Create(request.ToStateCtrlNbr),
                effectiveDateUtc.ToUniversalTime(),
                userId,
                context.CancellationToken);
            var tzId = await seniorityAppService.GetSeniorityWorkAreaTimeZoneIdAsync(
                ControlNumber.Create(request.SeniorityCtrlNbr), context.CancellationToken);
            return MapPendingToResponse(pending, found: true, workAreaClock.ResolveTimeZone(tzId));
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<PendingStateChangeResponse> GetPendingStateChangeAsync(GetPendingStateChangeRequest request, ServerCallContext context)
    {
        try
        {
            var (pending, tzId) = await seniorityAppService.GetPendingChangeAsync(
                ControlNumber.Create(request.SeniorityCtrlNbr), context.CancellationToken);

            return pending is null
                ? new PendingStateChangeResponse { Found = false }
                : MapPendingToResponse(pending, found: true, workAreaClock.ResolveTimeZone(tzId));
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> CancelPendingStateChangeAsync(CancelPendingStateChangeRequest request, ServerCallContext context)
    {
        var user = context.GetHttpContext().User;
        if (!user.IsInRole(Roles.SystemAdmin) && !user.IsInRole(Roles.ParentAdmin) && !user.IsInRole(Roles.RailroadAdmin))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only administrators can cancel scheduled state changes."));

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "No authenticated user."));

        try
        {
            await seniorityAppService.CancelPendingChangeAsync(
                ControlNumber.Create(request.PendingChangeCtrlNbr),
                userId,
                context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<GetAllPendingStateChangesResponse> GetAllPendingStateChangesAsync(
        GetAllPendingStateChangesRequest request, ServerCallContext context)
    {
        var user = context.GetHttpContext().User;
        if (!user.IsInRole(Roles.SystemAdmin) && !user.IsInRole(Roles.ParentAdmin) && !user.IsInRole(Roles.RailroadAdmin))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only administrators can view scheduled state changes."));

        var railroadCtrlNbr = ControlNumber.Create(request.RailroadCtrlNbr);
        var items = await seniorityAppService.GetAllPendingAsync(railroadCtrlNbr, context.CancellationToken);

        // Resolve employee display names in batch
        var employeeUserIds = items.Select(i => i.EmployeeUserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var employeeNameMap = await employeeNameService.GetFullNameLnfBatchAsync(employeeUserIds!);

        // Resolve scheduler display names in batch
        var schedulerUserIds = items.Select(i => i.Pending.ScheduledByUserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var schedulerNameMap = await employeeNameService.GetFullNameLnfBatchAsync(schedulerUserIds!);

        var response = new GetAllPendingStateChangesResponse();
        foreach (var item in items)
        {
            employeeNameMap.TryGetValue(item.EmployeeUserId, out var fullName);
            schedulerNameMap.TryGetValue(item.Pending.ScheduledByUserId, out var scheduledByName);
            var tz = workAreaClock.ResolveTimeZone(item.WorkAreaTimeZoneId);
            response.PendingChanges.Add(new PendingStateChangeListItem
            {
                CtrlNbr = item.Pending.CtrlNbr.Value,
                SeniorityCtrlNbr = item.Pending.SeniorityCtrlNbr.Value,
                EmployeeCtrlNbr = item.Pending.EmployeeCtrlNbr.Value,
                EmployeeNumber = item.EmployeeNumber,
                EmployeeFullNameLnf = fullName ?? item.EmployeeUserId,
                FromStateCtrlNbr = item.Pending.FromSeniorityStateCtrlNbr.Value,
                FromStateName = item.FromStateName,
                ToStateCtrlNbr = item.Pending.ToSeniorityStateCtrlNbr.Value,
                ToStateName = item.ToStateName,
                EffectiveDateUtc = workAreaClock.FormatLocalIso(item.Pending.EffectiveDateUtc, tz),
                ScheduledByUserId = item.Pending.ScheduledByUserId,
                ScheduledByUserName = scheduledByName ?? item.Pending.ScheduledByUserId,
                ScheduledAtUtc = workAreaClock.FormatLocalIso(item.Pending.ScheduledAtUtc, tz)
            });
        }
        return response;
    }

    public override async Task<GetNextStateChangeEventResponse> GetNextStateChangeEventAsync(
        GetNextStateChangeEventRequest request, ServerCallContext context)
    {
        var nextRunResolver = serviceProvider.GetRequiredService<Application.BackgroundWorkers.IBackgroundJobNextRunResolver>();
        var workAreaRepo = serviceProvider.GetRequiredService<CrewService.Domain.Modules.TenantConfig.IDynamicGroupRepository>();

        var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var workAreas = await workAreaRepo.GetWorkAreasAsync(railroadCtrlNbr);

        DateTime? nextUtc = null;
        string? nextTzId = null;

        foreach (var workArea in workAreas)
        {
            var nextRun = await nextRunResolver.ResolveAsync(
                "SeniorityStateChange",
                workArea.CtrlNbr,
                workArea.OwningRailroadCtrlNbr,
                context.CancellationToken);

            if (nextRun is null)
                continue;

            var candidateUtc = DateTime.SpecifyKind(nextRun.NextUtc, DateTimeKind.Utc);
            if (!nextUtc.HasValue || candidateUtc < nextUtc.Value)
            {
                nextUtc = candidateUtc;
                nextTzId = workArea.TimeZoneId;
            }
        }

        if (!nextUtc.HasValue)
            return new GetNextStateChangeEventResponse { NextEventLocal = string.Empty };

        var tz = workAreaClock.ResolveTimeZone(nextTzId);
        return new GetNextStateChangeEventResponse
        {
            NextEventLocal = workAreaClock.FormatLocalIso(nextUtc.Value, tz)
        };
    }

    private PendingStateChangeResponse MapPendingToResponse(
        Domain.Models.Seniority.PendingSeniorityStateChange pending, bool found, TimeZoneInfo? tz)
    {
        return new PendingStateChangeResponse
        {
            CtrlNbr = pending.CtrlNbr.Value,
            SeniorityCtrlNbr = pending.SeniorityCtrlNbr.Value,
            EmployeeCtrlNbr = pending.EmployeeCtrlNbr.Value,
            FromStateCtrlNbr = pending.FromSeniorityStateCtrlNbr.Value,
            ToStateCtrlNbr = pending.ToSeniorityStateCtrlNbr.Value,
            EffectiveDateUtc = workAreaClock.FormatLocalIso(pending.EffectiveDateUtc, tz),
            Status = pending.Status.ToString(),
            ScheduledByUserId = pending.ScheduledByUserId,
            ScheduledAtUtc = workAreaClock.FormatLocalIso(pending.ScheduledAtUtc, tz),
            Found = found,
            Success = true
        };
    }

    private static SeniorityResponse MapToResponse(Domain.Models.Seniority.Seniority seniority)
    {
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
}