# Spec_03: Payroll, Earnings, Rates, and Compensation
# Part 8: Payroll Approval Routing

## Overview

The payroll approval system routes payroll earning records to the appropriate approval officer based on pool number, earning code, and position hierarchy. Located primarily in `PayrollUtilities.cs` and `PayrollEarningRecord.cs`.

## Approval Trigger

In `PayrollEarningRecord.CreateEarningsApprovalRequiredRecord(db, user, now, approvalctrlnbr=0)`:

1. Resolve the current user's `Employee.ControlNumber` (or `999999` if `"autoprocess"`)
2. Load `PayrollRecord` and `PayrollCode` if not already loaded
3. **Check if approval is needed**: `Code.ApprovalRequired == true` OR `approvalctrlnbr != 0`
4. **Skip if self-approving**: If current user IS the approval officer ? skip
5. **Resolve officer**: If `approvalctrlnbr == 0` ? `PayrollUtilities.GetApprovalOfficer(db, PayrollRecord, Code)`
6. Create or update `EarningsApprovalRequiredRecord` with the resolved officer

## Officer Resolution � `GetApprovalOfficer(db, payrec, earncode)`

Three-tier cascading resolution:

### Step 1: Default Officer
`GetDefaultApprovalOfficer(db)` � finds the first user in the role named `"Payroll Approval"` via role manager.

### Step 2: Route Decision � `ApproveWithPayrollCodeOfficer(poolnbr, earncode)`

Determines whether to use the payroll-code-specific officer or the position-based officer:

| Earning Code | Code Description | Pool Restriction | Uses PayrollCode Officer |
|---|---|---|---|
| `"04"` | Vacation Week | Pool 10, 20 only | Yes |
| `"06"` | Vacation Day | Pool 10, 20 only | Yes |
| `"12"` | Personal Day | Pool 10, 20 only | Yes |
| `"41"` | Trainer Pay | Pool 10, 20 only | Yes |
| `"10"` | Jury Duty | All pools | Yes |
| `"11"` | Bereavement | All pools | Yes |
| `"21"` | Time Claim | Pool 10 only | Yes |
| `"43"` | Job Trainee | Pool 10 only | Yes |
| `"44"` | Other/Claims Payment | All pools | Yes |
| `"45"` | Safety Day | All pools (if `CompensationType` is null/empty) | Yes |
| All others | � | � | No ? use position officer |

### Step 3a: PayrollCode Officer � `GetApprovalOfficer(db, defaultOfficer, earncode)`

1. Find `PayrollCodeApprovalRole` where `PayrollCodeControlNumber == earncode` AND `Primary == true`
2. Get users with that role via `CollectionLists.GetPrimaryRoleUsers(db, roleId)` � these are users whose `ApplicationUser.PrimaryRoleID` matches
3. If no primary users ? fall back to `GetRoleUsers(db, roleId)` (all users in that role)
4. For each user:
   - Resolve `Employee.ControlNumber` from `EmployeeNumber`
   - If the officer matches the default officer ? return immediately (prefer default)
5. If no match with default ? return the first role user's employee control number

### Step 3b: Position Officer (when NOT using PayrollCode officer)

1. If `PayrollRecord` has no `DailyCrewPositionOnDutyPayrollRecords`:
   - Use `payrec.RailroadPoolEmployee.ApprovalOfficer` (cascading position ? craft resolution)
2. If it does have on-duty payroll records:
   - Find the last non-deleted record: `ondutyrec.DailyCrewPositionOnDutyRecord.DailyCrewPosition.Position.ApprovalOfficer`
   - This resolves through `PositionAlternateSupervisor` ? `Craft.ApprovalOfficer`

## Approval Entities

### EarningsApprovalRequiredRecord

**PK = FK** to `PayrollEarningRecord` (1:1). Does NOT inherit ControlNumberBase.

| Property | Type | Description |
|---|---|---|
| `PayrollEarningRecordControlNumber` | `long` | PK/FK |
| `ApprovalEmployeeControlNumber` | `long` | FK to Employee � the approving officer |
| `CreatedBy` | `string` | |
| `ModifiedBy` | `string` | |
| `CreatedDate` | `DateTime` | |
| `ModifiedDate` | `DateTime` | |

**Computed Properties**:

| Property | Logic |
|---|---|
| `IsCompleted` | `EarningsApprovalRecord != null OR EarningsDeclanationRecord != null` |
| `IsApproved` | `EarningsApprovalRecord != null` |
| `IsDeclined` | `EarningsDeclanationRecord != null` |

**Navigation**: `PayrollEarningRecord`, `EarningsApprovalRecord` (1:1), `EarningsDeclanationRecord` (1:1), `Employee`

### EarningsApprovalRecord

**PK = FK** to `EarningsApprovalRequiredRecord` (1:1). Does NOT inherit ControlNumberBase.

| Property | Type | Description |
|---|---|---|
| `PayrollEarningRecordControlNumber` | `long` | PK/FK |
| `Notes` | `string` | Optional approval notes |
| `CreatedBy` | `string` | Who approved |
| `CreatedDate` | `DateTime` | When approved |

### EarningsDeclanationRecord

**PK = FK** to `EarningsApprovalRequiredRecord` (1:1). Same structure as approval but represents a decline.

### EarningsApprovalEmployee

Links employees to approval responsibilities.

### PayrollCodeApprovalRole

**Inherits**: `ControlNumberBase`

| Property | Type | Description |
|---|---|---|
| `PayrollCodeControlNumber` | `long` | FK to PayrollCode |
| `RoleId` | `Guid` | ASP.NET Identity role GUID |
| `RoleName` | `string` | Display name |
| `Primary` | `bool` | Whether this is the primary approval role for the code |

## Payroll Review System

Separate from earnings approval, there is also a review system:

### PayrollReviewRequiredRecord
- Created when payroll records need manual review before processing
- Similar PK=FK pattern to `PayrollEarningRecord`

### PayrollReviewRecord
- 1:1 with `PayrollReviewRequiredRecord` � represents completion of review

## Key Business Rules

1. **Self-approval prevention**: If the current user IS the resolved approval officer, no approval record is created
2. **"autoprocess" user**: Gets `empctrlnbr = 999999` � never matches any real officer, so approval is always required for auto-generated records
3. **Primary role preference**: `GetPrimaryRoleUsers` checks `ApplicationUser.PrimaryRoleID` first, falling back to all role members
4. **Default officer fallback**: If no specific officer can be resolved, the default "Payroll Approval" role user is used
5. **Position-specific routing**: On-duty payroll records route to the position's approval officer (which may be an alternate supervisor), not the craft-level officer
# Part 16: Payroll Record Processing

## Overview

The payroll system generates, calculates, approves, reviews, and exports earning records. It centers on `PayrollRecord` (the header) and `PayrollEarningRecord` (the line items), with supporting entities for approval, review, and export.

## PayrollRecord

**Inherits**: `ControlNumberBase`

One per employee per work event. Header record containing employee/position context.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `EmployeeControlNumber` | `long` | FK to Employee |
| `RailroadEmployeeControlNumber` | `long` | FK to RailroadEmployee |
| `RailroadPoolEmployeeControlNumber` | `long` | FK to RailroadPoolEmployee |
| `CraftControlNumber` | `long` | FK to Craft |
| `WorkNumber` | `string` | Employee number |
| `Batch` | `string` | `[StringLength(6)]` � payroll batch identifier |
| `PayrollDate` | `DateTime` | Date the payroll record is for |
| `OnDutyDateTime` | `DateTime` | When employee went on duty |
| `OffDutyDateTime` | `DateTime` | When employee went off duty |
| `JobWorked` | `string` | `[StringLength(4)]` � job code worked |
| `JobPaid` | `string` | `[StringLength(4)]` � job code paid |
| `ManualEntry` | `bool` | Whether manually entered |
| `ICCNumber` | `string` | ICC department number |
| `DepartmentNumber` | `string` | Payroll department number |
| `GeneralLedgerNumber` | `string` | GL account number |
| `RatePercentage` | `int` | Pay rate percentage (for tiered employees) |

### Computed Properties

| Property | Logic |
|---|---|
| `OnDutyDateTimeString` | `OnDutyDateTime.ToString("MM/dd/yyyy hh:mm tt")` |
| `JobWorkedString` | `"{Assignment_ReliefName} {PositionName}"` from linked on-duty record; fallback: `JobWorked` |
| `STHours` | Sum of all earning records' ST hours (excluding Arbitrary codes) |
| `OTHours` | Sum of all earning records' OT hours (excluding Arbitrary codes) |
| `CalculatedEarnings` | Sum of all non-declined earning records' `CalculatedAmount` |
| `IsProcessed` | All earning records have been processed (final) |
| `IsReviewed` | No review required OR `PayrollReviewRecord` exists |
| `PayrollDepartment` | Cascading: on-duty record ? position record ? `RailroadPoolEmployee.PayrollDepartment` |

