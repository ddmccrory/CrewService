# Implementation Plan — Crews, Crew Positions, Crew Assignments & Operating Days

_Branches, commits, and build order for Crew Staffing frontend pages and the API entity consolidation that supports them._

---

## Design Decisions (from scoping discussion)

1. **Unified `CrewAssignment` entity** replaces three separate entities:
   - `CrewAttachmentTemplate` (regular crew → assignment link, no day mask)
   - `ReliefCoverageRule` (relief crew → assignment link, with day mask)
   - `CrewOffDay` (per-position rest days — redundant once the attachment carries a day mask)

2. **`DaysOfWeekMask`** (int bitmask, bit 0 = Sunday … bit 6 = Saturday) on `CrewAssignment` is the single mechanism for both regular and relief crew coverage days. The crew's `CrewType` (REGULAR / RELIEF) determines semantics.

3. **No schedule = no days.** A `DaysOfWeekMask` of `0` (or a missing row) means the crew does not cover the assignment on any day. Operating days must be explicitly set.

4. **Frontend pages**: List page at `/staffing/crews` + Detail page at `/staffing/crews/{CtrlNbr}` with Positions and Assignments tabs.

5. **`CrewAttachmentInstance`** (runtime crew → WorkInstance binding) is **unchanged** — it is a daily-operations concept, not a template/schedule concept.

---

## Repository & Branch Strategy

Single monorepo at `C:\Projects\CrewService` (remote: `origin` → GitHub). Both API (`CrewService.API/`) and Frontend (`CrewService.FrontEnd/`) live in the same repo.

### Branches

| Branch | Base | Scope | Merge Target |
|--------|------|-------|--------------|
| `feature/crew-assignment-consolidation` | `main` | Phases 1–3 (API entity consolidation) | → `main` |
| `feature/crews-frontend` | `main` (after above merges) | Phases 4–7 (frontend client + pages) | → `main` |

### Git Workflow

```bash
# ── API consolidation branch ──
git checkout main
git pull origin main
git checkout -b feature/crew-assignment-consolidation

# Phase 1 commits (domain)
# ... make changes ...
git add -A && git commit -m "feat(crews): add CrewAssignment entity"
git add -A && git commit -m "feat(crews): add ICrewAssignmentRepository"
git add -A && git commit -m "refactor(crews): remove CrewAttachmentTemplate, ReliefCoverageRule, CrewOffDay"

# Phase 2 commits (persistence)
git add -A && git commit -m "feat(crews): add CrewAssignmentConfiguration"
git add -A && git commit -m "refactor(crews): remove obsolete EF configs"
git add -A && git commit -m "feat(crews): add CrewAssignmentRepository, remove old repos"
git add -A && git commit -m "refactor(crews): update DI registrations"
git add -A && git commit -m "refactor(crews): update DbContext DbSets"
git add -A && git commit -m "feat(crews): add ConsolidateCrewAssignment migration"

# Phase 3 commits (presentation + tests)
git add -A && git commit -m "feat(crews): update crews.proto — CrewAssignment CRUD RPCs"
git add -A && git commit -m "feat(crews): update CrewsService — CrewAssignment methods"
git add -A && git commit -m "feat(crews): update AssignmentQueryService — day-of-week filter"
git add -A && git commit -m "chore(crews): update DevDataSeeder for CrewAssignment"
git add -A && git commit -m "test(crews): update unit tests for CrewAssignment"

# Push + PR + merge
git push -u origin feature/crew-assignment-consolidation
# PR → main, review, merge

# ── Frontend branch ──
git checkout main
git pull origin main
git checkout -b feature/crews-frontend

# Phase 4 commits (client)
git add -A && git commit -m "feat(ui): add CrewClient gRPC client"
git add -A && git commit -m "feat(ui): register CrewClient in Program.cs"

# Phase 5 commits (pages)
git add -A && git commit -m "feat(ui): add Crews list page"
git add -A && git commit -m "feat(ui): add CrewDetail page with Positions tab"
git add -A && git commit -m "feat(ui): add Assignments tab with operating days"

# Phase 6 commit (cleanup)
git add -A && git commit -m "chore(ui): remove staffing/crews placeholder route"

# Phase 7 commit (verify)
git add -A && git commit -m "chore: verify build and tests pass"

# Push + PR + merge
git push -u origin feature/crews-frontend
# PR → main, review, merge
```

---

