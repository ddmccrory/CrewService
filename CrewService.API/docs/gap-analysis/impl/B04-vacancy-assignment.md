# Impl Spec: `feature/gap-vacancy-assignment`

**Priority**: P0 – Core  
**Depends on**: `gap-daily-operations` (PositionSlotInstance), `gap-mark-off-system` (AbsenceCode, board impact), Boards module (existing)  
**Depended on by**: electronic-calling, background-services

## Overview

Builds the composable vacancy resolution engine that replaces SA's 500-line
`UpdateDailyCrewPositionVacancies()` method. Uses the existing `DispatchProjection`,
`DispatchDecisionLog`, `BoardMember`, `BoardCascadePolicy`, and `VacancyImpact` entities.

---

## 1. Aggregate Design

No new aggregates. This branch adds **application services and interfaces** that
orchestrate existing entities:

- `PositionSlotInstance` (from gap-daily-operations) — the slot being filled
- `BoardMember` (existing Boards module) — candidates for assignment
- `DispatchProjection` (existing Dispatching) — computed projection record
- `DispatchDecisionLog` (existing Dispatching) — audit trail of skip/select decisions
- `VacancyImpact` (existing AbsenceVacancy) — absence-to-slot link
- `BoardCascadePolicy` (existing Boards) — which boards to search in what order
- `OnDutyRecord` (from gap-daily-operations) — created when assignment is made

New entity needed: `VacancyResolutionRun` — tracks a single execution of the engine
for a work area + shift, providing idempotency and audit.

### `VacancyResolutionRun` — Dispatching module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| ShiftInstanceCtrlNbr | ControlNumber | FK → ShiftInstance |
| StartedAtUtc | DateTime | |
| CompletedAtUtc | DateTime? | |
| SlotsEvaluated | int | |
| SlotsFilled | int | |
| Status | string | "Running" → "Completed" / "Failed" |

---

## 2. Skip Rule Pipeline Design

### `ISkipRule` interface — Application layer

```
ISkipRule
  bool ShouldSkip(BoardMember candidate, PositionSlotInstance slot, SkipContext ctx)
  string RuleCode  // e.g., "WORKED_CAP", "ON_DUTY", "NOT_RESTED"
```

### Built-in skip rules (each is a separate class)

| Rule Class | RuleCode | SA Equivalent | Data Source |
|-----------|----------|--------------|-------------|
| `WorkedCapRule` | WORKED_CAP | 12-day worked cap | Count recent OnDutyRecords |
| `AlreadyOnDutyRule` | ON_DUTY | Already on-duty check | OnDutyRecord with Status = "OnDuty" |
| `AvailabilityRule` | NOT_AVAILABLE | Available by calling window end | OffDutyRecord.RestedAtUtc |
| `RestRule` | NOT_RESTED | Rested by calling window end | Delegates to FRA rest check if craft covered |
| `MarkOffRule` | MARKED_OFF | Active mark-off check | AbsenceRequest with Status = "APPROVED" |
| `QualificationRule` | NOT_QUALIFIED | Position requirements check | `EmployeeCertification` status from FRA module (B01) |
| `WeeklyHoursCapRule` | WEEKLY_CAP | 40h/week for some crafts | CraftOperationsPolicy.WeeklyHoursCap |

### `IAssignmentStrategy` interface

```
IAssignmentStrategy
  AssignmentResult TryAssign(BoardMember candidate, PositionSlotInstance slot, AssignmentContext ctx)
```

| Strategy | SA Equivalent | Notes |
|----------|--------------|-------|
| `StandardAssignmentStrategy` | Default path | Direct assignment |
| `ForemanHelperStrategy` | Helper search + swap | Configurable search scope (replaces hard-coded locations) |

---

## 3. Domain Event Catalog

| Event | Published By | Subscribers |
|-------|-------------|-------------|
| `VacancyResolutionCompletedDomainEvent` | `VacancyResolutionEngine` | Notification (gap-electronic-calling), logging |
| `PositionSlotFilledByVacancyDomainEvent` | Engine after assignment | Updates DispatchProjection, triggers OnDutyPlacementService |

### Reactive triggers (subscribes to events from other branches)

