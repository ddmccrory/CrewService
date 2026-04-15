# Impl Spec: `0.1.1/b15-qualifications-requirements`

**Priority**: P2 – Medium (qualification gating for vacancy assignment and compliance)
**Depends on**: B02 (OnDutyRecord), B01 (EmployeeCertification), Employee module, TenantConfig (DynamicGroup)
**Depended on by**: B04 (QualificationRule skip rule expansion), B07 (expiry background jobs)

## Overview

Adds a configurable qualification and prerequisite system that tracks employee
qualifications, evaluates prerequisites automatically, gates vacancy assignment
via skip rules, and enforces expiration with background workers. Qualification
types are parent-scoped with optional craft and group scope narrowing.

---

## 1. Aggregate Design

### `QualificationType` — Aggregate Root (Employees module)

Owns `QualificationPrerequisite` children. One aggregate per qualification
definition scoped to a parent. Prerequisites are managed through the root.

Transaction boundary: creating/updating a type and its prerequisites is a
single UoW commit.

### `EmployeeQualification` — Aggregate Root (Employees module)

Owns `QualificationEvidence` children. One aggregate per employee + qualification
type pair. Evidence records are added through the root.

Transaction boundary: granting/revoking a qualification and recording evidence
is a single UoW commit.

---

## 2. Entity Catalog

### `QualificationType` — Employees module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| ParentCtrlNbr | ControlNumber | FK → Parent |
| ScopeGroupCtrlNbr | ControlNumber? | FK → DynamicGroup (narrows to work area) |
| CraftCtrlNbr | ControlNumber? | FK → Craft (narrows to craft) |
| Code | string | Unique per parent, stored uppercase |
| Name | string | Display name |
| Description | string? | |
| EvaluationStrategy | string | "Manual", "TimeFromEvent", "ActivityCount", "TimeInRole", "QualificationHeld", "FraCertification" |
| ExpirationMonths | int? | Null = never expires |
| CalendarYearExpiry | bool | True = expires Dec 31 of computed year |
| GraceDays | int | Extra days after expiration before enforcement |
| RenewalLeadDays | int | Days before expiry to trigger "ExpiringSoon" |
| IsBlocking | bool | If true, blocks vacancy assignment when missing |
| IsActive | bool | Soft-disable |

### `QualificationPrerequisite` — Employees module (child of QualificationType)

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| QualificationTypeCtrlNbr | ControlNumber | FK → QualificationType |
| PrerequisiteKind | string | Matches `IPrerequisiteEvaluator.Kind` |
| Threshold | int | Numeric threshold (days, count, months) |
| ThresholdUnit | string | "Days", "Months", "Count" |
| EventSource | string? | e.g. "EmploymentDate" for TimeFromEvent |
| ActivityFilter | string? | Filter key for ActivityCount evaluator |
| RequiredQualTypeCtrlNbr | ControlNumber? | FK → QualificationType (for QualificationHeld) |
| Description | string | Human-readable rule description |

### `EmployeeQualification` — Employees module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| QualificationTypeCtrlNbr | ControlNumber | FK → QualificationType |
| AchievedAtUtc | DateTime | When qualification was granted |
| ExpiresAtUtc | DateTime? | Null = never expires |
| Status | string | "Pending", "Active", "ExpiringSoon", "Expired", "Revoked" |
| GrantedBy | string | Username or "SYSTEM" |
| RevokedAtUtc | DateTime? | Set on revocation |
| RevocationReason | string? | |

### `QualificationEvidence` — Employees module (child of EmployeeQualification)

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeQualificationCtrlNbr | ControlNumber | FK → EmployeeQualification |
| PrerequisiteCtrlNbr | ControlNumber? | FK → QualificationPrerequisite (links evidence to which rule was satisfied) |
| EvidenceType | string | "ManualCompletion", "TimeThresholdMet", "ActivityCountMet", "QualificationHeld" |
| EvidenceValue | string | Freeform detail |
| RecordedAtUtc | DateTime | |
| RecordedBy | string | Username or "SYSTEM" |

---

## 3. Domain Event Catalog

