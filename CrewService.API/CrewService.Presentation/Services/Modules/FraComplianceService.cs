using CrewService.Application.FraCompliance;
using CrewService.Presentation.Services;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class FraComplianceService(
    EmployeeNameService employeeNameService,
    IServiceProvider serviceProvider)
    : FraComplianceSrvc.FraComplianceSrvcBase
{
    public override async Task<SearchDutyToursResponse> SearchDutyTours(
        SearchDutyToursRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var criteria = new FraRecordSearchCriteria
        {
            EmployeeCtrlNbr = request.HasEmployeeCtrlNbr ? ControlNumber.Create(request.EmployeeCtrlNbr) : null,
            StartDateUtc = request.StartDate?.ToDateTime(),
            EndDateUtc = request.EndDate?.ToDateTime(),
            LocationCode = request.HasLocationCode ? request.LocationCode : null,
            RegulatoryStandardCode = request.HasRegulatoryStandardCode ? request.RegulatoryStandardCode : null,
            HasExcessService = request.HasHasExcessService ? request.HasExcessService : null,
        };
        var tours = await svc.SearchDutyToursAsync(criteria, context.CancellationToken);
        var response = new SearchDutyToursResponse();
        foreach (var tour in tours) response.DutyTours.Add(MapTour(tour));
        return response;
    }

    public override async Task<GetEmployeeCertificationsResponse> GetCertificationsByClient(
        GetCertificationsByClientRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var clientCtrlNbr = ControlNumber.Create(request.ClientCtrlNbr);
        var statuses = request.Statuses.Count > 0
            ? request.Statuses.ToList()
            : [CertificationStatuses.Pending, CertificationStatuses.Active];
        var (certifications, _) = await svc.GetCertificationsByEmployeeAsync(clientCtrlNbr, statuses, context.CancellationToken);
        var userMap = await employeeNameService.GetFullNameLnfBatchAsync(certifications.Select(c => c.UserId));

        var response = new GetEmployeeCertificationsResponse();
        response.Certifications.AddRange(certifications.Select(dto => MapCertification(dto, userMap)));
        return response;
    }

    public override async Task<CertificationRevocationResponse> StartCertificationRevocation(
        StartCertificationRevocationRequest request,
        ServerCallContext context)
    {
        var certificationRevocationService = serviceProvider.GetRequiredService<CertificationRevocationService>();
        var record = await certificationRevocationService.StartRevocationAsync(
            employeeCertificationCtrlNbr: ControlNumber.Create(request.EmployeeCertificationCtrlNbr),
            violationType: request.ViolationType,
            violationDateUtc: request.ViolationDate.ToDateTime(),
            ct: context.CancellationToken);

        return MapRevocation(record);
    }

    public override async Task<CertificationRevocationResponse> RecordRevocationNotice(
        RecordRevocationNoticeRequest request,
        ServerCallContext context)
    {
        var certificationRevocationService = serviceProvider.GetRequiredService<CertificationRevocationService>();
        var fraSvc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var ctrlNbr = ControlNumber.Create(request.RevocationRecordCtrlNbr);
        await certificationRevocationService.RecordWrittenNoticeAsync(ctrlNbr, context.CancellationToken);
        var record = await fraSvc.GetRevocationRecordAsync(ctrlNbr, context.CancellationToken);
        return MapRevocation(record);
    }

    public override async Task<CertificationRevocationResponse> ScheduleRevocationHearing(
        ScheduleRevocationHearingRequest request,
        ServerCallContext context)
    {
        var certificationRevocationService = serviceProvider.GetRequiredService<CertificationRevocationService>();
        var fraSvc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var ctrlNbr = ControlNumber.Create(request.RevocationRecordCtrlNbr);
        await certificationRevocationService.ScheduleHearingAsync(ctrlNbr, request.HearingDate.ToDateTime(), context.CancellationToken);
        var record = await fraSvc.GetRevocationRecordAsync(ctrlNbr, context.CancellationToken);
        return MapRevocation(record);
    }

    public override async Task<CertificationRevocationResponse> DecideRevocation(
        DecideRevocationRequest request,
        ServerCallContext context)
    {
        var certificationRevocationService = serviceProvider.GetRequiredService<CertificationRevocationService>();
        var fraSvc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var ctrlNbr = ControlNumber.Create(request.RevocationRecordCtrlNbr);
        await certificationRevocationService.DecideAsync(
            revocationRecordCtrlNbr: ctrlNbr,
            decision: request.Decision,
            revocationPeriodMonths: request.HasRevocationPeriodMonths ? request.RevocationPeriodMonths : null,
            ct: context.CancellationToken);

        var record = await fraSvc.GetRevocationRecordAsync(ctrlNbr, context.CancellationToken);
        return MapRevocation(record);
    }

    public override async Task<CertificationResponse> CreateEmployeeCertification(
        CreateEmployeeCertificationRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var certificationDate = DateOnly.Parse(request.CertificationDate);
        var parentCtrlNbr = request.HasParentCtrlNbr ? ControlNumber.Create(request.ParentCtrlNbr) : null;
        var certification = await svc.CreateEmployeeCertificationAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.RegulatoryQualificationCtrlNbr),
            request.CertificationType, certificationDate, parentCtrlNbr,
            request.CertificationNumber, context.CancellationToken);
        return MapCertification(certification);
    }

    public override async Task<CertificationResponse> UpdateEmployeeCertification(
        UpdateEmployeeCertificationRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var certificationDate = DateOnly.Parse(request.CertificationDate);
        var parentCtrlNbr = request.HasParentCtrlNbr ? ControlNumber.Create(request.ParentCtrlNbr) : null;
        try
        {
            var certification = await svc.UpdateEmployeeCertificationAsync(
                ControlNumber.Create(request.CtrlNbr),
                ControlNumber.Create(request.RegulatoryQualificationCtrlNbr),
                request.CertificationType, certificationDate, parentCtrlNbr,
                request.CertificationNumber, context.CancellationToken);
            return MapCertification(certification);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<Empty> DeleteEmployeeCertification(
        DeleteEmployeeCertificationRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        await svc.DeleteEmployeeCertificationAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
        return new Empty();
    }

    public override async Task<GetCertificationRevocationHistoryResponse> GetCertificationRevocationHistory(
        GetCertificationRevocationHistoryRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var certCtrlNbr = ControlNumber.Create(request.EmployeeCertificationCtrlNbr);
        var revocations = await svc.GetRevocationHistoryAsync(certCtrlNbr, context.CancellationToken);
        var response = new GetCertificationRevocationHistoryResponse();
        foreach (var r in revocations) response.Revocations.Add(MapRevocation(r));
        return response;
    }

    public override async Task<DrugAlcoholTestResponse> RecordDrugAlcoholTest(
        RecordDrugAlcoholTestRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        await EnsureEmployeeInSelectedParentAsync(request.EmployeeCtrlNbr, context.CancellationToken);
        decimal? alcoholResult = request.HasAlcoholResult ? Convert.ToDecimal(request.AlcoholResult) : null;
        var testRecord = await svc.RecordDrugAlcoholTestAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), request.TestType,
            request.TestDate.ToDateTime(), alcoholResult, request.DrugResult,
            request.SubstancesDetected, request.FederalAuthority, context.CancellationToken);
        var impactHandler = serviceProvider.GetRequiredService<DrugAlcoholCertificationImpactHandler>();
        await svc.HandleDrugAlcoholActionsAsync(testRecord, impactHandler, context.CancellationToken);
        return MapDrugAlcoholTest(testRecord);
    }

    public override async Task<GetDrugAlcoholTestsResponse> GetDrugAlcoholTests(
        GetDrugAlcoholTestsRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        await EnsureEmployeeInSelectedParentAsync(request.EmployeeCtrlNbr, context.CancellationToken);
        var tests = await svc.GetDrugAlcoholTestsAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        var response = new GetDrugAlcoholTestsResponse();
        response.Tests.AddRange(tests.Select(MapDrugAlcoholTest));
        return response;
    }

    public override async Task<GetDrugAlcoholActionsResponse> GetDrugAlcoholActions(
        GetDrugAlcoholActionsRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        await EnsureEmployeeInSelectedParentAsync(request.EmployeeCtrlNbr, context.CancellationToken);
        var actions = await svc.GetDrugAlcoholActionsAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        var response = new GetDrugAlcoholActionsResponse();
        response.Actions.AddRange(actions.Select(MapDrugAlcoholAction));
        return response;
    }

    public override async Task<VoluntaryReferralResponse> CreateVoluntaryReferral(
        CreateVoluntaryReferralRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        await EnsureEmployeeInSelectedParentAsync(request.EmployeeCtrlNbr, context.CancellationToken);
        var referral = await svc.CreateVoluntaryReferralAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        return MapVoluntaryReferral(referral);
    }

    public override async Task<GetVoluntaryReferralsResponse> GetVoluntaryReferrals(
        GetVoluntaryReferralsRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        await EnsureEmployeeInSelectedParentAsync(request.EmployeeCtrlNbr, context.CancellationToken);
        var referrals = await svc.GetVoluntaryReferralsAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        var response = new GetVoluntaryReferralsResponse();
        response.Referrals.AddRange(referrals.Select(MapVoluntaryReferral));
        return response;
    }

    public override async Task<VoluntaryReferralResponse> UpdateVoluntaryReferral(
        UpdateVoluntaryReferralRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var referral = await svc.GetVoluntaryReferralAsync(ControlNumber.Create(request.ReferralCtrlNbr), context.CancellationToken);
        await EnsureEmployeeInSelectedParentAsync(referral.EmployeeCtrlNbr.Value, context.CancellationToken);

        var actionDate = request.ActionDate is not null
            ? DateTime.SpecifyKind(request.ActionDate.ToDateTime(), DateTimeKind.Utc)
            : DateTime.UtcNow;

        switch (request.ActionType)
        {
            case "RecordSapEvaluation":
                referral.RecordSapEvaluation(actionDate);
                break;
            case "CompleteTreatment":
                referral.CompleteTreatment(actionDate);
                break;
            case "RecordReturnToDutyTest":
                referral.RecordReturnToDutyTest(actionDate, request.ReturnToDutyResult);
                break;
            case "Complete":
                referral.Complete();
                break;
            default:
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unknown referral action type: {request.ActionType}"));
        }

        await svc.UpdateVoluntaryReferralAsync(referral, context.CancellationToken);
        return MapVoluntaryReferral(referral);
    }

    private async Task EnsureEmployeeInSelectedParentAsync(long employeeCtrlNbr, CancellationToken ct)
    {
        if (employeeCtrlNbr <= 0)
            return;

        var selectedParentCtrlNbr = GetSelectedParentCtrlNbr();
        if (!selectedParentCtrlNbr.HasValue)
            return;

        var uowFactory = serviceProvider.GetRequiredService<Domain.Interfaces.IOrchestrationUnitOfWorkFactory>();
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(employeeCtrlNbr), ct)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee {employeeCtrlNbr} not found."));

        if (employee.ClientCtrlNbr.Value != selectedParentCtrlNbr.Value)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "The requested employee is outside the selected parent scope."));
    }

    private long? GetSelectedParentCtrlNbr()
    {
        var accessor = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var raw = accessor.HttpContext?.Request.Headers["x-parent-ctrl-nbr"].FirstOrDefault();
        return long.TryParse(raw, out var parentCtrlNbr) && parentCtrlNbr > 0 ? parentCtrlNbr : null;
    }

    public override async Task<DutyTourResponse> GetDutyTour(
        GetDutyTourRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        try { return MapTour(await svc.GetDutyTourAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<CertificationResponse> GetCertification(
        GetCertificationRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        try { return MapCertification(await svc.GetCertificationAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetEmployeeCertificationsResponse> GetEmployeeCertifications(
        GetEmployeeCertificationsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var employeeCtrlNbr = ControlNumber.Create(request.EmployeeCtrlNbr);
        var certifications = await svc.GetEmployeeCertificationsAsync(employeeCtrlNbr, context.CancellationToken);
        var (employeeNameLnf, employeeNumber) = await employeeNameService.GetEmployeeInfoAsync(employeeCtrlNbr);
        var response = new GetEmployeeCertificationsResponse();
        response.Certifications.AddRange(certifications.Select(c =>
        {
            var mapped = MapCertification(c);
            mapped.EmployeeNumber = employeeNumber;
            mapped.EmployeeNameLnf = employeeNameLnf;
            return mapped;
        }));
        return response;
    }

    public override async Task<GetCertificationEligibilityChecksResponse> GetCertificationEligibilityChecks(
        GetCertificationEligibilityChecksRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var certification = await svc.GetCertificationWithChecksAsync(
            ControlNumber.Create(request.EmployeeCertificationCtrlNbr), context.CancellationToken);
        var response = new GetCertificationEligibilityChecksResponse();
        if (certification is not null)
        {
            var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);
            foreach (var c in certification.EligibilityChecks)
                response.Checks.Add(MapEligibilityCheck(c, asOfDate));
        }
        return response;
    }

    public override async Task<CertificationEligibilityCheckResponse> AddCertificationEligibilityCheck(
        AddCertificationEligibilityCheckRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var parentCtrlNbr = request.HasParentCtrlNbr ? ControlNumber.Create(request.ParentCtrlNbr) : null;
        var stalenessLimitDays = await GetStalenessLimitDaysAsync(request.CheckType, parentCtrlNbr, context.CancellationToken);
        try
        {
            var (check, _) = await svc.AddEligibilityCheckAsync(
                ControlNumber.Create(request.EmployeeCertificationCtrlNbr),
                request.CheckType, DateOnly.Parse(request.EvaluationDate),
                stalenessLimitDays, request.Result, request.EvaluatorName,
                context.CancellationToken);
            return MapEligibilityCheck(check, DateOnly.FromDateTime(DateTime.UtcNow));
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<CertificationEligibilityCheckResponse> UpdateCertificationEligibilityCheck(
        UpdateCertificationEligibilityCheckRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var parentCtrlNbr = request.HasParentCtrlNbr ? ControlNumber.Create(request.ParentCtrlNbr) : null;
        var stalenessLimitDays = await GetStalenessLimitDaysAsync(request.CheckType, parentCtrlNbr, context.CancellationToken);
        try
        {
            var (check, _) = await svc.UpdateEligibilityCheckAsync(
                ControlNumber.Create(request.EligibilityCheckCtrlNbr),
                request.CheckType, DateOnly.Parse(request.EvaluationDate),
                stalenessLimitDays, request.Result, request.EvaluatorName,
                context.CancellationToken);
            return MapEligibilityCheck(check, DateOnly.FromDateTime(DateTime.UtcNow));
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<Empty> DeleteCertificationEligibilityCheck(
        DeleteCertificationEligibilityCheckRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        try { await svc.DeleteEligibilityCheckAsync(ControlNumber.Create(request.EligibilityCheckCtrlNbr), context.CancellationToken); return new Empty(); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<AddEmployeeRequirementResultResponse> AddEmployeeRequirementResult(
        AddEmployeeRequirementResultRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var parentCtrlNbr = request.HasParentCtrlNbr ? ControlNumber.Create(request.ParentCtrlNbr) : null;
        var stalenessLimitDays = await GetStalenessLimitDaysAsync(request.CheckType, parentCtrlNbr, context.CancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        try
        {
            var r = await svc.AddEmployeeRequirementResultAsync(
                ControlNumber.Create(request.EmployeeCtrlNbr),
                ControlNumber.Create(request.RegulatoryQualificationCtrlNbr),
                request.CertificationType, DateOnly.Parse(request.EvaluationDate),
                parentCtrlNbr, request.CheckType, request.Result, request.EvaluatorName,
                stalenessLimitDays, context.CancellationToken);
            return new AddEmployeeRequirementResultResponse
            {
                Check = MapEligibilityCheck(r.Check, today),
                Certification = MapCertification(r.Certification),
                CertificationActivated = r.Certification.Status == CertificationStatuses.Active
            };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.Internal, ex.Message)); }
    }
    public override async Task<CertificationComplianceSummaryResponse> GetCertificationComplianceSummary(
        GetCertificationComplianceSummaryRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.FraCompliance.FraComplianceService>();
        var certification = await svc.GetCertificationWithChecksAsync(
            ControlNumber.Create(request.EmployeeCertificationCtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Employee certification not found"));

        var expirationService = serviceProvider.GetRequiredService<CertificationExpirationService>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var response = new CertificationComplianceSummaryResponse
        {
            IsExpired = expirationService.IsExpired(certification, today),
            IsExpiringSoon = expirationService.IsExpiringSoon(certification, today),
            IsMonitoringObservationCurrent = certification.EligibilityChecks
                .Any(c => string.Equals(c.CheckType, "OperationalMonitoring", StringComparison.OrdinalIgnoreCase)
                       && c.Result == "Pass" && !c.IsStale(today)),
            IsComplianceTestCurrent = certification.EligibilityChecks
                .Any(c => string.Equals(c.CheckType, "ComplianceTest", StringComparison.OrdinalIgnoreCase)
                       && c.Result == "Pass" && !c.IsStale(today)),
            IsFullyCompliant = certification.Status == CertificationStatuses.Active
        };
        response.StaleOrMissingChecks.AddRange(
            EligibilityCheckStalenessLimits.Days.Keys.Where(type =>
                !certification.EligibilityChecks.Any(c =>
                    string.Equals(c.CheckType, type, StringComparison.OrdinalIgnoreCase)
                    && c.Result == "Pass"
                    && !c.IsStale(today))));
        return response;
    }

    public override async Task<FraCertificationConfigResponse> GetCertificationConfig(
        GetCertificationConfigRequest request,
        ServerCallContext context)
    {
        var configService = serviceProvider.GetRequiredService<FraCertificationConfigService>();
        var parentCtrlNbr = ControlNumber.Create(request.ParentCtrlNbr);
        var railroadCtrlNbr = request.HasRailroadCtrlNbr ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var config = await configService.GetOrDefaultAsync(parentCtrlNbr, railroadCtrlNbr, context.CancellationToken);
        return MapCertificationConfig(config);
    }

    public override async Task<GetCertificationCheckConfigsResponse> GetCertificationCheckConfigs(
        GetCertificationCheckConfigsRequest request,
        ServerCallContext context)
    {
        var configService = serviceProvider.GetRequiredService<FraCertificationConfigService>();
        var parentCtrlNbr = ControlNumber.Create(request.ParentCtrlNbr);
        var railroadCtrlNbr = request.HasRailroadCtrlNbr ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var configs = await configService.GetCheckConfigsOrDefaultAsync(parentCtrlNbr, railroadCtrlNbr, context.CancellationToken);
        var response = new GetCertificationCheckConfigsResponse();
        response.Configs.AddRange(configs.Select(MapCheckConfig));
        return response;
    }

    public override async Task<FraCertificationConfigResponse> UpsertCertificationConfig(
        UpsertCertificationConfigRequest request,
        ServerCallContext context)
    {
        var configService = serviceProvider.GetRequiredService<FraCertificationConfigService>();
        var parentCtrlNbr = ControlNumber.Create(request.ParentCtrlNbr);
        var railroadCtrlNbr = request.HasRailroadCtrlNbr ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var config = await configService.UpsertAsync(
            parentCtrlNbr, railroadCtrlNbr,
            request.CertCycleMonths, request.RecertWindowDays, request.RenewWindowDays,
            context.CancellationToken);
        return MapCertificationConfig(config);
    }

    public override async Task<FraCertificationCheckConfigResponse> UpsertCertificationCheckConfig(
        UpsertCertificationCheckConfigRequest request,
        ServerCallContext context)
    {
        var configService = serviceProvider.GetRequiredService<FraCertificationConfigService>();
        var parentCtrlNbr = ControlNumber.Create(request.ParentCtrlNbr);
        var railroadCtrlNbr = request.HasRailroadCtrlNbr ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var row = await configService.UpsertCheckConfigAsync(
            parentCtrlNbr, railroadCtrlNbr,
            request.CheckType, request.StalenessLimitDays, request.IsEnforced,
            context.CancellationToken);
        return MapCheckConfig(row);
    }

    private async Task<int> GetCertCycleMonthsAsync(ControlNumber? parentCtrlNbr, CancellationToken ct)
    {
        var configService = serviceProvider.GetRequiredService<FraCertificationConfigService>();
        return await configService.GetCertCycleMonthsAsync(parentCtrlNbr, ct);
    }

    private async Task<int> GetStalenessLimitDaysAsync(string checkType, ControlNumber? parentCtrlNbr, CancellationToken ct)
    {
        var configService = serviceProvider.GetRequiredService<FraCertificationConfigService>();
        return await configService.GetStalenessLimitDaysAsync(checkType, parentCtrlNbr, ct);
    }

    private static FraCertificationConfigResponse MapCertificationConfig(FraCertificationConfig c)
    {
        var response = new FraCertificationConfigResponse
        {
            CtrlNbr = c.CtrlNbr.Value,
            ParentCtrlNbr = c.ParentCtrlNbr.Value,
            CertCycleMonths = c.CertCycleMonths,
            RecertWindowDays = c.RecertWindowDays,
            RenewWindowDays = c.RenewWindowDays
        };
        if (c.RailroadCtrlNbr is not null)
            response.RailroadCtrlNbr = c.RailroadCtrlNbr.Value;
        return response;
    }

    private static FraCertificationCheckConfigResponse MapCheckConfig(FraCertificationCheckConfig c)
    {
        var response = new FraCertificationCheckConfigResponse
        {
            CtrlNbr = c.CtrlNbr.Value,
            ParentCtrlNbr = c.ParentCtrlNbr.Value,
            CheckType = c.CheckType,
            DisplayName = CertificationCheckDefaults.GetDisplayName(c.CheckType),
            StalenessLimitDays = c.StalenessLimitDays,
            IsEnforced = c.IsEnforced,
            IsEnforcementLocked = c.IsEnforcementLocked
        };
        if (c.RailroadCtrlNbr is not null)
            response.RailroadCtrlNbr = c.RailroadCtrlNbr.Value;
        return response;
    }

    private static DutyTourResponse MapTour(Domain.Modules.FraCompliance.FraDutyTour tour)
    {
        var response = new DutyTourResponse
        {
            CtrlNbr = tour.CtrlNbr.Value,
            EmployeeCtrlNbr = tour.EmployeeCtrlNbr.Value,
            RegulatoryStandardCtrlNbr = tour.RegulatoryStandardCtrlNbr.Value,
            DutyTourStart = Timestamp.FromDateTime(
                DateTime.SpecifyKind(tour.DutyTourStartUtc, DateTimeKind.Utc)),
            ConsecutiveDays = tour.ConsecutiveDays,
            IsQuickTieUp = tour.IsQuickTieUp,
            IsCertified = tour.IsCertified,
        };

        if (tour.DutyTourEndUtc.HasValue)
            response.DutyTourEnd = Timestamp.FromDateTime(
                DateTime.SpecifyKind(tour.DutyTourEndUtc.Value, DateTimeKind.Utc));

        if (tour.TotalTimeOnDutyMinutes.HasValue)
            response.TotalTimeOnDutyMinutes = tour.TotalTimeOnDutyMinutes.Value;

        if (tour.ExcessMinutes.HasValue)
            response.ExcessMinutes = tour.ExcessMinutes.Value;

        return response;
    }

    private static CertificationEligibilityCheckResponse MapEligibilityCheck(CertificationEligibilityCheck c, DateOnly asOfDate)
    {
        var response = new CertificationEligibilityCheckResponse
        {
            CtrlNbr = c.CtrlNbr.Value,
            EmployeeCertificationCtrlNbr = c.EmployeeCertificationCtrlNbr.Value,
            CheckType = c.CheckType,
            EvaluationDate = c.EvaluationDate.ToString("yyyy-MM-dd"),
            StalenessLimitDays = c.StalenessLimitDays,
            ExpiresAtDate = c.ExpiresAtDate.ToString("yyyy-MM-dd"),
            Result = c.Result,
            IsStale = c.IsStale(asOfDate)
        };

        if (!string.IsNullOrWhiteSpace(c.EvaluatorName))
            response.EvaluatorName = c.EvaluatorName;

        return response;
    }

    private static CertificationResponse MapCertification(EmployeeCertification c)
    {
        var response = new CertificationResponse
        {
            CtrlNbr = c.CtrlNbr.Value,
            EmployeeCtrlNbr = c.EmployeeCtrlNbr.Value,
            RegulatoryQualificationCtrlNbr = c.RegulatoryQualificationCtrlNbr.Value,
            CertificationType = c.CertificationType,
            Status = c.Status,
            ExpirationDate = c.ExpirationDate.ToString("yyyy-MM-dd"),
            EffectiveDate = c.CertificationDate.ToString("yyyy-MM-dd"),
            EmployeeNumber = string.Empty,
            EmployeeNameLnf = string.Empty
        };

        if (!string.IsNullOrWhiteSpace(c.CertificationNumber))
            response.CertificationNumber = c.CertificationNumber;

        return response;
    }

    private static CertificationResponse MapCertification(CertificationWithEmployeeDto dto, Dictionary<string, string> userMap)
    {
        var response = new CertificationResponse
        {
            CtrlNbr = dto.Certification.CtrlNbr.Value,
            EmployeeCtrlNbr = dto.Certification.EmployeeCtrlNbr.Value,
            RegulatoryQualificationCtrlNbr = dto.Certification.RegulatoryQualificationCtrlNbr.Value,
            CertificationType = dto.Certification.CertificationType,
            Status = dto.Certification.Status,
            ExpirationDate = dto.Certification.ExpirationDate.ToString("yyyy-MM-dd"),
            EffectiveDate = dto.Certification.CertificationDate.ToString("yyyy-MM-dd"),
            EmployeeNumber = dto.EmployeeNumber,
            EmployeeNameLnf = string.Empty
        };

        if (!string.IsNullOrEmpty(dto.UserId) && userMap.TryGetValue(dto.UserId, out var nameStr))
            response.EmployeeNameLnf = nameStr;

        if (!string.IsNullOrWhiteSpace(dto.Certification.CertificationNumber))
            response.CertificationNumber = dto.Certification.CertificationNumber;

        return response;
    }

    private static DrugAlcoholActionResponse MapDrugAlcoholAction(DrugAlcoholAction a)
    {
        var response = new DrugAlcoholActionResponse
        {
            CtrlNbr = a.CtrlNbr.Value,
            TestRecordCtrlNbr = a.TestRecordCtrlNbr.Value,
            EmployeeCtrlNbr = a.EmployeeCtrlNbr.Value,
            ActionType = a.ActionType,
            ActionDate = Timestamp.FromDateTime(DateTime.SpecifyKind(a.ActionDate, DateTimeKind.Utc))
        };

        if (!string.IsNullOrWhiteSpace(a.Notes))
            response.Notes = a.Notes;

        return response;
    }

    private static DrugAlcoholTestResponse MapDrugAlcoholTest(DrugAlcoholTestRecord t)
    {
        var response = new DrugAlcoholTestResponse
        {
            CtrlNbr = t.CtrlNbr.Value,
            EmployeeCtrlNbr = t.EmployeeCtrlNbr.Value,
            TestType = t.TestType,
            TestDate = Timestamp.FromDateTime(DateTime.SpecifyKind(t.TestDate, DateTimeKind.Utc)),
            IsViolation = t.IsViolation,
            FederalAuthority = t.FederalAuthority
        };

        if (t.AlcoholResult.HasValue)
            response.AlcoholResult = (double)t.AlcoholResult.Value;
        if (!string.IsNullOrWhiteSpace(t.DrugResult))
            response.DrugResult = t.DrugResult;
        if (!string.IsNullOrWhiteSpace(t.SubstancesDetected))
            response.SubstancesDetected = t.SubstancesDetected;

        return response;
    }

    private static VoluntaryReferralResponse MapVoluntaryReferral(VoluntaryReferral r)
    {
        var response = new VoluntaryReferralResponse
        {
            CtrlNbr = r.CtrlNbr.Value,
            EmployeeCtrlNbr = r.EmployeeCtrlNbr.Value,
            ReferralDate = Timestamp.FromDateTime(DateTime.SpecifyKind(r.ReferralDate, DateTimeKind.Utc)),
            FollowUpTestsRequired = r.FollowUpTestsRequired,
            Status = r.Status
        };

        if (r.SapEvaluationDate.HasValue)
            response.SapEvaluationDate = Timestamp.FromDateTime(DateTime.SpecifyKind(r.SapEvaluationDate.Value, DateTimeKind.Utc));
        if (r.TreatmentCompletedDate.HasValue)
            response.TreatmentCompletedDate = Timestamp.FromDateTime(DateTime.SpecifyKind(r.TreatmentCompletedDate.Value, DateTimeKind.Utc));
        if (r.ReturnToDutyTestDate.HasValue)
            response.ReturnToDutyTestDate = Timestamp.FromDateTime(DateTime.SpecifyKind(r.ReturnToDutyTestDate.Value, DateTimeKind.Utc));
        if (!string.IsNullOrWhiteSpace(r.ReturnToDutyResult))
            response.ReturnToDutyResult = r.ReturnToDutyResult;
        if (r.FollowUpEndDate.HasValue)
            response.FollowUpEndDate = Timestamp.FromDateTime(DateTime.SpecifyKind(r.FollowUpEndDate.Value, DateTimeKind.Utc));

        return response;
    }

    private static CertificationRevocationResponse MapRevocation(CertificationRevocationRecord r)
    {
        var response = new CertificationRevocationResponse
        {
            CtrlNbr = r.CtrlNbr.Value,
            EmployeeCertificationCtrlNbr = r.EmployeeCertificationCtrlNbr.Value,
            ViolationType = r.ViolationType,
            ViolationDate = Timestamp.FromDateTime(DateTime.SpecifyKind(r.ViolationDate, DateTimeKind.Utc)),
            SuspendedAtUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(r.SuspendedAtUtc, DateTimeKind.Utc))
        };

        if (r.WrittenNoticeAtUtc.HasValue)
            response.WrittenNoticeAtUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(r.WrittenNoticeAtUtc.Value, DateTimeKind.Utc));
        if (r.HearingScheduledUtc.HasValue)
            response.HearingScheduledUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(r.HearingScheduledUtc.Value, DateTimeKind.Utc));
        if (r.HearingHeldUtc.HasValue)
            response.HearingHeldUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(r.HearingHeldUtc.Value, DateTimeKind.Utc));
        if (r.PresidingOfficerCtrlNbr is not null)
            response.PresidingOfficerCtrlNbr = r.PresidingOfficerCtrlNbr.Value;
        if (!string.IsNullOrWhiteSpace(r.Decision))
            response.Decision = r.Decision;
        if (r.DecisionDate.HasValue)
            response.DecisionDate = Timestamp.FromDateTime(DateTime.SpecifyKind(r.DecisionDate.Value, DateTimeKind.Utc));
        if (r.RevocationPeriodMonths.HasValue)
            response.RevocationPeriodMonths = r.RevocationPeriodMonths.Value;
        if (r.RevocationEndsUtc.HasValue)
            response.RevocationEndsUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(r.RevocationEndsUtc.Value, DateTimeKind.Utc));

        return response;
    }
}

