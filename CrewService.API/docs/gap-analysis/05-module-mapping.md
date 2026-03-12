# 05 – SA Concept → CrewService Module Mapping

## How Legacy Concepts Map to the New Architecture

The legacy system is organized around **pools** (numbered: 10, 20, 30, 40, 50, 60) with hard-coded branching.
CrewService replaces this with **dynamic group hierarchy + craft-level policies**.

| SA Concept | SA Location | CrewService Module | CrewService Entity | Status |
|------------|-------------|--------------------|--------------------|--------|
| Client / Railroad / Pool hierarchy | `StrategicApplicationsContext` | **TenantConfig** | `DynamicGroup`, `GroupType`, `GroupAttributeDefinition/Value` | ✅ Implemented (dynamic tree) |
| Railroad | Direct entity | **Domain/Models** | `Railroad` | ✅ Implemented |
| RailroadPool (numbered pools) | Direct entity | **TenantConfig** | `DynamicGroup` (with `IsWorkArea`) | ✅ Replaced by dynamic grouping |
| `RailroadGroupPlacement` | SA pool-to-group mapping | **TenantConfig** | `RailroadGroupPlacement` | ✅ Implemented |
| Employee | Direct entity | **Domain/Models** | `Employee` | ✅ Implemented |
| Craft | Direct entity | **Domain/Models** | `Craft` | ✅ Implemented |
| Seniority / SeniorityState | Direct entities | **Domain/Models** | `Seniority`, `SeniorityState` | ✅ Implemented |
| Roster | Direct entity | **Domain/Models** | `Roster` | ✅ Implemented |
| Assignment / AssignmentType | Direct entities | **WorkManagement** | `AssignmentTemplate` | ✅ Implemented (modernized) |
| DailyAssignment / DailyAssignmentShift | Per-day generation | **WorkManagement** | `WorkInstance` | ✅ Implemented (abstracted) |
| Crew / CrewAssignment / CrewPosition | Direct entities | **Crews** | `Crew`, `CrewPosition`, `CrewIncumbency` | ✅ Implemented |
| Relief Crew coverage | `Crew` with off-day coverage | **Crews** | `ReliefCoverageRule`, `CrewAttachmentTemplate/Instance` | ✅ Implemented |
| Position / PositionRole | Direct entities | **WorkManagement/Crews** | `PositionRole`, `CrewPosition` | ✅ Implemented |
| Extra Board / Overtime Board | `DailyShiftExtraBoard` + positions | **Boards** | `ExtraBoard`, `BoardMember`, `BoardCascadePolicy` | ✅ Implemented (abstracted) |
| Bulletin / Bid / Award | `RailroadPositionBulletin*` | **Bulletins** | `PositionVacancy`, `Bulletin`, `BulletinBid` | ✅ Implemented |
| Seniority Moves | `SeniorityMove*` | **Policies** | `SeniorityMovePolicy`, `SeniorityMove` | ✅ Implemented |
| Displacement | Implicit in bulletin logic | **Policies** | `CraftDisplacementPolicy`, `DisplacementCase`, `DisplacementClaim` | ✅ Implemented |
| Bulletin Policy | Implicit in timer logic | **Policies** | `BulletinPolicy` | ✅ Implemented |
| Mark-Off / Mark-Up | `MarkOffRecord` + `MarkUpRecord` | **AbsenceVacancy** | `AbsenceRequest` | ⚠️ Partial – simplified |
| Vacancy Assignment | `DailyCrewPositionVacancy*` | **AbsenceVacancy** | `VacancyImpact` | ⚠️ Partial – simplified |
| Dispatching / Projection | Implicit in vacancy algo | **Dispatching** | `DispatchProjection`, `DispatchDecisionLog`, `DispatchOverride`, `EmployeeBooking` | ✅ Implemented (abstracted) |
| Payroll Record / Earning Record | `PayrollRecord` + `PayrollEarningRecord` | **Payroll** | `TimeEntry`, `PayrollRun`, `PayrollRecord` | ⚠️ Partial – simplified |
| DailyCrewPosition (full lifecycle) | 204-entity model | Multiple modules | Spread across modules | 🔴 Major gap (see below) |
| FRA Compliance | `FRARequirements` static class | Not yet modeled | — | 🔴 Not implemented — SA covers ~40% of CFR Part 228; see [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md) |
| MarkOffCode reference data | `MarkOffCode` entity | Not yet modeled | — | 🔴 Not implemented |
| PayrollCode / EarningCode | Complex code tables | Not yet modeled | — | 🔴 Not implemented |
| AtHoc integration | `AtHocService` static class | Not yet modeled | — | 🔴 Not implemented |
| Windows Services (6 sub-services) | 4 separate projects | Not yet modeled | — | 🔴 Not implemented |
| File Watchers (6 watchers) | `Global.asax` + payroll services | Not yet modeled | — | 🔴 Not implemented |
| Timer Architecture (17 categories) | `Global.asax` + call sheet service | Not yet modeled | — | 🔴 Not implemented |
| MSMQ message queue | Service inter-process comm | Not yet modeled | — | 🔴 Not implemented |
| Teams webhook integration | `ApplicationUtilities` | Not yet modeled | — | 🔴 Not implemented |
| BeSafe safety module | `SAClassLibrary` only | Not yet modeled | — | 🔴 Not implemented |

