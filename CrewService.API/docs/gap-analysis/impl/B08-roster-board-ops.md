# Impl Spec: `feature/gap-roster-board-ops`

**Priority**: P2 – Medium  
**Depends on**: `gap-daily-operations`, `gap-mark-off-system`  
**Depended on by**: Nothing

## Overview

Adds roster board lifecycle, hangout processing, board mark-offs, and daily employee
status records. SA entities: `RosterBoard`, `RosterBoardPosition`,
`DailyRosterBoardPositionHangoutRecord`, `DailyRailroadEmployeeStatusRecord`, etc.

---

## 1. Aggregate Design

### `RosterBoard` (root) — Boards module (extends existing)

```
RosterBoard (aggregate root)
  └── RosterBoardPosition (child)
```

- `RosterBoard.WorkAreaGroupCtrlNbr` → FK to DynamicGroup
- `RosterBoardPosition.EmployeeCtrlNbr` → FK to Employee

### `DailyEmployeeStatusRecord` (root) — new, Dispatching module

Daily snapshot of employee operational status. Standalone aggregate.

---

## 2. Entity Catalog

### `RosterBoard`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| CraftCtrlNbr | ControlNumber | FK → Craft |
| Name | string | |
| IsActive | bool | |

### `RosterBoardPosition`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| RosterBoardCtrlNbr | ControlNumber | FK → parent |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| PositionOrder | int | |
| HangoutStatus | string | "Active" / "HungOut" / "MarkedOff" |
| HangoutAtUtc | DateTime? | |

### `DailyEmployeeStatusRecord`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| RecordDate | DateOnly | |
| StatusCode | string | On-duty, off-duty, marked-off, available, etc. |
| SnapshotJson | string? | Point-in-time state capture |

---

## 3. Commit Sequence

### Commit 1: `gap(roster): add RosterBoard and RosterBoardPosition entities`
### Commit 2: `gap(roster): add hangout processing service`
### Commit 3: `gap(roster): add DailyEmployeeStatusRecord entity and snapshot service`
### Commit 4: `gap(roster): add roster board mark-off handling`
### Commit 5: `gap(roster): add gRPC endpoints and unit tests`

---

## 4. Acceptance Scenarios

**Scenario 1: Hangout processing**
```
GIVEN a RosterBoardPosition with HangoutStatus = "Active"
WHEN the hangout timer fires and the employee is available
THEN HangoutStatus = "HungOut", HangoutAtUtc is set
```

**Scenario 2: Daily status snapshot**
```
GIVEN Employee A is on-duty, Employee B is marked off
WHEN DailyEmployeeStatusRecord generation runs for today
THEN record for A has StatusCode = "OnDuty"
  AND record for B has StatusCode = "MarkedOff"
```
