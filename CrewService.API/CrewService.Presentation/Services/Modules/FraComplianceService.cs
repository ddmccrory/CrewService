using CrewService.Application.FraCompliance;
using CrewService.Presentation.Services;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class FraComplianceService(
    IFraDutyTourRepository dutyTourRepository,
    IEmployeeCertificationReadRepository employeeCertificationReadRepository,
    IEmployeeCertificationRepository employeeCertificationRepository,
    ICertificationRevocationRepository certificationRevocationRepository,
    IDrugAlcoholTestRepository drugAlcoholTestRepository,
    IVoluntaryReferralRepository voluntaryReferralRepository,
    EmployeeNameService employeeNameService,
    IServiceProvider serviceProvider)
    : FraComplianceSrvc.FraComplianceSrvcBase
{
    public override async Task<SearchDutyToursResponse> SearchDutyTours(
        SearchDutyToursRequest request, ServerCallContext context)
    {
        var criteria = new FraRecordSearchCriteria
        {
            EmployeeCtrlNbr = request.HasEmployeeCtrlNbr
                ? ControlNumber.Create(request.EmployeeCtrlNbr) : null,
            StartDateUtc = request.StartDate?.ToDateTime(),
            EndDateUtc = request.EndDate?.ToDateTime(),
            LocationCode = request.HasLocationCode ? request.LocationCode : null,
            RegulatoryStandardCode = request.HasRegulatoryStandardCode
                ? request.RegulatoryStandardCode : null,
            HasExcessService = request.HasHasExcessService ? request.HasExcessService : null,
        };

        var tours = await dutyTourRepository.SearchAsync(criteria, context.CancellationToken);

        var response = new SearchDutyToursResponse();
        foreach (var tour in tours)
            response.DutyTours.Add(MapTour(tour));

        return response;
    }

    public override async Task<GetEmployeeCertificationsResponse> GetCertificationsByClient(
        GetCertificationsByClientRequest request,
        ServerCallContext context)
    {
        var clientCtrlNbr = ControlNumber.Create(request.ClientCtrlNbr);
        var statuses = request.Statuses.Count > 0
            ? request.Statuses.ToList()
            : [CertificationStatuses.Pending, CertificationStatuses.Active];

        var certifications = await employeeCertificationReadRepository
            .GetByClientAndStatusesAsync(clientCtrlNbr, statuses, context.CancellationToken);

        // Batch-load user names to avoid N+1 lookups
        var userIds = certifications.Select(c => c.UserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var userMap = new Dictionary<string, string>();
        foreach (var uid in userIds)
        {
            var name = await employeeNameService.GetFullNameLnfAsync(uid);
            userMap[uid] = name;
        }

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
        var ctrlNbr = ControlNumber.Create(request.RevocationRecordCtrlNbr);
        await certificationRevocationService.RecordWrittenNoticeAsync(ctrlNbr, context.CancellationToken);
        var record = await certificationRevocationRepository.GetByCtrlNbrAsync(ctrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Revocation record not found"));
        return MapRevocation(record);
    }

    public override async Task<CertificationRevocationResponse> ScheduleRevocationHearing(
        ScheduleRevocationHearingRequest request,
        ServerCallContext context)
    {
        var certificationRevocationService = serviceProvider.GetRequiredService<CertificationRevocationService>();
        var ctrlNbr = ControlNumber.Create(request.RevocationRecordCtrlNbr);
        await certificationRevocationService.ScheduleHearingAsync(ctrlNbr, request.HearingDate.ToDateTime(), context.CancellationToken);
        var record = await certificationRevocationRepository.GetByCtrlNbrAsync(ctrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Revocation record not found"));
        return MapRevocation(record);
    }

    public override async Task<CertificationRevocationResponse> DecideRevocation(
        DecideRevocationRequest request,
        ServerCallContext context)
    {
        var certificationRevocationService = serviceProvider.GetRequiredService<CertificationRevocationService>();
        var ctrlNbr = ControlNumber.Create(request.RevocationRecordCtrlNbr);
        await certificationRevocationService.DecideAsync(
            revocationRecordCtrlNbr: ctrlNbr,
            decision: request.Decision,
            revocationPeriodMonths: request.HasRevocationPeriodMonths ? request.RevocationPeriodMonths : null,
            ct: context.CancellationToken);

        var record = await certificationRevocationRepository.GetByCtrlNbrAsync(ctrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Revocation record not found"));
        return MapRevocation(record);
    }

    public override async Task<CertificationResponse> CreateEmployeeCertification(
        CreateEmployeeCertificationRequest request,
        ServerCallContext context)
    {
        var certificationDate = DateOnly.Parse(request.CertificationDate);
        var certification = EmployeeCertification.Create(
            employeeCtrlNbr: ControlNumber.Create(request.EmployeeCtrlNbr),
            regulatoryQualificationCtrlNbr: ControlNumber.Create(request.RegulatoryQualificationCtrlNbr),
            certificationType: request.CertificationType,
            certificationDate: certificationDate,
            recertificationIntervalMonths: 36,
            certificationNumber: request.CertificationNumber);

        await employeeCertificationRepository.AddAsync(certification, context.CancellationToken);
        return MapCertification(certification);
    }

    public override async Task<CertificationResponse> UpdateEmployeeCertification(
        UpdateEmployeeCertificationRequest request,
        ServerCallContext context)
    {
        var certification = await employeeCertificationRepository
            .GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Employee certification not found"));

        var certificationDate = DateOnly.Parse(request.CertificationDate);
        certification.UpdateCertificationDetails(
            regulatoryQualificationCtrlNbr: ControlNumber.Create(request.RegulatoryQualificationCtrlNbr),
            certificationType: request.CertificationType,
            certificationDate: certificationDate,
            recertificationIntervalMonths: 36,
            certificationNumber: request.CertificationNumber);

        await employeeCertificationRepository.UpdateAsync(certification, context.CancellationToken);
        return MapCertification(certification);
    }

    public override async Task<Empty> DeleteEmployeeCertification(
        DeleteEmployeeCertificationRequest request,
        ServerCallContext context)
    {
        await employeeCertificationRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
        return new Empty();
    }

    public override async Task<GetCertificationRevocationHistoryResponse> GetCertificationRevocationHistory(
        GetCertificationRevocationHistoryRequest request,
        ServerCallContext context)
    {
        var certCtrlNbr = ControlNumber.Create(request.EmployeeCertificationCtrlNbr);
        var revocations = await certificationRevocationRepository
            .GetByCertificationCtrlNbrAsync(certCtrlNbr, context.CancellationToken);

        var response = new GetCertificationRevocationHistoryResponse();
        foreach (var r in revocations)
            response.Revocations.Add(MapRevocation(r));

        return response;
    }

    public override async Task<DrugAlcoholTestResponse> RecordDrugAlcoholTest(
        RecordDrugAlcoholTestRequest request,
        ServerCallContext context)
    {
        decimal? alcoholResult = request.HasAlcoholResult
            ? Convert.ToDecimal(request.AlcoholResult)
            : null;

        var testRecord = DrugAlcoholTestRecord.Create(
            employeeCtrlNbr: ControlNumber.Create(request.EmployeeCtrlNbr),
            testType: request.TestType,
            testDate: request.TestDate.ToDateTime(),
            alcoholResult: alcoholResult,
            drugResult: request.DrugResult,
            substancesDetected: request.SubstancesDetected,
            federalAuthority: request.FederalAuthority);

        await drugAlcoholTestRepository.AddAsync(testRecord, context.CancellationToken);

        await HandleDrugAlcoholActionsAsync(testRecord, context.CancellationToken);

        return MapDrugAlcoholTest(testRecord);
    }

    public override async Task<GetDrugAlcoholTestsResponse> GetDrugAlcoholTests(
        GetDrugAlcoholTestsRequest request,
        ServerCallContext context)
    {
        var tests = await drugAlcoholTestRepository
            .GetByEmployeeCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);

        var response = new GetDrugAlcoholTestsResponse();
        response.Tests.AddRange(tests.Select(MapDrugAlcoholTest));
        return response;
    }

    public override async Task<GetDrugAlcoholActionsResponse> GetDrugAlcoholActions(
        GetDrugAlcoholActionsRequest request,
        ServerCallContext context)
    {
        var drugAlcoholActionRepository = serviceProvider.GetRequiredService<IDrugAlcoholActionRepository>();
        var actions = await drugAlcoholActionRepository
            .GetByEmployeeCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);

        var response = new GetDrugAlcoholActionsResponse();
        response.Actions.AddRange(actions.Select(MapDrugAlcoholAction));
        return response;
    }

    public override async Task<VoluntaryReferralResponse> CreateVoluntaryReferral(
        CreateVoluntaryReferralRequest request,
        ServerCallContext context)
    {
        var referral = VoluntaryReferral.Create(ControlNumber.Create(request.EmployeeCtrlNbr));
        await voluntaryReferralRepository.AddAsync(referral, context.CancellationToken);
        return MapVoluntaryReferral(referral);
    }

    public override async Task<GetVoluntaryReferralsResponse> GetVoluntaryReferrals(
        GetVoluntaryReferralsRequest request,
        ServerCallContext context)
    {
        var referrals = await voluntaryReferralRepository
            .GetByEmployeeCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);

        var response = new GetVoluntaryReferralsResponse();
        response.Referrals.AddRange(referrals.Select(MapVoluntaryReferral));
        return response;
    }

    public override async Task<VoluntaryReferralResponse> UpdateVoluntaryReferral(
        UpdateVoluntaryReferralRequest request,
        ServerCallContext context)
    {
        var referralCtrlNbr = ControlNumber.Create(request.ReferralCtrlNbr);
        var referral = await voluntaryReferralRepository.GetByCtrlNbrAsync(referralCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Voluntary referral not found"));

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

        await voluntaryReferralRepository.UpdateAsync(referral, context.CancellationToken);
        return MapVoluntaryReferral(referral);
    }

    public override async Task<DutyTourResponse> GetDutyTour(
        GetDutyTourRequest request, ServerCallContext context)
    {
        var tour = await dutyTourRepository.GetByCtrlNbrAsync(
            ControlNumber.Create(request.CtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Duty tour not found"));

        return MapTour(tour);
    }

    public override async Task<CertificationResponse> GetCertification(
        GetCertificationRequest request, ServerCallContext context)
    {
        var certification = await employeeCertificationRepository
            .GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Certification not found"));

        return MapCertification(certification);
    }

    public override async Task<GetEmployeeCertificationsResponse> GetEmployeeCertifications(
        GetEmployeeCertificationsRequest request, ServerCallContext context)
    {
        var employeeCtrlNbr = ControlNumber.Create(request.EmployeeCtrlNbr);
        var certifications = await employeeCertificationReadRepository
            .GetByEmployeeCtrlNbrAsync(employeeCtrlNbr, context.CancellationToken);

        var response = new GetEmployeeCertificationsResponse();
        response.Certifications.AddRange(certifications.Select(MapCertification));

        return response;
    }

    public override async Task<GetCertificationEligibilityChecksResponse> GetCertificationEligibilityChecks(
        GetCertificationEligibilityChecksRequest request,
        ServerCallContext context)
    {
        var certCtrlNbr = ControlNumber.Create(request.EmployeeCertificationCtrlNbr);
        var certification = await employeeCertificationRepository
            .GetByCtrlNbrWithChecksAsync(certCtrlNbr, context.CancellationToken);

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
        var certification = await employeeCertificationRepository
            .GetByCtrlNbrWithChecksAsync(ControlNumber.Create(request.EmployeeCertificationCtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Employee certification not found"));

        var eligibilityService = serviceProvider.GetRequiredService<CertificationEligibilityService>();
        var evaluationDate = DateOnly.Parse(request.EvaluationDate);
        var stalenessLimitDays = eligibilityService.GetStalenessLimitDays(request.CheckType);

        var check = certification.AddEligibilityCheck(
            checkType: request.CheckType,
            evaluationDate: evaluationDate,
            stalenessLimitDays: stalenessLimitDays,
            result: request.Result,
            evaluatorName: request.EvaluatorName);

        await employeeCertificationRepository.UpdateAsync(certification, context.CancellationToken);
        return MapEligibilityCheck(check, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public override async Task<CertificationEligibilityCheckResponse> UpdateCertificationEligibilityCheck(
        UpdateCertificationEligibilityCheckRequest request,
        ServerCallContext context)
    {
        var checkCtrlNbr = ControlNumber.Create(request.EligibilityCheckCtrlNbr);
        var certification = await employeeCertificationRepository
            .GetByEligibilityCheckCtrlNbrWithChecksAsync(checkCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Eligibility check not found"));

        var eligibilityService = serviceProvider.GetRequiredService<CertificationEligibilityService>();
        var evaluationDate = DateOnly.Parse(request.EvaluationDate);
        var stalenessLimitDays = eligibilityService.GetStalenessLimitDays(request.CheckType);

        var check = certification.UpdateEligibilityCheck(
            eligibilityCheckCtrlNbr: checkCtrlNbr,
            checkType: request.CheckType,
            evaluationDate: evaluationDate,
            stalenessLimitDays: stalenessLimitDays,
            result: request.Result,
            evaluatorName: request.EvaluatorName);

        await employeeCertificationRepository.UpdateAsync(certification, context.CancellationToken);
        return MapEligibilityCheck(check, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public override async Task<Empty> DeleteCertificationEligibilityCheck(
        DeleteCertificationEligibilityCheckRequest request,
        ServerCallContext context)
    {
        var checkCtrlNbr = ControlNumber.Create(request.EligibilityCheckCtrlNbr);
        var certification = await employeeCertificationRepository
            .GetByEligibilityCheckCtrlNbrWithChecksAsync(checkCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Eligibility check not found"));

        certification.DeleteEligibilityCheck(checkCtrlNbr);
        await employeeCertificationRepository.UpdateAsync(certification, context.CancellationToken);
        return new Empty();
    }

    public override async Task<AddEmployeeRequirementResultResponse> AddEmployeeRequirementResult(
        AddEmployeeRequirementResultRequest request,
        ServerCallContext context)
    {
        var employeeCtrlNbr = ControlNumber.Create(request.EmployeeCtrlNbr);
        var regulatoryQualificationCtrlNbr = ControlNumber.Create(request.RegulatoryQualificationCtrlNbr);
        var evaluationDate = DateOnly.Parse(request.EvaluationDate);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var eligibilityService = serviceProvider.GetRequiredService<CertificationEligibilityService>();

        var existingCertifications = await employeeCertificationRepository
            .GetByEmployeeCtrlNbrAsync(employeeCtrlNbr, context.CancellationToken);

        var certification = existingCertifications
            .OrderByDescending(c => c.CertificationDate)
            .FirstOrDefault(c =>
                c.RegulatoryQualificationCtrlNbr == regulatoryQualificationCtrlNbr
                && string.Equals(c.CertificationType, request.CertificationType, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(c.Status, "Revoked", StringComparison.OrdinalIgnoreCase));

        if (certification is null)
        {
            certification = EmployeeCertification.Create(
                employeeCtrlNbr: employeeCtrlNbr,
                regulatoryQualificationCtrlNbr: regulatoryQualificationCtrlNbr,
                certificationType: request.CertificationType,
                certificationDate: evaluationDate,
                recertificationIntervalMonths: 36,
                certificationNumber: null);

            await employeeCertificationRepository.AddAsync(certification, context.CancellationToken);
            certification = await employeeCertificationRepository
                .GetByCtrlNbrWithChecksAsync(certification.CtrlNbr, context.CancellationToken)
                ?? throw new RpcException(new Status(StatusCode.Internal, "Failed to load created employee certification"));
        }
        else
        {
            certification = await employeeCertificationRepository
                .GetByCtrlNbrWithChecksAsync(certification.CtrlNbr, context.CancellationToken)
                ?? throw new RpcException(new Status(StatusCode.NotFound, "Employee certification not found"));
        }

        var stalenessLimitDays = eligibilityService.GetStalenessLimitDays(request.CheckType);
        var check = certification.AddEligibilityCheck(
            checkType: request.CheckType,
            evaluationDate: evaluationDate,
            stalenessLimitDays: stalenessLimitDays,
            result: request.Result,
            evaluatorName: request.EvaluatorName);

        var wasActivated = false;
        if (eligibilityService.AreAllChecksValid(certification, today) && certification.Status != CertificationStatuses.Active)
        {
            certification.Activate();
            wasActivated = true;
        }

        await employeeCertificationRepository.UpdateAsync(certification, context.CancellationToken);

        return new AddEmployeeRequirementResultResponse
        {
            Check = MapEligibilityCheck(check, today),
            Certification = MapCertification(certification),
            CertificationActivated = wasActivated
        };
    }

    public override async Task<CertificationComplianceSummaryResponse> GetCertificationComplianceSummary(
        GetCertificationComplianceSummaryRequest request,
        ServerCallContext context)
    {
        var certification = await employeeCertificationRepository
            .GetByCtrlNbrWithChecksAsync(ControlNumber.Create(request.EmployeeCertificationCtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Employee certification not found"));

        var eligibilityService = serviceProvider.GetRequiredService<CertificationEligibilityService>();
        var expirationService = serviceProvider.GetRequiredService<CertificationExpirationService>();
        var monitoringService = serviceProvider.GetRequiredService<CertificationMonitoringService>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var missing = eligibilityService.GetStaleOrMissingChecks(certification, today);

        var response = new CertificationComplianceSummaryResponse
        {
            IsExpired = expirationService.IsExpired(certification, today),
            IsExpiringSoon = expirationService.IsExpiringSoon(certification, today),
            IsMonitoringObservationCurrent = monitoringService.IsMonitoringObservationCurrent(certification),
            IsComplianceTestCurrent = monitoringService.IsComplianceTestCurrent(certification),
            IsFullyCompliant = monitoringService.IsFullyCompliant(certification) && missing.Count == 0
        };
        response.StaleOrMissingChecks.AddRange(missing);
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

    private async Task HandleDrugAlcoholActionsAsync(DrugAlcoholTestRecord testRecord, CancellationToken ct)
    {
        var drugAlcoholActionRepository = serviceProvider.GetRequiredService<IDrugAlcoholActionRepository>();
        var drugAlcoholCertificationImpactHandler = serviceProvider.GetRequiredService<DrugAlcoholCertificationImpactHandler>();

        if (!testRecord.IsViolation && !testRecord.IsAlcoholRemovalRange)
            return;

        var removed = DrugAlcoholAction.Create(
            testRecord.CtrlNbr,
            testRecord.EmployeeCtrlNbr,
            "RemovedFromService",
            "Auto-generated from FRA Part 219 test result");
        await drugAlcoholActionRepository.AddAsync(removed, ct);

        if (!testRecord.IsViolation)
            return;

        var prior = await drugAlcoholTestRepository.GetByEmployeeCtrlNbrAsync(testRecord.EmployeeCtrlNbr, ct);
        var ineligibility = drugAlcoholCertificationImpactHandler.DetermineIneligibility(testRecord, [.. prior.Where(p => p.CtrlNbr != testRecord.CtrlNbr)]);

        var certifications = await employeeCertificationRepository.GetByEmployeeCtrlNbrAsync(testRecord.EmployeeCtrlNbr, ct);
        foreach (var cert in certifications.Where(c => c.Status == CertificationStatuses.Active))
        {
            cert.Suspend($"Drug/alcohol violation ({ineligibility.ViolationCount})");
            await employeeCertificationRepository.UpdateAsync(cert, ct);
        }
    }
}
