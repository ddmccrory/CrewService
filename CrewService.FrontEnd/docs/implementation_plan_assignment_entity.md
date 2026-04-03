# Implementation Plan — Assignment Entity & Schedule

_Branches, commits, and build order for extracting assignments into a standalone entity with schedule/shift linkage._

---

## Design Decisions

See [`scope_assignment_entity_and_schedule.md`](scope_assignment_entity_and_schedule.md) for full rationale.

1. **Assignment becomes a standalone entity** — no longer a DynamicGroup node.
2. **AssignmentSchedule** links Assignment → ShiftDefinition with OperatingDaysMask.
3. **CrewAssignment FK** changes from DynamicGroup to Assignment.
4. **Two-schedule model** enables vacancy detection (schedule vs. crew coverage gap).
5. **No schedule = no days** — assignments must have explicit OperatingDaysMask.

---

## Branch Strategy

| Branch | Base | Scope | Merge Target |
|--------|------|-------|--------------|
| `feature/assignment-entity` | `main` | Phases 1–5 (API) | → `main` |
| `feature/assignments-frontend` | `main` (after above) | Phases 6–9 (Frontend) | → `main` |

---

## Phases & Commits

### Phase 1 — Domain: New Entities & FK Update

**Branch:** `feature/assignment-entity`

**Commit 1.1: Add Assignment and AssignmentSchedule entities**
- New file: `CrewService.API/CrewService.Domain/Modules/Crews/AssignmentEntities.cs`
  - `Assignment` class: `WorkAreaGroupCtrlNbr`, `DepartmentCtrlNbr?`, `Code`, `Name`, `OnDutyTime` (TimeOnly), `IsExtra`, `IsActive`
  - `AssignmentSchedule` class: `AssignmentCtrlNbr`, `ShiftDefinitionCtrlNbr`, `OperatingDaysMask` (int bitmask)
  - Factory methods: `Assignment.Create(...)`, `AssignmentSchedule.Create(...)`

**Commit 1.2: Add IAssignmentRepository and IAssignmentScheduleRepository**
- New file: `CrewService.API/CrewService.Domain/Modules/Crews/IAssignmentRepositories.cs`
  - `IAssignmentRepository` — `GetByWorkAreaAsync(ControlNumber)`, `GetAllByRailroadAsync(ControlNumber)`
  - `IAssignmentScheduleRepository` — `GetByAssignmentAsync(ControlNumber)`, `GetByShiftDefinitionAsync(ControlNumber)`

**Commit 1.3: Rename CrewAssignment FK**
- `CrewService.API/CrewService.Domain/Modules/Crews/CrewEntities.cs`
  - `CrewAssignment.AssignmentGroupCtrlNbr` → `AssignmentCtrlNbr`
  - Update `Create(...)` factory method parameter name
- `CrewService.API/CrewService.Domain/Modules/Crews/ICrewRepositories.cs`
  - `ICrewAssignmentRepository.GetByAssignmentGroupAsync` → `GetByAssignmentAsync`

### Phase 2 — Persistence: EF, Repos, DI, Migration

**Commit 2.1: Add AssignmentConfiguration and AssignmentScheduleConfiguration**
- New file: `CrewService.API/CrewService.Persistance/Modules/Crews/AssignmentConfigurations.cs`
  - `AssignmentConfiguration` — PK, ControlNumber conversions, FKs to DynamicGroup (work area) + Department, AuditStamp owned types
  - `AssignmentScheduleConfiguration` — PK, ControlNumber conversions, FKs to Assignment + ShiftDefinition, AuditStamp

**Commit 2.2: Update CrewAssignmentConfiguration FK target**
- `CrewService.API/CrewService.Persistance/Modules/Crews/CrewConfigurations.cs`
  - `CrewAssignmentConfiguration`: FK column rename `AssignmentGroupCtrlNbr` → `AssignmentCtrlNbr`, FK target changes from DynamicGroup to Assignment

**Commit 2.3: Add AssignmentRepository and AssignmentScheduleRepository**
- New file: `CrewService.API/CrewService.Persistance/Modules/Crews/AssignmentRepositories.cs`
  - Implements `IAssignmentRepository`, `IAssignmentScheduleRepository`

