# Part 339: Strict Full Sweep Tracker (Increment 1)

This section starts a strict, incremental full-codebase reconciliation pass so the process remains stable and does not stall.

---

## 339.1 Sweep Method (Strict / Incremental)

Review order used:

1. Executable runtime projects (services + console)
2. Web host orchestration + integration entry points
3. Configuration surfaces (`Web.config` + `App.config`)
4. Core shared utility/base patterns
5. Controller orchestration
6. Deep model logic and edge-case methods (next increments)

---

## 339.2 Source Inventory Baseline (excluding `bin/obj/packages`)

| Project | `all_cs` | `core_cs` (non-Designer, non-Migrations, non-AssemblyInfo) |
|---|---:|---:|
| `StrategicApplications` | 1024 | 472 |
| `SAClassLibrary` | 274 | 236 |
| `SADailyCallSheetService` | 31 | 21 |
| `SAImportPayrollService` | 8 | 4 |
| `SAAtHocMessageService` | 8 | 4 |
| `RestartApplicationPool` | 2 | 1 |

`core_cs` is the primary strict-sweep target for behavioral logic.

---

## 339.3 Pattern Scan Totals (code/config, same exclusions)

| Pattern | Match Count | Notes |
|---|---:|---|
| `autoprocess` | 48 | background identity + audit writes |
| `PoolNumber.Equals(` | 122 | heavy pool-specific branching |
| `Thread.Sleep(` | 48 | timer pacing, lock waits, id generation spacing |
| `new DateTime(9999, 12, 31)` | 19 | sentinel “no schedule” datetime |
| `CreateMSMQMessage(` | 11 | queue-producing call paths |
| `FormatName:DIRECT=OS:` | 13 | MSMQ endpoint hard-coding |
| `\\sql-svr` | 4 | UNC runtime paths |
| `\\Finance-svr` | 44 | payroll import/export/archive paths |

---

## 339.4 Increment 1 Reviewed Scope (completed)

### A) Executable runtime services and entry points

Fully reviewed process flow and hard-coded logic:

- `SADailyCallSheetService` service chain and all queue handlers
- `SAImportPayrollService` ADP/UKG import handlers
- `SAAtHocMessageService` call/on-duty flows
- `RestartApplicationPool` console restart loop

### B) Web host orchestration and core runtime entry

- `StrategicApplications/Global.asax.cs`
- `StrategicApplications/Utilities/ApplicationUtilities.cs`
- `StrategicApplications/Services/AtHocService.cs`
- `StrategicApplications/App_Start/Startup.Auth.cs`
- `StrategicApplications/Models/Context/StrategicApplicationsContext.cs`
- `SAClassLibrary/Context/SAClassLibraryContext.cs`

### C) Controller orchestration sweep (first strict block)

- `ProcessPayrollController` (period processing, trial/final branching, archive/write behavior)
- `MarkOffRequestController` (auto mark-up mappings + time normalization)
- `DailyAssignmentShiftController` (queue-producer behavior)
- `AccountController` (last-login/on-property tracking)

### D) Configuration surfaces

Reviewed:

- `StrategicApplications/Web.config`
- `SADailyCallSheetService/App.config`
- `SAImportPayrollService/App.config`
- `SAAtHocMessageService/App.config`
- `SAClassLibrary/App.config`
- `RestartApplicationPool/App.config`

#### Additional hard-coded configuration findings captured in this increment

1. `SAClassLibrary/App.config` has two separate `<connectionStrings>` blocks, one for each context name.
2. `StrategicApplicationsContext` constructor uses `"StrategicApplicationsDemoContext"` while `Web.config` also defines production context names.
3. Service/app configs include direct infrastructure hostnames (`SQL-SVR`, `PTRA-IT-LT-10`, `sql-svr`, `Finance-svr`).
4. AtHoc and Teams webhook settings are present in both web and service configs (duplicated integration surface).

### E) Core base/entity behavior spot-check (strict high-impact methods)

Reviewed and reconciled:

- `StrategicApplications/Models/BaseClasses/ControlNumberBase.cs`
- `SAClassLibrary/BaseClasses/ControlNumberBase.cs`
- `StrategicApplications/Models/IdentityModels.cs`
- `StrategicApplications/Models/RailroadPool.cs`
- `SAClassLibrary/Models/RailroadPool.cs`
- `StrategicApplications/Models/Shift.cs`
- `StrategicApplications/Models/DailyCrewPosition.cs` (vacancy/on-duty retrieval + fill path segment)
- `StrategicApplications/Models/MarkOffCode.cs`
- `StrategicApplications/Models/MarkOffRecord.cs` (vacation-week code resolution branch)
- `StrategicApplications/Models/RailroadPoolEmployee.cs` (mechanical overtime branch)

---

## 339.5 Strict Sweep Status After Increment 1

| Layer | Status |
|---|---|
| Executable service logic | **Complete (increment scope)** |
| Configuration layer | **Complete (increment scope)** |
| Web runtime orchestration | **Complete (increment scope)** |
| Controller layer | **Partial** (high-impact controllers done) |
| Deep model logic layer | **Partial** (high-impact methods done) |
| Remaining controllers/models/utilities | **Pending next increments** |

---

## 339.6 Next Increment Plan (Strict Sweep Increment 2)

Next block will process:

1. Remaining controller set (CRUD + process controllers not yet enumerated in detail)
2. `Models/Queries/*` complete pass (`CollectionLists`, `SelectLists`, etc.)
3. Additional model methods with pool-specific and pay-code-specific branching
4. Append file-group checklist (`Reviewed` / `Pending`) for strict traceability

# Part 341: Strict Full Sweep Tracker (Increment 3)

Increment 3 extends strict sweep coverage over additional process-heavy controllers and adds explicit controller progress tracking.

---

## 341.1 Controller Progress Snapshot

| Metric | Value |
|---|---:|
| Total controllers (`StrategicApplications/Controllers/*.cs`) | 116 |
| Reviewed in strict sweep so far | 12 |
| Remaining | 104 |

Reviewed set now includes:

- `AccountController`
- `DailyAssignmentShiftController`
- `DailyCrewPositionController`
- `FillVacancyController`
- `MarkOffRequestController`
- `NotificationController`
- `PayrollController`
- `PayrollReportController`
- `ProcessPayrollController`
- `RailroadPoolController`
- `RailroadPositionBulletinController`
- `SeniorityMoveController`

---

## 341.2 `DailyCrewPositionController` Hard-Coded / Process Findings

### Sentinel and defaults

- Manual crew-position create sets default railroad position control number to sentinel:
  - `99999999999999999`
- Created records from this path are `ExtraBoardOnly = true`.

### Tie-up and vacancy update side effects

- Tie-up flow creates off-duty records for all non-tied-up on-duty records.
- If employee worked and payroll is enabled, it emits manual tie-up notification logic.
- After tie-up: writes vacancy update request and attempts shift completion.

### Release path penalty-claim behavior

When `PenaltyClaim = true` on release:

- creates manual `PayrollRecord`
- hard-codes earning code `44`
- hard-codes `3` hours (`STHours = 03:00`, no OT)
- creates payroll review-required reason text

### Wait-loop synchronization pattern

For `Annul` and `DoNotFill`:

- busy-waits while `MvcApplication.VacancyRecordsProcessing` is true
- polling interval: `Thread.Sleep(250)`

Then recalculates vacancies and attempts shift completion.

---

## 341.3 `PayrollReportController` Findings

- Uses direct ADO.NET stored procedure call (`ACAReport`) instead of EF query.
- Passes current year as `@ReportYear` (`DateTime.Now.Year`).
- Streams CSV output with filename hard-coded as:
  - `AHCA Report.csv`

---

## 341.4 `RailroadPositionBulletinController` Findings

### UI scheduling visibility

- Reads `MvcApplication.nextBulletinUpdates` and displays sentinel year `9999` as “No Automatic Updates Scheduled”.

### Create/assign side effects

- On create, removes pending seniority moves for same position where move effective time is <= bulletin effective datetime.
- For each removed move, emits cancellation notification via move helper.
- After create/assign/delete-style operations, updates bulletin timer via `MvcApplication.SetBulletinTimer(pool)`.

### Interface file coupling

- Bulletin create path explicitly triggers outbound interface creation:
  - `CreateInterfaceFile("Bulletin", "Add")`.

---

## 341.5 `SeniorityMoveController` Findings

### Move type defaults and routing

- `NoAccess` route redirects to `Create` with type `"NA"` and effective date = next day + 1 minute.
- Standard create default type is `"SM"`.

### Craft-specific effective-date behavior in `ExtraBoard`

- `Engineer`: move constrained to bump effective date/work-week logic; may push +7 days.
- `Yardman`/`Yardmaster`: if effective datetime lands exactly midnight, adds +1 minute.

### Timer and messaging side effects

- If move effective time is already in past, may immediately redirect to assignment flow.
- When move-from-hangout immediate bump is performed by same employee, sends Teams `SystemMessage` text.
- Otherwise sets next seniority-move timer via `MvcApplication.SetSeniorityMoveTimer(pool)`.

### Delete helper behavior

- `DeleteSeniorityMove` may send cancellation notification to bumped employee when notify=true.
- Calls `db.SaveChangesAsync()` without awaiting in helper context.

---

## 341.6 Increment 3 Status Update

| Layer | Status |
|---|---|
| Executable services | Complete (increment scope) |
| Runtime/config | Complete (increment scope) |
| Controllers | **Expanded partial (12/116)** |
| Query/model deep logic | Partial |

Next strict increment will continue controller sweep in alphabetical blocks and add per-block reviewed checklist entries.

# Part 342: Strict Full Sweep Tracker (Increment 4)

Increment 4 continues the strict sweep with another process-heavy controller block and additional query-layer coverage.

---

## 342.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 16 |
| Remaining | 100 |

Newly reviewed in this increment:

- `DailyAssignmentController`
- `MarkOffRecordController`
- `RailroadInformationController`
- `PayrollCodeController`

---

## 342.2 `DailyAssignmentController` Findings

### Queue-coupled orchestration

- `CreateCrewPositions` does not directly create every daily crew position in-process; it emits MSMQ `DailyCrewPosition` create messages.

### Pool-specific defaults in manual create flow

- Pool `40` (Mechanical): assignment set from pool assignments; AFE disabled by default.
- Pool `50` (Maintenance of Way):
  - uses pool assignments, not shift assignments
  - hides/forces some request fields for employee-driven path
  - request note default text: manual creation message with employee name
- Other pools: uses shift assignments and default false flags.

### Hard-coded defaults

- Default straight-time hours shown in create form: `8`.
- For newly synthesized shift in create path, if not found, controller creates a `DailyAssignmentShift` record inline.

### Timer side effect

- After create, controller calls `MvcApplication.SetAtHocMessageTimer(dashift.RailroadPoolControlNumber)`.

---

## 342.3 `MarkOffRecordController` Findings

### Create-time pool-specific mark-off datetime behavior

- Pools `10/20/30`:
  - for XB/hangout employees, references shift calling-end windows and may roll to next day +1 minute
  - for assigned crew on off-day, iterates date forward to next non-off-day with +1 minute
- Pool `50` (Maintenance of Way): hard-set mark-off time to `12:01 AM` (`Date + 1 minute`)
- Default branch for other pools also advances off-day crew dates.

### LayedOffOnCall / MissedCall synchronization

- After create with `LayedOffOnCall` or `MissedCall`, controller busy-waits while `MvcApplication.VacancyRecordsProcessing` is true using `Thread.Sleep(1000)` before recalculating vacancies.

### Timer and scheduling visibility

- Index displays `nextRosterBoardMarkOffRecordUpdates`; sentinel year `9999` maps to “No Automatic Updates Scheduled”.

### Vacation relief visibility logic

- AJAX `VacationRelief` endpoint returns showlist only when pool is `40` and mark-off code starts with `V`.

---

## 342.4 `RailroadInformationController` Findings

### Publish/unpublish numbering and sentinel

- New/unpublished records use sentinel `RecordNumber = 999999`.
- Publish assigns sequential record numbers by information type; year prefix logic uses 2-digit year and resets to `YY0001` when needed.

### Role-based visibility

- Non-admin/non-information-admin users only see published records.

### HTML normalization hard-coding

On create/edit description:

- trims trailing slash/r/n characters
- ensures `<p>...</p>` wrapping when missing
- replaces `<div>` with `<p>` and `</div>` with `</p>`
- then HTML-decodes before persistence.

### Publish timer behavior

- If publish date is in future, calls `MvcApplication.SetPublishRailroadInformationTimer(railroad)`.
- If publish date is today/past, immediately notifies employees.

### PDF output behavior

- `View` and `ViewRecords` stream `Railroad Information Report.pdf` directly via `Response.OutputStream`.

---

## 342.5 `PayrollCodeController` Findings

Mostly CRUD-oriented but includes standardization rules:

- payroll code persisted as uppercase (`Code.ToUpper()`) on create and edit.
- supports configurable flags and defaults (`Arbitrary`, `Overtime`, `ApprovalRequired`, `Accumulator`, `CanBeSold`, default time/overtime/amount, compensation type).

---

## 342.6 Query Layer Increment (CollectionLists front section)

Reviewed additional early `CollectionLists` methods and confirmed:

1. Mixed-context query layer includes both:
   - `StrategicApplicationsContext` domain queries
   - `SAClassLibraryContext` query paths for BeSafe/SlowOrder style entities
2. Reuse of hard-coded role GUID remains present in craft approval officer queries.
3. Daily-assignment helper queries include explicit extra-board exclusion and date-window logic.

---

## 342.7 Next Strict Increment

Continue with next controller alphabetic block and then expand query-layer coverage beyond the opening `CollectionLists` section.

# Part 343: Strict Full Sweep Tracker (Increment 5)

Increment 5 continues the alphabetical controller sweep and updates strict coverage metrics.

---

## 343.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 22 |
| Remaining | 94 |

Newly reviewed this increment:

- `AssignmentController`
- `CrewController`
- `EmployeeController`
- `RailroadController`
- `PositionController`
- `RailroadPoolEmployeeController`

---

## 343.2 `AssignmentController` Findings

### Pool-specific selection behavior

- `AssignmentSelect` uses a special assignment source for pool `40` (Mechanical) vs normal pool assignment list for others.

### Mass board-order recalculation endpoint

`SetBoardOrder` recalculates and persists:

1. assignment-level board order (`assignment.SetBoardOrder()`)
2. on-duty-day board order with explicit `AssignmentTypeNumber + LocationBoardOrder + OnDutyTime`
3. future/open daily-assignment board orders

Hard-coded update scope includes future or current daily assignments only (`AssignmentDate >= Today` and shift open).

---

## 343.3 `CrewController` Findings

### Search normalization behavior

- In `Index`, if search string contains `"Relief"`, controller truncates to last character before matching against `CrewID`.

### Pool-specific lineup ordering

- For pool `50` (Maintenance of Way), lineup ordering is forced by `CrewNumber`.
- Standard lineup excludes extra-board-only assignment types and future-effective crews.

### Daily crew report orchestration

- `DailyCrewReport` delegates to `ApplicationUtilities.CreateRailroadPoolCrewPositionHistoryRecords(...)` with requesting user context.

---

## 343.4 `EmployeeController` Findings

### Password initialization hard-code pattern

- New user creation initializes password to employee number (`CreateAsync(user, employee.EmployeeNumber)`).

### Role assignment behavior

- On create/edit, client roles are manipulated by string name matching (`Name.Contains("Client")`).
- `PrimaryRoleID` can trigger additional role-add behavior.

### Status messaging side effects

- On create/edit/status change:
  - if status code `AT` -> sends create employee message
  - otherwise -> sends delete employee message

### Data normalization

- Social security numbers are normalized via non-numeric stripping before persistence.
- Issuing state is uppercased when present.

---

## 343.5 `RailroadController` Findings

### Auto-assignment timer coupling

- On railroad create/edit, controller calls `MvcApplication.CreateTimers()` to rebuild runtime automation timers after save.

### Input normalization

- `RailroadMark` is uppercased on create.

---

## 343.6 `PositionController` Findings

### Must-fill / alternate supervisor orchestration

- Position create/edit supports `MustFill` and optional `PositionAlternateSupervisor` with create/update/remove logic.

### Payroll and assignment defaults

- Persists payroll-linked flags/fields such as:
  - `PayrollCode`
  - `RailroadPayrollDepartmentControlNumber`
  - `TurnoverPay`
  - `AutoAssignVacation`

### Transaction usage

- Create/edit paths use read-committed transaction scope around multi-entity operations (position + optional alternate supervisor).

---

## 343.7 `RailroadPoolEmployeeController` Findings

### Pool-specific bulletin qualification path

- For pool `30` (Clerical), bulletin list uses all bulletins (qualification filter bypassed).
- Other pools use qualified-bulletin query paths.

### Viewed-record side-channel context

- Bulletin viewed record is written using `SAClassLibraryContext` (cross-context write path), not main web context.

### Employee create/edit high-impact behavior

- New user password initialized to employee number.
- Payroll tier assignment is persisted at pool-employee level.
- Edit path can add/remove overtime-board participation via
  `AddToOrRemoveFromDailyShiftOvertimeBoard(...)` if active roster has overtime board.

### Status-change result routing

- `StatusChange` may return control codes (`Create` / `Select`) from seniority update flow and redirect accordingly.

---

## 343.8 Next Strict Increment

Continue controller sweep in the next alphabetical block and then resume deeper `CollectionLists`/`SelectLists` method-region coverage.

# Part 344: Strict Full Sweep Tracker (Increment 6)

Increment 6 continues the strict controller sweep with another mixed block (routing/admin + core CRUD orchestration).

---

## 344.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 28 |
| Remaining | 88 |

Newly reviewed this increment:

- `AdminController`
- `HomeController`
- `DescriptionController`
- `LocationController`
- `ShiftController`
- `WeekDayController`

---

## 344.2 `AdminController` Findings

### App-pool control entry points

- `RestartAppPool` uses `ApplicationUtilities.RestartApplicationPool(MvcApplication.database, user)`.
- `RecycleAppPool` uses `ApplicationUtilities.RecycleApplicationPool(MvcApplication.database, user)`.

This uses runtime database name as pool-name argument source.

### Role-driven redirect map

`LoginRedirect` and `DefaultView` contain explicit role branching with assumptions for single-client/single-railroad deployments (`Count()==1` checks). Multi-tenant branches are marked as TODO comments.

---

## 344.3 `HomeController` Findings

### Forced sign-out behavior on landing

`Index` performs multi-path sign-out before redirecting to `Home`:

1. OWIN app cookie signout
2. Forms auth signout
3. Replaces `HttpContext.User` with blank principal

### Default view role routing

- If user flagged for password-reset condition (`CheckPasswordReset`), redirects to change-password.
- Railroad-employee/union-only users are redirected to profile detail directly.
- Other users route to admin default view with fixed return-url conventions (`/sa/...`).

---

## 344.4 `DescriptionController` Findings

### Input normalization

- Description code is uppercased on create.
- Maintains `EmergencyType` flag through create/edit flow.

### Create-by-code flow

- `Select` action redirects to `Create` with prefilled `code` parameter.

---

## 344.5 `LocationController` Findings

### Assignment side-effect on location edit

When editing a location, controller recalculates board order for all assignments on that location:

- `assignment.BoardOrder = assignment.SetBoardOrder()`

### Data defaults

- `LocationShortName` forced to empty string when null on create.

---

## 344.6 `ShiftController` Findings

### Shift ID and relief-flag are first-class inputs

- CRUD flow persists `ShiftID` (1-char) and `ReliefShift` flag directly.
- No advanced side-effects in controller; primarily persistence + validation handling.

---

## 344.7 `WeekDayController` Findings

### Weekday list management

- CRUD maintains `WeekDayNumber` and `WeekDayName` under client scope.
- Search is prefix-based case-insensitive by weekday name.

---

## 344.8 Next Strict Increment

Continue with the next controller batch (`Address`, `PhoneNumber`, `EmailAddress`, `EmploymentStatus`, and related profile/contact controllers), then update progress metrics again.

# Part 345: Strict Full Sweep Tracker (Increment 7)

Increment 7 continues without stopping and covers employee-profile/contact controllers plus client-level orchestration.

---

## 345.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 34 |
| Remaining | 82 |

Newly reviewed this increment:

- `AddressController`
- `PhoneNumberController`
- `EmailAddressController`
- `EmploymentStatusController`
- `EmployeeDetailController`
- `ClientController`

---

## 345.2 `AddressController` Findings

### Access-control nuance

- Authorization allows broad roles, but controller adds additional ownership checks for railroad-employee-only / union roles.
- Also contains a heuristic bypass tied to requester user last name containing `"Administrator"`.

### Input normalization

- Uses `TextInfo.ToTitleCase` for address/city formatting.
- State is uppercased.
- Optional `Address2` handled as nullable in edit and normalized when present.

---

## 345.3 `PhoneNumberController` Findings

### Normalization and sequencing

- Phone values stored after stripping non-numeric characters.
- `Create` pre-populates `CallingOrder` via `++count`.

### Change propagation side-effect

On create/edit/delete, calls employee side-effect method:

- `employee.PhoneNumberChange(phone, "Create|Edit|Delete")`

This indicates outbound synchronization behavior tied to contact updates.

### Access-control pattern

- Same ownership check pattern as address/email controllers for railroad-employee-only and union roles.

---

## 345.4 `EmailAddressController` Findings

### Change propagation side-effect

On create/edit/delete, calls:

- `employee.EmailAddressChange(email, "Create|Edit|Delete")`

### Type routing

- Email address type selection uses description code bucket `"EM"`.

### Access-control pattern

- Same ownership restriction pattern as address/phone flows.

---

## 345.5 `EmploymentStatusController` Findings

### Status code normalization

- `StatusCode` is uppercased on create and edit.

### Employment code binding

- Persisted status includes separate `EmploymentCode` dimension, selected from fixed list in select list helper.

---

## 345.6 `EmployeeDetailController` Findings

### Notification-first redirect behavior

- `Details` checks each railroad-pool employee context; if active craft has `ShowNotifications` and open notifications exist, redirects directly to `NotificationHistory` before rendering profile.

### External report-server coupling

- `EmployeeCalendar` writes `RailroadEmployeeCalendarRequest` then redirects to fixed SSRS host:
  - `http://sql-svr/ReportServer/...`

### Mark-up and Teams side effect

- Employee self-service mark-up path sends Teams `SystemMessage` after successful mark-up.

### Position history creator-name mapping

Hard-coded creator aliases in history output:

- `admin` -> `Administrator`
- `autoprocess` -> `Assignment Process`

### Tie-up permission rule

For own records, `CanTieUp` derives from:

- `requester.User.OnProperty || rpemployee.TieUpOffProperty`

with additional pool-50 create-record permissive behavior.

---

## 345.7 `ClientController` Findings

### Timer topology coupling

- Client create/edit paths call `MvcApplication.CreateTimers()` after successful save.

### Auto-assignment root setting

- `Client.AutoAssignments` is persisted at client level and surfaced in create/edit.

---

## 345.8 Next Strict Increment

Continue with additional process/matrix-heavy controllers (vacancy/mark-off/payroll-adjacent and bulletin/seniority-adjacent blocks), then update progress again.

# Part 346: Strict Full Sweep Tracker (Increment 8)

Increment 8 continues with payroll-interface and payroll-grouping controllers.

## 346.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 40 |
| Remaining | 76 |

Newly reviewed in this increment:

- `ADPInterfaceController`
- `UKGInterfaceController`
- `PayrollCategoryController`
- `PayrollCategoryCodeController`
- `PayrollReportGroupController`
- `PayrollReportGroupCategoryController`

## 346.2 Interface Controller Findings

### `ADPInterfaceController`

- Adds ADP interface rows by selected ADP column number.
- Uses ADP column select-list helper for fixed import-column mapping.

### `UKGInterfaceController`

- Forces `UKGEarningCode` uppercase on create.
- Uses fixed `ValueType` list (`GetUKGInterfaceColumns`) to define import matching field semantics.

## 346.3 Payroll Grouping Controller Findings

### `PayrollCategoryController`

- Stores reporting flags as three booleans/flags (`STime`, `OTime`, `Amount`) per category.
- Persists `ReportSortNumber` for category ordering in output reports.

### `PayrollCategoryCodeController`

- Manages many-to-many link between payroll categories and payroll codes.
- Composite key delete pattern (`FindAsync(category, id)`) indicates `(PayrollCategoryControlNumber, PayrollCodeControlNumber)` keying.

### `PayrollReportGroupController`

- Maintains top-level report groups with explicit `ReportGroupNumber` and `ReportGroupName`.

### `PayrollReportGroupCategoryController`

- Manages group-to-category relationship table via composite key.
- Uses client-scoped category list when creating link records.

## 346.4 Next Strict Increment

Continue with additional payroll and roster-rule controllers, then continue alphabetical controller sweep until all controller files are covered.

# Part 347: Strict Full Sweep Tracker (Increment 9)

Increment 9 extends coverage across roster/board/rule controllers and core seniority orchestration.

## 347.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 46 |
| Remaining | 70 |

Newly reviewed in this increment:

- `RosterController`
- `RosterBoardController`
- `RosterBoardPositionController`
- `RosterBulletinRuleController`
- `RosterSeniorityMoveRuleController`
- `SeniorityController`

## 347.2 Roster/Board Controller Findings

### `RosterController`

- Persists both `ExtraBoard` and `OvertimeBoard` toggles at roster level.
- Includes `Training` and payroll-department linkage on create/edit.

### `RosterBoardController`

- Uses integer `ExtraBoard` mode field plus flags: `ForceAssign`, `AutoAssign`, `BulletinPositions`, `ApplySeniorityMoveRule`, `ExtendedAbsence`.
- Board behavior configuration is centralized in board CRUD (not computed on save).

### `RosterBoardPositionController`

- Creates backing `RailroadPosition` (`"B"` board type) before creating `RosterBoardPosition`.
- Delete path is logical delete via `DeletedRailroadPosition` record with timestamp/user, not immediate hard removal.

## 347.3 Rule and Seniority Controller Findings

