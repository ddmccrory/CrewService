# Spec_02: Operations, Boards, Mark-Off, Vacancy, and Shifts
# Part 4: DailyCrewPosition Business Logic

## DailyCrewPosition

**Inherits**: `ControlNumberBase` (partial class)

Represents a single position on a daily assignment (call sheet). One per crew position per day per shift.

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `DailyAssignmentControlNumber` | `long` | `[Required, ForeignKey("DailyAssignment")]` |
| `RailroadPositionControlNumber` | `long` | `[Required, ForeignKey("RailroadPosition")]` |
| `CrewControlNumber` | `long` | `[Required, ForeignKey("Crew")]` |
| `PositionControlNumber` | `long` | `[Required, ForeignKey("Position")]` |
| `AssignmentDate` | `DateTime` | `[Required]` |
| `ExtraBoardOnly` | `bool` | `[Required]` |

### Navigation Properties

| Property | Type |
|---|---|
| `Crew` | `Crew` |
| `DailyAssignment` | `DailyAssignment` |
| `DailyCrewPositionAnnulment` | `DailyCrewPositionAnnulment` (1:1, nullable) |
| `DailyCrewPositionDoNotFill` | `DailyCrewPositionDoNotFill` (1:1, nullable) |
| `DailyCrewPositionSkip` | `DailyCrewPositionSkip` (1:1, nullable) |
| `MovedDailyCrewPosition` | `MovedDailyCrewPosition` (1:1, nullable) |
| `Position` | `Position` |
| `RailroadPosition` | `RailroadPosition` |
| `DailyCrewPositionVacancies` | `ICollection<DailyCrewPositionVacancy>` |
| `DailyCrewPositionOnDutyRecords` | `ICollection<DailyCrewPositionOnDutyRecord>` |
| `DailyCrewPositionElectronicCallRecords` | `ICollection<DailyCrewPositionElectronicCallRecord>` |

### Computed Properties � Payroll Codes (Pool-Specific)

**`JobCode`** � identifies the assignment for payroll:

| Pool | Format |
|---|---|
| 30 (Clerical), 60 (Patrolmen) | `"{PositionCode}{AssignmentNumber}"` |
| 50 (MoW) | `AssignmentName` |
| Default (10, 20, 40) | `"{AssignmentNumber}{PositionCode}"` |

**`PayrollCode`** � payroll earning code string:

| Pool | Format |
|---|---|
| 30, 50, 60 | `"{PositionCode}{PayrollCode}"` |
| Default | `"{PayrollCode}{PositionCode}"` |

**`DepartmentNumber`**: `Position.RailroadPayrollDepartment.DepartmentNumber.Substring(1)` (strips first char)

**`ICC_DepartmentNumber`**: `"{ICCNumber}{DepartmentNumber}"`

**`GeneralLedgerNumber`**: `Position.RailroadPayrollDepartment.GeneralLedgerNumber`

### Computed Properties � DateTime Calculations

| Property | Logic |
|---|---|
| `AssignmentOnDutyDateTime` | `AssignmentDate + DailyAssignment.AssignmentOnDutyTime` |
| `AssignmentOffDutyDateTime` | `AssignmentOnDutyDateTime + StraightTimeHours + UnpaidMealPeriodMinutes` |
| `AssignmentMaxOffDutyDateTime` | `AssignmentOnDutyDateTime + FRARequirements.MaxHours + UnpaidMealPeriodMinutes` |
| `StartCallTime` | Shift's calling time for this on-duty time; fallback: `DailyAssignmentShift.FirstCallingStartTime` |
| `EndCallTime` | `DailyAssignmentShift.LastCallingEndTime` |

### Computed Properties � Status Booleans

| Property | Logic |
|---|---|
| `HoursOfService` | `Position.Roster.Craft.HoursofService` |
| `IsAssigned` | Last on-duty record has `AssignedEmployee == true` |
| `IsForeman` | `Position.PositionName == "Foreman"` |
| `IsHelper` | `Position.PositionName == "Helper"` |
| `IsTrainee` | `Position.Roster.Training` |
| `IsAnnuled` | `DailyCrewPositionAnnulment != null` |
| `DoNotFill` | `DailyCrewPositionDoNotFill != null` |
| `Skipped` | `DailyCrewPositionSkip != null` |
| `IsFilled` | Any on-duty record with no mark-off and no unavailable |
| `IsOpen` | Any on-duty record where `IsOpen` |
| `IsOnDuty` | Any on-duty record where `IsOnDuty` |
| `IsTiedUp` | All on-duty records are `IsTiedUp` |
| `IsUnavailable` | Any on-duty record where `IsUnavailable` |
| `IsMoved` | `MovedDailyCrewPosition != null` |
| `VacationRelief` | Last on-duty record's mark-off has `CreatedFromTIES` |
| `MarkOffCode` | Last on-duty record's mark-off code string |

### DefaultJobPaid � Hard-Coded Pay Codes

| CraftName | Condition | Code |
|---|---|---|
| `"Engineer"` | Training | `"30H1"` |
| `"Engineer"` | Normal | `"10H1"` |
| `"Yardman"` | Foreman | `"101F"` |
| `"Yardman"` | Default (Helper) | `"101H"` |
| All others | � | `Craft.CraftPayCodes.PaidDayPaidCode` |

## Methods

### `CreateDailyCrewPositionOnDutyRecord(db, rpemployee, user, updatemarkoff, latecall=false)`

The core method for putting an employee on duty. Full workflow:

1. **Get last record**: `rpemployee.LastOnDutyRecord`
2. **Create on-duty record**: `DailyCrewPositionOnDutyRecord.CreateInstance(positionCtrlNbr, employeeCtrlNbr)`
3. **Resolve assigned position**:
   - If employee has no assigned position ? check `RailroadEmployee.AssignedPosition`
   - Trainees always treated as assigned to the current daily position
4. **Handle late call**: If `latecall == true`, add 90 minutes to current time for on-duty time
5. **Calculate previous rest**: `ondutyrec.CalculatePreviousRest(lastrecord)` ? stores hours/minutes
6. **Set assigned flag**: `AssignedEmployee = (this.RailroadPositionControlNumber == assignedPositionControlNumber)`
7. **Set job code**: From `this.JobCode`
8. **Determine payroll earning code**: Calls `GetPayrollEarningCode(rpemployee)`
9. **Calculate consecutive days**:
   - If rest >= `FRARequirements.ConsecutiveDayHours` (24 hours) OR no last record ? `1`
   - Else ? `lastrecord.ConsecutiveDays + 1`
10. **Calculate ST days worked / days worked**:
    - If no last record, OR at pay period boundary (day 1 or 16), OR not in current pay period ? reset to `1`
    - If last record was overtime ? `STDaysWorked` stays same, `DaysWorked` increments
    - If current is overtime ? `STDaysWorked` stays same
    - Else ? both increment
11. **Save** the on-duty record
12. **Create AFE billing record**: `ondutyrec.CreateDailyOnDutyAFEBillingRecord(db, user, now)`
13. **Update previous rest info**: `rpemployee.UpdatePreviousRestInformation(db, ondutyrec, user, now)`
14. **If position is annulled**: auto-create off-duty record at `AssignmentOffDutyDateTime`
15. **FRA compliance checks** (if `HoursOfService`):
    - If last record is called or on-duty, check if they'll be rested before next shift
    - If not ? create `DailyOnDutyUnavailableRecord`
    - Call `FRARequirements.CheckRestForNextOnDuty(db, ondutyrec, user)`
    - Check for "NN" (Not Notified) mark-off records; if employee has open notifications and isn't rested by calling time, create NN mark-off + send Teams message
16. **Pool 40 (Mechanical) special**: If not overtime, delete any existing on-duty record for the next shift (unless next shift wraps to shift 1)
17. **Update mark-offs**: If `updatemarkoff`, call `ondutyrec.UpdateDailyOnDutyMarkOffRecords(db, user, now)`

### `GetPayrollEarningCode(rpemployee, sameshift=true)`

Complex pool-specific payroll code determination:

**Default**: `"01"` (straight time, not overtime)

**Holiday shift**: `"05"` (holiday, overtime)

**Off-day / Worked-a-double / Unassigned** � pool-specific:

| Pool | Condition | Code |
|---|---|---|
| 10 | Off day | `"22"` (overtime) |
| 10 | Worked double | `"19"` (overtime) |
| 10 | Yardman moved to position | `"02"` (overtime) |
| 10 | Engineer on different shift | `"02"` (overtime) |
| 20 | Off day | `"22"` (overtime) |
| 20 | Worked double | `"19"` (overtime) |
| 20 | Different craft (not on hold-down) | `"02"` (overtime) |
| 40 | Same-shift vacancy | `"01"` (keep straight time) |
| 40 | Different-shift vacancy | `"02"` (overtime) |
| 40 | Off day | `"22"` (overtime) |
| 40 | Worked double | `"19"` (overtime) |
| 30, 50, 60 | Off day | `"22"` (overtime) |
| 30, 50, 60 | Worked double | `"19"` (overtime) |
| Default | Unassigned, not extra board | `"02"` (overtime) |

### `AnnulPosition(db, user, annuldatetime)`

Creates `DailyCrewPositionAnnulment` record with timestamp. Saves immediately.

### `DoNotFillPosition(db, user, nofilldatetime)`

Creates `DailyCrewPositionDoNotFill` record with timestamp. Saves immediately.

### `DeletePosition(db)`

Full cascade delete:
1. For each on-duty record:
   - Restore extra board position's `BoardOrder` and `TieUpOrder` from assignment record
   - Remove all payroll records (both `DailyCrewPositionOnDutyPayrollRecord` and associated `PayrollRecord`)
2. Remove all electronic call records
3. Remove the `DailyCrewPosition` itself
4. Save

### `RemoveDailyCrewPositionOnDutyRecords(db, rpemployee)`

Removes all on-duty records for a specific employee on this position:
1. Remove payroll records for each on-duty record
2. Remove on-duty records
3. Save
4. Update vacancy assignments for the pool/roster

### `GetStartCallTime(db)` / `GetCutOffDateTime(date)`

Lazy-load navigation properties if needed. `GetCutOffTime` delegates to `Assignment.GetCutOffTime(day, craft)` for craft-specific cut-off overrides.
# Part 5: DailyCrewPositionOnDutyRecord

## DailyCrewPositionOnDutyRecord

**Inherits**: `ControlNumberBase`

Tracks an employee being placed on duty for a specific crew position on a specific day.

### Stored Properties

| Property | Type | Attributes |
|---|---|---|
| `DailyCrewPositionControlNumber` | `long` | `[Required, ForeignKey("DailyCrewPosition")]` |
| `RailroadPoolEmployeeControlNumber` | `long` | `[Required]` FK |
| `RailroadPositionControlNumber` | `long` | `[Required]` FK � the employee's assigned position |
| `AssignmentOnDutyDate` | `DateTime` | `[Required]` |
| `AssignmentOnDutyTime` | `TimeSpan` | `[Required]` |
| `PreviousRestHours` | `int` | `[Required]` |
| `PreviousRestMinutes` | `int` | `[Required]` |
| `ConsecutiveDays` | `int` | `[Required]` � default `1` |
| `STDaysWorked` | `int` | `[Required]` |
| `DaysWorked` | `int` | `[Required]` |
| `AssignedEmployee` | `bool` | `[Required]` � true if working their own position |
| `JobCode` | `string` | `[Required, StringLength(4)]` |
| `PayrollCodeControlNumber` | `long` | `[Required]` FK to PayrollCode |
| `EarningCode` | `string` | `[Required, StringLength(2)]` |
| `AtHocMsgSent` | `bool` | `[Required]` |

### Navigation Properties

| Property | Type |
|---|---|
| `DailyCrewPosition` | `DailyCrewPosition` |
| `DailyCrewPositionOnDutyRecordLateCall` | `DailyCrewPositionOnDutyRecordLateCall` (1:1, nullable) |
| `DailyOnDutyUnavailableRecord` | `DailyOnDutyUnavailableRecord` (1:1, nullable) |
| `DailyOnDutyDidNotWorkRecord` | `DailyOnDutyDidNotWorkRecord` (1:1, nullable) |
| `DailyOnDutyPayrollInformation` | `DailyOnDutyPayrollInformation` (1:1, nullable) |
| `DailyCrewPositionOffDutyRecord` | `DailyCrewPositionOffDutyRecord` (1:1, nullable) |
| `DailyCrewPositionOnDutyMarkOffRecord` | `DailyCrewPositionOnDutyMarkOffRecord` (1:1, nullable) |
| `PayrollEarningCode` | `PayrollCode` |
| `RailroadPoolEmployee` | `RailroadPoolEmployee` |
| `RailroadPosition` | `RailroadPosition` |
| `DailyShiftExtraBoardPositionAssignments` | `ICollection<DailyShiftExtraBoardPositionAssignment>` |
| `DailyCrewPositionOnDutyPayrollRecords` | `ICollection<DailyCrewPositionOnDutyPayrollRecord>` |
| `DailyCrewPositionOnDutyFRARecords` | `ICollection<DailyCrewPositionOnDutyFRARecord>` |
| `DailyOnDutyAFEBillingRecords` | `ICollection<DailyOnDutyAFEBillingRecord>` |
| `DailyOnDutyMiscellaneousBillingRecords` | `ICollection<DailyOnDutyMiscellaneousBillingRecord>` |
| `DailyOnDutyLocomotiveRecords` | `ICollection<DailyOnDutyLocomotiveRecord>` |
| `DailyOnDutyZoneBillingRecords` | `ICollection<DailyOnDutyZoneBillingRecord>` |
| `DailyOnDutyRailroadMaterialRecords` | `ICollection<DailyOnDutyRailroadMaterialRecord>` |

### Computed DateTime Properties

| Property | Logic |
|---|---|
| `AssignmentOnDutyDateTime` | `AssignmentOnDutyDate + AssignmentOnDutyTime` |
| `AssignmentScheduledOffDutyDateTime` | `AssignmentOnDutyDateTime + StraightTimeHours` |
| `AssignmentOffDutyDateTime` | If tied up: `OffDutyRecord.Date + OffDutyRecord.Time`; else: scheduled + meal period minutes |
| `RestedDateTime` | If tied up: `OffDutyRecord.RestedDateTime`; else: `FRARequirements.GetRestDateTime(onDuty, offDuty)` |
| `PreviousRest` | `"{Hours} hrs {Minutes} mins"` |
| `TimeOnDuty` | `"{HoursOnDuty.Hours} hrs {HoursOnDuty.Minutes} mins"` |

### HoursOnDuty � Pool-Specific Meal Deduction

| Pool | Condition | Calculation |
|---|---|---|
| 50 (MoW) | Overtime OR `FirstMealPeriod == false` | `OffDuty - OnDuty` (NO meal deduction) |
| All others | Always | `OffDuty - OnDuty - UnpaidMealPeriodMinutes` |

### Status Boolean Properties

| Property | Logic |
|---|---|
| `IsCalled` | Not tied up, not marked off, not unavailable, not on duty, not annulled, not DoNotFill |
| `IsOnDuty` | Same as IsCalled BUT `DateTime.Now > AssignmentOnDutyDateTime` |
| `IsTiedUp` | OffDutyRecord exists AND not annulled AND (not DoNotFill OR (worked AND marked off)) AND not unavailable |
| `IsOpen` | Not tied up AND not unavailable AND not annulled AND not DoNotFill AND not marked off |
| `IsClosed` | Complete AND `Today > AssignmentOnDutyDateTime + 4 days` |
| `IsMarkedOff` | `DailyCrewPositionOnDutyMarkOffRecord != null` |
| `IsUnavailable` | `DailyOnDutyUnavailableRecord != null` |
| `IsLateCall` | `DailyCrewPositionOnDutyRecordLateCall != null` |
| `IsOnDutyUpdated` | If late call: `LateCall.Confirmed`; else: `true` |
| `IsOvertime` | `PayrollEarningCode.Overtime`; fallback: any payroll earning record is overtime |
| `IsOnOvertime` | `IsOnDuty AND (Now > ScheduledOffDuty OR IsOvertime)` |
| `OnTime` | Within last 30 minutes before scheduled off-duty (+ 15 min if TurnoverPay) |

### FRA & Rest Properties

| Property | Logic |
|---|---|
| `IsRestricted` | If complete ? false. If on duty: `Now > OnDuty + MaxHours`. If tied up: `(OffDuty - OnDuty) >= MaxHours` |
| `IsFRAComplete` | `All DailyCrewPositionOnDutyFRARecords.Completed` |
| `Complete` | If no off-duty ? false. If HoursOfService and has FRA records: `All FRA records completed`. Else: `OffDutyRecord.Complete` |

### Training & Employee Properties

| Property | Logic |
|---|---|
| `IsTraining` | Roster is training OR (Pool 10: yardman with cut-back seniority on engineer roster) |
| `HasTrainees` | Not if self is training. Not for Pool 40 (Mechanical). Else: `DailyAssignment.HasTrainingPositions(craftControlNumber)` |
| `AssignedEmployee` | True if employee is working their own permanent position |
| `EmployeeWorked` | No DidNotWork record AND not annulled AND not unavailable AND (if marked off: markoff time > on-duty time) |
| `EmployeeCalledRelief` | Mark-off code == `"CR"` |

### CanMoveToForeman

Only for Helper positions. Checks if a foreman position on the same assignment is unassigned:
- Neither position can be on duty or tied up
- If this employee is assigned, always can move
- If neither is assigned, checks seniority via `HasSeniority()`

## Methods

### `CreateDailyCrewPositionOffDutyRecord(db, offduty, user, now, complete=false, reason="NE")`

Creates the tie-up (off-duty) record. **Craft-specific rest calculation**:

| CraftName | RequiredRestTime | RestedDateTime | AvailableDateTime |
|---|---|---|---|
| `"Clerical"` | `Craft.RequiredRestHours` | `OnDutyDateTime + RestHours` | Overtime XB: `OffDutyDateTime`; else: `OnDutyDateTime + 1 day` |
| `"Yardmaster"` | `Craft.RequiredRestHours` | `OnDutyDateTime + RestHours` | `OnDutyDateTime + 1 day` |
| `"Engineer"` / `"Yardman"` | `FRARequirements.GetRestTime()` | `FRARequirements.GetRestDateTime()` | Same as RestedDateTime |
| Default | `Craft.RequiredRestHours` | `OnDutyDateTime + RestHours` | Same as RestedDateTime |
| Craft is null | 8 hours | `OffDutyDateTime + 8 hours` | � |

**Complete flag**: `true` if `!ProcessPayroll` on either employee or craft, or if explicitly passed.

**ConsecutiveDayRestedDateTime**: If employee worked ? `FRARequirements.GetConsecutiveDayRestDateTime(offduty)`; else ? `RestedDateTime`.

**Post-save actions** (only if employee worked):
1. `UpdatePreviousRestInformation()` on the employee
2. If `HoursofService` ? `FRARequirements.CheckFRARestCompliance(db, this, user)`
3. If off-duty time exceeds scheduled ? `UpdateHangoutNotificationDateTime()`
4. If on FIFO extra board ? `SetBoardOrder()` to reposition on board
5. If payroll tier < 100% ? `UpdatePayrollTierRate()`
6. For Engineer/Yardman: sends Teams tie-up message with on-duty, off-duty, rested times
7. Sends AtHoc off-duty message: `SendEmployeeOnDutyMessage(false, "", "")`

### `CreateManualTieUpNotification(db, user, now)`