## Phases & Commits

### Phase 1 — API Domain: Consolidate Entities

**Commit 1.1: Add `CrewAssignment` entity**
- `CrewService.API/CrewService.Domain/Modules/Crews/CrewEntities.cs`
  - Add `CrewAssignment` class with: `CrewCtrlNbr`, `AssignmentGroupCtrlNbr`, `DaysOfWeekMask` (int), `StartUtc`, `EndUtc`
  - Factory method: `Create(crewCtrlNbr, assignmentGroupCtrlNbr, daysOfWeekMask, startUtc, endUtc?)`

**Commit 1.2: Add `ICrewAssignmentRepository`**
- `CrewService.API/CrewService.Domain/Modules/Crews/ICrewRepositories.cs`
  - Add `ICrewAssignmentRepository` with `GetByCrewAsync(ControlNumber)` and `GetByAssignmentGroupAsync(ControlNumber)`

**Commit 1.3: Remove obsolete entities**
- `CrewService.API/CrewService.Domain/Modules/Crews/CrewEntities.cs`
  - Remove `CrewAttachmentTemplate` class
  - Remove `ReliefCoverageRule` class
- `CrewService.API/CrewService.Domain/Modules/Crews/ICrewRepositories.cs`
  - Remove `ICrewAttachmentTemplateRepository`
  - Remove `IReliefCoverageRuleRepository`
- `CrewService.API/CrewService.Domain/Modules/WorkManagement/CrewOffDay.cs`
  - Delete file

---

### Phase 2 — API Persistence: EF Configs, Repositories, Migration

**Commit 2.1: Add `CrewAssignmentConfiguration`**
- `CrewService.API/CrewService.Persistance/Configurations/CrewModuleConfiguration.cs`
  - Add `CrewAssignmentConfiguration` (PK, ControlNumber conversions, FKs to Crew + DynamicGroup, audit stamps)
  - FK to `Crew` → `DeleteBehavior.Cascade`
  - FK to `DynamicGroup` (assignment) → `DeleteBehavior.Restrict`

**Commit 2.2: Remove obsolete EF configs**
- `CrewService.API/CrewService.Persistance/Configurations/CrewModuleConfiguration.cs`
  - Remove `CrewAttachmentTemplateConfiguration`
  - Remove `ReliefCoverageRuleConfiguration`
- `CrewService.API/CrewService.Persistance/Configurations/CrewOffDayAbolishmentConfiguration.cs`
  - Remove `CrewOffDayConfiguration` class (keep `AbolishmentRecordConfiguration` if still needed, or remove file if both are obsolete)
- `CrewService.API/CrewService.Persistance/Modules/Crews/CrewConfigurations.cs`
  - Remove duplicate `CrewAttachmentTemplateConfiguration` and `ReliefCoverageRuleConfiguration`

**Commit 2.3: Add `CrewAssignmentRepository`, remove old repos**
- `CrewService.API/CrewService.Persistance/Modules/Crews/CrewRepositories.cs`
  - Add `CrewAssignmentRepository` implementing `ICrewAssignmentRepository`
  - Remove `CrewAttachmentTemplateRepository`
  - Remove `ReliefCoverageRuleRepository`

**Commit 2.4: Update DI registration**
- `CrewService.API/CrewService.Persistance/DependencyInjection.cs`
  - Replace `ICrewAttachmentTemplateRepository` → `ICrewAssignmentRepository`
  - Remove `IReliefCoverageRuleRepository`

**Commit 2.5: Update DbContext**
- `CrewService.API/CrewService.Persistance/Data/CrewServiceDbContext.cs`
  - Add `DbSet<CrewAssignment> CrewAssignments`
  - Remove `DbSet<CrewAttachmentTemplate> CrewAttachmentTemplates`
  - Remove `DbSet<ReliefCoverageRule> ReliefCoverageRules`
  - Remove `DbSet<CrewOffDay> CrewOffDays`

**Commit 2.6: EF Migration**
- Single migration: `ConsolidateCrewAssignment`
  - Creates `CrewAssignments` table
  - Migrates data from `CrewAttachmentTemplates` (with `DaysOfWeekMask = 0`) and `ReliefCoverageRules` into `CrewAssignments`
  - Drops `CrewAttachmentTemplates`, `ReliefCoverageRules`, `CrewOffDays` tables

---

### Phase 3 — API Presentation: Proto & gRPC Service Updates

