# Impl Spec: `feature/gap-daily-operations`

**Priority**: P0 – Core  
**Depends on**: Nothing (this is the foundation)  
**Depended on by**: mark-off-system, fra-compliance, vacancy-assignment, payroll-engine, and all Tier 2+ branches

## Overview

This branch builds the **daily position lifecycle** — the materialization of templates into
concrete daily records that employees are placed on-duty against. It is SA's `DailyAssignment` /
`DailyCrewPosition` / `DailyCrewPositionOnDutyRecord` / `DailyCrewPositionOffDutyRecord` entity
tree, re-architected as aggregates in CrewService.

---

## 1. Aggregate Design

### Aggregate 1: `ShiftInstance` (root) — Module: WorkManagement

The daily shift container. SA's `DailyAssignmentShift` equivalent.

```
ShiftInstance (aggregate root)
  └── PositionSlotInstance (child entity, owned collection)
```

**Why one aggregate**: A shift and its position slots are always created, validated, and
persisted together. You never create a position slot without its parent shift. The shift
is the consistency boundary — no two concurrent operations should be modifying the same
shift's slots simultaneously.

**Relationship to existing entities**:
- `ShiftInstance.WorkInstanceCtrlNbr` → FK to `WorkInstance` (existing, not owned)
- `WorkInstance` continues to exist as the day-level container; `ShiftInstance` is the shift-level subdivision
- `PositionSlotInstance.CrewPositionCtrlNbr` → FK to `CrewPosition` (existing, the template position)
- `PositionSlotInstance.IncumbentEmployeeCtrlNbr` → FK to `Employee` (existing, nullable — null means vacant)

### Aggregate 2: `OnDutyRecord` (root) — Module: Dispatching

The core on-duty tracking record. SA's `DailyCrewPositionOnDutyRecord` equivalent.

```
OnDutyRecord (aggregate root — standalone, not owned by ShiftInstance)
```

**Why separate aggregate**: The on-duty record has its own lifecycle (created when employee
reports, updated with off-duty time, triggers FRA checks, payroll records, billing records).
Multiple handlers from different modules react to it. It must be its own transaction boundary.

**Relationship to existing entities**:
- `OnDutyRecord.PositionSlotCtrlNbr` → FK to `PositionSlotInstance` (Aggregate 1)
- `OnDutyRecord.EmployeeCtrlNbr` → FK to `Employee` (existing)
- `OnDutyRecord.BookingCtrlNbr` → FK to `EmployeeBooking` (existing — the booking is created as a side effect of going on duty)
- Replaces the simple `EmployeeBooking` as the primary on-duty tracking mechanism; `EmployeeBooking` becomes the time-window reservation that the `OnDutyRecord` manages

### Aggregate 3: `OffDutyRecord` (root) — Module: Dispatching

The tie-up record. SA's `DailyCrewPositionOffDutyRecord` equivalent.

```
OffDutyRecord (aggregate root — standalone)
```

**Why separate from OnDutyRecord**: Off-duty has its own craft-specific rest calculation
pipeline and triggers independent side effects (FRA compliance check, board repositioning,
payroll tier update). It references the `OnDutyRecord` but is created in a separate operation.

**Relationship to existing entities**:
- `OffDutyRecord.OnDutyRecordCtrlNbr` → FK to `OnDutyRecord`
- `OffDutyRecord.EmployeeCtrlNbr` → FK to `Employee`

---

## 2. Entity Catalog

### `ShiftInstance` — WorkManagement module

| Property | Type | Source | Notes |
|----------|------|--------|-------|
| CtrlNbr | ControlNumber | PK | |
| WorkInstanceCtrlNbr | ControlNumber | FK → WorkInstance | The day-level parent |
| ShiftCode | string | SA `Shift.ShiftName` | E.g., "1", "2", "3" or tenant-defined |
| ShiftStartUtc | DateTime | Computed from template | |
| ShiftEndUtc | DateTime | Computed from template | |
| Status | string | Lifecycle | "Planned" → "Active" → "Completed" → "Cancelled" |
| IsComplete | bool | SA `DailyAssignmentShiftCompletion` | Replaces separate completion entity |
| CompletedAtUtc | DateTime? | | |
| Audit | AuditStamp | Existing VO | |