### Navigation Properties

| Property | Type |
|---|---|
| `Craft` | `Craft` |
| `Employee` | `Employee` |
| `RailroadEmployee` | `RailroadEmployee` |
| `RailroadPoolEmployee` | `RailroadPoolEmployee` |
| `PayrollRecordDelete` | `PayrollRecordDelete` (1:1, nullable � soft delete) |
| `PayrollRecordReviewRequired` | `PayrollReviewRequiredRecord` (1:1, nullable) |
| `PayrollEarningRecords` | `ICollection<PayrollEarningRecord>` |
| `DailyCrewPositionOnDutyPayrollRecords` | `ICollection<DailyCrewPositionOnDutyPayrollRecord>` |
| `DailyRailroadEmployeePositionPayrollRecords` | `ICollection<DailyRailroadEmployeePositionPayrollRecord>` |
| `DailyShiftExtraBoardPositionPayrollRecords` | `ICollection<DailyShiftExtraBoardPositionPayrollRecord>` |
| `PayrollHolidayRecordPayrollRecords` | `ICollection<PayrollHolidayRecordPayrollRecord>` |

### `CreatePayrollEarningRecord(db, earncode, st, ot, amount, user, edate)`

1. Create `PayrollEarningRecord` with code, ST/OT hours, amount
2. `CalculatePayrollAmounts()` � computes dollar amounts
3. Set `RecordCount = 1`, `Accumulator` from code
4. Save
5. If code has `CompensationType` ? check balance, remove unused requests if depleted
6. Returns the earning record

### `CalculatePayrollAmounts(db, earnrec)` � Rate Calculations

**40-hour overtime conversion** (Pools 20 Yardmaster, 30 Clerical):
- Extra board employees with code `"01"` or `"42"`: if `STHours + weekSTHours > 40` ? excess converts to OT

**ST rate calculation by payroll code**:

| Code | Rate Method |
|---|---|
| `"04"`, `"06"`, `"12"` | `GetCompensatedRate(STRate)` � vacation/personal day rate |
| `"13"` | `GetGuaranteeRate()` � guaranteed pay rate |
| Default | `GetStraightTimeRate()` |

**OT rate calculation**:
- Code `"20"` (Double Time): `STRate � 2 � OTHours`
- Default: `GetOvertimeRate() � OTHours`

**Rounding**: All amounts rounded UP to next penny (`Math.Ceiling(amount � 100) / 100`). Hours rounded up to next minute.

**Final**: `CalculatedAmount = STAmount + OTAmount + Amount`

### `GetCompensatedRate(strate)` � Pool-Specific

| Pool | Calculation |
|---|---|
| 10 (Yard & Enginemen) | Calls stored procedure to get vacation rate, divides by 8 |
| Default | `strate` (straight time rate) |

### `DebitCompensationAccount(db, earncode, hrs, paiddate)`

1. Get current balance: `RailroadEmployee.GetCompensationTimeAccountBalanceHours(type)`
2. If balance < requested hours ? cap at balance
3. If zero ? throw exception
4. Create withdrawal record with notes

### `CreatePayrollReviewRequiredRecord(db, reason, user, now)`

Creates `PayrollReviewRequiredRecord` if none exists. If already exists and reason contains `"The payroll record was changed"` ? appends reason.

### `CreatePayrollReviewRecord(db, notes, user, now)`

Only creates review if ALL earning approval records are completed. Creates `PayrollReviewRecord` + optional `ObjectNotes`.

---

## PayrollEarningRecord

**Inherits**: `ControlNumberBase`

Individual earning line item within a payroll record.

### Stored Properties

| Property | Type | Description |
|---|---|---|
| `PayrollRecordControlNumber` | `long` | FK to PayrollRecord |
| `PayrollCodeControlNumber` | `long` | FK to PayrollCode |
| `PayrollCode` | `string` | Denormalized code string |
| `STHours` | `TimeSpan` | Straight-time hours |
| `OTHours` | `TimeSpan` | Overtime hours |
| `STAmount` | `decimal` | Calculated ST dollar amount |
| `OTAmount` | `decimal` | Calculated OT dollar amount |
| `Amount` | `decimal` | Additional/arbitrary amount |
| `CalculatedAmount` | `decimal` | `STAmount + OTAmount + Amount` |
| `STPaid` | `decimal` | ST amount actually paid (from import) |
| `OTPaid` | `decimal` | OT amount actually paid |
| `PaidAmount` | `decimal` | Additional amount paid |
| `TotalPaid` | `decimal` | Total paid |
| `RecordCount` | `int` | Number of records aggregated |
| `Accumulator` | `bool` | Whether this code accumulates |

### Computed Properties

| Property | Logic |
|---|---|
| `IsProcessed` | If declined and not final-processed ? true. If `PayrollEarningProcessedRecord.FinalProcess` ? true |
| `ApprovalRequired` | `EarningsApprovalRequiredRecord != null` |
| `IsDeclined` | `EarningsApprovalRequiredRecord?.IsDeclined` |
| `TraineeClaimed` | For code `"41"`: resolves trainee name from on-duty payroll info |
| Display strings | `STAmountStr`, `OTAmountStr`, `AmountStr`, `CalculatedAmountStr` � formatted with `$` |

### Navigation Properties

| Property | Type |
|---|---|
| `PayrollRecord` | `PayrollRecord` |
| `Code` | `PayrollCode` |
| `EarningsApprovalRequiredRecord` | `EarningsApprovalRequiredRecord` (1:1, nullable) |
| `PayrollEarningProcessedRecord` | `PayrollEarningProcessedRecord` (1:1, nullable) |

### `CreateEarningsApprovalRequiredRecord(db, user, now, approvalctrlnbr)` � See Part 8

### `CreatePayrollEarningProcessedRecord(db, processrec, payperiod, final, user)`

Creates `PayrollEarningProcessedRecord` linking to a `PayrollPeriodProcessRecord`. Tracks whether this is a final or preliminary process.

---

## PayrollCode

**Inherits**: `ControlNumberBase`

Defines earning types in the system.

### Key Properties

| Property | Type | Description |
|---|---|---|
| `Code` | `string` | 2-character code (e.g., `"01"`, `"05"`, `"22"`) |
| `Description` | `string` | Human-readable name |
| `Overtime` | `bool` | Whether this is an overtime code |
| `ApprovalRequired` | `bool` | Whether earnings need approval |
| `Arbitrary` | `bool` | Fixed amount (not hours-based) |
| `Accumulator` | `bool` | Whether amounts accumulate |
| `CompensationType` | `string` | Compensation bank type (vacation, personal, etc.) |

### Known Payroll Codes

| Code | Description | Overtime |
|---|---|---|
| `01` | Straight Time | No |
| `02` | Overtime (unassigned) | Yes |
| `04` | Vacation Week | No |
| `05` | Holiday | Yes |
| `06` | Vacation Day | No |
| `10` | Jury Duty | No |
| `11` | Bereavement | No |
| `12` | Personal Day | No |
| `13` | Guarantee Pay | No |
| `19` | Worked a Double | Yes |
| `20` | Double Time | Yes |
| `21` | Time Claim | No |
| `22` | Off Day | Yes |
| `41` | Trainer Pay | No |
| `42` | Trainee Pay | No |
| `43` | Job Trainee | No |
| `44` | Other/Claims Payment | No |
| `45` | Safety Day | No |

---

## Linkage Entities

### DailyCrewPositionOnDutyPayrollRecord
Links `DailyCrewPositionOnDutyRecord` ? `PayrollRecord`. Created when an on-duty record generates payroll.

### DailyRailroadEmployeePositionPayrollRecord
Links daily position records ? `PayrollRecord`. For non-crew (board) positions.

### DailyShiftExtraBoardPositionPayrollRecord
Links extra board positions ? `PayrollRecord`. For XB-specific payroll.

### PayrollHolidayRecordPayrollRecord
Links holiday records ? `PayrollRecord`. For holiday pay generation.

---

## Review & Processing Entities

### PayrollReviewRequiredRecord
- PK = FK to PayrollRecord (1:1). `Reason` (string). Flags record for manual review.

### PayrollReviewRecord
- PK = FK to PayrollReviewRequiredRecord (1:1). Marks review as complete.

