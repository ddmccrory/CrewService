# Impl Spec: `feature/gap-fra-compliance`

**Priority**: P0 – Core  
**Depends on**: `gap-daily-operations` (OnDutyRecord, OffDutyRecord)  
**Depended on by**: vacancy-assignment (rest checks), mark-off-system (SR/NR/NN generation), background-services

## Overview

Implements full 49 CFR Part 228 (hours of service) compliance and 49 CFR Parts 240/242
(craft qualification/certification) requirements. Built from the regulations, not from SA's
partial implementation. SA covers ~40% of CFR Part 228; CrewService targets 100%.

FRA-regulated craft qualifications (Engineer certification under Part 240,
Conductor/Switchman certification under Part 242) are also system-level policies — they
apply to all railroads, not per-tenant configuration.

See [../06-fra-compliance-requirements.md](../06-fra-compliance-requirements.md) for the
complete CFR cross-reference.

---

## 1. Aggregate Design

### Aggregate 1: `FraDutyTour` (root) — Module: new `FraCompliance` module

The duty tour is the central compliance aggregate. It spans one or more assignments
within a single on-duty period and is the unit against which all CFR limits are checked.

```
FraDutyTour (aggregate root)
  ├── FraDutyTourSegment (child — each covered-service assignment)
  ├── FraTransportationSegment (child — deadhead periods)
  └── FraOtherServiceSegment (child — commingled/other service)
```

**Why one aggregate**: All segments are validated together for TTOD calculation. The
duty tour is the consistency boundary — no segment exists without its tour.

**Relationship to existing entities**:
- `FraDutyTour.EmployeeCtrlNbr` → FK to `Employee`
- `FraDutyTourSegment.OnDutyRecordCtrlNbr` → FK to `OnDutyRecord` (from gap-daily-operations)
- Created reactively: `OnDutyRecordCreatedDomainEvent` triggers tour creation/segment addition

### Aggregate 2: `FraExcessServiceReport` (root) — Module: FraCompliance

Standalone reportable-violation record. One per detected violation.

- `FraExcessServiceReport.DutyTourCtrlNbr` → FK to `FraDutyTour`
- `FraExcessServiceReport.EmployeeCtrlNbr` → FK to `Employee`

### Aggregate 3: `FraMonthlyAccumulator` (root) — Module: FraCompliance

Per-employee, per-month running totals. Updated on each tie-up.

- `FraMonthlyAccumulator.EmployeeCtrlNbr` → FK to `Employee`

---

## 2. Entity Catalog

### `FraDutyTour` — FraCompliance module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| RegulatoryStandardCtrlNbr | ControlNumber | FK → RegulatoryStandard |
| DutyTourStartUtc | DateTime | First on-duty time |
| DutyTourEndUtc | DateTime? | Final release time (null while active) |
| TotalTimeOnDutyMinutes | int? | Computed from segments (§228.203(c)(1)) |
| ExcessMinutes | int? | Amount TTOD + deadhead-to-release exceeds 720 min (§228.11(b)(13)) |
| ExcessServiceReason | string? | Required when TTOD > 12h (§228.203(c)(5)) |
| PriorTimeOffMinutes | int | System-calculated prior rest |
| EmployeeReportedPriorTimeOffMinutes | int? | Employee-reported (§228.203(c)(4)) |
| PriorTimeOffReconciled | bool | True when system/employee values match or resolved |
| ConsecutiveDays | int | Days in sequence with on-duty initiated (§228.11(b)(16)) |
| IsQuickTieUp | bool | Tie-up within 3 min of max (§228.203(c)(6)) |
| IsCertified | bool | Employee certification of record |
| Audit | AuditStamp | |

### `FraDutyTourSegment` — child of FraDutyTour

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| DutyTourCtrlNbr | ControlNumber | FK → FraDutyTour (parent) |
| OnDutyRecordCtrlNbr | ControlNumber | FK → OnDutyRecord |
| PositionDescription | string | Covered service position (§228.11(b)(2)) |
| StartLocationCode | string | (§228.11(b)(4)) |
| StartUtc | DateTime | |
| EndLocationCode | string? | (§228.11(b)(7)) |
| EndUtc | DateTime? | |
| SegmentOrder | int | Sequence within tour |

