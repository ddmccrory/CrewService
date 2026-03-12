# 02 – Automated Process Gaps (Timers, File Watchers, Background Services)

## Overview

The legacy SA system runs **significant automation** via:
1. **17 categories of per-pool timers** in `Global.asax.cs`
2. **6 FileSystemWatchers** in `Global.asax.cs`
3. **4 Windows Service projects** (6 sub-services in the call sheet service alone)
4. **MSMQ** for inter-process communication between web app and services

CrewService currently has **zero** background automation. This is the single largest architectural gap.

### Recommended Modern Replacement

Instead of `System.Timers.Timer` in `Global.asax` and separate Windows Services, use:
- **`IHostedService` / `BackgroundService`** in ASP.NET Core for in-process workers
- **Channels / message broker** (e.g., Azure Service Bus, RabbitMQ, or in-process `Channel<T>`) to replace MSMQ
- **`IFileSystemWatcher` abstraction** or polling service for file-based triggers
- **Per-tenant scheduling** via a scheduler (Quartz.NET or custom `PeriodicTimer` wrapper) that reads schedule config from the database

## Legacy Timer Architecture (Global.asax)

### Per-Pool Timers (17 categories × N pools)

| # | Timer Category | SA Handler | Purpose | CrewService Status |
|---|----------------|------------|---------|-------------------|
| 1 | `DailyCallSheetTimers` | `CreateDailyCallSheet` | Auto-generate daily call sheets per shift schedule | 🔴 Not implemented |
| 2 | `DailyExtraBoardTimers` | `CreateDailyShiftExtraBoards` | Create daily extra board records for next shift | 🔴 Not implemented |
| 3 | `BulletinTimers` | `ProcessBulletins` | Auto-assign bulletined positions by seniority | 🔴 Not implemented |
| 4 | `SeniorityMoveTimers` | `ProcessSeniorityMoves` | Process seniority move requests | 🔴 Not implemented |
| 5 | `HangoutTimers` | `ProcessHangouts` | Auto-assign board/hangout positions | 🔴 Not implemented |
| 6 | `DailyReportTimers` | `CreateDailyReport` | Generate daily operational reports | 🔴 Not implemented |
| 7 | `DailyVacationWeekTimers` | `ProcessDailyVacationWeek` | Process vacation week assignments | 🔴 Not implemented |
| 8 | `DailyOffDayTimers` | `CreateDailyOffDays` | Create off-day records | 🔴 Not implemented |
| 9 | `DailyRailroadEmployeeStatusTimers` | `CreateDailyRailroadEmployeeStatusRecords` | Create daily employee status snapshot | 🔴 Not implemented |
| 10 | `HolidayTimers` | `ProcessHoliday` | Process holiday records | 🔴 Not implemented |
| 11 | `MarkOffRequestTimers` | `ProcessMarkOffRequests` | Process pending mark-off requests | 🔴 Not implemented |
| 12 | `RosterBoardMarkOffTimers` | `ProcessRosterBoardMarkOffs` | Auto mark-off roster board employees | 🔴 Not implemented |
| 13 | `RosterBoardHangoutTimers` | `ProcessRosterBoardHangouts` | Auto hangout roster board employees | 🔴 Not implemented |
| 14 | `PublishRailroadInformationTimers` | `PublishRailroadInformation` | Publish scheduled railroad information | 🔴 Not implemented |
| 15 | `CreateHolidayTimers` | `CreateHolidayPayrollRecords` | Create holiday payroll records | 🔴 Not implemented |
| 16 | `AtHocMessageTimers` | `CreateAtHocMessages` | Electronic crew calling via AtHoc | 🔴 Not implemented |
| 17 | (Call Sheet Service) | `SetDailyCallSheetTimer` | Separate Windows Service timer per pool for call sheet creation | 🔴 Not implemented |

### Key Timer Behaviors to Preserve

1. **Per-pool isolation**: Each pool has its own timer instance; one pool's failure doesn't block others
2. **Self-rescheduling**: After each execution, the timer calculates the next fire time from schedule config
3. **Pool-specific scheduling**: Different pool numbers (10, 20, 30, 40, 50, 60) have different timing rules
4. **Concurrency guards**: `Dictionary<long, bool>` flags prevent concurrent execution per pool (e.g., `CallSheetInProgress`, `PoolInProgress`)
5. **60-second startup delay**: All services wait before starting work

### Recommended CrewService Approach

```
BackgroundService per timer category:
  - DailyCallSheetWorker
  - ExtraBoardWorker
  - BulletinWorker
  - SeniorityMoveWorker
  - HangoutWorker
  - MarkOffRequestWorker
  - VacancyAssignmentWorker
  - ... (one per logical function)

Each worker:
  1. Loads active tenants + work areas (replacing pool iteration)
  2. Per work area, reads schedule config from DB
  3. Uses PeriodicTimer or Quartz trigger
  4. Scoped DbContext per execution
  5. Concurrency guard via distributed lock or DB flag
```

