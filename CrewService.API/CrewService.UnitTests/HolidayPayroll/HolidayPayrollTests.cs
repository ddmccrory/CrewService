using CrewService.Application.HolidayManagement;
using CrewService.Application.Payroll;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.HolidayManagement;
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


using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Models.Seniority;
using Xunit;

namespace CrewService.UnitTests.HolidayPayroll;

public class HolidayTests
{
    [Fact]
    public void Create_SetsActiveByDefault()
    {
        var holiday = Holiday.Create(
            ControlNumber.Create(1), "July 4th", new DateOnly(2025, 7, 4));
        Assert.True(holiday.IsActive);
        Assert.Equal("July 4th", holiday.Name);
    }
}

public class HolidayQualificationServiceTests
{
    private sealed class FakeRuleRepo(List<HolidayQualificationRule> rules) : IHolidayQualificationRuleRepository
    {
        public Task<IReadOnlyList<HolidayQualificationRule>> GetByHolidayAsync(
            ControlNumber holidayCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<HolidayQualificationRule>>(rules);

        public Task<List<HolidayQualificationRule>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<HolidayQualificationRule>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<HolidayQualificationRule?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<HolidayQualificationRule?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(HolidayQualificationRule entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(HolidayQualificationRule entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public void Add(HolidayQualificationRule entity) => throw new NotImplementedException();
        public void Update(HolidayQualificationRule entity) => throw new NotImplementedException();
        public void Remove(HolidayQualificationRule entity) => throw new NotImplementedException();
    }

    private sealed class NullHolidayRepo : IHolidayRepository
    {
        public Task<IReadOnlyList<Holiday>> GetActiveByWorkAreaAsync(
            ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Holiday>>([]);

        public Task<List<Holiday>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Holiday>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Holiday?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Holiday?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(Holiday entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(Holiday entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public void Add(Holiday entity) => throw new NotImplementedException();
        public void Update(Holiday entity) => throw new NotImplementedException();
        public void Remove(Holiday entity) => throw new NotImplementedException();
    }

    private sealed class FakeHolidayQualUoW(
        IHolidayQualificationRuleRepository rules,
        IHolidayRepository holidays) : IOrchestrationUnitOfWork
    {
        public string CorrelationId => string.Empty;
        public string OrchestrationId => string.Empty;
        public IHolidayQualificationRuleRepository HolidayQualificationRules => rules;
        public IHolidayRepository Holidays => holidays;
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
        public ICallSheetRuleRepository CallSheetRules => null!;
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
        public IHolidayPayrollRecordRepository HolidayPayrollRecords => null!;
        public IEarningCodeRuleRepository EarningCodeRules => null!;
        public IPayRateRepository PayRates => null!;
        public IRailroadHolidaySelectionRepository RailroadHolidaySelections => null!;
        public IRoleRepository Roles => null!;
        public IFeatureRepository Features => null!;
        public IPermissionRepository Permissions => null!;
        public IPositionVacancyRepository PositionVacancies => null!;
        public IBulletinRepository Bulletins => null!;
        public IBulletinBidRepository BulletinBids => null!;
        public IBulletinRuleRepository BulletinRules => null!;
        public IEmployeeNotificationRepository EmployeeNotifications => null!;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IUserParentAssignmentRepository UserParentAssignments => null!;
        public IInvitationRepository Invitations => null!;
        public IPayrollTierRepository PayrollTiers => null!;
        public ISeniorityStateVacancyConfigRepository    SeniorityStateVacancyConfigs => throw new NotImplementedException();
        public IPendingSeniorityStateChangeRepository    PendingSeniorityStateChanges => throw new NotImplementedException();
    }

    private sealed class FakeHolidayQualUoWFactory(
        IHolidayQualificationRuleRepository rules,
        IHolidayRepository holidays) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(
            OrchestrationUnitOfWorkOptions? options = null, CancellationToken ct = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(new FakeHolidayQualUoW(rules, holidays));
    }

    private static HolidayQualificationService CreateHolidayQualSvc(
        IHolidayQualificationRuleRepository rules, IHolidayRepository holidays)
        => new(new FakeHolidayQualUoWFactory(rules, holidays));

    [Fact]
    public async Task NoRules_ReturnsQualified()
    {
        var service = CreateHolidayQualSvc(new FakeRuleRepo([]), new NullHolidayRepo());
        var result = await service.EvaluateAsync(ControlNumber.Create(1),
            new HolidayQualificationContext(ControlNumber.Create(10), true, true, null, null), TestContext.Current.CancellationToken);
        Assert.True(result.IsQualified);
    }

    [Fact]
    public async Task WorkedDayBefore_Passes()
    {
        var rules = new List<HolidayQualificationRule>
        {
            HolidayQualificationRule.Create(ControlNumber.Create(1), true, false),
        };
        var service = CreateHolidayQualSvc(new FakeRuleRepo(rules), new NullHolidayRepo());
        var result = await service.EvaluateAsync(ControlNumber.Create(1),
            new HolidayQualificationContext(ControlNumber.Create(10), true, false, null, null), TestContext.Current.CancellationToken);
        Assert.True(result.IsQualified);
    }

    [Fact]
    public async Task DidNotWorkDayBefore_Fails()
    {
        var rules = new List<HolidayQualificationRule>
        {
            HolidayQualificationRule.Create(ControlNumber.Create(1), true, false),
        };
        var service = CreateHolidayQualSvc(new FakeRuleRepo(rules), new NullHolidayRepo());
        var result = await service.EvaluateAsync(ControlNumber.Create(1),
            new HolidayQualificationContext(ControlNumber.Create(10), false, false, "V1", null), TestContext.Current.CancellationToken);
        Assert.False(result.IsQualified);
        Assert.Equal("Did not work day before", result.DisqualificationReason);
    }

    [Fact]
    public async Task DidNotWorkDayBefore_ExemptCode_Passes()
    {
        var rules = new List<HolidayQualificationRule>
        {
            HolidayQualificationRule.Create(ControlNumber.Create(1), true, false, exemptAbsenceCodes: "[\"V1\"]"),
        };
        var service = CreateHolidayQualSvc(new FakeRuleRepo(rules), new NullHolidayRepo());
        var result = await service.EvaluateAsync(ControlNumber.Create(1),
            new HolidayQualificationContext(ControlNumber.Create(10), false, false, "V1", null), TestContext.Current.CancellationToken);
        Assert.True(result.IsQualified);
    }

    [Fact]
    public async Task DidNotWorkDayAfter_Fails()
    {
        var rules = new List<HolidayQualificationRule>
        {
            HolidayQualificationRule.Create(ControlNumber.Create(1), false, true),
        };
        var service = CreateHolidayQualSvc(new FakeRuleRepo(rules), new NullHolidayRepo());
        var result = await service.EvaluateAsync(ControlNumber.Create(1),
            new HolidayQualificationContext(ControlNumber.Create(10), true, false, null, "NR"), TestContext.Current.CancellationToken);
        Assert.False(result.IsQualified);
        Assert.Equal("Did not work day after", result.DisqualificationReason);
    }
}

public class HolidayPayrollRecordTests
{
    [Fact]
    public void Create_Qualified_NoReason()
    {
        var record = HolidayPayrollRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), true);
        Assert.True(record.IsQualified);
        Assert.Null(record.DisqualificationReason);
    }

    [Fact]
    public void Create_Disqualified_HasReason()
    {
        var record = HolidayPayrollRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), false, "Did not work day before");
        Assert.False(record.IsQualified);
        Assert.Equal("Did not work day before", record.DisqualificationReason);
    }
}

public class UsHolidayCatalogTests
{
    [Fact]
    public void All_Contains11Holidays()
    {
        Assert.Equal(16, UsHolidayCatalog.All.Count);
    }