### `FraTransportationSegment` — child of FraDutyTour

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| DutyTourCtrlNbr | ControlNumber | FK → FraDutyTour (parent) |
| StartLocationCode | string | (§228.11(b)(8)) |
| StartUtc | DateTime | |
| EndLocationCode | string | |
| EndUtc | DateTime | |
| TransportMode | string | "Train", "TrackCar", "RRMotorVehicle", "PersonalAuto", etc. |
| IsToAssignment | bool | True = counts as on-duty; False = returning (neither on nor off) |

### `FraOtherServiceSegment` — child of FraDutyTour

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| DutyTourCtrlNbr | ControlNumber | FK → FraDutyTour (parent) |
| ServiceTypeCode | string | Identification code (§228.11(b)(10)) |
| StartLocationCode | string | |
| StartUtc | DateTime | |
| EndLocationCode | string | |
| EndUtc | DateTime | |
| IsCommingled | bool | True if not separated by qualifying off-duty period |

### `FraExcessServiceReport`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| DutyTourCtrlNbr | ControlNumber | FK → FraDutyTour |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| ViolationType | string | One of 10 types per §228.19(b)(1)–(10) |
| DetectedAtUtc | DateTime | |
| ExplanationText | string? | |
| ReportedToFra | bool | Tracks whether violation has been reported |
| ReportedAtUtc | DateTime? | |

### `FraMonthlyAccumulator`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| YearMonth | string | "2025-07" format |
| CoveredServiceMinutes | int | Running total (§228.11(b)(14)(i)) |
| DeadheadToReleaseMinutes | int | (§228.11(b)(14)(ii)) |
| OtherServiceMinutes | int | (§228.11(b)(14)(iii)) |
| DeadheadAfter12hMinutes | int | (§228.11(b)(15)) |

---

## 3. Domain Event Catalog

| Event | Published By | Subscribers |
|-------|-------------|-------------|
| `FraDutyTourOpenedDomainEvent` | `FraDutyTour.Create()` | — |
| `FraDutyTourClosedDomainEvent` | `FraDutyTour.Close()` | Monthly accumulator update, excess service detection |
| `FraExcessServiceDetectedDomainEvent` | `FraExcessServiceDetector` | Notification handler (Teams), auto mark-off (SR/NR/NN) via gap-mark-off-system |
| `FraConsecutiveDayLimitReachedDomainEvent` | `FraConsecutiveDayTracker` | Auto "SR" mark-off via gap-mark-off-system |

### Reactive Pattern: Subscribes to gap-daily-operations events

| Trigger Event (from daily-ops) | FRA Handler | Action |
|-------------------------------|-------------|--------|
| `OnDutyRecordCreatedDomainEvent` | `FraOnDutyHandler` | Open or append to `FraDutyTour`; run rest-for-next check; run consecutive day check |
| `OffDutyRecordCreatedDomainEvent` | `FraOffDutyHandler` | Close `FraDutyTour`; calculate TTOD; compute rest requirement; update `FraMonthlyAccumulator`; run all 10 excess service checks |

---

## 4. Configuration Model

FRA regulations are **federal law** — they apply to all railroads for all parents. The
regulatory limits must NOT be redefined per tenant. This is a system-level concern.

### `RegulatoryStandard` — FraCompliance module (system-level, seeded once)

Stores the values from 49 CFR Part 228. One row per regulation set (currently one: "CFR-228-Train").
Future government regulations (e.g., signal employee rules, state-level rules) become additional rows.

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| Code | string | "CFR-228-TRAIN", "CFR-228-SIGNAL", "CFR-228-DISPATCH" |
| Description | string | "49 CFR Part 228 — Train Employees" |
| MaxOnDutyMinutes | int | 720 (12h) |
| MinRestMinutes | int | 600 (10h) |
| Min8hRestInPreceding24h | bool | true for train employees |
| ConsecutiveDayLimit6 | int | 6 |
| ConsecutiveDayLimit7 | int | 7 |
| RestAfter6DaysMinutes | int | 2880 (48h) |
| RestAfter7DaysMinutes | int | 4320 (72h) |
| MonthlyCapMinutes | int | 16560 (276h) |
| DeadheadAfter12hMonthlyCapMinutes | int | 1800 (30h) |
| WreckReliefExtraMinutes | int | 240 (4h) |
| EffectiveDate | DateOnly | When this standard took effect |

**These are NOT configurable per tenant.** They are seeded from the CFR and only
change when the federal regulation changes (at which point a new row or an update
is applied system-wide).

### Per-craft coverage — extension to existing `Craft` entity

| Property | Type | Notes |
|----------|------|-------|
| IsHoursOfServiceCovered | bool | Does this craft fall under FRA hours-of-service? |
| RegulatoryStandardCtrlNbr | ControlNumber? | FK → RegulatoryStandard (null if not covered) |