**Commit 3.1: Update `crews.proto` — replace attachment + relief RPCs with CrewAssignment RPCs**
- `Protos/modules/crews.proto` (shared proto)
  - Remove: `GetCrewAttachmentTemplates`, `CreateCrewAttachmentTemplate`, `GetReliefCoverageRules`, `CreateReliefCoverageRule` RPCs
  - Remove: all `CrewAttachmentTemplate*` and `ReliefCoverageRule*` messages
  - Add RPCs:
    - `GetCrewAssignments(GetCrewAssignmentsRequest) returns (GetCrewAssignmentsResponse)` — GET `/v1/crews/{crew_ctrl_nbr}/assignments`
    - `CreateCrewAssignment(CreateCrewAssignmentRequest) returns (CrewAssignmentResponse)` — POST `/v1/crews/assignments`
    - `UpdateCrewAssignment(UpdateCrewAssignmentRequest) returns (CrewAssignmentResponse)` — PUT `/v1/crews/assignments/{ctrl_nbr}`
    - `DeleteCrewAssignment(DeleteCrewAssignmentRequest) returns (common.DeleteResponse)` — DELETE `/v1/crews/assignments/{ctrl_nbr}`
  - Add messages:
    - `GetCrewAssignmentsRequest { int64 crew_ctrl_nbr = 1; }`
    - `GetCrewAssignmentsResponse { repeated CrewAssignmentResponse assignments = 1; int32 total_count = 2; }`
    - `CreateCrewAssignmentRequest { int64 crew_ctrl_nbr = 1; int64 assignment_group_ctrl_nbr = 2; int32 days_of_week_mask = 3; string start_utc = 4; string end_utc = 5; }`
    - `UpdateCrewAssignmentRequest { int64 ctrl_nbr = 1; int32 days_of_week_mask = 2; string start_utc = 3; string end_utc = 4; }`
    - `DeleteCrewAssignmentRequest { int64 ctrl_nbr = 1; }`
    - `CrewAssignmentResponse { int64 ctrl_nbr = 1; int64 crew_ctrl_nbr = 2; int64 assignment_group_ctrl_nbr = 3; int32 days_of_week_mask = 4; string start_utc = 5; string end_utc = 6; }`
- `CrewService.API/Protos/modules/crews.proto` (API-side copy, if separate) — same changes

**Commit 3.2: Update `CrewsService.cs`**
- `CrewService.API/CrewService.Presentation/Services/Modules/CrewsService.cs`
  - Replace constructor params: remove `ICrewAttachmentTemplateRepository`, `IReliefCoverageRuleRepository`; add `ICrewAssignmentRepository`
  - Remove: `GetCrewAttachmentTemplates`, `CreateCrewAttachmentTemplate`, `GetReliefCoverageRules`, `CreateReliefCoverageRule`, `MapAttachmentTemplate`, `MapReliefRule`
  - Add: `GetCrewAssignments`, `CreateCrewAssignment`, `UpdateCrewAssignment`, `DeleteCrewAssignment`, `MapCrewAssignment`

**Commit 3.3: Update `AssignmentQueryService` — add day-of-week filter**
- `CrewService.API/CrewService.Persistance/Modules/DailyOperations/AssignmentTemplateQueryService.cs`
  - Change `dbContext.Set<CrewAttachmentTemplate>()` → `dbContext.Set<CrewAssignment>()`
  - Add filter: `var dayBit = 1 << (int)targetDate.DayOfWeek;` then `.Where(a => (a.DaysOfWeekMask & dayBit) != 0)`
  - No schedule (mask = 0) → assignment excluded from results

**Commit 3.4: Update `DevDataSeeder.cs`**
- `CrewService.API/CrewService.GrpcService/DevDataSeeder.cs`
  - Replace `ICrewAttachmentTemplateRepository` + `IReliefCoverageRuleRepository` with `ICrewAssignmentRepository`
  - Replace seed data: create `CrewAssignment` records instead of `CrewAttachmentTemplate` / `ReliefCoverageRule`
  - Seed example: Crew A → Job 101, Mon–Fri (`0b0111110`); Extra Crew → Job 101, Sat–Sun (`0b1000001`)

**Commit 3.5: Update tests**
- `CrewService.API/CrewService.UnitTests/Crews/CrewTests.cs`
  - Remove `CrewAttachmentTemplateTests` and `ReliefCoverageRuleTests`
  - Add `CrewAssignmentTests` — verify `Create` sets all properties including `DaysOfWeekMask`