### `PositionSlotInstance` — WorkManagement module (child of ShiftInstance)

| Property | Type | Source | Notes |
|----------|------|--------|-------|
| CtrlNbr | ControlNumber | PK | |
| ShiftInstanceCtrlNbr | ControlNumber | FK → ShiftInstance | Parent aggregate root |
| CrewPositionCtrlNbr | ControlNumber | FK → CrewPosition | The template position this slot materializes |
| IncumbentEmployeeCtrlNbr | ControlNumber? | FK → Employee | Null = vacant |
| Status | string | Lifecycle | "Open" → "Filled" → "OnDuty" → "TiedUp" / "Annulled" / "DoNotFill" / "Skipped" |
| IsAnnulled | bool | SA `DailyCrewPositionAnnulment` | Replaces separate annulment entity |
| IsDoNotFill | bool | SA `DailyCrewPositionDoNotFill` | |
| IsSkipped | bool | SA `DailyCrewPositionSkip` | |
| AnnulmentReason | string? | | |
| DisplayOrder | int | From CrewPosition.DisplayOrder | Materialized for sort stability |
| Audit | AuditStamp | Existing VO | |

### `OnDutyRecord` — Dispatching module

| Property | Type | Source | Notes |
|----------|------|--------|-------|
| CtrlNbr | ControlNumber | PK | |
| PositionSlotCtrlNbr | ControlNumber | FK → PositionSlotInstance | |
| EmployeeCtrlNbr | ControlNumber | FK → Employee | |
| BookingCtrlNbr | ControlNumber? | FK → EmployeeBooking | Created as side effect |
| OnDutyTimeUtc | DateTime | When employee reports | |
| ScheduledOnDutyTimeUtc | DateTime | From shift schedule | For late-call detection |
| IsLateCall | bool | Computed | OnDutyTimeUtc > ScheduledOnDutyTimeUtc + threshold |
| LateCallAdjustedTimeUtc | DateTime? | SA +90 min rule | Configurable per tenant |
| PreviousRestHours | decimal | Computed from last OffDutyRecord | |
| ConsecutiveDays | int | Computed from prior OnDutyRecords | |
| Status | string | Lifecycle | "Called" → "OnDuty" → "TiedUp" |
| IsAssigned | bool | Has incumbent | vs. extra-board fill |
| Audit | AuditStamp | Existing VO | |

### `OffDutyRecord` — Dispatching module

| Property | Type | Source | Notes |
|----------|------|--------|-------|
| CtrlNbr | ControlNumber | PK | |
| OnDutyRecordCtrlNbr | ControlNumber | FK → OnDutyRecord | |
| EmployeeCtrlNbr | ControlNumber | FK → Employee | Denorm for query perf |
| OffDutyTimeUtc | DateTime | When employee released | |
| TotalTimeOnDutyMinutes | int | Computed: OffDutyTime - OnDutyTime | |
| RestHoursRequired | decimal | Craft-specific calculation | Base 10h + penalty |
| RestedAtUtc | DateTime | OffDutyTime + RestHoursRequired | |
| ConsecutiveDayRestedAtUtc | DateTime | Rest required to reset consecutive day counter | |
| ReleaseReason | string | E.g., "Normal", "Annulled", "FRA", "Emergency" | |
| Audit | AuditStamp | Existing VO | |

---

## 3. Domain Event Catalog