### `RosterBulletinRuleController`

- Maintains bulletin timing fields and `ForcedAssignHours` directly as roster rule configuration.

### `RosterSeniorityMoveRuleController`

- Exposes `RequiredDays`, `RequestHours`, and `CancelHours` as editable roster-level rule inputs.

### `SeniorityController`

- Generates PDF reports and writes text report to hard-coded UNC path `\\sql-svr\sa\Reports\`.
- Edit flow contains explicit state-transition orchestration:
  - active/inactive-cutback transitions trigger assign/unassign side effects
  - removes unassigned moves/bids on deactivation path
- Active seniority changes can trigger craft messaging (`SendCraftMessage`).

## 347.4 Next Strict Increment

Continue with remaining bulletin/seniority-adjacent controllers and then resume general alphabetical sweep until controller folder is fully covered.

# Part 348: Strict Full Sweep Tracker (Increment 10)

Increment 10 continues with bulletin-bid, hold-down, and temporary-assignment orchestration controllers.

## 348.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 52 |
| Remaining | 64 |

Newly reviewed in this increment:

- `RailroadPositionBulletinBidController`
- `RailroadPoolEmployeeBulletinBidController`
- `RailroadPoolEmployeeSeniorityController`
- `RailroadPoolEmployeeSeniorityMoveController`
- `HoldDownController`
- `TemporaryAssignmentController`

## 348.2 Bulletin-Bid and Seniority-Employee Findings

### `RailroadPositionBulletinBidController`

- Pool-specific bid-eligible employee source:
  - pools `30/40`: all pool employees path
  - default: qualified/filtered pool employees path
- Creates bulletin interface file side effects on bid add/delete (`CreateInterfaceFile("Bid", ...)`).

### `RailroadPoolEmployeeBulletinBidController`

- Enforces ownership checks for railroad-employee-only/union users.
- Employee-side bid create/delete is simpler direct CRUD path than central bulletin-bid controller.

### `RailroadPoolEmployeeSeniorityController` and `...SeniorityMoveController`

- Seniority create/select flows activate state and can trigger assignment to positions and craft messaging.
- Employee seniority-move views enforce ownership checks and include roster-selection helper routing.

## 348.3 Hold-Down and Temporary Assignment Findings

### `HoldDownController`

- On create: releases existing open hold-downs for employee before adding new one.
- Yardmaster-specific behavior (pool `20`) creates automatic yardmaster mark-off/mark-up records on hold-down create/release.
- Sends Teams `SystemMessage` on hold-down create/release.
- Release default date is pool-specific:
  - pool `30`: now
  - others: tomorrow `12:01 AM`.

### `TemporaryAssignmentController`

- Pool `50` has special AFE visibility logic; other pools force billable/recollectable defaults false in UI flow.
- Edit flow can:
  - recreate temporary daily assignments
  - release open hold-down records
  - update vacancy records by pool or roster scope
- Assignment/release paths include multi-step transactional orchestration with vacancy recalculation after commit.

## 348.4 Next Strict Increment

Continue with remaining operational controllers (`DailyOnDuty*`, payroll-adjacent helpers, and remaining CRUD controllers) until controller folder is fully covered.

# Part 349: Strict Full Sweep Tracker (Increment 11)

Increment 11 covers the `DailyOnDuty*` billing/material/locomotive controller block.

## 349.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 58 |
| Remaining | 58 |

Newly reviewed in this increment:

- `DailyOnDutyAFEBillingRecordController`
- `DailyOnDutyFlagBillingRecordController`
- `DailyOnDutyLocomotiveRecordController`
- `DailyOnDutyMiscellaneousBillingRecordController`
- `DailyOnDutyRailroadMaterialRecordController`
- `DailyOnDutyZoneBillingRecordController`

## 349.2 Common Pattern Findings for `DailyOnDuty*` Controllers

- All six follow near-identical CRUD orchestration around a parent `DailyCrewPositionOnDutyRecord`.
- Most create/edit paths copy denormalized display fields from selected master entities (AFE/Zone/Material/Locomotive metadata) into child records.
- Errors are logged with controller-specific strings but operational behavior is standardized.

## 349.3 Specific Hard-Coded/Behavior Notes

- `DailyOnDutyLocomotiveRecordController`
  - default create selection prefers `RailroadLocomotiveType.Default`; otherwise reuses last on-duty locomotive type.
  - normalizes locomotive ID/type to uppercase in create/edit paths.
- `DailyOnDutyMiscellaneousBillingRecordController`
  - includes helper endpoint `SetBillableFlag` that derives billable default directly from selected work-code metadata.
- `DailyOnDutyAFE/Zone/Material` controllers
  - capture number/name/description snapshots from source tables on create/edit (not just FK).

## 349.4 Next Strict Increment

Continue with remaining controllers in alphabetical groups until `StrategicApplications/Controllers` is fully covered, then complete query/model sweep and finalize full-codebase status.

# Part 350: Strict Full Sweep Tracker (Increment 12)

Increment 12 reviews payroll-rate/tier/approval-role controller cluster.

## 350.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 64 |
| Remaining | 52 |

Newly reviewed in this increment:

- `PayrollCrewPositionAutoPayController`
- `PayrollCodePayRateController`
- `EngineerPayRateController`
- `PositionPayRateController`
- `RailroadPoolPayrollTierController`
- `PayrollCodeApprovalRoleController`

## 350.2 Payroll Rate/Tier Findings

- `PayrollCodePayRateController` explicitly anchors create/edit selectable positions to pool `10` (`FirstOrDefault(PoolNumber == 10)`) and arbitrary payroll codes.
- `EngineerPayRateController` includes helper endpoint to derive OT rate from ST rate using hard-coded multiplier `1.5` and rounding to 4 decimals.
- `PositionPayRateController` and `EngineerPayRateController` maintain effective-dated rate history by position/job code.

## 350.3 Auto-Pay / Tier / Approval-Role Findings

- `PayrollCrewPositionAutoPayController`
  - builds approval employee candidates from role memberships, excluding selected role names (`Railroad Employee`, dispatcher/timekeeper variants).
  - auto-pay records include toggles (`BasicDay`, `Arbitraries`) plus expiration date.
- `RailroadPoolPayrollTierController`
  - tiering model depends on `(RailroadPool, NumberOfDays, TypeOfDay, RatePercentage)`.
  - day-type options are sourced from fixed select-list helper (`PayrollTierDayTypes`).
- `PayrollCodeApprovalRoleController`
  - uses Identity role join/exclusion logic to prevent duplicate role-to-paycode bindings.
  - stores both `RoleId` and `RoleName` snapshots on linkage records.

## 350.4 Next Strict Increment

Continue with remaining controller files (operations + administrative CRUD) until full controller-folder completion, then proceed to full query/model residual pass.

# Part 351: Strict Full Sweep Tracker (Increment 13)

Increment 13 covers on-duty cutoff/day-time controllers and compensated-day configuration controllers.

## 351.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 70 |
| Remaining | 46 |

Newly reviewed in this increment:

- `OnDutyMoveCutOffTimeController`
- `AssignmentOnDutyTimeController`
- `AssignmentOnDutyDayController`
- `CrewOffDayController`
- `CraftPersonalDaysController`
- `CraftSickDaysController`

## 351.2 On-Duty/Cutoff Findings

- `OnDutyMoveCutOffTimeController` manages per-craft cut-off values scoped to a specific on-duty time entry.
- `AssignmentOnDutyTimeController` persists shift + on-duty + calling window tuples for pools.
- `AssignmentOnDutyDayController` computes and stores board-order for each assignment day using assignment type/location/on-duty time input.

## 351.3 Off-Day and Compensated-Day Rule Findings

- `CrewOffDayController`
  - supports reverse day ordering when crew `AddCrewOffDayValues == 8`.
  - uses composite-key delete for off-day entries `(CrewControlNumber, WeekDayControlNumber)`.
- `CraftPersonalDaysController` and `CraftSickDaysController`
  - both map service-year thresholds to entitlement day counts.
  - both are admin/HR-managed rule tables with simple create/delete flows.

## 351.4 Next Strict Increment

Continue with remaining craft/roster/payroll-support controllers and then move to full residual query/model pass after controller-folder completion.

# Part 352: Strict Full Sweep Tracker (Increment 14)

Increment 14 covers mark-off code/rules and vacation-request waitlist controllers.

## 352.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 76 |
| Remaining | 40 |

Newly reviewed in this increment:

- `CraftVacationDaysController`
- `CraftApprovalOfficerController`
- `CraftMarkOffCodeController`
- `MarkOffCodeController`
- `MarkOffRequestWaitListController`
- `RailroadEmployeeVacationRequestController`

## 352.2 Mark-Off/Craft Rule Findings

- `CraftVacationDaysController` mirrors personal/sick-day service-year entitlement table pattern.
- `CraftApprovalOfficerController` maintains craft-level approver assignment with `Primary` flag.
- `CraftMarkOffCodeController` supports craft-level per-code overrides:
  - `Exclude`
  - `ApprovalRequired`
  - `AutomaticMarkUpHours`
  and can toggle craft-wide approval-required state from index route.

## 352.3 `MarkOffCodeController` and Vacation Request Findings

- `MarkOffCodeController` uses `SAClassLibraryContext` for base mark-off-code CRUD and then bridges to `StrategicApplicationsContext` for payroll/approval child-link operations.
- Non-system-admin users are filtered from `SystemUseOnly` mark-off codes on index.
- Create sets default `ReportCode = "O"`.

- `MarkOffRequestWaitListController`
  - includes sentinel-based vacation-week UI behavior (`99999999999999999` pathways).
  - supports approval flow that may convert waitlist record into full mark-off request with officer assignment.

- `RailroadEmployeeVacationRequestController`
  - uses next-year planning model (`DateTime.Today.AddYears(1)`) with split/choice sequencing.
  - one-day vacation weeks tracked as hours (`weeks * 40`) in dedicated one-day-time table.
  - delete flow renumbers later choice numbers within split to keep contiguous ordering.

## 352.4 Next Strict Increment

Continue with remaining controllers (qualification/requirement/material/railroad metadata and report helpers), then finalize controller-folder completion and start residual query/model sweep.

# Part 353: Strict Full Sweep Tracker (Increment 15)

Increment 15 covers qualification and requirement-assignment controller family.

## 353.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 82 |
| Remaining | 34 |

Newly reviewed in this increment:

- `QualificationController`
- `PositionRequirementEmployeeController`
- `CraftRequirementEmployeeController`
- `ClientRequirementEmployeeController`
- `RailroadRequirementEmployeeController`
- `RailroadPoolRequirementEmployeeController`

## 353.2 Qualification/Requirement Common Findings

- Requirement-assignment controllers share a consistent lifecycle:
  - list grouped by employee (latest record retained)
  - create for employee
  - renew for employee/date
  - delete record
- Most index lists exclude terminated status code `XE` and sort by renewal/completion date then employee name.

## 353.3 Specific Notes

- `QualificationController`
  - assigns employee qualifications to positions with effective date.
  - create UI uses unassigned-qualification employee list scoped by roster/position.

- `RailroadPoolRequirementEmployeeController`
  - supports numeric vs text search branches and optional craft filter on requirement index.
  - filters out inactive/removed pool-employee states before grouping.

## 353.4 Next Strict Increment

Continue with remaining railroad metadata/material/zone/AFE controllers and any unresolved operational controllers, then complete full controller-folder coverage.

# Part 354: Strict Full Sweep Tracker (Increment 16)

Increment 16 covers railroad metadata controllers for AFE/material/zone/workcode/location.

## 354.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 88 |
| Remaining | 28 |

Newly reviewed in this increment:

- `RailroadAFEController`
- `RailroadMaterialCategoryController`
- `RailroadMaterialController`
- `RailroadZoneController`
- `RailroadWorkCodeController`
- `RailroadLocationController`

## 354.2 Metadata Controller Findings

- `RailroadAFEController` uppercases AFE number on create/edit and maintains number+description dictionary.
- `RailroadMaterialCategoryController` manages category-number/name taxonomy per railroad.
- `RailroadMaterialController` stores unit indicator uppercased and maps material to selected category.

- `RailroadZoneController` and `RailroadLocationController`
  - auto-suggest next number via `last number + 10` pattern.
  - persist number/name metadata pairs.

- `RailroadWorkCodeController`
  - also auto-suggests next code number via `+10` pattern.
  - includes billable flag (`BillableCode`) used by billing flows.

## 354.3 Next Strict Increment

Continue with remaining controller files (reports/print/helpers and leftover operational controllers) to finish full controller-folder coverage.

# Part 355: Strict Full Sweep Tracker (Increment 17)

Increment 17 reviews report/print helpers plus locomotive and payroll department metadata controllers.

## 355.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 94 |
| Remaining | 22 |

Newly reviewed in this increment:

- `PrintPDFController`
- `DailyReportController`
- `PayrollReportController` (rechecked)
- `LocomotiveInspectionRecordController`
- `RailroadLocomotiveTypeController`
- `RailroadPayrollDepartmentController`

## 355.2 Reporting/Print Findings

- `PrintPDFController` generates PDF view output from seniority selection model using custom `PdfViewController` pipeline.
- `DailyReportController.CovidReport` logs report view events in `SAClassLibraryContext` and redirects to fixed SSRS URL on `sql-svr`.
- `PayrollReportController` remains direct stored-procedure CSV export (`ACAReport`) with output filename `AHCA Report.csv`.

## 355.3 Locomotive/Payroll-Department Findings

- `LocomotiveInspectionRecordController`
  - creates/edits inspection data tied to daily on-duty locomotive records.
  - uppercases locomotive ID on create path.

- `RailroadLocomotiveTypeController`
  - uppercases locomotive type values.
  - when setting one type as `Default`, explicitly clears `Default` from all other locomotive types.

- `RailroadPayrollDepartmentController`
  - CRUD for `DepartmentName`, `ICCNumber`, `DepartmentNumber`, `GeneralLedgerNumber`.
  - contains a `Details` action that resolves `Position` instead of payroll department (likely legacy mismatch).

## 355.4 Next Strict Increment

Continue with remaining unresolved controllers until `StrategicApplications/Controllers` reaches full coverage, then proceed to residual query/model/utilities full-pass completion.

# Part 356: Strict Full Sweep Tracker (Increment 18)

Increment 18 covers assignment/craft/crew structural controllers.

## 356.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 100 |
| Remaining | 16 |

Newly reviewed in this increment:

- `AssignmentAbolishmentController`
- `AssignmentTypeController`
- `CraftController`
- `CrewAbolishmentController`
- `CrewPositionController`
- `CrewPositionAlternatePositionController`

## 356.2 Structural Controller Findings

- `AssignmentAbolishmentController`
  - removes future open daily assignments after abolishment date.
  - strips relief-crew on-duty-day links during abolishment handling.

- `AssignmentTypeController`
  - edit operation recalculates board order on all assignments under the type.

- `CraftController`
  - central craft behavior toggles include payroll, rest, vacation-assignment type, notification visibility, and mark-off policies.

- `CrewAbolishmentController`
  - performs heavy transactional cleanup: unassigns employees, removes bulletins/moves, releases hold-downs, and prunes future daily assignment crews.

- `CrewPositionController`
  - create path builds `RailroadPosition` + `CrewPosition`, auto-bulletins position, and may inject matching daily crew positions.
  - delete path performs soft delete via `DeletedRailroadPosition` after unassign/release/cleanup operations.

- `CrewPositionAlternatePositionController`
  - manages weekday-based alternate position mapping via composite key `(RailroadPositionControlNumber, WeekDayControlNumber)`.

## 356.3 Next Strict Increment

Continue with the final unresolved controller set (`BeSafe*`, `SlowOrder*`, employee/railroad residual controllers, and tie-up/workday controllers) to complete controller-folder coverage.

# Part 357: Strict Full Sweep Tracker (Increment 19)

Increment 19 covers `BeSafe*` and `SlowOrder` operational/reporting controllers.

## 357.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 106 |
| Remaining | 10 |

Newly reviewed in this increment:

- `BeSafeController`
- `BeSafeAreaController`
- `BeSafeCategoryController`
- `BeSafeEmailGroupController`
- `BeSafeSubdivisionController`
- `SlowOrderController`

## 357.2 BeSafe Findings

- `BeSafeController` is a high-orchestration controller with:
  - open/closed history filtering
  - employee/area/category filter composition
  - action records + notification side effects
  - PDF export for records/actions.

- BeSafe numbering pattern for new records:
  - record number seeded/reset by 2-digit year prefix (`YY0001`) when year changes.

- `BeSafeArea/Category/EmailGroup/Subdivision` controllers
  - mostly metadata CRUD in `SAClassLibraryContext`.
  - several entities use suggested next-number convention (`last + 10`).

## 357.3 Slow Order Findings

- `SlowOrderController` includes:
  - open/closed filtering with day-window history
  - change-record history tracking on edit
  - complete/delete modeled via companion records
  - PDF rendering for single record, change record, or filtered record sets.

- PDF output contains hard-coded footer/signature content for operations report branding.

## 357.4 Next Strict Increment

Cover final unresolved controllers (`SlowOrderArea`, railroad-employee residuals, tie-up/workday, requirement root controller, and training/qualification residuals) to finish complete controller-folder review.

# Part 358: Strict Full Sweep Tracker (Increment 20)

Increment 20 covers railroad-employee residual and qualification/tie-up controller block.

## 358.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 111 |
| Remaining | 5 |

Newly reviewed in this increment:

- `DailyOnDutyRecordTieUpController`
- `EngineerJobCodeController`
- `RailroadEmployeeCompensableTimeRecordController`
- `RailroadEmployeeController`
- `RailroadInformationTypeController`
- `RailroadPoolEmployeeQualificationController`

## 358.2 High-Impact Findings

- `DailyOnDutyRecordTieUpController`
  - contains heavy pool-specific tie-up/payroll branching (including mechanical/clerical/MoW/engineer logic).
  - embeds hard-coded clerical pay-grade mappings and various payroll meal/claim decision rules.

- `EngineerJobCodeController`
  - maintains effective engineer pay-class mapping to max locomotive weight-on-drivers.
  - delete is soft-style through `EngineerJobCodeDelete` record rather than immediate hard remove.

- `RailroadEmployeeCompensableTimeRecordController`
  - orchestrates compensable-time bulk loads and adjustments with role-gated special modes (admin-only sentinel values).
  - includes PDF earning summary export and year-offset reporting model.

- `RailroadEmployeeController` / `RailroadInformationTypeController` / `RailroadPoolEmployeeQualificationController`
  - continue established patterns: role updates with Identity, metadata CRUD with numbering defaults, and qualification assignment by effective date.

## 358.3 Next Strict Increment

Finish the final 5 unresolved controllers, then mark controller-folder sweep complete and continue residual query/model/utilities full-pass.

# Part 359: Strict Full Sweep Tracker (Increment 21 - Controller Completion)

Increment 21 completes the final unresolved controllers and closes controller-folder coverage.

## 359.1 Controller Progress Update

| Metric | Value |
|---|---:|
| Total controllers | 116 |
| Reviewed so far | 116 |
| Remaining | 0 |

Final controllers reviewed in this increment:

- `RailroadPoolEmployeeTrainingDateController`
- `RailroadPositionController`
- `RequirementController`
- `SlowOrderAreaController`
- `TemporaryAssignmentWorkDayController .cs`

## 359.2 Final Controller Findings

- `RailroadPoolEmployeeTrainingDateController`
  - training-date CRUD is coupled to daily crew-position on-duty record creation/removal and vacancy recalculation.
  - defaults training date from latest qualification/future training chronology.

- `RailroadPositionController`
  - high-impact assignment/unassignment/undo orchestration with bulletins, hold-down release, seniority-move cleanup, and hangout reassignment.

- `RequirementController`
  - root requirement matrix controller; aggregates inherited requirement scopes across Client/Railroad/Pool/Craft/Position.
  - create/select flows fan out to scope-specific link tables.

- `SlowOrderAreaController`
  - metadata CRUD with `last + 10` numbering convention.

- `TemporaryAssignmentWorkDayController .cs`
  - weekday changes trigger temporary daily-assignment regeneration and vacancy updates.
  - note: filename includes trailing space before `.cs` in repository path.

## 359.3 Controller Sweep Status

Controller-layer strict sweep is now complete (`116/116`).

## 359.4 Next Strict Phase

Proceed to residual full-codebase pass outside controllers:

1. complete remaining `Models/Queries` deep regions (`CollectionLists`, `SelectLists`, `Collections`)
2. complete model/service utility side-effect mapping (`Models`, `Utilities`, timer hooks, queue/file integrations)
3. finalize comprehensive completion checklist for full codebase coverage.

# Part 360: Strict Full Sweep Tracker (Increment 22 - Queries + Utilities Deep Pass)

Increment 22 completes full-file review of core query helpers and utility orchestration hubs.

## 360.1 Layer Progress Snapshot

| Layer | Status |
|---|---|
| Controllers | **Complete (116/116)** |
| `Models/Queries` (`CollectionLists`, `SelectLists`) | **Complete (deep pass)** |
| `Utilities` core files | **Expanded deep pass complete** |
| Remaining model/entity files and other folders | Pending next increments |

Files fully reviewed this increment:

- `Models/Queries/CollectionLists.cs` (full 2,941 lines)
- `Models/Queries/SelectLists.cs` (full 2,136 lines)
- `Utilities/ApplicationUtilities.cs`
- `Utilities/PayrollUtilities.cs`
- `Utilities/DateTimeUtilities.cs`
- `Utilities/EventLogger.cs`
- `Utilities/FileUtilities.cs`
- `Utilities/StringUtilities.cs`

## 360.2 High-Impact Query/Utility Findings

- `CollectionLists` centralizes extensive business rule filtering with repeated hard-coded code/value semantics (status codes, pool numbers, payroll periods, board IDs, sentinel IDs).
- `SelectLists` mirrors this with many fixed UI vocabularies and hard-coded role/board/payroll option sets.
- `ApplicationUtilities` is a primary orchestration hub for:
  - transaction policy factory
  - Teams webhook messaging
  - vacancy recalculation engine
  - file-queue command generation
  - vacation assignment processing.
- `PayrollUtilities` contains ADP/UKG integration pipelines with fixed network paths and format-specific branching.
- Utility layer includes low-level infrastructure couplings:
  - Windows Event Log source writes
  - direct file lock polling loops
  - network share export/report destinations.

## 360.3 Next Strict Increment

Proceed into remaining `Models` entity/domain files and other residual folders, then publish the final full-codebase completion checklist.

# Part 361: Strict Full Sweep Tracker (Increment 23 - Core Domain Model Deep Dive)

Increment 23 focuses on high-impact domain entities driving payroll, tie-up, assignment, and mark-off behavior.

## 361.1 Model Files Deep-Reviewed (high-impact)

- `Models/DailyCrewPositionOnDutyRecord.cs`
- `Models/RailroadPoolEmployee.cs`
- `Models/RailroadPosition.cs`
- `Models/PayrollRecord.cs`
- `Models/MarkOffRequestRecord.cs`
- `Models/Seniority.cs`

## 361.2 High-Impact Findings

- `DailyCrewPositionOnDutyRecord`
  - acts as the operational nexus for tie-up/payroll creation, mark-off propagation, FRA/rest checks, overtime/meal handling, and board-order updates.
  - contains extensive pool/craft-specific payroll branching and direct side-effects (Teams messages, vacancy updates, review-required records).

- `RailroadPoolEmployee`
  - central aggregate root for status/position/seniority/mark-off workflows.
  - includes broad orchestration methods for assignment transitions, seniority inactivation, open-record completion, holiday qualification, and overtime board management.

- `RailroadPosition`
  - encapsulates assignment/unassignment orchestration, bulletin lifecycle, daily crew-position creation/removal, and change-notification generation.
  - assignment behavior is highly coupled to board/crew type, cutoff-time checks, and hangout semantics.

- `PayrollRecord`
  - calculates rates via pool/craft/job-code matrix with hard-coded job parsing rules and overtime logic.
  - owns earning creation + review/approval-required record generation and compensation-account debit behavior.

- `MarkOffRequestRecord`
  - orchestrates conversion from request/waitlist into actual mark-off records with vacation-week capacity checks and split-week handling.
  - includes auto-markup behavior and vacancy timer side effects.

- `Seniority`
  - create/state transitions directly trigger position assignment/unassignment and craft messaging.

## 361.3 Next Strict Increment

Continue model-layer sweep for remaining entities not yet deep-reviewed, then finalize the full-codebase completion checklist across all folders.

# Part 362: Strict Full Sweep Tracker (Increment 24 - Operational Model Deep Dive II)

Increment 24 extends deep review into operational scheduling/absence/bidding entities.

## 362.1 Model Files Deep-Reviewed

- `Models/MarkOffRecord.cs`
- `Models/DailyCrewPosition.cs`
- `Models/DailyAssignmentShift.cs`
- `Models/HoldDown.cs`
- `Models/SeniorityMove.cs`
- `Models/RailroadPositionBulletin.cs`

## 362.2 High-Impact Findings

- `MarkOffRecord`
  - is the primary absence transaction orchestrator: create/change/delete/markup plus compensation-hour accounting, daily markoff projection, vacancy side effects, and interface-file generation.
  - includes substantial branch complexity around compensated codes (`CD/PD/SD/VD/V1..V5`), waitlist/request linkage, and auto-markup behavior.

- `DailyCrewPosition`
  - is the daily vacancy seat model coordinating on-duty record creation/updates, payroll earning code selection, moved-position tracking, hold-down/temporary assignment propagation, and shift-completion triggers.

- `DailyAssignmentShift`
  - is the call-sheet root aggregate responsible for creating daily assignments, temporary assignments, extra/overtime boards, and completion gating.

- `HoldDown`
  - encapsulates hold-down lifecycle and recursive release/propagation behavior that reassigns daily positions and updates extra-board ordering.

- `SeniorityMove`
  - controls bump/assignment transactions, cancellation notifications, and conflict resolution against other pending moves by seniority ordering.

- `RailroadPositionBulletin`
  - manages bulletin lifecycle (open/close/effective windows), bid assignment, no-bid/force-assign behavior, and downstream position assignment integration.

## 362.3 Next Strict Increment

Continue remaining model/entity folders (`ModelViews`, residual support entities, and cross-project service models) to complete the full-codebase sweep checklist.

# Part 363: Strict Full Sweep Tracker (Increment 25 - ModelViews Deep Pass I)

Increment 25 begins deep pass on high-impact view-model composition files.

## 363.1 ModelView Files Reviewed

- `Models/ModelViews/PayrollViews.cs`
- `Models/ModelViews/DailyOnDutyRecordTieUpViews.cs`
- `Models/ModelViews/MarkOffRecordViews.cs`
- `Models/ModelViews/EmployeeDetailViews.cs`
- `Models/ModelViews/DailyAssignmentShiftViews.cs`
- `Models/ModelViews/NotificationViews.cs`

## 363.2 High-Impact Findings

- `PayrollViews`
  - consolidates payroll record/earnings/holiday/review screens and summary counters.
  - embeds business-display decisions (approval/review/processed state derivation and role-dependent selection sets).

- `DailyOnDutyRecordTieUpViews`
  - captures tie-up, payroll, FRA-certification, locomotive/MoW billing, and validation rules in one large view-model family.
  - validation includes pool/craft-specific boundary rules and reason-required enforcement.

- `MarkOffRecordViews`
  - models create/edit/markup workflows with compensation-day counters and markoff metadata projection.

- `EmployeeDetailViews`
  - aggregates cross-domain employee profile details (seniority, paid/work day metrics, payroll history, notification history).
  - includes validation for overtime-call toggling based on current markoff/worked-shift conditions.

- `DailyAssignmentShiftViews`
  - coordinates call-sheet/board creation and completion actions via strongly typed selection/operation models.

- `NotificationViews`
  - structures notification queue/detail/attempt workflows with roster filtering and confirmation metadata.

## 363.3 Next Strict Increment

Continue remaining `ModelViews` and residual `NewModelViews`/service-model folders, then produce final checklist status for full codebase sweep completion.

# Part 364: Strict Full Sweep Tracker (Increment 26 - `NewModelViews` Completion)

Increment 26 completes `StrategicApplications/NewModelViews` coverage.

## 364.1 `NewModelViews` Files Reviewed

- `BeSafeViews.cs`
- `BeSafeAreaViews.cs`
- `BeSafeCategoryViews.cs`
- `BeSafeEmailGroupViews.cs`
- `BeSafeSubdivisionViews.cs`
- `SlowOrderViews.cs`
- `SlowOrderAreaViews.cs`
- `MarkOffCodeViews.cs`
- `RailroadInformationViews.cs`
- `RailroadInformationTypeViews.cs`

## 364.2 Findings

- BeSafe/SlowOrder `NewModelViews` emphasize record lifecycle UX states (`open/scheduled/resolved/closed`) and document-centric CRUD projections.
- `MarkOffCodeViews` carries legacy markoff/payroll linkage edit shapes (approval officers, markoff payroll codes, request/holiday flags).
- `RailroadInformation*Views` encapsulate publish/cancel/close workflow models and status staging for bulletin-style railroad communications.

## 364.3 Layer Status Update

- `Controllers`: complete
- `Models/Queries`: complete deep pass
- `Utilities`: complete deep pass
- `Models` high-impact entities: in progress (expanded)
- `Models/ModelViews`: in progress (expanded)
- `NewModelViews`: complete

## 364.4 Next Strict Increment

Continue residual `Models/ModelViews` files not yet deep-reviewed, then cover remaining cross-project service/model folders for final full-codebase completion checklist.

# Part 365: Strict Full Sweep Tracker (Increment 27 - ModelViews Deep Pass II)

Increment 27 extends `Models/ModelViews` coverage into identity, employee, assignment, crew, and position composition models.

## 365.1 ModelView Files Reviewed

- `Models/ModelViews/AccountViews.cs`
- `Models/ModelViews/AddressViews.cs`
- `Models/ModelViews/EmployeeViews.cs`
- `Models/ModelViews/AssignmentViews.cs`
- `Models/ModelViews/CrewViews.cs`
- `Models/ModelViews/PositionViews.cs`

## 365.2 Findings

- `AccountViews`
  - contains user/role/password edit models and role-selection projection (`IdentityRole`-backed) for account administration.

- `EmployeeViews`
  - central HR-style edit/create/detail payloads including employment status, user-role binding, overtime/payroll flags, and service-credit workflows.

- `AssignmentViews` + `CrewViews` + `PositionViews`
  - model core dispatch topology edits (assignment timing/location/type, crew setup, position pay/bulletin flags) and pool-dependent code formatting.

- `AddressViews`
  - straightforward address CRUD projection with description-type binding.

## 365.3 Next Strict Increment

Continue remaining `Models/ModelViews` files (craft/roster/pay code/qualification/seniority-move and similar) to complete this layer before final cross-project wrap-up.

# Part 366: Strict Full Sweep Tracker (Increment 28 - ModelViews Deep Pass III)

Increment 28 continues `Models/ModelViews` with craft/roster/paycode/seniority families.

## 366.1 ModelView Files Reviewed

- `Models/ModelViews/CraftViews.cs`
- `Models/ModelViews/RosterViews.cs`
- `Models/ModelViews/PayrollCodeViews.cs`
- `Models/ModelViews/QualificationViews.cs`
- `Models/ModelViews/SeniorityMoveViews.cs`
- `Models/ModelViews/RailroadPoolEmployeeViews.cs`

## 366.2 Findings

- `CraftViews` + `RosterViews`
  - define admin edit payloads for craft/roster policy flags that influence bulletin, seniority move, rest/payroll, and vacation behavior.

- `PayrollCodeViews`
  - models payroll code metadata management including accumulator/approval/compensation flags and ADP/UKG mappings.

- `QualificationViews`
  - encapsulates position qualification assignment lifecycle and effective-date controls.

- `SeniorityMoveViews` + `RailroadPoolEmployeeViews`
  - expose move/assignment workflows and broad railroad-pool employee state projections used across dispatch/operator screens.

## 366.3 Next Strict Increment

Finish remaining `Models/ModelViews` files not yet deep-reviewed, then finalize residual cross-project model/service folders and publish full completion checklist.

# Part 367: Strict Full Sweep Tracker (Increment 29 - ModelViews Deep Pass IV)

Increment 29 continues scheduling/topology-oriented `ModelViews` coverage.

## 367.1 ModelView Files Reviewed

- `Models/ModelViews/AssignmentTypeViews.cs`
- `Models/ModelViews/CrewAssignmentViews.cs`
- `Models/ModelViews/CrewOffDayViews.cs`
- `Models/ModelViews/CrewPositionViews.cs`
- `Models/ModelViews/CrewPositionAlternatePositionViews.cs`
- `Models/ModelViews/DailyAssignmentViews.cs`

## 367.2 Findings

- `AssignmentTypeViews`
  - exposes assignment-type policy flags (`AirPay`, `ExtraBoardOnly`) that drive downstream assignment creation behavior.

- `CrewAssignment/CrewOffDay/CrewPosition*Views`
  - define crew composition and day/position mapping screens (including alternate-position-per-weekday behavior).
  - `CrewPositionViews` also projects assignment/bulletin/vacancy status and deletion state.

- `DailyAssignmentViews`
  - provides daily-assignment operational models for create/delete/annul/move/notes flows and includes validation for manual on-duty record creation state.

## 367.3 Next Strict Increment

Continue remaining unreviewed `Models/ModelViews` files (ADP/engineering/billing/railroad-pool/seniority-move adjunct sets), then close out residual cross-project folders for final checklist publication.

# Part 368: Strict Full Sweep Tracker (Increment 30 - ModelViews Deep Pass V)

Increment 30 covers payroll-interface and engineering/payrate-oriented `ModelViews`.

## 368.1 ModelView Files Reviewed

- `Models/ModelViews/ADPInterfaceViews.cs`
- `Models/ModelViews/EngineerJobCodeViews.cs`
- `Models/ModelViews/EngineerPayRateViews.cs`
- `Models/ModelViews/PayrollCategoryViews.cs`
- `Models/ModelViews/PayrollCategoryCodeViews.cs`
- `Models/ModelViews/PayrollCodePayRateViews.cs`

## 368.2 Findings

- `ADPInterfaceViews`
  - models payroll-code-to-ADP-column mappings and lifecycle operations for export configuration.

- `EngineerJobCodeViews` + `EngineerPayRateViews`
  - define engineer/trainee pay class metadata and effective-date pay rate maintenance.

- `PayrollCategoryViews` + `PayrollCategoryCodeViews`
  - represent payroll reporting categories and included payroll-code memberships.

- `PayrollCodePayRateViews`
  - captures position/paycode-specific pay-rate schedules with effective dating.

## 368.3 Next Strict Increment

Continue remaining unreviewed `Models/ModelViews` files, then start residual cross-project folders (`SADailyCallSheetService`, `SAClassLibrary` adjuncts) to finalize the end-to-end sweep checklist.

This keeps the sweep deterministic and avoids long single-pass failure/stall behavior.

# Part 340: Strict Full Sweep Tracker (Increment 2)

This increment extends the strict sweep into additional controller orchestration and query-layer hard-coded logic.

---

## 340.1 Additional Controller Sweep (Increment 2)

### `FillVacancyController`

#### Board selection matrix (controller route + query source)

`Select(board)` routes to fixed board IDs:

| Board ID | Label | Source |
|---:|---|---|
| 0 | Same Assignment | on-duty records on same daily assignment |
| 1 | Extra Board | `GetAvailableDailyExtraBoardEmployees` |
| 2 | Off Day Board | `GetActiveOffDaySeniorityList` |
| 3 | Seniority Board (default) | `GetAvailableEmployeesInSeniorityOrder` |
| 4 | Overtime Board | `DailyShiftOvertimeBoardPositions` |
| 5 | Vacation Relief Board | `CrewPositions.Where(VacationRelief)` |
| 6 | Qualified Employee Board | `Qualifications.Where(PositionControlNumber)` |

Additional hard-coded behavior:

- Late call flag is set when `DateTime.Now > vacancy.EndCallTime` for non-board-0 flows.
- On acceptance with late call, creates `DailyCrewPositionOnDutyRecordLateCall` with `Confirmed=false` and `ArrivalDateTime = scheduled on-duty datetime`.

### `RailroadPoolController`

Increment-2 focus findings:

- `AssignVacationWeeks` delegates full processing to `ApplicationUtilities.AssignRailroadPoolVacationRequests(...)`.
- Pool create/edit operations call `MvcApplication.CreateTimers()` after persistence, meaning runtime timer topology is rebuilt on pool configuration changes.

### `PayrollController`

Hard-coded job/pool formatting in create flow:

- Pool-dependent job string construction varies by pool numbers `10/20/30/40/50/60`.
- Pool 10 label substitution: `"Yard and Enginemen" -> "T&E"` in display contexts.
- Pool 50 label substitution: prefixed as `"MofW - ..."` in assignment/position display.
- Explicit injection of legacy Yardman job codes if absent:
  - `100H`, `100F`, `101H`, `101F`

Fallback behavior:

- Missing payroll department -> fields set to string literals: `"Not Found"`.

### `NotificationController`

Increment-2 findings:

- Notification acceptance creates an automatic electronic confirmation note text with timestamp and employee name.
- Notification history/detail rendering resolves creator username via employee lookup; falls back to literal `"Administrator"` for `CreatedBy == "admin"`.

---

## 340.2 Query Layer Sweep (Increment 2)

### `SelectLists.GetVacancyBoards(pool)` hard-coded pool matrix

| Pool | Boards Returned |
|---:|---|
| 10 | 0,1,2,3 |
| 20 | 1,4,3 |
| 30 | 1,4,3 |
| 40 | 4,3,5 |
| 50 | 6,3 |
| 60 | 3 |

### Sentinel select-list value

`GetCraftWaitListMarkOffCodes` appends fixed sentinel option:

- Text: `VW - Vacation Weeks`
- Value: `99999999999999999`

### Legacy role-id hard-coding in `Collections.cs`

`GetCraftApprovalOfficers` and `GetUnassignedCraftApprovalOfficers` filter role membership by fixed GUID:

- `1d78b8ea-f36b-42a7-91fa-325f543aa2e9`

This duplicates role identity in code rather than resolving by role name.

---

## 340.3 Strict Sweep Status After Increment 2

| Layer | Status |
|---|---|
| Executable service logic | Complete (increment scope) |
| Config and runtime orchestration | Complete (increment scope) |
| Controller layer | **Expanded partial** (major process controllers advanced) |
| Query layer | **Started** (`SelectLists`, `CollectionLists`, `Collections` high-impact sections) |
| Deep model methods | Partial |
| Remaining controller/model/query files | Pending next increments |

---

## 340.4 Planned Increment 3 (next strict block)

1. Remaining process-heavy controllers:
   - `DailyCrewPositionController`
   - `PayrollReportController`
   - `RailroadPositionBulletinController`
   - `SeniorityMoveController`
2. Full pass on `CollectionLists` and `SelectLists` remaining regions
3. Add reviewed file-group checklist with explicit `Completed/Remaining` markers for controller and query folders

# Part 341: Strict Full Sweep Tracker (Increment 3 - Process + Import + Runtime Hard-Coded Logic)

This increment compares additional high-impact runtime/process files against the spec and captures fixed branching, sentinel values, path dependencies, and processing order.

## 341.1 Files Reviewed in this Increment

- `StrategicApplications/Controllers/ProcessPayrollController.cs`
- `SAImportPayrollService/Services/SAImportADPPayrollService.cs`
- `SAImportPayrollService/Services/SAImportUKGPayrollService.cs`
- `SADailyCallSheetService/Services/SADailyCallSheetService.cs`
- `SAAtHocMessageService/Services/SAAssignmentOnDutyService.cs`
- `SAAtHocMessageService/Services/SAAssignmentCallService.cs`
- `StrategicApplications/Services/AtHocService.cs`
- `StrategicApplications/Global.asax.cs`
- `RestartApplicationPool/Program.cs`

## 341.2 `ProcessPayrollController` - Additional Detailed Rules

### Pay period/date normalization

- Pay period is composed as `{MMDD}{yy}` from UI period + current year.
- Special carry-back rule: if computed period contains `1216`, year is forced to `DateTime.Today.AddMonths(-1).Year`.
- Base pay date is `{year}-{month}-15 23:59:59`.
- If day segment is `16`, pay date is converted to end-of-month `23:59:59`.

### Trial vs Final process branches

- Final process pulls records where:
  - `PayrollRecordDelete == null`
  - `PayrollDate <= paydate`
  - at least one earning has `PayrollEarningProcessedRecord.FinalProcess == false`
- Trial process removes an existing non-final process record for the same period before rebuilding.
- Trial validation explicitly rejects:
  - earnings with zero time and zero amount,
  - earnings requiring approval but lacking approval/declination record,
  - payroll records with unresolved review-required flags.

### Identity repair hard-coding in trial mode

- For each payroll record, controller cross-checks and force-corrects:
  - `EmployeeControlNumber`
  - `RailroadEmployeeControlNumber`
  - `RailroadPoolEmployeeControlNumber`
- Mismatch rows are appended to `badpayrollrecords.log` with old/new identities and job/date fields.

### Monthly incentive hard-coded earning creation

`CreateMonthlyPayRecords` uses static in-memory worklists (`RPEmployees`, `Records`) and creates synthetic earnings:

- Safety incentive:
  - payroll code fixed to `49`
  - amount from `viewModel.ProcessSafety`
  - skips employees whose month records only contain code `21` (time claim)
- Gloves incentive:
  - Yardman craft only
  - payroll code fixed to `63`
  - amount fixed to `3`
- Synthetic payroll record defaults include:
  - `JobWorked = JobPaid = "S" + DepartmentNumber.Substring(1)`
  - `ManualEntry = false`
  - department fallback literals: `"Not Found"`

### Archive/report path hard-coding

The process writes and/or copies logs, exports, and generated PDFs using fixed UNC roots:

- `\\Finance-svr\Payroll Exports\UKG\History\{payperiod}\`
- `\\Finance-svr\Payroll Exports\UKG\History\{payperiod}\Logs\`
- `\\Finance-svr\Payroll Exports\UKG\History\{payperiod}\Reports\`
- `\\Finance-svr\Payroll Exports\UKG\Logs\error.log.`
- `\\Finance-svr\Payroll Exports\UKG\Logs\badpayrollrecords.log`
- `\\Finance-svr\Payroll Exports\UKG\Reports\BatchSummary.txt`
- `\\Finance-svr\Payroll Exports\UKG\Reports\EarningSummary.txt`

---

## 341.3 Import Services - Fixed Parsing + Matching Rules

### `SAImportADPPayrollService`

- Watches `\\finance-svr\c$\Payroll Exports\ADP\Imports` for `PRPT1*.*`.
- Sleeps 5 seconds on file create before read.
- Uses fixed-width parsing and multiple hard-coded code remaps:
  - `H -> 05`, `M -> 65`, `P -> 12`, `S -> 03` (column 3)
  - `H -> 05` (column 4)
- Special amount-only fallback branch adds 1 synthetic hour for codes `14/15/16` to align matching.
- Meal period special case for code `18` uses `firstamount` two-row pairing logic.
- Department mismatch branch corrects payroll record department values and writes `Corrected Departments Report.txt`.

### `SAImportUKGPayrollService`

- Watches `\\finance-svr\c$\Payroll Exports\UKG\Imports` for `PRPT1*.*`.
- CSV field contract is fixed: `EmployeeNumber,PayrollDate,EarningCode,Hours,Amount`.
- Lookup requires `UKGInterfaces.UKGEarningCode` match.
- Matching priority is strict and ordered:
  1. ST hours + unpaid ST amount
  2. OT hours + unpaid OT amount
  3. lump-sum amount + unpaid paid-amount
- Non-matching lines are written to `{errorpath}\{original}.np`.

---

## 341.4 Scheduling/Timer Runtime Logic (Service + Web Host)

### `SADailyCallSheetService`

- Startup delay is hard-coded to `60000` ms.
- Pool scheduling uses fixed pool-number branches in `GetNextDailyCallSheet`:
  - 10/40: `LastCallingEndTime + 30 minutes`
  - 20/30: choose first vs last calling end by shift/on-duty-hour predicates
  - 50: `LastCallingEndTime + 2 hours`, +1 day if holiday
  - 60: `LastCallingEndTime + 30 minutes`
- Global pre-run adjustment: subtract 4 hours from computed process time.
- If computed time is in the past, fallback to `now + 180 seconds` snapped to zero seconds.
- Call-sheet creation message is MSMQ payload:
  - `poolControlNumber, shiftControlNumber, yyyy-MM-dd, processFlag`
  - `processFlag` is hard-coded `true` when generated from call-sheet timer flow.

### AtHoc Transport Details

- `AtHocService.GetToken()` posts form fields from appSettings:
  - `client_id`, `client_secret`, `grant_type`, `username`, `password`, `acr_values`, `scope`.
- All AtHoc GET/POST requests attach header literal format:
  - `Authorization: Bearer {token}`.
- `PublishAlert()` always emits schedule payload:
  - `AlertDuration = "5"`, `DurationUnit = "Minute"`.
- `SAAssignmentCallService` increments `nextcalltime` in fixed 5-minute steps and polls assignment responses for up to 6 minutes.
- `SAAssignmentCallService` throttles outbound call-message batches to 15 vacancies with `Thread.Sleep(60000)` between batches.
- `SAAssignmentOnDutyService` marks stale unsent on-duty records as sent when on-duty date is prior to `now.Date` and can force off-duty AtHoc message when tie-up exists.

### Global.asax Startup Constants

- The application startup constants include:
  - `user` as `"autoprocess"` for automated tasks.
  - `inbound` paths for message queue processing.
  - `delay` set to `600` (debug override `300`).
- Database-name toggles switch watcher root:
  - production DB -> `\\sql-svr\SA\Message Queue\Inbound`
  - non-production DB -> `\\sql-svr\SA\dev\Message Queue\Inbound`.
- `Application_Start` registers MVC routes/bundles, creates timers/watchers, and executes delayed re-scan of unprocessed inbound files via 5-second delayed timer.

### RestartApplicationPool Utility Behavior

- The utility requires `args[0]` as the application pool name.
- If the pool is currently started, it stops the pool first.
- The utility loops up to 10 iterations with `Thread.Sleep(1000)` to check the pool state.
- Each state transition is logged via `SAClassLibrary.Utilities.EventLogger`.

---

## 341.5 Additional Notes

This section captures runtime logic and utility behavior for scheduling, AtHoc transport, and application pool management.

## 341.6 Incremental Comparison Status (Post-Increment 3)

Completed in this increment:

- Deep process capture for payroll period execution and monthly incentive synthesis.
- Import-service fixed parsing/matching and non-processed-line handling.
- Call-sheet timer branch logic and AtHoc timer/polling behavior.
- Global startup/runtime path and environment toggles.
- Restart utility retry loop and state transition behavior.

Remaining for next incremental pass:

- Continue residual controller/query/modelview files not yet marked complete in strict sweep tracker.
- Reconcile any duplicate/overlapping sections created by earlier increment numbering drift (`Part 340`/`Part 367+`) and publish a normalized completion index.

# Part 342: Strict Full Sweep Tracker (Increment 4 - Call Response + Utility Hard-Coding)

## 342.1 Files Reviewed

- `SAAtHocMessageService/Services/SAAssignmentCallService.cs` (response/timer sections)
- `StrategicApplications/Utilities/ApplicationUtilities.cs` (core utility/runtime sections)

## 342.2 `SAAssignmentCallService` Additional Hard-Coded Logic

### Response polling and acceptance behavior

- Response polling window is hard-limited to **6 minutes** from each electronic call record creation time.
- Poll loop sleeps every **5 seconds**.
- Accept path uses strict text check: response string must contain `"Accept"`.
- On accept, service performs in order:
  1. Fill vacancy (`DailyCrewPosition.FillVacancy(...)`)
  2. Create `DailyCrewPositionElectronicResponseRecord` with fixed values:
     - `ResponseID = "1"`
     - `ResponseText = "Accepted"`
  3. Send assignment confirm message via AtHoc
  4. Optionally append late-call notes
  5. Save changes and remove from active polling list

### Moved-assignment branch

- If `SendRequest == false`, employee is force-move-notified (no accept/reject required).
- Notes text is hard-coded to foreman-fill wording and appended directly to `DailyAssignment.Notes`.

### Late-call note threshold

- `SetEmployeeLateCallNotes` compares scheduled call-start (`AssignmentDate + CallingTimeStart`) to `nextcalltime`.
- If call-start is earlier than `nextcalltime`, note text includes a fixed **90-minute** arrival target from current time.

### Timer reseed behavior

- `SetAtHocMessageTimer` seeds from unique `AssignmentOnDutyTime.CallingTimeStart` values where:
  - pool has `ElectronicCrewCalling`
  - pool has at least one craft/roster marked `ExtraBoard`
- If no later call time exists for current day, timer rolls to next day first call time.
- Timer is offset by **-5 minutes** before call time when in future; otherwise fallback is `now + 60 seconds` snapped to zero seconds.

## 342.3 `ApplicationUtilities` Additional Hard-Coded Logic

- Global automation identity literals:
  - `user = "autoprocess"`
  - inbound processed/error folders are fixed derivatives of `MvcApplication.inbound`
- Transaction scope builders enforce fixed **30-minute timeout** for both ReadCommitted and Snapshot wrappers.
- Teams routing uses fixed message-type switch (`SystemMessage`, `SystemSupport`, `TieUpMessage`, `ECallMessage`) with demo-environment override to `TestMessage` webhook.
- `RestartApplicationPool` launcher shells external executable from `RestartAppPoolLocation` appSetting with `CreateNoWindow=true`, `UseShellExecute=false`.
- `CreateNewControlNumber()` relies on `DateTime.UtcNow` string (`yyyyMMddHHmmssfff`) and `Thread.Sleep(1)` to reduce collision risk.
- IP validation supports exact match and wildcard-octet subnet patterns from comma-delimited `AuthorizedIPSubnets` setting.

# Part 343: Strict Full Sweep Tracker (Increment 5 - Daily Call Sheet MSMQ Pipeline)

## 343.1 Files Reviewed

- `SADailyCallSheetService/Services/SADailyAssignmentShiftService.cs`
- `SADailyCallSheetService/Services/SADailyAssignmentService.cs`
- `SADailyCallSheetService/Services/SADailyCrewPositionService.cs`
- `SADailyCallSheetService/Services/SADailyOnDutyRecordService.cs`
- `SADailyCallSheetService/Services/SADailyOnDutyMarkOffRecordService.cs`
- `SADailyCallSheetService/Utilities/ServiceUtilities.cs`

## 343.2 Queue + Startup Hard-Coded Behavior

- Each service has fixed startup delay `60000` ms before queue consumption begins.
- Queue names are loaded from appSettings with explicit debug fallbacks (`dev...Queue`).
- Message queues are consumed with `MessageQueueTransactionType.Automatic` and XML string formatter.
- All services use `user = "autoprocess"` for created/modified metadata where records are created.

### MSMQ host hard-coding

`ServiceUtilities.CreateMSMQMessage` uses fixed direct format path base:

- Production: `FormatName:DIRECT=OS:SQL-SVR\private$\`
- Debug: `FormatName:DIRECT=OS:PTRA-IT-LT-10\private$\`

## 343.3 Daily Assignment Shift Creation Rules

### `SADailyAssignmentShiftService.CreateNewRecord`

- Creates `DailyAssignmentShift` and initializes `Notes = ""`.
- Pool-specific branch for Pool 30 (Clerical):
  - if shift date is a holiday, auto-creates `HoldDownRelease` records for all open hold-downs in that pool.
- `finally` block always reseeds call-sheet timer via `SADailyCallSheetService.SetDailyCallSheetTimer(pool)`.

### `SetBoardOrder` deterministic concatenation

Board order is built as concatenated string and converted to `long`:

1. `(OnDutyHour + 10)`
2. `(OnDutyMinute + 10)`
3. For pools 10/40 only: `Location.BoardOrder`
4. `AssignmentTypeNumber`
5. `AssignmentNumber`

This introduces implicit hard-coded ordering precedence tied to string composition.

## 343.4 Daily Assignment + Crew Position Message Flow

### `SADailyAssignmentService`

- Creates `DailyAssignment` with hard-coded defaults:
  - `Notes = ""`
  - `Billable = false`
  - `Recollectable = false`
  - `EmergencyCallOut = true` only for pool 40; false otherwise.
- Training-position message creation uses sentinel railroad position control number:
  - `99999999999999999`
- Outbound `DailyCrewPosition` create payload includes:
  - daily assignment control number, railroad position control number, assignment date, extra-board-only flag, crew control number, position control number.

### `SADailyCrewPositionService`

- Retries up to 10 times (100 ms sleep) for newly-created `DailyCrewPosition` visibility before aborting send path.
- Auto on-duty message emission is suppressed if employee is expected to be moved by pending seniority move before assignment on-duty time.
- Assignment eligibility helper blocks auto-assignment when employee has any conflicting:
  - open hold-down on other position,
  - open temporary assignment on same weekday,
  - training assignment on same date.

## 343.5 On-Duty + Mark-Off Queue Chaining

### `SADailyOnDutyRecordService`

- Before on-duty record creation, always writes daily employee status/position record via `GetDailyRailroadEmployeePositionRecord`.
- After on-duty creation, emits `DailyMarkOffRecord` create messages for open mark-offs that overlap on-duty datetime and are not deleted.

### `SADailyOnDutyMarkOffRecordService`

- Applies mark-off linkage via `UpdateDailyCrewPositionOnDutyMarkOffRecord`.
- Always emits a filesystem update-vacancy request file (`*.UV`) to fixed path:
  - `\\sql-svr\SA\Message Queue\Inbound`
- UV file content is tab-delimited: `method`, `pool`, `roster`.

# Part 344: Strict Full Sweep Tracker (Increment 6 - Shared Utility Behavior)

## 344.1 Files Reviewed

- `SAClassLibrary/Utilities/FileUtilities.cs`
- `SAClassLibrary/Utilities/EventLogger.cs`
- `SAClassLibrary/Utilities/ClassLibraryUtilities.cs`
- `SAClassLibrary/Utilities/TransactionScopeBuilder.cs`
- `StrategicApplications/Utilities/FileUtilities.cs`
- `StrategicApplications/Utilities/EventLogger.cs`

## 344.2 File Utility Semantics (Both Projects)

`FileUtilities` implementations in class library and web project are functionally mirrored.

Hard-coded operational behavior:

- Lock polling uses fixed sleep interval `100 ms` in all file-lock wait loops.
- `WriteFile` always recreates files via `File.Create(path)` (overwrite semantics) and writes UTF-8 with BOM (`UTF8Encoding(true)`).
- `MoveFile` and `CopyFile` delete destination/target file first when present.
- `CopyFile(filepath, newfile)` is implemented as **move/replace**, not stream copy:
  - deletes `filepath`
  - moves `newfile -> filepath`
- `DeleteFile` does not guard with existence checks after unlock polling; `File.Delete` is executed directly.
- `GetAllContentsAsString` trims trailing CR/LF only (`TrimEnd('\r','\n')`).

## 344.3 Event Logger Hard-Coded Defaults

### `StrategicApplications.Utilities.EventLogger`

- Default event source: `Train Crew Reporting`
- Default log: `Application`
- Default event IDs by severity:
  - Information = `200`
  - Warning = `800`
  - Error = `900`

### `SAClassLibrary.Utilities.EventLogger`

- Default event source: `Strategic Applications`
- Default log: `Crew Management Service Log`
- Same severity ID defaults (`200/800/900`).

Observed implementation quirk (both variants):

- Warning overloads check `EventLog.SourceExists(dfltsource)` even when creating custom `source/log`, then write using default source in some overloads; this can lead to source/log mismatch behavior.

## 344.4 Control Number and Pay-Period Helpers

### `CreateNewControlNumber` (ClassLibrary + Web utility families)

- Uses `DateTime.UtcNow` formatted as `yyyyMMddHHmmssfff` and cast to `long`.
- Forced `Thread.Sleep(1)` before generation to reduce same-millisecond collisions.

### `ClassLibraryUtilities.IsInCurrentPayPeriod(currentdate, lastdate)`

- Returns false if year or month differ.
- Month is split into fixed halves:
  - days `1-15`
  - days `16-end`
- Logic is explicitly anchored on day `15` threshold.

## 344.5 Transaction Scope Defaults

### SAClassLibrary `TransactionScopeBuilder`

- ReadCommitted and Snapshot wrappers use `TransactionManager.DefaultTimeout`.

### Web `ApplicationUtilities.TransactionScopeBuilder` (captured earlier, reaffirmed)

- ReadCommitted/Snapshot wrappers use fixed timeout `00:30:00`.

This creates cross-project timeout divergence between service/class-library and web utility wrappers.

# Part 345: Strict Full Sweep Tracker (Increment 7 - Global Host Orchestration Deep Pass)

## 345.1 File Reviewed

- `StrategicApplications/Global.asax.cs` (watcher and timer orchestration sections)

## 345.2 Inbound File Watcher Contract

The host creates six `FileSystemWatcher` instances (prod + dev), each with fixed extension filters:

- `*.hr` -> holiday record ingestion
- `*.uv` -> vacancy update ingestion
- `*.esr` -> employee status record ingestion

Operational behavior:

- Created + Deleted handlers maintain `...RecordsExist` booleans.
- Re-entrancy guard booleans prevent concurrent processing loops:
  - `HolidayRecordsProcessing`
  - `VacancyRecordsProcessing`
  - `StatusRecordsProcessing`
- On processing exception, files are moved to `inbounderror` and processing flags are reset.

## 345.3 Timer Topology Creation Rules

`CreateTimers()` constructs timers by client/railroad/pool with policy-flag gates.

### Railroad-scoped timers

- `DailyRailroadEmployeeStatusTimers`
- `DailyReportTimers`
- `DailyVacationWeekTimers`
- `CreateHolidayTimers`
- `HolidayTimers`
- `PublishRailroadInformationTimers`

### Pool-scoped timers

- `MarkOffRequestTimers`
- `RosterBoardMarkOffTimers`
- `RosterBoardHangoutTimers`
- `DailyOffDayTimers`
- `AtHocMessageTimers`
- `BulletinTimers` (only when `pool.AutoBulletins`)
- `SeniorityMoveTimers` (only when `pool.AutoMoves`)
- `HangoutTimers` (only when `pool.AutoHangouts`)
- `DailyCallSheetTimers` (always created)
- `DailyExtraBoardTimers` (only when `pool.AutoCallSheets`)

When auto flags are disabled, corresponding `next...Updates` entries are forced to sentinel `DateTime(9999,12,31)` and timer dictionary entries are removed.

## 345.4 Timer Interval Hard-Coding Details

- Bulletin/SeniorityMove/Hangout timers add explicit `+5 seconds` offset before interval calculation.
- Daily call-sheet timer uses `SetNextCallTime(pool).AddSeconds(15)`.
- CreateHoliday/Holiday/PublishRailroadInformation timers only enable when target date is within `DateTime.Today.AddDays(5)`.
- AtHoc/MarkOffRequest timers only enable if computed interval is positive.

## 345.5 Fail-Safe and Disable Behavior

- If client or railroad auto-assignment gates fail, host invokes disable routines:
  - `client.DisableRailroadAutoFunctions(db)` or
  - `railroad.DisableRailroadPoolAutoFunctions(db)`
- Then calls `ClearTimers()` to purge orchestration state dictionaries.

`ClearTimers()` explicitly clears timer dictionaries for:

- AtHoc, bulletin, seniority move, hangout
- daily railroad status/report/callsheet/offday
- holiday/create-holiday
- mark-off request and roster-board mark-off/hangout

# Part 346: Strict Full Sweep Tracker (Increment 8 - AtHoc On-Duty Service Deep Pass)

## 346.1 File Reviewed

- `SAAtHocMessageService/Services/SAAssignmentOnDutyService.cs`

## 346.2 Startup and Pool Timer Seeding

- Service start delay is fixed at `60000` ms.
- Timer topology is seeded only for clients/railroads where:
  - `Client.AutoAssignments == true`
  - `Railroad.AutoAssignments == true`
  - railroad has pools.
- One timer per pool is created and stored in `AtHocMessageTimers`.

## 346.3 On-Duty Record Selection and Suppression

Candidate query:

- pool match
- `AtHocMsgSent == false`
- ordered by `AssignmentOnDutyDate`, `AssignmentOnDutyTime`.

For records on current date/time window:

- sent only if all are null:
  - annulment
  - do-not-fill
  - did-not-work
  - on-duty mark-off
  - off-duty record

Otherwise record is force-marked sent without outbound AtHoc call.

For past-dated unsent records:

- force-marked sent.
- if off-duty record exists, sends explicit off-duty employee message (`OnDuty=false`).

## 346.4 Message Dispatch + Persistence Order

- Batch call uses `AtHocService.ProcessEmployeeOnDutyMessages(records)`.
- On success, each candidate record is updated:
  - `AtHocMsgSent = true`
  - `ModifiedBy = "autoprocess"`
  - `ModifiedDate = now`
- Service sleeps fixed 1 minute before reseeding pool timer.

## 346.5 Next-Update Computation

`GetDailyOnDutyRecordUpdate(pool)`:

- default sentinel: `DateTime(9999,12,31)`.
- if unsent record exists, update = earliest `AssignmentOnDutyDateTime`.
- if computed time is not in future, fallback = `now + delay` then snap seconds to 0.

# Part 347: Strict Full Sweep Tracker (Increment 9 - Payroll Export Rule Matrix Deep Pass)

## 347.1 File Reviewed

- `StrategicApplications/Utilities/PayrollUtilities.cs` (ADP/UKG export branches)

## 347.2 ADP Export Column-Mapping Logic (Detailed)

`CreateADPPayrollFile` writes output by `earning.Code.ADPInterface.ColumnNumber` with hard-coded branch rules:

- `1/2`: ST/OT core fields; if amount present, code+amount are pushed to earnings-3 column.
- `3/4`: hours/amount distributed across Hours3/Hours4/Earnings5 columns.
- `5`: amount-only route -> Hours4 fields.
- `6`: ST hours into memo fields and optional memo amount pair.

Special branch behavior:

- ADP code `20` (double time) is forcibly transmitted through straight-time slot when only OT exists.
- For pool batch `1020`, additional crew-consist synthetic line `",,,,,,,,4, 5.00,,"` is emitted when employee is not protected and code conditions match.

## 347.3 UKG Export Normalization Rules

`CreateUKGPayrollFile` applies hard-coded job-code normalization before writing:

- `101D -> 10H1`
- `A122 -> A123`
- `100F -> 101F`
- `100H -> 101H`
- incentive jobs beginning with `S` send blank `jobpaid`.

Rate adjustment rule:

- if `RatePercentage < 100` and `jobpaid` not blank, suffix percentage digits to `jobpaid` token.

## 347.4 File Output and Error Contract

- Any missing interface configuration appends to `error.log.` (note trailing period in filename in several branches).
- Trial-mode writes include:
  - ADP: `EPIPT190.csv`, `VALPT1AA.csv`, `Batch99.csv`
  - UKG: `UKGPT1.csv`, report text/pdf files
- `ProcessPayrollController.Status` is continuously overwritten with record counts through export loops.

# Part 348: Strict Full Sweep Tracker (Increment 10 - Service Host/Installer Contracts)

## 348.1 Files Reviewed

- `SADailyCallSheetService/Program.cs`
- `SADailyCallSheetService/ProjectInstaller.Designer.cs`
- `SAImportPayrollService/Program.cs`
- `SAImportPayrollService/ProjectInstaller.Designer.cs`
- `SAAtHocMessageService/Program.cs`
- `SAAtHocMessageService/ProjectInstaller.Designer.cs`

## 348.2 Executable Service Chains (Run Order Contracts)

### Daily call sheet host process

`SADailyCallSheetService` process runs six services in one executable:

1. `SADailyCallSheetService`
2. `SADailyAssignmentShiftService`
3. `SADailyAssignmentService`
4. `SADailyCrewPositionService`
5. `SADailyOnDutyRecordService`
6. `SADailyOnDutyMarkOffRecordService`

### Payroll import host process

`SAImportPayrollService` process runs:

1. `SAImportADPPayrollService`
2. `SAImportUKGPayrollService`

### AtHoc host process

`SAAtHocMessageService` process runs:

1. `SAAssignmentCallService`
2. `SAAssignmentOnDutyService`

## 348.3 Installer Hard-Coded Deployment Behavior

Across all three Windows-service projects:

- Service process account is fixed to `ServiceAccount.LocalSystem`.
- Installer entries use static service names (not environment-suffixed).

### Daily call sheet dependency chain

Hard-coded `ServicesDependedOn` creates sequential startup expectations:

- `SADailyAssignmentShiftService` depends on `SADailyCallSheetService`
- `SADailyAssignmentService` depends on `SADailyAssignmentShiftService`
- `SADailyCrewPositionService` depends on `SADailyAssignmentService`
- `SADailyOnDutyRecordService` depends on `SADailyCrewPositionService`
- `SADailyOnDutyMarkOffRecordService` depends on `SADailyOnDutyRecordService`

All listed DailyCallSheet-related services are configured `ServiceStartMode.Automatic`.

## 348.4 Operational Implication

- The solution encodes orchestration in two layers:
  - logical chaining via queue messages
  - OS startup ordering via installer dependencies
- This means deployment-time service naming and dependency integrity are hard requirements for end-to-end daily processing.

# Part 349: Strict Full Sweep Tracker (Increment 11 - Configuration File Hard-Coding)

## 349.1 Files Reviewed

- `SADailyCallSheetService/App.config`
- `SAImportPayrollService/App.config`
- `SAAtHocMessageService/App.config`
- `SAClassLibrary/App.config`
- `RestartApplicationPool/App.config`

## 349.2 Queue and Host Endpoint Constants

### `SADailyCallSheetService/App.config`

Production MSMQ direct-format endpoints are fixed to `SQL-SVR` private queues:

- `dailyassignmentshift`
- `dailyassignment`
- `dailycrewposition`
- `dailyondutyrecord`
- `dailymarkoffrecord`

Debug endpoints are fixed to `PTRA-IT-LT-10` equivalents.

## 349.3 Database Connection Hard-Coding

Multiple projects pin SQL host to `sql-svr` with integrated security and dual catalogs:

- `StrategicApplications`
- `StrategicApplicationsDemo`

Observed config quirk in `SAClassLibrary/App.config`:

- two separate `<connectionStrings>` elements are declared (non-standard structure but present in file).

## 349.4 AtHoc Service AppSettings Contract

`SAAtHocMessageService/App.config` defines hard-coded integration contract keys:

- base URL and auth settings (`AtHocURL`, `ClientID`, `ClientSecret`, `GrantType`, `UserName`, `Password`, `AcrValues`, `Scope`)
- template IDs (`AssignmentCallTemplate`, `AssignmentMoveTemplate`, `AssignmentConfirmTemplate`)
- endpoint suffixes (`GetTokenURL`, `SyncUserURL`, `PublishAlertURL`, `GetAlertResponseURL`, `DetailsByUsersReportURL`)
- Teams webhooks (`TestMessage`, `ECallMessage`, `SystemSupport`)

These values are consumed directly by runtime request construction in `AtHocService`.

## 349.5 Runtime/Binary Binding Notes

- All reviewed executable configs target `.NETFramework,Version=v4.7.2`.
- `RestartApplicationPool/App.config` contains explicit binding redirect for `System.Reflection.TypeExtensions`.
- Service configs include broad assembly binding redirects (`Newtonsoft.Json`, OWIN, BouncyCastle, etc.) that lock runtime assembly resolution behavior.

# Part 350: Strict Full Sweep Tracker (Increment 12 - Web Host Configuration Deep Pass)

## 350.1 File Reviewed

- `StrategicApplications/Web.config`

## 350.2 Connection String and Environment Routing

`Web.config` carries parallel connection entries for multiple contexts:

- `SAClassLibraryContext`
- `SAClassLibraryDemoContext`
- `StrategicApplicationsContext`
- `StrategicApplicationsDemoContext`
- `DevelopmentDatabaseContext` (localhost)

This enables runtime switching by database name logic in `Global.asax` / utility methods, while preserving fixed server/catalog mappings in config.

## 350.3 AppSettings Runtime Contracts

High-impact hard-coded keys consumed directly by runtime logic include:

- network/pool utility routing:
  - `AuthorizedIPSubnets`
  - `RestartAppPoolLocation`
  - `MSMQServer`
- AtHoc integration contract:
  - base/auth keys (`AtHocURL`, OAuth-related fields)
  - template IDs
  - endpoint suffixes
- Teams/webhook channels:
  - `SystemMessage`, `TieUpMessage`, `SystemSupport`, `TestMessage`, `ECallMessage`

## 350.4 Request/Runtime Hard Limits

- `<httpRuntime maxQueryStringLength="4096" />`
- IIS `<requestLimits maxQueryString="4096" maxUrl="4096" />`

These values align with controller/report routes that can carry long query parameters and represent fixed transport constraints.

## 350.5 Assembly Resolution Pinning

`Web.config` contains broad binding redirects (MVC, OWIN, Newtonsoft, BouncyCastle, diagnostics/runtime packages). This locks assembly resolution to specific versions and is part of runtime behavior specification for deployment consistency.

## 350.6 Security/Operations Observation

- Integration credentials/secrets and webhook URLs are stored in plain appSettings entries and consumed directly by runtime services.
- This is an explicit hard-coded operational pattern in current architecture (configuration-managed but not secret-store-backed).

# Part 351: Strict Full Sweep Tracker (Increment 13 - Startup/Auth/Routing Contracts)

## 351.1 Files Reviewed

- `StrategicApplications/Startup.cs`
- `StrategicApplications/App_Start/Startup.Auth.cs`
- `StrategicApplications/App_Start/RouteConfig.cs`
- `StrategicApplications/App_Start/FilterConfig.cs`
- `StrategicApplications/App_Start/BundleConfig.cs`

## 351.2 OWIN Authentication Hard-Coding

`ConfigureAuth` defines fixed cookie auth behavior:

- `AuthenticationType = ApplicationCookie`
- login path fixed to `/Account/Login`
- cookie expiration fixed to **480 minutes** (8 hours)
- external sign-in cookie enabled for third-party flow staging

No third-party provider integration is active (provider stubs are commented out).

## 351.3 MVC Route and Filter Contract

- Single default route template:
  - `{controller}/{action}/{id}`
  - defaults: `Home/Index`, optional `id`
- Global filter registration includes only `HandleErrorAttribute`.

This means cross-cutting authorization is not globally injected and remains controller/action attributed.

## 351.4 Bundle and Theme Runtime Behavior

`BundleConfig` defines static bundle paths and a runtime theme switch pipeline.

### Default front-end bundle composition

- Scripts: jQuery, validation, Modernizr, moment/bootstrap/datetime/fullcalendar/ckeditor/respond
- Styles default theme: `bootstrap-spacelab.css` + calendar/datetime/site styles

### Dynamic theme switching

- If user not authenticated: fallback to default theme.
- If authenticated user not cached: resolved via `MvcApplication.RegisterUser(username)`.
- If `ThemeFile` empty: default theme.
- Else: style bundle uses `~/Content/{ThemeFile}`.

### Debug-only database theme overrides

When compiled DEBUG, database name can force theme regardless of user theme:

- `DevelopmentDatabase` -> `bootstrap-cerulean.css`
- `StrategicApplicationsDemo` -> `bootstrap-superhero.css`

# Part 352: Strict Full Sweep Tracker (Increment 14 - Account/Admin Control-Flow Rules)

## 352.1 Files Reviewed

- `StrategicApplications/Controllers/AccountController.cs`
- `StrategicApplications/Controllers/AdminController.cs`

## 352.2 Login/Auth Hard-Coded Behavior

### `AccountController.Login` (GET)

- Always signs out existing app cookie and forms auth before rendering login.
- DEBUG-only credential prefill is hard-coded:
  - username: `1074`
  - password: `10Dr0wss@p74`

### `AccountController.Login` (POST)

- On valid credentials:
  - signs in via application cookie
  - forces password reset when hashed password equals username (`CheckPasswordReset`)
  - hard-coded admin branch:
    - if `UserName == "admin"` -> `Home/Home`
    - else -> `EmployeeDetail/Details`

### Sign-in metadata side effects

- `SetLastLoginDateTime` persists:
  - `LastLogin`
  - `IPAddress`
  - `OnProperty = CheckOnPropertyIPAddress(ip)`
- Also writes `UserLoginRecord` snapshot of prior user login fields.

## 352.3 Role/Redirect Branching (`AdminController`)

`LoginRedirect` and `DefaultView` encode fixed role-priority navigation:

- System/Client Administrator -> client index path
- Railroad Administrator -> railroad index when single-client DB
- Supervisor/HR/Dispatcher/Timekeeper -> pool index when single-railroad DB
- fallthrough -> account login

`PayrollLoginRedirect` role rules:

- System Admin / Client Admin / Timekeeper -> client payroll path
- Railroad Admin / Supervisor -> railroad payroll path only when single client

## 352.4 App-Pool Operations

- `RestartAppPool` restricted to `System Administrator` and calls:
  - `ApplicationUtilities.RestartApplicationPool(MvcApplication.database, user)`
- `RecycleAppPool` also allows `Railroad Crew Dispatcher` and calls recycle utility with same pool-name source.

This ties administrative pool operations directly to current runtime database name string.

# Part 353: Strict Full Sweep Tracker (Increment 15 - Service ViewModel Wrappers Deep Pass)

## 353.1 Files Reviewed

- `SADailyCallSheetService/Models/SV_DailyCrewPosition.cs`
- `SADailyCallSheetService/Models/SV_DailyCrewPositionOnDutyRecord.cs`
- `SADailyCallSheetService/Models/SV_RailroadPoolEmployee.cs`
- `SADailyCallSheetService/Models/SV_DailyAssignmentShift.cs`
- `SADailyCallSheetService/Models/SV_Shift.cs`
- `SADailyCallSheetService/Models/SV_MarkOffRecord.cs`

## 353.2 Job Code / Shift / Calling-Time Hard-Coded Rules

### `SV_DailyCrewPosition.JobCode`

Pool-specific formatting:

- pool 30/60: `{PositionCode}{AssignmentNumber}`
- pool 50: `AssignmentName`
- default: `{AssignmentNumber}{PositionCode}`

### `SV_Shift` fixed shift graph

- previous/next shift IDs are hard-coded as a 3-shift ring:
  - `1 <-> 2 <-> 3` with wrap.
- `FirstCallingTime` and `LastCallingTime` append `+30 minutes` and add `ShiftID` as seconds discriminator.

## 353.3 On-Duty Record Creation and Pay-Code Logic

`SV_DailyCrewPosition.CreateDailyCrewPositionOnDutyRecord` hard-coded behaviors:

- trainee handling can force assignment to current daily position.
- negative rest collapses to `0h 0m`.
- `AtHocMsgSent` reset to `false` on create.
- consecutive day reset threshold uses FRA constant (`ConsecutiveDayHours`, 24-hour break rule).
- ST/day counters reset on day `1` or `16`, pay-period transitions, or missing last record.

`GetPayrollEarningCode` encodes pool/case matrix with fixed payroll code outputs (`01`, `02`, `05`, `19`, `20`, `22`, etc.) and special guards:

- no overtime for same-shift vacancy in several branches.
- pool 40 double-time escalation if prior day was overtime off-day chain.
- overtime suppression for training and certain bulletined/hangout combinations.

## 353.4 Mark-Off Linkage and Compensation Branching

`SV_DailyCrewPositionOnDutyRecord.IsMarkedOffThisDateTime` applies layered checks:

- mark-up-hour windows from craft-specific or markoff-code default settings
- on-duty vs off-duty boundary checks with `ApprovedByAgreement` branch
- special handling for `NR`, `NN`, `SR` using call-start cutoff

`UpdateDailyCrewPositionOnDutyMarkOffRecord` includes hard-coded behavior:

- if MO code is `CR` and employee is extra-board/hangout and markoff occurs after on-duty, mark-off link is created with `Ignore = true`.
- otherwise decrements `STDaysWorked` and `DaysWorked` by 1 when mark-off link is attached.

`SV_MarkOffRecord` hard-coded vacation/week mapping:

- `V1..V5` interpreted as vacation-week family.
- thresholds map to fixed hours (`40/80/120/160/200`) and day windows; overflow transitions to `VO` or `EV` code paths.

## 353.5 RailroadPoolEmployee Wrapper Logic

`SV_RailroadPoolEmployee` wraps operational identity/state with fixed interpretations:

- `CurrentPosition` prefers open hold-down; otherwise assigned position.
- `IsExtraBoardOrHangout` is derived strictly from roster-board position state.
- `GetLastOrOpenMarkOffRecords` returns open records first, else falls back to last non-deleted mark-off.

# Part 354: Strict Full Sweep Tracker (Increment 16 - Query Layer Hard-Coded Lists)

## 354.1 Files Reviewed

- `StrategicApplications/Models/Queries/SelectLists.cs`
- `StrategicApplications/Models/Queries/CollectionLists.cs`
- `StrategicApplications/Models/Queries/Collections.cs`

## 354.2 `SelectLists` Hard-Coded UI Defaults and Sentinels

Observed fixed defaults and literals:

- user exclusion: `UserName != "admin"` in client-unselected user list.
- default locomotive type selection attempts `"MK 1500D"`.
- default railroad material code selection uses `"2020"`.
- craft wait-list sentinel option is appended with fixed value:
  - text: `VW - Vacation Weeks`
  - value: `99999999999999999`
- railroad pool crafts list prepends fixed "All Crafts" option with value `0` and pre-selects it.

Additional deterministic UI shaping:

- week-day selectors always exclude `WeekDayNumber == 0`.
- several list builders inject suffix labels (`" Craft"`, `" Roster"`) into display text.

## 354.3 Role and Employment-Code Hard-Coding in Query Filters

`CollectionLists` and `Collections` encode fixed identity/status checks:

- craft-approval-officer role GUID hard-coded:
  - `1d78b8ea-f36b-42a7-91fa-325f543aa2e9`
- active employment checks use code literals (`AT`, `OL`, `XE`) in multiple methods.
- payroll windows include fixed 90-day lookback and 60-of-90 active-day threshold branch.

## 354.4 Shift/Assignment and Date Window Query Contracts

- shift lists exclude `ReliefShift == true` by default.
- daily assignment queries enforce combined constraints:
  - assignment effective/abolishment date bounds
  - weekday-name membership check
  - exclusion of already-created daily assignments for same date
  - exclusion of extra-board-only assignment types in standard daily set

These are deterministic business predicates reused across UI/service flows.

## 354.5 Parallel `CollectionLists` vs `Collections` Behavior Drift

Both files contain near-duplicate query methods with subtle differences (e.g., some state-code checks and date-field usage variants). This duplicate-query topology is itself a hard-coded architectural trait and a potential divergence risk.

# Part 355: Strict Full Sweep Tracker (Increment 17 - Utility Date/String + Vacancy Engine Deep Pass)

## 355.1 Files Reviewed

- `StrategicApplications/Utilities/DateTimeUtilities.cs`
- `StrategicApplications/Utilities/StringUtilities.cs`
- `StrategicApplications/Utilities/ApplicationUtilities.cs` (additional vacancy/pay-period regions)

## 355.2 `DateTimeUtilities` Hard-Coded Calendar Rules

- `CreateDateFromString` only parses compact numeric formats (`MMddyy`) and pads lengths `5/7` with leading `0` before parse.
- `CalculateYears` uses float subtraction on formatted `yyyy.MMdd` values (not calendar-aware year diff).
- Half-month logic across utility methods is consistently anchored on day `16` split.
- Vacation request alignment helper computes next-year alignment by weekday offset from Jan 1.
- `GetDisabledWeekDays*` methods generate byte arrays by removing only one allowed weekday from full `0..6` list.

## 355.3 `StringUtilities` Contract

- Numeric checks are ASCII-digit only (`'0'..'9'`), no sign/decimal handling.
- `AddLeadingZeros` pads strictly to target width by prefixing `"0"` in loop.
- `GetNthIndex` returns `-1` when nth delimiter not found.

## 355.4 `ApplicationUtilities` Additional Hard-Coded Behavior

### Period mapping (`GetStartEndPeriod`)

- Period IDs are fixed semantic codes (`0,1,2,3,4,5,7,8,9` with default fallback).
- Half-month windows are hard-split into `1-15` and `16-end`.
- Pay-period branch (`7`) chooses first day by paydate day > 15 rule.

### Vacancy request file emission

- `CreateUpdateVacancyRequest` writes `*.UV` files only when `MvcApplication.VacancyRecordsProcessing == false`.
- Payload is tab-delimited: `method`, `pool`, `roster`.

### Vacancy engine no-bid branch details

Additional deterministic traits in `UpdateDailyCrewPositionVacancies`:

- pool 50 uses pool-wide vacancy clearing/query behavior; other pools use roster-scoped clearing/query.
- if shift-scoped vacancy query returns none (or all start in future), logic falls back to all-shift roster query.
- extra-board ordering is fixed by `TieUpOrder` then `BoardOrder`.
- no-bid handling computes vacancy/extra-board indexes and removes candidates based on index comparison and shift/time precedence.
- vacancy assignment creation uses calling-time lookup from `AssignmentOnDutyTime` and persists paired vacancy + vacancy-employee records in same flow.

# Part 356: Strict Full Sweep Tracker (Increment 18 - Base Types and Interface Layer)

## 356.1 Files Reviewed

- `StrategicApplications/Models/BaseClasses/ControlNumberBase.cs`
- `SAClassLibrary/BaseClasses/ControlNumberBase.cs`
- `StrategicApplications/Models/Interfaces/IAutoMarkUp.cs`
- `StrategicApplications/Models/Interfaces/IAvailableEmployeeRepository.cs`
- `StrategicApplications/Models/Interfaces/ICacheProvider.cs`
- `StrategicApplications/Models/Repositories/DefaultCacheProvider.cs`

## 356.2 Control Number Base-Type Divergence

Both projects enforce non-DB-generated `long` control numbers and identical audit columns, but generation source differs:

- Web model base: `ApplicationUtilities.CreateNewControlNumber()`
- Class library base: inline `Thread.Sleep(1)` + `DateTime.UtcNow("yyyyMMddHHmmssfff")`

This is a hard-coded dual-generator pattern and can produce subtle cross-layer timing behavior differences.

## 356.3 Interface Contract Observations

### `IAutoMarkUp`

- Defines one required rule hook:
  - `GetAutomaticMarkUpDateTime(MarkOffRecord)`
- Interface is internal (no explicit access modifier), limiting visibility to assembly scope.

### `IAvailableEmployeeRepository`

- Declares cacheable employee source methods (extra board/off-day/seniority ordered lists) and cache reset.
- No implementation file exists in current repository snapshot; this is an explicit documented gap between contract and concrete class set.

## 356.4 Cache Provider Duplication

Two `ICacheProvider` contracts are present:

- `StrategicApplications.Models.Interfaces.ICacheProvider`
- `StrategicApplications.Models.Repositories.ICacheProvider` (nested in `DefaultCacheProvider.cs`)

`DefaultCacheProvider` implements the repository-namespace interface, not the interface-namespace contract.

Runtime hard-coding in `DefaultCacheProvider`:

- uses `MemoryCache.Default`
- expiration policy is absolute (`DateTime.Now + cacheTime minutes`)
- no sliding expiration branch.

# Part 357: Strict Full Sweep Tracker (Increment 19 - Identity + Context Initialization)

## 357.1 Files Reviewed

- `StrategicApplications/Models/IdentityModels.cs`
- `StrategicApplications/Models/ApplicationUserManager.cs`
- `StrategicApplications/Models/Context/StrategicApplicationsContext.cs`
- `SAClassLibrary/Context/SAClassLibraryContext.cs`

## 357.2 Identity Defaults and Hard-Coded User Profile Rules

`ApplicationUser` creation (`CreateInstance(EmployeeCreateView, user)`) enforces fixed defaults:

- `UserName = EmployeeNumber`
- `ThemeFile = "bootstrap-spacelab.css"`
- `OnProperty = false`
- `IPAddress = "Not Known"`
- `LastLogin = now`

Name-normalization policy:

- first/middle/last names are converted to title case only when entire input is all-upper or all-lower.

`ApplicationUserManager` hard-codes minimum password length to **4**.

## 357.3 Identity Messaging Side Effect

`ApplicationUser.SendOfficerMessage()` condition uses:

- `if (!this.Roles.Any(r => r.Equals("Railroad Employee")))`

Since `Roles` is `IdentityUserRole` collection (not role-name strings), this equality check is structurally mismatched and should be treated as hard-coded behavior risk in officer-message triggering logic.

When condition passes, message format is fixed:

- `Craft,{EmployeeNumber},Officer`

and dispatched through `AtHocService.ProcessEmployeeMessage`.

## 357.4 Context Initialization Contracts

### `StrategicApplicationsContext`

- constructor uses hard-coded base name `"StrategicApplicationsDemoContext"`.
- initializer is explicitly set to `CreateDatabaseIfNotExists<StrategicApplicationsContext>`.
- sync `SaveChanges` catches concurrency exceptions and retries once after reloading single conflicting entry.

### `SAClassLibraryContext`

- constructor uses `"name=SAClassLibraryDemoContext"`.
- initializer disabled (`Database.SetInitializer(null)`).
- extensive fluent relationship mapping in `OnModelCreating` including explicit many-to-many key mapping and cascade directives.

# Part 358: Strict Full Sweep Tracker (Increment 20 - `VacancyAssignmentService` Deep Pass)

## 358.1 File Reviewed

- `StrategicApplications/Services/VacancyAssignmentService.cs`

## 358.2 Static In-Memory Working Sets

Service uses static mutable lists across calls:

- `vacancylist`
- `xbpositionlist`
- `rosterxblist`

This is a hard-coded process model with shared in-memory state rather than scoped/local working sets.

## 358.3 Vacancy Rebuild and Ordering Contracts

`AssignDailyCrewPositionVacancyRecords` behavior:

- clears existing persisted vacancy records for target scope before rebuild.
- rebuild ordering is deterministic:
  - skip flag
  - assignment date
  - daily assignment board order
  - position number
- vacancy numbers are regenerated sequentially (`vacnbr++`) during rebuild.

Scope branches:

- roster + shift
- roster all-shifts
- pool-wide fallback

All branches exclude positions with electronic call records and require open/unfilled conditions.

## 358.4 No-Bid and Extra-Board Assignment Rules

- no-bid branch requests forced-assignment list by position/roster and attempts to reserve youngest eligible extra-board employee.
- no-bid assignment path immediately removes chosen vacancy from working list and corresponding extra-board position from roster list.

General assignment rules include hard-coded constraints:

- if first XB position has `DaysWorked >= 12`, service tries to pick next with `< 12`.
- qualification check is required before direct assignment.
- if unqualified, service attempts special foreman swap logic and helper-move fallback.

## 358.5 Foreman Swap / Helper Fallback Branches

`CheckForForemanNextExtraBoardPosition`:

- performs two-step swap using current+next vacancy and current+next XB positions.
- depends on position-number ordering and rested-time check at vacancy end-call time.

`CheckForEligibleHelpers` fallback can:

- set `vacancy.ExtraBoard = false`
- assign non-extra-board employee
- create a new vacancy for displaced position and reinsert into ordered vacancy list.

# Part 359: Strict Full Sweep Tracker (Increment 21 - Process/Dispatch ViewModel + Report Endpoint)

## 359.1 Files Reviewed

- `StrategicApplications/Models/ModelViews/ProcessPayrollViews.cs`
- `StrategicApplications/Models/ModelViews/FillVacancyViews.cs`
- `StrategicApplications/Models/ModelViews/OnDutyRecordViews.cs`
- `StrategicApplications/Controllers/DailyReportController.cs`

## 359.2 ViewModel Hard-Coded Presentation Rules

### `ProcessPayrollViews`

- `PayrollPeriodProcessRecordView.FinalProcess` renders fixed text values `"Yes"` / `"No"`.
- `CreateMonthlyPayRecordsView` defaults:
  - `ProcessGloves = true`
  - `PaymentDate` defaults to:
    - previous month end if day < 16
    - current month day 15 otherwise.

### `FillVacancyViews`

- projected employee display appends `" (ec)"` when employee has alert phone/email.
- null projection text is fixed to `"No Employee Available"`.
- no-bid indicator checks only latest unassigned no-bid bulletin for assignment date.
- pool-aware contact view stores `RailroadPoolNumber` for downstream conditional UI/actions.

### `OnDutyRecordHistoryView`

- assignment display is fixed to `"{CrewName} {PositionName}"` string composition.
- assigned status renders fixed `"Yes"` / `"No"`.
- off-duty/time-on-duty values are blank strings when no off-duty record exists.

## 359.3 `DailyReportController` Hard-Coded Redirect + Audit Behavior

- `CovidReport` writes report-view audit record to class-library DB when `report` is non-empty and `rre != 0`.
- Redirect URL is hard-coded to SSRS endpoint:
  - `http://sql-svr/ReportServer?%2FOperational%2FPTRA%20COVID%20Activity%20Report&rs%3AParameterLanguage=en-US`

