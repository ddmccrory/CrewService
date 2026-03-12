# StrategicApplications Complete System Specification
# Spec_01: Architecture, Primary Keys, Pools, and Entity Reference
# Part 01 - Solution Architecture

## 1. Solution Overview

The **Strategic Applications** solution targets **.NET Framework 4.7.2** and consists of **6 projects**. All projects use **Entity Framework 6.4.4**.

## 2. Projects

| # | Project | Output Type | Description |
|---|---------|-------------|-------------|
| 1 | `StrategicApplications.csproj` | **Library** (ASP.NET MVC) | Main web application. Hosted in IIS/IIS Express. Uses TypeScript 3.9. |
| 2 | `SAClassLibrary.csproj` | **Library** (Class Library) | Shared data-access library with its own DbContext, models, and base classes. |
| 3 | `SADailyCallSheetService.csproj` | **WinExe** | Background service for daily call-sheet processing. |
| 4 | `SAImportPayrollService.csproj` | **WinExe** | Background service for payroll import processing. |
| 5 | `SAAtHocMessageService.csproj` | **WinExe** | Background service for ad-hoc messaging. |
| 6 | `RestartApplicationPool.csproj` | **Exe** (Console) | Utility to restart the IIS application pool. |

---

## 3. Database Contexts

### 3.1 `StrategicApplicationsContext`

- **File:** `StrategicApplications\Models\Context\StrategicApplicationsContext.cs`
- **Base class:** `IdentityDbContext<ApplicationUser>` (ASP.NET Identity)
- **Connection string name:** `"StrategicApplicationsDemoContext"` (bare string, not `name=`)
- **Database initializer:** `CreateDatabaseIfNotExists<StrategicApplicationsContext>` � creates DB on first use; never drops or migrates.

#### DbSet Listings (204 DbSets)

| # | DbSet Property | Entity Type |
|---|----------------|-------------|
| 1 | `ADPInterfaces` | `ADPInterface` |
| 2 | `Addresses` | `Address` |
| 3 | `Assignments` | `Assignment` |
| 4 | `AssignmentAbolishments` | `AssignmentAbolishment` |
| 5 | `AssignmentOnDutyDays` | `AssignmentOnDutyDay` |
| 6 | `AssignmentOnDutyTimes` | `AssignmentOnDutyTime` |
| 7 | `AssignmentTypes` | `AssignmentType` |
| 8 | `ChangeMoveOrBulletins` | `ChangeMoveOrBulletin` |
| 9 | `ChangeNotifications` | `ChangeNotification` |
| 10 | `Clients` | `Client` |
| 11 | `ClientRequirementEmployees` | `ClientRequirementEmployee` |
| 12 | `ClientRequirements` | `ClientRequirement` |
| 13 | `Crafts` | `Craft` |
| 14 | `CraftMarkOffAllowances` | `CraftMarkOffAllowance` |
| 15 | `CraftPersonalDays` | `CraftPersonalDays` |
| 16 | `CraftSickDays` | `CraftSickDays` |
| 17 | `CraftVacationDays` | `CraftVacationDays` |
| 18 | `CraftRequirementEmployees` | `CraftRequirementEmployee` |
| 19 | `CraftMarkOffCodes` | `CraftMarkOffCode` |
| 20 | `CraftApprovalOfficers` | `CraftApprovalOfficer` |
| 21 | `CraftRequirements` | `CraftRequirement` |
| 22 | `Crews` | `Crew` |
| 23 | `CrewAbolishments` | `CrewAbolishment` |
| 24 | `CrewAssignments` | `CrewAssignment` |
| 25 | `CrewOffDays` | `CrewOffDay` |
| 26 | `CrewPositions` | `CrewPosition` |
| 27 | `CrewPositionAlternatePositions` | `CrewPositionAlternatePosition` |
| 28 | `DailyAssignments` | `DailyAssignment` |
| 29 | `DailyAssignmentAFERecords` | `DailyAssignmentAFERecord` |
| 30 | `DailyAssignmentAnnulments` | `DailyAssignmentAnnulment` |
| 31 | `DailyAssignmentCrews` | `DailyAssignmentCrew` |
| 32 | `DailyAssignmentShiftCompletions` | `DailyAssignmentShiftCompletion` |
| 33 | `DailyAssignmentShifts` | `DailyAssignmentShift` |
| 34 | `DailyAssignmentRequests` | `DailyAssignmentRequest` |
| 35 | `DailyCrewHistories` | `DailyCrewHistory` |
| 36 | `DailyCrewPositions` | `DailyCrewPosition` |
| 37 | `DailyCrewPositionAnnulments` | `DailyCrewPositionAnnulment` |
| 38 | `DailyCrewPositionDoNotFills` | `DailyCrewPositionDoNotFill` |
| 39 | `DailyCrewPositionElectronicCallRecords` | `DailyCrewPositionElectronicCallRecord` |
| 40 | `DailyCrewPositionElectronicResponseRecords` | `DailyCrewPositionElectronicResponseRecord` |
| 41 | `DailyCrewPositionSkips` | `DailyCrewPositionSkip` |
| 42 | `DailyCrewPositionHistories` | `DailyCrewPositionHistory` |
| 43 | `DailyCrewPositionOnDutyRecords` | `DailyCrewPositionOnDutyRecord` |
| 44 | `DailyCrewPositionOnDutyFRARecords` | `DailyCrewPositionOnDutyFRARecord` |
| 45 | `DailyCrewPositionOnDutyPayrollRecords` | `DailyCrewPositionOnDutyPayrollRecord` |
| 46 | `DailyCrewPositionOnDutyRecordLateCalls` | `DailyCrewPositionOnDutyRecordLateCall` |
| 47 | `DailyCrewPositionOnDutyMarkOffRecords` | `DailyCrewPositionOnDutyMarkOffRecord` |
| 48 | `DailyCrewPositionOffDutyRecords` | `DailyCrewPositionOffDutyRecord` |
| 49 | `DailyCrewPositionVacancies` | `DailyCrewPositionVacancy` |
| 50 | `DailyCrewPositionVacancyEmployees` | `DailyCrewPositionVacancyEmployee` |
| 51 | `DailyExtraBoardMarkOffRecords` | `DailyExtraBoardMarkOffRecord` |
| 52 | `DailyFRACommingleRecords` | `DailyFRACommingleRecord` |
| 53 | `DailyFRADeadheadRecords` | `DailyFRADeadheadRecord` |
| 54 | `DailyShiftExtraBoards` | `DailyShiftExtraBoard` |
| 55 | `DailyShiftExtraBoardPositions` | `DailyShiftExtraBoardPosition` |
| 56 | `DailyShiftOvertimeBoards` | `DailyShiftOvertimeBoard` |
| 57 | `DailyShiftOvertimeBoardPositions` | `DailyShiftOvertimeBoardPosition` |
| 58 | `DailyShiftExtraBoardPostionPayrollRecords` | `DailyShiftExtraBoardPositionPayrollRecord` |
| 59 | `DailyShiftExtraBoardPostionAssignments` | `DailyShiftExtraBoardPositionAssignment` |
| 60 | `DailyOnDutyDidNotWorkRecords` | `DailyOnDutyDidNotWorkRecord` |
| 61 | `DailyOnDutyUnavailableRecords` | `DailyOnDutyUnavailableRecord` |
| 62 | `DailyOnDutyLocomotiveRecords` | `DailyOnDutyLocomotiveRecord` |
| 63 | `DailyOnDutyPayrollInformationRecords` | `DailyOnDutyPayrollInformation` |
| 64 | `DailyOnDutyRailroadMaterialRecords` | `DailyOnDutyRailroadMaterialRecord` |
| 65 | `DailyOnDutyAFEBillingRecords` | `DailyOnDutyAFEBillingRecord` |
| 66 | `DailyOnDutyMiscellaneousBillingRecords` | `DailyOnDutyMiscellaneousBillingRecord` |
| 67 | `DailyOnDutyZoneBillingRecords` | `DailyOnDutyZoneBillingRecord` |
| 68 | `DailyRailroadEmployeeStatusRecords` | `DailyRailroadEmployeeStatusRecord` |
| 69 | `DailyRailroadEmployeePositionRecords` | `DailyRailroadEmployeePositionRecord` |
| 70 | `DailyRailroadEmployeePositionPayrollRecords` | `DailyRailroadEmployeePositionPayrollRecord` |
| 71 | `DailyRailroadPositionOffDayRecords` | `DailyRailroadPositionOffDayRecord` |
| 72 | `DailyRailroadPositionOffDayEmployeeRecords` | `DailyRailroadPositionOffDayEmployeeRecord` |
| 73 | `DailyRosterBoardPostionHangoutRecords` | `DailyRosterBoardPositionHangoutRecord` |
| 74 | `DailyRailroadEmployeePositionMarkOffRecords` | `DailyRailroadEmployeePositionMarkOffRecord` |
| 75 | `DeletedRailroadPositions` | `DeletedRailroadPosition` |
| 76 | `Descriptions` | `Description` |
| 77 | `EarningsApprovalEmployees` | `EarningsApprovalEmployee` |
| 78 | `EarningsApprovalRequiredRecords` | `EarningsApprovalRequiredRecord` |
| 79 | `EarningsApprovalRecords` | `EarningsApprovalRecord` |
| 80 | `EarningsDeclanationRecords` | `EarningsDeclanationRecord` |
| 81 | `EmailAddresses` | `EmailAddress` |
| 82 | `Employees` | `Employee` |
| 83 | `EngineerJobCodes` | `EngineerJobCode` |
| 84 | `EngineerPayRates` | `EngineerPayRate` |
| 85 | `EngineerJobCodeDeletes` | `EngineerJobCodeDelete` |
| 86 | `EmployeePriorServiceCredits` | `EmployeePriorServiceCredit` |
| 87 | `EmploymentStatus` | `EmploymentStatus` |
| 88 | `EmploymentStatusHistory` | `EmploymentStatusHistory` |
| 89 | `FillVacancyLog` | `FillVacancyLog` |
| 90 | `HoldDownReleases` | `HoldDownRelease` |
| 91 | `HoldDowns` | `HoldDown` |
| 92 | `Holidays` | `Holiday` |
| 93 | `HolidayQualifyRecords` | `HolidayQualifyRecord` |
| 94 | `Locations` | `Location` |
| 95 | `LocomotiveInspectionRecords` | `LocomotiveInspectionRecord` |
| 96 | `MarkOffCodes` | `MarkOffCode` |
| 97 | `MarkOffCodeApprovalOfficers` | `MarkOffCodeApprovalOfficer` |
| 98 | `MarkOffPayrollCodes` | `MarkOffPayrollCode` |
| 99 | `MarkOffRecords` | `MarkOffRecord` |
| 100 | `MarkOffRequestRecords` | `MarkOffRequestRecord` |
| 101 | `MarkOffRequestMarkOffRecords` | `MarkOffRequestMarkOffRecord` |
| 102 | `MarkOffRequestApprovals` | `MarkOffRequestApproval` |
| 103 | `MarkOffRequestWaitListRecords` | `MarkOffRequestWaitListRecord` |
| 104 | `MarkOffRequestMarkOffRequestWaitListRecords` | `MarkOffRequestMarkOffRequestWaitListRecord` |
| 105 | `MarkOffRecordApprovals` | `MarkOffRecordApproval` |
| 106 | `MarkOffRecordDeletes` | `MarkOffRecordDelete` |
| 107 | `MarkOffRequestDeletes` | `MarkOffRequestDelete` |
| 108 | `MarkOffRequestTempRecords` | `MarkOffRequestTempRecord` |
| 109 | `MarkUpRecords` | `MarkUpRecord` |
| 110 | `MarkOffRequestMarkUpRecords` | `MarkOffRequestMarkUpRecord` |
| 111 | `MovedDailyCrewPositions` | `MovedDailyCrewPosition` |
| 112 | `ObjectNotes` | `ObjectNotes` |
| 113 | `OnDutyMoveCutOffTimes` | `OnDutyMoveCutOffTime` |
| 114 | `PayRates` | `PayRate` |
| 115 | `PayrollCodes` | `PayrollCode` |
| 116 | `PayrollCodePayRates` | `PayrollCodePayRate` |
| 117 | `PayrollCategories` | `PayrollCategory` |
| 118 | `PayrollCategoryCodes` | `PayrollCategoryCode` |
| 119 | `PayrollReportGroups` | `PayrollReportGroup` |
| 120 | `PayrollReportGroupCategories` | `PayrollReportGroupCategory` |
| 121 | `PayrollCodeApprovalRoles` | `PayrollCodeApprovalRole` |
| 122 | `PayrollCrewPositionAutoPayRecords` | `PayrollCrewPositionAutoPayRecord` |
| 123 | `PayrollRecords` | `PayrollRecord` |
| 124 | `PayrollReviewRecords` | `PayrollReviewRecord` |
| 125 | `PayrollEarningProcessedRecords` | `PayrollEarningProcessedRecord` |
| 126 | `PayrollPeriodProcessRecords` | `PayrollPeriodProcessRecord` |
| 127 | `PayrollRecordDeletes` | `PayrollRecordDelete` |
| 128 | `PayrollReviewRequiredRecords` | `PayrollReviewRequiredRecord` |
| 129 | `PayrollEarningRecords` | `PayrollEarningRecord` |
| 130 | `PayrollHolidayRecords` | `PayrollHolidayRecord` |
| 131 | `PayrollHolidayRecordPayrollRecords` | `PayrollHolidayRecordPayrollRecord` |
| 132 | `PhoneNumbers` | `PhoneNumber` |
| 133 | `Positions` | `Position` |
| 134 | `PositionPayRates` | `PositionPayRate` |
| 135 | `PositionAlternateSupervisors` | `PositionAlternateSupervisor` |
| 136 | `PositionRequirementEmployees` | `PositionRequirementEmployee` |
| 137 | `PositionRequirements` | `PositionRequirement` |
| 138 | `Qualifications` | `Qualification` |
| 139 | `Railroads` | `Railroad` |
| 140 | `RailroadAFEs` | `RailroadAFE` |
| 141 | `RailroadInformationRecords` | `RailroadInformationRecord` |
| 142 | `RailroadInformationCancelRecords` | `RailroadInformationCancelRecord` |
| 143 | `RailroadInformationCompleteRecords` | `RailroadInformationCloseRecord` |
| 144 | `RailroadInformationDeleteRecords` | `RailroadInformationDeleteRecord` |
| 145 | `RailroadInformationPublishRecords` | `RailroadInformationPublishRecord` |
| 146 | `RailroadInformationTypes` | `RailroadInformationType` |
| 147 | `RailroadInformationReadbyEmployeeRecords` | `RailroadInformationReadbyEmployeeRecord` |
| 148 | `RailroadEmployees` | `RailroadEmployee` |
| 149 | `RailroadEmployeeCalendarRequests` | `RailroadEmployeeCalendarRequest` |
| 150 | `RailroadEmployeeCompensableTimeRecords` | `RailroadEmployeeCompensableTimeRecord` |
| 151 | `RailroadEmployeeVacationOneDayTimeRecords` | `RailroadEmployeeVacationOneDayTimeRecord` |
| 152 | `RailroadEmployeeVacationRequests` | `RailroadEmployeeVacationRequest` |
| 153 | `RailroadEmployeeVacationRequestAssignments` | `RailroadEmployeeVacationRequestAssignment` |
| 154 | `RailroadLocomotiveTypes` | `RailroadLocomotiveType` |
| 155 | `RailroadMaterialCategories` | `RailroadMaterialCategory` |
| 156 | `RailroadMaterials` | `RailroadMaterial` |
| 157 | `RailroadWorkCodes` | `RailroadWorkCode` |
| 158 | `RailroadZones` | `RailroadZone` |
| 159 | `RailroadLocations` | `RailroadLocation` |
| 160 | `RailroadPayrollDepartments` | `RailroadPayrollDepartment` |
| 161 | `RailroadPools` | `RailroadPool` |
| 162 | `RailroadPoolEmployees` | `RailroadPoolEmployee` |
| 163 | `RailroadPoolEmployeePositions` | `RailroadPoolEmployeePosition` |
| 164 | `RailroadPoolEmployeePositionHistory` | `RailroadPoolEmployeePositionHistory` |
| 165 | `RailroadPoolEmployeeTrainingDates` | `RailroadPoolEmployeeTrainingDate` |
| 166 | `RailroadPoolMarkOffAllowances` | `RailroadPoolMarkOffAllowance` |
| 167 | `RailroadPoolPayrollTiers` | `RailroadPoolPayrollTier` |
| 168 | `RailroadPoolRequirementEmployees` | `RailroadPoolRequirementEmployee` |
| 169 | `RailroadPoolRequirements` | `RailroadPoolRequirement` |
| 170 | `RailroadPositions` | `RailroadPosition` |
| 171 | `RailroadPositionBulletinAssignments` | `RailroadPositionBulletinAssignment` |
| 172 | `RailroadPositionBulletinBids` | `RailroadPositionBulletinBid` |
| 173 | `RailroadPositionBulletinBidAssignments` | `RailroadPositionBulletinBidAssignment` |
| 174 | `RailroadPositionBulletinNoBids` | `RailroadPositionBulletinNoBid` |
| 175 | `RailroadPositionBulletins` | `RailroadPositionBulletin` |
| 176 | `RailroadPositionChanges` | `RailroadPositionChange` |
| 177 | `RailroadPositionChangeRailroadInformationRecords` | `RailroadPositionChangeRailroadInformationRecord` |
| 178 | `RailroadRequirementEmployees` | `RailroadRequirementEmployee` |
| 179 | `RailroadRequirements` | `RailroadRequirement` |
| 180 | `RefreshRates` | `RefreshRate` |
| 181 | `RemovedRailroadPoolEmployees` | `RemovedRailroadPoolEmployee` |
| 182 | `Requirements` | `Requirement` |
| 183 | `RequirementDeletes` | `RequirementDelete` |
| 184 | `RosterBoardPositions` | `RosterBoardPosition` |
| 185 | `RosterBoards` | `RosterBoard` |
| 186 | `RosterBulletinRules` | `RosterBulletinRule` |
| 187 | `Rosters` | `Roster` |
| 188 | `RosterSeniorityMoveRules` | `RosterSeniorityMoveRule` |
| 189 | `Seniority` | `Seniority` |
| 190 | `SeniorityEndDate` | `SeniorityEndDate` |
| 191 | `SeniorityMoveAssignments` | `SeniorityMoveAssignment` |
| 192 | `SeniorityMoves` | `SeniorityMove` |
| 193 | `SeniorityMoveWillWork` | `SeniorityMoveWillWork` |
| 194 | `SeniorityStates` | `SeniorityState` |
| 195 | `Shifts` | `Shift` |
| 196 | `TemporaryAssignmentAssignedEmployees` | `TemporaryAssignmentAssignedEmployee` |
| 197 | `TemporaryAssignmentReleases` | `TemporaryAssignmentRelease` |
| 198 | `TemporaryAssignmentWorkDays` | `TemporaryAssignmentWorkDay` |
| 199 | `TemporaryAssignments` | `TemporaryAssignment` |
| 200 | `TemporaryAssignmentAFERecords` | `TemporaryAssignmentAFERecord` |
| 201 | `UserRoles` | `IdentityUserRole` |
| 202 | `UKGInterfaces` | `UKGInterface` |
| 203 | `UserLoginRecords` | `UserLoginRecord` |
| 204 | `WeekDays` | `WeekDay` |

> The inherited `IdentityDbContext<ApplicationUser>` base class also provides the standard ASP.NET Identity tables: `Users`, `Roles`, `UserClaims`, `UserLogins`, and `UserRoles`.

#### SaveChanges Override Behavior

**`SaveChanges()` (synchronous):**

1. Calls `base.SaveChanges()`.
2. On `DbUpdateConcurrencyException`: logs via `EventLogger`, reloads the conflicting entity with `ex.Entries.Single().Reload()`, then retries `base.SaveChanges()`.
3. On `DbEntityValidationException`: iterates every validation error, writes each to `Trace.TraceInformation` (format: `"Class: {FullName}, Property: {PropertyName}, Error: {ErrorMessage}"`) and to `EventLogger.WriteErrorLogEvent`, then **re-throws**.

**`SaveChangesAsync(CancellationToken)` (asynchronous):**

1. Calls `base.SaveChangesAsync(cancellationToken)`.
2. On `DbEntityValidationException`: same trace + event-log loop as the sync version, then **re-throws**. No concurrency retry in the async path.

---

### 3.2 `SAClassLibraryContext`

- **File:** `SAClassLibrary\Context\SAClassLibraryContext.cs`
- **Base class:** `DbContext` (plain EF6, no Identity)
- **Connection string name:** `"name=SAClassLibraryDemoContext"` (uses the `name=` prefix syntax)
- **Database initializer:** `null` � explicitly disabled. The context assumes the database already exists; never creates, drops, or migrates.
- **Declared as:** `partial class` (allows extension in additional files)
- **DbSet modifier:** All DbSets are `virtual` (except `UKGInterfaces`), enabling mocking/proxying.
- **Fluent API:** Contains an extensive `OnModelCreating` override with relationship configuration, cascade-delete rules, and many-to-many join-table mappings.

#### DbSet Listings (215 DbSets)

| # | DbSet Property | Entity Type |
|---|----------------|-------------|
| 1 | `Addresses` | `Address` |
| 2 | `ADPInterfaces` | `ADPInterface` |
| 3 | `AspNetRoles` | `AspNetRole` |
| 4 | `AspNetUserClaims` | `AspNetUserClaim` |
| 5 | `AspNetUserLogins` | `AspNetUserLogin` |
| 6 | `AspNetUsers` | `AspNetUser` |
| 7 | `AssignmentAbolishments` | `AssignmentAbolishment` |
| 8 | `AssignmentOnDutyDays` | `AssignmentOnDutyDay` |
| 9 | `AssignmentOnDutyTimes` | `AssignmentOnDutyTime` |
| 10 | `Assignments` | `Assignment` |
| 11 | `AssignmentTypes` | `AssignmentType` |
| 12 | `BeSafeCategories` | `BeSafeCategory` |
| 13 | `BeSafeEmailGroups` | `BeSafeEmailGroup` |
| 14 | `BeSafeRecords` | `BeSafeRecord` |
| 15 | `BeSafeAreas` | `BeSafeArea` |
| 16 | `BeSafeActionRecords` | `BeSafeActionRecord` |
| 17 | `BeSafeChangeRecords` | `BeSafeChangeRecord` |
| 18 | `BeSafeResolveRecords` | `BeSafeResolveRecord` |
| 19 | `BeSafeSubdivisions` | `BeSafeSubdivision` |
| 20 | `BeSafeDeleteRecords` | `BeSafeDeleteRecord` |
| 21 | `ChangeMoveOrBulletins` | `ChangeMoveOrBulletin` |
| 22 | `ChangeNotifications` | `ChangeNotification` |
| 23 | `ClientRequirementEmployees` | `ClientRequirementEmployee` |
| 24 | `ClientRequirements` | `ClientRequirement` |
| 25 | `Clients` | `Client` |
| 26 | `CraftApprovalOfficers` | `CraftApprovalOfficer` |
| 27 | `CraftMarkOffAllowances` | `CraftMarkOffAllowance` |
| 28 | `CraftMarkOffCodes` | `CraftMarkOffCode` |
| 29 | `CraftPayCodes` | `CraftPayCode` |
| 30 | `CraftPersonalDays` | `CraftPersonalDay` |
| 31 | `CraftRequirementEmployees` | `CraftRequirementEmployee` |
| 32 | `Crafts` | `Craft` |
| 33 | `CraftSickDays` | `CraftSickDay` |
| 34 | `CraftVacationDays` | `CraftVacationDay` |
| 35 | `CrewAbolishments` | `CrewAbolishment` |
| 36 | `CrewAssignments` | `CrewAssignment` |
| 37 | `CrewPositionAlternatePositions` | `CrewPositionAlternatePosition` |
| 38 | `CrewPositions` | `CrewPosition` |
| 39 | `Crews` | `Crew` |
| 40 | `DailyAssignmentAFERecords` | `DailyAssignmentAFERecord` |
| 41 | `DailyAssignmentAnnulments` | `DailyAssignmentAnnulment` |
| 42 | `DailyAssignmentRequests` | `DailyAssignmentRequest` |
| 43 | `DailyAssignments` | `DailyAssignment` |
| 44 | `DailyAssignmentCrews` | `DailyAssignmentCrew` |
| 45 | `DailyAssignmentShiftCompletions` | `DailyAssignmentShiftCompletion` |
| 46 | `DailyAssignmentShifts` | `DailyAssignmentShift` |
| 47 | `DailyCrewHistories` | `DailyCrewHistory` |
| 48 | `DailyCrewPositionAnnulments` | `DailyCrewPositionAnnulment` |
| 49 | `DailyCrewPositionDoNotFills` | `DailyCrewPositionDoNotFill` |
| 50 | `DailyCrewPositionElectronicCallRecords` | `DailyCrewPositionElectronicCallRecord` |
| 51 | `DailyCrewPositionElectronicResponseRecords` | `DailyCrewPositionElectronicResponseRecord` |
| 52 | `DailyCrewPositionHistories` | `DailyCrewPositionHistory` |
| 53 | `DailyCrewPositionOffDutyRecords` | `DailyCrewPositionOffDutyRecord` |
| 54 | `DailyCrewPositionOnDutyFRARecords` | `DailyCrewPositionOnDutyFRARecord` |
| 55 | `DailyCrewPositionOnDutyRecordLateCalls` | `DailyCrewPositionOnDutyRecordLateCall` |
| 56 | `DailyCrewPositionOnDutyMarkOffRecords` | `DailyCrewPositionOnDutyMarkOffRecord` |
| 57 | `DailyCrewPositionOnDutyRecords` | `DailyCrewPositionOnDutyRecord` |
| 58 | `DailyCrewPositionOnDutyPayrollRecords` | `DailyCrewPositionOnDutyPayrollRecord` |
| 59 | `DailyCrewPositions` | `DailyCrewPosition` |
| 60 | `DailyCrewPositionSkips` | `DailyCrewPositionSkip` |
| 61 | `DailyCrewPositionVacancies` | `DailyCrewPositionVacancy` |
| 62 | `DailyCrewPositionVacancyEmployees` | `DailyCrewPositionVacancyEmployee` |
| 63 | `DailyExtraBoardMarkOffRecords` | `DailyExtraBoardMarkOffRecord` |
| 64 | `DailyFRACommingleRecords` | `DailyFRACommingleRecord` |
| 65 | `DailyFRADeadheadRecords` | `DailyFRADeadheadRecord` |
| 66 | `DailyOnDutyAFEBillingRecords` | `DailyOnDutyAFEBillingRecord` |
| 67 | `DailyOnDutyDidNotWorkRecords` | `DailyOnDutyDidNotWorkRecord` |
| 68 | `DailyOnDutyLocomotiveRecords` | `DailyOnDutyLocomotiveRecord` |
| 69 | `DailyOnDutyMiscellaneousBillingRecords` | `DailyOnDutyMiscellaneousBillingRecord` |
| 70 | `DailyOnDutyPayrollInformations` | `DailyOnDutyPayrollInformation` |
| 71 | `DailyOnDutyRailroadMaterialRecords` | `DailyOnDutyRailroadMaterialRecord` |
| 72 | `DailyOnDutyUnavailableRecords` | `DailyOnDutyUnavailableRecord` |
| 73 | `DailyOnDutyZoneBillingRecords` | `DailyOnDutyZoneBillingRecord` |
| 74 | `DailyRailroadEmployeePositionRecords` | `DailyRailroadEmployeePositionRecord` |
| 75 | `DailyRailroadEmployeeStatusRecords` | `DailyRailroadEmployeeStatusRecord` |
| 76 | `DailyRailroadPositionOffDayEmployeeRecords` | `DailyRailroadPositionOffDayEmployeeRecord` |
| 77 | `DailyRailroadPositionOffDayRecords` | `DailyRailroadPositionOffDayRecord` |
| 78 | `DailyRosterBoardPositionHangoutRecords` | `DailyRosterBoardPositionHangoutRecord` |
| 79 | `DailyRailroadEmployeePositionMarkOffRecords` | `DailyRailroadEmployeePositionMarkOffRecord` |
| 80 | `DailyShiftExtraBoardPositionAssignments` | `DailyShiftExtraBoardPositionAssignment` |
| 81 | `DailyShiftExtraBoardPositions` | `DailyShiftExtraBoardPosition` |
| 82 | `DailyShiftExtraBoards` | `DailyShiftExtraBoard` |
| 83 | `DailyShiftOvertimeBoardPositions` | `DailyShiftOvertimeBoardPosition` |
| 84 | `DailyShiftOvertimeBoards` | `DailyShiftOvertimeBoard` |
| 85 | `DeletedRailroadPositions` | `DeletedRailroadPosition` |
| 86 | `Descriptions` | `Description` |
| 87 | `EarningsApprovalRecords` | `EarningsApprovalRecord` |
| 88 | `EarningsApprovalRequiredRecords` | `EarningsApprovalRequiredRecord` |
| 89 | `EarningsDeclanationRecords` | `EarningsDeclanationRecord` |
| 90 | `EmailAddresses` | `EmailAddress` |
| 91 | `EmployeePriorServiceCredits` | `EmployeePriorServiceCredit` |
| 92 | `Employees` | `Employee` |
| 93 | `EmploymentStatus` | `EmploymentStatus` |
| 94 | `EmploymentStatusHistories` | `EmploymentStatusHistory` |
| 95 | `EngineerJobCodeDeletes` | `EngineerJobCodeDelete` |
| 96 | `EngineerJobCodes` | `EngineerJobCode` |
| 97 | `EngineerPayRates` | `EngineerPayRate` |
| 98 | `FillVacancyLogs` | `FillVacancyLog` |
| 99 | `HoldDownReleases` | `HoldDownRelease` |
| 100 | `HoldDowns` | `HoldDown` |
| 101 | `HolidayQualifyRecords` | `HolidayQualifyRecord` |
| 102 | `Holidays` | `Holiday` |
| 103 | `Locations` | `Location` |
| 104 | `LocomotiveInspectionRecords` | `LocomotiveInspectionRecord` |
| 105 | `MarkOffCodeApprovalOfficers` | `MarkOffCodeApprovalOfficer` |
| 106 | `MarkOffCodes` | `MarkOffCode` |
| 107 | `MarkOffMarkUpHours` | `MarkOffMarkUpHours` |
| 108 | `MarkOffPayrollCodes` | `MarkOffPayrollCode` |
| 109 | `MarkOffRecordApprovals` | `MarkOffRecordApproval` |
| 110 | `MarkOffRecordDeletes` | `MarkOffRecordDelete` |
| 111 | `MarkOffRecords` | `MarkOffRecord` |
| 112 | `MarkOffRequestApprovals` | `MarkOffRequestApproval` |
| 113 | `MarkOffRequestDeletes` | `MarkOffRequestDelete` |
| 114 | `MarkOffRequestMarkOffRecords` | `MarkOffRequestMarkOffRecord` |
| 115 | `MarkOffRequestMarkUpRecords` | `MarkOffRequestMarkUpRecord` |
| 116 | `MarkOffRequestRecords` | `MarkOffRequestRecord` |
| 117 | `MarkOffRequestTempRecords` | `MarkOffRequestTempRecord` |
| 118 | `MarkOffRequestWaitListRecords` | `MarkOffRequestWaitListRecord` |
| 119 | `MarkUpRecords` | `MarkUpRecord` |
| 120 | `MovedDailyCrewPositions` | `MovedDailyCrewPosition` |
| 121 | `ObjectNotes` | `ObjectNote` |
| 122 | `OffPropertyTieUpRecords` | `OffPropertyTieUpRecord` |
| 123 | `OnDutyMoveCutOffTimes` | `OnDutyMoveCutOffTime` |
| 124 | `PayRates` | `PayRate` |
| 125 | `PayrollCategories` | `PayrollCategory` |
| 126 | `PayrollCodeApprovalRoles` | `PayrollCodeApprovalRole` |
| 127 | `PayrollCodePayRates` | `PayrollCodePayRate` |
| 128 | `PayrollCodes` | `PayrollCode` |
| 129 | `PayrollCrewPositionAutoPayRecords` | `PayrollCrewPositionAutoPayRecord` |
| 130 | `PayrollEarningProcessedRecords` | `PayrollEarningProcessedRecord` |
| 131 | `PayrollEarningRecords` | `PayrollEarningRecord` |
| 132 | `PayrollHolidayRecords` | `PayrollHolidayRecord` |
| 133 | `PayrollPeriodProcessRecords` | `PayrollPeriodProcessRecord` |
| 134 | `PayrollRecordDeletes` | `PayrollRecordDelete` |
| 135 | `PayrollRecords` | `PayrollRecord` |
| 136 | `PayrollReportGroups` | `PayrollReportGroup` |
| 137 | `PayrollReviewRecords` | `PayrollReviewRecord` |
| 138 | `PayrollReviewRequiredRecords` | `PayrollReviewRequiredRecord` |
| 139 | `PhoneNumbers` | `PhoneNumber` |
| 140 | `PositionAlternateSupervisors` | `PositionAlternateSupervisor` |
| 141 | `PositionPayRates` | `PositionPayRate` |
| 142 | `PositionRequirementEmployees` | `PositionRequirementEmployee` |
| 143 | `PositionRequirements` | `PositionRequirement` |
| 144 | `Positions` | `Position` |
| 145 | `Qualifications` | `Qualification` |
| 146 | `RailroadAFEs` | `RailroadAFE` |
| 147 | `RailroadEmployeeCalendarRequests` | `RailroadEmployeeCalendarRequest` |
| 148 | `RailroadEmployeeCompensableTimeRecords` | `RailroadEmployeeCompensableTimeRecord` |
| 149 | `RailroadEmployeeReportViewedRecords` | `RailroadEmployeeReportViewedRecord` |
| 150 | `RailroadEmployees` | `RailroadEmployee` |
| 151 | `RailroadEmployeeVacationOneDayTimeRecords` | `RailroadEmployeeVacationOneDayTimeRecord` |
| 152 | `RailroadEmployeeVacationRequestAssignments` | `RailroadEmployeeVacationRequestAssignment` |
| 153 | `RailroadEmployeeVacationRequests` | `RailroadEmployeeVacationRequest` |
| 154 | `RailroadInformationRecords` | `RailroadInformationRecord` |
| 155 | `RailroadInformationCancelRecords` | `RailroadInformationCancelRecord` |
| 156 | `RailroadInformationCompleteRecords` | `RailroadInformationCloseRecord` |
| 157 | `RailroadInformationDeleteRecords` | `RailroadInformationDeleteRecord` |
| 158 | `RailroadInformationPublishRecords` | `RailroadInformationPublishRecord` |
| 159 | `RailroadInformationTypes` | `RailroadInformationType` |
| 160 | `RailroadInformationReadbyEmployeeRecords` | `RailroadInformationReadbyEmployeeRecord` |
| 161 | `RailroadLocations` | `RailroadLocation` |
| 162 | `RailroadLocomotiveTypes` | `RailroadLocomotiveType` |
| 163 | `RailroadMaterialCategories` | `RailroadMaterialCategory` |
| 164 | `RailroadMaterials` | `RailroadMaterial` |
| 165 | `RailroadPayrollDepartments` | `RailroadPayrollDepartment` |
| 166 | `RailroadPoolEmployeeBulletinsViewedRecords` | `RailroadPoolEmployeeBulletinsViewedRecord` |
| 167 | `RailroadPoolEmployeePositionHistories` | `RailroadPoolEmployeePositionHistory` |
| 168 | `RailroadPoolEmployeePositions` | `RailroadPoolEmployeePosition` |
| 169 | `RailroadPoolEmployees` | `RailroadPoolEmployee` |
| 170 | `RailroadPoolEmployeeTrainingDates` | `RailroadPoolEmployeeTrainingDate` |
| 171 | `RailroadPoolMarkOffAllowances` | `RailroadPoolMarkOffAllowance` |
| 172 | `RailroadPoolRequirementEmployees` | `RailroadPoolRequirementEmployee` |
| 173 | `RailroadPoolRequirements` | `RailroadPoolRequirement` |
| 174 | `RailroadPools` | `RailroadPool` |
| 175 | `RailroadPositionBulletinAssignments` | `RailroadPositionBulletinAssignment` |
| 176 | `RailroadPositionBulletinBidAssignments` | `RailroadPositionBulletinBidAssignment` |
| 177 | `RailroadPositionBulletinBids` | `RailroadPositionBulletinBid` |
| 178 | `RailroadPositionBulletinNoBids` | `RailroadPositionBulletinNoBid` |
| 179 | `RailroadPositionBulletins` | `RailroadPositionBulletin` |
| 180 | `RailroadPositionChanges` | `RailroadPositionChange` |
| 181 | `RailroadPositions` | `RailroadPosition` |
| 182 | `RailroadRequirementEmployees` | `RailroadRequirementEmployee` |
| 183 | `RailroadRequirements` | `RailroadRequirement` |
| 184 | `Railroads` | `Railroad` |
| 185 | `RailroadWorkCodes` | `RailroadWorkCode` |
| 186 | `RailroadZones` | `RailroadZone` |
| 187 | `RefreshRates` | `RefreshRate` |
| 188 | `RemovedRailroadPoolEmployees` | `RemovedRailroadPoolEmployee` |
| 189 | `RequirementDeletes` | `RequirementDelete` |
| 190 | `Requirements` | `Requirement` |
| 191 | `RosterBoardPositions` | `RosterBoardPosition` |
| 192 | `RosterBoards` | `RosterBoard` |
| 193 | `RosterBulletinRules` | `RosterBulletinRule` |
| 194 | `Rosters` | `Roster` |
| 195 | `RosterSeniorityMoveRules` | `RosterSeniorityMoveRule` |
| 196 | `Seniorities` | `Seniority` |
| 197 | `SeniorityEndDates` | `SeniorityEndDate` |
| 198 | `SeniorityMoveAssignments` | `SeniorityMoveAssignment` |
| 199 | `SeniorityMoves` | `SeniorityMove` |
| 200 | `SeniorityMoveWillWorks` | `SeniorityMoveWillWork` |
| 201 | `SeniorityStates` | `SeniorityState` |
| 202 | `Shifts` | `Shift` |
| 203 | `SlowOrderAreas` | `SlowOrderArea` |
| 204 | `SlowOrderRecords` | `SlowOrderRecord` |
| 205 | `SlowOrderChangeRecords` | `SlowOrderChangeRecord` |
| 206 | `SlowOrderCompleteRecords` | `SlowOrderCompleteRecord` |
| 207 | `SlowOrderDeleteRecords` | `SlowOrderDeleteRecord` |
| 208 | `TemporaryAssignmentAFERecords` | `TemporaryAssignmentAFERecord` |
| 209 | `TemporaryAssignmentAssignedEmployees` | `TemporaryAssignmentAssignedEmployee` |
| 210 | `TemporaryAssignmentReleases` | `TemporaryAssignmentRelease` |
| 211 | `TemporaryAssignments` | `TemporaryAssignment` |
| 212 | `TemporaryAssignmentWorkDays` | `TemporaryAssignmentWorkDay` |
| 213 | `UKGInterfaces` | `UKGInterface` |
| 214 | `UserLoginRecords` | `UserLoginRecord` |
| 215 | `WeekDays` | `WeekDay` |

#### Entities Unique to `SAClassLibraryContext`

`AspNetRole`, `AspNetUserClaim`, `AspNetUserLogin`, `AspNetUser`, `BeSafeCategory`, `BeSafeEmailGroup`, `BeSafeRecord`, `BeSafeArea`, `BeSafeActionRecord`, `BeSafeChangeRecord`, `BeSafeResolveRecord`, `BeSafeSubdivision`, `BeSafeDeleteRecord`, `CraftPayCode`, `MarkOffMarkUpHours`, `OffPropertyTieUpRecord`, `RailroadEmployeeReportViewedRecord`, `RailroadPoolEmployeeBulletinsViewedRecord`, `SlowOrderArea`, `SlowOrderRecord`, `SlowOrderChangeRecord`, `SlowOrderCompleteRecord`, `SlowOrderDeleteRecord`

#### Entities Unique to `StrategicApplicationsContext`

`CrewOffDay`, `CraftRequirement`, `EarningsApprovalEmployee`, `DailyRailroadEmployeePositionPayrollRecord`, `MarkOffRequestMarkOffRequestWaitListRecord`, `PayrollCategoryCode`, `PayrollReportGroupCategory`, `PayrollHolidayRecordPayrollRecord`, `RailroadPositionChangeRailroadInformationRecord`, `RailroadPoolPayrollTier`, `IdentityUserRole` (as `UserRoles`)

---

## 4. Authentication Configuration

**File:** `StrategicApplications\App_Start\Startup.Auth.cs`

The application uses **OWIN cookie authentication** configured via `Startup.ConfigureAuth(IAppBuilder)`:

| Setting | Value |
|---------|-------|
| Authentication type | `DefaultAuthenticationTypes.ApplicationCookie` |
| Cookie expiration | **480 minutes** (`TimeSpan.FromMinutes(480)`) � 8 hours |
| Login path | `/Account/Login` |
| External sign-in cookie | Enabled via `app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie)` |
| Third-party providers | All commented out (Microsoft, Twitter, Facebook, Google) |

---

## 5. ApplicationUser (Identity Model)

**File:** `StrategicApplications\Models\IdentityModels.cs`
**Base class:** `IdentityUser` (ASP.NET Identity)

### Persisted Properties

| Property | Data Type | Required | StringLength | Other Attributes |
|----------|-----------|----------|--------------|------------------|
| `EmployeeNumber` | `string` | Yes | 25 (min 4) | `[Display(Name = "Employee Number")]` |
| `FirstName` | `string` | Yes | 250 | `[Display(Name = "First Name")]` |
| `MiddleName` | `string` | No | 250 | `[Display(Name = "Middle Name")]` |
| `LastName` | `string` | Yes | 250 | `[Display(Name = "Last Name")]` |
| `ThemeFile` | `string` | Yes | 100 | � |
| `LastLogin` | `DateTime` | Yes | � | `[DataType(DataType.DateTime)]`, `[DisplayFormat("MM/dd/yyyy hh:mm tt")]` |
| `IPAddress` | `string` | Yes | (nvarchar max) | `[Display(Name = "IP Address")]` |
| `OnProperty` | `bool` | Yes | � | `[Display(Name = "On Property")]` |
| `PrimaryRoleID` | `string` | Yes | 50 | � |
| `CreatedBy` | `string` | Yes | 50 | � |
| `ModifiedBy` | `string` | Yes | 250 | � |
| `CreatedDate` | `DateTime` | Yes | � | � |
| `ModifiedDate` | `DateTime` | Yes | � | � |

> Inherited from `IdentityUser`: `Id`, `UserName`, `Email`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEndDateUtc`, `LockoutEnabled`, `AccessFailedCount`.

### Navigation Properties

| Property | Type |
|----------|------|
| `UserLoginRecords` | `virtual ICollection<UserLoginRecord>` |

### Computed Properties (`[NotMapped]`)

| Property | Format |
|----------|--------|
| `EmployeeNumber_Initials` | `"EmpNbr (FML)"` |
| `EmployeeNumber_Initials_LastNameFirst` | `"EmpNbr - Last, F. M."` |
| `EmpNbr_FullName` | `"EmpNbr - First M. Last"` |
| `Initials` | `"FML"` |
| `FullName` | `"First M. Last"` |
| `FullName_LastNameFirst` | `"Last, First M."` |
| `Initials_LastName` | `"F. M. Last"` |

### Constructor / Factory

- Default constructor: `public ApplicationUser() { }`
- Private constructor accepts `EmployeeCreateView` + `string user`. Sets defaults: `ThemeFile = "bootstrap-spacelab.css"`, `OnProperty = false`, `IPAddress = "Not Known"`. Applies `TextInfo.ToTitleCase` when all characters are the same case.
- Public factory: `ApplicationUser.CreateInstance(EmployeeCreateView employee, string user)`

---

## 6. ControlNumberBase (Shared Base Entity)

Both projects define an abstract `ControlNumberBase` class implementing `IControlNumber`. It serves as the base for most domain entities.

### Common Schema

| Property | Data Type | Attributes |
|----------|-----------|------------|
| `ControlNumber` | `long` | `[Key]`, `[DatabaseGenerated(DatabaseGeneratedOption.None)]` � application-generated, not database-assigned |
| `CreatedBy` | `string` | `[Required]`, `[StringLength(50)]` |
| `ModifiedBy` | `string` | `[Required]`, `[StringLength(50)]` |
| `CreatedDate` | `DateTime` | `[Required]` |
| `ModifiedDate` | `DateTime` | `[Required]` |

### Key-Generation Differences

| Project | File | Strategy |
|---------|------|----------|
| **StrategicApplications** | `Models\BaseClasses\ControlNumberBase.cs` | Delegates to `ApplicationUtilities.CreateNewControlNumber()` |
| **SAClassLibrary** | `BaseClasses\ControlNumberBase.cs` | Calls `Thread.Sleep(1)` then generates `Convert.ToInt64(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"))` � a UTC-based millisecond timestamp with a 1 ms sleep to avoid collisions |

---

## 7. Global.asax.cs � Application Lifecycle

**File:** `StrategicApplications\Global.asax.cs`
**Class:** `MvcApplication : HttpApplication`

### Key Static State

| Field | Type | Purpose |
|-------|------|---------|
| `user` | `const string` (`"autoprocess"`) | Identity for automated background operations |
| `inbound` | `string` | `\\sql-svr\SA\Message Queue\Inbound` � production message queue path |
| `dev_inbound` | `string` | `\\sql-svr\SA\dev\Message Queue\Inbound` � development message queue path |
| `database` / `databasename` | `string` | Runtime-resolved database connection info |
| `delay` | `int` (600) | Timer delay in seconds |
| `ActiveUsers` | `Dictionary<string, ApplicationUser>` | In-memory cache of currently logged-in users |

### FileSystemWatchers (6 total)

- **Production:** `HolidayRecordWatcher`, `VacancyUpdateWatcher`, `StatusUpdateWatcher`
- **Development:** `DevHolidayRecordWatcher`, `DevVacancyUpdateWatcher`, `DevStatusUpdateWatcher`

### Timer Dictionaries (16 categories)

`BulletinTimers`, `SeniorityMoveTimers`, `HangoutTimers`, `DailyCallSheetTimers`, `DailyExtraBoardTimers`, `DailyReportTimers`, `DailyVacationWeekTimers`, `DailyOffDayTimers`, `DailyRailroadEmployeeStatusTimers`, `HolidayTimers`, `MarkOffRequestTimers`, `RosterBoardMarkOffTimers`, `RosterBoardHangoutTimers`, `PublishRailroadInformationTimers`, `CreateHolidayTimers`, `AtHocMessageTimers`

Each has a corresponding `nextXxxUpdates` dictionary (`Dictionary<long, DateTime>`) tracking the next scheduled execution.

### Processing Flags

`HolidayRecordsProcessing`, `VacancyRecordsProcessing`, `StatusRecordsProcessing`, `BoardProcessing` � prevent concurrent execution of background tasks.

### In-Progress Guards

`CallSheetInProgress`, `ExtraBoardInProgress`, `HolidayPayrollInProgress` � keyed by `long` (railroad control number).
# Part 2: Primary Key Generation

## 2.1 Base Class: ControlNumberBase

All domain entities inherit from `ControlNumberBase`, which implements `IControlNumber`.

**File**: `StrategicApplications\Models\BaseClasses\ControlNumberBase.cs`

### Properties

| Property | Type | Attributes | Description |
|---|---|---|---|
| `ControlNumber` | `long` | `[Key, DatabaseGenerated(None)]` | Primary key, generated in constructor |
| `CreatedBy` | `string` | `[Required, StringLength(50)]` | Username or "autoprocess" |
| `ModifiedBy` | `string` | `[Required, StringLength(50)]` | Username or "autoprocess" |
| `CreatedDate` | `DateTime` | `[Required]` | UTC timestamp at creation |
| `ModifiedDate` | `DateTime` | `[Required]` | UTC timestamp at last modification |

### Key Generation Algorithm

The constructor calls `ApplicationUtilities.CreateNewControlNumber()`:

```

# Part 49: Incremental Full-Codebase Reconciliation (Services, Integrations, Hard-Coded Rules)

This section is an explicit cross-check pass against non-entity runtime logic in all executable projects:

- `StrategicApplications` (web host/runtime automation)
- `SADailyCallSheetService`
- `SAImportPayrollService`
- `SAAtHocMessageService`
- `RestartApplicationPool`
- shared utilities in `SAClassLibrary`

---

## 49.1 Runtime Topology (Executable Flow)

### Windows service startup behavior (common pattern)

All three service-host projects (`SADailyCallSheetService`, `SAImportPayrollService`, `SAAtHocMessageService`) use a startup delay timer:

- hard-coded delay: `60000` ms (1 minute)
- `OnStart()` logs “will be ready at …”
- initial timer disables itself on first tick
- real watchers/queue listeners/timers are then initialized

This prevents immediate processing during service bootstrap.

---

## 49.2 `SADailyCallSheetService` Detailed Process Capture

### Hosted services (single process)

`Program.cs` launches 6 service classes in one process:

1. `SADailyCallSheetService`
2. `SADailyAssignmentShiftService`
3. `SADailyAssignmentService`
4. `SADailyCrewPositionService`
5. `SADailyOnDutyRecordService`
6. `SADailyOnDutyMarkOffRecordService`

### Messaging transport hard-coding

`ServiceUtilities.CreateMSMQMessage()` hard-codes direct private queue hosts:

- PROD: `FormatName:DIRECT=OS:SQL-SVR\private$\`
- DEBUG: `FormatName:DIRECT=OS:PTRA-IT-LT-10\private$\`

### Global hard-coded automation identity

- `user = "autoprocess"` is used for created/modified audit fields across all records created by these services.

### Queue payload contracts (CSV, positional)

#### `DailyAssignmentShift` create message body

Generated by call sheet service:

`poolControlNumber,shiftControlNumber,yyyy-MM-dd,processFlag`

`processFlag` is hard-coded `true` when generated from call-sheet timer flow.

#### `DailyAssignment` create message body

`assignmentCtr,locationCtr,typeCtr,boardOrder,assignmentNumber,assignmentName,airPay,onDutyTime,hours,dailyAssignmentShiftCtr,processFlag`

#### `DailyCrewPosition` create message body

`dailyAssignmentCtr,railroadPositionCtr,assignmentDate,extraBoardOnly,crewCtr,positionCtr`

#### `DailyOnDutyRecord` create message body

`dailyCrewPositionCtr,railroadPoolEmployeeCtr`

#### `DailyMarkOffRecord` create message body

`dailyCrewPositionOnDutyRecordCtr,markOffRecordCtr`

### Pool-number specific hard-coded rules (service layer)

#### Daily call sheet timing rules

Pool-specific behavior in `GetNextDailyCallSheet()` and `GetNextDailyAssignmentShift()`:

- `10` Yard & Enginemen: use last-calling-based schedule
- `20` Yardmasters: first-calling exceptions for shift 2/3 with 12/16-hour patterns
- `30` Clerical: first-calling exception when shift 2 has 15-hour assignments
- `40` Mechanical: same branch as pool 10 in major call-sheet timing
- `50` Maintenance of Way:
  - mark-off update loop skipped in `CreateDailyCallSheet()`
  - next processing uses `LastCallingEndTime + 2h`
  - holiday shift adds `+1 day`
- `60` Patrolmen: uses 30-minute offset logic similar to clerical timing paths

Additional hard-coded timer adjustment:

- `processdatetime = processdatetime.AddHours(-4)` before final validation
- fallback if late/missed window: next run = now rounded to minute + `180` seconds

### Other hard-coded values in call-sheet pipeline

- no-work sentinel datetime: `new DateTime(9999, 12, 31)`
- duplicate prevention: if `DailyAssignmentShift` already exists for pool/date/shift, service logs and reschedules only
- maintenance of way check: `if (!poolNumber.Equals(50))` for mark-off refresh step

### DailyAssignmentShift creation side effect (pool 30 only)

When creating a new shift, if pool number is `30` (Clerical) **and** shift date is a holiday:

- all unreleased hold-downs in that pool are auto-released with `HoldDownRelease.CreateInstance(..., "autoprocess")`

### Board-order synthetic encoding

`SetBoardOrder()` composes board order by string-concatenating:

- `(onDutyHour + 10)`
- `(onDutyMinute + 10)`
- location board order (for pool 10/40)
- assignment type number
- assignment number

Then converts to `long`.

### Training special-case constants

- training-created daily crew position messages use railroad position control number:
  - `99999999999999999`

### On-duty mark-off service writes file-based vacancy update trigger

`SADailyOnDutyMarkOffRecordService.CreateUpdateVacancyRequest()` hard-codes output path:

- `\\sql-svr\SA\Message Queue\Inbound`

File format:

- extension: `.UV`
- fields are tab-delimited: `method<TAB>pool<TAB>roster`
- filename is timestamp control number

---

## 49.3 `SAImportPayrollService` Detailed Process Capture

### Hosted services

1. `SAImportADPPayrollService`
2. `SAImportUKGPayrollService`

Both use:

- file watcher filter: `PRPT1*.*`
- 5-second sleep before file processing (copy completion guard)
- move processed files to `History`
- move failed files to `Processing Error`

### ADP import hard-coded parsing and mapping

Paths (hard-coded UNC):

- root: `\\finance-svr\c$\Payroll Exports\ADP\Imports`
- error: `<root>\Processing Error`
- history: `<root>\History`

Fixed-width parsing assumptions include:

- prefix skip for lines containing `DP1`
- specific substring widths/offsets for employee, ICC, dept, hours, amounts, codes

Hard-coded code remaps:

- `H -> 05`
- `M -> 65`
- `P -> 12`
- `S -> 03`
- for col4: `H -> 05`

Hard-coded earning selection behavior:

- default regular/overtime path uses codes `01` and `02`
- vacation variant `V` maps to `04` with alt `06`
- special col5 codes `14/15/16` add `+1` hour to ST (temporary workaround documented in code)
- special meal-period historical handling for payroll code `18`

Progress logging cadence:

- every `2500` read lines

Unmatched records:

- written to `.np` file in error folder
- optional corrected department report written as `Corrected Departments Report.txt`

### UKG import hard-coded parsing and matching

Paths (hard-coded UNC):

- root: `\\finance-svr\c$\Payroll Exports\UKG\Imports`
- error/history subfolders same pattern as ADP

CSV assumptions:

- header contains `Employee Number`
- fields: `[0]=emp`, `[1]=paydate`, `[2]=UKG code`, `[3]=hours`, `[4]=amount`

Matching hierarchy per payroll record:

1. match ST unpaid (`STHours` + `STPaid == 0`)
2. else match OT unpaid (`OTHours` + `OTPaid == 0`)
3. else match amount unpaid (`Amount` + `PaidAmount == 0`)

Unprocessed rows also emitted to `.np` file.

---

## 49.4 `SAAtHocMessageService` Detailed Process Capture

### Hosted services

1. `SAAssignmentCallService`
2. `SAAssignmentOnDutyService`

### Assignment-call timing and batching hard-coding

`SAAssignmentCallService`:

- calculates next call time by unique `CallingTimeStart` values across electronic-calling pools
- schedules execution at `calltime - 5 minutes`
- fallback if missed window: `now + 60 seconds` rounded to minute

Batch behavior:

- sends calls in chunks of `15`
- sleeps `60000` ms between chunks

Response polling behavior:

- loop checks while response window still open (6-minute window from record create time)
- polling sleep interval: `5000` ms

Late-call note logic:

- if actual call occurs after planned call start, note says employee should arrive by `now + 90 minutes`

### Electronic call vacancy hard-coded decision logic

Never-fill path:

- `Position.MustFill == 2` means “Never Fill”
- service marks do-not-fill, ties up position, deletes vacancy

Clerical (pool 30) filters:

- optional vacancy (`MustFill == 1`) truncates later clerical assignments
- >1 must-fill clerical vacancy clears all clerical call assignments for that batch path

Yardmaster (pool 20) filter:

- if more than one yardmaster vacancy in batch, removes yardmaster items from electronic call list

Alert channel requirement for extra-board calls:

- employee must have Alert phone/email unless vacancy is non-extra-board

### On-duty AtHoc service (`SAAssignmentOnDutyService`)

Per-pool timers created for pools under auto-enabled clients/railroads.

For due records:

- sends on-duty messages only when record is active and not annulled/do-not-fill/marked-off/tied-up/did-not-work
- marks stale prior-day unsent records as sent
- if off-duty exists, sends off-duty employee message (`SendEmployeeOnDutyMessage(false, ...)`)

Post-run pause:

- hard-coded 1-minute sleep before timer recalculation

---

## 49.5 `AtHocService` Integration Mapping (Hard-Coded)

### Device mapping by description

Phone mappings:

- `"Home Phone" -> "Device:homePhone"` (event 5)
- `"Mobile Phone" -> "Device:207ac0c6-0732-476f-9a4f-4204dae80dae"` (event 2)
- `"Emergency Phone" -> "Device:emergencyPhone"` (event 3)
- `"Work Phone" -> "Device:workPhone"` (event 4)
- `"Alert Phone (text message)" -> "Device:sms"` (event 1)

Email mappings:

- `"Alert Email Address" -> "Device:Email-Personal"` (event 7)
- `"Work Email Address" -> "Device:Email-Work"` (event 6)

Employee sync event IDs:

- On-duty yes = event `8`
- On-duty no = event `9`
- Craft update = event `10`
- Create user = event `11`
- Delete user = event `12`
- alert publish/response flow uses event `14`

Alert publish duration is hard-coded:

- `AlertDuration = "5"`
- `DurationUnit = "Minute"`

Authentication/token fields are taken from app settings keys:

- `ClientID`, `ClientSecret`, `GrantType`, `UserName`, `Password`, `AcrValues`, `Scope`

---

## 49.6 `RestartApplicationPool` Console Utility Logic

`RestartApplicationPool\Program.cs` behavior:

- requires `args[0]` as application pool name
- if pool currently started: stop first
- up to 10 loop iterations with `Thread.Sleep(1000)`
- if state is not `Stopping/Starting/Started`, attempts `Start()`
- logs each state transition via `SAClassLibrary.Utilities.EventLogger`

No argument-length guard exists; invalid/no args can throw before null checks.

---

## 49.7 Shared Utility Hard-Coded/Behavioral Rules

### Control number generation (repeated pattern)

Used in multiple projects:

- `Thread.Sleep(1)`
- `DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")` -> `long`

### Transaction scope defaults differ by project

- `SAClassLibrary.TransactionScopeBuilder`: timeout = `TransactionManager.DefaultTimeout`
- `StrategicApplications.ApplicationUtilities.TransactionScopeBuilder`: timeout fixed to `00:30:00`

### Event logger defaults differ by project

- `StrategicApplications.Utilities.EventLogger` default source/log:
  - source = `"Train Crew Reporting"`
  - log = `"Application"`
- `SAClassLibrary.Utilities.EventLogger` default source/log:
  - source = `"Strategic Applications"`
  - log = `"Crew Management Service Log"`

### File utility locking strategy (`SAClassLibrary`)

All file operations loop while `IsFileLocked(file)` with `Thread.Sleep(100)`.

`IsFileLocked` implementation determines lock availability by attempting to open a `StreamReader`; `IOException` means locked.

---

## 49.8 Cross-check Notes vs Prior Spec Sections

This increment captures service/integration logic not fully represented in earlier entity-centric sections, especially:

- end-to-end queue payload schemas and queue host hard-coding
- service-specific timer and fallback scheduling constants
- ADP/UKG import parser field assumptions and code remap tables
- AtHoc external integration event/device mappings
- file-path-based inter-process signaling (`*.UV`, `*.hr`, `*.esr`)

This section should be treated as the operational companion to Parts 1–48.
Thread.Sleep(1);
return Convert.ToInt64(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
```

- Format: `yyyyMMddHHmmssfff` � year, month, day, hour, minute, second, millisecond (17 digits)
- Example: `20250115143022547` = 2025-01-15 14:30:22.547 UTC
- `Thread.Sleep(1)` ensures millisecond uniqueness between sequential creates
- No database sequence or GUID involved � purely timestamp-based
- Risk: concurrent inserts from multiple threads/processes within the same millisecond could collide despite the sleep

### Interface

```csharp
public interface IControlNumber
{
    long ControlNumber { get; set; }
    string CreatedBy { get; set; }
    string ModifiedBy { get; set; }
    DateTime CreatedDate { get; set; }
    DateTime ModifiedDate { get; set; }
}
```

## 2.2 Exceptions to the Pattern

Two entities do NOT use `ControlNumberBase` and instead use foreign-key-as-primary-key (1:1 relationships):

### DailyCrewPositionOffDutyRecord

```csharp
[Required]
[Key, ForeignKey("DailyCrewPositionOnDutyRecord")]
[DatabaseGenerated(DatabaseGeneratedOption.None)]
public long DailyCrewPositionOnDutyRecordControlNumber { get; set; }
```

- PK = FK to `DailyCrewPositionOnDutyRecord`
- Does NOT inherit `ControlNumberBase`
- Has its own `CreatedBy` (string 50) and `CreatedDate` properties but no `ModifiedBy`/`ModifiedDate`
- 1:1 with `DailyCrewPositionOnDutyRecord`

### CraftPayCodes

```csharp
[Key, ForeignKey("Craft")]
public long CraftControlNumber { get; set; }
```

- PK = FK to `Craft`
- 1:1 with `Craft`

## 2.3 Common Creation Pattern

Throughout the codebase, entities are created using a consistent pattern:

```csharp
var now = DateTime.Now;

var entity = EntityType.CreateInstance(/* params */);

entity.Property1 = value1;
entity.Property2 = value2;

entity.CreatedBy = user;    // username string or "autoprocess"
entity.CreatedDate = now;

entity.ModifiedBy = user;
entity.ModifiedDate = now;

db.EntitySet.Add(entity);
db.SaveChanges();
```

- `CreateInstance()` is a static factory method wrapping private constructors
- The base constructor fires first, generating the `ControlNumber`
- `CreatedBy` and `ModifiedBy` are always set to the same value on initial creation
- `now` is captured once and reused for both timestamps to ensure consistency

## 2.4 Migration Considerations

- The `Thread.Sleep(1)` approach is not safe for high-concurrency or distributed scenarios
- Recommended replacements: database sequences, `Guid.NewGuid()`, Snowflake IDs, or Hi-Lo algorithm
- Any replacement must maintain `long` type compatibility or require FK migration across all ~230 entity types
- The timestamp-based format provides natural chronological ordering which some queries may depend on
# Part 3a: Entity Catalog � Organizational Entities

## Client

**Inherits**: `ControlNumberBase`

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `ClientName` | `string` | `[Required, StringLength(250)]` |
| `AutoAssignments` | `bool` | `[Required]` |

### Navigation Properties

| Property | Type |
|---|---|
| `ClientRequirements` | `ICollection<ClientRequirement>` |
| `Descriptions` | `ICollection<Description>` |
| `Employees` | `ICollection<Employee>` |
| `EmploymentStatusCodes` | `ICollection<EmploymentStatus>` |
| `Holidays` | `ICollection<Holiday>` |
| `MarkOffCodes` | `ICollection<MarkOffCode>` |
| `PayrollCodes` | `ICollection<PayrollCode>` |
| `PayrollCategories` | `ICollection<PayrollCategory>` |
| `PayrollReportGroups` | `ICollection<PayrollReportGroup>` |
| `Railroads` | `ICollection<Railroad>` |
| `WeekDays` | `ICollection<WeekDay>` |

### Constructors

- `Client()` � default
- `Client(string name)` � private, sets `ClientName`
- `CreateInstance()` � returns `new Client(String.Empty)`

### Methods

**`DisableRailroadAutoFunctions(db)`**:
- Iterates all `Railroads`
- Sets each `railroad.AutoAssignments = false`
- Marks entity as `Modified`, calls `db.SaveChanges()`
- Then calls `railroad.DisableRailroadPoolAutoFunctions(db)`
- On exception: logs via `EventLogger`, throws `"Automation was not disabled. Please call support"`

---

## Railroad

**Inherits**: `ControlNumberBase` (partial class)

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `ClientControlNumber` | `long` | `[Required]` FK to Client |
| `RailroadMark` | `string` | `[Required, StringLength(4)]` |
| `RailroadName` | `string` | `[Required, StringLength(250)]` |
| `AutoAssignments` | `bool` | `[Required]` |

### Computed Properties

| Property | Logic |
|---|---|
| `RailroadMark_Name` | `RailroadMark + " " + RailroadName` |

### Navigation Properties

| Property | Type |
|---|---|
| `Client` | `Client` |
| `EngineerJobCodes` | `ICollection<EngineerJobCode>` |
| `RailroadEmployees` | `ICollection<RailroadEmployee>` |
| `RailroadLocomotiveTypes` | `ICollection<RailroadLocomotiveType>` |
| `RailroadPayrollDepartments` | `ICollection<RailroadPayrollDepartment>` |
| `RailroadPools` | `ICollection<RailroadPool>` |
| `RailroadRequirements` | `ICollection<RailroadRequirement>` |
| `PayrollHolidayRecords` | `ICollection<PayrollHolidayRecord>` |
| `PayrollPeriodProcessRecords` | `ICollection<PayrollPeriodProcessRecord>` |
| `RailroadAFEs` | `ICollection<RailroadAFE>` |
| `RailroadMaterialCategories` | `ICollection<RailroadMaterialCategory>` |
| `RailroadZones` | `ICollection<RailroadZone>` |
| `RailroadWorkCodes` | `ICollection<RailroadWorkCode>` |
| `RailroadLocations` | `ICollection<RailroadLocation>` |

### Constructors

- `Railroad()` � default
- `Railroad(long client)` � private, sets `ClientControlNumber`
- `CreateInstance(long client)` � factory

### Methods

**`DisableRailroadPoolAutoFunctions(db)`**:
- Iterates all `RailroadPools`
- Sets `AutoBulletins`, `AutoMoves`, `AutoHangouts`, `AutoCallSheets`, `AutoVacancyAssignments` all to `false`
- Marks entity as Modified, saves once after loop
- On exception: logs, throws `"Automation was not disabled. Please call support"`

**`CreatePayrollHolidayRecords(db, holiday)`**:
- Finds all `DailyRailroadEmployeeStatusRecords` where `Date == holiday.HolidayDate` and `EmploymentCode == "AT"`
- For each active `RailroadPoolEmployee` (where `IsActive` and no existing holiday payroll record):
  - Checks `ProcessPayroll` flag on both craft and employee
  - If craft is null, falls back to `RailroadEmployee.ActiveCraft`
  - Creates `PayrollHolidayRecord` with employee's pay info

---

## RailroadPool

**Inherits**: `ControlNumberBase` (partial class)

### Stored Properties

| Property | Type | Attributes | Default |
|---|---|---|---|
| `RailroadControlNumber` | `long` | FK to Railroad | � |
| `PoolName` | `string` | `[Required, StringLength(250)]` | � |
| `PoolNumber` | `int` | `[Required]` | � |
| `AllowBulletins` | `bool` | `[Required]` | `true` |
| `AllowSeniorityMoves` | `bool` | `[Required]` | `true` |
| `AllowHoldDowns` | `bool` | `[Required]` | � |
| `AllowTemporaryAssignments` | `bool` | `[Required]` | `false` |
| `AutoBulletins` | `bool` | `[Required]` | � |
| `AutoMoves` | `bool` | `[Required]` | � |
| `AutoHangouts` | `bool` | `[Required]` | � |
| `AutoCallSheets` | `bool` | `[Required]` | � |
| `AutoVacancyAssignments` | `bool` | `[Required]` | � |
| `ElectronicCrewCalling` | `bool` | `[Required]` | � |

### Known Pool Numbers (Hard-Coded Throughout Codebase)

| PoolNumber | Name | Used In |
|---|---|---|
| 10 | Yard and Enginemen | Vacancy assignment, DefaultJobWorked/Paid, IsProtected, IsHelperOnly, FRA, call sheets, mark-off requests |
| 20 | Yardmasters | Electronic calling, extra board timing, call sheet timing |
| 30 | Clerical | Off-day hold-down logic, extra board timing, electronic calling, DefaultJobWorked/Paid |
| 40 | Mechanical | HasTrainees exclusion, EmergencyCallOut default, DefaultJobWorked/Paid, extra board timing |
| 50 | Maintenance of Way | HoursOnDuty meal deduction, DefaultJobWorked/Paid, call sheet timing |
| 60 | Patrolmen | DefaultJobWorked/Paid, call sheet timing |

### Computed Properties (all `[NotMapped]`)

| Property | Logic |
|---|---|
| `RailroadPositionBulletins` | `CollectionLists.GetRailroadPoolRailroadPositionBulletins(ControlNumber)` |
| `BulletinCount` | `RailroadPositionBulletins.Count` |
| `SeniorityMoveCount` | `CollectionLists.GetSeniorityMoveCount(ControlNumber)` |
| `HoldDownCount` | `CollectionLists.GetRailroadPoolHoldDownCount(ControlNumber)` |
| `NotificationCount` | `CollectionLists.GetRailroadPoolNotificationCount(ControlNumber)` |
| `MarkOffRecordCount` | `CollectionLists.GetOpenMarkOffRecordCount(ControlNumber)` |
| `UnassignedEmployeeCount` | `CollectionLists.GetUnassignedRailroadPoolEmployeeCount(ControlNumber)` |

### Navigation Properties

| Property | Type |
|---|---|
| `Railroad` | `Railroad` |
| `Assignments` | `ICollection<Assignment>` |
| `AssignmentOnDutyTimes` | `ICollection<AssignmentOnDutyTime>` |
| `AssignmentTypes` | `ICollection<AssignmentType>` |
| `Crafts` | `ICollection<Craft>` |
| `Crews` | `ICollection<Crew>` |
| `DailyAssignmentShifts` | `ICollection<DailyAssignmentShift>` |
| `Locations` | `ICollection<Location>` |
| `RailroadPoolEmployees` | `ICollection<RailroadPoolEmployee>` |
| `RailroadPoolPayrollTiers` | `ICollection<RailroadPoolPayrollTier>` |
| `RailroadPoolRequirements` | `ICollection<RailroadPoolRequirement>` |
| `RailroadPoolMarkOffAllowances` | `ICollection<RailroadPoolMarkOffAllowance>` |
| `Shifts` | `ICollection<Shift>` |

### Methods

**`GetRailroadPoolEmployees(db, empcode, searchString, pool, status, roster)`**:
- Switch on `empcode`:
  - `"AT"` ? Active employees with seniority in a last-active roster within the pool
  - `"NA"`, `"XE"` ? Filtered by employment status code only
  - Default ? `CollectionLists.GetCurrentRailroadPoolEmployees()`
- Search: if `StringUtilities.IsNumeric(searchString)` ? `EmployeeNumber.StartsWith`; else ? `LastName.StartsWith`
- Optional `roster` filter applies seniority roster filter

**`CreateOffDays(db, offdaydate, enddate, user)`**:
- If `PoolNumber > 10`: sleeps `PoolNumber * 1000` ms (stagger processing)
- Wraps in `TransactionScope(ReadCommitted)`
- For each date in range:
  - Finds crews with off-days matching weekday, where `EffectiveDate <= date` and no abolishment before date
  - For each crew position (where railroad position not deleted):
    - Creates `DailyRailroadPositionOffDayRecord` if not exists
    - Creates `DailyRailroadPositionOffDayEmployeeRecord` for each assigned employee
  - **Pool 30 (Clerical) special**: also creates off-day employee records for hold-down employees (where hold-down is open and assign date < current date)
# Part 3b: Entity Catalog � Craft & Configuration Entities

## Craft

**Inherits**: `ControlNumberBase`

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `RailroadPoolControlNumber` | `long` | FK to RailroadPool |
| `CraftName` | `string` | `[Required, StringLength(250)]` |
| `CraftPluralName` | `string` | `[Required, StringLength(250)]` |
| `CraftNumber` | `int` | `[Required]` |
| `AutoMarkUp` | `bool` | `[Required]` |
| `ApproveAllMarkOffs` | `bool` | `[Required]` |
| `MarkOffHours` | `int` | `[Required]` |
| `MarkUpHours` | `int` | `[Required]` |
| `RequiredRestHours` | `int` | `[Required]` |
| `MaximumVacationDayTime` | `int` | `[Required]` |
| `UnpaidMealPeriodMinutes` | `int` | `[Required]` |
| `HoursofService` | `bool` | `[Required]` |
| `ProcessPayroll` | `bool` | `[Required]` |
| `ShowNotifications` | `bool` | `[Required]` |
| `VacationAssignmentType` | `int` | `[Required]` |

### Known Craft Names (Hard-Coded Throughout Codebase)

| CraftName | Used In |
|---|---|
| `"Engineer"` | DefaultJobWorked/Paid, rest calculations, FRA compliance, IsEngineer checks |
| `"Yardman"` | DefaultJobWorked/Paid, rest calculations, IsSemiProtected, IsYardman, IsHelperOnly |
| `"Clerical"` | Rest/availability calculations (available next day), off-duty AvailableDateTime |
| `"Yardmaster"` | Rest/availability calculations (available next day) |

### Computed Properties

| Property | Logic |
|---|---|
| `ApprovalOfficer` | `CraftApprovalOfficers.FirstOrDefault(o => o.Primary)?.EmployeeControlNumber ?? 0` |
| `HasSickDays` | `CraftSickDays != null && CraftSickDays.Count != 0` |

### Navigation Properties

| Property | Type |
|---|---|
| `RailroadPool` | `RailroadPool` |
| `CraftPayCodes` | `CraftPayCodes` (1:1) |
| `CraftPersonalDays` | `ICollection<CraftPersonalDays>` |
| `CraftMarkOffCodes` | `ICollection<CraftMarkOffCode>` |
| `CraftSickDays` | `ICollection<CraftSickDays>` |
| `CraftVacationDays` | `ICollection<CraftVacationDays>` |
| `CraftRequirements` | `ICollection<CraftRequirement>` |
| `CutOffTimes` | `ICollection<OnDutyMoveCutOffTime>` |
| `Rosters` | `ICollection<Roster>` |
| `CraftApprovalOfficers` | `ICollection<CraftApprovalOfficer>` |
| `MarkOffRequestRecords` | `ICollection<MarkOffRequestRecord>` |
| `PayrollRecords` | `ICollection<PayrollRecord>` |
| `CraftMarkOffAllowances` | `ICollection<CraftMarkOffAllowance>` |
| `MarkOffRequestWaitListRecords` | `ICollection<MarkOffRequestWaitListRecord>` |

### Constructors

- `Craft()` � default
- `Craft(string name)` � private
- `Craft(long pool)` � private, sets `RailroadPoolControlNumber`
- `CreateInstance()` ? `new Craft(String.Empty)`
- `CreateInstance(long pool)` ? `new Craft(pool)`

### Methods

**`GetVacationDays(int years)`**: Returns vacation days for service years.
- `CraftVacationDays.OrderByDescending(d => d.ServiceYears).FirstOrDefault(d => years >= d.ServiceYears)?.VacationDays ?? 0`

**`GetPersonalDays(int years)`**: Same pattern with `CraftPersonalDays`.

**`GetSickDays(int years)`**: Same pattern with `CraftSickDays`.

**`SetApprovalRequiredFlag(db, approval, user, now)`**:
- If `ApproveAllMarkOffs != approval`: updates flag, marks Modified, saves

---

## CraftPayCodes

**Does NOT inherit ControlNumberBase** � uses FK-as-PK pattern (1:1 with Craft).

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `CraftControlNumber` | `long` | `[Required, Key, ForeignKey("Craft"), DatabaseGenerated(None)]` |
| `PaidDayWorkedCode` | `string` | `[Required, StringLength(4)]` |
| `PaidDayPaidCode` | `string` | `[Required, StringLength(4)]` |
| `VacationDayWorkedCode` | `string` | `[Required, StringLength(4)]` |
| `VacationDayPaidCode` | `string` | `[Required, StringLength(4)]` |
| `PersonalDayWorkedCode` | `string` | `[Required, StringLength(4)]` |
| `PersonalDayPaidCode` | `string` | `[Required, StringLength(4)]` |
| `GuaranteePaidCode` | `string` | `[Required, StringLength(4)]` |

### Navigation Properties

| Property | Type |
|---|---|
| `Craft` | `Craft` |

### Usage in Business Logic

These codes are used as fallback values in `DefaultJobWorked` and `DefaultJobPaid` calculations on `RailroadPoolEmployee`, `DailyCrewPosition`, and `RailroadPosition` when the employee is on a roster board or lacks a specific position assignment.

---

## Roster

**Inherits**: `ControlNumberBase` (partial class)

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `CraftControlNumber` | `long` | FK to Craft |
| `RailroadPayrollDepartmentControlNumber` | `long` | `[Required]` FK |
| `RosterName` | `string` | `[Required, StringLength(250)]` |
| `RosterPluralName` | `string` | `[Required, StringLength(250)]` |
| `RosterNumber` | `int` | `[Required]` |
| `Training` | `bool` | `[Required]` |
| `ExtraBoard` | `bool` | `[Required]` |
| `OvertimeBoard` | `bool` | `[Required]` |

### Navigation Properties

| Property | Type |
|---|---|
| `Craft` | `Craft` |
| `RosterBulletinRule` | `RosterBulletinRule` (1:1) |
| `RosterSeniorityMoveRule` | `RosterSeniorityMoveRule` (1:1) |
| `RailroadPayrollDepartment` | `RailroadPayrollDepartment` |
| `DailyShiftOvertimeBoards` | `ICollection<DailyShiftOvertimeBoard>` |
| `Positions` | `ICollection<Position>` |
| `RosterBoards` | `ICollection<RosterBoard>` |
| `Seniority` | `ICollection<Seniority>` |

### Key Flags in Business Logic

- `Training == true` ? positions on this roster are training positions; affects `IsTraining` on on-duty records
- `ExtraBoard == true` ? roster has extra board; used in `CreateDailyShiftExtraBoards`
- `OvertimeBoard == true` ? roster has overtime board; used in `CreateDailyShiftOvertimeBoard`

---

## CraftVacationDays / CraftPersonalDays / CraftSickDays

All three follow the same pattern. Each inherits `ControlNumberBase`.

### Stored Properties (each)

| Property | Type | Attributes |
|---|---|---|
| `CraftControlNumber` | `long` | FK to Craft |
| `ServiceYears` | `int` | `[Required]` |
| `VacationDays` / `PersonalDays` / `SickDays` | `int` | `[Required]` |

Used by `Craft.GetVacationDays(years)`, `GetPersonalDays(years)`, `GetSickDays(years)` � finds the highest `ServiceYears` threshold the employee has met.

---

## CraftMarkOffCode

**Inherits**: `ControlNumberBase`

Links a `Craft` to a `MarkOffCode` with craft-specific overrides.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `CraftControlNumber` | `long` | FK to Craft |
| `MarkOffCodeControlNumber` | `long` | FK to MarkOffCode |
| `Exclude` | `bool` | Whether this mark-off code is excluded for this craft |
| `AutomaticMarkUpHours` | `int` | Craft-specific override for automatic mark-up hours |

---

## CraftApprovalOfficer

**Inherits**: `ControlNumberBase`

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `CraftControlNumber` | `long` | FK to Craft |
| `EmployeeControlNumber` | `long` | FK to Employee |
| `Primary` | `bool` | Whether this is the primary approval officer |

Used by `Craft.ApprovalOfficer` computed property and cascading approval officer resolution.

---

## CraftMarkOffAllowance

**Inherits**: `ControlNumberBase`

Mark-off allowance configuration per craft. FK to `Craft`.

---

## CraftRequirement / CraftRequirementEmployee

**Inherits**: `ControlNumberBase`

Per-craft requirement tracking. `CraftRequirement` defines requirements; `CraftRequirementEmployee` tracks employee compliance.

---

## OnDutyMoveCutOffTime

**Inherits**: `ControlNumberBase`

FK to `Craft`. Defines cut-off times for on-duty position moves per craft.

---

## RosterBoard

**Inherits**: `ControlNumberBase`

### Key Properties

| Property | Type | Description |
|---|---|---|
| `RosterControlNumber` | `long` | FK to Roster |
| `AutoAssign` | `bool` | Whether auto-assignment is enabled |
| `ApplySeniorityMoveRule` | `bool` | Whether seniority rules apply for moves |
| `ExtraBoard` | `int` | 0=not extra board, non-zero=extra board |
| `IsFirstInFirstOutBoard` | `bool` | FIFO ordering for board |

### Navigation Properties

| Property | Type |
|---|---|
| `Roster` | `Roster` |
| `RosterBoardPositions` | `ICollection<RosterBoardPosition>` |
| `DailyShiftExtraBoards` | `ICollection<DailyShiftExtraBoard>` |

---

## RosterBoardPosition

**Inherits**: `ControlNumberBase`

### Key Properties

| Property | Type | Description |
|---|---|---|
| `RosterBoardControlNumber` | `long` | FK to RosterBoard |
| `IsExtraBoard` | `bool` | Whether this specific position is extra board |
| `TieUpOrder` | `int` | Order for tie-up processing |
| `BoardOrder` | `int` | Display/processing order on the board |

---

## RosterBulletinRule / RosterSeniorityMoveRule

Both 1:1 with `Roster`. Define posting and assignment rules for bulletins and seniority moves respectively.

---

## RailroadPayrollDepartment

**Inherits**: `ControlNumberBase`

### Key Properties

| Property | Type | Description |
|---|---|---|
| `RailroadControlNumber` | `long` | FK to Railroad |
| `DepartmentNumber` | `string` | Department identifier (first char stripped in some usages) |
| `ICCNumber` | `string` | ICC reporting number |
| `GeneralLedgerNumber` | `string` | GL number for billing |

Used in payroll code generation: `DailyCrewPosition.DepartmentNumber` strips first char via `Substring(1)`. `ICC_DepartmentNumber` = `"{ICCNumber}{DepartmentNumber}"`.
# Part 3c: Entity Catalog � Crew & Assignment Entities

## Crew

**Inherits**: `ControlNumberBase` (partial class)

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `RailroadPoolControlNumber` | `long` | FK to RailroadPool |
| `AssignmentTypeControlNumber` | `long` | FK to AssignmentType |
| `ShiftControlNumber` | `long` | FK to Shift |
| `CrewNumber` | `int` | `[Required]` |
| `CrewName` | `string` | `[Required, StringLength(250)]` |
| `EffectiveDate` | `DateTime` | `[Required, DataType(Date)]` |
| `ReliefJob` | `bool` | `[Required]` |

### Computed Properties

| Property | Logic |
|---|---|
| `CrewID` | If name contains "Relief" ? last char; if "XB" ? "XB"; else ? `CrewName` |
| `CrewIDName` | If name contains "Relief" ? `"RLF-" + last char`; else ? `CrewName` |
| `CrewID_CrewNbr` | `"{CrewIDName} ({count of non-deleted positions})"` |
| `Crew_CrewName` | `"Crew " + CrewName` |
| `CrewInformation` | Multi-line text: off days + positions with assigned employees |

### Navigation Properties

| Property | Type |
|---|---|
| `Shift` | `Shift` |
| `RailroadPool` | `RailroadPool` |
| `CrewPositions` | `ICollection<CrewPosition>` |
| `CrewAssignments` | `ICollection<CrewAssignment>` |
| `CrewOffDays` | `ICollection<CrewOffDay>` |
| `CrewAbolishment` | `CrewAbolishment` (1:1, nullable) |

---

## CrewPosition

**Does NOT inherit ControlNumberBase** � uses FK-as-PK pattern (1:1 with RailroadPosition). Implements `IAutoMarkUp`.

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `RailroadPositionControlNumber` | `long` | `[Required, Key, ForeignKey("RailroadPosition"), DatabaseGenerated(None)]` |
| `CrewControlNumber` | `long` | `[Required]` FK to Crew |
| `PositionControlNumber` | `long` | `[Required]` FK to Position |
| `EffectiveDate` | `DateTime` | `[Required]` |
| `VacationRelief` | `bool` | `[Required]` |
| `ExtraBoardOnly` | `bool` | `[Required]` � default `true` |
| `CreatedBy` | `string` | `[Required, StringLength(50)]` |
| `ModifiedBy` | `string` | `[Required, StringLength(50)]` |
| `CreatedDate` | `DateTime` | `[Required]` |
| `ModifiedDate` | `DateTime` | `[Required]` |

### Computed Properties

| Property | Logic |
|---|---|
| `CrewPositionName` | `"{Crew.CrewName} {Position.PositionName}"` |
| `ShortCrewPositionName` | Pool 10: `"{CrewIDName}({PositionInitial})"` ; else: `CrewIDName` |
| `PayrollCode` | Pool 30/50/60: `"{PositionCode}{PayrollCode}"` ; default: `"{PayrollCode}{PositionCode}"` |

### Navigation Properties

| Property | Type |
|---|---|
| `Crew` | `Crew` |
| `Position` | `Position` |
| `RailroadPosition` | `RailroadPosition` |
| `CrewPositionAlternatePositions` | `ICollection<CrewPositionAlternatePosition>` |
| `PayrollCrewPositionAutoPayRecords` | `ICollection<PayrollCrewPositionAutoPayRecord>` |

---

## Assignment

**Inherits**: `ControlNumberBase` (partial class)

### Stored Properties

| Property | Type | Attributes | Default |
|---|---|---|---|
| `RailroadPoolControlNumber` | `long` | FK to RailroadPool | � |
| `LocationControlNumber` | `long` | FK to Location | � |
| `AssignmentOnDutyTimeControlNumber` | `long` | FK to AssignmentOnDutyTime | � |
| `AssignmentTypeControlNumber` | `long` | FK to AssignmentType | � |
| `BoardOrder` | `long` | `[Required]` | � |
| `AssignmentNumber` | `int` | `[Required]` | � |
| `AssignmentName` | `string` | `[Required, StringLength(250)]` | � |
| `EffectiveDate` | `DateTime` | `[Required, DataType(Date)]` | `DateTime.Today` |
| `AssignedAirPay` | `bool` | `[Required]` | � |
| `WorkArea` | `string` | `[Required, StringLength(50)]` | `"Roustabout"` |

### Computed Properties

| Property | Logic |
|---|---|
| `Assignment_AssignmentName` | `"Assignment " + AssignmentName` |
| `Assignment_Location` | `"Assignment {Name} - {Location.LocationName}"` |

### Navigation Properties

| Property | Type |
|---|---|
| `AssignmentType` | `AssignmentType` |
| `AssignmentAbolishment` | `AssignmentAbolishment` (1:1, nullable) |
| `AssignmentOnDutyTime` | `AssignmentOnDutyTime` |
| `Location` | `Location` |
| `RailroadPool` | `RailroadPool` |
| `AssignmentOnDutyDays` | `ICollection<AssignmentOnDutyDay>` |
| `DailyAssignments` | `ICollection<DailyAssignment>` |
| `TemporaryAssignments` | `ICollection<TemporaryAssignment>` |

---

## Shift

**Inherits**: `ControlNumberBase` (partial class)

### Stored Properties

| Property | Type | Attributes | Default |
|---|---|---|---|
| `RailroadPoolControlNumber` | `long` | FK to RailroadPool | � |
| `ShiftID` | `string` | `[Required, StringLength(1)]` | � |
| `ShiftName` | `string` | `[Required, StringLength(250)]` | � |
| `ReliefShift` | `bool` | `[Required]` | `false` |

### Computed Properties � Shift Sequencing

| Property | ShiftID "1" | ShiftID "2" | ShiftID "3" |
|---|---|---|---|
| `PreviousShiftID` | `"3"` | `"1"` | `"2"` |
| `NextShiftID` | `"2"` | `"3"` | `"1"` |

Shifts form a circular sequence: 1 ? 2 ? 3 ? 1. When advancing from shift 3 to shift 1, the date increments by one day (see `GetNextDailyAssignmentShift` in Global.asax).

### Navigation Properties

| Property | Type |
|---|---|
| `RailroadPool` | `RailroadPool` |
| `Crews` | `ICollection<Crew>` |
| `OnDutyTimes` | `ICollection<AssignmentOnDutyTime>` |
| `DailyAssignmentShifts` | `ICollection<DailyAssignmentShift>` |

---

## Supporting Entities (Brief)

### AssignmentType
**Inherits**: `ControlNumberBase`
- `AssignmentTypeName` (string), `ExtraBoardOnly` (bool)

### AssignmentAbolishment
**Inherits**: `ControlNumberBase`
- FK to Assignment. `AbolishmentDate` (DateTime). Soft-deletes an assignment.

### AssignmentOnDutyDay
**Inherits**: `ControlNumberBase`
- FK to Assignment + WeekDay + AssignmentOnDutyTime. Links an assignment to which days of the week it operates.

### AssignmentOnDutyTime
**Inherits**: `ControlNumberBase`
- FK to RailroadPool + Shift
- `OnDutyTime` (TimeSpan), `CallingTimeStart` (TimeSpan), `CallingTimeEnd` (TimeSpan)
- Defines when employees go on duty and the calling window for a shift

### CrewAssignment
**Inherits**: `ControlNumberBase`
- Links `Crew` to `AssignmentOnDutyDay`. Defines which crew works which assignment on which day.

### CrewOffDay
**Inherits**: `ControlNumberBase`
- FK to Crew + WeekDay. Defines which days a crew is off.

### CrewAbolishment
**Inherits**: `ControlNumberBase`
- FK to Crew. `AbolishmentDate` (DateTime). Soft-deletes a crew.

### CrewPositionAlternatePosition
**Inherits**: `ControlNumberBase`
- Defines alternate position assignments for crew positions.

### Location
**Inherits**: `ControlNumberBase`
- `LocationName` (string), `LocationNumber` (int), `OnDutyLocation` (bool)
- **Hard-coded location numbers in vacancy assignment**: 11, 13, 14 (used in helper search order)

### WeekDay
**Inherits**: `ControlNumberBase`
- `WeekDayName` (string) � e.g. "Monday", "Tuesday", etc.
- Used by crew off-days, assignment on-duty days, and off-day processing
# Part 3d: Entity Catalog � Position & RailroadPosition Entities

## Position

**Inherits**: `ControlNumberBase` (partial class)

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `RosterControlNumber` | `long` | FK to Roster |
| `RailroadPayrollDepartmentControlNumber` | `long` | `[Required]` FK |
| `PositionName` | `string` | `[Required, StringLength(250)]` |
| `PositionNumber` | `int` | `[Required]` |
| `PositionCode` | `string` | `[Required, StringLength(2)]` |
| `PayrollCode` | `string` | `[Required, StringLength(10)]` |
| `BulletinPosition` | `bool` | `[Required]` � default `true` |
| `CertificationPay` | `bool` | `[Required]` |
| `TurnoverPay` | `bool` | `[Required]` |
| `AutoAssignVacation` | `bool` | `[Required]` |
| `MustFill` | `int` | `[Required]` |

### MustFill Values (Hard-Coded)

| Value | Meaning | Behavior |
|---|---|---|
| 0 | Must Fill | Position must be filled during vacancy assignment |
| 1 | Optional | May be filled; special handling in Pool 30 (Clerical) electronic calling |
| 2 | Never Fill | Auto-set to DoNotFill during vacancy processing |

### Computed Properties

| Property | Logic |
|---|---|
| `PositionInitial` | `PositionName.Substring(0, 1)` |
| `CurrentSTPayRate` | Latest effective `PositionPayRate.STHourRate` where `EffectiveDate <= today` |
| `CurrentOTPayRate` | Latest effective `PositionPayRate.OTHourRate` where `EffectiveDate <= today` |
| `ApprovalOfficer` | `PositionAlternateSupervisor?.EmployeeControlNumber ?? Roster.Craft.ApprovalOfficer` |

### Navigation Properties

| Property | Type |
|---|---|
| `Roster` | `Roster` |
| `RailroadPayrollDepartment` | `RailroadPayrollDepartment` |
| `PositionAlternateSupervisor` | `PositionAlternateSupervisor` (1:1, nullable) |
| `CrewPositions` | `ICollection<CrewPosition>` |
| `DailyCrewPositions` | `ICollection<DailyCrewPosition>` |
| `PositionPayRates` | `ICollection<PositionPayRate>` |
| `PositionRequirements` | `ICollection<PositionRequirement>` |
| `Qualifications` | `ICollection<Qualification>` |
| `RailroadPoolEmployeeTrainingDates` | `ICollection<RailroadPoolEmployeeTrainingDate>` |
| `CrewPositionAlternatePositions` | `ICollection<CrewPositionAlternatePosition>` |
| `PayrollCodePayRates` | `ICollection<PayrollCodePayRate>` |

---

## RailroadPosition

**Inherits**: `ControlNumberBase` (partial class). Implements `IAutoMarkUp`.

This is a **polymorphic entity** � can represent either a crew position or a roster board position, discriminated by `PositionType`.

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `PositionType` | `string` | `[Required, StringLength(1)]` � `"C"` = Crew, `"B"` = Board |

### Type Discriminator Properties

| Property | Logic |
|---|---|
| `IsCrewPosition` | `PositionType == "C" && CrewPosition != null` |
| `IsRosterBoardPosition` | `PositionType == "B" && RosterBoardPosition != null` |

### Delegated Properties (all `[NotMapped]`)

All of these delegate to either `CrewPosition` or `RosterBoardPosition` based on `PositionType`:

| Property | Crew Source | Board Source |
|---|---|---|
| `RailroadName` | `CrewPosition.Crew.RailroadPool.Railroad.RailroadMark_Name` | `RosterBoardPosition.RosterBoard.Roster.Craft.RailroadPool.Railroad.RailroadMark_Name` |
| `RailroadPoolNumber` | `CrewPosition.Crew.RailroadPool.PoolNumber` | `RosterBoardPosition.RosterBoard.Roster.Craft.RailroadPool.PoolNumber` |
| `RailroadPoolName` | `CrewPosition.Crew.RailroadPool.PoolName` | (same chain via board) |
| `RailroadPoolControlNumber` | `CrewPosition.Position.Roster.Craft.RailroadPoolControlNumber` | (same chain via board) |
| `RosterControlNumber` | `CrewPosition.Position.RosterControlNumber` | `RosterBoardPosition.RosterBoard.RosterControlNumber` |
| `CraftControlNumber` | `CrewPosition.Position.Roster.CraftControlNumber` | (same chain via board) |
| `CraftName` | `CrewPosition.Position.Roster.Craft.CraftName` | (same chain via board) |
| `Craft` | `CrewPosition.Position.Roster.Craft` | (same chain via board) |
| `PositionName` | `CrewPosition.Position.PositionName` | `RosterBoardPosition.PositionName` |
| `PositionNumber` | `CrewPosition.Position.PositionNumber` | `RosterBoardPosition.PositionNumber` |
| `PositionCode` | `CrewPosition.Position.PositionCode` | N/A |
| `PayrollCode` | `CrewPosition.Position.PayrollCode` | N/A |
| `CrewName` | `CrewPosition.Crew.CrewName` | `string.Empty` |
| `CrewNumber` | `CrewPosition.Crew.CrewNumber.ToString("N0")` | `string.Empty` |
| `RosterNumber` | `CrewPosition.Position.Roster.RosterNumber` | (same chain via board) |
| `RosterName` | `CrewPosition.Position.Roster.RosterName` | (same chain via board) |
| `RequiredRestHours` | `CrewPosition.Position.Roster.Craft.RequiredRestHours` | (same chain via board) |
| `ApprovalOfficer` | `CrewPosition.Position.ApprovalOfficer` | (via board roster craft) |

### ShortCrewPositionName � Pool-Specific Formatting

| Pool | Crew Position Format | Board Position Format |
|---|---|---|
| 10 (Yard & Enginemen) | Relief: `"R{last char}-{PositionCode}"` ; else: `"{CrewName}{PositionCode}"` | If `ApplySeniorityMoveRule`: `"EXBD"` |
| 30 (Clerical) | `"{CrewName (no dashes)}{PositionCode}"` | � |
| 40 (Mechanical) | `"{CrewName (no dashes)}{PositionCode}"` | � |
| 50 (MoW) | `CrewName` | � |
| 60 (Patrolmen) | `CrewName` | � |
| Default | `"{CrewName}{PositionCode}"` | � |

### Boolean Properties

| Property | Logic |
|---|---|
| `IsAssigned` | `RailroadPoolEmployeePosition != null` |
| `IsHangout` | `RosterBoardPosition != null && RosterBoardPosition.RosterBoard.AutoAssign` |
| `IsReliefCrew` | `CrewPosition != null && CrewPosition.Crew.Shift.ReliefShift` |
| `IsTraineePosition` | Roster.Training (on either crew or board) |
| `IsExtraBoard` | `RosterBoardPosition?.IsExtraBoard ?? false` |
| `IsSelected` | Has seniority moves with no assignment |
| `IsBulletined` | Has bulletins with no assignment and no no-bid |
| `IsNoBid` | Has bulletins where `IsNoBid == true` |
| `IsMarkedOff` | Delegates to `RailroadPoolEmployeePosition.IsMarkedOff` |
| `IsCalled` | Delegates to `RailroadPoolEmployeePosition.IsCalled` |
| `IsOnDuty` | Delegates to `RailroadPoolEmployeePosition.IsOnDuty` |
| `IsRested` | Delegates to `RailroadPoolEmployeePosition.IsRested` |
| `CanMoveOffPosition` | Board: `RosterBoardPosition.RosterBoard.Available`; Crew: `true` |
| `VacationRelief` | `CrewPosition?.VacationRelief ?? false` |
| `CalculateRest` | `Craft.HoursofService` (via either path) |
| `AutoMarkUp` | `Craft.AutoMarkUp` (via either path) |
| `BulletinPosition` | Only if unassigned and not already bulletined; crew: `Position.BulletinPosition`; board: checks board bulletin settings + percentage |

### DefaultJobPaid � Hard-Coded Pay Codes

```
IF Craft != null:
  SWITCH CraftName:
    "Engineer":
      IsTraineePosition ? "30H1"
      else              ? "10H1"
    "Yardman":
      IsRosterBoardPosition ? "100H"
      PositionName "Foreman" ? "101F"
      else (Helper)         ? "101H"
    Default:
      ? Craft.CraftPayCodes.PaidDayPaidCode
```

### PayrollDepartment

Delegates to the appropriate `RailroadPayrollDepartment` based on position type:
- Crew: `CrewPosition.Position.RailroadPayrollDepartment`
- Board: `RosterBoardPosition.RosterBoard.Roster.RailroadPayrollDepartment`

### Navigation Properties

| Property | Type |
|---|---|
| `CrewPosition` | `CrewPosition` (1:1, nullable) |
| `RosterBoardPosition` | `RosterBoardPosition` (1:1, nullable) |
| `DeletedRailroadPosition` | `DeletedRailroadPosition` (1:1, nullable � soft delete) |
| `RailroadPoolEmployeePosition` | `RailroadPoolEmployeePosition` (1:1, nullable) |
| `HoldDowns` | `ICollection<HoldDown>` |
| `DailyRailroadPositionOffDayRecords` | `ICollection<DailyRailroadPositionOffDayRecord>` |
| `DailyRailroadEmployeePositionRecords` | `ICollection<DailyRailroadEmployeePositionRecord>` |
| `RailroadPositionBulletins` | `ICollection<RailroadPositionBulletin>` |
| `RailroadPositionChanges` | `ICollection<RailroadPositionChange>` |
| `LocomotiveInspectionRecords` | `ICollection<LocomotiveInspectionRecord>` |
| `SeniorityMoves` | `ICollection<SeniorityMove>` |
| `MarkOffRecords` | `ICollection<MarkOffRecord>` |

---

## Supporting Entities

### PositionPayRate
**Inherits**: `ControlNumberBase`
- `PositionControlNumber` (long FK), `EffectiveDate` (DateTime), `STHourRate` (double), `OTHourRate` (double)

### PositionAlternateSupervisor
**Inherits**: `ControlNumberBase`
- FK to Position + Employee. Overrides the craft-level approval officer for a specific position.

### PositionRequirement / PositionRequirementEmployee
**Inherits**: `ControlNumberBase`
- Position-level qualification requirements and employee compliance tracking.

### DeletedRailroadPosition
**Inherits**: `ControlNumberBase`
- 1:1 with RailroadPosition. Soft-delete marker. Checked throughout: `RailroadPosition.DeletedRailroadPosition == null` means active.

### RailroadPoolEmployeePosition
**Inherits**: `ControlNumberBase`
- Links a `RailroadPoolEmployee` to a `RailroadPosition` � the current assignment.
- `RailroadPoolEmployeeControlNumber` (long FK), `RailroadPositionControlNumber` (long FK), `AssignedDate` (DateTime)
# Part 3e: Entity Catalog � Employee Entities

## Employee

**Inherits**: `ControlNumberBase` (partial class)

### Stored Properties

| Property | Type | Attributes | Default |
|---|---|---|---|
| `ClientControlNumber` | `long` | FK to Client | � |
| `EmploymentStatusControlNumber` | `long` | `[Required]` FK | � |
| `UserID` | `string` | `[Required, StringLength(128)]` FK to AspNetUsers | � |
| `EmployeeNumber` | `string` | `[Required, StringLength(4, Min=4)]` | � |
| `SocialSecurityNumber` | `string` | `[Required, StringLength(9, Min=9)]` | � |
| `DriversLicenseNumber` | `string` | `[StringLength(50)]` | � |
| `IssuingState` | `string` | `[StringLength(2)]` | `"TX"` |
| `Race` | `string` | `[Required, StringLength(100)]` | � |
| `ShirtSize` | `string` | `[StringLength(5)]` | � |
| `FlagCode` | `string` | `[StringLength(1)]` | � |
| `Gender` | `string` | `[Required, StringLength(1)]` | � |
| `MarriageStatus` | `bool` | `[Required]` | � |
| `BirthDate` | `DateTime` | `[Required, DataType(Date)]` | `Today.Year - 25, 01, 01` |
| `AllowFMLAMarkOff` | `bool` | `[Required]` | `false` |
| `CallForOvertime` | `bool` | `[Required]` | `true` |
| `ProcessPayroll` | `bool` | `[Required]` | `true` |
| `TieUpOffProperty` | `bool` | `[Required]` | `false` |
| `EmploymentDate` | `DateTime` | `[Required, DataType(Date)]` | `DateTime.Today` |

### Computed Properties

| Property | Logic |
|---|---|
| `DisplayName` | Complex: trims `.` and spaces from first/middle/last; if first name is 1 char, adds period; middle always abbreviated to initial with period; commas stripped from last name |
| `FullName` | Delegates to `User.FullName` |
| `FullName_LastNameFirst` | Delegates to `User.FullName_LastNameFirst` |
| `EmpNbr_FullName` | `"{EmployeeNumber} - {User.FullName}"` |
| `EmpNbr_FullName_LastNameFirst` | `"{EmployeeNumber} - {User.FullName_LastNameFirst}"` |
| `EmpNbr_Initials_LastName` | `"{EmployeeNumber} - {User.Initials_LastName}"` |
| `EmpNbr_Initials_LastNameFirst` | Delegates to `User.EmployeeNumber_Initials_LastNameFirst` |
| `EmpNbr_FullName_LastNameFirst_ApprovalCount` | Padded name + approval count |
| `Age` | `DateTimeUtilities.CalculateYears(BirthDate)` � uses float subtraction `(yyyy.MMdd - yyyy.MMdd)` cast to int |
| `ServiceYears` | `CalculateYears(EmploymentDate) + ServiceCredit.ServiceYears + (ServiceCredit.ServiceMonths / 12) + (ServiceCredit.ServiceDays / 365)` � integer division |
| `VacationServiceYears` | `Now.Year - adjustedEmploymentDate.Year` (adjusted by service credit) |
| `NextYearVacationServiceYears` | `Now.AddYears(1).Year - adjustedEmploymentDate.Year` |
| `PriorServiceCredit` | Formatted string: `"{Years} Years, {Months} Months, {Days} Days"` or `"None"` |
| `IsActive` | `Status.EmploymentCode.Contains("AT")` |
| `IsOutOfService` | `Status.EmploymentCode.Contains("OS")` |
| `IsOnExtendedAbsence` | `Status.EmploymentCode.Contains("EA")` |
| `IsRestricted` | `RailroadEmployees.Any(e => e.IsRestricted)` |
| `IsRested` | `RailroadEmployees.Any(e => e.IsRested)` |
| `IsRailroadEmployeeRoleOnly` | `User.Roles.All(r => r.RoleId == "8a36ccc0-8478-4ef2-b651-6187a12215cf")` |
| `IsUnionRepresentativeRole` | `User.Roles.Any(r => r.RoleId == "073f1b6b-a776-49bb-97fa-4cca4ad51382")` |
| `ApprovalCount` | Opens new DbContext ? `CollectionLists.GetApprovalPayrollEarningRecords(db, ControlNumber).Count` |

### Hard-Coded Role GUIDs

| GUID | Role Name | Used In |
|---|---|---|
| `8a36ccc0-8478-4ef2-b651-6187a12215cf` | Railroad Employee | `IsRailroadEmployeeRoleOnly` |
| `073f1b6b-a776-49bb-97fa-4cca4ad51382` | Railroad Union Representative | `IsUnionRepresentativeRole` |

### Navigation Properties

| Property | Type |
|---|---|
| `Client` | `Client` |
| `User` | `ApplicationUser` |
| `Status` | `EmploymentStatus` |
| `ServiceCredit` | `EmployeePriorServiceCredit` (1:1, nullable) |
| `Addresses` | `ICollection<Address>` |
| `EmailAddresses` | `ICollection<EmailAddress>` |
| `PayrollRecords` | `ICollection<PayrollRecord>` |
| `PhoneNumbers` | `ICollection<PhoneNumber>` |
| `RailroadEmployees` | `ICollection<RailroadEmployee>` |
| `EmploymentStatusHistory` | `ICollection<EmploymentStatusHistory>` |
| `MarkOffRecords` | `ICollection<MarkOffRecord>` |
| `MarkOffRequestRecords` | `ICollection<MarkOffRequestRecord>` |
| `MarkOffRecordApprovals` | `ICollection<MarkOffRecordApproval>` |
| `MarkOffCodeApprovalOfficers` | `ICollection<MarkOffCodeApprovalOfficer>` |
| `CraftApprovalOfficers` | `ICollection<CraftApprovalOfficer>` |
| `ClientRequirements` | `ICollection<ClientRequirementEmployee>` |
| `DailyAssignmentRequests` | `ICollection<DailyAssignmentRequest>` |
| `EarningsApprovalEmployees` | `ICollection<EarningsApprovalEmployee>` |
| `EarningsApprovalRequiredRecords` | `ICollection<EarningsApprovalRequiredRecord>` |
| `MarkOffRequestApprovalRequiredRecords` | `ICollection<MarkOffRequestApproval>` |
| `PositionAlternateSupervisors` | `ICollection<PositionAlternateSupervisor>` |
| `MarkOffRequestWaitListRecords` | `ICollection<MarkOffRequestWaitListRecord>` |

### Methods

**`CreateEmploymentStatusHistory(db, user, status, statusdate)`**: Creates `EmploymentStatusHistory` record, saves.

**`StatusChange(db, statusctrlnbr, user)`**: Updates `EmploymentStatusControlNumber`, marks Modified, saves.

**`SendCreateEmployeeMessage()`**: Sends AtHoc "Create" message with `UserName, FirstName, LastName, DisplayName`. Then `Thread.Sleep(2500)`, then syncs all phone numbers and email addresses.

**`SendDeleteEmployeeMessage(username)`**: Sends AtHoc "Delete" message: `"Delete,{username}"`.

**`SendEmployeeOnDutyMessage(onduty, location, shift)`**: Sends AtHoc on/off duty message: `"OnDuty,{username},{onduty},{location},{shift}"`.

**`PhoneNumberChange(phone, action)`**: Opens new DbContext, finds all phones of same type/employee. Formats: `"{username},{type},{callingOrder},{number}"`. Sends via `AtHocService.ProcessPhoneNumberMessage()`.

**`EmailAddressChange(email, action)`**: Same pattern. Format: `"{username},{type},{email}"`. Sends via `AtHocService.ProcessEmailMessage()`.

---

## RailroadEmployee

**Inherits**: `ControlNumberBase` (partial class)

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `EmployeeControlNumber` | `long` | FK to Employee |
| `RailroadControlNumber` | `long` | FK to Railroad |
| `AssignedPoolsOnly` | `bool` | `[Required]` |

### Computed Properties

| Property | Logic |
|---|---|
| `AssignedPosition` | First active-seniority RailroadPoolEmployee's first position's RailroadPosition |
| `CurrentPosition` | Priority: open HoldDown position ? active seniority employee's CurrentPosition |
| `ActiveCraft` | `AssignedPosition?.Craft` |
| `LastActiveCraft` | If ActiveCraft null ? scans last 400 `DailyRailroadEmployeeStatuses` for "AT" status with position records ? returns that position's Craft |
| `ActiveSeniority` | First active seniority record across all RailroadPoolEmployees |
| `ActiveCraftSeniorityDate` | `ActiveSeniority.RosterDate` formatted as `"MM/dd/yyyy"` |
| `NextYearVacationDays` | `ActiveCraft.GetVacationDays(Employee.NextYearVacationServiceYears)` |
| `NextYearUnassignedVacationWeeks` | `(NextYearVacationDays / 5) - assignedWeeks - oneDayWeeks` |
| `TieUpOffProperty` | Delegates to `Employee.TieUpOffProperty` |
| `IsActive` | Delegates to `Employee.IsActive` |
| `IsRestricted` | `RailroadPoolEmployees.Any(e => e.IsRestricted)` |
| `IsRested` | `RailroadPoolEmployees.Any(e => e.IsRested)` |
| `IsVacationRelief` | `AssignedPosition?.VacationRelief ?? false` |
| `HasCompensatedDays` | `GetCompensationTimeAccountBalanceDays("CD") > 0` |
| `HasPersonalDays` | `GetCompensationTimeAccountBalanceDays("PD") > 0` |
| `HasSickDays` | `GetCompensationTimeAccountBalanceDays("SD") > 0` |
| Delegated name props | `EmployeeNumber`, `EmpNbr_FullName`, `EmpNbr_FullName_LastNameFirst`, `FullName_LastNameFirst`, `BirthDate`, `Age`, `EmploymentDate`, `ServiceYears` � all delegate to `Employee` |

### Navigation Properties

| Property | Type |
|---|---|
| `Employee` | `Employee` |
| `Railroad` | `Railroad` |
| `RailroadPoolEmployees` | `ICollection<RailroadPoolEmployee>` |
| `DailyRailroadEmployeeStatuses` | `ICollection<DailyRailroadEmployeeStatusRecord>` |
| `RailroadEmployeeVacationRequests` | `ICollection<RailroadEmployeeVacationRequest>` |
| `RailroadEmployeeVacationOneDayTimeRecords` | `ICollection<RailroadEmployeeVacationOneDayTimeRecord>` |
| `RailroadEmployeeCalendarRequests` | `ICollection<RailroadEmployeeCalendarRequest>` |
| `RailroadEmployeeCompensableTimeRecords` | `ICollection<RailroadEmployeeCompensableTimeRecord>` |

---

## Supporting Entities

### EmploymentStatus
- `EmploymentCode` (string), `StatusName` (string)
- Known codes: `"AT"` (Active), `"OS"` (Out of Service), `"EA"` (Extended Absence), `"NA"`, `"XE"`

### EmploymentStatusHistory
- FK to Employee + EmploymentStatus. `StatusChangeDate` (DateTime). Audit trail.

### EmployeePriorServiceCredit
- 1:1 with Employee. `ServiceYears` (int), `ServiceMonths` (int), `ServiceDays` (int).
- Used to adjust seniority/vacation calculations.

### ApplicationUser (IdentityUser extension)
- `EmployeeNumber` (string 25), `FirstName` (string 250), `MiddleName` (string 250), `LastName` (string 250)
- `ThemeFile` (string 100), `LastLogin` (DateTime), `IPAddress` (string), `OnProperty` (bool)
- `PrimaryRoleID` (string 50) � used in payroll approval routing to find primary role user
- `CreatedBy`, `ModifiedBy` (string), `CreatedDate`, `ModifiedDate` (DateTime)

### Address
- FK to Employee. Standard address fields.

### PhoneNumber
- FK to Employee + Description (as PhoneNumberType). `Number` (string), `CallingOrder` (int).

### EmailAddress
- FK to Employee + Description (as EmailAddressType). `Email` (string).
# Part 3f: Entity Catalog � RailroadPoolEmployee

## RailroadPoolEmployee

**Inherits**: `ControlNumberBase` (partial class)

The central operational entity linking an employee to a pool.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `RailroadPoolControlNumber` | `long` | FK to RailroadPool |
| `RailroadEmployeeControlNumber` | `long` | FK to RailroadEmployee |
| `RailroadPoolPayrollTierControlNumber` | `long` | FK to RailroadPoolPayrollTier |

### Navigation Properties

| Property | Type |
|---|---|
| `RailroadPool` | `RailroadPool` |
| `RailroadEmployee` | `RailroadEmployee` |
| `RailroadPoolPayrollTier` | `RailroadPoolPayrollTier` (nullable) |
| `Seniority` | `ICollection<Seniority>` |
| `Qualifications` | `ICollection<Qualification>` |
| `MarkOffRecords` | `ICollection<MarkOffRecord>` |
| `HoldDowns` | `ICollection<HoldDown>` |
| `SeniorityMoves` | `ICollection<SeniorityMove>` |
| `RailroadPoolEmployeePositions` | `ICollection<RailroadPoolEmployeePosition>` |
| `RailroadPositionHistory` | `ICollection<RailroadPoolEmployeePositionHistory>` |
| `DailyCrewPositionOnDutyRecords` | `ICollection<DailyCrewPositionOnDutyRecord>` |
| `DailyShiftExtraBoardPositions` | `ICollection<DailyShiftExtraBoardPosition>` |
| `RailroadPositionChanges` | `ICollection<RailroadPositionChange>` |
| `DailyRailroadEmployeePositionRecords` | `ICollection<DailyRailroadEmployeePositionRecord>` |
| `DailyCrewPositionVacancyEmployees` | `ICollection<DailyCrewPositionVacancyEmployee>` |
| `PayrollHolidayRecords` | `ICollection<PayrollHolidayRecord>` |

### Delegated Identity Properties (all `[NotMapped]`)

All delegate through `RailroadEmployee.Employee`:

| Property | Source |
|---|---|
| `EmployeeControlNumber` | `RailroadEmployee.EmployeeControlNumber` |
| `UserID` | `RailroadEmployee.Employee.UserID` |
| `EmployeeNumber` | `RailroadEmployee.EmployeeNumber` |
| `EmpNbr_FullName` | `RailroadEmployee.EmpNbr_FullName` |
| `EmpNbr_FullName_LastNameFirst` | `RailroadEmployee.EmpNbr_FullName_LastNameFirst` |
| `EmpNbr_Initials_LastNameFirst` | `RailroadEmployee.Employee.EmpNbr_Initials_LastNameFirst` |
| `FullName_LastNameFirst` | `RailroadEmployee.FullName_LastNameFirst` |
| `BirthDate` | `RailroadEmployee.BirthDate` |
| `Age` | `RailroadEmployee.Age` |
| `EmploymentDate` | `RailroadEmployee.EmploymentDate` |
| `ServiceYears` | `RailroadEmployee.ServiceYears` |
| `TieUpOffProperty` | `RailroadEmployee.TieUpOffProperty` |
| `ProcessPayroll` | `RailroadEmployee.ProcessPayroll` |
| `IsRailroadEmployeeRoleOnly` | `RailroadEmployee.Employee.IsRailroadEmployeeRoleOnly` |
| `IsUnionRepresentativeRole` | `RailroadEmployee.Employee.IsUnionRepresentativeRole` |
| `SeniorityDate_Rank` | `ActiveSeniority.RosterDate.AddSeconds(ActiveSeniority.Rank)` � for ordering |

### Seniority & Craft Properties (all `[NotMapped]`)

| Property | Logic |
|---|---|
| `ActiveSeniority` (private) | `RailroadEmployee.ActiveSeniority`; if null, falls back to seniority matching AssignedPosition roster; then first active seniority in pool |
| `ActiveRoster` | `ActiveSeniority?.Roster` |
| `ActiveCraft` | `ActiveRoster?.Craft` |
| `LastActiveCraft` | `ActiveCraft` ? `RailroadEmployee.ActiveCraft` ? last "AT" daily position record's craft |
| `IsActive` | Has active seniority in this pool |
| `IsCutBack` | Has cut-back seniority in this pool |
| `IsYardman` | `ActiveCraft?.CraftName == "Yardman"` (falls back to RailroadEmployee.ActiveCraft) |
| `IsEngineer` | `ActiveCraft?.CraftName == "Engineer"` (same fallback) |
| `IsForeman` | `LastAssignedPosition?.PositionCode == "F"` |
| `IsHelperOnly` | **Pool 10 only**: active, no "Foreman" qualification effective by now |
| `IsProtected` | **Pool 10 only**: `EmploymentDate < 1981-01-01` |
| `IsSemiProtected` | **Yardman craft only**: `EmploymentDate < 1991-01-01` |

### Position Properties (all `[NotMapped]`)

| Property | Logic |
|---|---|
| `AssignedPosition` | First `RailroadPoolEmployeePositions` entry's RailroadPosition; falls back to `RailroadEmployee.AssignedPosition` |
| `CurrentPosition` | Priority: most recent open HoldDown's position ? `AssignedPosition` |
| `LastActivePosition` | `CurrentPosition` ? if null, most recent `RailroadPositionHistory` entry |
| `LastAssignedPosition` | `AssignedPosition` ? if null, most recent crew-position history entry |
| `LastPosition` | Last `RailroadPositionHistory` entry ? if null, last `DailyRailroadEmployeePositionRecords` entry |
| `IsAssigned` | `RailroadPoolEmployeePositions.Count > 0` |
| `IsExtraBoardOrHangout` | `CurrentPosition?.IsRosterBoardPosition ?? false` |
| `IsExtraBoard` | CurrentPosition is roster board AND `RosterBoardPosition.IsExtraBoard` |
| `AssignedDays` | `RailroadPoolEmployeePositions.First()?.GetAssignedDays() ?? 0` |
| `AssignedDaysText` | `""` / `"(1 day)"` / `"(N days)"` |
| `BumpDate` | `AssignedPosition.RailroadPoolEmployeePosition.GetBumpDate()` or today |
| `CanBump` (private) | No pending seniority moves without assignment AND `Today <= BumpDate` |
| `CanMoveToExtraBoard` | On hangout with AutoAssign ? true; OR current position can move off AND any extra board employee on same roster has less seniority |

### PayrollDepartment Resolution

`PayrollDepartment` � cascading resolution:
1. `CurrentPosition.PayrollDepartment`
2. If null: `RailroadEmployee.AssignedPosition.PayrollDepartment`
3. If null: `LastPosition` � crew: `Position.RailroadPayrollDepartment`; board: `RosterBoard.Roster.RailroadPayrollDepartment`

### DefaultJobWorked � Pool-Number-Driven

| Pool | Craft/Condition | Code |
|---|---|---|
| 10 | Engineer | `"100D"` |
| 10 | Yardman, RosterBoard | `"100H"` |
| 10 | Yardman, Foreman | `"101F"` |
| 10 | Yardman, Helper | `"101H"` |
| 30, 60 | RosterBoard | `CraftPayCodes.PaidDayWorkedCode` |
| 30, 60 | Crew | `"{PositionCode}{CrewNumber}"` |
| 40 | RosterBoard | `CraftPayCodes.PaidDayWorkedCode` |
| 40 | Crew | `"{CrewNumber}{PositionCode}"` |
| 50 | RosterBoard | `CraftPayCodes.PaidDayWorkedCode` |
| 50 | Crew | `"{CrewName}"` |
| Default | � | `CraftPayCodes.PaidDayWorkedCode` |
| Fallback | If "Error" | `RailroadEmployee.LastActiveCraft.CraftPayCodes.PaidDayWorkedCode` |

### DefaultJobPaid � Pool-Number-Driven

| Pool | Craft/Condition | Code |
|---|---|---|
| 10 | Engineer, Training | `"30H1"` |
| 10 | Engineer | `"10H1"` |
| 10 | Yardman, RosterBoard | `"100H"` |
| 10 | Yardman, Crew | `"{PayrollCode}{PositionCode}"` |
| 30, 60 | RosterBoard | `CraftPayCodes.PaidDayPaidCode` |
| 30, 60 | Crew | `"{PositionCode}{PayrollCode}"` |
| 40 | RosterBoard | `CraftPayCodes.PaidDayWorkedCode` |
| 40 | Crew | `"{PayrollCode}{PositionCode}"` |
| 50 | RosterBoard | `CraftPayCodes.PaidDayWorkedCode` |
| 50 | Crew | `"{PositionCode}{PayrollCode}"` |
| Default | RosterBoard | `CraftPayCodes.PaidDayPaidCode` |
| Default | Crew | `"{PayrollCode}{PositionCode}"` |

### ApprovalOfficer � Cascading Resolution

1. `CurrentPosition.ApprovalOfficer`
2. If null: `LastAssignedPosition.ApprovalOfficer`
3. If null: `LastActiveCraft.ApprovalOfficer`
4. If null: `RailroadEmployee.LastActiveCraft.ApprovalOfficer`
5. If all null: `0`

### Mark-Off Properties (all `[NotMapped]`)

| Property | Logic |
|---|---|
| `CurrentOpenMarkOffRecord` | First open mark-off ordered by MarkOffDateTime asc |
| `LastOpenMarkOffRecord` | Last mark-off by date; returns if not closed |
| `OpenMarkOffRecord` | Last open mark-off by date |
| `IsMarkedOff` | `CurrentOpenMarkOffRecord` or `LastOpenMarkOffRecord` is open |

### On-Duty & Rest Properties (all `[NotMapped]`)

| Property | Logic |
|---|---|
| `IsCalled` | Opens new DbContext; finds on-duty records with no tie-up/annulment/DoNotFill/unavailable/markoff/DidNotWork; checks if on-duty time is in the future |
| `IsOnDuty` | Same query; checks if on-duty time is in the past |
| `IsOnOffDay` | `IsOffDay(DateTime.Today.DayOfWeek.ToString())` |
| `CalledForPosition` | Last on-duty record where `IsCalled` ? `"{Assignment_ReliefName} {PositionName}"` |
| `OnDutyPosition` | Last on-duty record where `IsOnDuty` ? same format |
| `LastOnDutyRecord` | Last on-duty record (skipping future) where `EmployeeWorked` |
| `LastOpenOnDutyRecord` | Last on-duty (skipping future, take 10) where worked and no off-duty |
| `NextOnDutyRecord` | Most recent future on-duty with no annulment/DoNotFill/off-duty/mark-off |
| `LastCompletedOnDutyRecord` | Last on-duty where `IsTiedUp` and `EmployeeWorked` |
| `LastOffDutyRecord` | Opens new DbContext; queries off-duty records ordered by date desc |
| `IsAvailable` | `LastOffDutyRecord.AvailableDateTime <= Now` |
| `IsRested` | `LastOffDutyRecord.RestedDateTime <= Now` |
| `IsRestricted` | If rested ? false; `LastCompletedOnDutyRecord.IsRestricted` |
| `AvailableDateTime` | `LastOffDutyRecord.AvailableDateTime` if future; else `1900-01-01` |
| `RestedDateTime` | `LastOffDutyRecord.RestedDateTime` if future; else `1900-01-01` |

### Status Display Property

`Status` � complex display string, evaluated in this priority order:

1. **MarkedOff** ? `"Marked Off {MarkOffCode.Description}"` or `"Marked up at {datetime}"`
2. **HoldDown** (if pool allows) ? `"On {PositionName} Hold Down"`
3. **Called** (extra board only) ? `"Called for {Position}"`
4. **OnDuty** ? `"Working {Position}"`
5. **Unassigned** ? seniority state description or employment status name
6. **Rest/Availability** (craft-dependent):
   - Engineer/Yardman: `"Not rested until {datetime}"` or `"Rested"`
   - Clerical/Yardmaster: `"Not available until {datetime}"` or `"Available"`

`NewStatus` � same logic but uses `LastOffDutyRecord` query (more efficient) instead of filtered on-duty record query.

### Consecutive Days & Work Tracking

| Property | Logic |
|---|---|
| `ConsecutiveDays` | Opens DbContext; queries last 30 on-duty records before today; finds last worked; checks 24hr rest or `ConsecutiveDayRestedDateTime`; returns count or 0 if rested |
| `NewConsecutiveDays` | Uses `LastOffDutyRecord` approach; returns `DailyCrewPositionOnDutyRecord.ConsecutiveDays` |
| `STDaysWorked` | Last worked on-duty record in current pay period; returns `STDaysWorked` (or -1 if still on duty) |
| `NewSTDaysWorked` | Uses `LastOffDutyRecord` approach |
| `DaysWorked` | Same pattern as `STDaysWorked` for total days |
| `TwentyFourHourRestDateTime` | `LastCompletedOnDutyRecord.OffDutyRecord.ConsecutiveDayRestedDateTime` |
| `TwentyFourHourRestDateTimeString` | Formatted or empty if not applicable |

### HangoutNotificationControlNumber

`RailroadPositionChanges.OrderByDescending(CreatedDate).FirstOrDefault(c => c.RailroadPosition.IsHangout && !c.IsComplete && !c.EmployeeOnly)?.ControlNumber ?? 0`
# Part 23: Identity & Authentication

## Overview

The application uses ASP.NET Identity 2.0 with OWIN middleware for authentication. Forms authentication with cookie-based sessions.

## Stack

| Component | Technology |
|---|---|
| User Store | `UserStore<ApplicationUser>` backed by `StrategicApplicationsContext` |
| User Manager | `ApplicationUserManager` (custom `UserManager<ApplicationUser>`) |
| Sign-In | OWIN `AuthenticationManager.SignIn()` + `FormsAuthentication` |
| Cookie | `DefaultAuthenticationTypes.ApplicationCookie` |
| OWIN Startup | `Startup.Configuration(app)` ? `ConfigureAuth(app)` |

## Login Flow

1. `GET /Account/Login` � clears existing session, signs out
2. In DEBUG: pre-fills username `"1074"` and password
3. `POST /Account/Login`:
   - `UserManager.FindAsync(username, password)`
   - If found ? `SignInAsync(user, rememberMe)`
   - If password reset required ? redirect to `ChangePassword`
   - If `"admin"` ? redirect to `Home/Home`
   - Else ? redirect to `EmployeeDetail/Details`
4. `MvcApplication.RegisterUser(user)` � adds to `ActiveUsers` dictionary

## LogOff

1. Remove from `MvcApplication.ActiveUsers`
2. `AuthenticationManager.SignOut()`
3. `FormsAuthentication.SignOut()`
4. Set `HttpContext.User` to empty principal

## Authorization

- `[Authorize(Roles = "System Administrator")]` � on admin actions
- `[Authorize(Roles = "Railroad Employee")]` � on employee self-service
- `[AllowAnonymous]` � on Login/Register

## Known Roles (from code references)

| Role | Usage |
|---|---|
| `System Administrator` | User management, system config |
| `Railroad Auditor` | Default payroll approval officer |
| `Railroad Human Resources` | Payroll approval for employees/timekeepers |
| `Railroad Employee` | Self-service (mark-off requests, bulletins) |
| `Railroad Timekeeper` | Payroll entry, mark-off creation |
| `Railroad Supervisor` | Position management, approval |

## ApplicationUser

Extends `IdentityUser`. See Part 3e for full property list. Key additions: `PrimaryRoleID`, `FullName`, `EmpNbr_FullName`.
# Part 40: Complete Entity Index

## Overview

Full alphabetical index of all entities in the system with their location and Part reference.

## Web App Entities (StrategicApplications\Models\)

| Entity | Inherits | Part |
|---|---|---|
| `Address` | `ControlNumberBase` | 3e |
| `Assignment` | `ControlNumberBase` | 3c |
| `AssignmentAbolishment` | FK to Assignment | 3c |
| `AssignmentOnDutyDay` | `ControlNumberBase` | 3c |
| `AssignmentOnDutyTime` | `ControlNumberBase` | 3c |
| `AssignmentType` | `ControlNumberBase` | 3c |
| `ApplicationUser` | `IdentityUser` | 3e, 23 |
| `ChangeNotification` | `ControlNumberBase` | 32 |
| `ChangeMoveOrBulletin` | � | 32 |
| `Client` | `ControlNumberBase` | 3a |
| `Craft` | `ControlNumberBase` | 3b |
| `CraftApprovalOfficer` | `ControlNumberBase` | 8 |
| `CraftMarkOffAllowance` | � | 28 |
| `CraftMarkOffCode` | � | 14 |
| `CraftPayCode` | � | 35 |
| `CraftPersonalDay` | � | 28 |
| `CraftSickDay` | � | 28 |
| `CraftVacationDay` | � | 28 |
| `Crew` | `ControlNumberBase` | 3c |
| `CrewAbolishment` | FK to Crew | 3c |
| `CrewAssignment` | `ControlNumberBase` | 3c |
| `CrewOffDay` | � | 3c |
| `CrewPosition` | `ControlNumberBase` | 3c |
| `CrewPositionAlternatePosition` | � | 3c |
| `DailyAssignment` | `ControlNumberBase` | 4 |
| `DailyAssignmentAFERecord` | � | 29 |
| `DailyAssignmentAnnulment` | FK to DailyAssignment | 4 |
| `DailyAssignmentCrew` | � | 4 |
| `DailyAssignmentRequest` | `ControlNumberBase` | 4 |
| `DailyAssignmentShift` | `ControlNumberBase` | 4 |
| `DailyAssignmentShiftCompletion` | FK to Shift | 4 |
| `DailyCrewHistory` | `ControlNumberBase` | 4 |
| `DailyCrewPosition` | `ControlNumberBase` | 4 |
| `DailyCrewPositionAnnulment` | FK to DailyCrewPosition | 4 |
| `DailyCrewPositionDoNotFill` | FK to DailyCrewPosition | 4 |
| `DailyCrewPositionElectronicCallRecord` | `ControlNumberBase` | 9 |
| `DailyCrewPositionElectronicResponseRecord` | `ControlNumberBase` | 9 |
| `DailyCrewPositionHistory` | `ControlNumberBase` | 4 |
| `DailyCrewPositionOffDutyRecord` | FK to OnDutyRecord | 5 |
| `DailyCrewPositionOnDutyFRARecord` | FK to OnDutyRecord | 7 |
| `DailyCrewPositionOnDutyMarkOffRecord` | FK to OnDutyRecord | 14 |
| `DailyCrewPositionOnDutyPayrollRecord` | � | 16 |
| `DailyCrewPositionOnDutyRecord` | `ControlNumberBase` | 5 |
| `DailyCrewPositionOnDutyRecordLateCall` | FK to OnDutyRecord | 5 |
| `DailyCrewPositionSkip` | `ControlNumberBase` | 6 |
| `DailyCrewPositionVacancy` | `ControlNumberBase` | 6 |
| `DailyCrewPositionVacancyEmployee` | `ControlNumberBase` | 6 |
| `DailyExtraBoardMarkOffRecord` | � | 14, 19 |
| `DailyFRACommingleRecord` | `ControlNumberBase` | 7 |
| `DailyFRADeadheadRecord` | `ControlNumberBase` | 7 |
| `DailyOnDutyAFEBillingRecord` | `ControlNumberBase` | 29 |
| `DailyOnDutyDidNotWorkRecord` | FK to OnDutyRecord | 29 |
| `DailyOnDutyLocomotiveRecord` | `ControlNumberBase` | 29 |
| `DailyOnDutyMiscellaneousBillingRecord` | `ControlNumberBase` | 29 |
| `DailyOnDutyPayrollInformation` | FK to OnDutyRecord | 29 |
| `DailyOnDutyRailroadMaterialRecord` | `ControlNumberBase` | 29 |
| `DailyOnDutyUnavailableRecord` | FK to OnDutyRecord | 29 |
| `DailyOnDutyZoneBillingRecord` | `ControlNumberBase` | 29 |
| `DailyRailroadEmployeePositionMarkOffRecord` | `ControlNumberBase` | 14, 31 |
| `DailyRailroadEmployeePositionPayrollRecord` | � | 16, 31 |
| `DailyRailroadEmployeePositionRecord` | `ControlNumberBase` | 31 |
| `DailyRailroadEmployeeStatusRecord` | `ControlNumberBase` | 31 |
| `DailyRailroadPositionOffDayEmployeeRecord` | � | 31 |
| `DailyRailroadPositionOffDayRecord` | Composite PK | 31 |
| `DailyRosterBoardPositionHangoutRecord` | � | 21, 31 |
| `DailyShiftExtraBoard` | `ControlNumberBase` | 19 |
| `DailyShiftExtraBoardPosition` | `ControlNumberBase` | 19 |
| `DailyShiftExtraBoardPositionAssignment` | FK to XBPosition | 19 |
| `DailyShiftExtraBoardPositionPayrollRecord` | � | 16 |
| `DailyShiftOvertimeBoard` | `ControlNumberBase` | 39 |
| `DailyShiftOvertimeBoardPosition` | `ControlNumberBase` | 39 |
| `DeletedRailroadPosition` | FK to RailroadPosition | 3d |
| `Description` | `ControlNumberBase` | � |
| `EarningsApprovalRecord` | FK to EarningsRequired | 8 |
| `EarningsApprovalRequiredRecord` | FK to EarningRecord | 8 |
| `EarningsDeclanationRecord` | FK to EarningsRequired | 8 |
| `EmailAddress` | `ControlNumberBase` | 3e |
| `Employee` | `ControlNumberBase` | 3e |
| `EmploymentStatus` | � | 3e |
| `EmploymentStatusHistory` | `ControlNumberBase` | 3e |
| `EmployeePriorServiceCredit` | `ControlNumberBase` | 3e |
| `EngineerJobCode` | `ControlNumberBase` | 34 |
| `EngineerJobCodeDelete` | FK to EngineerJobCode | 34 |
| `EngineerPayRate` | `ControlNumberBase` | 34, 35 |
| `FillVacancyLog` | `ControlNumberBase` | 6 |
| `FRARequirements` | Static constants | 7 |
| `HoldDown` | `ControlNumberBase` | 27 |
| `HoldDownRelease` | FK to HoldDown | 27 |
| `Holiday` | `ControlNumberBase` | 20 |
| `HolidayQualifyRecord` | `ControlNumberBase` | 20 |
| `Location` | `ControlNumberBase` | � |
| `LocomotiveInspectionRecord` | `ControlNumberBase` | 34 |
| `MarkOffCode` | `ControlNumberBase` | 14 |
| `MarkOffCodeApprovalOfficer` | � | 14 |
| `MarkOffMarkUpHours` | FK to MarkOffCode | 14 |
| `MarkOffPayrollCode` | FK to MarkOffCode | 14 |
| `MarkOffRecord` | `ControlNumberBase` | 14 |
| `MarkOffRecordApproval` | FK to MarkOffRecord | 14 |
| `MarkOffRecordDelete` | FK to MarkOffRecord | 14 |
| `MarkOffRequestApproval` | FK to RequestRecord | 14 |
| `MarkOffRequestDelete` | FK to RequestRecord | 14 |
| `MarkOffRequestMarkOffRecord` | Composite | 14 |
| `MarkOffRequestMarkUpRecord` | FK to RequestRecord | 14 |
| `MarkOffRequestRecord` | `ControlNumberBase` | 14 |
| `MarkOffRequestTempRecord` | `ControlNumberBase` | 14 |
| `MarkOffRequestWaitListRecord` | `ControlNumberBase` | 14 |
| `MarkUpRecord` | FK to MarkOffRecord | 14 |
| `MovedDailyCrewPosition` | FK to DailyCrewPosition | 4 |
| `ObjectNotes` | FK to parent entity | 16 |
| `OffPropertyTieUpRecord` | � | 29 |
| `OnDutyMoveCutOffTime` | � | 5 |
| `PayRate` | `ControlNumberBase` | 35 |
| `PayrollCategory` | `ControlNumberBase` | 16 |
| `PayrollCode` | `ControlNumberBase` | 16 |
| `PayrollCodeApprovalRole` | � | 8 |
| `PayrollCodePayRate` | � | 35 |
| `PayrollCrewPositionAutoPayRecord` | � | 29 |
| `PayrollEarningProcessedRecord` | FK to EarningRecord | 16 |
| `PayrollEarningRecord` | `ControlNumberBase` | 16 |
| `PayrollHolidayRecord` | `ControlNumberBase` | 20 |
| `PayrollHolidayRecordPayrollRecord` | � | 16, 20 |
| `PayrollPeriodProcessRecord` | `ControlNumberBase` | 16 |
| `PayrollRecord` | `ControlNumberBase` | 16 |
| `PayrollRecordDelete` | FK to PayrollRecord | 16 |
| `PayrollReportGroup` | `ControlNumberBase` | 16 |
| `PayrollReviewRecord` | FK to ReviewRequired | 16 |
| `PayrollReviewRequiredRecord` | FK to PayrollRecord | 16 |
| `PhoneNumber` | `ControlNumberBase` | 3e |
| `Position` | `ControlNumberBase` | 3d |
| `PositionAlternateSupervisor` | � | 3d |
| `PositionPayRate` | � | 35 |
| `PositionRequirement` | � | 22 |
| `PositionRequirementEmployee` | � | 22 |
| `Qualification` | `ControlNumberBase` | 22 |
| `Railroad` | `ControlNumberBase` | 3a |
| `RailroadAFE` | `ControlNumberBase` | � |
| `RailroadEmployee` | `ControlNumberBase` | 3e |
| `RailroadEmployeeCalendarRequest` | `ControlNumberBase` | � |
| `RailroadEmployeeCompensableTimeRecord` | `ControlNumberBase` | 28 |
| `RailroadEmployeeReportViewedRecord` | � | � |
| `RailroadEmployeeVacationOneDayTimeRecord` | � | 33 |
| `RailroadEmployeeVacationRequest` | `ControlNumberBase` | 33 |
| `RailroadEmployeeVacationRequestAssignment` | FK to VacRequest | 33 |
| `RailroadInformationCancelRecord` | FK to InfoRecord | 26 |
| `RailroadInformationCloseRecord` | FK to InfoRecord | 26 |
| `RailroadInformationDeleteRecord` | FK to InfoRecord | 26 |
| `RailroadInformationPublishRecord` | FK to InfoRecord | 26 |
| `RailroadInformationReadbyEmployeeRecord` | � | 26 |
| `RailroadInformationRecord` | `ControlNumberBase` | 26 |
| `RailroadInformationType` | `ControlNumberBase` | 26 |
| `RailroadLocation` | `ControlNumberBase` | � |
| `RailroadLocomotiveType` | `ControlNumberBase` | � |
| `RailroadMaterial` | `ControlNumberBase` | � |
| `RailroadMaterialCategory` | `ControlNumberBase` | � |
| `RailroadPayrollDepartment` | `ControlNumberBase` | � |
| `RailroadPool` | `ControlNumberBase` | 3a |
| `RailroadPoolEmployee` | `ControlNumberBase` | 3f |
| `RailroadPoolEmployeeBulletinsViewedRecord` | � | 26 |
| `RailroadPoolEmployeePosition` | `ControlNumberBase` | 3d |
| `RailroadPoolEmployeePositionHistory` | `ControlNumberBase` | 3d |
| `RailroadPoolEmployeeTrainingDate` | `ControlNumberBase` | � |
| `RailroadPoolMarkOffAllowance` | � | 28 |
| `RailroadPoolPayrollTier` | `ControlNumberBase` | 35 |
| `RailroadPoolRequirement` | � | 22 |
| `RailroadPoolRequirementEmployee` | � | 22 |
| `RailroadPosition` | `ControlNumberBase` | 3d |
| `RailroadPositionBulletin` | `ControlNumberBase` | 15 |
| `RailroadPositionBulletinAssignment` | FK to Bulletin | 15 |
| `RailroadPositionBulletinBid` | `ControlNumberBase` | 15 |
| `RailroadPositionBulletinBidAssignment` | FK to Bid | 15 |
| `RailroadPositionBulletinNoBid` | FK to Bulletin | 15 |
| `RailroadPositionChange` | `ControlNumberBase` | 32 |
| `RailroadPositionChangeRailroadInformationRecord` | � | 26, 32 |
| `RailroadRequirement` | � | 22 |
| `RailroadRequirementEmployee` | � | 22 |
| `RailroadWorkCode` | `ControlNumberBase` | � |
| `RailroadZone` | `ControlNumberBase` | � |
| `RefreshRate` | � | � |
| `RemovedRailroadPoolEmployee` | FK to RPE | 3f |
| `Requirement` | `ControlNumberBase` | 22 |
| `RequirementDelete` | FK to Requirement | 22 |
| `Roster` | `ControlNumberBase` | 15 |
| `RosterBoard` | `ControlNumberBase` | 21 |
| `RosterBoardPosition` | FK to RailroadPosition | 21 |
| `RosterBulletinRule` | FK to Roster | 15 |
| `RosterSeniorityMoveRule` | FK to Roster | 15 |
| `Seniority` | `ControlNumberBase` | 15 |
| `SeniorityEndDate` | FK to Seniority | 15 |
| `SeniorityMove` | `ControlNumberBase` | 15 |
| `SeniorityMoveAssignment` | FK to SeniorityMove | 15 |
| `SeniorityMoveWillWork` | FK to SeniorityMove | 15 |
| `SeniorityState` | PK: StateID | 15 |
| `Shift` | `ControlNumberBase` | 3c |
| `TemporaryAssignment` | `ControlNumberBase` | 27 |
| `TemporaryAssignmentAFERecord` | � | 27 |
| `TemporaryAssignmentAssignedEmployee` | � | 27 |
| `TemporaryAssignmentRelease` | FK to TempAssignment | 27 |
| `TemporaryAssignmentWorkDay` | � | 27 |
| `WeekDay` | � | 3c |
# Part 50: Gap Analysis � FRA Complete, Crew Off-Day, CrewPosition, HoldDown

Gaps 141-155 covering FRA full logic, crew off-day math, auto mark-up timing, hold-down release, and crew position formatting.

---

## GAP 141: FRA Rest Time Formula (Missing from Part 7)

```
RestTime = 10 hours (base)
if TimeOnDuty > 12 hours:
  RestTime += (TimeOnDuty.Hours - 12) hours + TimeOnDuty.Minutes
```

`GetRestDateTime()` = OffDuty + RestTime.
`GetConsecutiveDayRestDateTime()` = OffDuty + 24 hours.

---

## GAP 142: FRA CheckFRARestCompliance Full Flow (Missing from Part 7)

```
if ConsecutiveDays < 6:
  Find next on-duty record
  if next on-duty has HoursOfService ? CheckRestForNextOnDuty()
else:
  Auto mark-off with code "SR" (Safety Rest)
  Text: "FRA required {MarkUpHours} hour safety rest"
  Send Teams SystemMessage
```

### CheckRestForNextOnDuty

```
if last record employee worked:
  nextdatetime = nextOnDuty time
  if employee has unconfirmed notification ? use EndCallTime instead
  if RestedDateTime > nextdatetime:
    Auto mark-off with code "NR" (Not Rested)
    Text: "{Name} is not rested until {RestedDateTime}"
```

---

## GAP 143: Crew.FirstWorkDay / LastWorkDay Off-Day Sum Map (Missing from Part 3c)

Hard-coded mapping from sum of off-day `WeekDayNumber` values to first/last work day:

### 3 Off-Days: FirstWorkDay

| OffDayCount Sum | FirstWorkDay |
|---|---|
| 15 | Sunday |
| 18 | Monday |
| 14 | Tuesday |
| 10 | Wednesday |
| 6 | Thursday |
| 9 | Friday |
| 12 | Saturday |

### 2 Off-Days: FirstWorkDay

| OffDayCount Sum | FirstWorkDay |
|---|---|
| 11 | Sunday |
| 13 | Monday |
| 8 | Tuesday |
| 3 | Wednesday |
| 5 | Thursday |
| 7 | Friday |
| 9 | Saturday |

LastWorkDay has the reverse mapping. Uses modular arithmetic on `WeekDayNumber`.

---

## GAP 144: CrewPosition.ShortCrewPositionName Pool 10 Format (Missing from Part 3c)

```csharp
if Pool 10: "{CrewIDName}({PositionInitial})"
else: "{CrewIDName}"
```

Also `CrewID` parsing: "Relief" crews ? last character; "XB" crews ? "XB"; otherwise ? CrewName.

---

## GAP 145: CrewPosition.PayrollCode Pool Switch (4th copy) (Missing from Part 3c)

```csharp
case 30: case 50: case 60:
    return "{PositionCode}{PayrollCode}";
default:
    return "{PayrollCode}{PositionCode}";
```

4th location of this pool-specific formatting (also in DailyCrewPosition, RailroadPoolEmployee, RailroadPosition).

---

## GAP 146: CrewPosition.GetAutomaticMarkUpDateTime � NR/SR Exception (Missing from Part 14)

```csharp
switch (morecord.MOCode)
{
    case "NR":
    case "SR":
        return markOffDateTime.AddHours(muhrs) - seconds;  // exact time minus seconds
    default:
        return markOffDateTime.Date.AddHours(muhrs).AddMinutes(1);  // midnight + hours + 1 min
}
```

"NR" and "SR" mark-ups use exact time; all others use midnight-based.

---

## GAP 147: CrewPosition.GetLastWorkDateAndOffDutyTime (Missing from Part 15)

Used for seniority move bump date calculation:
```
lastWorkDate = crew.GetLastWorkDate(bumpdate)
offDutyTime = onDutyTime + StraightTimeHours
lastWorkDateTime = lastWorkDate + offDutyTime

if lastWorkDateTime < requestDate + RequestHours
  OR bumpdate > lastWorkDateTime:
    add 7 days (next week)
```

Uses `RosterSeniorityMoveRule.RequestHours` for the comparison.

---

## GAP 148: HoldDown Release � Pool 30 Date Adjustment (Missing from Part 27)

```csharp
if (this.RailroadPosition.RailroadPoolNumber.Equals(30)) // Clerical
{
    // Set release date to the daily crew position on duty time
    if (dcposition != null) rlsdate = dcposition.AssignmentOnDutyDateTime;
}
```

Pool 30 (Clerical) adjusts hold-down release date to the actual on-duty time of the position.

---

## GAP 149: HoldDown Recursive Release (Missing from Part 27)

`ReleaseOpenHoldDownRecord()` is recursive:
```
if employee has assigned position:
  Look for another hold-down on assigned position (by different employee)
  if found and open ? recursively release that hold-down first
Release this hold-down
Re-assign daily crew positions to the original assigned employee
```

Ensures hold-down chain is properly unwound.

---

## GAP 150: Crew.AddCrewOffDayValues Magic Numbers (Missing from Part 3c)

```csharp
if (this.AddCrewOffDayValues.Equals(8) || this.AddCrewOffDayValues.Equals(14))
    offdays = this.CrewOffDays.OrderByDescending(d => d.WeekDay.WeekDayNumber).ToList();
```

Values 8 and 14 trigger reverse day ordering for off-day display. These correspond to specific day combinations.

---

## GAP 151: Crew.IsOffDay / IsWorkDay (Missing from Part 3c)

```csharp
IsOffDay(day) = CrewOffDays.Any(d => d.WeekDay.WeekDayName.Equals(day))
IsWorkDay(day) = CrewAssignments.Any(a => a.AssignmentOnDutyDay.WeekDay.WeekDayName.Equals(day))
```

Both use string comparison on day name (e.g., `"Monday"`).

---

## GAP 152: Crew.GetNextWorkDate (Missing from Part 3c)

```csharp
if (!IsWorkDay(date.DayOfWeek.ToString()))
    return GetFirstWorkDate(date, this.FirstWorkDay);
return date;
```

Advances to next work day using the FirstWorkDay property.

---

## GAP 153: Duplicated Pool-Specific Code Formatting � Summary

The job code / payroll code formatting by pool number appears in **4 separate locations**:

| Location | File |
|---|---|
| `DailyCrewPosition.JobCode` / `PayrollCode` | DailyCrewPosition.cs |
| `RailroadPoolEmployee.GetJobCode()` / `GetPayCode()` | RailroadPoolEmployee.cs |
| `CrewPosition.PayrollCode` | CrewPosition.cs |
| (implicit in) `RailroadPosition` delegates | RailroadPosition.cs |

Each uses pool number switch with the same pattern:
- Pools 30/50/60: `"{PositionCode}{X}"`
- Default: `"{X}{PositionCode}"`

---

## GAP 154: DefaultJobPaid � Duplicated in 3 Locations

| Location | File | Differences |
|---|---|---|
| `RailroadPoolEmployee.DefaultJobPaid` | RailroadPoolEmployee.cs | Pool-number switch, XB handling |
| `RailroadPosition.DefaultJobPaid` | RailroadPosition.cs | CraftName switch, no pool check |
| `DailyCrewPosition.DefaultJobPaid` | DailyCrewPosition.cs | CraftName switch, no pool check |

All share Engineer="10H1", Yardman Foreman="101F", Yardman Helper="101H".

---

## GAP 155: Crew Entity Complete Properties (Missing from Part 3c)

| Property | Type | Description |
|---|---|---|
| `AssignmentTypeControlNumber` | `long` | FK |
| `RailroadPoolControlNumber` | `long` | FK |
| `ShiftControlNumber` | `long` | FK |
| `CrewNumber` | `int` | Ordering number |
| `CrewName` | `string(250)` | Display name |
| `EffectiveDate` | `DateTime` | Default `DateTime.Today` |
| `ReliefJob` | `bool` | Whether this is a relief crew |

### Navigation
- `AssignmentType`, `CrewAbolishment`, `RailroadPool`, `Shift`
- `CrewAssignments`, `CrewPositions`, `CrewOffDays`
- `DailyAssignmentCrews`, `DailyCrewPositions`, `RailroadPoolEmployeeTrainingDates`
# Part 54: Gap Analysis � RailroadEmployee, Assignment, DailyAssignment, BoardOrder

Gaps 199-213 covering compensation time accounting, qualifying hours, vacation conversion, board ordering, and assignment entity logic.

---

## GAP 199: Compensation Time Account Entry Types (Missing from Part 28)

Three entry types in `RailroadEmployeeCompensableTimeRecord`:

| EntryType | Effect on Balance |
|---|---|
| `"Credit"` | Positive (adds hours) |
| `"Debit"` | Negative (subtracts hours) |
| `"Adjust"` | Positive or negative (corrections) |

Balance = sum(Credits) + sum(Adjustments) + sum(Debits). Debits are stored as negative values.

---

## GAP 200: Compensation Days Calculation (Missing from Part 28)

```csharp
if (type.Equals("VW"))
    return balance / 40;  // vacation weeks ? 40 hours per week
else
    return balance / 8;   // all other types ? 8 hours per day
```

Year filtering: uses `EntryDate.Year` to match selected year (current year + offset).

---

## GAP 201: Qualifying Hours � Guarantee Cap at 480 (Missing from Part 16)

```csharp
// Guarantee hours (code "13") capped at 480 (60 days � 8 hours)
if (ghrs > 480) ghrs = 480;

hours = regular hours + capped guarantee hours
```

Qualifying records: non-declined, Accumulator=true, no CompensationType (except "Holiday").

---

## GAP 202: Vacation Week Conversion Rules (Missing from Part 28)

`CanConvertVacationWeek` / `CanConvertVacation()`:

Cannot convert if:
1. VW balance < 40 hours
2. No active craft
3. Already at `Craft.MaximumVacationDayTime`
4. Non-Yardmaster pools: if InitialHours=0 AND AdditionalHours?0

New entity: `RailroadEmployeeVacationOneDayTimeRecord` with `InitialHours` and `AdditionalHours`.

---

## GAP 203: Daily Status Record � FlagCode Date Check (Missing from Part 5)

```csharp
if (sdate > 2020-03-15)
    srec.FlagCode = this.Employee.FlagCode;
```

Hard-coded date `2020-03-15` � FlagCode only populated after this date. Historical records before this date have no FlagCode.

---

## GAP 204: Employment Status History Lookup for Past Dates (Missing from Part 5)

```csharp
if (date < today):
    status = EmploymentStatusHistory.OrderByDesc
        .FirstOrDefault(h => h.StatusChangeDate <= date)
else:
    use current Employee.EmploymentStatus
```

Creates `DailyRailroadEmployeeStatusRecord` with historical status for backdated records.

---

## GAP 205: Assignment.SetBoardOrder Formula (Missing from Part 4)

```csharp
hrs = (OnDutyTime.Hours + 10)
mins = (OnDutyTime.Minutes + 10)
boardOrder = "{hrs}{mins}[locationOrder?]{typeNumber}{assignmentNumber}"
```

Pool 10 (Y&E) and Pool 40 (Mechanical): include `Location.BoardOrder` in the format.
All other pools: exclude location order.

---

## GAP 206: Assignment.GetCrewAssignment � Pool 40/50 Fallback (Missing from Part 4)

```csharp
if (assignmentday == null):
    case 40: case 50:  // Mechanical, MOW
        assignmentday = AssignmentOnDutyDays.FirstOrDefault()  // use any day
    default:
        return null  // no assignment for this day
```

Pools 40 and 50 fall back to first available day if no specific day-of-week match.

---

## GAP 207: DailyAssignment Entity Complete (Missing from Part 4)

| Property | Type | Description |
|---|---|---|
| `DailyAssignmentShiftControlNumber` | `long` | FK |
| `AssignmentControlNumber` | `long` | FK |
| `LocationControlNumber` | `long` | FK |
| `AssignmentTypeControlNumber` | `long` | FK |
| `BoardOrder` | `long` | Sort order |
| `AssignmentNumber` | `int` | Display number |
| `AssignmentName` | `string(250)` | Display name |
| `AssignmentOnDutyTime` | `TimeSpan` | Scheduled on-duty time |
| `StraightTimeHours` | `int` | Hours for this day |
| `Billable` | `bool` | Customer billing |
| `Recollectable` | `bool` | AFE recollectable |
| `EmergencyCallOut` | `bool` | Emergency flag |
| `AssignedAirPay` | `bool` | Air pay flag |
| `Notes` | `string(500)` | Optional notes |

### Navigation
- `Assignment`, `AssignmentType`, `DailyAssignmentShift`, `DailyCrew`, `Location`
- `DailyAssignmentAnnulment`, `DailyAssignmentRequest`, `DailyAssignmentAFERecord`
- `DailyCrewPositions`

---

## GAP 208: Assignment.WorkArea Default (Missing from Part 4)

```csharp
this.WorkArea = "Roustabout";  // hard-coded default
```

---

## GAP 209: RailroadEmployee.CurrentPosition � HoldDown Priority (Missing from Part 3d)

```csharp
if (any RailroadPoolEmployee.IsOnHoldDown):
    return first open HoldDown's RailroadPosition
else:
    return active seniority RailroadPoolEmployee.CurrentPosition
```

Hold-down position takes priority over assigned position.

---

## GAP 210: RailroadEmployee.LastActiveCraft Fallback (Missing from Part 3d)

```csharp
if ActiveCraft == null:
    Search last 400 DailyRailroadEmployeeStatusRecords
    Find first with StatusCode "AT" and any position records
    Return that position's craft
```

Hard-coded limit of 400 records for performance.

---

## GAP 211: Sick Day Display � Pool 30 Only (Missing from Part 28)

```csharp
if (pool 30 Clerical):
    display = "{vdays} Vacation, {pdays} Personal, {sdays} Sick"
else:
    display = "{vdays} Vacation, {pdays} Personal"  // no sick days shown
```

---

## GAP 212: Mark-Off Request Cleanup � "VW" Special Handling (Missing from Part 28)

When removing unused mark-off requests after balance depletion:

```
if type == "VW":
    Delete all requests with code starting with "V" EXCEPT "VD"
else:
    Delete requests matching exact code
```

Also removes from wait list records.

---

## GAP 213: DailyAssignment.TieupTime � Latest Off-Duty (Missing from Part 4)

```csharp
foreach position ? foreach on-duty record with off-duty:
    if EmployeeWorked AND offduty > tieuptime ? tieuptime = offduty
return latest tieuptime formatted as "hh:mm tt"
```

Returns the latest off-duty time across all positions where the employee actually worked.
# Part 55: Gap Analysis - Craft, Roster, RosterBoard, and RailroadPool Retrieval Nuances

Gaps 214-219 capture logic details verified directly in model code that were either implicit or missing in earlier sections.

---

## GAP 214: `RailroadPool.GetRailroadPoolEmployees` "AT" Search Path Drops Seniority Filter

For `empcode == "AT"`:

- **No search string** path requires active seniority in the pool (`Seniority.Any(LastActiveRoster && same pool)`).
- **With search string** path (numeric employee number or last-name prefix) filters by employment status and pool, but does **not** re-apply that seniority predicate.

Result: active-status searches can return pool members that match status but are not constrained by the same seniority condition used by the default `AT` path.

---

## GAP 215: `RailroadPool.CreateOffDays` Exception Handling Commits Scope

`CreateOffDays(...)` wraps work in a transaction scope and logs start/complete messages.

- On exception, it logs an error (`EventLogger.WriteErrorLogEvent(...)`) but does **not** rethrow.
- `scope.Complete()` is called after the catch block.

Operationally, failure is log-only from the caller perspective; method completion logging still executes.

---

## GAP 216: Clerical Hold-Down Off-Day Inserts Skip Duplicate Check

In `CreateOffDays(...)`, regular off-day employee inserts guard against duplicates:

`!db.DailyRailroadPositionOffDayEmployeeRecords.Any(r => r.RailroadPoolEmployeeControlNumber == ... && r.AssignmentDate == date)`

But in pool `30` (Clerical) hold-down branch, inserted `DailyRailroadPositionOffDayEmployeeRecord` rows are added without the same `Any(...)` duplicate guard.

---

## GAP 217: `RosterBoard.ExtraBoardPercentage` Uses Integer Division

`ExtraBoardPercentage` computes:

`Convert.ToInt32((boardcount / crewcount) * 100)`

Because both operands are integers, division truncates before multiplication.

- Example: `boardcount=1`, `crewcount=3` => `(1/3)=0` => percentage `0`.

This is the implemented behavior used by `PercentageBelowRequirement`.

---

## GAP 218: Extra Board Percentage Requirement Is Hard-Coded to 22

`RosterBoard.PercentageBelowRequirement` uses fixed threshold:

`requirement = 22`

and returns `ExtraBoardPercentage < 22`.

This requirement is not data-driven and is embedded in model logic.

---

## GAP 219: Craft Service-Day Resolution Uses Descending Threshold Match

`Craft.GetVacationDays(years)`, `GetPersonalDays(years)`, and `GetSickDays(years)` all use the same selection pattern:

1. Sort policy rows by `ServiceYears` descending.
2. Select first row where `years >= ServiceYears` (implemented via `!years.CompareTo(d.ServiceYears).Equals(-1)`).
3. Return day count (`VacationDays`/`PersonalDays`/`SickDays`), else `0` when no threshold is met.

This confirms the policy model is "highest eligible threshold wins" rather than exact-year matching.

# Part 56: Gap Analysis - Crew Scheduling Helpers and RosterBoardPosition PK/FK Behavior

Gaps 220-225 capture additional verified model logic from `Crew.cs` and `RosterBoardPosition.cs` that was not fully documented in earlier parts.

---

## GAP 220: `RosterBoardPosition` Uses FK-as-PK (Not `ControlNumberBase`)

`RosterBoardPosition` does **not** inherit `ControlNumberBase`.

- Primary key is `RailroadPositionControlNumber`
- It is also a foreign key to `RailroadPosition`
- Audit fields (`CreatedBy`, `ModifiedBy`, `CreatedDate`, `ModifiedDate`) are stored directly on the entity

This aligns with the same FK-as-PK pattern used by other 1:1 entities in the codebase.

---

## GAP 221: Roster-Board Auto Mark-Up Uses Exact-Time Calculation for All Codes

`RosterBoardPosition.GetAutomaticMarkUpDateTime(morecord)`:

1. Gets `muhrs` from `MarkOffCode.AutomaticMarkUpHours(lastActiveCraft)`
2. If `muhrs == 0`, returns `DateTime.Now`
3. Otherwise returns:
   - `MarkOffDateTime + muhrs`, then
   - subtracts the seconds component (`Subtract(new TimeSpan(0, 0, MarkOffDateTime.Second))`)

Unlike `CrewPosition`, there is no `NR/SR` special-case branch in this method.

---

## GAP 222: `Crew.GetOnDutyTime` Fallback Rule

`Crew.GetOnDutyTime(date)` behavior:

- If no crew assignments exist: returns `00:00:00`
- Else tries to match assignment by weekday name
- If no weekday match exists: falls back to `CrewAssignments.FirstOrDefault()`

So on-duty time resolution is permissive and can use the first configured assignment when day-specific mapping is absent.

---

## GAP 223: `Crew.GetCrewAssignment` Uses `SingleOrDefault`

`Crew.GetCrewAssignment(date)` uses:

`SingleOrDefault(ca => ca.AssignmentOnDutyDay.WeekDay.WeekDayName == date.DayOfWeek.ToString())`

If duplicate assignments exist for the same crew/day, this can throw (`InvalidOperationException`) rather than returning first-match.

---

## GAP 224: `Crew.GetCrewName` Suppresses Long Non-Relief Names

`Crew.GetCrewName(date)` logic:

- For non-relief crews, only uses `CrewName` when length is **4 or less**
- If that path yields empty, falls back to the current day's assignment name

This means longer non-relief crew names are intentionally replaced by assignment-derived naming in this helper.

---

## GAP 225: `Crew.GetWorkEndDateTime` Falls Back to Input Date

`Crew.GetWorkEndDateTime(movedate)`:

- If daily crew assignment exists: returns `movedate.Date + (onDutyTime + StraightTimeHours)`
- If assignment is missing: returns the original `movedate` unchanged

No meal-period adjustment is applied in this helper; it uses straight-time hours only.

# Part 57: Gap Analysis - Assignment and DailyAssignment Runtime Nuances

Gaps 226-230 add verified details from `Assignment.cs` and `DailyAssignment.cs` that were previously implicit.

---

## GAP 226: `Assignment.GetCutOffTime` Two-Stage Resolution

`GetCutOffTime(day, craft)` resolves in this order:

1. Find day-specific `AssignmentOnDutyDay` by weekday name (`SingleOrDefault`)
2. If missing, check `AssignmentOnDutyTime.CutOffTimes` for craft-specific override
3. If both missing, return `00:00:00`

So cut-off lookup is day-first, then craft fallback at the on-duty-time level.

---

## GAP 227: `Assignment.SetBoardOrder(...)` Opens a New DbContext Per Call

Inside `SetBoardOrder(int typenbr, decimal locationorder, TimeSpan ondutytime)`, the method opens a new `StrategicApplicationsContext` and re-queries pool by `RailroadPoolControlNumber` to apply pool-specific formatting.

This means board-order computation depends on a fresh database lookup each invocation (not only current navigation state).

---

## GAP 228: `DailyAssignment` Copy Constructor Does Not Copy `BoardOrder`

`DailyAssignment(long ctrlnbr, Assignment assignment)` copies key fields from `Assignment`, but `BoardOrder` assignment is explicitly commented out:

`//this.BoardOrder = assignment.BoardOrder;`

`BoardOrder` must therefore be set later (e.g., via `SetBoardOrder()`) to avoid remaining default/uninitialized.

---

## GAP 229: `DailyAssignment.IsTiedUp` Uses `Any`, Not `All`

`IsTiedUp` returns true when **any** `DailyCrewPosition` is tied up:

`DailyCrewPositions.Any(p => p.IsTiedUp)`

So assignment-level tied-up status means partial completion can flip the flag; it is not an "all positions tied up" check.

---

## GAP 230: Trainee Position Collector Can Return Duplicates

`GetCraftTraineePositions(craft)` loops positions, then loops all on-duty records and adds `onduty.DailyCrewPosition` when `onduty.IsTraining`.

Because no `Distinct`/dedupe step is applied, the same `DailyCrewPosition` can be added multiple times if it has multiple training on-duty records.

# Part 58: Gap Analysis - Shift Time Helpers and Edge-Case Behavior

Gaps 231-234 add verified `Shift` logic details from `Shift.cs` not fully captured in prior sections.

---

## GAP 231: Invalid Shift IDs Resolve to `"0"`

Both `PreviousShiftID`, `NextShiftID`, and static `GetNextShiftID(string shift)` return `"0"` for non-`1/2/3` values.

This is the sentinel value used by shift-sequencing helpers for invalid input.

---

## GAP 232: `FirstCallingTime(date)` Adds 30 Minutes and Shift-ID Seconds

When an on-duty record exists, first-calling time is calculated as:

`date + CallingTimeStart + 30 minutes + Convert.ToInt32(ShiftID) seconds`

The seconds component introduces small per-shift time offsets (`+1s`, `+2s`, `+3s`) to disambiguate otherwise equal timestamps.

---

## GAP 233: `FirstCallingTime` Null OnDuty Path Has a Null-Reference Risk

If `OnDutyTimes.FirstOrDefault()` returns null, method executes:

`date.Add(new TimeSpan(0, 0, 0));`

but does **not** return. It then still evaluates `onduty.CallingTimeStart` in the final return statement.

Result: null path can throw at runtime instead of safely returning `date`.

---

## GAP 234: `LastCallingTime(date)` Uses Midnight Special Case

`LastCallingTime` behavior:

- If no on-duty time: `date + 23:59 + ShiftID seconds`
- If `CallingTimeEnd == 23:59`: `date + CallingTimeEnd + ShiftID seconds`
- Else: `date + CallingTimeEnd + 30 minutes + ShiftID seconds`

So non-midnight calling-end values always receive the extra 30-minute offset.

# Part 59: Gap Analysis - Roster and Craft Model Specifics

Gaps 235-239 capture verified details from `Roster.cs` and `Craft.cs` to close remaining model-level documentation gaps.

---

## GAP 235: `Roster` Factory Requires Craft Control Number

`Roster` exposes only:

`Roster.CreateInstance(long craft)`

There is no zero-argument factory. Roster creation path is explicitly craft-scoped at instantiation time.

---

## GAP 236: Web-App Navigation Name Is `Seniority` (Singular)

In `StrategicApplications.Models.Roster`, the navigation property is named:

`ICollection<Seniority> Seniority`

In `SAClassLibrary` the equivalent property is `Seniorities` (plural). This naming mismatch is a cross-project model-shape nuance.

---

## GAP 237: `Craft.ApprovalOfficer` Only Uses Primary-Flag Match

`Craft.ApprovalOfficer` resolves by:

`CraftApprovalOfficers.FirstOrDefault(o => o.Primary)`

If no primary officer exists, it returns `0`; it does not fall back to a non-primary officer.

---

## GAP 238: `Craft.SetApprovalRequiredFlag(...)` Is Change-Only Persist

`SetApprovalRequiredFlag(db, approval, user, now)` updates and saves only when

`ApproveAllMarkOffs != approval`

No-op calls do not touch `ModifiedBy/ModifiedDate` and do not issue `SaveChanges()`.

---

## GAP 239: `Craft` Service-Day Lookups Are `internal`

`GetVacationDays(int years)`, `GetPersonalDays(int years)`, and `GetSickDays(int years)` are declared `internal` in `StrategicApplications` model code.

They are intended for in-assembly domain logic consumption rather than public API exposure.

# Part 60: Gap Analysis - DailyAssignmentShift and DailyAssignmentRequest Behaviors

Gaps 240-245 add verified runtime details from `DailyAssignmentShift.cs` and `DailyAssignmentRequest.cs`.

---

## GAP 240: `DailyAssignmentShift` Time Properties Re-query DB Per Access

`IsHoliday`, `FirstOnDutyStartTime`, `FirstCallingStartTime`, `FirstCallingEndTime`, `LastOnDutyStartTime`, `LastCallingStartTime`, and `LastCallingEndTime` each open a new `StrategicApplicationsContext` and query related data during property access.

These are not cached calculations and may incur repeated database calls.

---

## GAP 241: `DailyAssignmentShift` On-Duty-Time Accessors Assume At Least One Row

Several properties call:

`shift.OnDutyTimes.OrderBy(...).FirstOrDefault().<property>`

without null-checking the `FirstOrDefault()` result. If a shift has no `OnDutyTimes`, these properties can throw at runtime.

---

## GAP 242: `LastCallingEndTime` Has Midnight-Rollover Special Case

When last `CallingTimeEnd == 00:00:00`, `LastCallingEndTime` returns:

`AssignmentDate + 1 day + 00:00:00`

Otherwise it returns `AssignmentDate + CallingTimeEnd` on the same date.

---

## GAP 243: Duplicate Call-Sheet Check Is Shift/Date Only (Pool Not Included)

Inside private `CreateDailyAssignmentShift(...)`, duplicate detection uses:

`FirstOrDefault(a => a.AssignmentDate == this.AssignmentDate && a.ShiftControlNumber == this.ShiftControlNumber)`

`RailroadPoolControlNumber` is not part of this predicate.

---

## GAP 244: `CallSheetInProgress` Flag Can Remain True on Exception Path

`MvcApplication.CallSheetInProgress[this.RailroadPoolControlNumber]` is set to `true` at method start and reset to `false` only at the normal end of method.

If an exception is thrown before the final reset, this in-progress guard can remain stuck `true` for that pool.

---

## GAP 245: `DailyAssignmentRequest` Uses FK-as-PK and Create-Only Audit

`DailyAssignmentRequest`:

- Does not inherit `ControlNumberBase`
- Uses `DailyAssignmentControlNumber` as both PK and FK
- Stores `CreatedBy`/`CreatedDate` only (no `ModifiedBy`/`ModifiedDate`)

This models a one-request-per-daily-assignment shape with create-time audit only.

# Part 61: Gap Analysis - Client/Railroad Automation Disable and Holiday Export Details

Gaps 246-250 document additional verified behavior from `Client.cs` and `Railroad.cs`.

---

## GAP 246: `Client.DisableRailroadAutoFunctions` Marks Client Entity, Not Railroad

Inside the railroad loop, method sets:

`db.Entry(this).State = EntityState.Modified`

after changing `railroad.AutoAssignments`.

So explicit state-marking targets `Client` (`this`) rather than the modified `Railroad` row.

---

## GAP 247: `Railroad.DisableRailroadPoolAutoFunctions` Marks Railroad Entity, Not Pool Rows

Inside pool loop, method updates pool automation flags, then sets:

`db.Entry(this).State = EntityState.Modified`

`this` is the `Railroad`, not each `RailroadPool`. Persistence therefore relies on tracked pool change detection instead of explicit per-pool state marking.

---

## GAP 248: Holiday Payroll Record Creation Is File-Queue Based (`.HR`)

`Railroad.CreatePayrollHolidayRecords(...)` does not directly insert payroll holiday rows in this method path; it writes `.HR` request files to inbound queue path for downstream processing.

Payload format (tab-delimited):

`"{HolidayName} {Year}\t{EmpNbr_FullName}\t{RailroadControlNumber}\t{RPEControlNumber}\t{HolidayControlNumber}"`

---

## GAP 249: Holiday Export Path Uses Production Inbound Constant

The method uses:

`path = MvcApplication.inbound`

and does not branch to `dev_inbound` inside this model method.

---

## GAP 250: Holiday Export Method Is Log-and-Continue on Exceptions

`CreatePayrollHolidayRecords(...)` wraps full flow in try/catch and only logs on failure (`EventLogger.WriteErrorLogEvent(...)`) without rethrowing.

Caller-visible behavior is non-throwing even when holiday export fails.

# Part 62: Gap Analysis - Additional DailyCrewPosition Edge Cases

Gaps 251-254 capture extra verified behaviors from `DailyCrewPosition.cs` that were not explicitly documented earlier.

---

## GAP 251: `DailyCrewPosition.IsTiedUp` Lacks Null Guard

`IsTiedUp` checks `this.DailyCrewPositionOnDutyRecords.Count.Equals(0)` before evaluating `All(...)`, but does not first null-check the collection reference.

If navigation collection is null (not loaded/initialized), property access can throw.

---

## GAP 252: `GetCutOffDateTime(date)` Uses `AssignmentDate` Weekday, Not Input Date Weekday

Method computes:

`day = this.AssignmentDate.DayOfWeek.ToString()`

and then resolves cutoff via assignment/day mapping. The `date` parameter is used only as a base timestamp to apply the resulting cutoff time.

---

## GAP 253: Department Number Uses Blind `Substring(1)`

`DepartmentNumber` returns `Position.RailroadPayrollDepartment.DepartmentNumber.Substring(1)` when department exists, without length guard.

If department number is a single character or empty, this computation can fail.

---

## GAP 254: `DeletePosition(...)` Only Restores Extra-Board Order for First Assignment Link

For each on-duty record, reset logic uses:

`record.DailyShiftExtraBoardPositionAssignments.FirstOrDefault()`

Only that first linked assignment (if any) is used to restore `BoardOrder`/`TieUpOrder` before deletion.

# Part 63: Gap Analysis - DailyCrewPositionOnDutyRecord Additional Risks/Assumptions

Gaps 255-259 capture additional verified details from `DailyCrewPositionOnDutyRecord.cs`.

---

## GAP 255: `JobCode` Length Constraint Can Conflict with Pool-50 Job-Code Source

`DailyCrewPositionOnDutyRecord.JobCode` is constrained to length 4 (`[StringLength(4, MinimumLength = 4)]`).

However, upstream `DailyCrewPosition.JobCode` for pool 50 (MoW) returns `AssignmentName` (free-form string), which can exceed 4 characters.

---

## GAP 256: `IsFRAComplete` Uses `All(...)` Without Null Guard

`IsFRAComplete` directly evaluates:

`DailyCrewPositionOnDutyFRARecords.All(r => r.Completed)`

without null-checking the collection reference first.

---

## GAP 257: `CanMoveToForeman` Null Check Against `Where(...)` Result Is Ineffective

Method does:

`var positions = ...Where(...); if (positions == null) return false;`

`Where(...)` returns a non-null enumerable; practical empty-case handling occurs only during iteration.

---

## GAP 258: Off-Duty FRA Recheck Path Assumes CurrentPosition Craft Availability

After tie-up save, method evaluates:

`if (craft.HoursofService || this.RailroadPoolEmployee.RailroadEmployee.CurrentPosition.Craft.HoursofService)`

The second operand assumes `CurrentPosition` and its `Craft` are available.

---

## GAP 259: On-Duty Record "Update" Is Implemented as Create-New + Delete-Old

`UpdateDailyCrewPositionOnDutyRecord(...)` creates a new on-duty row, copies fields, adds it, then removes the current row (`db.DailyCrewPositionOnDutyRecords.Remove(this)`).

So update semantics are record replacement rather than in-place mutation.

# Part 64: Gap Analysis - ApplicationUtilities and App-Pool Control Helpers

Gaps 260-265 capture additional verified utility-layer behavior from `ApplicationUtilities.cs` and `RestartApplicationPool/Program.cs`.

---

## GAP 260: Snapshot Transaction Helper Method Name Is `CreateShapshot`

`ApplicationUtilities.TransactionScopeBuilder` exposes snapshot builder method with misspelled name:

`CreateShapshot()`

Behavior is snapshot isolation + 30-minute timeout, but call-site API name is typo-preserved.

---

## GAP 261: App-Pool Restart Helper Starts External Process Without Waiting

`ApplicationUtilities.RestartApplicationPool(poolname, user)` starts `RestartApplicationPool.exe` via `Process.Start(...)` and immediately sends Teams support message; it does not wait for exit code/completion.

---

## GAP 262: Teams Sender Builds New `HttpClient` Per Call

`SendTeamsMessage(message, uri)` constructs a new `HttpClient` for each send and posts JSON body to `BaseAddress`.

No client reuse pool is implemented in this helper.

---

## GAP 263: `GetDatabaseName` Uses String-Delimiter Parsing Assumptions

`GetDatabaseName(connString)` extracts between:

- start token: `"Initial Catalog="`
- end token: `";Integrated"`

This parsing assumes that exact delimiter pattern exists in the connection string format.

---

## GAP 264: IP Subnet Check Supports `*` Wildcards Per Octet

`CheckOnPropertyIPAddress(inbndIP)`:

- first checks exact-match entries
- for dotted IPv4, splits octets and treats `*` in configured subnet octets as wildcard matches

---

## GAP 265: Console Restart Utility Logs Failures but Does Not Propagate Errors

`RestartApplicationPool/Program.cs` wraps stop/start loop in try/catch; failures are logged through `SAClassLibrary.Utilities.EventLogger` and then swallowed (no rethrow / non-zero exit signaling).

# Part 65: Gap Analysis - Roster Bulletin and Seniority-Move Rule Entities

Gaps 266-270 capture additional verified details from `RosterBulletinRule.cs` and `RosterSeniorityMoveRule.cs`.

---

## GAP 266: Both Rule Entities Use FK-as-PK 1:1 Shape

`RosterBulletinRule` and `RosterSeniorityMoveRule` both use:

`[Key, ForeignKey("Roster"), DatabaseGenerated(None)] public long RosterControlNumber { get; set; }`

Neither inherits `ControlNumberBase`.

---

## GAP 267: `RosterBulletinRule` Constructor Applies Hard-Coded Default Times

Defaults at construction:

- `BulletinStartTime = 04:00:00`
- `BulletinCloseTime = 04:00:00`
- `BulletinEffectiveTime = 04:00:00`
- `BulletinCutOffTime = 15:00:00`
- `BulletinHours = 24`

---

## GAP 268: `SelectedDay` Is Real-Time Cutoff Comparator

`SelectedDay` delegates to `SelectDayByCutoffTime()`:

`Convert.ToInt32(DateTime.Now.TimeOfDay > BulletinCutOffTime)`

Returns `1` after cutoff, else `0`.

---

## GAP 269: `RosterSeniorityMoveRule` Has No Constructor Defaults

`RequiredDays`, `RequestHours`, and `CancelHours` are required fields but are not assigned defaults in constructor logic.

---

## GAP 270: Rule Factories Are Roster-Scoped Only

Both entities expose only `CreateInstance(long roster)` factory methods, keeping creation explicitly tied to an existing roster key.

# Part 66: Gap Analysis - `StrategicApplicationsContext` Subtleties

Gaps 271-275 capture additional verified DbContext-level behavior from `StrategicApplicationsContext.cs`.

---

## GAP 271: Context Constructor Hard-Binds to Demo Connection Name

`StrategicApplicationsContext()` calls:

`base("StrategicApplicationsDemoContext")`

and applies `CreateDatabaseIfNotExists` initializer in constructor. Runtime context selection is therefore constructor-bound unless code/config is changed.

---

## GAP 272: Concurrency Retry Path Assumes Exactly One Entry

On `DbUpdateConcurrencyException`, retry logic calls:

`ex.Entries.Single().Reload()`

before retrying `base.SaveChanges()`. This assumes a single conflicting entry in exception payload.

---

## GAP 273: `SaveChangesAsync` Validation Catch Is Non-`await` Pattern

`SaveChangesAsync(CancellationToken)` returns `base.SaveChangesAsync(...)` directly inside try block (no `async/await`).

As implemented, validation exceptions that surface asynchronously after task return are not handled by this local try/catch scope.

---

## GAP 274: Strategic Context Exposes Manual `UserRoles` DbSet

In addition to Identity base sets, context explicitly declares:

`DbSet<IdentityUserRole> UserRoles`

which is also listed among domain DbSets.

---

## GAP 275: Several DbSet Property Names Preserve Legacy Typos

Examples include:

- `DailyShiftExtraBoardPostionPayrollRecords`
- `DailyShiftExtraBoardPostionAssignments`
- `DailyRosterBoardPostionHangoutRecords`

These property names intentionally retain historical "Postion" spelling and are part of live context API surface.

# Part 67: Gap Analysis - `SAClassLibraryContext` Mapping/Behavior Additions

Gaps 276-280 capture additional verified behavior from `SAClassLibraryContext.cs`.

---

## GAP 276: SAClassLibrary Context Also Hard-Binds to Demo Connection Name

Constructor uses:

`base("name=SAClassLibraryDemoContext")`

with initializer disabled (`SetInitializer(null)`). Runtime selection is constructor-bound unless code/config is changed.

---

## GAP 277: No Custom `SaveChanges` / `SaveChangesAsync` Overrides in SAClassLibrary Context

Unlike web `StrategicApplicationsContext`, `SAClassLibraryContext` does not override save methods for concurrency retry or validation-error tracing.

---

## GAP 278: SAClassLibrary Model Uses Many-to-Many Mapping for `Craft` ↔ `Requirement`

`OnModelCreating` includes:

`Craft.HasMany(e => e.Requirements).WithMany(e => e.Crafts).Map(... "CraftRequirements" ...)`

This explicit M:N join-table mapping differs from web-model shape that exposes `CraftRequirement` entity directly.

---

## GAP 279: `AssignmentOnDutyDay` ↔ `CrewAssignment` Is Optional 1:1 with Cascade Delete

`OnModelCreating` configures:

`AssignmentOnDutyDay.HasOptional(e => e.CrewAssignment).WithRequired(e => e.AssignmentOnDutyDay).WillCascadeOnDelete()`

This is an explicit optional-dependent 1:1 mapping with cascade behavior.

---

## GAP 280: SAClassLibrary Context Includes Identity-Like Tables as First-Class DbSets

Context exposes `AspNetUsers`, `AspNetRoles`, `AspNetUserClaims`, `AspNetUserLogins` directly as entity sets, indicating mixed identity/domain data access in the same EF model.

# Part 68: Gap Analysis - Position and RailroadPosition Additional Nuances

Gaps 281-286 capture additional verified behavior from `Position.cs` and `RailroadPosition.cs`.

---

## GAP 281: `Position.PositionInitial` Assumes Non-Empty Name

`PositionInitial` is implemented as `PositionName.Substring(0, 1)` with no null/length guard.

---

## GAP 282: `RailroadPosition.CurrentHoldDown` Uses `SingleOrDefault`

`CurrentHoldDown` resolves with:

`HoldDowns.SingleOrDefault(h => h.HasReleaseRecord == false)`

If more than one open hold-down exists, this can throw instead of selecting one.

---

## GAP 283: `ManualAssign` Requires Sentinel Position Type `"D"`

`RailroadPosition.ManualAssign(...)` looks up a default railroad position by `PositionType == "D"` and uses that control number as assignment reference for manual assignment flow.

---

## GAP 284: Extra-Board Assign Wait Loop Assumes Dictionary Key Exists

In `Assign(...)`, extra-board path spins on:

`while (MvcApplication.ExtraBoardInProgress[this.RailroadPoolControlNumber]) Thread.Sleep(1000);`

This assumes the pool key already exists in `ExtraBoardInProgress` dictionary.

---

## GAP 285: `BoardName`/`BoardOrCrewName` Composition Can Duplicate Roster Name

`BoardName` already prefixes roster name (`RosterName + " " + BoardName`), while `BoardOrCrewName` for board positions returns `RosterName + " " + BoardName`, producing a doubled roster prefix in board path.

---

## GAP 286: `CreateDailyRailroadPositionOffDayEmployeeRecord` Uses `>=` Date Filter

Off-day records are selected with:

`DailyRailroadPositionOffDayRecords.Where(r => !r.AssignmentDate.CompareTo(adate.Date).Equals(-1))`

which includes records on/after assignment date (not just exact-date match).

# Part 69: Gap Analysis - RailroadPoolEmployeePosition Semantics

Gaps 287-291 capture additional verified details from `RailroadPoolEmployeePosition.cs`.

---

## GAP 287: `RailroadPoolEmployeePosition` Uses FK-as-PK and Create-Only Audit

Entity uses `RailroadPositionControlNumber` as PK/FK (1:1 to `RailroadPosition`) and stores `CreatedBy`/`CreatedDate` only.

No `ModifiedBy` / `ModifiedDate` fields exist on this assignment-link row.

---

## GAP 288: Factory Allows Zero Employee Control Number

`CreateInstance(long position, long rpemployee = 0)` allows default `rpemployee` value `0`, enabling construction before employee linkage is set.

---

## GAP 289: `GetBumpDate()` Applies Craft Gating

Custom bump-date logic is only implemented for craft names `Engineer` and `Yardman`; default branch returns `DateTime.Now`.

---

## GAP 290: Hangout Assignment Date Uses Confirmed-Notification + 48 Hours

`GetHangoutAssignmentDateTime()` locates matching change notification and returns:

`notify.NotifyDateTime.AddHours(48)`

when a confirmed notification is found.

---

## GAP 291: Hangout Date Fallback Sentinel Is `9999-12-31`

If lookup fails (or errors), method returns `new DateTime(9999, 12, 31)` as no-date sentinel.

# Part 70: Gap Analysis - IdentityModels and Role-Assignment Nuances

Gaps 292-296 capture additional verified behavior from `IdentityModels.cs`.

---

## GAP 292: `ApplicationUser.GetUserName` Uses Direct Dictionary Indexer

`GetUserName(string user)` returns:

`MvcApplication.ActiveUsers[user].FullName`

No key-existence guard is applied; missing user key can throw.

---

## GAP 293: `ApplicationUser.AddRole` Adds `IdentityUserRole` but Does Not Save

`AddRole(db, roleid)` inserts into `db.UserRoles` when absent and sends officer message, but does not call `db.SaveChanges()` within the method.

Persistence is deferred to caller transaction flow.

---

## GAP 294: `SendOfficerMessage` Role Check Compares Role Objects to String Literal

Method condition:

`if (!this.Roles.Any(r => r.Equals("Railroad Employee")))`

`this.Roles` contains role-link objects, so this comparison does not compare role names directly.

---

## GAP 295: ApplicationUser Name Formatting Methods Assume Non-Empty Core Name Fields

Computed properties (`Initials`, `EmployeeNumber_Initials`, `Initials_LastName`, etc.) use `Substring(0, 1)` on first/last (and optionally middle) name values without additional length guards.

---

## GAP 296: IdentityManager Uses Shared Context Field Across Operations

`IdentityManager` keeps a single `StrategicApplicationsContext db` field and creates stores/managers against that shared context for each operation until `Dispose()` is called.

# Part 71: Gap Analysis - Service Utility Implementation Details

Gaps 297-301 capture additional verified implementation details from service host/utility code.

---

## GAP 297: Daily-Call-Sheet Service Utility Uses Local Timestamp Conversion Without Culture Argument

`SADailyCallSheetService.Utilities.ServiceUtilities.CreateNewControlNumber()` uses:

`Convert.ToInt64(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"))`

without explicit culture parameter.

---

## GAP 298: MSMQ Sender Uses Direct Queue Construction Without Existence Guard

`CreateMSMQMessage(...)` constructs `new MessageQueue(qname)` directly from formatted path and sends immediately.

No pre-check/creation flow for missing queues is present in this helper.

---

## GAP 299: MSMQ Send Uses Automatic Transaction Type

Service utility sends with:

`queue.Send(message, MessageQueueTransactionType.Automatic)`

which relies on automatic transactional behavior of target queue/environment.

---

## GAP 300: Service Utility Queue Object Is Not Explicitly Disposed

`MessageQueue` is instantiated as local variable and used for send; helper does not wrap it in a `using` scope.

---

## GAP 301: Executable Service Hosts Register Multiple `ServiceBase` Instances in One Process

`Program.cs` in each service project builds a `ServiceBase[]` and passes all configured services to `ServiceBase.Run(...)` (e.g., 6 services in `SADailyCallSheetService`, 2 in each of payroll and AtHoc hosts).

# Part 72: Gap Analysis - RailroadPoolEmployee Additional Property Semantics

Gaps 302-306 capture additional verified behavior from `RailroadPoolEmployee.cs`.

---

## GAP 302: `IsHelperOnly` Opens a New DbContext During Property Evaluation

`IsHelperOnly` creates `new StrategicApplicationsContext()` inside getter to load roster positions, then checks foreman qualification.

This property is not purely in-memory and can execute DB I/O per access.

---

## GAP 303: `CurrentNotifiedDateTime` Assumes Assigned Position Exists

Getter returns:

`GetCurrentNotifiedDateTime(this.AssignedPosition.RailroadPoolEmployeePosition.AssignedDate)`

No null guards are applied before dereferencing `AssignedPosition` chain.

---

## GAP 304: `LastActivePosition` Returns `AssignedPosition` When `CurrentPosition` Exists

`LastActivePosition` logic:

- if `CurrentPosition == null`: uses latest position history
- else: returns `AssignedPosition`

So open hold-down `CurrentPosition` does not directly flow through this property when assignment exists.

---

## GAP 305: `SeniorityDate_Rank` Uses First Active Seniority Without Ordering

When active seniority exists, property uses `this.Seniority.First(s => s.SeniorityState.Active)` and adds `Rank` seconds to `RosterDate`.

No explicit order is applied when multiple active entries are present.

---

## GAP 306: `BumpDate` Falls Back to "Now on Today" When Unassigned

If `RailroadPoolEmployeePositions.Count == 0`, `BumpDate` returns:

`DateTime.Today.Date.Add(DateTime.Today.TimeOfDay)`

instead of a neutral/sentinel value.

# Part 73: Gap Analysis - Open-File Reconciliation (`Roster` / `Craft`)

Gaps 307-311 provide additional open-file reconciliation details from `StrategicApplications\Models\Roster.cs` and `StrategicApplications\Models\Craft.cs`.

---

## GAP 307: `Roster` Is Minimal Data Model (No Computed/Behavioral Properties)

`Roster` in web model is primarily structural: required fields + navigation sets + simple factory; it does not implement additional computed display helpers in this class file.

---

## GAP 308: `Roster` Constructor/Factory Pattern Is Craft-Scoped Only

Instantiation path is:

- private ctor `Roster(long craft)` sets `CraftControlNumber`
- public `CreateInstance(long craft)` wraps it

No alternative factory overloads are present in this file.

---

## GAP 309: `Craft.CreateInstance()` and `Craft.CreateInstance(long)` Initialize Differently

- `CreateInstance()` -> private `Craft(string name)` -> initializes `CraftName` to empty string
- `CreateInstance(long pool)` -> private `Craft(long pool)` -> initializes pool link only

So empty-name initialization is not applied in pool-scoped factory path.

---

## GAP 310: `Craft` Service-Year Day Lookup Methods Are Internal Domain Helpers

`GetVacationDays`, `GetPersonalDays`, and `GetSickDays` are non-public (`internal`) and are intended for domain computations within assembly boundaries.

---

## GAP 311: `Craft.SetApprovalRequiredFlag` Is Immediate Persist on Change

When `ApproveAllMarkOffs` value changes, method updates modified audit fields, marks entity `Modified`, and calls `SaveChanges()` immediately in-method.

# Part 74: Gap Analysis - Global Runtime Watcher/Startup Nuances

Gaps 312-316 capture additional verified behavior from `Global.asax.cs` startup/watcher logic.

---

## GAP 312: `inbounderror` Is Initialized Before Environment Inbound Switch

`inbounderror` is declared once as `inbound + "\\Processing Error"` at field initialization time.

Later in non-production startup path, `inbound` is reassigned to `dev_inbound`, but `inbounderror` field is not recomputed.

---

## GAP 313: DEBUG Startup Disables Timer Setup Flag

In `#if DEBUG` path:

- `delay = 300`
- `settimers = false`

This changes startup scheduling behavior relative to non-debug runtime.

---

## GAP 314: Production/Dev Watchers Share the Same Processing Flags

Both production and development watcher handlers gate on shared booleans (`HolidayRecordsProcessing`, `VacancyRecordsProcessing`, `StatusRecordsProcessing`) rather than separate prod/dev flags.

---

## GAP 315: `CreateWatchers()` Uses TransactionScope for FileSystemWatcher Initialization

Watcher creation is wrapped in `ApplicationUtilities.TransactionScopeBuilder.CreateReadCommitted()` even though this routine is file-system watcher setup logic.

---

## GAP 316: Delayed Startup Scan Explicitly Triggers Watcher Handlers

`FireDelayedEvent(...)` disables delay timer and directly calls trigger handlers (prod and/or dev) to process existing queue files at startup rather than waiting for only new file events.

# Part 75: Gap Analysis - Global Timer-Initialization Edge Cases

Gaps 317-321 capture additional verified `Global.asax.cs` timer bootstrap nuances.

---

## GAP 317: `CreateTimers()` Client Loop Re-queries All Railroads

Inside each auto-enabled client iteration, code uses:

`var railroads = db.Railroads.Where(r => r.RailroadPools.Count > 0);`

without filtering by current client control number.

---

## GAP 318: Railroad Loop Re-queries All Pools

Inside each auto-enabled railroad iteration, pool source is:

`var pools = db.RailroadPools.ToList();`

without filtering by current railroad control number.

---

## GAP 319: Daily Call-Sheet Timer Is Set Even When `settimers == false`

`CreateTimers()` always creates/updates daily call-sheet timers and invokes `SetDailyCallSheetTimer(pool.ControlNumber)` outside the `settimers` guard used by most other timer categories.

---

## GAP 320: Timer Interval Uses `(int)TotalMilliseconds` Cast Across Schedulers

Multiple `SetXxxTimer` routines set interval via:

`timer.Interval = (int)((update - DateTime.Now).TotalMilliseconds)`

This narrows a `double` duration into `int` before assignment.

---

## GAP 321: `SetCreateHolidayDatesTimer` Date Recomposition Uses Current Month/Day Pattern

When computed update is in the past, method reassigns with:

`new DateTime(update.Year, now.Month, now.AddDays(1).Day, 0, 1, 0)`

using current-month and tomorrow-day components rather than preserving original target month/day semantics.

# Part 76: Gap Analysis - Payroll Import Service Implementation Details

Gaps 322-326 capture additional verified details from `SAImportADPPayrollService.cs` and `SAImportUKGPayrollService.cs`.

---

## GAP 322: Service `OnStop()` Uses Error-Level Logging

Both ADP and UKG import services log stop events through `EventLogger.WriteErrorLogEvent(...)` rather than information-level logging.

---

## GAP 323: ADP Corrected-Department Report Path Concatenation Omits Directory Separator

ADP corrected department report file path is built as:

`string.Format("{0}{1}.txt", path, filename)`

which concatenates directly to the import root path string.

---

## GAP 324: Unprocessed Rows Are Persisted as `.np` Side Files in Error Folder

Both import services write unprocessed lines to:

`<errorpath>\<original-file-name>.np`

and delete the `.np` file when no unmatched lines remain.

---

## GAP 325: ADP `LineCount` and UKG `LineCount` Skip Different Header Markers

- ADP count excludes lines containing `"DP1"`
- UKG count excludes lines containing `"Employee Number"`

---

## GAP 326: ADP Meal-Period Historical Handling Uses `firstamount` Pairing State

For payroll code `18` special handling, ADP parser keeps a running `firstamount` value to pair split records across consecutive lines before applying paid totals.

# Part 77: Gap Analysis - AtHoc Service Implementation Nuances

Gaps 327-331 capture additional verified behavior from `SAAssignmentCallService.cs` and `SAAssignmentOnDutyService.cs`.

---

## GAP 327: Assignment-Call Service Initializes Against First Railroad Only

Startup timer tick resolves railroad context via:

`railroad = db.Railroads.FirstOrDefault();`

before setting message timer, rather than iterating all railroads.

---

## GAP 328: Assignment-Call Publish Cycle Advances `nextcalltime` by +5 Minutes Before Query

At start of publish handler, code does:

`nextcalltime = nextcalltime.AddMinutes(5);`

and then queries vacancies based on that adjusted call time.

---

## GAP 329: On-Duty Service Post-Run Includes Fixed 1-Minute Sleep

After message processing and save, on-duty service sleeps exactly one minute before recalculating timer schedule.

---

## GAP 330: On-Duty Timer Scheduling Requires Positive Interval

`SetAtHocMessageTimer(pool)` computes interval and enables timer only when `(update - now).TotalMilliseconds > 0`; non-positive intervals are not scheduled.

---

## GAP 331: AtHoc Service Stop Events Are Logged Through Error-Level API

`SAAssignmentOnDutyService.OnStop()` logs service stop via `WriteErrorLogEvent(...)` rather than information-level log method.

# Part 78: Gap Analysis - Daily On-Duty / Mark-Off Service Queue Handling Nuances

Gaps 332-336 capture additional verified behavior from `SADailyOnDutyRecordService.cs` and `SADailyOnDutyMarkOffRecordService.cs`.

---

## GAP 332: Mark-Off Service Startup Log Text Uses Crew-Position Wording

`SADailyOnDutyMarkOffRecordService.OnStart(...)` log message states "Daily Crew Position Service started" although service class is mark-off specific.

---

## GAP 333: Queue Listener Setup Uses `MessageQueue` Instances Without `using`

Both services create `MessageQueue` objects for begin-peek listeners without explicit disposal scope in startup helper methods.

---

## GAP 334: On-Duty Record Service Uses Static `fields` Array for Message Parsing

`SADailyOnDutyRecordService` declares static `string[] fields;` and reuses it in queue callback, making parsed message storage shared across callback invocations.

---

## GAP 335: On-Duty Record Service Temporarily Disables EF Auto-Detect-Changes

`ReadDailyOnDutyRecordQueue(...)` sets:

`db.Configuration.AutoDetectChangesEnabled = false;`

for processing scope and restores it in `finally`.

---

## GAP 336: Queue Re-Peek Is Triggered on Success Path Only

Both queue readers call `msgqueue.BeginPeek()` at end of successful processing flow; exception paths log errors but do not re-arm peek in catch block.

# Part 79: Gap Analysis - DailyAssignment / DailyAssignmentShift Service Queue Details

Gaps 337-341 capture additional verified behavior from `SADailyAssignmentService.cs` and `SADailyAssignmentShiftService.cs`.

---

## GAP 337: Both Services Use Static Parsed-Message Arrays (`fields`)

Each service stores message body split results in a static `string[] fields`, which is then consumed by downstream helper methods in the same class.

---

## GAP 338: `SADailyAssignmentService.GetAssignmentCrew` Uses `Contains(dayname)`

Crew join filter uses weekday match via `WeekDayName.Contains(dayname)` (not strict equality), then returns `SingleOrDefault()`.

---

## GAP 339: Crew-Position Message `process` Flag Reads from Shared `fields[10]`

`CreateDailyCrewPositionMessage(...)` determines whether to emit queue message using:

`var process = Convert.ToBoolean(fields[10]);`

This value comes from class-level parsed message array state.

---

## GAP 340: Assignment Message `process` Flag in Shift Service Comes from `fields[3]`

`SADailyAssignmentShiftService.CreateDailyAssignmentMessage(...)` appends process value using class-level parse array index:

`body.Append(string.Format("{0}", fields[3]));`

---

## GAP 341: Queue Re-Peek and Stop Logging Patterns Match Other Services

- `msgqueue.BeginPeek()` is called on success path after message handling
- `OnStop()` in both services logs via `WriteErrorLogEvent(...)`

# Part 80: Gap Analysis - Cross-Project Model Naming Divergences

Gaps 342-346 capture additional verified naming-shape differences between `StrategicApplications` model classes and `SAClassLibrary` model classes.

---

## GAP 342: Craft Pay-Code Entity Name Differs by Project

- Web app model: `CraftPayCodes`
- SAClassLibrary model: `CraftPayCode`

This affects navigation/property names and generated metadata shapes.

---

## GAP 343: Craft Personal-Day Entity Name Differs by Project

- Web app model: `CraftPersonalDays`
- SAClassLibrary model: `CraftPersonalDay`

---

## GAP 344: Craft Sick-Day Entity Name Differs by Project

- Web app model: `CraftSickDays`
- SAClassLibrary model: `CraftSickDay`

---

## GAP 345: Craft Vacation-Day Entity Name Differs by Project

- Web app model: `CraftVacationDays`
- SAClassLibrary model: `CraftVacationDay`

---

## GAP 346: Roster Seniority Collection Name Differs by Project

- Web app model: `Roster.Seniority`
- SAClassLibrary model: `Roster.Seniorities`

Cross-project reflection/mapping code must account for these pluralization differences.

# Part 81: Gap Analysis - AssignmentOnDutyDay / CrewAssignment Shape Details

Gaps 347-351 capture additional verified behavior from `AssignmentOnDutyDay.cs` and `CrewAssignment.cs`.

---

## GAP 347: `CrewAssignment` Uses FK-as-PK Pattern

`CrewAssignment` primary key is `AssignmentOnDutyDayControlNumber` and is also FK to `AssignmentOnDutyDay` (1:1-style dependent row).

---

## GAP 348: `CrewAssignment` Stores Create Audit Only

Entity includes `CreatedBy`/`CreatedDate` but no `ModifiedBy`/`ModifiedDate` properties.

---

## GAP 349: `Assignment_ReliefName` Suffix Is Applied Only for Relief Shift Crews

`CrewAssignment.Assignment_ReliefName` returns:

- `"{AssignmentName} ({CrewID})"` when `Crew.Shift.ReliefShift == true`
- otherwise plain `AssignmentName`

---

## GAP 350: `AssignmentOnDutyDay` Defaults `StraightTimeHours` to 8

Constructor initializes `StraightTimeHours = 8` before assignment-day-specific overrides.

---

## GAP 351: Assignment-Day Cutoff Lookup Uses `SingleOrDefault`

`AssignmentOnDutyDay.GetCutOffTime(craft)` resolves craft cutoff from `AssignmentOnDutyTime.CutOffTimes.SingleOrDefault(...)`; duplicate craft cutoff rows would raise runtime exception.

# Part 82: Gap Analysis - WeekDay and CrewOffDay Structural Details

Gaps 352-356 capture additional verified details from `WeekDay.cs` and `CrewOffDay.cs`.

---

## GAP 352: `CrewOffDay` Uses Composite Key (Crew + WeekDay)

`CrewOffDay` key is composite:

- `CrewControlNumber` (Order 1)
- `WeekDayControlNumber` (Order 2)

with both marked `DatabaseGenerated(None)`.

---

## GAP 353: `CrewOffDay` Does Not Inherit `ControlNumberBase`

No `ControlNumber` primary key and no create/modify audit columns are defined in this entity.

---

## GAP 354: `CrewOffDay.CreateInstance(long crew)` Seeds Crew Only

Factory constructor sets `CrewControlNumber` only; `WeekDayControlNumber` must be assigned separately before persistence.

---

## GAP 355: `WeekDay` Is Client-Scoped Master Data

`WeekDay` includes required `ClientControlNumber`, meaning weekday definitions are modeled per-client rather than a global static lookup table.

---

## GAP 356: `WeekDay` Drives Multiple Scheduling Link Types

`WeekDay` navigations include:

- `CrewOffDays`
- `AssignmentOnDutyDays`
- `CrewPositionAlternatePositions`
- `TemporaryAssignmentWorkDays`

showing weekday linkage across several scheduling subsystems.

# Part 83: Gap Analysis - CraftPayCodes and RosterBoardPosition Additional Details

Gaps 357-361 capture additional verified model details from `CraftPayCodes.cs` and `RosterBoardPosition.cs`.

---

## GAP 357: `CraftPayCodes` Uses FK-as-PK 1:1 with `Craft`

`CraftPayCodes` key is `CraftControlNumber` decorated as `[Key, ForeignKey("Craft"), DatabaseGenerated(None)]`.

---

## GAP 358: `CraftPayCodes` Has No Audit Columns in This Entity

Entity stores code fields only; it does not include `CreatedBy/CreatedDate/ModifiedBy/ModifiedDate` properties.

---

## GAP 359: `CraftPayCodes` Factory Is Craft-Scoped Only

`CreateInstance(long craft)` sets only `CraftControlNumber`; all code values must be assigned by caller before save.

---

## GAP 360: `RosterBoardPosition.IsExtraBoard` Mirrors `RosterBoard.ExtraBoard != 0`

`IsExtraBoard` does not inspect position-level marker; it reflects board-level setting:

`!RosterBoard.ExtraBoard.Equals(0)`

---

## GAP 361: Roster-Board Auto-MarkUp Returns Immediate `DateTime.Now` When Hours = 0

`GetAutomaticMarkUpDateTime(morecord)` returns current time when computed automatic mark-up hours are zero, otherwise uses `MarkOffDateTime + hours - seconds`.

# Part 84: Gap Analysis - Additional Web vs SAClassLibrary `Roster`/`Craft` Shape Differences

Gaps 362-366 capture additional verified cross-project model-shape differences for `Roster` and `Craft` classes.

---

## GAP 362: SAClassLibrary `Roster` Constructor Eager-Initializes Collections

`SAClassLibrary.Models.Roster()` initializes `DailyShiftOvertimeBoards`, `Positions`, `RosterBoards`, and `Seniorities` as `HashSet<>` in constructor.

Web `StrategicApplications.Models.Roster` constructor does not initialize these collections in-file.

---

## GAP 363: SAClassLibrary `Craft` Constructor Eager-Initializes Large Navigation Set

`SAClassLibrary.Models.Craft()` initializes many nav collections (`CraftApprovalOfficers`, `CraftMarkOffAllowances`, `Rosters`, `Requirements`, etc.) as `HashSet<>` values.

Web `StrategicApplications.Models.Craft` does not initialize these nav collections in-file.

---

## GAP 364: Craft Requirement Navigation Name Differs Across Projects

- Web model: `CraftRequirements` / `CraftRequirement` entities
- SAClassLibrary model: `Requirements` / `Requirement` entities (many-to-many mapping)

---

## GAP 365: Craft Cutoff-Time Navigation Name Differs Across Projects

- Web model property: `CutOffTimes`
- SAClassLibrary model property: `OnDutyMoveCutOffTimes`

Both represent craft-level on-duty move cutoff configuration.

---

## GAP 366: Craft Pay-Code Navigation Name Differs Across Projects

- Web model property: `CraftPayCodes`
- SAClassLibrary model property: `CraftPayCode`

This affects reflection-based mapping and serialization naming expectations.

# Part 85: Gap Analysis - Additional Open-File (`Roster.cs` / `Craft.cs`) Field-Level Notes

Gaps 367-371 capture additional field-level observations verified from the currently open web-model files.

---

## GAP 367: `Roster.CraftControlNumber` Is Not Decorated `[Required]` in Web Model

In `StrategicApplications.Models.Roster`, `CraftControlNumber` is declared without `[Required]` attribute even though roster creation logic is craft-scoped.

---

## GAP 368: `Craft.RailroadPoolControlNumber` Is Not Decorated `[Required]` in Web Model

`StrategicApplications.Models.Craft` defines `RailroadPoolControlNumber` without `[Required]` attribute in this class file.

---

## GAP 369: `Roster` Required Boolean Flags Have No Constructor Defaults

`Training`, `ExtraBoard`, and `OvertimeBoard` are required fields but are not initialized in constructor logic in `Roster.cs`.

---

## GAP 370: `Craft.SetApprovalRequiredFlag` Has No Internal Try/Catch Handling

Method performs direct update/save sequence when value changes and does not wrap persistence in local exception handling in this class.

---

## GAP 371: `Craft.HasSickDays` Is Collection-Count Based

`HasSickDays` returns true only when `CraftSickDays` navigation collection reference is non-null and count is non-zero; no query fallback is embedded in property logic.

# Part 86: Gap Analysis - Mark-Off Code/Record Additional Implementation Notes

Gaps 372-376 capture additional verified behavior from `MarkOffCode.cs` and `MarkOffRecord.cs`.

---

## GAP 372: `MarkOffCode.Code_Description` Is a Computed Getter Decorated `[Required]`

`Code_Description` returns formatted code/description string from getter logic but is also decorated `[Required]` in the model class.

---

## GAP 373: `MarkOffCode.AutoMarkUpHours` Has Hard-Coded Fallback Table

When no `MarkOffMarkUpHours` row exists, fallback values are code-driven:

- `V1..V5` => `168/336/504/672/840`
- `CD/PD/SD/VD` => `24`

---

## GAP 374: `GetCraftOfficers(...)` Builds Transient Officer List (No Immediate Persist)

Method merges craft approval officers into an in-memory list for select-list output; newly created `MarkOffCodeApprovalOfficer` objects are added to local collection, not directly saved to context in this method.

---

## GAP 375: `MarkOffRecord.CreatedByName` / `ModifiedByName` Open New DbContext Per Access

Both computed properties create a new `StrategicApplicationsContext` and query `Users` by username, making them DB-backed computed properties.

---

## GAP 376: `MarkOffRecord` Constructor Seeds `OriginalRecord` Snapshot Container

Default constructor initializes internal `OriginalRecord` object (`MarkOffCopy`) alongside default mark-off values; this snapshot container is used for record-copy/tracking behavior in mark-off workflows.

# Part 87: Gap Analysis - MarkOffRequestRecord Additional Runtime Details

Gaps 377-381 capture additional verified behavior from `MarkOffRequestRecord.cs`.

---

## GAP 377: Vacation-Week TimeOff Uses Fixed 7-Day Week Multiplication

If no markup record exists and mark-off code is vacation week (`V*`, excluding `VD`), `TimeOff` returns:

`new TimeSpan(weeks * 7, 0, 0, 0)`

based on numeric suffix in code (`V1..V5`).

---

## GAP 378: `DaysOff` Rounds Partial-Day Hours Up by +1 Day

`DaysOff` starts at `0`, adds `1` when `TimeOff.Hours > 0`, then adds `TimeOff.Days`.

---

## GAP 379: Mark-Off Request -> Mark-Off Record Flow First Resolves Open Record Collision

`CreateMarkOffRecord(...)` checks `LastOpenMarkOffRecord`; if same date/code it exits early, otherwise may auto-create markup on prior open record before creating a new mark-off record.

---

## GAP 380: Vacation-Week Request Matching Includes Prior-Week Fallback

When linking generated mark-off to request, vacation-week path first searches request date, then falls back to `requestDate - 7 days` for same mark-off code.

---

## GAP 381: Available Vacation Week Logic Throws if Craft/Pool Allowance Missing

`GetAvailableVacationWeeks(...)` requires either craft-level or pool-level `VW` allowance; if both are absent it logs error and throws exception.

# Part 88: Gap Analysis - Craft Mark-Off Configuration Entity Details

Gaps 382-386 capture additional verified details from `CraftMarkOffCode.cs` and `CraftMarkOffAllowance.cs`.

---

## GAP 382: `CraftMarkOffCode` Is a Full `ControlNumberBase` Entity (Not Pure Join Table)

Although it links craft and mark-off code, it carries extra behavior fields (`Exclude`, `ApprovalRequired`, `AutomaticMarkUpHours`) and full audit/key infrastructure from `ControlNumberBase`.

---

## GAP 383: `CraftMarkOffCode.CreateInstance(long craft)` Seeds Craft Only

Factory sets `CraftControlNumber` only; caller must set `MarkOffCodeControlNumber` and required override fields before save.

---

## GAP 384: `CraftMarkOffAllowance` Uses Numeric Capacity Fields as Persisted Inputs

Allowance rows store `TotalNumber`, `CalculatedNumber`, and `NumberAllowed` as persisted values (not computed `[NotMapped]` properties in this class).

---

## GAP 385: `CraftMarkOffAllowance.AllowanceType` Is Required but Unbounded String

`AllowanceType` has `[Required]` but no explicit `[StringLength]` in this class definition.

---

## GAP 386: `CraftMarkOffAllowance.CreateInstance(long craft)` Seeds Craft Link Only

Factory constructor sets only `CraftControlNumber`; year/type/allowance values are assigned by caller logic.

# Part 89: Gap Analysis - Additional `RosterBoard` Computation Notes

Gaps 387-390 capture additional verified `RosterBoard.cs` computation nuances.

---

## GAP 387: Vacancy Averages Depend on Date-Span Day Count Without Guard

`AverageDailyVacanciesLast30Days` and `AverageDailyVacanciesLast12Months` compute:

`vacancies / days`

where `days = enddate.Subtract(startdate).Days`.

No local zero-day guard is present in these getters.

---

## GAP 388: Required Extra-Board Positions Formula Uses Integer Math End-to-End

`NbrOfRequiredExtraBoardPositions` is:

`(((AverageDailyVacanciesLast30Days * 365) / 24) / 12)`

All terms are integer arithmetic, so intermediate truncation applies.

---

## GAP 389: `ExtraBoardPercentage` Returns 100 When Crew Count Is Zero

Getter explicitly short-circuits:

`if (crewcount == 0) return 100;`

before computing board/crew ratio.

---

## GAP 390: `PercentageBelowRequirement` Uses Strict Less-Than Check

With hardcoded requirement `22`, property returns `ExtraBoardPercentage < 22`; equal-to-22 is treated as meeting threshold.

# Part 90: Gap Analysis - Craft Requirement Entity Pair Details

Gaps 391-395 capture additional verified details from `CraftRequirement.cs` and `CraftRequirementEmployee.cs`.

---

## GAP 391: `CraftRequirement` Uses Composite Key Join Shape (No ControlNumber)

`CraftRequirement` key is composite (`CraftControlNumber`, `RequirementControlNumber`) with both columns `DatabaseGenerated(None)`.

---

## GAP 392: `CraftRequirement` Maintains Child Collection of Employee Completions

The join entity includes navigation collection `CraftEmployees` (`ICollection<CraftRequirementEmployee>`), linking requirement definitions to per-employee completion rows.

---

## GAP 393: `CraftRequirementEmployee` Expiration Logic Has Calendar-Year Branch

- If `Requirement.CalendarYear == true`: expiration is `12/31` of next year
- Else: expiration = `CompletedDateTime + RequirementTerm years`

---

## GAP 394: Renewal Date Logic Has Calendar-Year Override

- Calendar-year requirements renew on `01/01` of next year
- Otherwise: `ExpireDateTime - RenewDelayDays`

`CanRenew` becomes true when `RenewDateTime <= DateTime.Today`.

---

## GAP 395: `CraftRequirementEmployee.Create(...)` Persists Immediately

`Create(db, empl, cdate, user)` assigns completion + audit values, adds entity, and calls `db.SaveChanges()` in-method.

# Part 91: Gap Analysis - RailroadPayrollDepartment Field/Factory Notes

Gaps 396-400 capture additional verified details from `RailroadPayrollDepartment.cs`.

---

## GAP 396: Department String Fields Are Required but Have No Length Attributes in Class

`DepartmentName`, `ICCNumber`, `DepartmentNumber`, and `GeneralLedgerNumber` are `[Required]` but have no explicit `[StringLength]` annotations in this model file.

---

## GAP 397: `RailroadMark_Name` Is `[NotMapped]` and Constructor-Seeded

`RailroadMark_Name` is not persisted by EF and is populated in private constructor from supplied `Railroad` object.

---

## GAP 398: Factory Requires Full `Railroad` Object, Not Just Control Number

`CreateInstance(Railroad railroad)` seeds both `RailroadControlNumber` and display helper field (`RailroadMark_Name`) from object input.

---

## GAP 399: Department Model Is Parent for Both `Position` and `Roster`

Entity contains navigation collections for both `Positions` and `Rosters`, reflecting payroll department linkage at both position and roster configuration layers.

---

## GAP 400: Constructor Does Not Set Any Default Code/Name Values

Beyond railroad linkage fields, required department strings are left for caller assignment before persistence.

# Part 92: Gap Analysis - Client Requirement Entity Pair Details

Gaps 401-405 capture additional verified details from `ClientRequirement.cs` and `ClientRequirementEmployee.cs`.

---

## GAP 401: `ClientRequirement` Uses Composite Key Join Shape (Client + Requirement)

`ClientRequirement` is keyed by (`ClientControlNumber`, `RequirementControlNumber`) with both columns `DatabaseGenerated(None)`.

---

## GAP 402: `ClientRequirement` Tracks Employee Completions Through Child Collection

Entity exposes `Employees` navigation (`ICollection<ClientRequirementEmployee>`) as bridge from requirement definition to per-employee completion rows.

---

## GAP 403: `ClientRequirementEmployee` Renewal/Expiration Logic Mirrors Craft Requirement Pattern

Calendar-year requirements expire at end of next year and renew at start of next year; non-calendar-year requirements use term years and `RenewDelayDays` subtraction.

---

## GAP 404: `ClientRequirementEmployee.Create(...)` Persists Immediately

`Create(db, empl, cdate, user)` sets completion + audit fields, adds row, and calls `db.SaveChanges()` within method.

---

## GAP 405: `ClientRequirementEmployee` Factories Provide Two-Stage Initialization

- `CreateInstance(req)` seeds composite requirement keys
- `CreateInstance(req, empl)` additionally seeds employee link

Completion timestamp and audit values are assigned later by `Create(...)`.

# Part 93: Gap Analysis - Additional Open-File (`Craft.cs` / `Roster.cs`) Null-Assumption Notes

Gaps 406-409 capture additional verified null-assumption nuances from the currently open web-model files.

---

## GAP 406: `Craft.GetVacationDays/GetPersonalDays/GetSickDays` Assume Collections Are Available

Each method directly calls LINQ operators on corresponding navigation collection (`CraftVacationDays`, `CraftPersonalDays`, `CraftSickDays`) without local null-guard before ordering/filtering.

---

## GAP 407: `Craft.ApprovalOfficer` Assumes `CraftApprovalOfficers` Collection Exists

Property calls `CraftApprovalOfficers.FirstOrDefault(...)` directly; null collection state is not guarded in getter.

---

## GAP 408: `Roster` Exposes No In-Class Validation Helpers for Required Fields

`Roster.cs` defines required data attributes but does not add explicit in-class guard/validation methods; validation is attribute + EF/model-binding driven.

---

## GAP 409: `Roster.CreateInstance(long craft)` Leaves Other Required Properties for Caller

Factory seeds only `CraftControlNumber`; required fields such as payroll department, names, number, and boolean flags are populated later by caller workflows.

# Part 94: Gap Analysis - Requirement / RequirementDelete Additional Notes

Gaps 410-414 capture additional verified details from `Requirement.cs` and `RequirementDelete.cs`.

---

## GAP 410: `Requirement` Constructor Seeds Default Term/Delay

`Requirement.CreateInstance(int nbr)` sets:

- `RequirementTerm = 3`
- `RenewDelayDays = 364`

as constructor defaults.

---

## GAP 411: `GetRequirementLevelName()` Resolves Through Priority Chain

Requirement level display name resolves in this order based on first available relationship:

`Client -> Railroad -> RailroadPool -> Craft -> Position`

---

## GAP 412: `GetRequirementLevelName()` Assumes Position Requirement Exists in Fallback Path

If prior scopes are null, fallback path dereferences `position.Position.PositionName` without additional null guard for missing position-level mapping.

---

## GAP 413: `RequirementDelete` Uses FK-as-PK Soft-Delete Marker Shape

`RequirementDelete` key is `RequirementControlNumber` mapped as `[Key, ForeignKey("Requirement"), DatabaseGenerated(None)]`.

---

## GAP 414: `RequirementDelete` Stores Create-Only Audit Fields

Delete marker contains `CreatedBy` and `CreatedDate` only; no modify audit fields are defined in this entity.

# Part 95: Gap Analysis - Position Requirement Entity Pair Details

Gaps 415-419 capture additional verified details from `PositionRequirement.cs` and `PositionRequirementEmployee.cs`.

---

## GAP 415: `PositionRequirement` Uses Composite Key Join Shape (Position + Requirement)

`PositionRequirement` key is (`PositionControlNumber`, `RequirementControlNumber`) with both marked `DatabaseGenerated(None)`.

---

## GAP 416: `PositionRequirement` Tracks Employee Completion Rows via `PositionEmployees`

Entity exposes navigation collection `PositionEmployees` (`ICollection<PositionRequirementEmployee>`) for per-employee requirement completion records.

---

## GAP 417: Position Requirement Employee Renewal/Expiration Follows Calendar-Year Branch Rules

Expiration and renewal follow same pattern as other requirement-employee entities:

- calendar-year requirements: end/start of next year boundaries
- non-calendar-year: `RequirementTerm` years with `RenewDelayDays` subtraction

---

## GAP 418: `PositionRequirementEmployee.Create(...)` Persists Immediately

Method assigns employee/completion/audit fields, adds entity, and executes `db.SaveChanges()` in-method.

---

## GAP 419: `PositionRequirementEmployee` Supports Two-Stage Factory Initialization

- `CreateInstance(req)` seeds requirement keys
- `CreateInstance(req, empl)` seeds requirement keys plus employee link

Completion timestamp + audit values are set by `Create(...)`.

# Part 96: Gap Analysis - Railroad Requirement Entity Pair Details

Gaps 420-424 capture additional verified details from `RailroadRequirement.cs` and `RailroadRequirementEmployee.cs`.

---

## GAP 420: `RailroadRequirement` Uses Composite Key Join Shape (Railroad + Requirement)

`RailroadRequirement` key is (`RailroadControlNumber`, `RequirementControlNumber`) with both key columns `DatabaseGenerated(None)`.

---

## GAP 421: `RailroadRequirement` Tracks Employee Completion Rows via `RailroadEmployees`

Entity exposes child collection `RailroadEmployees` (`ICollection<RailroadRequirementEmployee>`) for requirement completion records at railroad scope.

---

## GAP 422: Railroad Requirement Employee Renewal/Expiration Uses Same Calendar-Year Branch Model

Expiration/renewal logic follows calendar-year override behavior versus term-year + renew-delay calculations, mirroring other requirement-employee entity families.

---

## GAP 423: `RailroadRequirementEmployee.Create(...)` Persists Immediately

Method sets employee/completion/audit fields, adds row, and calls `db.SaveChanges()` in-method.

---

## GAP 424: `RailroadRequirementEmployee` Supports Two-Stage Factory Initialization

- `CreateInstance(req)` seeds requirement keys
- `CreateInstance(req, empl)` seeds requirement keys + employee link

Completion timestamp and audit are set by `Create(...)`.

# Part 97: Gap Analysis - RailroadPool Requirement Entity Pair Details

Gaps 425-429 capture additional verified details from `RailroadPoolRequirement.cs` and `RailroadPoolRequirementEmployee.cs`.

---

## GAP 425: `RailroadPoolRequirement` Uses Composite Key Join Shape (Pool + Requirement)

`RailroadPoolRequirement` key is (`RailroadPoolControlNumber`, `RequirementControlNumber`) with both key columns `DatabaseGenerated(None)`.

---

## GAP 426: `RailroadPoolRequirement` Tracks Employee Completion Rows via `RailroadPoolEmployees`

Entity exposes child collection `RailroadPoolEmployees` (`ICollection<RailroadPoolRequirementEmployee>`) for requirement completion records at pool scope.

---

## GAP 427: RailroadPool Requirement Employee Renewal/Expiration Uses Same Calendar-Year Branch Model

Expiration and renewal follow the same calendar-year override versus term-year/renew-delay behavior used by other requirement-employee entity families.

---

## GAP 428: `RailroadPoolRequirementEmployee.Create(...)` Persists Immediately

Method sets employee/completion/audit fields, adds row, and calls `db.SaveChanges()` in-method.

---

## GAP 429: `RailroadPoolRequirementEmployee.GetCraftStatus(craft)` Resolves Seniority Text by Non-Training Roster Match

Method selects first seniority record matching craft and non-training roster, then returns `SeniorityState.StateDescription` (or empty string when none).

# Part 98: Gap Analysis - RailroadEmployee Vacation Request Entity Details

Gaps 430-434 capture additional verified details from `RailroadEmployeeVacationRequest.cs` and `RailroadEmployeeVacationRequestAssignment.cs`.

---

## GAP 430: Vacation Request Is `ControlNumberBase` but Assignment Is FK-as-PK Child

- `RailroadEmployeeVacationRequest` inherits `ControlNumberBase`
- `RailroadEmployeeVacationRequestAssignment` uses `RailroadEmployeeVacationRequestControlNumber` as `[Key, ForeignKey(...), DatabaseGenerated(None)]`

---

## GAP 431: Vacation Request Assignment Stores Create-Only Audit

Assignment entity contains `CreatedBy`/`CreatedDate` only; no modify audit fields are defined.

---

## GAP 432: Vacation Request Factory Seeds RailroadEmployee Link Only

`RailroadEmployeeVacationRequest.CreateInstance(long rremployee)` sets only `RailroadEmployeeControlNumber`; request metadata fields are populated later.

---

## GAP 433: `IsAssigned` Is Direct 1:1 Navigation Presence Check

Vacation request assignment status is simply:

`RailroadEmployeeVacationRequestAssignment != null`

with no additional state evaluation in this class.

---

## GAP 434: Vacation Request Assignment Factory Seeds Parent Link Only

`RailroadEmployeeVacationRequestAssignment.CreateInstance(long vacrequest)` initializes only parent request FK/PK; `CraftControlNumber`, `Notes`, and create-audit values are caller-assigned.

# Part 99: Gap Analysis - Seniority / SeniorityEndDate Additional Notes

Gaps 435-439 capture additional verified details from `Seniority.cs` and `SeniorityEndDate.cs`.

---

## GAP 435: `Seniority` Constructor Defaults `RosterDate` to `DateTime.Now`

Base constructor seeds `RosterDate` with current timestamp unless overridden by factory constructor arguments.

---

## GAP 436: `Seniority.Create(...)` Future-Roster-Date Rule Sets State to 0

When `RosterDate > now`, create logic forces:

`StateID = 0`

before persistence.

---

## GAP 437: Active Seniority Creation Triggers Inactivation + Position Assignment Flow

If `SeniorityState.Active` is true during create flow, code inactivates other active seniority records and then invokes employee position assignment for the new roster.

---

## GAP 438: `CreateSeniorityFile()` Uses Craft-Name Mapping Overrides

Craft-name output mapping includes hard-coded transformations:

- `Yardman` -> `SWITCHMEN`
- `Engineer` -> `ENGINEERS`
- otherwise uppercase craft name

and emits roster date/rank tab-delimited values.

---

## GAP 439: `SeniorityEndDate` Is FK-as-PK with Create-Only Audit

`SeniorityEndDate` uses `SeniorityControlNumber` as key/foreign key and stores create audit (`CreatedBy`, `CreatedDateTime`) without modify audit fields.

# Part 100: Gap Analysis - RailroadEmployeeCompensableTimeRecord Sell-Time Flow Notes

Gaps 440-444 capture additional verified details from `RailroadEmployeeCompensableTimeRecord.cs`.

---

## GAP 440: Sell-Time Flow Is Log-and-Swallow on Exceptions

`SellCompensableTime(...)` wraps full workflow in try/catch and logs failures without rethrowing, so caller-visible behavior is non-throwing on internal errors.

---

## GAP 441: Craft-Null Branch Still Dereferences `craft.ControlNumber`

In craft-null path, code sets:

`payrec.CraftControlNumber = craft.ControlNumber`

after assigning fallback batch value, which dereferences null craft.

---

## GAP 442: Payroll Tier Rate Uses Direct Dereference

Sell-time flow assigns:

`payrec.RatePercentage = rpemployee.RailroadPoolPayrollTier.RatePercentage`

without local null guard for missing payroll tier link.

---

## GAP 443: Earnings Split Loop Decrements by `DefaultTime.Hours`

While `hours > 0`, loop subtracts integer hour component of payroll code default time. Configuration with zero default-time hours would risk non-terminating loop behavior.

---

## GAP 444: Sell-Time Flow Optionally Creates MarkOffRequestDelete Marker

When `request != 0`, method creates/ensures a `MarkOffRequestDelete` record with current timestamp and user attribution.

# Part 101: Gap Analysis - Additional Misc Entity Notes (`RefreshRate` + SAClassLibrary Report View Record)

Gaps 445-449 capture additional verified details from `RefreshRate.cs` (web) and `RailroadEmployeeReportViewedRecord.cs` (SAClassLibrary).

---

## GAP 445: `RefreshRate.CreateInstance()` Seeds Empty Description Only

Factory creates `new RefreshRate(string.Empty)`; required `RefreshRateSeconds` must be caller-assigned before persistence.

---

## GAP 446: `RefreshRate` Uses Required Bounded Description + Required Seconds Integer

`Description` has `[StringLength(100)]` and `RefreshRateSeconds` is required numeric field; no additional in-class validation methods are defined.

---

## GAP 447: `RailroadEmployeeReportViewedRecord` Is SAClassLibrary-Only in Current Codebase

This entity is defined in `SAClassLibrary.Models` and does not appear as a corresponding web-model class file in `StrategicApplications\Models`.

---

## GAP 448: Report-Viewed Record Factory Seeds Employee + Pool Links Only

`CreateInstance(long rremployee, long rrpool)` initializes relationship keys; view metadata fields (`EmployeeNumber`, `ReportName`, `ViewDateTime`) are caller-assigned.

---

## GAP 449: Report-Viewed Record Inherits `ControlNumberBase` in SAClassLibrary

SAClassLibrary report-view record includes standard control-number and audit fields through `ControlNumberBase` inheritance.

# Part 102: Gap Analysis - Removed Pool Employee + Calendar Request Entity Notes

Gaps 450-454 capture additional verified details from `RemovedRailroadPoolEmployee.cs` and `RailroadEmployeeCalendarRequest.cs`.

---

## GAP 450: `RemovedRailroadPoolEmployee` Is FK-as-PK Removal Marker

Entity key is `RailroadPoolEmployeeControlNumber` with `[Key, ForeignKey("RailroadPoolEmployee"), DatabaseGenerated(None)]`.

---

## GAP 451: Removed-Pool-Employee Marker Uses Create-Only Audit Fields

`RemovedRailroadPoolEmployee` stores `CreatedBy` and `CreatedDateTime` only (no modify audit fields).

---

## GAP 452: `RemovedRailroadPoolEmployee.CreateInstance(long)` Seeds Only FK/PK

Factory initializes only `RailroadPoolEmployeeControlNumber`; removal datetime and audit values are caller-assigned.

---

## GAP 453: `RailroadEmployeeCalendarRequest` Inherits `ControlNumberBase` but Declares No `[Required]` Attributes in Class

Class relies on plain property definitions for employee link, name snapshot, request timestamp, and used-flag state.

---

## GAP 454: Calendar Request Factory Captures Employee Name Snapshot at Creation

`CreateInstance(RailroadEmployee)` copies `EmpNbr_FullName` into `RailroadEmployeeName`, sets `RequestDateTime = DateTime.Now`, and `Used = false`.

# Part 103: Gap Analysis - RailroadEmployeeVacationOneDayTimeRecord Notes

Gaps 455-458 capture additional verified details from `RailroadEmployeeVacationOneDayTimeRecord.cs`.

---

## GAP 455: Vacation One-Day Time Record Inherits `ControlNumberBase`

Entity carries standard control-number key and create/modify audit fields via base inheritance.

---

## GAP 456: Factory Captures Employee Snapshot Name at Creation

`CreateInstance(RailroadEmployee)` copies `EmpNbr_FullName` into `RailroadEmployeeNbr_Name` and seeds employee link.

---

## GAP 457: Pool Number Seeding Depends on `ActiveCraft` Availability

Constructor sets `PoolNumber` only when `rremployee.ActiveCraft != null`; otherwise pool number remains caller-assigned/default.

---

## GAP 458: EntryDate/Hour Fields Are Caller-Populated After Factory Creation

`EntryDate`, `InitialHours`, and `AdditionalHours` are required but not initialized in constructor/factory path.

# Part 104: Gap Analysis - Additional Sell-Time Audit/Reason Record Notes

Gaps 459-462 capture additional verified details from `RailroadEmployeeCompensableTimeRecord.SellCompensableTime(...)`.

---

## GAP 459: Sell-Time Always Creates Payroll Review Required Record for Generated Payroll Row

After payroll record creation, flow creates `PayrollReviewRequiredRecord` with reason text and audit fields for review routing.

---

## GAP 460: Review Reason Text Embeds Employee Name, Hours, Type Name, and Notes

Reason format:

`"{EmpNbr_FullName} sold {hours} {typename} hours for reason: {notes}"`

---

## GAP 461: Sell-Time Withdrawal Logs User Identity Resolution Preference

Flow attempts to resolve acting user to employee full name (`EmpNbr_FullName`) for narrative text; falls back to raw `userid` when user row is not found.

---

## GAP 462: Request Delete Marker Creation Is Idempotent by Lookup

When request id is supplied, code first checks `MarkOffRequestDeletes.Find(request)` and creates marker only when missing.

# Part 105: Gap Analysis - Craft Service-Day Tier Entities (`Vacation/Personal/Sick`)

Gaps 463-466 capture additional verified details from `CraftVacationDays.cs`, `CraftPersonalDays.cs`, and `CraftSickDays.cs`.

---

## GAP 463: All Three Craft Service-Day Tier Entities Inherit `ControlNumberBase`

`CraftVacationDays`, `CraftPersonalDays`, and `CraftSickDays` each include full control-number/audit base fields plus tier-specific service-year/day values.

---

## GAP 464: Tier Rows Are Craft-Linked via Required FK

Each entity has required `CraftControlNumber` with `[ForeignKey("Craft")]` to represent per-craft entitlement thresholds.

---

## GAP 465: Factory Methods Seed Craft Link Only

`CreateInstance(long craft)` in each class initializes only `CraftControlNumber`; `ServiceYears` and day-count fields are caller-assigned.

---

## GAP 466: Tier Entities Store Raw Threshold Inputs (No In-Class Derived Logic)

These classes define data-only shape (`ServiceYears` + days) without local computation methods; threshold selection logic is implemented in `Craft` methods.

# Part 106: Gap Analysis - Craft/MarkOff Approval Officer Entity Notes

Gaps 467-470 capture additional verified details from `CraftApprovalOfficer.cs` and `MarkOffCodeApprovalOfficer.cs`.

---

## GAP 467: Both Approval-Officer Entities Inherit `ControlNumberBase`

`CraftApprovalOfficer` and `MarkOffCodeApprovalOfficer` are full entities with control-number/audit base fields (not pure join records).

---

## GAP 468: `CraftApprovalOfficer` Carries Primary-Flag Semantics

`CraftApprovalOfficer` includes required `Primary` boolean used by `Craft.ApprovalOfficer` resolution logic.

---

## GAP 469: `MarkOffCodeApprovalOfficer` Has No Primary Flag in Model

Mark-off-code approval officer rows model code/employee linkage only in this class; no primary designation field is present.

---

## GAP 470: Approval-Officer Factory Methods Seed Parent Link Only

- `CraftApprovalOfficer.CreateInstance(long craft)` sets craft FK
- `MarkOffCodeApprovalOfficer.CreateInstance(long code)` sets mark-off-code FK

Employee linkage values are caller-populated later.

# Part 107: Gap Analysis - TemporaryAssignment / TemporaryAssignmentWorkDay Notes

Gaps 471-475 capture additional verified details from `TemporaryAssignment.cs` and `TemporaryAssignmentWorkDay.cs`.

---

## GAP 471: `TemporaryAssignment.IsOpen`/`IsClosed` Are Release-Record Driven

Open/closed state is computed entirely from `TemporaryAssignmentRelease` presence and release-date comparison (no separate status field in class).

---

## GAP 472: Unassign/Delete Flows Use Multiple Overloaded Delete Methods by Scope

Temporary-assignment cleanup supports deletion by specific call sheet, by pool/workday, or globally, each unassigning related daily crew positions before removing daily assignments.

---

## GAP 473: Recursive Unassign Path Handles Moved Temporary Daily Positions

`UnassignTemporaryDailyPosition(...)` recursively unwinds `MovedDailyCrewPosition` chains and re-fills original positions where applicable.

---

## GAP 474: `DeleteTemporaryAssignment(...)` Removes Linked Object Notes

Delete flow explicitly removes `ObjectNotes` rows with `ObjectControlNumber == TemporaryAssignment.ControlNumber` before deleting temporary assignment row.

---

## GAP 475: `TemporaryAssignmentWorkDay.CreateInstance(...)` Seeds Assignment Link Only

Factory for work-day rows initializes `TemporaryAssignmentControlNumber`; `WeekDayControlNumber` is caller-assigned.

# Part 108: Gap Analysis - Payroll Tier / PayrollCodePayRate Entity Notes

Gaps 476-480 capture additional verified details from `RailroadPoolPayrollTier.cs` and `PayrollCodePayRate.cs`.

---

## GAP 476: `RailroadPoolPayrollTier` Uses Base Namespace from `SAClassLibrary.BaseClasses`

The web model file imports `SAClassLibrary.BaseClasses` for `ControlNumberBase`, unlike most web entities that use `StrategicApplications.Models.BaseClasses`.

---

## GAP 477: Payroll Tier Factory Seeds Only NotMapped Railroad Display Field

`RailroadPoolPayrollTier.CreateInstance(Railroad railroad)` constructor path populates `RailroadMark_Name` (`[NotMapped]`) and does not set required persisted tier fields.

---

## GAP 478: `RailroadPoolPayrollTier` Required Persisted Fields Are Caller-Assigned

`RailroadPoolControlNumber`, `NumberOfDays`, `TypeOfDay`, and `RatePercentage` are required and not initialized by constructor/factory in this class.

---

## GAP 479: `PayrollCodePayRate.CreateInstance(long paycode)` Seeds Payroll Code Link Only

Factory sets only `PayrollCodeControlNumber`; required `PositionControlNumber`, `Amount`, and `EffectiveDate` are caller-populated.

---

## GAP 480: `PayrollCodePayRate` Stores Effective-Dated Flat Amount

Entity models amount by payroll code + position + effective date and does not include separate ST/OT rate columns in this class.

# Part 109: Gap Analysis - PayRate / PositionPayRate Entity Notes

Gaps 481-484 capture additional verified details from `PayRate.cs` and `PositionPayRate.cs`.

---

## GAP 481: `PayRate` Stores Single Float Rate + Description Pair

`PayRate` uses one `float Rate` value with required short description text; it is not split into ST/OT components in this entity.

---

## GAP 482: `PayRate.CreateInstance(rate, desc)` Is Fully Initializing Factory

Factory constructor for `PayRate` assigns both required fields (`Rate`, `Description`) directly at creation.

---

## GAP 483: `PositionPayRate` Models Effective-Dated ST/OT Rates Per Position

`PositionPayRate` stores required `STHourRate`, `OTHourRate`, and `EffectiveDate` per `PositionControlNumber`.

---

## GAP 484: `PositionPayRate.CreateInstance(long position)` Seeds Position Link Only

Factory assigns only `PositionControlNumber`; required rate and effective-date values are caller-populated.

# Part 110: Gap Analysis - Deleted Position + Position History Entity Notes

Gaps 485-489 capture additional verified details from `DeletedRailroadPosition.cs` and `RailroadPoolEmployeePositionHistory.cs`.

---

## GAP 485: `DeletedRailroadPosition` Is FK-as-PK Delete Marker for `RailroadPosition`

Delete marker key is `RailroadPositionControlNumber` decorated as `[Key, ForeignKey("RailroadPosition"), DatabaseGenerated(None)]`.

---

## GAP 486: `DeletedRailroadPosition` Stores Create-Only Audit + Deleted Timestamp

Entity stores `DeletedDateTime`, `CreatedBy`, and `CreatedDateTime`; no modify audit fields are defined.

---

## GAP 487: `DeletedRailroadPosition.CreateInstance(long)` Seeds Position Link Only

Factory initializes only `RailroadPositionControlNumber`; delete timestamp and create-audit values are caller-populated.

---

## GAP 488: `RailroadPoolEmployeePositionHistory` Inherits `ControlNumberBase`

History rows have independent control-number identity and full base audit field support.

---

## GAP 489: Position-History Factory Seeds Link Keys Only

`CreateInstance(long rposition, long rpemployee)` sets relationship keys; assignment metadata (`AssignmentType`, `AssignmentControlNumber`, `AssignedDate`) is assigned later.

# Part 111: Gap Analysis - UserLoginRecord / ObjectNotes Entity Notes

Gaps 490-494 capture additional verified details from `UserLoginRecord.cs` and `ObjectNotes.cs`.

---

## GAP 490: `UserLoginRecord` Uses `ControlNumberBase` with Non-Generated UserID Field

Login record entity has base control-number identity, while `UserID` is required and marked `DatabaseGenerated(None)`.

---

## GAP 491: Login Record Factory Seeds User ID Only

`CreateInstance(string id)` initializes only `UserID`; employee number, login datetime, IP, and on-property state are caller-populated.

---

## GAP 492: `ObjectNotes` Uses External Object Identifier as Primary Key

`ObjectControlNumber` is the primary key (`DatabaseGenerated(None)`), representing notes keyed directly by external object control number.

---

## GAP 493: `ObjectNotes` Factory Seeds Object Link Only

`CreateInstance(long ctrlnbr)` sets only `ObjectControlNumber`; notes text and audit fields are assigned by caller flow.

---

## GAP 494: `ObjectNotes` Keeps Full Create/Modify Audit Fields Despite Non-Base Class Shape

Entity does not inherit `ControlNumberBase` but still includes explicit `CreatedBy/CreatedDate/ModifiedBy/ModifiedDate` fields.

# Part 112: Gap Analysis - OffPropertyTieUpRecord / PayrollCrewPositionAutoPayRecord Notes

Gaps 495-499 capture additional verified details from `OffPropertyTieUpRecord` (SAClassLibrary) and `PayrollCrewPositionAutoPayRecord.cs`.

---

## GAP 495: `OffPropertyTieUpRecord` Is SAClassLibrary Entity with User-ID Seed Factory

Factory `CreateInstance(string id)` initializes `AspNetUserId`; employee number, tie-up timestamp, and text are caller-assigned.

---

## GAP 496: `PayrollCrewPositionAutoPayRecord` Uses RailroadPoolEmployee FK as Primary Key

Key is `[Key, ForeignKey("RailroadPoolEmployee")] RailroadPoolEmployeeControlNumber`, modeling one autopay config row per pool-employee.

---

## GAP 497: Auto-Pay Record Stores Create-Only Audit Fields

Entity contains `CreatedBy`/`CreatedDate` and does not define modify-audit fields in class.

---

## GAP 498: Auto-Pay Creation Skips When Expired or Holiday

`CreateAutomaticPayrollRecord(...)` exits when expiration date is before on-duty date; it also suppresses processing when assignment date is a holiday.

---

## GAP 499: Auto-Pay Flow Rebuilds Payroll Record by Delete-and-Recreate Pattern

Method removes matching existing payroll records (`JobWorked` + `OnDutyDateTime`) before creating new payroll and review-required records for the autopay scenario.

# Part 113: Gap Analysis - Railroad Information Type/Record Entity Notes

Gaps 500-504 capture additional verified details from `RailroadInformationType.cs` and `RailroadInformationRecord.cs`.

---

## GAP 500: Railroad Information Type Factory Seeds Railroad Link Only

`RailroadInformationType.CreateInstance(long railroad)` initializes only `RailroadControlNumber`; required type/signature fields are caller-populated.

---

## GAP 501: Railroad Information Type Required Text Fields Have No Explicit Length Attributes

`TypeName`, `SignatureName`, and `SignatureTitle` are required in class but do not define `[StringLength]` constraints here.

---

## GAP 502: Railroad Information Record Publish Status Is Computed from Publish Child Record

`IsPublished` is true only when publish child exists and `PublishDate <= now`.

---

## GAP 503: Railroad Information Record `PublishDate` Falls Back to `DateTime.Today`

If no publish child exists, `PublishDate` getter returns current date (not sentinel max/min datetime).

---

## GAP 504: Railroad Information Record Factory Seeds Railroad Link Only

`CreateInstance(long railroad)` sets only `RailroadControlNumber`; required record metadata fields (`RecordNumber`, `Title`, `Description`, type FK) are populated by caller logic.

# Part 114: Gap Analysis - Railroad Information Publish/Cancel Record Notes

Gaps 505-508 capture additional verified details from `RailroadInformationPublishRecord.cs` and `RailroadInformationCancelRecord.cs`.

---

## GAP 505: Publish/Cancel Records Use FK-as-PK 1:1 Shape to Information Record

Both entities key on `RailroadInformationRecordControlNumber` with `[Key, ForeignKey("RailroadInformationRecord"), DatabaseGenerated(None)]`.

---

## GAP 506: Publish Record Defaults `EmployeesNotified` to False

`RailroadInformationPublishRecord` default constructor initializes `EmployeesNotified = false`.

---

## GAP 507: Publish/Cancel Entities Store Create-Only Audit

Both classes include `CreatedBy` and `CreatedDate` and do not define modify audit fields.

---

## GAP 508: Factory Methods Seed Parent Record Link Only

`CreateInstance(long record)` in both entities initializes only the parent record control number; publish/cancel datetime and create-audit values are caller-assigned.

# Part 115: Gap Analysis - Railroad Information Close/Delete Record Notes

Gaps 509-512 capture additional verified details from `RailroadInformationCloseRecord.cs` and `RailroadInformationDeleteRecord.cs`.

---

## GAP 509: Close/Delete Records Use FK-as-PK 1:1 Shape to Information Record

Both entities key on `RailroadInformationRecordControlNumber` with `[Key, ForeignKey("RailroadInformationRecord"), DatabaseGenerated(None)]`.

---

## GAP 510: Close/Delete Entities Store Create-Only Audit

Both classes include `CreatedBy` and `CreatedDate` and do not define modify audit fields.

---

## GAP 511: Close/Delete Datetimes Are Separate Required Fields

- close entity uses `CloseDate`
- delete entity uses `DeleteDate`

These are distinct from create-audit timestamps.

---

## GAP 512: `CreateInstance(long record)` Seeds Parent Link Only

Factory constructors initialize only parent record control number; close/delete datetime and create-audit values are caller-assigned.

# Part 116: Gap Analysis - Read/Viewed Tracking Entity Notes

Gaps 513-516 capture additional verified details from `RailroadInformationReadbyEmployeeRecord.cs` and `RailroadPoolEmployeeBulletinsViewedRecord` (SAClassLibrary).

---

## GAP 513: Both Read/Viewed Tracking Entities Inherit `ControlNumberBase`

Each record type has independent control-number identity and create/modify audit support through base class inheritance.

---

## GAP 514: Railroad Information Read-by-Employee Factory Seeds Information Record Link Only

`CreateInstance(long record)` initializes `RailroadInformationRecordControlNumber`; employee link and read timestamp are caller-assigned.

---

## GAP 515: Bulletin-Viewed Record Is SAClassLibrary Entity in Current Codebase

`RailroadPoolEmployeeBulletinsViewedRecord` is defined in `SAClassLibrary.Models` and used as tracking record at class-library data-model layer.

---

## GAP 516: Bulletin-Viewed Factory Seeds Pool-Employee Link Only

`CreateInstance(long rremployee)` initializes `RailroadPoolEmployeeControlNumber`; `ViewDateTime` is caller-populated.

# Part 117: Gap Analysis - RailroadPositionChange / ChangeNotification Notes

Gaps 517-521 capture additional verified details from `RailroadPositionChange.cs` and `ChangeNotification.cs`.

---

## GAP 517: Railroad Position Change Completion Depends on Confirmed Notifications When Required

`IsComplete` returns true by notification confirmation when `NotificationRequired == true`; otherwise change is treated complete by default.

---

## GAP 518: Change-Notification Creation Methods Add Hangout Records as Side Effect

Both manual and automatic notification creation paths call `RailroadPosition.CreateDailyRosterBoardPositionHangoutRecord(...)` in addition to inserting notification rows.

---

## GAP 519: Confirmed Notification Flow Includes Could-Not-Notify (`NN`) Mark-Off Reconciliation

`CheckForCouldNotNotifyMarkOffRecord(...)` can delete or markup existing `NN` mark-off records based on notify datetime and then update related daily mark-off projections.

---

## GAP 520: `GetOpenRailroadPositionChanges(...)` Returns `null` for No Open Moves

Method returns `null` (not empty list) when underlying move/change list count is zero.

---

## GAP 521: `ChangeNotification.NotifiedDays` Is Derived from Current Time Only When Confirmed

`NbrOfDaysNotified` is `DateTime.Now - NotifyDateTime` only for confirmed notifications; otherwise zero timespan.

# Part 118: Gap Analysis - PositionAlternateSupervisor Entity Notes

Gaps 522-525 capture additional verified details from `PositionAlternateSupervisor.cs`.

---

## GAP 522: Position Alternate Supervisor Uses FK-as-PK 1:1 Shape

Primary key is `PositionControlNumber` with `[Key, ForeignKey("Position"), DatabaseGenerated(None)]`, modeling one optional alternate-supervisor row per position.

---

## GAP 523: Alternate Supervisor Employee Link Is Optional in Class

`EmployeeControlNumber` has no `[Required]` attribute in this model file.

---

## GAP 524: Alternate Supervisor Includes Explicit Create + Modify Audit Fields

Entity stores `CreatedBy/CreatedDate` and `ModifiedBy/ModifiedDate` directly (no base-class inheritance).

---

## GAP 525: `CreateInstance(long position)` Seeds Position Link Only

Factory initializes only `PositionControlNumber`; employee link and audit values are caller-assigned.

# Part 119: Gap Analysis - Change-to-Information Bridge Entity Notes

Gaps 526-529 capture additional verified details from `RailroadPositionChangeRailroadInformationRecord.cs`.

---

## GAP 526: Bridge Entity Uses Change Record FK as Primary Key

`RailroadPositionChangeControlNumber` is `[Key, ForeignKey("RailroadPositionChange"), DatabaseGenerated(None)]`.

---

## GAP 527: Bridge Stores Required Information-Record Link as Non-Key Field

`RailroadInformationRecordControlNumber` is required but not part of a composite key in this class shape.

---

## GAP 528: Bridge Entity Has No Audit Fields

Class contains linkage columns only and does not define created/modified audit properties.

---

## GAP 529: `CreateInstance(changerec, inforec)` Fully Seeds Linkage Pair

Factory constructor assigns both required relationship control numbers in one call.

# Part 120: Gap Analysis - README Architecture Statements vs Current Runtime Implementation

Gaps 530-534 capture additional verified observations from `README.md` compared to current code implementation.

---

## GAP 530: README Background-Processing Summary Omits MSMQ-Based Service Paths

README highlights file-based watchers/timers, but solution also contains MSMQ-driven Windows service flows (`System.Messaging`) for daily call-sheet and mark-off/on-duty pipelines.

---

## GAP 531: README Migration Section References External Target Path Outside Repo

`Target Project: C:\Projects\CrewService\CrewService.API` is documented in README but is not part of current workspace tree.

---

## GAP 532: README Marks Migration Progress as In-Progress While Current Repo Remains .NET Framework Runtime

Current working code in this repo is still .NET Framework 4.7.2 MVC/EF6 + service ecosystem; migration plan content is forward-looking documentation.

---

## GAP 533: README Entity Tables Reflect Intentional Domain Snapshot, Not Exhaustive Runtime Behavior

Entity tables in README summarize core fields but do not capture computed-property side effects and runtime service orchestration behaviors documented in this spec.

---

## GAP 534: README and Runtime Together Indicate Hybrid Transitional State

Documentation and codebase jointly indicate active transition planning (gRPC/.NET 8 target) while production behavior remains on existing framework and processing infrastructure.

# Part 121: Gap Analysis - ChangeMoveOrBulletin / FillVacancyLog Notes

Gaps 535-539 capture additional verified details from `ChangeMoveOrBulletin.cs` and `FillVacancyLog.cs`.

---

## GAP 535: `ChangeMoveOrBulletin` Uses Composite Key Bridge Shape

Bridge key is (`RailroadPositionChangeControlNumber`, `MoveOrBulletinControlNumber`) with both marked `DatabaseGenerated(None)`.

---

## GAP 536: `ChangeMoveOrBulletin` Contains NotMapped `SeniorityMove` Navigation Placeholder

Class includes `[NotMapped] virtual SeniorityMove SeniorityMove` property, indicating non-persisted helper linkage in this model.

---

## GAP 537: `GetChangeOrMoveBulletins(...)` Is Pure Control-Number Filter

Query helper returns all bridge rows matching `MoveOrBulletinControlNumber` with no additional open/closed or type filtering logic.

---

## GAP 538: `FillVacancyLog.CreateInstance(...)` Midnight-Crossing Adjustment Does Not Reassign `etime`

When `stime > etime`, method executes `etime.Add(new TimeSpan(24, 0, 0));` without assigning result back to `etime` before duration calculation.

---

## GAP 539: FillVacancyLog Snapshot Captures Display Strings at Creation Time

Log constructor stores shift number, formatted crew/position text, and employee full name as strings rather than keeping relational links.

# Part 122: Gap Analysis - Additional Open-File (`Roster`/`Craft`) Structural Notes

Gaps 540-543 capture additional verified structural notes from the currently open `Roster.cs` and `Craft.cs` files.

---

## GAP 540: `Roster` Daily Board Navigation Is Overtime-Board Focused

`Roster` directly exposes `ICollection<DailyShiftOvertimeBoard> DailyShiftOvertimeBoards`; daily extra-board tracking is modeled via other entities/paths.

---

## GAP 541: `Craft.CutOffTimes` Navigation Uses `OnDutyMoveCutOffTime` Entity Type

In web model, craft-level cutoff configuration is surfaced through `ICollection<OnDutyMoveCutOffTime> CutOffTimes` naming/typing.

---

## GAP 542: `Craft.CreateInstance()` and `Craft.CreateInstance(long pool)` Do Not Populate All Required Core Fields

Factories seed limited values (`CraftName` or `RailroadPoolControlNumber`) while required fields such as plural name, craft number, and configuration flags remain caller-populated.

---

## GAP 543: `Roster` and `Craft` Constructors Do Not Initialize Navigation Collections In-File

Both classes rely on external initialization/loading for nav collections in this web-model code file.

# Part 123: Gap Analysis - PhoneNumber / EmailAddress Entity Notes

Gaps 544-548 capture additional verified details from `PhoneNumber.cs` and `EmailAddress.cs`.

---

## GAP 544: Both Contact Entities Inherit `ControlNumberBase`

`PhoneNumber` and `EmailAddress` use control-number identity and base audit fields via inheritance.

---

## GAP 545: Contact Factories Seed Employee Link Only

`CreateInstance(Employee employee)` in both classes initializes only `EmployeeControlNumber`; contact values/types are caller-assigned.

---

## GAP 546: `PhoneNumber` Includes Dial Prefix Display Helpers

`DisplayOne` returns `"1 "` when `DialOne == true`, and `DisplayNumber` prepends that prefix to stored number when applicable.

---

## GAP 547: `PhoneNumber.Number` Has Explicit 12-Character Bound

Phone number value is `[StringLength(12)]` in this model shape.

---

## GAP 548: `EmailAddress.Email` Has Explicit 250-Character Bound

Email field uses `[StringLength(250)]` and no additional in-class normalization logic.

# Part 124: Gap Analysis - Address / Description Entity Notes

Gaps 549-553 capture additional verified details from `Address.cs` and `Description.cs`.

---

## GAP 549: `Address.CreateInstance(Employee)` Seeds Employee Link and Default State `TX`

Address factory constructor initializes `EmployeeControlNumber` and defaults `State` to `"TX"`.

---

## GAP 550: `Address.CreateAddressFile()` Emits Uppercased Tab-Delimited Snapshot

Method outputs address fields as uppercase tab-delimited text, with blank placeholder for empty `Address2`.

---

## GAP 551: `Address.CreateAddressFile()` Catches and Rethrows Wrapped Exception

On failure, method throws `new Exception(ex.Message, ex.InnerException)` rather than preserving original exception object.

---

## GAP 552: `Description.Code` Is Strict Two-Character Required Field

Description type code uses `[StringLength(2, MinimumLength = 2)]` plus required attribute.

---

## GAP 553: `Description` Is Client-Scoped and Drives Contact-Type Taxonomy

Entity carries `ClientControlNumber` and provides shared categorization rows for addresses, phone numbers, and email addresses via navigation collections.

# Part 125: Gap Analysis - Holiday / HolidayQualifyRecord Entity Notes

Gaps 554-558 capture additional verified details from `Holiday.cs` and `HolidayQualifyRecord.cs`.

---

## GAP 554: `Holiday.CreateHoliday(...)` De-Duplicates by Date Only

Static create helper checks `!db.Holidays.Any(h => h.HolidayDate.Equals(holidaydate))` without railroad/client qualifier in duplicate predicate.

---

## GAP 555: `Holiday.CreateHoliday(...)` Resolves Client via Railroad Lookup

Helper reads client control number from `db.Railroads.Find(railroad).ClientControlNumber` and creates holiday as client-scoped row.

---

## GAP 556: Holiday Factory Seeds Client Link Only

`Holiday.CreateInstance(long client)` initializes only `ClientControlNumber`; date/name and audit fields are caller-assigned.

---

## GAP 557: `HolidayQualifyRecord` Inherits `ControlNumberBase` and Uses Record-Link Factory

`CreateInstance(long record)` seeds `PayrollHolidayRecordControlNumber`; qualification details are populated by caller.

---

## GAP 558: Holiday Qualify Fields Are Required but Unbounded Strings in Class

`Pre_Post` and `Code` are required string fields with no explicit `[StringLength]` attributes in this model definition.

# Part 126: Gap Analysis - PayrollHolidayRecord Processing Notes

Gaps 559-563 capture additional verified details from `PayrollHolidayRecord.cs` and `PayrollHolidayRecordPayrollRecord.cs`.

---

## GAP 559: Holiday Qualification Aggregate Requires PRE + POST Success (with Holiday-Day Check)

`Qualified` and qualification flow evaluate pre/post records (and holiday-day record in qualification method) before payroll processing path is triggered.

---

## GAP 560: Yardmaster Pool (20) Auto-Qualifies PRE/HLDY/POST in Qualify Flow

`QualifyHolidayRecord(...)` hard-sets `pre/hldy/post = true` for pool number 20 before payroll-record processing step.

---

## GAP 561: Holiday Payroll Creation Uses Duplicate Guard by Existing Earnings Signature

`ProcessHolidayPayrollRecord(...)` checks existing payroll records/earnings (code/time signature) before creating new holiday payroll record.

---

## GAP 562: Holiday Payroll Process Sets `ReviewRequired=false` on Success Path

After processing logic completes for qualifying position/craft scenario, record is marked non-review-required and saved.

---

## GAP 563: Payroll-Holiday Link Table Uses Composite Key Pair

`PayrollHolidayRecordPayrollRecord` key is (`PayrollHolidayRecordControlNumber`, `PayrollRecordControlNumber`) with both columns `DatabaseGenerated(None)`.

# Part 127: Gap Analysis - MarkOffRequest Wait-List Entity Notes

Gaps 564-568 capture additional verified details from `MarkOffRequestWaitListRecord.cs` and `MarkOffRequestMarkOffRequestWaitListRecord.cs`.

---

## GAP 564: Wait-List Record Inherits `ControlNumberBase`

`MarkOffRequestWaitListRecord` has independent control-number identity and base audit field support.

---

## GAP 565: Wait-List Factory Seeds RailroadEmployee Link Only

`CreateInstance(long rremployee)` initializes `RailroadEmployeeControlNumber`; other required linkage/code/date fields are caller-populated.

---

## GAP 566: Wait-List Entity Stores Duplicated Identity/Code Snapshot Fields

Entity persists both control-number links and direct string snapshots (`EmployeeNumber`, `MOCode`) as required fields.

---

## GAP 567: Request-to-WaitList Bridge Uses Composite Key Pair

`MarkOffRequestMarkOffRequestWaitListRecord` key is (`MarkOffRequestRecordControlNumber`, `MarkOffRequestWaitListRecordControlNumber`) with both columns `DatabaseGenerated(None)`.

---

## GAP 568: Wait-List Bridge Factory Fully Seeds Both Link Keys

`CreateInstance(long request, long waitlist)` assigns both required linkage columns in constructor path.

# Part 128: Gap Analysis - MarkOff Delete-Marker Entity Notes

Gaps 569-573 capture additional verified details from `MarkOffRequestDelete.cs` and `MarkOffRecordDelete.cs`.

---

## GAP 569: Both Delete Markers Use FK-as-PK 1:1 Shape

- `MarkOffRequestDelete.MarkOffRequestRecordControlNumber`
- `MarkOffRecordDelete.MarkOffRecordControlNumber`

Each is `[Key, ForeignKey(...), DatabaseGenerated(None)]`.

---

## GAP 570: Delete Marker Entities Store Create-Only Audit + Deleted Timestamp

Both classes persist `DeletedDateTime`, `CreatedBy`, and `CreatedDateTime` only.

---

## GAP 571: Delete Marker Factories Seed Parent Link Only

`CreateInstance(long ctrlnbr)` in both entities initializes only parent FK/PK; delete timestamp and create-audit values are caller-assigned.

---

## GAP 572: Delete Marker Classes Are Non-`ControlNumberBase` Shapes

Neither class inherits base control-number entity type; they model key-sharing child rows attached to parent records.

---

## GAP 573: Delete Markers Preserve Parent Navigation for Query Traversal

Each class keeps virtual parent navigation (`MarkOffRequestRecord` / `MarkOffRecord`) for relationship access despite slim marker payload.

# Part 129: Gap Analysis - Payroll Review Required/Review Record Notes

Gaps 574-578 capture additional verified details from `PayrollReviewRequiredRecord.cs` and `PayrollReviewRecord.cs`.

---

## GAP 574: Payroll Review Entities Use FK-as-PK 1:1 Shapes

Both entities key on `PayrollRecordControlNumber` with `[Key, ForeignKey(...), DatabaseGenerated(None)]`.

---

## GAP 575: `PayrollReviewRequiredRecord` Stores Reason + Full Create/Modify Audit

Required-review row captures textual reason and both created/modified attribution fields.

---

## GAP 576: `PayrollReviewRecord` Represents Completion/Resolution Marker via 1:1 Link

Review row links to required-review row through `PayrollRecordReviewReason` navigation, with create/modify audit timestamps.

---

## GAP 577: Both Factories Seed Payroll Record Link Only

`CreateInstance(long record)` in both entities initializes only `PayrollRecordControlNumber`; reason and audit values are caller-assigned.

---

## GAP 578: Review-Required and Review-Completed Rows Are Modeled as Separate Entities

Schema uses separate tables/entities for "needs review" versus "review completed" state rather than single status flag on one row.

# Part 130: Gap Analysis - Payroll Record Delete / Earning Processed Notes

Gaps 579-583 capture additional verified details from `PayrollRecordDelete.cs` and `PayrollEarningProcessedRecord.cs`.

---

## GAP 579: `PayrollRecordDelete` Uses FK-as-PK Delete Marker Shape

Delete marker key is `PayrollRecordControlNumber` with `[Key, ForeignKey("PayrollRecord"), DatabaseGenerated(None)]`.

---

## GAP 580: Payroll Record Delete Marker Stores Create-Only Audit + Deleted Timestamp

Entity stores `DeletedDateTime`, `CreatedBy`, and `CreatedDateTime`; no modify audit fields are defined.

---

## GAP 581: `PayrollEarningProcessedRecord` Keys on Earning Record with Additional Process Record Link

Primary key/foreign key is `PayrollEarningRecordControlNumber`; process-record control number is separately stored with `DatabaseGenerated(None)`.

---

## GAP 582: Earning Processed Record Stores Create Attribution but No Modify Audit in Class

Entity includes `CreatedBy`, process timestamp, period, and final-process flag without explicit modified fields.

---

## GAP 583: Earning Processed Factory Seeds Earning Record Link Only

`CreateInstance(long record)` initializes only `PayrollEarningRecordControlNumber`; pay-period/process metadata is caller-assigned.

# Part 131: Gap Analysis - Qualification / EngineerPayRate Entity Notes

Gaps 584-588 capture additional verified details from `Qualification.cs` and `EngineerPayRate.cs`.

---

## GAP 584: `Qualification` Provides Two Single-Link Factories

Factories can seed either:

- `RailroadPoolEmployeeControlNumber`, or
- `PositionControlNumber`

with the complementary link populated later by caller workflows.

---

## GAP 585: Qualification Effective Date Is Required but Not Factory-Initialized

`EffectiveDate` is required and must be assigned after factory creation.

---

## GAP 586: `EngineerPayRate` Models Four Rate Columns (Engineer/Trainee ST/OT)

Entity stores required effective-dated rates:

- `ESTHourRate`, `EOTHourRate`
- `TSTHourRate`, `TOTHourRate`

---

## GAP 587: Engineer Pay-Rate Factory Seeds Job-Code Link Only

`CreateInstance(long jobcode)` initializes `EngineerJobCodeControlNumber`; all rate values and effective date are caller-assigned.

---

## GAP 588: Engineer Pay-Rate Uses Effective-Dated Row Pattern

`EffectiveDate` is required on each engineer-pay-rate row, supporting historical/future rate versioning.

# Part 132: Gap Analysis - EngineerJobCode / EngineerJobCodeDelete Notes

Gaps 589-593 capture additional verified details from `EngineerJobCode.cs` and `EngineerJobCodeDelete.cs`.

---

## GAP 589: Engineer Job Code Factory Seeds Railroad Link Only

`EngineerJobCode.CreateInstance(long railroad)` initializes only `RailroadControlNumber`; required job-code fields are caller-populated.

---

## GAP 590: Engineer Job Code Includes Computed Display Field for Code + Weight

`LocomotiveType_Weight` returns formatted `"{PayClassCode} - {MaxWeightOnDrivers}"` as `[NotMapped]` helper text.

---

## GAP 591: Engineer Job Code Delete Marker Uses FK-as-PK Shape

`EngineerJobCodeDelete` key is `EngineerJobCodeControlNumber` with `[Key, ForeignKey("EngineerJobCode"), DatabaseGenerated(None)]`.

---

## GAP 592: Engineer Job Code Delete Marker Stores Create-Only Audit + Deleted Timestamp

Delete marker contains `DeletedDateTime`, `CreatedBy`, and `CreatedDateTime` with no modify audit fields.

---

## GAP 593: Engineer Job Code Delete Factory Seeds Link Only

`CreateInstance(long code)` initializes only `EngineerJobCodeControlNumber`; delete timestamp and create-audit values are caller-assigned.

# Part 133: Gap Analysis - README Migration Taskboard vs Current Repository State

Gaps 594-598 capture additional verified README-to-codebase alignment notes.

---

## GAP 594: README gRPC Service Matrix Is Target-State Specification, Not Implemented Runtime in This Repo

Listed gRPC service contracts (`EmployeeService`, `SeniorityService`, `RosterService`, etc.) are migration targets; current repository runtime remains ASP.NET MVC + existing service processes.

---

## GAP 595: README Proto/EF Core Task Checklist Is Still Open Planning Artifact

Proto creation and EF Core entity migration tasks are documented as unchecked action items, indicating pending work rather than delivered code in this workspace.

---

## GAP 596: README Notes Include Security Requirement (PII Encryption/Masking) as Forward/Operational Constraint

PII handling requirement is documented at README note level and should be treated as architectural/security constraint for migration and future refactors.

---

## GAP 597: README Session Expiration Value Is Operational Configuration Note

Session expiration (`480 minutes`) is documented in notes section and should be validated against runtime auth configuration when modernizing auth flow.

---

## GAP 598: README Describes Dual Environment Naming (`StrategicApplications` / `DevelopmentDatabase`) That Must Be Reconciled with Constructor-Bound Context Names

Runtime context constructors currently bind to demo/development context names in code, so environment naming in docs should be cross-checked during migration planning.

# Part 134: Gap Analysis - PayrollCode / PayrollCodeApprovalRole Notes

Gaps 599-603 capture additional verified details from `PayrollCode.cs` and `PayrollCodeApprovalRole.cs`.

---

## GAP 599: Payroll Code Factory Sets Client Link and Defaults `Arbitrary=false`

`PayrollCode.CreateInstance(long client)` seeds `ClientControlNumber` and explicitly defaults `Arbitrary` to false in constructor.

---

## GAP 600: `PayrollCode.Code_Description` Is NotMapped Display Helper

Computed property returns `"{Code} - {Description}"` and is marked `[NotMapped]` for UI/display usage.

---

## GAP 601: Payroll Code Supports Both ADP and UKG Interface Linkages

Entity exposes direct `ADPInterface` reference and `ICollection<UKGInterface>` references, indicating multi-payroll-system mapping support.

---

## GAP 602: `PayrollCodeApprovalRole` Stores Role by Both Guid and Name

Approval-role row persists `RoleId` (`Guid`) and `RoleName` (`string`) together, plus `Primary` designation.

---

## GAP 603: Payroll-Code-ApprovalRole Factory Seeds Payroll Link Only

`CreateInstance(long payroll)` initializes only `PayrollCodeControlNumber`; role identifiers/naming/primary flag are caller-assigned.

# Part 135: Gap Analysis - PayrollCategory / PayrollCategoryCode Notes

Gaps 604-608 capture additional verified details from `PayrollCategory.cs` and `PayrollCategoryCode.cs`.

---

## GAP 604: Payroll Category Factory Seeds Client Link Only

`PayrollCategory.CreateInstance(long client)` initializes only `ClientControlNumber`; required category metadata/flags are caller-populated.

---

## GAP 605: Payroll Category Has Three Required Reporting Dimension Flags

`STime`, `OTime`, and `Amount` are required booleans controlling reporting/aggregation dimensions in this entity.

---

## GAP 606: `PayrollCategoryCode` Uses Composite Key Bridge Shape

Bridge key is (`PayrollCategoryControlNumber`, `PayrollCodeControlNumber`) with both columns marked `DatabaseGenerated(None)`.

---

## GAP 607: PayrollCategoryCode Provides Single-Key and Full-Pair Factory Overloads

- `CreateInstance(long category)` seeds category link only
- `CreateInstance(long category, long code)` seeds both bridge keys

---

## GAP 608: PayrollCategoryCode Is Non-`ControlNumberBase` Join Entity

Bridge class models pure key linkage and does not inherit control-number/audit base fields.

# Part 136: Gap Analysis - PayrollReportGroup / GroupCategory Notes

Gaps 609-613 capture additional verified details from `PayrollReportGroup.cs` and `PayrollReportGroupCategory.cs`.

---

## GAP 609: Payroll Report Group Factory Seeds Client Link Only

`PayrollReportGroup.CreateInstance(long client)` initializes only `ClientControlNumber`; report-group number/name are caller-populated.

---

## GAP 610: `PayrollReportGroupCategory` Uses Composite Key Bridge Shape

Bridge key is (`PayrollReportGroupControlNumber`, `PayrollCategoryControlNumber`) with both columns `DatabaseGenerated(None)`.

---

## GAP 611: Single-Parameter GroupCategory Constructor Assigns `PayrollCategoryControlNumber`

`PayrollReportGroupCategory(long group)` sets `PayrollCategoryControlNumber = group` (not report-group key), which differs from two-parameter constructor intent.

---

## GAP 612: GroupCategory Factory Overloads Expose Both Partial and Full Link Seeding

- `CreateInstance(long group)` uses single-parameter constructor
- `CreateInstance(long group, long category)` seeds both keys explicitly

---

## GAP 613: PayrollReportGroupCategory Is Non-`ControlNumberBase` Join Entity

Bridge class models pure key linkage and does not include control-number/audit base inheritance.

# Part 137: Gap Analysis - ADPInterface / UKGInterface Entity Notes

Gaps 614-618 capture additional verified details from `ADPInterface.cs` and `UKGInterface.cs`.

---

## GAP 614: ADP Interface Uses PayrollCode FK-as-PK Shape

`ADPInterface` keys on `PayrollCodeControlNumber` with `[Key, ForeignKey("PayrollCode"), DatabaseGenerated(None)]`.

---

## GAP 615: ADP Interface `ADPCode` Is Private-Set and Derived in Constructor Flow

`ADPCode` has private setter and is assigned by `SetADPCode(...)` during create path based on column/code mapping rules.

---

## GAP 616: ADP Column 6 Uses Hard-Coded Payroll-Code Translation Table

For `ColumnNumber == 6`, specific payroll codes map to alternate ADP symbols (`03->S`, `04/06->V`, `12->P`, `14..18->4..8`).

---

## GAP 617: UKG Interface Uses Unique Index on `UKGEarningCode`

`UKGEarningCode` is required, max length 25, and marked `[Index(IsUnique = true)]`.

---

## GAP 618: UKG Interface Factory Seeds PayrollCode Link Only

`CreateInstance(PayrollCode code)` initializes `PayrollCodeControlNumber`; `UKGEarningCode` and `ValueType` are caller-assigned.

# Part 138: Gap Analysis - MarkOffPayrollCode / MarkOffMarkUpHours Notes

Gaps 619-623 capture additional verified details from `MarkOffPayrollCode.cs` and `MarkOffMarkUpHours.cs`.

---

## GAP 619: Both Mark-Off Config Entities Use MarkOffCode FK-as-PK Shape

Each class keys on `MarkoffCodeControlNumber` with `[Key, ForeignKey("MarkOffCode"), DatabaseGenerated(None)]`.

---

## GAP 620: MarkOffPayrollCode Adds PayrollCode Link and Basic-Day Flag

`MarkOffPayrollCode` stores required `PayrollCodeControlNumber` and `BasicDay` behavior flag in addition to mark-off-code linkage.

---

## GAP 621: MarkOffMarkUpHours Stores Required Integer Markup-Hour Override

`MarkOffMarkUpHours.MarkUpHours` provides explicit override value used when present in mark-off-code logic.

---

## GAP 622: Both Entities Store Full Create/Modify Audit Fields

Each class includes required `CreatedBy/CreatedDate` and `ModifiedBy/ModifiedDate` columns.

---

## GAP 623: Factory Methods Seed MarkOffCode Link Only

`CreateInstance(long markoff)` in both entities initializes only `MarkoffCodeControlNumber`; other required fields are caller-assigned.

# Part 139: Gap Analysis - PayrollPeriodProcessRecord / PayrollEarningRecord Notes

Gaps 624-629 capture additional verified details from `PayrollPeriodProcessRecord.cs` and `PayrollEarningRecord.cs`.

---

## GAP 624: Payroll-Period Process Factory Seeds Railroad Link Only

`PayrollPeriodProcessRecord.CreateInstance(long railroad)` initializes only `RailroadControlNumber`; period text/date and output paths are caller-assigned.

---

## GAP 625: Payroll-Period Process Path Fields Are Required but Unbounded Strings in Class

`ErrorLogPath`, `PayrollPath`, and `ReportPath` are required without explicit `[StringLength]` annotations in this model.

---

## GAP 626: Payroll-Earning Constructor Seeds Default Counters/Amounts

Base constructor path initializes:

- `RecordCount = 1`
- `CalculatedAmount = 0`
- `PaidAmount = 0`

---

## GAP 627: `IsProcessed` Treats Declined Non-Final Earnings as Processed

If earning is declined and processed record is null or non-final, `IsProcessed` returns true per getter logic.

---

## GAP 628: Compensation Debit/Credit Helpers Depend on `HttpContext.Current.User`

Both `DebitCompensationAccount` and `CreditCompensationAccount` read current username from web `HttpContext` without local null guard.

---

## GAP 629: `CreditCompensationAccount` Uses "Debit" Entry Type String in Account Entry Call

Credit helper currently calls `CreateCompensationTimeAccountEntry(..., "Debit", ...)` while adding positive hour adjustment, which is a noteworthy label/value combination.

# Part 140: Gap Analysis - OnDutyMoveCutOffTime / MarkUpRecord Notes

Gaps 630-634 capture additional verified details from `OnDutyMoveCutOffTime.cs` and `MarkUpRecord.cs`.

---

## GAP 630: On-Duty Move Cutoff Entity Inherits `ControlNumberBase`

`OnDutyMoveCutOffTime` has independent control-number identity plus base audit fields.

---

## GAP 631: On-Duty Move Cutoff Factory Seeds On-Duty-Time Link Only

`CreateInstance(long onduty)` initializes `AssignmentOnDutyTimeControlNumber`; required craft link and cutoff time are caller-assigned.

---

## GAP 632: MarkUpRecord Uses FK-as-PK 1:1 Shape to MarkOffRecord

Key is `MarkOffRecordControlNumber` with `[Key, ForeignKey("MarkOffRecord"), DatabaseGenerated(None)]`.

---

## GAP 633: MarkUpRecord Name-Resolution Properties Query Users via New DbContext Per Access

`CreatedByName` and `ModifiedByName` each instantiate `StrategicApplicationsContext` and query users by username.

---

## GAP 634: `ModifiedByName` Autoprocess Check Uses `CreatedBy` Field

In `ModifiedByName` getter, fallback check references `this.CreatedBy.Equals("autoprocess")` before querying `ModifiedBy` user.

# Part 141: Gap Analysis - MarkOff Request/Record Approval Entity Notes

Gaps 635-639 capture additional verified details from `MarkOffRequestApproval.cs` and `MarkOffRecordApproval.cs`.

---

## GAP 635: Both Approval Entities Use FK-as-PK 1:1 Shapes

- `MarkOffRequestApproval` keyed by `MarkOffRequestRecordControlNumber`
- `MarkOffRecordApproval` keyed by `MarkOffRecordControlNumber`

Each uses `[Key, ForeignKey(...), DatabaseGenerated(None)]`.

---

## GAP 636: Approval Entities Store Optional Approver Employee Link in Class

`EmployeeControlNumber` is present in both entities without `[Required]` attribute in class definition.

---

## GAP 637: Approval Entities Include Full Create/Modify Audit Fields

Both entities define required `CreatedBy/CreatedDate` and `ModifiedBy/ModifiedDate` properties.

---

## GAP 638: Approval Factories Seed Parent Link Only

`CreateInstance(long record)` initializes parent record key only; approver linkage and audit values are caller-assigned.

---

## GAP 639: Request and Record Approval Are Modeled Separately

Schema keeps distinct entities for request approval and record approval rather than sharing a combined approval table.

# Part 142: Gap Analysis - EmploymentStatus / EmploymentStatusHistory Notes

Gaps 640-644 capture additional verified details from `EmploymentStatus.cs` and `EmploymentStatusHistory.cs`.

---

## GAP 640: EmploymentStatus Factory Seeds Client Link Only

`EmploymentStatus.CreateInstance(long client)` initializes only `ClientControlNumber`; status code/name/number/employment code are caller-assigned.

---

## GAP 641: EmploymentStatus Includes Both Primary Employee and Historical/Daily Usage Navigations

Entity holds direct employee links plus history (`EmploymentStatusHistory`) and daily status record (`DailyRailroadEmployeeStatuses`) navigations.

---

## GAP 642: EmploymentStatusHistory Is Full `ControlNumberBase` Entity (Not Composite Join)

History rows have independent control-number identity with required employee/status/date payload.

---

## GAP 643: EmploymentStatusHistory Factory Fully Seeds Relationship Keys

`CreateInstance(long employee, long status)` sets both required FK fields; status-change date and audit values are caller-assigned.

---

## GAP 644: Employment Status and History Model Separate Current Lookup from Temporal Events

Current status definitions are modeled in `EmploymentStatus`, while change events are tracked in `EmploymentStatusHistory` rows.

# Part 143: Gap Analysis - Employee Prior Service Credit + Employee Display/Service Notes

Gaps 645-649 capture additional verified details from `EmployeePriorServiceCredit.cs` and `Employee.cs`.

---

## GAP 645: Employee Prior Service Credit Uses FK-as-PK 1:1 Shape

`EmployeePriorServiceCredit` keys on `EmployeeControlNumber` with `[Key, ForeignKey("Employee"), DatabaseGenerated(None)]`.

---

## GAP 646: Employee Prior Service Credit Stores Create-Only Audit

Entity defines `CreatedBy` and `CreatedDate` only; no modify-audit fields are present.

---

## GAP 647: Prior Service Credit Factory Seeds Employee Link Only

`CreateInstance(long employee)` initializes `EmployeeControlNumber`; service-year/month/day values and create-audit are caller-assigned.

---

## GAP 648: Employee Service-Years Calculation Uses Integer Division for Prior Months/Days

`Employee.ServiceYears` derives additional years from prior credit using integer math:

- `months = ServiceMonths / 12`
- `days = ServiceDays / 365`

fractional remainder is truncated.

---

## GAP 649: `EmpNbr_FullName_LastNameFirst_ApprovalCount` Padding Loop Reassigns to Single Space

While-loop currently sets `name = " "` when length is below 20, which does not increase toward target length and can result in non-terminating loop behavior.

# Part 144: Gap Analysis - Employee Computed Property and Defaulting Notes

Gaps 650-654 capture additional verified details from `Employee.cs`.

---

## GAP 650: Employment-State Booleans Use `Contains(...)` Matching on EmploymentCode

`IsActive`, `IsOutOfService`, and `IsOnExtendedAbsence` evaluate using `Status.EmploymentCode.Contains("AT"|"OS"|"EA")` rather than exact-code equality.

---

## GAP 651: Role Checks Depend on Hard-Coded Role GUID Strings

- Railroad Employee-only role: `8a36ccc0-8478-4ef2-b651-6187a12215cf`
- Union Representative role: `073f1b6b-a776-49bb-97fa-4cca4ad51382`

---

## GAP 652: `ApprovalCount` Property Opens New DbContext per Access

Getter creates a new `StrategicApplicationsContext` and computes count via `CollectionLists.GetApprovalPayrollEarningRecords(...)`.

---

## GAP 653: Employee Constructor Applies Domain Defaults

Default constructor seeds values including:

- BirthDate = Jan 1 of (current year - 25)
- EmploymentDate = today
- IssuingState = `TX`
- CallForOvertime = `true`, ProcessPayroll = `true`

---

## GAP 654: Employee Navigation Surface Includes Multiple Approval/Requirement Collections

Entity directly holds numerous workflow collections (earnings approvals, markoff approvals, requirement links, wait-list records), making it a central aggregate in approval and scheduling flows.

# Part 145: Gap Analysis - Client / Railroad Auto-Function and Holiday-File Notes

Gaps 655-660 capture additional verified details from `Client.cs` and `Railroad.cs`.

---

## GAP 655: Client Factory Seeds Name as Empty String

`Client.CreateInstance()` returns `new Client(String.Empty)`; required/operational fields such as automation flags are caller-populated.

---

## GAP 656: `Client.DisableRailroadAutoFunctions(...)` Marks Client Entity Modified While Toggling Railroad Flags

Inside railroad loop, method updates `railroad.AutoAssignments` but calls `db.Entry(this).State = EntityState.Modified` before save.

---

## GAP 657: Railroad Factory Seeds Client Link Only

`Railroad.CreateInstance(long client)` initializes only `ClientControlNumber`; mark/name/automation fields are caller-assigned.

---

## GAP 658: `Railroad.DisableRailroadPoolAutoFunctions(...)` Marks Railroad Entity Modified While Toggling Pool Flags

Method updates each pool’s auto flags, then sets `db.Entry(this).State = EntityState.Modified` inside loop before single save.

---

## GAP 659: Railroad Holiday-Record Generation Writes `.HR` Files Named by Employee Full Name

`CreatePayrollHolidayRecords(...)` writes holiday request files to inbound path with filename pattern:

`{EmpNbr_FullName}.HR`

---

## GAP 660: Railroad Holiday-Record Flow Is File-Emission Based (Not Immediate Payroll Row Insert)

Method emits inbound `.HR` files with structured text payload; downstream processing stages create payroll-holiday records.

# Part 146: Gap Analysis - RailroadPool Query/Defaulting Notes

Gaps 661-666 capture additional verified details from `RailroadPool.cs`.

---

## GAP 661: RailroadPool Constructor Sets Only Subset of Boolean Defaults

Default constructor initializes:

- `AllowBulletins = true`
- `AllowSeniorityMoves = true`
- `AllowTemporaryAssignments = false`

Other required boolean fields are caller-populated.

---

## GAP 662: Multiple Pool Dashboard Counters Are NotMapped Query Delegates

`BulletinCount`, `SeniorityMoveCount`, `HoldDownCount`, `NotificationCount`, `MarkOffRecordCount`, and `UnassignedEmployeeCount` call query helpers at access time.

---

## GAP 663: `GetRailroadPoolEmployees(...)` Uses Code-Driven Branching (`AT`, `NA`, `XE`, default)

Filtering logic changes by `empcode` value, including different seniority/status constraints per branch.

---

## GAP 664: "AT" Branch Includes LastActiveRoster Seniority Constraint

For active-employee branch, query requires `Seniority.Any(s => s.LastActiveRoster && same-pool craft)` in addition to employment status.

---

## GAP 665: `CreateOffDays(...)` Applies Pool-Number-Based Sleep Throttle

Method delays execution with `Thread.Sleep(this.PoolNumber * 1000)` when `PoolNumber > 10` before off-day processing loop.

---

## GAP 666: Roster Filter Is Applied as Final In-Memory/Post-Query Restriction

If `roster > 0`, method applies additional filtering on resulting pool-employee set for matching `RosterControlNumber` + `LastActiveRoster`.

# Part 147: Gap Analysis - RailroadPoolEmployeeTrainingDate Notes

Gaps 667-671 capture additional verified details from `RailroadPoolEmployeeTrainingDate.cs`.

---

## GAP 667: Training-Date Factory Seeds Pool-Employee Link Only

`CreateInstance(long rpemployee)` initializes `RailroadPoolEmployeeControlNumber`; crew/position/date fields are caller-populated.

---

## GAP 668: Training On-Duty Creation Is Conditional on Existing DailyAssignmentCrew Match

`CreateDailyCrewPositionOnDutyRecord(...)` first looks up matching daily assignment crew for target date/crew; if none exists, method exits without creating records.

---

## GAP 669: Training Daily Position Creation Uses Sentinel RailroadPosition Control Number

When creating temporary training crew-position row, method calls:

`CrewPosition.CreateInstance(crewnbr, 99999999999999999, posnbr, tdate, ...)`

using sentinel railroad-position control-number value.

---

## GAP 670: Existing-Record Guard Requires Existing On-Duty Record for Same Employee/Position/Date

Duplicate-prevention query checks existing daily crew position with same date/position and existing on-duty record for target railroad-pool-employee.

---

## GAP 671: Training On-Duty Creation Saves Immediately In-Method

After adding daily crew position and creating on-duty record, method executes `db.SaveChanges()` directly.

# Part 148: Gap Analysis - Earnings Approval Required/Employee Entity Notes

Gaps 672-676 capture additional verified details from `EarningsApprovalRequiredRecord.cs` and `EarningsApprovalEmployee.cs`.

---

## GAP 672: EarningsApprovalRequiredRecord Uses FK-as-PK 1:1 Shape to PayrollEarningRecord

Primary key is `PayrollEarningRecordControlNumber` with `[Key, ForeignKey("PayrollEarningRecord"), DatabaseGenerated(None)]`.

---

## GAP 673: Required-Approval Completion State Is Derived from Child Approval/Declination Rows

`IsCompleted` is true when either `EarningsApprovalRecord` or `EarningsDeclanationRecord` exists.

---

## GAP 674: `IsApproved`/`IsDeclined` Are Presence-Based Flags

- `IsApproved` => approval row exists
- `IsDeclined` => declination row exists

No extra status field is used in this entity.

---

## GAP 675: `EarningsApprovalEmployee` Uses Composite Key Bridge (Earning + Employee)

Bridge key is (`PayrollEarningRecordControlNumber`, `ApprovalEmployeeControlNumber`) with first key explicitly `DatabaseGenerated(None)`.

---

## GAP 676: EarningsApprovalEmployee Factory Seeds Earning Link Only

`CreateInstance(long earning)` initializes `PayrollEarningRecordControlNumber`; approver employee key is caller-populated.

# Part 149: Gap Analysis - PhantomJsWrapper Legacy Utility Notes

Gaps 677-681 capture additional verified details from `PhantomJsWrapper/PhantomJsWorker.cs` and `PhantomJsWrapper.csproj`.

---

## GAP 677: PhantomJsWrapper Targets `.NET Framework 4.5.2` (Not 4.7.2)

`PhantomJsWrapper.csproj` specifies `<TargetFrameworkVersion>v4.5.2</TargetFrameworkVersion>`.

---

## GAP 678: Phantom Wrapper Depends on Bundled `phantomjs.exe` + Script Content

Project includes `phantomjs.exe` and `grabWebPageToPDF.js` as content artifacts used by execution helper.

---

## GAP 679: PDF Capture Helper Enforces `.pdf` Extension and Sanitizes Output Filename

`GrabWebPageToPDF(...)` validates output extension equals `.pdf` and replaces spaces/quotes in generated file name.

---

## GAP 680: Phantom Process Timeout Path Is Non-Throwing

When process does not exit within 5 minutes, timeout exception throw is commented out, so method returns result object without raising timeout error.

---

## GAP 681: Execution Errors Are Wrapped into Result Exception Field

Helper catches exceptions and stores `new Exception(result.StartInfo.ToString(), ex)` in returned result object instead of rethrowing.

# Part 150: Gap Analysis - RestartApplicationPool Utility Notes

Gaps 682-686 capture additional verified details from `RestartApplicationPool/Program.cs` and `RestartApplicationPool.csproj`.

---

## GAP 682: Restart Utility Expects Application-Pool Name in `args[0]` Without Argument Guard

`Program.Main` directly indexes `args[0]` when resolving target app pool.

---

## GAP 683: Restart Loop Runs Up to 10 Iterations with 1-Second Sleep

After stop/start logic, program loops with `Thread.Sleep(1000)` until counter reaches 10.

---

## GAP 684: Restart Utility Exception Handling Is Log-and-Swallow

Failures in restart flow are logged via `EventLogger.WriteErrorLogEvent(...)` and not rethrown.

---

## GAP 685: Restart Utility Project Targets `.NET Framework 4.7.2`

`RestartApplicationPool.csproj` explicitly sets `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>`.

---

## GAP 686: Restart Utility Project Carries ClickOnce/Publish Metadata

Project file includes publish URL and ClickOnce-oriented properties (`PublishUrl`, `Install`, `BootstrapperEnabled`, etc.), indicating deployment as distributable utility executable.

# Part 151: Gap Analysis - SAAtHocMessageService Installer/Project Metadata Notes

Gaps 687-691 capture additional verified details from `SAAtHocMessageService/ProjectInstaller.cs` and `SAAtHocMessageService.csproj`.

---

## GAP 687: SAAtHoc Project Installer Class Is Minimal Wrapper

`ProjectInstaller` only invokes `InitializeComponent()` and relies on designer/configured installer components for service installation behavior.

---

## GAP 688: SAAtHoc Service Project Targets `.NET Framework 4.7.2`

Project file explicitly sets `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>`.

---

## GAP 689: SAAtHoc Service Project Is `WinExe` + ServiceProcess-Based Deployment Unit

Output type is `WinExe` and project references `System.ServiceProcess`, aligning with Windows-service host model.

---

## GAP 690: SAAtHoc Service Project Directly References Web-App Project

`SAAtHocMessageService.csproj` includes project reference to `StrategicApplications.csproj`, indicating shared model/utility usage across host boundary.

---

## GAP 691: SAAtHoc Service Project Includes ClickOnce/Bootstrapper Metadata

Project contains publish/install/bootstrapper properties (`PublishUrl`, `Install`, `BootstrapperEnabled`, etc.), indicating packaged deployment configuration in project metadata.

# Part 152: Gap Analysis - Service Host Project Metadata (DailyCallSheet / ImportPayroll)

Gaps 692-696 capture additional verified details from `SADailyCallSheetService.csproj` and `SAImportPayrollService.csproj`.

---

## GAP 692: Both Service Host Projects Target `.NET Framework 4.7.2` as `WinExe`

Both project files specify `v4.7.2` and `OutputType=WinExe`, aligning with Windows service host executables.

---

## GAP 693: DailyCallSheet Service Project Explicitly References `System.Messaging`

`SADailyCallSheetService.csproj` includes `System.Messaging`, matching queue-driven service behavior in code.

---

## GAP 694: Both Service Projects Depend on SAClassLibrary via ProjectReference

Each host project directly references `..\SAClassLibrary\SAClassLibrary.csproj` for shared models/utilities.

---

## GAP 695: DailyCallSheet Service Project Contains UNC Publish Metadata

Project metadata includes publish settings such as `PublishUrl=\\sql-svr\SA\Services\` and related ClickOnce/bootstrapper properties.

---

## GAP 696: ImportPayroll Service Project Includes Placeholder `Models` Folder Entry in Project File

`SAImportPayrollService.csproj` contains `<Folder Include="Models\" />` despite current implementation centering on service classes.

# Part 153: Gap Analysis - AssemblyInfo/Package Metadata Notes (Utility-Service Projects)

Gaps 697-701 capture additional verified metadata details from:

- `PhantomJsWrapper/Properties/AssemblyInfo.cs`
- `RestartApplicationPool/Properties/AssemblyInfo.cs`
- `SAAtHocMessageService/Properties/AssemblyInfo.cs`
- `SAAtHocMessageService/packages.config`

---

## GAP 697: Utility/Service AssemblyInfo Files Use Fixed `1.0.0.0` Versioning

All reviewed AssemblyInfo files specify fixed `AssemblyVersion("1.0.0.0")` and `AssemblyFileVersion("1.0.0.0")`.

---

## GAP 698: Reviewed AssemblyInfo Files Set `ComVisible(false)`

These projects explicitly hide types from COM by default.

---

## GAP 699: Assembly GUIDs Align with Project Identity Values

Assembly-level GUID attributes correspond to project-specific identities used for COM/type library metadata.

---

## GAP 700: SAAtHocMessageService packages.config Pins EF6 + ASP.NET Identity 2.2.3

NuGet package manifest includes `EntityFramework 6.4.4`, `Microsoft.AspNet.Identity.Core 2.2.3`, and `Microsoft.AspNet.Identity.EntityFramework 2.2.3` targeting `net472`.

---

## GAP 701: Service Metadata Indicates Classic AssemblyInfo + packages.config Pattern

Reviewed project metadata follows legacy .NET Framework project conventions (AssemblyInfo + packages.config + non-SDK csproj).

# Part 154: Gap Analysis - IControlNumber Interface Parity Across Projects

Gaps 702-705 capture additional verified details from:

- `SAClassLibrary/Interfaces/IControlNumber.cs`
- `StrategicApplications/Models/Interfaces/IControlNumber.cs`

---

## GAP 702: Both Projects Define Parallel `IControlNumber` Interfaces

Both SAClassLibrary and StrategicApplications contain their own `IControlNumber` interface definitions in project-local namespaces.

---

## GAP 703: Interface Contract Requires Read-Only `ControlNumber`

`ControlNumber` is exposed as getter-only (`long ControlNumber { get; }`) in both interface definitions.

---

## GAP 704: Interface Contract Standardizes Create/Modify Audit Fields

Both interfaces require:

- `CreatedBy` / `ModifiedBy`
- `CreatedDate` / `ModifiedDate`

---

## GAP 705: Cross-Project Interface Duplication Implies Namespace-Specific Type Identity

Although signatures are identical, interface types are distinct by namespace/project, which affects direct type interchange between project boundaries.

# Part 155: Gap Analysis - SAAtHoc Service Designer Partial-Class Notes

Gaps 706-709 capture additional verified details from:

- `SAAtHocMessageService/Services/SAAssignmentCallService.Designer.cs`
- `SAAtHocMessageService/Services/SAAssignmentOnDutyService.Designer.cs`

---

## GAP 706: Designer Partials Set ServiceName Explicitly per Service

- `SAAssignmentCallService`
- `SAAssignmentOnDutyService`

service names are assigned in generated `InitializeComponent()` methods.

---

## GAP 707: Designer Partials Implement Standard `Dispose(bool)` Component Cleanup Pattern

Both generated classes dispose `components` container when `disposing` is true.

---

## GAP 708: Designer Files Are Minimal and Contain No Business Logic

Generated partials provide host-component wiring only; runtime scheduling/message logic resides in corresponding non-designer service files.

---

## GAP 709: Service Name Binding Relies on Generated Partial Preservation

Correct Windows-service identity depends on generated `ServiceName` assignments, reinforcing need to preserve designer partial consistency.

# Part 156: Gap Analysis - Service/Utility `packages.config` Metadata Notes

Gaps 710-714 capture additional verified details from:

- `RestartApplicationPool/packages.config`
- `SADailyCallSheetService/packages.config`
- `SAImportPayrollService/packages.config`

---

## GAP 710: DailyCallSheet and ImportPayroll Service Hosts Pin EF 6.4.4 for `net472`

Both service package manifests currently include `EntityFramework` version `6.4.4` targeting `.NET Framework 4.7.2`.

---

## GAP 711: RestartApplicationPool Uses Broad NetStandard Compatibility Package Set on `net472`

Restart utility package manifest includes a large compatibility package set (`NETStandard.Library 1.6.0` + many `System.*` packages) alongside `Microsoft.Web.Administration`.

---

## GAP 712: RestartApplicationPool Depends on `Microsoft.Web.Administration 11.1.0` via NuGet

Package configuration and project references align around IIS administration API package consumption.

---

## GAP 713: Package Manifests Reflect Legacy `packages.config` Dependency Management Model

Reviewed projects use `packages.config` rather than SDK-style `PackageReference` dependency declarations.

---

## GAP 714: Service/Utility Package Targets Match Workspace Baseline (`net472`)

All reviewed package manifests specify `targetFramework="net472"`, consistent with repository-wide .NET Framework runtime baseline.

# Part 157: Gap Analysis - Service Installer + Assembly Metadata Notes (ImportPayroll / DailyCallSheet)

Gaps 715-719 capture additional verified details from:

- `SAImportPayrollService/ProjectInstaller.cs`
- `SAImportPayrollService/Properties/AssemblyInfo.cs`
- `SADailyCallSheetService/ProjectInstaller.cs`
- `SADailyCallSheetService/Properties/AssemblyInfo.cs`

---

## GAP 715: ImportPayroll and DailyCallSheet Installer Classes Are Minimal Designer Wrappers

Both `ProjectInstaller` classes only call `InitializeComponent()` and rely on generated installer components for service registration settings.

---

## GAP 716: Both Service AssemblyInfo Files Use Fixed `1.0.0.0` Assembly/File Versions

Reviewed assembly metadata is fixed-version (no wildcard auto-incrementing).

---

## GAP 717: Both Service AssemblyInfo Files Set `ComVisible(false)`

COM visibility is disabled by default for these service assemblies.

---

## GAP 718: Service Assembly GUIDs Map to Project Identity Metadata

Assembly GUID values correspond to project identity values used for COM/type library metadata.

---

## GAP 719: ImportPayroll and DailyCallSheet Service Metadata Retains Classic AssemblyInfo Pattern

Both projects use legacy .NET Framework assembly metadata conventions via explicit `AssemblyInfo.cs` declarations.

# Part 158: Gap Analysis - Additional Service/Utility Project File Baseline Notes

Gaps 720-724 capture additional verified baseline metadata from project files:

- `SAAtHocMessageService/SAAtHocMessageService.csproj`
- `RestartApplicationPool/RestartApplicationPool.csproj`
- `PhantomJsWrapper/PhantomJsWrapper.csproj`

---

## GAP 720: SAAtHoc Service Host Project Targets `.NET Framework 4.7.2`

`SAAtHocMessageService/SAAtHocMessageService.csproj` explicitly targets `v4.7.2` and is configured as Windows service host executable (`WinExe`).

---

## GAP 721: RestartApplicationPool Utility Project Targets `.NET Framework 4.7.2`

`RestartApplicationPool/RestartApplicationPool.csproj` explicitly targets `v4.7.2` and references IIS administration package/runtime dependencies.

---

## GAP 722: PhantomJsWrapper Project Targets Older `.NET Framework 4.5.2`

`PhantomJsWrapper/PhantomJsWrapper.csproj` remains on `v4.5.2`, creating a project-level framework-version variance within repository.

---

## GAP 723: Service/Utility Project Files Maintain Non-SDK Legacy MSBuild Format

Reviewed project files use classic MSBuild XML with explicit reference items/import targets rather than SDK-style project schema.

---

## GAP 724: Multiple Service/Utility Project Files Retain Publish/Bootstrapper Metadata

Project files include publish/install/bootstrapper settings, indicating deployment configuration is encoded at csproj layer for service/utility artifacts.

# Part 159: Gap Analysis - SAClassLibrary Early Migration Baseline Notes

Gaps 725-729 capture additional verified details from:

- `SAClassLibrary/Migrations/202001180331567_InitialMigration.cs`
- `SAClassLibrary/Migrations/202001180331567_InitialMigration.Designer.cs`
- `SAClassLibrary/Migrations/202001191335135_ChangeRailroadAreaName.cs`
- `SAClassLibrary/Migrations/202001191335135_ChangeRailroadAreaName.Designer.cs`

---

## GAP 725: SAClassLibrary Initial Migration Is No-Op Skeleton

`InitialMigration` defines empty `Up()` and `Down()` methods, serving as baseline migration marker.

---

## GAP 726: Migration Designer Metadata Is EF6.4-Generated (`EntityFramework.Migrations 6.4.0`)

Designer files implement `IMigrationMetadata` and pull target model snapshot from embedded migration resources.

---

## GAP 727: `ChangeRailroadAreaName` Migration Renames `RailroadAreas` to `SlowOrderAreas`

`Up()` performs table rename from `dbo.RailroadAreas` -> `dbo.SlowOrderAreas`.

---

## GAP 728: `ChangeRailroadAreaName` Down Path Reverts Table Name

`Down()` renames `dbo.SlowOrderAreas` back to `dbo.RailroadAreas`.

---

## GAP 729: Designer Metadata IDs Align with Timestamped Migration Class Names

Reviewed designer `Id` values match migration file timestamp/name convention, supporting deterministic EF6 migration ordering.

# Part 160: Gap Analysis - SAClassLibrary Migration Notes (CleanStartPoint + SlowOrderTables)

Gaps 730-734 capture additional verified details from:

- `SAClassLibrary/Migrations/202001191514129_CreateCleanStartPoint.cs`
- `SAClassLibrary/Migrations/202001191514129_CreateCleanStartPoint.Designer.cs`
- `SAClassLibrary/Migrations/202001300254299_AddSlowOrderTables.cs`
- `SAClassLibrary/Migrations/202001300254299_AddSlowOrderTables.Designer.cs`

---

## GAP 730: `CreateCleanStartPoint` Migration Is No-Op Marker

Migration defines empty `Up()` and `Down()` methods as baseline/versioning marker.

---

## GAP 731: `AddSlowOrderTables` Introduces Core Slow-Order Record + Change/Complete/Delete Child Tables

Migration creates `SlowOrderRecords`, `SlowOrderChangeRecords`, `SlowOrderCompleteRecords`, and `SlowOrderDeleteRecords` with key/relationship constraints.

---

## GAP 732: Slow-Order Complete/Delete Tables Use FK-as-PK One-to-One Marker Pattern

Both complete/delete tables key on `SlowOrderRecordControlNumber` and reference parent slow-order record.

---

## GAP 733: `AddSlowOrderTables` Down Path Fully Drops Added FKs/Indexes/Tables

Rollback reverses foreign keys and indexes prior to dropping all four slow-order tables.

---

## GAP 734: Reviewed Designer Files Use Standard EF6 Migration Metadata Pattern

Designer classes implement `IMigrationMetadata` with timestamped `Id` and resource-backed `Target` model snapshots.

# Part 161: Gap Analysis - SAClassLibrary Migration Notes (BeSafe Tables + Record Refactor)

Gaps 735-739 capture additional verified details from:

- `SAClassLibrary/Migrations/202002161448560_AddBeSafeTables.cs`
- `SAClassLibrary/Migrations/202002161448560_AddBeSafeTables.Designer.cs`
- `SAClassLibrary/Migrations/202002170336059_ChangeBeSafeRecord.cs`
- `SAClassLibrary/Migrations/202002170336059_ChangeBeSafeRecord.Designer.cs`

---

## GAP 735: `AddBeSafeTables` Introduces Full BeSafe Table Family

Migration creates BeSafe core/child tables including records, actions, categories, email groups, changes, delete markers, and resolve markers.

---

## GAP 736: BeSafe Delete/Resolve Tables Use FK-as-PK Marker Pattern

`BeSafeDeleteRecords` and `BeSafeResolveRecords` key directly on `BeSafeRecordControlNumber` and reference parent BeSafe record.

---

## GAP 737: `ChangeBeSafeRecord` Refactors BeSafe Ownership Link from Pool-Employee to Railroad-Employee

Migration drops `RailroadPoolEmployeeControlNumber` FK and adds required `RailroadEmployeeControlNumber` FK in `BeSafeRecords`.

---

## GAP 738: `ChangeBeSafeRecord` Adds `ActionDate` Field to BeSafeActionRecords

Refactor migration introduces required action-date timestamp column for BeSafe action rows.

---

## GAP 739: Reviewed BeSafe Migration Designer Files Follow Standard EF6 Metadata Contract Pattern

Designer files implement `IMigrationMetadata` with timestamped migration `Id` and resource-backed target model snapshots.

# Part 162: Gap Analysis - SAClassLibrary Migration Notes (BeSafe Area/Subdivision)

Gaps 740-744 capture additional verified details from:

- `SAClassLibrary/Migrations/202002190324303_AddBeSafeArea.cs`
- `SAClassLibrary/Migrations/202002190324303_AddBeSafeArea.Designer.cs`
- `SAClassLibrary/Migrations/202002270244218_AddBeSafeSubdivision.cs`
- `SAClassLibrary/Migrations/202002270244218_AddBeSafeSubdivision.Designer.cs`

---

## GAP 740: `AddBeSafeArea` Introduces `BeSafeAreas` Table and Links BeSafeRecords to Area

Migration creates `BeSafeAreas` and adds required `BeSafeAreaControlNumber` FK column to `BeSafeRecords`.

---

## GAP 741: `AddBeSafeSubdivision` Introduces `BeSafeSubdivisions` and Links Areas to Subdivisions

Migration creates `BeSafeSubdivisions` and adds required `BeSafeSubdivisionControlNumber` FK column to `BeSafeAreas`.

---

## GAP 742: Both Migrations Apply Explicit Foreign-Key Indexes for New Link Columns

Each migration creates indexes for newly introduced FK columns before binding foreign-key constraints.

---

## GAP 743: Down Paths Fully Revert Added Columns/Tables and Relationships

Rollback logic drops FKs/indexes then removes added columns/tables for both area and subdivision migration steps.

---

## GAP 744: Designer Files Follow Standard EF6 Metadata Contract Pattern

Both reviewed designer files implement `IMigrationMetadata` with timestamped migration identifiers and resource-backed target snapshots.

# Part 163: Gap Analysis - SAClassLibrary Migration Notes (Railroad Information Schema)

Gaps 745-749 capture additional verified details from:

- `SAClassLibrary/Migrations/202003030311537_AddRailroadInformationType.cs`
- `SAClassLibrary/Migrations/202003030311537_AddRailroadInformationType.Designer.cs`
- `SAClassLibrary/Migrations/202003281058275_AddRailroadInformationTables.cs`
- `SAClassLibrary/Migrations/202003281058275_AddRailroadInformationTables.Designer.cs`

---

## GAP 745: `AddRailroadInformationType` Creates Core Information-Type Lookup Table

Migration introduces `RailroadInformationTypes` with railroad FK, type number/name, and audit columns.

---

## GAP 746: `AddRailroadInformationTables` Adds Information Record Core + Publish/Close/Cancel/Delete Child Tables

Migration introduces `RailroadInformationRecords` and one-to-one child marker tables for cancel/close/delete/publish lifecycle events.

---

## GAP 747: Information Child Lifecycle Tables Use FK-as-PK Pattern in Migration

Each lifecycle table keys on `RailroadInformationRecordControlNumber`, matching one-row-per-record lifecycle marker design.

---

## GAP 748: Information-Type Table Is Extended in Same Migration with Header/Signature Fields

`AddRailroadInformationTables` adds `HeaderTitle`, `SignatureName`, and `SignatureTitle` columns to `RailroadInformationTypes`.

---

## GAP 749: Reviewed Designer Files Follow Standard EF6 Metadata Contract Pattern

Both designer files implement `IMigrationMetadata` and expose timestamped migration IDs with resource-backed target snapshots.

# Part 164: Gap Analysis - SAClassLibrary Migration Notes (OffProperty Tie-Up + MarkOffCode Flags)

Gaps 750-754 capture additional verified details from:

- `SAClassLibrary/Migrations/202004030029124_AddOffPropertyTieUpRecord.cs`
- `SAClassLibrary/Migrations/202004030029124_AddOffPropertyTieUpRecord.Designer.cs`
- `SAClassLibrary/Migrations/202004060125106_ChangeMarkOffCode_AddEmployeeCanMarkOffUp.cs`
- `SAClassLibrary/Migrations/202004060125106_ChangeMarkOffCode_AddEmployeeCanMarkOffUp.Designer.cs`

---

## GAP 750: Off-Property Tie-Up Migration Adds Dedicated Tracking Table with Optional AspNetUser Link

`AddOffPropertyTieUpRecord` creates `OffPropertyTieUpRecords` with optional `AspNetUserId` FK and tie-up metadata/audit columns.

---

## GAP 751: Off-Property Tie-Up Table Uses Nullable User Foreign Key

Migration defines `AspNetUserId` as nullable string(128), enabling tie-up records without mandatory user-link presence.

---

## GAP 752: MarkOffCode Flag Migration Adds Employee-Controlled MarkOff/MarkUp Booleans

`ChangeMarkOffCode_AddEmployeeCanMarkOffUp` adds required boolean columns:

- `EmployeeCanMarkOff`
- `EmployeeCanMarkUp`

---

## GAP 753: MarkOffCode Flag Migration Has Symmetric Down Rollback

Down path drops both newly added mark-off/mark-up permission columns.

---

## GAP 754: Reviewed Designer Files Follow Standard EF6 Metadata Contract Pattern

Both designer files implement `IMigrationMetadata` with timestamped migration IDs and resource-backed target snapshots.

# Part 165: Gap Analysis - SAClassLibrary Migration Notes (SyncDatabase2 + Sync_Database1)

Gaps 755-759 capture additional verified details from:

- `SAClassLibrary/Migrations/202005130053324_SyncDatabase2.cs`
- `SAClassLibrary/Migrations/202005130053324_SyncDatabase2.Designer.cs`
- `SAClassLibrary/Migrations/202006201230575_Sync_Database1.cs`
- `SAClassLibrary/Migrations/202006201230575_Sync_Database1.Designer.cs`

---

## GAP 755: `SyncDatabase2` Removes Legacy Optional PayrollRecord Foreign-Key Columns

Migration drops three optional FK columns from `PayrollRecords` linking to holiday, daily-employee-position, and daily-extra-board-position tables.

---

## GAP 756: `SyncDatabase2` Down Path Recreates Removed PayrollRecord Columns/Indexes/FKs

Rollback logic restores dropped columns, indexes, and FK relationships.

---

## GAP 757: `Sync_Database1` Migration Is No-Op Marker

`Sync_Database1` defines empty `Up()` and `Down()` methods as migration synchronization marker.

---

## GAP 758: Reviewed Designer Files Follow Standard EF6 Metadata Contract Pattern

Both designer files implement `IMigrationMetadata` with timestamped IDs and resource-backed target snapshots.

---

## GAP 759: Sync-Named Migrations Indicate Periodic Schema Alignment Steps

Naming and content pattern show explicit schema sync checkpoints (including no-op markers) in migration history.

# Part 166: Gap Analysis - SAClassLibrary Migration Notes (Sync_Database2 + AddUkGInterface)

Gaps 760-764 capture additional verified details from:

- `SAClassLibrary/Migrations/202201111714028_Sync_Database2.cs`
- `SAClassLibrary/Migrations/202201111714028_Sync_Database2.Designer.cs`
- `SAClassLibrary/Migrations/202203041312488_AddUkGInterface.cs`
- `SAClassLibrary/Migrations/202203041312488_AddUkGInterface.Designer.cs`

---

## GAP 760: `Sync_Database2` Migration Is No-Op Marker

Migration defines empty `Up()` and `Down()` methods, indicating schema synchronization checkpoint without direct DDL changes.

---

## GAP 761: `AddUkGInterface` Migration Is Also No-Op Marker in Current File

Migration currently contains empty `Up()`/`Down()` bodies despite name indicating UKG-related schema intent.

---

## GAP 762: Reviewed Designer Metadata Uses EF Migration Generator Version `6.4.4`

These designer files show `GeneratedCode("EntityFramework.Migrations", "6.4.4")` metadata version marker.

---

## GAP 763: Timestamped Migration IDs Continue Deterministic Ordering Convention

Designer `Id` values match timestamped naming pattern (`202201...`, `202203...`) used for ordered migration history.

---

## GAP 764: Sync/Feature-Named No-Op Migrations Indicate Historical Alignment Steps

Combined pattern of named but no-op migrations suggests explicit schema-version alignment milestones in SAClassLibrary migration lineage.

# Part 167: Gap Analysis - SAClassLibrary Migration Notes (Payroll Rate% Marker + BeSafe RecordNumber)

Gaps 765-769 capture additional verified details from:

- `SAClassLibrary/Migrations/202204181731424_ChangePayrollRecord_AddRatePercentage.cs`
- `SAClassLibrary/Migrations/202204181731424_ChangePayrollRecord_AddRatePercentage.Designer.cs`
- `SAClassLibrary/Migrations/202208221414114_ChangeBeSafeRecord_AddRecordNumber.cs`
- `SAClassLibrary/Migrations/202208221414114_ChangeBeSafeRecord_AddRecordNumber.Designer.cs`

---

## GAP 765: `ChangePayrollRecord_AddRatePercentage` Migration Is No-Op Marker

Migration currently contains empty `Up()`/`Down()` methods, indicating schema-version checkpoint without direct DDL in file.

---

## GAP 766: `ChangeBeSafeRecord_AddRecordNumber` Adds Required Integer Record Number to BeSafeRecords

`Up()` adds non-null `RecordNumber` column to `dbo.BeSafeRecords`.

---

## GAP 767: `ChangeBeSafeRecord_AddRecordNumber` Down Path Drops Added Column

`Down()` removes `RecordNumber` from `dbo.BeSafeRecords`.

---

## GAP 768: Reviewed Designer Metadata Uses EF Migration Generator Version `6.4.4`

Designer files declare `GeneratedCode("EntityFramework.Migrations", "6.4.4")` metadata marker.

---

## GAP 769: Timestamped IDs Continue Deterministic Migration Ordering Convention

Designer `Id` values match timestamped migration names (`202204...`, `202208...`), supporting deterministic EF6 migration sequencing.

# Part 168: Gap Analysis - SAClassLibrary Identity/Address/ADP Model Notes

Gaps 770-774 capture additional verified details from:

- `SAClassLibrary/Models/ADPInterface.cs`
- `SAClassLibrary/Models/Address.cs`
- `SAClassLibrary/Models/AspNetUser.cs`
- `SAClassLibrary/Models/AspNetRole.cs`

---

## GAP 770: SAClassLibrary `ADPInterface` Uses Key-Only Payroll Link + Mutable `ADPCode`

Class keys by `PayrollCodeControlNumber`, stores `ColumnNumber`, and exposes mutable `ADPCode` property (no private-set restriction in this model).

---

## GAP 771: SAClassLibrary `Address` Is Direct POCO with Explicit Control/Audit Fields

Address model defines direct `ControlNumber` key and create/modify audit properties in-class (non-base-class shape).

---

## GAP 772: SAClassLibrary `AspNetUser` Constructor Eager-Initializes Identity/Domain Navigation Sets

Constructor initializes user claims/logins, linked employees, login records, and roles as `HashSet<>` collections.

---

## GAP 773: SAClassLibrary `AspNetUser` Includes Multiple NotMapped Name Formatting Variants

Model provides formatted helper outputs for employee-number/full-name and lastname-first variants via `[NotMapped]` computed properties.

---

## GAP 774: SAClassLibrary `AspNetRole` Is Minimal Id/Name + Users Collection Model

Role model includes key `Id`, required `Name`, and many-to-many navigation to users with constructor-based collection initialization.

# Part 169: Gap Analysis - SAClassLibrary Identity Join Models + Assignment Baseline Notes

Gaps 775-779 capture additional verified details from:

- `SAClassLibrary/Models/AspNetUserClaim.cs`
- `SAClassLibrary/Models/AspNetUserLogin.cs`
- `SAClassLibrary/Models/Assignment.cs`
- `SAClassLibrary/Models/AssignmentAbolishment.cs`

---

## GAP 775: SAClassLibrary `AspNetUserClaim` Uses Integer Identity Key + Required UserId Link

Claim rows key on integer `Id` and require `UserId` (string length 128) with optional claim type/value strings.

---

## GAP 776: SAClassLibrary `AspNetUserLogin` Uses Three-Part Composite Key

Login rows are keyed by (`LoginProvider`, `ProviderKey`, `UserId`) composite key pattern.

---

## GAP 777: SAClassLibrary `Assignment` Constructor Eager-Initializes Related Collections

Constructor initializes `AssignmentOnDutyDays`, `DailyAssignments`, and `TemporaryAssignments` as `HashSet<>` collections.

---

## GAP 778: SAClassLibrary `Assignment` Includes Required `WorkArea` Field

Assignment model requires `WorkArea` (`string(50)`), alongside core board/order/type/location linkage fields.

---

## GAP 779: SAClassLibrary `AssignmentAbolishment` Uses AssignmentControlNumber as Key

Abolishment row is keyed by `AssignmentControlNumber` (non-generated) and stores create attribution with abolishment datetime.

# Part 170: Gap Analysis - SAClassLibrary Assignment/OnDuty/Location Baseline Notes

Gaps 780-784 capture additional verified details from:

- `SAClassLibrary/Models/AssignmentOnDutyDay.cs`
- `SAClassLibrary/Models/AssignmentOnDutyTime.cs`
- `SAClassLibrary/Models/AssignmentType.cs`
- `SAClassLibrary/Models/Location.cs`

---

## GAP 780: SAClassLibrary AssignmentOnDutyDay Defaults StraightTimeHours to 8

Constructor sets `StraightTimeHours = 8`, matching baseline default in assignment-day modeling.

---

## GAP 781: SAClassLibrary AssignmentOnDutyTime Eager-Initializes Related Collections

Constructor initializes assignment-day/assignment/cutoff/temporary-assignment collections as `HashSet<>`.

---

## GAP 782: SAClassLibrary AssignmentType Includes AirPay/ExtraBoardOnly/TypeNumber Flags

Assignment type model stores classification metadata (`AirPay`, `ExtraBoardOnly`, `AssignmentTypeNumber`) alongside naming/audit fields.

---

## GAP 783: SAClassLibrary Location Includes Decimal BoardOrder + Optional Short Name

Location model uses `decimal BoardOrder` and optional `LocationShortName` (`string(4)`) in addition to required location name.

---

## GAP 784: All Reviewed SAClassLibrary POCOs in This Set Use Explicit Control/Audit Columns (Non-base-Class Shape)

These specific classes define key/audit properties directly in-file and initialize related collections where appropriate.

# Part 171: Gap Analysis - SAClassLibrary Crew/CrewPosition Baseline Notes

Gaps 785-789 capture additional verified details from:

- `SAClassLibrary/Models/Crew.cs`
- `SAClassLibrary/Models/CrewAssignment.cs`
- `SAClassLibrary/Models/CrewOffDay.cs`
- `SAClassLibrary/Models/CrewPosition.cs`

---

## GAP 785: SAClassLibrary `Crew` Constructor Defaults `EffectiveDate` to Today

Crew baseline constructor initializes `EffectiveDate = DateTime.Today`.

---

## GAP 786: SAClassLibrary `Crew` Includes Name-Derived CrewID/CrewIDName Formatting Rules

Computed identifiers derive special values for names containing `Relief` or `XB`, otherwise returning crew name directly.

---

## GAP 787: SAClassLibrary `CrewAssignment` Uses Assignment-Day Control Number as Key

`CrewAssignment` key is `AssignmentOnDutyDayControlNumber` (non-generated), with create attribution fields included in class.

---

## GAP 788: SAClassLibrary `CrewOffDay` Uses Composite Key (Crew + WeekDay)

`CrewOffDay` key is (`CrewControlNumber`, `WeekDayControlNumber`) with both columns `DatabaseGenerated(None)`.

---

## GAP 789: SAClassLibrary `CrewPosition` Defaults `ExtraBoardOnly` to True in Constructor

Constructor sets `ExtraBoardOnly = true`; full-factory overload can override with explicit value when creating fully-populated records.

# Part 172: Gap Analysis - SAClassLibrary Craft Family Baseline Notes

Gaps 790-794 capture additional verified details from:

- `SAClassLibrary/Models/Craft.cs`
- `SAClassLibrary/Models/CraftApprovalOfficer.cs`
- `SAClassLibrary/Models/CraftMarkOffAllowance.cs`
- `SAClassLibrary/Models/CraftMarkOffCode.cs`

---

## GAP 790: SAClassLibrary `Craft` Constructor Eager-Initializes Broad Navigation Set

Constructor initializes numerous related collections (approval officers, allowances, markoff codes, day tables, requests, cutoff times, payroll records, rosters, requirements).

---

## GAP 791: SAClassLibrary Craft Family Uses Explicit Control/Audit Column Pattern (Non-base-Class Shape)

Reviewed craft-family models declare `ControlNumber` and audit columns directly in POCO class definitions.

---

## GAP 792: SAClassLibrary `Craft` Uses Singular `CraftPayCode` Navigation Naming

Craft-to-pay-code relation is modeled as `CraftPayCode` in SAClassLibrary POCO shape.

---

## GAP 793: SAClassLibrary `CraftMarkOffAllowance` Persists Total/Calculated/Allowed Values as Direct Columns

Allowance model stores `TotalNumber`, `CalculatedNumber`, and `NumberAllowed` directly in row payload.

---

## GAP 794: SAClassLibrary `CraftMarkOffCode` Stores Approval/Exclude/AutomaticMarkup Override Columns

Craft-markoff link model includes explicit override behavior columns (`ApprovalRequired`, `Exclude`, `AutomaticMarkUpHours`).

# Part 173: Gap Analysis - SAClassLibrary Craft Day-Tier and PayCode Notes

Gaps 795-799 capture additional verified details from:

- `SAClassLibrary/Models/CraftPersonalDay.cs`
- `SAClassLibrary/Models/CraftSickDay.cs`
- `SAClassLibrary/Models/CraftVacationDay.cs`
- `SAClassLibrary/Models/CraftPayCode.cs`

---

## GAP 795: SAClassLibrary Craft Day-Tier Entities Use Explicit ControlNumber Keys

`CraftPersonalDay`, `CraftSickDay`, and `CraftVacationDay` each key on direct `ControlNumber` with non-generated value.

---

## GAP 796: SAClassLibrary Craft Day-Tier Rows Persist Direct Service-Year Threshold + Day Values

Each day-tier entity stores `ServiceYears` plus day-count column (`PersonalDays`, `SickDays`, `VacationDays`) with direct audit columns.

---

## GAP 797: SAClassLibrary Craft Day-Tier Models Use Singular Naming (`...Day`) vs Web Plural (`...Days`)

Cross-project model naming differs between SAClassLibrary (`CraftPersonalDay`, etc.) and web-model entity names (`CraftPersonalDays`, etc.).

---

## GAP 798: SAClassLibrary CraftPayCode Uses CraftControlNumber as FK-as-PK One-to-One Key

Craft pay-code row is keyed directly by `CraftControlNumber`, representing one pay-code bundle per craft.

---

## GAP 799: SAClassLibrary CraftPayCode Requires Seven Distinct 4-Character Payroll Code Fields

Model enforces required string(4) fields for paid/vacation/personal worked+paid mappings plus guarantee paid code.

# Part 174: Gap Analysis - SAClassLibrary DailyAssignment Family Baseline Notes

Gaps 800-804 capture additional verified details from:

- `SAClassLibrary/Models/DailyAssignment.cs`
- `SAClassLibrary/Models/DailyAssignmentCrew.cs`
- `SAClassLibrary/Models/DailyAssignmentShift.cs`
- `SAClassLibrary/Models/DailyAssignmentShiftCompletion.cs`

---

## GAP 800: SAClassLibrary DailyAssignment Constructor Defaults `Notes` to Empty String

Base constructor initializes `Notes = string.Empty`.

---

## GAP 801: SAClassLibrary DailyAssignment IsAnnuled Flag Is Navigation-Presence Check

`IsAnnuled` returns true when `DailyAssignmentAnnulment` navigation exists.

---

## GAP 802: SAClassLibrary DailyAssignmentCrew Uses DailyAssignmentControlNumber as FK-as-PK Key

`DailyAssignmentCrew` keys directly on `DailyAssignmentControlNumber` and stores crew link as required payload.

---

## GAP 803: SAClassLibrary DailyAssignmentShift Computed Time Boundaries Open New Context Per Getter

First/last on-duty/calling computed properties each create `SAClassLibraryContext` to resolve shift/on-duty-time values when needed.

---

## GAP 804: SAClassLibrary DailyAssignmentShiftCompletion Uses Shift FK-as-PK Completion Marker Pattern

Completion row keys on `DailyAssignmentShiftControlNumber` and stores completion/create attribution fields.

# Part 175: Gap Analysis - SAClassLibrary DailyCrewPosition Family Baseline Notes

Gaps 805-809 capture additional verified details from:

- `SAClassLibrary/Models/DailyCrewPosition.cs`
- `SAClassLibrary/Models/DailyCrewPositionOnDutyRecord.cs`
- `SAClassLibrary/Models/DailyCrewPositionOffDutyRecord.cs`
- `SAClassLibrary/Models/DailyCrewPositionVacancy.cs`

---

## GAP 805: SAClassLibrary DailyCrewPosition Uses Presence-Based Annul/DoNotFill/Skip Flags

`IsAnnuled`, `DoNotFill`, and `IsSkipped` are computed from corresponding navigation-marker presence.

---

## GAP 806: SAClassLibrary DailyCrewPosition JobCode Logic Branches by Pool Number

`JobCode` formatting changes for pool 30/60, pool 50, and default branch, using assignment/position components differently.

---

## GAP 807: SAClassLibrary DailyCrewPositionOnDutyRecord Defaults ConsecutiveDays to 1

On-duty-record constructor initializes `ConsecutiveDays = 1`.

---

## GAP 808: SAClassLibrary DailyCrewPositionOnDutyRecord Uses Presence-Based State Flags for MarkedOff/Unavailable/LateCall/TiedUp

Multiple operational states are computed through related navigation-record existence and annul/do-not-fill checks.

---

## GAP 809: SAClassLibrary DailyCrewPositionVacancy Uses Composite Key (Position + VacancyNumber) with Factory Overloads

Vacancy rows key on (`DailyCrewPositionControlNumber`, `VacancyNumber`) and provide overloads for default (`nbr=0`, `xb=false`) or explicit vacancy number/extra-board setting.

# Part 176: Gap Analysis - SAClassLibrary Daily On-Duty Billing/Material Baseline Notes

Gaps 810-814 capture additional verified details from:

- `SAClassLibrary/Models/DailyOnDutyAFEBillingRecord.cs`
- `SAClassLibrary/Models/DailyOnDutyMiscellaneousBillingRecord.cs`
- `SAClassLibrary/Models/DailyOnDutyZoneBillingRecord.cs`
- `SAClassLibrary/Models/DailyOnDutyRailroadMaterialRecord.cs`

---

## GAP 810: SAClassLibrary DailyOnDutyAFEBillingRecord Inherits ControlNumberBase and Uses Record-Link Factory

Factory `CreateInstance(long record)` seeds `DailyCrewPositionOnDutyRecordControlNumber`; AFE fields/hours are caller-populated.

---

## GAP 811: Miscellaneous/Zone/Material On-Duty Records Use Explicit ControlNumber + Audit Column Pattern

These SAClassLibrary POCOs define direct `ControlNumber` keys and explicit create/modify audit fields in-class.

---

## GAP 812: SAClassLibrary Billing Record Models Store ST/OT Billing Hours as TimeSpan Fields

Reviewed billing-related entities include `STBHours` and `OTBHours` columns for hourly split capture.

---

## GAP 813: SAClassLibrary DailyOnDutyMiscellaneousBillingRecord Carries WorkCode + Location Linkage and Free-Form Notes

Model combines work-code/location linkage with billable flag, notes, and billing-hour values.

---

## GAP 814: SAClassLibrary DailyOnDutyRailroadMaterialRecord Captures Material Snapshot Fields Inline

Material record stores category/type/code/description/unit/quantity fields directly as row payload tied to on-duty record.

# Part 177: Gap Analysis - SAClassLibrary Daily On-Duty Status/Payroll Marker Notes

Gaps 815-819 capture additional verified details from:

- `SAClassLibrary/Models/DailyOnDutyMarkOffRecord.cs`
- `SAClassLibrary/Models/DailyOnDutyUnavailableRecord.cs`
- `SAClassLibrary/Models/DailyOnDutyDidNotWorkRecord.cs`
- `SAClassLibrary/Models/DailyOnDutyPayrollInformation.cs`

---

## GAP 815: SAClassLibrary DailyOnDutyMarkOffRecord Uses OnDuty FK-as-PK Shape with Default `Ignore=false`

Key is `DailyCrewPositionOnDutyRecordControlNumber`; constructor defaults `Ignore` to false.

---

## GAP 816: SAClassLibrary DailyOnDutyMarkOffRecord Computes Compensation Flag from Related MarkOffCode

`IsCompensated` returns `MarkOffCode.IsCompensated` via related code entity.

---

## GAP 817: SAClassLibrary DailyOnDutyUnavailableRecord Uses OnDuty Record Control Number as Key

Unavailable marker row is keyed by `DailyCrewPositionOnDutyRecordControlNumber` and carries markoff-code snapshot (`MOCode`) plus optional linkage IDs.

---

## GAP 818: SAClassLibrary DailyOnDutyDidNotWorkRecord Uses OnDuty FK-as-PK Marker Pattern

Did-not-work marker row keys on `DailyCrewPositionOnDutyRecordControlNumber` and stores creating user/date with optional employee link.

---

## GAP 819: SAClassLibrary DailyOnDutyPayrollInformation Uses OnDuty FK-as-PK Payload Model

Payroll-info row keys on `DailyCrewPositionOnDutyRecordControlNumber` and stores meal/air/training claim and approval fields inline.

# Part 178: Gap Analysis - SAClassLibrary Daily Railroad Employee/Position Baseline Notes

Gaps 820-824 capture additional verified details from:

- `SAClassLibrary/Models/DailyRailroadEmployeePositionRecord.cs`
- `SAClassLibrary/Models/DailyRailroadEmployeePositionMarkOffRecord.cs`
- `SAClassLibrary/Models/DailyRailroadEmployeeStatusRecord.cs`
- `SAClassLibrary/Models/DailyRailroadPositionOffDayRecord.cs`

---

## GAP 820: SAClassLibrary DailyRailroadEmployeePositionRecord Computes Hangout Flags from Navigation Presence

`IsHangout` and `IsNotifiedHangout` are derived from related hangout record existence and markoff-code excused flag.

---

## GAP 821: SAClassLibrary DailyRailroadEmployeePositionMarkOffRecord Inherits ControlNumberBase with Position-Link Factory

Factory `CreateInstance(long position)` seeds `DailyRailroadEmployeePositionRecordControlNumber`; markoff payload fields are caller-populated.

---

## GAP 822: SAClassLibrary DailyRailroadEmployeeStatusRecord Factory Seeds Employee + RailroadEmployee Links

`CreateInstance(RailroadEmployee rremployee)` copies both `RailroadEmployeeControlNumber` and parent `EmployeeControlNumber` from supplied railroad-employee entity.

---

## GAP 823: SAClassLibrary DailyRailroadPositionOffDayRecord Uses Composite Key (RailroadPosition + AssignmentDate)

Off-day record keys on (`RailroadPositionControlNumber`, `AssignmentDate`) and stores position-name snapshot/audit fields.

---

## GAP 824: SAClassLibrary DailyRailroadPositionOffDayRecord Constructor Eager-Initializes OffDayEmployee Collection

Constructor initializes `DailyRailroadPositionOffDayEmployeeRecords` as `HashSet<>`.

# Part 179: Gap Analysis - SAClassLibrary Daily Shift Board Baseline Notes

Gaps 825-829 capture additional verified details from:

- `SAClassLibrary/Models/DailyShiftExtraBoard.cs`
- `SAClassLibrary/Models/DailyShiftExtraBoardPosition.cs`
- `SAClassLibrary/Models/DailyShiftOvertimeBoard.cs`
- `SAClassLibrary/Models/DailyShiftOvertimeBoardPosition.cs`

---

## GAP 825: SAClassLibrary DailyShiftExtraBoard Uses Shift+RosterBoard FK Links with Required Percent/Capacity Fields

Model stores required completion/capacity/percentage values (`Completed`, `AverageVacancies`, `RequiredPositions`, `ExtraBoardPercentage`).

---

## GAP 826: SAClassLibrary DailyShiftExtraBoard Computes Board Type from `RosterBoard.ExtraBoard`

`IsRotatingBoard` and `IsFirstInFirstOutBoard` derive from extra-board mode values (`2` and `1` respectively).

---

## GAP 827: SAClassLibrary DailyShiftExtraBoardPosition Uses Presence-Based Assignment Flag

`IsAssigned` is computed via `DailyShiftExtraBoardPositionAssignment != null`.

---

## GAP 828: SAClassLibrary DailyShiftOvertimeBoard Uses Explicit ControlNumber + RotatingBoard Flag

Overtime-board model stores direct key/audit fields plus completion/rotation state and related board-position collection.

---

## GAP 829: SAClassLibrary DailyShiftOvertimeBoardPosition Includes Required Two-Character Position-Type Code

Overtime-board position row requires `PostionType` (string length 2) and tracks board ordering and board datetime.

# Part 180: Gap Analysis - SAClassLibrary Earnings Approval Family Baseline Notes

Gaps 830-834 capture additional verified details from:

- `SAClassLibrary/Models/EarningsApprovalRequiredRecord.cs`
- `SAClassLibrary/Models/EarningsApprovalEmployee.cs`
- `SAClassLibrary/Models/EarningsApprovalRecord.cs`
- `SAClassLibrary/Models/EarningsDeclanationRecord.cs`

---

## GAP 830: SAClassLibrary EarningsApprovalRequiredRecord Uses PayrollEarningRecordControlNumber as Key

Required-approval row is keyed by earning-record control number and stores approval employee link plus audit columns.

---

## GAP 831: SAClassLibrary EarningsApprovalEmployee Uses Composite Key Bridge (Earning + Employee)

Bridge key is (`PayrollEarningRecordControlNumber`, `ApprovalEmployeeControlNumber`) with factory seeding earning link.

---

## GAP 832: SAClassLibrary EarningsApprovalRecord Uses FK-as-PK One-to-One Pattern

Approval row keys on `PayrollEarningRecordControlNumber` and links to required-approval parent.

---

## GAP 833: SAClassLibrary EarningsDeclanationRecord Uses FK-as-PK One-to-One Pattern with Required Notes

Declination row keys on `PayrollEarningRecordControlNumber` and requires `Notes` plus full audit fields.

---

## GAP 834: SAClassLibrary Approval/Declination Rows Are Modeled as Separate Entities

Approval completion and declination outcomes are represented with distinct one-to-one child tables rather than a single status column.

# Part 181: Gap Analysis - SAClassLibrary Employee/Employment Baseline Notes

Gaps 835-839 capture additional verified details from:

- `SAClassLibrary/Models/Employee.cs`
- `SAClassLibrary/Models/EmploymentStatu.cs`
- `SAClassLibrary/Models/EmployeePriorServiceCredit.cs`
- `SAClassLibrary/Models/EmploymentStatusHistory.cs`

---

## GAP 835: SAClassLibrary `Employee` Constructor Applies Same Core Default Domain Values as Web Model

Defaults include birthdate baseline, `EmploymentDate=today`, `IssuingState=TX`, and standard payroll/overtime/tie-up boolean defaults.

---

## GAP 836: SAClassLibrary Employee Role/Approval Computed Properties Are Partially Commented-Out

Several optional computed behaviors (approval count/role checks and others) are present but commented out in this class snapshot.

---

## GAP 837: SAClassLibrary Employment Status Source File Uses Singular Filename Typo Pattern

Employment status model is stored in `EmploymentStatu.cs` while class name remains `EmploymentStatus`.

---

## GAP 838: SAClassLibrary EmploymentStatus Constructor Eager-Initializes Related Collections

Constructor initializes daily-status records, employees, and status-history collections as `HashSet<>`.

---

## GAP 839: SAClassLibrary EmployeePriorServiceCredit and EmploymentStatusHistory Use Explicit Key/Audit POCO Pattern

Both models define key and audit payload columns directly in-file (non-base-class shape), with direct navigation links to employee/employment status entities.

# Part 182: Gap Analysis - SAClassLibrary Client/Description/Contact Baseline Notes

Gaps 840-844 capture additional verified details from:

- `SAClassLibrary/Models/Client.cs`
- `SAClassLibrary/Models/Description.cs`
- `SAClassLibrary/Models/EmailAddress.cs`
- `SAClassLibrary/Models/PhoneNumber.cs`

---

## GAP 840: SAClassLibrary Client Constructor Eager-Initializes Broad Domain Collections

Client model constructor initializes requirements, descriptions, employees, statuses, holidays, markoff/payroll sets, railroads, and weekdays collections.

---

## GAP 841: SAClassLibrary Description Constructor Initializes Address/Email/Phone Collections

Description model prepares all three contact-type collections as `HashSet<>` at construction.

---

## GAP 842: SAClassLibrary Contact Models Use Explicit ControlNumber + Audit POCO Pattern

`EmailAddress` and `PhoneNumber` define direct keys and create/modify audit fields in-class (non-base-class shape).

---

## GAP 843: SAClassLibrary Description `Code` Is Required Two-Character Field

Description code is required with string length constrained to 2 characters.

---

## GAP 844: SAClassLibrary PhoneNumber Persists Dialing Metadata Inline

Phone model stores both `CallingOrder` and `DialOne` directly, along with number text and contact-type/employee links.

# Part 183: Gap Analysis - SAClassLibrary Holiday/HoldDown Baseline Notes

Gaps 845-849 capture additional verified details from:

- `SAClassLibrary/Models/Holiday.cs`
- `SAClassLibrary/Models/HolidayQualifyRecord.cs`
- `SAClassLibrary/Models/HoldDown.cs`
- `SAClassLibrary/Models/HoldDownRelease.cs`

---

## GAP 845: SAClassLibrary Holiday Constructor Eager-Initializes PayrollHolidayRecord Collection

Holiday model constructor initializes `PayrollHolidayRecords` as `HashSet<>`.

---

## GAP 846: SAClassLibrary HolidayQualifyRecord Uses Explicit ControlNumber Key and Full Audit Columns

Qualification rows key by direct `ControlNumber` and include create/modify audit fields in-class.

---

## GAP 847: SAClassLibrary HoldDown Open/Closed State Is Release-Record Driven

`IsClosed` and `IsOpen` are derived from release-record presence and released-date comparison.

---

## GAP 848: SAClassLibrary HoldDownRelease Uses FK-as-PK One-to-One Pattern

`HoldDownRelease` keys on `HoldDownControlNumber` with `[Key, ForeignKey("HoldDown"), DatabaseGenerated(None)]`.

---

## GAP 849: SAClassLibrary HoldDownRelease Provides Overloads for Existing HoldDown or Direct ControlNumber Paths

Factory overloads support creation from `HoldDown` object or direct hold-down control number with release datetime and user attribution.

# Part 184: Gap Analysis - SAClassLibrary MarkOff Family Baseline Notes

Gaps 850-854 capture additional verified details from:

- `SAClassLibrary/Models/MarkOffCode.cs`
- `SAClassLibrary/Models/MarkOffRecord.cs`
- `SAClassLibrary/Models/MarkOffRequestRecord.cs`
- `SAClassLibrary/Models/MarkUpRecord.cs`

---

## GAP 850: SAClassLibrary MarkOffCode Constructor Defaults `ReportColor` to `Black`

`MarkOffCode(long client)` initializes `ReportColor = "Black"` for new code creation path.

---

## GAP 851: SAClassLibrary MarkOffCode AutoMarkUpHours Uses Built-In Code-to-Hour Mapping Fallback

When explicit `MarkOffMarkUpHours` row is absent, `AutoMarkUpHours` returns hard-coded hour values for vacation/day-off code sets.

---

## GAP 852: SAClassLibrary MarkOffRecord Open/Closed State Includes Delete and Future-Date Guards

`IsOpen` returns false for deleted records and for markoff datetimes in the future, otherwise relies on closed-state computation.

---

## GAP 853: SAClassLibrary MarkOffRequestRecord Constructor Eager-Initializes Temp/WaitList Collections

`MarkOffRequestRecord` constructor initializes `MarkOffRequestTempRecords` and `MarkOffRequestWaitListRecords` as `HashSet<>`.

---

## GAP 854: SAClassLibrary MarkUpRecord Uses MarkOffRecordControlNumber as One-to-One Key

Markup record is keyed by `MarkOffRecordControlNumber` with mark-up datetime and create/modify audit payload fields.

# Part 185: Gap Analysis - SAClassLibrary Payroll Code/Category/ReportGroup Baseline Notes

Gaps 855-859 capture additional verified details from:

- `SAClassLibrary/Models/PayrollCode.cs`
- `SAClassLibrary/Models/PayrollCategory.cs`
- `SAClassLibrary/Models/PayrollReportGroup.cs`
- `SAClassLibrary/Models/PayrollCodeApprovalRole.cs`

---

## GAP 855: SAClassLibrary PayrollCode Constructor Eager-Initializes Multiple Related Collections

Constructor initializes daily on-duty links, markoff-payroll links, approval-role links, pay-rate links, autopay links, earning links, and category links.

---

## GAP 856: SAClassLibrary PayrollCode Includes ADP Single Navigation + UKG Collection Navigation

Model exposes `ADPInterface` (single) and `UKGInterfaces` (collection) relationship surfaces.

---

## GAP 857: SAClassLibrary PayrollCategory Constructor Initializes PayrollCodes and PayrollReportGroups Collections

Category model constructor prepares many-to-many relationship sets with payroll codes and report groups.

---

## GAP 858: SAClassLibrary PayrollReportGroup Constructor Initializes PayrollCategories Collection

Report group model constructor eagerly initializes category linkage collection for grouping relationships.

---

## GAP 859: SAClassLibrary PayrollCodeApprovalRole Uses Explicit ControlNumber Key + Role Guid/Name Pairing

Approval-role row stores direct key/audit columns with both `RoleId` (`Guid`) and `RoleName` fields plus `Primary` flag.

# Part 186: Gap Analysis - SAClassLibrary BeSafe Record/Action/Change/Resolve Notes

Gaps 860-864 capture additional verified details from:

- `SAClassLibrary/Models/BeSafeRecord.cs`
- `SAClassLibrary/Models/BeSafeActionRecord.cs`
- `SAClassLibrary/Models/BeSafeChangeRecord.cs`
- `SAClassLibrary/Models/BeSafeResolveRecord.cs`

---

## GAP 860: SAClassLibrary BeSafeRecord Factory Seeds Railroad Link Only

`BeSafeRecord.CreateInstance(long railroad)` initializes `RailroadControlNumber`; other required fields are caller-populated.

---

## GAP 861: SAClassLibrary BeSafeRecord Includes Required `RecordNumber` Field

BeSafe core record model requires explicit integer `RecordNumber` as part of record payload.

---

## GAP 862: SAClassLibrary BeSafeActionRecord and BeSafeChangeRecord Inherit ControlNumberBase with Parent-Link Factories

Both entities use constructor/factory paths that seed `BeSafeRecordControlNumber` and rely on callers for title/description/action date payload fields.

---

## GAP 863: SAClassLibrary BeSafeResolveRecord Uses FK-as-PK One-to-One Pattern

Resolve record keys on `BeSafeRecordControlNumber` with `[Key, ForeignKey("BeSafeRecord"), DatabaseGenerated(None)]`.

---

## GAP 864: SAClassLibrary BeSafeResolveRecord Factory Uses Parameter Name `sloworder` While Targeting BeSafeRecord Key

Constructor/factory parameter naming (`sloworder`) does not match BeSafe domain naming but maps to `BeSafeRecordControlNumber` key assignment.

# Part 187: Gap Analysis - SAClassLibrary BeSafe Area/Category/Delete/EmailGroup Notes

Gaps 865-869 capture additional verified details from:

- `SAClassLibrary/Models/BeSafeArea.cs`
- `SAClassLibrary/Models/BeSafeCategory.cs`
- `SAClassLibrary/Models/BeSafeDeleteRecord.cs`
- `SAClassLibrary/Models/BeSafeEmailGroup.cs`

---

## GAP 865: SAClassLibrary BeSafeArea/BeSafeCategory/BeSafeEmailGroup Factories Seed Railroad Link Only

`CreateInstance(long railroad)` in these entities initializes `RailroadControlNumber`; remaining required fields are caller-populated.

---

## GAP 866: SAClassLibrary BeSafeArea and BeSafeCategory Include Required Numeric Ordering Fields

`BeSafeAreaNumber` and `BeSafeCategoryNumber` are required integer sequence identifiers in corresponding models.

---

## GAP 867: SAClassLibrary BeSafeCategory Requires Email Group Link

Category model includes required `BeSafeEmailGroupControlNumber`, tying category classification to notification group configuration.

---

## GAP 868: SAClassLibrary BeSafeDeleteRecord Uses FK-as-PK One-to-One Delete Marker Pattern

Delete marker keys on `BeSafeRecordControlNumber` with `[Key, ForeignKey("BeSafeRecord"), DatabaseGenerated(None)]`.

---

## GAP 869: SAClassLibrary BeSafeEmailGroup Stores Name + Address as Required Payload

Email-group model requires both `BeSafeEmailGroupName` and `BeSafeEmailGroupAddress` fields in entity payload.

# Part 188: Gap Analysis - SAClassLibrary BeSafe Subdivision / Change / ClientRequirement Notes

Gaps 870-874 capture additional verified details from:

- `SAClassLibrary/Models/BeSafeSubdivision.cs`
- `SAClassLibrary/Models/ChangeMoveOrBulletin.cs`
- `SAClassLibrary/Models/ChangeNotification.cs`
- `SAClassLibrary/Models/ClientRequirement.cs`

---

## GAP 870: SAClassLibrary BeSafeSubdivision Factory Seeds Railroad Link Only

`CreateInstance(long railroad)` initializes `RailroadControlNumber`; required subdivision number/name remain caller-populated.

---

## GAP 871: SAClassLibrary ChangeMoveOrBulletin Uses Composite Key Bridge Shape

Bridge key is (`RailroadPositionChangeControlNumber`, `MoveOrBulletinControlNumber`) with both columns `DatabaseGenerated(None)`.

---

## GAP 872: SAClassLibrary Change Models Use `RailroadPositionChanx` Navigation Naming

Both `ChangeMoveOrBulletin` and `ChangeNotification` expose navigation property named `RailroadPositionChanx`.

---

## GAP 873: SAClassLibrary ChangeNotification Uses Explicit ControlNumber Key and Inline Audit Columns

Notification rows store direct key, notify details, confirmation/notes, and create/modify audit fields.

---

## GAP 874: SAClassLibrary ClientRequirement Uses Composite Key (ClientControlNumber + RequirementControlNumber)

Client requirement model keys on client/requirement pair and constructor initializes linked `ClientRequirementEmployees` collection.

# Part 189: Gap Analysis - SAClassLibrary Requirement/Crew-Abolishment/Alt-Position Notes

Gaps 875-879 capture additional verified details from:

- `SAClassLibrary/Models/ClientRequirementEmployee.cs`
- `SAClassLibrary/Models/CraftRequirementEmployee.cs`
- `SAClassLibrary/Models/CrewAbolishment.cs`
- `SAClassLibrary/Models/CrewPositionAlternatePosition.cs`

---

## GAP 875: SAClassLibrary ClientRequirementEmployee Uses Explicit ControlNumber Key with Composite Link Payload

Model stores client/requirement/employee links plus completion datetime and full audit fields under direct `ControlNumber` identity.

---

## GAP 876: SAClassLibrary CraftRequirementEmployee Uses Explicit ControlNumber Key and RailroadPoolEmployee Link

Model captures craft/requirement/railroad-pool-employee completion with direct key/audit payload.

---

## GAP 877: SAClassLibrary CrewAbolishment Uses CrewControlNumber as One-to-One Key

Crew abolishment row keys directly by `CrewControlNumber` and stores abolishment datetime with create attribution.

---

## GAP 878: SAClassLibrary CrewPositionAlternatePosition Uses Composite Key (RailroadPosition + WeekDay)

Alternate-position mapping row keys on railroad-position + weekday pair and stores alternate position plus audit fields.

---

## GAP 879: Reviewed Requirement/Crew Mapping Models Use Explicit In-Class Audit Columns (Non-base-Class Pattern)

All four reviewed entities define create/modify attribution columns directly rather than inheriting base audit members.

# Part 190: Gap Analysis - SAClassLibrary Daily Assignment/History Marker Notes

Gaps 880-884 capture additional verified details from:

- `SAClassLibrary/Models/DailyAssignmentAFERecord.cs`
- `SAClassLibrary/Models/DailyAssignmentAnnulment.cs`
- `SAClassLibrary/Models/DailyAssignmentRequest.cs`
- `SAClassLibrary/Models/DailyCrewHistory.cs`

---

## GAP 880: SAClassLibrary DailyAssignmentAFERecord Uses DailyAssignment FK-as-PK One-to-One Pattern

AFE record keys on `DailyAssignmentControlNumber` and stores AFE number/description with create/modify audit fields.

---

## GAP 881: SAClassLibrary DailyAssignmentAnnulment Uses DailyAssignment FK-as-PK One-to-One Pattern

Annulment row keys on `DailyAssignmentControlNumber` and stores annulment datetime with create attribution.

---

## GAP 882: SAClassLibrary DailyAssignmentRequest Uses DailyAssignment FK-as-PK One-to-One Pattern

Request row keys on `DailyAssignmentControlNumber`, links optional requesting employee, and stores required request notes.

---

## GAP 883: SAClassLibrary DailyCrewHistory Constructor Eager-Initializes Position History Collection

`DailyCrewHistory` constructor initializes `DailyCrewPositionHistories` as `HashSet<>`.

---

## GAP 884: SAClassLibrary DailyCrewHistory Stores Snapshot-Style Shift/Crew/Pool Fields Inline

History model persists shift IDs/names, crew and pool identifying data, off-days text, and on-duty time as direct payload columns.

# Part 191: Gap Analysis - SAClassLibrary DailyCrewPosition Marker/Electronic-Call Notes

Gaps 885-889 capture additional verified details from:

- `SAClassLibrary/Models/DailyCrewPositionAnnulment.cs`
- `SAClassLibrary/Models/DailyCrewPositionDoNotFill.cs`
- `SAClassLibrary/Models/DailyCrewPositionElectronicCallRecord.cs`
- `SAClassLibrary/Models/DailyCrewPositionElectronicResponseRecord.cs`

---

## GAP 885: SAClassLibrary DailyCrewPositionAnnulment Uses DailyCrewPosition FK-as-PK One-to-One Marker Pattern

Annulment row keys on `DailyCrewPositionControlNumber` and stores annulment datetime plus create attribution.

---

## GAP 886: SAClassLibrary DailyCrewPositionDoNotFill Uses DailyCrewPosition FK-as-PK One-to-One Marker Pattern

Do-not-fill row keys on `DailyCrewPositionControlNumber` and stores do-not-fill datetime plus create attribution.

---

## GAP 887: SAClassLibrary DailyCrewPositionElectronicCallRecord Inherits ControlNumberBase with Position-Link Factory

Factory `CreateInstance(long dcposition)` seeds daily-crew-position link; call payload fields are caller-populated.

---

## GAP 888: SAClassLibrary Electronic Call Record Stores Alert/Employee/Job Snapshot Fields Inline

Call record requires `AlertUniqueIdentifier`, `EmployeeNumber`, `JobCode`, vacancy number, and `SendRequest` state.

---

## GAP 889: SAClassLibrary DailyCrewPositionElectronicResponseRecord Uses CallRecord FK-as-PK One-to-One Pattern

Response row keys on `DailyCrewPositionElectronicCallRecordControlNumber` and stores response ID/text with create attribution.

# Part 192: Gap Analysis - SAClassLibrary DailyCrewPosition History/FRA/MarkOff/Payroll Link Notes

Gaps 890-894 capture additional verified details from:

- `SAClassLibrary/Models/DailyCrewPositionHistory.cs`
- `SAClassLibrary/Models/DailyCrewPositionOnDutyFRARecord.cs`
- `SAClassLibrary/Models/DailyCrewPositionOnDutyMarkOffRecord.cs`
- `SAClassLibrary/Models/DailyCrewPositionOnDutyPayrollRecord.cs`

---

## GAP 890: SAClassLibrary DailyCrewPositionHistory Uses Snapshot-Oriented Position/Employee Display Fields

Model stores position name, employee display text, craft/position/roster numbers, and linkage IDs as direct history payload.

---

## GAP 891: SAClassLibrary DailyCrewPositionOnDutyFRARecord Uses Explicit ControlNumber Key with Extensive FRA Payload

FRA record stores certification, location, covered-service, monthly totals, and completion data in one row keyed by direct control number.

---

## GAP 892: SAClassLibrary DailyCrewPositionOnDutyMarkOffRecord Uses OnDuty FK-as-PK + Unique MarkOffLink

Model keys by `DailyCrewPositionOnDutyRecordControlNumber` and enforces unique index on `DailyRailroadEmployeePositionMarkOffRecordControlNumber`.

---

## GAP 893: SAClassLibrary DailyCrewPositionOnDutyPayrollRecord Uses Composite Key Bridge (OnDuty + PayrollRecord)

Link entity keys on (`DailyCrewPositionOnDutyRecordControlNumber`, `PayrollRecordControlNumber`) with non-generated pair values.

---

## GAP 894: OnDuty MarkOff/Payroll Link Entities Provide Pair-Seeding Factories

Both link entities expose `CreateInstance(...)` overloads that seed key linkage values for bridge row creation.

# Part 193: Gap Analysis - SAClassLibrary LateCall/Skip/VacancyEmployee/ExtraBoardMarkOff Notes

Gaps 895-899 capture additional verified details from:

- `SAClassLibrary/Models/DailyCrewPositionOnDutyRecordLateCall.cs`
- `SAClassLibrary/Models/DailyCrewPositionSkip.cs`
- `SAClassLibrary/Models/DailyCrewPositionVacancyEmployee.cs`
- `SAClassLibrary/Models/DailyExtraBoardMarkOffRecord.cs`

---

## GAP 895: SAClassLibrary DailyCrewPositionOnDutyRecordLateCall Uses OnDuty FK-as-PK One-to-One Pattern

Late-call row keys on `DailyCrewPositionOnDutyRecordControlNumber` and stores late-call/arrival/confirmation details with audit fields.

---

## GAP 896: SAClassLibrary DailyCrewPositionSkip Uses DailyCrewPosition FK-as-PK One-to-One Marker Pattern

Skip marker keys on `DailyCrewPositionControlNumber` and stores create attribution fields.

---

## GAP 897: SAClassLibrary DailyCrewPositionVacancyEmployee Uses Composite Key (Position + VacancyNumber)

Vacancy employee row keys on (`DailyCrewPositionControlNumber`, `VacancyNumber`) and stores required railroad-pool-employee link.

---

## GAP 898: SAClassLibrary DailyCrewPositionVacancyEmployee Factory Fully Seeds Key + Employee Link

`CreateInstance(long position, int vacnbr, long employee)` assigns both key parts and employee FK.

---

## GAP 899: SAClassLibrary DailyExtraBoardMarkOffRecord Uses ExtraBoardPosition FK-as-PK Snapshot Pattern

Row keys on `DailyShiftExtraBoardPositionControlNumber` and stores markoff/projected-assignment/tieup/board-order snapshot plus guarantee-loss flag.

# Part 194: Gap Analysis - SAClassLibrary FRA/Locomotive/Position-Payroll Link Notes

Gaps 900-904 capture additional verified details from:

- `SAClassLibrary/Models/DailyFRACommingleRecord.cs`
- `SAClassLibrary/Models/DailyFRADeadheadRecord.cs`
- `SAClassLibrary/Models/DailyOnDutyLocomotiveRecord.cs`
- `SAClassLibrary/Models/DailyRailroadEmployeePositionPayrollRecord.cs`

---

## GAP 900: SAClassLibrary DailyFRACommingleRecord Uses OnDutyFRA FK-as-PK One-to-One Pattern

Commingle row keys on `DailyCrewPositionOnDutyFRARecordControlNumber` and stores start/end location/time with required notes/create attribution.

---

## GAP 901: SAClassLibrary DailyFRADeadheadRecord Mirrors Commingle FK-as-PK One-to-One Pattern

Deadhead row uses same key and similar payload structure (start/end location/time, notes, create attribution).

---

## GAP 902: SAClassLibrary DailyOnDutyLocomotiveRecord Uses Explicit ControlNumber Key with Locomotive Snapshot Payload

Model stores locomotive ID/type/weight and related railroad-locomotive-type link with create/modify audit fields.

---

## GAP 903: SAClassLibrary DailyOnDutyLocomotiveRecord Links Optional LocomotiveInspectionRecord

Entity includes navigation to related `LocomotiveInspectionRecord` for inspection-state linkage.

---

## GAP 904: SAClassLibrary DailyRailroadEmployeePositionPayrollRecord Uses Composite Key Bridge (PositionRecord + PayrollRecord)

Bridge entity keys on (`DailyRailroadEmployeePositionRecordControlNumber`, `PayrollRecordControlNumber`) and provides pair-seeding factory method.

# Part 195: Gap Analysis - SAClassLibrary OffDay/Hangout/ExtraBoard Assignment-Payroll Link Notes

Gaps 905-909 capture additional verified details from:

- `SAClassLibrary/Models/DailyRailroadPositionOffDayEmployeeRecord.cs`
- `SAClassLibrary/Models/DailyRosterBoardPositionHangoutRecord.cs`
- `SAClassLibrary/Models/DailyShiftExtraBoardPositionAssignment.cs`
- `SAClassLibrary/Models/DailyShiftExtraBoardPositionPayrollRecord.cs`

---

## GAP 905: SAClassLibrary DailyRailroadPositionOffDayEmployeeRecord Uses Explicit ControlNumber Key with OffDay/Employee Link Payload

Model stores railroad position/date + railroad-pool-employee linkage and create/modify audit fields under direct control-number key.

---

## GAP 906: SAClassLibrary DailyRosterBoardPositionHangoutRecord Uses Position-Record FK-as-PK Marker Pattern

Hangout row keys on `DailyRailroadEmployeePositionRecordControlNumber` and stores markoff-code snapshot and related change-control reference.

---

## GAP 907: SAClassLibrary DailyShiftExtraBoardPositionAssignment Uses ExtraBoardPosition FK-as-PK One-to-One Assignment Link

Assignment row keys on `DailyShiftExtraBoardPositionControlNumber` and references assigned on-duty record plus board/tieup order snapshots.

---

## GAP 908: SAClassLibrary DailyShiftExtraBoardPositionPayrollRecord Uses Composite Key Bridge (ExtraBoardPosition + PayrollRecord)

Bridge entity keys on (`DailyShiftExtraBoardPositionControlNumber`, `PayrollRecordControlNumber`) with non-generated pair values.

---

## GAP 909: SAClassLibrary ExtraBoard Position Payroll Bridge Provides Pair-Seeding Factory

`CreateInstance(long position, long payroll)` seeds both key columns for bridge row creation.

# Part 196: Gap Analysis - SAClassLibrary DeletedRailroadPosition + Engineer Job/Rate Notes

Gaps 910-914 capture additional verified details from:

- `SAClassLibrary/Models/DeletedRailroadPosition.cs`
- `SAClassLibrary/Models/EngineerJobCode.cs`
- `SAClassLibrary/Models/EngineerJobCodeDelete.cs`
- `SAClassLibrary/Models/EngineerPayRate.cs`

---

## GAP 910: SAClassLibrary DeletedRailroadPosition Uses RailroadPositionControlNumber as One-to-One Key

Delete marker row keys directly by `RailroadPositionControlNumber` and stores delete/create timestamps with create attribution.

---

## GAP 911: SAClassLibrary EngineerJobCode Constructor Eager-Initializes EngineerPayRates Collection

Engineer-job-code model constructor initializes related pay-rate collection as `HashSet<>`.

---

## GAP 912: SAClassLibrary EngineerJobCodeDelete Uses EngineerJobCodeControlNumber as One-to-One Delete Marker Key

Delete marker model keys by engineer-job-code control number and stores delete/create metadata.

---

## GAP 913: SAClassLibrary EngineerPayRate Uses Explicit ControlNumber Key with Effective-Dated Rate Payload

Engineer pay-rate rows store engineer/trainee ST/OT rate columns plus `EffectiveDate` and full audit fields.

---

## GAP 914: SAClassLibrary Engineer Job/Rate Models Use Explicit In-Class Key/Audit Pattern (Non-base-Class Shape)

Reviewed engineer-domain entities define direct control-number keys and audit columns in POCO class definitions.

# Part 197: Gap Analysis - SAClassLibrary FRARequirements / FillVacancyLog / HoldDownReleas / MarkOffCodeApprovalOfficer Notes

Gaps 915-919 capture additional verified details from:

- `SAClassLibrary/Models/FRARequirements.cs`
- `SAClassLibrary/Models/FillVacancyLog.cs`
- `SAClassLibrary/Models/HoldDownReleas.cs`
- `SAClassLibrary/Models/MarkOffCodeApprovalOfficer.cs`

---

## GAP 915: SAClassLibrary FRARequirements Encodes FRA Constants as Static Properties

Static values include `MaxHours=12`, `RestHours=10`, `ConsecutiveDays=6`, and `ConsecutiveDayHours=24`.

---

## GAP 916: SAClassLibrary FRARequirements Calculates Additional Rest When On-Duty Time Exceeds MaxHours

`GetRestTime(...)` increases baseline rest by overtime-on-duty duration components beyond max-hours threshold.

---

## GAP 917: SAClassLibrary FillVacancyLog Uses Explicit ControlNumber Key with Text Snapshot Fields

Vacancy log row stores shift/position/employee display text snapshots plus duration and full audit fields.

---

## GAP 918: SAClassLibrary Includes Legacy-Name HoldDown Release POCO `HoldDownReleas` Mapped to `HoldDownReleases` Table

Type name is `HoldDownReleas` with `[Table("HoldDownReleases")]` mapping and explicit hold-down control-number key.

---

## GAP 919: SAClassLibrary MarkOffCodeApprovalOfficer Uses Explicit ControlNumber Key with MarkOffCode+Employee Link Payload

Approval-officer mapping row stores code/employee linkage and full create/modify audit columns.

# Part 198: Gap Analysis - SAClassLibrary LocomotiveInspection + MarkOff MarkUp/Payroll Mapping Notes

Gaps 920-924 capture additional verified details from:

- `SAClassLibrary/Models/LocomotiveInspectionRecord.cs`
- `SAClassLibrary/Models/MarkOffMarkUpHour.cs`
- `SAClassLibrary/Models/MarkOffMarkUpHours.cs`
- `SAClassLibrary/Models/MarkOffPayrollCode.cs`

---

## GAP 920: SAClassLibrary LocomotiveInspectionRecord Uses DailyOnDutyLocomotive FK-as-PK One-to-One Pattern

Inspection row keys on `DailyOnDutyLocomotiveRecordControlNumber` and stores inspection/fuel/repair payload plus create/modify attribution.

---

## GAP 921: SAClassLibrary Contains Two Near-Duplicate MarkOff MarkUp Models (`MarkOffMarkUpHour` and `MarkOffMarkUpHours`)

Both classes map same key/payload shape (`MarkoffCodeControlNumber`, `MarkUpHours`, audit fields, `MarkOffCode` navigation).

---

## GAP 922: SAClassLibrary MarkOffMarkUp Models Use MarkOffCodeControlNumber as One-to-One Key

Both mark-up models key directly by markoff-code control number with non-generated key.

---

## GAP 923: SAClassLibrary MarkOffPayrollCode Uses MarkOffCodeControlNumber as One-to-One Key

Mapping row keys by markoff-code control number and stores linked payroll code + basic-day flag.

---

## GAP 924: Reviewed MarkOff Mapping Models Use Explicit In-Class Audit Columns (Non-base-Class Pattern)

Mark-up and markoff-payroll mapping POCOs define create/modify attribution fields directly in entity definitions.

# Part 199: Gap Analysis - SAClassLibrary MarkOff Record/Request Approval & Delete Marker Notes

Gaps 925-929 capture additional verified details from:

- `SAClassLibrary/Models/MarkOffRecordApproval.cs`
- `SAClassLibrary/Models/MarkOffRecordDelete.cs`
- `SAClassLibrary/Models/MarkOffRequestApproval.cs`
- `SAClassLibrary/Models/MarkOffRequestDelete.cs`

---

## GAP 925: SAClassLibrary MarkOffRecordApproval Uses MarkOffRecordControlNumber as One-to-One Key

Approval row keys on `MarkOffRecordControlNumber` and stores optional approving employee plus full create/modify audit payload.

---

## GAP 926: SAClassLibrary MarkOffRecordDelete Uses MarkOffRecordControlNumber as One-to-One Delete Marker Key

Delete marker row keys on `MarkOffRecordControlNumber` and stores deleted/create datetime + created-by attribution.

---

## GAP 927: SAClassLibrary MarkOffRequestApproval Uses MarkOffRequestRecordControlNumber as One-to-One Key

Request-approval row keys on `MarkOffRequestRecordControlNumber` and stores optional approving employee and full create/modify audit fields.

---

## GAP 928: SAClassLibrary MarkOffRequestDelete Uses MarkOffRequestRecordControlNumber as One-to-One Delete Marker Key

Delete marker row keys on `MarkOffRequestRecordControlNumber` and stores deleted/create datetime + created-by attribution.

---

## GAP 929: SAClassLibrary MarkOff Approval/Delete Marker Models Follow Explicit In-Class Key/Audit POCO Pattern

All four reviewed entities define direct key and audit payload columns without control-number base inheritance.

# Part 200: Gap Analysis - SAClassLibrary MarkOff Request Link/Temp/WaitList Notes

Gaps 930-934 capture additional verified details from:

- `SAClassLibrary/Models/MarkOffRequestMarkOffRecord.cs`
- `SAClassLibrary/Models/MarkOffRequestMarkUpRecord.cs`
- `SAClassLibrary/Models/MarkOffRequestTempRecord.cs`
- `SAClassLibrary/Models/MarkOffRequestWaitListRecord.cs`

---

## GAP 930: SAClassLibrary MarkOffRequestMarkOffRecord Uses Composite Key Bridge (Request + Record)

Bridge key is (`MarkOffRequestRecordControlNumber`, `MarkOffRecordControlNumber`) with create attribution payload.

---

## GAP 931: SAClassLibrary MarkOffRequestMarkUpRecord Uses MarkOffRequestRecordControlNumber as One-to-One Key

Markup-request row keys on request control number and stores markup datetime with create/modify audit fields.

---

## GAP 932: SAClassLibrary MarkOffRequestTempRecord Uses Explicit ControlNumber Key with Reschedule Payload

Temp-request row stores request link, replacement request date, and week-count (`NbrOfWeeks`) plus audit fields.

---

## GAP 933: SAClassLibrary MarkOffRequestWaitListRecord Constructor Eager-Initializes Related Request Collection

Wait-list record constructor initializes `MarkOffRequestRecords` as `HashSet<>`.

---

## GAP 934: SAClassLibrary MarkOffRequestWaitListRecord Uses Snapshot-Style Employee/Code/Date Payload

Model stores employee and markoff identity snapshot fields (`EmployeeNumber`, `MOCode`, request/entry datetimes) alongside linkage IDs and audit fields.

# Part 201: Gap Analysis - SAClassLibrary Move/Note/OffPropertyTieUp/Cutoff-Time Notes

Gaps 935-939 capture additional verified details from:

- `SAClassLibrary/Models/MovedDailyCrewPosition.cs`
- `SAClassLibrary/Models/ObjectNote.cs`
- `SAClassLibrary/Models/OffPropertyTieUpRecord.cs`
- `SAClassLibrary/Models/OnDutyMoveCutOffTime.cs`

---

## GAP 935: SAClassLibrary MovedDailyCrewPosition Uses DailyCrewPositionControlNumber as One-to-One Key

Move-tracking row keys on `DailyCrewPositionControlNumber` and stores old-position/employee linkage plus create attribution.

---

## GAP 936: SAClassLibrary ObjectNote Uses ObjectControlNumber as Direct Key with Required Notes + Full Audit

Object-note model stores freeform notes and create/modify attribution under direct object-control-number identity.

---

## GAP 937: SAClassLibrary OffPropertyTieUpRecord Inherits ControlNumberBase with AspNetUser-Link Factory

Factory `CreateInstance(string id)` seeds `AspNetUserId`; tie-up text/datetime/employee fields are caller-populated.

---

## GAP 938: SAClassLibrary OffPropertyTieUpRecord Keeps Optional User-Link and Text Snapshot Payload

Model stores optional ASP.NET user link with employee-number/tie-up-datetime/text data for off-property tie-up events.

---

## GAP 939: SAClassLibrary OnDutyMoveCutOffTime Uses Explicit ControlNumber Key and Inline Audit Columns

Cutoff-time model stores craft+on-duty-time linkage and move-cutoff `TimeSpan` with create/modify attribution fields.

# Part 202: Gap Analysis - SAClassLibrary PayRate / Payroll Code-Rate / AutoPay / EarningProcessed Notes

Gaps 940-944 capture additional verified details from:

- `SAClassLibrary/Models/PayRate.cs`
- `SAClassLibrary/Models/PayrollCodePayRate.cs`
- `SAClassLibrary/Models/PayrollCrewPositionAutoPayRecord.cs`
- `SAClassLibrary/Models/PayrollEarningProcessedRecord.cs`

---

## GAP 940: SAClassLibrary PayRate Uses Explicit ControlNumber Key with Float `Rate` Field

Pay-rate model stores numeric rate (`float`) and description/audit payload under direct control-number identity.

---

## GAP 941: SAClassLibrary PayrollCodePayRate Uses Explicit ControlNumber Key with Effective-Dated Amount

Code-pay-rate row stores payroll-code link, optional position link, amount, effective date, and full create/modify audit fields.

---

## GAP 942: SAClassLibrary PayrollCrewPositionAutoPayRecord Uses RailroadPoolEmployeeControlNumber as Key

Auto-pay row keys on `RailroadPoolEmployeeControlNumber` and stores related railroad position/employee/payroll code linkage with expiration and behavior flags.

---

## GAP 943: SAClassLibrary PayrollCrewPositionAutoPayRecord Includes BasicDay + Arbitraries Flags

Auto-pay model explicitly persists `BasicDay` and `Arbitraries` behavior booleans.

---

## GAP 944: SAClassLibrary PayrollEarningProcessedRecord Uses PayrollEarningRecordControlNumber as One-to-One Process Marker Key

Processed-row model stores pay period, processed datetime, final-process flag, creator, and optional payroll-period-process link.

# Part 203: Gap Analysis - SAClassLibrary Payroll Earning/Holiday/Process Baseline Notes

Gaps 945-949 capture additional verified details from:

- `SAClassLibrary/Models/PayrollEarningRecord.cs`
- `SAClassLibrary/Models/PayrollHolidayRecord.cs`
- `SAClassLibrary/Models/PayrollHolidayRecordPayrollRecord.cs`
- `SAClassLibrary/Models/PayrollPeriodProcessRecord.cs`

---

## GAP 945: SAClassLibrary PayrollEarningRecord Constructor Defaults RecordCount/Calculated/PaidAmount

Default constructor initializes `RecordCount=1`, `CalculatedAmount=0`, and `PaidAmount=0`.

---

## GAP 946: SAClassLibrary PayrollEarningRecord Approval/Decline Flags Are Presence-Based

`IsApproved`/`IsDeclined` are derived by checking existence of related approval/declination rows.

---

## GAP 947: SAClassLibrary PayrollHolidayRecord `Qualified` Requires PRE then POST Qualify Success

Qualification logic checks PRE record first, then validates POST record only if PRE was qualified.

---

## GAP 948: SAClassLibrary PayrollHolidayRecordPayrollRecord Uses Composite Bridge Key (HolidayRecord + PayrollRecord)

Bridge entity keys on (`PayrollHolidayRecordControlNumber`, `PayrollRecordControlNumber`) with pair-seeding factory.

---

## GAP 949: SAClassLibrary PayrollPeriodProcessRecord Constructor Eager-Initializes Processed-Earnings Collection

Constructor initializes `PayrollEarningProcessedRecords` as `HashSet<>`; model stores process metadata, output paths, and full create/modify audit fields.

# Part 204: Gap Analysis - SAClassLibrary PayrollRecord/Delete/Review Baseline Notes

Gaps 950-954 capture additional verified details from:

- `SAClassLibrary/Models/PayrollRecord.cs`
- `SAClassLibrary/Models/PayrollRecordDelete.cs`
- `SAClassLibrary/Models/PayrollReviewRecord.cs`
- `SAClassLibrary/Models/PayrollReviewRequiredRecord.cs`

---

## GAP 950: SAClassLibrary PayrollRecord Factory Seeds Employee/Railroad/WorkNumber from RailroadPoolEmployee

`CreateInstance(RailroadPoolEmployee rpemployee)` copies railroad-pool, railroad-employee, employee, and work-number values from supplied employee entity.

---

## GAP 951: SAClassLibrary PayrollRecord Includes Required `RatePercentage` Column

Payroll record model explicitly stores required integer `RatePercentage` field as part of payroll payload.

---

## GAP 952: SAClassLibrary PayrollRecordDelete Uses PayrollRecordControlNumber as One-to-One Delete Marker Key

Delete marker row keys on payroll-record control number and stores deleted/create timestamp + created-by attribution.

---

## GAP 953: SAClassLibrary PayrollReviewRequiredRecord Uses PayrollRecordControlNumber as One-to-One Review-Required Key

Review-required row keys on payroll-record control number and stores required reason with full create/modify audit fields.

---

## GAP 954: SAClassLibrary PayrollReviewRecord Uses PayrollRecordControlNumber as One-to-One Review-Completed Marker Key

Review-completion row keys on payroll-record control number and stores create/modify audit fields linked to required-review parent row.

# Part 205: Gap Analysis - SAClassLibrary Position/AltSupervisor/PayRate/Requirement Notes

Gaps 955-959 capture additional verified details from:

- `SAClassLibrary/Models/Position.cs`
- `SAClassLibrary/Models/PositionAlternateSupervisor.cs`
- `SAClassLibrary/Models/PositionPayRate.cs`
- `SAClassLibrary/Models/PositionRequirement.cs`

---

## GAP 955: SAClassLibrary Position Constructor Eager-Initializes Broad Related Collections

Constructor initializes alternate-position, crew-position, daily-position, pay-rate, requirement, qualification, and training-date collections.

---

## GAP 956: SAClassLibrary Position Includes Required `PositionCode` and Required Integer `MustFill`

Position model enforces required two-character position code and required integer must-fill setting.

---

## GAP 957: SAClassLibrary PositionAlternateSupervisor Uses PositionControlNumber as One-to-One Key

Alternate-supervisor row keys on `PositionControlNumber` and stores linked employee and create/modify audit fields.

---

## GAP 958: SAClassLibrary PositionPayRate Uses Explicit ControlNumber Key with Effective-Dated ST/OT Rates

Position pay-rate row stores ST/OT hour rates, effective date, and full create/modify attribution.

---

## GAP 959: SAClassLibrary PositionRequirement Uses Composite Key (PositionControlNumber + RequirementControlNumber)

Requirement mapping keys on position/requirement pair and constructor initializes related `PositionRequirementEmployees` collection.

# Part 206: Gap Analysis - SAClassLibrary PositionRequirementEmployee / Qualification / Railroad / RailroadAFE Notes

Gaps 960-964 capture additional verified details from:

- `SAClassLibrary/Models/PositionRequirementEmployee.cs`
- `SAClassLibrary/Models/Qualification.cs`
- `SAClassLibrary/Models/Railroad.cs`
- `SAClassLibrary/Models/RailroadAFE.cs`

---

## GAP 960: SAClassLibrary PositionRequirementEmployee Uses Explicit ControlNumber Key with PositionRequirement Link Payload

Model stores position+requirement+railroad-pool-employee completion under direct control-number identity with full audit fields.

---

## GAP 961: SAClassLibrary Qualification Uses Explicit ControlNumber Key with Position/Employee Effective-Date Payload

Qualification row stores linked position/employee and required effective date plus full create/modify audit columns.

---

## GAP 962: SAClassLibrary Railroad Constructor Eager-Initializes Broad Cross-Domain Collections

Constructor initializes collections spanning engineer/payroll/pool/location/material/slow-order/BeSafe/railroad-information domains.

---

## GAP 963: SAClassLibrary Railroad Exposes `RailroadMark_Name` Computed Concatenation Property

Computed property returns concatenated railroad mark and name (`RailroadMark + " " + RailroadName`).

---

## GAP 964: SAClassLibrary RailroadAFE Uses Explicit ControlNumber Key with Required AFE Number/Description + Full Audit

AFE model stores railroad link and required AFE identity/description payload with create/modify attribution fields.

# Part 207: Gap Analysis - SAClassLibrary RailroadArea/RailroadEmployee/LocomotiveType/Location Notes

Gaps 965-969 capture additional verified details from:

- `SAClassLibrary/Models/RailroadArea.cs`
- `SAClassLibrary/Models/RailroadEmployee.cs`
- `SAClassLibrary/Models/RailroadLocomotiveType.cs`
- `SAClassLibrary/Models/RailroadLocation.cs`

---

## GAP 965: SAClassLibrary RailroadArea Constructor Seeds Railroad Link (Protected Constructor Pattern)

`RailroadArea` uses protected constructor taking railroad control number and stores required area number/name fields in derived payload.

---

## GAP 966: SAClassLibrary RailroadEmployee Exposes Multiple Employee Delegation Properties via `[NotMapped]`

`TieUpOffProperty`, `EmployeeNumber`, `EmpNbr_FullName`, `BirthDate`, and `EmploymentDate` delegate directly to linked `Employee` fields.

---

## GAP 967: SAClassLibrary RailroadEmployee Contains Extensive Commented-Out Operational Computed Logic

Large sections of advanced position/seniority/comp-time related computed logic are present but commented out in this class snapshot.

---

## GAP 968: SAClassLibrary RailroadLocomotiveType Constructor Eager-Initializes On-Duty Locomotive Record Collection

Constructor initializes `DailyOnDutyLocomotiveRecords` as `HashSet<>`; model includes required `Default` boolean flag.

---

## GAP 969: SAClassLibrary RailroadLocation Constructor Eager-Initializes Miscellaneous Billing Collection

`RailroadLocation` initializes `DailyOnDutyMiscellaneousBillingRecords` and stores location-number/name with full create/modify audit fields.

# Part 208: Gap Analysis - SAClassLibrary RailroadEmployee Calendar/Compensable/ReportViewed/VacationOneDay Notes

Gaps 970-974 capture additional verified details from:

- `SAClassLibrary/Models/RailroadEmployeeCalendarRequest.cs`
- `SAClassLibrary/Models/RailroadEmployeeCompensableTimeRecord.cs`
- `SAClassLibrary/Models/RailroadEmployeeReportViewedRecord.cs`
- `SAClassLibrary/Models/RailroadEmployeeVacationOneDayTimeRecord.cs`

---

## GAP 970: SAClassLibrary RailroadEmployeeCalendarRequest Uses Explicit ControlNumber Key with Used-Flag Request Payload

Calendar-request row stores railroad-employee link/name, request datetime, used-state flag, and full create/modify audit fields.

---

## GAP 971: SAClassLibrary RailroadEmployeeCompensableTimeRecord Inherits ControlNumberBase with Employee-Link Factory

Factory `CreateInstance(long rremployee)` seeds employee link; pool/compensation/entry/amount payload is caller-populated.

---

## GAP 972: SAClassLibrary RailroadEmployeeReportViewedRecord Inherits ControlNumberBase with Employee+Pool Factory

Factory `CreateInstance(long rremployee, long rrpool)` seeds employee/pool links for report-view audit row.

---

## GAP 973: SAClassLibrary RailroadEmployeeVacationOneDayTimeRecord Uses Explicit ControlNumber Key with Hour-Balance Payload

Model stores initial/additional hour totals, pool number, entry date, and employee display snapshot field.

---

## GAP 974: Reviewed RailroadEmployee Auxiliary Tracking Models Use Mixed Base-Class and Explicit-Key Patterns

Some entities inherit `ControlNumberBase` (compensable/report-viewed), while others use explicit direct-key POCO style (calendar request/vacation one-day).

# Part 209: Gap Analysis - SAClassLibrary VacationRequest Assignment + RailroadInformation Cancel/Close Notes

Gaps 975-979 capture additional verified details from:

- `SAClassLibrary/Models/RailroadEmployeeVacationRequest.cs`
- `SAClassLibrary/Models/RailroadEmployeeVacationRequestAssignment.cs`
- `SAClassLibrary/Models/RailroadInformationCancelRecord.cs`
- `SAClassLibrary/Models/RailroadInformationCloseRecord.cs`

---

## GAP 975: SAClassLibrary RailroadEmployeeVacationRequest Factory Seeds Employee Link Only

`CreateInstance(long rremployee)` sets `RailroadEmployeeControlNumber`; split/choice/weeks/date/waitlist/auto flags are caller-populated.

---

## GAP 976: SAClassLibrary RailroadEmployeeVacationRequest `IsAssigned` Is Presence-Based

Computed flag returns true when one-to-one `RailroadEmployeeVacationRequestAssignment` row exists.

---

## GAP 977: SAClassLibrary RailroadEmployeeVacationRequestAssignment Uses VacationRequest FK-as-PK One-to-One Pattern

Assignment row keys on `RailroadEmployeeVacationRequestControlNumber` and stores craft/notes/create attribution payload.

---

## GAP 978: SAClassLibrary RailroadInformationCancelRecord Uses RailroadInformationRecord FK-as-PK One-to-One Marker Pattern

Cancel row keys on `RailroadInformationRecordControlNumber` and stores cancel datetime + create attribution.

---

## GAP 979: SAClassLibrary RailroadInformationCloseRecord Uses RailroadInformationRecord FK-as-PK One-to-One Marker Pattern

Close row keys on `RailroadInformationRecordControlNumber` and stores close datetime + create attribution.

# Part 210: Gap Analysis - SAClassLibrary RailroadInformation Delete/Publish/Read/Record Notes

Gaps 980-984 capture additional verified details from:

- `SAClassLibrary/Models/RailroadInformationDeleteRecord.cs`
- `SAClassLibrary/Models/RailroadInformationPublishRecord.cs`
- `SAClassLibrary/Models/RailroadInformationReadbyEmployeeRecord.cs`
- `SAClassLibrary/Models/RailroadInformationRecord.cs`

---

## GAP 980: SAClassLibrary RailroadInformationDeleteRecord Uses RailroadInformationRecord FK-as-PK One-to-One Marker Pattern

Delete marker keys on `RailroadInformationRecordControlNumber` and stores delete/create datetimes plus created-by attribution.

---

## GAP 981: SAClassLibrary RailroadInformationPublishRecord Uses RailroadInformationRecord FK-as-PK One-to-One Marker Pattern

Publish marker keys on `RailroadInformationRecordControlNumber` and stores publish datetime, `EmployeesNotified`, and create attribution.

---

## GAP 982: SAClassLibrary RailroadInformationReadbyEmployeeRecord Inherits ControlNumberBase with Record-Link Factory

Factory `CreateInstance(long record)` seeds information-record link; employee/read-datetime are caller-populated.

---

## GAP 983: SAClassLibrary RailroadInformationRecord Uses Presence-Based `IsPublished` + PublishDate Fallback Logic

`IsPublished` checks publish marker presence; `PublishDate` returns marker date when present, otherwise defaults to `DateTime.Today`.

---

## GAP 984: SAClassLibrary RailroadInformationRecord Factory Seeds Railroad Link Only

`CreateInstance(long railroad)` initializes `RailroadControlNumber`; type/record number/title/description and audit fields are caller-populated.

# Part 211: Gap Analysis - SAClassLibrary RailroadInformationType / Material / MaterialCategory / PayrollDepartment Notes

Gaps 985-989 capture additional verified details from:

- `SAClassLibrary/Models/RailroadInformationType.cs`
- `SAClassLibrary/Models/RailroadMaterial.cs`
- `SAClassLibrary/Models/RailroadMaterialCategory.cs`
- `SAClassLibrary/Models/RailroadPayrollDepartment.cs`

---

## GAP 985: SAClassLibrary RailroadInformationType Factory Seeds Railroad Link Only

`CreateInstance(long railroad)` initializes `RailroadControlNumber`; type/header/signature fields are caller-populated.

---

## GAP 986: SAClassLibrary RailroadInformationType Requires SignatureName and SignatureTitle

Both signature fields are required payload fields; `HeaderTitle` remains optional.

---

## GAP 987: SAClassLibrary RailroadMaterial Uses Explicit ControlNumber Key with Required Material Identity Fields

Material model requires type/code/description/unit indicator and stores material-category linkage with full create/modify audit fields.

---

## GAP 988: SAClassLibrary RailroadMaterialCategory Constructor Eager-Initializes RailroadMaterials Collection

Category constructor initializes related materials collection as `HashSet<>`.

---

## GAP 989: SAClassLibrary RailroadPayrollDepartment Constructor Eager-Initializes Positions Collection

Payroll-department model initializes `Positions` collection and stores required department/ICC/general-ledger fields with full create/modify audit payload.

# Part 212: Gap Analysis - SAClassLibrary RailroadPool Allowance/Tier/Requirement Notes

Gaps 990-994 capture additional verified details from:

- `SAClassLibrary/Models/RailroadPoolMarkOffAllowance.cs`
- `SAClassLibrary/Models/RailroadPoolPayrollTier.cs`
- `SAClassLibrary/Models/RailroadPoolRequirement.cs`
- `SAClassLibrary/Models/RailroadPoolRequirementEmployee.cs`

---

## GAP 990: SAClassLibrary RailroadPoolMarkOffAllowance Uses Explicit ControlNumber Key with Allowance Snapshot Payload

Model stores pool/year/calculated/allowed/total values, allowance type, and full create/modify audit fields.

---

## GAP 991: SAClassLibrary RailroadPoolPayrollTier Inherits ControlNumberBase with Pool-Link Factory

Factory `CreateInstance(long rrpool)` seeds `RailroadPoolControlNumber`; day-type/rate-percentage payload is caller-populated.

---

## GAP 992: SAClassLibrary RailroadPoolRequirement Uses Composite Key (RailroadPoolControlNumber + RequirementControlNumber)

Requirement mapping keys on pool/requirement pair and constructor initializes related requirement-employee collection.

---

## GAP 993: SAClassLibrary RailroadPoolRequirementEmployee Uses Explicit ControlNumber Key with Pool/Requirement/Employee Payload

Completion row stores pool+requirement linkage, railroad-pool-employee link, completion datetime, and full create/modify audit fields.

---

## GAP 994: Reviewed RailroadPool Requirement Models Use Mixed Composite-Mapping + Explicit Completion Row Pattern

`RailroadPoolRequirement` defines composite mapping key while `RailroadPoolRequirementEmployee` captures completions under separate direct control-number identity.

# Part 213: Gap Analysis - SAClassLibrary RailroadPoolEmployee + BulletinsViewed + Position/PositionHistory Notes

Gaps 995-999 capture additional verified details from:

- `SAClassLibrary/Models/RailroadPoolEmployee.cs`
- `SAClassLibrary/Models/RailroadPoolEmployeeBulletinsViewedRecord.cs`
- `SAClassLibrary/Models/RailroadPoolEmployeePosition.cs`
- `SAClassLibrary/Models/RailroadPoolEmployeePositionHistory.cs`

---

## GAP 995: SAClassLibrary RailroadPoolEmployee Delegates Multiple Identity Fields via `[NotMapped]`

`EmployeeControlNumber`, `UserID`, `EmployeeNumber`, and `EmpNbr_FullName` delegate to linked railroad-employee/employee objects.

---

## GAP 996: SAClassLibrary RailroadPoolEmployee Contains Extensive Commented-Out Default Job/Pay and Position Logic

Large blocks of operational logic (default job worked/paid and related assignment helpers) remain present but commented out in this class snapshot.

---

## GAP 997: SAClassLibrary RailroadPoolEmployeeBulletinsViewedRecord Inherits ControlNumberBase with Employee-Link Factory

Factory `CreateInstance(long rremployee)` seeds railroad-pool-employee link for bulletin-view audit row creation.

---

## GAP 998: SAClassLibrary RailroadPoolEmployeePosition Uses RailroadPositionControlNumber as One-to-One Assignment Key

Assignment row keys on railroad-position control number and stores employee/type/control/date assignment payload.

---

## GAP 999: SAClassLibrary RailroadPoolEmployeePositionHistory Uses Explicit ControlNumber Key for Assignment Snapshot History

History rows store position/employee assignment snapshots (type/control/date) with full create/modify audit columns.

# Part 214: Gap Analysis - SAClassLibrary TrainingDate + Bulletin Assignment/BidAssignment/NoBid Notes

Gaps 1000-1004 capture additional verified details from:

- `SAClassLibrary/Models/RailroadPoolEmployeeTrainingDate.cs`
- `SAClassLibrary/Models/RailroadPositionBulletinAssignment.cs`
- `SAClassLibrary/Models/RailroadPositionBulletinBidAssignment.cs`
- `SAClassLibrary/Models/RailroadPositionBulletinNoBid.cs`

---

## GAP 1000: SAClassLibrary RailroadPoolEmployeeTrainingDate Uses Explicit ControlNumber Key with Employee/Crew/Position Training Snapshot

Training-date row stores linked pool employee, crew, position, training date, and full create/modify audit fields.

---

## GAP 1001: SAClassLibrary RailroadPositionBulletinAssignment Uses BulletinControlNumber as One-to-One Assignment Key

Assignment row keys on bulletin control number and stores assigned pool employee, assigned datetime, and create attribution.

---

## GAP 1002: SAClassLibrary RailroadPositionBulletinBidAssignment Uses BidControlNumber as One-to-One Assignment Key

Bid-assignment row keys on bulletin-bid control number and stores linked bulletin control number with create attribution.

---

## GAP 1003: SAClassLibrary RailroadPositionBulletinNoBid Uses BulletinControlNumber as One-to-One No-Bid Marker Key

No-bid row keys on bulletin control number and stores assigned datetime and create attribution.

---

## GAP 1004: SAClassLibrary Bulletin Assignment/BidAssignment/NoBid Models Use Explicit In-Class Key/Audit Marker Pattern

All three reviewed bulletin outcome entities define direct key fields with minimal assignment marker payload.

# Part 215: Gap Analysis - SAClassLibrary RefreshRate / RemovedPoolEmployee / RequirementDelete / RailroadRequirementEmployee Notes

Gaps 1005-1009 capture additional verified details from:

- `SAClassLibrary/Models/RefreshRate.cs`
- `SAClassLibrary/Models/RemovedRailroadPoolEmployee.cs`
- `SAClassLibrary/Models/RequirementDelete.cs`
- `SAClassLibrary/Models/RailroadRequirementEmployee.cs`

---

## GAP 1005: SAClassLibrary RefreshRate Uses Explicit ControlNumber Key with Seconds-Based Interval Payload

Refresh-rate model stores required description and integer `RefreshRateSeconds` with full create/modify audit fields.

---

## GAP 1006: SAClassLibrary RemovedRailroadPoolEmployee Uses RailroadPoolEmployeeControlNumber as One-to-One Removal Marker Key

Removal marker row keys on railroad-pool-employee control number and stores removed/create datetime + created-by attribution.

---

## GAP 1007: SAClassLibrary RequirementDelete Uses RequirementControlNumber as One-to-One Delete Marker Key

Delete marker row keys on requirement control number and stores create attribution/date.

---

## GAP 1008: SAClassLibrary RailroadRequirementEmployee Uses Explicit ControlNumber Key with Railroad/Requirement/Employee Completion Payload

Completion row stores railroad+requirement linkage, railroad-employee link, completion datetime, and full create/modify audit fields.

---

## GAP 1009: Reviewed Requirement/Removal Marker Models Follow Explicit In-Class Key/Audit POCO Pattern

All four entities define direct key and audit payload columns without control-number base inheritance.

# Part 216: Gap Analysis - SAClassLibrary RailroadRequirement / WorkCode / Zone / Requirement Notes

Gaps 1010-1014 capture additional verified details from:

- `SAClassLibrary/Models/RailroadRequirement.cs`
- `SAClassLibrary/Models/RailroadWorkCode.cs`
- `SAClassLibrary/Models/RailroadZone.cs`
- `SAClassLibrary/Models/Requirement.cs`

---

## GAP 1010: SAClassLibrary RailroadRequirement Uses Composite Key (RailroadControlNumber + RequirementControlNumber)

Railroad requirement mapping keys on railroad/requirement pair and constructor initializes related completion collection.

---

## GAP 1011: SAClassLibrary RailroadWorkCode Constructor Eager-Initializes Miscellaneous Billing Collection

`RailroadWorkCode` initializes `DailyOnDutyMiscellaneousBillingRecords` and includes required work-code name plus billable-code flag.

---

## GAP 1012: SAClassLibrary RailroadZone Uses Explicit ControlNumber Key with ZoneNumber/ZoneName Payload

Zone model stores required zone name, numeric zone number, railroad link, and full create/modify audit fields.

---

## GAP 1013: SAClassLibrary Requirement Constructor Eager-Initializes Broad Mapping Collections

Requirement constructor initializes client/position/pool/railroad requirement mappings and associated craft collection.

---

## GAP 1014: SAClassLibrary Requirement Includes RequirementDelete One-to-One Delete Marker Navigation

Requirement model directly exposes `RequirementDelete` navigation for delete-marker linkage.

# Part 217: Gap Analysis - SAClassLibrary Roster / RosterBoard / RosterBoardPosition / RosterBulletinRule Notes

Gaps 1015-1019 capture additional verified details from:

- `SAClassLibrary/Models/Roster.cs`
- `SAClassLibrary/Models/RosterBoard.cs`
- `SAClassLibrary/Models/RosterBoardPosition.cs`
- `SAClassLibrary/Models/RosterBulletinRule.cs`

---

## GAP 1015: SAClassLibrary Roster Constructor Eager-Initializes OvertimeBoard/Position/Board/Seniority Collections

Constructor initializes daily overtime boards, positions, roster boards, and seniorities as `HashSet<>` collections.

---

## GAP 1016: SAClassLibrary RosterBoard Constructor Eager-Initializes ExtraBoard and BoardPosition Collections

`RosterBoard` initializes `DailyShiftExtraBoards` and `RosterBoardPositions`; model stores multiple board behavior flags including integer `ExtraBoard` mode.

---

## GAP 1017: SAClassLibrary RosterBoardPosition Uses RailroadPositionControlNumber as One-to-One Key

Board-position row keys on railroad-position control number and stores roster-board link plus position number/name and full create/modify audit fields.

---

## GAP 1018: SAClassLibrary RosterBulletinRule Uses RosterControlNumber as One-to-One Rule Key

Bulletin-rule row keys on roster control number and stores bulletin timing values, bulletin hours, forced-assign hours, and effective day.

---

## GAP 1019: SAClassLibrary Roster Model Exposes One-to-One BulletinRule and SeniorityMoveRule Navigations

`Roster` directly includes `RosterBulletinRule` and `RosterSeniorityMoveRule` as single related rule entities.

# Part 218: Gap Analysis - SAClassLibrary RosterSeniorityMoveRule / Seniority / SeniorityEndDate / SeniorityMove Notes

Gaps 1020-1024 capture additional verified details from:

- `SAClassLibrary/Models/RosterSeniorityMoveRule.cs`
- `SAClassLibrary/Models/Seniority.cs`
- `SAClassLibrary/Models/SeniorityEndDate.cs`
- `SAClassLibrary/Models/SeniorityMove.cs`

---

## GAP 1020: SAClassLibrary RosterSeniorityMoveRule Uses RosterControlNumber as One-to-One Rule Key

Rule row keys on roster control number and stores required/cancel/request hours with full create/modify audit fields.

---

## GAP 1021: SAClassLibrary Seniority Uses Explicit ControlNumber Key with Rank/State/CanTrain Payload

Seniority row stores roster and employee links, roster date/rank, state ID, `CanTrain`, and `LastActiveRoster` flags.

---

## GAP 1022: SAClassLibrary SeniorityEndDate Uses SeniorityControlNumber as One-to-One End-Date Marker Key

End-date marker row keys on seniority control number and stores end datetime with create attribution.

---

## GAP 1023: SAClassLibrary SeniorityMove Uses Explicit ControlNumber Key with MoveType/AutoProcess Fields

Move row stores pool-employee/position links, request/effective datetimes, move type, and auto-process flag plus full create/modify audit fields.

---

## GAP 1024: SAClassLibrary SeniorityMove Links Optional Assignment and WillWork Outcome Rows

Model includes `SeniorityMoveAssignment` and `SeniorityMoveWillWork` one-to-one outcome navigations.

# Part 219: Gap Analysis - SAClassLibrary SeniorityMove Assignment/WillWork + SeniorityState + Shift Notes

Gaps 1025-1029 capture additional verified details from:

- `SAClassLibrary/Models/SeniorityMoveAssignment.cs`
- `SAClassLibrary/Models/SeniorityMoveWillWork.cs`
- `SAClassLibrary/Models/SeniorityState.cs`
- `SAClassLibrary/Models/Shift.cs`

---

## GAP 1025: SAClassLibrary SeniorityMoveAssignment Uses SeniorityMoveControlNumber as One-to-One Assignment Marker Key

Assignment marker keys on seniority-move control number and stores assigned datetime plus create attribution.

---

## GAP 1026: SAClassLibrary SeniorityMoveWillWork Uses SeniorityMoveControlNumber as One-to-One WillWork Marker Key

Will-work marker keys on seniority-move control number and stores boolean decision plus create attribution.

---

## GAP 1027: SAClassLibrary SeniorityState Constructor Eager-Initializes Seniorities Collection

`SeniorityState` initializes `Seniorities` as `HashSet<>`; model stores Active/CutBack/Inactive booleans per state.

---

## GAP 1028: SAClassLibrary Shift Computes Previous/Next Shift IDs via Hardcoded 1-2-3 Rotation Mapping

`PreviousShiftID` and `NextShiftID` are derived from fixed three-shift circular mapping.

---

## GAP 1029: SAClassLibrary Shift Constructor Defaults `ReliefShift=false` and Factory Seeds Pool Link

Default constructor sets `ReliefShift` false; `CreateInstance(long pool)` seeds `RailroadPoolControlNumber`.

# Part 220: Gap Analysis - SAClassLibrary SlowOrder Area/Change/Complete/Delete Notes

Gaps 1030-1034 capture additional verified details from:

- `SAClassLibrary/Models/SlowOrderArea.cs`
- `SAClassLibrary/Models/SlowOrderChangeRecord.cs`
- `SAClassLibrary/Models/SlowOrderCompleteRecord.cs`
- `SAClassLibrary/Models/SlowOrderDeleteRecord.cs`

---

## GAP 1030: SAClassLibrary SlowOrderArea Factory Seeds Railroad Link Only

`CreateInstance(long railroad)` initializes `RailroadControlNumber`; area number/name are caller-populated.

---

## GAP 1031: SAClassLibrary SlowOrderChangeRecord Inherits ControlNumberBase with Parent-Link Factory

Factory `CreateInstance(long sloworder)` seeds slow-order-record link while title/description payload is caller-populated.

---

## GAP 1032: SAClassLibrary SlowOrderCompleteRecord Uses SlowOrderRecord FK-as-PK One-to-One Marker Pattern

Complete marker keys on `SlowOrderRecordControlNumber` and stores completion datetime plus create attribution.

---

## GAP 1033: SAClassLibrary SlowOrderDeleteRecord Uses SlowOrderRecord FK-as-PK One-to-One Marker Pattern

Delete marker keys on `SlowOrderRecordControlNumber` and stores delete datetime plus create attribution.

---

## GAP 1034: SAClassLibrary SlowOrder Complete/Delete Markers Share Constructor/Factory Key-Seeding Pattern

Both models expose constructors and `CreateInstance(long sloworder)` helpers to seed the one-to-one key linkage value.

# Part 221: Gap Analysis - SAClassLibrary SlowOrderRecord + TemporaryAssignment/AFE/AssignedEmployee Notes

Gaps 1035-1039 capture additional verified details from:

- `SAClassLibrary/Models/SlowOrderRecord.cs`
- `SAClassLibrary/Models/TemporaryAssignment.cs`
- `SAClassLibrary/Models/TemporaryAssignmentAFERecord.cs`
- `SAClassLibrary/Models/TemporaryAssignmentAssignedEmployee.cs`

---

## GAP 1035: SAClassLibrary SlowOrderRecord Factory Seeds Railroad Link Only

`CreateInstance(long railroad)` initializes `RailroadControlNumber`; slow-order area/title/description fields are caller-populated.

---

## GAP 1036: SAClassLibrary TemporaryAssignment Open/Closed State Is Release-Record Driven

`HasReleaseRecord`, `IsClosed`, and `IsOpen` derive from one-to-one release marker presence and released-date comparison.

---

## GAP 1037: SAClassLibrary TemporaryAssignment Factory Seeds Assignment Link Only

`CreateInstance(long assignment)` sets `AssignmentControlNumber`; temporary assignment payload fields are caller-populated.

---

## GAP 1038: SAClassLibrary TemporaryAssignmentAFERecord Uses TemporaryAssignmentControlNumber as One-to-One AFE Marker Key

AFE row keys on temporary-assignment control number and stores required AFE number/description plus full create/modify audit fields.

---

## GAP 1039: SAClassLibrary TemporaryAssignmentAssignedEmployee Uses TemporaryAssignmentControlNumber as One-to-One Assignment Marker Key

Assigned-employee row keys on temporary-assignment control number and stores linked railroad-pool-employee with create attribution.

# Part 222: Gap Analysis - SAClassLibrary TemporaryAssignmentRelease(s) / WorkDay / UKGInterface Notes

Gaps 1040-1044 capture additional verified details from:

- `SAClassLibrary/Models/TemporaryAssignmentReleas.cs`
- `SAClassLibrary/Models/TemporaryAssignmentRelease.cs`
- `SAClassLibrary/Models/TemporaryAssignmentWorkDay.cs`
- `SAClassLibrary/Models/UKGInterface.cs`

---

## GAP 1040: SAClassLibrary Includes Duplicate-Named Temporary Assignment Release POCOs Mapping to Same Table

Both `TemporaryAssignmentReleas` and `TemporaryAssignmentRelease` map to `[Table("TemporaryAssignmentReleases")]` with matching key/payload shape.

---

## GAP 1041: SAClassLibrary TemporaryAssignmentRelease Models Use TemporaryAssignmentControlNumber as One-to-One Release Marker Key

Release marker rows key on temporary-assignment control number and store release datetime plus create attribution.

---

## GAP 1042: SAClassLibrary TemporaryAssignmentWorkDay Uses Explicit ControlNumber Key with TemporaryAssignment+WeekDay Link Payload

Workday rows store temporary assignment and weekday links with full create/modify audit fields.

---

## GAP 1043: SAClassLibrary UKGInterface Inherits ControlNumberBase and Uses PayrollCode-Link Factory

Factory `CreateInstance(PayrollCode code)` seeds payroll-code link for UKG mapping entity.

---

## GAP 1044: SAClassLibrary UKGInterface Enforces Unique `UKGEarningCode` with Private Setter

`UKGEarningCode` is `[Index(IsUnique = true)]` and `private set`, constraining update flow to entity construction/persistence patterns.

# Part 223: Gap Analysis - SAClassLibrary UserLoginRecord / WeekDay / AssemblyInfo / Project File Notes

Gaps 1045-1049 capture additional verified details from:

- `SAClassLibrary/Models/UserLoginRecord.cs`
- `SAClassLibrary/Models/WeekDay.cs`
- `SAClassLibrary/Properties/AssemblyInfo.cs`
- `SAClassLibrary/SAClassLibrary.csproj`

---

## GAP 1045: SAClassLibrary UserLoginRecord Uses Explicit ControlNumber Key with Login Snapshot + Network Context

Model stores user/employee identifiers, login datetime, IP address, on-property flag, and full create/modify audit fields.

---

## GAP 1046: SAClassLibrary WeekDay Inherits ControlNumberBase with Client-Link Factory

Factory `CreateInstance(long client)` seeds `ClientControlNumber`; weekday number/name values are caller-populated.

---

## GAP 1047: SAClassLibrary AssemblyInfo Uses Fixed `1.0.0.0` Versioning and `ComVisible(false)`

Assembly metadata sets fixed assembly/file versions and disables COM visibility by default.

---

## GAP 1048: SAClassLibrary Project Targets `.NET Framework 4.7.2` as Legacy Non-SDK Class Library

`SAClassLibrary.csproj` uses classic MSBuild project format with `OutputType=Library` and `TargetFrameworkVersion=v4.7.2`.

---

## GAP 1049: SAClassLibrary Project File Enumerates Broad EF6 Model Surface Directly in Compile Include List

Project file contains explicit compile include entries for large entity/model/migration inventory (non-SDK explicit-file declaration style).

# Part 224: Gap Analysis - SAClassLibrary RailroadPosition / Bulletin / BulletinBid / PositionChange Notes

Gaps 1050-1054 capture additional verified details from:

- `SAClassLibrary/Models/RailroadPosition.cs`
- `SAClassLibrary/Models/RailroadPositionBulletin.cs`
- `SAClassLibrary/Models/RailroadPositionBulletinBid.cs`
- `SAClassLibrary/Models/RailroadPositionChange.cs`

---

## GAP 1050: SAClassLibrary RailroadPosition Uses Multiple `[NotMapped]` Derived Flags and Name/Pool/Craft Accessors

Entity derives `IsCrewPosition`, `IsRosterBoardPosition`, `IsTraineePosition`, `IsBulletined`, and board/craft/pool/roster identity helpers from linked navigation graph.

---

## GAP 1051: SAClassLibrary RailroadPositionChange Constructor Eager-Initializes Move/Bulletin and Notification Collections

`RailroadPositionChange` constructor initializes `ChangeMoveOrBulletins` and `ChangeNotifications` as `HashSet<>` collections.

---

## GAP 1052: SAClassLibrary RailroadPositionChange Uses Presence/Confirmation-Based `IsComplete` and `IsOpen` Computation

When notification is required, completion depends on existence of at least one confirmed change-notification row.

---

## GAP 1053: SAClassLibrary RailroadPositionBulletin Constructor Eager-Initializes Bid and BidAssignment Collections

Bulletin constructor initializes `RailroadPositionBulletinBids` and `RailroadPositionBulletinBidAssignments` collections.

---

## GAP 1054: SAClassLibrary RailroadPositionChange `CreateInstance(long,long)` Parameter Order Appears Inverted Relative to Private Constructor Signature

Factory method passes `(rposition, rpemployee)` into constructor expecting `(rpemployee, rrposition)`, implying positional argument inversion risk.

# Part 225: Gap Analysis - SAClassLibrary PositionChange-Info Bridge / Chanx Legacy Type / Package Metadata + SADailyCallSheet `SV_Crew`

Gaps 1055-1059 capture additional verified details from:

- `SAClassLibrary/Models/RailroadPositionChangeRailroadInformationRecord.cs`
- `SAClassLibrary/Models/RailroadPositionChanx.cs`
- `SAClassLibrary/packages.config`
- `SADailyCallSheetService/Models/SV_Crew.cs`

---

## GAP 1055: SAClassLibrary RailroadPositionChangeRailroadInformationRecord Uses PositionChange FK-as-PK One-to-One Link-Plus-Target Pattern

Entity keys on `RailroadPositionChangeControlNumber` and stores linked `RailroadInformationRecordControlNumber` with pair-seeding factory.

---

## GAP 1056: SAClassLibrary Includes Legacy-Named `RailroadPositionChanx` Entity Parallel to `RailroadPositionChange`

`RailroadPositionChanx` retains older naming shape and includes similar change payload and collection navigations.

---

## GAP 1057: SAClassLibrary `packages.config` Pins `EntityFramework 6.4.4` for `net472`

Package metadata currently includes EF6 only, targeting .NET Framework 4.7.2.

---

## GAP 1058: SADailyCallSheetService `SV_Crew` Wraps `Crew` and Resolves Assignment by Day-Name Match

`GetCrewAssignment(DateTime date)` selects crew assignment whose on-duty weekday name matches target date day-of-week string.

---

## GAP 1059: SADailyCallSheetService `SV_Crew` Uses Factory-Based Wrapper Construction

`CreateInstance(Crew crew)` returns lightweight service-view wrapper around a `Crew` domain entity.

# Part 226: Gap Analysis - SADailyCallSheetService `SV_RailroadEmployee` / `SV_RailroadPosition` / Project File / `DailyCrewPositionRecordService` Notes

Gaps 1060-1064 capture additional verified details from:

- `SADailyCallSheetService/Models/SV_RailroadEmployee.cs`
- `SADailyCallSheetService/Models/SV_RailroadPosition.cs`
- `SADailyCallSheetService/SADailyCallSheetService.csproj`
- `SADailyCallSheetService/Services/DailyCrewPositionRecordService.cs`

---

## GAP 1060: SADailyCallSheet `SV_RailroadEmployee` Wraps RailroadEmployee with Derived Position/Craft/Seniority Helpers

Wrapper exposes assigned/current position, active/last-active craft, active seniority, and compensation-account helper methods.

---

## GAP 1061: SADailyCallSheet `SV_RailroadPosition` Provides Oldest-Seniority-Move Selection + DailyPositionRecord Ensure/Create Flow

Wrapper includes ordered seniority-move selection and helper methods to retrieve/create daily railroad employee position records.

---

## GAP 1062: SADailyCallSheet Project File Targets `.NET Framework 4.7.2` as Legacy Windows Service Host (`WinExe`)

`SADailyCallSheetService.csproj` uses classic MSBuild schema, references EF6.4.4, and project-references `SAClassLibrary`.

---

## GAP 1063: SADailyCallSheet Project File Encodes Service Publish/Bootstrapper Metadata at csproj Layer

Project file includes publish path/install/bootstrapper settings and explicit compile include list for service and service-view model files.

---

## GAP 1064: `DailyCrewPositionRecordService` Service Class Is Skeleton with No Runtime Logic Beyond Generated Start/Stop Stubs

Current implementation contains placeholder `OnStart`/`OnStop` TODO bodies and constructor initialization only.

# Part 227: Gap Analysis - SADailyCallSheetService Designer Service Stubs (DailyCrewPositionRecord / Assignment / AssignmentShift / CallSheet)

Gaps 1065-1069 capture additional verified details from:

- `SADailyCallSheetService/Services/DailyCrewPositionRecordService.Designer.cs`
- `SADailyCallSheetService/Services/SADailyAssignmentService.Designer.cs`
- `SADailyCallSheetService/Services/SADailyAssignmentShiftService.Designer.cs`
- `SADailyCallSheetService/Services/SADailyCallSheetService.Designer.cs`

---

## GAP 1065: Reviewed SADailyCallSheetService Designer Files Follow Standard Windows Service Designer Pattern

Each file defines partial service class with designer-managed `IContainer` field and `Dispose(bool)` pattern.

---

## GAP 1066: `DailyCrewPositionRecordService` Designer Sets `ServiceName` to `DailyCrewPositionRecordService`

`InitializeComponent()` sets explicit service name for this host.

---

## GAP 1067: `SADailyAssignmentService` Designer Sets `ServiceName` to `SADailyAssignmentService`

`InitializeComponent()` sets explicit service name for this host.

---

## GAP 1068: `SADailyAssignmentShiftService` and `SADailyCallSheetService` Designers Set Matching Host Service Names

Each designer assigns service name literal matching class/service host identity.

---

## GAP 1069: Reviewed Designer Files Are Minimal Host Metadata Stubs Without Scheduling/Processing Logic

All four files only contain container/dispose/service-name wiring; operational logic exists in corresponding non-designer service classes.

# Part 228: Gap Analysis - SADailyCallSheetService `SADailyCrewPositionRecordService` + Additional Designer Stubs

Gaps 1070-1074 capture additional verified details from:

- `SADailyCallSheetService/Services/SADailyCrewPositionRecordService.Designer.cs`
- `SADailyCallSheetService/Services/SADailyCrewPositionRecordService.cs`
- `SADailyCallSheetService/Services/SADailyCrewPositionService.Designer.cs`
- `SADailyCallSheetService/Services/SADailyOnDutyMarkOffRecordService.Designer.cs`

---

## GAP 1070: `SADailyCrewPositionRecordService` Uses Delayed Timer Startup and Queue BeginPeek Pattern

Service waits `delay=60000ms`, then initializes MSMQ peek workflow for daily crew position processing queue.

---

## GAP 1071: `SADailyCrewPositionRecordService` Creates DailyCrewPosition Records from Queue Payload and Emits Follow-Up OnDuty Queue Messages

Service parses queue message fields, creates daily crew position rows, and enqueues create-on-duty messages through MSMQ utility calls.

---

## GAP 1072: `SADailyCrewPositionRecordService` Implements Domain-Specific OnDuty Candidate Selection Logic

Selection flow checks temporary assignments, hold-downs, training dates, and seniority-move will-work outcomes before sending on-duty creation message.

---

## GAP 1073: `SADailyCrewPositionService.Designer` and `SADailyOnDutyMarkOffRecordService.Designer` Are Minimal ServiceName Metadata Stubs

Both designer files only define container/dispose wiring and set explicit `ServiceName` values.

---

## GAP 1074: `SADailyCrewPositionRecordService.Designer` Mirrors Same Minimal Windows-Service Designer Pattern

Designer class sets `ServiceName="SADailyCrewPositionRecordService"` with no operational logic beyond generated host metadata.

# Part 229: Gap Analysis - SADailyCallSheet `SADailyOnDutyRecordService.Designer` + `FileUtilities` and SAImportPayroll Project/ADP Designer Notes

Gaps 1075-1079 capture additional verified details from:

- `SADailyCallSheetService/Services/SADailyOnDutyRecordService.Designer.cs`
- `SADailyCallSheetService/Utilities/FileUtilities.cs`
- `SAImportPayrollService/SAImportPayrollService.csproj`
- `SAImportPayrollService/Services/SAImportADPPayrollService.Designer.cs`

---

## GAP 1075: `SADailyOnDutyRecordService.Designer` Is Minimal Windows-Service Designer Stub

Designer contains standard container/dispose wiring and sets `ServiceName = "SADailyOnDutyRecordService"`.

---

## GAP 1076: `SADailyCallSheetService/Utilities/FileUtilities.cs` Declares `SAClassLibrary.Utilities` Namespace

Utility class resides under service project path but is namespaced into shared `SAClassLibrary.Utilities` namespace.

---

## GAP 1077: `FileUtilities` Implements File-Operation Lock Polling via `IsFileLocked` + `Thread.Sleep(100)` Loops

Move/copy/delete/read helpers repeatedly poll file lock state before IO operations.

---

## GAP 1078: `SAImportPayrollService.csproj` Targets `.NET Framework 4.7.2` as Legacy Windows Service Host (`WinExe`) with EF6.4.4

Project uses classic MSBuild format, references EF6, and project-references `SAClassLibrary`.

---

## GAP 1079: `SAImportADPPayrollService.Designer` Is Minimal ServiceName Metadata Stub

Designer class uses standard service designer pattern and sets `ServiceName = "SAImportADPPayrollService"`.

# Part 230: Gap Analysis - SADailyCallSheet OnDuty Designer + FileUtilities + SAImportPayroll Project + StrategicApplications Package Inventory Notes

Gaps 1080-1084 capture additional verified details from:

- `SADailyCallSheetService/Services/SADailyOnDutyRecordService.Designer.cs`
- `SADailyCallSheetService/Utilities/FileUtilities.cs`
- `SAImportPayrollService/SAImportPayrollService.csproj`
- `StrategicApplications/packages.config`

---

## GAP 1080: `SADailyOnDutyRecordService.Designer` Is Minimal ServiceName Metadata Stub

Designer class only defines container/dispose pattern and sets `ServiceName = "SADailyOnDutyRecordService"`.

---

## GAP 1081: `FileUtilities.CopyFile(...)` Deletes Existing Target Path Before Moving New File into Place

Method lock-polls existing path, deletes existing file if present, then moves replacement file to target location.

---

## GAP 1082: `SAImportPayrollService.csproj` Uses Legacy WinExe Service Host Pattern with EF6 + SAClassLibrary Project Reference

Project targets `v4.7.2`, references EntityFramework 6.4.4, and includes ADP/UKG service components.

---

## GAP 1083: `StrategicApplications/packages.config` Contains Broad Legacy Package Surface Across ASP.NET MVC/OWIN/EF6/UI/PDF/Interop Domains

Package list includes MVC5 stack, OWIN identity, EF6.4.4, Bootstrap/jQuery, iText7, and numerous compatibility/runtime support packages.

---

## GAP 1084: StrategicApplications Dependency Set Includes Mixed-Era UI Stack Packages (Bootstrap 4 + Bootstrap 3/Legacy Assets)

Package inventory shows parallel references across newer and legacy frontend package lines, indicating layered historical dependency evolution.

# Part 231: Gap Analysis - SAImport UKG Designer + StrategicApplications Scripts README / Project File / Views Web.config Notes

Gaps 1085-1089 capture additional verified details from:

- `SAImportPayrollService/Services/SAImportUKGPayrollService.Designer.cs`
- `StrategicApplications/Scripts/README.md`
- `StrategicApplications/StrategicApplications.csproj`
- `StrategicApplications/Views/Web.config`

---

## GAP 1085: `SAImportUKGPayrollService.Designer` Is Minimal ServiceName Metadata Stub

Designer class follows standard service designer pattern and sets `ServiceName = "SAImportUKGPayrollService"`.

---

## GAP 1086: `StrategicApplications/Scripts/README.md` Is Upstream Popper.js Third-Party Documentation Artifact

Script-folder README content reflects embedded third-party package documentation rather than project-authored application documentation.

---

## GAP 1087: `StrategicApplications.csproj` Uses Legacy ASP.NET MVC Web Project Schema (`ToolsVersion=12.0`) Targeting `.NET Framework 4.7.2`

Project file is classic non-SDK ASP.NET MVC format with IIS Express and TypeScript tooling metadata.

---

## GAP 1088: `StrategicApplications.csproj` Maintains Broad Explicit Assembly Reference Inventory Across Legacy + Compatibility Libraries

Project references include MVC/OWIN/EF6 stack, iText7 components, PowerShell reference assemblies, and extensive compatibility/runtime packages.

---

## GAP 1089: `StrategicApplications/Views/Web.config` Enforces Razor View Runtime Setup and Blocks Direct View Requests

View-level config sets MVC Razor host/namespaces and configures `BlockViewHandler` to return not-found for direct view path access.

# Part 232: Gap Analysis - StrategicApplications Web Transform Files + IIS Express `applicationhost.config` Files

Gaps 1090-1094 capture additional verified details from:

- `StrategicApplications/Web.Debug.config`
- `StrategicApplications/Web.Release.config`
- `.vs/StrategicApplications/config/applicationhost.config`
- `.vs/config/applicationhost.config`

---

## GAP 1090: StrategicApplications Web Transform Files Are Mostly Template Defaults with Minimal Active Transform Rules

`Web.Debug.config` remains template-comment driven; `Web.Release.config` includes explicit compilation debug-attribute removal transform.

---

## GAP 1091: StrategicApplications `Web.Release.config` Applies `xdt:Transform="RemoveAttributes(debug)"` on `<compilation>`

Release transform currently focuses on removing debug attribute from compilation section.

---

## GAP 1092: Repository Tracks Two Separate IIS Express `applicationhost.config` Files under `.vs` with Similar Baseline Structure

Both files include broad IIS Express section registrations, application pool defaults, and site definitions.

---

## GAP 1093: `.vs` `applicationhost.config` Files Contain Local Developer Site Bindings and Physical Paths (Environment-Specific Artifacts)

Tracked configs include local absolute paths and localhost binding ports, indicating machine/environment-specific IIS Express metadata.

---

## GAP 1094: Both `.vs` `applicationhost.config` Variants Include Legacy/Additional Site Entries Beyond Main StrategicApplications Site

Each file lists multiple site definitions (including historical service sites) under `system.applicationHost/sites`.

# Part 233: Gap Analysis - Package Analyzer Documentation Artifacts (CodeAnalysis/FxCop 2.9.x)

Gaps 1095-1099 capture additional verified details from:

- `packages/Microsoft.CodeAnalysis.Analyzers.2.9.4/documentation/Microsoft.CodeAnalysis.Analyzers.md`
- `packages/Microsoft.CodeAnalysis.FxCopAnalyzers.2.9.6/documentation/Analyzer Configuration.md`
- `packages/Microsoft.CodeAnalysis.FxCopAnalyzers.2.9.6/documentation/Microsoft.CodeAnalysis.FxCopAnalyzers.md`
- `packages/Microsoft.CodeAnalysis.FxCopAnalyzers.2.9.8/documentation/Analyzer Configuration.md`

---

## GAP 1095: Repository Tracks Analyzer Rule Catalog Markdown for `Microsoft.CodeAnalysis.Analyzers 2.9.4`

Catalog file enumerates RS rule IDs and metadata (category/enabled/codefix/description), indicating package documentation artifacts are committed.

---

## GAP 1096: Repository Tracks FxCop Analyzer Configuration Guidance Markdown for Version `2.9.6`

Documentation includes `.editorconfig` option schema (`dotnet_code_quality.*`) and supported option sections.

---

## GAP 1097: Repository Tracks FxCop Analyzer Rule Catalog Markdown for Version `2.9.6`

Catalog file lists broad CA rule inventory with links, defaults, and descriptions across design/performance/security/usage domains.

---

## GAP 1098: Repository Also Tracks FxCop Analyzer Configuration Guidance Markdown for Version `2.9.8`

Versioned analyzer-config documentation appears alongside 2.9.6 variant, suggesting multiple package-version docs coexist in repository.

---

## GAP 1099: Remaining Sweep Includes Primarily Package Documentation/Tooling Config Artifacts

Current unreviewed set is now dominated by package-level docs and compiler/analyzer config files under `packages/*`.

# Part 234: Gap Analysis - Package Analyzer Documentation Artifacts (FxCop 2.9.8 + VersionCheck 2.9.x)

Gaps 1100-1104 capture additional verified details from:

- `packages/Microsoft.CodeAnalysis.FxCopAnalyzers.2.9.8/documentation/Microsoft.CodeAnalysis.FxCopAnalyzers.md`
- `packages/Microsoft.CodeAnalysis.VersionCheckAnalyzer.2.9.6/documentation/Analyzer Configuration.md`
- `packages/Microsoft.CodeAnalysis.VersionCheckAnalyzer.2.9.6/documentation/Microsoft.CodeAnalysis.VersionCheckAnalyzer.md`
- `packages/Microsoft.CodeAnalysis.VersionCheckAnalyzer.2.9.8/documentation/Analyzer Configuration.md`

---

## GAP 1100: Repository Tracks FxCop Analyzer Rule Catalog Markdown for Version `2.9.8`

Rule catalog documents CA rule metadata in table form, mirroring 2.9.x package-version documentation artifact strategy.

---

## GAP 1101: Repository Tracks VersionCheckAnalyzer `.editorconfig` Guidance Documentation (2.9.6)

Configuration markdown mirrors analyzer package guidance for `dotnet_code_quality.*` option formats and examples.

---

## GAP 1102: Repository Tracks VersionCheckAnalyzer Rule Catalog with Single `CA9999` Version-Mismatch Rule (2.9.6)

Catalog indicates package’s primary concern is analyzer host/version compatibility verification.

---

## GAP 1103: Repository Also Tracks VersionCheckAnalyzer `.editorconfig` Guidance Documentation (2.9.8)

Multiple versioned copies of near-identical analyzer configuration docs are committed in package subtree.

---

## GAP 1104: Remaining Unreviewed Files Continue to Be Dominated by `packages/*` Tooling/Documentation Config Artifacts

Sweep focus has shifted from solution source to package-managed documentation and compiler/analyzer config payload files.

# Part 235: Gap Analysis - Package VersionCheck 2.9.8 Catalog + Roslyn45 Compiler Config Artifacts

Gaps 1105-1109 capture additional verified details from:

- `packages/Microsoft.CodeAnalysis.VersionCheckAnalyzer.2.9.8/documentation/Microsoft.CodeAnalysis.VersionCheckAnalyzer.md`
- `packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform.2.0.1/tools/Roslyn45/VBCSCompiler.exe.config`
- `packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform.2.0.1/tools/Roslyn45/csc.exe.config`
- `packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform.2.0.1/tools/Roslyn45/vbc.exe.config`

---

## GAP 1105: Repository Tracks VersionCheckAnalyzer 2.9.8 Rule Catalog with Single `CA9999` Entry

VersionCheck analyzer catalog continues single-rule model focused on analyzer host/version mismatch detection.

---

## GAP 1106: Repository Tracks Roslyn45 `VBCSCompiler.exe.config` with Explicit Assembly Binding Redirects and Keepalive Setting

Config sets server GC options, redirects Roslyn dependency versions, and includes compiler server keepalive app setting.

---

## GAP 1107: Repository Tracks Roslyn45 `csc.exe.config` with Roslyn Dependency Binding Redirects

C# compiler config includes runtime GC settings and binding redirects for CodeAnalysis/Immutable/Metadata assemblies.

---

## GAP 1108: Repository Tracks Roslyn45 `vbc.exe.config` with Roslyn Dependency Binding Redirects

VB compiler config mirrors compiler runtime/binding redirect pattern used by Roslyn45 `csc.exe.config`.

---

## GAP 1109: Remaining Sweep Scope Continues Into Package Toolchain Config Variants (RoslynLatest, CodeQuality, NetCore, NetFramework)

Unreviewed set is now largely compiler/analyzer package metadata and documentation subtrees.

# Part 236: Gap Analysis - Package RoslynLatest Compiler Config Artifacts

Gaps 1110-1114 capture additional verified details from:

- `packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform.2.0.1/tools/RoslynLatest/VBCSCompiler.exe.config`
- `packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform.2.0.1/tools/RoslynLatest/csc.exe.config`
- `packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform.2.0.1/tools/RoslynLatest/csi.exe.config`
- `packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform.2.0.1/tools/RoslynLatest/vbc.exe.config`

---

## GAP 1110: Repository Tracks RoslynLatest Compiler Runtime Config Artifacts under DotNetCompilerPlatform Package

Tracked package includes per-tool config files for VBCS compiler server, C# compiler, C# interactive host, and VB compiler.

---

## GAP 1111: RoslynLatest Configs Pin CodeAnalysis Binding Redirects to `2.9.0.0`

Compiler configs redirect `Microsoft.CodeAnalysis*` assemblies to version `2.9.0.0` within runtime binding sections.

---

## GAP 1112: RoslynLatest Configs Include Broad .NET Runtime Dependency Binding Redirect Matrix

Configs define redirects for immutable collections, metadata, IO/compression, crypto, threading, XML reader/writer, and related support assemblies.

---

## GAP 1113: RoslynLatest `VBCSCompiler.exe.config` Includes Keepalive AppSetting for Compiler Server Lifetime

Compiler server config sets `keepalive=600` seconds as idle timeout control.

---

## GAP 1114: RoslynLatest Config Files Target .NET Framework Runtime Startup Profile (`v4.0`, sku `.NETFramework,Version=v4.6`)

All reviewed RoslynLatest tool configs declare shared runtime startup profile for packaged compiler tooling execution.

# Part 237: Gap Analysis - Package CodeQuality Analyzer Documentation Artifacts (2.9.6/2.9.8)

Gaps 1115-1119 capture additional verified details from:

- `packages/Microsoft.CodeQuality.Analyzers.2.9.6/documentation/Analyzer Configuration.md`
- `packages/Microsoft.CodeQuality.Analyzers.2.9.6/documentation/Microsoft.CodeQuality.Analyzers.md`
- `packages/Microsoft.CodeQuality.Analyzers.2.9.8/documentation/Analyzer Configuration.md`
- `packages/Microsoft.CodeQuality.Analyzers.2.9.8/documentation/Microsoft.CodeQuality.Analyzers.md`

---

## GAP 1115: Repository Tracks CodeQuality Analyzer `.editorconfig` Configuration Guidance for Version `2.9.6`

Documentation describes `dotnet_code_quality.*` general/specific option formats and option catalog semantics.

---

## GAP 1116: Repository Tracks CodeQuality Analyzer Rule Catalog Markdown for Version `2.9.6`

Catalog contains CA rule inventory/metadata spanning design, maintainability, naming, usage, reliability, and security categories.

---

## GAP 1117: Repository Also Tracks CodeQuality Analyzer `.editorconfig` Guidance for Version `2.9.8`

Near-parallel versioned documentation indicates duplicated analyzer guidance artifacts per package version.

---

## GAP 1118: Repository Also Tracks CodeQuality Analyzer Rule Catalog Markdown for Version `2.9.8`

Rule catalog mirrors 2.9.6 style with updated package-version context while preserving broad CA table format.

---

## GAP 1119: Package Subtree Sweep Continues Through Versioned Analyzer Documentation Families (CodeQuality/NetCore/NetFramework)

Unreviewed artifacts remain concentrated in analyzer-doc package directories plus ancillary compiler/tool configs.

# Part 238: Gap Analysis - Package Net Compilers + NetCore Analyzer Documentation Artifacts

Gaps 1120-1124 capture additional verified details from:

- `packages/Microsoft.Net.Compilers.1.0.0/tools/VBCSCompiler.exe.config`
- `packages/Microsoft.NetCore.Analyzers.2.9.6/documentation/Analyzer Configuration.md`
- `packages/Microsoft.NetCore.Analyzers.2.9.6/documentation/Microsoft.NetCore.Analyzers.md`
- `packages/Microsoft.NetCore.Analyzers.2.9.8/documentation/Analyzer Configuration.md`

---

## GAP 1120: Repository Tracks Legacy `Microsoft.Net.Compilers 1.0.0` VBCS Compiler Server Config Artifact

Config sets server/concurrent GC behavior and compiler server keepalive timeout (`600` seconds).

---

## GAP 1121: Repository Tracks NetCore Analyzer `.editorconfig` Guidance Documentation for Version `2.9.6`

Documentation mirrors common analyzer configuration schema for `dotnet_code_quality.*` options.

---

## GAP 1122: Repository Tracks NetCore Analyzer Rule Catalog Markdown for Version `2.9.6`

Catalog emphasizes globalization/interoperability/performance/reliability/security CA rules relevant to .NET Core analyzer package scope.

---

## GAP 1123: Repository Also Tracks NetCore Analyzer `.editorconfig` Guidance Documentation for Version `2.9.8`

Versioned analyzer-configuration docs continue 2.9.x parallel artifact pattern.

---

## GAP 1124: Remaining Unreviewed Artifacts Continue Across NetCore/NetFramework Analyzer Docs and Third-Party Package Source/License Files

Sweep is now primarily package-asset metadata/documentation/source rather than solution-authored runtime code.

# Part 239: Gap Analysis - Package NetCore 2.9.8 Catalog + NetFramework Analyzer Documentation Artifacts

Gaps 1125-1129 capture additional verified details from:

- `packages/Microsoft.NetCore.Analyzers.2.9.8/documentation/Microsoft.NetCore.Analyzers.md`
- `packages/Microsoft.NetFramework.Analyzers.2.9.6/documentation/Analyzer Configuration.md`
- `packages/Microsoft.NetFramework.Analyzers.2.9.6/documentation/Microsoft.NetFramework.Analyzers.md`
- `packages/Microsoft.NetFramework.Analyzers.2.9.8/documentation/Analyzer Configuration.md`

---

## GAP 1125: Repository Tracks NetCore Analyzer Rule Catalog Markdown for Version `2.9.8`

Catalog continues .NET Core-focused CA rule metadata coverage across globalization, reliability, performance, and security areas.

---

## GAP 1126: Repository Tracks NetFramework Analyzer `.editorconfig` Guidance for Version `2.9.6`

Configuration guidance file mirrors shared analyzer option schema (`dotnet_code_quality.*`) and configuration patterns.

---

## GAP 1127: Repository Tracks NetFramework Analyzer Rule Catalog Markdown for Version `2.9.6`

Catalog includes targeted .NET Framework-specific security/design rules (e.g., XML processing, antiforgery validation, CSE handling).

---

## GAP 1128: Repository Also Tracks NetFramework Analyzer `.editorconfig` Guidance for Version `2.9.8`

Versioned documentation duplicates analyzer-config reference structure across package revisions.

---

## GAP 1129: Remaining Unreviewed Files Are Largely Package Licenses and Third-Party Source Trees (System.Net.FtpClient)

Final sweep is predominantly external package payload files rather than first-party application artifacts.

# Part 240: Gap Analysis - NetFramework Analyzer 2.9.8 Catalog + Newtonsoft License Artifacts + System.Net.FtpClient Assembly Metadata

Gaps 1130-1134 capture additional verified details from:

- `packages/Microsoft.NetFramework.Analyzers.2.9.8/documentation/Microsoft.NetFramework.Analyzers.md`
- `packages/Newtonsoft.Json.12.0.2/LICENSE.md`
- `packages/Newtonsoft.Json.13.0.1/LICENSE.md`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/AssemblyInfo.cs`

---

## GAP 1130: Repository Tracks NetFramework Analyzer Rule Catalog Markdown for Version `2.9.8`

Catalog remains focused on a compact .NET Framework-specific security/design rule subset (XML/antiforgery/CSE-related guidance).

---

## GAP 1131: Repository Tracks Multiple Versioned Newtonsoft.Json License Files (`12.0.2`, `13.0.1`)

Both package directories include MIT license text as committed package artifact payload.

---

## GAP 1132: Newtonsoft.Json 12.x and 13.x License Text Content Is Equivalent MIT License Body

Reviewed license files contain matching permission/disclaimer text and copyright attribution.

---

## GAP 1133: Repository Tracks Third-Party `System.Net.FtpClient` Source Tree Including Assembly Metadata File

`AssemblyInfo.cs` identifies package as `System.Net.FtpClient` with FTP/FTPS client description metadata.

---

## GAP 1134: `System.Net.FtpClient` Assembly Metadata Uses Auto-Incrementing `AssemblyVersion("1.0.*")` Pattern

Third-party source artifact retains wildcard versioning style and fixed assembly GUID/resource-language attributes.

# Part 241: Gap Analysis - System.Net.FtpClient Checksum Extension Source Artifacts (GetChecksum/MD5/XCRC/XMD5)

Gaps 1135-1139 capture additional verified details from:

- `packages/System.Net.FtpClient.1.0.5824.34026/source/Extensions/GetChecksum.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/Extensions/MD5.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/Extensions/XCRC.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/Extensions/XMD5.cs`

---

## GAP 1135: System.Net.FtpClient Package Source Includes Checksum Extension Orchestrator with Capability-Based Algorithm Fallback Chain

`GetChecksum` probes `HASH` first, then MD5/XMD5/XSHA1/XSHA256/XSHA512/XCRC based on advertised server capabilities.

---

## GAP 1136: System.Net.FtpClient Extension Source Uses Legacy APM Begin/End Async Pattern with Static IAsyncResult->Delegate Dictionaries

Reviewed checksum-related extensions maintain async delegate maps (`m_asyncmethods`) for Begin/End method pairs.

---

## GAP 1137: `MD5.cs` Implements Non-Standard FTP `MD5` Command and Normalizes Reply by Trimming Leading Path Prefix

Method executes `MD5 {path}` and, when response starts with file path, trims the prefix before returning hash text.

---

## GAP 1138: `XCRC.cs` and `XMD5.cs` Implement Non-Standard FTP Commands with Direct Reply Message Return

Both execute command and throw `FtpCommandException` on failure; successful response returns raw reply message payload.

---

## GAP 1139: Remaining Unreviewed System.Net.FtpClient Source Sweep Continues Through Core Client/DataStream/Enums/Interfaces and Additional Hash Extensions

Outstanding package source files are concentrated in broader FTP client implementation surface under same third-party package tree.

# Part 242: Gap Analysis - System.Net.FtpClient SHA Extension Source + Core FtpClient Class Snapshot

Gaps 1140-1144 capture additional verified details from:

- `packages/System.Net.FtpClient.1.0.5824.34026/source/Extensions/XSHA1.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/Extensions/XSHA256.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/Extensions/XSHA512.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpClient.cs`

---

## GAP 1140: System.Net.FtpClient Package Source Includes Non-Standard SHA Hash Extensions (`XSHA1`, `XSHA256`, `XSHA512`)

Each extension executes corresponding `XSHA*` FTP command and throws `FtpCommandException` on unsuccessful reply.

---

## GAP 1141: SHA Extension Files Reuse Legacy Begin/End Asynchronous Delegate Mapping Pattern

All three SHA extension classes maintain static `IAsyncResult` -> delegate dictionaries for async lifecycle tracking.

---

## GAP 1142: `FtpClient` Class Implements Broad Configurable Control-Connection Surface with Clone-Aware Thread-Safe Data Connection Model

Class includes lock-based synchronization, async method tracking, clone semantics, and extensive connection behavior properties.

---

## GAP 1143: `FtpClient` Defaults Include `Encoding.ASCII`, `DataConnectionType.AutoPassive`, and Port Auto-Selection (21/990 by Encryption Mode)

Class-level defaults and property logic establish baseline FTP/FTPS behavior and connection negotiation strategy.

---

## GAP 1144: `FtpClient` Includes Azure-Focused Stale Data Check Toggle and Extensive XML Documentation Examples

`StaleDataCheck` property docs reference Azure behavior workaround; class docs embed numerous usage example links.

# Part 243: Gap Analysis - System.Net.FtpClient DataStream/Enums/Exceptions/ExtensionAttribute Source Artifacts

Gaps 1145-1149 capture additional verified details from:

- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpDataStream.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpEnums.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpExceptions.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpExtensionAttribute.cs`

---

## GAP 1145: `FtpDataStream` Extends `FtpSocketStream` and Couples Data Channel Lifecycle Back to Control Connection

On close/dispose it routes completion handling through `ControlConnection.CloseDataStream(this)` and tracks command status/position.

---

## GAP 1146: `FtpDataStream` Constructor Auto-Accepts Certificate Validation for Cloned Data Connections

Data stream constructor registers certificate validation callback that forces `e.Accept = true` for cloned stream connections.

---

## GAP 1147: `FtpEnums.cs` Defines Broad FTP Capability/Connection/Permission/Protocol Enum Surface

File centralizes protocol enums including encryption modes, response types, capability flags, hash algorithms, IP versions, data connection types, and permission flags.

---

## GAP 1148: `FtpExceptions.cs` Provides Specialized FTP Exception Hierarchy with Reply-Code-Aware `FtpCommandException`

Command exception interprets completion-code prefix into transient/permanent negative response classifications.

---

## GAP 1149: `FtpExtensionAttribute.cs` Contains Conditional .NET 2.0 Compatibility Shim for Extension Methods

`#if NET2` block defines `System.Runtime.CompilerServices.ExtensionAttribute` to support extension syntax on older framework target.

# Part 244: Gap Analysis - System.Net.FtpClient Extensions/Hash/ListItem/Reply Source Artifacts

Gaps 1150-1154 capture additional verified details from:

- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpExtensions.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpHash.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpListItem.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpReply.cs`

---

## GAP 1150: `FtpExtensions.cs` Provides Path and Date Normalization Helpers Used Across FTP Parsing/Operations

Includes `GetFtpPath`, path-segment append, directory/file extraction helpers, and multi-format FTP date parsing utility.

---

## GAP 1151: `FtpHash` Encapsulates Server-Returned Hash Metadata and Stream/File Verification Logic with Algorithm Dispatch

Supports SHA1/SHA256/SHA512/MD5 verification (CRC explicitly not implemented) and case-insensitive computed-vs-server hash comparison.

---

## GAP 1152: `FtpListItem` Is Rich Mutable Listing Record with Parser-Driven Population and Link Resolution Support

Entity carries type/name/path/link/date/size/permission fields and static parsing pipeline over configurable parser collection.

---

## GAP 1153: `FtpReply` Is Struct-Based Reply Model with Computed Type/Success/ErrorMessage Semantics

Reply interprets first status-code digit for response classification and composes error message from info-message stream + final message.

---

## GAP 1154: System.Net.FtpClient Source Surface Remaining Includes Socket/Trace/Interface/Project Definitions and iText License Artifacts

Unreviewed files now consist of remaining FTP core plumbing classes/interfaces/project files plus trailing package license documents.

# Part 245: Gap Analysis - System.Net.FtpClient Socket/Trace/Interface Source Artifacts

Gaps 1155-1159 capture additional verified details from:

- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpSocketStream.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/FtpTrace.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/IFtpClient.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/IFtpListItem.cs`

---

## GAP 1155: `FtpSocketStream` Implements Core Stream/Socket/SSL Transport Layer with Poll-Based Connectivity Checking

Class combines raw socket, network stream, optional SSL stream, and periodic `Socket.Poll()` connectivity validation behavior.

---

## GAP 1156: `FtpSocketStream` Exposes Certificate Validation Event Pipeline via `FtpSocketStreamSslValidation`

Validation args include cert/chain/policy-errors plus mutable `Accept` flag that drives authentication acceptance outcome.

---

## GAP 1157: `FtpTrace` Provides Static Listener-Based Logging Abstraction with Optional Flush-on-Write Behavior

Trace helper supports add/remove listeners and debug-aware write/writeline dispatch for FTP transaction diagnostics.

---

## GAP 1158: `IFtpClient` Defines Large Public Contract Surface Mirroring Full FtpClient Control Connection Feature Set

Interface exposes broad property/method/event contract for connectivity, capabilities, execution, data transfer, and async operations.

---

## GAP 1159: `IFtpListItem` Defines Mutable Listing Record Contract Aligned with FtpListItem Parser-Oriented Property Model

Interface includes path/name/link/date/size/permissions/input properties matching list-parsing result object semantics.

# Part 246: Gap Analysis - System.Net.FtpClient Reply Interface/Project Files + iText7 License Artifact

Gaps 1160-1164 capture additional verified details from:

- `packages/System.Net.FtpClient.1.0.5824.34026/source/IFtpReply.cs`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/System.Net.FtpClient.NET2.csproj`
- `packages/System.Net.FtpClient.1.0.5824.34026/source/System.Net.FtpClient.csproj`
- `packages/itext7.7.1.10/LICENSE.md`

---

## GAP 1160: `IFtpReply` Defines Reply Contract Used by Struct Implementation (`FtpReply`)

Interface models response type/code/message/info/success/error-message properties for server command responses.

---

## GAP 1161: System.Net.FtpClient Package Includes Separate Legacy `.NET 2.0` Project File (`System.Net.FtpClient.NET2.csproj`)

NET2 project targets `v2.0`, defines `NET2` symbol, and includes compatibility source file set.

---

## GAP 1162: System.Net.FtpClient Package Also Includes Non-NET2 Project File (`System.Net.FtpClient.csproj`) with Similar Compile Surface

Primary project defines library output with overlapping compile inventory and includes package nuspec file as non-compile artifact.

---

## GAP 1163: Repository Tracks iText7 Package License Artifact Indicating Commercial-or-AGPL Dual Licensing Model

`LICENSE.md` states commercial licensing contact and AGPL fallback terms with GNU license reference.

---

## GAP 1164: Remaining Sweep Scope Is Final Package License Documentation and a Small Set of System.Net.FtpClient + Popper Package Artifacts

Outstanding files are now concentrated in third-party package metadata/license/readme payloads.

# Part 247: Gap Analysis - iText7 License/AGPL Artifacts Across Versions (`7.1.10`, `7.1.14`, `7.1.7`)

Gaps 1165-1169 capture additional verified details from:

- `packages/itext7.7.1.10/gnu-agpl-v3.0.md`
- `packages/itext7.7.1.14/LICENSE.md`
- `packages/itext7.7.1.14/gnu-agpl-v3.0.md`
- `packages/itext7.7.1.7/LICENSE.md`

---

## GAP 1165: Repository Tracks Full AGPL v3 License Text Artifacts Within iText7 Package Directories

`gnu-agpl-v3.0.md` files include extensive GNU Affero GPL legal text payload as packaged documentation artifacts.

---

## GAP 1166: iText7 `7.1.14` License File States Commercial-or-AGPL Dual Licensing Model

`LICENSE.md` indicates commercial licensing contact path with AGPL terms as alternative licensing route.

---

## GAP 1167: iText7 `7.1.7` License Text Reflects Earlier AGPL Notice Style with Additional Warranty/Producer-Line Clauses

Older version license includes explicit Section 7 additions and producer-line retention language.

---

## GAP 1168: iText7 Versioned Package Directories Contain Non-Uniform License Wording Across Releases

Reviewed license artifacts show wording/format differences between 7.1.7 and later 7.1.10/7.1.14 package versions.

---

## GAP 1169: Remaining Unreviewed Files Are Predominantly Additional iText/pdfhtml License Docs, Popper README Artifacts, and `packages/repositories.config`

Final remaining sweep consists of package metadata/legal/readme files.

# Part 248: Gap Analysis - iText7 License/AGPL Artifacts Across Additional Versions (`7.1.7`, `7.1.8`, `7.1.9`)

Gaps 1170-1174 capture additional verified details from:

- `packages/itext7.7.1.7/gnu-agpl-v3.0.md`
- `packages/itext7.7.1.8/LICENSE.md`
- `packages/itext7.7.1.8/gnu-agpl-v3.0.md`
- `packages/itext7.7.1.9/LICENSE.md`

---

## GAP 1170: Repository Tracks Full AGPL v3 Legal Text Artifact for iText7 `7.1.7`

`gnu-agpl-v3.0.md` payload contains full GNU Affero GPL v3 legal document text.

---

## GAP 1171: iText7 `7.1.8` License File Uses Older AGPL Notice Style with Additional Section-7 Clauses and Commercial Release Language

License text includes added warranty/producer-line provisions and commercial-license release messaging.

---

## GAP 1172: Repository Tracks Matching AGPL Full-Text Artifact for iText7 `7.1.8`

Version directory includes complete AGPL text file in addition to short-form license notice.

---

## GAP 1173: iText7 `7.1.9` License File Uses Commercial-or-AGPL Dual-Licensing Notice Format

`LICENSE.md` states commercial sales contact with AGPL alternative terms.

---

## GAP 1174: iText7 Package License Artifacts Vary by Version in Wording Style While Retaining AGPL/Commercial Themes

Reviewed versions demonstrate mixed short-form notice phrasing but consistent AGPL-based/open-source plus commercial licensing framing.

# Part 249: Gap Analysis - iText7 `7.1.9` AGPL Text + iText7.pdfhtml License/AGPL Artifacts (`2.1.7`, `3.0.3`)

Gaps 1175-1179 capture additional verified details from:

- `packages/itext7.7.1.9/gnu-agpl-v3.0.md`
- `packages/itext7.pdfhtml.2.1.7/LICENSE.md`
- `packages/itext7.pdfhtml.2.1.7/gnu-agpl-v3.0.md`
- `packages/itext7.pdfhtml.3.0.3/LICENSE.md`

---

## GAP 1175: Repository Tracks Full AGPL v3 Legal Text Artifact for iText7 `7.1.9`

`gnu-agpl-v3.0.md` contains full GNU Affero GPL v3 text as package documentation payload.

---

## GAP 1176: `itext7.pdfhtml 2.1.7` License File Uses Commercial-or-AGPL Dual Licensing Notice Format

`LICENSE.md` states commercial sales contact path and AGPL alternative terms.

---

## GAP 1177: `itext7.pdfhtml 2.1.7` Package Also Includes Full AGPL v3 Text Artifact

`gnu-agpl-v3.0.md` mirrors AGPL full-text legal document structure seen in sibling iText package trees.

---

## GAP 1178: `itext7.pdfhtml 3.0.3` License File Continues Commercial-or-AGPL Dual Licensing Notice Pattern

Reviewed license wording aligns with newer iText package short-form dual-license notice style.

---

## GAP 1179: Final Remaining Unreviewed Files Are Popper Package README Artifacts and One `itext7.pdfhtml` AGPL Text File

End-state sweep now has only a small set of package readme/legal artifacts pending.

# Part 250: Gap Analysis - Final Package Legal/README Artifacts (`itext7.pdfhtml 3.0.3` AGPL + Popper `README.md` Variants)

Gaps 1180-1184 capture additional verified details from:

- `packages/itext7.pdfhtml.3.0.3/gnu-agpl-v3.0.md`
- `packages/popper.js.1.14.3/content/Scripts/README.md`
- `packages/popper.js.1.16.0/content/Scripts/README.md`
- `packages/popper.js.1.16.1/content/Scripts/README.md`

---

## GAP 1180: Repository Tracks Full AGPL v3 Text Artifact for `itext7.pdfhtml 3.0.3`

`gnu-agpl-v3.0.md` carries complete GNU Affero GPL legal document within package directory.

---

## GAP 1181: Repository Tracks Multiple Versioned Popper Package README Artifacts Under `packages/popper.js.*`

Each package version includes large upstream Popper.js README content in `content/Scripts/README.md`.

---

## GAP 1182: Popper README Artifacts Are Third-Party Upstream Documentation, Not Solution-Authored Application Documentation

Files primarily contain Popper project marketing/usage/install/docs references and badge blocks.

---

## GAP 1183: Popper README Wording/Badge Set Varies Across Versions (`1.14.3` vs `1.16.0`/`1.16.1`) While Keeping Same Core Narrative

Versioned readmes show minor badge/content formatting differences with substantially similar conceptual documentation body.

---

## GAP 1184: Full Sweep Coverage Reached for Tracked `*.cs`, `*.md`, `*.csproj`, `*.config` Files in Repository

All files matching sweep filter are now referenced in the specification gap-analysis series.

