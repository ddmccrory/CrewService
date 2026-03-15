# Phase 5 — Seniority (Craft → Roster → Seniority)

**Branch:** `feature/api-seniority`
**Depends on:** Phase 3 (Craft.DynamicGroupCtrlNbr) + Phase 4 (Seniority.EmployeeCtrlNbr)

## Why Fifth

Crafts define labor classifications (Engineer, Conductor). Rosters organize
employees within a craft by seniority. Seniority records place employees on
rosters with a rank. This data drives crew assignment, bulletins, vacancy
resolution, and payroll — nearly everything downstream.

## Domain Entities

| Entity | Location | Status |
|--------|----------|--------|
| `Craft` | `Models/Seniority/Craft.cs` | ✅ Complete (rich config properties) |
| `Roster` | `Models/Seniority/Roster.cs` | ✅ Complete |
| `Seniority` | `Models/Seniority/Seniority.cs` | ✅ Complete |
| `SeniorityState` | `Models/Seniority/SeniorityState.cs` | ✅ Complete |

## Repositories

| Interface | Status |
|-----------|--------|
| `ICraftRepository` | ✅ + `GetByDynamicGroupCtrlNbrAsync` |
| `IRosterRepository` | ✅ + `GetByCraftCtrlNbrAsync` |
| `ISeniorityRepository` | ✅ + `GetByRosterCtrlNbrAsync` / `GetByEmployeeCtrlNbrAsync` |
| `ISeniorityStateRepository` | ✅ Base CRUD |

## gRPC Services

| Service | Status |
|---------|--------|
| `CraftService` | ✅ Exists — audit |
| `RosterService` | ✅ Exists — audit |
| `SeniorityService` | ✅ Exists — audit |
| `SeniorityStateService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: verify craft service CRUD + GetByDynamicGroup` | All 15+ craft properties |
| 2 | `audit: verify roster service CRUD + GetByCraft` | Training/ExtraBoard/OvertimeBoard flags |
| 3 | `audit: verify seniority service CRUD + GetByRoster + GetByEmployee` | Rank, state, canTrain |
| 4 | `audit: verify seniority state service` | Reference data CRUD |
| 5 | `fix: fill missing RPCs` | Wire stubs |
| 6 | `test: craft-roster-seniority hierarchy` | Create craft → roster → place employees |

## Railroad Setup Story

> A CraftManager creates Craft "Engineer" linked to the "Yard" work area group.
> Creates Roster "Yard Engineers" under that craft. Adds Seniority records
> placing employees on the roster with ranks (1=most senior). Sets craft
> policies: autoMarkUp, markOffHours, requiredRestHours, processPayroll, etc.