    [Fact]
    public void GetByCode_ReturnsMatch()
    {
        var holiday = UsHolidayCatalog.GetByCode("INDEPENDENCE");
        Assert.NotNull(holiday);
        Assert.Equal("Independence Day", holiday!.Name);
    }

    [Fact]
    public void GetByCode_Invalid_ReturnsNull()
    {
        Assert.Null(UsHolidayCatalog.GetByCode("FAKE"));
    }

    [Fact]
    public void IndependenceDay_ResolvesJuly4()
    {
        var holiday = UsHolidayCatalog.GetByCode("INDEPENDENCE")!;
        Assert.Equal(new DateOnly(2025, 7, 4), holiday.DateResolver(2025));
    }

    [Fact]
    public void MemorialDay_LastMondayInMay()
    {
        var holiday = UsHolidayCatalog.GetByCode("MEMORIAL")!;
        var date = holiday.DateResolver(2025);
        Assert.Equal(DayOfWeek.Monday, date.DayOfWeek);
        Assert.Equal(5, date.Month);
    }

    [Fact]
    public void Thanksgiving_FourthThursdayInNovember()
    {
        var holiday = UsHolidayCatalog.GetByCode("THANKSGIVING")!;
        var date = holiday.DateResolver(2025);
        Assert.Equal(DayOfWeek.Thursday, date.DayOfWeek);
        Assert.Equal(11, date.Month);
    }

