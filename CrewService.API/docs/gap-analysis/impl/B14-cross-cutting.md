# Impl Spec: `feature/gap-cross-cutting`

**Priority**: P1 – High (cross-cutting infrastructure used by multiple branches)  
**Depends on**: Nothing (foundation)  
**Depended on by**: B01, B02, B06, B07 (all branches that send notifications or reference locations)

## Overview

Adds cross-cutting infrastructure and reference entities that multiple branches depend on:
Teams webhook integration, operational location/zone model, AFE/billing reference data,
and change notification tracking.

---

## 1. Teams Webhook Integration

### `IOperationalNotifier` — Infrastructure (interface)

Abstraction for operational channel messaging. Used by B01 (FRA violations), B06
(crew calling), B07 (worker status), B02 (tie-up notifications).

```
IOperationalNotifier
  Task SendAsync(NotificationChannel channel, string subject, string body)
```

### `NotificationChannel` — enum

| Value | SA Equivalent | Purpose |
|-------|-------------|---------|
| SystemMessage | `SystemMessage` | Mark-offs, FRA violations, operational errors |
| SystemSupport | `SystemSupport` | Service status, timer start/stop, diagnostics |
| TieUp | `TieUpMessage` | Engineer/Yardman tie-up notifications |
| ElectronicCall | `ECallMessage` | Crew calling send/accept/reject |
| Test | `TestMessage` | Demo/test environment routing |

### `TeamsWebhookConfig` — TenantConfig module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber? | Null = railroad-wide default |
| Channel | string | NotificationChannel value |
| WebhookUrl | string | Office 365 connector URL |
| IsEnabled | bool | |

---

## 2. Reference Data via Dynamic Groups

Locations, zones, and other reference data use the existing `DynamicGroup` +
`GroupType` + `GroupAttributeDefinition/Value` system. No new entity classes needed.

### GroupType seed rows

| GroupType Name | Purpose |
|---------------|---------|
| Location | Operational locations (used by FRA segments, billing) |
| Zone | Geographic zones |
| AFE | Authorization for Expenditure codes |
| WorkCode | Work/job codes |
| Material | Material/supply codes |
| LocomotiveType | Locomotive type codes |

### GroupAttributeDefinition examples

| GroupType | Attribute | DataType | Notes |
|-----------|-----------|----------|-------|
| Location | IsHomeTerminal | bool | FRA consecutive-day rest location |
| Location | ZoneGroupCtrlNbr | ControlNumber | FK → Zone DynamicGroup |
| AFE | IsActive | bool | |
| Material | CategoryCode | string | |
| Material | UnitCost | decimal | |

---

## 3. Change Notification Tracking

### `ChangeNotification` — Dispatching module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| ChangeType | string | "Move", "Bulletin", "PositionChange" |
| EffectiveDate | DateOnly | |
| Description | string | |
| Status | string | "Pending" → "Applied" → "Cancelled" |
| CreatedAtUtc | DateTime | |

---

## 4. Commit Sequence

### Commit 1: `gap(xcut): add IOperationalNotifier + TeamsWebhookConfig`
### Commit 2: `gap(xcut): add Teams webhook implementation`
### Commit 3: `gap(xcut): seed GroupType rows for Location, Zone, AFE, WorkCode, Material, LocomotiveType`
### Commit 4: `gap(xcut): add ChangeNotification entity`
### Commit 5: `gap(xcut): add gRPC endpoints and unit tests`
