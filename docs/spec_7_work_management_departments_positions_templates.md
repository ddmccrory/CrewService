# SPEC-7 — Work Management: Departments & Position Roles

_System spec for the Work Management module expansion. Date: 2025-06-20. Updated: 2025-06-21._

---

## 1. Purpose

Extend the Work Management module with two capabilities:

1. **Departments** — organizational grouping above Craft (e.g., "Operating", "Mechanical").
2. **Position Roles** — granular job functions within a Craft (e.g., "Conductor/Foreman" under Trainman). Full CRUD (currently only Create + Read exist).

> **Note on staffing:** Position definitions on jobs are handled at the **Crew** level, not the AssignmentTemplate level. The existing `CrewPosition` entity (in the Crews module) defines what positions a crew needs. Crews are attached to AssignmentTemplates via `CrewAttachmentTemplate`. See section 2.3 for details.

---

## 2. Domain Model

### 2.1 Hierarchy

```
Railroad (DynamicGroup)
+-- Department (new)
    +-- Craft: Engineer
    |     +-- PositionRole: Engineer
    +-- Craft: Trainman
          +-- PositionRole: Conductor/Foreman  (AlternateName: "Conductor")
          +-- PositionRole: Brakeman/Switchman  (AlternateName: "Switchman")
```

Staffing is defined at the Crew level and attached to templates:

```
Crew: "Road Crew A"  (existing Crews module)
+-- CrewPosition: Engineer x 1       -> PositionRole: Engineer
+-- CrewPosition: Conductor x 1      -> PositionRole: Conductor/Foreman
+-- CrewPosition: Brakeman x 1       -> PositionRole: Brakeman/Switchman

CrewAttachmentTemplate  (existing Crews module)
+-- Links Crew -> AssignmentTemplate: "Job 101"
```

### 2.2 Entity Definitions

#### Department (New)

| Field | Type | Description |
|-------|------|-------------|
| CtrlNbr | PK (ControlNumber) | Auto-generated |
| RailroadCtrlNbr | FK -> DynamicGroup | Scoped to a railroad |
| Name | string (100) | Display name (e.g., "Operating") |

- Railroad-scoped reference data. Simple CRUD.
- No AlternateName — Department uses Name only.

#### Craft (Existing — Modify)

| Change | Description |
|--------|-------------|
| Add `DepartmentCtrlNbr` | FK -> Department, **nullable**. Existing crafts unaffected. |

#### PositionRole (Existing — Modify)

| Field | Change | Description |
|-------|--------|-------------|
| Code | Make nullable | Some railroads may not need short codes |
| AlternateName | Add (nullable, 100) | Alternate display name toggled by a tenant profile setting |

- AlternateName example: Name = "Brakeman/Switchman", AlternateName = "Switchman".
- The profile setting for toggling display is deferred — fields are added now.

### 2.3 Existing Crew Staffing Model (Reference — No Changes)

Position definitions on jobs are handled by the **Crews module**, not Work Management:

| Entity | Module | Purpose |
|--------|--------|---------|
| `Crew` | Crews | A named crew with a type and home group |
| `CrewPosition` | Crews | Defines a seat on a crew -> FK to `PositionRole` + `DisplayOrder` |
| `CrewIncumbency` | Crews | Who fills each `CrewPosition` (employee + date range) |
| `CrewAttachmentTemplate` | Crews | Links a `Crew` to an `AssignmentTemplate` (date range) |
| `CrewAttachmentInstance` | Crews | Links a `Crew` to a `WorkInstance` (override) |

The staffing chain is:

```
AssignmentTemplate <- CrewAttachmentTemplate -> Crew -> CrewPosition -> PositionRole -> Craft -> Department
```

- Crews define **what positions** they need (via `CrewPosition`).
- Crews are **attached to templates** (via `CrewAttachmentTemplate`), not the other way around.
- When a `WorkInstance` is generated, `PositionSlot` rows come from the crew's positions, not from the template directly.

> This spec does **not** modify any Crews module entities. The Crew CRUD UI is a separate effort.

---
## 3. API Changes (CrewService.API)

### 3.1 Domain Layer

| File | Action |
|------|--------|
| `Domain/Modules/WorkManagement/WorkManagementEntities.cs` | Add `Department` entity |
| `Domain/Modules/WorkManagement/WorkManagementEntities.cs` | Modify `PositionRole`: add `AlternateName`, make `Code` nullable, add `Update`/`Delete` methods |
| `Domain/Modules/WorkManagement/IWorkManagementRepositories.cs` | Add `IDepartmentRepository` |
| `Domain/Models/Seniority/Craft.cs` | Add nullable `DepartmentCtrlNbr` property |

### 3.2 Persistence Layer

| File | Action |
|------|--------|
| `Persistance/Modules/WorkManagement/WorkManagementConfigurations.cs` | Add `DepartmentConfiguration`; update `PositionRoleConfiguration` |
| `Persistance/Modules/WorkManagement/WorkManagementRepositories.cs` | Add `DepartmentRepository` |
| `Persistance/Configurations/WorkManagementModuleConfiguration.cs` | Register new entity configs |
| Migration | Single migration covering all schema changes |

### 3.3 Proto / gRPC