- `CrewService.API/CrewService.UnitTests/WorkManagement/WorkManagementTests.cs`
  - Remove `CrewOffDayTests`
- `CrewService.API/CrewService.UnitTests/Persistence/ForeignKeyIntegrityTests.cs`
  - Replace `[InlineData("CrewAttachmentTemplate", "Crew", DeleteBehavior.Cascade)]` with `[InlineData("CrewAssignment", "Crew", DeleteBehavior.Cascade)]`

---

### Phase 4 — Frontend gRPC Client

**Commit 4.1: Add `CrewClient.cs`**
- `CrewService.BlazorUI/Clients/CrewClient.cs`
  - Extends `BaseGrpcClient<CrewsSrvc.CrewsSrvcClient>`
  - Methods:
    - `GetAllCrewsAsync(long homeGroupCtrlNbr)` / `GetAllCrewsByTypeAsync(string crewType)`
    - `GetCrewAsync(long ctrlNbr)`
    - `CreateCrewAsync(...)` / `UpdateCrewAsync(...)` / `DeleteCrewAsync(...)`
    - `GetCrewPositionsAsync(long crewCtrlNbr)` / `CreateCrewPositionAsync(...)`
    - `GetCrewIncumbenciesAsync(long crewPositionCtrlNbr)` / `CreateCrewIncumbencyAsync(...)`
    - `GetCrewAssignmentsAsync(long crewCtrlNbr)` / `CreateCrewAssignmentAsync(...)` / `UpdateCrewAssignmentAsync(...)` / `DeleteCrewAssignmentAsync(...)`

**Commit 4.2: Register client in `Program.cs`**
- `CrewService.BlazorUI/Program.cs`
  - Add `builder.Services.AddScoped<CrewClient>();`

---

### Phase 5 — Frontend Pages

**Commit 5.1: Crews list page**
- `CrewService.BlazorUI/Components/Pages/Staffing/Crews.razor`
  - Route: `@page "/staffing/crews"`
  - `@rendermode InteractiveServer`, `@inherits AppComponentBase`
  - `DataTable<CrewResponse>` with columns: Name, Type, Home Group, Active, Actions
  - Toolbar: Create Crew button
  - Row actions: View (navigates to detail), Edit, Delete (all evenly-sized buttons)
  - Create/Edit Modal: Name, Type (REGULAR/RELIEF dropdown), Home Group (work area selector via `TenantConfigClient`), IsActive (slide switch)
  - Delete confirmation Modal

**Commit 5.2: Crew detail page**
- `CrewService.BlazorUI/Components/Pages/Staffing/CrewDetail.razor`
  - Route: `@page "/staffing/crews/{CtrlNbr:long}"`
  - `@rendermode InteractiveServer`, `@inherits AppComponentBase`
  - `BackNavButton` → navigates to `/staffing/crews`
  - Header: crew name, type badge, active badge
  - `TabPanel` with two tabs: Positions, Assignments

**Commit 5.3: Positions tab content (in CrewDetail.razor)**
- `DataTable<CrewPositionResponse>` with columns: Display Order, Position Role (resolved name), Actions
- Add Position Modal: PositionRole dropdown (from `WorkManagementClient.GetPositionRolesAsync`), DisplayOrder (int)
- Row actions: Edit, Delete

**Commit 5.4: Assignments tab content (in CrewDetail.razor)**
- `DataTable<CrewAssignmentResponse>` with columns: Assignment (resolved name), Operating Days (7 day indicators + summary text), Effective Date, Actions
- Add Assignment Modal:
  - Assignment dropdown (DynamicGroups of type "Assignment" via `TenantConfigClient.GetGroupsByTypeNameAsync`)
  - Operating Days: 7 checkboxes (Sun–Sat), Bootstrap slide switch style, maps to/from `DaysOfWeekMask` bitmask
  - Effective From (date), Effective To (date, optional)
- Edit Assignment Modal: same fields (assignment read-only, days + dates editable)
- Row actions: Edit, Delete

---

### Phase 6 — Navigation & Cleanup

**Commit 6.1: Remove placeholder route**
- `CrewService.BlazorUI/Components/Pages/Placeholder.razor`
  - Remove `@page "/staffing/crews"` (NavMenu already has the link under Crew Staffing group)

---