This is the **only per-railroad decision**: which crafts are covered and which
`RegulatoryStandard` applies to them. The limits themselves are universal.

### `RegulatoryQualification` — FraCompliance module (system-level, seeded once)

FRA-mandated craft qualifications are also federal law, not per-railroad policy.

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| Code | string | "CFR-240-ENGINEER", "CFR-242-CONDUCTOR", etc. |
| CfrPart | string | "49 CFR Part 240", "49 CFR Part 242" |
| Description | string | "Locomotive Engineer Certification" |
| RequiresCertification | bool | |
| RecertificationIntervalMonths | int? | E.g., 36 months |
| EffectiveDate | DateOnly | |

**Seed rows (system-level, all tenants):**

| Code | CFR Part | Description |
|------|---------|-------------|
| CFR-240-ENGINEER | 49 CFR Part 240 | Locomotive Engineer Certification |
| CFR-242-CONDUCTOR | 49 CFR Part 242 | Conductor Certification |
| CFR-242-SWITCHMAN | 49 CFR Part 242 | Switchman Certification |

### Per-craft qualification linkage — `CraftRegulatoryQualification` (junction)

| Property | Type | Notes |
|----------|------|-------|
| CraftCtrlNbr | ControlNumber | FK → Craft (composite PK) |
| RegulatoryQualificationCtrlNbr | ControlNumber | FK → RegulatoryQualification (composite PK) |

Per-railroad decision: which crafts require which FRA qualifications. The qualification
definitions and certification requirements themselves are universal.

### `EmployeeCertification` — FraCompliance module

Tracks an employee's current certification status against a `RegulatoryQualification`.

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| RegulatoryQualificationCtrlNbr | ControlNumber | FK → RegulatoryQualification |
| CertificationType | string | "LocomotiveService", "TrainService", "Passenger", "Freight", "Yard" (§240.107/§242.107) |
| CertificationDate | DateOnly | Date of issuance (§240.223/§242.207) |
| ExpirationDate | DateOnly | Max 36 months from CertificationDate (§240.217(c)(1)) |
| Status | string | "Active" / "Suspended" / "Revoked" / "Expired" / "Pending" |
| CertificationNumber | string? | External certification ID |
| SuspendedAtUtc | DateTime? | Immediate on reliable information (§240.307(b)(1)) |
| SuspensionReason | string? | Written reason (§240.307(b)(2)) |
| RevocationPeriodEndUtc | DateTime? | When revocation period ends |
| LastMonitoringObservationUtc | DateTime? | Must occur ≥1x/calendar year (§240.303(b)) |
| LastComplianceTestUtc | DateTime? | Must occur ≥1x/calendar year (§240.303(c)) |

The vacancy assignment `QualificationRule` (B04) checks `EmployeeCertification.Status`
before placing an employee — if the position requires a `RegulatoryQualification` and the
employee's certification is not "Active", they are skipped.

**System-level enforcement**: All certification lifecycle management — 36-month intervals,
eligibility checks, staleness limits, revocation procedures, monitoring requirements, and
Part 219 drug/alcohol compliance — are system-level processes enforced for every covered
employee across all railroads. These are not tenant-configurable. See
[../08-fra-certification-requirements.md](../08-fra-certification-requirements.md) and
[../09-fra-drug-alcohol-requirements.md](../09-fra-drug-alcohol-requirements.md) for
the complete CFR cross-reference.

### `CertificationEligibilityCheck` — FraCompliance module (child of EmployeeCertification)

Tracks each prerequisite evaluation for initial certification or recertification (§240.203).

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCertificationCtrlNbr | ControlNumber | FK → EmployeeCertification (parent) |
| CheckType | string | "SafetyConduct", "MotorVehicle", "SubstanceAbuse", "Vision", "Hearing", "Knowledge", "Performance" |
| EvaluationDate | DateOnly | Date evaluation was conducted |
| StalenessLimitDays | int | 366 or 450 depending on type (§240.217(a)) |
| ExpiresAtDate | DateOnly | Computed: EvaluationDate + StalenessLimitDays |
| Result | string | "Pass" / "Fail" / "Conditional" |
| EvaluatorName | string? | Examiner / supervisor name |

### `CertificationRevocationRecord` — FraCompliance module