This endpoint is fixed and bypasses route-based report selection.

# Part 360: Strict Full Sweep Tracker (Increment 22 - Payroll Report Generation Deep Pass)

## 360.1 File Reviewed

- `StrategicApplications/Utilities/PayrollUtilities.cs` (batch/earning summary report sections)

## 360.2 Batch Summary Parsing Contract

`CreateBatchSummaryReport` reads from fixed file:

- `\\Finance-svr\Payroll Exports\ADP\EPIPT190.csv`

Hard-coded parser assumptions:

- comma-delimited ADP export structure with positional indexes (`fields[1]`, `fields[5]`, etc.)
- header row is identified by literal `"Co Code"`
- code `20` is specially aggregated into Hours4 bucket
- key buckets are string dictionaries with reserved keys:
  - `Batch {id}`
  - `Total`

Formatting rules are fixed-width text alignment with manual left-padding loops before output.

## 360.3 Earning Summary Aggregation Rules

`CreateEarningSummaryReport` applies fixed grouping topology:

- primary ordering differs for pool 10 vs all other pools
- aggregation dictionaries split by odd/even payroll code numeric parity:
  - odd codes -> totals set 1
  - even codes -> totals set 2

Totals are maintained at four levels simultaneously:

- employee
- craft
- pool
- grand total

