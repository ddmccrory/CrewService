# Implementation Plan: Call Sheet Generation (Per-Shift, Manual)

## Scope

Implement manual call sheet generation on a **per-shift** basis. Given a work area, shift definition, and target date, produce a call sheet showing every assignment and crew position scheduled for that shift. All positions will be **Open** (vacant) — no incumbent prefill, no extra board vacancy filling.

### What a Call Sheet Is

The call sheet is the core daily operational document. It is a **point-in-time snapshot** listing every position that must be staffed for a specific shift on a specific date. All reference data (department name, assignment code/name, craft role name) is denormalized onto the call sheet entities so the record is self-contained and does not change if the underlying configuration is modified later.

### Display Hierarchy

```
Call Sheet — Work Area X, Date Y
└── Department: Transportation
    └── Shift: 1st (0700–1500) "First Trick"
        ├── Assignment TY-101 "Pool Turn 101"
        │   ├── Position 1: Engineer — Open
        │   └── Position 2: Conductor — Open
        └── Assignment TY-102 "Pool Turn 102"
            ├── Position 1: Engineer — Open
            └── Position 2: Conductor — Open
```

### Generation Granularity

Each shift is generated **independently**. A dispatcher generates the 1st shift call sheet, and later (or a different dispatcher) generates the 2nd shift. They are not batch-created for a whole date.

### Key Decisions

| Decision | Resolution |
|----------|-----------|
| Snapshot approach | **Denormalize everything** — department name, shift display name, assignment code/name, craft role name are all stored on the call sheet entities |
| Empty shifts | **Skip** — if a shift definition has no assignments matching the target date's day-of-week, return an error rather than creating an empty ShiftInstance |
| Idempotency | **Error** — if a call sheet already exists for the same work area + shift + date, return an error. A separate "Regenerate" action can be added later |
| WorkInstance lifecycle | **Lazy creation** — the WorkInstance (per work area + date container) is created on first shift generation and reused for subsequent shifts on the same date |
| Incumbents | **None** — all PositionSlotInstances will have status "Open". CrewIncumbency is queried but will return empty |
| Extra boards | **Not implemented** — out of scope for this phase |
| Department | **Involved** — department is a grouping level on the call sheet; denormalized from ShiftDefinition onto ShiftInstance |

---

## Branch Strategy

**Feature branch:** `feature/call-sheet-generation`
Created from: `main`

### Commit Plan

| # | Commit | Layer | Description |
|---|--------|-------|-------------|
| 1 | `feat(domain): add snapshot fields to ShiftInstance and PositionSlotInstance` | Domain | Add DepartmentCtrlNbr, DepartmentName, ShiftDisplayName to ShiftInstance. Add AssignmentCtrlNbr, AssignmentCode, AssignmentName, CraftRoleName to PositionSlotInstance. Update Create() factories and AddPositionSlot(). |
| 2 | `feat(application): enrich DTOs and update generation service` | Application | Enrich AssignmentDto with DepartmentCtrlNbr, AssignmentCode, AssignmentName. Enrich CrewPositionDto with CraftRoleName. Revise CallSheetGenerationService to per-shift generation with lazy WorkInstance creation, duplicate check, and error on empty/duplicate. |
| 3 | `feat(persistence): enrich AssignmentQueryService and add EF config` | Persistence | Update AssignmentQueryService to resolve assignment code/name, department, and craft role names. Add EF configuration for new ShiftInstance/PositionSlotInstance columns. Update ShiftInstanceRepository for duplicate checking. |
| 4 | `feat(proto): add GenerateCallSheet RPC and enrich response messages` | Proto/gRPC | Add GenerateCallSheet RPC to daily_operations.proto. Enrich DailyShiftInstanceResponse with department and display name fields. Enrich DailyPositionSlotResponse with assignment and craft role fields. Update DailyOperationsService implementation. Fix GetCallSheet bug (query by work area + date, not misuse of ctrl nbr). |
| 5 | `feat(frontend): add DailyOperationsClient and Call Sheet page` | Frontend | Create DailyOperationsClient gRPC wrapper. Create Call Sheet page with work area/shift/date selection, Generate button (modal), and grouped display (Department → Shift → Assignment → Positions). |

---

## Detailed Changes by Layer