    [Fact]
    public void Easter2025_April20()
    {
        var holiday = UsHolidayCatalog.GetByCode("EASTER")!;
        Assert.Equal(new DateOnly(2025, 4, 20), holiday.DateResolver(2025));
    }

    [Fact]
    public void GoodFriday2025_April18()
    {
        var holiday = UsHolidayCatalog.GetByCode("GOOD_FRIDAY")!;
        Assert.Equal(new DateOnly(2025, 4, 18), holiday.DateResolver(2025));
    }

    [Fact]
    public void Easter2026_April5()
    {
        var holiday = UsHolidayCatalog.GetByCode("EASTER")!;
        Assert.Equal(new DateOnly(2026, 4, 5), holiday.DateResolver(2026));
    }

    [Fact]
    public void DayAfterThanksgiving_IsFriday()
    {
        var holiday = UsHolidayCatalog.GetByCode("DAY_AFTER_THANKSGIVING")!;
        var date = holiday.DateResolver(2025);
        Assert.Equal(DayOfWeek.Friday, date.DayOfWeek);
        Assert.Equal(11, date.Month);
    }

    [Fact]
    public void ChristmasEve_December24()
    {
        var holiday = UsHolidayCatalog.GetByCode("CHRISTMAS_EVE")!;
        Assert.Equal(new DateOnly(2025, 12, 24), holiday.DateResolver(2025));
    }

    [Fact]
    public void NewYearsEve_December31()
    {
        var holiday = UsHolidayCatalog.GetByCode("NEW_YEARS_EVE")!;
        Assert.Equal(new DateOnly(2025, 12, 31), holiday.DateResolver(2025));
    }
}

public class RailroadHolidaySelectionTests
{
    [Fact]
    public void Create_ActiveByDefault()
    {
        var selection = RailroadHolidaySelection.Create(ControlNumber.Create(1), "CHRISTMAS");
        Assert.True(selection.IsActive);
        Assert.Equal("CHRISTMAS", selection.HolidayCode);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var selection = RailroadHolidaySelection.Create(ControlNumber.Create(1), "CHRISTMAS");
        selection.Deactivate();
        Assert.False(selection.IsActive);
    }

