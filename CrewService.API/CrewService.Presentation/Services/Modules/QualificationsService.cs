using CrewService.Application.Qualifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public sealed class QualificationsService(IServiceProvider serviceProvider) : QualificationsSrvc.QualificationsSrvcBase
{
    public override async Task<QualificationTypeResponse> CreateQualificationType(
        CreateQualificationTypeRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        var qualificationType = await svc.CreateQualificationTypeAsync(
            parentCtrlNbr: ControlNumber.Create(request.ParentCtrlNbr),
            code: request.Code,
            name: request.Name,
            evaluationStrategy: string.IsNullOrWhiteSpace(request.EvaluationStrategy) ? EvaluationStrategies.Manual : request.EvaluationStrategy,
            scopeGroupCtrlNbr: request.ScopeGroupCtrlNbr > 0 ? ControlNumber.Create(request.ScopeGroupCtrlNbr) : null,
            craftCtrlNbr: request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null,
            regulatoryQualificationCtrlNbr: request.RegulatoryQualificationCtrlNbr > 0 ? ControlNumber.Create(request.RegulatoryQualificationCtrlNbr) : null,
            description: string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            expirationMonths: request.ExpirationMonths > 0 ? request.ExpirationMonths : null,
            calendarYearExpiry: request.CalendarYearExpiry,
            graceDays: request.GraceDays,
            renewalLeadDays: request.RenewalLeadDays,
            isBlocking: request.IsBlocking,
            ct: context.CancellationToken);
        return MapQualificationType(qualificationType);
    }

    public override async Task<QualificationTypeResponse> UpdateQualificationType(
        UpdateQualificationTypeRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        try
        {
            var qualificationType = await svc.UpdateQualificationTypeAsync(
                ctrlNbr: ControlNumber.Create(request.QualificationTypeCtrlNbr),
                name: request.Name,
                description: string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
                evaluationStrategy: string.IsNullOrWhiteSpace(request.EvaluationStrategy) ? string.Empty : request.EvaluationStrategy,
                scopeGroupCtrlNbr: request.ScopeGroupCtrlNbr > 0 ? ControlNumber.Create(request.ScopeGroupCtrlNbr) : null,
                craftCtrlNbr: request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null,
                expirationMonths: request.ExpirationMonths > 0 ? request.ExpirationMonths : null,
                calendarYearExpiry: request.CalendarYearExpiry,
                graceDays: request.GraceDays,
                renewalLeadDays: request.RenewalLeadDays,
                isBlocking: request.IsBlocking,
                restrictionLabel: string.IsNullOrWhiteSpace(request.RestrictionLabel) ? null : request.RestrictionLabel,
                ct: context.CancellationToken);
            return MapQualificationType(qualificationType);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
    }

    public override async Task<DeleteResponse> DeleteQualificationType(
        DeleteQualificationTypeRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        try
        {
            await svc.DeleteQualificationTypeAsync(ControlNumber.Create(request.QualificationTypeCtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
    }

    public override async Task<QualificationTypeResponse> SetQualificationTypeActive(
        SetQualificationTypeActiveRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        try
        {
            var qualificationType = await svc.SetQualificationTypeActiveAsync(
                ControlNumber.Create(request.QualificationTypeCtrlNbr), request.IsActive, context.CancellationToken);
            return MapQualificationType(qualificationType);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
    }

    public override async Task<GetQualificationRequirementsResponse> GetQualificationRequirements(
        GetQualificationRequirementsRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        var requirements = await svc.GetQualificationRequirementsAsync(
            ControlNumber.Create(request.QualificationTypeCtrlNbr), context.CancellationToken);
        var response = new GetQualificationRequirementsResponse();
        response.Requirements.AddRange(requirements.Select(MapRequirement));
        return response;
    }

    public override async Task<QualificationRequirementResponse> AddQualificationRequirement(
        AddQualificationRequirementRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        try
        {
            var requirement = await svc.AddQualificationRequirementAsync(
                qualificationTypeCtrlNbr: ControlNumber.Create(request.QualificationTypeCtrlNbr),
                requirementKind: request.RequirementKind,
                threshold: request.Threshold,
                thresholdUnit: request.ThresholdUnit,
                description: request.Description ?? string.Empty,
                eventSource: string.IsNullOrWhiteSpace(request.EventSource) ? null : request.EventSource,
                activityFilter: string.IsNullOrWhiteSpace(request.ActivityFilter) ? null : request.ActivityFilter,
                requiredQualTypeCtrlNbr: request.RequiredQualTypeCtrlNbr > 0 ? ControlNumber.Create(request.RequiredQualTypeCtrlNbr) : null,
                requiredRegulatoryQualCtrlNbr: request.RequiredRegulatoryQualCtrlNbr > 0 ? ControlNumber.Create(request.RequiredRegulatoryQualCtrlNbr) : null,
                ct: context.CancellationToken);
            return MapRequirement(requirement);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<QualificationRequirementResponse> UpdateQualificationRequirement(
        UpdateQualificationRequirementRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        try
        {
            var requirement = await svc.UpdateQualificationRequirementAsync(
                reqCtrlNbr: ControlNumber.Create(request.RequirementCtrlNbr),
                threshold: request.Threshold,
                thresholdUnit: request.ThresholdUnit ?? string.Empty,
                description: request.Description ?? string.Empty,
                eventSource: string.IsNullOrWhiteSpace(request.EventSource) ? null : request.EventSource,
                activityFilter: string.IsNullOrWhiteSpace(request.ActivityFilter) ? null : request.ActivityFilter,
                requiredQualTypeCtrlNbr: request.RequiredQualTypeCtrlNbr > 0 ? ControlNumber.Create(request.RequiredQualTypeCtrlNbr) : null,
                requiredRegulatoryQualCtrlNbr: request.RequiredRegulatoryQualCtrlNbr > 0 ? ControlNumber.Create(request.RequiredRegulatoryQualCtrlNbr) : null,
                ct: context.CancellationToken);
            return MapRequirement(requirement);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<DeleteResponse> RemoveQualificationRequirement(
        RemoveQualificationRequirementRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        try
        {
            await svc.RemoveQualificationRequirementAsync(ControlNumber.Create(request.RequirementCtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetQualificationTypesResponse> GetQualificationTypes(
        GetQualificationTypesRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        var types = await svc.GetQualificationTypesAsync(
            ControlNumber.Create(request.ParentCtrlNbr), request.ActiveOnly, context.CancellationToken);
        var response = new GetQualificationTypesResponse();
        response.QualificationTypes.AddRange(types.Select(MapQualificationType));
        return response;
    }

    public override async Task<GetEmployeeQualificationsResponse> GetEmployeeQualifications(
        GetEmployeeQualificationsRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        var qualifications = await svc.GetEmployeeQualificationsAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        var response = new GetEmployeeQualificationsResponse();
        response.Qualifications.AddRange(qualifications.Select(MapEmployeeQualification));
        return response;
    }

    public override async Task<GetRegulatoryQualificationsResponse> GetRegulatoryQualifications(
        GetRegulatoryQualificationsRequest request,
        ServerCallContext context)
    {
        var regulatoryQualificationCatalog = serviceProvider.GetRequiredService<IRegulatoryQualificationCatalog>();
        var catalog = await regulatoryQualificationCatalog.GetAllAsync(context.CancellationToken);
        var response = new GetRegulatoryQualificationsResponse();
        response.Qualifications.AddRange(catalog.Select(r => new RegulatoryQualificationCatalogItem
        {
            CtrlNbr = r.CtrlNbr.Value,
            Code = r.Code,
            CfrPart = r.CfrPart,
            Description = r.Description
        }));

        return response;
    }

    public override async Task<EmployeeQualificationResponse> GrantEmployeeQualification(
        GrantEmployeeQualificationRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
        var grantedBy = string.IsNullOrWhiteSpace(request.GrantedBy) ? SystemActors.System : request.GrantedBy;
        DateTime? expiresAtUtc = request.ExpiresAtUtc is not null ? request.ExpiresAtUtc.ToDateTime() : null;
        try
        {
            var result = await svc.GrantEmployeeQualificationAsync(
                ControlNumber.Create(request.EmployeeCtrlNbr),
                ControlNumber.Create(request.QualificationTypeCtrlNbr),
                grantedBy, expiresAtUtc,
                string.IsNullOrWhiteSpace(request.EvidenceValue) ? null : request.EvidenceValue,
                context.CancellationToken);
            return MapEmployeeQualification(result);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
    }

    public override async Task<EmployeeQualificationResponse> RevokeEmployeeQualification(
        RevokeEmployeeQualificationRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        try
        {
            var qualification = await svc.RevokeEmployeeQualificationAsync(
                ControlNumber.Create(request.EmployeeQualificationCtrlNbr), request.Reason, context.CancellationToken);
            return MapEmployeeQualification(qualification);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<CheckEligibilityResponse> CheckEligibility(
        CheckEligibilityRequest request,
        ServerCallContext context)
    {
        var employeeEligibilityService = serviceProvider.GetRequiredService<EmployeeEligibilityService>();
        var result = await employeeEligibilityService.CheckEligibilityAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.PositionSlotCtrlNbr),
            context.CancellationToken);

        var response = new CheckEligibilityResponse
        {
            IsEligible = result.IsEligible,
        };

        response.BlockingReasons.AddRange(result.BlockingReasons.Select(r => new EligibilityBlockingReason
        {
            RuleCode = r.RuleCode,
            Description = r.Description
        }));

        return response;
    }

    public override async Task<GetEligibleEmployeesForCraftRoleResponse> GetEligibleEmployeesForCraftRole(
        GetEligibleEmployeesForCraftRoleRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Qualifications.QualificationsService>();
        var employeeNameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();

        var (employees, _, requiredQuals, qualsByEmployee) = await svc.GetEligibleEmployeesDataAsync(
            ControlNumber.Create(request.CraftRoleCtrlNbr),
            ControlNumber.Create(request.ClientCtrlNbr),
            context.CancellationToken);

        var nameMap = await employeeNameSvc.GetFullNameLnfBatchAsync(employees.Select(e => e.UserId));
        var requiredTypeCtrlNbrs = requiredQuals.Select(q => q.QualificationTypeCtrlNbr).ToHashSet();

        IEnumerable<EligibleEmployeeItem> eligible;
        if (requiredQuals.Count == 0)
        {
            eligible = employees.Select(e => new EligibleEmployeeItem
            {
                CtrlNbr = e.CtrlNbr.Value,
                EmployeeNumber = e.EmployeeNumber,
                FullNameLnf = nameMap.GetValueOrDefault(e.UserId ?? "", string.Empty)
            });
        }
        else
        {
            eligible = employees
                .Where(e =>
                {
                    var held = qualsByEmployee.GetValueOrDefault(e.CtrlNbr, []);
                    return requiredTypeCtrlNbrs.IsSubsetOf(held);
                })
                .Select(e => new EligibleEmployeeItem
                {
                    CtrlNbr = e.CtrlNbr.Value,
                    EmployeeNumber = e.EmployeeNumber,
                    FullNameLnf = nameMap.GetValueOrDefault(e.UserId ?? "", string.Empty)
                });
        }

        var sorted = eligible.OrderBy(e => e.FullNameLnf, StringComparer.OrdinalIgnoreCase).ToList();
        var response = new GetEligibleEmployeesForCraftRoleResponse();
        response.Employees.AddRange(sorted);
        return response;
    }

    private static QualificationTypeResponse MapQualificationType(QualificationType qualificationType)
    {
        var response = new QualificationTypeResponse
        {
            CtrlNbr = qualificationType.CtrlNbr.Value,
            ParentCtrlNbr = qualificationType.ParentCtrlNbr.Value,
            Code = qualificationType.Code,
            Name = qualificationType.Name,
            Description = qualificationType.Description ?? string.Empty,
            EvaluationStrategy = qualificationType.EvaluationStrategy,
            CalendarYearExpiry = qualificationType.CalendarYearExpiry,
            GraceDays = qualificationType.GraceDays,
            RenewalLeadDays = qualificationType.RenewalLeadDays,
            IsBlocking = qualificationType.IsBlocking,
            IsActive = qualificationType.IsActive,
            IsSystemSeeded = qualificationType.IsSystemSeeded,
        };

        if (qualificationType.ScopeGroupCtrlNbr is not null)
            response.ScopeGroupCtrlNbr = qualificationType.ScopeGroupCtrlNbr.Value;

        if (qualificationType.CraftCtrlNbr is not null)
            response.CraftCtrlNbr = qualificationType.CraftCtrlNbr.Value;

        if (qualificationType.RegulatoryQualificationCtrlNbr is not null)
            response.RegulatoryQualificationCtrlNbr = qualificationType.RegulatoryQualificationCtrlNbr.Value;

        if (qualificationType.ExpirationMonths.HasValue)
            response.ExpirationMonths = qualificationType.ExpirationMonths.Value;

        if (qualificationType.RestrictionLabel is not null)
            response.RestrictionLabel = qualificationType.RestrictionLabel;

        return response;
    }

    private static EmployeeQualificationResponse MapEmployeeQualification(EmployeeQualification qualification)
    {
        var response = new EmployeeQualificationResponse
        {
            CtrlNbr = qualification.CtrlNbr.Value,
            EmployeeCtrlNbr = qualification.EmployeeCtrlNbr.Value,
            QualificationTypeCtrlNbr = qualification.QualificationTypeCtrlNbr.Value,
            AchievedAtUtc = qualification.AchievedAtUtc.HasValue ? Timestamp.FromDateTime(DateTime.SpecifyKind(qualification.AchievedAtUtc.Value, DateTimeKind.Utc)) : null,
            Status = qualification.Status,
            GrantedBy = qualification.GrantedBy,
            RevocationReason = qualification.RevocationReason ?? string.Empty,
        };

        if (qualification.ExpiresAtUtc.HasValue)
        {
            response.ExpiresAtUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(qualification.ExpiresAtUtc.Value, DateTimeKind.Utc));
        }

        if (qualification.RevokedAtUtc.HasValue)
        {
            response.RevokedAtUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(qualification.RevokedAtUtc.Value, DateTimeKind.Utc));
        }

        return response;
    }

    private static QualificationRequirementResponse MapRequirement(QualificationRequirement requirement)
    {
        var response = new QualificationRequirementResponse { RequirementKind = requirement.RequirementKind,
            Threshold = requirement.Threshold,
            ThresholdUnit = requirement.ThresholdUnit,
            Description = requirement.Description,
            EventSource = requirement.EventSource ?? string.Empty,
            ActivityFilter = requirement.ActivityFilter ?? string.Empty,
        };

        if (requirement.RequiredQualTypeCtrlNbr is not null)
            response.RequiredQualTypeCtrlNbr = requirement.RequiredQualTypeCtrlNbr.Value;

        if (requirement.RequiredRegulatoryQualCtrlNbr is not null)
            response.RequiredRegulatoryQualCtrlNbr = requirement.RequiredRegulatoryQualCtrlNbr.Value;

        return response;
    }
}