### PayrollEarningProcessedRecord
- PK = FK to PayrollEarningRecord (1:1). `PayrollPeriodProcessRecordControlNumber`, `PayPeriod`, `FinalProcess`, `ProcessedDateTime`.

### PayrollRecordDelete
- PK = FK to PayrollRecord (1:1). Soft delete marker.

---

## Payroll Flow Summary

```
Work event occurs (on-duty, mark-off, holiday, etc.)
  ?
PayrollRecord created (header with employee/position context)
  ?
PayrollEarningRecord(s) created (line items)
  ?? CalculatePayrollAmounts() � rates � hours ? amounts
  ?? 40-hour OT conversion for XB employees (Pool 20/30)
  ?? Compensation bank debit if applicable
  ?
Approval routing (if ApprovalRequired)
  ?? EarningsApprovalRequiredRecord ? routes to officer
  ?? Officer approves ? EarningsApprovalRecord
  ?? Officer declines ? EarningsDeclanationRecord
  ?
Review (if ReviewRequired)
  ?? PayrollReviewRequiredRecord with reason
  ?? Reviewer completes ? PayrollReviewRecord
  ?
Processing
  ?? PayrollEarningProcessedRecord (preliminary)
  ?? Export to ADP/UKG file
  ?? PayrollEarningProcessedRecord (final after import confirms)
  ?
Import paid amounts
  ?? SAImportADPPayrollService reads export file
  ?? Updates STPaid, OTPaid, PaidAmount, TotalPaid
```
# Part 20: Holiday Processing

## Overview

The holiday system manages holiday definitions, qualification tracking (pre/post work requirements), and holiday payroll generation.

## Holiday Entity

**Inherits**: `ControlNumberBase`

| Property | Type | Description |
|---|---|---|
| `ClientControlNumber` | `long` | FK to Client |
| `HolidayDate` | `DateTime` | The holiday date |
| `HolidayName` | `string` | `[StringLength(100)]` |

### `CreateHoliday(railroad, date, name, user, now)`
Opens own DbContext. Skips if holiday already exists for that date.

---

## PayrollHolidayRecord

**Inherits**: `ControlNumberBase`

Per-employee holiday record. Tracks qualification and payroll generation.

| Property | Type | Description |
|---|---|---|
| `RailroadControlNumber` | `long` | FK |
| `RailroadPoolEmployeeControlNumber` | `long` | FK |
| `HolidayControlNumber` | `long` | FK |
| `ReviewRequired` | `bool` | Needs manual review |

### `Qualified` (Computed)
Checks both PRE and POST `HolidayQualifyRecord`s. Both must be qualified for the employee to receive holiday pay.

### `ProcessHolidayPayrollRecord(db, user, now)`
1. Get job code, pay code, ST hours for the holiday date
2. Find employee's position record for that date
3. If craft processes payroll AND employee processes payroll:
   - Get payroll code `"05"` (Holiday)
   - Create payroll record with holiday earning

---

## HolidayQualifyRecord

Tracks whether an employee worked the qualifying day before/after a holiday.

| Property | Type | Description |
|---|---|---|
| `PayrollHolidayRecordControlNumber` | `long` | FK |
| `Pre_Post` | `string` | `"PRE"` or `"POST"` |
| `Qualified` | `bool` | Whether employee qualifies |
| `QualifyDate` | `DateTime` | The work day checked |

## Holiday Flow

```
Holiday created ? PayrollHolidayRecord per employee
  ?
Pre-holiday work day:
  QualifyHolidayRecord() checks if employee worked ? PRE HolidayQualifyRecord
  ?
Post-holiday work day:
  QualifyHolidayRecord() checks if employee worked ? POST HolidayQualifyRecord
  ?
Both PRE and POST qualified ? ProcessHolidayPayrollRecord()
  ? PayrollRecord with code "05" (Holiday)
```
# Part 28: Compensation Time Accounts

## Overview

Tracks banked time (vacation, personal, sick) that employees can draw from for paid absences.

## RailroadEmployeeCompensableTimeRecord

Tracks deposits and withdrawals for compensation time banks.

| Property | Type | Description |
|---|---|---|
| `RailroadEmployeeControlNumber` | `long` | FK |
| `CompensationType` | `string` | Type of bank (vacation, personal, sick, etc.) |
| `Hours` | `double` | Positive = deposit, negative = withdrawal |
| `BalanceDate` | `DateTime` | Date of transaction |
| `Notes` | `string` | Description of transaction |

## Key Methods on RailroadEmployee

- `GetCompensationTimeAccountBalanceHours(type)` ? `double`: Sum of all records for type
- `CreateCompensationTimeAccountWithdrawl(type, hrs, date, notes, user, now)`: Creates negative record
- `RemoveUnusedMarkOffRequestRecords(db, type, user)`: When balance depleted, removes future requests
- `RemoveUnusedMarkOffWaitListRecords(db, type)`: When balance depleted, removes wait list entries

## Craft-Level Configuration

| Entity | Description |
|---|---|
| `CraftVacationDay` | Vacation days allowed per service year per craft |
| `CraftPersonalDay` | Personal days per craft |
| `CraftSickDay` | Sick days per craft |
| `CraftMarkOffAllowance` | Mark-off allowance per code per craft |
| `RailroadPoolMarkOffAllowance` | Pool-level override |

## Flow

```
Annual allocation ? CompensableTimeRecord (positive deposit)
  ?
Employee requests mark-off (VD, PD, SD, etc.)
  ?
MarkOffRecord.CreateMarkOffRecord() calculates CompHours
  ?
PayrollRecord.DebitCompensationAccount() ? CompensableTimeRecord (negative withdrawal)
  ?
Balance check ? if depleted ? remove future requests and wait list entries
```
# Part 34: Engineer-Specific Logic

## Overview

Engineers have unique pay rate calculations based on locomotive weight class.

## EngineerJobCode

**Inherits**: `ControlNumberBase`

Defines pay classifications based on locomotive weight.

| Property | Type | Description |
|---|---|---|
| `RailroadControlNumber` | `long` | FK |
| `PayClassCode` | `string` | Pay classification code |
| `TraineePayClassCode` | `string` | Pay code for trainees on this class |
| `MaxWeightOnDrivers` | `int` | Maximum weight on drivers (tons) � determines pay tier |

### `LocomotiveType_Weight` (Computed): `"{PayClassCode} - {MaxWeightOnDrivers}"`

### Soft Delete: `EngineerJobCodeDelete` (1:1 optional)

---

## EngineerPayRate

Defines actual pay rates for each engineer job code.

| Property | Type | Description |
|---|---|---|
| `EngineerJobCodeControlNumber` | `long` | FK |
| Per-rate fields | `decimal` | Various rate components |

## Usage in Payroll

`PayrollRecord.GetStraightTimeRate(db)` and `GetOvertimeRate(db)`:
- For Pool 10 (Yard & Enginemen): rate lookup uses `EngineerJobCode` + `EngineerPayRate`
- Locomotive weight from on-duty locomotive record determines which job code applies
- Stored procedure called for vacation rate calculation (see Part 16)

## LocomotiveInspectionRecord

Tracks locomotive inspections performed by engineers during on-duty periods. Linked to `DailyOnDutyLocomotiveRecord`.
# Part 35: Pay Rates & Tiers

## Overview

The pay rate system has multiple layers: base rates, position-specific rates, craft-specific code rates, engineer weight-based rates, and pool-level payroll tiers.

## Rate Entities

| Entity | Description | Scope |
|---|---|---|
| `PayRate` | Base pay rates by craft | Craft-level default |
| `PositionPayRate` | Override rate for specific position | Position-level |
| `PayrollCodePayRate` | Rate for a specific payroll code | Code-level |
| `CraftPayCode` | Links craft to payroll code with rate info | Craft � Code |
| `EngineerPayRate` | Weight-class-based rates for engineers | Engineer-specific |
| `RailroadPoolPayrollTier` | Tiered pay rates by pool (new employee progression) | Pool-level |

## Rate Resolution Order

When calculating pay for an employee:

1. Check `PositionPayRate` for the worked position
2. If not found ? check `CraftPayCode` for craft � payroll code
3. If not found ? use `PayRate` base rate for craft
4. For engineers ? override with `EngineerPayRate` based on locomotive weight
5. Apply `RailroadPoolPayrollTier.RatePercentage` if employee is in a progression tier

## RailroadPoolPayrollTier

| Property | Type | Description |
|---|---|---|
| `RailroadPoolControlNumber` | `long` | FK |
| `TierNumber` | `int` | Tier level |
| `RatePercentage` | `int` | Percentage of base rate (e.g., 80 = 80%) |
| `TierMonths` | `int` | Months at this tier before progression |

