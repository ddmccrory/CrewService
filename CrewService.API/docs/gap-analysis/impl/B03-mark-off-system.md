# Impl Spec: `feature/gap-mark-off-system`

**Priority**: P0 – Core  
**Depends on**: `gap-daily-operations` (PositionSlotInstance, OnDutyRecord)  
**Depended on by**: vacancy-assignment, payroll-engine, fra-compliance (SR/NR/NN auto marks)

## Overview

This branch expands the simplified `AbsenceRequest` into a full mark-off/mark-up lifecycle
with configurable absence codes, auto-markup durations, approval workflows, compensation
balance tracking, and board-impact side effects.

SA has ~15 mark-off entities. CrewService currently has 2 (`AbsenceRequest`, `VacancyImpact`).

---

## 1. Aggregate Design

### Aggregate 1: `AbsenceCode` (root) — Module: AbsenceVacancy

Reference data entity. SA's `MarkOffCode` equivalent. Standalone aggregate — rarely
mutated after initial setup.

### Aggregate 2: `AbsenceRequest` (root, existing — extended) — Module: AbsenceVacancy

The existing `AbsenceRequest` is extended with new properties and child entities.

```
AbsenceRequest (aggregate root — existing, extended)
  ├── AbsenceApproval (child entity — new)
  └── AbsenceMarkUp (child entity — new)
```

**Why children**: Approval and mark-up are tightly coupled to the absence lifecycle.
They are always queried and validated together with the parent request.

### Aggregate 3: `CompensationBalance` (root) — Module: AbsenceVacancy

Running balance of compensated absence hours per employee per compensation type.
Separate aggregate because it spans multiple `AbsenceRequest` instances.

**Relationship to existing entities**:
- `AbsenceRequest.AbsenceCodeCtrlNbr` → FK to `AbsenceCode` (new)
- `AbsenceRequest.PositionSlotCtrlNbr` → FK to `PositionSlotInstance` (from gap-daily-operations)
- `AbsenceRequest.EmployeeCtrlNbr` → FK to `Employee` (existing)
- `CompensationBalance.EmployeeCtrlNbr` → FK to `Employee` (existing)

---

## 2. Entity Catalog

### `AbsenceCode` — AbsenceVacancy module (new reference entity)

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| Code | string | Short code: "V1", "SR", "NR", "NN", "CD", "PD", etc. |
| Description | string | "Vacation Week 1", "Safety Rest", etc. |
| IsExcused | bool | Excused absence |
| IsCompensated | bool | Draws from compensation balance |
| RequiresApproval | bool | Needs officer approval before taking effect |
| IsSystemOnly | bool | Only system can create (SR, NR, NN) |
| IsHolidayExempt | bool | Does not apply on holidays |
| DefaultAutoMarkUpHours | decimal? | Null = no auto mark-up; e.g., V1=168h, CD=24h |
| IsActive | bool | |

### `AbsenceCodeCraftOverride` — AbsenceVacancy module (new)

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| AbsenceCodeCtrlNbr | ControlNumber | FK → AbsenceCode |
| CraftCtrlNbr | ControlNumber | FK → Craft |
| OverrideAutoMarkUpHours | decimal | Craft-specific override of default duration |

### `AbsenceApproval` — AbsenceVacancy module (child of AbsenceRequest)

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| AbsenceRequestCtrlNbr | ControlNumber | FK → AbsenceRequest (parent) |
| ApprovalOfficerCtrlNbr | ControlNumber | FK → Employee |
| Status | string | "PENDING" → "APPROVED" / "DECLINED" |
| DecidedAtUtc | DateTime? | |
| Notes | string? | |

### `AbsenceMarkUp` — AbsenceVacancy module (child of AbsenceRequest)

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| AbsenceRequestCtrlNbr | ControlNumber | FK → AbsenceRequest (parent) |
| ScheduledMarkUpUtc | DateTime | Computed: StartUtc + auto-markup hours |
| ActualMarkUpUtc | DateTime? | Null until mark-up occurs |
| IsAutoMarkUp | bool | System-generated vs. manual |

### `CompensationBalance` — AbsenceVacancy module (new)

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| CompensationType | string | E.g., "VACATION", "PERSONAL", "COMP" |
| BalanceHours | decimal | Running total, decremented on use |
| AsOfUtc | DateTime | Last update timestamp |

### Extensions to existing `AbsenceRequest`

| New Property | Type | Notes |
|-------------|------|-------|
| AbsenceCodeCtrlNbr | ControlNumber | FK → AbsenceCode (replaces string `ReasonCode`) |
| PositionSlotCtrlNbr | ControlNumber? | FK → PositionSlotInstance (from gap-daily-operations) |
| MarkOffStartUtc | DateTime? | Actual mark-off effective time (may differ from request StartUtc) |
| IsSystemGenerated | bool | Created by FRA compliance, holiday, etc. |

---

## 3. Domain Event Catalog

| Event | Published By | Subscribers |
|-------|-------------|-------------|
| `AbsenceCodeCreatedDomainEvent` | `AbsenceCode.Create()` | — |
| `AbsenceRequestedDomainEvent` (existing) | `AbsenceRequest.Create()` | Approval routing, vacancy impact creation |
| `AbsenceApprovalDecidedDomainEvent` | `AbsenceApproval.Decide()` | If approved: mark-off activation, board impact |
| `AbsenceMarkUpScheduledDomainEvent` | `AbsenceMarkUp.Create()` | Background worker schedules the future mark-up |
| `AbsenceMarkedUpDomainEvent` | `AbsenceMarkUp.Execute()` | Board repositioning, vacancy impact clearance, compensation balance update |
| `CompensationBalanceDebitedDomainEvent` | `CompensationBalance.Debit()` | Auto-remove pending requests if balance ≤ 0 |

