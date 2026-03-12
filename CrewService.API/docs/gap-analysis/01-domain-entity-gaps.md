# 01 – Domain Entity Gaps

## Overview

The legacy SA system has **204 DbSets** in `StrategicApplicationsContext` and **215 DbSets** in `SAClassLibraryContext`.
CrewService currently has entities spread across 8 domain modules + core models. This document catalogs what exists,
what is partially covered, and what is missing entirely.

## ✅ Fully Mapped (entity exists with equivalent intent)

These SA entities have a direct or abstracted equivalent in CrewService:

| SA Entity | CrewService Equivalent | Notes |
|-----------|----------------------|-------|
| `Employee` | `Employee` | Core fields present; some SA-specific fields (EmpNbr format, etc.) may need extension |
| `Address` | `Address` | ✅ |
| `EmailAddress` | `EmailAddress` | ✅ |
| `PhoneNumber` | `PhoneNumber` | ✅ |
| `Railroad` | `Railroad` | ✅ |
| `Craft` | `Craft` | ✅ |
| `Seniority` | `Seniority` | ✅ |
| `SeniorityState` | `SeniorityState` | ✅ |
| `Roster` | `Roster` | ✅ |
| `EmploymentStatus` | `EmploymentStatus` | ✅ |
| `EmploymentStatusHistory` | `EmploymentStatusHistory` | ✅ |
| `EmployeePriorServiceCredit` | `EmployeePriorServiceCredit` | ✅ |
| `PayrollTier` (via `RailroadPoolPayrollTier`) | `PayrollTier` | ✅ |
| `Invitation` (new) | `Invitation` | ✅ New concept not in SA |
| `UserParentAssignment` (new) | `UserParentAssignment` | ✅ New concept not in SA |
| `Parent` (new) | `Parent` | ✅ Multi-tenant concept not in SA |

## ⚠️ Partially Mapped (concept exists but simplified or incomplete)

| SA Entity Group | CrewService Coverage | Gap Description |
|-----------------|---------------------|-----------------|
| `Crew`, `CrewAssignment`, `CrewOffDay`, `CrewAbolishment` | `Crew`, `CrewPosition`, `CrewIncumbency`, `CrewAttachmentTemplate/Instance` | Missing: `CrewOffDay` equivalent, `CrewAbolishment` workflow |
| `Assignment`, `AssignmentOnDutyDay/Time`, `AssignmentType`, `AssignmentAbolishment` | `AssignmentTemplate`, `WorkInstance` | Missing: on-duty day/time sub-entities, abolishment records |
| `Position`, `PositionPayRate`, `PositionAlternateSupervisor`, `PositionRequirement*` | `PositionRole`, `CrewPosition` | Missing: pay rates, alternate supervisors, position requirements/qualifications |
| `RailroadPool*` (Pool, PoolEmployee, PoolMarkOffAllowance, etc.) | `DynamicGroup` (with attributes) | Pool-specific behavior (numbered pools 10-60) replaced by dynamic grouping; pool-specific business rules not yet ported |
| `MarkOffRecord`, `MarkUpRecord`, `MarkOffCode`, `MarkOffRecordApproval` | `AbsenceRequest` | Heavily simplified: SA has 15+ mark-off entities; CrewService has 2 |
| `PayrollRecord`, `PayrollEarningRecord`, `PayrollCode*` | `TimeEntry`, `PayrollRun`, `PayrollRecord` | SA has 20+ payroll entities; CrewService has 3 |
| `Bulletin` entities (`RailroadPositionBulletin*`) | `PositionVacancy`, `Bulletin`, `BulletinBid` | Core bulletin flow present; missing: no-bid bulletin handling, bulletin assignment records |
| `ExtraBoard` entities (`DailyShiftExtraBoard*`) | `ExtraBoard`, `BoardMember` | Core board structure present; missing: daily shift-specific board records, position payroll records |

## 🔴 Not Mapped (no equivalent entity in CrewService)

### Daily Operations Entities (~40 entities)
These form the core **daily call sheet / on-duty / off-duty lifecycle**:

- `DailyAssignment` / `DailyAssignmentShift` / `DailyAssignmentShiftCompletion`
- `DailyAssignmentCrew` / `DailyAssignmentAnnulment` / `DailyAssignmentRequest`
- `DailyAssignmentAFERecord`
- `DailyCrewPosition` (the central daily operations entity)
- `DailyCrewPositionAnnulment` / `DailyCrewPositionDoNotFill` / `DailyCrewPositionSkip`
- `DailyCrewPositionOnDutyRecord` (the core on-duty tracking entity)
- `DailyCrewPositionOffDutyRecord`
- `DailyCrewPositionOnDutyRecordLateCall`
- `DailyCrewPositionOnDutyMarkOffRecord`
- `DailyCrewPositionOnDutyPayrollRecord`
- `DailyCrewPositionOnDutyFRARecord`
- `DailyCrewPositionVacancy` / `DailyCrewPositionVacancyEmployee`
- `DailyCrewPositionElectronicCallRecord` / `DailyCrewPositionElectronicResponseRecord`
- `DailyCrewPositionHistory`
- `DailyCrewHistory`
- `MovedDailyCrewPosition`
- `DailyOnDutyUnavailableRecord` / `DailyOnDutyDidNotWorkRecord`
- `DailyOnDutyPayrollInformation`
- `DailyOnDutyAFEBillingRecord` / `DailyOnDutyMiscellaneousBillingRecord` / `DailyOnDutyZoneBillingRecord`
- `DailyOnDutyLocomotiveRecord` / `DailyOnDutyRailroadMaterialRecord`

