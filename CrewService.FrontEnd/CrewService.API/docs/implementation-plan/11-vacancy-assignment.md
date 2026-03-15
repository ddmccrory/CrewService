# Phase 11 — Vacancy Assignment Engine

**Branch:** `feature/api-vacancy-assignment`
**Depends on:** Phase 7 (bulletins) + Phase 10 (mark-off triggers vacancies)

## Application Layer

| Component | Source |
|-----------|--------|
| `VacancyResolutionEngine` | `Application/VacancyAssignment/` |
| `ISkipRule` pipeline | `Application/VacancyAssignment/Rules/` |
| `IAssignmentStrategy` | `Application/VacancyAssignment/` |

## gRPC Service

| Service | Status |
|---------|--------|
| `VacancyAssignmentService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: vacancy resolution run RPCs` | Trigger/Query runs |
| 2 | `audit: skip rule configuration RPCs` | Configure skip rules per craft |
| 3 | `audit: assignment strategy RPCs` | Strategy selection |
| 4 | `fix: fill missing RPCs` | Wire stubs |
| 5 | `test: vacancy resolution end-to-end` | Vacancy → skip rules → assign |
