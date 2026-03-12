# Impl Spec: `feature/gap-railroad-information`

**Priority**: P3 – Lower  
**Depends on**: Nothing  
**Depended on by**: Nothing

## Overview

Adds railroad information records (bulletins, notices, operational messages) with
publish/cancel/close lifecycle and employee read-receipt tracking.

---

## 1. Entity Catalog

### `RailroadInformation` (root) — new RailroadInfo module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| InformationType | string | Configurable type code |
| Subject | string | |
| Body | string | |
| Status | string | "Draft" → "Published" → "Closed" / "Cancelled" |
| PublishedAtUtc | DateTime? | |
| ClosedAtUtc | DateTime? | |

### `RailroadInformationReadReceipt`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| InformationCtrlNbr | ControlNumber | FK → RailroadInformation |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| ReadAtUtc | DateTime | |

---

## 2. Commit Sequence

### Commit 1: `gap(info): add RailroadInformation entity with lifecycle`
### Commit 2: `gap(info): add RailroadInformationReadReceipt entity`
### Commit 3: `gap(info): add publish timer integration (via WorkerSchedule)`
### Commit 4: `gap(info): add gRPC endpoints and unit tests`

---

## 3. Acceptance Scenarios

**Scenario 1: Publish and track reads**
```
GIVEN a RailroadInformation in "Draft" status
WHEN published
THEN Status = "Published", PublishedAtUtc set
  AND when Employee A views it → ReadReceipt created
```