One per revocation proceeding. Tracks the full §240.307/§242.407 process.

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCertificationCtrlNbr | ControlNumber | FK → EmployeeCertification |
| ViolationType | string | §240.305(a)(1-6) or §242.403(e)(1-12) violation code |
| ViolationDate | DateTime | |
| SuspendedAtUtc | DateTime | Immediate suspension (§240.307(b)(1)) |
| WrittenNoticeAtUtc | DateTime? | Within 96 hours (§240.307(b)(2)) |
| HearingScheduledUtc | DateTime? | Within 10 days of suspension (§240.307(c)(1)) |
| HearingHeldUtc | DateTime? | |
| PresidingOfficerCtrlNbr | ControlNumber? | Must not be investigating officer |
| Decision | string? | "Revoked" / "Reinstated" |
| DecisionDate | DateTime? | |
| RevocationPeriodMonths | int? | Per §240.117/§240.119 |
| RevocationEndsUtc | DateTime? | |
| HearingRecordRetainUntil | DateOnly? | Decision date + 3 years (§240.307(b)(7)) |

### `DrugAlcoholTestRecord` — FraCompliance module

Per-test record for all Part 219 testing types. System-level — all covered employees.

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| TestType | string | "PostAccident", "ReasonableSuspicion", "ReasonableCause", "Random", "PreEmployment", "ReturnToDuty", "FollowUp" |
| TestDate | DateTime | |
| AlcoholResult | decimal? | BAC concentration |
| DrugResult | string? | "Negative", "Positive", "Refused" |
| SubstancesDetected | string? | JSON array if positive |
| IsViolation | bool | True if ≥0.04 alcohol or positive drug (§219.101/§219.102) |
| FederalAuthority | bool | True = FRA test; False = company authority |
| Audit | AuditStamp | |

### `DrugAlcoholAction` — FraCompliance module

Tracks responsive actions per §219.104.

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| TestRecordCtrlNbr | ControlNumber | FK → DrugAlcoholTestRecord |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| ActionType | string | "RemovedFromService", "WrittenNotice", "HearingScheduled", "SAPReferral", "ReturnToDuty", "FollowUpScheduled" |
| ActionDate | DateTime | |
| Notes | string? | |

### `VoluntaryReferral` — FraCompliance module

Tracks §219.403 voluntary referral and return-to-duty process.

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| ReferralDate | DateTime | |
| SapEvaluationDate | DateTime? | |
| TreatmentCompletedDate | DateTime? | |
| ReturnToDutyTestDate | DateTime? | |
| ReturnToDutyResult | string? | Must be negative/<0.02 |
| FollowUpTestsRequired | int | Min 6 in first 12 months |
| FollowUpEndDate | DateTime? | Max 60 months |
| Status | string | "Referred", "InTreatment", "ReturnToDuty", "FollowUp", "Completed" |

---

## 5. Commit Sequence

### Commit 1: `gap(fra): add RegulatoryStandard entity with CFR seed data`
- Domain entity, EF config, migration
- Seed CFR-228-TRAIN, CFR-228-SIGNAL, CFR-228-DISPATCH rows
- System-level — applies to all tenants
- No dependencies on other gap branches

### Commit 2: `gap(fra): add IsHoursOfServiceCovered + RegulatoryStandard FK to Craft`
- Extend existing Craft entity with two new properties
- Migration to add columns

### Commit 3: `gap(fra): add FraDutyTour aggregate with segment children`
- `FraDutyTour`, `FraDutyTourSegment`, `FraTransportationSegment`, `FraOtherServiceSegment`
- EF config, migration
- **Depends on**: Commits 1–2; gap-daily-operations (OnDutyRecord FK)

### Commit 4: `gap(fra): add FraDutyTourCalculator service`
- Calculate TTOD from segments (covered + commingled + deadhead-to-assignment)
- Prior time off calculation
- Prior time off reconciliation (system vs. employee)
- **Depends on**: Commit 3

### Commit 5: `gap(fra): add FraRestValidator service`
- 10h post-tour rest check
- 8h-in-preceding-24h check
- Penalty rest for excess service (10h + excess hours)
- Quick tie-up detection (within 3 min of max)
- **Depends on**: Commits 3, 4

### Commit 6: `gap(fra): add FraConsecutiveDayTracker service`
- Two-tier consecutive day tracking (6→48h, 7→72h)
- Home terminal awareness
- Auto-SR mark-off trigger (raises event for gap-mark-off-system)
- **Depends on**: Commits 3, 4

### Commit 7: `gap(fra): add FraMonthlyAccumulator entity and tracker`
- Entity, EF config
- `FraMonthlyCapTracker` — updates on each tour close, checks 276h and 30h caps
- **Depends on**: Commits 3, 4

