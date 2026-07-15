using CrewService.Application.Payroll;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.HolidayManagement;

using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Models.Seniority;
using Xunit;

namespace CrewService.UnitTests.Payroll;

public class EarningCodeResolverTests
{
    private sealed class FakeRuleRepo(List<EarningCodeRule> rules) : IEarningCodeRuleRepository
    {
        public Task<IReadOnlyList<EarningCodeRule>> GetActiveByWorkAreaAsync(
            ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EarningCodeRule>>(rules);

        public Task<List<EarningCodeRule>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<EarningCodeRule>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<EarningCodeRule?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<EarningCodeRule?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(EarningCodeRule entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(EarningCodeRule entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public void Add(EarningCodeRule entity) => throw new NotImplementedException();
        public void Update(EarningCodeRule entity) => throw new NotImplementedException();
        public void Remove(EarningCodeRule entity) => throw new NotImplementedException();
    }

    private sealed class FakePayrollUoW(IEarningCodeRuleRepository earningCodeRules) : IOrchestrationUnitOfWork
    {
        public string CorrelationId => string.Empty;
        public string OrchestrationId => string.Empty;
        public IEarningCodeRuleRepository EarningCodeRules => earningCodeRules;
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
                public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
        public IEmployeeRepository Employees => null!;
        public IEmailAddressRepository EmailAddresses => null!;
        public IParentRepository Parents => null!;
        public IAddressTypeRepository AddressTypes => null!;
        public IPhoneNumberTypeRepository PhoneNumberTypes => null!;
        public IEmailAddressTypeRepository EmailAddressTypes => null!;
        public IEmploymentStatusRepository EmploymentStatuses => null!;
        public IEmploymentStatusHistoryRepository EmploymentStatusHistory => null!;
        public IEmployeePriorServiceCreditRepository EmployeePriorServiceCredits => null!;
        public ICraftRepository Crafts => null!;
        public IRosterRepository Rosters => null!;
        public ISeniorityRepository Seniority => null!;
        public ISeniorityStateRepository SeniorityStates => null!;
        public IGroupTypeRepository GroupTypes => null!;
        public IDynamicGroupRepository DynamicGroups => null!;
        public IGroupAttributeDefinitionRepository AttributeDefinitions => null!;
        public IGroupAttributeValueRepository AttributeValues => null!;
        public IStaffablePositionRepository StaffablePositions => null!;
        public IPositionAssignmentRepository PositionAssignments => null!;
        public IBoardCascadePolicyRepository BoardCascadePolicies => null!;
        public IRequiredPositionsStrategyRepository RequiredPositionsStrategies => throw new NotImplementedException();
        public ICraftRequiredPositionsStrategyRepository CraftRequiredPositionsStrategies => throw new NotImplementedException();
        public IRosterBoardRepository RosterBoards => null!;
        public ICrewRepository Crews => null!;
        public ICrewPositionRepository CrewPositions => null!;
        public ICrewIncumbencyRepository CrewIncumbencies => null!;
        public ICrewAssignmentRepository CrewAssignments => null!;
        public ICrewAttachmentInstanceRepository CrewAttachmentInstances => null!;
        public IAssignmentRepository Assignments => null!;
        public IAssignmentScheduleRepository AssignmentSchedules => null!;
        public IDepartmentRepository Departments => null!;
        public ICraftRoleRepository CraftRoles => null!;
        public ICraftRoleQualificationRepository CraftRoleQualifications => null!;
        public IWorkInstanceRepository WorkInstances => null!;
        public IPositionSlotRepository PositionSlots => null!;
        public ISlotRequirementRepository SlotRequirements => null!;
        public IShiftDefinitionRepository ShiftDefinitions => null!;
        public IShiftInstanceRepository ShiftInstances => null!;
        public IOnDutyRecordRepository OnDutyRecords => null!;
        public IOffDutyRecordRepository OffDutyRecords => null!;
        public ICraftOperationsPolicyRepository CraftOperationsPolicies => null!;
        public ICraftDisplacementPolicyRepository CraftDisplacementPolicies => null!;
        public IDisplacementCaseRepository DisplacementCases => null!;
        public IDisplacementClaimRepository DisplacementClaims => null!;
        public IBulletinPolicyRepository BulletinPolicies => null!;
        public ISeniorityMovePolicyRepository SeniorityMovePolicies => null!;
        public ISeniorityMoveRepository SeniorityMoves => null!;
        public IDispatchProjectionRepository DispatchProjections => null!;
        public IDispatchDecisionLogRepository DispatchDecisionLogs => null!;
        public IDispatchOverrideRepository DispatchOverrides => null!;
        public IEmployeeBookingRepository EmployeeBookings => null!;
        public IEmployeeCertificationRepository EmployeeCertifications => null!;
        public IEmployeeCertificationReadRepository EmployeeCertificationReads => null!;
        public IFraCertificationConfigRepository FraCertificationConfigs => null!;
        public IFraCertificationCheckConfigRepository FraCertificationCheckConfigs => null!;
        public IFraDutyTourRepository FraDutyTours => null!;
        public IRegulatoryStandardRepository RegulatoryStandards => null!;
        public IRegulatoryQualificationRepository RegulatoryQualifications => null!;
        public ICertificationRevocationRepository CertificationRevocations => null!;
        public IDrugAlcoholTestRepository DrugAlcoholTests => null!;
        public IDrugAlcoholActionRepository DrugAlcoholActions => null!;
        public IVoluntaryReferralRepository VoluntaryReferrals => null!;
        public IQualificationTypeRepository QualificationTypes => null!;
        public IQualificationRequirementRepository QualificationRequirements => null!;
        public IEmployeeQualificationRepository EmployeeQualifications => null!;
    public IEmployeeQualificationSuspensionRepository QualificationSuspensions => null!;
        public IAbsenceRequestRepository AbsenceRequests => null!;
        public IVacancyImpactRepository VacancyImpacts => null!;
        public ISafetyObservationRepository SafetyObservations => null!;
        public ISafetyObservationResolutionRepository SafetyResolutions => null!;
        public ISafetyCategoryRepository SafetyCategories => null!;
        public IRailroadInformationRepository RailroadInformation => null!;
        public IRailroadInformationReadReceiptRepository RailroadInformationReadReceipts => null!;
        public ITimeEntryRepository TimeEntries => null!;
        public IPayrollRunRepository PayrollRuns => null!;
        public IPayrollRecordRepository PayrollRecords => null!;
        public IPayrollExportBatchRepository PayrollExportBatches => null!;
        public IPayrollImportRecordRepository PayrollImportRecords => null!;
        public IHolidayRepository Holidays => null!;
        public IHolidayQualificationRuleRepository HolidayQualificationRules => null!;
        public IHolidayPayrollRecordRepository HolidayPayrollRecords => null!;
        public IPayRateRepository PayRates => null!;
        public IRailroadHolidaySelectionRepository RailroadHolidaySelections => null!;
        public IRoleRepository Roles => null!;
        public IFeatureRepository Features => null!;
        public IPermissionRepository Permissions => null!;
        public IPositionVacancyRepository PositionVacancies => null!;
        public IBulletinRepository Bulletins => null!;
        public IBulletinBidRepository BulletinBids => null!;
        public IBulletinRuleRepository BulletinRules => null!;
        public ICallSheetRuleRepository CallSheetRules => null!;
        public IDepartmentReassignmentRuleRepository DepartmentReassignmentRules => null!;
        public IEmployeeNotificationRepository EmployeeNotifications => null!;
        public INotificationTypeConfigRepository NotificationTypeConfigs => null!;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IUserParentAssignmentRepository UserParentAssignments => null!;
        public IInvitationRepository Invitations => null!;
        public IPayrollTierRepository PayrollTiers => null!;
        public ISeniorityStateVacancyConfigRepository    SeniorityStateVacancyConfigs => throw new NotImplementedException();
        public IPendingSeniorityStateChangeRepository    PendingSeniorityStateChanges => throw new NotImplementedException();
    }

    private sealed class FakePayrollUoWFactory(IEarningCodeRuleRepository earningCodeRules) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(
            OrchestrationUnitOfWorkOptions? options = null, CancellationToken ct = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(new FakePayrollUoW(earningCodeRules));
    }

    [Fact]
    public async Task Resolve_OffDayNotHoliday_ReturnsOT()
    {
        var rules = new List<EarningCodeRule>
        {
            EarningCodeRule.Create(ControlNumber.Create(1), 1, "IsOffDay=true,IsHoliday=false", "OT", false, true),
            EarningCodeRule.Create(ControlNumber.Create(1), 2, "IsOffDay=true,IsHoliday=true", "HO", false, true),
        };

        var resolver = new EarningCodeResolver(new FakePayrollUoWFactory(new FakeRuleRepo(rules)));
        var result = await resolver.ResolveAsync(ControlNumber.Create(1),
            new EarningContext(true, false, false, null, null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("OT", result!.ResultCode);
    }

    [Fact]
    public async Task Resolve_OffDayHoliday_ReturnsHO()
    {
        var rules = new List<EarningCodeRule>
        {
            EarningCodeRule.Create(ControlNumber.Create(1), 1, "IsOffDay=true,IsHoliday=false", "OT", false, true),
            EarningCodeRule.Create(ControlNumber.Create(1), 2, "IsOffDay=true,IsHoliday=true", "HO", false, true),
        };

        var resolver = new EarningCodeResolver(new FakePayrollUoWFactory(new FakeRuleRepo(rules)));
        var result = await resolver.ResolveAsync(ControlNumber.Create(1),
            new EarningContext(true, true, false, null, null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("HO", result!.ResultCode);
    }

    [Fact]
    public async Task Resolve_NoMatch_ReturnsNull()
    {
        var rules = new List<EarningCodeRule>
        {
            EarningCodeRule.Create(ControlNumber.Create(1), 1, "IsOffDay=true", "OT", false, true),
        };

        var resolver = new EarningCodeResolver(new FakePayrollUoWFactory(new FakeRuleRepo(rules)));
        var result = await resolver.ResolveAsync(ControlNumber.Create(1),
            new EarningContext(false, false, false, null, null), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
}

public class PayRateTests
{
    [Fact]
    public void CalculatePay_Regular_ReturnsBaseRate()
    {
        var rate = PayRate.Create(ControlNumber.Create(1), DateTime.UtcNow, 25m);
        Assert.Equal(200m, rate.CalculatePay(8m, false));
    }

    [Fact]
    public void CalculatePay_Overtime_AppliesMultiplier()
    {
        var rate = PayRate.Create(ControlNumber.Create(1), DateTime.UtcNow, 25m, 1.5m);
        Assert.Equal(75m, rate.CalculatePay(2m, true));
    }
}

public class EarningApprovalTests
{
    [Fact]
    public void Approve_SetsStatusAndTimestamp()
    {
        var approval = EarningApproval.Create(
            ControlNumber.Create(1), 1, ControlNumber.Create(99));
        approval.Approve();

        Assert.Equal("APPROVED", approval.Status);
        Assert.NotNull(approval.DecidedAtUtc);
    }

    [Fact]
    public void Decline_SetsStatusAndTimestamp()
    {
        var approval = EarningApproval.Create(
            ControlNumber.Create(1), 1, ControlNumber.Create(99));
        approval.Decline();

        Assert.Equal("DECLINED", approval.Status);
        Assert.NotNull(approval.DecidedAtUtc);
    }
}

public class PayrollRecordTests
{
    [Fact]
    public void SetEarningCode_SetsProperties()
    {
        var record = PayrollRecord.Create(1, 2, "REG", 200m, 8m);
        record.SetEarningCode("OT", true, ControlNumber.Create(100));

        Assert.Equal("OT", record.ResolvedEarningCode);
        Assert.True(record.RequiresApproval);
        Assert.Equal(100, record.OnDutyRecordCtrlNbr!.Value);
    }
}