| Event | Published By | Subscribers |
|-------|-------------|-------------|
| `QualificationTypeCreatedDomainEvent` | `QualificationType.Create()` | Audit log |
| `QualificationTypeUpdatedDomainEvent` | `QualificationType.Update()` | Audit log |
| `EmployeeQualificationGrantedDomainEvent` | `EmployeeQualification.Create()` | Audit log, reactive prereq re-evaluation |
| `EmployeeQualificationExpiredDomainEvent` | `EmployeeQualification.Expire()` | Audit log, notification (future) |
| `EmployeeQualificationRevokedDomainEvent` | `EmployeeQualification.Revoke()` | Audit log |
| `EmployeeQualificationExpiringSoonDomainEvent` | `EmployeeQualification.MarkExpiringSoon()` | Notification (future) |

### Reactive Triggers (subscribes to events from other branches)

| Trigger Event | Handler | Action |
|--------------|---------|--------|
| `OnDutyRecordCreatedDomainEvent` (B02) | `DomainEventReactor` → `QualificationReactiveService` | Re-evaluate ActivityCount-strategy qualifications for the employee |
| `EmployeeCreatedDomainEvent` (Employee) | `DomainEventReactor` → `QualificationReactiveService` | Evaluate all active qualification types for the new employee |

---

## 4. Prerequisite Evaluator Pipeline

### `IPrerequisiteEvaluator` interface — Application layer

```
IPrerequisiteEvaluator
  string Kind                          // matches QualificationPrerequisite.PrerequisiteKind
  Task<EvaluationResult> EvaluateAsync(ControlNumber employeeCtrlNbr, QualificationPrerequisite rule, CancellationToken ct)
```

### Built-in evaluators

| Evaluator Class | Kind | Data Source | Logic |
|----------------|------|-------------|-------|
| `ManualCompletionEvaluator` | Manual | — | Always returns `RequiresManualAction` |
| `TimeFromEventEvaluator` | TimeFromEvent | `Employee.EmploymentDate` via `IEmployeeRepository` | Elapsed days/months since event ≥ threshold |
| `ActivityCountEvaluator` | ActivityCount | `OnDutyRecord` count via `IOnDutyRecordCounter` | Completed on-duty records ≥ threshold |
| `TimeInRoleEvaluator` | TimeInRole | `PositionAssignment` via `ICraftMembershipDateProvider` | Days/months since earliest active assignment ≥ threshold |
| `QualificationHeldEvaluator` | QualificationHeld | `EmployeeQualification` via `IEmployeeQualificationRepository` | Employee holds active/expiring-soon qualification of required type |

### Infrastructure read providers (Persistence layer)

| Provider | Interface | Purpose |
|----------|-----------|---------|
| `OnDutyRecordCounter` | `IOnDutyRecordCounter` | Counts completed on-duty records for ActivityCount evaluator |
| `CraftMembershipDateProvider` | `ICraftMembershipDateProvider` | Gets earliest active position assignment date for TimeInRole |
| `FraCertificationChecker` | `IFraCertificationChecker` | Checks active FRA certification status (B01 integration) |

---

## 5. Eligibility Gate & Vacancy Integration

### `EmployeeEligibilityService` — Application layer

Checks whether an employee is eligible for a position slot by evaluating:
1. **Craft membership** — employee has active seniority on a roster for the required craft role
2. **FRA certification** — delegates to `IFraCertificationChecker` for FraCertification-strategy types
3. **Qualification status** — employee holds an Active or ExpiringSoon qualification for each blocking type

Returns `EligibilityResult` with `IsEligible` flag and list of `BlockingReason(RuleCode, Description)`.

### `SkipContextProvider` integration (Persistence layer)

The existing `SkipContextProvider` (from B04) calls `EmployeeEligibilityService.CheckEligibilityAsync`
to populate `SkipContext.IsQualified` and `SkipContext.QualificationBlockingReasons`.

### `VacancyResolutionEngine` skip logging

When the `QualificationRule` (`NOT_QUALIFIED`) fires, `BuildSkipDecisionJson` serializes the
blocking reasons into the `DispatchDecisionLog` payload for audit.

---

## 6. Background Workers

| Worker | Schedule | Logic |
|--------|----------|-------|
| `QualificationExpiryNotifyWorker` | Every 24h | Scans all qualifications with `ExpiresAtUtc`; marks Active → ExpiringSoon at 60/30/14/7 day thresholds |
| `QualificationExpiryEnforcerWorker` | Every 1h | Finds qualifications past `ExpiresAtUtc + GraceDays`; transitions to Expired status |
| `PrerequisiteEvaluationWorker` | Every 24h | For all active non-Manual qualification types, evaluates all employees against prerequisites; auto-creates Pending qualifications when all prereqs satisfied |