| Event | Published By | Subscribers (this branch) | Subscribers (other branches) |
|-------|-------------|--------------------------|------------------------------|
| `ShiftInstanceCreatedDomainEvent` | `ShiftInstance.Create()` | — | `gap-vacancy-assignment`: triggers vacancy evaluation for new slots |
| `PositionSlotStatusChangedDomainEvent` | `PositionSlotInstance` status setters | — | `gap-vacancy-assignment`: re-evaluate when slot becomes vacant; `gap-mark-off-system`: link mark-offs |
| `OnDutyRecordCreatedDomainEvent` | `OnDutyRecord.Create()` | Creates `EmployeeBooking` | `gap-fra-compliance`: FRA rest/consecutive check; `gap-payroll-engine`: earning record; `gap-mark-off-system`: link mark-offs |
| `OffDutyRecordCreatedDomainEvent` | `OffDutyRecord.Create()` | Updates `EmployeeBooking.EndUtc` | `gap-fra-compliance`: rest calculation, FRA record; `gap-payroll-engine`: tie-up payroll; `gap-vacancy-assignment`: board repositioning |
| `PositionSlotAnnulledDomainEvent` | `PositionSlotInstance.Annul()` | Auto-creates `OffDutyRecord` if on-duty | `gap-vacancy-assignment`: remove from vacancy pool |
| `CallSheetGeneratedDomainEvent` | `CallSheetGenerationService` | — | `gap-background-services`: log/notify |

### Event Flow: Daily Call Sheet Generation

```
Timer fires (gap-background-services)
  → CallSheetGenerationService.Generate(workAreaCtrlNbr, targetDate)
    → Load AssignmentTemplates for work area (existing)
    → For each template matching targetDate:
        → Create WorkInstance (existing entity)
        → For each configured shift:
            → Create ShiftInstance
              → raises ShiftInstanceCreatedDomainEvent
            → For each active CrewPosition on the template's attached Crew:
                → Create PositionSlotInstance (child of ShiftInstance)
                → If CrewIncumbency exists and employee is available:
                    → Set IncumbentEmployeeCtrlNbr, Status = "Filled"
                → Else:
                    → Status = "Open" (vacant)
    → raises CallSheetGeneratedDomainEvent
```

### Event Flow: On-Duty Placement

```
PlaceOnDutyCommand (API or vacancy assignment)
  → OnDutyPlacementService.Execute(positionSlotCtrlNbr, employeeCtrlNbr, onDutyTimeUtc)
    → Load PositionSlotInstance, validate status is "Filled" or "Open"
    → Calculate previousRestHours from employee's last OffDutyRecord
    → Calculate consecutiveDays from employee's recent OnDutyRecords
    → Create OnDutyRecord
      → raises OnDutyRecordCreatedDomainEvent
    → Update PositionSlotInstance.Status → "OnDuty"
      → raises PositionSlotStatusChangedDomainEvent
    → Create EmployeeBooking (existing entity) as time-window reservation
```

### Event Flow: Off-Duty (Tie-Up)

```
TieUpCommand (API)
  → TieUpService.Execute(onDutyRecordCtrlNbr, offDutyTimeUtc, releaseReason)
    → Load OnDutyRecord, validate status is "OnDuty"
    → Calculate totalTimeOnDuty, restHoursRequired (craft-specific strategy)
    → Create OffDutyRecord
      → raises OffDutyRecordCreatedDomainEvent
    → Update OnDutyRecord.Status → "TiedUp"
    → Update PositionSlotInstance.Status → "TiedUp"
    → Update EmployeeBooking.EndUtc
```

---

## 4. Configuration Model

### `ShiftDefinition` — WorkManagement module (new reference entity)

Replaces SA's `Shift` table. Defines the shifts available for a work area.

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| WorkAreaGroupCtrlNbr | ControlNumber | FK → DynamicGroup |
| ShiftCode | string | "1", "2", "3" or tenant-defined |
| DisplayName | string | "First Shift", "Day Shift", etc. |
| DefaultStartTime | TimeOnly | E.g., 07:00 |
| DefaultEndTime | TimeOnly | E.g., 15:00 |
| DisplayOrder | int | Circular sequence position |
| IsActive | bool | |

### `CraftOperationsPolicy` — Policies module (new typed policy entity)