| Service | RPC | Status |
|---------|-----|--------|
| **WorkManagementSrvc** | `GetPositionRoles(craft_ctrl_nbr)` | Exists |
| | `CreatePositionRole(craft_ctrl_nbr, code, name)` | Exists |
| | `UpdatePositionRole(ctrl_nbr, code, name, alternate_name)` | **Add** |
| | `DeletePositionRole(ctrl_nbr)` | **Add** |
| **DepartmentSrvc** (new) | `GetDepartments(railroad_ctrl_nbr)` | **Add** |
| | `CreateDepartment(railroad_ctrl_nbr, name)` | **Add** |
| | `UpdateDepartment(ctrl_nbr, name)` | **Add** |
| | `DeleteDepartment(ctrl_nbr)` | **Add** |
| **CraftSrvc** (existing) | Add `department_ctrl_nbr` field to create/update/response messages | **Modify** |

> Department gets its own gRPC service following the one-service-per-domain-area pattern (CraftSrvc, InvitationSrvc, etc.).

### 3.4 Proto Messages (New/Modified)

```protobuf
// -- Department --
message GetDepartmentsRequest { int64 railroad_ctrl_nbr = 1; }
message GetDepartmentsResponse {
  repeated DepartmentResponse departments = 1;
  int32 total_count = 2;
}
message CreateDepartmentRequest {
  int64 railroad_ctrl_nbr = 1;
  string name = 2;
}
message UpdateDepartmentRequest {
  int64 ctrl_nbr = 1;
  string name = 2;
}
message DeleteDepartmentRequest { int64 ctrl_nbr = 1; }
message DepartmentResponse {
  int64 ctrl_nbr = 1;
  int64 railroad_ctrl_nbr = 2;
  string name = 3;
}

// -- PositionRole (additions to existing) --
message UpdatePositionRoleRequest {
  int64 ctrl_nbr = 1;
  string code = 2;
  string name = 3;
  string alternate_name = 4;
}
message DeletePositionRoleRequest { int64 ctrl_nbr = 1; }
// Modify existing PositionRoleResponse: add alternate_name field
```

### 3.5 Permission Feature Keys

| Key | Description |
|-----|-------------|
| `work-management/departments` | Department CRUD page |
| `work-management/assignment-templates` | Assignment Template management |
| `work-management/position-roles` | Position Role management |

---

## 4. Frontend Changes (CrewService.FrontEnd)

### 4.1 gRPC Clients

| File | Description |
|------|-------------|
| `Clients/WorkManagementClient.cs` | New. Template CRUD, PositionRole full CRUD |
| `Clients/DepartmentClient.cs` | New. Department CRUD |

### 4.2 Pages

| Route | File | Description |
|-------|------|-------------|
| `/work-management/departments` | `Components/Pages/WorkManagement/Departments.razor` | Railroad dropdown -> DataTable (Name) + Create/Edit/Delete modals |
| `/work-management/position-roles` | `Components/Pages/WorkManagement/PositionRoles.razor` | Craft dropdown -> DataTable (Code, Name, AlternateName) + Create/Edit/Delete modals |
| `/work-management/assignment-templates` | `Components/Pages/WorkManagement/AssignmentTemplates.razor` | Work-area dropdown -> DataTable + Create/Edit/Delete modals. |

All pages use `@rendermode InteractiveServer` with `AuthenticationStateProvider` for claims.
All modals follow the Modal component pattern (Modal with BodyContent/Footer render fragments, form id linking).
Edit/Delete buttons in row action columns on lister pages.

### 4.3 Navigation

Add **"Work Management"** nav group to `NavMenu.razor` (railroad-scoped section), containing:
- Departments
- Assignment Templates
- Position Roles

### 4.4 Existing Page Updates

| File | Change |
|------|--------|
| `Placeholder.razor` | Remove `@page "/daily/assignments"` route |
| `Components/Pages/Employees/Crafts` (existing) | Add department dropdown to create/edit (follow-up -- not in initial pass) |
| `Program.cs` | Register `WorkManagementClient`, `DepartmentClient` |

---

## 5. Deferred Items

| Item | Reason |
|------|--------|
| Work Instances, Position Slots, Bind/Unbind | Operational -- build after templates + roles are solid |
| Slot Requirements, Crew attachment | Depends on Work Instances |
| Instance generation from templates | Depends on Work Instances + Crew Positions |
| Crew CRUD UI (Crews, CrewPositions) | Separate feature -- Crews module already has domain entities |
| Craft <-> Department dropdown on Crafts page | Separate follow-up after Department entity exists |
| AlternateName display toggle (tenant config) | Fields added now, toggle wired later |
| Craft.AlternateName | Not needed yet -- only Department and PositionRole have dual naming |
| SignalR/real-time push | Deferred to broader SignalR buildout |

---

## 6. Key Design Decisions

1. **Department is its own gRPC service** -- follows the one-service-per-domain-area pattern in the codebase.
2. **Craft.DepartmentCtrlNbr is nullable** -- proper FK addition (not a join table workaround) per copilot instructions to prefer proper fixes.
3. **PositionRole.Code becomes nullable** -- not all railroads use short codes.
4. **Position definitions live on Crew, not AssignmentTemplate** -- the existing `CrewPosition` entity (Crews module) defines what positions a crew needs. Crews are attached to AssignmentTemplates via `CrewAttachmentTemplate`. This spec does not introduce a redundant TemplatePosition entity.
5. **Position Roles are craft-scoped, not railroad-scoped** -- the existing entity has no railroad FK; it is just `CraftCtrlNbr`. This is correct per domain model.
6. **Single migration** for all schema changes in this spec.