---

## 7. Reactive Event Dispatch

### `IDomainEventReactor` — Domain interface

```
IDomainEventReactor
  Task ReactAsync(IReadOnlyList<DomainEvent> events, CancellationToken ct)
```

### `DomainEventReactor` — Infrastructure implementation

Dispatches committed domain events to in-process handlers. Currently routes:
- `OnDutyRecordCreatedDomainEvent` → `QualificationReactiveService.HandleOnDutyRecordCreatedAsync`
- `EmployeeCreatedDomainEvent` → `QualificationReactiveService.HandleEmployeeCreatedAsync`

Uses `IServiceScopeFactory` to resolve scoped services per dispatch batch.

---

## 8. gRPC Contract (`qualifications.proto`)

| RPC | HTTP Mapping | Purpose |
|-----|-------------|---------|
| `CreateQualificationType` | POST `/v1/qualifications/types` | Create a new qualification type for a parent |
| `SetQualificationTypeActive` | POST `/v1/qualifications/types/active` | Activate/deactivate a type |
| `GetQualificationTypes` | GET `/v1/qualifications/types/{parent_ctrl_nbr}` | List types for a parent (optional active-only filter) |
| `GetEmployeeQualifications` | GET `/v1/qualifications/employees/{employee_ctrl_nbr}` | List qualifications for an employee |
| `GrantEmployeeQualification` | POST `/v1/qualifications/grant` | Manually grant a qualification to an employee |
| `RevokeEmployeeQualification` | POST `/v1/qualifications/revoke` | Revoke a qualification with reason |
| `CheckEligibility` | POST `/v1/qualifications/check-eligibility` | Check employee eligibility for a position slot |

---

## 9. Frontend (BlazorUI)

### `QualificationsClient` — gRPC client wrapper

Wraps all 7 RPCs with error logging via `BaseGrpcClient`.

### `Qualifications.razor` — Page (`/employees/qualifications`)

| Section | Features |
|---------|----------|
| Qualification Types table | DataTable with code/name/strategy/blocking/status columns, Create Type modal, Activate/Deactivate toggle |
| Employee Qualification Dashboard | FilterSelect employee picker, DataTable with code/status/achieved/expires columns |
| Eligibility Check tool | Manual employee + position slot input, displays eligible/not-eligible badge with blocking reasons |

### Navigation

Added under **People** nav group with `employees/qualifications` permission key.

---

## 10. Commit Sequence

### Commit 1: `gap(quals): add qualification aggregates and persistence mappings`
- `QualificationType`, `QualificationPrerequisite`, `EmployeeQualification`, `QualificationEvidence` entities
- `IQualificationTypeRepository`, `IQualificationPrerequisiteRepository`, `IEmployeeQualificationRepository` interfaces
- EF configurations (`QualificationTypeConfiguration`, `EmployeeQualificationConfiguration`)
- Repository implementations, `DbContext` DbSets, persistence DI wiring

### Commit 2: `gap(quals): wire evaluators, eligibility gate, workers, and reactive handlers`
- `IPrerequisiteEvaluator` interface and 5 evaluator implementations
- `PrerequisiteEvaluationService` orchestrator
- `EmployeeEligibilityService` with craft membership + FRA cert + qualification checks
- `QualificationReactiveService` for event-driven re-evaluation
- `IDomainEventReactor` interface + `DomainEventReactor` infrastructure implementation
- `QualificationDomainEvents` (6 event types)
- `IOnDutyRecordCounter`, `ICraftMembershipDateProvider`, `IFraCertificationChecker` read providers
- 3 background workers (ExpiryNotify, ExpiryEnforcer, PrerequisiteEvaluation)
- `SkipContext.IsQualified` / `QualificationBlockingReasons` fields + skip logging in `VacancyResolutionEngine`
- Application + Infrastructure DI wiring

### Commit 3: `gap(quals): add qualifications gRPC contract and service endpoints`
- `qualifications.proto` with 7 RPCs and all request/response messages
- `QualificationsService` gRPC service implementation in Presentation layer
- Proto reference in `CrewService.Presentation.csproj`

### Commit 4: `feat(frontend): add employee qualifications UI and navigation`
- `QualificationsClient` gRPC client wrapper
- `Qualifications.razor` page with type management, employee dashboard, eligibility check
- NavMenu link under People group
- `Program.cs` client registration

