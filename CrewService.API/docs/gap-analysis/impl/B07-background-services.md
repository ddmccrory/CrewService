# Impl Spec: `feature/gap-background-services`

**Priority**: P1 – High  
**Depends on**: All Tier 1 branches  
**Depended on by**: Nothing (this is the hosting layer)

## Overview

Replaces SA's 17 `Global.asax` timer categories, 6 file watchers, and 4 Windows Services
with ASP.NET Core `BackgroundService` workers using DB-driven scheduling.

---

## 1. Aggregate Design

### `WorkerSchedule` (root) — Infrastructure module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| WorkerType | string | "CallSheet", "Vacancy", "MarkOff", "FraCheck", etc. |
| CronExpression | string? | Optional cron-based schedule |
| NextFireUtc | DateTime? | Self-rescheduling (SA pattern preserved) |
| IsEnabled | bool | |
| LastRunUtc | DateTime? | |
| LastRunStatus | string? | "Success" / "Failed" |

### `WorkerExecutionLog` (root) — Infrastructure module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkerScheduleCtrlNbr | ControlNumber | FK → WorkerSchedule |
| StartedAtUtc | DateTime | |
| CompletedAtUtc | DateTime? | |
| Status | string | "Running" → "Success" / "Failed" |
| ErrorMessage | string? | |

### `ProcessingLock` (root) — Infrastructure module

Replaces SA's `Dictionary<long, bool>` in-memory guards with DB-level advisory locks.

| Property | Type | Notes |
|----------|------|-------|
| LockKey | string | PK — e.g., "CallSheet:{workAreaCtrlNbr}" |
| AcquiredByInstance | string | Host identifier |
| AcquiredAtUtc | DateTime | |
| ExpiresAtUtc | DateTime | Auto-release safety net |

---

## 2. Worker Catalog

Each worker is a `BackgroundService` that: loads active `WorkerSchedule` records,
acquires `ProcessingLock`, delegates to the appropriate application service, logs result.

| Worker Class | SA Equivalent | Delegates To |
|-------------|--------------|-------------|
| `DailyCallSheetWorker` | `DailyCallSheetTimers` + `SADailyCallSheetService` | `CallSheetGenerationService` (daily-ops) |
| `VacancyAssignmentWorker` | Vacancy timer in `Global.asax` | `VacancyResolutionEngine` (vacancy-assignment) |
| `MarkOffRequestWorker` | `MarkOffRequestTimers` | Mark-off processing service (mark-off-system) |
| `AutoMarkUpWorker` | Built into mark-off creation | Evaluates due `AbsenceMarkUp` records |
| `BulletinProcessingWorker` | `BulletinTimers` | Existing Bulletins module |
| `SeniorityMoveWorker` | `SeniorityMoveTimers` | Existing Policies module |
| `FraComplianceWorker` | Embedded in on-duty flow | Batch FRA checks for pending records |
| `CrewCallingWorker` | `SAAssignmentCallService` | `CrewCallingService` (electronic-calling) |
| `PayrollImportWorker` | `SAImportPayrollService` file watcher | File polling + `PayrollImportService` |
| `DailyReportWorker` | `DailyReportTimers` | Report generation (reporting-exports) |

---

## 3. Domain Event Catalog

| Event | Published By | Subscribers |
|-------|-------------|-------------|
| `WorkerExecutionStartedDomainEvent` | Worker base class | Logging |
| `WorkerExecutionCompletedDomainEvent` | Worker base class | Schedule update (NextFireUtc), logging |
| `WorkerExecutionFailedDomainEvent` | Worker base class | Alert notification (Teams) |
| `ProcessingLockConflictDomainEvent` | Lock acquisition | Logging (another instance holds the lock) |

---

## 4. Commit Sequence

### Commit 1: `gap(workers): add WorkerSchedule, WorkerExecutionLog, ProcessingLock entities`
### Commit 2: `gap(workers): add WorkerBase abstract BackgroundService`
### Commit 3: `gap(workers): add DailyCallSheetWorker`
### Commit 4: `gap(workers): add VacancyAssignmentWorker`
### Commit 5: `gap(workers): add MarkOffRequestWorker and AutoMarkUpWorker`
### Commit 6: `gap(workers): add remaining workers (Bulletin, SeniorityMove, FRA, Calling)`
### Commit 7: `gap(workers): add PayrollImportWorker with file polling`
### Commit 8: `gap(workers): add call sheet pipeline wiring via domain events + outbox`
- Wire the generation pipeline using existing outbox pattern (OutboxMessage/IOutboxDispatcher)
- `CallSheetWorker` → shift creation → position creation → on-duty placement → mark-off link
- Each stage publishes domain events; next stage subscribes via outbox dispatcher
- Uses existing `OutboxMessage` / `IOutboxDispatcher` infrastructure
### Commit 9: `gap(workers): add gRPC endpoints for schedule management`
### Commit 10: `gap(workers): add unit tests`

---

## 5. Acceptance Scenarios

**Scenario 1: DB-driven schedule fires**
```
GIVEN WorkerSchedule for "CallSheet" with NextFireUtc = now, IsEnabled = true
WHEN DailyCallSheetWorker evaluates schedule
THEN ProcessingLock is acquired
  AND CallSheetGenerationService.Generate() is called
  AND WorkerExecutionLog is created with Status = "Success"
  AND WorkerSchedule.NextFireUtc is recalculated for next shift
```

**Scenario 2: Lock prevents concurrent execution**
```
GIVEN ProcessingLock "CallSheet:{workArea}" held by instance A
WHEN instance B tries to acquire the same lock
THEN acquisition fails, ProcessingLockConflictDomainEvent is raised
  AND instance B skips this cycle
```

**Scenario 3: Failed execution with auto-release**
```
GIVEN a worker crashes mid-execution
WHEN ProcessingLock.ExpiresAtUtc is reached
THEN the lock is available for the next instance
  AND WorkerExecutionLog shows Status = "Failed"
```