Extends the existing policy pattern (`CraftDisplacementPolicy`, `BulletinPolicy`, etc.)
to cover daily operations behavior.

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| CraftCtrlNbr | ControlNumber | FK → Craft |
| LateCallThresholdMinutes | int | SA hard-codes 90; configurable per tenant |
| RestCalculationStrategy | string | "FRA" / "FixedHours" / "CraftConfigured" |
| FixedRestHours | decimal? | Used when strategy = FixedHours |
| ConsecutiveDayResetHours | decimal | SA hard-codes 24; configurable |
| DeleteConflictingNextShift | bool | SA pool 40 special case |
| AutoAnnulCreatesOffDuty | bool | SA: annulled position auto-creates tie-up |

This is a **typed entity** (like `CraftDisplacementPolicy`), not a generic attribute bag.
Typed entities give compile-time safety and are the pattern already established in the
Policies module. The `GroupAttributeDefinition/Value` system is reserved for truly
freeform tenant-specific metadata.

---

## 5. Commit Sequence

### Commit 1: `gap(daily-ops): add ShiftDefinition reference entity`

**Files**: Domain (entity), Persistence (config), Application (CRUD use cases), Presentation (gRPC)

- `ShiftDefinition` entity in WorkManagement module
- EF configuration + migration
- Basic CRUD application service + gRPC endpoints
- **No dependencies on other new entities**

### Commit 2: `gap(daily-ops): add ShiftInstance and PositionSlotInstance aggregates`

**Files**: Domain (entities, events), Persistence (config)

- `ShiftInstance` entity in WorkManagement module
- `PositionSlotInstance` as owned child of `ShiftInstance`
- Domain events: `ShiftInstanceCreatedDomainEvent`, `PositionSlotStatusChangedDomainEvent`, `PositionSlotAnnulledDomainEvent`
- EF configuration with `ShiftInstance` → `WorkInstance` FK, `PositionSlotInstance` → `CrewPosition` FK
- **Depends on**: Commit 1 (ShiftDefinition for ShiftCode reference)

### Commit 3: `gap(daily-ops): add OnDutyRecord and OffDutyRecord aggregates`

**Files**: Domain (entities, events), Persistence (config)

- `OnDutyRecord` entity in Dispatching module
- `OffDutyRecord` entity in Dispatching module
- Domain events: `OnDutyRecordCreatedDomainEvent`, `OffDutyRecordCreatedDomainEvent`
- EF configuration with FKs to `PositionSlotInstance`, `Employee`, `EmployeeBooking`
- **Depends on**: Commit 2 (PositionSlotInstance must exist)

### Commit 4: `gap(daily-ops): add CraftOperationsPolicy entity`

**Files**: Domain (entity), Persistence (config), Application (CRUD), Presentation (gRPC)

- `CraftOperationsPolicy` entity in Policies module
- EF configuration + migration
- Basic CRUD application service + gRPC endpoints
- **No dependencies on Commits 2–3** (can be done in parallel)

### Commit 5: `gap(daily-ops): add CallSheetGenerationService`

**Files**: Application (service), Domain (repository interfaces)

- `IShiftDefinitionRepository`, `IShiftInstanceRepository` in WorkManagement
- `CallSheetGenerationService` orchestrator:
  - Loads `AssignmentTemplate`s for a work area
  - Filters by target date (using `RecurrenceJson`)
  - Creates `WorkInstance` per matching template
  - Creates `ShiftInstance` per `ShiftDefinition`
  - Creates `PositionSlotInstance` per active `CrewPosition` on the attached `Crew`
  - Resolves incumbents via `CrewIncumbency`
- **Depends on**: Commits 1, 2

### Commit 6: `gap(daily-ops): add OnDutyPlacementService`

**Files**: Application (service), Domain (repository interfaces)

- `IOnDutyRecordRepository`, `IOffDutyRecordRepository` in Dispatching
- `OnDutyPlacementService`:
  - Validates position slot status
  - Calculates previous rest from last `OffDutyRecord`
  - Calculates consecutive days from recent `OnDutyRecord`s
  - Detects late call using `CraftOperationsPolicy.LateCallThresholdMinutes`
  - Creates `OnDutyRecord` + `EmployeeBooking`
  - Updates `PositionSlotInstance` status