### Commit 8: `gap(fra): add FraExcessServiceReport and detector`
- Entity, EF config
- `FraExcessServiceDetector` — evaluates all 10 violation types on tour close
- **Depends on**: Commits 4, 5, 6, 7

### Commit 9: `gap(fra): add reactive domain event handlers`
- `FraOnDutyHandler` subscribes to `OnDutyRecordCreatedDomainEvent`
- `FraOffDutyHandler` subscribes to `OffDutyRecordCreatedDomainEvent`
- Wires the full pipeline: open tour → add segment → close → calculate → validate → detect
- **Depends on**: All previous commits

### Commit 10: `gap(fra): add FRA record search service and gRPC`
- `FraRecordSearchService` — query by 7 CFR-mandated criteria (§228.203(d))
- gRPC endpoints for FRA record management
- **Depends on**: Commits 3, 8

### Commit 11: `gap(fra): add Part 228 unit tests`
- TTOD calculation, rest validation, consecutive day tiers, monthly caps, all 10 violation types
- **Depends on**: Commits 1–10

### — Part 240/242: Certification Lifecycle —

### Commit 12: `gap(fra): add RegulatoryQualification entity with CFR seed data`
- Seed CFR-240-ENGINEER, CFR-242-CONDUCTOR, CFR-242-SWITCHMAN
- `CraftRegulatoryQualification` junction entity
- System-level — applies to all tenants
- **Depends on**: Commit 2 (Craft entity must exist)

### Commit 13: `gap(fra): add EmployeeCertification entity`
- Full entity with all §240.223 / §242.207 fields
- `CertificationEligibilityCheck` child entity
- EF config, migration
- **Depends on**: Commit 12

### Commit 14: `gap(fra): add certification lifecycle services`
- `CertificationEligibilityService` — validates all 7 check types, enforces staleness limits (§240.217)
- `CertificationExpirationService` — flags certifications expiring within 36-month interval
- `CertificationMonitoringService` — tracks annual observation + compliance test (§240.303)
- **Depends on**: Commit 13

### Commit 15: `gap(fra): add CertificationRevocationRecord and revocation workflow`
- Entity, EF config
- `CertificationRevocationService` — immediate suspension, notice, hearing scheduling, decision
- Enforces §240.307 timeline (written notice within 96h, hearing within 10 days)
- Cross-revocation: conductor cert revocation for (e)(1-5)/(e)(12) also revokes engineer cert (§242.213(h))
- **Depends on**: Commit 13

### — Part 219: Drug & Alcohol Compliance —

### Commit 16: `gap(fra): add DrugAlcoholTestRecord and DrugAlcoholAction entities`
- Entities, EF config, migration
- Covers all 7 test types (§219 subparts B–G, K)
- Three-tier alcohol threshold logic (<0.02 negative, 0.02-0.039 removal, ≥0.04 violation)
- **Depends on**: Nothing (standalone entities referencing Employee)

### Commit 17: `gap(fra): add VoluntaryReferral entity and return-to-duty service`
- Entity, EF config
- `ReturnToDutyService` — SAP evaluation, treatment completion, return-to-duty test, follow-up scheduling
- Enforces min 6 follow-up tests in 12 months, max 60-month follow-up window
- **Depends on**: Commit 16

### Commit 18: `gap(fra): add D&A certification impact handler`
- Reactive handler: `DrugAlcoholTestRecord` with `IsViolation = true` triggers `CertificationRevocationService`
- Ineligibility periods: 1st = during treatment, 2nd = 2 years, 3rd+ = permanent (§240.119(e))
- Refusal to test treated as single violation
- **Depends on**: Commits 15, 16

### — Combined —

### Commit 19: `gap(fra): add gRPC endpoints for certification and D&A management`
- Certification CRUD, eligibility check queries, revocation history
- D&A test record management, voluntary referral workflow
- Monitoring compliance dashboard queries
- **Depends on**: Commits 13–18

### Commit 20: `gap(fra): add Parts 240/242/219 unit tests`
- Certification eligibility staleness, expiration, monitoring
- Revocation workflow timeline enforcement, cross-revocation
- D&A threshold logic, ineligibility periods, return-to-duty process
- **Depends on**: All previous commits

---

## 6. Acceptance Scenarios

