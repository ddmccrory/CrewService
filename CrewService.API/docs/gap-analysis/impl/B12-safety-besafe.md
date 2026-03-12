# Impl Spec: `feature/gap-safety-besafe`

**Priority**: P3 – Lower  
**Depends on**: Nothing  
**Depended on by**: Nothing

## Overview

Adds the BeSafe safety observation module. SA has this in `SAClassLibrary` only.
Standalone module for recording, categorizing, and resolving safety observations.

---

## 1. Entity Catalog

### `SafetyObservation` (root) — new Safety module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| ObserverEmployeeCtrlNbr | ControlNumber | FK → Employee |
| CategoryCode | string | Configurable (SA: BeSafeCategory) |
| AreaCode | string | Configurable (SA: BeSafeArea) |
| SubdivisionCode | string? | Configurable (SA: BeSafeSubdivision) |
| Description | string | |
| ObservedAtUtc | DateTime | |
| Status | string | "Open" → "ActionTaken" → "Resolved" |

### `SafetyObservationAction` (child of SafetyObservation)

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| ObservationCtrlNbr | ControlNumber | FK → parent |
| ActionDescription | string | |
| TakenByCtrlNbr | ControlNumber | FK → Employee |
| TakenAtUtc | DateTime | |

### `SafetyObservationResolution`

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| ObservationCtrlNbr | ControlNumber | FK → SafetyObservation |
| ResolutionDescription | string | |
| ResolvedByCtrlNbr | ControlNumber | FK → Employee |
| ResolvedAtUtc | DateTime | |

---

## 2. Commit Sequence

### Commit 1: `gap(safety): add SafetyObservation aggregate with Action child`
### Commit 2: `gap(safety): add SafetyObservationResolution entity`
### Commit 3: `gap(safety): add configurable category/area/subdivision reference data`
### Commit 4: `gap(safety): add gRPC endpoints and unit tests`

---

## 3. Acceptance Scenarios

**Scenario 1: Full observation lifecycle**
```
GIVEN an observer reports a safety concern
WHEN SafetyObservation.Create(category, area, description)
THEN Status = "Open"
  AND when an action is taken → Status = "ActionTaken"
  AND when resolved → Status = "Resolved"
```