Pool-10 branch includes additional craft-level subtotaling before pool totals, while non-pool-10 branch bypasses that specific craft sequence.

## 360.4 Output Channel Characteristics

- report builders produce plain text blocks later converted to PDF with Courier font in caller flows.
- `ProcessPayrollController.Status` is used as mutable progress text during long report generation.

# Part 361: Strict Full Sweep Tracker (Increment 23 - Payroll Reporting Endpoint Pass)

## 361.1 Files Reviewed

- `StrategicApplications/Controllers/PayrollReportController.cs`
- `StrategicApplications/Models/ModelViews/PayrollReportGroupViews.cs`

## 361.2 `PayrollReportController` Hard-Coded Reporting Flow

### `ACAReport()`

- Uses direct SQL connection string key:
  - `StrategicApplicationsContext`
- Executes stored procedure by literal name:
  - `ACAReport`
- Supplies one fixed parameter:
  - `@ReportYear = DateTime.Now.Year`

Result export behavior:

- materializes full result set into `DataTable`
- manually writes CSV using `,` delimiter and `\n` row terminator
- output filename is fixed to:
  - `AHCA Report.csv`

## 361.3 ModelView Contract Notes

`PayrollReportGroupViews` enforce fixed display and validation assumptions:

- `ReportGroupName` max length `50`
- group-number display labeling is fixed (`Category Number`, `Group Number`) depending on view type
- view model projection is direct entity-to-DTO mapping with no transformation rules beyond field copy.