## PayrollRecord.RatePercentage

Stored on each payroll record � the tier percentage in effect when the record was created. Used by `CalculatePayrollAmounts()` to apply the correct rate.
# Part 45: Gap Analysis � Assignment, Tie-Up Processing, Payroll Creation

Continued from Parts 41-43. Gaps 71-85 covering RailroadPosition.Assign(), tie-up post-processing, payroll creation, and unavailable record logic.

---

## GAP 71: RailroadPosition.Assign() Full Flow (Missing from Part 3d)

Parameters: `(db, user, employee, type, assignmentctrlnbr, adate, notify, checkcutofftime)`

1. If XB position ? busy-wait `ExtraBoardInProgress[pool]`
2. Create `RailroadPoolEmployeePosition` with `AssignmentType = type`
3. Complete open notifications
4. If `type == "FA"` (force assign):
   - If not already called/on-duty ? auto mark-off with code `"NN"` (Not Notified)
5. If `type == "NA"` ? force `notify = true`
6. Create `RailroadPositionChange` notification
7. Create daily position record
8. If crew position:
   - Assign daily crew positions
   - If off-day ? create off-day employee record
   - If Pool 20 (Yardmaster) ? update overtime board
9. If board position ? assign daily extra board position
10. If hangout ? create hangout record, set timers

---

## GAP 72: ManualAssign Uses PositionType "D" (Missing from Part 3d)

`ManualAssign()` looks up a default position via `PositionType.Equals("D")`. Known PositionType values:
- `"D"` � Default/placeholder position
- Other values used but not documented

Logs error if default position not found.

---

## GAP 73: "NN" Not Notified Auto Mark-Off (Missing from Part 14)

When force-assigning a bulletin (`type == "FA"`):
- If employee is not called and not on-duty ? create mark-off with code `"NN"`
- Added to the complete mark-off code list: `"NN"` = Not Notified

---

## GAP 74: "UA" Unavailable Auto Mark-Off (Missing from Part 14)

`CreateDailyOnDutyUnavailableRecord()` � marks employee unavailable when already working another shift:

```
if (sameDate AND sameShift) OR lastShift == thisShift.PreviousShiftID:
  Create unavailable record with code "UA"
  Send Teams "SystemMessage"
```

Uses `Shift.PreviousShiftID` to determine shift adjacency.

---

## GAP 75: Tie-Up Post-Processing Steps (Incomplete in Part 5)

After off-duty record is created, these steps execute:

1. `ConsecutiveDayRestedDateTime` = FRA calc if worked, else RestedDateTime
2. `UpdatePreviousRestInformation()` � updates next on-duty record's rest fields
3. If craft has `HoursofService` ? `FRARequirements.CheckFRARestCompliance()`
4. If tied up late ? `UpdateHangoutNotificationDateTime()`
5. If XB FIFO board ? `SetBoardOrder()` using off-duty time
6. If payroll tier < 100% ? `UpdatePayrollTierRate()`
7. Send AtHoc off-duty message

---

## GAP 76: AFE Billing � Pool 50 Only (Missing from Part 29)

`CreateDailyOnDutyAFEBillingRecord()`:
```
if Pool 50 (Maintenance of Way)
  AND assignment is Recollectable
  AND employee has open TemporaryAssignment with AFE record:
    Create DailyOnDutyAFEBillingRecord with AFE number/description
```

Only Pool 50 auto-creates AFE billing records from temporary assignments.

---

## GAP 77: IsMarkedOffThisDateTime Logic (Missing from Part 14)

Determines if a mark-off applies to a specific on-duty record. Complex date overlap logic:

1. Check mark-off datetime is NOT before on-duty datetime
2. If auto mark-up hours exist ? check on-duty falls within mark-off window
3. Uses `CraftMarkOffCode.AutomaticMarkUpHours` (craft-specific override) before `MarkOffMarkUpHours.MarkUpHours`
4. If on-duty ? check mark-off before off-duty, with exception for `ApprovedByAgreement` codes
5. If marked up ? special handling for codes `"NR"`, `"NN"`, `"SR"`: uses `GetStartCallTime()` instead of on-duty time

---

## GAP 78: Vacation Week Mark-Off Code Resolution (Missing from Part 14)

`MarkOffRecord.GetVacationWeekMarkOffCode()` � when a V1-V5 mark-off spans multiple days, each daily record may get a different code depending on whether it falls on a work day or off day.

Multi-day PD/SD/VD mark-offs also get special handling:
```
if date > markoff date AND code is PD/SD/VD:
  Create additional comp hours record for each extra day
```

---

## GAP 79: ProcessPayroll � Pool 10 Moved Position Exception (Missing from Part 16)

```csharp
if (pool.PoolNumber.Equals(10) && this.DailyCrewPosition.MovedDailyCrewPosition != null)
```

Pool 10 (Y&E) always creates a new payroll record when the position was moved (employee swapped positions mid-shift). Other pools update existing records.

---

## GAP 80: Annulled Position Payroll (Missing from Part 16)

```
if position IsAnnulled AND not marked off AND not unavailable AND not holiday:
  Create payroll record using scheduled on/off duty times
  Flag for review: "Position {jobworked} was anulled"
```

Annulled positions still generate payroll (guarantee pay) unless it's a holiday.

---

## GAP 81: Payroll Review vs Marked Off While Working (Missing from Part 16)

When payroll is reprocessed (not first time):
- If mark-off code has `MarkOffPayrollCode` AND `!BasicDay` ? always flag for review
- Otherwise ? flag for review with user name who changed it
- Existing payroll records are soft-deleted (`PayrollRecordDelete`) before recreation

---

## GAP 82: Shift.PreviousShiftID (Missing from Part 3c)

`Shift` has `PreviousShiftID` used in unavailable record logic to determine if an employee's last on-duty record was from the immediately preceding shift.

---

## GAP 83: DailyOnDutyPayrollInformation.JobPaidCode Override (Missing from Part 29)

In `CreateOnDutyPayrollRecord()`:
```csharp
if (this.DailyOnDutyPayrollInformation != null)
    jobpaid = this.DailyOnDutyPayrollInformation.JobPaidCode;
```

The `DailyOnDutyPayrollInformation` record can override the default job-paid code. Also contains `FirstMealPeriod` flag used by Pool 50 meal deduction logic.

---

## GAP 84: ManualTieUpNotification (Missing from Part 5)

Created when employee is marked off while working and on-duty record is incomplete:

```
Text: "{EmpName} has an outstanding on duty record, from assignment {Name} 
       on duty at {DateTime}, that requires completion."
NotificationRequired = true
EmployeeOnly = true
```

Checks for existing duplicate notification before creating (prevents repeats).

---

## GAP 85: Complete Mark-Off Code Reference (Updated)

All 16 codes found in hard-coded logic:

| Code | Description | Auto | System |
|---|---|---|---|
| `"SR"` | Safety Rest (FRA 6-day) | Yes | Yes |
| `"NR"` | Not Rested (FRA rest) | Yes | Yes |
| `"NN"` | Not Notified (force assign) | No | Yes |
| `"CR"` | Called Relief | No | Yes |
| `"MC"` | Missed Call | No | Yes |
| `"UA"` | Unavailable (shift overlap) | No | Yes |
| `"FL"` | FMLA | No | No |
| `"LD"` | Light Duty | No | No |
| `"CY"` | Called for Yardmaster | No | Yes |
| `"YT"` | Yardmaster Training | No | Yes |
| `"V1"`-`"V5"` | Vacation weeks (1-5) | Yes | No |
| `"VD"` | Vacation Day | Yes | No |
| `"CD"` | Compensated Day | Yes | No |
| `"PD"` | Personal Day | Yes | No |
| `"SD"` | Sick Day | Yes | No |
# Part 46: Gap Analysis � Arbitrary Earnings, Earning Codes, Payroll Rules

Gaps 86-100 covering payroll earning code processing, arbitrary pay, and pool/craft-specific payroll rules.

---

## GAP 86: Arbitrary Earnings by Pool (Missing from Part 16)

`ProcessPayrollArbitraryEarnings()` adds extra pay based on pool and craft:

| Pool | Craft | Earnings Added |
|---|---|---|
| 10 | Engineer | Crew consist pay (code 14), Travel pay (15), Meals (18), Certification (59) |
| 10 | Yardman | Crew consist pay (14), Travel pay (15), Air pay (16), Meals (18), Certification (59) |
| 20 | All | Turnover pay (24) |
| 30 | All | Turnover pay (24) |
| 40 | All | Meals (18) only |
| 50 | All | MOW meals (special rules) |
| 60 | All | None |