## File Watchers (Global.asax + Payroll Services)

### Web App File Watchers (Global.asax)

| # | Watcher | File Pattern | Purpose | CrewService Status |
|---|---------|-------------|---------|-------------------|
| 1 | `HolidayRecordWatcher` | `*.hr` | Process holiday record files | 🔴 Not implemented |
| 2 | `VacancyUpdateWatcher` | `*.uv` | Trigger vacancy re-evaluation | 🔴 Not implemented |
| 3 | `StatusUpdateWatcher` | `*.esr` | Process employee status record files | 🔴 Not implemented |
| 4-6 | Dev equivalents | Same patterns | Development environment versions | 🔴 Not applicable |

### Payroll Service File Watchers

| # | Service | Watch Path | File Pattern | Purpose | CrewService Status |
|---|---------|------------|-------------|---------|-------------------|
| 1 | `SAImportADPPayrollService` | `\\finance-svr\...\ADP\Imports` | `PRPT1*.*` | Import ADP payroll paid amounts | 🔴 Not implemented |
| 2 | `SAImportUKGPayrollService` | `\\finance-svr\...\UKG\Imports` | `PRPT1*.*` | Import UKG payroll paid amounts | 🔴 Not implemented |

### Key File Watcher Behaviors to Preserve

1. **5-second delay** after file creation before processing (wait for copy to complete)
2. **Sequential processing**: One file at a time per watcher
3. **Error quarantine**: Failed files moved to `Processing Error` subdirectory
4. **History archive**: Successful files moved to `History` subdirectory
5. **Processing guards**: Boolean flags prevent concurrent processing

### Recommended CrewService Approach

- Replace UNC file shares with **blob storage** or **SFTP** with polling
- Use `BackgroundService` with `FileSystemWatcher` or periodic polling
- Error files → dead-letter container/directory with metadata
- History → archive container with timestamp prefix

## Windows Services (4 Projects → Hosted Services)

### SADailyCallSheetService (6 sub-services)

| Sub-Service | Purpose | Message Queue | CrewService Status |
|-------------|---------|--------------|-------------------|
| `SADailyCallSheetService` | Creates daily call sheets on timer | Produces → `dailyassignmentshift` | 🔴 Not implemented |
| `SADailyAssignmentShiftService` | Creates DailyAssignmentShift records | Consumes → produces `dailyassignment` | 🔴 Not implemented |
| `SADailyAssignmentService` | Creates DailyAssignment records | Consumes → produces `dailycrewposition` | 🔴 Not implemented |
| `SADailyCrewPositionService` | Creates DailyCrewPosition records | Consumes → produces `dailyondutyrecord` | 🔴 Not implemented |
| `SADailyOnDutyRecordService` | Places employees on duty | Consumes → produces `dailymarkoffrecord` | 🔴 Not implemented |
| `SADailyOnDutyMarkOffRecordService` | Links mark-offs to on-duty records | Consumes (terminal) | 🔴 Not implemented |

**MSMQ Pipeline**: The 6 sub-services form a sequential pipeline via MSMQ queues. Each service consumes from one queue and produces to the next.

### SAImportPayrollService (2 sub-services)

| Sub-Service | Purpose | CrewService Status |
|-------------|---------|-------------------|
| `SAImportADPPayrollService` | ADP payroll file import via FileSystemWatcher | 🔴 Not implemented |
| `SAImportUKGPayrollService` | UKG payroll file import via FileSystemWatcher | 🔴 Not implemented |

### SAAtHocMessageService (2 sub-services)

| Sub-Service | Purpose | CrewService Status |
|-------------|---------|-------------------|
| `SAAssignmentCallService` | Electronic crew calling at scheduled times | 🔴 Not implemented |
| `SAAssignmentOnDutyService` | Batch on-duty status sync to AtHoc | 🔴 Not implemented |

### RestartApplicationPool

| Purpose | CrewService Status |
|---------|-------------------|
| IIS app pool restart utility | 🔴 Not applicable (Kestrel-based) |

### Recommended MSMQ Replacement

The call sheet service's 6-stage MSMQ pipeline can be replaced with:

**Option A**: In-process `Channel<T>` pipeline (simplest for single-process deployment)
```
CallSheetWorker → Channel<ShiftMessage> → ShiftWorker → Channel<AssignmentMessage> → ...
```

**Option B**: Message broker (Azure Service Bus / RabbitMQ) for distributed deployment
```
Topic: daily-operations
  Subscriptions: shift-created → assignment-created → position-created → onduty-created → markoff-linked
```

**Option C**: Saga/orchestration pattern with outbox
```
DailyCallSheetSaga orchestrates the pipeline steps via domain events + outbox
```

---

## Cross-References

- Business logic these workers execute: [03-business-logic-gaps.md](03-business-logic-gaps.md)
- External integrations driven by workers: [04-integration-gaps.md](04-integration-gaps.md)
- FRA compliance workers needed: [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md)
