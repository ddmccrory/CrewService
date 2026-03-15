# Phase 10 — Absence & Mark-Off

**Branch:** `feature/api-absence-markoff`
**Depends on:** Phase 9 (policies govern mark-off/mark-up behavior)

## Domain Entities

| Entity | Source |
|--------|--------|
| `AbsenceCode` | `Modules/AbsenceVacancy/AbsenceCode.cs` |
| `AbsenceCodeCraftOverride` | `Modules/AbsenceVacancy/AbsenceCodeCraftOverride.cs` |
| `AbsenceVacancyEntities` | `Modules/AbsenceVacancy/AbsenceVacancyEntities.cs` |
| `CompensationBalance` | `Modules/AbsenceVacancy/CompensationBalance.cs` |

## gRPC Services

| Service | Status |
|---------|--------|
| `MarkOffService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: absence code CRUD + craft overrides` | Reference data setup |
| 2 | `audit: absence request lifecycle RPCs` | Submit/Approve/Deny/Cancel |
| 3 | `audit: compensation balance RPCs` | Balance tracking |
| 4 | `audit: mark-off/mark-up operational RPCs` | Mark off → impact board → mark up |
| 5 | `fix: fill missing RPCs` | Wire stubs |
| 6 | `test: mark-off lifecycle` | Request → approve → board impact → mark-up |
