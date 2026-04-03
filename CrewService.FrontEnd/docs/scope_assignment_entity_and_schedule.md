# Scope — Assignment Entity & Schedule (Call Sheet Foundation)

_Extracts assignments from DynamicGroup into a standalone entity, adds AssignmentSchedule for shift/day linkage, and updates the CrewAssignment FK — laying the foundation for per-shift call sheet generation._

---

## Background & Design Decisions

### Problem Statement

Call sheet generation needs to produce a list of positions for a specific **work area + date + shift**. The current data model has no way to determine which assignments belong to which shift:

- Assignments are `DynamicGroup` nodes with GroupType "Assignment" — they have no shift, no on-duty time, no operating schedule.
- `CrewAssignment` links a crew to an assignment with a `DaysOfWeekMask`, but there is no shift dimension.
- `AssignmentQueryService.GetTemplatesForDateAsync` returns **all** assignments for a work area + date, and `CallSheetGenerationService` duplicates every assignment's positions into every shift — wrong.

### Design Decisions (from scoping discussion)

1. **Assignment becomes a standalone entity.** Assignments outgrow DynamicGroup once they carry operational properties (department, on-duty time, schedule, shift linkage). Keeping them in GroupTypes while bolting on AssignmentSchedule would create a fragmented UX: "go here to create the assignment, go there to set its schedule." The user experience should be consistent with Crews — one entity, one detail page, everything managed in context.

2. **AssignmentSchedule is a separate child entity.** It links an Assignment to a ShiftDefinition with an `OperatingDaysMask`. This is the assignment's **operating schedule** — "Job 101 runs on 1st shift, Mon–Sat."

3. **CrewAssignment stays but changes its FK target.** `CrewAssignment.AssignmentGroupCtrlNbr` → `CrewAssignment.AssignmentCtrlNbr`, pointing to the new `Assignment` entity. `DaysOfWeekMask` remains as the **crew staffing schedule** — "Crew A covers Job 101, Mon–Fri."

4. **Two schedules enable vacancy detection.** The gap between AssignmentSchedule (job runs Mon–Sat) and CrewAssignment (crew covers Mon–Fri) = Saturday is a **vacancy** → the position slots appear on the call sheet as unfilled → queued for extra board fill via VacancyResolutionEngine.

5. **"No schedule = no days" safety rule.** An Assignment with no `AssignmentSchedule` row does not appear on any call sheet. An assignment must have an explicitly set `OperatingDaysMask` to be included in generation.

6. **Calling windows are deferred.** Calling windows (start/end times for electronic calling) will be handled at the craft level, not on the assignment. Out of scope for this work.

7. **Work areas stay in DynamicGroup.** They are organizational hierarchy — that's what DynamicGroup is for. Assignments are operational entities that reference a work area via FK.

---

## Entity Design

### `Assignment` — New domain entity

| Property | Type | Description |
|----------|------|-------------|
| `CtrlNbr` | `ControlNumber` | PK (from `Entity` base) |
| `WorkAreaGroupCtrlNbr` | `ControlNumber` | FK → `DynamicGroup` (work area) |
| `DepartmentCtrlNbr` | `ControlNumber?` | FK → `Department` |
| `Code` | `string` | Short identifier (e.g., "YD3"), displayed uppercase |
| `Name` | `string` | Display name (e.g., "Yard Job 3") |
| `OnDutyTime` | `TimeOnly` | Default on-duty time for the assignment |
| `IsExtra` | `bool` | Extra board only (true = not a regular assignment) |
| `IsActive` | `bool` | Soft active/inactive flag |

**Module**: New file in `CrewService.Domain/Modules/Staffing/` (or `Crews/` — follows the existing crew module convention since assignments and crews are tightly coupled).

### `AssignmentSchedule` — New child entity

| Property | Type | Description |
|----------|------|-------------|
| `CtrlNbr` | `ControlNumber` | PK |
| `AssignmentCtrlNbr` | `ControlNumber` | FK → `Assignment` |
| `ShiftDefinitionCtrlNbr` | `ControlNumber` | FK → `ShiftDefinition` |
| `OperatingDaysMask` | `int` | Bitmask: bit 0 = Sunday … bit 6 = Saturday |

An assignment can have multiple schedules (e.g., different shifts on different days), but the common case is one schedule per assignment.

### `CrewAssignment` — Existing entity, FK change

