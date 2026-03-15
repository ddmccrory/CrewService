# Phase 2 — Parent & Railroad

**Branch:** `feature/api-parent-railroad`
**Depends on:** Phase 1 (a ParentAdmin must exist)

## Why Second

A `Parent` is the top-level tenant (e.g., "Genesee & Wyoming"). `Railroad`s belong
to a Parent. Everything downstream — groups, crafts, employees — is scoped to a
Railroad. The ParentAdmin from Phase 1 creates these.

## Domain Entities

| Entity | Location | Status |
|--------|----------|--------|
| `Parent` | `Models/Parents/Parent.cs` | ✅ Complete (Create/Update/Delete + Railroads nav) |
| `Railroad` | `Models/Railroads/Railroad.cs` | ✅ Complete (Create/Update/Delete) |
| `PayrollTier` | `Models/Railroads/PayrollTier.cs` | ✅ Complete |

## Repositories

| Interface | Location | Status |
|-----------|----------|--------|
| `IParentRepository` | `Modules/Employees/IEmployeesRepositories.cs` | ✅ Defined |
| `IRailroadRepository` | `Modules/Employees/IEmployeesRepositories.cs` | ✅ Defined |
| `IPayrollTierRepository` | `Modules/Employees/IEmployeesRepositories.cs` | ✅ Defined |

## gRPC Services

| Service | Location | Status |
|---------|----------|--------|
| `ParentService` | `Presentation/Services/ParentService.cs` | ✅ Exists — audit |
| `RailroadService` | `Presentation/Services/RailroadService.cs` | ✅ Exists — audit |
| `PayrollTierService` | `Presentation/Services/PayrollTierService.cs` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: verify parent proto covers full CRUD + list-with-railroads` | Compare RPCs to domain |
| 2 | `audit: verify railroad proto covers CRUD + GetByParent` | Compare RPCs to domain |
| 3 | `audit: verify payroll tier proto covers CRUD + GetByDynamicGroup` | Compare RPCs to domain |
| 4 | `fix: fill any missing RPC implementations` | Wire stubs |
| 5 | `test: parent and railroad lifecycle tests` | Create parent → add railroad → update → delete |

## Railroad Setup Story

> Jane (ParentAdmin) calls `CreateParent(name="Port of Tampa Bay")`, then
> `CreateRailroad(parentCtrlNbr=X, rrMark="PTRA", name="PTRA Railroad")`.
> She now has a tenant shell. Next she configures its organizational structure.
