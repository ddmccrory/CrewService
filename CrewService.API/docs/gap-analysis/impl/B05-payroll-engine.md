# Impl Spec: `feature/gap-payroll-engine`

**Priority**: P1 – High  
**Depends on**: `gap-daily-operations` (OnDutyRecord, OffDutyRecord), `gap-mark-off-system` (AbsenceCode)  
**Depended on by**: holiday-payroll, reporting-exports

## Overview

Extends the existing `TimeEntry`, `PayrollRun`, and `PayrollRecord` entities with earning
code resolution, approval routing, pay rate calculation, and period processing. SA's earning
code branches are one railroad's rules — CrewService models them as a configurable rule engine.

---

## 1. Aggregate Design

### Aggregate 1: `PayrollRun` (existing, extended) — Payroll module

```
PayrollRun (aggregate root — existing)
  └── PayrollRecord (child — existing, extended with earning code + approval)
```

### Aggregate 2: `EarningCodeRule` (root) — Payroll module (new reference data)

Standalone configurable rule entity. One per condition set per work area.

### Aggregate 3: `EarningApproval` (root) — Payroll module

Separate from PayrollRecord because approval routing may span multiple records
and involves officer resolution from a different bounded context.

**Relationships**:
- `PayrollRecord.OnDutyRecordCtrlNbr` → FK to `OnDutyRecord` (from daily-ops)
- `PayrollRecord.EarningCodeRuleCtrlNbr` → FK to `EarningCodeRule`
- `EarningApproval.PayrollRecordCtrlNbr` → FK to `PayrollRecord`

---

## 2. Entity Catalog

### `EarningCodeRule` — Payroll module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| Priority | int | Evaluated in order, first match wins |
| ConditionsJson | string | Structured conditions (see §10 of 07-design-improvements) |
| ResultCode | string | The earning code assigned |
| IsActive | bool | |

### `PayRate` — Payroll module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| PositionRoleCtrlNbr | ControlNumber? | FK → PositionRole (null = default rate) |
| CraftCtrlNbr | ControlNumber | FK → Craft |
| EffectiveDate | DateTime | |
| HourlyRate | decimal | |
| OvertimeMultiplier | decimal | Default 1.5 |

### `EarningApproval` — Payroll module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| PayrollRecordCtrlNbr | ControlNumber | FK → PayrollRecord |
| ApprovalTier | int | 1 = default officer, 2 = code-specific, 3 = position-based |
| OfficerCtrlNbr | ControlNumber | FK → Employee |
| Status | string | "PENDING" → "APPROVED" / "DECLINED" |
| DecidedAtUtc | DateTime? | |

### Extensions to existing `PayrollRecord`

| New Property | Type | Notes |
|-------------|------|-------|
| OnDutyRecordCtrlNbr | ControlNumber? | FK → OnDutyRecord |
| ResolvedEarningCode | string | Output of rule engine |
| RequiresApproval | bool | From earning code rule |

---

## 3. Domain Event Catalog

| Event | Published By | Subscribers |
|-------|-------------|-------------|
| `EarningCodeResolvedDomainEvent` | `EarningCodeResolver` | Approval routing if required |
| `EarningApprovalDecidedDomainEvent` | `EarningApproval.Decide()` | PayrollRecord status update |
| `PayrollPeriodLockedDomainEvent` | `PayrollRun.Lock()` | Export readiness check |

### Reactive: subscribes to daily-ops events

| Trigger | Handler | Action |
|---------|---------|--------|
| `OnDutyRecordCreatedDomainEvent` | `PayrollEarningHandler` | Resolve earning code, create PayrollRecord |
| `OffDutyRecordCreatedDomainEvent` | `PayrollTieUpHandler` | Finalize hours, calculate pay amount |

---

## 4. Configuration Model

`EarningCodeRule` entities ARE the configuration — each tenant defines their own rules
as data rows. The original railroad's 20+ branches become seed data.

---

## 5. Commit Sequence

### Commit 1: `gap(payroll): add EarningCodeRule entity and resolver service`
### Commit 2: `gap(payroll): add PayRate entity`
### Commit 3: `gap(payroll): extend PayrollRecord with earning code and OnDutyRecord FK`
### Commit 4: `gap(payroll): add EarningApproval entity and routing service`
### Commit 5: `gap(payroll): add period processing service (trial/final)`
### Commit 6: `gap(payroll): add reactive event handlers`
### Commit 7: `gap(payroll): add gRPC endpoints`
### Commit 8: `gap(payroll): add unit tests`

---

## 6. Acceptance Scenarios

**Scenario 1: Earning code resolution**
```
GIVEN EarningCodeRule priority 1: IsOffDay=true, IsHoliday=false → code "OT"
  AND EarningCodeRule priority 2: IsOffDay=true, IsHoliday=true → code "HO"
  AND employee works on an off-day that is not a holiday
WHEN EarningCodeResolver evaluates
THEN ResolvedEarningCode = "OT" (first match wins)
```

**Scenario 2: Three-tier approval**
```
GIVEN an earning code that requires approval
WHEN EarningApproval is created
THEN Tier 1 officer is resolved from default payroll officer
  AND if Tier 1 officer IS the employee → escalate to Tier 2 (code-specific)
```