| Trigger Event | Handler | Action |
|--------------|---------|--------|
| `ShiftInstanceCreatedDomainEvent` (daily-ops) | `VacancyEvaluationTrigger` | Queue vacancy evaluation for new shift |
| `PositionSlotStatusChangedDomainEvent` (daily-ops) | `VacancyEvaluationTrigger` | Re-evaluate if slot became "Open" |
| `AbsenceMarkedUpDomainEvent` (mark-off) | `BoardAvailabilityHandler` | Employee now available; re-evaluate |

---

## 4. Configuration Model

Uses existing `BoardCascadePolicy` + `CraftOperationsPolicy` (from gap-daily-operations):

- `CraftOperationsPolicy.VacancyScope` — Roster vs WorkArea level query
- `CraftOperationsPolicy.BoardSortStrategy` — TieUpFirst / BoardOrderFirst / FIFO
- `CraftOperationsPolicy.HelperSearchEnabled` — enables ForemanHelperStrategy
- `BoardCascadePolicy` (existing) — board search order per craft/work area

---

## 5. Commit Sequence

### Commit 1: `gap(vacancy): add ISkipRule interface and built-in rule implementations`
- Interface + 7 rule classes in Application layer
- Unit-testable in isolation with no infrastructure dependencies

### Commit 2: `gap(vacancy): add IAssignmentStrategy interface and implementations`
- Standard + ForemanHelper strategies
- Configurable search scope via CraftOperationsPolicy

### Commit 3: `gap(vacancy): add VacancyResolutionRun entity`
- Domain entity, EF config, migration

### Commit 4: `gap(vacancy): add VacancyResolutionEngine orchestrator`
- Loads open slots for a shift, resolves boards via BoardCascadePolicy
- Iterates candidates through ISkipRule pipeline
- Assigns via IAssignmentStrategy, logs to DispatchDecisionLog
- Creates VacancyResolutionRun tracking record

### Commit 5: `gap(vacancy): add NoBidBulletinHandler pre-phase`
- Runs BEFORE general vacancy processing for each shift
- Queries `Bulletin` records with zero bids past bid deadline
- Force-assigns by reverse seniority (most junior qualified employee)
- Logs to DispatchDecisionLog with reason "NoBidForceAssign"
- **Depends on**: Commit 4, existing Bulletins module (Bulletin, BulletinBid)

### Commit 6: `gap(vacancy): add reactive event handlers`
- Subscribe to ShiftInstanceCreated, PositionSlotStatusChanged, AbsenceMarkedUp
- Trigger VacancyResolutionEngine for affected work area/shift

### Commit 7: `gap(vacancy): add gRPC endpoints`
- Manual trigger, query resolution runs, query decision logs

### Commit 8: `gap(vacancy): add unit tests`
- Each skip rule independently, engine with mock rules, strategy selection
- No-bid force-assignment by reverse seniority

---

## 6. Acceptance Scenarios

**Scenario 1: Standard vacancy fill**
```
GIVEN ShiftInstance with 1 open PositionSlotInstance
  AND ExtraBoard "XB-Yard" with 3 BoardMembers ordered [A, B, C]
  AND Employee A is marked off, Employee B is rested and available
WHEN VacancyResolutionEngine runs
THEN Employee A is skipped (MARKED_OFF logged to DispatchDecisionLog)
  AND Employee B is assigned to the slot
  AND PositionSlotInstance.Status = "Filled", IncumbentEmployeeCtrlNbr = B
  AND DispatchProjection is updated
```

**Scenario 2: All candidates exhausted**
```
GIVEN 1 open slot and all board members fail skip rules
WHEN VacancyResolutionEngine runs
THEN PositionSlotInstance.Status remains "Open"
  AND DispatchDecisionLog contains one entry per candidate with skip reason
  AND VacancyResolutionRun.SlotsFilled = 0
```

**Scenario 3: Board cascade**
```
GIVEN BoardCascadePolicy: search "XB-Primary" first, then "XB-Overtime"
  AND no candidates pass on XB-Primary
WHEN VacancyResolutionEngine runs
THEN XB-Overtime members are evaluated
  AND DispatchDecisionLog shows cascade progression
```

**Scenario 4: Craft-specific vacancy scope**
```
GIVEN CraftOperationsPolicy.VacancyScope = "WorkArea" (like SA pool 50/MoW)
WHEN VacancyResolutionEngine collects open slots
THEN slots from ALL rosters in the work area are included (not just one roster)
```
