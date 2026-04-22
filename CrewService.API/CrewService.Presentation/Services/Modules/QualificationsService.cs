using CrewService.Application.Qualifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public sealed class QualificationsService(
    IQualificationTypeRepository qualificationTypeRepository,
    IQualificationRequirementRepository qualificationRequirementRepository,
    IEmployeeQualificationRepository employeeQualificationRepository,
    EmployeeEligibilityService employeeEligibilityService,
    IRegulatoryQualificationCatalog regulatoryQualificationCatalog,
    IOrchestrationUnitOfWorkFactory uowFactory)
    : QualificationsSrvc.QualificationsSrvcBase
{
    public override async Task<QualificationTypeResponse> CreateQualificationType(
        CreateQualificationTypeRequest request,
        ServerCallContext context)
    {
        var qualificationType = QualificationType.Create(
            parentCtrlNbr: ControlNumber.Create(request.ParentCtrlNbr),
            code: request.Code,
            name: request.Name,
            evaluationStrategy: string.IsNullOrWhiteSpace(request.EvaluationStrategy) ? EvaluationStrategies.Manual : request.EvaluationStrategy,
            scopeGroupCtrlNbr: request.ScopeGroupCtrlNbr > 0 ? ControlNumber.Create(request.ScopeGroupCtrlNbr) : null,
            craftCtrlNbr: request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null,
            regulatoryQualificationCtrlNbr: request.RegulatoryQualificationCtrlNbr > 0
                ? ControlNumber.Create(request.RegulatoryQualificationCtrlNbr)
                : null,
            description: string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            expirationMonths: request.ExpirationMonths > 0 ? request.ExpirationMonths : null,
            calendarYearExpiry: request.CalendarYearExpiry,
            graceDays: request.GraceDays,
            renewalLeadDays: request.RenewalLeadDays,
            isBlocking: request.IsBlocking);

        await using var uow = await uowFactory.CreateAsync();
        uow.QualificationTypes.Add(qualificationType);
        await uow.CommitAsync(context.CancellationToken);
        return MapQualificationType(qualificationType);
    }

    public override async Task<QualificationTypeResponse> UpdateQualificationType(
        UpdateQualificationTypeRequest request,
        ServerCallContext context)
    {
        await using var uow = await uowFactory.CreateAsync();
        var qualificationType = await uow.QualificationTypes
            .GetByCtrlNbrAsync(ControlNumber.Create(request.QualificationTypeCtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Qualification type not found"));

        if (qualificationType.IsSystemSeeded || qualificationType.EvaluationStrategy == EvaluationStrategies.FraCertification)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "FRA-managed qualification types cannot be modified from this menu"));

        qualificationType.Update(
            name: request.Name,
            description: string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            evaluationStrategy: string.IsNullOrWhiteSpace(request.EvaluationStrategy) ? qualificationType.EvaluationStrategy : request.EvaluationStrategy,
            scopeGroupCtrlNbr: request.ScopeGroupCtrlNbr > 0 ? ControlNumber.Create(request.ScopeGroupCtrlNbr) : null,
            craftCtrlNbr: request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null,
            expirationMonths: request.ExpirationMonths > 0 ? request.ExpirationMonths : null,
            calendarYearExpiry: request.CalendarYearExpiry,
            graceDays: request.GraceDays,
            renewalLeadDays: request.RenewalLeadDays,
            isBlocking: request.IsBlocking);

        uow.QualificationTypes.Update(qualificationType);
        await uow.CommitAsync(context.CancellationToken);
        return MapQualificationType(qualificationType);
    }

    public override async Task<DeleteResponse> DeleteQualificationType(
        DeleteQualificationTypeRequest request,
        ServerCallContext context)
    {
        await using var uow = await uowFactory.CreateAsync();
        var ctrlNbr = ControlNumber.Create(request.QualificationTypeCtrlNbr);
        var qualificationType = await uow.QualificationTypes.GetByCtrlNbrAsync(ctrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Qualification type not found"));

        if (qualificationType.IsSystemSeeded || qualificationType.EvaluationStrategy == EvaluationStrategies.FraCertification)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "FRA-managed qualification types cannot be deleted"));

        uow.QualificationTypes.Remove(qualificationType);
        await uow.CommitAsync(context.CancellationToken);
        return new DeleteResponse { Success = true };
    }

    public override async Task<QualificationTypeResponse> SetQualificationTypeActive(
        SetQualificationTypeActiveRequest request,
        ServerCallContext context)
    {
        await using var uow = await uowFactory.CreateAsync();
        var qualificationType = await uow.QualificationTypes
            .GetByCtrlNbrAsync(ControlNumber.Create(request.QualificationTypeCtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Qualification type not found"));

        if (qualificationType.IsSystemSeeded || qualificationType.EvaluationStrategy == EvaluationStrategies.FraCertification)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "FRA-managed qualification types cannot be modified from this menu"));

        if (request.IsActive)
            qualificationType.Activate();
        else
            qualificationType.Deactivate();

        uow.QualificationTypes.Update(qualificationType);
        await uow.CommitAsync(context.CancellationToken);
        return MapQualificationType(qualificationType);
    }

    public override async Task<GetQualificationRequirementsResponse> GetQualificationRequirements(
        GetQualificationRequirementsRequest request,
        ServerCallContext context)
    {
        var requirements = await qualificationRequirementRepository
            .GetByQualificationTypeCtrlNbrAsync(ControlNumber.Create(request.QualificationTypeCtrlNbr));

        var response = new GetQualificationRequirementsResponse();
        response.Requirements.AddRange(requirements.Select(MapRequirement));
        return response;
    }

    public override async Task<QualificationRequirementResponse> AddQualificationRequirement(
        AddQualificationRequirementRequest request,
        ServerCallContext context)
    {
        await using var uow = await uowFactory.CreateAsync();
        var qualificationType = await uow.QualificationTypes
            .GetByCtrlNbrAsync(ControlNumber.Create(request.QualificationTypeCtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Qualification type not found"));

        var requirement = qualificationType.AddRequirement(
            requirementKind: request.RequirementKind,
            threshold: request.Threshold,
            thresholdUnit: request.ThresholdUnit,
            description: request.Description ?? string.Empty,
            eventSource: string.IsNullOrWhiteSpace(request.EventSource) ? null : request.EventSource,
            activityFilter: string.IsNullOrWhiteSpace(request.ActivityFilter) ? null : request.ActivityFilter,
            requiredQualTypeCtrlNbr: request.RequiredQualTypeCtrlNbr > 0
                ? ControlNumber.Create(request.RequiredQualTypeCtrlNbr)
                : null,
            requiredRegulatoryQualCtrlNbr: request.RequiredRegulatoryQualCtrlNbr > 0
                ? ControlNumber.Create(request.RequiredRegulatoryQualCtrlNbr)
                : null);

        uow.QualificationTypes.Update(qualificationType);
        await uow.CommitAsync(context.CancellationToken);
        return MapRequirement(requirement);
    }

    public override async Task<QualificationRequirementResponse> UpdateQualificationRequirement(
        UpdateQualificationRequirementRequest request,
        ServerCallContext context)
    {
        await using var uow = await uowFactory.CreateAsync();
        var reqCtrlNbr = ControlNumber.Create(request.RequirementCtrlNbr);
        var requirement = await uow.QualificationRequirements.GetByCtrlNbrAsync(reqCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "requirement not found"));

        requirement.Update(
            threshold: request.Threshold,
            thresholdUnit: request.ThresholdUnit ?? string.Empty,
            description: request.Description ?? string.Empty,
            eventSource: string.IsNullOrWhiteSpace(request.EventSource) ? null : request.EventSource,
            activityFilter: string.IsNullOrWhiteSpace(request.ActivityFilter) ? null : request.ActivityFilter,
            requiredQualTypeCtrlNbr: request.RequiredQualTypeCtrlNbr > 0
                ? ControlNumber.Create(request.RequiredQualTypeCtrlNbr)
                : null,
            requiredRegulatoryQualCtrlNbr: request.RequiredRegulatoryQualCtrlNbr > 0
                ? ControlNumber.Create(request.RequiredRegulatoryQualCtrlNbr)
                : null);

        uow.QualificationRequirements.Update(requirement);
        await uow.CommitAsync(context.CancellationToken);
        return MapRequirement(requirement);
    }

    public override async Task<DeleteResponse> RemoveQualificationRequirement(
        RemoveQualificationRequirementRequest request,
        ServerCallContext context)
    {
        await using var uow = await uowFactory.CreateAsync();
        var reqCtrlNbr = ControlNumber.Create(request.RequirementCtrlNbr);
        var requirement = await uow.QualificationRequirements.GetByCtrlNbrAsync(reqCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "requirement not found"));

        var qualificationType = await uow.QualificationTypes
            .GetByCtrlNbrAsync(requirement.QualificationTypeCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Qualification type not found"));

        qualificationType.RemoveRequirement(reqCtrlNbr);
        uow.QualificationRequirements.Remove(requirement);
        uow.QualificationTypes.Update(qualificationType);
        await uow.CommitAsync(context.CancellationToken);
        return new DeleteResponse { Success = true };
    }

    public override async Task<GetQualificationTypesResponse> GetQualificationTypes(
        GetQualificationTypesRequest request,
        ServerCallContext context)
    {
        var parentCtrlNbr = ControlNumber.Create(request.ParentCtrlNbr);
        var types = request.ActiveOnly
            ? await qualificationTypeRepository.GetActiveByParentCtrlNbrAsync(parentCtrlNbr)
            : await qualificationTypeRepository.GetByParentCtrlNbrAsync(parentCtrlNbr);

        var response = new GetQualificationTypesResponse();
        response.QualificationTypes.AddRange(types.Select(MapQualificationType));
        return response;
    }

    public override async Task<GetEmployeeQualificationsResponse> GetEmployeeQualifications(
        GetEmployeeQualificationsRequest request,
        ServerCallContext context)
    {
        var employeeCtrlNbr = ControlNumber.Create(request.EmployeeCtrlNbr);
        var qualifications = await employeeQualificationRepository.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);

        var response = new GetEmployeeQualificationsResponse();
        response.Qualifications.AddRange(qualifications.Select(MapEmployeeQualification));
        return response;
    }

    public override async Task<GetRegulatoryQualificationsResponse> GetRegulatoryQualifications(
        GetRegulatoryQualificationsRequest request,
        ServerCallContext context)
    {
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
        var employeeCtrlNbr = ControlNumber.Create(request.EmployeeCtrlNbr);
        var qualificationTypeCtrlNbr = ControlNumber.Create(request.QualificationTypeCtrlNbr);

        var existing = await employeeQualificationRepository
            .GetByEmployeeAndTypeAsync(employeeCtrlNbr, qualificationTypeCtrlNbr);

        var grantedBy = string.IsNullOrWhiteSpace(request.GrantedBy)
            ? SystemActors.System
            : request.GrantedBy;

        var status = string.IsNullOrWhiteSpace(request.Status)
            ? QualificationStatuses.Active
            : request.Status;

        DateTime? expiresAtUtc = null;
        if (request.ExpiresAtUtc is not null)
            expiresAtUtc = request.ExpiresAtUtc.ToDateTime();

        if (existing is null)
        {
            var created = EmployeeQualification.Create(
                employeeCtrlNbr,
                qualificationTypeCtrlNbr,
                grantedBy,
                expiresAtUtc,
                status);

            if (!string.IsNullOrWhiteSpace(request.EvidenceValue))
            {
                created.AddEvidence(
                    EvidenceTypes.ManualCompletion,
                    request.EvidenceValue,
                    grantedBy);
            }

            await employeeQualificationRepository.AddAsync(created, context.CancellationToken);
            return MapEmployeeQualification(created);
        }

        existing.Reinstate(expiresAtUtc);
        if (!string.IsNullOrWhiteSpace(request.EvidenceValue))
        {
            existing.AddEvidence(
                EvidenceTypes.ManualCompletion,
                request.EvidenceValue,
                grantedBy);
        }

        await employeeQualificationRepository.UpdateAsync(existing, context.CancellationToken);
        return MapEmployeeQualification(existing);
    }

    public override async Task<EmployeeQualificationResponse> RevokeEmployeeQualification(
        RevokeEmployeeQualificationRequest request,
        ServerCallContext context)
    {
        var qualification = await employeeQualificationRepository
            .GetByCtrlNbrAsync(ControlNumber.Create(request.EmployeeQualificationCtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Employee qualification not found"));

        qualification.Revoke(request.Reason);
        await employeeQualificationRepository.UpdateAsync(qualification, context.CancellationToken);

        return MapEmployeeQualification(qualification);
    }

    public override async Task<CheckEligibilityResponse> CheckEligibility(
        CheckEligibilityRequest request,
        ServerCallContext context)
    {
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

        return response;
    }

    private static EmployeeQualificationResponse MapEmployeeQualification(EmployeeQualification qualification)
    {
        var response = new EmployeeQualificationResponse
        {
            CtrlNbr = qualification.CtrlNbr.Value,
            EmployeeCtrlNbr = qualification.EmployeeCtrlNbr.Value,
            QualificationTypeCtrlNbr = qualification.QualificationTypeCtrlNbr.Value,
            AchievedAtUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(qualification.AchievedAtUtc, DateTimeKind.Utc)),
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