# Part 362: Strict Full Sweep Tracker (Increment 24 - Payroll Import Endpoint Branches)

## 362.1 File Reviewed

- `StrategicApplications/Controllers/ProcessPayrollController.cs` (import endpoint region)

## 362.2 `ProcessADPImport` Hard-Coded Endpoint Logic

- Expects uploaded file reference but reads from fixed network path:
  - `\\finance-svr\c$\Payroll Exports\ADP\Imports\{file.FileName}`
- If file does not exist at that path:
  - logs warning
  - sets static status text
  - sleeps fixed `5000 ms`
- Processing delegates directly to service method:
  - `SAImportADPPayrollService.CreateEarningAmountPaidRecords(filepath)`

## 362.3 `ProcessPayrollImportFile` Endpoint Logic

- Shares same null-file and path-check pattern as ADP import endpoint.
- Uses same fixed UNC path root for file existence check.
- Actual processing uses uploaded stream via:
  - `PayrollUtilities.CreatePayrollRecordsFromImport(file)`

## 362.4 Shared Status Contract

- Both endpoints drive long-running UI text through static mutable field:
  - `ProcessPayrollController.Status`
- Status reset to empty string on completion.
- `StatusUpdate()` exposes this value via JSON GET:
  - `{ result = ProcessPayrollController.Status }`

# Part 363: Strict Full Sweep Tracker (Increment 25 - `SAClassLibraryContext` Mapping Rules)

## 363.1 File Reviewed

- `SAClassLibrary/Context/SAClassLibraryContext.cs` (fluent mapping region)

## 363.2 Time Precision and Temporal Field Mapping

`AssignmentOnDutyTime` fields are explicitly mapped to SQL precision `0`:

- `OnDutyTime`
- `CallingTimeStart`
- `CallingTimeEnd`

This is a hard-coded truncation policy for persisted time precision.

## 363.3 One-to-One and Optional-Dependent Cascade Contracts

The model uses many explicit optional 1:1 relationships with cascade delete enabled, including:

- `DailyCrewPositionOnDutyRecord` -> off-duty/late-call/did-not-work/on-duty-markoff/payroll-info/unavailable
- `DailyCrewPosition` -> annulment/do-not-fill/skip/moved-position
- `DailyAssignment` -> AFE/annulment/request
- `DailyAssignmentShift` -> completion
- `HoldDown` -> hold-down release

This creates deterministic dependent cleanup behavior when primary records are removed.

## 363.4 Many-to-Many / Link-Table Key Hard-Coding

Fluent mapping includes explicit join table naming and key column names (example):

- `CraftRequirements` link table
  - left key: `CraftControlNumber`
  - right key: `RequirementControlNumber`
- Identity role/user mapping to `AspNetUserRoles` with explicit key names.

## 363.5 Cascade Delete Suppression Zones

Several parent-child relationships explicitly disable cascade delete (`WillCascadeOnDelete(false)`), especially around core operational entities (`AssignmentOnDutyTime`, `AssignmentType`, `Employee`, `EmploymentStatus`, `Location`, etc.).

This enforces manual cleanup responsibility in higher-level business flows.

# Part 365: Strict Full Sweep Tracker (Increment 27 - `NewModelViews` Behavioral Defaults)

## 365.1 Files Reviewed

- `StrategicApplications/NewModelViews/BeSafeViews.cs`
- `StrategicApplications/NewModelViews/MarkOffCodeViews.cs`
- `StrategicApplications/NewModelViews/RailroadInformationViews.cs`
- `StrategicApplications/NewModelViews/RailroadInformationTypeViews.cs`
- `StrategicApplications/NewModelViews/SlowOrderViews.cs`

## 365.2 Common ViewModel Pattern Observations

- Most view models are direct projection wrappers with no service calls and minimal transformation.
- status/label outputs are frequently literalized (`Yes/No`, `Pending`, `Scheduled`, `Cancelled`, etc.).
- create/close/cancel forms commonly default date fields to `DateTime.Today`.
- multiple create/edit forms mark rich text fields with `[AllowHtml]`, reflecting intentional HTML payload acceptance.

## 365.3 BeSafe View-Specific Logic

- `BeSafeView.CanEdit` is computed by exact username match against current HTTP identity.
- `BeSafeView.Resolved` is inferred solely from presence of `BeSafeResolveRecord`.
- `BeSafeActionView.ActionDateStr` format is fixed to `MM/dd/yyyy hh:mm tt`.

## 365.4 Railroad Information View Status Logic

`RailroadInformationView` status derivation is branch-ordered:

1. no publish record -> `Pending`
2. published but not notified -> `Scheduled`
3. notified + cancel record -> `Cancelled`
4. close record present -> status overwritten to `Closed - {date}`

Close-state assignment has final precedence and can override prior status text.

## 365.5 Mark-Off Code View Constraints

- mark-off code display/editor contracts enforce `Code` length `2` and `Description` max length `250`.
- UI booleans expose multiple policy toggles directly (approval, holiday, employee self-service, record hours, allow request), reflecting hard-coded policy surface in forms.

## 365.6 Slow Order View Defaults

- complete view defaults `CompleteDate = DateTime.Today`.
- change view timestamps use fixed long-date display format (`MMMM dd, yyyy hh:mm tt`).
- index models carry `Areas` and `Statuses` selector placeholders for filtered list rendering.

# Part 366: Strict Full Sweep Tracker (Increment 28 - Remaining `NewModelViews` Catalog Pass)

## 366.1 Files Reviewed

- `StrategicApplications/NewModelViews/BeSafeAreaViews.cs`
- `StrategicApplications/NewModelViews/BeSafeCategoryViews.cs`
- `StrategicApplications/NewModelViews/BeSafeEmailGroupViews.cs`
- `StrategicApplications/NewModelViews/BeSafeSubdivisionViews.cs`
- `StrategicApplications/NewModelViews/SlowOrderAreaViews.cs`

## 366.2 CRUD ViewModel Contract Patterns

These files follow a consistent hard-coded CRUD DTO pattern:

- `IndexView` carries `RailroadControlNumber` + `RailroadMark_Name` context.
- `Create/Edit/Delete` views are strict property copies from entity with data-annotation display labels.
- create constructors seed railroad context from parent railroad object only (no derived defaults beyond that).

## 366.3 Naming/Display Literal Behavior

Display names are explicitly hard-coded for UI:

- BeSafe domain uses labels like `Subdivision Number`, `Email Group Number`, `District Name`.
- SlowOrderArea uses `Zone Number`/`Zone Name` display labels while underlying entity naming remains `SlowOrderArea...`.

This produces intentional terminology overlays between domain entities and UI vocabulary.

## 366.4 Relationship Exposure in Views

- `BeSafeAreaView` conditionally resolves `BeSafeSubdivisionName` only when subdivision navigation is present.
- `BeSafeCategoryView` projects linked email group name (`BeSafeEmailGroupName`) directly.

No fallback labels are injected for missing linked entities (other than null-safe omission in the area/subdivision view).

# Part 367: Strict Full Sweep Tracker (Increment 29 - Cross-Project Model Duplication Map)

## 367.1 Scope

Cross-project filename comparison between:

- `SAClassLibrary/Models`
- `StrategicApplications/Models`

## 367.2 Observed Duplication Topology

- Common model filename count: **195**
- SAClassLibrary-only filenames: **34**
- StrategicApplications-only filenames: **15**

This confirms a large mirrored model layer with selective divergence per project.

## 367.3 SAClassLibrary-Only Naming/Typo Variants (Notable)

Examples of class-library-only filenames indicating naming drift/legacy variants:

- `EmploymentStatu.cs`
- `HoldDownReleas.cs`
- `TemporaryAssignmentReleas.cs`
- `RailroadPositionChanx.cs`
- `MarkOffMarkUpHour.cs`

These are hard-coded artifact names in source and may represent legacy/partial model generation remnants.

## 367.4 StrategicApplications-Only Additions (Representative)

Examples unique to web project model folder:

- `IdentityModels.cs`
- `ApplicationUserManager.cs`
- reporting/grouping adjuncts (`PayrollCategoryCode.cs`, `PayrollReportGroupCategory.cs`)
- web-domain naming variants (`CraftPayCodes.cs`, `ObjectNotes.cs`, pluralized craft day models)

## 367.5 Architectural Implication

- The system encodes domain model behavior in two overlapping assemblies with non-trivial naming divergence.
- Any logic/spec sweep must account for both mirrored entities and per-project variants to avoid false equivalence.

# Part 368: Strict Full Sweep Tracker (Increment 30 - Migration Configuration Behavior)

## 368.1 Files Reviewed

- `StrategicApplications/Migrations/Configuration.cs`
- `SAClassLibrary/Migrations/Configuration.cs`

## 368.2 `StrategicApplications` Migration Configuration

Hard-coded migration/runtime behavior:

- `AutomaticMigrationsEnabled = true`
- context key fixed to:
  - `StrategicApplications.Models.Context.StrategicApplicationsContext`
- `Seed()` unconditionally calls `AddUserAndRoles()`.

`AddUserAndRoles()` uses fixed role bootstrap sequence:

- System Administrator
- Client Administrator
- Railroad Administrator
- Client Supervisor
- Railroad Supervisor
- Railroad Crew Dispatcher
- Railroad Employee
- Railroad Union Representative

Seeded admin user details are hard-coded:

- username: `Admin`
- name fields: `PTRA Administrator`
- created/modified by: `admin`
- initial password literal: `5@nD1eg0`

Role assignment hard-coded to `System Administrator`.

## 368.3 `SAClassLibrary` Migration Configuration

- `AutomaticMigrationsEnabled = false`
- `Seed()` contains no operational data seeding logic (comment-only template).

## 368.4 Cross-Project Migration Divergence

- Web project allows automatic schema migration and seeds identity/roles/admin credentials.
- Class library project disables automatic migrations and does not seed data.

This divergence is a core deployment-time behavior characteristic for environment provisioning.

# Part 369: Strict Full Sweep Tracker (Increment 31 - Safety/Information Controller Deep Pass)

## 369.1 Files Reviewed

- `StrategicApplications/Controllers/RailroadInformationController.cs`
- `StrategicApplications/Controllers/SlowOrderController.cs`
- `StrategicApplications/Controllers/BeSafeController.cs`

## 369.2 Shared Controller Patterns

- All three controllers use iText-based PDF generation with fixed Courier-style formatting and direct `Response.OutputStream` writes.
- Error branches uniformly log and return support-oriented model-state messages.
- Return navigation is strongly coupled to `returnUrl` pass-through redirects.

## 369.3 `RailroadInformationController` Hard-Coded Rules

- New records are initialized with sentinel `RecordNumber = 999999` before publish flow assigns final number.
- HTML description normalization is manually enforced:
  - trims tail chars
  - injects `<p>`/`</p>` wrappers if absent
  - replaces `<div>` with `<p>` before decode
- Non-admins can only view published records (`System Administrator` or `Railroad Information Administrator` bypass).
- `Unpublish` resets record number to `999999` and removes publish record.
- `View`/`ViewRecords` output file header is fixed:
  - `Railroad Information Report.pdf`

## 369.4 `SlowOrderController` Hard-Coded Rules

- index status selector defaults to string `"Open"`; non-open path parses `status` as integer days for closed-window query.
- edit operation always writes a `SlowOrderChangeRecord` snapshot before mutating current record.
- complete/delete actions create explicit complete/delete records instead of hard deleting source record.
- PDF endpoints always emit filename `Slow Order Report.pdf`.
- report header text in PDF is fixed to `PTRA Track Bulletins` and standard train-notice language.

## 369.5 `BeSafeController` Hard-Coded Rules

- index defaults: `status = "Open"`, with closed path using integer-day parse logic.
- employee role branch:
  - `Railroad Employee` users default filter to their own employee control number.
  - non-employee roles default to "All Employees" selector.
- record number generation uses YY-prefixed sequence:
  - first record each year -> `{yy}0001`
  - otherwise increment last record number.
