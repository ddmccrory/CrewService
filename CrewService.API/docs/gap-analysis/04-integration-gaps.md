# 04 – Integration Gaps

## Overview

The legacy SA system integrates with several external systems.
CrewService currently has no external integrations beyond its own gRPC API.

---

## 1. AtHoc Mass Notification (Electronic Crew Calling)

**SA Location**: `AtHocService` static class, `SAAtHocMessageService` Windows Service  
**Status**: 🔴 Not implemented

### What SA Does

- OAuth2 authentication against AtHoc REST API
- **User sync**: Push phone numbers, email addresses, on-duty status, craft, and employee lifecycle events to AtHoc
- **Alert publishing**: Three alert templates — Assignment Call (accept/reject), Assignment Confirm (FYI), Assignment Move (FYI)
- **Response polling**: 5-second intervals for up to 6 minutes to retrieve accept/reject responses
- **Device mapping**: 7 device types (SMS, Mobile, Emergency, Work, Home phones + Work/Alert email)
- **Batched sending**: Groups of 15 alerts with 60-second pause between batches
- **Timer-driven**: `SAAssignmentCallService` fires at each calling time; `SAAssignmentOnDutyService` batch-syncs on-duty status

### CrewService Approach

| Commit Scope | Description |
|-------------|-------------|
| Domain: `NotificationRequest` entity | Abstract notification with template, recipients, payload, status |
| Domain: `NotificationResponse` entity | Response tracking (accepted/rejected/timeout) |
| Infrastructure: `ICrewNotificationProvider` interface | Abstraction over AtHoc (or any future notification system) |
| Infrastructure: AtHoc provider implementation | OAuth2 client, user sync, alert publish, response poll |
| Worker: `CrewCallingWorker` | `BackgroundService` replacing `SAAssignmentCallService` timer |

---

## 2. Microsoft Teams Webhooks

**SA Location**: `ApplicationUtilities.TeamsSendChatMessage()`  
**Status**: 🔴 Not implemented

### What SA Does

Routes messages to 5 Teams channels via Office 365 webhook URLs:

| Channel | Purpose |
|---------|---------|
| `SystemMessage` | Mark-offs, FRA violations, operational errors |
| `SystemSupport` | Service status, timer start/stop, diagnostics |
| `TieUpMessage` | Engineer/Yardman tie-up notifications (on-duty, off-duty, rested times) |
| `ECallMessage` | Electronic crew calling send/accept/reject |
| `TestMessage` | Demo/test environment routing |

### CrewService Approach

| Commit Scope | Description |
|-------------|-------------|
| Infrastructure: `IOperationalNotifier` interface | Abstraction for operational channel messaging |
| Infrastructure: Teams webhook implementation | HTTP POST to configurable webhook URLs per channel |
| Configuration: per-tenant webhook URLs | Store in `GroupAttributeValue` or dedicated config table |

---

## 3. ADP / UKG Payroll System Integration

**SA Location**: `SAImportPayrollService`, `PayrollUtilities`, `ProcessPayrollController`  
**Status**: 🔴 Not implemented

### What SA Does

**Export (SA → Payroll System)**:
- CSV generation with employee number, batch, earning codes, hours, amounts
- Separate formats for ADP (`ADPInterface`) and UKG (`UKGInterface`)

**Import (Payroll System → SA)**:
- `FileSystemWatcher` monitors `\\finance-svr` UNC shares for `PRPT1*.*` files
- CSV parsing: matches to existing payroll records by employee + period
- Creates interface records with paid amounts
- File lifecycle: Import → History (success) or Processing Error (failure)

### CrewService Approach

| Commit Scope | Description |
|-------------|-------------|
| Domain: `PayrollExportRecord` / `PayrollImportRecord` entities | Track export batches and import reconciliation |
| Application: `IPayrollExportFormatter` interface | Strategy for ADP vs. UKG CSV format |
| Application: `PayrollImportService` | Parse CSV, match to payroll records, create import records |
| Worker: `PayrollImportWorker` | `BackgroundService` with file polling (replacing FileSystemWatcher on UNC) |

---

## 4. MSMQ (Inter-Process Messaging)

**SA Location**: `Global.asax` queue producers, `SADailyCallSheetService` queue consumers  
**Status**: 🔴 Not implemented (MSMQ is deprecated)

### What SA Does

5 named private queues form the call-sheet pipeline:
1. `dailyassignmentshift`
2. `dailyassignment`
3. `dailycrewposition`
4. `dailyondutyrecord`
5. `dailymarkoffrecord`

Web app also produces messages; services consume and produce to the next queue.

### CrewService Approach

MSMQ should **not** be ported. Replacement options:

| Option | Fit |
|--------|-----|
| In-process `Channel<T>` pipeline | Best for single-process deployment (simplest) |
| Outbox + domain events | Already partially implemented (`OutboxMessage` entity exists) |
| Azure Service Bus / RabbitMQ | Best for distributed multi-process deployment |

The existing `OutboxMessage` / `IOutboxDispatcher` / `OutboxPublisherService` in CrewService.Infrastructure is the natural starting point.

---

## 5. IIS Application Pool Management

**SA Location**: `RestartApplicationPool` console app, `ApplicationUtilities.RecycleApplicationPool()`  
**Status**: 🔴 Not applicable

CrewService runs on Kestrel, not IIS. No equivalent needed. Health checks and graceful restart are handled by the hosting environment (Docker/systemd/Azure App Service).

---

## 6. Windows Event Log

**SA Location**: `EventLogger` (in both web app and SAClassLibrary)  
**Status**: 🔴 Not implemented (not needed in same form)

### What SA Does

All services and the web app write to Windows Event Log via `EventLog.WriteEntry()` with source `"Train Crew Reporting"`. Three levels: Information (200), Warning (800), Error (900).

### CrewService Approach

Already handled by ASP.NET Core's built-in `ILogger<T>` + structured logging. No gap here — just ensure operational events (FRA violations, mark-offs, timer status) are logged at appropriate levels with structured properties for querying.

---

## 7. File-Based Interface System

**SA Location**: `MarkOffRecord.CreateInterfaceFile()`, various controllers  
**Status**: 🔴 Not implemented

### What SA Does

Creates files on UNC shares to sync mark-off add/change/delete events with external systems. Files written to `\\sql-svr\SA\Message Queue\Inbound` with specific extensions (`.hr`, `.uv`, `.esr`).

### CrewService Approach

Replace file-based messaging with domain events published through the outbox:

| SA File Type | CrewService Replacement |
|-------------|------------------------|
| `.hr` (holiday records) | `HolidayRecordCreated` domain event |
| `.uv` (vacancy updates) | `VacancyImpactCreated` domain event (already exists) |
| `.esr` (employee status) | `EmploymentStatusChanged` domain event |
| Mark-off interface files | `AbsenceRequested` / `AbsenceApproved` domain events (already exist) |

The outbox pattern already in place (`OutboxMessage` → `OutboxPublisherService`) is the correct mechanism.

---

## Cross-References

- Business logic that drives these integrations: [03-business-logic-gaps.md](03-business-logic-gaps.md)
- Background workers hosting integration logic: [02-automated-process-gaps.md](02-automated-process-gaps.md)
- FRA compliance (drives excess-service reporting integration): [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md)