---

## GAP 87: Hard-Coded Payroll Earning Codes (Missing from Part 16)

All earning codes referenced in hard-coded logic:

| Code | Description | Special Logic |
|---|---|---|
| `"01"` | Straight time | Training check, 40hr OT conversion, moved position check |
| `"02"` | Overtime (called) | All hours ? OT; Yardman Foreman "H"?"F" fix |
| `"05"` | Holiday | OT approval exemption for Pool 30 |
| `"10"` | Jury Duty | 8 hours ST, always requires approval |
| `"11"` | Bereavement | 8 hours ST, always requires approval |
| `"14"` | Crew consist pay | Protected/semi-protected only, amount from PayrollCodePayRate |
| `"15"` | Travel pay | Protected only, location-based half/full rate |
| `"16"` | Air pay | Protected only, if assignment has AirPay flag |
| `"18"` | Meal period | First/Second meal, craft-specific rules |
| `"19"` | Overtime (other) | All hours ? OT |
| `"20"` | Double time | All hours ? OT (then doubled in rate calc) |
| `"22"` | Overtime (other) | Same as "02" including Yardman fix |
| `"24"` | Turnover pay | 15 minutes ST if Position.TurnoverPay |
| `"41"` | Trainer pay | Auto-added when earning code is "42" |
| `"42"` | Training (with trainer) | From DailyOnDutyPayrollInformation.TrainingClaimed |
| `"43"` | Training (on training roster) | From IsTraining flag |
| `"44"` | Company business | Compensated, with arbitrary earnings |
| `"45"` | Company business (alt) | Same as "44" |
| `"48"` | Annulled position pay | Guarantee pay for annulled positions |
| `"59"` | Certification pay | If Position.CertificationPay |

---

## GAP 88: Trainer Pay Auto-Add (Missing from Part 16)

When earning code is `"42"` (training with trainer), code `"41"` (trainer pay) is auto-added:

| Pool | Trainer Pay ST Hours |
|---|---|
| 10 (Y&E), 20 (YM) | 2 hours |
| 30 (Clerical) | 1 hour |
| Others | 0 (no trainer pay) |

Always flags for review: "Trainer pay earning record requires approval."

---

## GAP 89: Mechanical "Porkchop Pay" (Missing from Part 16)

Pool 40 (Mechanical) has minimum OT rounding based on `ADPInterface.ColumnNumber`:

```
Column 1 or 3 (Straight time):
  if OT > 0 AND OT < 1 hour ? round up to 1 hour
Column 2 or 4 (Overtime):
  if OT > STHours AND OT < (STHours + 1) ? round up to (STHours + 1)
```

Code comment: "Mechanical Department Porkchop Pay"

---

## GAP 90: MOW Overtime Minimum and Double Time (Missing from Part 16)

Pool 50 (Maintenance of Way) special rules:

1. **Minimum OT**: If overtime, minimum is 2 hours 40 minutes
2. **Double time over 16 hours**: If HoursOnDuty > 16h, excess is paid at double time (code `"20"`)
3. **OT ST override**: If `IsOvertime`, set ST hours = HoursOnDuty (full hours paid at OT rate)

---

## GAP 91: Travel Pay Location-Based Rates (Missing from Part 16)

Travel pay (code `"15"`) for Pool 10 protected employees only:

| Location.LocationName | Rate |
|---|---|
| `"Manchester Yard"` | 50% of PayrollCodePayRate |
| `"Pasadena Yard"` | 100% of PayrollCodePayRate |
| Any other | No travel pay |

---

## GAP 92: Crew Consist Pay � Protected vs Semi-Protected (Missing from Part 34)

Code `"14"` (crew consist):

**Yardman (semi-protected)**: Always gets crew consist pay if `IsSemiProtected`

**Engineer (protected)**: Only gets crew consist pay if another protected engineer is on the SAME assignment (crew of 2+ protected engineers)

---

## GAP 93: Payroll Batch Number Format (Missing from Part 16)

```csharp
batch = string.Format("{0}{1}", craft.RailroadPool.PoolNumber, craft.CraftNumber);
```
Default: `"9999"` if craft is null. Otherwise `"{PoolNumber}{CraftNumber}"`.

---

## GAP 94: Engineer/Yardman JobPaid Auto-Fix (Missing from Part 16)

In `CreatePayrollRecord()`, if `jobPaid == jobWorked`:

| CraftName | Fix |
|---|---|
| `"Engineer"` | Change to `"10O1"` |
| `"Yardman"` | Change to `"100{last char of jobPaid}"` |

Prevents accidentally using the worked code as the paid code.

---

## GAP 95: OT Approval Rules by Pool (Missing from Part 8)

Pools 20/30/40/50/60 require approval for these conditions:
1. Any overtime (not code "05" holiday, except Pool 30 which exempts "05")
2. On-duty time changed from scheduled
3. `ReleaseReason == "SR"` (tied up early)
4. Tied up next work day (off-duty date > today)

Pool 10 (Y&E): Does NOT require approval for overtime.

---

## GAP 96: MOW Meal Period Rules (Missing from Part 16)

Pool 50 meal periods have unique rules:

1. First meal: only if on-duty > 6h 30m
2. Emergency call-out OT meals: 1 hour per 5 hours of OT
3. Regular OT meals: 30 minutes if OT > 4 hours
4. Second meal: if claimed value is 31 ? round down to 30

---

## GAP 97: Compensated Mark-Off Payroll Processing (Missing from Part 16)

When marked off AND mark-off code is compensated:

- If `BasicDay`: use scheduled on/off times (not actual). Pool 10 overrides jobpaid to `DefaultJobPaid`
- If CompensationType exists: use comp hours from mark-off record
- Code `"10"` ? jury duty (8h, approval required)
- Code `"11"` ? bereavement (8h, approval required)
- Codes `"44"`, `"45"` ? company business (with arbitrary earnings)

---

## GAP 98: Pool 10 Moved Position "Code 48" Pay (Missing from Part 16)

When Y&E employee is moved and original position is annulled/not filled:

```
if (code "01" or "02") AND position was moved:
  penaltytime = moved.CreatedDate + 2 hours
  if offDuty > penaltytime AND (original annulled OR DoNotFill):
    Create separate payroll record for original position
    Process annulled earnings with code "48"
    Engineer: force jobPaid = "10H1"
```

The 2-hour window: "employee can work 1.5 hours without penalty + 30 min lag"

---

## GAP 99: OT Board Position Ordering on Tie-Up (Missing from Part 39)

`SetOvertimePositionBoardOrder()`:
- Only triggers if earning code is overtime AND not (code "05" on Pool 30)
- Pool 40 (Mechanical): if no OT position found, creates one on the current open OT board
- All pools: delegates to `DailyShiftOvertimeBoard.SetOvertimePositionBoardOrder()`

---

## GAP 100: ReleaseReason Values (Updated from Gap 45)

Complete list of `DailyCrewPositionOffDutyRecord.ReleaseReason` values:

| Value | Meaning | Payroll Impact |
|---|---|---|
| `"NE"` | Normal End (default) | None |
| `"CR"` | Called Relief | Special off-duty, record stays "open" for vacancy |
| `"SR"` | Tied up early | Triggers approval requirement |
# Part 47: Gap Analysis � Earning Code Determination, DailyCrewPosition

Gaps 101-110 covering the master payroll earning code determination, DailyCrewPosition properties, late call, and double-time rules.

---

## GAP 101: GetPayrollEarningCode � Master OT Determination (Missing from Part 16)

`DailyCrewPosition.GetPayrollEarningCode()` � ~200 lines, determines ST vs OT for every on-duty record.

### Base Rules (all pools)
- Holiday ? code `"05"` (overtime=true)
- Assigned employee on regular position ? code `"01"` (straight time)
- Trainee ? always code `"01"`

### When NOT assigned or off-day or worked-a-double:

| Pool | Off Day | Worked Double | Not Assigned (non-XB) | Special |
|---|---|---|---|---|
| 10 (Y&E) | `"22"` | `"19"` | `"02"` only if Yardman moved OR Engineer diff shift | � |
| 20 (YM) | `"22"` | `"19"` | `"02"` if different craft roster (not hold-down) | � |
| 40 (Mech) | `"22"` or `"20"` | `"19"` | `"02"` | Same-shift=ST; bulletined+hangout=ST; vacation relief=ST |
| 50 (MOW) | `"22"` | `"19"` | `"01"` (no OT for non-assigned) | � |
| Default | `"22"` | `"19"` | `"02"` if not XB/hangout | Training=ST |