### 1. Domain — `ShiftInstance.cs`

**Add properties:**
```
DepartmentCtrlNbr?      (ControlNumber?)
DepartmentName?         (string?)
ShiftDisplayName        (string)
```

**Update** `Create()` to accept new parameters.

### 1. Domain — `PositionSlotInstance.cs`

**Add properties:**
```
AssignmentCtrlNbr       (ControlNumber)
AssignmentCode          (string)
AssignmentName          (string)
CraftRoleName           (string)
```

**Update** `Create()` to accept new parameters.
**Update** `ShiftInstance.AddPositionSlot()` to pass new parameters through.

### 2. Application — `DailyOperationsRepositories.cs` (DTOs)

**`AssignmentDto`** — Add: `DepartmentCtrlNbr?`, `AssignmentCode`, `AssignmentName`
**`CrewPositionDto`** — Add: `CraftRoleName`

### 2. Application — `CallSheetGenerationService.cs`

**Revised signature:**
```csharp
GenerateAsync(workAreaGroupCtrlNbr, shiftDefinitionCtrlNbr, targetDate)
```

**Logic:**
1. Load ShiftDefinition — validate exists and is active
2. Load Department if shift has DepartmentCtrlNbr (for name snapshot)
3. Find or create WorkInstance for work area + date (lazy)
4. Check duplicate: ShiftInstance with same WorkInstanceCtrlNbr + ShiftCode already exists? → error
5. Query AssignmentQueryService for this shift + date
6. If no assignments match → error: "No assignments scheduled for this shift on [date]"
7. Create ShiftInstance with snapshot fields
8. For each assignment → for each position → AddPositionSlot with snapshot fields
9. Persist and return

**New dependencies:** `IWorkInstanceRepository`, `IDepartmentRepository`

### 3. Persistence — `AssignmentTemplateQueryService.cs`

**Enrich to:**
- Include `Assignment.Code`, `Assignment.Name`, `Assignment.DepartmentCtrlNbr` in `AssignmentDto`
- Query `CraftRole` table to resolve `CrewPosition.CraftRoleCtrlNbr` → `CraftRole.Name` for `CrewPositionDto`

### 3. Persistence — EF Configuration

Add column mappings for new `ShiftInstance` and `PositionSlotInstance` properties.

### 3. Persistence — `ShiftInstanceRepository`

Add method to check for existing shift by work instance + shift code (duplicate guard).

### 4. Proto — `daily_operations.proto`

**New RPC:**
```protobuf
rpc GenerateCallSheet (GenerateCallSheetRequest) returns (GenerateCallSheetResponse) {
  option (google.api.http) = { post: "/v1/daily-operations/call-sheet/generate" body: "*" };
}

message GenerateCallSheetRequest {
  int64 work_area_group_ctrl_nbr = 1;
  int64 shift_definition_ctrl_nbr = 2;
  string target_date = 3;
}

message GenerateCallSheetResponse {
  DailyShiftInstanceResponse shift = 1;
}
```

**Enriched `DailyShiftInstanceResponse`:**
```protobuf
optional int64 department_ctrl_nbr = 7;
string department_name = 8;
string shift_display_name = 9;
```

**Enriched `DailyPositionSlotResponse`:**
```protobuf
int64 assignment_ctrl_nbr = 9;
string assignment_code = 10;
string assignment_name = 11;
string craft_role_name = 12;
```

**Fix `GetCallSheet`:** Query by work area + date → find WorkInstance → return shifts.

### 5. Frontend — `DailyOperationsClient.cs`

gRPC client wrapper following existing patterns (e.g., AssignmentClient).

### 5. Frontend — Call Sheet Page

- Route: `/daily-operations/call-sheet`
- `@rendermode InteractiveServer`
- Work area dropdown (`FilterSelect`), date picker
- Displays existing generated shifts for the selected work area + date
- "Generate Shift" button → modal with shift definition dropdown (`SelectInput`) → calls GenerateCallSheet
- Results grouped: Department → Shift → Assignment → Positions (all "Open")

---

## Out of Scope

- Automatic/scheduled call sheet generation (background service)
- Incumbent prefill (CrewIncumbency)
- Extra board vacancy filling
- Regenerate/delete call sheet
- FRA compliance checks
- Electronic calling / AtHoc integration