**Scenario 1: Normal 10h on-duty tour**
```
GIVEN Employee A (Train employee) goes on duty at 07:00
WHEN tied up at 17:00 (10h tour)
THEN FraDutyTour.TotalTimeOnDutyMinutes = 600
  AND OffDutyRecord.RestHoursRequired = 10.0
  AND no FraExcessServiceReport is created
```

**Scenario 2: Excess service — penalty rest**
```
GIVEN Employee A goes on duty at 07:00
WHEN tied up at 20:00 (13h tour, 1h excess)
THEN FraDutyTour.ExcessMinutes = 60
  AND OffDutyRecord.RestHoursRequired = 11.0 (10h + 1h penalty)
  AND FraExcessServiceReport created: ViolationType = "ExceededMaxOnDuty"
```

**Scenario 3: Consecutive day limit — 6 days → 48h rest**
```
GIVEN Employee A has initiated on-duty periods for 6 consecutive days
  AND day 6 ended at home terminal
WHEN the system evaluates consecutive days
THEN FraConsecutiveDayLimitReachedDomainEvent is raised
  AND an auto "SR" AbsenceRequest is created (via mark-off-system)
  AND required rest = 48h at home terminal
```

**Scenario 4: 276h monthly cap**
```
GIVEN Employee A has accumulated 275h in July
WHEN a new tour adds 3h (total = 278h)
THEN FraExcessServiceReport created: ViolationType = "ExceededMonthlyCap"
  AND FraMonthlyAccumulator total = 278h
```

**Scenario 5: Prior time off reconciliation**
```
GIVEN the system calculates prior rest = 12h
  AND the employee reports prior rest = 10h
WHEN the FRA record is submitted
THEN FraDutyTour.PriorTimeOffReconciled = false
  AND the system requires reconciliation before certification
```

**Scenario 6: Quick tie-up**
```
GIVEN Employee A has been on duty for 11h 58m (2 min from 12h max)
WHEN the employee initiates tie-up
THEN FraDutyTour.IsQuickTieUp = true
  AND only minimal fields are required (§228.203(c)(6))
```

### Part 240/242 Certification Scenarios

**Scenario 7: Certification expiration — 36-month interval**
```
GIVEN Engineer A has EmployeeCertification issued 2022-07-01
  AND ExpirationDate = 2025-07-01
WHEN the current date is 2025-07-01
THEN EmployeeCertification.Status = "Expired"
  AND Employee A cannot be placed on duty as engineer
```

**Scenario 8: Eligibility check staleness**
```
GIVEN Engineer A's vision exam was conducted 2024-01-15
  AND StalenessLimitDays = 450
WHEN the railroad makes a recertification decision on 2025-05-15 (486 days later)
THEN the vision check is stale — recertification is blocked
```

**Scenario 9: Revocation for signal violation**
```
GIVEN Engineer A commits a §240.305(a)(1) signal violation
WHEN reliable information is received
THEN EmployeeCertification.Status = "Suspended" immediately
  AND CertificationRevocationRecord created
  AND WrittenNoticeAtUtc must be set within 96 hours
  AND HearingScheduledUtc must be within 10 days
```

**Scenario 10: Cross-revocation — conductor to engineer**
```
GIVEN Employee B holds both conductor and engineer certification
WHEN conductor cert is revoked for §242.403(e)(1) signal violation
THEN engineer certification is ALSO revoked (§242.213(h))
```

### Part 219 Drug & Alcohol Scenarios

**Scenario 11: Alcohol test — 0.02–0.039 range**
```
GIVEN Employee A tests 0.03 BAC under Federal authority
THEN DrugAlcoholTestRecord created with IsViolation = false
  AND Employee removed from service for minimum 8 hours
  AND certification is NOT affected (§219.101(a)(4))
```

**Scenario 12: Alcohol test — ≥0.04 violation**
```
GIVEN Employee A tests 0.05 BAC (first offense)
THEN DrugAlcoholTestRecord created with IsViolation = true
  AND DrugAlcoholAction "RemovedFromService" created immediately
  AND CertificationRevocationService triggered
  AND ineligibility period = during evaluation + treatment
```

**Scenario 13: Second D&A violation — 2-year ineligibility**
```
GIVEN Employee A has one prior §219.102 violation within 60 months
WHEN a second violation occurs
THEN ineligibility period = 2 years from notification date
```

**Scenario 14: Return-to-duty process**
```
GIVEN Employee A completed SAP treatment
WHEN return-to-duty test is negative
THEN VoluntaryReferral.Status = "FollowUp"
  AND minimum 6 follow-up tests scheduled in first 12 months
  AND follow-up may continue up to 60 months
```
