# End-to-End Implementation Plan

## Numbering Convention

- **Analysis docs** (`docs/gap-analysis/01–09`): Reference material — gap analysis, regulatory specs
- **Build specs** (`docs/gap-analysis/impl/B01–B14`): Implementation specs in **build order**

The `B` prefix distinguishes build specs from analysis docs and reflects execution sequence.

## Build Sequence

### Phase 0a: Cross-Cutting Infrastructure (no dependencies)

Reference entities and shared services used by multiple later branches.

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B14 | `feature/gap-cross-cutting` | [B14-cross-cutting.md](B14-cross-cutting.md) | 5 | Nothing |

Phase 0a delivers: `IOperationalNotifier`, Teams webhook, `RailroadLocation`, `RailroadZone`,
`RailroadAFE`, `RailroadWorkCode`, `RailroadMaterial`, `RailroadLocomotiveType`, `ChangeNotification`.

### Phase 0b: System-Level Regulatory Foundation

These are federal requirements seeded once, shared by all railroads.

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B01 | `feature/gap-fra-compliance` | [B01-fra-compliance.md](B01-fra-compliance.md) | 20 | B14 (locations for FRA segments) |

Phase 0b delivers: `RegulatoryStandard` (Part 228), `RegulatoryQualification` (Parts 240/242),
`EmployeeCertification`, `DrugAlcoholTestRecord`, and all system-level FRA entities.

### Phase 1: Daily Operations Foundation

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B02 | `feature/gap-daily-operations` | [B02-daily-operations.md](B02-daily-operations.md) | 12 | B01, B14 (billing ref entities) |

Phase 1 delivers: `ShiftDefinition`, `ShiftInstance`, `PositionSlotInstance`, `OnDutyRecord`,
`OffDutyRecord`, `CraftOperationsPolicy`, `CallSheetGenerationService`, `OnDutyPlacementService`,
`TieUpService`.

### Phase 2: Mark-Off System

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B03 | `feature/gap-mark-off-system` | [B03-mark-off-system.md](B03-mark-off-system.md) | 8 | B02 (PositionSlotInstance) |

Phase 2 delivers: `AbsenceCode`, `AbsenceCodeCraftOverride`, expanded `AbsenceRequest`,
`AbsenceApproval`, `AbsenceMarkUp`, `CompensationBalance`, board-impact handlers.

### Phase 3: Vacancy Assignment

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B04 | `feature/gap-vacancy-assignment` | [B04-vacancy-assignment.md](B04-vacancy-assignment.md) | 8 | B02, B03, B01 (QualificationRule) |

Phase 3 delivers: `ISkipRule` pipeline, `IAssignmentStrategy`, `VacancyResolutionEngine`,
`VacancyResolutionRun`, reactive event handlers.

### Phase 4: Payroll Engine

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B05 | `feature/gap-payroll-engine` | [B05-payroll-engine.md](B05-payroll-engine.md) | 8 | B02, B03 |

Phase 4 delivers: `EarningCodeRule`, `PayRate`, `EarningApproval`, extended `PayrollRecord`,
earning code resolver, period processing.

### Phase 5: Electronic Calling

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B06 | `feature/gap-electronic-calling` | [B06-electronic-calling.md](B06-electronic-calling.md) | 6 | B02, B04 |

Phase 5 delivers: `NotificationRequest`, `NotificationResponse`, `ICrewNotificationProvider`,
AtHoc implementation, `NotificationProviderConfig`.

### Phase 6: Background Services

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B07 | `feature/gap-background-services` | [B07-background-services.md](B07-background-services.md) | 10 | B02, B03, B04, B05, B06 |

Phase 6 delivers: `WorkerSchedule`, `WorkerExecutionLog`, `ProcessingLock`, `WorkerBase`,
all 10 `BackgroundService` workers.

### Phase 7: Roster Board Operations

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B08 | `feature/gap-roster-board-ops` | [B08-roster-board-ops.md](B08-roster-board-ops.md) | 5 | B02, B03 |

Phase 7 delivers: `RosterBoard`, `RosterBoardPosition`, `DailyEmployeeStatusRecord`,
hangout processing.

### Phase 8: Holiday Payroll

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B09 | `feature/gap-holiday-payroll` | [B09-holiday-payroll.md](B09-holiday-payroll.md) | 4 | B05, B03 |

Phase 8 delivers: `Holiday`, `HolidayQualificationRule`, `HolidayPayrollRecord`.

### Phase 9: Reporting & Exports

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B10 | `feature/gap-reporting-exports` | [B10-reporting-exports.md](B10-reporting-exports.md) | 6 | B05 |