### Pool 40 (Mechanical) Special: Off-Day Double Time
```
if off day AND last completed record was code "22" AND consecutive day:
  code = "20" (double time)
else:
  code = "22"
```

---

## GAP 102: WorkedaDouble Logic (Missing from Part 16)

Determines if an employee is working a "double" (two shifts in quick succession):

```
Find last on-duty record (different shift, not after current, not OT, not marked off, not hold-down)
timeBetweenStarts = thisOnDuty - lastOnDuty

Pool 50 (MOW):
  if timeBetweenStarts < 22h 30m ? double

All other pools:
  if both are assigned employees ? NOT double
  if STDaysWorked >= 12 in same pay period ? double
  if timeBetweenStarts < 22h 30m ? double
```

Hard-coded: **22 hours 30 minutes** threshold, **12 ST days** per pay period.

---

## GAP 103: STDaysWorked / DaysWorked Counter (Missing from Part 5)

On-duty record creation resets or increments day counters:

```
Reset to 1 if:
  - No previous record, OR
  - Date is 1st or 16th of month AND different date from last record, OR
  - Not in current pay period

Increment rules:
  - If last record was OT: STDaysWorked stays same, DaysWorked +1
  - If this record is OT: STDaysWorked stays same, DaysWorked +1
  - Otherwise: both +1
```

Pay periods are semi-monthly: 1st-15th and 16th-end of month.

---

## GAP 104: Late Call � 90 Minute Delay (Missing from Part 9)

When `latecall = true` in `CreateDailyCrewPositionOnDutyRecord()`:

```csharp
var starttime = now.AddMinutes(90);
ondutydate = starttime.Date;
ondutytime = new TimeSpan(starttime.Hours, starttime.Minutes, 0);
```

Hard-coded: **90 minutes** added to current time for late call on-duty.

---

## GAP 105: Unavailable Check on On-Duty Creation (Missing from Part 5)

When creating on-duty record for HoursOfService crafts:

```
if last record is called or on-duty:
  resttime = lastOnDuty + MaxHours(12) + RestHours(10) = 22 hours ahead
  if resttime > thisOnDuty ? create unavailable record
```

---

## GAP 106: "NN" Auto Mark-Off on Not-Rested Employee (Missing from Part 14)

During on-duty creation for HoursOfService crafts, if employee has open notifications but no existing "NN" mark-off:

```
if NOT late call
  AND lastRecord.RestedDateTime > LastCallingEndTime
  AND thisOnDuty > lastRecord.RestedDateTime:
    Create mark-off code "NN" with text "{Name} is not rested until {date} and has not been notified."
    Send Teams SystemMessage
```

---

## GAP 107: Mechanical Next-Shift Deletion (Missing from Part 5)

Pool 40 (Mechanical) only, when on-duty is NOT overtime:

```
if employee has next on-duty record
  AND next shift is not first shift ("1")
  AND next on-duty is on the expected next shift:
    Delete the next on-duty record
```

Uses `Shift.NextShiftID` to determine expected next shift.

---

## GAP 108: DailyCrewPosition.JobCode and PayrollCode by Pool (Incomplete in Part 4)

Third location of pool-specific job code formatting:

### JobCode
| Pool | Format |
|---|---|
| 30 (Clerical), 60 (Patrol) | `"{PositionCode}{AssignmentNumber}"` |
| 50 (MOW) | `"{AssignmentName}"` |
| Default (10, 20, 40) | `"{AssignmentNumber}{PositionCode}"` |

### PayrollCode
| Pool | Format |
|---|---|
| 30 (Clerical), 50 (MOW), 60 (Patrol) | `"{PositionCode}{PayrollCode}"` |
| Default (10, 20, 40) | `"{PayrollCode}{PositionCode}"` |

### DefaultJobPaid (third copy of this logic)
Same Engineer/Yardman hard-coded values as RailroadPoolEmployee and RailroadPosition.

---

## GAP 109: DailyCrewPosition Position Name Checks (Missing from Part 4)

Hard-coded position name comparisons:
- `IsForeman`: `Position.PositionName.Equals("Foreman")`
- `IsHelper`: `Position.PositionName.Equals("Helper")`

Used for CanMoveToForeman logic and job code formatting.

---

## GAP 110: Trainee On-Duty � Assigned Employee Logic (Missing from Part 5)

When creating on-duty records, trainees are treated as assigned employees:

```
if employee.AssignedPosition.IsTraineePosition:
  assignedposctrlnbr = THIS daily crew position's RailroadPosition
  (not the trainee's actual assigned position)
```

This ensures trainees get code `"01"` (straight time) and are counted as "assigned."

---

## GAP 111: DepartmentNumber Substring (Missing from Part 4)

```csharp
DepartmentNumber = this.Position.RailroadPayrollDepartment.DepartmentNumber.Substring(1);
```

Strips the first character of the department number. The `ICC_DepartmentNumber` property concatenates ICC + full department number.
# Part 51: Gap Analysis � PayrollUtilities, Approval Officer, ADP/UKG Interface

Gaps 156-168 covering payroll approval routing, ADP/UKG file generation, hard-coded job code fixes, and batch 1020 crew consist.

---

## GAP 156: Approval Officer Role Hierarchy (Missing from Part 8)

`GetDefaultApprovalOfficer()`:
```
if HttpContext is null OR user is null ? "Railroad Human Resources"
if user is "Railroad Employee" or "Railroad Timekeeper" ? "Railroad Human Resources"
else ? "Railroad Auditor"
```

Falls back to first user in the resolved role.

---

## GAP 157: ApproveWithPayrollCodeOfficer � Earning Code/Pool Matrix (Missing from Part 8)

Determines whether to use payroll code's own approval officer or the position's officer:

| Earning Code | Pool Restriction | Uses PayrollCode Officer |
|---|---|---|
| `"04"` (Vacation Week) | Pool 10, 20 only | Yes |
| `"06"` (Vacation Day) | Pool 10, 20 only | Yes |
| `"12"` (Personal Day) | Pool 10, 20 only | Yes |
| `"41"` (Trainer Pay) | Pool 10, 20 only | Yes |
| `"10"` (Jury Duty) | All pools | Yes |
| `"11"` (Bereavement) | All pools | Yes |
| `"21"` (Time Claim) | Pool 10 only | Yes |
| `"43"` (Job Trainee) | Pool 10 only | Yes |
| `"44"` (Other/Claims) | All pools | Yes |
| `"45"` (Safety Day) | All pools (only if no CompensationType) | Yes |
| All others | � | No (use position officer) |

New earning codes found: `"04"`, `"06"`, `"12"`, `"21"`.

---

## GAP 158: UKG Interface � Job Paid Code Corrections (Missing from Part 38)

Hard-coded job-paid corrections applied before file generation (both UKG and ADP):

| Original | Corrected | Comment |
|---|---|---|
| `"101D"` | `"10H1"` | Engineer wrong payroll code |
| `"A122"` | `"A123"` | Unknown correction |
| `"100F"` | `"101F"` | Yardman Foreman fix |
| `"100H"` | `"101H"` | Yardman Helper fix |
| Starts with `"S"` | `""` (empty) | Gloves/safety incentives � no job paid |

### Rate-based job code suffix
```csharp
if RatePercentage < 100 AND jobPaid not empty:
    ukgjobpaid = "{jobPaid}{RatePercentage}"
```

---

## GAP 159: Batch Number Corrections (Missing from Part 16)

```csharp
if (payrec.Batch.StartsWith("40")) payrec.Batch = "4010";
if (payrec.Batch.StartsWith("50")) payrec.Batch = "5010";
```

Mechanical (40xx) and MOW (50xx) batch numbers are forced to `"4010"` and `"5010"` during file generation.

---

## GAP 160: ADP Column Number Routing (Missing from Part 38)

`ADPInterface.ColumnNumber` determines which CSV column receives the earning:

| Column | Content |
|---|---|
| 1, 2 | Reg Hours, OT Hours (columns 5-6) |
| 3, 4 | Hours 3/4 Code + Amount (columns 7-10) |
| 5 | Earnings 5 Code + Amount (columns 11-12) |
| 6 | Earnings 3 Code + Amount (columns 15-16) |

Special: Code `"20"` (Double Time) on columns 3/4 ? OT hours are placed in ST column.

---

## GAP 161: Batch 1020 Crew Consist � $5.00 Auto-Add (Missing from Part 34)

```csharp
if (payrec.Batch.Equals("1020"))
{
    if (!payrec.RailroadPoolEmployee.IsProtected)
    {
        detail.AppendLine(",,,,,,,,4, 5.00,,");
    }
}
```