- action creation sends notification message with fixed prefix:
  - `The following action has been recorded for Be Safe ...`
- `View` endpoint currently emits `Slow Order Report.pdf` filename (shared literal with slow-order controller).

# Part 370: Strict Full Sweep Tracker (Increment 32 - Area/Category/Admin Catalog Controllers)

## 370.1 Files Reviewed

- `StrategicApplications/Controllers/BeSafeAreaController.cs`
- `StrategicApplications/Controllers/BeSafeCategoryController.cs`
- `StrategicApplications/Controllers/BeSafeEmailGroupController.cs`
- `StrategicApplications/Controllers/BeSafeSubdivisionController.cs`
- `StrategicApplications/Controllers/SlowOrderAreaController.cs`
- `StrategicApplications/Controllers/RailroadInformationTypeController.cs`

## 370.2 Shared CRUD Behavioral Pattern

All reviewed controllers implement a similar hard-coded CRUD flow:

- index builds view models from collection queries.
- create/edit set `CreatedBy/ModifiedBy` using `User.Identity.Name`.
- delete performs direct entity removal (no soft-delete record in this catalog layer).
- all error paths log with `EventLogger` and return support-oriented `ModelState` messages.

## 370.3 Numbering Seed Rules

Each catalog create action seeds the next number with same pattern:

- `nextnbr = 10`
- find last record by descending number
- if present, `nextnbr += lastNumber`

This creates leap-style numbering growth (increment by +10 from prior absolute value), not simple `+1`.

Applied in:

- `BeSafeAreaNumber`
- `BeSafeCategoryNumber`
- `BeSafeEmailGroupNumber`
- `BeSafeSubdivisionNumber`
- `SlowOrderAreaNumber`
- `RailroadInformationType.TypeNumber`

## 370.4 Context Usage Split

- BeSafe/SlowOrder catalog controllers use `SAClassLibraryContext`.
- `RailroadInformationTypeController` uses `StrategicApplicationsContext`.

This split mirrors domain-storage separation observed in other workflow controllers.

## 370.5 Notable Binding/Field Contract Details

- `BeSafeCategoryController` bind include strings contain typographical inconsistencies (`ControlNumber.BeSafeEmailGroupControlNumber`, `BeSafeCategoryaNumber`) but runtime logic sets values from strongly typed view model properties.
- all create/edit actions trust posted numeric identifiers for linked entities (subdivision/email group/type) and apply them directly.

# Part 371: Strict Full Sweep Tracker (Increment 33 - Notification/Home/Print + FillVacancy Controller Pass)

## 371.1 Files Reviewed

- `StrategicApplications/Controllers/NotificationController.cs`
- `StrategicApplications/Controllers/HomeController.cs`
- `StrategicApplications/Controllers/PrintPDFController.cs`
- `StrategicApplications/Controllers/FillVacancyController.cs`

## 371.2 `NotificationController` Hard-Coded Workflow Rules

- `Index` uses `days == 0` as open/active notification path; non-zero routes to history query.
- `AcceptNotification` writes a synthetic electronic confirmation note:
  - `Accepted in Crew Management System by ...`
- notify/edit forms use fixed select-list providers for notification type, yes/no, and employee notification numbers.
- after notification edit save, controller force-updates hangout timer for pool:
  - `MvcApplication.SetHangoutTimer(...)`

## 371.3 `HomeController` Auth Reset + Default Routing

- `Index` always signs out existing auth/session state and redirects to `Home` action.
- `DefaultView` role branch rules:
  - railroad employee/union representative -> `EmployeeDetail/Details`
  - others -> `Admin/DefaultView` with fixed return targets
- if password reset policy triggers (`CheckPasswordReset`) user is redirected to `Account/ChangePassword` with fixed `returnUrl = "Login/"`.

## 371.4 `PrintPDFController` Contract

- controller is role-restricted at class level to admin/dispatcher/supervisory roles.
- only exposed action in reviewed file (`PrintSeniority`) renders PDF title literal:
  - `Crew Management - Unassigned Employees`
- output view template name is fixed:
  - `PrintSeniority`

## 371.5 `FillVacancyController` Additional Hard-Coded Logic

- board selection switch uses fixed integer board IDs (`0,1,2,4,5,6,default`) to select data source and return button label.
- `AcceptCall` late-call default is triggered when current time exceeds vacancy `EndCallTime` and board != 0.
- successful fill path sequence:
  - `FillVacancy(...)`
  - optional create `DailyCrewPositionOnDutyRecordLateCall`
  - set overtime board order
  - remove vacancy records
  - append `FillVacancyLog` record
- `ChangeArrival` recalculates previous rest by subtracting duty-time delta and recomputes consecutive days using FRA rest threshold.

# Part 372: Strict Full Sweep Tracker (Increment 34 - Mark-Off Controller Family Pass)

## 372.1 Files Reviewed

- `StrategicApplications/Controllers/MarkOffCodeController.cs`
- `StrategicApplications/Controllers/MarkOffRecordController.cs`
- `StrategicApplications/Controllers/MarkOffRequestController.cs`

## 372.2 `MarkOffCodeController` Hard-Coded Rules

- non-system-admin users cannot see `SystemUseOnly` mark-off codes in index listing.
- code values are forced uppercase on create/edit.
- newly-created code sets fixed `ReportCode = "O"`.
- controller mixes contexts in subflows:
  - primary CRUD via `SAClassLibraryContext`
  - payroll-code and approval-officer child inserts via `StrategicApplicationsContext` in local `using` scope.

## 372.3 `MarkOffRecordController` Hard-Coded Branches

- index open/closed mode toggles by literal `recs == "Open"`; otherwise `recs` is parsed as day-count window.
- paid-day index uses literal search sentinels:
  - `"99"` -> reset code filter to empty
  - `"0"` -> set `all = false`
- create mark-off defaults vary by pool number:
  - pools 10/20/30: off-day roll-forward and extra-board call-time handling
  - pool 50: force mark-off time to `12:01 AM`
- create/mark-up flows call vacancy refresh after processing:
  - `ApplicationUtilities.UpdateDailyCrewPositionVacancies(...)`
- create flow may block while vacancy processing flag is active when missed-call/on-call path is used.

## 372.4 `MarkOffRequestController` Hard-Coded Branches

- request-create visibility window is pool-specific:
  - pool 10 -> 45-day horizon
  - pool 20 -> 60-day horizon
  - default -> remainder-of-year horizon
- request records use mark-off datetime normalized to `RequestDate + 1 minute`.
- automatic mark-off path creates real mark-off immediately if request datetime is already in past.
- mark-up day selector for edit is constrained by fixed hour buckets:
  - `24/48/168/336/504/672/840` with mapped day/week option sets.
- vacation-week code branches trigger vacation-waitlist reassignment; non-vacation date changes trigger compensable-day waitlist reassignment.

# Part 373: Strict Full Sweep Tracker (Increment 35 - Payroll Definition Controller Pass)

## 373.1 Files Reviewed

- `StrategicApplications/Controllers/PayrollCodeController.cs`
- `StrategicApplications/Controllers/PayrollCategoryController.cs`
- `StrategicApplications/Controllers/PayrollCodeApprovalRoleController.cs`

## 373.2 `PayrollCodeController` Hard-Coded Behavior

- role gate is fixed to:
  - `System Administrator`, `Client Administrator`, `Railroad Timekeeper`.
- create/edit forces payroll code text uppercase.
- create/edit view always loads fixed yes/no and compensable-type select lists.
- fields like `Accumulator`, `CanBeSold`, `CompensationType`, `DefaultTime/Overtime/Amount` are directly mapped from UI to entity with no additional derived logic.

## 373.3 `PayrollCategoryController` Hard-Coded Behavior

- similar role gate as payroll code controller.
- category create/edit directly map:
  - `PayrollCategoryNumber`
  - `ReportSortNumber`
  - `StraightTime`, `Overtime`, `Amount` booleans/flags
- index search filter is prefix-based on `PayrollCategoryName`.

## 373.4 `PayrollCodeApprovalRoleController` Behavior

- create path computes available roles by subtracting already-mapped approval roles from full identity-role set.
- selected role is persisted with both:
  - `RoleId`
  - `RoleName` snapshot at creation time
- includes `Primary` boolean flag for approval role precedence.
- if payroll code or role lookup fails, controller throws explicit exception messages and logs support error.

# Part 374: Strict Full Sweep Tracker (Increment 36 - Payroll Grouping/Mapping Controllers)

## 374.1 Files Reviewed

- `StrategicApplications/Controllers/PayrollReportGroupController.cs`
- `StrategicApplications/Controllers/PayrollReportGroupCategoryController.cs`
- `StrategicApplications/Controllers/PayrollCategoryCodeController.cs`

## 374.2 Shared Controller Pattern

- all reviewed controllers are role-gated to payroll admin roles (`System Administrator`, `Client Administrator`, `Railroad Timekeeper`).
- many-to-many mapping entities are created via static factory helpers (`CreateInstance(...)`) using parent+child IDs.
- delete actions remove mapping rows directly by composite key lookup.

## 374.3 `PayrollReportGroupController` Behavior

- index search is prefix-based on `ReportGroupName`.
- create/edit map only number/name metadata and audit fields.
- no auto-number logic is applied in controller; number is user-supplied.

## 374.4 `PayrollReportGroupCategoryController` Behavior

- index materializes categories through join table (`PayrollReportGroupCategories`) ordered by category number.
- create action exposes available categories via `SelectLists.GetPayrollCategories(client)`.
- create persists relationship row without additional validation beyond model state and DB save constraints.

## 374.5 `PayrollCategoryCodeController` Behavior

- index resolves payroll-code list through `PayrollCategoryCodes` mapping table.
- create action offers payroll codes for the client and inserts one category/code mapping row.
- delete removes mapping by composite key `(category, code)`.

# Part 375: Strict Full Sweep Tracker (Increment 37 - `PayrollController` Deep Pass)

## 375.1 File Reviewed

- `StrategicApplications/Controllers/PayrollController.cs`

## 375.2 Index Filtering and Period Behavior

- Payroll index period selection uses `payperiod` with special custom-date branch only for value `10`.
- all other payperiod values route through `ApplicationUtilities.GetStartEndPeriod(payperiod)`.
- `ViewBag.CanCreate` is hard-coded as `payperiod < 7`.

Filters supported in query path:

- railroad employee
- code
- craft/pool
- review status flag (`"false"` default)

## 375.3 Manual Payroll Create Contracts

### Job/assignment formatting by pool number

`Create` action builds assignment/job text with pool-specific formats:

- pools 10/20/40 -> `{assignment}{positionCode}`
- pools 30/60 -> `{positionCode}{assignment}`
- pool 50 -> assignment only

Additional injected yard/engine literals include:

- `Yardman - 100H`
- `Yardman - 100F`
- `Yardman - 101H`
- `Yardman - 101F`

### Manual-entry and review-required side effects

- `payrec.ManualEntry` is set by comparing current user employee number to target employee number.
- every manual create also inserts `PayrollReviewRequiredRecord` with fixed reason prefix:
  - `Payroll record was manually entered by ...`

### Accounting fallback literals

If payroll department lookup fails, these fields are set to literal `"Not Found"`:

- ICC number
- department number
- general ledger number

## 375.4 Earning/Create Validation Behavior

- selected earning code is mandatory; missing code throws exception with explicit control-number text.
- compensable-account debit logic can override ST hours before earning record creation.
- approval-required records are created using `PayrollUtilities.GetApprovalOfficer(...)` result.
- object-note append behavior concatenates with `\r\n` when note already exists.

# Part 376: Strict Full Sweep Tracker (Increment 38 - Mark-Off Waitlist Workflow Pass)

## 376.1 File Reviewed

- `StrategicApplications/Controllers/MarkOffRequestWaitListController.cs`

## 376.2 Waitlist Query and View Routing Rules

- `Index` resolves request-linked waitlist records via join table (`MarkOffRequestMarkOffRequestWaitListRecords`).
- if no linked records exist, flow redirects to `WaitListIndex`.
- `WaitListIndex` defaults selected request date to first available vacation-week option when no date is provided.

## 376.3 Create/Link Behavior

Two create paths exist:

1. employee-linked waitlist insert from existing request (`WaitList` action)
2. dispatcher/admin manual waitlist insert (`Create` action)

Both paths persist:

- waitlist record
- optional link row to mark-off request record

Shared assignment defaults include:

- `EntryDateTime = now`
- mark-off code + employee/craft snapshot copied from request/employee context.

## 376.4 Vacation-Week Specific Filters

Vacation-week source lists repeatedly use fixed filter rules:

- mark-off code starts with `V`
- excludes `VD`
- request date must be in future (`> today`)

These filters drive edit/approve selection lists.

## 376.5 Approval Flow Contracts

- supervisor role special case: officer list is constrained to current logged-in employee only.
- otherwise officer list uses craft approval officer selector.
- approve action:
  - ensures link-row exists when request-control number supplied
  - attempts to find existing compatible mark-off request
  - if missing, auto-creates one via `AssignCompensableDayWaitListRequest` and adds approval record
  - if found, removes waitlist record.

# Part 378: Strict Full Sweep Tracker (Increment 40 - Pay Rate Controller Cluster)

## 378.1 Files Reviewed

- `StrategicApplications/Controllers/PayrollCodePayRateController.cs`
- `StrategicApplications/Controllers/EngineerPayRateController.cs`
- `StrategicApplications/Controllers/PositionPayRateController.cs`

## 378.2 Shared CRUD and Access Pattern

- all three controllers are restricted to payroll/hr/admin role sets.
- index actions sort by effective date descending within entity-specific grouping.
- create/edit/delete follow direct entity mutation with audit field updates and support-style error messages.

## 378.3 `PayrollCodePayRateController` Hard-Coded Rules

- create action preloads position selector from first pool matching `PoolNumber == 10`.
- payroll-code selector is limited to arbitrary payroll codes (`GetArbitraryPayrollCodes`).
- index ordering prioritizes payroll code, then position, then effective date descending.

## 378.4 `EngineerPayRateController` Hard-Coded Rules

- engineer rate model carries four explicit rate channels:
  - `ESTHourRate`, `EOTHourRate`, `TSTHourRate`, `TOTHourRate`
- helper endpoint `GetOTRate(rate)` returns `rate * 1.5` rounded to 4 decimals.

## 378.5 `PositionPayRateController` Hard-Coded Rules

- position rate model captures fixed ST/OT rate pair per effective date.
- no derived validation or cross-rate normalization is applied in controller layer.

# Part 379: Strict Full Sweep Tracker (Increment 41 - `DailyAssignmentController` Deep Pass)

## 379.1 File Reviewed

- `StrategicApplications/Controllers/DailyAssignmentController.cs`

## 379.2 Index and Crew-Position Projection Rules

- index computes `rremponly` based on requester role flags (`IsRailroadEmployeeRoleOnly` / `IsUnionRepresentativeRole`).
- position summary builds `ExtraPosition` entries by counting repeated daily crew positions per assignment/position.
- refresh interval is hard-coded:
  - `ViewBag.Refresh = 300`

## 379.3 Queue-Based Crew-Position Creation

- `CreateCrewPositions` emits MSMQ `DailyCrewPosition` create messages when daily assignment has no crew positions.
- payload is fixed comma sequence:
  - daily assignment CN, railroad position CN, assignment date, extra-board flag, crew CN, position CN.

## 379.4 Create-On-Duty / Create Assignment Branching

- create-on-duty defaults to first on-duty time after current time for employee pool; fallback is first pool on-duty time.
- `Create` action accepts either daily-shift ID or pool-employee ID in the same `id` parameter and branches accordingly.
- if no matching daily shift exists for selected on-duty date/shift, controller creates in-memory/persisted `DailyAssignmentShift`.

Pool-specific defaults during create:

- pool 40: AFE hidden; billable/recollectable false.
- pool 50: may hide fields and inject request note for manual/employee path.
- default: shift assignments + billable/recollectable false + emergency callout false.

## 379.5 Assignment Create Persistence Sequence

Within transaction scope, create flow performs in order:

1. ensure/create daily shift
2. create `DailyAssignment`
3. create `DailyAssignmentRequest`
4. create `DailyAssignmentCrew`
5. optional `DailyAssignmentAFERecord`
6. save
7. call `CreateDailyCrewPositions(...)`

Post-transaction side effects:

- `MvcApplication.SetAtHocMessageTimer(pool)`
- vacancy refresh (`UpdateDailyCrewPositionVacancies`)
- shift completion reevaluation (`CompleteDailyAssignmentShift`).

# Part 380: Strict Full Sweep Tracker (Increment 42 - Daily Shift/Crew Position Controllers)

## 380.1 Files Reviewed

- `StrategicApplications/Controllers/DailyAssignmentShiftController.cs`
- `StrategicApplications/Controllers/DailyCrewPositionController.cs`

## 380.2 `DailyAssignmentShiftController` Hard-Coded Behavior

- index status/open handling uses `open` integer parameter; non-zero path reorders sheets descending by date/shift.
- `Create` emits MSMQ `DailyAssignmentShift` message with fixed CSV body fields:
  - pool CN, shift CN, assignment date (`yyyy-MM-dd`), create-crew-positions flag.
- view paths auto-redirect to board detail when only one extra-board/overtime-board option exists.
- move-board-position actions renormalize board order in fixed increments:
  - start at `1000`
  - increment by `10`.
- refresh action deletes all daily assignments for shift then recreates call-sheet assignments and recalculates vacancies.

## 380.3 `DailyCrewPositionController` Hard-Coded Behavior

- manual create path uses sentinel railroad-position control number:
  - `99999999999999999` (default railroad position)
- created manual positions are marked `ExtraBoardOnly = true`.
- tie-up operation:
  - creates off-duty records for all non-tied-up on-duty records
  - can emit manual tie-up notification for payroll-processing employees
  - writes vacancy update request file (`DailyCrewPositionController_Tieup`).

### Remove/Release/Annul/DoNotFill branches

- remove/release delete latest on-duty record and can reset extra-board tie-up order using assignment tie-up metadata.
- release optional penalty-claim path creates 3-hour manual payroll record using payroll code `44` and review-required reason text.
- annul and do-not-fill paths create off-duty records and then wait on vacancy-processing gate before vacancy recalculation.

## 380.4 Shift Completion and Vacancy Side Effects

- multiple branches call `CompleteDailyAssignmentShift(...)` after position state changes.
- vacancy refresh/update is a recurring hard-coded side effect after create/delete/remove/release/annul/do-not-fill operations.

# Part 381: Strict Full Sweep Tracker (Increment 43 - On-Duty Billing Controller Cluster)

## 381.1 Files Reviewed

- `StrategicApplications/Controllers/DailyOnDutyAFEBillingController.cs`
- `StrategicApplications/Controllers/DailyOnDutyMiscellaneousBillingController.cs`
- `StrategicApplications/Controllers/DailyOnDutyZoneBillingController.cs`
- `StrategicApplications/Controllers/DailyOnDutyFlagBillingController.cs`

## 381.2 Shared CRUD Pattern

- each controller anchors records to a `DailyCrewPositionOnDutyRecord` parent.
- create/edit flows map view-model fields directly onto billing record entities.
- audit fields (`CreatedBy/Date`, `ModifiedBy/Date`) are set from current user and current time.
- delete removes records directly from DB set.

## 381.3 Billing-Dimension Specific Behavior

### AFE billing

- selected railroad AFE control number is resolved to snapshot fields:
  - `AFENumber`
  - `AFEDescription`
- stores separate ST/OT billed hours (`STBHours`, `OTBHours`).

### Miscellaneous billing

- binds both work code and location control numbers.
- exposes explicit billable flag and free-form notes.
- helper endpoint `SetBillableFlag(workCodeId)` returns string literal `"True"`/`"False"` based on work-code billable setting.

### Zone billing

- selected zone control number is resolved to snapshot fields:
  - `ZoneNumber`
  - `ZoneName`
- stores ST/OT billed hours.

### Flag billing

- stores `ProjectName`, `Billable`, and single-hour quantity field.

# Part 382: Strict Full Sweep Tracker (Increment 44 - On-Duty Equipment/Material Controllers)

## 382.1 Files Reviewed

- `StrategicApplications/Controllers/DailyOnDutyLocomotiveRecordController.cs`
- `StrategicApplications/Controllers/DailyOnDutyRailroadMaterialRecordController.cs`
- `StrategicApplications/Controllers/LocomotiveInspectionRecordController.cs`

## 382.2 `DailyOnDutyLocomotiveRecordController` Rules

- default locomotive type on create comes from first `RailroadLocomotiveType` marked `Default`.
- if prior locomotive records exist for same on-duty record, last record’s type is reused as default.
- locomotive IDs are normalized to uppercase on create/edit.
- helper endpoint `GetLocomotiveWeight(typeId)` returns locomotive type name + weight pair.

## 382.3 `DailyOnDutyRailroadMaterialRecordController` Rules

- create/edit resolve selected material control number into snapshot fields:
  - category name
  - material type/code/description/unit indicator
- quantity is stored with these snapshots; selector source is railroad material type list by railroad.
- edit preselects material by matching stored `MaterialCode`.

## 382.4 `LocomotiveInspectionRecordController` Rules

- `Create` redirects to `Edit` when inspection record already exists for daily locomotive record.
- inspected locomotive ID is uppercased on create.
- inspection location selector derives from active craft/pool location set.
- edit updates inspection timing/fuel/repairs and audit fields; no delete endpoint is present in reviewed file.

# Part 383: Strict Full Sweep Tracker (Increment 45 - Tie-Up and Related On-Duty Controllers)

## 383.1 Files Reviewed

- `StrategicApplications/Controllers/DailyOnDutyRecordTieUpController.cs`

## 383.2 Tie-Up Entry Routing Matrix

`TieUpProcess` routes by pool and state:

- pool 10:
  - if not on-duty-updated -> redirect to arrival-change flow
  - if unrestricted + engineer craft -> locomotive flow
  - else payroll flow
- pool 20/30:
  - if trainees exist -> payroll flow
- pool 40:
  - if assignment location contains `"Rip"` -> payroll flow
- pool 50:
  - always `MofWBilling` flow
- default fallback -> generic tie-up create flow.

## 383.3 Clerical Pay Grade Hard-Coding

Controller seeds static dictionary with fixed job-code -> pay-grade mappings (examples: `102->4`, `170->5`, `100/112/130/199->8`).

This is in-memory static mapping logic local to controller.

## 383.4 Locomotive Flow Rules

- total locomotive weight is summed across on-duty locomotive records.
- job-paid code is selected by first engineer job-code threshold where `MaxWeightOnDrivers >= total`.
- fallback is highest threshold when none match.
- trainee roster path switches to trainee pay-class code.

## 383.5 MofW Billing/Payroll Information Rules

- MofW billing aggregates four data sets for display:
  - railroad material records
  - AFE billing records
  - zone billing records
  - miscellaneous billing records
- meal-period defaults write claimed minutes as:
  - meal period true -> `0`
  - meal period false -> `30`

## 383.6 Payroll Tie-Up Rule Matrix

pool-based UI toggles in payroll view:

- pool 10: first/second meal + air pay paths enabled based on selections/time.
- pool 40: second meal + air pay forced false; first-meal claim options adjusted.
- other pools: meal/air claims forced false.

Additional hard-coded threshold:

- second meal eligibility suppressed when `TimeOnDuty <= 9:19`.

# Part 384: Strict Full Sweep Tracker (Increment 46 - `AssignmentController` Pass)

## 384.1 File Reviewed

- `StrategicApplications/Controllers/AssignmentController.cs`

## 384.2 Index/Detail/Select Hard-Coded Branches

- index filters active assignments by `AssignmentAbolishment == null` and optional assignment type/search prefix.
- assignment-select source differs for pool 40:
  - pool 40 -> mechanical assignment selector
  - default -> standard railroad-pool assignment selector.
- details view excludes extra-board-only assignment types and only includes effective/non-abolished records.

## 384.3 Board Order Recompute Flow

`SetBoardOrder` action recomputes board order for:

- assignment master records
- assignment on-duty-day records
- future/open daily assignments

Additional hard-coded side effect:

- fills missing `WorkArea` from assignment location name.

## 384.4 Create/Edit Assignment Rules

- board order is always derived via `assignment.SetBoardOrder(...)` rather than accepting posted value.
- create/edit both reload lookup lists (`OnDutyTimes`, `Locations`, yes/no) on failure.
- created assignment records capture:
  - type, on-duty time, location, assignment number/name, effective date, assigned air pay, work area.

# Part 385: Strict Full Sweep Tracker (Increment 47 - Assignment On-Duty Day/Time + Cutoff Controllers)

## 385.1 Files Reviewed

- `StrategicApplications/Controllers/AssignmentOnDutyTimeController.cs`
- `StrategicApplications/Controllers/AssignmentOnDutyDayController.cs`
- `StrategicApplications/Controllers/OnDutyMoveCutOffTimeController.cs`

## 385.2 `AssignmentOnDutyTimeController` Behavior

- manages pool-level on-duty/calling windows keyed by shift.
- index ordering is fixed by shift ID then on-duty time.
- create/edit write explicit `OnDutyTime`, `CallingTimeStart`, `CallingTimeEnd` values and standard audit fields.

## 385.3 `AssignmentOnDutyDayController` Behavior

- on-duty-day create calculates board order using assignment type, location board order, and selected on-duty time.
- crew assignment flow (`Assign`) creates `CrewAssignment` with `AssignedDate = DateTime.Today`.
- unassign deletes `CrewAssignment` row directly.
- weekday options for on-duty day creation are sourced as unassigned days for target assignment.

## 385.4 `OnDutyMoveCutOffTimeController` Behavior

- index lists craft-specific move cutoff times for a specific `AssignmentOnDutyTime`.
- create uses unassigned-craft selector (`GetUnassignedMoveCutOffCrafts`).
- each cutoff binds one craft + one assignment-on-duty-time with explicit `MoveCutOffTime`.

# Part 386: Strict Full Sweep Tracker (Increment 48 - Crew/CrewPosition Controller Cluster)

## 386.1 Files Reviewed

