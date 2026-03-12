# Impl Spec: `feature/gap-holiday-payroll`

**Priority**: P2 – Medium  
**Depends on**: `gap-payroll-engine`, `gap-mark-off-system`  
**Depended on by**: Nothing

## Overview

Adds holiday definitions, qualification rules, and holiday-specific payroll record
generation. SA entities: `Holiday`, `HolidayQualifyRecord`, `PayrollHolidayRecord`.

---

## 1. Aggregate Design

### `Holiday` (root) — Payroll module (new reference entity)

### `HolidayQualificationRule` (root) — Payroll module

Defines per-craft/work-area rules for holiday pay qualification.

---

## 2. Entity Catalog

### `Holiday`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| Name | string | "New Year's Day", etc. |
| ObservedDate | DateOnly | |
| IsActive | bool | |

### `HolidayQualificationRule`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| HolidayCtrlNbr | ControlNumber | FK → Holiday |
| CraftCtrlNbr | ControlNumber? | Null = all crafts |
| RequireWorkDayBefore | bool | Must work the day before |
| RequireWorkDayAfter | bool | Must work the day after |
| ExemptAbsenceCodes | string? | JSON array of AbsenceCode codes that don't disqualify |

### `HolidayPayrollRecord`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| HolidayCtrlNbr | ControlNumber | FK → Holiday |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| PayrollRecordCtrlNbr | ControlNumber | FK → PayrollRecord |
| IsQualified | bool | Result of qualification check |
| DisqualificationReason | string? | |

---

## 3. Commit Sequence

### Commit 1: `gap(holiday): add Holiday reference entity`
### Commit 2: `gap(holiday): add HolidayQualificationRule entity and service`
### Commit 3: `gap(holiday): add HolidayPayrollRecord entity and generation service`
### Commit 4: `gap(holiday): add gRPC endpoints and unit tests`

---

## 4. Acceptance Scenarios

**Scenario 1: Holiday qualification — passes**
```
GIVEN Holiday "July 4th" with RequireWorkDayBefore = true
  AND Employee A worked July 3rd
WHEN holiday qualification runs
THEN HolidayPayrollRecord.IsQualified = true
```

**Scenario 2: Holiday qualification — fails**
```
GIVEN same holiday rule
  AND Employee B was marked off July 3rd with code "V1" (not in exempt list)
WHEN holiday qualification runs
THEN HolidayPayrollRecord.IsQualified = false
  AND DisqualificationReason = "Did not work day before"
```