Batch `"1020"` (Yardman ST) automatically adds $5.00 crew consist pay (ADP code 4) for non-protected employees. On columns 3/4, also excludes if ADPCode contains "4" or cost number contains "SWMT".

---

## GAP 162: ADP Cost Number Format (Missing from Part 38)

```csharp
costnbr = "{PayrollDate:yyMMdd}{DayOfWeek+1}{JobWorked}{JobPaid}"
```

15-character fixed format. DayOfWeek is 1-indexed (Sunday=1).

---

## GAP 163: UKG CSV Format (Missing from Part 38)

UKG output CSV columns:
```
EmployeeNumber, UKGEarningCode, Hours (or empty), Amount (or empty), JobPaid, PayrollDate
```

Three value types per earning: `"ST Hours"`, `"OT Hours"`, `"Amount"`. Each maps to a `UKGInterface` record.

---

## GAP 164: ADP CSV Header (Missing from Part 38)

ADP file header columns:
```
Co Code, Batch ID, File #, Pay #, Temp Cost Number,
Reg Hours, O/T Hours, Hours 3 Code, Hours 3 Amount,
Hours 4 Code, Hours 4 Amount, Earnings 5 Code, Earnings 5 Amount,
Memo Code, Memo Amount, Earnings 3 Code, Earnings 3 Amount
```

17 columns total. `Co Code` is always `"PT1"`.

---

## GAP 165: Payroll File Network Paths (Missing from Part 13)

| Path | Purpose |
|---|---|
| `\\Finance-svr\Payroll Exports\UKG\UKGPT1.csv` | UKG payroll file |
| `\\Finance-svr\Payroll Exports\UKG\Reports\` | UKG batch/earning summaries |
| `\\Finance-svr\Payroll Exports\UKG\Logs\error.log` | UKG error log |
| `\\Finance-svr\Payroll Exports\ADP\EPIPT190.csv` | ADP payroll file |
| `\\Finance-svr\Payroll Exports\ADP\VALPT1AA.csv` | ADP job cost file |
| `\\Finance-svr\Payroll Exports\ADP\Batch99.csv` | ADP excluded TIES records |
| `\\Viper\payroll\ADPPTRA\EPIPT190.csv` | ADP source file (from TIES) |

---

## GAP 166: TIES Integration in ADP Export (Missing from Part 38)

The ADP export reads `\\Viper\payroll\ADPPTRA\EPIPT190.csv` and:
1. Splits records: employee matches go to `excludefile`, non-matches stay in `payfile`
2. Excluded TIES records that don't overlap with new records go to Batch99
3. Both new and remaining TIES records are merged into final `EPIPT190.csv`

---

## GAP 167: Payroll Import CSV Format (Missing from Part 38)

`CreatePayrollRecordsFromImport()` reads a CSV:
```
Employee Number, Lump Sum Amount, Pay Code, On Duty Date
```

Creates payroll records with lump-sum amounts using the employee's default job worked/paid codes.

---

## GAP 168: Complete Earning Code Reference � 24 Codes

| Code | Description | New in this pass |
|---|---|---|
| `"04"` | Vacation Week | Yes |
| `"06"` | Vacation Day | Yes |
| `"12"` | Personal Day | Yes |
| `"21"` | Time Claim | Yes |
| All others | See Gap 87 | � |
# Part 53: Gap Analysis � PayrollRecord Rate Calculation, TemporaryAssignment, RosterBoardPosition

Gaps 183-198 covering pay rate lookups, job-paid parsing, compensated rates, 40h XB overtime, and temporary assignment lifecycle.

---

## GAP 183: JobPaid Substring Parsing for Pay Rates (Missing from Part 16)

Each pool parses the 4-character `JobPaid` differently to look up position pay rates:

| Pool | PayrollCode chars | PositionCode chars | Example |
|---|---|---|---|
| 10 Engineer | N/A (uses EngineerPayRates) | N/A | `"10O1"` ? EngineerJobCode.PayClassCode |
| 10 Yardman | Fixed `"101"` | `JobPaid[3]` | `"101F"` ? PayrollCode=101, PositionCode=F |
| 20 (YM), 40 (Mech) | `JobPaid[0..2]` | `JobPaid[3]` | `"201A"` ? PayrollCode=201, PositionCode=A |
| 30 (Clerical), 60 (Patrol) | `JobPaid[1..3]` | `JobPaid[0]` | `"A123"` ? PayrollCode=123, PositionCode=A |
| 50 (MOW) | `JobPaid[2..3]` | `JobPaid[0..1]` | `"EL10"` ? PayrollCode=10, PositionCode=EL |

Pool 30/60 special fix: if PayrollCode == `"122"` ? change to `"123"`.

---

## GAP 184: Engineer Pay Rate Tables (Missing from Part 16)

Engineers use separate `EngineerPayRates` table (not `PositionPayRates`):

| Field | ST | OT | Training ST | Training OT |
|---|---|---|---|---|
| `ESTHourRate` | ? | | | |
| `EOTHourRate` | | ? | | |
| `TSTHourRate` | | | ? | |
| `TOTHourRate` | | | | ? |

Lookup: first by `PayClassCode`, fallback to `TraineePayClassCode`.

---

## GAP 185: Pool 10 Compensated Rate � Stored Procedure (Missing from Part 16)

```csharp
case 10: // Yard and Enginemen
    using (var cmd = new SqlCommand("RailroadEmployeeVacationRate", conn))
    {
        cmd.Parameters.Add(new SqlParameter("@railroad_employee_control_number", this.RailroadEmployeeControlNumber));
        vrate = Convert.ToDouble(rdr["DailyRate"]) / 8;
    }
    if (strate > vrate) vrate = strate;  // use whichever is higher
```

Only Pool 10 uses a stored procedure for vacation/compensated rate. All other pools use ST rate.

---

## GAP 186: Guarantee Rate � "600A" Job Code (Missing from Part 16)

```csharp
case "600A":
    strate = average of PayrollCode="101" PositionCode="H" and PositionCode="F" rates
    strate /= 2
```

Job code `"600A"` gets a blended rate (average of Helper and Foreman). All other job codes use standard ST rate.

---

## GAP 187: 40-Hour XB Overtime (Missing from Part 16)

Pools 20 (Yardmaster) and 30 (Clerical) only:
```
if employee is XB AND (earning code "01" or "42"):
    maxhours = 40
    if (thisSTHours + weekSTHours) > 40:
        push excess into OT hours
        reduce ST hours accordingly
```

Uses `GetStraightTimeHoursThisWeek()` to track weekly accumulation. New earning code: `"13"` (Guarantee).

---

## GAP 188: Rate Tier Percentage Application (Missing from Part 16)

```csharp
double tier = RailroadPoolPayrollTier.RatePercentage * 0.01m;
strate = Math.Ceiling(strate * tier * 100.0) / 100.0;  // round up to penny
```

Tier is applied to both ST and OT rates. Compensated rate is calculated BEFORE tier.

---

## GAP 189: Double Time Rate Calculation (Missing from Part 16)

```csharp
if (earnrec.PayrollCode.Equals("20")) // Double Time
    earnrec.STAmount = (GetStraightTimeRate(db) * 2) * othrs;
```

Code `"20"` OT hours are paid at 2� ST rate, placed in `STAmount` (not `OTAmount`).

---

## GAP 190: Amount Rounding � Ceiling to Penny (Missing from Part 16)

```csharp
earnrec.STAmount = Math.Ceiling(earnrec.STAmount * 100) / 100;
earnrec.OTAmount = Math.Ceiling(earnrec.OTAmount * 100) / 100;
```

All payroll amounts are rounded UP to the nearest penny (ceiling, not standard rounding).

---

## GAP 191: Compensation Account Zero-Balance Cleanup (Missing from Part 28)

After creating an earning record with a CompensationType:
```
if balance <= 0:
    RemoveUnusedMarkOffRequestRecords(compType)
    RemoveUnusedMarkOffWaitListRecords(compType)
```

Automatically cleans up pending requests when account is depleted.

---

## GAP 192: RosterBoardPosition Auto Mark-Up � Always Exact Time (Missing from Part 21)

```csharp
// RosterBoardPosition:
return morecord.MarkOffDateTime.AddHours(muhrs) - seconds;  // always exact time

