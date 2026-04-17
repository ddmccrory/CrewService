using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class FraComplianceClient(
    GrpcChannelProvider channelProvider,
    CircuitTokenProvider tokenProvider,
    AppContextService appContext,
    ILogger<FraComplianceClient> logger)
    : BaseGrpcClient<FraComplianceSrvc.FraComplianceSrvcClient>(
        channelProvider,
        tokenProvider,
        appContext,
        callInvoker => new FraComplianceSrvc.FraComplianceSrvcClient(callInvoker),
        logger)
{
    public async Task<SearchDutyToursResponse> SearchDutyToursAsync(SearchDutyToursRequest request)
    {
        try
        {
            return await _client.SearchDutyToursAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetEmployeeCertificationsResponse> GetCertificationsByClientAsync(long clientCtrlNbr, params string[] statuses)
    {
        try
        {
            var request = new GetCertificationsByClientRequest
            {
                ClientCtrlNbr = clientCtrlNbr
            };

            if (statuses is { Length: > 0 })
                request.Statuses.AddRange(statuses);

            return await _client.GetCertificationsByClientAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CertificationResponse> UpdateEmployeeCertificationAsync(UpdateEmployeeCertificationRequest request)
    {
        try
        {
            return await _client.UpdateEmployeeCertificationAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task DeleteEmployeeCertificationAsync(long certificationCtrlNbr)
    {
        try
        {
            await _client.DeleteEmployeeCertificationAsync(new DeleteEmployeeCertificationRequest
            {
                CtrlNbr = certificationCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CertificationEligibilityCheckResponse> UpdateCertificationEligibilityCheckAsync(UpdateCertificationEligibilityCheckRequest request)
    {
        try
        {
            return await _client.UpdateCertificationEligibilityCheckAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task DeleteCertificationEligibilityCheckAsync(long eligibilityCheckCtrlNbr)
    {
        try
        {
            await _client.DeleteCertificationEligibilityCheckAsync(new DeleteCertificationEligibilityCheckRequest
            {
                EligibilityCheckCtrlNbr = eligibilityCheckCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<AddEmployeeRequirementResultResponse> AddEmployeeRequirementResultAsync(AddEmployeeRequirementResultRequest request)
    {
        try
        {
            return await _client.AddEmployeeRequirementResultAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetCertificationEligibilityChecksResponse> GetCertificationEligibilityChecksAsync(long employeeCertificationCtrlNbr)
    {
        try
        {
            return await _client.GetCertificationEligibilityChecksAsync(new GetCertificationEligibilityChecksRequest
            {
                EmployeeCertificationCtrlNbr = employeeCertificationCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CertificationEligibilityCheckResponse> AddCertificationEligibilityCheckAsync(AddCertificationEligibilityCheckRequest request)
    {
        try
        {
            return await _client.AddCertificationEligibilityCheckAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CertificationComplianceSummaryResponse> GetCertificationComplianceSummaryAsync(long employeeCertificationCtrlNbr)
    {
        try
        {
            return await _client.GetCertificationComplianceSummaryAsync(new GetCertificationComplianceSummaryRequest
            {
                EmployeeCertificationCtrlNbr = employeeCertificationCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CertificationRevocationResponse> StartCertificationRevocationAsync(long employeeCertificationCtrlNbr, string violationType, DateTime violationDateUtc)
    {
        try
        {
            return await _client.StartCertificationRevocationAsync(new StartCertificationRevocationRequest
            {
                EmployeeCertificationCtrlNbr = employeeCertificationCtrlNbr,
                ViolationType = violationType,
                ViolationDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(violationDateUtc, DateTimeKind.Utc))
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetDrugAlcoholActionsResponse> GetDrugAlcoholActionsAsync(long employeeCtrlNbr)
    {
        try
        {
            return await _client.GetDrugAlcoholActionsAsync(new GetDrugAlcoholActionsRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CertificationRevocationResponse> RecordRevocationNoticeAsync(long revocationRecordCtrlNbr)
    {
        try
        {
            return await _client.RecordRevocationNoticeAsync(new RecordRevocationNoticeRequest
            {
                RevocationRecordCtrlNbr = revocationRecordCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CertificationRevocationResponse> ScheduleRevocationHearingAsync(long revocationRecordCtrlNbr, DateTime hearingDateUtc)
    {
        try
        {
            return await _client.ScheduleRevocationHearingAsync(new ScheduleRevocationHearingRequest
            {
                RevocationRecordCtrlNbr = revocationRecordCtrlNbr,
                HearingDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(hearingDateUtc, DateTimeKind.Utc))
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CertificationRevocationResponse> DecideRevocationAsync(long revocationRecordCtrlNbr, string decision, int? revocationPeriodMonths = null)
    {
        try
        {
            var request = new DecideRevocationRequest
            {
                RevocationRecordCtrlNbr = revocationRecordCtrlNbr,
                Decision = decision
            };

            if (revocationPeriodMonths.HasValue)
                request.RevocationPeriodMonths = revocationPeriodMonths.Value;

            return await _client.DecideRevocationAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetEmployeeCertificationsResponse> GetEmployeeCertificationsAsync(long employeeCtrlNbr)
    {
        try
        {
            return await _client.GetEmployeeCertificationsAsync(new GetEmployeeCertificationsRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CertificationResponse> CreateEmployeeCertificationAsync(CreateEmployeeCertificationRequest request)
    {
        try
        {
            return await _client.CreateEmployeeCertificationAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetCertificationRevocationHistoryResponse> GetCertificationRevocationHistoryAsync(long employeeCertificationCtrlNbr)
    {
        try
        {
            return await _client.GetCertificationRevocationHistoryAsync(new GetCertificationRevocationHistoryRequest
            {
                EmployeeCertificationCtrlNbr = employeeCertificationCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DrugAlcoholTestResponse> RecordDrugAlcoholTestAsync(RecordDrugAlcoholTestRequest request)
    {
        try
        {
            return await _client.RecordDrugAlcoholTestAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetDrugAlcoholTestsResponse> GetDrugAlcoholTestsAsync(long employeeCtrlNbr)
    {
        try
        {
            return await _client.GetDrugAlcoholTestsAsync(new GetDrugAlcoholTestsRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<VoluntaryReferralResponse> CreateVoluntaryReferralAsync(long employeeCtrlNbr)
    {
        try
        {
            return await _client.CreateVoluntaryReferralAsync(new CreateVoluntaryReferralRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetVoluntaryReferralsResponse> GetVoluntaryReferralsAsync(long employeeCtrlNbr)
    {
        try
        {
            return await _client.GetVoluntaryReferralsAsync(new GetVoluntaryReferralsRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<VoluntaryReferralResponse> UpdateVoluntaryReferralAsync(UpdateVoluntaryReferralRequest request)
    {
        try
        {
            return await _client.UpdateVoluntaryReferralAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