### Event Flow: Mark-Off with Auto Mark-Up

```
CreateAbsenceRequest(employeeCtrlNbr, absenceCodeCtrlNbr, startUtc)
  → Load AbsenceCode — check RequiresApproval
  → If RequiresApproval:
      → Create AbsenceApproval(PENDING)
      → raises AbsenceRequestedDomainEvent
      → [wait for approval decision]
  → If !RequiresApproval or after approval:
      → Resolve auto-markup hours (AbsenceCode.Default → CraftOverride)
      → Create AbsenceMarkUp(scheduledMarkUpUtc = startUtc + hours)
      → If IsCompensated: debit CompensationBalance
      → Create VacancyImpact (existing) for affected PositionSlotInstance
      → If on extra board: reposition to back (OrderIndex += large offset)
      → raises AbsenceMarkedUpDomainEvent (when auto mark-up fires)
```

---

## 4. Configuration Model

No new policy entity needed. The `AbsenceCode` + `AbsenceCodeCraftOverride` reference
entities serve as the configuration surface. Each tenant defines their own codes with
their own flags and durations — the original railroad's 20+ codes become seed data.

---

## 5. Commit Sequence

### Commit 1: `gap(mark-off): add AbsenceCode and AbsenceCodeCraftOverride entities`
- Domain entities, EF config, migration, basic CRUD gRPC
- No dependencies on other gap branches

### Commit 2: `gap(mark-off): extend AbsenceRequest with AbsenceCode FK and new properties`
- Add `AbsenceCodeCtrlNbr`, `PositionSlotCtrlNbr`, `MarkOffStartUtc`, `IsSystemGenerated` to existing `AbsenceRequest`
- Migration to add columns
- Update existing `Create()` factory method
- **Depends on**: Commit 1

### Commit 3: `gap(mark-off): add AbsenceApproval child entity`
- Domain entity, EF config as owned by AbsenceRequest
- Approval routing logic in application service
- **Depends on**: Commit 2

### Commit 4: `gap(mark-off): add AbsenceMarkUp child entity and auto-markup service`
- Domain entity, EF config
- `AutoMarkUpService` — resolves duration from AbsenceCode → CraftOverride, creates AbsenceMarkUp
- **Depends on**: Commits 2, 3

### Commit 5: `gap(mark-off): add CompensationBalance entity and balance tracking`
- Domain entity, EF config
- `CompensationBalanceService` — debit on mark-off, credit on cancellation
- Auto-remove unfulfilled requests when balance ≤ 0
- **Depends on**: Commit 2

### Commit 6: `gap(mark-off): add board-impact side effects`
- Domain event handler for `AbsenceRequestedDomainEvent` → reposition `BoardMember.OrderIndex`
- Domain event handler for `AbsenceMarkedUpDomainEvent` → restore board position
- **Depends on**: Commits 2, 4; references `BoardMember` (existing Boards module)

### Commit 7: `gap(mark-off): add gRPC presentation for mark-off operations`
- Proto definitions, query/command endpoints
- **Depends on**: All previous commits

### Commit 8: `gap(mark-off): add unit tests`
- Approval workflow, auto-markup duration resolution, compensation balance, board repositioning
- **Depends on**: All previous commits

---

## 6. Acceptance Scenarios

### Mark-Off Codes

**Scenario 1: Tenant-configurable codes**
```
GIVEN AbsenceCode "V1" with DefaultAutoMarkUpHours = 168, IsCompensated = true
  AND AbsenceCodeCraftOverride for Craft "Yardmaster" with OverrideAutoMarkUpHours = 120
WHEN resolving auto-markup hours for a Yardmaster employee using code "V1"
THEN the resolved hours = 120 (craft override wins)
```

### Approval Workflow

**Scenario 2: Approval-required mark-off**
```
GIVEN AbsenceCode "V1" with RequiresApproval = true
WHEN CreateAbsenceRequest(employee, "V1", startUtc)
THEN AbsenceRequest.Status = "PENDING"
  AND an AbsenceApproval is created with Status = "PENDING"
  AND no VacancyImpact is created yet
```

**Scenario 3: Approval granted**
```
GIVEN a pending AbsenceApproval
WHEN AbsenceApproval.Decide("APPROVED", officerCtrlNbr)
THEN AbsenceRequest.Status = "APPROVED"
  AND AbsenceMarkUp is created with ScheduledMarkUpUtc
  AND VacancyImpact is created for the employee's position slot
```

### Auto Mark-Up

**Scenario 4: Auto mark-up fires**
```
GIVEN an active absence with AbsenceMarkUp.ScheduledMarkUpUtc = 2025-07-07 07:00
WHEN the current time reaches 2025-07-07 07:00
THEN AbsenceMarkUp.ActualMarkUpUtc is set
  AND AbsenceRequest.Status = "COMPLETED"
  AND VacancyImpact.ImpactEndUtc is set
  AND AbsenceMarkedUpDomainEvent is raised
```

### System-Generated Marks

**Scenario 5: FRA safety rest (SR) — system only**
```
GIVEN AbsenceCode "SR" with IsSystemOnly = true
  AND an employee with ConsecutiveDays = 6
WHEN FRA compliance handler detects the threshold
THEN an AbsenceRequest is created with IsSystemGenerated = true, Code = "SR"
  AND no approval is required (system marks bypass approval)
```

### Board Impact

**Scenario 6: Extra board repositioning on mark-off**
```
GIVEN Employee A is BoardMember with OrderIndex = 3 on ExtraBoard "XB-Yard"
WHEN Employee A is marked off
THEN BoardMember.OrderIndex is set to a large value (pushed to back)
  AND when marked up, BoardMember.OrderIndex is restored based on tie-up order
```