### Extra Board Daily Entities (~8 entities)
- `DailyShiftExtraBoard` / `DailyShiftExtraBoardPosition`
- `DailyShiftExtraBoardPositionAssignment` / `DailyShiftExtraBoardPositionPayrollRecord`
- `DailyShiftOvertimeBoard` / `DailyShiftOvertimeBoardPosition`
- `DailyExtraBoardMarkOffRecord`

### Roster Board Entities (~6 entities)
- `RosterBoard` / `RosterBoardPosition`
- `DailyRosterBoardPositionHangoutRecord`
- `DailyRailroadEmployeePositionRecord` / `DailyRailroadEmployeePositionPayrollRecord`
- `DailyRailroadEmployeePositionMarkOffRecord`
- `DailyRailroadEmployeeStatusRecord`
- `DailyRailroadPositionOffDayRecord` / `DailyRailroadPositionOffDayEmployeeRecord`

### Mark-Off Extended Entities (~10 entities)
- `MarkOffCode` (reference data with 20+ properties)
- `MarkOffPayrollCode` / `MarkOffMarkUpHours`
- `MarkOffCodeApprovalOfficer`
- `CraftMarkOffCode` / `CraftMarkOffAllowance`
- `MarkOffRequestRecord` / `MarkOffRequestApproval`
- `MarkOffRequestWaitListRecord` / `MarkOffRequestMarkOffRequestWaitListRecord`
- `MarkOffRequestMarkOffRecord` / `MarkOffRequestMarkUpRecord`
- `MarkOffRequestTempRecord` / `MarkOffRequestDelete` / `MarkOffRecordDelete`

### Payroll Extended Entities (~15 entities)
- `PayrollCode` / `PayrollCodePayRate` / `PayrollCodeApprovalRole`
- `PayrollCategory` / `PayrollCategoryCode`
- `PayrollReportGroup` / `PayrollReportGroupCategory`
- `PayrollCrewPositionAutoPayRecord`
- `PayrollReviewRecord` / `PayrollReviewRequiredRecord`
- `PayrollEarningProcessedRecord` / `PayrollPeriodProcessRecord`
- `PayrollRecordDelete`
- `PayrollHolidayRecord` / `PayrollHolidayRecordPayrollRecord`
- `EarningsApprovalRequiredRecord` / `EarningsApprovalRecord` / `EarningsDeclanationRecord` / `EarningsApprovalEmployee`

### FRA Compliance Entities (~3 SA entities → ~9 needed per CFR Part 228)

SA entities:
- `DailyCrewPositionOnDutyFRARecord`
- `DailyFRACommingleRecord`
- `DailyFRADeadheadRecord`

Additional entities required for full 49 CFR Part 228 compliance (see [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md)):
- `FraDutyTour` — root entity for a complete duty tour spanning multiple assignments
- `FraDutyTourSegment` — each covered-service assignment within a tour
- `FraInterimRelease` — qualifying break ≥4h at designated terminal (broken/aggregate service)
- `FraTransportationSegment` — per-segment deadhead with mode, start/end location/time
- `FraOtherServiceSegment` — non-covered service at behest of railroad with service type code
- `FraExcessServiceReport` — reportable violation record (10 violation types per §228.19)
- `FraMonthlyAccumulator` — per-employee monthly running totals (276h cap, 30h deadhead cap)

### Reference / Configuration Entities (~25 entities)
- `MarkOffCode` (core reference), `PayRate`, `EngineerPayRate`, `EngineerJobCode`
- `Location`, `RailroadLocation`, `RailroadZone`, `RailroadAFE`
- `RailroadWorkCode`, `RailroadMaterial`, `RailroadMaterialCategory`
- `RailroadLocomotiveType`, `RailroadPayrollDepartment`
- `Qualification`, `Requirement`, `RequirementDelete`
- `Shift`, `WeekDay`, `RefreshRate`, `Description`
- `Holiday`, `HolidayQualifyRecord`
- `ChangeNotification`, `ChangeMoveOrBulletin`, `RailroadPositionChange`
- `OnDutyMoveCutOffTime`
- `LocomotiveInspectionRecord`

### Railroad Information Entities (~7 entities)
- `RailroadInformationRecord` / `RailroadInformationType`
- `RailroadInformationCancelRecord` / `RailroadInformationCloseRecord`
- `RailroadInformationDeleteRecord` / `RailroadInformationPublishRecord`
- `RailroadInformationReadbyEmployeeRecord`

### Integration Entities (~5 entities)
- `ADPInterface` / `UKGInterface`
- `UserLoginRecord`
- `FillVacancyLog`
- `ObjectNotes`

### BeSafe Module Entities (~7 entities, SAClassLibrary only)
- `BeSafeRecord` / `BeSafeCategory` / `BeSafeArea` / `BeSafeSubdivision`
- `BeSafeActionRecord` / `BeSafeChangeRecord` / `BeSafeResolveRecord`
- `BeSafeDeleteRecord` / `BeSafeEmailGroup`

---

## Cross-References

- Business logic gaps driving entity needs: [03-business-logic-gaps.md](03-business-logic-gaps.md)
- SA concept → CrewService module mapping: [05-module-mapping.md](05-module-mapping.md)
- FRA entities required by 49 CFR Part 228: [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md)