If employee worked and no existing unconfirmed notification:
- Creates `RailroadPositionChange` notification
- Text: `"{employee} has an outstanding on duty record, from assignment {name} on duty at {datetime}, that requires completion."`
- `NotificationRequired = true`, `EmployeeOnly = true`

### `UpdateDailyCrewPositionOnDutyRecord(db, cposition, onduty, pcode, assigned, user, now)`

Creates a new on-duty record on a different `DailyCrewPosition` (moving employee between positions):
1. If current position has DoNotFill ? remove it
2. Create new on-duty record on `cposition`
3. Resolve `RailroadPositionControlNumber` from current employee position
4. Determine if same shift ? affects payroll code (overtime determination)
5. Recalculate earning code via `GetPayrollEarningCode()`
6. Copy `PreviousRestHours/Minutes`, `ConsecutiveDays`, `STDaysWorked`, `DaysWorked` from current record
7. Save new record, update mark-off records

### `CalculatePreviousRest(lastrecord)`

Returns `TimeSpan` between this on-duty datetime and the last record's off-duty datetime. If last record has no off-duty record ? returns `TimeSpan(0)`.

### `CreateDailyOnDutyUnavailableRecord(db, lastrecord, user, now)`

Creates `DailyOnDutyUnavailableRecord` linking this record to the previous record that caused the FRA unavailability.

### `CreateDailyOnDutyAFEBillingRecord(db, user, now)`

If the assignment has AFE records, creates `DailyOnDutyAFEBillingRecord` entries for each.

### `UpdateDailyOnDutyMarkOffRecords(db, user, now)`

Updates mark-off linkage when an employee is placed on duty while having an active mark-off.

## DailyCrewPositionOffDutyRecord (1:1 with OnDutyRecord)

**Does NOT inherit ControlNumberBase** � PK = FK to `DailyCrewPositionOnDutyRecord`.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `DailyCrewPositionOnDutyRecordControlNumber` | `long` | PK/FK |
| `AssignmentOffDutyDate` | `DateTime` | Date of tie-up |
| `AssignmentOffDutyTime` | `TimeSpan` | Time of tie-up |
| `RequiredRestTime` | `TimeSpan` | Calculated rest needed |
| `RestedDateTime` | `DateTime` | When employee is fully rested |
| `AvailableDateTime` | `DateTime` | When employee is available (may differ from rested) |
| `ConsecutiveDayRestedDateTime` | `DateTime` | 24hr consecutive-day rest boundary |
| `Complete` | `bool` | Whether payroll processing is done |
| `ReleaseReason` | `string` | Default `"NE"` |
| `CreatedBy` | `string` | |
| `CreatedDate` | `DateTime` | |
# Part 6: Vacancy Assignment Service

## Overview

The vacancy assignment system automatically determines which extra board employees should fill open crew positions. Located in `ApplicationUtilities.UpdateDailyCrewPositionVacancies()`.

## Entry Points

### `UpdateDailyCrewPositionVacancies(db, pool)` � Pool-Level

1. Checks `PoolInProgress` dictionary to prevent concurrent execution
2. Gets all rosters (including training) via `CollectionLists.GetRailroadPoolRostersWithTraining(pool)`
3. Calls the roster-level method for each roster

### `UpdateDailyCrewPositionVacancies(db, pool, roster, shift=0)` � Roster-Level

Main method. Only runs if `RailroadPool.AutoVacancyAssignments == true`.

## Vacancy Query

`GetDailyCrewPositionVacancies(db, roster, shift)` returns open positions:

```
WHERE Position.RosterControlNumber == roster
  AND DailyAssignmentShift.Completion == null        (shift not completed)
  AND DailyCrewPositionAnnulment == null              (not annulled)
  AND DailyCrewPositionDoNotFill == null              (not marked DoNotFill)
  AND (no on-duty records OR all on-duty records are marked-off or unavailable)
ORDER BY DailyCrewPositionSkip != null ASC,           (skipped positions last)
         AssignmentDate ASC,
         DailyAssignment.BoardOrder ASC,
         Position.PositionNumber ASC
```

**Pool 50 (MoW) exception**: Vacancies are queried at pool level, not roster level. Vacancy records are also cleared at pool level.

## Extra Board Query

```
WHERE DailyShiftExtraBoard.RosterBoard.RosterControlNumber == roster
  AND DailyShiftExtraBoard.Completed == false
ORDER BY TieUpOrder ASC, BoardOrder ASC
```

## Algorithm � Phase 1: No-Bid Bulletin Handling

Before processing general vacancies, checks for "no bid" bulletins:

1. Find vacancies where the railroad position has an active no-bid bulletin with `AssignDateTime.Date == AssignmentDate`
2. For each no-bid vacancy:
   a. Get force-assignment seniority list: `CollectionLists.GetForceAssignmentSeniorityList(db, positionName, rosterControlNumber)` � ordered by most junior first
   b. For each junior employee:
      - If on extra board:
        - Find their position in the extra board list
        - If the no-bid is on a different shift, check if extra board count can cover that shift's vacancies
        - Compare extra board position index vs vacancy position index to determine priority
        - If vacancy on-duty time >= no-bid time, remove XB employee (they'll fill the no-bid)
      - If employee is not called, not on duty, not marked off, and rested before end of calling time ? create vacancy assignment
      - If employee is already on duty on same shift at same/later time ? add their position back to vacancies and reassign

## Algorithm � Phase 2: Extra Board Assignment

Processes remaining vacancies after no-bid handling:

```
WHILE vacancies remain:
  FOR each vacancy (starting from first):
    Get calling time from assignment or cached vactimes dictionary
    
    Re-add any temporarily removed XB employees, re-sort by TieUpOrder/BoardOrder
    
    IF extra board is empty:
      Create unfilled vacancy record ? remove from list ? break
    
    Get end calling time for this vacancy
    
    FOR each extra board employee:
      SKIP rules (remove from board temporarily or permanently):
        - If DaysWorked >= 12 ? find next with < 12 days
        - If already on duty ? remove permanently
        - If already assigned to another vacancy ? remove permanently  
        - If not available by end of calling time ? move to temp list
        - If marked off with markup after end of calling time ? move to temp list
      
      QUALIFICATION CHECK:
        employee.IsQualified(positionControlNumber, assignmentDate)
      
      IF qualified:
        Create DailyCrewPositionVacancy + DailyCrewPositionVacancyEmployee
        Remove vacancy and XB employee from lists ? break
      
      IF vacancy is Foreman AND not qualified:
        ? Enter Helper Search Logic (see below)
      
      ELSE:
        Move XB employee to temp list (may be used for later vacancies)
```

## Helper Search Logic

When a Foreman vacancy can't be filled by the next XB employee:

1. **Swap check**: If next vacancy is a Helper on the same assignment:
   - Check if the *next* XB employee is qualified for Foreman
   - If yes: assign current XB to Helper, next XB to Foreman
   - Both get removed from their respective lists

2. **Eligible helper search**: `CheckForEligibleHelpers(db, vacancy)`:
   - Searches other assignments' Helper positions for an employee who can be promoted to Foreman
   - Search order by location numbers: 11, 13, 14 (hard-coded)
   - If found: assign the helper's employee to the Foreman vacancy, and the helper's position becomes a new vacancy
   - The new helper vacancy gets the original calling time
   - If the helper was on the extra board and same on-duty time ? remove their original vacancy record

## Entities Created

### DailyCrewPositionVacancy

Composite key: `DailyCrewPositionControlNumber` + `VacancyNumber`

| Property | Type | Description |
|---|---|---|
| `DailyCrewPositionControlNumber` | `long` | FK to DailyCrewPosition |
| `VacancyNumber` | `int` | Sequential number within the position |
| `CallingTime` | `TimeSpan` | When to call the assigned employee |
| `ExtraBoardEmployee` | `bool` | Whether the assigned employee is from XB |

### DailyCrewPositionVacancyEmployee

Composite key: `DailyCrewPositionControlNumber` + `VacancyNumber`

| Property | Type | Description |
|---|---|---|
| `DailyCrewPositionControlNumber` | `long` | FK |
| `VacancyNumber` | `int` | Matches vacancy |
| `RailroadPoolEmployeeControlNumber` | `long` | The employee assigned to fill |

## Key Business Rules

1. **12-day cap**: Employees with >= 12 `DaysWorked` are skipped if anyone with fewer exists
2. **Availability check**: `endCallTime > employee.AvailableDateTime` � employee must be available before calling window closes
3. **Rest check**: `endCallTime > employee.RestedDateTime` � employee must be rested
4. **Mark-off check**: If XB employee has a mark-off, their mark-up time must be before end of calling
5. **Qualification check**: `IsQualified()` checks position requirements against employee qualifications with effective date
6. **Same-shift priority**: No-bid employees on different shifts only get pulled if their shift has enough coverage
7. **Pool-in-progress guard**: `PoolInProgress` dictionary prevents concurrent vacancy updates for the same pool
8. **Error recovery**: On failure, calls `CreateUpdateVacancyRequest()` to queue a retry
# Part 7: FRA Compliance

## Overview

The Federal Railroad Administration (FRA) Hours of Service compliance system is implemented in the static class `FRARequirements` and the entity `DailyCrewPositionOnDutyFRARecord`. It applies only to crafts where `Craft.HoursofService == true` (typically Engineer and Yardman).

## FRA Constants

| Constant | Value | Description |
|---|---|---|
| `MaxHours` | `12` | Maximum hours on duty before restriction |
| `RestHours` | `10` | Minimum required rest hours after tie-up |
| `ConsecutiveDays` | `6` | Maximum consecutive work days before mandatory rest |
| `ConsecutiveDayHours` | `24` | Hours of rest needed to break consecutive day count |

## Rest Time Calculations

### `GetRestTime(onduty, offduty)` ? TimeSpan

Base rest = 10 hours. If time on duty exceeds 12 hours, penalty rest is added:

```
timeonduty = offduty - onduty  (UTC)
IF timeonduty > 12 hours:
  resttime = 10 hours + (timeonduty.Hours - 12 hours) + timeonduty.Minutes
ELSE:
  resttime = 10 hours
```

### `GetRestDateTime(onduty, offduty)` ? DateTime

```
return offduty + GetRestTime(onduty, offduty)
```

### `GetConsecutiveDayRestDateTime(offduty)` ? DateTime

```
return offduty + 24 hours
```

## Compliance Checks

### `CheckFRARestCompliance(db, record, user)`

Called after every tie-up (off-duty record creation) for HoursOfService crafts.

**If consecutive days < 6**:
1. Find the employee's next on-duty record via `CollectionLists.GetRailroadEmployeeNextOnDutyRecord()`
2. If next record exists and is HoursOfService ? call `CheckRestForNextOnDuty()`

**If consecutive days >= 6** (mandatory safety rest):
1. Find mark-off code `"SR"` (Safety Rest)
2. Create mark-off record with text: `"FRA required {hours} hour safety rest"`
3. Mark-off starts at `AssignmentOffDutyDateTime`
4. Send Teams message: `"{employee} has worked {N} consecutive days and has been marked off for {text}"`
5. Log error event

### `CheckRestForNextOnDuty(db, ondutyrec, user)`

Determines whether the employee will be rested in time for their next assignment.

**Two modes based on current record state**:

1. **If tied up** (just completed):
   - `lastrecord` = current record
   - `nextrecord` = first future on-duty record after off-duty time that isn't currently on-duty or tied-up

2. **If not tied up** (just placed on duty):
   - `nextrecord` = current record
   - `lastrecord` = `RailroadEmployee.LastCompletedOnDutyRecord`

### `CheckRestForNextOnDuty(db, rpemployee, lastrecord, nextrecord, user)`

Core rest-for-next logic:

1. If both records exist AND last record's employee worked:
2. Determine `nextdatetime`:
   - Default: `nextrecord.AssignmentOnDutyDateTime`
   - If employee has an unconfirmed notification for the next position (within 2 days): use `nextrecord.DailyCrewPosition.EndCallTime` instead (they haven't acknowledged yet)
3. **If `lastrecord.RestedDateTime > nextdatetime`** ? employee won't be rested in time:
   - Find mark-off code `"NR"` (Not Rested)
   - Create mark-off record: `"{employee} is not rested until {datetime}"`
   - Mark-off starts at `lastrecord.AssignmentOffDutyDateTime`
   - Send Teams message with same text
   - Log error event
   - All wrapped in `TransactionScope(ReadCommitted)`

## Integration Points

### On-Duty Record Creation (`CreateDailyCrewPositionOnDutyRecord`)

1. **Consecutive days calculation**: If rest >= 24 hours since last record ? reset to 1; else increment
2. **Unavailable check**: If last record is called/on-duty and `lastOnDuty + MaxHours + RestHours > thisOnDutyTime` ? create `DailyOnDutyUnavailableRecord`
3. **Not Notified check**: If employee has open notifications and isn't rested by last calling end time but IS rested before assignment time ? create `"NN"` mark-off + Teams message

### Off-Duty Record Creation (`CreateDailyCrewPositionOffDutyRecord`)

1. **Craft-specific rest**: Only Engineer/Yardman use FRA rest calculations; others use fixed hours
2. **Engineer/Yardman tie-up**: Sends Teams message with on-duty, off-duty, and rested times
3. **Post-save**: Calls `CheckFRARestCompliance()` which may auto-mark-off the employee

### On-Duty Record `IsRestricted` Property

```
IF complete ? false
IF on duty:  Now > OnDutyDateTime + 12 hours ? true
IF tied up:  (OffDuty - OnDuty) >= 12 hours  ? true
```

## DailyCrewPositionOnDutyFRARecord Entity

**Inherits**: `ControlNumberBase`

Per-on-duty-record FRA compliance form data.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `DailyCrewPositionOnDutyRecordControlNumber` | `long` | `[Required, ForeignKey]` |
| `EmployeeNumber` | `string` | Denormalized for FRA reporting |
| `EmployeeName` | `string` | Denormalized for FRA reporting |
| `PreviousRest` | `string` | Text representation of prior rest |
| `AssignmentName` | `string` | Assignment identifier |
| `OnDutyLocationControlNumber` | `long` | FK to Location |
| `OnDutyLocation` | `string` | Denormalized location name |
| `OnDutyDateTime` | `DateTime` | |
| `OffDutyLocationControlNumber` | `long` | FK to Location |
| `OffDutyLocation` | `string` | Denormalized location name |
| `OffDutyDateTime` | `DateTime` | |
| `CoveredServiceTime` | `TimeSpan` | Time under FRA coverage |
| `Completed` | `bool` | Whether FRA form is complete |
| `CertifiedBy` | `string` | Who certified the record |
| `CertifiedDateTime` | `DateTime` | When certified |
| `MonthlyCoveredServiceTime` | `long` | Running monthly total |
| `MonthlyCommingledTime` | `long` | Running monthly commingled total |
| `MonthlyDeadheadTime` | `long` | Running monthly deadhead total |

### Computed Properties

| Property | Logic |
|---|---|
| `CommingledTime` | `DailyFRACommingleRecord.EndDateTime - StartDateTime` (UTC) or `TimeSpan(0)` |

### Navigation Properties

| Property | Type |
|---|---|
| `DailyCrewPositionOnDutyRecord` | `DailyCrewPositionOnDutyRecord` |
| `DailyFRACommingleRecord` | `DailyFRACommingleRecord` (1:1, nullable) |
| `DailyFRADeadheadRecord` | `DailyFRADeadheadRecord` (1:1, nullable) |

## Related Entities

### DailyFRACommingleRecord
Records time when an employee performs both covered and non-covered service in the same duty period.
- `StartDateTime`, `EndDateTime`, `CommingleLocation`

### DailyFRADeadheadRecord
Records deadhead (travel) time that counts toward FRA hours.
- `StartDateTime`, `EndDateTime`, `DeadheadLocation`

## Auto-Generated Mark-Off Codes

| Code | Meaning | Trigger |
|---|---|---|
| `"SR"` | Safety Rest | ConsecutiveDays >= 6 |
| `"NR"` | Not Rested | RestedDateTime > next on-duty time |
| `"NN"` | Not Notified | Employee has open notifications, not rested by calling time |
# Part 9: AtHoc Service & Electronic Crew Calling

## Overview

The AtHoc integration provides mass notification for crew calling, employee management, and on-duty status updates. It consists of:

1. **`AtHocService`** � static class in the web app (`StrategicApplications\Services\AtHocService.cs`) that wraps the AtHoc REST API
2. **`SAAtHocMessageService`** � Windows Service with two sub-services:
   - `SAAssignmentCallService` � sends electronic call messages at scheduled calling times
   - `SAAssignmentOnDutyService` � sends on-duty status updates

## AtHoc REST API Integration

### Authentication

`GetToken()` � OAuth2 token retrieval:
- POST to `{AtHocURL}{GetTokenURL}` with `application/x-www-form-urlencoded`
- Parameters from `AppSettings`: `ClientID`, `ClientSecret`, `GrantType`, `UserName`, `Password`, `AcrValues`, `Scope`
- Parses token from JSON response by string splitting (not JSON deserialization)

### Configuration Keys (all from `AppSettings`)

| Key | Purpose |
|---|---|
| `AtHocURL` | Base URL for AtHoc API |
| `GetTokenURL` | Token endpoint path |
| `SyncUserURL` | User sync endpoint path |
| `PublishAlertURL` | Alert publishing endpoint path |
| `GetAlertResponseURL` | Alert response query path |
| `DetailsByUsersReportURL` | Response details suffix |
| `AssignmentCallTemplate` | Template name for call alerts |
| `AssignmentConfirmTemplate` | Template name for confirm alerts |
| `AssignmentMoveTemplate` | Template name for move alerts |

## AtHocService Static Methods

### User Sync Methods

**`ProcessPhoneNumberMessage(message)`** � syncs phone to AtHoc:
- Message format: `"{loginid},{description},{callingOrder},{phoneNumber}"`
- Device mapping:

| SA Description | AtHoc DeviceID | EventID |
|---|---|---|
| `"Alert Phone (text message)"` | `Device:sms` | 1 |
| `"Mobile Phone"` | `Device:207ac0c6-0732-476f-9a4f-4204dae80dae` | 2 |
| `"Emergency Phone"` | `Device:emergencyPhone` | 3 |
| `"Work Phone"` | `Device:workPhone` | 4 |
| `"Home Phone"` | `Device:homePhone` | 5 |

**`ProcessEmailAddressMessage(message)`** � syncs email to AtHoc:
- Message format: `"{loginid},{description},{emailaddress}"`
- Device mapping:

| SA Description | AtHoc DeviceID | EventID |
|---|---|---|
| `"Work Email Address"` | `Device:Email-Work` | 6 |
| `"Alert Email Address"` | `Device:Email-Personal` | 7 |

**`ProcessEmployeeMessage(message)`** � dispatches by action:

| Action | JSON Payload | EventID |
|---|---|---|
| `"OnDuty"` (true) | `LOGIN_ID, On-Duty:Yes, On-Duty-Location, Shift` | 8 |
| `"OnDuty"` (false) | `LOGIN_ID, On-Duty:No, empty location/shift` | 9 |
| `"Craft"` | `LOGIN_ID, Craft:{name}` | 10 |
| `"Create"` | `LOGIN_ID, FIRSTNAME, LASTNAME, DISPLAYNAME` | 11 |
| `"Delete"` | `LOGIN_ID, STATUS:DEL` | 12 |

**`ProcessEmployeeOnDutyMessages(records)`** � batch on-duty sync:
- Builds JSON array of all on-duty records with `LOGIN_ID`, `On-Duty:Yes`, `On-Duty-Location`, `Shift`
- Single POST to SyncUser URL, EventID 8

### Alert Publishing Methods

**`ProcessAssignmentCallMessage(employee, job, location, oddate, odtime, user)`**:
- Text: `"{employee} is called for assignment {job}, on duty at {location}, {date} at {time}."`
- Publishes via `AssignmentCallTemplate`, sends Teams "ECallMessage" on success

**`ProcessAssignmentConfirmMessage(employee, job, user)`**:
- Text: `"{employee} is confirmed for assignment {job}."`
- Publishes via `AssignmentConfirmTemplate`

**`ProcessAssignmentMovedMessage(employee, job, location, oddate, odtime, user)`**:
- Text: `"{employee} has been moved to assignment {job}, on duty at {location}, {date} at {time} to fill a foreman position."`
- Only sends if employee has Alert phone or Alert email
- If no alert devices: returns `"Auid:Message not sent"`, appends "(Employee has not been notified)" to Teams

**`PublishAlert(template, body, user)`** � core alert method:
- POST to `{AtHocURL}{PublishAlertURL}`
- JSON body with `TemplateCommonName`, `Content.Body`, `TargetUsers.TargetUserNames`, `Schedule` (5 minutes duration)
- Returns response containing `Auid` (Alert Unique Identifier) on success

**`ProcessAssignmentResponseMessage(alertid)`**:
- GET to `{AtHocURL}{GetAlertResponseURL}{alertid}{DetailsByUsersReportURL}`
- Returns response text (contains "Accept" or "Reject")

## SAAssignmentCallService � Windows Service

### Timer Setup

1. On start: 60-second delay timer
2. After delay: `Timer_Tick` fires:
   - Loads first `Railroad` from DB
   - Calls `SetAtHocMessageTimer(railroadControlNumber, DateTime.Now)` to find next calling time
   - Sends Teams "SystemSupport" message with next timer

### `PublishAssignmentCallMessages` � Main Processing Loop

Triggered at each calling time. Workflow:

#### Phase 1: Auto-DoNotFill (MustFill == 2 / Never Fill)

```
Find vacancies WHERE:
  - Pool has ElectronicCrewCalling enabled
  - Shift has calling time matching nextcalltime
  - AssignmentDate matches
  - CallStartTime <= nextcalltime
  - Not already DoNotFill
  - Position.MustFill == 2 (Never Fill)

For each: DoNotFillPosition ? CreateOffDutyRecords ? Remove vacancy
```

#### Phase 2: Electronic Call Messages

Query assigned vacancies:
```
WHERE ElectronicCrewCalling enabled
  AND no existing electronic call records
  AND MustFill != 2
  AND shift calling time matches
  AND AssignmentDate matches
  AND CallStartTime <= nextcalltime
ORDER BY ExtraBoard ASC, CrewName ASC, PositionCode ASC
```

#### Phase 2a: Pool-Specific Filtering

**Pool 30 (Clerical)**: If any Optional (MustFill=1) vacancies exist:
- If more than 1 MustFill vacancy ? remove ALL clerical vacancies
- Else ? remove clerical vacancies after the first Optional one

**Pool 20 (Yardmaster)**: If more than 1 Yardmaster vacancy ? remove all

#### Phase 2b: Alert Device Filter

Only include employees who have "Alert" phone numbers or "Alert" email addresses, OR are non-extra-board (assigned employees always get called).

#### Phase 2c: Availability Checks (Extra Board Only)

| Pool | Available If |
|---|---|
| 20 (Yardmaster) | ST hours this week < 40 AND assignment date is not a holiday |
| 30 (Clerical) | ST hours this week < 40 |
| Default | Always available (only rest check applies) |

Additional: `IsRested` must be true for extra board employees.

#### Phase 2d: Send Calls (batches of 15)

For each vacancy:
- **Extra board**: `ProcessAssignmentCallMessage()` � requires accept/reject
- **Assigned (moved)**: `ProcessAssignmentMovedMessage()` � notification only

On success (response contains "Auid"):
- Parse `AlertUniqueIdentifier` from response
- Create `DailyCrewPositionElectronicCallRecord` with `AlertUniqueIdentifier`, `EmployeeNumber`, `JobCode`, `SendRequest` flag
- Between batches of 15: `Thread.Sleep(60000)` (1 minute)

#### Phase 3: Retrieve Responses

`RetrieveAssignmentCallResponses(db, ecalllist)`:

- Polls every 5 seconds for up to 6 minutes per call record
- For each call where `SendRequest == true`:
  - `ProcessAssignmentResponseMessage(alertid)` ? check for "Accept"
  - **Accepted**: `FillVacancy(db, vacancy)` ? places employee on duty; sends Teams "ECallMessage"; creates `DailyCrewPositionElectronicResponseRecord` with `ResponseID:"1"`, `ResponseText:"Accepted"`
  - **Rejected/No response**: Creates response record with `ResponseID:"2"`, `ResponseText:"Rejected"` or timed out

## Entities

### DailyCrewPositionElectronicCallRecord
**Inherits**: `ControlNumberBase`

| Property | Type | Description |
|---|---|---|
| `DailyCrewPositionControlNumber` | `long` | FK |
| `RailroadPoolEmployeeControlNumber` | `long` | FK |
| `AlertUniqueIdentifier` | `string` | AtHoc alert GUID |
| `EmployeeNumber` | `string` | Denormalized |
| `JobCode` | `string` | Denormalized |
| `SendRequest` | `bool` | True=extra board (needs response); False=assigned (notification only) |

### DailyCrewPositionElectronicResponseRecord
**Inherits**: `ControlNumberBase`

| Property | Type | Description |
|---|---|---|
| `DailyCrewPositionElectronicCallRecordControlNumber` | `long` | FK |
| `ResponseID` | `string` | `"1"` = Accepted, `"2"` = Rejected |
| `ResponseText` | `string` | `"Accepted"` or `"Rejected"` |
# Part 12: Global.asax � Application Lifecycle & Timer Architecture

## Overview

`Global.asax.cs` (`MvcApplication : HttpApplication`) is the central orchestrator for the web application. It manages:
- Application startup/shutdown
- 17 categories of per-pool timers for automated processing
- 6 FileSystemWatchers for file-based message processing
- Active user session tracking

## Static Fields

### Configuration

| Field | Type | Value |
|---|---|---|
| `user` | `string` (const) | `"autoprocess"` |
| `inbound` | `string` | `@"\\sql-svr\SA\Message Queue\Inbound"` |
| `dev_inbound` | `string` | `@"\\sql-svr\SA\dev\Message Queue\Inbound"` |
| `database` | `string` | Set at startup from connection string |
| `databasename` | `string` | Display name for database |
| `delay` | `int` | `600` (seconds); `300` in DEBUG |
| `sa_auto` | `bool` | `true` if production database |
| `dev_auto` | `bool` | `true` if non-production |
| `settimers` | `bool` | `true`; `false` in DEBUG |

### Timer Dictionaries (all `Dictionary<long, Timer>`, keyed by pool ControlNumber)

| Dictionary | Purpose |
|---|---|
| `BulletinTimers` | Auto-assign position bulletins |
| `SeniorityMoveTimers` | Process seniority moves |
| `HangoutTimers` | Process hangout (auto-assign board) notifications |
| `DailyCallSheetTimers` | Create daily call sheets |
| `DailyExtraBoardTimers` | Create daily extra boards |
| `DailyReportTimers` | Generate daily reports |
| `DailyVacationWeekTimers` | Process vacation week assignments |
| `DailyOffDayTimers` | Create off-day records |
| `DailyRailroadEmployeeStatusTimers` | Create daily employee status records |
| `HolidayTimers` | Process holiday records |
| `MarkOffRequestTimers` | Process mark-off requests |
| `RosterBoardMarkOffTimers` | Auto mark-off roster board employees |
| `RosterBoardHangoutTimers` | Auto hangout roster board employees |
| `PublishRailroadInformationTimers` | Publish railroad information records |
| `CreateHolidayTimers` | Create holiday payroll records |
| `AtHocMessageTimers` | AtHoc electronic calling timers |

Each timer dictionary has a corresponding `nextXxxUpdates` dictionary (`Dictionary<long, DateTime>`) tracking the next scheduled fire time.

### Processing Guards

| Field | Type | Purpose |
|---|---|---|
| `HolidayRecordsProcessing` | `bool` | Prevents concurrent holiday file processing |
| `VacancyRecordsProcessing` | `bool` | Prevents concurrent vacancy file processing |
| `StatusRecordsProcessing` | `bool` | Prevents concurrent status file processing |
| `BoardProcessing` | `bool` | Prevents concurrent board processing |
| `CallSheetInProgress` | `Dictionary<long, bool>` | Per-pool call sheet guard |
| `ExtraBoardInProgress` | `Dictionary<long, bool>` | Per-pool extra board guard |
| `HolidayPayrollInProgress` | `Dictionary<long, bool>` | Per-pool holiday payroll guard |

### Active Users

`ActiveUsers` � `Dictionary<string, ApplicationUser>` � in-memory session cache keyed by username.

## Application_Start()

1. **DEBUG mode**: Sets `delay = 300`, `settimers = false`
2. **Database detection**: Reads connection string ? sets `database` and `databasename`
   - `"DevelopmentDatabase"` ? `"Development Database"`
   - `"StrategicApplicationsDemo"` ? `"Demo Database"`
   - `"StrategicApplications"` ? production (enables `sa_auto`)
3. **Clear active users**
4. **Register MVC**: Areas, Filters, Routes, Bundles
5. **Set environment flags**: `sa_auto` / `dev_auto` based on database name
6. **Start delayed event timer**: 5-second timer to fire `FireDelayedEvent`
7. **Create timers**: `CreateTimers()` � initializes all 17 timer categories
8. **Create watchers**: `CreateWatchers()` � initializes 6 FileSystemWatchers
9. **Enable delayed event timer**
10. **Log start**: `"Train Crew Reporting Application Started"`

## Application_End()

Clears `ActiveUsers`, logs `"Train Crew Reporting Application Ended"`.

## User Registration

**`RegisterUser(ApplicationUser)`**: Adds/updates user in `ActiveUsers` dictionary.

**`RegisterUser(string username)`**: Loads user from DB, registers, returns user.

## FireDelayedEvent (5-second post-start)

Triggers all file watchers to process any files that existed before startup:
- Production: `TriggerHolidayRecordWatcherEvent`, `TriggerVacancyUpdateWatcherEvent`, `TriggerStatusUpdateWatcherEvent`
- Development: Dev equivalents of the same

## FileSystemWatchers

### Production Watchers

| Watcher | Pattern | Trigger Method | Processing Method |
|---|---|---|---|
| `HolidayRecordWatcher` | `*.hr` | `TriggerHolidayRecordWatcherEvent` | `ApplicationUtilities.CreateHolidayRecord(file)` |
| `VacancyUpdateWatcher` | `*.uv` | `TriggerVacancyUpdateWatcherEvent` | `ApplicationUtilities.UpdateCrewPositionVacancies(file)` |
| `StatusUpdateWatcher` | `*.esr` | `TriggerStatusUpdateWatcherEvent` | `ApplicationUtilities.CreateRailroadEmployeeStatusRecord(file)` |

### Development Watchers

Identical structure with `Dev` prefix, watching `dev_inbound` path. Enabled only when `dev_auto == true`.

### File Watcher Pattern (all follow same pattern)

```
IF NOT already processing:
  Set processing = true
  WHILE files exist in directory:
    Get first file
    TRY:
      Process file
    CATCH:
      Log error
      Move file to error directory
      Reset processing flag
  Reset processing flag
```

All watchers have both `Created` and `Deleted` event handlers. The `Deleted` handler updates the `XxxRecordsExist` flag.

## Timer Architecture

### CreateTimers()

Iterates: Clients ? Railroads ? RailroadPools

For each pool where the parent Client and Railroad both have `AutoAssignments == true`:

| Pool Flag | Timer Created | Handler |
|---|---|---|
| `AutoBulletins` | `BulletinTimers[pool]` | `ProcessBulletins` |
| `AutoMoves` | `SeniorityMoveTimers[pool]` | `ProcessSeniorityMoves` |
| `AutoHangouts` | `HangoutTimers[pool]` | `ProcessHangouts` |
| `AutoCallSheets` | `DailyCallSheetTimers[pool]` | `CreateDailyCallSheet` |
| Always | `DailyExtraBoardTimers[pool]` | `CreateDailyShiftExtraBoards` |
| Always | `DailyReportTimers[pool]` | `CreateDailyReport` |
| Always | `DailyVacationWeekTimers[pool]` | `ProcessDailyVacationWeek` |
| Always | `DailyOffDayTimers[pool]` | `CreateDailyOffDays` |
| Always | `DailyRailroadEmployeeStatusTimers[pool]` | `CreateDailyRailroadEmployeeStatusRecords` |
| Always | `HolidayTimers[pool]` | `ProcessHoliday` |
| Always | `MarkOffRequestTimers[pool]` | `ProcessMarkOffRequests` |
| Always | `RosterBoardMarkOffTimers[pool]` | `ProcessRosterBoardMarkOffs` |
| Always | `RosterBoardHangoutTimers[pool]` | `ProcessRosterBoardHangouts` |
| Always | `PublishRailroadInformationTimers[pool]` | `PublishRailroadInformation` |
| Always | `CreateHolidayTimers[pool]` | `CreateHolidayPayrollRecords` |
| `ElectronicCrewCalling` | `AtHocMessageTimers[pool]` | `CreateAtHocMessages` |

If `settimers == true`, each timer is initialized by calling its corresponding `SetXxxTimer(pool)` method.

### Timer Set Pattern

Each `SetXxxTimer(pool)` method:
1. Gets current time
2. Calls `GetNextXxx(pool)` to calculate next fire time
3. Stores in `nextXxxUpdates[pool]`
4. Calculates `interval = (nextTime - now).TotalMilliseconds`
5. If interval > 0: sets `timer.Interval = interval`, `timer.Enabled = true`

### Pool Number-Specific Timer Scheduling

Different pools have different scheduling rules. Key examples:

**Call Sheet Timing** (varies by pool):

| Pool | Schedule Logic |
|---|---|
| 10 (Yard & Enginemen) | Based on shift calling times |
| 20 (Yardmaster) | Specific calling windows |
| 30 (Clerical) | Different timing than yard |
| 40 (Mechanical) | Based on shift patterns |
| 50 (MoW) | Pool-level (not per-roster) |
| 60 (Patrolmen) | Similar to clerical |

**Extra Board Timing** (varies by pool):

| Pool | Schedule Logic |
|---|---|
| 20 (Yardmaster) | 40-hour week check |
| 30 (Clerical) | 40-hour week check |
| 40 (Mechanical) | Shift-based |
| Others | Standard shift-based |

## Key Global Methods

### `GetNextDailyAssignmentShift(lastShift, pool, lastDate)` ? DateTime

Circular shift advancement:
- Shift 1 ? 2 ? 3 ? 1
- When wrapping from 3 to 1: date increments by 1 day
- Shift ID encoded in the seconds component of the returned DateTime

### `CreateDailyCallSheet` � Timer Handler

1. Identifies pool from timer dictionary
2. Updates mark-off records for last shift (not Pool 50)
3. Calculates next shift via `GetNextDailyAssignmentShift`
4. Gets assignments for that shift/date
5. If no assignments on calculated shift: advances to next shift (with date wrap)
6. Checks for duplicate call sheet
7. Calls web app endpoint to create the call sheet
8. Resets timer for next call sheet

### `CreateDailyShiftExtraBoards` � Timer Handler

Creates extra board records for the next shift. Pool-specific logic for board ordering.

### `ProcessBulletins` � Timer Handler

Auto-assigns bulletined positions based on seniority and bulletin rules.

### `ProcessSeniorityMoves` � Timer Handler

Processes seniority move requests based on roster seniority move rules.

### `ProcessHangouts` � Timer Handler

Assigns employees to hangout (auto-assign board) positions.
# Part 14: Mark-Off System

## Overview

The mark-off system manages employee absences from their positions. A "mark-off" removes an employee from active duty; a "mark-up" returns them. The system integrates with vacancy assignment, payroll, FRA compliance, extra board management, and electronic calling.

## MarkOffCode

**Inherits**: `ControlNumberBase`

Defines the types of absences available in the system.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `ClientControlNumber` | `long` | FK to Client |
| `Code` | `string` | `[Required, StringLength(2)]` � unique code |
| `ReportCode` | `string` | `[Required, StringLength(1)]` � reporting abbreviation |
| `Description` | `string` | `[Required, StringLength(250)]` |
| `Excused` | `bool` | Whether absence is excused |
| `RecordHours` | `bool` | Whether to track hours off |
| `AllowRequest` | `bool` | Whether employees can request this mark-off |
| `SystemUseOnly` | `bool` | If true, only system can create (not users) |
| `ApprovalRequired` | `bool` | Whether mark-off needs approval |
| `ApprovedByAgreement` | `bool` | Whether pre-approved by labor agreement |
| `HolidayExempt` | `bool` | Whether exempt from holiday pay rules |
| `HolidayQualify` | `bool` | Whether this counts for holiday qualification |
| `ReportColor` | `string` | `[Required]` � UI display color |

### Computed Properties

| Property | Logic |
|---|---|
| `Code_Description` | `"{Code} - {Description}"` |
| `IsCompensated` | `MarkOffPayrollCode != null` � has an associated payroll code |
| `IsAutoMarkup` | `MarkOffMarkUpHours != null` � auto mark-up configured |
| `IsVacationWeek` | `Code.StartsWith("V") && Code != "VD"` |

### AutoMarkUpHours � Hard-Coded Vacation Durations

| Code | Hours | Description |
|---|---|---|
| `V1` | 168 | 1 week vacation (7 � 24) |
| `V2` | 336 | 2 weeks vacation (14 � 24) |
| `V3` | 504 | 3 weeks vacation (21 � 24) |
| `V4` | 672 | 4 weeks vacation (28 � 24) |
| `V5` | 840 | 5 weeks vacation (35 � 24) |
| `CD` | 24 | Compensated day |
| `PD` | 24 | Personal day |
| `SD` | 24 | Sick day |
| `VD` | 24 | Vacation day |
| Others | `MarkOffMarkUpHours.MarkUpHours` | Configurable via related entity |

### `AutomaticMarkUpHours(craft)` � Craft-Specific Override

1. If `Craft.AutoMarkUp == false` ? return 0 (no auto mark-up for this craft)
2. Check `CraftMarkOffCodes` for craft-specific override
3. If found ? return `craftcode.AutomaticMarkUpHours`
4. If not found ? fall back to `MarkOffMarkUpHours.MarkUpHours` or 0

### Known Mark-Off Codes

| Code | Description | System | Auto Mark-Up |
|---|---|---|---|
| `SR` | Safety Rest (FRA) | Yes | Yes |
| `NR` | Not Rested | Yes | Yes |
| `NN` | Not Notified | Yes | Yes |
| `CR` | Called Relief | No | No |
| `LD` | Light Duty | No | No |
| `OS` | Out of Service | No | No |
| `FL` | FMLA | No | No |
| `PB` | Personal Business | No | No |
| `V1`-`V5` | Vacation weeks | No | Yes (168-840 hrs) |
| `VD` | Vacation day | No | Yes (24 hrs) |
| `CD` | Compensated day | No | Yes (24 hrs) |
| `PD` | Personal day | No | Yes (24 hrs) |
| `SD` | Sick day | No | Yes (24 hrs) |
| `S` | Suspension | No | No |

### `GetMarkOffCode(code, days)` � External Code Translation

Translates external system codes to internal codes:

| External Code | Internal Code |
|---|---|
| `DS`, `FU`, `Q`, `RS` | `OS` (Out of Service) |
| `F1`-`F5` | `FL` (FMLA) |
| `NB` | `PB` (Personal Business) |
| `UN`, `WN` | `NN` (Not Notified) |
| `SP` | `S` (Suspension) |
| `V` (10/14 days) | `V2` (2-week vacation) |
| `V` (7 days) | `V1` (1-week vacation) |

### Navigation Properties

| Property | Type |
|---|---|
| `Client` | `Client` |
| `MarkOffPayrollCode` | `MarkOffPayrollCode` (1:1, nullable) |
| `MarkOffMarkUpHours` | `MarkOffMarkUpHours` (1:1, nullable) |
| `CraftMarkOffCodes` | `ICollection<CraftMarkOffCode>` |
| `MarkOffCodeApprovalOfficers` | `ICollection<MarkOffCodeApprovalOfficer>` |
| `MarkOffRecords` | `ICollection<MarkOffRecord>` |
| `MarkOffRequestRecords` | `ICollection<MarkOffRequestRecord>` |
| `DailyRosterBoardPositionHangoutRecords` | `ICollection<DailyRosterBoardPositionHangoutRecord>` |
| `DailyRosterBoardPositionMarkOffRecords` | `ICollection<DailyRailroadEmployeePositionMarkOffRecord>` |
| `MarkOffRequestWaitListRecords` | `ICollection<MarkOffRequestWaitListRecord>` |

---

## MarkOffRecord

**Inherits**: `ControlNumberBase`

The core mark-off entity. One record per absence period per employee.

### Stored Properties

| Property | Type | Attributes | Default |
|---|---|---|---|
| `EmployeeControlNumber` | `long` | `[Required]` FK | � |
| `EmployeeNumber` | `string` | `[Required, StringLength(4)]` | � |
| `RailroadPoolEmployeeControlNumber` | `long` | `[Required]` FK | � |
| `RailroadPositionControlNumber` | `long` | `[Required]` FK | � |
| `MarkOffCodeControlNumber` | `long` | `[Required]` FK | � |
| `MOCode` | `string` | `[Required, StringLength(2)]` | � |
| `MarkOffDateTime` | `DateTime` | `[Required]` | `DateTime.Now` |
| `RestrictMarkUp` | `bool` | `[Required]` | `false` |
| `RequirePaperwork` | `bool` | `[Required]` | `false` |
| `ApprovalRequired` | `bool` | `[Required]` | � |
| `CreatedFromTIES` | `bool` | `[Required]` | � |
| `Notes` | `string` | nullable | `string.Empty` |
| `LaidOffOnCall` | `bool` | `[Required]` | � |
| `CompHours` | `double` | `[Required]` | � |

### Computed Properties

| Property | Logic |
|---|---|
| `HasMarkUpRecord` | `MarkUpRecord != null` |
| `IsDeleted` | `MarkOffRecordDelete != null` |
| `IsClosed` | Deleted OR (has mark-up AND `Now > MarkUpDateTime`) |
| `IsOpen` | Not deleted AND `Now >= MarkOffDateTime` AND not closed |
| `IsCompensated` | `MarkOffCode.IsCompensated` |
| `IsLightDuty` | `MarkOffCode.Code == "LD"` |
| `IsAutoMarkup` | `MarkOffCode.AutomaticMarkUpHours(Craft) != 0` |
| `IsVacationWeek` | Code is V1-V5 |
| `TimeOff` | If no mark-up: `Now - MarkOffDateTime`; else: `MarkUpDateTime - MarkOffDateTime` (UTC) |
| `TimeOff_Days` | `TimeOff.Days` (floored to 0) |
| `TimeOff_Hours` | `TimeOff.Hours` (floored to 0) |
| `CreatedByName` | Opens new DbContext ? resolves username to `User.FullName`; `"autoprocess"` ? `"Automatic Process"` |
| `ModifiedByName` | Same pattern |

### Navigation Properties

| Property | Type |
|---|---|
| `Employee` | `Employee` |
| `RailroadPoolEmployee` | `RailroadPoolEmployee` |
| `RailroadPosition` | `RailroadPosition` |
| `MarkOffCode` | `MarkOffCode` |
| `MarkOffRecordDelete` | `MarkOffRecordDelete` (1:1, nullable � soft delete) |
| `MarkUpRecord` | `MarkUpRecord` (1:1, nullable) |
| `MarkOffRecordApproval` | `MarkOffRecordApproval` (1:1, nullable) |
| `DailyExtraBoardMarkOffRecords` | `ICollection<DailyExtraBoardMarkOffRecord>` |
| `DailyRailroadEmployeePositionMarkOffRecords` | `ICollection<DailyRailroadEmployeePositionMarkOffRecord>` |
| `MarkOffRequestMarkOffRecords` | `ICollection<MarkOffRequestMarkOffRecord>` |

### `CreateMarkOffRecord(db, rpectrlnbr, code, notes, user, modate, restrictmarkup, officer, ...)`

Full workflow for creating a mark-off:

**Step 1 � Initialize**:
- Load `MarkOffCode`, `RailroadPoolEmployee`, `CurrentPosition`, `Pool`, `Craft`
- Set all stored properties from parameters
- `CompHours` = `GetTotalMarkOffCompHours()` � accumulated compensation hours
- `ApprovalRequired` = `markoffcode.GetApprovalRequired(craftControlNumber)`
- If `requirepaperwork` ? force `RestrictMarkUp = true`

**Step 2 � Save record and update daily records**:
- If employee is on extra board / hangout ? `CreateDailyRailroadEmployeePositionMarkOffRecord()`
- Else (crew position) ? `UpdateDailyCrewPositionOnDutyMarkOffRecords()`

**Step 3 � Link to mark-off request** (if exists):
- If `reqctrlnbr` provided ? use that request
- Else for vacation codes (V* except VD):
  - Pool 10: calculate week start from Jan 1 day-of-week alignment
  - Find matching open request by date and code
  - If not found: try 7 days prior
- Else for other codes: find by exact date and code match
- If no request found AND code ends with "D" AND has payroll code ? auto-create request + mark-up record
  - Non-Clerical pools: auto mark-up at `MarkOffDateTime + 1 day` (XB) or `midnight + 1 min`

**Step 4 � Link request to mark-off**:
- Create `MarkOffRequestMarkOffRecord` linking request ? mark-off
- If request has mark-up record ? apply mark-up to this mark-off
- Complete any wait-list notification for this date

**Step 5 � Approval officer**:
- If `officer` provided ? create `MarkOffRecordApproval`

**Step 6 � Compensation balance check**:
- If code has `MarkOffPayrollCode` with `CompensationType`:
  - Calculate remaining balance: `currentBalance - CompHours`
  - If balance <= 0 ? `RemoveUnusedMarkOffRequestRecords()` and `RemoveUnusedMarkOffWaitListRecords()`

**Step 7 � Post-save actions** (not for vacation relief):
- Create interface file: `CreateInterfaceFile(db, "Add")`
- If auto-markup:
  - `CreateOriginalMarkOffRecord()` � snapshot for audit
  - `MarkUp()` with auto-calculated datetime
  - Create update interface file
- Else (not auto-markup):
  - Wait for `CallSheetInProgress` to clear (polling with `Thread.Sleep(1000)`)
  - Find current extra board position
  - `UpdateDailyExtraBoardMarkOffRecord()`
  - Set `TieUpOrder` to `modate + 10 years` (pushes to back of board)
  - `SetRosterBoardMarkOffTimer()`

**Step 8 � Off-duty record creation**:
- `CreateDailyCrewPositionOffDutyRecord()` � see below

### `CreateDailyCrewPositionOffDutyRecord(db, user, now)` � Private

Called after mark-off creation. If mark-off is within 2 hours of now AND no mark-up:

1. Find an on-duty record within 1 day of mark-off where employee is on duty (not tied up)
2. Skip if mark-off request records exist
3. If employee worked:
   - If code == `"CR"` (Called Relief): create off-duty with `"CR"` release reason
   - Else: create off-duty + payroll record, flag for payroll review with reason `"Marked off while working. Code {code} - {description}"`
   - If on-duty record not complete ? `CreateManualTieUpNotification()`
4. Trigger vacancy update for the pool/roster
5. Attempt to complete the daily assignment shift

### `DeleteMarkOffRecord(db, user)`

1. If employee is on extra board/hangout:
   - Find current XB position ? remove `DailyExtraBoardMarkOffRecord`
   - Reset `TieUpOrder` and `BoardOrder` to original values
2. Delete daily mark-off linkage records
3. Remove `MarkOffRequestMarkOffRecords` links
4. Create `MarkOffRecordDelete` soft-delete record
5. Reset roster board mark-off timer
6. Trigger vacancy update

### `ChangeMarkOffRecord(db, code, empl, mo_datetime, mu_datetime, notes, user, ...)`

Updates an existing mark-off. If code changes:
- If new code has no auto-markup hours AND old had mark-up ? delete mark-up, send Teams message
- Update all stored properties that changed
- Recalculate approval, restrict-markup, compensation hours
- If mark-up datetime changed ? update or create mark-up record
- Regenerate interface file

### `MarkUp(db, markupDateTime, user)`

Creates or updates `MarkUpRecord` with the specified datetime.

### `AddMarkUpHours()` ? DateTime

`MarkUpRecord.MarkUpDateTime + Craft.MarkUpHours` � prevents working before mark-up buffer expires.

---

## MarkUpRecord

**PK = FK** to `MarkOffRecord` (1:1). Does NOT inherit ControlNumberBase.

| Property | Type | Description |
|---|---|---|
| `MarkOffRecordControlNumber` | `long` | PK/FK |
| `MarkUpDateTime` | `DateTime` | When employee returns to duty |
| `CreatedFromTIES` | `bool` | Created from TIES import |
| `CreatedBy` | `string` | |
| `CreatedDate` | `DateTime` | |
| `ModifiedBy` | `string` | |
| `ModifiedDate` | `DateTime` | |
| `CreatedByName` | computed | Opens DbContext to resolve name |
| `ModifiedByName` | computed | Opens DbContext to resolve name |

---

## MarkOffRequestRecord

**Inherits**: `ControlNumberBase`

Advance request for future mark-off (vacation, personal day, etc.).

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `EmployeeControlNumber` | `long` | FK |
| `RailroadEmployeeControlNumber` | `long` | FK |
| `RailroadPoolEmployeeControlNumber` | `long` | FK |
| `CraftControlNumber` | `long` | FK |
| `MarkOffCodeControlNumber` | `long` | FK |
| `MarkOffDateTime` | `DateTime` | Requested mark-off time |
| `RequestDate` | `DateTime` | Date of the request |
| `EntryDateTime` | `DateTime` | When request was entered (default `DateTime.Now`) |
| `AutomaticMarkOff` | `bool` | Whether system auto-marks-off |
| `Notes` | `string` | nullable |

### Computed Properties

| Property | Logic |
|---|---|
| `TimeOff` | If no mark-up: vacation weeks ? `weeks � 7 days`; else `Now - MarkOffDateTime`. With mark-up: handles DST comparison |
| `NumberOfDays` | If no mark-up ? 0; else ? `DaysOff` |
| `DaysOff` | `TimeOff.Days + (1 if TimeOff.Hours > 0)` |
| `IsComplete` | `MarkOffRequestMarkOffRecords.Count > 0` |
| `RequestDateString` | `RequestDate.ToString("MMMM dd, yyyy")` |

### Navigation Properties

| Property | Type |
|---|---|
| `Employee` | `Employee` |
| `RailroadEmployee` | `RailroadEmployee` |
| `RailroadPoolEmployee` | `RailroadPoolEmployee` |
| `Craft` | `Craft` |
| `MarkOffCode` | `MarkOffCode` |
| `MarkOffRequestDelete` | `MarkOffRequestDelete` (1:1, nullable) |
| `MarkOffRequestMarkUpRecord` | `MarkOffRequestMarkUpRecord` (1:1, nullable) |
| `MarkOffRequestApproval` | `MarkOffRequestApproval` (1:1, nullable) |
| `MarkOffRequestMarkOffRecords` | `ICollection<MarkOffRequestMarkOffRecord>` |
| `MarkOffRequestTempRecords` | `ICollection<MarkOffRequestTempRecord>` |
| `MarkOffRequestMarkOffRequestWaitListRecords` | `ICollection<MarkOffRequestMarkOffRequestWaitListRecord>` |

### `CreateMarkOffRecord(db, user, vacrelief, update)` � Request Fulfillment

1. Check for existing open mark-off on same date/code ? skip if duplicate
2. If prior open mark-off exists on earlier date ? auto mark-up at request date
3. Create `MarkOffRecord` via `CreateMarkOffRecord()` (the full workflow above)
4. If request has `NumberOfDays > 0` and no mark-up ? auto mark-up at `MarkOffDateTime + days`
5. If `update` ? trigger vacancy update + reset mark-off request timer

---

## Supporting Entities

### MarkOffRecordDelete
- PK = FK to MarkOffRecord. `DeletedDateTime`, `CreatedBy`, `CreatedDateTime`. Soft-delete marker.

### MarkOffRecordApproval
- PK = FK to MarkOffRecord. `EmployeeControlNumber` (the approving officer). Audit fields.

### MarkOffRequestMarkUpRecord
- PK = FK to MarkOffRequestRecord. `MarkUpDateTime`. Pre-scheduled mark-up for the request.

### MarkOffRequestMarkOffRecord
- Composite key: `MarkOffRequestRecordControlNumber` + `MarkOffRecordControlNumber`. Links a request to its fulfilled mark-off.

### MarkOffRequestApproval
- PK = FK to MarkOffRequestRecord. `EmployeeControlNumber` (approver). Approval/denial tracking.

### MarkOffRequestDelete
- PK = FK to MarkOffRequestRecord. Soft-delete marker.

### MarkOffRequestWaitListRecord
**Inherits**: `ControlNumberBase`
- When more employees request a date than allowed, extras go on the wait list.
- FK to `MarkOffCode`, `RailroadPoolEmployee`. `RequestDate`, `WaitListOrder`.

### DailyExtraBoardMarkOffRecord
- Links mark-off to extra board position. `TieUpOrder`, `BoardOrder` � saved for restoration on delete.

### DailyRailroadEmployeePositionMarkOffRecord
- Links mark-off to daily position record. `MOCode` denormalized.

### DailyCrewPositionOnDutyMarkOffRecord
- PK = FK to `DailyCrewPositionOnDutyRecord`. Links on-duty record to position mark-off.

### MarkOffPayrollCode
- PK = FK to MarkOffCode. Links to `PayrollCode`. Defines the payroll code used when compensation is deducted.

### MarkOffMarkUpHours
- PK = FK to MarkOffCode. `MarkUpHours` (int). Configurable auto mark-up hours (overridden by hard-coded vacation values).

### CraftMarkOffCode
- Links Craft to MarkOffCode with `AutomaticMarkUpHours` override. Allows per-craft mark-up timing.

### MarkOffCodeApprovalOfficer
- Links MarkOffCode to Employee. Defines who approves a specific type of mark-off.

---

## Mark-Off Flow Summary

```
Employee/System requests mark-off
  ?
MarkOffRequestRecord created (if advance request)
  ?
MarkOffRecord.CreateMarkOffRecord() � full workflow
  ?? Sets all properties, calculates comp hours
  ?? Updates daily records (XB mark-off or crew position mark-off)
  ?? Links to request if found
  ?? Creates approval record if officer specified
  ?? Checks compensation balance
  ?? Creates interface file
  ?? If auto-markup: creates mark-up record immediately
  ?? If not auto-markup: updates XB position, pushes to back of board
  ?? If on-duty: creates off-duty record + payroll + review
  ?
Vacancy assignment updates (employees shifted to fill gap)
  ?
Mark-up occurs (auto or manual)
  ?? MarkUpRecord created
  ?? XB position restored
  ?? Vacancy assignment re-runs
```
# Part 15: Seniority & Bulletin System

## Overview

The seniority system determines employee ranking within roster positions. The bulletin system manages the process of posting open positions for bidding and assignment. Seniority moves allow employees to exercise seniority to claim positions.

## SeniorityState

Lookup table defining seniority lifecycle states.

| StateID | StateDescription | Active | CutBack | Inactive |
|---|---|---|---|---|
| (known states) | | | | |

### Properties

| Property | Type | Description |
|---|---|---|
| `StateID` | `int` | PK (no auto-generate) |
| `StateDescription` | `string` | `[Required, StringLength(50)]` |
| `Active` | `bool` | Employee is actively assigned to this roster |
| `CutBack` | `bool` | Employee was cut back (displaced) from this roster |
| `Inactive` | `bool` | Seniority record is inactive |

Only one of `Active`, `CutBack`, `Inactive` is true at a time. Used throughout the system to determine position assignments and training eligibility.

---

## Seniority

**Inherits**: `ControlNumberBase` (partial class)

Links a `RailroadPoolEmployee` to a `Roster` with a date and rank.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `RosterControlNumber` | `long` | FK to Roster |
| `RailroadPoolEmployeeControlNumber` | `long` | FK to RailroadPoolEmployee |
| `LastActiveRoster` | `bool` | `[Required]` � snapshot of whether this was the active roster |
| `RosterDate` | `DateTime` | `[Required]` � seniority date (default `DateTime.Now`) |
| `Rank` | `int` | `[Required]` � position within the roster (lower = more senior) |
| `StateID` | `int` | `[Required]` � FK to SeniorityState |
| `CanTrain` | `bool` | `[Required]` � whether employee can train others |

### Computed Properties

| Property | Logic |
|---|---|
| `RosterDate_Rank` | `"{MM/dd/yyyy} {rank with leading zeros to 3}"` � for display/sort |
| `SeniorityYears` | `DateTimeUtilities.CalculateYears(RosterDate)`; returns 0 if `9999-12-31` |
| `EmploymentDate` | Delegates through `RailroadPoolEmployee.RailroadEmployee.Employee.EmploymentDate` |
| `ServiceYears` | Delegates to `Employee.VacationServiceYears` |

### Navigation Properties

| Property | Type |
|---|---|
| `RailroadPoolEmployee` | `RailroadPoolEmployee` |
| `Roster` | `Roster` |
| `SeniorityState` | `SeniorityState` |
| `SeniorityEndDate` | `SeniorityEndDate` (1:1, nullable) |

### `Create(db, user)`

1. If `RosterDate` is in the future ? set `StateID = 0` (pending)
2. Set `LastActiveRoster = SeniorityState.Active`
3. Save to `db.Seniority`
4. **If state is Active**:
   - `RailroadPoolEmployee.InactivateSeniority(db, user, thisControlNumber)` � deactivates all other active seniority records and unassigns associated positions
   - `RailroadPoolEmployee.AssignRailroadPosition(db, user, now, rosterControlNumber)` � assigns employee to appropriate position on this roster

---

## Roster

**Inherits**: `ControlNumberBase` (partial class)

Groups positions and seniority within a craft.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `CraftControlNumber` | `long` | FK to Craft |
| `RailroadPayrollDepartmentControlNumber` | `long` | `[Required]` FK |
| `RosterName` | `string` | `[Required, StringLength(250)]` |
| `RosterPluralName` | `string` | `[Required, StringLength(250)]` |
| `RosterNumber` | `int` | `[Required]` � ordering |
| `Training` | `bool` | `[Required]` � roster is for trainees |
| `ExtraBoard` | `bool` | `[Required]` � roster has an extra board |
| `OvertimeBoard` | `bool` | `[Required]` � roster has an overtime board |

### Navigation Properties

| Property | Type |
|---|---|
| `Craft` | `Craft` |
| `RosterBulletinRule` | `RosterBulletinRule` (1:1, nullable) |
| `RosterSeniorityMoveRule` | `RosterSeniorityMoveRule` (1:1, nullable) |
| `RailroadPayrollDepartment` | `RailroadPayrollDepartment` |
| `DailyShiftOvertimeBoards` | `ICollection<DailyShiftOvertimeBoard>` |
| `Positions` | `ICollection<Position>` |
| `RosterBoards` | `ICollection<RosterBoard>` |
| `Seniority` | `ICollection<Seniority>` |

---

## RailroadPositionBulletin

**Inherits**: `ControlNumberBase` (partial class)

Represents a posted opening for a railroad position that employees can bid on.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `RailroadPositionControlNumber` | `long` | `[Required]` FK |
| `OpenDateTime` | `DateTime` | `[Required]` � when bidding opens |
| `CloseDateTime` | `DateTime` | `[Required]` � when bidding closes |
| `EffectiveDateTime` | `DateTime` | `[Required]` � when assignment takes effect |

### Computed Status Properties

| Property | Logic |
|---|---|
| `NumberOfBids` | `RailroadPositionBulletinBids.Count` |
| `NumberOfUnassignedBids` | Count of bids where `BidAssignment == null` |
| `IsOpen` | Not closed AND `Now > OpenDateTime` |
| `IsClosed` | `Now > CloseDateTime` |
| `IsActive` | If closed: no assignment and no no-bid. If open: `Now >= OpenDateTime` |
| `NoBidsReceived` | Closed AND `NumberOfBids == 0` |
| `IsNoBid` | Closed AND `NumberOfUnassignedBids == 0` (all bids assigned elsewhere) |
| `ForceAssign` | `IsNoBid AND !RailroadPosition.IsExtraBoard` � crew positions must be force-assigned |
| `SetAsNoBid` | `IsNoBid AND RailroadPosition.IsExtraBoard` � XB positions just get no-bid flag |
| `IsAssigned` | Has `BulletinAssignment` OR `BulletinNoBid` |
| `IsUnAssigned` | Closed AND not assigned AND not no-bid |
| `CanCancelBulletin` | `!IsClosed` |
| `CanAssignBulletin` | `IsUnAssigned AND AssignDateTime <= Now` |
| `CanSetAsNoBid` | No no-bid record AND closed |
| `CanForceAssignBulletin` | `ForceAssign AND Now > AssignDateTime` |
| `AssignedByBulletin` | Position's current assignment type == `"BA"` |
| `VacatedBy` | Last `RailroadPositionHistory` entry's employee name |

### `AssignDateTime` � Craft-Specific Forced Assignment Timing

When a bulletin is a no-bid and the position is a crew position (not XB):

1. Get `BulletinRule` and crew position's `Crew`
2. Find next work date from `EffectiveDateTime`
3. Calculate: `nextWorkDate + OnDutyTime - ForcedAssignHours`
4. **Craft overrides**:
   - Clerical ? `CloseDateTime` (assign immediately at close)
   - Engineer ? `EffectiveDateTime` if same day as next work date
   - Mechanical ? `EffectiveDateTime` always
   - Default ? calculated `assigndatetime`

### Navigation Properties

| Property | Type |
|---|---|
| `RailroadPosition` | `RailroadPosition` |
| `RailroadPositionBulletinAssignment` | `RailroadPositionBulletinAssignment` (1:1, nullable) |
| `RailroadPositionBulletinNoBid` | `RailroadPositionBulletinNoBid` (1:1, nullable) |
| `RailroadPositionBulletinBids` | `ICollection<RailroadPositionBulletinBid>` |
| `RailroadPositionBulletinBidAssignments` | `ICollection<RailroadPositionBulletinBidAssignment>` |

### `Assign(db, rpectrlnbr, assigndate, type, user)`

1. Update `EffectiveDateTime` if it differs from `assigndate`
2. Find the bidding employee
3. If employee is on a hangout position AND this is a no-bid ? suppress notification
4. `RemoveUnassignedSeniorityMoves()` � clean up pending moves
5. `UnassignRailroadPositions()` � remove from current position on this roster
6. For ALL of this employee's unassigned bids on assignable bulletins ? create `BulletinBidAssignment` (marks bids as used)
7. Create `RailroadPositionBulletinAssignment` record
8. `RailroadPosition.Assign()` � physically assign employee to the position

### `AutomaticAssignment(db, bulletin, user)` � Recursive

Called by the bulletin timer. Processes bids in seniority order:

1. Get bids sorted by seniority via `CollectionLists.GetRailroadPositionBulletinBids()`
2. For each bid:
   - Check `IsQualified()` for the position
   - If preference == 1 (highest priority) ? assign immediately
   - If preference > 1 ? check if employee has higher-preference bids on other active bulletins
     - **Recursively** call `AutomaticAssignment()` on those higher-preference bulletins first
     - If employee gets assigned to a higher-preference bulletin ? skip to next bid
     - If NOT assigned to higher-preference ? assign to this bulletin

### `SetNoBid(db, bulletin, user)`

Creates `RailroadPositionBulletinNoBid` record. Creates interface file.

### `SetBulletinDays(selectedDay)`

Calculates Open/Close/Effective datetimes using `BulletinRule`:
- `OpenDateTime` = today + selectedDay, at `BulletinStartTime`
- `CloseDateTime` = OpenDateTime + `BulletinHours`, at `BulletinCloseTime`
- `EffectiveDateTime` = CloseDateTime + `EffectiveDay`, at `BulletinEffectiveTime`

### `RemoveRailroadPositionBulletin(db, date, user)`

Cancels a bulletin. For each bid, creates a `RailroadPositionChange` notification: `"The bulletin for position {name}, effective {date}, has been canceled."`

---

## SeniorityMove

**Inherits**: `ControlNumberBase` (partial class)

An employee's request to exercise seniority to move to a different position.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `RailroadPoolEmployeeControlNumber` | `long` | `[Required]` FK � the moving employee |
| `RailroadPositionControlNumber` | `long` | `[Required]` FK � the target position |
| `EffectiveDateTime` | `DateTime` | `[Required]` � when the move takes effect |
| `RequestDateTime` | `DateTime` | `[Required]` � when the move was requested (default `DateTime.Now`) |
| `MoveType` | `string` | `[Required]` � type of seniority move |
| `AutoProcess` | `bool` | `[Required]` � whether to process automatically |

### Computed Properties

| Property | Logic |
|---|---|
| `WillWorkOption` | True if employee's current position is a crew position (not board) AND the effective datetime matches the on-duty time |
| `IsMoveFromHangout` | Current position is a roster board with `AutoAssign` |

### Navigation Properties

| Property | Type |
|---|---|
| `RailroadPosition` | `RailroadPosition` (target) |
| `RailroadPoolEmployee` | `RailroadPoolEmployee` (moving employee) |
| `SeniorityMoveAssignment` | `SeniorityMoveAssignment` (1:1, nullable) |
| `SeniorityMoveWillWork` | `SeniorityMoveWillWork` (1:1, nullable) |

### `CanAssign()` ? bool

`EffectiveDateTime < Now` � move can be processed.

### `CanCancel()` ? bool

`(EffectiveDateTime - Now).TotalHours > SeniorityMoveRule.CancelHours` � within cancellation window.

### `Assign(db, assigndate, user)`

Full seniority move execution:

1. **Identify players**: `bumpingemployee` (the mover), `bumpedemployee` (current occupant of target position)
2. If target position is occupied:
   - Get current occupant
   - `RailroadPosition.Unassign()` � remove occupant from position
   - `bumpedemployee.RemoveUnassignedSeniorityMoves()` � clean up their pending moves
   - `bumpedemployee.AssignRailroadPosition()` � auto-assign to a hangout position
   - If NOT moving from hangout AND bumped employee lands on hangout ? create bump notification
3. **Remove mover's pending moves**: `bumpingemployee.RemoveUnassignedSeniorityMoves()`
4. **Unassign mover**: `bumpingemployee.UnassignRailroadPositions()`
5. **Create assignment record**: `SeniorityMoveAssignment`
6. **Assign mover to target**: `RailroadPosition.Assign(db, user, rpemployee, "SM", ...)`
   - Type `"SM"` = Seniority Move

---

## Supporting Entities

### RosterBulletinRule
- PK = FK to Roster (1:1). Configures bulletin timing per roster.
- `BulletinHours` (int): hours the bulletin stays open
- `BulletinStartTime` (TimeSpan): time of day bulletin opens
- `BulletinCloseTime` (TimeSpan): time of day bulletin closes
- `BulletinEffectiveTime` (TimeSpan): time effective assignment starts
- `EffectiveDay` (int): days after close for effective date
- `ForcedAssignHours` (int): hours before on-duty to force-assign no-bids

### RosterSeniorityMoveRule
- PK = FK to Roster (1:1). Configures seniority move rules per roster.
- `CancelHours` (int): minimum hours before effective to allow cancellation

### RailroadPositionBulletinBid
**Inherits**: `ControlNumberBase`
- `RailroadPositionBulletinControlNumber` (long): FK to bulletin
- `RailroadPoolEmployeeControlNumber` (long): FK to bidding employee
- `Preference` (int): bid priority (1 = highest preference)
- Navigation: `RailroadPositionBulletin`, `RailroadPoolEmployee`, `RailroadPositionBulletinBidAssignment` (1:1, nullable)

### RailroadPositionBulletinAssignment
- PK = FK to Bulletin (1:1). `RailroadPoolEmployeeControlNumber`, `AssignedDateTime`. Records who won the bulletin.

### RailroadPositionBulletinBidAssignment
- PK = FK to BulletinBid. `RailroadPositionBulletinControlNumber` � marks which bulletin the bid was consumed by.

### RailroadPositionBulletinNoBid
- PK = FK to Bulletin (1:1). `AssignedDateTime`. Marks bulletin as no-bid (no qualified bidders or all bids consumed elsewhere).

### SeniorityMoveAssignment
- PK = FK to SeniorityMove (1:1). Records completion of the move.

### SeniorityMoveWillWork
- PK = FK to SeniorityMove (1:1). Records employee's election to work their old position before the move.

### SeniorityEndDate
- PK = FK to Seniority (1:1). `EndDate` (DateTime). Optional end date for seniority record.

---

## Process Flow � Bulletin Lifecycle

```
Position becomes vacant
  ?
RailroadPositionBulletin created with Open/Close/Effective dates
  ?
Bidding period (Open ? Close)
  ?? Employees create RailroadPositionBulletinBid with Preference
  ?? Can cancel bulletin during this period
  ?
Bulletin closes (Now > CloseDateTime)
  ?
AutomaticAssignment() � recursive by preference
  ?? Highest-preference bid with qualified employee wins
  ?? Lower-preference bids checked recursively
  ?? All consumed bids get BidAssignment records
  ?
If bids exist ? Assign() ? position occupied
If no valid bids:
  ?? Crew position ? ForceAssign at AssignDateTime (junior employee bumped)
  ?? XB position ? SetAsNoBid
```

## Process Flow � Seniority Move Lifecycle

```
Employee requests move to target position
  ?
SeniorityMove created with EffectiveDateTime
  ?
Timer checks CanAssign() periodically
  ?
When EffectiveDateTime passes:
  Assign()
    ?? Target occupant bumped to hangout
    ?? Mover removed from current position
    ?? Mover assigned to target position (type "SM")
    ?? Notifications sent to bumped employee
```
# Part 19: Extra Board Management

## Overview

The extra board system manages relief employees who fill vacancies on crew positions. Extra board employees are ordered by tie-up time and board order, and assigned to vacant positions as needed.

## RosterBoard

**Inherits**: `ControlNumberBase`

Defines a board within a roster (extra board, hangout board, etc.).

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `RosterControlNumber` | `long` | FK to Roster |
| `BoardNumber` | `int` | Ordering number |
| `BoardName` | `string` | Display name |
| `Available` | `bool` | Whether board is active |
| `ExtraBoard` | `int` | `0`=not XB, `1`=FIFO, `2`=rotating |
| `ForceAssign` | `bool` | Force-assign from this board |
| `AutoAssign` | `bool` | Auto-assign (hangout board) |
| `BulletinPositions` | `bool` | Positions are bulletined |
| `ApplySeniorityMoveRule` | `bool` | Seniority moves apply |
| `ExtendedAbsence` | `bool` | For extended absence tracking |

### Board Types

| ExtraBoard Value | Type | Ordering |
|---|---|---|
| `0` | Not an extra board | N/A |
| `1` | First-In-First-Out (FIFO) | Earliest tie-up first |
| `2` | Rotating | Round-robin assignment |

### Computed Properties

| Property | Logic |
|---|---|
| `PositionCount` | `CollectionLists.GetRosterBoardCount()` |
| `AverageDailyVacanciesLast30Days` | Vacancies in date range / days |
| `AverageDailyVacanciesLast12Months` | Same for 12 months |

---

## DailyShiftExtraBoard

**Inherits**: `ControlNumberBase`

One instance per roster board per shift. Container for extra board positions.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `DailyAssignmentShiftControlNumber` | `long` | FK to DailyAssignmentShift |
| `RosterBoardControlNumber` | `long` | FK to RosterBoard |
| `Completed` | `bool` | Whether this board's shift is done |
| `AverageVacancies` | `int` | Average vacancies for this board |
| `RequiredPositions` | `int` | Positions needed |
| `ExtraBoardPercentage` | `int` | Board fill percentage |

### Computed

| Property | Logic |
|---|---|
| `IsRotatingBoard` | `RosterBoard.ExtraBoard == 2` |
| `IsFirstInFirstOutBoard` | `RosterBoard.ExtraBoard == 1` |

### `CreateDailyShiftExtraBoardPositions(db, shift, board, user, now)`

Two modes:

**First board (no previous)**: Creates from `GetAvailableExtraBoardPositions()`:
- `BoardOrder = 0`, `TieUpOrder = 0`
- Snapshots: `Status`, `TwentyFourHourRestDateTime`, position name, consecutive days, ST days worked
- If marked off ? `UpdateDailyExtraBoardMarkOffRecords()`
- Runs `QualifyHolidayRecord()` for each employee

**Subsequent boards**: Copies from last uncompleted board:
- Orders by `TieUpOrder` then `BoardOrder`
- Re-sequences `BoardOrder` in increments of 10
- Preserves `TieUpOrder`
- If employee was called (assigned + on-duty is called) ? copies assignment
- Refreshes status snapshots from live employee data

---

## DailyShiftExtraBoardPosition

**Inherits**: `ControlNumberBase`

One employee slot on a daily extra board.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `DailyShiftExtraBoardControlNumber` | `long` | FK |
| `RailroadPoolEmployeeControlNumber` | `long` | FK |
| `BoardOrder` | `int` | Position on the board (increments of 10) |
| `TieUpOrder` | `long` | Tie-up timestamp for ordering (lower = tied up earlier = first out) |
| `DaysWorked` | `int` | ST days worked snapshot |
| `ConsecutiveDays` | `int` | Consecutive days snapshot |
| `Status` | `string` | Employee status snapshot |
| `TwentyFourHourRestDateTime` | `string` | 24-hour rest datetime snapshot |
| `RosterBoardPositionName` | `string` | Position name snapshot |

### Computed Properties

| Property | Logic |
|---|---|
| `IsMarkedOff` | Has mark-off record AND `Now <= MarkUpDateTime` |
| `IsAssigned` | `DailyShiftExtraBoardPositionAssignment != null` |
| `IsEmployeeAssigned` | Checks current + completed boards for assignment |
| `DailyCrewPositionControlNumber` | Resolves from assignment or last completed board |
| `RosterControlNumber` | `DailyShiftExtraBoard.RosterBoard.RosterControlNumber` |

### Key Methods

- `SetTieUpOrder(db, tieuporder, user, now, save)` � updates tie-up order
- `ResetTieUpOrder(db, tieuporder, boardorder, user)` � restores after mark-off delete
- `UpdateDailyExtraBoardMarkOffRecord(db, markoff, user, now)` � links mark-off
- `UpdateDailyExtraBoardMarkOffRecords(db, user, now)` � processes all open mark-offs

---

## DailyShiftExtraBoardPositionAssignment

Links an XB position to a `DailyCrewPositionOnDutyRecord` when the employee is assigned to fill a vacancy.

| Property | Type | Description |
|---|---|---|
| `DailyShiftExtraBoardPositionControlNumber` | `long` | FK (PK) |
| `DailyCrewPositionOnDutyRecordControlNumber` | `long` | FK |
| `BoardOrder` | `int` | Saved board order at assignment time |
| `TieUpOrder` | `long` | Saved tie-up order at assignment time |

---

## Board Ordering Logic

### FIFO Board (ExtraBoard == 1)
- Employee who tied up earliest (lowest `TieUpOrder`) is first out
- After tie-up: `TieUpOrder` = encoded datetime of off-duty time

### Rotating Board (ExtraBoard == 2)
- Round-robin: after assignment, employee moves to back of board
- `TieUpOrder` set to future date (e.g., `modate.AddYears(10)`)

### Mark-Off Impact
- When marked off: `TieUpOrder` pushed to far future (`modate + 10 years`)
- Original `TieUpOrder` and `BoardOrder` saved in `DailyExtraBoardMarkOffRecord`
- When mark-off deleted: restored from saved values
# Part 21: Roster Board & Hangout System

## Overview

Roster boards are non-crew positions within a roster. They include extra boards, hangout (auto-assign) boards, and extended absence boards. Employees land on these boards when not assigned to a specific crew position.

## RosterBoardPosition

**PK = FK** to `RailroadPosition` (1:1). Implements `IAutoMarkUp`.

| Property | Type | Description |
|---|---|---|
| `RailroadPositionControlNumber` | `long` | PK/FK |
| `RosterBoardControlNumber` | `long` | FK to RosterBoard |
| `PositionNumber` | `int` | Ordering within the board |
| `PositionName` | `string` | Display name |

### Computed Properties

| Property | Logic |
|---|---|
| `ApprovalOfficer` | `RosterBoard.Roster.Craft.ApprovalOfficer` |
| `IsExtraBoard` | `RosterBoard.ExtraBoard != 0` |
| `IsExtendedAbsence` | `RosterBoard.ExtendedAbsence` |
| `IsHangout` | `RosterBoard.AutoAssign` (hangout = auto-assign board) |

## Board Types and Employee Placement

| Board Type | `RosterBoard` Flags | When Employee Lands Here |
|---|---|---|
| Extra Board | `ExtraBoard != 0` | Assigned to XB position on roster |
| Hangout | `AutoAssign = true` | Bumped from crew position by seniority move; waiting for assignment |
| Extended Absence | `ExtendedAbsence = true` | Long-term leave (FMLA, disability, etc.) |
| Force Assign | `ForceAssign = true` | Junior employees forced into positions via no-bid bulletins |

## Hangout Processing

The hangout timer (`ProcessHangouts` in Global.asax) auto-assigns employees from hangout boards to open crew positions:

1. Find employees on hangout boards (`AutoAssign = true`)
2. Check if there are open crew positions on the same roster
3. If qualified ? assign employee to open position
4. Create `DailyRosterBoardPositionHangoutRecord` tracking the auto-assignment

## DailyRosterBoardPositionHangoutRecord

| Property | Type | Description |
|---|---|---|
| `RailroadPoolEmployeeControlNumber` | `long` | FK |
| `MarkOffCodeControlNumber` | `long` | FK (nullable) |
| `HangoutDateTime` | `DateTime` | When placed on hangout |

## Roster Board Mark-Off Processing

The `RosterBoardMarkOffTimers` auto-process mark-offs for board employees:
- Checks if board employees have pending mark-off requests
- Creates mark-off records for requested dates
- Updates `DailyRailroadEmployeePositionMarkOffRecord`
# Part 27: Temporary Assignments & HoldDowns

## Overview

Two mechanisms for temporary duty changes.

## TemporaryAssignment

**Inherits**: `ControlNumberBase`

Temporary modification to a base assignment (different schedule, billing, etc.).

| Property | Type | Description |
|---|---|---|
| `AssignmentControlNumber` | `long` | FK to base Assignment |
| `TemporaryAssignmentName` | `string` | Display name |
| `StartDate` | `DateTime` | When temp assignment begins |
| `AssignmentOnDutyTimeControlNumber` | `long` | FK � may differ from base |
| `StraightTimeHours` | `int` | ST hours for this temp assignment |
| `Billable` | `bool` | Whether billable |
| `Recollectable` | `bool` | Whether recollectable |

### Related Entities

| Entity | Description |
|---|---|
| `TemporaryAssignmentRelease` | End/release record |
| `TemporaryAssignmentWorkDay` | Which days of the week this runs |
| `TemporaryAssignmentAssignedEmployee` | Employee assigned to temp |
| `TemporaryAssignmentAFERecord` | AFE billing for temp |

### Status: `IsOpen` = no release record OR release date in future. `IsClosed` = released.

---

## HoldDown

**Inherits**: `ControlNumberBase`

An employee "holding down" a position temporarily (usually an extra board employee filling a regular position for a defined period).

| Property | Type | Description |
|---|---|---|
| `RailroadPoolEmployeeControlNumber` | `long` | FK � employee holding down |
| `RailroadPositionControlNumber` | `long` | FK � position being held |
| `StartDate` | `DateTime` | Hold-down start |
| `EndDate` | `DateTime` | Expected end |

### Related: `HoldDownRelease` � early release from hold-down.
# Part 29: On-Duty Billing & Tie-Up

## Overview

When employees work, various billing records are attached to their on-duty records for cost tracking and invoicing.

## Billing Record Types

All link to `DailyCrewPositionOnDutyRecord` via FK.

| Entity | Description | Controller |
|---|---|---|
| `DailyOnDutyAFEBillingRecord` | AFE (Authorization for Expenditure) billing | `DailyOnDutyAFEBillingController` |
| `DailyOnDutyZoneBillingRecord` | Zone-based billing | `DailyOnDutyZoneBillingController` |
| `DailyOnDutyMiscellaneousBillingRecord` | Miscellaneous charges | `DailyOnDutyMiscellaneousBillingController` |
| `DailyOnDutyLocomotiveRecord` | Locomotive usage records | `DailyOnDutyLocomotiveRecordController` |
| `DailyOnDutyRailroadMaterialRecord` | Material usage records | `DailyOnDutyRailroadMaterialRecordController` |
| `DailyOnDutyPayrollInformation` | Payroll-specific on-duty info (trainee, etc.) |
| `DailyOnDutyUnavailableRecord` | Tracks unavailable time during duty |
| `DailyOnDutyDidNotWorkRecord` | Records when employee was on duty but didn't work |
| `DailyAssignmentAFERecord` | AFE record at the assignment level |

## Tie-Up Process

"Tie-up" = going off duty. Managed by `DailyOnDutyRecordTieUpController`.

### Flow
```
Employee reports off duty
  ?
DailyCrewPositionOffDutyRecord created
  ?? Off-duty datetime recorded
  ?? FRA hours checked (Part 7)
  ?? OffPropertyTieUpRecord if off-property
  ?
PayrollRecord auto-generated
  ?? ST/OT hours calculated
  ?? Billing records attached
  ?? Auto-pay records applied (PayrollCrewPositionAutoPayRecord)
  ?
Extra board TieUpOrder updated (Part 19)
  ?
Teams "TieUpMessage" sent
  ?
DailyAssignmentShift completion check
```

### OffPropertyTieUpRecord
When employee ties up away from home terminal. Tracks location for deadhead billing.

### PayrollCrewPositionAutoPayRecord
Configuration for automatic payroll additions per crew position (e.g., travel pay, meal allowance). Applied during tie-up.
# Part 31: Daily Status Records

## Overview

The system creates a daily snapshot of every employee's status and position. These records drive holiday qualification, payroll, and reporting.

## DailyRailroadEmployeeStatusRecord

**Inherits**: `ControlNumberBase`

One per employee per day. Snapshots employment status.

| Property | Type | Description |
|---|---|---|
| `EmployeeControlNumber` | `long` | FK |
| `RailroadEmployeeControlNumber` | `long` | FK |
| `Date` | `DateTime` | The date |
| `EmploymentStatusControlNumber` | `long` | FK to EmploymentStatus |
| `StatusCode` | `string` | `[StringLength(4)]` � denormalized status code (e.g., `"AT"`) |
| `FlagCode` | `string` | `[StringLength(1)]` � optional flag |

### Timer: `DailyRailroadEmployeeStatusTimers` creates these daily for every employee.

---

## DailyRailroadEmployeePositionRecord

**Inherits**: `ControlNumberBase`

Child of status record. Snapshots what position each employee is on each day.

| Property | Type | Description |
|---|---|---|
| `DailyRailroadEmployeeStatusRecordControlNumber` | `long` | FK |
| `RailroadPoolEmployeeControlNumber` | `long` | FK |
| `RailroadPositionControlNumber` | `long` | FK |

### Computed Properties

| Property | Logic |
|---|---|
| `IsHangout` | `DailyRosterBoardPositionHangoutRecord != null` |
| `IsNotifiedHangout` | Is hangout AND mark-off code is not excused |

### Related

- `DailyRosterBoardPositionHangoutRecord` � if employee is on a hangout board
- `DailyRailroadEmployeePositionPayrollRecords` � payroll generated from position
- `DailyRailroadEmployeePositionMarkOffRecords` � mark-offs during position

### `CreateDailyRosterBoardPositionHangoutRecord(db, change, code, user, now)`

Creates a hangout record linking position record to the change notification and mark-off code.

### `CreatePayrollRecord(db, morec, paydate, user, now)`

Creates payroll from a daily position mark-off record.

---

## DailyRailroadPositionOffDayRecord

**Composite PK**: `RailroadPositionControlNumber` + `AssignmentDate`. Tracks which positions have off-days.

| Property | Type | Description |
|---|---|---|
| `RailroadPositionControlNumber` | `long` | PK/FK |
| `AssignmentDate` | `DateTime` | PK |
| `PositionName` | `string` | Snapshot |

Child: `DailyRailroadPositionOffDayEmployeeRecord` � tracks which employee was on the position.

### Timer: `DailyOffDayTimers` creates these daily.
# Part 32: Change Notification System

## Overview

Tracks position changes and notifies affected employees. Two entities work together: `RailroadPositionChange` (the event) and `ChangeNotification` (the notification attempt).

## RailroadPositionChange

**Inherits**: `ControlNumberBase` (partial class)

| Property | Type | Description |
|---|---|---|
| `RailroadPositionControlNumber` | `long` | FK � position that changed |
| `RailroadPoolEmployeeControlNumber` | `long` | FK � affected employee |
| `ChangeDateTime` | `DateTime` | When the change takes effect |
| `ChangeText` | `string` | Description of the change |
| `NotificationRequired` | `bool` | Whether employee must be notified |
| `ShowInHistory` | `bool` | Whether to display in position history |
| `EmployeeOnly` | `bool` | Whether only the employee can see it |

### Computed

| Property | Logic |
|---|---|
| `IsComplete` | If notification not required ? true. Else ? has at least one confirmed `ChangeNotification` |
| `IsOpen` | `!IsComplete` |

### Navigation

- `ChangeNotifications` � `ICollection<ChangeNotification>`
- `MoveOrBulletins` � `ICollection<ChangeMoveOrBulletin>` (links to seniority move or bulletin)
- `DailyRosterBoardPositionHangoutRecords` � hangout records created by this change

---

## ChangeNotification

**Inherits**: `ControlNumberBase`

Each notification attempt for a position change.

| Property | Type | Description |
|---|---|---|
| `RailroadPositionChangeControlNumber` | `long` | FK |
| `NotifyDateTime` | `DateTime` | When notification sent/attempted |
| `NotificationType` | `string` | `"Automatic"`, `"Phone"`, `"AtHoc"`, etc. |
| `PhoneNumber` | `string` | Phone number called (if phone notification) |
| `Confirmed` | `bool` | Whether employee confirmed receipt |
| `Notes` | `string` | `[StringLength(256)]` |

### Computed

| Property | Logic |
|---|---|
| `NbrOfDaysNotified` | If confirmed: `Now - NotifyDateTime`. Else: 0 |
| `NotifiedDays` | `NbrOfDaysNotified.Days` |

---

## ChangeMoveOrBulletin

Links a `RailroadPositionChange` to either a `SeniorityMove` or `RailroadPositionBulletin`.

| Property | Type | Description |
|---|---|---|
| `RailroadPositionChangeControlNumber` | `long` | FK |
| `MoveOrBulletinControlNumber` | `long` | FK to move or bulletin |

---

## When Changes Are Created

Position changes are created by many operations:
- Bulletin assignment/cancellation
- Seniority move (bumping)
- Vacancy assignment
- Mark-off while on duty
- Hangout assignment
- Electronic crew calling
- Wait list completion
# Part 33: Vacation Request System

## Overview

Annual vacation scheduling with seniority-based assignment, split weeks, and wait-list support.

## RailroadEmployeeVacationRequest

**Inherits**: `ControlNumberBase`

| Property | Type | Description |
|---|---|---|
| `RailroadEmployeeControlNumber` | `long` | FK |
| `PoolNumber` | `int` | Which pool the request is for |
| `SplitNbr` | `int` | Split number (vacation can be split into multiple blocks) |
| `ChoiceNbr` | `int` | Choice preference (1st, 2nd, 3rd choice) |
| `NbrOfWeeks` | `int` | Number of weeks requested |
| `RequestDate` | `DateTime` | Week start date requested |
| `CreateWaitList` | `bool` | Whether to add to wait list if denied |
| `AutoCreated` | `bool` | System-generated request |

### `IsAssigned` (Computed): `RailroadEmployeeVacationRequestAssignment != null`

## RailroadEmployeeVacationRequestAssignment

Links assigned vacation request. Created when request is fulfilled.

## Related Configuration

- `CraftVacationDay` � vacation days per service year per craft
- `RailroadEmployeeVacationOneDayTimeRecord` � one-day vacation time tracking

## Timer: `DailyVacationWeekTimers`

Processes pending vacation week assignments daily. Checks each pool for employees with upcoming vacation requests that need to generate mark-off records (V1-V5).
# Part 39: Overtime Board

## Overview

Separate from the extra board. The overtime board tracks employees available for overtime assignments.

## DailyShiftOvertimeBoard

**Inherits**: `ControlNumberBase`

One per roster per shift. Container for overtime positions.

| Property | Type | Description |
|---|---|---|
| `DailyAssignmentShiftControlNumber` | `long` | FK |
| `RosterControlNumber` | `long` | FK |
| `Completed` | `bool` | Whether processed |

## DailyShiftOvertimeBoardPosition

Employees on the overtime board for a given shift.

| Property | Type | Description |
|---|---|---|
| `DailyShiftOvertimeBoardControlNumber` | `long` | FK |
| `RailroadPoolEmployeeControlNumber` | `long` | FK |

## Usage

- Created by `DailyExtraBoardTimers` alongside extra boards
- Only for rosters where `Roster.OvertimeBoard == true`
- Queried by `GetRailroadPoolDailyOvertimeBoards(db, pool, shift)`
# Part 43: Gap Analysis � OnDutyRecord, PayrollRecord, Bulletin

Continued from Parts 41-42. Gaps 58-70 from DailyCrewPositionOnDutyRecord, PayrollRecord, and RailroadPositionBulletin.

---

## GAP 58: Tie-Up Rest/Available Calculation by CraftName (Missing from Part 5)

`CreateDailyCrewPositionOffDutyRecord()` � rest and availability calculation varies by CraftName:

| CraftName | Rest Time | Rested At | Available At | Special Rules |
|---|---|---|---|---|
| `"Clerical"` | `Craft.RequiredRestHours` | `OnDuty + RestHours` | `OnDuty + 1 day` | XB + OT ? available at off-duty |
| `"Yardmaster"` | `Craft.RequiredRestHours` | `OnDuty + RestHours` | `OnDuty + 1 day` | � |
| `"Engineer"` | FRA dynamic (10h + excess) | FRA calculated | `RestedDateTime` | Teams "TieUpMessage" sent |
| `"Yardman"` | FRA dynamic (10h + excess) | FRA calculated | `RestedDateTime` | Teams "TieUpMessage" sent |
| Default | `Craft.RequiredRestHours` | `OnDuty + RestHours` | `RestedDateTime` | � |
| null/error | 8 hours | `OffDuty + 8h` | � | Fallback |

### Default ReleaseReason parameter: `"NE"` (Normal End)

### Off-duty `Complete` flag
```
Complete = !employee.ProcessPayroll || !craft.ProcessPayroll || complete_param
```

---

## GAP 59: HoursOnDuty Pool 50 Exception (Missing from Part 5)

```csharp
// Pool 50 (Maintenance of Way):
if IsOvertime OR (DailyOnDutyPayrollInformation != null AND !FirstMealPeriod):
  HoursOnDuty = OffDuty - OnDuty  // NO meal deduction
else:
  HoursOnDuty = (OffDuty - OnDuty) - UnpaidMealPeriodMinutes
```

All other pools always deduct `Craft.UnpaidMealPeriodMinutes`.

---

## GAP 60: OnDutyRecord IsTraining � Pool 10 CutBack Logic (Missing from Part 5)

```
IsTraining:
  if Roster.Training ? true
  if Pool 10 (Y&E):
    if employee has CutBack seniority on this roster ? true
    (Covers Yardmen working as Engineers for refresher trips)
```

### HasTrainees � Pool 40 Exception
```
if Pool 40 (Mechanical): HasTrainees = false  // "Mechanical does not receive training pay"
else: check DailyAssignment for training positions on same craft
```

---

## GAP 61: OnTime / TurnoverPay (Missing from Part 5)

`OnTime` � employee is within 30 minutes of scheduled off-duty:
```
if Now + 30min < ScheduledOffDuty ? false
if Position.TurnoverPay ? add 15 minutes to off-duty
if Now > adjusted offduty ? false
else ? true
```

`Position.TurnoverPay` adds 15 hard-coded minutes to the on-duty window.

---

## GAP 62: IsClosed � 4-Day Closure Rule (Missing from Part 5)

```csharp
IsClosed = Complete AND Today > OnDutyDateTime + 4 days
```

Records are editable for 4 days after on-duty. After that, they are "closed".

---

## GAP 63: CanMoveToForeman Logic (Missing from Part 5)

Allows a Helper to be promoted to Foreman on the same assignment:
```
if this position IsHelper:
  Find Foreman positions on same DailyAssignment that are NOT assigned
  if neither position is on-duty or tied up:
    if this employee is the assigned employee ? can move
    if not assigned ? check seniority (HasSeniority)
```

---

## GAP 64: PayrollRecord Full Stored Properties (Incomplete in Part 16)

| Property | Type | Description |
|---|---|---|
| `EmployeeControlNumber` | `long` | FK (denormalized) |
| `RailroadEmployeeControlNumber` | `long` | FK (denormalized) |
| `RailroadPoolEmployeeControlNumber` | `long` | FK |
| `CraftControlNumber` | `long` | FK |
| `WorkNumber` | `string` | Work/job number |
| `Batch` | `string(6)` | Payroll batch number |
| `PayrollDate` | `DateTime` | Pay date |
| `OnDutyDateTime` | `DateTime` | On-duty timestamp |
| `OffDutyDateTime` | `DateTime` | Off-duty timestamp |
| `JobWorked` | `string(4)` | Job code worked |
| `JobPaid` | `string(4)` | Job code paid |
| `ManualEntry` | `bool` | Manually created |
| `ICCNumber` | `string` | ICC number |
| `DepartmentNumber` | `string` | Department code |
| `GeneralLedgerNumber` | `string` | GL code |
| `RatePercentage` | `int` | Tier rate percentage in effect |

### Computed Properties

| Property | Logic |
|---|---|
| `STHours` | Sum of `PayrollEarningRecords.STHours` (excluding Arbitrary) |
| `OTHours` | Sum of `PayrollEarningRecords.OTHours` (excluding Arbitrary) |
| `CalculatedEarnings` | Sum of `PayrollEarningRecords.CalculatedAmount` (excluding Declined) |
| `IsProcessed` | All earning records processed |
| `IsReviewed` | `PayrollReviewRequired == null` OR has `PayrollReviewRecord` |
| `PayrollDepartment` | From on-duty record ? position record ? pool employee fallback |

---

## GAP 65: Bulletin AssignDateTime by CraftName (Missing from Part 15)

Force-assign timing varies by craft:

| CraftName | AssignDateTime |
|---|---|
| `"Clerical"` | `CloseDateTime` (assign immediately at bulletin close) |
| `"Engineer"` | `EffectiveDateTime` (if same day as next work date) |
| `"Mechanical"` | `EffectiveDateTime` |
| Default | `NextWorkDate + OnDutyTime - ForcedAssignHours` |

For non-force-assign bulletins: always `EffectiveDateTime`.

### ForceAssign vs SetAsNoBid

| Condition | Result |
|---|---|
| No bids AND crew position | `ForceAssign = true` ? assign least-senior qualified employee |
| No bids AND extra board | `SetAsNoBid = true` ? record as no-bid, don't force |

---

## GAP 66: Bulletin Assignment Type Codes (Missing from Part 15)

| Code | Meaning | Used In |
|---|---|---|
| `"BA"` | Bulletin Assignment | `RailroadPoolEmployeePosition.AssignmentType` |
| `"SM"` | Seniority Move | `SeniorityMove.MoveType`, `RailroadPoolEmployeePosition.AssignmentType` |
| `"FA"` | Force Assignment | Bulletin no-bid force assign |
| `"MA"` | Manual Assignment | Admin-assigned |

### `Bulletin.Assign()` Flow
1. Update effective date if changed
2. Find bidding employee
3. If employee is on hangout AND no-bid ? skip notification
4. Remove unassigned seniority moves
5. Unassign employee from current roster positions
6. Assign all pending bulletin bids for this employee
7. Create `RailroadPositionBulletinAssignment` record
8. Call `RailroadPosition.Assign()` with type code

---

## GAP 67: AutomaticAssignment Bid Processing (Incomplete in Part 15)

`RailroadPositionBulletin.AutomaticAssignment()`:

1. Get bids in seniority order via `CollectionLists.GetRailroadPositionBulletinBids()`
2. For each bid (seniority order):
   - Check `IsQualified(position)` 
   - If `Preference == 1` ? assign immediately, break
   - If `Preference > 1`:
     - Check if employee can move to XB (`CanMoveToExtraBoard`)
     - Check if employee had no access (`HadNoAccess`) 
     - If had no access ? skip this bid (don't count against them)
     - Otherwise ? assign, break
3. If no bids assignable and no bids at all ? process no-bid
4. If no bids assignable but bids exist ? record each bid not assigned

---

## GAP 68: OnDutyRecord Complete State (Missing from Part 5)

```
Complete:
  if no off-duty record ? false
  if position has HoursOfService:
    if no FRA records ? OffDutyRecord.Complete
    else ? all FRA records completed
  else ? OffDutyRecord.Complete
```

### EmployeeWorked
```
if no DidNotWork record:
  if annulled OR unavailable ? false
  if marked off ? only if mark-off AFTER on-duty time (worked partial)
  else ? true
else ? false
```

---

## GAP 69: OnDutyRecord.EmployeeCalledRelief (Missing from Part 5)

```csharp
EmployeeCalledRelief = DailyCrewPositionOnDutyMarkOffRecord != null
    AND mark-off code == "CR"
```

Specific handling: employees who were called to relief mid-shift.

---

## GAP 70: DailyCrewPositionOnDutyRecord Full Stored Properties (Incomplete in Part 5)

| Property | Type | Default | Description |
|---|---|---|---|
| `DailyCrewPositionControlNumber` | `long` | � | FK |
| `RailroadPoolEmployeeControlNumber` | `long` | � | FK |
| `RailroadPositionControlNumber` | `long` | � | FK |
| `AssignmentOnDutyDate` | `DateTime` | � | Scheduled date |
| `AssignmentOnDutyTime` | `TimeSpan` | � | Scheduled time |
| `PreviousRestHours` | `int` | 0 | Hours since last off-duty |
| `PreviousRestMinutes` | `int` | 0 | Minutes since last off-duty |
| `ConsecutiveDays` | `int` | `1` | Hard-coded default in constructor |
| `STDaysWorked` | `int` | 0 | ST days worked in pay period |
| `DaysWorked` | `int` | 0 | Total days worked |
| `AssignedEmployee` | `bool` | � | Whether this is the assigned (regular) employee |
| `JobCode` | `string(4)` | � | Job code for this on-duty |
| `PayrollCodeControlNumber` | `long` | 0 | FK to PayrollCode |
| `EarningCode` | `string(2)` | � | Earning code |
| `AtHocMsgSent` | `bool` | � | Whether AtHoc notification was sent |

### Key Computed Properties Summary

| Property | Logic |
|---|---|
| `AssignmentOnDutyDateTime` | `Date + Time` |
| `AssignmentOffDutyDateTime` | If tied up ? from off-duty record; else ? `OnDuty + STHours + MealMinutes` |
| `AssignmentScheduledOffDutyDateTime` | `OnDuty + STHours` |
| `HoursOnDuty` | Pool 50 special; all others deduct meal period |
| `IsRestricted` | `TimeOnDuty >= FRA MaxHours (12)` |
| `IsOnDuty` | Not tied up, not marked off, not annulled, past on-duty time |
| `IsCalled` | Not tied up, not marked off, not on-duty yet |
| `IsTiedUp` | Has off-duty record, not annulled, not unavailable |
| `IsOvertime` | Earning code is overtime OR any payroll earning is OT |
| `IsOnOvertime` | Currently on duty AND past scheduled off-duty OR overtime earning |
| `IsLateCall` | Has `DailyCrewPositionOnDutyRecordLateCall` record |
| `IsOnDutyUpdated` | If late call ? late call confirmed; else true |
# Part 48: Gap Analysis � MarkUp, SeniorityMove, DailyAssignmentShift, DailyCrewPosition FillVacancy

Gaps 112-125 covering mark-up processing, seniority move assignment, shift creation, and remaining logic.

---

## GAP 112: MarkUp "SL" Code ? Employee FlagCode Change (Missing from Part 14)

On mark-up, if mark-off code is `"SL"`:
```
if Employee.FlagCode == "C" ? change to "R"
```

New mark-off code: `"SL"` (not previously documented). Employee flag codes `"C"` and `"R"` control some status behavior.

---

## GAP 113: Mark-Up TieUpOrder Encoding (Missing from Part 14)

On mark-up, XB positions get tie-up order set to mark-up time:
```csharp
tieuporder = Convert.ToInt64(mudate.ToString("yyyyMMddHHmm"));
```

On mark-off, XB positions get pushed to back:
```csharp
tieuporder = Convert.ToInt64(modate.AddYears(10).ToString("yyyyMMddHHmm"));
```

Mark-up also moves Mechanical employees to bottom of overtime board.

---

## GAP 114: "CR" Mark-Off Ignore Flag (Missing from Part 14)

When updating on-duty mark-off records and code is `"CR"` (Called Relief):
```
if code == "CR" AND employee is XB/hangout AND markoff after onduty:
  DailyCrewPositionOnDutyMarkOffRecord.Ignore = true
```

The `Ignore` flag on on-duty mark-off records � not previously documented.

---

## GAP 115: SeniorityMove.Assign Full Flow (Incomplete in Part 15)

1. Get bumped employee (current position holder)
2. Unassign bumped employee from position
3. Remove bumped employee's pending seniority moves
4. Assign bumped employee to hangout board
5. If move is from non-hangout AND bumped employee now on hangout:
   - Find original bump notification
   - Auto-complete open notifications
   - Create hangout record, set hangout timer
6. Unassign bumping employee from current positions
7. If `MoveType == "NA"` ? remove bumping employee's pending moves
8. Assign bumping employee to target position
9. Remove/notify other valid seniority moves for same position
10. Create `SeniorityMoveAssignment` record
11. If `MoveType == "NA"` AND position is bulletined ? create `BulletinAssignment` too

---

## GAP 116: SeniorityMove.MoveType Values (Incomplete in Part 15)

| MoveType | Description |
|---|---|
| `"SM"` | Seniority Move (exercise seniority rights) |
| `"NA"` | No Access (force assign for no-bid bulletin) |

`"NA"` triggers: remove all pending moves for bumping employee + create bulletin assignment if position is bulletined.

---

## GAP 117: SeniorityMove Cancel Hours (Missing from Part 15)

```csharp
CanCancel = (EffectiveDateTime - Now).TotalHours > rule.CancelHours
```

Uses `RosterSeniorityMoveRule.CancelHours` � not previously documented as a configurable rule property.

---

## GAP 118: DailyAssignmentShift.IsHoliday Opens Own DbContext (Missing from Part 4)

6 computed properties on `DailyAssignmentShift` open their own `StrategicApplicationsContext`:
- `IsHoliday`
- `FirstOnDutyStartTime`, `FirstCallingStartTime`, `FirstCallingEndTime`
- `LastOnDutyStartTime`, `LastCallingStartTime`, `LastCallingEndTime`

---

## GAP 119: Shift Completion Logic (Missing from Part 4)

`CompleteDailyAssignmentShift()` � auto-completes a shift only if ALL positions are resolved:

```
for each DailyAssignment:
  for each DailyCrewPosition:
    if no on-duty records AND no annulment AND no DoNotFill ? RETURN (not complete)
    if any on-duty record has no off-duty record ? RETURN (not complete)
if all positions resolved ? create DailyAssignmentShiftCompletion record
```

---

## GAP 120: Pool 30 Clerical Holiday Hold-Down Release (Missing from Part 27)

During shift creation for Pool 30 (Clerical):
```
if shift date is a holiday:
  Release ALL open hold-downs for the pool
```

No other pool releases hold-downs on holidays.

---

## GAP 121: CheckDailyCrewPositionOnDutyMarkOffRecords � Pool 50 Excluded (Missing from Part 4)

```csharp
if (!this.RailroadPool.PoolNumber.Equals(50)) // Maintenance of Way excluded
```

Pool 50 (MOW) is excluded from the mark-off record checking pass during shift processing.

---

## GAP 122: DailyAssignment Default ST Hours = 8 (Missing from Part 4)

When `AssignmentOnDutyDay` is null for a given day:
```csharp
hours = 8;  // hard-coded default
```

The on-duty time falls back to `Assignment.AssignmentOnDutyTime.OnDutyTime` and board order to `Assignment.SetBoardOrder()`.

---

## GAP 123: CallingTimeEnd Midnight Handling (Missing from Part 4)

```csharp
if (calltime.Equals(new TimeSpan(0, 0, 0)))
    return this.AssignmentDate.AddDays(1).Add(calltime);
```

When `CallingTimeEnd` is midnight (`00:00:00`), it's treated as the next day.

---

## GAP 124: ChangeMarkUpRecord Deletes Mark-Up (Missing from Part 14)

When mark-up datetime is changed to `0001-01-01` or before mark-off time:
- Deletes the mark-up record entirely
- Sends Teams `"SystemMessage"`: "Mark up record for {name} was deleted by user {user}"

---

## GAP 125: Updated Mark-Off Code List (17 codes)

| Code | Description | New in this pass |
|---|---|---|
| `"SL"` | (Unknown � triggers FlagCode change) | Yes |
| `"NN"` | Not Notified | Previously documented |
| `"UA"` | Unavailable | Previously documented |
| All others | See Gap 85 | � |
# Part 49: Gap Analysis � Compensation Hours, Vacation Weeks, Hangout, Bulletin, Unassign

Gaps 126-140 covering compensation time calculations, vacation week resolution, hangout record creation, bulletin timing, and position unassignment.

---

## GAP 126: Vacation Week Comp Hours (Missing from Part 28)

Hard-coded ST hours for vacation weeks:

| Code | ST Hours | Max Days | Vacation Days |
|---|---|---|---|
| `"V1"` | 40 | 7 | 5 |
| `"V2"` | 80 | 14 | 10 |
| `"V3"` | 120 | 21 | 15 |
| `"V4"` | 160 | 28 | 20 |
| `"V5"` | 200 | 35 | 25 |

Compensation account type: `"VW"` for all vacation weeks.

---

## GAP 127: Compensation Account Types (Missing from Part 28)

| MOCode | CompensationType | Description |
|---|---|---|
| `"CD"` | `"CD"` | Compensated Day |
| `"PD"` | `"PD"` | Personal Day |
| `"SD"` | `"SD"` | Sick Day |
| `"VD"` | `"VD"` | Vacation Day |
| `"V1"`-`"V5"` | `"VW"` | Vacation Week |

Hours are capped at remaining balance. Creates `CompensationTimeAccountWithdrawl` on mark-off, `CompensationTimeAccountEntry(Debit)` on delete/change.

---

## GAP 128: Day-Type Hour Calculation Rules (Missing from Part 28)

| Calc Method | Default Hours | Position Type | Pool Exception |
|---|---|---|---|
| `CalculateCompensatedDayHours()` | 8 | Always 8 | None |
| `CalculatePersonalDayHours()` | 8 | Crew: ST from assignment | Yardmasters always 8 |
| `CalculateSickDayHours()` | 8 | Crew: ST from assignment | None |
| `CalculateVacationDayHours()` | 8 | Crew: ST from assignment | XB Yardmasters: 12 if all jobs are 12h |
| `CalculateVacationWeekHours()` | 8 | Crew: ST from assignment | None |

All skip off-days when finding the matching crew assignment.

---

## GAP 129: Vacation Week Daily Code Resolution (Missing from Part 14)

`GetVacationWeekMarkOffCode()` determines per-day code within a vacation week:

1. If off-day ? code `"VO"` (Vacation Off-day)
2. If comp hours not yet exceeded ? keep original code (V1-V5)
3. If vacation days used but total days not exceeded ? code `"VO"`
4. If all days exceeded ? code `"EV"` (Excess Vacation)

New mark-off codes found: `"VO"` (Vacation Off-day), `"EV"` (Excess Vacation).

---

## GAP 130: Hangout Record "HO" and "HN" Codes (Missing from Part 21)

`CreateDailyRosterBoardPositionHangoutRecord()`:

- Before notification confirmed: code `"HO"` (Hangout)
- After notification confirmed: code `"HN"` (Hangout Notified)
- `"HN"` duration uses `MarkOffMarkUpHours / 24` to calculate days
- If record already exists as `"HO"`, changes to `"HN"` after confirmation

New mark-off codes: `"HO"`, `"HN"`.

---

## GAP 131: Hangout Record Removal � 30 Min / 24 Hr Rules (Missing from Part 21)

`RemoveDailyRosterBoardPositionHangoutRecords()`:

- If unassigned within 30 minutes of notification ? remove same-day hangout records
- If unassigned within 24 hours of notification ? remove future hangout records
- Both use notification confirmation datetime as reference

---

## GAP 132: Bulletin Timing Formula (Missing from Part 15)

`CreateRailroadPositionBulletin()`:

```
OpenDateTime  = date + BulletinRule.BulletinStartTime
CloseDateTime = date + BulletinHours + BulletinCloseTime
EffectiveDateTime = date + EffectiveDay + BulletinHours + BulletinEffectiveTime

if current time > BulletinStartTime ? shift all dates +1 day
```

Commented-out code for Pool 40 (Mechanical) weekend adjustment � was disabled 5/29/2020.

---

## GAP 133: Unassign Flow (Missing from Part 3d)

`RailroadPosition.Unassign()`:

1. If future date ? force to midnight+1min of that date
2. Create `RailroadPositionChange` (notify=false)
3. Create `RailroadPoolEmployeePositionHistory` record (preserves assignment type/date)
4. Complete open notifications
5. If crew position ? remove future on-duty records (respects cutoff time)
6. If board position ? remove XB position + mark-off + assignment records
7. If hangout ? remove hangout records (30min/24hr rules)
8. Remove `RailroadPoolEmployeePosition`
9. If hangout ? set hangout timers

---

## GAP 134: XB Position Assignment TieUpOrder (Missing from Part 19)

`AssignDailyExtraBoardPosition()`:
```csharp
xbpos.TieUpOrder = Convert.ToInt64(adate.ToString("yyyyMMddHHmm"));
```

Also captures snapshot: `Status`, `TwentyFourHourRestDateTime`, `ConsecutiveDays`, `DaysWorked`, `RosterBoardPositionName`.

---

## GAP 135: CutOff Time for On-Duty Assignment (Missing from Part 4)

`GetCutOffDateTime()` � uses `Assignment.GetCutOffTime(dayName, craftControlNumber)` to determine a per-day, per-craft cutoff time. On-duty records are only created if current time is before the cutoff.

---

## GAP 136: Mark-Off Delete Protection (Missing from Part 14)

```
if any DailyCrewPositionOnDutyMarkOffRecord exists
  AND on-duty record's DailyCrewPosition has >1 on-duty records:
    throw "Vacancies have been filled. Cannot delete."
```

Prevents deleting mark-offs that caused vacancy fills.

---

## GAP 137: Interface File OriginalRecord Format (Missing from Part 38)

`CreateOriginalMarkOffRecord()` stores a snapshot in `MarkOffCopy` struct:
- `empno`: 4-char employee number
- `code`: 2-char mark-off code  
- `moday`/`modate`/`motime`: Day abbreviated (3 chars), `MMddyy`, `HHmm`
- `mouser`: `"AUTO"` for autoprocess, else user initials uppercase
- `muday`/`mudate`/`mutime`/`muuser`: Same format for mark-up
- `jobno`: `RailroadPosition.ShortCrewPositionName`

Used for interface file generation.

---

## GAP 138: Seniority Move Oldest Priority (Missing from Part 15)

`GetOldestSeniorityMove(effdate)`:
```
Order by: EffectiveDateTime ASC
  then by: BoardOrCrewName_PositionName ASC
  then by: Active Seniority RosterDate ASC
  then by: Active Seniority Rank ASC
```

Determines which move to process first when multiple moves target the same position.

---

## GAP 139: Complete Mark-Off Code List � 21 Codes

| Code | Description | New |
|---|---|---|
| `"HO"` | Hangout | Yes |
| `"HN"` | Hangout Notified | Yes |
| `"VO"` | Vacation Off-day | Yes |
| `"EV"` | Excess Vacation | Yes |
| `"SL"` | (Unknown � FlagCode trigger) | Part 48 |
| All others | See Gap 85, Gap 125 | � |

---

## GAP 140: XB Yardmaster 12-Hour Vacation Day (Missing from Part 28)

```csharp
if (pool.PoolName.Equals("Yardmasters"))
{
    var twelvehourday = db.AssignmentOnDutyDays
        .Where(d => d.CrewAssignment.Crew.RailroadPoolControlNumber.Equals(pool.ControlNumber)
            && d.WeekDay.WeekDayName.Equals(oddate.DayOfWeek.ToString()))
        .All(d => d.StraightTimeHours.Equals(12));

    if (twelvehourday) hours = 12;
}
```

XB Yardmasters get 12 vacation day hours only if ALL yardmaster assignments on that day of week are 12-hour shifts.
# Part 52: Gap Analysis � Extra Board, Overtime Board, LoseGuarantee, Board Ordering

Gaps 169-182 covering extra board position ordering, overtime board position types, LoseGuarantee logic, and board creation.

---

## GAP 169: ExtraBoard Type Values (Missing from Part 19)

`RosterBoard.ExtraBoard` integer values:

| Value | Type | Description |
|---|---|---|
| 0 | Not an extra board | Regular roster board |
| 1 | First-In-First-Out (FIFO) | Board ordering by tie-up time |
| 2 | Rotating | Board rotates through employees |

---

## GAP 170: LoseGuarantee � Mark-Off Day Limits (Missing from Part 19)

`DailyShiftExtraBoardPosition.LoseGuarantee()` determines if an XB employee loses their daily guarantee pay:

| MOCode | Max Guaranteed Days | Beyond Limit |
|---|---|---|
| `"VD"` (Vacation Day) | 1 | No guarantee |
| `"PD"` (Personal Day) | 1 | No guarantee |
| `"SR"` (Safety Rest) | 2 | No guarantee |
| `"V1"` | 5 | No guarantee |
| `"V2"` | 10 | No guarantee |
| `"V3"` | 15 | No guarantee |
| `"V4"` | 20 | No guarantee |
| `"V5"` | 25 | No guarantee |
| All others | No limit | Every 3rd day loses guarantee |

Formula for "every 3rd": `(count % 3) == 1`

Guarantee is also restored if mark-up occurs before `LastCallingEndTime`.

---

## GAP 171: TieUpOrder Encoding/Decoding (Missing from Part 19)

TieUpOrder is a `long` stored as `yyyyMMddHHmm` format:

```csharp
// Encode
tieuporder = Convert.ToInt64(dateTime.ToString("yyyyMMddHHmm"));

// Decode
GetTieUpOrderDateTime(long) ? parses back: "{MM}/{dd}/{yyyy} {HH}:{mm}:00"
```

Mark-off offset: `markOffDateTime.AddYears(10)` � pushes marked-off employees to end of board.
Future TieUpOrder (>5 years ahead) is capped at current time with `BoardOrder = 9999`.

---

## GAP 172: XB Position Availability Check (Missing from Part 19)

`IsAvailableAtEndCallTime(endcalltime)`:
```
NOT available if:
  - On duty or called
  - endcalltime < RestedDateTime
  - endcalltime < AvailableDateTime
  - endcalltime < mark-off MarkUpDateTime
```

`IsAvailable` (no time check):
```
NOT markedOff AND NOT employeeAssigned AND NOT onDuty
```

---

## GAP 173: Overtime Board Position Types (Missing from Part 39)

| PostionType | BoardOrder Range | Description |
|---|---|---|
| `"OT"` | 1000+ | Regular overtime (CallForOvertime employees, non-XB) |
| `"CB"` | 2000+ | Cutback positions (Pool 20 Yardmasters only) |
| `"MO"` | 9000+ | Marked-off/moved positions |

Initial creation: OT positions from seniority list, CB from cutback list.
Subsequent shifts: copy from previous board with order preserved.

---

## GAP 174: OT Board � Pool 20 Cutback Refresh (Missing from Part 39)

Pool 20 (Yardmasters) cutback positions are NOT copied from previous board. They are regenerated from `GetCutBackRosterSeniorityList()` each shift. All other position types are copied.

---

## GAP 175: OT Board � Future BoardDateTime Positions (Missing from Part 39)

When copying OT positions from previous board:
```
if position.BoardDateTime > now:
  newPosition.BoardOrder = boardorder + 4000  // pushed to back
  newPosition.BoardDateTime = position.BoardDateTime  // keep future date
else:
  newPosition.BoardOrder = boardorder  // normal order
  newPosition.BoardDateTime = now
```

BoardOrder +4000 ensures future-dated positions sort after current ones.

---

## GAP 176: SetOvertimePositionBoardOrder � Pool 40 Skip Rules (Missing from Part 39)

When reordering OT board after a vacancy fill:

Pool 40 (Mechanical) skips are:
1. Non-emergency call-out: entire reorder is skipped
2. Currently on duty AND not enough rest for next shift: skip
3. PreviousRestHours == 0 AND off-duty equals vacancy start: skip (worked 16h)

All pools skip if:
- No last on-duty record
- Employee working same start time as vacancy

---

## GAP 177: XB Board BoardOrder Increment = 10 (Missing from Part 19)

```csharp
boardorder += 10;  // XB board
boardorder++;      // OT board initial
boardorder += 10;  // OT board subsequent
```

XB board uses 10-step increments allowing insertions. OT initial uses 1-step (no room for insertion), subsequent uses 10-step.

---

## GAP 178: XB Board Creation � First vs Subsequent (Missing from Part 19)

**First board for a roster board** (no previous board exists):
- Positions from `GetAvailableExtraBoardPositions()`
- BoardOrder/TieUpOrder start at 0
- Each position checks for existing mark-offs

**Subsequent boards** (previous board exists):
- Copy positions from last uncompleted board
- Preserve TieUpOrder, recalculate BoardOrder at 10-step
- Copy any existing `DailyShiftExtraBoardPositionAssignment` for called employees

---

## GAP 179: SetTieUpOrder Bottom vs Non-Bottom (Missing from Part 19)

`SetTieUpOrder(db, tieuporder, user, now, bottom)`:

**bottom = true**: Place after last position with same or lower tie-up order:
```
BoardOrder = lastPosition.BoardOrder + 1
```

**bottom = false**: Push to back by adding 9000:
```
if BoardOrder < 9000: BoardOrder += 9000
```

---

## GAP 180: DailyExtraBoardMarkOffRecord Fields (Missing from Part 19)

| Field | Description |
|---|---|
| `MarkOffCode` | String copy of mark-off code |
| `ProjectedAssignment` | String: assignment name + position, or "Not projected to work" |
| `TieUpOrder` | Snapshot of board order at mark-off time |
| `BoardOrder` | Snapshot of board order at mark-off time |
| `LoseGuarantee` | Whether employee loses guarantee (see Gap 170) |

---

## GAP 181: Craft.RequiredRestHours (Missing from Part 3c)

Used in Pool 40 (Mechanical) OT board skip logic:
```csharp
var reqrest = new TimeSpan(this.Roster.Craft.RequiredRestHours, 0, 0);
```

Craft-specific required rest between assignments (different from FRA rest which is 10h).

---

## GAP 182: XB Position Snapshot Fields (Missing from Part 19)

Each XB position stores a snapshot updated on status change:

| Field | Source |
|---|---|
| `Status` | `RailroadPoolEmployee.Status` |
| `TwentyFourHourRestDateTime` | `RailroadPoolEmployee.TwentyFourHourRestDateTimeString` |
| `ConsecutiveDays` | `RailroadPoolEmployee.ConsecutiveDays` |
| `DaysWorked` | `RailroadPoolEmployee.GetSTDaysWorked(assignmentDate)` |
| `RosterBoardPositionName` | `CurrentPosition.PositionName` |
# Part 55: Gap Analysis � TieUp Controller, GetJobPaidCode, Meal Periods, Clerical Pay Grades

Gaps 214-228 covering the master GetJobPaidCode resolution, tie-up routing, meal period rules, off-property tie-up, and clerical/mechanical pay grade hierarchies.

---

## GAP 214: GetJobPaidCode � Master Job Paid Resolution (Missing from Part 16)

`DailyOnDutyRecordTieUpController.GetJobPaidCode()` � 6th location of pool-specific logic, and the **definitive** version used at tie-up time.

### Per-Pool Logic

| Pool | Default | Override Logic |
|---|---|---|
| 10 (Y&E) | `LastActivePosition.DefaultJobPaid` | If Yardman: use `DailyCrewPosition.PayrollCode`; if not assigned: replace first 3 chars with `"100"` |
| 20 (YM) | `DailyCrewPosition.PayrollCode` | No override |
| 30 (Clerical) | `DailyCrewPosition.PayrollCode` | If same shift, unassigned employee, and assigned position has higher pay grade ? use assigned position's payroll code |
| 40 (Mech) | `DailyCrewPosition.PayrollCode` | If same shift, unassigned employee, and assigned position has higher mechanical code ? use assigned position's payroll code |
| 50 (MOW) | `DailyCrewPosition.PayrollCode` | If assigned position has higher ST pay rate ? use assigned position's payroll code |
| 60 (Patrol) | `DailyCrewPosition.PayrollCode` | No override |

---

## GAP 215: Clerical Pay Grade Hierarchy (Missing from Part 16)

Hard-coded dictionary in `DailyOnDutyRecordTieUpController`:

| PayrollCode | Grade (higher = higher pay) |
|---|---|
| `"102"` | 4 |
| `"116"` | 4 |
| `"123"` | 4 |
| `"170"` | 5 |
| `"135"` | 6 |
| `"150"` | 6 |
| `"104"` | 7 |
| `"100"` | 8 |
| `"112"` | 8 |
| `"130"` | 8 |
| `"199"` | 8 |

Employee gets the HIGHER of: vacancy pay grade or assigned position pay grade (same shift only).

---

## GAP 216: Mechanical Position Code Hierarchy (Missing from Part 16)

Hard-coded enum in `DailyOnDutyRecordTieUpController`:

```csharp
private enum MechanicalCodes { Y, T, L, S };
```

Ordinal values: Y=0, T=1, L=2, S=3. Higher ordinal = higher pay.
Employee gets the HIGHER of: vacancy position code or current position code (same shift only).

---

## GAP 217: Pool 10 Yardman "100" vs "101" Rule (Missing from Part 16)

```csharp
if CraftName == "Yardman":
    jobpaid = DailyCrewPosition.PayrollCode  // "101F" or "101H"
    if NOT assigned:
        jobpaid = replace first 3 chars with "100"  // "100F" or "100H"
```

`"101"` = assigned position rate, `"100"` = non-assigned position rate.

---

## GAP 218: TieUp Process Routing (Missing from Part 34)

`TieUpProcess()` determines which screens to show based on pool:

| Pool | Routing |
|---|---|
| 10 (Y&E) | If not updated ? ChangeArrival; if restricted ? Create; if Engineer ? Locomotive; else ? Payroll |
| 20 (YM) | If has trainees ? Payroll; else ? Create |
| 30 (Clerical) | If has trainees ? Payroll; else ? Create |
| 40 (Mech) | If location contains "Rip" ? Payroll; else ? Create |
| 50 (MOW) | Always ? MofWBilling first |
| Default | ? Create |

---

## GAP 219: Engineer Locomotive Weight ? JobPaid (Missing from Part 16)

```csharp
total = sum of all LocomotiveWeight records
jobpaid = EngineerJobCodes.OrderBy(MaxWeightOnDrivers)
    .First(c => MaxWeightOnDrivers >= total)
if trainee: use TraineePayClassCode
else: use PayClassCode
```

Engineer job code determined by total locomotive weight.

---

## GAP 220: Meal Period Rules (Missing from Part 34)

### First Meal Period
- Starts at OnDuty + 4:30 (Pool 10)
- MOW: Configurable, default 0 or 30 minutes claimed

### Second Meal Period
- Only available if TimeOnDuty > 9:19
- Starts at FirstMeal + 20 minutes + 4:30
- Pool 40 (Mechanical): No second meal, no air pay
- All other pools (non-10, non-40): No meal periods or air pay

### Meal Period Claims
- 0 = not claimed
- 30 = claimed but not taken (30 min penalty for MOW)
- 31 = second meal not applicable

---

## GAP 221: Off-Property Tie-Up Detection (Missing from Part 34)

```csharp
var onproperty = ApplicationUtilities.CheckOnPropertyIPAddress(Request.UserHostAddress);
if (!onproperty):
    Create OffPropertyTieUpRecord in SAClassLibrary database
    Text: "{name} tied up off property @ DateTime {now}"
```

Uses IP address to detect off-property tie-ups. Records stored in separate `SAClassLibraryContext`.

---

## GAP 222: Max Off-Duty DateTime � Non-Pool 10 Cap (Missing from Part 34)

```csharp
if NOT Pool 10:
    maxdatetime = DateTime.Now.AddMinutes(30)
```

Pool 10 (Y&E) uses the FRA maximum. All other pools cap off-duty time at 30 minutes from now.

---

## GAP 223: Notes Required Rules (Missing from Part 34)

| Pool | Notes Required When |
|---|---|
| 10 (Y&E) | Any completed FRA records exist |
| 40 (Mech) | Time on duty > ST + meal AND earning code "01" |
| 50 (MOW) | Time on duty > ST + meal AND earning code "01"; if OT: ST hours set to 0:01 |

### Reason Required
All pools: if total time < straight time hours.

---

## GAP 224: MOW Overtime � 1 Minute ST (Missing from Part 34)

```csharp
case 50: // Maintenance of Way
    if (ondutyrec.IsOvertime)
        sthours = new TimeSpan(0, 1, 0);  // 1 minute
```

MOW overtime records have a 1-minute effective ST hours � ensures "needs reason" is always false for OT.

---

## GAP 225: Payroll Info Applies to All Non-CR Records (Missing from Part 34)

```csharp
if (record.EmployeeCalledRelief):
    CreatePayrollInformation for this record only
else:
    CreatePayrollInformation for ALL non-CalledRelief on-duty records on the position
```

Payroll information is shared across all on-duty records for the same position unless CalledRelief.

---

## GAP 226: Clerical PayrollCode ? Pay Grade Comparison (Missing from Part 16)

Clerical "pay up" logic:
```
if assigned position is crew AND not assigned employee AND same shift:
    Get vacancy payroll code grade from clericalpaygrades dict
    Get assigned position payroll code grade from dict
    if assigned grade > vacancy grade ? use assigned position's payroll code
```

Only applies when employee is filling a DIFFERENT position on the SAME shift.

---

## GAP 227: MOW "Pay Up" by Rate (Missing from Part 16)

```csharp
if (position.Position.CurrentSTPayRate > record.DailyCrewPosition.Position.CurrentSTPayRate)
    jobpaid = position.CrewPosition.PayrollCode;
```

MOW uses actual `CurrentSTPayRate` comparison (not grade lookups). New property: `Position.CurrentSTPayRate`.

---

## GAP 228: Duplicated GetJobPaidCode � 5th Copy of Pool Logic

| Location | Type | Used When |
|---|---|---|
| `DailyOnDutyRecordTieUpController.GetJobPaidCode()` | Static method | Tie-up time (definitive) |
| `RailroadPoolEmployee.DefaultJobPaid` | Property | Default/fallback |
| `RailroadPosition.DefaultJobPaid` | Property | Default/fallback |
| `DailyCrewPosition.DefaultJobPaid` | Property | Default/fallback |
| `DailyCrewPosition.PayrollCode` | Property | Base code before override |
# Part 56: Gap Analysis � FillVacancy Flow, Board Selection, Arrival Adjustment, FRA Consecutive Days

Gaps 229-245 covering vacancy board types, FillVacancy complete flow, XB tie-up ordering, Yardmaster auto mark-off, and arrival time adjustment.

---

## GAP 229: Vacancy Board Types (Missing from Part 19)

`FillVacancyController.Select()` routes to 7 board types:

| Board | Source | Description |
|---|---|---|
| 0 | Same Assignment | Employees already on-duty on the same assignment |
| 1 | Extra Board | Available XB employees (from daily XB) |
| 2 | Off-Day Board | Seniority-ordered off-day employees |
| 4 | Overtime Board | OT board positions for the shift |
| 5 | Vacation Relief Board | Employees on vacation relief crew positions |
| 6 | Qualified Employee Board | Employees qualified for the specific position |
| default | Seniority Board | All available employees in seniority order |

Board 0 filters: must have on-duty records, be qualified for position, and not marked off.

---

## GAP 230: FillVacancy Complete Flow (Missing from Part 19)

`DailyCrewPosition.FillVacancy()` full algorithm:

### Phase 1: Determine existing on-duty record
```
Get XB position for this employee on this shift
If DoNotFill ? remove DoNotFill records
Get existing on-duty record for employee (pool-specific lookup)
```

### Phase 2A: No existing record (or tied up)
```
Create new on-duty record
If XB position exists AND not on hold-down:
  Save board/tieup order snapshot
  Calculate new tieup order:
    - FIFO board: AssignmentOffDutyDateTime
    - Rotating board: DateTime.Now
  Compare with prior positions to handle late calls
  If NOT (Pool 30 Clerical + overtime) ? SetTieUpOrder
If Pool 20 (Yardmaster):
  Create mark-off record (onduty - 89 minutes)
  Create mark-up record (offduty time)
```

### Phase 2B: Existing record found (not tied up)
```
If movedrec ? create MovedDailyCrewPosition record
If Pool 10 AND on-duty AND same assignment ? position move:
  Create off-duty for old position
  Create payroll record for old segment
  Flag PayrollReviewRequired
  Transfer rest hours
Else ? update on-duty record to new position
```

### Phase 3: XB assignment tracking
```
Create DailyShiftExtraBoardPositionAssignment (snapshot of board/tieup order)
Set AtHoc message timer
```

---

## GAP 231: Pool 30 Clerical XB � No Rotation on OT (Missing from Part 19)

```csharp
if (Position.Roster.Craft.RailroadPool.PoolNumber.Equals(30) && ondutyrec.PayrollEarningCode.Overtime)
    setorder = false;
```

Clerical XB employees don't rotate to the bottom of the board when working overtime.

---

## GAP 232: Yardmaster Auto Mark-Off on Vacancy Fill (Missing from Part 14)

```csharp
case 20: // Yardmaster
    markoff = CreateYardmasterMarkoffRecord(db, position, ondutyDateTime - 89 minutes)
    if NOT on hold-down:
        CreateYardmasterMarkupRecord(db, offdutyDateTime, markoff, user)
```

Yardmasters get an automatic mark-off 89 minutes before on-duty time when filling a vacancy.

---

## GAP 233: GetDailyCrewPositionOnDutyRecord � Pool 10 Next-Shift Lookup (Missing from Part 19)

```csharp
case 10: // Y&E
    Look for records on same day
    If none found ? look for records on next day + next shift
default:
    Look for records on same shift only
```

Pool 10 uses `Shift.NextShiftID` for cross-shift employee tracking.

---

## GAP 234: Late Call Handling (Missing from Part 19)

When `latecall = true`:
```
Create DailyCrewPositionOnDutyRecordLateCall:
  - LateCallDateTime = now
  - ArrivalDateTime = scheduledOnDutyDateTime
  - Confirmed = false
  - Notes from user
```

`AcceptCall()` checks: `DateTime.Now > EndCallTime && board != 0` ? late call.

---

## GAP 235: ChangeArrival � Consecutive Days Recalculation (Missing from Part 7)

```csharp
difference = originalOnDuty - newArrivalTime
newPrevRest = prevRest - difference

if prevRest >= 24:00 OR no last record:
    ConsecutiveDays = 1  // rest breaks the chain
else:
    ConsecutiveDays = lastRecord.ConsecutiveDays + 1
```

Per FRA: 24:00 hours and greater breaks consecutive days (per Carl Matejka's FRA conversation).

---

## GAP 236: Force Assign (Missing from Part 19)

`ForceAssign` bypasses normal board order. Sets `viewModel.ForceAssign = true`, which is passed to `SetOvertimePositionBoardOrder()` as the `force` parameter.

---

## GAP 237: FillVacancy Log (Missing from Part 19)

```csharp
db.FillVacancyLog.Add(FillVacancyLog.CreateInstance(vacancy, rpemployee, startTime, endTime, user));
```

Tracks every vacancy fill with start/end times for performance monitoring.

---

## GAP 238: Pool 10 On-Duty Position Move (Missing from Part 19)

When employee is moved on the same assignment while on duty:
```
1. Create off-duty record for current position
2. Create payroll record for the worked segment
3. Flag for PayrollReview with reason text
4. Create new on-duty record on new position
5. Transfer PreviousRestHours/Minutes to new record
```

---

## GAP 239: DoNotFill Cleanup (Missing from Part 4)

```csharp
RemoveDoNotFillRecords():
  foreach on-duty record on the position:
    Remove all payroll records
    Remove DailyCrewPositionOnDutyPayrollRecords
  Remove DailyCrewPositionDoNotFill record
```

Clears all auto-generated payroll before reassigning.

---

## GAP 240: Hold-Down Position Chain Fill (Missing from Part 27)

`AssignHoldDownPositions()` � recursive:
```
For each hold-down on position (by different employee):
  If not released (or release date > assignment date):
    Fill the position with hold-down employee
  If hold-down employee has assigned position:
    Recursively fill hold-down positions on THAT position
```

---

## GAP 241: ChangeEmployee � Did Not Work Record (Missing from Part 4)

```csharp
ChangeEmployee():
  Create new on-duty record for replacement
  Create DailyOnDutyDidNotWorkRecord for original employee
```

New entity: `DailyOnDutyDidNotWorkRecord`.

---

## GAP 242: Auto-Pay Crew Position Records (Missing from Part 16)

```csharp
if (this.RailroadPosition.IsCrewPosition && !processed)
    foreach autopayrec in CrewPosition.PayrollCrewPositionAutoPayRecords:
        autopayrec.CreateAutomaticPayrollRecord(db, record, user, now)
```

`PayrollCrewPositionAutoPayRecord` � employees paid what the crew position makes without working. Processed once per position tie-up.

---

## GAP 243: XB Rotating Board TieUpOrder = Now (Missing from Part 19)

```csharp
if (xbposition.DailyShiftExtraBoard.IsRotatingBoard)
    newtieuporder = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmm"));
else:
    newtieuporder = Convert.ToInt64(AssignmentOffDutyDateTime.ToString("yyyyMMddHHmm"));
```

FIFO boards use off-duty time; rotating boards use current time.

---

## GAP 244: Completed XB Board Forward Update (Missing from Part 19)

```csharp
if (xbposition.DailyShiftExtraBoard.Completed):
    Find all open XB positions for employee
    Set tieup order on those positions too
```

When the current XB board is already completed, the order update propagates to the next active board.

---

## GAP 245: Pool 10 On-Duty Time Preservation (Missing from Part 19)

```csharp
case 10: // Y&E
    if same craft AND new position starts later ? use original on-duty time
```

Pool 10 preserves the earlier on-duty time when moving within the same craft on the same shift.
