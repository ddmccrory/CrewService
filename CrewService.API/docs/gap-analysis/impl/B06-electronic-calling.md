# Impl Spec: `feature/gap-electronic-calling`

**Priority**: P1 – High  
**Depends on**: `gap-daily-operations` (OnDutyRecord, PositionSlotInstance), `gap-vacancy-assignment`  
**Depended on by**: background-services

## Overview

Replaces SA's `AtHocService` static class and `SAAtHocMessageService` Windows Service
with an abstracted notification provider behind `ICrewNotificationProvider`.

---

## 1. Aggregate Design

### `NotificationRequest` (root) — new Notifications module

```
NotificationRequest (aggregate root)
  └── NotificationResponse (child — accept/reject tracking)
```

- `NotificationRequest.PositionSlotCtrlNbr` → FK to PositionSlotInstance
- `NotificationRequest.EmployeeCtrlNbr` → FK to Employee

---

## 2. Entity Catalog

### `NotificationRequest`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| PositionSlotCtrlNbr | ControlNumber | FK → PositionSlotInstance |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| TemplateType | string | "AssignmentCall" / "AssignmentConfirm" / "AssignmentMove" |
| SentAtUtc | DateTime | |
| ExpiresAtUtc | DateTime | SentAtUtc + polling window (configurable, default 6 min) |
| Status | string | "Sent" → "Accepted" / "Rejected" / "Expired" / "Failed" |
| ExternalId | string? | Provider-specific ID (AtHoc alert ID) |

### `NotificationResponse` (child)

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| NotificationRequestCtrlNbr | ControlNumber | FK → parent |
| ResponseType | string | "Accept" / "Reject" |
| ReceivedAtUtc | DateTime | |
| DeviceType | string? | Phone, SMS, Email, etc. |

---

## 3. Domain Event Catalog

| Event | Published By | Subscribers |
|-------|-------------|-------------|
| `CrewCallSentDomainEvent` | NotificationRequest.Create() | Logging, Teams notification |
| `CrewCallRespondedDomainEvent` | NotificationResponse.Create() | If accepted → trigger OnDutyPlacement; if rejected → next candidate |
| `CrewCallExpiredDomainEvent` | Polling timeout | Next candidate in vacancy engine |

---

## 4. Configuration Model

### `NotificationProviderConfig` — Notifications module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| ProviderType | string | "AtHoc" / "Twilio" / "Mock" |
| ConfigJson | string | Provider-specific (OAuth2 URL, API key, etc.) |
| PollingIntervalSeconds | int | Default 5 |
| PollingTimeoutMinutes | int | Default 6 |
| BatchSize | int | Default 15 |
| BatchPauseSeconds | int | Default 60 |

---

## 5. Commit Sequence

### Commit 1: `gap(calling): add NotificationRequest/Response entities`
### Commit 2: `gap(calling): add ICrewNotificationProvider interface`
### Commit 3: `gap(calling): add AtHoc provider implementation`
### Commit 4: `gap(calling): add NotificationProviderConfig entity`
### Commit 5: `gap(calling): add CrewCallingService orchestrator`
### Commit 6: `gap(calling): add gRPC endpoints and unit tests`

---

## 6. Acceptance Scenarios

**Scenario 1: Successful crew call**
```
GIVEN a vacancy-assigned employee with phone number on file
  AND NotificationProviderConfig.ProviderType = "AtHoc"
WHEN CrewCallingService sends assignment call
THEN NotificationRequest created with Status = "Sent"
  AND polling begins at 5-second intervals
  AND when employee accepts → NotificationResponse with "Accept"
  AND CrewCallRespondedDomainEvent triggers OnDutyPlacement
```

**Scenario 2: Call timeout → next candidate**
```
GIVEN a sent NotificationRequest
WHEN PollingTimeoutMinutes (6 min) elapses with no response
THEN NotificationRequest.Status = "Expired"
  AND CrewCallExpiredDomainEvent triggers next candidate in vacancy engine
```