## SA Entities Not Directly Mapped — Key Omissions

The following SA entities have **no CrewService equivalent** and represent the most significant structural gaps. They are grouped by the gap-closure branch where they should be addressed.

| Gap Branch | SA Entities Missing | Count |
|------------|-------------------|-------|
| `feature/gap-daily-operations` | `DailyAssignment*`, `DailyCrewPosition*`, `DailyOnDuty*`, `MovedDailyCrewPosition`, `Shift` | ~40 |
| `feature/gap-mark-off-system` | `MarkOffCode`, `MarkOffPayrollCode`, `MarkOffMarkUpHours`, `CraftMarkOff*`, `MarkOffRequest*`, `MarkOffRecordApproval` | ~15 |
| `feature/gap-fra-compliance` | `DailyCrewPositionOnDutyFRARecord`, `DailyFRACommingleRecord`, `DailyFRADeadheadRecord` + new CFR-required entities | ~9 |
| `feature/gap-payroll-engine` | `PayrollCode*`, `PayrollCategory*`, `PayrollReportGroup*`, `EarningsApproval*`, `PayrollReview*`, `PayRate*` | ~20 |
| `feature/gap-vacancy-assignment` | `DailyCrewPositionVacancy*`, `FillVacancyLog` | ~3 |
| `feature/gap-electronic-calling` | `DailyCrewPositionElectronicCallRecord`, `DailyCrewPositionElectronicResponseRecord` | ~2 |
| `feature/gap-roster-board-ops` | `RosterBoard*`, `DailyRailroadEmployee*`, `DailyRailroadPosition*`, `DailyExtraBoardMarkOffRecord` | ~10 |
| `feature/gap-holiday-payroll` | `Holiday`, `HolidayQualifyRecord`, `PayrollHolidayRecord*` | ~4 |
| `feature/gap-railroad-information` | `RailroadInformation*` | ~7 |
| `feature/gap-reporting-exports` | `ADPInterface`, `UKGInterface` | ~2 |
| `feature/gap-safety-besafe` | `BeSafe*` | ~9 |

## Cross-References

- Entity-level details: [01-domain-entity-gaps.md](01-domain-entity-gaps.md)
- Automated process details: [02-automated-process-gaps.md](02-automated-process-gaps.md)
- Business logic details: [03-business-logic-gaps.md](03-business-logic-gaps.md)
- Integration details: [04-integration-gaps.md](04-integration-gaps.md)
- FRA regulatory compliance: [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md)