- **Depends on**: Commits 2, 3, 4

### Commit 7: `gap(daily-ops): add TieUpService`

**Files**: Application (service, strategy interfaces)

- `IRestCalculationStrategy` interface
- `FraRestCalculationStrategy` (delegates to FRA module when it exists; stub for now returning 10h base)
- `FixedHoursRestCalculationStrategy`
- `TieUpService`:
  - Loads `OnDutyRecord`, validates status
  - Resolves `IRestCalculationStrategy` from `CraftOperationsPolicy`
  - Creates `OffDutyRecord` with computed rest
  - Updates `OnDutyRecord`, `PositionSlotInstance`, `EmployeeBooking`
- **Depends on**: Commits 3, 4

### Commit 8: `gap(daily-ops): add gRPC presentation for daily operations`

**Files**: Presentation (proto, service), Application (query handlers)

- Proto definitions for ShiftInstance, PositionSlotInstance, OnDutyRecord, OffDutyRecord
- Query endpoints: GetCallSheet(workArea, date), GetPositionSlots(shiftInstance), GetOnDutyRecords(date)
- Command endpoints: PlaceOnDuty, TieUp, AnnulPosition, SkipPosition, DoNotFill
- **Depends on**: Commits 5, 6, 7

### Commit 9: `gap(daily-ops): add unit tests for core lifecycle`

**Files**: UnitTests

- `CallSheetGenerationServiceTests` — template filtering, shift creation, incumbent resolution
- `OnDutyPlacementServiceTests` — rest calculation, consecutive days, late call detection
- `TieUpServiceTests` — rest strategy selection, status transitions
- `ShiftInstanceTests` — aggregate creation, position slot lifecycle
- **Depends on**: Commits 1–8

### Commit 10: `gap(daily-ops): add CrewOffDay and abolishment entities`

**Files**: Domain (entities, events), Persistence (config)

- `CrewOffDay` entity in WorkManagement — per-crew-position off-day schedule
- `AbolishmentRecord` entity in WorkManagement — tracks crew/assignment/position abolishment
  - `AbolishmentType`: "Crew" / "Assignment" / "Position"
  - `EffectiveDate`, `Reason`, `RestoredDate` (null if still abolished)
- `PositionSlotInstance` extended: skip slot creation for abolished positions and off-days
- `CallSheetGenerationService` updated: filter out abolished + off-day positions during generation
- Domain events: `PositionAbolishedDomainEvent`, `PositionRestoredDomainEvent`
- **Depends on**: Commits 2, 5

### Commit 11: `gap(daily-ops): add on-duty billing record entities`

**Files**: Domain (entities), Persistence (config)

Children of `OnDutyRecord` — per-on-duty billing and equipment tracking:

- `OnDutyBillingRecord` entity — AFE, zone, and miscellaneous billing
  - `BillingType`: "AFE" / "Zone" / "Miscellaneous"
  - `BillingCode`, `Amount`, `Hours`, `Description`
- `OnDutyLocomotiveRecord` entity — locomotive usage per on-duty
  - `LocomotiveNumber`, `LocomotiveTypeCode`, `Hours`
- `OnDutyMaterialRecord` entity — materials used per on-duty
  - `MaterialCode`, `CategoryCode`, `Quantity`, `UnitCost`
- EF config as owned children of `OnDutyRecord`
- **Depends on**: Commit 3

### Commit 12: `gap(daily-ops): add unit tests for off-day, abolishment, billing`

- Off-day filtering in call sheet generation
- Abolished position skip logic
- Billing record CRUD on OnDutyRecord
- **Depends on**: Commits 10, 11

---

## 6. Acceptance Scenarios

### Call Sheet Generation

