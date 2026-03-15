# Phase 15 — Reporting & Exports

**Branch:** `feature/api-reporting-exports`
**Depends on:** Phase 9 (payroll data to export)

## Domain Entities

| Entity | Source |
|--------|--------|
| `PayrollExportBatch` | `Modules/Payroll/PayrollExportBatch.cs` |
| `PayrollImportRecord` | `Modules/Payroll/PayrollImportRecord.cs` |

## Application Layer

| Component | Source |
|-----------|--------|
| Formatters (ADP/UKG) | `Application/ReportingExports/Formatters/` |
| Renderers | `Application/ReportingExports/Renderers/` |

## gRPC Service

| Service | Status |
|---------|--------|
| `ReportingExportsService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: payroll export batch RPCs` | Create/Query/Download |
| 2 | `audit: payroll import record RPCs` | Upload/Validate/Process |
| 3 | `audit: report generation RPCs` | Format selection, rendering |
| 4 | `fix: fill missing RPCs` | Wire stubs |
| 5 | `test: export batch lifecycle` | Generate → download |