    [Fact]
    public void Activate_SetsActive()
    {
        var selection = RailroadHolidaySelection.Create(ControlNumber.Create(1), "CHRISTMAS", false);
        selection.Activate();
        Assert.True(selection.IsActive);
    }
}

public class HolidayAutoGenerationServiceTests
{
    private sealed class FakeSelectionRepo(List<RailroadHolidaySelection> selections) : IRailroadHolidaySelectionRepository
    {
        public Task<IReadOnlyList<RailroadHolidaySelection>> GetActiveByWorkAreaAsync(
            ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RailroadHolidaySelection>>(
                [.. selections.Where(s => s.WorkAreaGroupCtrlNbr.Value == workAreaGroupCtrlNbr.Value)]);

        public Task<bool> HasOwnSelectionsAsync(
            ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(selections.Any(s => s.WorkAreaGroupCtrlNbr.Value == workAreaGroupCtrlNbr.Value));

        public Task<List<RailroadHolidaySelection>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<RailroadHolidaySelection>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<RailroadHolidaySelection?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<RailroadHolidaySelection?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(RailroadHolidaySelection entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(RailroadHolidaySelection entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public void Add(RailroadHolidaySelection entity) => throw new NotImplementedException();
        public void Update(RailroadHolidaySelection entity) => throw new NotImplementedException();
        public void Remove(RailroadHolidaySelection entity) => throw new NotImplementedException();
    }

    private sealed class FakeHolidayRepo(List<Holiday> holidays) : IHolidayRepository
    {
        public Task<IReadOnlyList<Holiday>> GetActiveByWorkAreaAsync(
            ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Holiday>>(holidays);

        public Task<List<Holiday>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Holiday>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Holiday?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Holiday?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(Holiday entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(Holiday entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public void Add(Holiday entity) => throw new NotImplementedException();
        public void Update(Holiday entity) => throw new NotImplementedException();
        public void Remove(Holiday entity) => throw new NotImplementedException();
    }

    private sealed class FakeAutoGenUoW(
        IRailroadHolidaySelectionRepository selections,
        IHolidayRepository holidays) : IOrchestrationUnitOfWork
    {
        public string CorrelationId => string.Empty;
        public string OrchestrationId => string.Empty;
        public IRailroadHolidaySelectionRepository RailroadHolidaySelections => selections;
        public IHolidayRepository Holidays => holidays;
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
        public IEmployeeRepository Employees => null!;
        public IParentRepository Parents => null!;
        public IEmailAddressRepository EmailAddresses => null!;
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
        public ICallSheetRuleRepository CallSheetRules => null!;
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
        public IHolidayQualificationRuleRepository HolidayQualificationRules => null!;
        public IHolidayPayrollRecordRepository HolidayPayrollRecords => null!;
        public IEarningCodeRuleRepository EarningCodeRules => null!;
        public IPayRateRepository PayRates => null!;
        public IRoleRepository Roles => null!;
        public IFeatureRepository Features => null!;
        public IPermissionRepository Permissions => null!;
        public IPositionVacancyRepository PositionVacancies => null!;
        public IBulletinRepository Bulletins => null!;
        public IBulletinBidRepository BulletinBids => null!;
        public IBulletinRuleRepository BulletinRules => null!;
        public IEmployeeNotificationRepository EmployeeNotifications => null!;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IUserParentAssignmentRepository UserParentAssignments => null!;
        public IInvitationRepository Invitations => null!;
        public IPayrollTierRepository PayrollTiers => null!;
        public ISeniorityStateVacancyConfigRepository    SeniorityStateVacancyConfigs => throw new NotImplementedException();
        public IPendingSeniorityStateChangeRepository    PendingSeniorityStateChanges => throw new NotImplementedException();
    }

    private sealed class FakeAutoGenUoWFactory(
        IRailroadHolidaySelectionRepository selections,
        IHolidayRepository holidays) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(
            OrchestrationUnitOfWorkOptions? options = null, CancellationToken ct = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(new FakeAutoGenUoW(selections, holidays));
    }

    private static HolidayAutoGenerationService CreateAutoGenSvc(
        IRailroadHolidaySelectionRepository selections, IHolidayRepository holidays)
        => new(new FakeAutoGenUoWFactory(selections, holidays));

    [Fact]
    public async Task GeneratesHolidaysFromSelections()
    {
        var selections = new List<RailroadHolidaySelection>
        {
            RailroadHolidaySelection.Create(ControlNumber.Create(1), "CHRISTMAS"),
            RailroadHolidaySelection.Create(ControlNumber.Create(1), "NEW_YEAR"),
        };

        var service = CreateAutoGenSvc(new FakeSelectionRepo(selections), new FakeHolidayRepo([]));

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026, ct: TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, h => h.Name == "Christmas Day");
        Assert.Contains(result, h => h.Name == "New Year's Day");
    }

    [Fact]
    public async Task SkipsAlreadyExisting()
    {
        var selections = new List<RailroadHolidaySelection>
        {
            RailroadHolidaySelection.Create(ControlNumber.Create(1), "CHRISTMAS"),
        };
        var existing = new List<Holiday>
        {
            Holiday.Create(ControlNumber.Create(1), "Christmas Day", new DateOnly(2026, 12, 25)),
        };

        var service = CreateAutoGenSvc(new FakeSelectionRepo(selections), new FakeHolidayRepo(existing));

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026, ct: TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }

    [Fact]
    public async Task WeekendHoliday_ShiftsToObservedDate()
    {
        // July 4, 2026 is a Saturday → observed Friday July 3
        var selections = new List<RailroadHolidaySelection>
        {
            RailroadHolidaySelection.Create(ControlNumber.Create(1), "INDEPENDENCE"),
        };

        var service = CreateAutoGenSvc(new FakeSelectionRepo(selections), new FakeHolidayRepo([]));

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026, ct: TestContext.Current.CancellationToken);
        Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 7, 3), result[0].ObservedDate);
    }

    [Fact]
    public async Task ChildInheritsFromParent_WhenNoOwnSelections()
    {
        // Parent (group 100) has selections, child (group 1) has none
        var selections = new List<RailroadHolidaySelection>
        {
            RailroadHolidaySelection.Create(ControlNumber.Create(100), "CHRISTMAS"),
            RailroadHolidaySelection.Create(ControlNumber.Create(100), "NEW_YEAR"),
        };

        var service = CreateAutoGenSvc(new FakeSelectionRepo(selections), new FakeHolidayRepo([]));

        var result = await service.GenerateForYearAsync(
            ControlNumber.Create(1), 2026, ControlNumber.Create(100), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, h => h.Name == "Christmas Day");
        Assert.Contains(result, h => h.Name == "New Year's Day");
    }

    [Fact]
    public async Task ChildOverridesParent_WhenOwnSelectionsExist()
    {
        // Parent (100) has 2 selections, child (1) has its own 1 selection
        var selections = new List<RailroadHolidaySelection>
        {
            RailroadHolidaySelection.Create(ControlNumber.Create(100), "CHRISTMAS"),
            RailroadHolidaySelection.Create(ControlNumber.Create(100), "NEW_YEAR"),
            RailroadHolidaySelection.Create(ControlNumber.Create(1), "INDEPENDENCE"),
        };

        var service = CreateAutoGenSvc(new FakeSelectionRepo(selections), new FakeHolidayRepo([]));

        var result = await service.GenerateForYearAsync(
            ControlNumber.Create(1), 2026, ControlNumber.Create(100), TestContext.Current.CancellationToken);

        // Child's own selection wins — only Independence Day, not parent's Christmas/New Year
        Assert.Single(result);
        Assert.Equal("Independence Day", result[0].Name);
    }

    [Fact]
    public async Task NoParent_NoSelections_ReturnsEmpty()
    {
        var service = CreateAutoGenSvc(new FakeSelectionRepo([]), new FakeHolidayRepo([]));

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026, ct: TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }
}