**Scenario 1: Standard weekday generation**
```
GIVEN an AssignmentTemplate "Yard-101" with RecurrenceJson for Mon-Fri
  AND the template has an attached Crew with 3 active CrewPositions
  AND the work area has 3 ShiftDefinitions (shifts "1", "2", "3")
  AND today is Wednesday
WHEN CallSheetGenerationService.Generate(workAreaCtrlNbr, today)
THEN 1 WorkInstance is created with Status = "Planned"
  AND 3 ShiftInstances are created (one per shift definition)
  AND each ShiftInstance has 3 PositionSlotInstances
  AND slots with active CrewIncumbency have Status = "Filled"
  AND slots without incumbency have Status = "Open"
```

**Scenario 2: Weekend — no generation**
```
GIVEN the same AssignmentTemplate with RecurrenceJson for Mon-Fri
  AND today is Saturday
WHEN CallSheetGenerationService.Generate(workAreaCtrlNbr, today)
THEN no WorkInstance is created
```

**Scenario 3: Abolished position excluded**
```
GIVEN a CrewPosition with IsActive = false (abolished)
WHEN CallSheetGenerationService generates for the attached crew
THEN no PositionSlotInstance is created for that position
```

### On-Duty Placement

**Scenario 4: Normal on-duty**
```
GIVEN a PositionSlotInstance with Status = "Filled" and IncumbentEmployeeCtrlNbr = Employee A
  AND Employee A's last OffDutyRecord shows RestedAtUtc < now (fully rested)
WHEN OnDutyPlacementService.Execute(slotCtrlNbr, employeeA, now)
THEN an OnDutyRecord is created with Status = "OnDuty"
  AND PositionSlotInstance.Status = "OnDuty"
  AND an EmployeeBooking is created with StartUtc = now
  AND OnDutyRecordCreatedDomainEvent is raised
```

**Scenario 5: Late call detection**
```
GIVEN a PositionSlotInstance on a shift starting at 07:00
  AND CraftOperationsPolicy.LateCallThresholdMinutes = 90
WHEN OnDutyPlacementService.Execute(slotCtrlNbr, employee, 08:35)
THEN OnDutyRecord.IsLateCall = true
  AND OnDutyRecord.LateCallAdjustedTimeUtc = 10:05 (08:35 + 90 min)
```

**Scenario 6: Not rested — rejected**
```
GIVEN Employee A's last OffDutyRecord shows RestedAtUtc > now (not yet rested)
WHEN OnDutyPlacementService.Execute(slotCtrlNbr, employeeA, now)
THEN a DomainException is thrown: "Employee is not rested"
  AND no OnDutyRecord is created
```

### Tie-Up (Off-Duty)

**Scenario 7: Normal tie-up with FRA rest**
```
GIVEN an OnDutyRecord with OnDutyTimeUtc = 07:00, Status = "OnDuty"
  AND CraftOperationsPolicy.RestCalculationStrategy = "FRA"
  AND the employee worked 10 hours (no excess)
WHEN TieUpService.Execute(onDutyRecordCtrlNbr, 17:00, "Normal")
THEN an OffDutyRecord is created:
  - TotalTimeOnDutyMinutes = 600
  - RestHoursRequired = 10.0
  - RestedAtUtc = 03:00 next day (17:00 + 10h)
  AND OnDutyRecord.Status = "TiedUp"
  AND PositionSlotInstance.Status = "TiedUp"
  AND EmployeeBooking.EndUtc = 17:00
```

**Scenario 8: Excess service — penalty rest**
```
GIVEN an OnDutyRecord with OnDutyTimeUtc = 07:00
  AND the employee worked 13 hours (1 hour excess)
WHEN TieUpService.Execute(onDutyRecordCtrlNbr, 20:00, "Normal")
THEN OffDutyRecord.RestHoursRequired = 11.0 (10h base + 1h penalty)
  AND OffDutyRecord.RestedAtUtc = 07:00 next day (20:00 + 11h)
```

**Scenario 9: Annulled position auto tie-up**
```
GIVEN a PositionSlotInstance with Status = "OnDuty"
  AND an active OnDutyRecord for that slot
WHEN PositionSlotInstance.Annul("Assignment cancelled")
THEN PositionSlotAnnulledDomainEvent is raised
  AND TieUpService auto-creates an OffDutyRecord with ReleaseReason = "Annulled"
```