- `StrategicApplications/Controllers/CrewController.cs`
- `StrategicApplications/Controllers/CrewPositionController.cs`
- `StrategicApplications/Controllers/CrewOffDayController.cs`

## 386.2 `CrewController` Hard-Coded Behavior

- index supports shift filter and crew-ID prefix search; if search includes `"Relief"`, only trailing character is used as filter token.
- lineup excludes extra-board-only assignment types and orders crews differently for pool 50 (by crew number).
- daily crew report action delegates to `CreateRailroadPoolCrewPositionHistoryRecords(...)`.

## 386.3 `CrewPositionController` Hard-Coded Behavior

- create flow seeds a new railroad position with class code `"C"`.
- create transaction sequence:
  - create railroad position
  - create crew position
  - create initial bulletin
  - add corresponding open daily crew positions for active daily crews
  - update vacancy list.
- delete flow performs multi-step teardown:
  - unassign employee if occupied
  - remove bulletins and seniority moves
  - release hold-downs
  - remove open daily crew positions
  - persist `DeletedRailroadPosition` record.

## 386.4 `CrewOffDayController` Hard-Coded Behavior

- index ordering is by weekday number, but reverses order when `Crew.AddCrewOffDayValues == 8`.
- create updates parent crew modified metadata as part of off-day insert.
- delete uses composite key (`crew`, `day`) row removal.

# Part 387: Strict Full Sweep Tracker (Increment 49 - Abolishment + Temporary Assignment Controllers)

## 387.1 Files Reviewed

- `StrategicApplications/Controllers/CrewAbolishmentController.cs`
- `StrategicApplications/Controllers/AssignmentAbolishmentController.cs`
- `StrategicApplications/Controllers/TemporaryAssignmentController.cs`

## 387.2 `CrewAbolishmentController` Hard-Coded Behavior

- abolishment process computes end-of-tour datetime via `crew.GetWorkEndDateTime(abolishmentDate)`.
- for each crew position, flow can:
  - unassign occupied railroad position
  - remove unassigned seniority moves
  - assign employee to hangout position
  - auto-complete open notifications when hangout assigned.
- additional teardown removes bulletins/seniority moves/hold-downs and future daily assignment crews after abolish date.

## 387.3 `AssignmentAbolishmentController` Hard-Coded Behavior

- removes open future daily assignments beyond abolishment date.
- removes on-duty-day rows only when linked crew assignment exists and crew is relief shift.
- creates `AssignmentAbolishment` audit record with posted abolishment date.

## 387.4 `TemporaryAssignmentController` Hard-Coded Behavior

- create/edit pool 50 (MofW) uses AFE visibility based on recollectable flag; non-50 pools force billing/recollectable false and AFE control number 0.
- create/edit support optional object notes and optional temporary-assignment AFE record snapshots.
- edit sequence includes:
  - delete/recreate temporary daily assignments
  - release open hold-down records
  - regenerate daily shift temporary assignments
  - refresh vacancies (pool or roster-scoped based on return value).

Assignment/Release specifics:

- assign action selects candidates from qualification list tied to today’s on-duty-day crew position.
- release action creates `TemporaryAssignmentRelease` and deletes future open temporary daily assignments beyond released date.

# Part 388: Strict Full Sweep Tracker (Increment 50 - `TemporaryAssignmentWorkDayController` Pass)

## 388.1 File Reviewed

- `StrategicApplications/Controllers/TemporaryAssignmentWorkDayController .cs`

## 388.2 Notable File-Level Characteristic

- controller filename includes a trailing space before `.cs` in repository path.

This is an explicit repository artifact that can affect tooling/path matching.

## 388.3 Controller Behavior

- index lists temporary-assignment work days ordered by weekday number.
- create inserts one weekday row and immediately regenerates temporary daily assignments via `CreateDailyShiftTemporaryAssignments`.
- delete removes matching temporary daily assignments for the deleted weekday before deleting workday record.
- both create/delete actions recalculate vacancies after assignment regeneration/removal.

## 388.4 Logging/Message Literal Notes

- error log strings contain typo variants (`emporaryAssignment...`, `AemporaryAssignment...`) in failure branches.

# Part 389: Strict Full Sweep Tracker (Increment 51 - Railroad Pool Employee + Seniority Controllers)

## 389.1 Files Reviewed

- `StrategicApplications/Controllers/RailroadPoolEmployeeController.cs`
- `StrategicApplications/Controllers/RailroadPoolEmployeeSeniorityController.cs`

## 389.2 `RailroadPoolEmployeeController` Hard-Coded Behavior

- `Index` delegates employee retrieval to `RailroadPool.GetRailroadPoolEmployees(...)` with status/roster/search filters.
- `UnassignedIndex` exposes timer status text from two app-level timer dictionaries (hangout + roster-board hangout updates).
- bulletin visibility branch for clerical pool (`PoolNumber == 30`) uses non-qualified bulletin query path; other pools use qualified query path.
- bulletin view-tracking writes record into `SAClassLibraryContext` when requester views own bulletins.

### Create/Edit/Status/Assign flows

- create user path uses employee number as initial password when new identity user is created.
- user role assignment logic attempts to add both selected role and primary role.
- edit normalizes issuing state to uppercase and strips nonnumeric chars from SSN.
- status change can redirect to seniority create/select flow based on result token (`Create` / `Select`).
- assign action branch:
  - if target position is bulletined -> redirects to bulletin assign flow
  - else unassigns current positions and manually assigns target position.

## 389.3 `RailroadPoolEmployeeSeniorityController` Behavior

- seniority index defaults to active-only view (active state and no end-date).
- create action uses unassigned-roster selector and seniority-state selector.
- create/select both trigger `SendCraftMessage()` side effect after successful activation/creation.
- select action hard-sets selected seniority to active state (`StateID = 1`), marks last active roster, and assigns railroad position for roster on change date.

# Part 390: Strict Full Sweep Tracker (Increment 52 - Bulletin Controller Cluster)

## 390.1 Files Reviewed

- `StrategicApplications/Controllers/RailroadPositionBulletinController.cs`
- `StrategicApplications/Controllers/RailroadPositionBulletinBidController.cs`
- `StrategicApplications/Controllers/RailroadPoolEmployeeBulletinBidController.cs`

## 390.2 `RailroadPositionBulletinController` Hard-Coded Behavior

- index open/history mode uses `recs == "Open"` else parses day-count history window.
- create requires roster bulletin rule; missing rule produces explicit model-state error path.
- create bulletin side effects:
  - removes pending seniority moves effective at/before bulletin effective datetime
  - logs cancellation notifications for removed moves
  - writes interface file (`Bulletin`, `Add`)
  - triggers bulletin timer update.
- set-no-bid flow calls `SetNoBid(...)` and persists no-bid state.

## 390.3 `RailroadPositionBulletinBidController` Hard-Coded Behavior

- employee list source differs by pool:
  - pools 30/40 -> all pool employees without bids
  - default -> standard pool employees without bids
- create/delete bid actions emit interface files via parent bulletin (`Bid Add/Delete`).

## 390.4 `RailroadPoolEmployeeBulletinBidController` Behavior

- role-restricted employee users can only manage their own bid records; mismatches redirect to login.
- create writes direct bid row with preference and audit metadata.
- delete resolves bid by bulletin + employee composite pair and removes it.

# Part 391: Strict Full Sweep Tracker (Increment 53 - Railroad Pool/Position Controller Cluster)

## 391.1 Files Reviewed

- `StrategicApplications/Controllers/RailroadPoolController.cs`
- `StrategicApplications/Controllers/RailroadPositionController.cs`

## 391.2 `RailroadPoolController` Hard-Coded Behavior

- index refresh hint is fixed to `300` seconds (`ViewBag.Refresh`).
- index can limit pools to assigned-only when requester railroad employee has `AssignedPoolsOnly` flag.
- off-day and roster-markoff generation actions run date-range loops with explicit record creation criteria (including 24-hour threshold checks for markoff records).
- compensable-record creation maps vacation-week MO codes (`V1..V5`) to `VW` compensation type.
- pool create/edit toggles include automation flags (`AutoBulletins`, `AutoMoves`, `AutoHangouts`, `AutoCallSheets`, `AutoVacancyAssignments`, `ElectronicCrewCalling`), and both actions call `MvcApplication.CreateTimers()` after successful save.

## 391.3 `RailroadPositionController` Hard-Coded Behavior

- history action defaults to 3-day window and supports position/employee filter combinations.
- history display usernames map special creator tokens:
  - `admin` -> `Administrator`
  - `autoprocess` -> `Assignment Process`
- assign/unassign behavior includes protective date clamping for non-system-admin users (past dates pushed to now).
- assign/unassign lifecycle performs consistent sequence:
  - remove seniority moves
  - release hold-downs
  - unassign position
  - create bulletin
  - assign employee to hangout when appropriate
  - remove unassigned seniority moves.
- undo flow reconstructs latest prior position assignment from position-history snapshot and requeues on-duty message creation for open unfilled daily crew positions.

# Part 392: Strict Full Sweep Tracker (Increment 54 - Qualification + Seniority Move Controllers)

## 392.1 Files Reviewed

- `StrategicApplications/Controllers/QualificationController.cs`
- `StrategicApplications/Controllers/RailroadPoolEmployeeQualificationController.cs`
- `StrategicApplications/Controllers/SeniorityMoveController.cs`

## 392.2 Qualification Controller Rules

- qualification index sorting prioritizes employee last/first/middle name, then qualification effective date.
- create paths for both position-centric and employee-centric qualification controllers use unassigned-position/employee selectors.
- qualification create/edit set standard audit fields and persist direct effective-date updates.

## 392.3 `SeniorityMoveController` Hard-Coded Branches

- index filters by roster and loads timer status from `nextSeniorityMoveUpdates` dictionary.
- `NoAccess` action routes to create with fixed move type `NA` and default effective date of next day + 1 minute.
- `ExtraBoard` computes effective date using roster rule request hours and craft-specific rules:
  - engineer uses bump-effective-date logic with possible 7-day roll
  - yardman/yardmaster midnight values adjusted by +1 minute.

## 392.4 Seniority Move Create/Assign/Delete Behavior

- create sets `AutoProcess = !IsMoveFromHangout`.
- if move is immediate (effective datetime in past), flow can redirect directly to assign action.
- immediate move-from-hangout scenario for same user can send Teams system message via `TeamsSendChatMessage`.
- assign flow calls `move.Assign(...)` inside transaction and refreshes seniority-move timer.
- delete flow enforces self-access checks for railroad-employee/union-representative users.

Deletion side effect:

- when deleting move with occupied target position and notify enabled, cancellation notification is emitted to bumped employee.

# Part 393: Strict Full Sweep Tracker (Increment 55 - Employee/Employment/EmployeeDetail Controllers)

## 393.1 Files Reviewed

- `StrategicApplications/Controllers/EmployeeController.cs`
- `StrategicApplications/Controllers/EmploymentStatusController.cs`
- `StrategicApplications/Controllers/EmployeeDetailController.cs`

## 393.2 `EmployeeController` Hard-Coded Behavior

- create uses employee number as initial identity password when creating new user.
- create/edit status paths send create/delete employee external messages based on employment code (`AT` active check).
- edit removes all current client roles then adds newly selected client role.
- status-change writes employment-status history and emits employee create/delete message side effect.
- delete removes both identity user and employee inside transaction scope.

## 393.3 `EmploymentStatusController` Behavior

- status code values are forced uppercase on create/edit.
- status catalogs are client-scoped and searchable by prefix on status name.
- employment-code selector is sourced from fixed employment-code list provider.

## 393.4 `EmployeeDetailController` Behavior

- `Details` redirects to notification history when any active pool employee has open notifications and craft shows notifications.
- `EmployeeCalendar` writes `RailroadEmployeeCalendarRequest` and redirects to fixed SSRS report URL with request control number.
- many actions enforce employee self-access for railroad-employee/union-representative roles and redirect to login on mismatch.

### History and profile-specific behavior

- on-duty/payroll history actions compute and roll minute totals into hour buckets.
- mark-up action sends Teams system message on successful mark up.
- daily call-sheet view sets cross-pool navigation hints (yard/yardmaster/clerical relationships) via pool-number branches.
- notification details map `admin` to `Administrator` in attempt history display.

# Part 394: Strict Full Sweep Tracker (Increment 56 - Contact/Profile Metadata Controller Cluster)

## 394.1 Files Reviewed

- `StrategicApplications/Controllers/AddressController.cs`
- `StrategicApplications/Controllers/PhoneNumberController.cs`
- `StrategicApplications/Controllers/EmailAddressController.cs`
- `StrategicApplications/Controllers/DescriptionController.cs`

## 394.2 Shared Access Control Pattern

- employee/union-representative users are restricted to self-record operations; non-self attempts redirect to login.
- many actions bypass this strict self-check for users whose last name contains `Administrator`.

## 394.3 `AddressController` Rules

- address text normalization on create/edit:
  - address/city title-cased via en-US text info
  - state forced uppercase
- description selector is fixed to description-code domain `AD`.

## 394.4 `PhoneNumberController` Rules

- phone numbers are normalized to numeric-only digits (`RemoveNonNumericCharacters`).
- create initializes calling order from provided count + 1.
- description selector is fixed to domain `PH`.
- create/edit/delete invoke `employee.PhoneNumberChange(phone, action)` side effect.

## 394.5 `EmailAddressController` Rules

- description selector is fixed to domain `EM`.
- create/edit/delete invoke `employee.EmailAddressChange(email, action)` side effect.

## 394.6 `DescriptionController` Rules

- description codes are forced uppercase on create.
- select flow routes through explicit description-code picker into create form prefill.
- supports `EmergencyType` boolean/flag metadata and yes/no selector population.

# Part 395: Strict Full Sweep Tracker (Increment 57 - Top-Level Admin/Client/Railroad Controllers)

## 395.1 Files Reviewed

- `StrategicApplications/Controllers/ClientController.cs`
- `StrategicApplications/Controllers/RailroadController.cs`
- `StrategicApplications/Controllers/AdminController.cs`

## 395.2 Client/Railroad Controller Hard-Coded Behavior

- both client and railroad create/edit paths call `MvcApplication.CreateTimers()` after successful save.
- railroad mark is forced uppercase on create.
- payroll list variants (`Client.Payroll`, `Railroad.Payroll`) reuse primary index filtering pattern and attach employee context IDs.
- `AutoAssignments` flag is directly exposed and persisted at both client and railroad levels.

## 395.3 `AdminController` Routing Matrix

- app-pool operations are role-gated:
  - restart -> system admin only
  - recycle -> system admin + crew dispatcher
- login redirects are role/record-count dependent:
  - system/client admins -> client index
  - railroad admin -> railroad index when single client exists
  - supervisor/hr/dispatcher/timekeeper -> railroad-pool index when single railroad exists
- unresolved multi-client/multi-railroad branches are explicitly left as comments (no implementation).

## 395.4 Payroll Login Redirect Logic

- `PayrollLoginRedirect` routes:
  - system/client/timekeeper -> client payroll index
  - railroad admin/supervisor -> railroad payroll index if exactly one client exists
- fallback for unmatched paths redirects to login.

# Part 396: Strict Full Sweep Tracker (Increment 58 - Account Controller Auth/Identity Pass)

## 396.1 File Reviewed

- `StrategicApplications/Controllers/AccountController.cs`

## 396.2 Login/Auth Hard-Coded Behavior

- debug login defaults are hard-coded under `#if DEBUG` (`UserName=1074`, fixed password literal).
- login success routing:
  - `admin` username -> `Home/Home`
  - all others -> `EmployeeDetail/Details`
- password-reset enforcement uses `CheckPasswordReset` (password hash equals username hash) and redirects to change-password flow.

## 396.3 Password/Role Management Contracts

- reset password sets password hash to username (default reset baseline).
- force password path disallows new password equal to username.
- create user path defaults theme to `bootstrap-spacelab.css` and `LastLogin = DateTime.Today`.
- role-assignment path (`Roles` action) clears all roles then reapplies selected roles.

## 396.4 Sign-In and Login History Side Effects

`SignInAsync` side effects:

- issues application cookie identity
- updates last login metadata
- registers active user in `MvcApplication.ActiveUsers`

`SetLastLoginDateTime` writes:

- login timestamp
- IP address
- on-property flag from IP check
- creates `UserLoginRecord` snapshot entry.

## 396.5 Auth Session Reset Behavior

- login GET and logoff both explicitly sign out OWIN cookie + forms auth and reset `HttpContext.User` principal.

# Part 397: Strict Full Sweep Tracker (Increment 59 - Seniority Roster Rule Controllers)

## 397.1 Files Reviewed

- `StrategicApplications/Controllers/RailroadPoolEmployeeSeniorityMoveController.cs`
- `StrategicApplications/Controllers/RosterSeniorityMoveRuleController.cs`
- `StrategicApplications/Controllers/RosterBulletinRuleController.cs`

## 397.2 `RailroadPoolEmployeeSeniorityMoveController` Behavior

- self-access guard is enforced for railroad-employee/union-representative users.
- `Index` shows only unassigned seniority moves (`SeniorityMoveAssignment == null`).
- `Seniority` action builds ordered seniority list via collection-query helper.
- roster selection flow auto-redirects when exactly one roster option exists.

## 397.3 `RosterSeniorityMoveRuleController` Behavior

- manages per-roster seniority move rule fields:
  - `RequiredDays`
  - `RequestHours`
  - `CancelHours`
- standard create/edit/delete CRUD with role gate and audit metadata updates.

## 397.4 `RosterBulletinRuleController` Behavior

- manages per-roster bulletin timing/rule fields:
  - start/close/cutoff times
  - effective day/time
  - bulletin hours
  - forced-assign hours
- create/edit load fixed effective-day selector list.

# Part 398: Strict Full Sweep Tracker (Increment 60 - Roster/Board Controller Cluster)

## 398.1 Files Reviewed

- `StrategicApplications/Controllers/RosterController.cs`
- `StrategicApplications/Controllers/RosterBoardController.cs`
- `StrategicApplications/Controllers/RosterBoardPositionController.cs`

## 398.2 `RosterController` Hard-Coded Behavior

- roster index is craft-scoped and ordered by roster number.
- create/edit expose training, extra-board, overtime-board, and payroll-department linkage fields.
- roster create/edit populate yes/no selectors and railroad payroll department options from railroad context.

## 398.3 `RosterBoardController` Hard-Coded Behavior

- board configuration includes explicit booleans/flags:
  - `Available`
  - `ExtraBoard`
  - `ForceAssign`
  - `AutoAssign`
  - `BulletinPositions`
  - `ApplySeniorityMoveRule`
  - `ExtendedAbsence`
- extra-board selector uses dedicated `ExtraBoardValues()` list.

## 398.4 `RosterBoardPositionController` Hard-Coded Behavior

- create seeds associated railroad position using position type `"B"` and uppercases board position name.
- board position display default count increments to derive initial `PositionNumber` suggestion.
- delete operation persists soft-delete metadata via `DeletedRailroadPosition` record instead of direct hard delete.

# Part 400: Strict Full Sweep Tracker (Increment 62 - HoldDown/Position/Requirement Controllers)

## 400.1 Files Reviewed

- `StrategicApplications/Controllers/HoldDownController.cs`
- `StrategicApplications/Controllers/PositionController.cs`
- `StrategicApplications/Controllers/PositionRequirementEmployeeController.cs`

## 400.2 `HoldDownController` Hard-Coded Behavior

- create flow releases any existing open hold-downs for selected employee before creating new hold-down.
- yardmaster special handling (`PoolNumber == 20`) creates yardmaster markoff/markup records on hold-down assign/release.
- release default date:
  - clerical pool (30) -> now
  - all others -> tomorrow `12:01 AM`.
- create/release emit Teams system messages and information log events.

## 400.3 `PositionController` Hard-Coded Behavior

- position create/edit includes policy toggles:
  - bulletin position
  - certification pay
  - turnover pay
  - auto-assign vacation
  - must-fill mode
  - alternate supervisor
- alternate supervisor CRUD is embedded in position create/edit logic (add/update/remove `PositionAlternateSupervisor`).

## 400.4 `PositionRequirementEmployeeController` Behavior

- index filters out removed employees and `XE` employment-code status.
- index groups records by employee and keeps latest row per employee before ordering.
- create/renew both use `PositionRequirementEmployee.Create(... completedDateTime ...)` pattern for requirement completion snapshot.

# Part 401: Strict Full Sweep Tracker (Increment 63 - Requirement Controller Family)

## 401.1 Files Reviewed

- `StrategicApplications/Controllers/RequirementController.cs`
- `StrategicApplications/Controllers/RailroadRequirementEmployeeController.cs`
- `StrategicApplications/Controllers/CraftRequirementEmployeeController.cs`

## 401.2 `RequirementController` Hard-Coded Behavior

- provides layered requirement views by scope/type:
  - client
  - railroad
  - railroad pool
  - craft
  - position
- each deeper scope aggregates inherited requirements from broader scopes plus local scope requirements.
- create default requirement numbering increments by `+10` from current max (or starts at `10`).
- calendar-year requirement branch hard-sets:
  - `RequirementTerm = 1`
  - `RenewDelayDays = 365`.

## 401.3 Requirement Linking Behavior

- `Select` and `Create` map requirements into scope-specific join tables (`ClientRequirement`, `RailroadRequirement`, `RailroadPoolRequirement`, `CraftRequirement`, `PositionRequirement`) based on type string.

## 401.4 Employee Requirement Controllers

`RailroadRequirementEmployeeController` and `CraftRequirementEmployeeController` share pattern:

- exclude inactive/removed employee states (`XE`, removed pool employee conditions).
- group by employee and keep latest requirement row before sorting.
- create/renew call `.Create(db, employeeId, completedDate, user)` on requirement-employee entity wrappers.
- delete removes selected requirement-record row directly.

# Part 402: Strict Full Sweep Tracker (Increment 64 - Client/Pool Requirement Employee Controllers)

## 402.1 Files Reviewed

- `StrategicApplications/Controllers/ClientRequirementEmployeeController.cs`
- `StrategicApplications/Controllers/RailroadPoolRequirementEmployeeController.cs`

## 402.2 Shared Requirement-Employee Pattern

- index filters exclude employment code `XE` and deduplicate by employee via latest record selection (`GroupBy(...).Select(Last)`).
- create/renew actions delegate to entity `Create(...)` helper with completion date and user audit context.
- delete removes selected requirement-record entry directly.

## 402.3 `ClientRequirementEmployeeController` Specifics

- client-scope requirement employees are ordered by:
  - renew date
  - completion date
  - employee name.
- search filter is last-name prefix.

## 402.4 `RailroadPoolRequirementEmployeeController` Specifics

- extra filters include:
  - employee has non-inactive seniority
  - removed pool employee records excluded.
- optional craft filter restricts employees to those with seniority in selected craft.
- search supports numeric employee-number prefix and textual last-name prefix branches.

# Part 404: Strict Full Sweep Tracker (Increment 66 - Payroll Processing Controller Pass)

## 404.1 File Reviewed

- `StrategicApplications/Controllers/ProcessPayrollController.cs`

## 404.2 Static Process State Behavior

Controller uses static mutable process state:

- `Records` list
- `RPEmployees` list
- `Status` string

This indicates cross-request/shared processing state semantics during long-running payroll operations.

## 404.3 Monthly Pay Record Generation Rules

- safety incentive branch uses payroll code `49` and inserts amount from selected incentive value.
- glove incentive branch applies only to `Yardman` craft using payroll code `63` and fixed amount `3`.
- accounting fallback literals for missing payroll department remain `Not Found` for ICC/department/GL fields.

## 404.4 Payroll Period Process Rules

- payroll period number combines selected period fragment with current/tuned year suffix.
- `1216` case adjusts year context with prior-month logic.
- final vs trial modes diverge:
  - final mode requires un-finalized processed earnings
  - trial mode can remove prior non-final process artifacts for same period.

Validation logic includes:

- missing earnings records
- zero-time/zero-amount earnings
- unapproved earnings
- unreviewed payroll review-required records.

## 404.5 File/Report Output Paths and Side Effects

hard-coded export/log/history paths under UNC shares:

- `\\Finance-svr\\Payroll Exports\\UKG\\...`
- history/log/report subfolders per pay period.

process writes:

- UKG payroll files
- excluded-record CSV
- batch-summary PDF converted from text report.

# Part 405: Strict Full Sweep Tracker (Increment 67 - Reporting Controller Pass)

## 405.1 Files Reviewed

- `StrategicApplications/Controllers/PayrollReportController.cs`
- `StrategicApplications/Controllers/DailyReportController.cs`

## 405.2 `PayrollReportController` Behavior

- `ACAReport` executes stored procedure `ACAReport` with `@ReportYear = DateTime.Now.Year`.
- result-set is manually serialized to CSV via `DataTable` iteration and returned as:
  - filename: `AHCA Report.csv`
  - mime type: `text/csv`.

## 405.3 `DailyReportController` Behavior

- `Index` and `EmployeeIndex` share same view-model construction pattern (`DailyReportIndexView.CreateInstance`).
- `CovidReport` performs optional report-view audit insert into `SAClassLibraryContext` when report name + employee CN are provided.
- then redirects to fixed SSRS URL:
  - `http://sql-svr/ReportServer?...PTRA COVID Activity Report...`

# Part 406: Strict Full Sweep Tracker (Increment 68 - Payroll Interface Mapping Controllers)

## 406.1 Files Reviewed

- `StrategicApplications/Controllers/ADPInterfaceController.cs`
- `StrategicApplications/Controllers/UKGInterfaceController.cs`

## 406.2 Shared Behavior

- both controllers are restricted to `System Administrator` and `Railroad Timekeeper` roles.
- both are scoped by payroll code and expose index/create/delete for interface mappings.
- create paths set standard audit fields and persist direct mapping rows.

## 406.3 `ADPInterfaceController` Rules

- create uses selected ADP column metadata source (`GetADPInterfaceColumns`).
- interface row is built via `ADPInterface.CreateInstance(code, selectedColumn)`.

## 406.4 `UKGInterfaceController` Rules