// CrewPosition:
case "NR": case "SR": return exact time
default: return midnight + hours + 1 min
```

Board positions always use exact-time mark-up. Crew positions use midnight-based except for NR/SR.

---

## GAP 193: TemporaryAssignment Complete Entity (Missing from Part 4)

| Property | Type | Description |
|---|---|---|
| `AssignmentControlNumber` | `long` | FK to Assignment |
| `TemporaryAssignmentName` | `string` | Display name |
| `StartDate` | `DateTime` | Start date |
| `AssignmentOnDutyTimeControlNumber` | `long` | FK to AssignmentOnDutyTime |
| `StraightTimeHours` | `int` | Hours per day |
| `Billable` | `bool` | Customer billing flag |
| `Recollectable` | `bool` | AFE recollectable flag |

### Navigation
- `TemporaryAssignmentAssignedEmployee`
- `TemporaryAssignmentRelease`
- `TemporaryAssignmentAFERecord`
- `TemporaryAssignmentWorkDays`

---

## GAP 194: TemporaryAssignment Hold-Down Release Chain (Missing from Part 27)

On temp assignment release:
```
Find hold-down on assigned employee's current position (by different employee)
Release that hold-down with date + 1 day
```

Ensures displaced employees return when temp ends.

---

## GAP 195: TemporaryAssignment Moved Position Undo (Missing from Part 4)

When unassigning a temp daily position that was moved:
```
Find MovedDailyCrewPosition
Recursively unassign the old position
Fill the old position back with the original employee
Delete the MovedDailyCrewPosition record
```

---

## GAP 196: PayrollRecord.PayrollDepartment Lookup Chain (Missing from Part 16)

```
1. Check DailyCrewPositionOnDutyPayrollRecords ? Position.RailroadPayrollDepartment
2. Else check DailyRailroadEmployeePositionPayrollRecords ? RailroadPosition.PayrollDepartment
3. Else fallback to RailroadPoolEmployee.PayrollDepartment
```

Three-level fallback for payroll department resolution.

---

## GAP 197: PayrollRecord STHours/OTHours Skip Arbitrary (Missing from Part 16)

```csharp
foreach (var rec in this.PayrollEarningRecords)
{
    if (!rec.Code.Arbitrary)  // skip arbitrary earnings
    {
        hrs += rec.STHours.Hours;
```

The `Arbitrary` flag on `PayrollCode` excludes certain earnings from total hours display.

---

## GAP 198: Complete Earning Code Reference � 25 Codes

| Code | Description | New in this pass |
|---|---|---|
| `"13"` | Guarantee | Yes |
| `"20"` | Double Time (STAmount = 2� ST rate) | Updated with rate logic |
| All others | See Gaps 87, 168 | � |
# Part 57: Gap Analysis � ProcessPayroll, MarkOffRequest, Monthly Pay, Payroll Period

Gaps 246-260 covering payroll processing trial/final flow, monthly safety/glove pay, mark-off request scheduling, vacation week day-count mapping, and payroll period number format.

---

## GAP 246: Payroll Period Number Format (Missing from Part 16)

```
PayPeriod = "{MM}{dd}{yy}" as 6-digit integer
  MM = month
  dd = 01 (first half) or 16 (second half)
  yy = 2-digit year

PayDate:
  if dd == 01-15: paydate = month/15 23:59:59
  if dd == 16:    paydate = last day of month 23:59:59
```

Special case: period "1216" uses previous year for the year component.

---

## GAP 247: Trial vs Final Payroll Process (Missing from Part 16)

### Trial Process
1. If previous trial exists for same period ? delete processed records
2. Query unprocessed records (PayrollEarningProcessedRecord == null)
3. Validate: employee FK integrity, zero-time earnings, unapproved earnings, unreviewed records
4. Generate ADP + UKG files
5. Create `PayrollPeriodProcessRecord` (FinalProcess = false)

### Final Process
1. Query records where FinalProcess = false
2. Generate ADP + UKG files
3. Mark all `PayrollEarningProcessedRecord.FinalProcess = true`

---

## GAP 248: Payroll Record FK Integrity Check (Missing from Part 16)

During trial processing, for each payroll record:
```
Verify Employee, RailroadEmployee, RailroadPoolEmployee FKs match WorkNumber
If mismatch ? auto-correct FKs and log to badpayrollrecords.log
```

Self-healing data integrity check.

---

## GAP 249: Monthly Safety Incentive (Missing from Part 16)

Earning code `"49"` � safety incentive:
```
Eligible: ProcessPayroll=true, Active during month, has payroll record (not time claim "21")
Amount: user-specified (viewModel.ProcessSafety)
JobCode: "S" + DepartmentNumber[1..3]
```

New earning code: `"49"` (Safety Incentive).

---

## GAP 250: Monthly Glove Allowance (Missing from Part 16)

Earning code `"63"` � glove allowance:
```
Eligible: Yardman craft only, has payroll record in the month
Amount: $3.00 (hard-coded)
JobCode: "S" + DepartmentNumber[1..3]
```

New earning code: `"63"` (Glove Allowance).

---

## GAP 251: Mark-Off Request Day Limits by Pool (Missing from Part 14)

| Pool | Max Days Ahead |
|---|---|
| 10 (Y&E) | 45 days |
| 20 (Yardmasters) | 60 days |
| Others | Remaining days in year |

Controls whether "Create" button is shown for future dates.

---

## GAP 252: Mark-Off Request Auto Mark-Off (Missing from Part 14)

```csharp
if (AutomaticMarkOff && RequestDate < now):
    if XB employee && RequestDate not future ? set to now
    if Pool 40 && code starts with "V" ? vacrelief = true
    CreateMarkOffRecord(user, vacrelief)
```

Immediate mark-off execution when the request date is in the past and auto-mark-off is enabled.

---

## GAP 253: Vacation Week Duration ? MarkUpHours Mapping (Missing from Part 14)

| muhrs | Duration Display | Days |
|---|---|---|
| 24 | 1 day | 1 |
| 48 | 2 days | 2 |
| 168 | 1 week (or "Will Mark Up When Ready") | 7 |
| 336 | 2 weeks (or "Will Mark Up When Ready") | 14 |
| 504 | 3 weeks (or "Will Mark Up When Ready") | 21 |
| 672 | 4 weeks (or "Will Mark Up When Ready") | 28 |
| 840 | 5 weeks (or "Will Mark Up When Ready") | 35 |

Vacation weeks: `muhrs = V{n} * 7 * 24` (e.g., V3 = 3 � 7 � 24 = 504).

---

## GAP 254: Wait List Integration (Missing from Part 14)

On request creation:
```
If employee has wait list record for same date + code ? remove wait list record
```

Entity: `MarkOffRequestWaitListRecord` � holds pending requests that couldn't be fulfilled.

---

## GAP 255: PayrollPeriodProcessRecord History Paths (Missing from Part 16)

```
PayrollPath  = \\Finance-svr\Payroll Exports\UKG\History\{payperiod}\
ErrorLogPath = \\Finance-svr\Payroll Exports\UKG\History\{payperiod}\Logs\
ReportPath   = \\Finance-svr\Payroll Exports\UKG\History\{payperiod}\Reports\
```

History organized by payroll period number.

---

## GAP 256: Both ADP and UKG Generated (Missing from Part 38)

Trial processing calls BOTH file generators:
```csharp
result = PayrollUtilities.CreateADPPayrollFile(payrecords, ...);
result = PayrollUtilities.CreateUKGPayrollFile(payrecords, ...);
```

System generates both ADP and UKG files simultaneously (dual payroll system).

---

## GAP 257: Safety Eligibility � Excludes Time Claims (Missing from Part 16)

```csharp
.Where(e => e.PayrollRecords.Any(r => ... && r.PayrollEarningRecords.Any(er => !er.Code.Code.Equals("21"))))
```

Employees with ONLY time claim records (code "21") do NOT qualify for safety incentive.

---

## GAP 258: Next Period Auto-Calculation (Missing from Part 16)

```
if lastPeriod.FinalProcess:
    if day == 1: nextPeriod = day 16 of same month
    else: nextPeriod = day 1 of next month (- 15 days)
else:
    pre-fill with last period number (for retry)
```

---

## GAP 259: Complete Earning Code Reference � 27 Codes

| Code | Description | New in this pass |
|---|---|---|
| `"49"` | Safety Incentive | Yes |
| `"63"` | Glove Allowance ($3.00) | Yes |
| All others | See Gaps 87, 168, 198 | � |

---

## GAP 260: Static State in ProcessPayrollController (Missing from Part 13)

```csharp
internal static List<PayrollRecord> Records = new List<PayrollRecord>();
internal static List<RailroadPoolEmployee> RPEmployees = new List<RailroadPoolEmployee>();
internal static string Status = string.Empty;
```

Mutable static state shared across requests � thread-safety risk in multi-user environment.