### Phase 7 — Build & Verify

**Commit 7.1: Verify full build**
- API project builds with no errors
- Frontend project builds with no errors
- All existing tests pass
- New `CrewAssignmentTests` pass

---

## Execution Order

| Step | Phase | What | Depends On |
|------|-------|------|------------|
| 1 | Phase 1 | Domain: add CrewAssignment, remove old entities | — |
| 2 | Phase 2 | Persistence: EF configs, repos, DI, DbContext, migration | Phase 1 |
| 3 | Phase 3 | Presentation: proto, gRPC service, query service, seeder, tests | Phase 2 |
| 4 | Phase 4 | Frontend: CrewClient + Program.cs registration | Phase 3 |
| 5 | Phase 5 | Frontend: Crews list page + CrewDetail page with tabs | Phase 4 |
| 6 | Phase 6 | Cleanup: remove placeholder route | Phase 5 |
| 7 | Phase 7 | Build verify + test run | Phase 6 |

---

## Files Changed Summary

### Entities removed (3)
| File | Action |
|------|--------|
| `CrewEntities.cs` — `CrewAttachmentTemplate` | Remove class |
| `CrewEntities.cs` — `ReliefCoverageRule` | Remove class |
| `CrewOffDay.cs` | Delete file |

### Entity added (1)
| File | Action |
|------|--------|
| `CrewEntities.cs` — `CrewAssignment` | Add class (CrewCtrlNbr, AssignmentGroupCtrlNbr, DaysOfWeekMask, StartUtc, EndUtc) |

### Repository changes
| File | Action |
|------|--------|
| `ICrewRepositories.cs` | Remove `ICrewAttachmentTemplateRepository`, `IReliefCoverageRuleRepository`; add `ICrewAssignmentRepository` |
| `CrewRepositories.cs` | Remove `CrewAttachmentTemplateRepository`, `ReliefCoverageRuleRepository`; add `CrewAssignmentRepository` |

### EF / Persistence changes
| File | Action |
|------|--------|
| `CrewModuleConfiguration.cs` | Remove `CrewAttachmentTemplateConfiguration`, `ReliefCoverageRuleConfiguration`; add `CrewAssignmentConfiguration` |
| `CrewConfigurations.cs` | Remove duplicate configs for removed entities |
| `CrewOffDayAbolishmentConfiguration.cs` | Remove `CrewOffDayConfiguration` |
| `CrewServiceDbContext.cs` | Swap DbSets |
| `DependencyInjection.cs` | Swap DI registrations |
| New migration | `ConsolidateCrewAssignment` |

### Proto / gRPC changes
| File | Action |
|------|--------|
| `Protos/modules/crews.proto` | Replace attachment + relief RPCs/messages with CrewAssignment CRUD |
| `CrewsService.cs` | Replace implementation methods |
| `AssignmentTemplateQueryService.cs` | Use `CrewAssignment` + day filter |
| `DevDataSeeder.cs` | Update seed data |

### Test changes
| File | Action |
|------|--------|
| `CrewTests.cs` | Replace `CrewAttachmentTemplateTests` + `ReliefCoverageRuleTests` → `CrewAssignmentTests` |
| `WorkManagementTests.cs` | Remove `CrewOffDayTests` |
| `ForeignKeyIntegrityTests.cs` | Update inline data |

### Frontend (all new)
| File | Action |
|------|--------|
| `Clients/CrewClient.cs` | New — all Crews gRPC methods |
| `Program.cs` | Add `CrewClient` registration |
| `Components/Pages/Staffing/Crews.razor` | New — list page |
| `Components/Pages/Staffing/CrewDetail.razor` | New — detail page with Positions + Assignments tabs |
| `Components/Pages/Placeholder.razor` | Remove `@page "/staffing/crews"` |

---

## Notes

- Each commit is independently buildable within its phase.
- Phases 1–3 are in `CrewService.API`. Phases 4–7 are in `CrewService.FrontEnd`.
- `CrewAttachmentInstance` (runtime crew → WorkInstance binding) is **not touched** — it stays as-is for daily operations.
- `CrewIncumbency` management (assigning employees to positions) is included as read-only display in the Positions tab. Full incumbency CRUD is a **follow-up**.
- The `AssignmentQueryService` day-of-week filter closes the operating-days gap left when `RecurrenceJson` was removed during the AssignmentTemplate → DynamicGroup migration.