| Property | Before | After |
|----------|--------|-------|
| `AssignmentGroupCtrlNbr` | FK → `DynamicGroup` | Renamed to `AssignmentCtrlNbr`, FK → `Assignment` |

All other properties unchanged: `CrewCtrlNbr`, `DaysOfWeekMask`, `StartUtc`, `EndUtc`.

---

## Query Flow — Call Sheet Generation

For **Work Area X, Date Y, Shift Z**:

1. Find `AssignmentSchedule` rows where `ShiftDefinitionCtrlNbr == Z` AND day-of-week bit set for Y.
2. Join to `Assignment` where `WorkAreaGroupCtrlNbr == X` AND `IsActive == true`.
3. For each matched assignment, join to `CrewAssignment` where day-of-week bit set for Y AND date range active.
4. From `CrewAssignment` → `Crew` → `CrewPosition` → `CrewIncumbency`.
5. Positions from assignments whose `AssignmentSchedule` matches but have no `CrewAssignment` for that day → **vacant slots** (prefilled with no incumbent).
6. Create `PositionSlotInstance` rows from the result.

---

## Affected Existing Code

### API Domain
- `CrewEntities.cs` — `CrewAssignment.AssignmentGroupCtrlNbr` renamed to `AssignmentCtrlNbr`
- `ICrewRepositories.cs` — `ICrewAssignmentRepository.GetByAssignmentGroupAsync` renamed
- New `Assignment` entity + `AssignmentSchedule` entity files

### API Application
- `DailyOperationsRepositories.cs` — `IAssignmentQueryService` gains `shiftDefinitionCtrlNbr` parameter; `AssignmentDto` gains `AssignmentCtrlNbr` (of new type)
- `CallSheetGenerationService.cs` — refactored to accept single shift, uses new query

### API Persistence
- `AssignmentTemplateQueryService.cs` — query rewritten to join `Assignment` + `AssignmentSchedule` instead of DynamicGroup
- New EF configurations for `Assignment` and `AssignmentSchedule`
- EF migration: new tables, `CrewAssignment` FK change, data migration from DynamicGroup seeds

### API Presentation / Proto
- New `assignments.proto` with full CRUD for Assignment + AssignmentSchedule
- `crews.proto` — `CrewAssignmentResponse.assignment_group_ctrl_nbr` → `assignment_ctrl_nbr`
- `CrewsService.cs` — updated to use renamed FK

### Frontend
- New `AssignmentClient.cs` gRPC client
- New `Assignments.razor` lister page at `/staffing/assignments`
- New `AssignmentDetail.razor` detail page at `/staffing/assignments/{CtrlNbr}` with Schedule and Crew Assignments tabs
- `NavMenu.razor` — add Assignments link under Crew Staffing
- `CrewDetail.razor` — Assignments tab FK references updated

---

## Out of Scope

- Calling windows (deferred to craft-level configuration)
- Call sheet generation UI and background service (separate follow-on feature)
- Removing the "Assignment" GroupType from the system (can be cleaned up later; existing groups remain as organizational references)
- Extra board management pages
- Vacancy resolution engine changes

---

## UI Design

### Assignments Lister (`/staffing/assignments`)

Same pattern as Crews page:
- Railroad-scoped (requires parent + railroad context)
- DataTable columns: **Code** (uppercase, first, default sort) | **Name** | **Work Area** | **Department** | **On-Duty Time** | **Shift** | **Operating Days** | **Status** | **Actions**
- Create modal: Work Area dropdown, Department dropdown, Code, Name, On-Duty Time, IsExtra switch, IsActive switch
- Edit/Delete buttons in row actions

### Assignment Detail (`/staffing/assignments/{CtrlNbr}`)

Same pattern as CrewDetail:
- Header: Name, Code badge, Active/Inactive badge, IsExtra badge
- Tabs: **Schedule** | **Crew Assignments**

**Schedule tab:**
- DataTable of `AssignmentSchedule` rows: Shift (from ShiftDefinition display name), Operating Days (formatted like "Mon–Fri")
- Add Schedule modal: Shift dropdown (populated from ShiftDefinitions for this work area), Operating Days checkboxes (Sun–Sat)
- Edit/Delete in row actions

**Crew Assignments tab:**
- DataTable of `CrewAssignment` rows: Crew Name, Days (formatted), Effective Date Range
- Add Crew Assignment modal: Crew dropdown (populated from crews in same work area), Days checkboxes, Start/End dates
- Edit/Delete in row actions
