# Phase 6 — Crews & Work Management

**Branch:** `feature/api-crews-work`
**Depends on:** Phase 3 (work area groups) + Phase 5 (crafts for PositionRole)

## Why Sixth

Crews are the operational units — a named group of positions that work together.
Positions are defined by PositionRoles (tied to crafts). AssignmentTemplates
define recurring jobs. WorkInstances are daily materializations of those templates.

## Domain Entities — Crews Module

| Entity | Location | Status |
|--------|----------|--------|
| `Crew` | `Modules/Crews/CrewEntities.cs` | ✅ Complete |
| `CrewPosition` | `Modules/Crews/CrewEntities.cs` | ✅ Complete |
| `CrewIncumbency` | `Modules/Crews/CrewEntities.cs` | ✅ Complete |
| `CrewAttachmentTemplate` | `Modules/Crews/CrewEntities.cs` | ✅ Complete |
| `CrewAttachmentInstance` | `Modules/Crews/CrewEntities.cs` | ✅ Complete |
| `ReliefCoverageRule` | `Modules/Crews/CrewEntities.cs` | ✅ Complete |
| `CrewOffDay` | `Modules/WorkManagement/CrewOffDay.cs` | ✅ Complete |

## Domain Entities — Work Management Module

| Entity | Location | Status |
|--------|----------|--------|
| `AssignmentTemplate` | `Modules/WorkManagement/WorkManagementEntities.cs` | ✅ Complete |
| `WorkInstance` | `Modules/WorkManagement/WorkManagementEntities.cs` | ✅ Complete |
| `PositionRole` | `Modules/WorkManagement/WorkManagementEntities.cs` | ✅ Complete |
| `PositionSlot` | `Modules/WorkManagement/WorkManagementEntities.cs` | ✅ Complete |
| `SlotRequirement` | `Modules/WorkManagement/WorkManagementEntities.cs` | ✅ Complete |
| `AbolishmentRecord` | `Modules/WorkManagement/AbolishmentRecord.cs` | ✅ Complete |

## gRPC Services

| Service | Status |
|---------|--------|
| `CrewsService` | ✅ Exists — audit all RPCs |
| `WorkManagementService` | ✅ Exists — audit all RPCs |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: crew CRUD + GetByHomeGroup + GetByType` | Crew entity RPCs |
| 2 | `audit: crew position CRUD + incumbency lifecycle` | Create/bind/unbind positions |
| 3 | `audit: crew attachment templates + relief coverage rules` | Template ↔ crew links |
| 4 | `audit: assignment template CRUD + work instance lifecycle` | Create/status transitions |
| 5 | `audit: position role + slot CRUD + slot requirements` | Bind/Unbind/SlotRequirement |
| 6 | `audit: abolishment records + crew off days` | Create/Restore/query |
| 7 | `fix: fill missing RPCs` | Wire stubs |
| 8 | `test: crew and work management lifecycle` | End-to-end crew → job → slot |

## Railroad Setup Story

> A CrewManager defines PositionRoles ("Engineer", "Conductor") tied to crafts.
> Creates Crew "Yard Crew A" in the "Yard" work area with two CrewPositions.
> Assigns incumbent employees via CrewIncumbency. Creates AssignmentTemplate
> "Morning Shift" with recurrence. Each day, a WorkInstance materializes from
> the template with PositionSlots. Employees are bound to slots.