**Commit 2.4: Update DI registrations**
- `CrewService.API/CrewService.Persistance/DependencyInjection.cs`
  - Add `IAssignmentRepository` → `AssignmentRepository`
  - Add `IAssignmentScheduleRepository` → `AssignmentScheduleRepository`

**Commit 2.5: Update DbContext**
- `CrewService.API/CrewService.Persistance/Data/CrewServiceDbContext.cs`
  - Add `DbSet<Assignment>`, `DbSet<AssignmentSchedule>`

**Commit 2.6: EF Migration**
- New migration: creates `Assignments` and `AssignmentSchedules` tables
- Renames `CrewAssignments.AssignmentGroupCtrlNbr` → `AssignmentCtrlNbr` with FK change

### Phase 3 — Proto & gRPC Services

**Commit 3.1: Create assignments.proto**
- New file: `Protos/modules/assignments.proto` (shared)
- Copy to: `CrewService.API/Protos/modules/assignments.proto`
- Service `AssignmentsSrvc` with RPCs:
  - `GetAssignments` — GET `/v1/assignments?work_area_group_ctrl_nbr={}`
  - `GetAssignment` — GET `/v1/assignments/{ctrl_nbr}`
  - `CreateAssignment` — POST `/v1/assignments`
  - `UpdateAssignment` — PUT `/v1/assignments/{ctrl_nbr}`
  - `DeleteAssignment` — DELETE `/v1/assignments/{ctrl_nbr}`
  - `GetAssignmentSchedules` — GET `/v1/assignments/{assignment_ctrl_nbr}/schedules`
  - `CreateAssignmentSchedule` — POST `/v1/assignments/schedules`
  - `UpdateAssignmentSchedule` — PUT `/v1/assignments/schedules/{ctrl_nbr}`
  - `DeleteAssignmentSchedule` — DELETE `/v1/assignments/schedules/{ctrl_nbr}`
- Add Protobuf reference to `CrewService.Presentation.csproj` (Server) and `CrewService.BlazorUI.csproj` (Client)

**Commit 3.2: Update crews.proto FK field rename**
- `Protos/modules/crews.proto` + `CrewService.API/Protos/modules/crews.proto`
  - `CreateCrewAssignmentRequest.assignment_group_ctrl_nbr` → `assignment_ctrl_nbr`
  - `CrewAssignmentResponse.assignment_group_ctrl_nbr` → `assignment_ctrl_nbr`

**Commit 3.3: Add AssignmentsService.cs**
- New file: `CrewService.API/CrewService.Presentation/Services/Modules/AssignmentsService.cs`
  - Implements all RPCs from `AssignmentsSrvc`
  - Injects `IAssignmentRepository`, `IAssignmentScheduleRepository`

**Commit 3.4: Update CrewsService.cs**
- `CrewService.API/CrewService.Presentation/Services/Modules/CrewsService.cs`
  - Update `CreateCrewAssignment` / `MapCrewAssignment` for renamed FK field

### Phase 4 — Query Service, Seed Data, Tests

**Commit 4.1: Update IAssignmentQueryService interface**
- `CrewService.API/CrewService.Application/DailyOperations/DailyOperationsRepositories.cs`
  - `GetTemplatesForDateAsync` gains `ControlNumber shiftDefinitionCtrlNbr` parameter
  - `AssignmentDto` gains `AssignmentCtrlNbr` property (new Assignment entity PK)

**Commit 4.2: Rewrite AssignmentTemplateQueryService**
- `CrewService.API/CrewService.Persistance/Modules/DailyOperations/AssignmentTemplateQueryService.cs`
  - Replace DynamicGroup join with: `Assignment` → `AssignmentSchedule` (filtered by shift + day bit) → `CrewAssignment` → `CrewPosition` → `CrewIncumbency`
  - Day filter: `var dayBit = 1 << (int)targetDate.DayOfWeek;` then `.Where(s => (s.OperatingDaysMask & dayBit) != 0)`