Phase 9 delivers: `PayrollExportBatch`, `PayrollImportRecord`, ADP/UKG formatters.

### Phase 10: Railroad Information (Independent)

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B11 | `feature/gap-railroad-information` | [B11-railroad-information.md](B11-railroad-information.md) | 4 | Nothing |

### Phase 11: Safety/BeSafe (Independent)

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B12 | `feature/gap-safety-besafe` | [B12-safety-besafe.md](B12-safety-besafe.md) | 4 | Nothing |

### Phase 12: Qualifications & Requirements

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B15 | `feature/gap-qualifications-requirements` | [B15-qualifications-requirements.md](B15-qualifications-requirements.md) | 13 | B02 (OnDutyRecord), B01 (EmployeeCertification), Employee module, TenantConfig (DynamicGroup) |

Phase 12 delivers: `QualificationType`, `QualificationPrerequisite`, `EmployeeQualification`,
`QualificationEvidence`, `IPrerequisiteEvaluator` pipeline, `EmployeeEligibilityService`,
expanded `QualificationRule` skip rule, expiry background jobs.

### Phase 13: PTRA Seed Data

| # | Branch | Spec | Commits | Depends On |
|---|--------|------|---------|------------|
| B13 | `feature/gap-ptra-seed` | [B13-ptra-seed-data.md](B13-ptra-seed-data.md) | 1 | All above |

---

## Full Dependency Graph

```
B14 (Cross-Cutting — Teams, reference entities, change notifications)
B01 (FRA Compliance — system-level) ◄─── B14 (locations for FRA segments)
 └─► B02 (Daily Operations) ◄─── B14 (billing reference entities)
      ├─► B03 (Mark-Off System)
      │    ├─► B04 (Vacancy Assignment) ◄─── B01 (QualificationRule)
      │    │    └─► B06 (Electronic Calling)
      │    ├─► B05 (Payroll Engine)
      │    │    ├─► B09 (Holiday Payroll)
      │    │    └─► B10 (Reporting/Exports)
      │    └─► B08 (Roster Board Ops)
      └─► B07 (Background Services) ◄─── B03, B04, B05, B06

B11 (Railroad Information) ── independent
B12 (Safety/BeSafe) ── independent

B15 (Qualifications & Requirements) ◄─── B02 (OnDutyRecord), B01 (EmployeeCertification)
 ├─► Expands B04 QualificationRule skip rule
 └─► Background jobs (QualificationExpiryEnforcer, PrerequisiteEvaluationJob)

B13 (PTRA Seed Data) ◄─── all above
```

---

## Summary

| Phase | Branch | Commits | Running Total |
|-------|--------|---------|--------------|
| 0a | Cross-Cutting Infrastructure | 5 | 5 |
| 0b | FRA Compliance | 20 | 25 |
| 1 | Daily Operations | 12 | 37 |
| 2 | Mark-Off System | 8 | 45 |
| 3 | Vacancy Assignment | 8 | 53 |
| 4 | Payroll Engine | 8 | 61 |
| 5 | Electronic Calling | 6 | 67 |
| 6 | Background Services | 10 | 77 |
| 7 | Roster Board Ops | 5 | 82 |
| 8 | Holiday Payroll | 4 | 86 |
| 9 | Reporting/Exports | 6 | 92 |
| 10 | Railroad Information | 4 | 96 |
| 11 | Safety/BeSafe | 4 | 100 |
| 12 | Qualifications & Requirements | 13 | 114 |
| 13 | PTRA Seed Data | 1 | 115 |
| **Total** | **15 branches** | **115 commits** | |

## Conventions

- **All new entities** follow the existing `AuditStamp` owned value object pattern
  (`CreatedBy`, `ModifiedBy`, `DeletedBy`) already established in the codebase.
- **Backend-first**: All 15 branches build the CrewService.API backend. Frontend
  (CrewService.FrontEnd/BlazorUI) is a separate follow-on phase with its own plan.
- **No data migration**: Greenfield build with seed data only (B13).

## Per-Spec Structure

Each spec follows this format:

1. **Aggregate Design** — root vs child entities, transaction boundaries
2. **Entity Catalog** — each new entity with properties, module placement, relationship to existing entities
3. **Domain Event Catalog** — event name, publisher, subscriber(s), side effects
4. **Configuration Model** — typed policy entities and/or attribute definitions
5. **Commit Sequence** — ordered commits with dependency rationale
6. **Acceptance Scenarios** — Given/When/Then per commit
