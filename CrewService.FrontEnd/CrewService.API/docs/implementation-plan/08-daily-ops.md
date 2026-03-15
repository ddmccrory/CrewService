# Phase 8 — Daily Operations & Dispatching

**Branch:** `feature/api-daily-ops`
**Depends on:** Phase 6 (work instances/slots) + Phase 7 (boards)

## Why Eighth

Daily operations are the real-time execution layer. ShiftDefinitions configure
shift patterns. ShiftInstances are daily materializations. PositionSlotInstances
track who is actually on each position for a given shift. Dispatching entities
handle projections, decision logs, overrides, and bookings.

## Domain Entities

| Entity | Source |
|--------|--------|
| `ShiftDefinition` | `Modules/WorkManagement/ShiftDefinition.cs` |
| `ShiftInstance` | `Modules/WorkManagement/ShiftInstance.cs` |
| `PositionSlotInstance` | `Modules/WorkManagement/PositionSlotInstance.cs` |
| `OnDutyRecord` | `Modules/Dispatching/OnDutyRecord.cs` |
| `OffDutyRecord` | `Modules/Dispatching/OffDutyRecord.cs` |
| `DailyEmployeeStatusRecord` | `Modules/Dispatching/DailyEmployeeStatusRecord.cs` |
| `DispatchProjection` | `Modules/Dispatching/DispatchingEntities.cs` |
| `DispatchDecisionLog` | `Modules/Dispatching/DispatchingEntities.cs` |
| `DispatchOverride` | `Modules/Dispatching/DispatchingEntities.cs` |
| `EmployeeBooking` | `Modules/Dispatching/DispatchingEntities.cs` |
| `ChangeNotification` | `Modules/Dispatching/ChangeNotification.cs` |
| `OnDutyBilling*` | `Modules/Dispatching/OnDutyBillingEntities.cs` |
| `VacancyResolutionRun` | `Modules/Dispatching/VacancyResolutionRun.cs` |

## gRPC Services

| Service | Status |
|---------|--------|
| `DailyOperationsService` | ✅ Exists — audit |
| `DispatchingService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: shift definition CRUD + GetByWorkArea` | Define shift patterns |
| 2 | `audit: shift instance lifecycle RPCs` | Create/Activate/Complete/Cancel + position slots |
| 3 | `audit: position slot instance operations` | Fill/MarkOnDuty/MarkTiedUp/Annul/Skip/DoNotFill |
| 4 | `audit: on-duty/off-duty record RPCs` | Placement and tie-up |
| 5 | `audit: dispatch projection + decision log RPCs` | Projection creation, log queries |
| 6 | `audit: dispatch override RPCs` | Create/Approve/Reject |
| 7 | `audit: employee booking + daily status RPCs` | Booking lifecycle |
| 8 | `fix: fill missing RPCs` | Wire stubs |
| 9 | `test: shift → slot → on-duty → tie-up flow` | Daily operations lifecycle |