**Commit 4.3: Update DevDataSeeder**
- `CrewService.API/CrewService.GrpcService/DevDataSeeder.cs`
  - Inject `IAssignmentRepository`, `IAssignmentScheduleRepository`
  - Seed Assignment entities (replacing DynamicGroup "Assignment" type nodes)
  - Seed AssignmentSchedule rows linking assignments to shifts with operating days
  - Update CrewAssignment seed data to use new Assignment FKs

**Commit 4.4: Add/update tests**
- Add `AssignmentTests` — verify `Assignment.Create` and `AssignmentSchedule.Create` set all properties
- Update `CrewAssignmentTests` — verify renamed `AssignmentCtrlNbr` property
- Update `ForeignKeyIntegrityTests` — inline data for new entities

---

### Phase 5 — API Build Verify

**Commit 5.1: Verify API build and tests**
- All API projects build with no errors
- All existing tests pass
- New `AssignmentTests` pass

### Phase 6 — Frontend Client

**Branch:** `feature/assignments-frontend`

**Commit 6.1: Add AssignmentClient.cs**
- New file: `CrewService.BlazorUI/Clients/AssignmentClient.cs`
  - Extends `BaseGrpcClient<AssignmentsSrvc.AssignmentsSrvcClient>`
  - Methods: `GetAllAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
  - Schedule methods: `GetSchedulesAsync`, `CreateScheduleAsync`, `UpdateScheduleAsync`, `DeleteScheduleAsync`

**Commit 6.2: Add proto reference + register client**
- `CrewService.BlazorUI/CrewService.BlazorUI.csproj`
  - Add `<Protobuf Include="..\..\Protos\modules\assignments.proto" GrpcServices="Client" ProtoRoot="..\..\Protos" />`
- `CrewService.BlazorUI/Program.cs`
  - Add `builder.Services.AddScoped<AssignmentClient>();`

---

### Phase 7 — Frontend Pages

**Commit 7.1: Assignments list page**
- New file: `CrewService.BlazorUI/Components/Pages/Staffing/Assignments.razor`
  - Route: `@page "/staffing/assignments"`
  - `@rendermode InteractiveServer`, `@inherits AppComponentBase`
  - `DataTable<AssignmentResponse>` — columns: Code (uppercase, first, default sort), Name, Work Area, Department, On-Duty Time, Status, Actions
  - Create modal (Modal pattern from Invitations): Work Area dropdown, Department dropdown, Code, Name, On-Duty Time, IsExtra switch, IsActive switch
  - Edit/Delete buttons in row actions

**Commit 7.2: AssignmentDetail page with Schedule tab**
- New file: `CrewService.BlazorUI/Components/Pages/Staffing/AssignmentDetail.razor`
  - Route: `@page "/staffing/assignments/{CtrlNbr:long}"`
  - `@rendermode InteractiveServer`, `@inherits AppComponentBase`
  - `BackNavButton` → `/staffing/assignments`
  - Header: Name, Code badge, Active/Inactive badge, IsExtra badge
  - `TabPanel` with two tabs: Schedule, Crew Assignments
  - **Schedule tab**: `DataTable<AssignmentScheduleResponse>` — Shift name, Operating Days (formatted)
  - Add Schedule modal: Shift dropdown (from ShiftDefinitions for work area), Operating Days checkboxes (Sun–Sat, slide switches)
  - Edit/Delete in row actions

**Commit 7.3: Crew Assignments tab**
- Same file: `AssignmentDetail.razor`
  - `DataTable<CrewAssignmentResponse>` — Crew Name, Days (formatted), Effective Date Range
  - Add Crew Assignment modal: Crew dropdown (crews in same work area), Days checkboxes, Start/End dates
  - Edit/Delete in row actions

---

### Phase 8 — Navigation & Cleanup

**Commit 8.1: Add Assignments to NavMenu**
- `CrewService.BlazorUI/Components/Layout/NavMenu.razor`
  - Add `Assignments` link under Crew Staffing group (between Crews and Extra Boards)
  - Route: `/staffing/assignments`

**Commit 8.2: Update CrewDetail.razor references**
- `CrewService.BlazorUI/Components/Pages/Staffing/CrewDetail.razor`
  - Assignments tab: update FK field references from `AssignmentGroupCtrlNbr` → `AssignmentCtrlNbr`
  - Assignment name resolution: query new `AssignmentClient` instead of `TenantConfigClient` group lookup

---

### Phase 9 — Frontend Build Verify

**Commit 9.1: Verify full build**
- Frontend project builds with no errors
- API project still builds with no errors
- All tests pass

---

## Execution Order

| Step | Phase | What | Depends On |
|------|-------|------|------------|
| 1 | Phase 1 | Domain: Assignment + AssignmentSchedule entities, CrewAssignment FK rename | — |
| 2 | Phase 2 | Persistence: EF configs, repos, DI, DbContext, migration | Phase 1 |
| 3 | Phase 3 | Proto: assignments.proto, update crews.proto, gRPC services | Phase 2 |
| 4 | Phase 4 | Query service rewrite, seed data, tests | Phase 3 |
| 5 | Phase 5 | API build verify | Phase 4 |
| 6 | Phase 6 | Frontend: AssignmentClient + proto ref + registration | Phase 5 |
| 7 | Phase 7 | Frontend: Assignments list + AssignmentDetail pages | Phase 6 |
| 8 | Phase 8 | Navigation: NavMenu + CrewDetail reference updates | Phase 7 |
| 9 | Phase 9 | Frontend build verify | Phase 8 |

---

## Files Changed Summary

### New files
| File | Description |
|------|-------------|
| `CrewService.Domain/Modules/Crews/AssignmentEntities.cs` | Assignment + AssignmentSchedule domain entities |
| `CrewService.Domain/Modules/Crews/IAssignmentRepositories.cs` | Repository interfaces |
| `CrewService.Persistance/Modules/Crews/AssignmentConfigurations.cs` | EF configurations |
| `CrewService.Persistance/Modules/Crews/AssignmentRepositories.cs` | Repository implementations |
| `Protos/modules/assignments.proto` | Shared proto (+ API copy) |
| `CrewService.Presentation/Services/Modules/AssignmentsService.cs` | gRPC service |
| `CrewService.BlazorUI/Clients/AssignmentClient.cs` | Frontend gRPC client |
| `CrewService.BlazorUI/Components/Pages/Staffing/Assignments.razor` | List page |
| `CrewService.BlazorUI/Components/Pages/Staffing/AssignmentDetail.razor` | Detail page |

### Modified files
| File | Change |
|------|--------|
| `CrewEntities.cs` | `CrewAssignment.AssignmentGroupCtrlNbr` → `AssignmentCtrlNbr` |
| `ICrewRepositories.cs` | Rename `GetByAssignmentGroupAsync` → `GetByAssignmentAsync` |
| `CrewConfigurations.cs` | Update CrewAssignment FK target |
| `DependencyInjection.cs` | Add Assignment + AssignmentSchedule repo registrations |
| `CrewServiceDbContext.cs` | Add DbSets for new entities |
| `crews.proto` (shared + API) | Rename FK field in CrewAssignment messages |
| `CrewsService.cs` | Update for renamed FK field |
| `DailyOperationsRepositories.cs` | Add shift parameter to IAssignmentQueryService |
| `AssignmentTemplateQueryService.cs` | Full rewrite — query Assignment + AssignmentSchedule |
| `DevDataSeeder.cs` | Seed Assignment + AssignmentSchedule data |
| `CrewService.Presentation.csproj` | Add assignments.proto reference |
| `CrewService.BlazorUI.csproj` | Add assignments.proto reference |
| `Program.cs` (BlazorUI) | Register AssignmentClient |
| `NavMenu.razor` | Add Assignments link |
| `CrewDetail.razor` | Update Assignments tab FK references |

---

## Notes

- Each commit is independently buildable within its phase.
- Phases 1–5 are in `CrewService.API`. Phases 6–9 are in `CrewService.FrontEnd`.
- `CallSheetGenerationService` refactoring (accept single shift instead of looping all) is a **follow-on** after this work — it depends on the query service rewrite from Phase 4.
- Removing the "Assignment" GroupType from DynamicGroup is **deferred** — existing groups remain as organizational references until cleanup.
- `CrewAttachmentInstance` (runtime crew → WorkInstance binding) is **not touched**.