### Commit 5: `test(quals): cover eligibility, prerequisite orchestration, and skip logging`
- `EmployeeEligibilityServiceTests` — craft membership, FRA cert, blocking qualification scenarios
- `PrerequisiteEvaluationServiceTests` — all-satisfied auto-creation, partial failure, no-prereq passthrough
- `VacancyAssignmentTests` — qualification skip logging with blocking reasons in decision log

### Commit 6: `docs(gap): update implementation plan progress for B15`
- Added Phase 12 (Qualifications & Requirements) to `00-plan-overview.md`
- Updated dependency graph, summary table, branch count

### Commit 7: `fix(ef): add b15 migration for qualification model changes`
- EF Core migration `20260415022715_B15Qualifications`
- Updated `CrewServiceDbContextModelSnapshot`

---

## 11. Acceptance Scenarios

### Scenario 1: Create and query qualification types

```
GIVEN a parent with no qualification types
WHEN admin creates a type with Code="FOREMAN", EvaluationStrategy="Manual", IsBlocking=true
THEN the type appears in GetQualificationTypes with Code="FOREMAN", IsActive=true
AND QualificationTypeCreatedDomainEvent is raised
```

### Scenario 2: Grant and revoke employee qualification

```
GIVEN QualificationType "FOREMAN" exists
AND Employee A has no qualifications
WHEN admin grants FOREMAN to Employee A with Status="Active"
THEN GetEmployeeQualifications returns 1 qualification with Status="Active"
AND EmployeeQualificationGrantedDomainEvent is raised

WHEN admin revokes the qualification with reason "Failed re-certification"
THEN qualification Status becomes "Revoked", RevokedAtUtc is set
AND EmployeeQualificationRevokedDomainEvent is raised
```

### Scenario 3: Prerequisite auto-evaluation

```
GIVEN QualificationType "SENIOR_FOREMAN" with EvaluationStrategy="TimeFromEvent"
AND prerequisite: Kind="TimeFromEvent", Threshold=365, ThresholdUnit="Days", EventSource="EmploymentDate"
AND Employee B was hired 400 days ago
WHEN PrerequisiteEvaluationService evaluates Employee B
THEN all prerequisites satisfied
AND EmployeeQualification created with Status="Pending", GrantedBy="SYSTEM"
AND evidence record links to the prerequisite
```

### Scenario 4: Vacancy assignment skip on missing qualification

```
GIVEN PositionSlot requires blocking QualificationType "FOREMAN"
AND Employee C is on ExtraBoard but has no FOREMAN qualification
WHEN VacancyResolutionEngine evaluates Employee C
THEN Employee C is skipped with RuleCode="NOT_QUALIFIED"
AND DispatchDecisionLog records blocking reasons including "NOT_QUALIFIED: Missing or none qualification: Foreman"
```

### Scenario 5: Expiration lifecycle

```
GIVEN Employee D holds FOREMAN qualification expiring in 30 days
WHEN QualificationExpiryNotifyWorker runs
THEN qualification Status changes to "ExpiringSoon"
AND EmployeeQualificationExpiringSoonDomainEvent is raised with DaysRemaining=30

GIVEN the same qualification is now past ExpiresAtUtc + GraceDays
WHEN QualificationExpiryEnforcerWorker runs
THEN qualification Status changes to "Expired"
AND EmployeeQualificationExpiredDomainEvent is raised
```

### Scenario 6: Reactive re-evaluation on OnDutyRecord

```
GIVEN QualificationType "EXPERIENCED" with EvaluationStrategy="ActivityCount"
AND prerequisite: Kind="ActivityCount", Threshold=50, ThresholdUnit="Count"
AND Employee E has 49 completed on-duty records
WHEN Employee E completes their 50th on-duty record (OnDutyRecordCreatedDomainEvent)
THEN DomainEventReactor dispatches to QualificationReactiveService
AND PrerequisiteEvaluationService creates a Pending qualification for Employee E
```

### Scenario 7: Eligibility check with FRA certification

```
GIVEN PositionSlot requires QualificationType with EvaluationStrategy="FraCertification"
AND Employee F has an active FRA EmployeeCertification (from B01)
WHEN CheckEligibility is called for Employee F
THEN result is IsEligible=true, no blocking reasons

GIVEN Employee G has no active FRA certification
WHEN CheckEligibility is called for Employee G
THEN result is IsEligible=false, blocking reason "FRA_CERT_MISSING"
```
