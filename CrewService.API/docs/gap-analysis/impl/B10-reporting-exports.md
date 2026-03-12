# Impl Spec: `feature/gap-reporting-exports`

**Priority**: P2 – Medium  
**Depends on**: `gap-payroll-engine`  
**Depended on by**: Nothing

## Overview

Adds payroll CSV export (ADP/UKG formats), payroll import reconciliation, and daily
operational report generation. SA: `ADPInterface`, `UKGInterface`, PDF generation.

---

## 1. Aggregate Design

### `PayrollExportBatch` (root) — Payroll module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| PayrollRunCtrlNbr | ControlNumber | FK → PayrollRun |
| ExportFormat | string | "ADP" / "UKG" |
| GeneratedAtUtc | DateTime | |
| RecordCount | int | |
| FilePath | string? | Output location |

### `PayrollImportRecord` (root) — Payroll module

| Property | Type | Notes |
|----------|------|-------|
| CtrlNbr | ControlNumber | PK |
| SourceFile | string | Original filename |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| PayrollRecordCtrlNbr | ControlNumber? | FK → matched PayrollRecord |
| PaidAmount | decimal | |
| ImportedAtUtc | DateTime | |
| MatchStatus | string | "Matched" / "Unmatched" / "Error" |

---

## 2. Commit Sequence

### Commit 1: `gap(exports): add IPayrollExportFormatter interface and ADP/UKG implementations`
### Commit 2: `gap(exports): add PayrollExportBatch entity and export service`
### Commit 3: `gap(exports): add PayrollImportRecord entity and import service`
### Commit 4: `gap(exports): add daily report generation service`
### Commit 5: `gap(exports): add PDF report generation`
- `IReportRenderer` interface with PDF implementation
- Payroll earning statements, daily operational reports
- Replaces SA's iText-based generation
### Commit 6: `gap(exports): add gRPC endpoints and unit tests`

---

## 3. Acceptance Scenarios

**Scenario 1: ADP export**
```
GIVEN a locked PayrollRun with 50 PayrollRecords
WHEN PayrollExportService.Export(runCtrlNbr, "ADP")
THEN CSV generated in ADP format with 50 rows
  AND PayrollExportBatch created with RecordCount = 50
```

**Scenario 2: Payroll import match**
```
GIVEN an imported CSV line with employee "12345" and period "2025-07-01"
WHEN PayrollImportService processes the file
THEN PayrollImportRecord matched to existing PayrollRecord
  AND PaidAmount is recorded
```
