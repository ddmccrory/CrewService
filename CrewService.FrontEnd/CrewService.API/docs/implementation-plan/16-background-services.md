# Phase 16 — Background Services

**Branch:** `feature/api-background-services`
**Depends on:** Phases 8–13 (workers process data from all operational modules)

## Domain Entities

| Entity | Source |
|--------|--------|
| `WorkerEntities` | `Modules/Infrastructure/WorkerEntities.cs` |

## Application Layer

| Component | Source |
|-----------|--------|
| `WorkerBase` | `Application/BackgroundWorkers/` |
| Worker implementations | `Application/BackgroundWorkers/Workers/` |

## gRPC Service

| Service | Status |
|---------|--------|
| `BackgroundServicesService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: worker schedule + execution log RPCs` | Schedule/Query/Enable/Disable |
| 2 | `audit: processing lock RPCs` | Acquire/Release/Query |
| 3 | `audit: worker status + health RPCs` | Status queries |
| 4 | `fix: fill missing RPCs` | Wire stubs |
| 5 | `test: worker lifecycle` | Schedule → execute → log |