- create uses UKG value-type selector (`GetUKGInterfaceColumns`).
- `UKGEarningCode` is normalized to uppercase before save.
- interface row stores `UKGEarningCode + ValueType` binding per payroll code.

# Part 407: Strict Full Sweep Tracker (Increment 69 - Railroad Catalog Entity Controllers)

## 407.1 Files Reviewed

- `StrategicApplications/Controllers/RailroadLocationController.cs`
- `StrategicApplications/Controllers/RailroadMaterialController.cs`
- `StrategicApplications/Controllers/RailroadZoneController.cs`

## 407.2 Shared CRUD Behavior

- each controller provides straightforward index/create/edit/delete for railroad-scoped catalog entities.
- create/edit set standard audit metadata from current user/time.

## 407.3 Numbering and Normalization Rules

- location/zone create use `nextnbr = 10` + last-number pattern for default number suggestion.
- railroad material create/edit normalizes `MaterialUnitIndicator` to uppercase.

## 407.4 Sorting/Index Rules

- railroad location index sourced from railroad-location collection helper.
- railroad zone index sourced from railroad-zone collection helper.
- railroad material index ordered by material category number, material type, and material code.

# Part 408: Strict Full Sweep Tracker (Increment 70 - Railroad Support Catalog Controllers)

## 408.1 Files Reviewed

- `StrategicApplications/Controllers/RailroadMaterialCategoryController.cs`
- `StrategicApplications/Controllers/RailroadLocomotiveTypeController.cs`
- `StrategicApplications/Controllers/RailroadPayrollDepartmentController.cs`

## 408.2 `RailroadMaterialCategoryController` Behavior

- standard railroad-scoped category CRUD for material category number/name.
- index uses collection-list helper for category retrieval.

## 408.3 `RailroadLocomotiveTypeController` Behavior

- locomotive type values are normalized to uppercase on create/edit.
- edit default-toggle rule:
  - setting one type as default forces all other locomotive types to `Default = false`.

## 408.4 `RailroadPayrollDepartmentController` Behavior

- maintains payroll accounting mapping fields per railroad:
  - department name
  - ICC number
  - department number
  - general ledger number
- used by payroll-record generation logic for accounting field population.

# Part 409: Strict Full Sweep Tracker (Increment 71 - AFE/WorkCode Catalog Controllers)

## 409.1 Files Reviewed

- `StrategicApplications/Controllers/RailroadAFEController.cs`
- `StrategicApplications/Controllers/RailroadWorkCodeController.cs`

## 409.2 `RailroadAFEController` Behavior

- railroad AFE create/edit normalizes `AFENumber` to uppercase.
- maintains AFE number + description catalog used by assignment/billing flows.

## 409.3 `RailroadWorkCodeController` Behavior

- create suggests default `WorkCodeNumber` via `nextnbr = 10 + last.WorkCodeNumber` pattern within railroad scope.
- work-code records include explicit `BillableCode` boolean used by misc billing helper logic.
- create/edit surfaces yes/no selector for billable flag.

# Part 412: Strict Full Sweep Tracker (Increment 74 - Engineer Job Code + Payroll Tier Controllers)

## 412.1 Files Reviewed

- `StrategicApplications/Controllers/EngineerJobCodeController.cs`
- `StrategicApplications/Controllers/RailroadPoolPayrollTierController.cs`

## 412.2 `EngineerJobCodeController` Behavior

- engineer job/pay class codes are normalized to uppercase (`PayClassCode`, `TraineePayClassCode`).
- mapping is weight-threshold based (`MaxWeightOnDrivers`) and is consumed by tie-up locomotive job-paid logic.
- delete path uses soft-delete style artifact (`EngineerJobCodeDelete`) with deleted timestamp, rather than immediate hard removal.

## 412.3 `RailroadPoolPayrollTierController` Behavior

- payroll tier records are railroad/pool scoped and ordered by pool name then number of days.
- optional pool filter narrows displayed tiers.
- create/edit fields:
  - number of days
  - day type
  - rate percentage.
- day-type selector is sourced from `PayrollTierDayTypes()`.

# Part 413: Strict Full Sweep Tracker (Increment 75 - Railroad Employee + Vacation Request Controllers)

## 413.1 Files Reviewed

- `StrategicApplications/Controllers/RailroadEmployeeController.cs`
- `StrategicApplications/Controllers/RailroadEmployeeVacationRequestController.cs`

## 413.2 `RailroadEmployeeController` Behavior

- mirrors employee identity flows at railroad scope:
  - create user with employee-number default password
  - assign railroad role(s)
  - create employment-status history
  - send create/delete employee message based on employment code.
- `Status` action triggers daily railroad employee status record generation for selected date.
- `Select` action attaches an existing employee to railroad and applies default railroad role.
- `Remove` action strips railroad roles and reassigns `Client Employee` role before removing railroad-employee link.

## 413.3 `RailroadEmployeeVacationRequestController` Behavior

- request index focuses on next-year vacation planning using split buckets (`SplitNbr` 1..5).
- one-day vacation weeks are tracked via `RailroadEmployeeVacationOneDayTimeRecord` (`hours = weeks * 40`).
- maximum one-day-week cap derives from craft `MaximumVacationDayTime / 40`.
- create/edit/delete maintain ordered choice numbers within each split bucket.

Additional hard-coded rules:

- vacation week baseline = `NextYearVacationDays / 5`.
- remaining weeks endpoint returns JSON with weeks-left + one-day-week count.

# Part 415: Strict Full Sweep Tracker (Increment 77 - BeSafe Controller Family Pass)

## 415.1 Files Reviewed

- `StrategicApplications/Controllers/BeSafeController.cs`
- `StrategicApplications/Controllers/BeSafeCategoryController.cs`
- `StrategicApplications/Controllers/BeSafeSubdivisionController.cs`
- `StrategicApplications/Controllers/BeSafeAreaController.cs`

## 415.2 `BeSafeController` Hard-Coded Behavior

- open/closed mode uses status string (`"Open"` or numeric day window).
- record-number generation uses year-prefixed sequence:
  - first record in year -> `yy0001`
  - subsequent -> increment prior record number.
- action/resolve operations create notification notes to originating railroad employee.
- report rendering endpoints return PDF directly from in-memory streams.

## 415.3 BeSafe Catalog Controllers

category/subdivision/area controllers are railroad-scoped catalogs with number defaults using `nextnbr = 10 + lastNumber` pattern.

Additional branch behavior:

- area depends on subdivision selector (`BeSafeSubdivisionControlNumber`).
- category depends on BeSafe email-group selector.

## 415.4 Context/Dependency Notes

- BeSafe controller family uses `SAClassLibraryContext` and models rather than `StrategicApplicationsContext` for key data paths.

# Part 416: Strict Full Sweep Tracker (Increment 78 - Slow Order Controller Family Pass)

## 416.1 Files Reviewed

- `StrategicApplications/Controllers/SlowOrderController.cs`
- `StrategicApplications/Controllers/SlowOrderAreaController.cs`

## 416.2 `SlowOrderController` Hard-Coded Behavior

- open/history mode uses `status` string (`"Open"` else day-window integer).
- optional area filter is applied after record retrieval.
- edit flow snapshots prior title/description into `SlowOrderChangeRecord` before updating active record.
- complete/delete actions create dedicated lifecycle records (`SlowOrderCompleteRecord`, `SlowOrderDeleteRecord`) instead of hard-removing main record.

## 416.3 PDF Output and Formatting Rules

- `View`, `ViewChange`, `ViewOrders` render PDFs directly to response stream.
- generated report title/footer strings include fixed text and static signature line (`Brian J. Mooney - Terminal Superintendent`).
- report numbering/line formatting uses fixed-width/title padding and area-based page breaks.

## 416.4 `SlowOrderAreaController` Behavior

- area catalog uses default number seed pattern (`10 + last area number`).
- area CRUD is railroad-scoped and backed by `SAClassLibraryContext` models.

# Part 421: Strict Full Sweep Tracker (Increment 83 - Payroll/Seniority/Training Controllers)

## 421.1 Files Reviewed

- `StrategicApplications/Controllers/PayrollController.cs`
- `StrategicApplications/Controllers/SeniorityController.cs`
- `StrategicApplications/Controllers/RailroadPoolEmployeeTrainingDateController.cs`

## 421.2 `PayrollController` Behavior

- payroll index supports pay-period/start-end/custom review filters and aggregates ST/OT totals by rolling minute-overflow into hour buckets.
- manual payroll create includes pool-specific job-code composition rules:
  - pools 10/20/40 use assignment + position-code suffix
  - pools 30/60 use position-code prefix
  - pool 50 uses assignment-only job string.
- manual create always inserts payroll review-required record with reason referencing manual entry actor.
- missing payroll department fields are filled with `"Not Found"` literals.

## 421.3 `SeniorityController` Behavior

- active-only filter path requires active state + null seniority end date.
- provides PDF reporting endpoints for full seniority and off-day contact lists using fixed-width text layouts.
- edit state-transition logic branches:
  - inactive/cutback -> active: inactivate other active seniority + assign hangout position
  - active -> inactive/cutback: unassign positions + remove unassigned moves/bids.
- active seniority updates trigger craft-message side effect.

## 421.4 `RailroadPoolEmployeeTrainingDateController` Behavior

- training date create defaults to next logical date based on last qualification/training records.
- create/edit/delete synchronize corresponding `DailyCrewPosition` records for training assignment date.
- operations use transaction scope and call `CreateDailyCrewPositionOnDutyRecord(...)` helper for synchronization.
- `ChangeDate` JSON endpoint returns daily-crew selector list for target pool/date.

# Part 417: Strict Full Sweep Tracker (Increment 79 - BeSafe Email Group + Mark-Off Code Controllers)

## 417.1 Files Reviewed

- `StrategicApplications/Controllers/BeSafeEmailGroupController.cs`
- `StrategicApplications/Controllers/MarkOffCodeController.cs`

## 417.2 `BeSafeEmailGroupController` Behavior

- railroad-scoped email-group catalog with default number pattern (`10 + last group number`).
- stores group number/name/address used by BeSafe category notification routing.
- data context is `SAClassLibraryContext`.

## 417.3 `MarkOffCodeController` Behavior

- non-system-admin users do not see `SystemUseOnly` mark-off codes in index.
- mark-off code create/edit normalizes `Code` to uppercase.
- create sets fixed default `ReportCode = "O"`.
- mark-off code tracks flags for approval, agreement, employee mark-off/mark-up permissions, holiday exemption/qualification, and report color.

### Mark-off auxiliary mappings

- payroll-code mappings (`MarkOffPayrollCodes`) support `BasicDay` flag.
- approval officer mappings (`MarkOffCodeApprovalOfficers`) are client-officer scoped.

# Part 418: Strict Full Sweep Tracker (Increment 80 - Core Routing and Officer/Location Controllers)

## 418.1 Files Reviewed

- `StrategicApplications/Controllers/CraftApprovalOfficerController.cs`
- `StrategicApplications/Controllers/CrewPositionAlternatePositionController.cs`
- `StrategicApplications/Controllers/LocationController.cs`
- `StrategicApplications/Controllers/HomeController.cs`

## 418.2 `CraftApprovalOfficerController` Behavior

- manages craft-specific approval officers with optional `Primary` flag.
- create officer selection uses unassigned-craft-officer selector by client/craft scope.

## 418.3 `CrewPositionAlternatePositionController` Behavior

- maps alternate position per crew position + weekday (composite key delete path).
- weekday selection is constrained by `AlternatePositionDays(...)` helper.

## 418.4 `LocationController` Behavior

- manages pool-scoped location definitions including board-order and on-duty-location flag.
- location edit recalculates board order for all assignments tied to edited location.

## 418.5 `HomeController` Behavior

- index forcibly signs out auth cookies/forms principal and redirects to `Home` view route.
- `DefaultView` routing branch:
  - railroad employee/union-only -> employee detail route
  - other roles -> admin default view with return metadata
- if password reset is required (`username==password` hash check), redirects to account change-password flow.

# Part 419: Strict Full Sweep Tracker (Increment 81 - Fill Vacancy Controller Deep Pass)

## 419.1 File Reviewed

- `StrategicApplications/Controllers/FillVacancyController.cs`

## 419.2 Vacancy Board Selection Matrix

`Select(board)` supports multiple sourcing modes:

- `0` same-assignment currently on-duty qualified employees
- `1` extra board
- `2` off-day board
- `4` overtime board (with report visibility)
- `5` vacation relief board
- `6` qualified employee board
- default seniority board.

Each branch sets a specific return-button label and list source strategy.

## 419.3 Accept/Assign Flow Rules

- late-call state is derived from current time vs vacancy end-call time (except board 0 path).
- `AcceptCall` flow:
  - fills vacancy (`FillVacancy`)
  - optional late-call record insert
  - applies overtime board order logic
  - removes vacancy rows
  - writes fill-vacancy log entry.

## 419.4 Arrival-Change/FRA Rules

- arrival-change confirmation can mark late-call record as confirmed.
- recalculates prior-rest hours/minutes by delta between old/new on-duty timestamps.
- updates FRA consecutive-days counter using rest threshold (`FRARequirements.ConsecutiveDayHours`).
- optional redirect into tie-up process when invoked from FRA pathway.

## 419.5 Contact Report Output

- overtime/extra-board contact reports generate fixed-format PDF output with monospaced text blocks and phone details.
- report lines include mark-off code, current position, and seniority date/year context.

# Part 420: Strict Full Sweep Tracker (Increment 82 - Payroll Approval Role Mapping Pass)

## 420.1 File Reviewed

- `StrategicApplications/Controllers/PayrollCodeApprovalRoleController.cs`

## 420.2 Controller Behavior

- maps payroll codes to approval roles through `PayrollCodeApprovalRoles` rows.
- create form excludes roles already mapped to target payroll code.
- create stores:
  - role id
  - role name snapshot
  - primary flag
  - audit metadata.

## 420.3 Role Resolution Rules

- create validates both payroll code and selected identity role exist before insert.
- delete removes role-mapping row directly by mapping control number.

# Part 422: Strict Full Sweep Tracker (Increment 84 - Payroll Report Group Controllers)

## 422.1 Files Reviewed

- `StrategicApplications/Controllers/PayrollReportGroupController.cs`
- `StrategicApplications/Controllers/PayrollReportGroupCategoryController.cs`

## 422.2 `PayrollReportGroupController` Behavior

- manages client-scoped payroll report groups (number + name).
- index supports name-prefix search and orders by report-group number.
- standard CRUD with audit metadata updates.

## 422.3 `PayrollReportGroupCategoryController` Behavior

- maps report groups to payroll categories via composite key records.
- index displays categories ordered by payroll category number.
- create uses payroll category selector for parent client scope.
- delete resolves mapping row by `(group, category)` composite key.

# Part 423: Strict Full Sweep Tracker (Increment 85 - Daily Assignment Controller Deep Pass)

## 423.1 File Reviewed

- `StrategicApplications/Controllers/DailyAssignmentController.cs`

## 423.2 Core Index/Create Behavior

- index computes extra-position display model by counting repeated positions per assignment and labeling duplicate slots as `"Extra ..."`.
- create flow supports dual `id` semantics:
  - daily-assignment-shift control number
  - railroad-pool-employee control number (manual/employee-initiated path).
- pool-specific create UI branching:
  - pool 40 mechanical path
  - pool 50 MofW path with AFE visibility/request-note differences
  - default path for remaining pools.

## 423.3 Assignment Creation Side Effects

- creates coordinated records in one transaction:
  - `DailyAssignment`
  - `DailyAssignmentRequest`
  - `DailyAssignmentCrew`
  - optional `DailyAssignmentAFERecord`.
- invokes crew-position generation (`assignmentcrew.CreateDailyCrewPositions(...)`).
- updates vacancies and can complete shift-state post-creation (`CompleteDailyAssignmentShift`).
- triggers at-hoc timer update (`SetAtHocMessageTimer`).

## 423.4 Vacancy/On-Duty Helpers

- `CreateCrewPositions` path sends MSMQ `DailyCrewPosition` create messages.
- `CreateOnDutyRecord` routes through extra-assignment creation pipeline using selected on-duty date/time.

## 423.5 Move-To-Foreman Behavior

- `MoveToForeman` swaps on-duty records between helper and available foreman positions (excluding position type `D`).
- gated by `CanMoveToForeman` (or system-admin override).

# Part 425: Strict Full Sweep Tracker (Increment 87 - Daily Assignment Shift Controller Pass)

## 425.1 File Reviewed

- `StrategicApplications/Controllers/DailyAssignmentShiftController.cs`

## 425.2 Call-Sheet Index/Board Visibility Behavior

- call-sheet index flags whether pool has extra-board and overtime-board rosters.
- open/closed work-period filtering is supported via status selector.
- next-update timestamps surface from app-level timer dictionaries:
  - call sheet updates
  - extra board updates.

## 425.3 Create/Refresh Mechanics

- call-sheet create sends MSMQ message (`DailyAssignmentShift`,`Create`) rather than direct inline creation logic.
- refresh operation can recreate all daily assignments for a shift by removing existing rows and re-running `CreateDailyAssignments(...)`.
- post-refresh vacancy recalculation is explicitly invoked.

## 425.4 Extra Board / Overtime Board Flows

- view selectors auto-redirect when only one board exists.
- board-position move actions (`MoveOTBoardPosition`, `MoveXBBoardPosition`) use after/before direction and then normalize board order in `+10` increments starting at `1000`.

## 425.5 Shift Completion Behavior

- completion path delegates to `CompleteDailyAssignmentShift(...)` with selected completion datetime.

# Part 426: Strict Full Sweep Tracker (Increment 88 - Daily Crew Position Controller Deep Pass)

## 426.1 File Reviewed

- `StrategicApplications/Controllers/DailyCrewPositionController.cs`

## 426.2 Position Lifecycle Actions

- create extra position uses default railroad-position control number sentinel `99999999999999999` and marks `ExtraBoardOnly = true`.
- tie-up iterates all on-duty records, creates off-duty records where needed, and can generate manual tie-up notifications for payroll-processing employees.
- delete/remove/release/annul/do-not-fill/skip/unskip all trigger vacancy recalculation workflows after state changes.

## 426.3 Release/Penalty Claim Behavior

- release supports optional penalty-claim payroll creation:
  - creates payroll record with 3-hour earning code `44`
  - creates payroll review-required reason text
  - creates earning approval-required record.
- when release/remove involves extra-board assignment linkage, tie-up/order is reset through extra-board position helper.

## 426.4 Annul / Do-Not-Fill Behavior

- both actions run under transaction scope and:
  - mark position state (annul / do-not-fill)
  - create off-duty records
  - wait for vacancy processing flag clearance before recalculation
  - complete daily assignment shift using selected action datetime.

## 426.5 Employee Change/Tie-Up Notes

- employee change delegates to `position.ChangeEmployee(...)` helper.
- tie-up logs include username resolution fallback (`unknown`) when user lookup fails.

# Part 427: Strict Full Sweep Tracker (Increment 89 - Daily On-Duty Billing/Material Controller Cluster)

## 427.1 Files Reviewed

- `StrategicApplications/Controllers/DailyOnDutyAFEBillingController.cs`
- `StrategicApplications/Controllers/DailyOnDutyZoneBillingController.cs`
- `StrategicApplications/Controllers/DailyOnDutyMiscellaneousBillingController.cs`
- `StrategicApplications/Controllers/DailyOnDutyRailroadMaterialRecordController.cs`

## 427.2 Shared Behavior Pattern

- all four controllers are on-duty-record scoped child-entry CRUD flows.
- create/edit actions snapshot descriptive fields from selected master record (AFE, zone, material) into on-duty child rows.

## 427.3 AFE / Zone Billing Rules

- AFE entries persist `AFENumber/AFEDescription + STBHours/OTBHours` snapshots.
- zone entries persist `ZoneNumber/ZoneName + STBHours/OTBHours` snapshots.

## 427.4 Misc Billing Rules

- misc entries persist work-code + location + billable flag + ST/OT billing hours + notes.
- JSON helper `SetBillableFlag` returns billable default string based on selected railroad work code’s `BillableCode`.

## 427.5 Railroad Material Rules

- material entries persist category/type/code/description/unit indicator snapshots plus quantity.
- material selectors are filtered by railroad/pool context.

# Part 428: Strict Full Sweep Tracker (Increment 90 - Daily On-Duty Locomotive Controller Pass)

## 428.1 File Reviewed

- `StrategicApplications/Controllers/DailyOnDutyLocomotiveRecordController.cs`

## 428.2 Controller Behavior

- manages on-duty locomotive records per daily crew position on-duty record.
- create defaults locomotive type to:
  - railroad default locomotive type when available
  - otherwise last locomotive type used on same on-duty record.

## 428.3 Data Normalization/Helpers

- `GetLocomotiveWeight` JSON endpoint returns locomotive type + weight for selected type control number.
- locomotive IDs are normalized to uppercase on create/edit.
- create converts formatted weight text to integer (comma removal).

# Part 429: Strict Full Sweep Tracker (Increment 91 - Daily On-Duty Tie-Up Controller Deep Pass)

## 429.1 File Reviewed

- `StrategicApplications/Controllers/DailyOnDutyRecordTieUpController.cs`

## 429.2 Tie-Up Process Routing Rules

- `TieUpProcess` branches by pool number:
  - pool 10 (yard/engine) can route to arrival correction, locomotive step, or payroll
  - pools 20/30 route through payroll when trainee conditions apply
  - pool 40 routes through payroll for RIP-track location patterns
  - pool 50 routes through MofW billing.

## 429.3 Payroll/Billing Rule Artifacts

- static clerical pay-grade dictionary is seeded with fixed code->grade map.
- locomotive step computes total locomotive weight and resolves job-paid code via engineer job-code weight thresholds (with trainee pay-code branch).
- MofW billing captures first-meal period behavior and approval officer defaults.

## 429.4 Payroll Information Persistence

- `CreatePayrollInformation(...)` writes/updates `DailyOnDutyPayrollInformation` snapshot fields including:
  - job paid code
  - meal period flags/start times/claims/approvals
  - air claim/approval
  - training claim + trainee on-duty reference.

## 429.5 Tie-Up Create Behavior

- initializes on/off duty locations from assignment location by default.
- emits FRA warning/certification messaging based on hours-of-service/restriction state.
- supports early-release reason handling with default fallback `NE` when not provided.

# Part 430: Strict Full Sweep Tracker (Increment 92 - Locomotive Inspection Controller Pass)

## 430.1 Files Reviewed

- `StrategicApplications/Controllers/LocomotiveInspectionRecordController.cs`

## 430.2 Controller Behavior

- inspection records are tied to daily on-duty locomotive records.
- create action redirects to edit when inspection already exists for target locomotive-on-duty record.
- inspection capture includes:
  - locomotive ID
  - location
  - inspected datetime
  - fuel reading
  - repairs-needed flag
  - inspected-by user text.

## 430.3 Data Rules

- locomotive ID is normalized to uppercase on create.
- location selector is sourced from all locations for employee’s active craft pool/railroad context.

# Part 431: Strict Full Sweep Tracker (Increment 93 - Assignment Controller Pass)

## 431.1 File Reviewed

- `StrategicApplications/Controllers/AssignmentController.cs`

## 431.2 Assignment Index/Details Behavior

- index excludes abolished assignments (`AssignmentAbolishment == null`) and supports assignment-type + name-prefix filtering.
- detail view excludes extra-board-only assignment types and future-effective assignment rows.
- detail supports optional shift filtering for both assignments and relief crews.

## 431.3 Assignment Select/Create Branching

- assignment selector uses mechanical-specific assignment query for pool 40; default query for other pools.
- create/edit populate on-duty time and location selectors scoped to railroad pool.
- board order is computed via `SetBoardOrder(...)` using assignment type/location/on-duty context.

## 431.4 Board-Order Recalculation Workflow

- `SetBoardOrder` action recomputes board order for:
  - assignments
  - assignment on-duty days
  - future/open daily assignments.
- also backfills `WorkArea` from location name when missing.

# Part 432: Strict Full Sweep Tracker (Increment 94 - Assignment On-Duty Day/Time Controllers)

## 432.1 Files Reviewed

- `StrategicApplications/Controllers/AssignmentOnDutyDayController.cs`
- `StrategicApplications/Controllers/AssignmentOnDutyTimeController.cs`

## 432.2 `AssignmentOnDutyDayController` Behavior

- stores weekday-specific assignment scheduling metadata:
  - weekday
  - on-duty time
  - work-day order
  - straight-time hours.
- computes board order per day using assignment type/location/on-duty-time context.
- supports crew assignment/unassignment against specific assignment-on-duty-day entries.

## 432.3 `AssignmentOnDutyTimeController` Behavior

- manages pool-scoped on-duty time definitions linked to shifts.
- stores on-duty time and calling-time window (`CallingTimeStart`, `CallingTimeEnd`).
- index ordering is by shift ID then on-duty time.

# Part 433: Strict Full Sweep Tracker (Increment 95 - Engineer Pay Rate Controller Pass)

## 433.1 File Reviewed

- `StrategicApplications/Controllers/EngineerPayRateController.cs`

## 433.2 Controller Behavior

- manages pay-rate history rows scoped to engineer job code.
- index ordering is by effective date descending.
- create/edit persist separate engineer/trainee straight-time and overtime rates:
  - `ESTHourRate`
  - `EOTHourRate`
  - `TSTHourRate`
  - `TOTHourRate`
- effective-date field controls historical applicability.

## 433.3 Helper Endpoint

- `GetOTRate(double rate)` returns JSON overtime rate using `rate * 1.5` rounded to 4 decimals.

# Part 434: Strict Full Sweep Tracker (Increment 96 - On-Duty Move Cutoff Controller Pass)

## 434.1 File Reviewed

- `StrategicApplications/Controllers/OnDutyMoveCutOffTimeController.cs`

## 434.2 Controller Behavior

- manages on-duty-time-specific move cutoff times by craft.
- index is assignment-on-duty-time scoped and ordered by craft name.
- create path restricts craft selector to unassigned move-cutoff crafts for selected pool/on-duty-time.
- create stores craft + cutoff time with standard audit metadata.
