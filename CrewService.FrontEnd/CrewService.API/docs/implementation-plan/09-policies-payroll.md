# Phase 9 — Policies & Payroll

**Branch:** `feature/api-policies-payroll`
**Depends on:** Phase 5 (crafts) + Phase 8 (duty records drive payroll)

## Why Ninth

Craft operations policies control how mark-offs, mark-ups, and rest work per
craft. Pay rates, earning code rules, and holiday definitions drive payroll
calculations. These must be configured before daily payroll processing runs.

## Domain Entities

| Entity | Source |
|--------|--------|
| `CraftOperationsPolicy` | `Modules/Policies/CraftOperationsPolicy.cs` |
| `PolicyEntities` (additional) | `Modules/Policies/PolicyEntities.cs` |
| `PayRate` | `Modules/Payroll/PayRate.cs` |
| `EarningCodeRule` | `Modules/Payroll/EarningCodeRule.cs` |
| `PayrollEntities` | `Modules/Payroll/PayrollEntities.cs` |
| `Holiday` | `Modules/Payroll/Holiday.cs` |
| `HolidayQualificationRule` | `Modules/Payroll/HolidayQualificationRule.cs` |
| `HolidayPayrollRecord` | `Modules/Payroll/HolidayPayrollRecord.cs` |
| `RailroadHolidaySelection` | `Modules/HolidayManagement/RailroadHolidaySelection.cs` |

## gRPC Services

| Service | Status |
|---------|--------|
| `PoliciesService` | ✅ Exists — audit |
| `PayrollService` | ✅ Exists — audit |
| `PayrollEngineService` | ✅ Exists — audit |
| `HolidayManagementService` | ✅ Exists — audit |
| `HolidayPayrollService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: craft operations policy CRUD` | Per-craft operational rules |
| 2 | `audit: pay rate + earning code rule CRUD` | Rate definitions |
| 3 | `audit: payroll record lifecycle RPCs` | Create/process/approve |
| 4 | `audit: holiday + qualification rule + selection RPCs` | Holiday setup |
| 5 | `audit: holiday payroll processing RPCs` | Holiday pay generation |
| 6 | `fix: fill missing RPCs` | Wire stubs |
| 7 | `test: policy and payroll configuration flow` | End-to-end |
