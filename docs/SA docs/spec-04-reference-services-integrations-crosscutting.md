# Spec_04: Reference, Services, Integrations, Configuration, and Cross-Cutting Gap Analysis
# Part 10: Windows Services

## Overview

The solution includes 4 Windows Service projects (plus the console app). All use `System.Timers.Timer` and run as `"autoprocess"` user.

| Project | Sub-Services | Purpose |
|---|---|---|
| SADailyCallSheetService | 6 services | Daily call sheet creation, shift management, on-duty processing |
| SAImportPayrollService | 2 services | ADP and UKG payroll file import |
| SAAtHocMessageService | 2 services | Electronic crew calling and on-duty notifications (covered in Part 9) |
| RestartApplicationPool | Console app | IIS app pool restart utility |

## SADailyCallSheetService

### Program.cs � 6 Sub-Services

```csharp
new SADailyCallSheetService(),
new SADailyAssignmentShiftService(),
new SADailyAssignmentService(),
new SADailyCrewPositionService(),
new SADailyOnDutyRecordService(),
new SADailyOnDutyMarkOffRecordService()
```

### Service View Models

The service uses lightweight view model wrappers in `SADailyCallSheetService\Models\`:

| Model | Wraps |
|---|---|
| `SV_Shift` | `Shift` � adds `NextShiftID`, `PreviousShiftID` |
| `SV_Crew` | `Crew` |
| `SV_DailyAssignmentShift` | `DailyAssignmentShift` |
| `SV_DailyCrewPosition` | `DailyCrewPosition` |
| `SV_DailyCrewPositionOnDutyRecord` | `DailyCrewPositionOnDutyRecord` |
| `SV_MarkOffRecord` | `MarkOffRecord` |
| `SV_RailroadEmployee` | `RailroadEmployee` |
| `SV_RailroadPoolEmployee` | `RailroadPoolEmployee` |
| `SV_RailroadPosition` | `RailroadPosition` |

These use `SAClassLibraryContext` (separate from the web app's context) to access the same database.

### SADailyCallSheetService (Main)

**Purpose**: Automatically creates daily call sheets (shift assignments) for each pool.

**Timer Architecture**:
- Initial 60-second delay timer on start
- After delay: creates per-pool timers stored in `DailyCallSheetTimers` dictionary
- Each pool's timer fires at the next calculated call sheet time

**`CreateAndSetTimers()`**:
1. Iterates all clients ? railroads ? pools
2. For each pool where `AutoCallSheets == true`:
   - Creates a timer with `CreateDailyCallSheet` handler
   - Stores in `DailyCallSheetTimers[pool.ControlNumber]`
   - Calls `SetDailyCallSheetTimer(pool)` to calculate next fire time
3. For disabled pools: sets next update to `9999-12-31`, removes timer

**`CreateDailyCallSheet(sender, e)`** � Main workflow:

1. **Identify pool** from timer dictionary: `DailyCallSheetTimers.FirstOrDefault(t => t.Value == timer).Key`
2. **Find last shift**: Most recent uncompleted `DailyAssignmentShift` for this pool, ordered by date/shift descending
3. **Update mark-offs** (not Pool 50/MoW): For each on-duty record on the last shift, call `UpdateDailyOnDutyMarkOffRecords()`
4. **Calculate next shift**: `GetNextDailyAssignmentShift(lastShift, pool, lastDate)`:
   - Uses circular shift sequencing: 1?2?3?1
   - When wrapping from shift 3 to shift 1, date increments by 1 day
   - Encodes shift ID in the seconds component of the returned DateTime
5. **Find assignments**: `GetAllDailyAssignmentsByShift(db, pool, date, shift)`:
   - Assignments where `EffectiveDate < date` AND `AbolishmentDate > date`
   - Has on-duty days matching the day of week
   - Matches the shift
   - Not `ExtraBoardOnly` type
   - EXCEPT assignments that already have a `DailyAssignment` for this date
6. **Skip if exists**: If `DailyAssignmentShift` already exists for pool/date/shift ? log and return
7. **Create**: `SendCreateDailyCallSheetMessage()` ? triggers creation

**`SetDailyCallSheetTimer(pool)`**:
- Calculates `GetNextDailyCallSheet(pool)` ? next DateTime
- Sets `timer.Interval` to milliseconds until that time
- Enables the timer

### SADailyAssignmentShiftService

Creates `DailyAssignmentShift` records � the container for a shift's worth of assignments.

### SADailyAssignmentService

Creates `DailyAssignment` records within a shift, linking assignments to the shift container.

### SADailyCrewPositionService

Creates `DailyCrewPosition` records for each crew position on each assignment within a shift.

### SADailyOnDutyRecordService

Processes on-duty records � places assigned employees on duty for their positions.

### SADailyOnDutyMarkOffRecordService

Links mark-off records to on-duty records when employees are marked off during a shift.

---

## SAImportPayrollService

### Program.cs � 2 Sub-Services

```csharp
new SAImportADPPayrollService(),
new SAImportUKGPayrollService()
```

### SAImportADPPayrollService

**Purpose**: Monitors a file share for ADP payroll export files and imports paid amounts.

**Configuration**:
- Watch path: `\\finance-svr\c$\Payroll Exports\ADP\Imports`
- Error path: `{path}\Processing Error`
- History path: `{path}\History`
- File pattern: `PRPT1*.*`

**Architecture**:
- 60-second startup delay
- Creates `FileSystemWatcher` on the import directory
- On file created (or startup): processes all files in directory

**`TriggerFileWatcherEvent()`**:
1. `Thread.Sleep(5000)` � wait for file copy to complete
2. While files exist in directory:
   - Get first file
   - `CreateEarningAmountPaidRecords(file)` � parse and import
   - Move to history path on success
   - Move to error path on failure

**`CreateEarningAmountPaidRecords(adpfile)`**:
- Reads CSV file line by line
- Skips header row (contains "Employee Number")
- For each line: parses fields, matches to existing payroll records, creates `ADPInterface` records with paid amounts
- Tracks record counts for logging

### SAImportUKGPayrollService

**Purpose**: Identical architecture to ADP service but for UKG (Ultimate Kronos Group) payroll system.

**Configuration**:
- Watch path: `\\finance-svr\c$\Payroll Exports\UKG\Imports`
- Same error/history path structure
- Same `PRPT1*.*` file pattern

**`CreateEarningAmountPaidRecords(ukgfile)`**:
- Same CSV parsing pattern as ADP
- Creates `UKGInterface` records instead of `ADPInterface`
- Different field mapping for UKG format

---

## RestartApplicationPool

Console application that restarts the IIS application pool. Used as a scheduled task or manual recovery tool.

---

## Common Patterns Across All Services

1. **60-second startup delay**: All services wait 1 minute after Windows service start before beginning work
2. **`"autoprocess"` user**: All automated operations use this string for `CreatedBy`/`ModifiedBy` fields
3. **SAClassLibraryContext**: Services use the class library's DbContext, not the web app's `StrategicApplicationsContext`
4. **EventLogger**: All services log to Windows Event Log via `EventLogger.WriteInformationLogEvent()` and `WriteErrorLogEvent()`
5. **Teams integration**: Call sheet and AtHoc services send Teams messages via `ApplicationUtilities.TeamsSendChatMessage()`
6. **FileSystemWatcher**: Payroll import services use file watchers with 5-second sleep for copy completion
7. **Timer-per-pool**: Call sheet service maintains individual timers per railroad pool
# Part 11: Utility Classes

## Overview

| File | Namespace | Purpose |
|---|---|---|
| `ApplicationUtilities.cs` | `StrategicApplications.Utilities` | Core app utilities: control numbers, Teams, transactions, vacancy, IP checks |
| `DateTimeUtilities.cs` | `StrategicApplications.Utilities` | Date math, year calculations, week start |
| `StringUtilities.cs` | `StrategicApplications.Utilities` | Numeric checks, formatting |
| `EventLogger.cs` | `StrategicApplications.Utilities` | Windows Event Log wrapper |
| `PayrollUtilities.cs` | `StrategicApplications.Utilities` | Approval routing, PDF generation, payroll import |
| `FileUtilities.cs` | `StrategicApplications.Utilities` | File/directory operations |
| `ClassLibraryUtilities.cs` | `SAClassLibrary.Utilities` | Class library equivalents |
| `ServiceUtilities.cs` | `SADailyCallSheetService.Utilities` | Call sheet service helpers |
| `TransactionScopeBuilder.cs` | `SAClassLibrary.Utilities` | Class library transaction scope |

## ApplicationUtilities

### Static Fields

| Field | Type | Value/Purpose |
|---|---|---|
| `PoolInProgress` | `Dictionary<long, bool>` | Guards against concurrent vacancy processing per pool |
| `culture` | `CultureInfo` | `"en-US"` |
| `inboundprcsd` | `string` | `MvcApplication.inbound + @"\Processed"` |
| `inbounderror` | `string` | `MvcApplication.inbound + @"\Processing Error"` |
| `user` | `string` | `"autoprocess"` |

### TransactionScopeBuilder (Nested Class)

**`CreateReadCommitted()`**:
```
IsolationLevel = ReadCommitted
Timeout = 30 minutes
TransactionScopeAsyncFlowOption.Enabled
```

**`CreateShapshot()`** (note: typo in source):
```
IsolationLevel = Snapshot
Timeout = 30 minutes
TransactionScopeAsyncFlowOption.Enabled
```

### `CreateNewControlNumber()` ? long

```csharp
Thread.Sleep(1);
return Convert.ToInt64(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", culture), culture);
```

Used by `ControlNumberBase` constructor for all entity primary keys.

### `TeamsSendChatMessage(messagetext, type)` ? Task\<HttpResponseMessage\>

Routes messages to Microsoft Teams channels via webhook URLs from AppSettings.

**Channel routing** (production � Demo database goes to TestMessage):

| Type | AppSettings Key | Purpose |
|---|---|---|
| `"SystemMessage"` | `SystemMessage` | System notifications (mark-offs, FRA, errors) |
| `"SystemSupport"` | `SystemSupport` | Service status, timer updates |
| `"TieUpMessage"` | `TieUpMessage` | Engineer/Yardman tie-up notifications |
| `"ECallMessage"` | `ECallMessage` | Electronic crew calling messages |

**`SendTeamsMessage(message, uri)`**: Creates `Message { Text }` object, serializes with `JsonConvert`, POSTs to webhook URI.

### `RestartApplicationPool(poolname, user)` ? Task\<HttpResponseMessage\>

Starts external process: `ConfigurationManager.AppSettings["RestartAppPoolLocation"]` with pool name as argument. Sends Teams notification.

### `RecycleApplicationPool(poolname, user)` ? Task\<HttpResponseMessage\>

Uses `Microsoft.Web.Administration.ServerManager` to recycle IIS app pool directly. Sends Teams notification.

### `CheckOnPropertyIPAddress(inbndIP)` ? bool

Compares incoming IP against `AppSettings["AuthorizedIPSubnets"]` to determine if user is on-property.

### `GetUserName(user)` ? string

Resolves username to `Employee.EmpNbr_FullName`. Opens new DbContext.

### `GetDatabaseName(connectionString)` ? string

Parses database name from connection string.

### `IsInCurrentPayPeriod(today, date)` ? bool

Determines if a date falls within the current semi-monthly pay period (1st-15th or 16th-end of month).

### `UpdateDailyCrewPositionVacancies(...)` � See Part 6

### `CreateUpdateVacancyRequest(...)` � Error recovery for vacancy processing

---

## DateTimeUtilities

### `DateTimeExtensions.StartOfWeek(dt, startOfWeek)` ? DateTime (Extension Method)

```csharp
int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
return dt.AddDays(-1 * diff).Date;
```

### `CalculateYears(startDate)` ? int

**Unusual implementation** � uses float subtraction:
```csharp
var now = float.Parse(DateTime.Now.ToString("yyyy.MMdd"));
var dob = float.Parse(startDate.ToString("yyyy.MMdd"));
return (int)(now - dob);
```
Example: `2025.0115 - 1990.0620 = 35.xxxx ? 35`
Note: This loses precision for dates close to today's month/day.

### `CreateDateFromString(datestr)` ? DateTime

Parses non-delimited date strings:
- 6 digits: `MMDDYY` ? `DateTime(20YY, MM, DD)`
- 5 or 7 digits: prepends `"0"` first
- Delimited dates (`/` or `-`): falls through (empty implementation)

### `CalculateDays(startDate, countFirstDay)` ? int

Days between start and today. If `countFirstDay`, subtracts 1 from start.

### `GetFutureDate(startDate, addDays, countFirstDay)` ? DateTime

Adds days to start. Floors to today if result is in the past.

### `GetDaysToAdd(current, desired)` ? int

Calculates days to add from one day-of-week to another (wraps around week).

---

## StringUtilities

### `IsNumeric(str)` ? bool

Character-by-character check for `'0'-'9'`. No negative/decimal support.

### `RemoveNonNumericCharacters(str)` ? string

Strips all non-digit characters.

### `AddLeadingZeros(cnt, str)` ? string

Pads string with leading zeros to reach `cnt` length.

### `GetNthIndex(s, c, n)` ? int

Finds the index of the Nth occurrence of character `c` in string `s`.

---

## EventLogger

**Default source**: `"Train Crew Reporting"`
**Default log**: `"Application"`

### Overloads

All methods auto-create the event source if it doesn't exist:

| Method | Parameters | EventType | Default ID |
|---|---|---|---|
| `WriteInformationLogEvent` | `(source, log, message, id=200)` | Information | 200 |
| `WriteInformationLogEvent` | `(source, message, id=200)` | Information | 200 |
| `WriteInformationLogEvent` | `(message, id=200)` | Information | 200 |
| `WriteWarningLogEvent` | `(source, log, message, id=800)` | Warning | 800 |
| `WriteWarningLogEvent` | `(source, message, id=800)` | Warning | 800 |
| `WriteWarningLogEvent` | `(message, id=800)` | Warning | 800 |
| `WriteErrorLogEvent` | `(source, log, message, id=900)` | Error | 900 |
| `WriteErrorLogEvent` | `(source, message, id=900)` | Error | 900 |
| `WriteErrorLogEvent` | `(message, id=900)` | Error | 900 |
| `WriteErrorLogEvent` | `(Exception)` | Error | 900 |
| `WriteErrorLogEvent` | `(DbEntityValidationError)` | Error | 900 |

The `Exception` overload formats: `"An error occurred in the {Source} application. {Message} {InnerException}"`.

The `DbEntityValidationError` overload formats: `"Property: {PropertyName}, Error: {ErrorMessage}"`.

---

## PayrollUtilities

### `GetDefaultApprovalOfficer(db)` ? long

Resolves default officer based on current user's role:
- No HttpContext OR "Railroad Employee" / "Railroad Timekeeper" ? role `"Railroad Human Resources"`
- Else ? role `"Railroad Auditor"`

### PDF Generation

Uses **iText** library (`iText.Kernel`, `iText.Layout`):
- `PdfFont` from `StandardFonts.COURIER`
- `PageSize.LETTER.Rotate()` for landscape
- Generates payroll reports, earning statements

### `CreatePayrollRecordsFromImport(adpfile)` � Web Upload

Processes uploaded ADP CSV files (same format as service import but via HTTP upload).

### Approval routing � See Part 8

---

## FileUtilities

### `CreateDirectoryPath(path)` ? string

Creates directory if it doesn't exist, returns path.

### `MoveFile(source, destinationDir)` ? void

Moves file to destination directory. Creates directory if needed.

---

## SAClassLibrary Duplicates

The class library (`SAClassLibrary`) contains its own copies of:
- `EventLogger` � same interface, used by Windows services
- `FileUtilities` � same interface
- `TransactionScopeBuilder` � same `CreateReadCommitted()` implementation
- `ClassLibraryUtilities` � service-specific helpers

These exist because the Windows services reference `SAClassLibrary` (not the web app project) to avoid pulling in ASP.NET dependencies.
# Part 13: Configuration Dependencies

## Connection Strings

### Web App (Web.config)

| Name | Server | Database | Notes |
|---|---|---|---|
| `StrategicApplicationsContext` | `sql-svr` | `StrategicApplications` | Production � used by `StrategicApplicationsContext` class |
| `StrategicApplicationsDemoContext` | `sql-svr` | `StrategicApplicationsDemo` | Demo � used by `StrategicApplicationsContext` (base constructor selects by name) |
| `SAClassLibraryContext` | `sql-svr` | `StrategicApplications` | Used when web app references SAClassLibrary |
| `SAClassLibraryDemoContext` | `sql-svr` | `StrategicApplicationsDemo` | Demo equivalent |
| `DevelopmentDatabaseContext` | `localhost` | `DevelopmentDatabase` | Local dev |

All use: `Integrated Security=SSPI`, `MultipleActiveResultSets=True`, `System.Data.SqlClient`.

### Windows Services (App.config)

SAClassLibrary, SADailyCallSheetService, SAImportPayrollService, SAAtHocMessageService all share:

| Name | Server | Database |
|---|---|---|
| `SAClassLibraryContext` | `sql-svr` | `StrategicApplications` |
| `SAClassLibraryDemoContext` | `sql-svr` | `StrategicApplicationsDemo` |

### DbContext Selection

- `StrategicApplicationsContext` constructor: `base("StrategicApplicationsDemoContext")` � **hardcoded to Demo**
- `SAClassLibraryContext` constructor: `base("name=SAClassLibraryContext")` � uses production
- To switch environments: change the constructor parameter or swap connection string names

## AppSettings � Web App

### Infrastructure

| Key | Value | Used By |
|---|---|---|
| `AuthorizedIPSubnets` | `::1, 127.0.0.1, 192.168.101.*, 192.168.102.*, 192.168.105.*` | `ApplicationUtilities.CheckOnPropertyIPAddress()` |
| `RestartAppPoolLocation` | `C:\SA\RestartApplicationPool\RestartApplicationPool.exe` | `ApplicationUtilities.RestartApplicationPool()` |
| `MSMQServer` | `FormatName:DIRECT=OS:SQL-PTRA-SVR\private$\` | Legacy MSMQ reference |

### AtHoc Integration

| Key | Value | Used By |
|---|---|---|
| `AtHocURL` | `https://alerts3.athoc.com` | Base URL for all AtHoc API calls |
| `ClientID` | `ptrausersync-d1166fc05983` | OAuth2 |
| `ClientSecret` | `6ebb946c250e4d0a8eb58a99fd96c215` | OAuth2 |
| `GrantType` | `password` | OAuth2 |
| `UserName` | `SDKServiceAccountV2` | OAuth2 service account |
| `Password` | `SDKV2()%#!@` | OAuth2 |
| `AcrValues` | `tenant:PortTermRail` | OAuth2 tenant |
| `Scope` | `openid%20profile%20athoc.iws.web.api` | OAuth2 (URL-encoded) |
| `GetTokenURL` | `/authservices/auth/connect/token` | Token endpoint |
| `SyncUserURL` | `/api/v2/orgs/PortTermRail/users/SyncByCommonNames?...` | User sync endpoint |
| `PublishAlertURL` | `/api/v2/orgs/PortTermRail/alerts` | Alert publish |
| `GetAlertResponseURL` | `/api/v2/orgs/PortTermRail/alerts/` | Alert response query |
| `DetailsByUsersReportURL` | `/report/DetailsByUsers` | Response detail suffix |
| `AssignmentCallTemplate` | `2b78ec30-5939-4a17-af99-8955e7632d31` | Call alert template GUID |
| `AssignmentMoveTemplate` | `e8ab9a79-641a-4b21-931f-9daef0299a04` | Move alert template GUID |
| `AssignmentConfirmTemplate` | `71801e5c-7b5e-4834-a1ac-8defc7c1b044` | Confirm alert template GUID |

### Teams Webhook URLs

| Key | Purpose |
|---|---|
| `SystemMessage` | System notifications (FRA, mark-offs) |
| `TieUpMessage` | Engineer/Yardman tie-up notifications |
| `SystemSupport` | Service status, timer updates |
| `TestMessage` | Demo/test environment channel |
| `ECallMessage` | Electronic crew calling messages |

All are Office 365 webhook URLs pointing to the `ptrasupport` tenant.

## AppSettings � SADailyCallSheetService

### MSMQ Queues (Production)

| Key | Queue Path |
|---|---|
| `DailyAssignmentShiftQueue` | `FormatName:DIRECT=OS:SQL-SVR\private$\dailyassignmentshift` |
| `DailyAssignmentQueue` | `FormatName:DIRECT=OS:SQL-SVR\private$\dailyassignment` |
| `DailyCrewPositionQueue` | `FormatName:DIRECT=OS:SQL-SVR\private$\dailycrewposition` |
| `DailyOnDutyRecordQueue` | `FormatName:DIRECT=OS:SQL-SVR\private$\dailyondutyrecord` |
| `DailyMarkOffRecordQueue` | `FormatName:DIRECT=OS:SQL-SVR\private$\dailymarkoffrecord` |

### MSMQ Queues (Development)

Same keys with `dev` prefix, pointing to `PTRA-IT-LT-10` machine.

## system.web Configuration

| Setting | Value |
|---|---|
| `authentication` | `Forms` with `loginUrl="~/Account/Login"` |
| `compilation` | `debug="true"`, `targetFramework="4.7.2"` |
| `httpRuntime` | `targetFramework="4.7.2"`, `maxQueryStringLength="4096"` |
| `customErrors` | `mode="Off"`, `defaultRedirect="Error.htm"` |

## system.webServer Configuration

- `requestFiltering`: `maxQueryString="4096"`, `maxUrl="4096"`
- Removes `FormsAuthenticationModule` (OWIN handles auth)
- Configures `ExtensionlessUrlHandler-Integrated-4.0`

## Key Assembly Binding Redirects

| Assembly | Redirected To |
|---|---|
| `Newtonsoft.Json` | `13.0.0.0` |
| `Microsoft.Owin` | `4.1.1.0` |

# Part 57: Incremental Reconciliation – Controller Workflow and Hard-Coded Decision Logic

This pass focuses on high-impact controller-level orchestration logic not fully covered by entity-only documentation.

---

## 57.1 `ProcessPayrollController` Hard-Coded Process Rules

### Sentinel and period behavior

- Last payroll period lookup excludes control number `99999999999999999`.
- When user-selected payroll period contains `1216`, year is forced to previous month/year context before date construction.
- End-of-period paydate is normalized to `23:59:59`.
- If period day is `16`, paydate is shifted to **last day of month** at `23:59:59`.

### Trial vs Final process branching

- **Final process** loads records where any earning line has `PayrollEarningProcessedRecord.FinalProcess == false`.
- **Trial process** removes prior non-final process artifacts for same period, then re-creates export artifacts.

### Hard-coded UNC filesystem output

Controller writes and archives payroll artifacts in hard-coded paths:

- `\\Finance-svr\Payroll Exports\UKG\Logs\`
- `\\Finance-svr\Payroll Exports\UKG\History\{payperiod}\`
- `\\Finance-svr\Payroll Exports\UKG\History\{payperiod}\Logs\`
- `\\Finance-svr\Payroll Exports\UKG\History\{payperiod}\Reports\`

And copies fixed file names when present:

- `UKGPT1.csv` -> history as `UKGPTI.csv`
- `ExcludedTIESRecords.csv`
- `Reports\BatchSummary.txt` -> converted to `BatchSummary.pdf`

### Runtime data correction behavior before export

For trial runs, controller actively repairs denormalized references in `PayrollRecord` when mismatches are detected:

- `EmployeeControlNumber`
- `RailroadEmployeeControlNumber`
- `RailroadPoolEmployeeControlNumber`

Corrections are logged to `badpayrollrecords.log`.

### Progress/update cadence

Controller updates static status text (`ProcessPayrollController.Status`) throughout long-running steps, including:

- retrieval
- validation
- file generation
- finalization counters

---

## 57.2 `MarkOffRequestController` Hard-Coded Time/Duration Rules

### Mark-up hours to selectable durations mapping

`Edit` view maps automatic mark-up hour values to fixed day/week options:

| AutomaticMarkUpHours | UI Options |
|---|---|
| `24` | `1 day` |
| `48` | `2 days` |
| `168` | `Will Mark Up When Ready`, `1 week` |
| `336` | `Will Mark Up When Ready`, `2 weeks` |
| `504` | `Will Mark Up When Ready`, `3 weeks` |
| `672` | `Will Mark Up When Ready`, `4 weeks` |
| `840` | `Will Mark Up When Ready`, `5 weeks` |

### Vacation week inference logic

If `AutomaticMarkUpHours(craft) == 0` and code is vacation week (`V*`, excluding `VD`, not system-use-only):

- week count is derived from mark-off code digit (`V1..V5`)
- days = `weekDigit * 7`
- mark-up datetime = `MarkOffDateTime + days`

### Minute normalization and extra-board immediacy

- Mark-off timestamps are normalized with `+1 minute` in several paths.
- If employee is extra board and mark-off is not in the future, mark-off time is forced to `DateTime.Now`.

### Pool-specific vacation relief flag behavior

- Pool `40` (Mechanical) has inverted vacation-relief handling in mark-off creation paths (`vacrelief` derived from pool number and `V*` code).

### Timer side effects

After request changes and auto-mark-off updates:

- `MvcApplication.SetMarkOffRequestTimer(poolControlNumber)` is called to reschedule processing.

---

## 57.3 `DailyAssignmentShiftController` Runtime Orchestration Rules

### Controller is queue-producer, not direct creator

`Create` action does not create shift records directly; it sends MSMQ message:

`poolCtr,shiftCtr,yyyy-MM-dd,createCrewPositions`

Queue label/type:

- queue name: `DailyAssignmentShift`
- label: `Create`

### UI exposure of sentinel scheduling

Index action reads `MvcApplication.nextCallSheetUpdates` and `nextExtraBoardUpdates`.

If year is `9999`, UI displays:

- `No Automatic Updates Scheduled`

### Current-extra-board redirect behavior

When `current=true` and selected shift extra boards are completed, controller auto-selects newest shift that still has extra boards.

---

## 57.4 `AccountController` Login Tracking and On-Property Derivation

On sign-in:

1. prior external cookie is signed out
2. app cookie issued
3. `SetLastLoginDateTime(userId)` runs
4. user is registered in `MvcApplication.ActiveUsers`

### Login-history record behavior

`CreateUserLoginRecord` persists previous login snapshot (`user.LastLogin`, `user.OnProperty`, `user.IPAddress`) before user object is updated to current login values.

### On-property flag

`OnProperty` is derived from `ApplicationUtilities.CheckOnPropertyIPAddress(ip)` and therefore tied to `AuthorizedIPSubnets` wildcard matching rules.

---

## 57.5 Cross-Cut Hard-Coded Patterns Observed in Controller Layer

- Broad use of string-based role checks in `[Authorize]` attributes.
- Multiple long-running actions rely on static/shared status fields for user feedback.
- Heavy use of fixed UNC paths for payroll operations and archiving.
- Many time rules are encoded via concrete minute/day offsets instead of configuration keys.

This section is the controller-process complement to service/runtime sections and should be read with Parts 49–56.
| `Microsoft.Owin.Security` | `4.1.1.0` |
| `Microsoft.Owin.Security.Cookies` | `4.1.1.0` |
| `Microsoft.AspNet.Identity.Core` | `2.0.0.0` |
| `System.Web.Helpers` | `3.0.0.0` |
| `System.Web.Mvc` | `5.2.7.0` |
| `System.Web.WebPages` | `3.0.0.0` |

## File Share Dependencies

| Path | Purpose | Used By |
|---|---|---|
| `\\sql-svr\SA\Message Queue\Inbound` | Production message queue files | Global.asax FileSystemWatchers |
| `\\sql-svr\SA\dev\Message Queue\Inbound` | Development message queue files | Global.asax Dev watchers |
| `\\finance-svr\c$\Payroll Exports\ADP\Imports` | ADP payroll import | SAImportADPPayrollService |
| `\\finance-svr\c$\Payroll Exports\UKG\Imports` | UKG payroll import | SAImportUKGPayrollService |

### File Extensions

| Extension | Purpose | Handler |
|---|---|---|
| `*.hr` | Holiday records | `TriggerHolidayRecordWatcherEvent` |
| `*.uv` | Vacancy updates | `TriggerVacancyUpdateWatcherEvent` |
| `*.esr` | Employee status records | `TriggerStatusUpdateWatcherEvent` |
| `PRPT1*.*` | Payroll export files | ADP/UKG import services |

## Server Infrastructure

| Server | Role |
|---|---|
| `sql-svr` | SQL Server (production DB), file shares, MSMQ |
| `finance-svr` | Payroll file exports (ADP/UKG) |
| `localhost` | Development database |
| `PTRA-IT-LT-10` | Development MSMQ |
# Part 17: CollectionLists / Query Layer

## Overview

The query layer consists of two static classes containing LINQ-to-Entities query methods that serve as a data access abstraction. Both live in `StrategicApplications\Models\Queries\`.

| File | Class | Methods | Lines |
|---|---|---|---|
| `CollectionLists.cs` | `CollectionLists` | ~195 | ~2,941 |
| `Collections.cs` | `Collections` | ~169 | Similar |

**`Collections.cs` is a near-duplicate of `CollectionLists.cs`** � many methods are identical. `CollectionLists` appears to be the actively maintained version with additional methods.

All methods are `public static`, take a `StrategicApplicationsContext` (or occasionally `SAClassLibraryContext`) as the first parameter, and return `ICollection<T>` or single entities.

## Method Categories

### Railroad Reference Data

| Method | Returns | Filter |
|---|---|---|
| `GetRailroadAFEs(db, railroad)` | `ICollection<RailroadAFE>` | By railroad, ordered by AFENumber |
| `GetRailroadLocations(db, railroad)` | `ICollection<RailroadLocation>` | By railroad, ordered by LocationNumber |
| `GetRailroadWorkCodes(db, railroad)` | `ICollection<RailroadWorkCode>` | By railroad, ordered by WorkCodeNumber |
| `GetRailroadLocomotiveTypes(db, railroad)` | `ICollection<RailroadLocomotiveType>` | By railroad, ordered by LocomotiveType |
| `GetRailroadMaterialTypes(db, railroad)` | `ICollection<RailroadMaterial>` | By railroad, ordered by category then code |
| `GetRailroadMaterialCategories(db, railroad)` | `ICollection<RailroadMaterialCategory>` | By railroad |
| `GetRailroadMaterials(db, category)` | `ICollection<RailroadMaterial>` | By category |
| `GetRailroadZones(db, railroad)` | `ICollection<RailroadZone>` | By railroad |
| `GetRailroadInformationTypes(db, railroad)` | `ICollection<RailroadInformationType>` | By railroad |
| `GetEngineerJobCodes(db, railroad)` | `ICollection<EngineerJobCode>` | Non-deleted, ordered by MaxWeightOnDrivers |

### Safety (SAClassLibraryContext)

| Method | Returns | Notes |
|---|---|---|
| `GetSlowOrderAreas(db, railroad)` | `ICollection<SlowOrderArea>` | Uses `SAClassLibraryContext` |
| `GetSlowOrderRecords(db, railroad)` | `ICollection<SlowOrderRecord>` | Ordered by area then title |
| `GetBeSafeAreas(db, railroad)` | `ICollection<BeSafeArea>` | |
| `GetBeSafeSubdivisions(db, railroad)` | `ICollection<BeSafeSubdivision>` | |
| `GetBeSafeCategories(db, railroad)` | `ICollection<BeSafeCategory>` | |
| `GetBeSafeEmailGroups(db, railroad)` | `ICollection<BeSafeEmailGroup>` | |

### Employee Queries

| Method | Returns | Key Filter Logic |
|---|---|---|
| `GetActiveRailroadEmployees(db, railroad)` | `ICollection<RailroadEmployee>` | Status `"AT"`, not removed, ordered by name |
| `GetActiveRailroadEmployeesForPayroll(db, railroad)` | `ICollection<RailroadEmployee>` | Includes recently terminated (XE within 90 days) |
| `GetActiveRailroadEmployeesForPayroll60of90Days(db, railroad)` | `ICollection<RailroadEmployee>` | At least 60 active statuses in last 90 daily records |
| `GetActiveRailroadPoolEmployees(db, pool)` | `ICollection<RailroadPoolEmployee>` | Active/CutBack seniority, not removed |
| `GetActiveRailroadPoolEmployeesNotMarkedOff(db, pool)` | `ICollection<RailroadPoolEmployee>` | Active, no open mark-off records |
| `GetActiveRailroadPoolEmployeesWithoutMarkOffRequest(db, pool, reqdate)` | `ICollection<RailroadPoolEmployee>` | Active, no request on given date |
| `GetActiveCraftRailroadPoolEmployees(db, pool, rrposition)` | `ICollection<RailroadPoolEmployee>` | Qualified for position, not currently assigned, ordered by seniority |

### Approval Officers

| Method | Returns | Key Logic |
|---|---|---|
| `GetCraftApprovalOfficers(db, client, craft)` | `ICollection<Employee>` | In role `"1d78b8ea..."`, active, assigned to craft |
| `GetUnassignedCraftApprovalOfficers(db, client, craft)` | `ICollection<Employee>` | Same role, NOT assigned to craft |
| `GetAlternateCraftSupervisors(db, craft)` | `ICollection<CraftApprovalOfficer>` | Non-primary officers |

### Position Queries

| Method | Returns | Key Logic |
|---|---|---|
| `GetRailroadPoolPositions(db, pool)` | `ICollection<Position>` | All positions in pool |
| `GetDailyCrewPositions(db, crew, adate)` | `ICollection<CrewPosition>` | Non-deleted, effective before date |

### Payroll Queries

| Method | Returns | Key Logic |
|---|---|---|
| `GetApprovalPayrollEarningRecords(db, approval)` | `ICollection<PayrollEarningRecord>` | Pending approval, non-deleted, for specific officer |

### Crew & Assignment Queries

| Method | Returns | Key Logic |
|---|---|---|
| `GetAssignmentCrew(db, assignment, date)` | `Crew` | Join with AssignmentOnDutyDays by day name |
| `GetAllDailyCrews(db, pool, date)` | `ICollection<CrewAssignment>` | Active crews, effective, not abolished, not extra assignment, ordered by shift then name |
| `GetAllDailyAssignmentsByShift(db, pool, date, shift)` | `ICollection<Assignment>` | Active, matching shift, not yet having DailyAssignment for this date |
| `GetAllDailyTemporaryAssignmentsByShift(db, pool, date, shift)` | `ICollection<TemporaryAssignment>` | Active temp assignments with matching work days |
| `GetAssignmentShifts(db, pool, shift)` | `ICollection<Shift>` | Excludes relief shifts |

### Mark-Off Code Queries

| Method | Returns | Key Logic |
|---|---|---|
| `GetClientMarkOffCodes(db, fmla)` | `ICollection<MarkOffCode>` | Non-system, requestable; optionally includes FMLA |
| `GetCraftMarkOffCodes(db, client, craft, request, missedcall, calledrelief)` | `ICollection<MarkOffCode>` | Excludes craft-excluded codes; filters by purpose flags |
| `GetCraftWaitListMarkOffCodes(db, pool)` | `ICollection<MarkOffCode>` | Codes ending in "D" with payroll code, non-excluded |

### Roster & Board Queries

| Method | Returns | Key Logic |
|---|---|---|
| `GetRailroadPoolRosters(pool)` | `ICollection<Roster>` | Has active seniority (opens own DbContext) |
| `GetRailroadPoolRostersWithTraining(pool)` | `ICollection<Roster>` | Active seniority or CanTrain (opens own DbContext) |
| `GetRailroadPoolExtraBoards(db, pool)` | `ICollection<RosterBoard>` | ExtraBoard != 0, has active seniority |
| `GetRailroadPoolDailyExtraBoards(db, pool, shift)` | `ICollection<DailyShiftExtraBoard>` | By shift and pool |
| `GetRailroadPoolDailyOvertimeBoards(db, pool, shift)` | `ICollection<DailyShiftOvertimeBoard>` | By shift and pool |

### FRA / On-Duty Queries

| Method | Returns | Key Logic |
|---|---|---|
| `GetRailroadEmployeeNextOnDutyRecord(db, rremployee)` | `DailyCrewPositionOnDutyRecord` | Future on-duty record: not annulled, not DoNotFill, no off-duty, no payroll; ordered descending by date/time |

### Phone / Email

| Method | Returns | Key Logic |
|---|---|---|
| `GetEmployeePhoneNumbers(db, employee)` | `ICollection<PhoneNumber>` | Ordered by CallingOrder, formats 10-digit numbers |
| `GetEmployeeNotificationPhoneNumbers(db, employee)` | `ICollection<PhoneNumber>` | Excludes emergency-type numbers |

### Seniority & Bulletin Queries

| Method | Returns | Key Logic |
|---|---|---|
| `GetForceAssignmentSeniorityList(db, positionName, rosterControlNumber)` | `ICollection<Seniority>` | Most junior first � for no-bid force assignment |
| `GetRailroadPositionBulletinBids(db, bulletinControlNumber)` | `ICollection<RailroadPositionBulletinBid>` | Ordered by seniority for automatic assignment |

### Identity / Role Queries

| Method | Returns | Key Logic |
|---|---|---|
| `GetRoleUsers(db, roleId)` | `ICollection<ApplicationUser>` | All users in a role |
| `GetPrimaryRoleUsers(db, roleId)` | `ICollection<ApplicationUser>` | Users whose `PrimaryRoleID` matches |

### Vacancy Queries

| Method | Returns | Key Logic |
|---|---|---|
| `GetDailyCrewPositionVacancies(db, pool)` | `List<DailyCrewPosition>` | Pool-level vacancy query (see Part 6) |
| `GetDailyCrewPositionVacancies(db, roster, shift)` | `List<DailyCrewPosition>` | Roster/shift-level vacancy query |

## Query Patterns

### Common Filter Pattern

Most queries follow this structure:
```csharp
db.EntitySet
    .Where(e => e.ForeignKey.Equals(parameter)
        && additional_conditions)
    .OrderBy(e => e.SortField)
    .ThenBy(e => e.SecondarySortField)
    .ToList();
```

### Except Pattern (Exclusion Queries)

Used to find "unassigned" or "not yet processed" entities:
```csharp
db.EntitySet.Where(base_conditions)
    .Except(db.EntitySet
        .Join(db.RelatedSet.Where(filter),
            e => e.Key,
            r => r.ForeignKey,
            (e, r) => new { Entity = e })
        .Select(x => x.Entity))
    .ToList();
```

### Join Pattern (Cross-Entity Filtering)

Used when filtering requires traversing relationships not directly navigable:
```csharp
db.PrimarySet.Where(conditions)
    .Join(db.SecondarySet.Where(secondaryConditions),
        p => p.Key,
        s => s.ForeignKey,
        (p, s) => new { Primary = p, Secondary = s })
    .Select(x => x.Primary)
    .ToList();
```

### Self-Opening Context Pattern

A few methods open their own `StrategicApplicationsContext`:
```csharp
public static ICollection<Roster> GetRailroadPoolRosters(long pool)
{
    using (var db = new StrategicApplicationsContext())
    {
        return db.Rosters.Where(...).ToList();
    }
}
```
These are called from contexts where no DbContext is available (e.g., timer handlers).

## Key Observations

1. **No repository pattern** � queries are direct LINQ-to-Entities against DbContext
2. **No caching** � every call hits the database
3. **Eager materialization** � all queries call `.ToList()`, materializing entire result sets
4. **Duplicate classes** � `Collections.cs` and `CollectionLists.cs` share ~90% of methods
5. **Mixed DbContext usage** � most use `StrategicApplicationsContext`; safety/BeSafe queries use `SAClassLibraryContext`
6. **Hard-coded GUIDs** � role IDs like `"1d78b8ea-f36b-42a7-91fa-325f543aa2e9"` appear directly in queries
7. **Phone formatting** � query methods format phone numbers (presentation logic in data layer)
# Part 18: SAClassLibrary � Shared Class Library

## Overview

`SAClassLibrary` is a separate class library project that provides a **read-only data access layer** for the Windows services. It mirrors the web app's entity model but uses its own `DbContext` and contains no business logic methods (no `Create`, `Assign`, etc.). The Windows services reference this library instead of the web app to avoid ASP.NET dependencies.

**Target Framework**: .NET Framework 4.7.2

## Why It Exists

The web app's entities (`StrategicApplications.Models`) contain domain logic that depends on ASP.NET (`HttpContext`, `MvcApplication`, Identity, etc.). Windows services cannot reference those. `SAClassLibrary` provides:

1. Entity models as **plain POCOs** � properties only, no methods
2. A separate `DbContext` � `SAClassLibraryContext`
3. Duplicated utility classes � `EventLogger`, `FileUtilities`, `TransactionScopeBuilder`
4. Access to the same SQL Server database

## SAClassLibraryContext

```csharp
public partial class SAClassLibraryContext : DbContext
{
    public SAClassLibraryContext() : base("name=SAClassLibraryDemoContext")
    {
        Database.SetInitializer<SAClassLibraryContext>(null);
    }
    // ~220 DbSet properties
}
```

- Constructor hardcoded to `"SAClassLibraryDemoContext"` (same pattern as web app � points to Demo)
- `Database.SetInitializer(null)` � disables EF migrations (read-only context)
- Contains **~220 DbSet properties** � one for every table in the database
- No `OnModelCreating` override � relies on convention and data annotations

## Entity Models � POCO Only

SAClassLibrary models are property-only versions of the web app entities. Example comparison:

**Web App** (`StrategicApplications\Models\Seniority.cs`):
- Inherits `ControlNumberBase`
- Has `[NotMapped]` computed properties (`SeniorityYears`, `RosterDate_Rank`)
- Has `Create()`, `CreateSeniorityFile()` methods
- References `ApplicationUtilities`, `AtHocService`, etc.

**Class Library** (`SAClassLibrary\Models\Seniority.cs`):
- Standalone class with `[Key]` on `ControlNumber`
- Only stored properties + navigation properties
- No methods, no computed properties, no business logic

## Project Structure

```
SAClassLibrary\
??? BaseClasses\
?   ??? ControlNumberBase.cs        � Same as web app's ControlNumberBase
??? Context\
?   ??? SAClassLibraryContext.cs     � DbContext with ~220 DbSets
??? Interfaces\
?   ??? IControlNumber.cs           � Same interface as web app
??? Models\
?   ??? (220+ .cs files)            � POCO entity models
?   ??? FRARequirements.cs          � Static constants only (no methods)
??? Utilities\
?   ??? ClassLibraryUtilities.cs    � Service-specific helpers
?   ??? EventLogger.cs              � Windows Event Log wrapper (duplicate)
?   ??? FileUtilities.cs            � File operations (duplicate)
?   ??? TransactionScopeBuilder.cs  � Transaction scope (duplicate)
??? Migrations\
?   ??? Configuration.cs
?   ??? 202006201230575_Sync_Database1.cs
?   ??? 202201111714028_Sync_Database2.cs
?   ??? 202203041312488_AddUkGInterface.cs
?   ??? 202204181731424_ChangePayrollRecord_AddRatePercentage.cs
?   ??? 202208221414114_ChangeBeSafeRecord_AddRecordNumber.cs
??? App.config
```

## Duplicated Utilities

These classes exist in both the web app and the class library with identical interfaces:

| Class | Web App Path | SAClassLibrary Path |
|---|---|---|
| `EventLogger` | `StrategicApplications\Utilities\` | `SAClassLibrary\Utilities\` |
| `FileUtilities` | `StrategicApplications\Utilities\` | `SAClassLibrary\Utilities\` |
| `TransactionScopeBuilder` | Nested in `ApplicationUtilities` | `SAClassLibrary\Utilities\` |
| `ControlNumberBase` | `StrategicApplications\Models\BaseClasses\` | `SAClassLibrary\BaseClasses\` |
| `IControlNumber` | `StrategicApplications\Models\Interfaces\` | `SAClassLibrary\Interfaces\` |

## Consumers

| Project | How It Uses SAClassLibrary |
|---|---|
| `SADailyCallSheetService` | Reads shifts, assignments, crews, positions to create call sheets |
| `SAImportPayrollService` | Reads/writes payroll records for ADP/UKG import |
| `SAAtHocMessageService` | Reads vacancies, employees, positions for electronic calling |
| `StrategicApplications` (web) | Some `CollectionLists` queries use `SAClassLibraryContext` for BeSafe/SlowOrder data |

## Migrations

The class library has its own EF migrations, separate from the web app:

| Migration | Date | Change |
|---|---|---|
| `Sync_Database1` | 2020-06-20 | Initial sync with production schema |
| `Sync_Database2` | 2022-01-11 | Schema sync update |
| `AddUkGInterface` | 2022-03-04 | Added `UKGInterface` table |
| `ChangePayrollRecord_AddRatePercentage` | 2022-04-18 | Added `RatePercentage` to PayrollRecord |
| `ChangeBeSafeRecord_AddRecordNumber` | 2022-08-22 | Added `RecordNumber` to BeSafeRecord |

## Key Differences from Web App Models

1. **No `ControlNumberBase` inheritance** on most models � instead uses `[Key][DatabaseGenerated(None)] public long ControlNumber`
2. **No `[NotMapped]` computed properties** � no `IsOnDuty`, `IsRested`, etc.
3. **No domain methods** � no `CreateMarkOffRecord()`, `Assign()`, etc.
4. **No ASP.NET references** � no `HttpContext`, `MvcApplication`, Identity
5. **Namespace**: `SAClassLibrary.Models` vs `StrategicApplications.Models`
6. **Navigation properties preserved** � same FK relationships for EF navigation
# Part 22: Requirements & Qualifications

## Overview

Two systems control what employees can do: **Requirements** (certifications/training that expire) and **Qualifications** (position-specific abilities).

## Requirement

**Inherits**: `ControlNumberBase`

Defines a certification or training requirement.

| Property | Type | Description |
|---|---|---|
| `RequirementNumber` | `int` | Unique number |
| `RequirementTerm` | `int` | Duration (default 3 years) |
| `RenewDelayDays` | `int` | Days before expiry to renew (default 364) |
| `RequirementType` | `string` | `[StringLength(12)]` � type classification |
| `RequirementName` | `string` | `[StringLength(50)]` |
| `RequirementDescription` | `string` | `[StringLength(500)]` |
| `CalendarYear` | `bool` | Whether term is calendar-year based |

### Requirement Hierarchy

Requirements can be assigned at multiple levels:

| Entity | Join Table | Scope |
|---|---|---|
| `Client` | `ClientRequirement` ? `ClientRequirementEmployee` | All employees |
| `Railroad` | `RailroadRequirement` ? `RailroadRequirementEmployee` | Railroad employees |
| `RailroadPool` | `RailroadPoolRequirement` ? `RailroadPoolRequirementEmployee` | Pool employees |
| `Craft` | `CraftRequirement` ? `CraftRequirementEmployee` | Craft employees |
| `Position` | `PositionRequirement` ? `PositionRequirementEmployee` | Position-specific |

Each `*RequirementEmployee` record tracks: `EmployeeControlNumber`, `CompletedDate`, `ExpirationDate`.

---

## Qualification

**Inherits**: `ControlNumberBase`

Links an employee to a position they're qualified to work.

| Property | Type | Description |
|---|---|---|
| `PositionControlNumber` | `long` | FK to Position |
| `RailroadPoolEmployeeControlNumber` | `long` | FK to RailroadPoolEmployee |
| `EffectiveDate` | `DateTime` | When qualification takes effect |

### Usage

- `RailroadPoolEmployee.IsQualified(position)` checks if the employee has a qualification for that position with `EffectiveDate <= Now`
- Used by bulletin assignment (`AutomaticAssignment`) to verify bidders
- Used by vacancy assignment to verify extra board employees can fill positions
- Used by `GetActiveCraftRailroadPoolEmployees()` query to filter eligible employees
# Part 24: Controllers Overview

## Overview

The web app has **113 MVC controllers** in `StrategicApplications\Controllers\`. All inherit from `Controller` and follow standard ASP.NET MVC 5 patterns.

## Common Controller Pattern

```csharp
public class XxxController : Controller
{
    readonly StrategicApplicationsContext db = new StrategicApplicationsContext();
    // Actions: Index, Details, Create, Edit, Delete
}
```

- DbContext is instantiated as a field (not injected)
- No dependency injection framework
- No base controller class (each inherits `Controller` directly)

## Controller Categories

### System Administration
| Controller | Purpose |
|---|---|
| `AccountController` | Login, logout, user management, password reset |
| `AdminController` | System admin dashboard |
| `HomeController` | Home page, landing pages |

### Organization
| Controller | Purpose |
|---|---|
| `ClientController` | Client CRUD |
| `RailroadController` | Railroad CRUD |
| `RailroadPoolController` | Pool CRUD |
| `CraftController` | Craft CRUD |
| `RosterController` | Roster CRUD |
| `RosterBoardController` | Board CRUD |
| `RosterBoardPositionController` | Board position CRUD |
| `LocationController` | Location CRUD |

### Employee Management
| Controller | Purpose |
|---|---|
| `EmployeeController` | Employee CRUD |
| `EmployeeDetailController` | Employee self-service detail view |
| `RailroadEmployeeController` | Railroad-level employee management |
| `RailroadPoolEmployeeController` | Pool-level employee management |
| `EmploymentStatusController` | Employment status changes |
| `AddressController` | Employee addresses |
| `PhoneNumberController` | Employee phone numbers |
| `EmailAddressController` | Employee email addresses |

### Seniority & Bulletins
| Controller | Purpose |
|---|---|
| `SeniorityController` | Seniority record management |
| `RailroadPoolEmployeeSeniorityController` | Employee-level seniority |
| `SeniorityMoveController` | Seniority move processing |
| `RailroadPoolEmployeeSeniorityMoveController` | Employee seniority moves |
| `RailroadPositionBulletinController` | Bulletin management |
| `RailroadPositionBulletinBidController` | Bid management |
| `RailroadPoolEmployeeBulletinBidController` | Employee bid management |
| `RosterBulletinRuleController` | Bulletin rule config |
| `RosterSeniorityMoveRuleController` | Move rule config |

### Crew & Assignment
| Controller | Purpose |
|---|---|
| `CrewController` | Crew CRUD |
| `CrewPositionController` | Crew position management |
| `CrewPositionAlternatePositionController` | Alternate position config |
| `CrewAbolishmentController` | Crew abolishment |
| `CrewOffDayController` | Off-day configuration |
| `AssignmentController` | Assignment CRUD |
| `AssignmentOnDutyDayController` | On-duty day config |
| `AssignmentOnDutyTimeController` | On-duty time config |
| `AssignmentAbolishmentController` | Assignment abolishment |
| `AssignmentTypeController` | Assignment type config |
| `TemporaryAssignmentController` | Temp assignment management |
| `TemporaryAssignmentWorkDayController` | Temp work day config |

### Daily Operations
| Controller | Purpose |
|---|---|
| `DailyAssignmentShiftController` | Call sheet / shift management |
| `DailyAssignmentController` | Daily assignment operations |
| `DailyCrewPositionController` | Daily position management |
| `DailyReportController` | Daily report generation |
| `FillVacancyController` | Vacancy fill operations |
| `DailyOnDutyRecordTieUpController` | Tie-up processing |
| `NotificationController` | Change notifications |

### On-Duty Billing
| Controller | Purpose |
|---|---|
| `DailyOnDutyAFEBillingController` | AFE billing records |
| `DailyOnDutyZoneBillingController` | Zone billing records |
| `DailyOnDutyMiscellaneousBillingController` | Misc billing |
| `DailyOnDutyLocomotiveRecordController` | Locomotive records |
| `DailyOnDutyRailroadMaterialRecordController` | Material records |
| `DailyOnDutyFlagBillingController` | Flag billing |

### Mark-Off
| Controller | Purpose |
|---|---|
| `MarkOffRecordController` | Mark-off CRUD |
| `MarkOffRequestController` | Mark-off request management |
| `MarkOffRequestWaitListController` | Wait list management |
| `MarkOffCodeController` | Mark-off code config |
| `CraftMarkOffCodeController` | Craft-level code config |

### Payroll
| Controller | Purpose |
|---|---|
| `PayrollController` | Payroll record management |
| `ProcessPayrollController` | Payroll processing/export |
| `PayrollReportController` | Payroll reports |
| `PayrollCodeController` | Payroll code config |
| `PayrollCategoryController` | Category config |
| `PayrollCategoryCodeController` | Category code config |
| `PayrollCodeApprovalRoleController` | Approval role config |
| `PayrollCodePayRateController` | Pay rate config |
| `PayrollReportGroupController` | Report group config |
| `PayrollReportGroupCategoryController` | Report group categories |
| `PayrollCrewPositionAutoPayController` | Auto-pay config |
| `ADPInterfaceController` | ADP interface records |
| `UKGInterfaceController` | UKG interface records |
| `RailroadEmployeeCompensableTimeRecordController` | Comp time management |

### Position & Qualification
| Controller | Purpose |
|---|---|
| `PositionController` | Position CRUD |
| `RailroadPositionController` | Railroad position management |
| `QualificationController` | Qualification management |
| `RailroadPoolEmployeeQualificationController` | Employee qualifications |
| `PositionPayRateController` | Position pay rates |
| `HoldDownController` | Hold-down assignments |

### Requirements
| Controller | Purpose |
|---|---|
| `RequirementController` | Requirement CRUD |
| `ClientRequirementEmployeeController` | Client-level |
| `RailroadRequirementEmployeeController` | Railroad-level |
| `RailroadPoolRequirementEmployeeController` | Pool-level |
| `CraftRequirementEmployeeController` | Craft-level |
| `PositionRequirementEmployeeController` | Position-level |

### Safety
| Controller | Purpose |
|---|---|
| `BeSafeController` | BeSafe record management |
| `BeSafeAreaController` | Area config |
| `BeSafeCategoryController` | Category config |
| `BeSafeSubdivisionController` | Subdivision config |
| `BeSafeEmailGroupController` | Email group config |
| `SlowOrderController` | Slow order management |
| `SlowOrderAreaController` | Slow order area config |
| `LocomotiveInspectionRecordController` | Locomotive inspections |

### Railroad Reference Data
| Controller | Purpose |
|---|---|
| `RailroadAFEController` | AFE management |
| `RailroadLocationController` | Location management |
| `RailroadWorkCodeController` | Work code management |
| `RailroadZoneController` | Zone management |
| `RailroadLocomotiveTypeController` | Locomotive types |
| `RailroadMaterialController` | Materials |
| `RailroadMaterialCategoryController` | Material categories |
| `RailroadPayrollDepartmentController` | Payroll departments |
| `EngineerJobCodeController` | Engineer job codes |
| `EngineerPayRateController` | Engineer pay rates |
| `RailroadPoolPayrollTierController` | Payroll tiers |
| `RailroadInformationController` | Information records |
| `RailroadInformationTypeController` | Information types |
| `RailroadEmployeeVacationRequestController` | Vacation requests |

### Other
| Controller | Purpose |
|---|---|
| `CraftApprovalOfficerController` | Approval officer config |
| `CraftPersonalDaysController` | Personal day config |
| `CraftSickDaysController` | Sick day config |
| `CraftVacationDaysController` | Vacation day config |
| `DescriptionController` | Description CRUD |
| `OnDutyMoveCutOffTimeController` | Move cut-off config |
| `PrintPDFController` | PDF report generation |
| `RailroadPoolEmployeeTrainingDateController` | Training dates |
| `ShiftController` | Shift config |
| `WeekDayController` | Week day config |
# Part 25: BeSafe & Slow Order Subsystem

## Overview

Two safety tracking systems: **BeSafe** (safety observations/hazard reports) and **Slow Orders** (track speed restrictions).

## BeSafe System

Safety observation reporting. Uses `SAClassLibraryContext` for some queries.

### Entities

| Entity | Description |
|---|---|
| `BeSafeRecord` | Core observation record (`RecordNumber`, area, category, description) |
| `BeSafeArea` | Geographic areas for observations |
| `BeSafeSubdivision` | Track subdivisions |
| `BeSafeCategory` | Observation categories (safety, hazard, etc.) |
| `BeSafeEmailGroup` | Notification email groups |
| `BeSafeActionRecord` | Corrective action taken |
| `BeSafeChangeRecord` | Changes to observation |
| `BeSafeResolveRecord` | Resolution record |
| `BeSafeDeleteRecord` | Soft delete |

### Lifecycle: Open ? Action ? Resolve ? Close/Delete

## Slow Order System

Track speed restriction management. Also uses `SAClassLibraryContext`.

### Entities

| Entity | Description |
|---|---|
| `SlowOrderRecord` | Core record (area, title, speed limit, milepost range) |
| `SlowOrderArea` | Geographic areas for slow orders |
| `SlowOrderChangeRecord` | Changes to slow order |
| `SlowOrderCompleteRecord` | Completion/lifting of restriction |
| `SlowOrderDeleteRecord` | Soft delete |

### Lifecycle: Created ? Changed (optional) ? Completed ? Deleted (optional)
# Part 26: Railroad Information System

## Overview

Publishes bulletins and announcements to railroad employees with read-tracking.

### Entities

| Entity | Description |
|---|---|
| `RailroadInformationType` | Category types (by railroad) |
| `RailroadInformationRecord` | Core record (type, title, body, effective date) |
| `RailroadInformationPublishRecord` | Marks record as published |
| `RailroadInformationReadbyEmployeeRecord` | Tracks which employees read it |
| `RailroadInformationCancelRecord` | Cancellation |
| `RailroadInformationCloseRecord` | Closure/completion |
| `RailroadInformationDeleteRecord` | Soft delete |
| `RailroadPositionChangeRailroadInformationRecord` | Links info to position changes |

### Timer

`PublishRailroadInformationTimers` in Global.asax auto-publishes records at scheduled times.

### Employee View

`RailroadPoolEmployeeBulletinsViewedRecord` tracks when pool employees view bulletins.
# Part 30: Views & ModelViews

## Overview

The MVC layer uses Razor views (`.cshtml`) with dedicated view model classes in `StrategicApplications\Models\ModelViews\`.

## ModelViews

**103 view model files** in `StrategicApplications\Models\ModelViews\`.

### Pattern

View models follow this naming convention:
- `{Entity}Views.cs` � contains multiple view model classes per file
- Each file typically contains: `IndexViewModel`, `DetailsViewModel`, `CreateViewModel`, `EditViewModel`

### Common Structure

```csharp
public class XxxIndexViewModel
{
    public ICollection<Xxx> Items { get; set; }
    public SelectList FilterOptions { get; set; }
}

public class XxxDetailsViewModel
{
    public Xxx Item { get; set; }
    public ICollection<RelatedEntity> Related { get; set; }
}

public class XxxCreateViewModel
{
    public Xxx Item { get; set; }
    public SelectList Lookups { get; set; }
}
```

### Key View Model Files

| File | Contains |
|---|---|
| `AccountViews.cs` | `LoginViewModel`, `EditUserViewModel`, `UserIndexViewModel` |
| `DailyAssignmentShiftViews.cs` | Call sheet views with shift/pool selectors |
| `DailyCrewPositionViews.cs` | Position detail with on-duty/vacancy info |
| `EmployeeDetailViews.cs` | Employee self-service dashboard |
| `MarkOffRecordViews.cs` | Mark-off creation/edit with code selectors |
| `PayrollViews.cs` | Payroll record views with earning details |
| `SeniorityViews.cs` | Seniority management with roster selectors |

## Views (Razor)

Views are in `StrategicApplications\Views\{ControllerName}\`. Standard MVC structure:

```
Views\
??? Shared\
?   ??? _Layout.cshtml          � master layout
?   ??? _LoginPartial.cshtml    � login status bar
?   ??? Error.cshtml            � error page
??? Home\
?   ??? Index.cshtml
??? Account\
?   ??? Login.cshtml
?   ??? ...
??? DailyAssignmentShift\
?   ??? Index.cshtml            � call sheet list
?   ??? Details.cshtml          � shift detail
?   ??? ...
??? (113 controller folders)
```

### Layout & Styling

- Bootstrap 3.x for responsive layout
- jQuery for client-side behavior
- `BundleConfig.cs` manages JS/CSS bundles
- Custom CSS in `Content\` directory
# Part 36: Migrations Overview

## Overview

The solution has two sets of EF Code-First migrations: one for the web app and one for SAClassLibrary.

## Web App Migrations

Located in `StrategicApplications\Migrations\`. Hundreds of migration files spanning 2016-2022+. Each migration has a `.cs` file and a `.Designer.cs` file.

### Naming Convention

`{Timestamp}_{Description}.cs` � e.g., `201603221949381_ChangeAssignmentType_AddExtraBoardOnly.cs`

### Configuration

`Configuration.cs` with `AutomaticMigrationsEnabled = false`.

## SAClassLibrary Migrations

Located in `SAClassLibrary\Migrations\`. Only 5 migrations (synced from production):

| Migration | Date | Change |
|---|---|---|
| `Sync_Database1` | 2020-06-20 | Initial schema sync |
| `Sync_Database2` | 2022-01-11 | Schema update |
| `AddUkGInterface` | 2022-03-04 | UKG table |
| `ChangePayrollRecord_AddRatePercentage` | 2022-04-18 | Rate percentage |
| `ChangeBeSafeRecord_AddRecordNumber` | 2022-08-22 | Record number |

## Key Notes

- Web app owns the schema � SAClassLibrary migrations are catch-up syncs
- `SAClassLibraryContext` uses `Database.SetInitializer(null)` � no auto-migration
- Both contexts point to the same database
# Part 37: Daily Report System

## Overview

The `DailyReportTimers` in Global.asax generate daily operational reports per pool. Managed by `DailyReportController`.

## Timer Flow

1. Per-pool timer fires at configured time
2. Generates report covering the shift's assignments, vacancies, mark-offs, and extra board status
3. Report data comes from `DailyAssignmentShift` and its children

## DailyReportController

Provides views for:
- Shift-level summary (assignments, vacancies, extra board)
- Employee attendance (on-duty, marked off, off-day)
- Position fill status

## PrintPDFController

Uses **iText** library to generate PDF versions of daily reports. Landscape layout with `StandardFonts.COURIER`.
# Part 38: Interface Files & MSMQ

## Overview

The system communicates between components via file-based messages and MSMQ (Microsoft Message Queuing).

## File-Based Interface

Mark-off records, bulletins, and position changes generate interface files written to the inbound message queue directory.

### File Extensions

| Extension | Content | Handler |
|---|---|---|
| `.hr` | Holiday records | `TriggerHolidayRecordWatcherEvent` |
| `.uv` | Vacancy updates | `TriggerVacancyUpdateWatcherEvent` |
| `.esr` | Employee status records | `TriggerStatusUpdateWatcherEvent` |

### Flow

```
Entity method calls CreateInterfaceFile()
  ? Writes file to \\sql-svr\SA\Message Queue\Inbound
  ? FileSystemWatcher detects new file
  ? Handler processes file
  ? File moved to \Processed or \Processing Error
```

## MSMQ (SADailyCallSheetService)

The call sheet service uses MSMQ for inter-service communication.

### Queues (Production)

| Queue | Purpose |
|---|---|
| `dailyassignmentshift` | Shift creation messages |
| `dailyassignment` | Assignment creation messages |
| `dailycrewposition` | Crew position creation messages |
| `dailyondutyrecord` | On-duty record messages |
| `dailymarkoffrecord` | Mark-off record messages |

All queues on `SQL-SVR\private$\`. Dev queues on `PTRA-IT-LT-10\private$\`.

## ADPInterface / UKGInterface

Database tables for payroll export/import tracking:
- `ADPInterface` � ADP payroll records with paid amounts
- `UKGInterface` � UKG payroll records with paid amounts

Managed by `ADPInterfaceController` and `UKGInterfaceController`.
# Gap Analysis & Hard-Coded Logic Addendum

## Overview

This document captures hard-coded business logic, magic numbers, and undocumented behavior found during a full codebase review that was missing or incomplete in Parts 1-40.

---

## GAP 1: Pool Number Identity Map (Missing from Part 3a)

Throughout the codebase, pool-specific logic is driven by hard-coded `PoolNumber` comparisons. These are the known pools:

| PoolNumber | Name | Key Behaviors |
|---|---|---|
| `10` | Yard and Enginemen | Engineer/Yardman crafts, locomotive weight pay, protected/semi-protected status, vacation week start from Jan 1 |
| `20` | Yardmaster | 40-hour OT conversion for XB employees |
| `30` | Clerical | 40-hour OT conversion for XB employees, mark-up at `midnight + 1 min`, bulletin assigns at CloseDateTime |
| `40` | Mechanical | Overtime board management, bulletin assigns at EffectiveDateTime, job code format `{CrewNumber}{PositionCode}` |
| `50` | Maintenance of Way | Job code format `{CrewName}` |
| `60` | Patrolmen | Same job code logic as Clerical (pools 30 and 60 share `case` blocks) |

---

## GAP 2: Protected/Semi-Protected Status (Missing from Part 3f)

Hard-coded employment date thresholds in `RailroadPoolEmployee`:

### `IsProtected`
```
Pool 10 (Yard & Enginemen) only:
  Employee.EmploymentDate < January 1, 1981 ? protected
```
Protected employees have different labor agreement rules.

### `IsSemiProtected`
```
CraftName contains "Yardman" only:
  Employee.EmploymentDate < January 1, 1991 ? semi-protected
```

### `IsHelperOnly`
```
Pool 10 only:
  Check if employee has "Foreman" qualification on active roster
  If NO Foreman qualification ? IsHelperOnly = true
```
Opens its own DbContext to query positions.

---

## GAP 3: DefaultJobWorked / DefaultJobPaid (Missing from Part 3f)

Complex pool-specific job code generation in `RailroadPoolEmployee`:

### DefaultJobWorked by Pool

| Pool | Craft | Position | Code |
|---|---|---|---|
| 10 | Engineer | Any | `"100D"` |
| 10 | Yardman | Roster Board | `"100H"` |
| 10 | Yardman | Foreman | `"101F"` |
| 10 | Yardman | Other | `"101H"` |
| 30, 60 | Any | Board/unassigned | `CraftPayCodes.PaidDayWorkedCode` |
| 30, 60 | Any | Crew position | `"{PositionCode}{CrewNumber}"` |
| 40 | Any | Board/unassigned | `CraftPayCodes.PaidDayWorkedCode` |
| 40 | Any | Crew position | `"{CrewNumber}{PositionCode}"` |
| 50 | Any | Board/unassigned | `CraftPayCodes.PaidDayWorkedCode` |
| 50 | Any | Crew position | `"{CrewName}"` |
| Default | Any | Any | `CraftPayCodes.PaidDayWorkedCode` |

### DefaultJobPaid by Pool

| Pool | Craft | Position | Code |
|---|---|---|---|
| 10 | Engineer | Trainee | `"30H1"` |
| 10 | Engineer | Regular | `"10H1"` |
| 10 | Yardman | Board | `"100H"` |
| 10 | Yardman | Crew | `"{PayrollCode}{PositionCode}"` |
| 30, 60 | Any | Board/unassigned | `CraftPayCodes.PaidDayPaidCode` |
| 30, 60 | Any | Crew | `"{PositionCode}{PayrollCode}"` |
| 40 | Any | Board/unassigned | `CraftPayCodes.PaidDayWorkedCode` |
| 40 | Any | Crew | `"{PayrollCode}{PositionCode}"` |
| 50 | Any | Board/unassigned | `CraftPayCodes.PaidDayWorkedCode` |
| 50 | Any | Crew | `"{PositionCode}{PayrollCode}"` |
| Default | Any | Board/unassigned | `CraftPayCodes.PaidDayPaidCode` |
| Default | Any | Crew | `"{PayrollCode}{PositionCode}"` |

**Note**: Pool 40 (Mechanical) reverses the order of `CrewNumber`/`PositionCode` compared to other pools.

---

## GAP 4: FRARequirements Static Constants (Incomplete in Part 7)

All values are hard-coded static properties:

| Property | Value | Description |
|---|---|---|
| `MaxHours` | `12` | Maximum hours on duty |
| `RestHours` | `10` | Minimum rest hours after duty |
| `ConsecutiveDays` | `6` | Maximum consecutive work days |
| `ConsecutiveDayHours` | `24` | Required rest after consecutive day limit |

### `GetRestTime(onduty, offduty)` � Dynamic Rest Calculation
```
resttime = 10 hours
timeonduty = offduty - onduty (UTC)
if timeonduty > 12 hours:
  resttime += (timeonduty.Hours - 12)
return resttime
```
i.e., for every hour over 12 worked, add an extra hour of rest.

### `CheckFRARestCompliance(db, record, user)`
```
if consecutiveDays < 6:
  Check rest for next on-duty record
else:
  Auto mark-off with code "SR" (Safety Rest)
  Send Teams "SystemMessage"
  Write event log
```

### `CheckRestForNextOnDuty(db, rpemployee, lastrecord, nextrecord, user)`
```
if lastrecord.RestedDateTime > nextrecord.OnDutyDateTime:
  if unconfirmed notification exists within 2 days:
    use EndCallTime instead of OnDutyDateTime
  Auto mark-off with code "NR" (Not Rested)
  Send Teams "SystemMessage"
  Write event log
```

---

## GAP 5: Mechanical Overtime Board Logic (Missing entirely)

`RailroadPoolEmployee.MechanicalMoveOrRemoveFromOvertimeBoard()` � Pool 40 only:

```
if markoff code IsCompensated AND RecordHours:
  MechanicalMoveToBottomOvertimeBoard("MO", markoffDateTime)
else:
  if Employee.CallForOvertime:
    Set CallForOvertime = false
    RemoveFromDailyShiftOvertimeBoard()
```

### `MechanicalMoveToBottomOvertimeBoard(postype, movedatetime)`
```
if postype == "MO":
  boardorder starts at 9000
  BoardDateTime = movedatetime.AddYears(10)  ? pushes to far back
else:
  boardorder starts at 5000
  BoardDateTime = movedatetime
```

**Note**: Comment in code says `"RecordHours is an unused field that needs to be renamed"` � field is repurposed.

---

## GAP 6: CraftPayCodes Entity (Missing entirely)

**PK = FK** to `Craft` (1:1). Defines per-craft default job codes.

| Property | Type | Description |
|---|---|---|
| `CraftControlNumber` | `long` | PK/FK |
| `PaidDayWorkedCode` | `string(4)` | Default job-worked code |
| `PaidDayPaidCode` | `string(4)` | Default job-paid code |
| `VacationDayWorkedCode` | `string(4)` | Vacation day job-worked |
| `VacationDayPaidCode` | `string(4)` | Vacation day job-paid |
| `PersonalDayWorkedCode` | `string(4)` | Personal day job-worked |
| `PersonalDayPaidCode` | `string(4)` | Personal day job-paid |
| `GuaranteePaidCode` | `string(4)` | Guarantee pay code |

Used extensively by `DefaultJobWorked`/`DefaultJobPaid` as fallback codes.

---

## GAP 7: Craft Properties (Incomplete in Part 3b)

Missing stored properties on `Craft`:

| Property | Type | Description |
|---|---|---|
| `MarkOffHours` | `int` | Hours before mark-off takes effect |
| `MarkUpHours` | `int` | Hours after mark-up before employee can work |
| `RequiredRestHours` | `int` | Minimum rest hours (craft-level, separate from FRA) |
| `MaximumVacationDayTime` | `int` | Maximum vacation day time (hours) |
| `UnpaidMealPeriodMinutes` | `int` | Unpaid meal period deducted from hours |
| `HoursofService` | `bool` | Whether FRA Hours of Service rules apply |
| `ProcessPayroll` | `bool` | Whether payroll is auto-processed for this craft |
| `ShowNotifications` | `bool` | Whether to show change notifications |
| `VacationAssignmentType` | `int` | How vacation is assigned (seniority, FIFO, etc.) |

### Craft Methods (Missing)

- `GetVacationDays(years)` ? looks up `CraftVacationDays` by service years (descending, first match ? years)
- `GetPersonalDays(years)` ? same pattern with `CraftPersonalDays`
- `GetSickDays(years)` ? same pattern with `CraftSickDays`
- `SetApprovalRequiredFlag(db, approval, user, now)` ? toggles `ApproveAllMarkOffs`

---

## GAP 8: RailroadPool Properties (Incomplete in Part 3a)

Missing stored properties on `RailroadPool`:

| Property | Type | Default | Description |
|---|---|---|---|
| `AllowBulletins` | `bool` | `true` | Enable bulletin system |
| `AllowSeniorityMoves` | `bool` | `true` | Enable seniority moves |
| `AllowHoldDowns` | `bool` | � | Enable hold-downs |
| `AllowTemporaryAssignments` | `bool` | `false` | Enable temp assignments |
| `AutoBulletins` | `bool` | � | Auto-process bulletins |
| `AutoMoves` | `bool` | � | Auto-process seniority moves |
| `AutoHangouts` | `bool` | � | Auto-process hangouts |
| `AutoCallSheets` | `bool` | � | Auto-create call sheets |
| `AutoVacancyAssignments` | `bool` | � | Auto-fill vacancies |
| `ElectronicCrewCalling` | `bool` | � | Enable electronic calling |

### Pool Dashboard Counts (computed, each opens own DbContext via CollectionLists):
- `BulletinCount`, `SeniorityMoveCount`, `HoldDownCount`
- `NotificationCount`, `MarkOffRecordCount`, `UnassignedEmployeeCount`

---

## GAP 9: Position Resolution Chain (Missing from Part 3f)

`RailroadPoolEmployee` has **6 different position properties** with distinct resolution logic:

| Property | Logic |
|---|---|
| `AssignedPosition` | `RailroadPoolEmployeePositions.First().RailroadPosition` ? fallback to `RailroadEmployee.AssignedPosition` |
| `CurrentPosition` | Check open `HoldDowns` first (most recent open hold-down's position) ? fallback to `AssignedPosition` |
| `LastActivePosition` | `CurrentPosition` ? fallback to most recent `RailroadPositionHistory` |
| `LastAssignedPosition` | `AssignedPosition` ? fallback to most recent crew position in history |
| `LastPosition` | Most recent `RailroadPositionHistory` ? fallback to most recent `DailyRailroadEmployeePositionRecord` |
| `PayrollDepartment` | `CurrentPosition.PayrollDepartment` ? `RailroadEmployee.AssignedPosition` ? `LastPosition` (crew?Position dept, board?Roster dept) |

---

## GAP 10: SeniorityDate_Rank Encoding (Missing from Part 3f)

```csharp
SeniorityDate_Rank = ActiveSeniority.RosterDate.AddSeconds(Rank)
```
This encodes rank INTO the datetime for sorting � employees with same seniority date are ordered by rank via fractional seconds.

---

## GAP 11: Hard-Coded Role GUIDs (Missing from Part 23)

| GUID | Role |
|---|---|
| `1d78b8ea-f36b-42a7-91fa-325f543aa2e9` | Railroad Auditor (used in approval officer queries) |

Used directly in LINQ queries in `CollectionLists.cs` and `Collections.cs`.

---

## GAP 12: Debug Credentials (Missing from Part 23)

In `AccountController.Login()`:
```csharp
#if DEBUG
    viewModel.UserName = "1074";
    viewModel.Password = "10Dr0wss@p74";
#endif
```

---

## GAP 13: 40-Hour OT Conversion Details (Incomplete in Part 16)

Only applies to Pools 20 (Yardmaster) and 30 (Clerical):
```
if Pool is 20 or 30
  AND employee IsExtraBoard
  AND payroll code is "01" or "42":
    maxhours = 40
    sthours = GetStraightTimeHoursThisWeek()
    if (STHours + sthours) > 40:
      excess converts from ST to OT
```

---

## GAP 14: Payroll Code "20" Double Time (Incomplete in Part 16)

```
if payrollCode == "20":
  STAmount = STRate � 2 � OTHours  ? NOT OTRate, uses doubled STRate
  OTAmount = 0                     ? recorded in STAmount despite being overtime
else:
  OTAmount = OTRate � OTHours
```

---

## GAP 15: RailroadPoolMarkOffAllowance (Missing entirely)

Tracks mark-off quotas per pool per year.

| Property | Type | Description |
|---|---|---|
| `RailroadPoolControlNumber` | `long` | FK |
| `Year` | `int` | Calendar year |
| `TotalNumber` | `double` | Total mark-offs available |
| `CalculatedNumber` | `double` | System-calculated value |
| `NumberAllowed` | `int` | Maximum allowed |
| `AllowanceType` | `string` | Type of allowance |

---

## GAP 16: Vacation Week Start Date � Pool 10 Special Logic (Incomplete in Part 14)

In `CreateMarkOffRecord()` when finding matching vacation requests:
```csharp
if pool.PoolNumber == 10:
  firstday = DayOfWeek of January 1 of modate.Year
  days = firstday - modate.DayOfWeek
  rdate = modate.AddDays(days).Date  ? aligns to Jan 1 week-start
```
Pool 10 vacation weeks start on the same day of week as January 1. Other pools use the mark-off date directly.

---

## GAP 17: Mark-Up Timing by Position Type (Incomplete in Part 14)

When auto-creating mark-up from a day-code request:
```
if employee IsExtraBoard:
  markupDateTime = request.MarkOffDateTime.AddDays(1)
else:
  markupDateTime = MarkOffDateTime.AddDays(1).Date.AddMinutes(1)  ? midnight + 1 min
```
Except Pool 30 (Clerical) � no auto mark-up created at all.

---

## GAP 18: TieUpOrder Magic Numbers (Scattered across Parts 14, 19)

| Context | TieUpOrder Value | Meaning |
|---|---|---|
| New XB position | `0` | Not yet ordered |
| After mark-off | `modate.AddYears(10).ToString("yyyyMMddHHmm")` | Pushed to far back |
| After tie-up | Encoded off-duty datetime | FIFO ordering |
| Mark-off deleted | Restored from `DailyExtraBoardMarkOffRecord` | Original order |

---

## GAP 19: CallForOvertime Flag (Missing entirely)

`Employee.CallForOvertime` (bool) � when set, employee appears on overtime board. Mechanical pool mark-off clears this flag and removes employee from OT board.

---

## GAP 20: RailroadPoolRequirement Pool-Specific Logic (Missing entirely)

Contains hard-coded pool number checks for requirement processing. Different pools have different requirement validation rules.

---

## GAP 21: Interfaces (Missing entirely)

| Interface | Implementors | Description |
|---|---|---|
| `IControlNumber` | `ControlNumberBase` | Defines `ControlNumber` property |
| `IAutoMarkUp` | `RosterBoardPosition`, `CrewPosition` | `GetAutomaticMarkUpDateTime(MarkOffRecord)` � position-type-specific auto mark-up calculation |
| `IAvailableEmployeeRepository` | Unknown/unused? | `GetExtraBoardEmployees()`, `GetAvailableOffDayEmployeesInSeniorityOrder()`, `GetAvailableEmployeesInSeniorityOrder()` |
| `ICacheProvider` | Unknown/unused? | `Get()`, `Set()`, `IsSet()`, `Invalidate()` � caching interface (not clearly implemented) |

---

## GAP 22: Structs (Missing entirely)

| Struct | Properties | Usage |
|---|---|---|
| `QualifyRecord` | `QualifyDate`, `Qualify`, `Code` | Used in holiday qualification logic |
| `StartEndPeriod` | `StartDate`, `EndDate`, `Period` | Used in payroll period calculations |

---

## GAP 23: SelectLists Query Class (Missing entirely)

`StrategicApplications\Models\Queries\SelectLists.cs` � **~2,136 lines**, static class.

- **Static DbContext field**: `public static StrategicApplicationsContext db = new StrategicApplicationsContext();` � shared instance (potential concurrency issue)
- Returns `SelectList` objects for dropdown population
- Wraps `CollectionLists` methods or direct LINQ queries
- Used by controllers to populate view model dropdowns

---

## GAP 24: DateTimeUtilities Hard-Coded Logic (Incomplete in Part 11)

### `CreateDateFromString(datestr)`
Parses 6-digit strings as `MMddyy` ? prefixes with `"20"` for year (assumes 2000s).

### `CalculateYears(startDate)`
```csharp
now = float.Parse(DateTime.Now.ToString("yyyy.MMdd"))
dob = float.Parse(startDate.ToString("yyyy.MMdd"))
return (int)(now - dob)
```
Unconventional year calculation using float subtraction of formatted date strings.

### `GetDaysToAdd(current, desired)`
Comment: `"This code was changed from (nbr <= 0) 06/17/2016"` � documents a bug fix inline.

---

## GAP 25: Seniority Date 9999-12-31 Sentinel (Missing from Part 15)

```csharp
if (RosterDate.Equals(new DateTime(9999, 12, 31)))
    return 0;
```
Date `9999-12-31` is used as a sentinel for "no seniority date" / unranked.

---

## GAP 26: DailyShiftExtraBoardPosition Opens Own DbContext (Missing from Part 19)

Both `DailyCrewPositionControlNumber` and `IsEmployeeAssigned` computed properties open their own `StrategicApplicationsContext` to query completed boards when the current position has no assignment.

---

## GAP 27: Teams Message Types (Incomplete in Part 11)

Known message type strings used with `TeamsSendChatMessage()`:

| Type String | When Sent |
|---|---|
| `"SystemMessage"` | FRA violations, mark-off errors, system events |
| `"TieUpMessage"` | Employee tie-up notifications |
| `"VacancyMessage"` | Vacancy assignment notifications |

---

## GAP 28: Employee.CallForOvertime (Missing from Part 3e)

`Employee` has a `CallForOvertime` boolean property. When `true`, the employee appears on the overtime board. Cleared by Mechanical pool mark-off processing (Gap 5).

---

## GAP 29: RailroadEmployee Delegated Properties (Missing from Part 3e)

Properties on `RailroadEmployee` not captured:
- `TieUpOffProperty` (bool) � affects off-property tie-up processing
- `ProcessPayroll` (bool) � whether payroll is generated
- `LastActiveCraft` � most recent craft from seniority history
- `ActiveSeniority` � first active seniority record across all pools
- `AssignedPosition` � position from any pool employee assignment

---

## GAP 30: Position Resolution Chain (Missing from Part 3f)

`RailroadPoolEmployee` has **6 different position properties**:

| Property | Logic |
|---|---|
| `AssignedPosition` | `RailroadPoolEmployeePositions.First()` ? fallback `RailroadEmployee.AssignedPosition` |
| `CurrentPosition` | Open `HoldDowns` first ? fallback `AssignedPosition` |
| `LastActivePosition` | `CurrentPosition` ? fallback latest `RailroadPositionHistory` |
| `LastAssignedPosition` | `AssignedPosition` ? fallback latest crew position in history |
| `LastPosition` | Latest `RailroadPositionHistory` ? fallback latest `DailyRailroadEmployeePositionRecord` |
| `PayrollDepartment` | Cascades: `CurrentPosition` ? `RailroadEmployee.AssignedPosition` ? `LastPosition` |

---

## Summary of Critical Hard-Coded Logic

### Must-Capture for Rewrite (Federal/Labor Compliance)
1. **FRA constants** � 12h max, 10h rest, 6 consecutive days, 24h consecutive rest
2. **Protected status dates** � `EmploymentDate < 1981-01-01` (Pool 10) and `< 1991-01-01` (Yardman)
3. **Auto mark-off codes** � `"SR"` (Safety Rest), `"NR"` (Not Rested), `"CR"` (Called Relief), `"MC"` (Missed Call)

### Must-Capture for Payroll Accuracy
4. **Pool Number switch** � 10/20/30/40/50/60 drive job code format, OT rules, bulletin timing
5. **DefaultJobWorked/DefaultJobPaid** � pool � craft � position matrix
6. **Payroll code rates** � "04","06","12" ? compensated rate; "13" ? guarantee; "20" ? double time
7. **40-hour OT conversion** � pools 20/30 only, codes "01"/"42" only, XB only

### Must-Capture for Board/Assignment Accuracy
8. **TieUpOrder encoding** � `AddYears(10)` pushback, datetime encoding for FIFO
9. **Mechanical OT board orders** � 5000 (normal), 9000 (mark-off)
10. **Seniority date encoding** � `AddSeconds(Rank)` for sort tiebreaking
11. **Vacancy fill order** � `SeniorityDate_Rank` ordering, holddown priority

### Notable Code Smells to Address in Rewrite
12. Static DbContext in `SelectLists`
13. Computed properties opening own DbContext instances
14. Float-based year calculation
15. Debug credentials in source
16. Repurposed `RecordHours` field (comment says "needs to be renamed")
17. Busy-wait spin lock (`Thread.Sleep(1000)`) for board processing
18. `30.42` hard-coded month-to-day conversion
19. Multiple mark-off code strings embedded in logic (`"CY"`, `"YT"`, `"SR"`, `"NR"`, `"MC"`, `"CR"`, `"FL"`)

---

## Revised Total: 45 Gaps Identified

---

## GAP 31: Holiday Qualification Decision Tree (Incomplete in Part 20)

`GetQualifyRecord()` is ~400 lines of nested branching. Returns a `QualifyRecord` struct with `QualifyDate`, `Qualify` (bool), `Code` (string).

### Hard-Coded Qualification Codes

| Code | Meaning | Qualifies? |
|---|---|---|
| `"HLDY"` | Qualify date is itself a holiday | Yes |
| `"OFF"` | Crew position off-day (increment=0 only) | Yes |
| `"A"` | Available on extra board, no on-duty record | Yes |
| `"EA"` | Extended absence board | No |
| `"TR"` | On training roster board, no mark-off | No |
| `"NA"` | Non-active employment status | No |
| `"ER"` | Error/no matching record | No |
| `"NULL"` | No status records exist for date | Skipped |
| `"{JobCode}"` | Worked the qualifying day | Yes |
| `"{JobCode}(A)"` | Position was annulled | Yes |
| `"{JobCode}(U)"` | Unavailable record exists | Yes |
| `"{JobCode}(S)"` | Did-not-work record (sub) | Yes |
| `"{MOCode}"` | Marked off | Depends on code flags |

### Mark-Off Code Holiday Flags

| Flag | Behavior |
|---|---|
| `HolidayQualify = true` | Mark-off qualifies for holiday | 
| `HolidayExempt = true` | Skip this day, check next (recursive) |
| Neither | Does not qualify |

### Recursive Logic

When `HolidayExempt` is true and `increment != 0` (PRE or POST):
- Recursively calls `GetQualifyRecord(qualdate + increment, increment)`
- Walks forward (POST) or backward (PRE) through days until finding a qualifying or disqualifying day
- Stops recursion if the next date is itself a holiday ? returns `"HLDY"` qualified

### Position Type Priority

1. **Hangout** ? check on-duty, then mark-off, then hangout record
2. **Crew position** ? check on-duty, then off-day, then did-not-work
3. **Roster board (XB)** ? check on-duty, then mark-off
4. **Roster board (other)** ? check mark-off, default `"TR"`
5. **Extended absence** ? always `"EA"` (not qualified)

---

## GAP 32: UpdateSeniorityStatus State Machine (Missing from Part 3f)

`RailroadPoolEmployee.UpdateSeniorityStatus()` handles employment status transitions:

### Status Code: `"AT"` (Active)
1. Send AtHoc create message
2. If 0 seniority records ? return `"Create"` (needs seniority setup)
3. If 1 seniority record ? activate it (`StateID = 1`), assign roster board position, send craft message ? `"Complete"`
4. If >1 seniority records ? return `"Select"` (user must choose)

### Status Code: `"NA"` (Not Active / Leave)
1. Unassign positions from active roster
2. Remove unassigned seniority moves
3. Complete open mark-off records (mark-up or delete)
4. Complete open notifications
5. Remove open on-duty records
6. Remove future training dates
7. Set all active seniority to `StateID = 0`
8. Send AtHoc delete message ? `"Complete"`

### Status Code: `"XE"` (Terminated)
All of `"NA"` plus:
1. Unassign positions from ALL rosters (not just active)
2. Create `SeniorityEndDate` records
3. Delete ALL qualifications (`db.Qualifications.RemoveRange(...)`)
? `"Complete"`

### Post-Processing (all codes)
- Create `EmploymentStatusHistory` record
- If pre-dated: remove conflicting daily status records in range
- If not `"AT"`: create daily status records from status date through today

---

## GAP 33: GetJobCode / GetPayCode Pool Switch (Incomplete in Part 3f)

These are SEPARATE from `DefaultJobWorked`/`DefaultJobPaid` � they resolve the actual job/pay code for a specific date using on-duty and position records.

### `GetJobCode(db, date)` � Job Code by Pool (when on-duty record missing or XB)

| Pool | Format |
|---|---|
| 10 (Y&E), 20 (YM), 40 (Mech) | `"{AssignmentNumber}{PositionCode}"` |
| 30 (Clerical), 60 (Patrol) | `"{PositionCode}{AssignmentNumber}"` |
| 50 (MOW) | `"{AssignmentName}"` |

If on-duty record exists and not XB ? uses `ondutyrec.JobCode`.

### `GetPayCode(db, date)` � Pay Code by Pool

| Pool | Format |
|---|---|
| 10 (Y&E) Engineer | `"10H1"` (hard-coded) |
| 10 (Y&E) XB/Hangout | `"100{PositionCode}"` |
| 10 (Y&E) Other | `"101{PositionCode}"` |
| 20 (YM), 40 (Mech) | `"{PayrollCode}{PositionCode}"` |
| 30 (Clerical), 50 (MOW), 60 | `"{PositionCode}{PayrollCode}"` |

### `GetStraightTimeHours(date)` � Default 8 hours
Falls back to `AssignmentOnDutyDay.StraightTimeHours` from crew assignment.

---

## GAP 34: Yardmaster Cross-Pool Mark-Off (Missing entirely)

When a Yardmaster employee fills a vacancy in a different pool:

### `CreateYardmasterMarkoffRecord(db, position, onduty)`
- If vacancy pool ? employee's active pool:
  - Find employee in active pool
  - Create mark-off with code `"CY"` (Called for Yardmaster duty)
  - If position is on a training roster ? use code `"YT"` (Yardmaster Training) instead

### `CreateYardmasterMarkupRecord(db, offduty, markoff, user)`
- Marks up the cross-pool mark-off when employee ties up
- If employee's craft has `HoursofService` ? checks FRA rest compliance

---

## GAP 35: Payroll Tier Day Calculation (Missing from Part 35)

`RailroadPoolPayrollTier.TypeOfDay` determines how days are counted:

| TypeOfDay | Method | Description |
|---|---|---|
| `1` | Calendar Days | `date - EmploymentDate` |
| `2` | Working Days | Count of on-duty records + prior service credit |

### Prior Service Credit Conversion
```
days += ServiceYears � 365
days += 30.42 � ServiceMonths  (hard-coded month-to-day)
days += ServiceDays
```

### `UpdatePayrollTierRate(db, ondutydate)`
Called during on-duty processing. Finds the matching tier where `NumberOfDays <= employeeDays` (descending), updates if changed.

---

## GAP 36: Overtime Board Exclusion Rule (Missing from Part 39)

```csharp
// In AddToDailyShiftOvertimeBoard():
if (this.RailroadPool.PoolNumber.Equals(20) && this.IsExtraBoard)
    return;  // Yardmaster XB employees excluded from OT board
```

Also: `while (MvcApplication.BoardProcessing) Thread.Sleep(1000)` � busy-wait spin lock before board operations.

### OT Board Position Types
| `PostionType` | Meaning |
|---|---|
| `"OT"` | Normal overtime position |
| `"MO"` | Marked-off (pushed to back) |

---

## GAP 37: Bulletin No-Access Logic (Missing from Part 15)

`HadNoAccess(bulletin)` determines if employee couldn't bid due to work schedule:

1. If on-duty or marked off ? **not** no-access (had opportunity)
2. If was last employee on that position ? **not** no-access
3. If no last on-duty record ? **is** no-access
4. If on hangout board ? **not** no-access
5. If on crew position:
   - If off-duty AFTER bulletin open or next on-duty BEFORE bulletin open ? not no-access
   - Otherwise: check `RailroadPoolEmployeeBulletinsViewedRecords` (via `SAClassLibraryContext`) � last 100 view records, any during bulletin window ? not no-access
6. Default ? **is** no-access

This determines force-assignment eligibility for no-bid bulletins.

---

## GAP 38: Seniority Comparison (Missing from Part 15)

`HasSeniority(otherEmployee)`:
```
Compare RosterDate: earlier date = more senior
If same RosterDate: lower Rank = more senior
```

`EmployeeCanBump(position)`:
```
if no current position ? can bump
if current position has no CrewPosition AND board doesn't bulletin ? only bump to crew (not board)
if CanBump AND IsQualified:
  lastWorkTime > BumpDate
  AND lastWorkTime - Now > SeniorityMoveRule.RequestHours
```

### Bump Date Calculation
`RailroadPoolEmployeePosition.GetBumpDate()` � date after which employee can exercise seniority move rights.

### Bid Eligibility
`CanBid(position)`:
- Must not have already bid (`HasNotBid`)
- XB employee cannot bid on XB position
- Cannot bid on position they were last assigned to

---

## GAP 39: Roster Board Assignment Logic (Missing from Part 21)

`GetRosterBoardPosition(db, roster)`:
- If `Employee.IsOnExtendedAbsence` ? find first open position on `ExtendedAbsence` board
- Else ? find first open position on `AutoAssign` (hangout) board
- Throws exception if board or positions not defined

`MoveToRosterBoard(db, user, adate)`:
- Skips if `Employee.IsOutOfService`
- Gets active roster ? finds board position ? `ManualAssign()`

`AssignToExtraBoard(db, roster, user, assigndatetime)`:
- Finds first open XB position (excluding those with pending seniority moves)
- Unassigns current positions first ? then assigns XB position

### TieUpOrder Encoding on Assignment/Unassignment
```
// When unassigned from crew and returning to XB:
tieuporder = Convert.ToInt64(now.ToString("yyyyMMddHHmm"))

// When assigned to crew from XB:
tieuporder = Convert.ToInt64(assigndate.AddYears(10).ToString("yyyyMMddHHmm"))
```

---

## GAP 40: Weekly ST Hours for 40-Hour OT (Missing from Part 16)

`GetStraightTimeHoursThisWeek(db)`:
- Week = Monday through Sunday (`StartOfWeek(DayOfWeek.Monday)`)
- Sums `STHours` from `PayrollEarningRecords` where `PayrollCode == "01" || "42"`
- Used by `CalculatePayrollAmounts()` for 40-hour OT conversion (Pools 20/30)

---

## GAP 41: Cleanup Methods on Status Change (Missing from Part 3f)

When employee status changes, these cleanup methods run:

| Method | Action |
|---|---|
| `CompleteOpenMarkOffRecords(db, user, date)` | Future MOs deleted; past MOs marked up at status date |
| `CompleteOpenNotifications(db, user, date)` | Auto-confirms all open notifications |
| `RemoveOpenDailyCrewPositionOnDutyRecords(db, date)` | Removes latest open on-duty if on/after date |
| `RemoveTrainingRecords(db, date)` | Removes future training dates |
| `RemoveUnassignedSeniorityMoves(db, user)` | Removes pending moves, sends cancel notifications |
| `RemoveUnassignedBulletinBids(db)` | Removes pending bulletin bids |
| `UnassignRailroadPositions(db, user, date, roster)` | Unassigns all positions, releases hold-downs, creates bulletins |
| `InactivateSeniority(db, user, activeseniority)` | Deactivates all seniority except specified one |

---

## GAP 42: Roster.OvertimeBoard Flag (Missing from Part 15)

`Roster.OvertimeBoard` (bool) � when `true`, the roster participates in overtime board processing. Checked by `GetOpenDailyOvertimeBoard()`.

---

## GAP 43: Employee.IsOnExtendedAbsence / IsOutOfService (Missing from Part 3e)

These flags on `Employee` control board assignment routing:
- `IsOnExtendedAbsence` ? routes to Extended Absence board
- `IsOutOfService` ? skips board assignment entirely

---

## GAP 44: MarkOffRecord.CreateInterfaceFile() (Missing from Part 14)

After mark-off creation/update/deletion, creates a file-based interface message:
```csharp
markoffrecord.CreateInterfaceFile(db, "Update")  // or "Create", "Delete"
```
Writes to the inbound message queue directory (see Part 38).

---

## GAP 45: DailyCrewPositionOffDutyRecord.ReleaseReason (Missing from Part 5)

`ReleaseReason` string on off-duty record. Known values:
- `"CR"` � Called Relief (employee was relieved mid-duty)
- `null`/empty � normal tie-up

Used in `GetOpenDailyCrewPositionOnDutyRecords()`: records with `ReleaseReason == "CR"` are still considered "open".
# Part 42: Gap Analysis Continued � MarkOff, RailroadPosition, MarkOffCode

Continued from Part 41. Additional hard-coded logic found in MarkOffRecord, RailroadPosition, and MarkOffCode.

---

## GAP 46: MarkOffCode.AutoMarkUpHours Hard-Coded Values (Missing from Part 14)

When `MarkOffMarkUpHours` is null, vacation codes use hard-coded hours:

| Code | Hours | Meaning |
|---|---|---|
| `"V1"` | 168 | 1 week (7 � 24) |
| `"V2"` | 336 | 2 weeks |
| `"V3"` | 504 | 3 weeks |
| `"V4"` | 672 | 4 weeks |
| `"V5"` | 840 | 5 weeks |
| `"CD"` | 24 | Compensated Day |
| `"PD"` | 24 | Personal Day |
| `"SD"` | 24 | Sick Day |
| `"VD"` | 24 | Vacation Day |
| All others | 0 | No auto mark-up |

When `MarkOffMarkUpHours` record exists ? uses `MarkUpHours` from that record.

---

## GAP 47: MarkOffCode Flags Complete List (Incomplete in Part 14)

| Property | Type | Description |
|---|---|---|
| `ClientControlNumber` | `long` | FK |
| `Code` | `string(2)` | Two-letter code |
| `ReportCode` | `string(1)` | Single-char report code |
| `Description` | `string(250)` | Display name |
| `Excused` | `bool` | Whether this is an excused absence |
| `RecordHours` | `bool` | Record hours (repurposed � see Gap 5) |
| `AllowRequest` | `bool` | Employees can request this code |
| `SystemUseOnly` | `bool` | Only system can create (not user-selectable) |
| `ApprovalRequired` | `bool` | Requires supervisor approval |
| `ApprovedByAgreement` | `bool` | Pre-approved by labor agreement |
| `HolidayExempt` | `bool` | Skip this day in holiday qualification (recursive) |
| `HolidayQualify` | `bool` | This mark-off qualifies for holiday pay |
| `ReportColor` | `string` | CSS color for reports (default `"Black"`) |

### Computed Properties

| Property | Logic |
|---|---|
| `IsCompensated` | `MarkOffPayrollCode != null` |
| `IsAutoMarkup` | `MarkOffMarkUpHours != null` |
| `IsVacationWeek` | `Code.StartsWith("V") && Code != "VD"` |

### Known System Mark-Off Codes

| Code | Description | SystemUseOnly |
|---|---|---|
| `"SR"` | Safety Rest (FRA consecutive days) | Yes |
| `"NR"` | Not Rested (FRA rest violation) | Yes |
| `"CR"` | Called Relief | Yes |
| `"MC"` | Missed Call | Yes |
| `"FL"` | FMLA | No |
| `"LD"` | Light Duty | No |
| `"CY"` | Called for Yardmaster | Yes |
| `"YT"` | Yardmaster Training | Yes |
| `"V1"` through `"V5"` | Vacation weeks | No |
| `"VD"` | Vacation Day | No |
| `"CD"` | Compensated Day | No |
| `"PD"` | Personal Day | No |
| `"SD"` | Sick Day | No |

---

## GAP 48: MarkOffRecord.CreateMarkOffRecord Full Flow (Incomplete in Part 14)

~250 lines. Full parameter list:

```
(db, rpectrlnbr, code, notes, user, modate, restrictmarkup,
 officer, requirepaperwork, vacationrelief, oncall, reqctrlnbr)
```

### Step-by-step:

1. **Set fields**: position, employee, code, datetime, comp hours
2. **Create daily records**:
   - XB/Hangout ? `CreateDailyRailroadEmployeePositionMarkOffRecord()`
   - Crew ? `UpdateDailyCrewPositionOnDutyMarkOffRecords()`
3. **Find matching request**: 
   - Vacation codes (V* not VD): Pool 10 aligns to Jan 1 week-start; checks �7 days
   - Other codes: match by date + code
4. **Auto-create request** for day-codes ending in "D" with payroll code:
   - Skip auto mark-up for Pool 30 (Clerical)
   - XB employees: mark-up = +1 day
   - Crew employees: mark-up = midnight +1 minute next day
5. **Link to request** ? create `MarkOffRequestMarkOffRecord`, apply mark-up from request
6. **Complete wait-list notifications** if request was from wait list
7. **Create approval** record if officer specified
8. **Debit compensation account** ? if balance ? 0, remove future requests/wait list entries
9. **Create interface file** ("Add")
10. **Auto mark-up** if `IsAutoMarkup` ? save original, mark up, update interface
11. **If NOT auto mark-up**: busy-wait for call sheet (`Thread.Sleep(1000)`), update XB mark-off record, set tie-up order to `modate.AddYears(10)`, set roster board mark-off timer
12. **Create off-duty record** if marked off within 2 hours of now and on-duty

---

## GAP 49: Mark-Off While On Duty (Missing from Part 14)

`CreateDailyCrewPositionOffDutyRecord()` � called after mark-off creation:

```
if markoff within 2 hours of now AND no mark-up:
  Find on-duty record within 1 day of mark-off
  if employee was working:
    if code == "CR" (Called Relief):
      Create off-duty with ReleaseReason = "CR"
    else:
      Create off-duty record
      Create payroll record
      Flag payroll for review: "Marked off while working"
    if on-duty not complete ? create ManualTieUpNotification
    Update vacancy for the pool
    Check shift completion
```

---

## GAP 50: RailroadPosition.PositionType and Polymorphism (Incomplete in Part 3d)

`RailroadPosition.PositionType` is `string(1)` � not documented what values mean.

### All computed properties branch on IsCrewPosition / IsRosterBoardPosition:

Every property on `RailroadPosition` follows this pattern:
```csharp
if (this.IsCrewPosition)
    return this.CrewPosition.xxx;
if (this.IsRosterBoardPosition)
    return this.RosterBoardPosition.xxx;
return default;
```

Affected properties: `RailroadName`, `RailroadPoolNumber`, `RailroadPoolName`, `RailroadPoolControlNumber`, `RosterControlNumber`, `CraftControlNumber`, `CraftName`, `Craft`, `RequiredRestHours`, `CalculateRest`, `AutoMarkUp`, `BulletinPosition`, `DefaultJobPaid`, `PayrollDepartment`, `SeniorityMoveRule`, and more.

### RailroadPosition Implements IAutoMarkUp

`GetAutomaticMarkUpDateTime(MarkOffRecord)` � delegates to `CrewPosition` or `RosterBoardPosition` based on type.

---

## GAP 51: RailroadPosition.DefaultJobPaid (Duplicate of RPE logic)

Hard-coded on `RailroadPosition` (separate from `RailroadPoolEmployee.DefaultJobPaid`):

| CraftName | Position | Code |
|---|---|---|
| `"Engineer"` + trainee | � | `"30H1"` |
| `"Engineer"` + regular | � | `"10H1"` |
| `"Yardman"` + board | � | `"100H"` |
| `"Yardman"` + `"Foreman"` | � | `"101F"` |
| `"Yardman"` + other | � | `"101H"` |
| All other crafts | � | `CraftPayCodes.PaidDayPaidCode` |

**Note**: This duplicates logic from `RailroadPoolEmployee.DefaultJobPaid` but only handles CraftName-based branching (no pool number switch).

---

## GAP 52: MarkOffRecord Stored Properties (Incomplete in Part 14)

| Property | Type | Description |
|---|---|---|
| `EmployeeControlNumber` | `long` | FK (denormalized) |
| `EmployeeNumber` | `string(4)` | Denormalized emp number |
| `RailroadPoolEmployeeControlNumber` | `long` | FK |
| `RailroadPositionControlNumber` | `long` | FK � position at time of mark-off |
| `MarkOffCodeControlNumber` | `long` | FK |
| `MOCode` | `string(2)` | Denormalized code |
| `MarkOffDateTime` | `DateTime` | When mark-off starts |
| `RestrictMarkUp` | `bool` | Prevent mark-up (requires paperwork) |
| `RequirePaperwork` | `bool` | Requires documentation |
| `ApprovalRequired` | `bool` | Needs supervisor approval |
| `CreatedFromTIES` | `bool` | Created from vacation relief system |
| `Notes` | `string` | Free text |
| `LaidOffOnCall` | `bool` | Employee was laid off but on-call |
| `CompHours` | `double` | Compensation hours deducted |

### Computed

| Property | Logic |
|---|---|
| `IsOpen` | Not deleted AND mark-off datetime in past AND not closed |
| `IsClosed` | Deleted OR (has mark-up AND mark-up in past) |
| `IsDeleted` | `MarkOffRecordDelete != null` |
| `IsLightDuty` | `Code == "LD"` |
| `IsVacationWeek` | Code is V1-V5 |
| `TimeOff` | `MarkUpDateTime - MarkOffDateTime` (or `Now - MarkOffDateTime`) |
| `CreatedByName` | Opens own DbContext to look up user full name |

---

## GAP 53: Roster Properties (Missing from Part 15)

| Property | Type | Description |
|---|---|---|
| `CraftControlNumber` | `long` | FK |
| `RailroadPayrollDepartmentControlNumber` | `long` | FK |
| `RosterName` | `string(250)` | Display name |
| `RosterPluralName` | `string(250)` | Plural form |
| `RosterNumber` | `int` | Ordering number |
| `Training` | `bool` | Whether this is a training roster |
| `ExtraBoard` | `bool` | Whether this roster has an extra board |
| `OvertimeBoard` | `bool` | Whether overtime board processing applies |

### Navigation

- `RosterBulletinRule` (1:1) � bulletin timing/rules
- `RosterSeniorityMoveRule` (1:1) � seniority move timing/rules
- `RailroadPayrollDepartment` � payroll department for this roster
- `Positions`, `RosterBoards`, `Seniority` collections

---

## GAP 54: MarkOffRecord.DeleteMarkOffRecord Flow (Missing from Part 14)

1. If XB/Hangout employee:
   - Find current XB position
   - Get saved mark-off record from `DailyExtraBoardMarkOffRecord`
   - `ResetTieUpOrder()` � restore original board order
   - Remove `DailyExtraBoardMarkOffRecord`
2. Delete daily mark-off records
3. Remove `MarkOffRequestMarkOffRecords` links
4. Create `MarkOffRecordDelete` (soft delete)
5. Set roster board mark-off timer
6. Update vacancy counts

---

## GAP 55: MarkOffRecord.ChangeMarkOffRecord (Missing from Part 14)

When code changes:
- If new code has no auto mark-up hours and old mark-up exists ? delete mark-up, send Teams notification
- If new code has auto mark-up ? recalculate mark-up datetime
- Recalculate `CompHours`
- Update daily mark-off records
- Reprocess XB mark-off/tie-up order if applicable
- Create interface file ("Update")

---

## GAP 56: Complete List of Known Hard-Coded Mark-Off Code Strings

Codes referenced directly in C# logic (not via DB lookup):

| Code | Where Used | Logic |
|---|---|---|
| `"SR"` | FRARequirements | Auto mark-off for consecutive days |
| `"NR"` | FRARequirements | Auto mark-off for not rested |
| `"CR"` | MarkOffRecord, DailyCrewPositionOffDutyRecord | Called Relief � special off-duty handling |
| `"MC"` | CraftMarkOffCodes query | Missed Call filter |
| `"FL"` | CollectionLists | FMLA filter (excluded unless fmla=true) |
| `"LD"` | MarkOffRecord.IsLightDuty | Light Duty check |
| `"CY"` | RailroadPoolEmployee | Called for Yardmaster |
| `"YT"` | RailroadPoolEmployee | Yardmaster Training |
| `"V1"`-`"V5"` | MarkOffCode, MarkOffRecord | Vacation weeks (1-5) |
| `"VD"` | MarkOffCode, MarkOffRecord | Vacation Day (single) |
| `"CD"` | MarkOffCode | Compensated Day |
| `"PD"` | MarkOffCode | Personal Day |
| `"SD"` | MarkOffCode | Sick Day |

---

## GAP 57: Thread Synchronization Patterns

Three busy-wait patterns found:

```csharp
// 1. Board processing lock
while (MvcApplication.BoardProcessing)
    Thread.Sleep(1000);

// 2. Call sheet in progress lock
while (MvcApplication.CallSheetInProgress[rpemployee.RailroadPoolControlNumber])
    Thread.Sleep(1000);

// 3. Vacancy processing (implied by static flags)
MvcApplication.SetRosterBoardMarkOffTimer(pool);
```

All use `Thread.Sleep(1000)` � no timeout, no cancellation, potential infinite loop.
# Part 44: Master Index & Complete Gap Summary

## All Documentation Parts

| Part | File | Topic |
|---|---|---|
| 01 | `Part01_SolutionArchitecture.md` | Solution structure, projects, dependencies |
| 02 | `Part02_PrimaryKeyGeneration.md` | ControlNumber snowflake PK generation |
| 03a | `Part03a_Entities_Organizational.md` | Client, Railroad, RailroadPool hierarchy |
| 03b | `Part03b_Entities_CraftConfiguration.md` | Craft config and approval officers |
| 03c | `Part03c_Entities_CrewAssignment.md` | Crew, Assignment, Shift, WeekDay |
| 03d | `Part03d_Entities_PositionRailroadPosition.md` | Position, RailroadPosition, RPEPosition |
| 03e | `Part03e_Entities_Employee.md` | Employee, RailroadEmployee, ApplicationUser |
| 03f | `Part03f_Entities_RailroadPoolEmployee.md` | RailroadPoolEmployee properties and delegation |
| 04 | `Part04_DailyCrewPosition.md` | DailyAssignmentShift ? DailyCrewPosition chain |
| 05 | `Part05_DailyCrewPositionOnDutyRecord.md` | On-duty lifecycle |
| 06 | `Part06_VacancyAssignment.md` | Vacancy detection and fill process |
| 07 | `Part07_FRACompliance.md` | Federal Railroad Administration rules |
| 08 | `Part08_PayrollApprovalRouting.md` | Payroll approval chain |
| 09 | `Part09_AtHocService.md` | Electronic calling and AtHoc integration |
| 10 | `Part10_WindowsServices.md` | 3 Windows Services |
| 11 | `Part11_UtilityClasses.md` | ApplicationUtilities, EventLogger, FileUtilities |
| 12 | `Part12_GlobalAsax.md` | Application_Start, 18+ timer categories |
| 13 | `Part13_ConfigurationDependencies.md` | Web.config, connection strings, appSettings |
| 14 | `Part14_MarkOffSystem.md` | Mark-off/mark-up lifecycle |
| 15 | `Part15_SeniorityBulletinSystem.md` | Seniority, bulletins, moves |
| 16 | `Part16_PayrollRecordProcessing.md` | Payroll generation, earning records, rate calc |
| 17 | `Part17_CollectionListsQueryLayer.md` | CollectionLists static query class |
| 18 | `Part18_SAClassLibrary.md` | Shared class library project |
| 19 | `Part19_ExtraBoardManagement.md` | Extra board FIFO/rotating logic |
| 20 | `Part20_HolidayProcessing.md` | Holiday qualification and pay |
| 21 | `Part21_RosterBoardHangout.md` | Roster boards, hangout, extended absence |
| 22 | `Part22_RequirementsQualifications.md` | Training requirements, position qualifications |
| 23 | `Part23_IdentityAuthentication.md` | ASP.NET Identity, OWIN, roles |
| 24 | `Part24_ControllersOverview.md` | 113 MVC controllers categorized |
| 25 | `Part25_BeSafeSlowOrder.md` | Safety observation and speed restriction systems |
| 26 | `Part26_RailroadInformation.md` | Information publishing and read-tracking |
| 27 | `Part27_TemporaryAssignmentsHoldDowns.md` | Temp assignments and hold-downs |
| 28 | `Part28_CompensationTimeAccounts.md` | Banked time (vacation, personal, sick) |
| 29 | `Part29_OnDutyBillingTieUp.md` | Billing records and tie-up process |
| 30 | `Part30_ViewsModelViews.md` | 103 view models, Razor views |
| 31 | `Part31_DailyStatusRecords.md` | Daily employee status and position snapshots |
| 32 | `Part32_ChangeNotificationSystem.md` | Position change notifications |
| 33 | `Part33_VacationRequests.md` | Vacation request and scheduling |
| 34 | `Part34_EngineerSpecificLogic.md` | Engineer job codes and weight-based pay |
| 35 | `Part35_PayRatesTiers.md` | Pay rate hierarchy and tiers |
| 36 | `Part36_MigrationsOverview.md` | EF migrations (web app + SAClassLibrary) |
| 37 | `Part37_DailyReportSystem.md` | Daily report timers and PDF generation |
| 38 | `Part38_InterfaceFilesMSMQ.md` | File-based messaging and MSMQ queues |
| 39 | `Part39_OvertimeBoard.md` | Overtime board entity and processing |
| 40 | `Part40_CompleteEntityIndex.md` | Alphabetical entity index (130+ entities) |
| 41 | `Part41_GapAnalysis_HardCodedLogic.md` | Gaps 1-45: Pools, FRA, Status, Holiday, Boards |
| 42 | `Part42_GapAnalysis_Continued.md` | Gaps 46-57: MarkOffCode, MarkOffRecord, RailroadPosition |
| 43 | `Part43_GapAnalysis_OnDuty_Payroll_Bulletin.md` | Gaps 58-70: OnDuty, PayrollRecord, Bulletin |
| 45 | `Part45_GapAnalysis_Assignment_TieUp_Payroll.md` | Gaps 71-85: Assign, TieUp, Payroll creation |
| 46 | `Part46_GapAnalysis_Earnings_PayrollRules.md` | Gaps 86-100: Earning codes, arbitrary pay, pool rules |
| 47 | `Part47_GapAnalysis_EarningCodeDetermination.md` | Gaps 101-111: OT determination, DailyCrewPosition |
| 48 | `Part48_GapAnalysis_MarkUp_Move_Shift.md` | Gaps 112-125: MarkUp, SeniorityMove, Shift creation |
| 49 | `Part49_GapAnalysis_CompHours_Vacation_Hangout.md` | Gaps 126-140: Comp hours, vacation weeks, hangout, bulletin |
| 50 | `Part50_GapAnalysis_FRA_Crew_HoldDown.md` | Gaps 141-155: FRA formulas, Crew off-day, HoldDown, duplication |
| 51 | `Part51_GapAnalysis_PayrollUtilities.md` | Gaps 156-168: Approval routing, ADP/UKG, job code fixes |
| 52 | `Part52_GapAnalysis_ExtraBoard_OvertimeBoard.md` | Gaps 169-182: XB/OT boards, LoseGuarantee, ordering |
| 53 | `Part53_GapAnalysis_PayRate_TempAssign.md` | Gaps 183-198: Pay rate parsing, 40h OT, temp assignments |
| 54 | `Part54_GapAnalysis_RREmp_Assignment_DailyAssign.md` | Gaps 199-213: Comp time, qualifying hrs, board order |
| 55 | `Part55_GapAnalysis_TieUpController.md` | Gaps 214-228: GetJobPaidCode, meal periods, pay grades |
| 56 | `Part56_GapAnalysis_FillVacancy_Boards.md` | Gaps 229-245: FillVacancy flow, boards, YM markoff, arrival |
| 57 | `Part57_GapAnalysis_ProcessPayroll_MarkOffRequest.md` | Gaps 246-260: Payroll processing, monthly pay, MO requests |
| 58 | `Part58_GapAnalysis_PayrollCtrl_DailyAssignCtrl.md` | Gaps 261-275: Manual payroll, pay periods, MSMQ, assignments |
| 59 | `Part59_FinalConsolidatedSummary.md` | **FINAL**: All 275 gaps unified, reference tables, priorities |
| 60 | `Part60_GapAnalysis_FinalScan.md` | Gaps 276-295: VacancyService, wait list, AutoPay, HoldDown |
| 61 | `Part61_GapAnalysis_VerificationScan.md` | Gaps 296-315: SeniorityMove, XB creation, NN code, SellTime, Holiday files |
| 62 | `Part62_GapAnalysis_WindowsServices.md` | Gaps 316-338: MSMQ pipeline, ADP/UKG import, VacWeek codes, comp hours |

---

## All 85 Gaps � Quick Reference

### Hard-Coded Pool Numbers (6 pools � 15+ switch statements)
- Gap 1: Pool identity map (10/20/30/40/50/60)
- Gap 3: DefaultJobWorked/DefaultJobPaid matrix
- Gap 5: Mechanical OT board (Pool 40 only)
- Gap 13: 40-hour OT conversion (Pools 20/30)
- Gap 16: Vacation week start date (Pool 10)
- Gap 33: GetJobCode/GetPayCode by pool
- Gap 36: OT board exclusion (Pool 20 XB)
- Gap 59: HoursOnDuty meal deduction (Pool 50)
- Gap 60: IsTraining CutBack (Pool 10), HasTrainees (Pool 40)

### Hard-Coded CraftName Strings
- Gap 2: Protected (Y&E), SemiProtected (Yardman)
- Gap 51: RailroadPosition.DefaultJobPaid (Engineer/Yardman)
- Gap 58: Tie-up rest calculation (Clerical/Yardmaster/Engineer/Yardman)
- Gap 65: Bulletin AssignDateTime (Clerical/Engineer/Mechanical)

### Hard-Coded Mark-Off Codes (14 codes)
- Gap 46: Auto mark-up hours (V1-V5=168-840, CD/PD/SD/VD=24)
- Gap 47: MarkOffCode flags
- Gap 56: Complete code-to-logic map

### Hard-Coded Date/Time Constants
- Gap 4: FRA constants (12h/10h/6d/24h)
- Gap 2: Employment dates (1981-01-01, 1991-01-01)
- Gap 18: TieUpOrder AddYears(10) pushback
- Gap 25: Seniority sentinel (9999-12-31)
- Gap 35: Month-to-day conversion (30.42)
- Gap 61: TurnoverPay 15 minutes
- Gap 62: IsClosed 4-day window

### Hard-Coded Payroll Codes
- Gap 14: Code "20" double time
- Gap 40: Codes "01"/"42" for weekly ST calculation
- Gap 66: Assignment types (BA/SM/FA/MA)

### Business Logic Trees
- Gap 31: Holiday qualification (~400 lines, 15 outcome codes, recursive)
- Gap 32: Status change state machine (AT/NA/XE)
- Gap 37: Bulletin no-access logic
- Gap 38: Seniority comparison and bump rules
- Gap 48: CreateMarkOffRecord full flow (~250 lines)
- Gap 67: AutomaticAssignment bid processing

### Missing Entities/Properties
- Gap 6: CraftPayCodes
- Gap 7: Craft (11 missing properties)
- Gap 8: RailroadPool (10 missing flags)
- Gap 9/30: Position resolution chain
- Gap 15: RailroadPoolMarkOffAllowance
- Gap 21: Interfaces (IAutoMarkUp, ICacheProvider, IAvailableEmployeeRepository)
- Gap 22: Structs (QualifyRecord, StartEndPeriod)
- Gap 23: SelectLists query class
- Gap 42/43: Roster.OvertimeBoard, Employee.IsOnExtendedAbsence
- Gap 52/53/64/70: Full property lists for MarkOffRecord, Roster, PayrollRecord, OnDutyRecord

### Code Quality Issues
- Gap 12: Debug credentials in source
- Gap 24: Float-based year calculation
- Gap 26: Computed properties opening own DbContext
- Gap 57: Thread.Sleep busy-wait (3 locations, no timeout)

---

## Codebase Statistics

| Metric | Count |
|---|---|
| Projects in solution | 5 |
| Controllers | 113 |
| Entity models (web app) | 130+ |
| Entity models (SAClassLibrary) | 220+ |
| View models | 103 |
| Migrations (web app) | Hundreds |
| Migrations (SAClassLibrary) | 5 |
| CollectionLists methods | ~200 |
| SelectLists methods | ~2,136 lines |
| RailroadPoolEmployee.cs | 3,982 lines |
| MarkOffRecord.cs | 2,375 lines |
| DailyCrewPositionOnDutyRecord.cs | 2,432 lines |
| RailroadPosition.cs | 1,703 lines |
| Global.asax.cs | ~1,500+ lines |
| Target framework | .NET Framework 4.7.2 |
| Total gaps documented | 338 |
# Part 58: Gap Analysis � PayrollController, DailyAssignmentController, Pay Period Ranges, MSMQ

Gaps 261-275 covering manual payroll entry, pay period date ranges, JobWorked formatting, daily assignment creation, MSMQ messaging, and extra position tracking.

---

## GAP 261: Pay Period Date Range Selection (Missing from Part 16)

`ApplicationUtilities.GetStartEndPeriod()` � 11 period types:

| Period | Date Range |
|---|---|
| 0 | Today |
| 1 | Yesterday |
| 2 | Current half-month (1st-15th or 16th-end) |
| 3 | Previous half-month |
| 4 | This month |
| 5 | Previous month |
| 7 | Last finalized pay period |
| 8 | Previous-previous period |
| 9 | Previous year |
| 10 | Custom date range |
| default | Year-to-date |

Half-month logic: if today < 16th ? current half is 1st-15th, prev half is prev month 16th-end.

---

## GAP 262: Manual Payroll Entry � JobWorked Formatting (Missing from Part 16)

6th copy of pool-specific code formatting, now for `JobWorked` field:

| Pool | Format | Example |
|---|---|---|
| 10 (Y&E) | `"{AssignmentWorked}{PositionCode}"` | `"101F"` |
| 20 (YM) | `"{AssignmentWorked}{PositionCode}"` | `"201A"` |
| 40 (Mech) | `"{AssignmentWorked}{PositionCode}"` | `"400Y"` |
| 30 (Clerical) | `"{PositionCode}{AssignmentWorked}"` | `"A123"` |
| 60 (Patrol) | `"{PositionCode}{AssignmentWorked}"` | `"P100"` |
| 50 (MOW) | `"{AssignmentWorked}"` (no position code) | `"EL10"` |

---

## GAP 263: Manual Entry Flag (Missing from Part 16)

```csharp
payrec.ManualEntry = db.Users.Find(userId).EmployeeNumber.Equals(rpemployee.EmployeeNumber);
```

True = employee entered their own record. False = timekeeper entered it for someone else.
All manual entries automatically get `PayrollReviewRequiredRecord`.

---

## GAP 264: Compensation Account Debit Before Earning (Missing from Part 28)

```csharp
if earncode has CompensationType AND ST hours > 0:
    sthrs = DebitCompensationAccount(earncode, sthrs.Hours, payrollDate)
```

Hours are debited from the compensation account BEFORE creating the earning record. The returned value may be reduced if balance is insufficient.

---

## GAP 265: Batch Number Formula (Missing from Part 16)

```csharp
payrec.Batch = "{PoolNumber}{CraftNumber}"
```

Examples: Pool 10 + Craft 20 = "1020", Pool 30 + Craft 10 = "3010".
Fallback: "9999" if no last active position found.

---

## GAP 266: DailyAssignment Creation � MSMQ Integration (Missing from Part 4)

```csharp
ServiceUtilities.CreateMSMQMessage("DailyCrewPosition", "Create", body.ToString());
```

Daily crew positions are created via MSMQ message queue (asynchronous):
```
Message body: "{DailyAssignmentCN},{RailroadPositionCN},{AssignmentDate},{ExtraBoardOnly},{CrewCN},{PositionCN}"
```

External service `SADailyCallSheetService` processes the queue.

---

## GAP 267: DailyAssignment Create � Pool-Specific UI (Missing from Part 4)

| Pool | Assignment Source | AFE Support | Billing Fields |
|---|---|---|---|
| 40 (Mech) | Pool assignments | No | No billable/recollectable |
| 50 (MOW) | Pool assignments | Yes (conditional) | Yes |
| Default | Shift assignments | No | No |

MOW employees creating their own assignments get auto-populated request notes and first approval officer.

---

## GAP 268: Extra Position Tracking (Missing from Part 4)

`ExtraPosition` struct tracks duplicate positions per assignment:
```csharp
if position appears > 1 time on same assignment:
    PositionName = "Extra {PositionName}"
    Count = number of occurrences
```

Used for call sheet display to differentiate regular vs extra positions.

---

## GAP 269: Employee Role-Based View Restrictions (Missing from Part 6)

```csharp
rremponly = employee.IsRailroadEmployeeRoleOnly || employee.IsUnionRepresentativeRole
```

Railroad employees and union representatives see restricted call sheet views.

---

## GAP 270: DailyAssignmentShift Auto-Creation (Missing from Part 4)

When an employee creates an assignment and no shift exists for the date:
```csharp
dashift = DailyAssignmentShift.CreateInstance(ondutyDate, poolCN, shiftCN);
db.DailyAssignmentShifts.Add(dashift);
```

The system auto-creates the daily assignment shift if it doesn't exist.

---

## GAP 271: Assignment On-Duty Day Hours Override (Missing from Part 4)

```csharp
var assignmentondutyday = assignment.AssignmentOnDutyDays
    .SingleOrDefault(d => d.WeekDay.WeekDayName.Equals(dayname));

if (assignmentondutyday != null)
    hours = assignmentondutyday.StraightTimeHours;
```

Day-specific ST hours from the assignment override the default `viewModel.StraightTimeHours`.

---

## GAP 272: PayrollController JobCode Display List � Pool-Specific Formatting (Missing from Part 16)

Job code dropdowns built differently per pool:

| Pool | Display Format | Value |
|---|---|---|
| 10 Y&E, 20 YM, 40 Mech | `"{PoolName} - {PayrollCode}{PositionCode}"` | last 4 chars |
| 30 Clerical, 60 Patrol | `"{PoolName} - {PositionCode}{PayrollCode}"` | last 4 chars |
| 50 MOW | `"MofW - {PositionCode}{PayrollCode}"` | last 4 chars |
| 10 Engineer | `"Engineer - {PayClassCode}"` | PayClassCode |

Pool 10 always adds: `"Yardman - 100H"`, `"100F"`, `"101H"`, `"101F"` (hard-coded).

---

## GAP 273: PayrollController EarningCounts � Minutes Overflow (Missing from Part 16)

```csharp
if (STMinutes > 0):
    sthours = STMinutes / 60
    stmins = STMinutes % 60
    STHours += sthours
    STMinutes = stmins
```

Manual minute-to-hour overflow calculation for display totals.

---

## GAP 274: AtHoc Message Timer Trigger Points (Missing from Part 12)

`MvcApplication.SetAtHocMessageTimer()` is called from:
1. `FillVacancy` (after vacancy fill)
2. `DailyAssignment.Create` (after extra assignment creation)
3. Multiple model methods (mark-off, mark-up, position changes)

Triggers emergency notification system updates.

---

## GAP 275: PayrollRecord.Batch Correction Summary

Batch number is set/corrected in 3 locations:

| Location | Logic |
|---|---|
| Auto-creation (tie-up) | `"{PoolNumber}{CraftNumber}"` |
| Manual creation | `"{PoolNumber}{CraftNumber}"` or `"9999"` |
| File export | Batch 40xx?`"4010"`, 50xx?`"5010"` |
# Part 59: Final Consolidated Summary � 275 Gaps Across 58 Documentation Parts

## Overview

This document consolidates all 275 gaps discovered during the deep-scan analysis of the StrategicApplications codebase (~30,000 lines of business logic across 33 files).

---

## Section A: Scan Statistics

| Metric | Value |
|---|---|
| Total gaps documented | 295 |
| Documentation parts | 58 (Parts 1-58) |
| Gap analysis files | 18 (Parts 41-60) |
| Files fully scanned | 38 |
| Business logic lines | ~33,000 |
| Earning codes | 27 |
| Mark-off codes | 21 |
| Pool-specific branches | 75+ |
| Duplicated logic patterns | 7+ copies |

---

## Section B: The 6 Railroad Pools � Complete Reference

| PoolNumber | Name | CraftNumbers | Key Behaviors |
|---|---|---|---|
| 10 | Yard & Enginemen | 10 (Engineer), 20 (Yardman) | FRA hours-of-service, locomotive weight pay, 100/101 rate split, 45-day MO request limit |
| 20 | Yardmasters | 30 (Yardmaster) | Auto mark-off 89min before on-duty, cutback OT board, 60-day MO request limit, 40h XB OT |
| 30 | Clerical | 40+ (varies) | Pay grade hierarchy (4-8), no XB rotation on OT, 40h XB OT, sick day tracking, "122"?"123" fix |
| 40 | Mechanical | 50+ (varies) | Position code enum (Y/T/L/S), OT board skip rules, vacation relief + V-code suppression, 16h rule |
| 50 | Maintenance of Way | 60+ (varies) | Reverse code format, AFE/zone billing, 1-min OT ST hours, rate-based pay-up, MSMQ assignment creation |
| 60 | Patrolmen | 70+ (varies) | Reverse code format (same as 30), minimal special logic |

---

## Section C: Complete Earning Code Reference (27 Codes)

| Code | Description | Key Rules |
|---|---|---|
| `"01"` | Straight Time | Default; triggers 40h XB OT (pools 20/30) |
| `"02"` | Overtime | Non-assigned employee, same craft |
| `"03"` | Overtime (Off Day) | Off-day work |
| `"04"` | Vacation Week | Compensated rate; pool 10/20 uses payroll-code officer |
| `"05"` | Holiday | Holiday qualified |
| `"06"` | Vacation Day | Compensated rate; pool 10/20 uses payroll-code officer |
| `"10"` | Jury Duty | All pools use payroll-code officer |
| `"11"` | Bereavement | All pools use payroll-code officer |
| `"12"` | Personal Day | Compensated rate; pool 10/20 uses payroll-code officer |
| `"13"` | Guarantee | Blended rate for "600A"; capped 480h qualifying |
| `"19"` | Doubleheader | Starts < 22:30 apart, last record not OT |
| `"20"` | Double Time | 2� ST rate placed in STAmount |
| `"21"` | Time Claim | Pool 10 uses payroll-code officer; excludes from safety |
| `"22"` | Off Day OT | Off-day + not XB/hangout |
| `"41"` | Trainer Pay | Pool 10/20 uses payroll-code officer |
| `"42"` | Trainee Pay | Triggers 40h XB OT (pools 20/30) |
| `"43"` | Job Trainee | Pool 10 uses payroll-code officer |
| `"44"` | Other/Claims | All pools use payroll-code officer |
| `"45"` | Safety Day | All pools if no CompensationType |
| `"49"` | Safety Incentive | Monthly; excludes time-claim-only employees |
| `"63"` | Glove Allowance | Monthly; Yardman only; $3.00 |
| `"50"` | Mark-Off Pay | Arbitrary flag |
| `"51"` | Comp Day Pay | Arbitrary flag |
| `"52"` | Vacation Day Pay | Arbitrary flag |
| `"53"` | Personal Day Pay | Arbitrary flag |
| `"54"` | Sick Day Pay | Arbitrary flag |
| `"55"` | Holiday Pay | Arbitrary flag |

---

## Section D: Complete Mark-Off Code Reference (21 Codes)

| Code | Description | Auto MarkUp | LoseGuarantee Max |
|---|---|---|---|
| `"AW"` | Absent Without Leave | No | every 3rd |
| `"CD"` | Compensated Day | Craft-specific hours | every 3rd |
| `"DI"` | Dismissed | No | N/A |
| `"FA"` | Family Leave | No | every 3rd |
| `"LV"` | Leave of Absence | No | every 3rd |
| `"ML"` | Medical Leave | No | every 3rd |
| `"MO"` | Mark Off (General) | No | every 3rd |
| `"NR"` | Not Rested | Auto (exact time) | every 3rd |
| `"PD"` | Personal Day | Craft-specific hours | 1 day |
| `"SD"` | Sick Day | Craft-specific hours | every 3rd |
| `"SR"` | Safety Rest | Auto (exact time) | 2 days |
| `"SU"` | Suspended | No | N/A |
| `"V1"` | Vacation 1 Week | 168 hours | 5 days |
| `"V2"` | Vacation 2 Weeks | 336 hours | 10 days |
| `"V3"` | Vacation 3 Weeks | 504 hours | 15 days |
| `"V4"` | Vacation 4 Weeks | 672 hours | 20 days |
| `"V5"` | Vacation 5 Weeks | 840 hours | 25 days |
| `"VD"` | Vacation Day | Craft-specific hours | 1 day |
| `"WC"` | Workers Comp | No | every 3rd |
| `"OJ"` | On-the-Job Injury | No | every 3rd |
| `"TR"` | Training | No | every 3rd |

---

## Section E: Critical Duplication Map

### E1: Job Code / Payroll Code Formatting (7 Locations)

| # | Location | File | Type |
|---|---|---|---|
| 1 | `DailyCrewPosition.JobCode` | DailyCrewPosition.cs | Property |
| 2 | `DailyCrewPosition.PayrollCode` | DailyCrewPosition.cs | Property |
| 3 | `RailroadPoolEmployee.GetJobCode()` | RailroadPoolEmployee.cs | Method |
| 4 | `RailroadPoolEmployee.GetPayCode()` | RailroadPoolEmployee.cs | Method |
| 5 | `CrewPosition.PayrollCode` | CrewPosition.cs | Property |
| 6 | `DailyOnDutyRecordTieUpController.GetJobPaidCode()` | Controller | **Definitive** |
| 7 | `PayrollController.Create` (dropdown builder) | Controller | UI display |

**Pattern**: Pools 10/20/40 = `{PayrollCode}{PositionCode}`, Pools 30/60 = `{PositionCode}{PayrollCode}`, Pool 50 = varies.

### E2: DefaultJobPaid (3 Locations)

| Location | File |
|---|---|
| `RailroadPoolEmployee.DefaultJobPaid` | RailroadPoolEmployee.cs |
| `RailroadPosition.DefaultJobPaid` | RailroadPosition.cs |
| `DailyCrewPosition.DefaultJobPaid` | DailyCrewPosition.cs |

### E3: Batch Number Correction (3 Locations)

| Location | Logic |
|---|---|
| Auto-creation (tie-up) | `"{PoolNumber}{CraftNumber}"` |
| Manual creation | Same or `"9999"` fallback |
| File export (ADP/UKG) | 40xx?`"4010"`, 50xx?`"5010"` |

---

## Section F: Key Business Flows (Condensed)

### F1: Vacancy Fill Flow (Gap 230)
```
1. Find XB position ? Remove DoNotFill ? Find existing on-duty record
2a. No record: Create on-duty ? Set XB order (FIFO=offduty, Rotating=now)
    ? Pool 20: auto mark-off (onduty-89min) ? YM mark-up (offduty)
2b. Existing: Create moved record ? Pool 10 on-duty move splits payroll
3. Create XB assignment snapshot ? Trigger AtHoc
```

### F2: Tie-Up Flow (Gap 218)
```
Pool 10: ChangeArrival ? Locomotive (Engineers) or Payroll ? Certify ? Create
Pool 20/30: Trainees? ? Payroll or Create
Pool 40: Location "Rip"? ? Payroll or Create
Pool 50: MofWBilling ? Create
```

### F3: Payroll Processing Flow (Gap 247)
```
Trial: Delete previous ? Validate FKs ? Check approvals ? Generate ADP+UKG
Final: Query unfinalized ? Generate ADP+UKG ? Mark FinalProcess=true
```

### F4: FRA Compliance Flow (Gap 142)
```
if ConsecutiveDays < 6: Check rest for next on-duty
  if not rested: auto mark-off "NR"
if ConsecutiveDays >= 6: auto mark-off "SR" (24h safety rest)
Rest = 10h base + excess over 12h worked
```

### F5: Mark-Off Request Flow (Gap 252)
```
Create request ? Set days/markup ? Approval officer
If auto + past date: immediate mark-off
If Pool 40 + V-code: vacation relief flag
Remove matching wait-list record
```

---

## Section G: Pay Rate Architecture (Condensed)

### G1: JobPaid Substring Parsing (Gap 183)

| Pool | PayrollCode | PositionCode | Example |
|---|---|---|---|
| 10 Engineer | EngineerPayRates table | N/A | `"10O1"` |
| 10 Yardman | Fixed `"101"` | `[3]` | `"101F"` |
| 20, 40 | `[0..2]` | `[3]` | `"201A"` |
| 30, 60 | `[1..3]` | `[0]` | `"A123"` |
| 50 | `[2..3]` | `[0..1]` | `"EL10"` |

### G2: Rate Hierarchy
```
1. Look up base rate from PositionPayRates (or EngineerPayRates)
2. Apply tier: rate � (RatePercentage / 100)
3. Ceiling to penny: Math.Ceiling(amount � 100) / 100
4. Special: Code "20" ? 2� ST rate; Code "13" ? blended "600A" rate
5. Compensated rate (Pool 10): stored procedure "RailroadEmployeeVacationRate"
```

### G3: Pay-Up Rules (Gap 214-217)
| Pool | Rule |
|---|---|
| 10 Yardman | Not assigned ? "100" prefix; assigned ? "101" prefix |
| 30 Clerical | Higher pay grade wins (grades 4-8, same shift) |
| 40 Mechanical | Higher position enum wins (Y<T<L<S, same shift) |
| 50 MOW | Higher `CurrentSTPayRate` wins |

---

## Section H: Board Management (Condensed)

### H1: Extra Board
| Property | Value |
|---|---|
| Type 0 | Not an extra board |
| Type 1 | FIFO � ordered by tie-up time |
| Type 2 | Rotating � ordered by current time |
| BoardOrder increment | 10 (allows insertions) |
| TieUpOrder format | `yyyyMMddHHmm` as long |
| Mark-off push | TieUpOrder += 10 years |
| LoseGuarantee | VD/PD=1, SR=2, V1-V5=5-25, others=every 3rd |

### H2: Overtime Board
| PositionType | Range | Description |
|---|---|---|
| `"OT"` | 1000+ | Regular overtime |
| `"CB"` | 2000+ | Cutback (Pool 20 only, refreshed each shift) |
| `"MO"` | 9000+ | Marked-off/moved |

Pool 40 skip rules: non-emergency, insufficient rest, 16h worked.

### H3: Vacancy Board Types (Gap 229)
| Board | Source |
|---|---|
| 0 | Same Assignment (on-duty employees) |
| 1 | Extra Board |
| 2 | Off-Day Board |
| 4 | Overtime Board |
| 5 | Vacation Relief Board |
| 6 | Qualified Employee Board |
| default | Seniority Board |

---

## Section I: Hard-Coded Values & Magic Numbers

| Value | Location | Meaning |
|---|---|---|
| `89` minutes | FillVacancy | YM auto mark-off before on-duty |
| `9999` | BoardOrder | Pushed to back of board |
| `9000` | BoardOrder offset | Non-bottom XB placement |
| `4000` | BoardOrder offset | Future-dated OT positions |
| `400` | Record limit | LastActiveCraft search limit |
| `480` | Hours cap | Guarantee qualifying hours (60 days) |
| `22:30` | TimeSpan | Doubleheader threshold |
| `$3.00` | Amount | Glove allowance |
| `$5.00` | Amount | Crew consist pay (Batch 1020) |
| `2020-03-15` | Date | FlagCode start date |
| `8` and `14` | Off-day sum | Reverse day ordering trigger |
| `10` hours | FRA | Base rest time |
| `12` hours | FRA | Max duty before extended rest |
| `24` hours | FRA | Consecutive day rest break |
| `6` days | FRA | Max consecutive days |
| `40` hours | Weekly cap | XB OT threshold (Pools 20/30) |
| `30` minutes | Time cap | Non-Pool-10 off-duty max from now |

---

## Section J: Network Paths & External Dependencies

| Path | Purpose |
|---|---|
| `\\Finance-svr\Payroll Exports\UKG\` | UKG payroll files |
| `\\Finance-svr\Payroll Exports\ADP\` | ADP payroll files |
| `\\Finance-svr\Payroll Exports\UKG\History\` | Historical payroll archives |
| `\\Finance-svr\Payroll Exports\UKG\Logs\` | Error logs |
| `\\Viper\payroll\ADPPTRA\EPIPT190.csv` | TIES source file |
| SQL SP `RailroadEmployeeVacationRate` | Pool 10 compensated rate |
| MSMQ `DailyCrewPosition` | Async crew position creation |
| AtHoc API | Emergency notification system |
| Microsoft Teams API | SystemMessage chat |
| SAClassLibrary DB | OffPropertyTieUpRecords, shared entities |

---

## Section K: Compensation Time Types

| Type Code | Name | Hours/Day | Hours/Week |
|---|---|---|---|
| `"VW"` | Vacation Week | �40 = weeks | 40 |
| `"VD"` | Vacation Day | �8 = days | 8 |
| `"PD"` | Personal Day | �8 = days | 8 |
| `"SD"` | Sick Day | �8 = days | 8 |
| `"CD"` | Compensated Day | �8 = days | 8 |

Entry types: `"Credit"` (positive), `"Debit"` (negative stored), `"Adjust"` (correction).

---

## Section L: Thread Safety Risks

| Location | Issue |
|---|---|
| `ProcessPayrollController.Records` | Static mutable `List<PayrollRecord>` |
| `ProcessPayrollController.RPEmployees` | Static mutable `List<RailroadPoolEmployee>` |
| `ProcessPayrollController.Status` | Static mutable string |
| `MvcApplication.BoardProcessing` | Static bool, polled with `Thread.Sleep(1000)` |
| `MvcApplication` timer dictionaries | 18+ static `Dictionary` instances |
| `VacancyAssignmentService` | 3 static mutable lists (vacancylist, xbpositionlist, rosterxblist) |
| `CollectionLists` | Static methods creating new `DbContext` per call |

---

## Section M: ADP/UKG File Formats

### ADP (17 columns)
```
Co Code, Batch ID, File #, Pay #, Temp Cost Number,
Reg Hours, O/T Hours, Hours 3 Code, Hours 3 Amount,
Hours 4 Code, Hours 4 Amount, Earnings 5 Code, Earnings 5 Amount,
Memo Code, Memo Amount, Earnings 3 Code, Earnings 3 Amount
```
Cost Number = `{yyMMdd}{DayOfWeek+1}{JobWorked}{JobPaid}` (15 chars)

### UKG (6 columns)
```
EmployeeNumber, UKGEarningCode, Hours, Amount, JobPaid, PayrollDate
```
Three rows per earning: ST Hours, OT Hours, Amount.

### Job Code Corrections at Export (Gap 158)
| Original | Corrected |
|---|---|
| `"101D"` | `"10H1"` |
| `"A122"` | `"A123"` |
| `"100F"` | `"101F"` |
| `"100H"` | `"101H"` |
| Starts with `"S"` | `""` (empty) |

---

## Section N: Pay Period System

### Period Format
`{MMddyy}` as 6-digit integer. Day=01 or 16.

### Date Ranges
- Day 01-15: paydate = month/15
- Day 16+: paydate = last day of month

### Trial/Final
- Trial: deletes previous trial, validates FKs, generates files
- Final: marks all processed records, archives to History folder

---

## Section O: Clerical Pay Grade & Mechanical Position Hierarchies

### Clerical (Gap 215)
| PayrollCode | Grade |
|---|---|
| 102, 116, 123 | 4 |
| 170 | 5 |
| 135, 150 | 6 |
| 104 | 7 |
| 100, 112, 130, 199 | 8 |

### Mechanical (Gap 216)
```
enum MechanicalCodes { Y=0, T=1, L=2, S=3 }
```
Higher ordinal = higher pay.

---

## Section P: Meal Period Rules (Gap 220)

| Pool | 1st Meal | 2nd Meal | Air Pay |
|---|---|---|---|
| 10 (Y&E) | On-Duty + 4:30 | 1st + 20min + 4:30 (if TOD > 9:19) | Yes |
| 40 (Mech) | On-Duty + configurable | No | No |
| Others | No | No | No |

Claim values: 0=not claimed, 30=claimed but not taken, 31=N/A.

---

## Section Q: Complete Gap Index (275 Gaps)

### Part 41 � Gaps 1-45: Pools, FRA, Status, Holiday, Boards
| Gap | Topic |
|---|---|
| 1-6 | Pool identity map, pool-specific properties, default job codes |
| 7-12 | FRA rest formulas, consecutive days, 6-day rule |
| 13-18 | 40h OT conversion, vacation week start, craft-specific hours |
| 19-24 | Status codes, mark-off codes, holiday calendar rules |
| 25-30 | Board types, XB/OT ordering, seniority move triggers |
| 31-36 | Bulletin logic, position code formatting, shift creation |
| 37-45 | Auto-pay, mark-up timing, off-day detection |

### Part 42 � Gaps 46-57: MarkOffCode, MarkOffRecord, RailroadPosition
| Gap | Topic |
|---|---|
| 46-51 | Auto mark-up hours, craft-specific mark-off duration |
| 52-57 | RailroadPosition properties, board/crew detection, off-day initials |

### Part 43 � Gaps 58-70: OnDuty, PayrollRecord, Bulletin
| Gap | Topic |
|---|---|
| 58-63 | On-duty record lifecycle, assignment/release tracking |
| 64-70 | Payroll record creation, bulletin response tracking |

### Part 45 � Gaps 71-85: Assignment, TieUp, Payroll
| Gap | Topic |
|---|---|
| 71-76 | Assignment on-duty days, work areas, cut-off times |
| 77-85 | Tie-up process routing, FRA certify, payroll info creation |

### Part 46 � Gaps 86-100: Earnings, Arbitrary Pay, Pool Rules
| Gap | Topic |
|---|---|
| 86-91 | Earning code determination, arbitrary flags, accumulator |
| 92-100 | Pool-specific earning rules, compensation type, approval routing |

### Part 47 � Gaps 101-111: Earning Code Determination, DailyCrewPosition
| Gap | Topic |
|---|---|
| 101-106 | OT determination, doubleheader, off-day OT |
| 107-111 | PayOvertime method, craft-specific OT exclusions |

### Part 48 � Gaps 112-125: MarkUp, SeniorityMove, Shift
| Gap | Topic |
|---|---|
| 112-118 | Board vs crew auto mark-up, exact time vs midnight |
| 119-125 | Seniority move types, shift creation, position assignment |

### Part 49 � Gaps 126-140: CompHours, Vacation, Hangout, Bulletin
| Gap | Topic |
|---|---|
| 126-130 | Compensation hours calculation, vacation day conversion |
| 131-135 | Hangout employee logic, extended absence boards |
| 136-140 | Bulletin response, qualification positions, training rosters |

### Part 50 � Gaps 141-155: FRA Formulas, Crew, HoldDown
| Gap | Topic |
|---|---|
| 141-145 | FRA rest calculation, 10h base + excess, commingled service |
| 146-150 | Crew off-day, position fill chain, DoNotFill |
| 151-155 | HoldDown entity, release chain, position displacement |

### Part 51 � Gaps 156-168: Approval Routing, ADP/UKG
| Gap | Topic |
|---|---|
| 156-160 | Approval officer hierarchy, earning code/pool matrix |
| 161-165 | UKG format, batch corrections, network paths |
| 166-168 | TIES integration, import CSV, 24 earning codes |

### Part 52 � Gaps 169-182: XB/OT Boards, LoseGuarantee
| Gap | Topic |
|---|---|
| 169-173 | Board types, LoseGuarantee limits, TieUpOrder encoding |
| 174-178 | Pool 20 cutback refresh, future-dated positions |
| 179-182 | Bottom/non-bottom placement, snapshot fields |

### Part 53 � Gaps 183-198: Pay Rates, Temp Assignments
| Gap | Topic |
|---|---|
| 183-188 | JobPaid parsing, engineer rates, tier application |
| 189-193 | Double time, rounding, compensation cleanup |
| 194-198 | Temp assignment release chain, moved position undo |

### Part 54 � Gaps 199-213: RR Employee, Assignment, DailyAssign
| Gap | Topic |
|---|---|
| 199-203 | Comp time entries, qualifying hours, vacation conversion |
| 204-208 | Status history, board order formula, pool fallbacks |
| 209-213 | HoldDown priority, LastActiveCraft, tie-up time |

### Part 55 � Gaps 214-228: TieUp Controller, GetJobPaidCode
| Gap | Topic |
|---|---|
| 214-217 | GetJobPaidCode definitive, clerical grades, mechanical enum |
| 218-222 | Tie-up routing, locomotive weight, meal periods |
| 223-228 | Notes required rules, MOW 1-min OT, payroll info sharing |

### Part 56 � Gaps 229-245: FillVacancy, Boards
| Gap | Topic |
|---|---|
| 229-233 | 7 board types, FillVacancy 3-phase flow, XB ordering |
| 234-238 | Late call, arrival adjustment, force assign |
| 239-245 | DoNotFill cleanup, hold-down chain, position move |

### Part 57 � Gaps 246-260: Process Payroll, MarkOff Requests
| Gap | Topic |
|---|---|
| 246-250 | Period format, trial/final, FK integrity, safety/glove pay |
| 251-255 | MO request limits, auto mark-off, vacation duration map |
| 256-260 | Dual ADP+UKG, safety exclusion, static state risk |

### Part 58 � Gaps 261-275: PayrollController, DailyAssignment
| Gap | Topic |
|---|---|
| 261-265 | Pay period ranges, JobWorked formatting, manual entry flag |
| 266-270 | MSMQ integration, pool-specific UI, extra positions |
| 271-275 | Day-specific hours, dropdown formatting, AtHoc triggers |

### Part 60 � Gaps 276-295: VacancyService, WaitList, AutoPay, HoldDown
| Gap | Topic |
|---|---|
| 276-281 | Vacancy assignment algorithm, 9-step helper search, 12-day limit |
| 282-285 | Vacation week availability, wait list CD/VW processing |
| 286-289 | AutoPay records, comp debit/credit, approval self-skip |
| 290-295 | HoldDown release chain, bump date, hangout 48h, Easter |

---

## Section R: Recommended Modernization Priorities

### Priority 1 � Extract Shared Logic
- Consolidate 7 copies of job code formatting into a single `JobCodeService`
- Consolidate 3 copies of DefaultJobPaid into a single resolution chain
- Extract pool-specific switch/case blocks into strategy pattern

### Priority 2 � Data-Drive Hard-Coded Values
- Move clerical pay grades (Gap 215) to database
- Move mechanical position codes (Gap 216) to database
- Move earning code ? approval officer matrix (Gap 157) to database
- Move LoseGuarantee limits (Gap 170) to configuration

### Priority 3 � Thread Safety
- Replace static mutable state in `ProcessPayrollController` with scoped services
- Replace `MvcApplication` timer dictionaries with background service pattern
- Replace `Thread.Sleep` polling with async/await

### Priority 4 � External Dependency Abstraction
- Abstract network paths into configuration
- Abstract stored procedure calls behind repository interfaces
- Abstract MSMQ into a message broker interface
- Abstract AtHoc/Teams into notification service interfaces
# Part 60: Final Scan � VacancyAssignmentService, MarkOffRequest, AutoPay, HoldDown, BumpDate

Gaps 276-295 covering the automated vacancy assignment algorithm, vacation week availability, wait list processing, auto-pay records, hold-down release chains, and bump date calculation.

---

## GAP 276: VacancyAssignmentService � Complete Algorithm (Missing from Part 6)

Automated vacancy-to-XB-employee matching algorithm:

```
1. Build vacancy list (unfilled, not annulled, not DoNotFill, not electronic-called)
2. Sort: roster ? date ? callStartTime ? vacancyNumber
3. Build XB position list for the shift (sorted TieUpOrder, BoardOrder)
4. For each vacancy:
   a. Filter XB to same roster, available at EndCallTime
   b. Check NoBid bulletin ? force-assign youngest
   c. If XB worked ? 12 days ? skip to next with < 12
   d. Check qualification ? assign if qualified
   e. If not qualified ? try foreman/helper swap
   f. If still not filled ? find eligible helper (9-step search)
   g. Create new vacancy for displaced helper position
```

### Static state risk:
```csharp
private static List<DailyCrewPositionVacancy> vacancylist;
private static List<DailyShiftExtraBoardPosition> xbpositionlist;
private static List<DailyShiftExtraBoardPosition> rosterxblist;
```

---

## GAP 277: Eligible Helper Search � 9-Step Location/Time Priority (Missing from Part 6)

When XB employee is not qualified for a foreman vacancy, search for a helper to displace:

| Step | Location | Time Comparison |
|---|---|---|
| 1 | Same assignment | N/A |
| 2 | Same location | Same start time |
| 3 | Same location | Earlier start time |
| 4 | Same location | Later start time |
| 5 | Next location | Same start time |
| 6 | Next location | Earlier start time |
| 7 | Next location | Later start time |
| 8 | Last location | Same start time |
| 9 | Last location | Earlier start time / Later start time |

### Location Rotation:
| Current LocationNumber | Next | Last |
|---|---|---|
| 11 | 14 | 13 |
| 13 | 14 | 11 |
| 14 | 13 | 11 |

Helper = PositionCode `"H"`, Foreman = PositionCode `"F"`.

---

## GAP 278: Foreman Protection Rule (Missing from Part 6)

```csharp
// Protect junior employee's right to work foreman on own assignment
if helper's assignment has an unfilled foreman position (all marked off):
    return null � don't displace this helper
```

Union-negotiated rule preventing helper displacement when their own foreman position is vacant.

---

## GAP 279: NoBid Bulletin Force Assignment (Missing from Part 15)

```csharp
if position has NoBid bulletin (IsNoBid && !IsAssigned && same date):
    youngest = GetForceAssignmentSeniorityList() � youngest qualified
    if youngest is on XB ? assign directly, skip normal matching
```

---

## GAP 280: XB 12-Day Work Limit (Missing from Part 19)

```csharp
if xbposition.DaysWorked >= 12:
    if any XB has DaysWorked < 12 ? skip to that one
```

Extra board employees with 12+ days worked are deprioritized.

---

## GAP 281: Vacancy Position Ordering (Missing from Part 6)

```csharp
dcpositions.OrderBy(cp.DailyCrewPositionSkip != null)  // skipped last
    .ThenBy(cp.AssignmentDate)
    .ThenBy(cp.DailyAssignment.BoardOrder)
    .ThenBy(cp.Position.PositionNumber)
```

`DailyCrewPositionSkip` positions are processed last.

---

## GAP 282: Vacation Week Availability Check (Missing from Part 33)

Multi-week vacation requests check availability across ALL weeks:

```csharp
V2 (2 weeks): check weeks at reqdate and reqdate+7
V3 (3 weeks): check weeks at reqdate, reqdate+7, reqdate+14
...
V5 (5 weeks): check 5 consecutive weeks

Each week checks: does any existing V2-V5 overlap with this week?
  week-1: same date V1-V5
  week-2: date-7 V2-V5
  week-3: date-14 V3-V5
  week-4: date-21 V4-V5
  week-5: date-28 V5
```

All weeks must be below `NumberAllowed` limit.

---

## GAP 283: Mark-Off Allowance � Craft vs Pool (Missing from Part 14)

```csharp
Pool 10: uses CraftMarkOffAllowances
Others:  uses RailroadPoolMarkOffAllowances
```

Entities: `CraftMarkOffAllowance` and `RailroadPoolMarkOffAllowance`.
Both have: `AllowanceType`, `NumberAllowed`, `Year`.

---

## GAP 284: Wait List CD Request � 2-Day Window (Missing from Part 14)

```csharp
Pool 10:
  if request date - 2 days <= today:
    Check CD allowance separately from other day types
    CD has its own max; other types share combined max
  else:
    Check individual allowance by code type

Others: Same logic but with pool-level allowances
```

---

## GAP 285: Wait List VW Processing (Missing from Part 33)

```csharp
For each VW wait list record:
  Find original MO request via MarkOffRequestMarkOffRequestWaitListRecord
  Check availability for all weeks
  If all available:
    Move original request to new date
    Create RailroadPositionChange notification (EmployeeOnly = true)
    Remove from wait list
```

---

## GAP 286: AutoPay Record � Complete Logic (Missing from Part 16)

`PayrollCrewPositionAutoPayRecord.CreateAutomaticPayrollRecord()`:

```
1. Check expiration date > on-duty date
2. Check not holiday AND ProcessPayroll
3. If BasicDay:
   - Engineer: jobpaid="10H1" (trainee: jobwrkd="100D", jobpaid="30H1")
   - Use assignment off-duty time (not actual)
   - No meal arbitraries
4. If NOT BasicDay:
   - Use actual off-duty time
   - Pay meals if Arbitraries flag set
5. Delete any existing payroll for same employee/job/onduty
6. Create payroll record + earnings + approval + review
```

New job codes: `"10H1"` (engineer basic day), `"30H1"` (trainee basic day), `"100D"` (trainee job worked).

---

## GAP 287: Earning Record Debit/Credit Compensation (Missing from Part 28)

```csharp
DebitCompensationAccount(): entry type "Debit", amount = -(STHours)
CreditCompensationAccount(): entry type "Debit", amount = +(STHours)  // NOTE: both use "Debit" type
```

Bug or design: `CreditCompensationAccount` uses entry type `"Debit"` with positive amount (for delete/decline reversal).

---

## GAP 288: Earning Approval � Self-Entry Skip (Missing from Part 8)

```csharp
if (Code.ApprovalRequired || approvalctrlnbr != 0) AND empctrlnbr != approvalctrlnbr:
    Create EarningsApprovalRequiredRecord
```

Approval is skipped if the entering user IS the approval officer. Also: `"autoprocess"` user gets empctrlnbr=999999.

---

## GAP 289: IsProcessed � Declined = Processed (Missing from Part 16)

```csharp
if (IsDeclined && (ProcessedRecord == null || !ProcessedRecord.FinalProcess)):
    return true;  // declined earnings count as "processed"
```

---

## GAP 290: HoldDown Release � Pool 30 Off-Duty Time (Missing from Part 27)

```csharp
if Pool 30 (Clerical):
    Find DailyCrewPosition for release date
    Set release date = that position's on-duty datetime
```

Clerical hold-down release aligns to the shift's on-duty time, not midnight.

---

## GAP 291: HoldDown � Recursive Release Chain (Missing from Part 27)

```csharp
ReleaseOpenHoldDownRecord():
  If employee has assigned position:
    Find hold-down on that position (by different employee)
    Recursively release THAT hold-down first
  Then release this hold-down
  Then re-assign employee to their daily crew positions
```

---

## GAP 292: BumpDate Calculation (Missing from Part 15)

```csharp
case "Engineer": case "Yardman":
  days = SeniorityMoveRule.RequiredDays
  if bulletin board position:
    return AssignedDate + RequiredDays (counting first day)
  if crew position:
    return AssignedDate + RequiredDays + on-duty time of that day
default:
  return DateTime.Now  // immediate bump
```

Bump date includes the on-duty time of the target day for crew positions.

---

## GAP 293: Hangout 48-Hour Window (Missing from Part 21)

```csharp
GetHangoutAssignmentDateTime():
  Find the confirmed change notification for the hangout assignment
  Return NotifyDateTime + 48 hours
```

Employee has 48 hours from notification to report.

---

## GAP 294: DateTimeUtilities � Easter Sunday Calculation (Missing from Part 20)

Full Easter algorithm (Computus) used for holiday calendar. Good Friday = Easter - 2 days.

---

## GAP 295: Vacation Week Start Date � Jan 1 Day-of-Week Alignment (Missing from Part 33)

```csharp
GetNextYearVacationRequestStartDate():
  jan1 = Jan 1 of next year
  offset = jan1.DayOfWeek - reqdate.DayOfWeek
  return reqdate + offset
```

Vacation weeks align to the day-of-week that January 1st falls on.
`GetDisabledWeekDays()` returns all days EXCEPT that day for calendar pickers.
# Part 61: Final Verification Scan � Remaining Entity Logic

Gaps 296-310 covering previously unscanned methods in DailyAssignmentCrew, DailyShiftExtraBoard, RailroadPositionChange, DailyRailroadEmployeePositionMarkOffRecord, RailroadEmployeeCompensableTimeRecord, RailroadPoolEmployeePosition, and requirement expiration logic.

---

## GAP 296: DailyAssignmentCrew.CreateDailyCrewPositions � Seniority Move Check (Missing from Part 4)

Before creating on-duty records for assigned employees, the system checks for same-day seniority moves:

```csharp
// Find seniority moves effective the same day
var senmove = employee.SeniorityMoves.FirstOrDefault(m => m.EffectiveDateTime.Date == assignmentDate);

if (senmove != null):
    // Check if this employee has the oldest seniority move for the position
    samemove = senmove == position.GetOldestSeniorityMove(assignmentDate)
    if samemove:
        if SeniorityMoveWillWork == null:
            willwork = senmove.EffectiveDateTime.TimeOfDay > AssignmentOnDutyTime
        else:
            willwork = SeniorityMoveWillWork.WillWork
```

Employee NOT created on-duty if their seniority move happens BEFORE the assignment on-duty time.

---

## GAP 297: DailyAssignmentCrew � Alternate Positions by Day-of-Week (Missing from Part 4)

```csharp
var altposition = crewPosition.CrewPositionAlternatePositions
    .FirstOrDefault(p => p.WeekDay.WeekDayName == assignmentDate.DayOfWeek.ToString());

if (altposition != null):
    use alternate position instead of default
```

Entity: `CrewPositionAlternatePosition` � different position for specific days of the week.

---

## GAP 298: DailyAssignmentCrew � Pool 50 Single-Position Special Case (Missing from Part 4)

```csharp
if Pool 50 (MOW) AND positions.Count == 1:
    if rpectrlnbr == 0:
        AssignHoldDownAndTemporaryPositions()  // normal flow
    else:
        Create on-duty for specific employee (rpectrlnbr)
else (all other pools):
    AssignHoldDownAndTemporaryPositions() for every position
```

MOW single-position crews use a different assignment path.

---

## GAP 299: DailyAssignmentCrew � Training Date Positions (Missing from Part 22)

```csharp
// Create positions for scheduled training dates
var training = db.RailroadPoolEmployeeTrainingDates
    .Where(d => d.TrainingDate == assignmentDate && d.CrewControlNumber == crew);

foreach training:
    Create temporary CrewPosition with ControlNumber = 99999999999999999
    Create DailyCrewPosition + OnDutyRecord for trainee
```

Uses sentinel value `99999999999999999` for the RailroadPositionControlNumber.

---

## GAP 300: DailyShiftExtraBoard � First Board vs Subsequent Boards (Missing from Part 19)

### First board (no previous XB exists):
```
Get available XB positions from CollectionLists
Create positions with BoardOrder=0, TieUpOrder=0
If marked off ? UpdateDailyExtraBoardMarkOffRecords
Qualify holiday record
```

### Subsequent boards (previous XB exists):
```
Copy positions from last uncompleted XB
Maintain TieUpOrder, reset BoardOrder to increments of 10
If "IsCalled" assignment exists ? copy assignment record
If marked off ? UpdateDailyExtraBoardMarkOffRecords
Qualify holiday record
```

---

## GAP 301: DailyShiftExtraBoard � XB Position Snapshot Fields (Missing from Part 19)

Each XB position stores a snapshot at creation time:

| Field | Source |
|---|---|
| `Status` | `rpemployee.Status` |
| `TwentyFourHourRestDateTime` | `rpemployee.TwentyFourHourRestDateTimeString` |
| `RosterBoardPositionName` | `rpemployee.CurrentPosition.PositionName` |
| `ConsecutiveDays` | `rpemployee.ConsecutiveDays` |
| `DaysWorked` | `rpemployee.GetSTDaysWorked(assignmentDate)` |

---

## GAP 302: RailroadPositionChange � "NN" (Could Not Notify) Mark-Off Handling (Missing from Part 32)

```csharp
CheckForCouldNotNotifyMarkOffRecord():
  if last open mark-off code == "NN":
    if notifyDate <= markOffDate:
      Delete the "NN" mark-off record
      Update daily records (XB or crew)
    else:
      Mark up at notifyDate
      If still marked off: update remaining records
```

New mark-off code: `"NN"` � Could Not Notify. Auto-deleted when notification is confirmed.

---

## GAP 303: RailroadPositionChange � Automatic Notification (Missing from Part 32)

```csharp
CreateAutomaticChangeNotification():
  NotificationType = "Automatic"
  Confirmed = true
  Notes = "Automatically created by a system process"
  CheckForCouldNotNotifyMarkOffRecord()
  CreateDailyRosterBoardPositionHangoutRecord()
```

System-generated notifications auto-confirm and trigger hangout board updates.

---

## GAP 304: DailyRailroadEmployeePositionMarkOffRecord � Mark-Off Payroll (Missing from Part 14)

Mark-off payroll record creation logic:

```csharp
if no on-duty record for the date OR employee is XB:
    Create payroll with mark-off code's PayrollCode
    ST = 8 hours default (or CompHours from mark-off record)
    OT = 0
    Amount = 0
else (has on-duty record):
    Create payroll from on-duty record instead (normal tie-up payroll)
```

Mark-off payroll only created when employee has no overlapping on-duty record (or is XB).

---

## GAP 305: DailyRailroadEmployeePositionMarkOffRecord � Change MO Code Updates Payroll (Missing from Part 14)

```csharp
ChangeDailyRailroadEmployeePositionPayrollRecords():
  if already processed ? return error
  if no payroll + new code is compensated + (BasicDay or XB):
    Create new payroll record
  else:
    Swap earning code to new mark-off's PayrollCode
    Reset STHours/OTHours/Amount to new code's defaults
```

---

## GAP 306: SellCompensableTime � Complete Flow (Missing from Part 28)

```csharp
1. Get balance (try current year, fallback to today's year if past year empty)
2. Cap hours at available balance
3. Withdraw from compensation account
4. Create payroll record (ManualEntry, ReviewRequired)
5. Loop: create earning records in DefaultTime increments
   (e.g., 8h per record) until all hours consumed
6. If selling from a request ? delete the request
```

Uses `PayrollCode.CanBeSold` flag to find the correct earning code. Multi-record creation for hours > default (e.g., selling 24 hours creates 3 � 8-hour earning records).

---

## GAP 307: Requirement Expiration Logic (Missing from Part 22)

```csharp
ExpireDateTime:
  if CalendarYear:
    return CompletedDate + 1 year ? Dec 31
  else:
    return CompletedDate + RequirementTerm years

RenewDateTime:
  if CalendarYear:
    return CompletedDate + 1 year ? Jan 1
  else:
    return ExpireDateTime - RenewDelayDays

CanRenew = RenewDateTime <= today
```

---

## GAP 308: BumpDate � Bulletin vs Crew Position (Missing from Part 15)

```csharp
Engineer/Yardman:
  if NO crew position (board position):
    if BulletinPositions:
      return AssignedDate + RequiredDays (with on-duty time based on assigned days)
    else:
      return Today (immediate)
  if crew position:
    Find on-duty time for the target day's assignment
    return AssignedDate + RequiredDays + on-duty time
default crafts:
  return DateTime.Now (immediate)
```

---

## GAP 309: Seniority.Create � Cascading Effects (Missing from Part 15)

```csharp
if state is Active:
  InactivateSeniority() � deactivates all other seniority records
  AssignRailroadPosition() � assigns employee to their new position
if RosterDate is future:
  StateID = 0 (pending)
```

Creating an active seniority triggers position reassignment for the employee.

---

## GAP 310: Seniority File Export Format (Missing from Part 38)

```
SWITCHMEN\t{yyyyMMdd}\t{00rank}    (for Yardman)
ENGINEERS\t{yyyyMMdd}\t{00rank}    (for Engineer)
{CRAFTNAME}\t{yyyyMMdd}\t{00rank}  (for all others)
```

Tab-delimited, rank zero-padded to 5 digits. Sent to AtHoc via `AtHocService.ProcessEmployeeMessage()` with craft name.

---

## GAP 311: Railroad.CreatePayrollHolidayRecords � File-Based Holiday Processing (Missing from Part 20)

```csharp
For each active employee on the holiday date:
  if ActiveCraft.ProcessPayroll AND employee.ProcessPayroll:
    Write .HR file to MvcApplication.inbound path
    File content: "{HolidayName} {Year}\t{EmpName}\t{RailroadCN}\t{RPEmployeeCN}\t{HolidayCN}"
```

Holiday payroll records created via file drop (`.HR` extension) to inbound folder, NOT direct DB insertion. Processed by external service.

---

## GAP 312: Shift.FirstCallingTime / LastCallingTime (Missing from Part 4)

```csharp
FirstCallingTime = date + first OnDutyTime.CallingTimeStart + 30 min + ShiftID seconds
LastCallingTime  = date + last  OnDutyTime.CallingTimeEnd   + 30 min + ShiftID seconds
```

ShiftID (1/2/3) added as seconds to prevent collisions between shifts. Midnight (23:59) is not extended by 30 min.

---

## GAP 313: Shift.GetNextShiftID � Rotation (Missing from Part 4)

```
"1" ? "2" ? "3" ? "1" (circular)
Default ? "0"
```

---

## GAP 314: RosterBoardPosition.GetAutomaticMarkUpDateTime (Missing from Part 14)

```csharp
muhrs = MarkOffCode.AutomaticMarkUpHours(craft)
if muhrs == 0: return DateTime.Now
return MarkOffDateTime + muhrs hours - seconds (strip seconds)
```

Board positions use this to calculate when automatic mark-up should fire.

---

## GAP 315: Mark-Off Code "NN" � Could Not Notify (Missing from Part 14)

Previously undocumented mark-off code. When a position change notification cannot be delivered:
- Employee gets `"NN"` mark-off
- When notification is later confirmed ? `"NN"` auto-deleted or marked up
- XB employees get daily position mark-off record updates
- Crew employees get on-duty mark-off record updates

Total mark-off codes: **22** (previously 21, adding `"NN"`).
# Part 62: Final Verification � Windows Service Business Logic

Gaps 316-330 covering the SADailyCallSheetService and SAImportPayrollService projects, which contain significant business logic not present in the main web application.

---

## GAP 316: MSMQ-Based Crew Position Creation Pipeline (Missing from Part 38)

The `SADailyCrewPositionService` receives MSMQ messages and creates crew positions:

```
Queue: DailyCrewPositionQueue
Message: "{DailyAssignmentCN},{RailroadPositionCN},{AssignmentDate},{ExtraBoardOnly},{CrewCN},{PositionCN}"

1. Create DailyCrewPosition record
2. Retry loop (10 attempts, 100ms sleep) to find the new record
3. Check for assigned employee on the RailroadPosition
4. Check for temporary assignments, training dates, hold-downs
5. Send MSMQ message to DailyOnDutyRecordQueue for each eligible employee
```

---

## GAP 317: Service-Side Seniority Move Check (Differs from Part 4 / Gap 296)

The service version differs from the web app version:

```csharp
// Web app (DailyAssignmentCrew):
senmove = employee.SeniorityMoves.FirstOrDefault(m => m.EffectiveDateTime.Date == assignmentDate);

// Service (SADailyCrewPositionService):
senmove = employee.SeniorityMoves.FirstOrDefault(m => m.SeniorityMoveAssignment == null);
// Then: willwork = senmove.EffectiveDateTime > assignmentDate + onDutyTime
```

Service checks for **unassigned** seniority moves (not date-specific). Also adds a `futuredate` check comparing EffectiveDateTime to assignment on-duty datetime.

---

## GAP 318: Service-Side On-Duty Record Creation Pipeline (Missing from Part 38)

```
Queue: DailyOnDutyRecordQueue
Message: "{DailyCrewPositionCN},{RailroadPoolEmployeeCN}"

1. Create DailyRailroadEmployeePositionRecord (status+position snapshot)
2. Create DailyCrewPositionOnDutyRecord
3. Send MSMQ message to DailyMarkOffRecordQueue for mark-off checks
```

---

## GAP 319: IsMarkedOffThisDateTime � Complex Mark-Off Overlap Detection (Missing from Part 14)

Service-side logic to determine if an on-duty record overlaps with a mark-off:

```
1. Check markoff datetime <= on-duty datetime
2. If auto-markup hours exist: check on-duty < markoff + hours
3. If currently on-duty:
   a. Without MO request: check off-duty > markoff (unless ApprovedByAgreement)
   b. With MO request: check off-duty > markoff (different date comparison)
4. If has markup record:
   a. NR/NN/SR codes: compare StartCallTime vs MarkUpDateTime
   b. Other codes: compare on-duty vs markoff + MarkUpHours
```

New code `"CR"` � Called Relief: special handling where mark-off AFTER on-duty datetime gets `Ignore = true`.

---

## GAP 320: Vacation Week ? Daily Code Conversion (Missing from Part 14)

`SV_MarkOffRecord.GetVacationWeekMarkOffCode()`:

```
if off-day ? return "VO" (Vacation Off-Day)
Count compensated hours used so far for this mark-off record

V1: max 40h / 5 paid days / 7 total days
V2: max 80h / 10 paid / 14 total
V3: max 120h / 15 paid / 21 total
V4: max 160h / 20 paid / 28 total
V5: max 200h / 25 paid / 35 total

if hours < max ? return original V-code
if paid days > max paid AND total days <= max total ? return "VO"
else ? return "EV" (Excess Vacation)
```

New mark-off codes: `"VO"` (Vacation Off-Day), `"EV"` (Excess Vacation). Total: **24 codes**.

---

## GAP 321: Comp Hours Calculation � Day Type Specific (Missing from Part 28)

| Code | Hours Calculation |
|---|---|
| CD | Always 8 hours |
| PD | Assignment ST hours for the day (YM always 8) |
| SD | Assignment ST hours for the day |
| VD | Assignment ST hours; XB YM gets 12 if all YM jobs are 12h that day |
| V1-V5 | Assignment ST hours per work day (skips off-days) |

All skip off-days by advancing to next work day. All capped at compensation account balance.

---

## GAP 322: Multi-Day Mark-Off Comp Hours Split (Missing from Part 28)

```csharp
GetDailyMarkOffCompHours():
  usedHours = sum of CompHours from all OTHER days of this mark-off
  balance = markOffRecord.CompHours - usedHours
  sthrs = calculate for this day's type
  return min(balance, sthrs)

GetTotalMarkOffCompHours():
  Called when mark-off extends past the first day (PD/SD/VD)
  Creates a NEW compensation account withdrawal for the additional day
```

First day uses `GetTotalMarkOffCompHours()`, subsequent days use `GetDailyMarkOffCompHours()` with running balance.

---

## GAP 323: Mark-Off Record Update � 3-Way Branch (Missing from Part 14)

`UpdateDailyCrewPositionOnDutyMarkOffRecord()` in the service:

```
if isMarkedOff AND (not tied-up OR code is "CR"):
  if markoff NOT deleted:
    Create DailyRailroadEmployeePositionMarkOffRecord
    Create DailyCrewPositionOnDutyMarkOffRecord
    if CR after on-duty ? set Ignore = true
    Adjust STDaysWorked and DaysWorked (-1)
  if markoff IS deleted:
    Delete compensation hours
    Remove mark-off records
    Remove off-duty record
    Remove DoNotFill
    Adjust STDaysWorked and DaysWorked (+1)
else (not marked off but HAS mark-off record):
  Delete compensation hours
  Remove all mark-off records
  Remove off-duty record
  Remove DoNotFill
  Adjust STDaysWorked and DaysWorked (+1)
```

---

## GAP 324: AFE Billing Record � Auto-Creation for MOW Temps (Missing from Part 29)

```csharp
CreateDailyOnDutyAFEBillingRecord():
  if Pool 50 (MOW) AND assignment is Recollectable:
    Find open temporary assignment with AFE record for this employee
    if found AND no AFE billing record exists:
      Create DailyOnDutyAFEBillingRecord with AFE number/description
      STBHours = temp assignment's StraightTimeHours
```

---

## GAP 325: ADP Import � Fixed-Width File Parsing (Missing from Part 38)

The `SAImportADPPayrollService` parses ADP response files (`PRPT1*.*`) with fixed-width columns:

```
Skip 2 bytes
ID: 5 chars | EmpNbr: 4 chars | Skip 43
ICC: 3 chars | Dept: 3 chars | Skip 12
TotalAmt: 9 chars (7.2) | Date: 6 chars (yyMMdd) | DayOfWeek: 1 char
JobWorked: 4 chars | JobPaid: 4 chars | Skip 3
Col1Hours: 8 chars | Col2Hours: 8 chars | Col3Hours: 8 chars | Col4Hours: 8 chars
Col1Amt: 10 chars | Col2Amt: 11 chars | Col3Amt: 11 chars | Col4Amt: 11 chars | Col5Amt: 9 chars
EmpName: 24 chars | Skip 43
Col3Code: 2 chars | Col4Code: 2 chars | Col5Code: 2 chars
```

Lines containing "DP1" are skipped (header).

---

## GAP 326: ADP Import � Earning Code Mapping (Missing from Part 38)

ADP response codes mapped to internal codes:

| ADP Code | Internal Code | Meaning |
|---|---|---|
| `"H"` | `"05"` | Holiday |
| `"M"` | `"65"` | Meal period |
| `"P"` | `"12"` | Personal day |
| `"S"` | `"03"` | Overtime |
| `"V"` | `"04"` / `"06"` | Vacation week / Vacation day |
| (has ST+OT) | `"01"` / `"02"` | Straight time / Overtime |

Codes `"14"`, `"15"`, `"16"` get 1 hour added to ST hours.

---

## GAP 327: ADP Import � Earning Record Matching (Missing from Part 38)

Three-pass matching to find the correct earning record:

```
Pass 1: Match by code + STHours (non-zero ST, STPaid == 0)
Pass 2: Match by code + OTHours (non-zero OT, OTPaid == 0)
Pass 3: Match by code + Amount  (non-zero amount, PaidAmount == 0)

Special: code "18" (meal period) � if 2 records, sort by STHours,
  compare amounts to assign first/last correctly
```

Matched records get `STPaid`, `OTPaid`, `PaidAmount`, `TotalPaid` updated.

---

## GAP 328: ADP Import � Department Auto-Correction (Missing from Part 16)

```csharp
if payrollRecord.ICCNumber != file.ICCNumber OR dept mismatch:
  Look up RailroadPayrollDepartment by ICC + dept
  Update payroll record's ICC, Dept, GL numbers
  Log to "Corrected Departments Report"
```

Second instance of self-healing FK correction (first was Gap 248 in trial processing).

---

## GAP 329: ADP Import � TimeSpan Second Rounding (Missing from Part 16)

```csharp
// ADP hours come back with fractional seconds
if hours.Seconds > 30:
  hours += (1 minute - seconds)  // round up
else:
  hours -= seconds  // round down
```

Applied to all 4 hour columns.

---

## GAP 330: Call Sheet Service � Mark-Off Record Refresh (Missing from Part 10)

Before creating a new call sheet, the service refreshes mark-off records:

```csharp
if NOT Pool 50 (MOW):
  For each assignment ? each crew position ? each on-duty record:
    UpdateDailyOnDutyMarkOffRecords()
```

Pool 50 (MOW) is excluded from automatic mark-off record refresh during call sheet creation.

---

## GAP 331: 3-Queue MSMQ Pipeline (Missing from Part 38)

Complete async pipeline:

```
Queue 1: DailyCrewPositionQueue    ? Creates DailyCrewPosition records
Queue 2: DailyOnDutyRecordQueue    ? Creates on-duty records
Queue 3: DailyMarkOffRecordQueue   ? Creates mark-off overlay records
```

Each queue feeds the next. All use `MessageQueueTransactionType.Automatic` with `PeekCompleted` async pattern.

---

## GAP 332: FileSystemWatcher for ADP Import (Missing from Part 38)

```csharp
filewatcher = new FileSystemWatcher(path, "PRPT1*.*");
filewatcher.Created += TriggerFileWatcherEvent;
```

ADP response files are detected via `FileSystemWatcher`, not polling. 5-second sleep after detection to allow copy completion. Unmatched records written to `.np` error file.

---

## GAP 333: Service-Side Earning Code � Pool 40 Mechanical Extensions (Missing from Part 16)

The service's `GetPayrollEarningCode()` adds Pool 40 rules NOT in the web app:

```
Pool 40 Mechanical:
  if same-shift duplicate on-duty ? no OT (same as web)
  NEW: if hangout employee + position is bulletined ? no OT
  NEW: if vacation relief AND mark-off contains "V" ? no OT
  NEW: if off-day + worked OT yesterday + consecutive day ? double time "20"
  NEW: off-day otherwise ? "22"
```

This is the **8th copy** of earning code determination logic.

---

## GAP 334: Service-Side Doubleheader � Pool 50 MOW Difference (Missing from Part 16)

```csharp
// Web app: if (!(lastrecord.AssignedEmployee && assigned))
// Service: Pool 50 MOW always checks 22:30 threshold
//          Other pools only check if NOT (both assigned to same position)

Pool 50: timeBetweenStarts < 22:30 ? always doubleheader
Others:  timeBetweenStarts < 22:30 ? doubleheader only if not both assigned
```

---

## GAP 335: UKG Import � CSV Format and Matching (Missing from Part 38)

```
Format: EmployeeNumber, PayDate, UKGEarningCode, Hours, Amount
Header: "Employee Number" (skipped)
```

Uses `UKGInterface` entity to map UKG earning codes to internal `PayrollCodeControlNumber`.
Same 3-pass matching as ADP (ST hours ? OT hours ? Amount).

Import path: `\\finance-svr\c$\Payroll Exports\UKG\Imports`

---

## GAP 336: Service STDaysWorked vs DaysWorked (Missing from Part 5)

```csharp
if lastRecord == null OR pay period boundary (1st/16th) OR different period:
  STDaysWorked = 1; DaysWorked = 1
else if last record was OT:
  STDaysWorked = same; DaysWorked += 1  // OT doesn't count as ST day
else:
  STDaysWorked += 1; DaysWorked += 1
```

---

## GAP 337: Service Trainee = Assigned (Missing from Part 22)

```csharp
if position.IsTraineePosition:
    assignedposctrlnbr = DailyCrewPosition.RailroadPositionControlNumber
```

Trainees treated as "assigned" for earning code purposes. Also excluded from OT in default pool logic.

---

## GAP 338: RemoveDoNotFill Cleanup Chain (Missing from Part 6)

```csharp
RemoveDoNotFillRecords():
  Remove all payroll records from on-duty records on the position
  Remove DailyCrewPositionOnDutyPayrollRecords
  Remove DailyCrewPositionDoNotFill record
```

