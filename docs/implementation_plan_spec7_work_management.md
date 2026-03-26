# Implementation Plan — SPEC-7: Work Management Expansion

_Branches, commits, and build order for Departments, Position Roles, and Template Positions._

---

## Branch Strategy

All work on branch: **`feature/work-management-departments-positions`** off `main`.

---

## Phases & Commits

### Phase 1 — API Domain Entities & Repositories

**Commit 1.1: Add Department entity + repository interface**
- `CrewService.API/CrewService.Domain/Modules/WorkManagement/WorkManagementEntities.cs` — add `Department` class
- `CrewService.API/CrewService.Domain/Modules/WorkManagement/IWorkManagementRepositories.cs` — add `IDepartmentRepository`

**Commit 1.2: Add TemplatePosition entity + repository interface**
- `CrewService.API/CrewService.Domain/Modules/WorkManagement/WorkManagementEntities.cs` — add `TemplatePosition` class
- `CrewService.API/CrewService.Domain/Modules/WorkManagement/IWorkManagementRepositories.cs` — add `ITemplatePositionRepository`

**Commit 1.3: Modify PositionRole — AlternateName, nullable Code, Update/Delete methods**
- `CrewService.API/CrewService.Domain/Modules/WorkManagement/WorkManagementEntities.cs` — modify `PositionRole`

**Commit 1.4: Add DepartmentCtrlNbr FK to Craft**
- `CrewService.API/CrewService.Domain/Models/Seniority/Craft.cs` — add nullable `DepartmentCtrlNbr`

---

### Phase 2 — API Persistence (EF Configs + Repositories + Migration)

**Commit 2.1: EF configurations for Department and TemplatePosition**
- `CrewService.API/CrewService.Persistance/Modules/WorkManagement/WorkManagementConfigurations.cs` — add `DepartmentConfiguration`, `TemplatePositionConfiguration`
- `CrewService.API/CrewService.Persistance/Configurations/WorkManagementModuleConfiguration.cs` — register new configs

**Commit 2.2: Update PositionRole EF config (nullable Code, AlternateName)**
- `CrewService.API/CrewService.Persistance/Modules/WorkManagement/WorkManagementConfigurations.cs` — modify `PositionRoleConfiguration`

**Commit 2.3: Repository implementations**
- `CrewService.API/CrewService.Persistance/Modules/WorkManagement/WorkManagementRepositories.cs` — add `DepartmentRepository`, `TemplatePositionRepository`

**Commit 2.4: EF Migration**
- Single migration: `AddDepartments_TemplatePositions_PositionRoleChanges`

---

### Phase 3 — API Proto & gRPC Services

**Commit 3.1: Department proto + gRPC service**
- `CrewService.API/Protos/modules/department.proto` — new proto file
- `CrewService.API/CrewService.Presentation/Services/Modules/DepartmentService.cs` — new gRPC service

**Commit 3.2: PositionRole Update + Delete RPCs**
- `CrewService.API/Protos/modules/work_management.proto` — add `UpdatePositionRole`, `DeletePositionRole` RPCs + messages
- `CrewService.API/CrewService.Presentation/Services/Modules/WorkManagementService.cs` — implement new RPCs

**Commit 3.3: TemplatePosition CRUD RPCs**
- `CrewService.API/Protos/modules/work_management.proto` — add TemplatePosition messages + RPCs
- `CrewService.API/CrewService.Presentation/Services/Modules/WorkManagementService.cs` — implement

**Commit 3.4: Craft proto — add department_ctrl_nbr**
- `CrewService.API/Protos/craft.proto` — add field to request/response messages
- `CrewService.API/CrewService.Presentation/Services/CraftService.cs` — update mapping

**Commit 3.5: Permission seed data**
- Seed `work-management/departments`, `work-management/assignment-templates`, `work-management/position-roles`

---

### Phase 4 — Frontend gRPC Clients

**Commit 4.1: WorkManagementClient.cs**
- `CrewService.BlazorUI/Clients/WorkManagementClient.cs` — Template CRUD, PositionRole full CRUD, TemplatePosition CRUD

**Commit 4.2: DepartmentClient.cs**
- `CrewService.BlazorUI/Clients/DepartmentClient.cs` — Department CRUD

**Commit 4.3: Register clients in Program.cs**
- `CrewService.BlazorUI/Program.cs` — add `WorkManagementClient`, `DepartmentClient` to DI

---

### Phase 5 — Frontend Pages

**Commit 5.1: Departments page**
- `CrewService.BlazorUI/Components/Pages/WorkManagement/Departments.razor`
- Route: `/work-management/departments`
- Railroad-scoped, InteractiveServer, modal CRUD

**Commit 5.2: Position Roles page**
- `CrewService.BlazorUI/Components/Pages/WorkManagement/PositionRoles.razor`
- Route: `/work-management/position-roles`
- Craft dropdown filter, InteractiveServer, modal CRUD

**Commit 5.3: Assignment Templates page**
- `CrewService.BlazorUI/Components/Pages/WorkManagement/AssignmentTemplates.razor`
- Route: `/work-management/assignment-templates`
- Work-area dropdown filter, InteractiveServer, modal CRUD
- Template positions sub-section (inline table within template detail/modal)

---

### Phase 6 — Navigation & Cleanup

**Commit 6.1: NavMenu — add Work Management group**
- `CrewService.BlazorUI/Components/Layout/NavMenu.razor` — new "Work Management" NavMenuGroup in railroad-scoped section

**Commit 6.2: Remove placeholder route**
- `CrewService.BlazorUI/Components/Pages/Placeholder.razor` — remove `@page "/daily/assignments"`

---

### Phase 7 — Build & Verify

**Commit 7.1: Verify full build**
- API project builds
- Frontend project builds
- No regressions in existing tests

---

## Execution Order

| Step | Phase | What | Depends On |
|------|-------|------|------------|
| 1 | Phase 1 | Domain entities + repo interfaces | — |
| 2 | Phase 2 | EF configs + repos + migration | Phase 1 |
| 3 | Phase 3 | Proto + gRPC services | Phase 2 |
| 4 | Phase 4 | Frontend clients | Phase 3 |
| 5 | Phase 5 | Frontend pages | Phase 4 |
| 6 | Phase 6 | Nav + cleanup | Phase 5 |
| 7 | Phase 7 | Build verify | Phase 6 |

---

## Notes

- Each commit is independently buildable within its phase.
- Phase 1–3 are in `CrewService.API` repo. Phase 4–7 are in `CrewService.FrontEnd` repo.
- Craft ↔ Department dropdown on the existing Crafts page is a **follow-up** after this plan completes.
- AlternateName display toggle (tenant config) is **deferred** — the field exists but the UI toggle comes later.
