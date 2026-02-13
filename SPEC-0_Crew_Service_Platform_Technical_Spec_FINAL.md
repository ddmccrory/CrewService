# SPEC-0-Crew Service Platform Technical Architecture Specification

_Build-handoff technical spec. Compiled from the provided scope/spec documents. Date: 2026-02-13 (America/Chicago)._

## 1. System Overview

### 1.1 Deployables
- **Crew.Api**: ASP.NET Core host exposing **canonical gRPC services** plus **REST transcoding** from the same `.proto` contracts.
- **Crew.Web**: **Blazor Server** application (server-side execution) calling Crew.Api via generated gRPC clients.

### 1.2 Target Framework
- Prefer **.NET 10** if permitted for the build; otherwise **.NET 8 LTS**.

### 1.3 Datastores
- **Development**: SQLite
- **Production**: SQL Server
- **Isolation model**: one physical database **per Parent tenant** (see Tenancy).

## 2. Tenancy, Authentication, Invitations

### 2.1 Tenant Model
- **Parent** = top-level tenant boundary (railroad/division).
- All requests operate within exactly one Parent context.

### 2.2 Parent Resolution (Authoritative Order)
Parent **MUST** be resolved **before**:
1) authentication/authorization, and 2) constructing any EF DbContext, because both depend on Parent-specific configuration.

**Resolution inputs** (in precedence order):
- **Subdomain** (preferred): `{parentSlug}.api.<domain>` and `{parentSlug}.web.<domain>`
- Optional header override for trusted internal callers: `X-Parent-Slug` (disabled for public clients unless explicitly allowed)

### 2.3 Parent Registry
A global registry store provides for each Parent:
- DB connection string (SQLite path in dev, SQL Server conn string in prod)
- Auth provider mode: External OIDC (Entra/ADFS) or Internal Identity
- OIDC authority/issuer, client IDs, audience, role-claim mapping settings

### 2.4 Invitations
- Invite-only onboarding.
- Invitation token is bound to **intended Parent** and cannot be redeemed across Parents.
- Invitation redemption produces an Identity user and stores audit trail (invited-by, redeemed-at, redeeming user).

### 2.5 Authorization
- Policy-based authorization with:
  - **RBAC** roles/permissions (App Roles preferred for Entra; groups optional)
  - **Scope checks**: WorkArea subtree access evaluation performed via a single service (e.g., `IAccessScopeService`).

## 3. Data Access & Persistence

### 3.1 DbContexts (Hard Requirement)
Exactly **two DbContexts** per Parent DB:
- `IdentityDbContext`: ASP.NET Core Identity + invitation tables + identity-audit
- `OperationsDbContext`: all operational domain tables

Each DbContext uses a distinct migrations history table (SQL Server) / separate migration assembly:
- `__EFMigrationsHistory_Identity`
- `__EFMigrationsHistory_Ops`

### 3.2 Modular Boundaries inside OperationsDbContext
- Operations tables are partitioned by **module ownership** (schema in SQL Server or naming prefix in SQLite).
- Each module ships:
  - EF entity mappings (`IEntityTypeConfiguration<T>`)
  - repositories for its aggregates
  - application services/use cases
- **Rule**: modules do not query/update other modules’ aggregates through EF navigation or repositories. Integrate via application interfaces and domain events.

### 3.3 Effective-Dating Standard
All effective-dated records use `[start_utc, end_utc)` semantics (end nullable for open-ended).
Validation rules:
- No overlap for the same natural key (e.g., employee craft membership, crew incumbency, roster membership).
- SQL Server can add filtered unique indexes where practical; SQLite relies on application-level validation.

## 4. Domain Modules

### 4.1 TenantConfig (Dynamic Groups & WorkAreas)
Responsibilities:
- GroupTypes, attribute definitions, and Dynamic Group tree maintenance.
- WorkArea designation (staffable leaves).
- Provide ancestry/subtree queries for scope checks and board applicability.

Key invariants:
- Tree must remain acyclic.
- WorkArea nodes must be addressable and stable IDs (GUID).

### 4.2 Employees
Responsibilities:
- Employee master record (identifiers, status).
- Contact methods (email/phone/address) with primary/verification flags.
- Crafts and craft eligibility.
- Qualifications with validity windows.
- Rosters/seniority lists per craft (effective-dated).
- Global availability state log (mark-off/mark-up etc.).
- Employee-to-Group membership integration (effective-dated), including **Home WorkArea** concept.
- Notifications: in-app notifications with group-based audience targeting and recipient snapshots.

Key invariants:
- Availability is **global** per employee (not board-specific).
- Exactly one active Home WorkArea membership at a time (unless policy allows Parent-wide roaming; represent as special scope).

### 4.3 WorkManagement (Templates, Instances, Slots)
Responsibilities:
- Define recurring work as AssignmentTemplates.
- Generate dated WorkInstances from templates; allow ad-hoc WorkInstances.
- Promote ad-hoc WorkInstances into templates.
- Define staffing demand as PositionSlots with SlotRequirements (equivalency/priorities).
- Authoritative **slot binding** and **unbinding** (single writer).

### 4.4 Crews (Regular & Relief)
Responsibilities:
- Define crews (Regular, Relief), their crew positions, and incumbency (effective-dated).
- Attach crews to AssignmentTemplates and/or override at WorkInstance level.
- Define relief coverage rules (e.g., which templates/days a relief crew covers).
- Provide incumbent prefill mapping from crew positions to PositionSlots using requirements equivalency.

Business rules (provided by user):
- Incumbent crew positions are filled unless employee is marked off.
- Incumbent (regular/relief) employees are not part of extra board.
- Each employee has **zero or one position** at a time; extra board slots count as positions.

### 4.5 Boards
Responsibilities:
- Define extra boards (primary and auxiliary).
- Boards are craft-specific and can be placed at any dynamic-group level above WorkAreas.
- Maintain board membership ordering/rotation state.
- Provide candidate iteration deterministically (including exhaustion state).

### 4.6 Dispatching
Responsibilities:
- Compute **projections** (non-binding) for vacancies.
- Execute **calling-time binding** (authoritative) with:
  - cascade selection (primary then auxiliary fallback per policy)
  - candidate ranking (policy-driven)
  - hard-stop eligibility checks (Employees)
  - global booking hold/no-overlap enforcement
  - binding via WorkManagement
  - advancing board state (Boards)
  - decision logs, skip reasons, override trails

### 4.7 AbsenceVacancy
Responsibilities:
- Absence/unavailability request intake (employee/dispatcher), approvals (if required).
- Translate effective absences/mark-offs into operational vacancy impacts on future WorkInstances/slots.
- Maintain causality links (absence → impacted slots) and audit.

### 4.8 Policies
Responsibilities:
- Agreement-driven policy selection & parameterization, scoped by Parent and Craft.
- Board ordering strategy + tie-breakers.
- Board cascade/applicability resolution strategy (including max levels and aux fallback controls).
- Calling window policy (how calling time is determined/used).
- Override policy (who can override what, and required reason codes).
- Displacement policy surface (rules vary by craft; configuration required).

## 5. Core Workflows

### 5.1 WorkInstance Generation + Incumbent Prefill
**Trigger**: scheduled generation or dispatcher ad-hoc creation.

Steps:
1. WorkManagement creates WorkInstance(s) for a WorkArea and time window.
2. WorkManagement creates baseline PositionSlots and any configured extra slots.
3. Crews resolves effective crew attachment for the WorkInstance.
4. For each PositionSlot, Crews attempts to map to a crew position using SlotRequirements priority.
5. Employees validates incumbent eligibility over the work window:
   - not marked off; availability permits
   - qualifications valid
   - no overlap/booking conflict
6. If eligible, WorkManagement binds slot to incumbent with `source=CREW_PREFILL` and records audit; otherwise slot remains OPEN.

### 5.2 Absence/Mark-Off → Vacancy Impact
Steps:
1. AbsenceVacancy records absence request or mark-off (with approval flow if enabled).
2. When effective, AbsenceVacancy computes impacted WorkInstances/slots and writes VacancyImpact records.
3. WorkManagement transitions impacted slots to OPEN (vacant) and stores causality reference.
4. When mark-up occurs (for open-ended), AbsenceVacancy closes the absence and stops future impacts after the mark-up time.

### 5.3 Projection (Non-Binding)
Steps:
1. Dispatching queries open vacancies (OPEN slots) from WorkManagement.
2. Policies resolves applicable boards for each vacancy (board placement in group ancestry; cascade mode; max levels).
3. Boards provides ordered candidates per board.
4. Employees filters by availability; qualifications may be evaluated for projection depending on policy (but must be strict at call time).
5. Dispatching returns projected winner + trace (boards considered, candidates, skip reasons). No writes to binding tables.

### 5.4 Calling-Time Binding (Authoritative)
Steps (per vacancy at calling time):
1. Policies resolves board cascade order: PRIMARY boards first, then AUX boards if enabled and PRIMARY exhausted.
2. For each board in order, iterate candidates deterministically.
3. For each candidate:
   - Employees performs **hard-stop** checks at call time (availability + qualification validity).
   - Dispatching attempts to acquire a global booking hold (atomic) for the vacancy window.
   - On hold success, WorkManagement binds employee to slot (idempotent), Boards advances state, Dispatching writes decision log.
   - On failure, record skip reason and continue.
4. Overrides:
   - Authorized users may override hard-stops; override requires reason and is fully audited; decision log records override path.

### 5.5 Displacement Window → Default Placement (Policy Surface)
Displacement rules vary by craft; system must provide a configuration surface and workflow artifacts.

Required minimum data model:
- `craft_displacement_policy` (craft_id, window_hours, seniority_basis, default_action, eligibility_selector_json)
- `displacement_case` (employee_id, craft_id, opened_utc, expires_utc, status)
- `displacement_claim` (case_id, target_employee_id, submitted_utc, decision, decided_utc, reason)

Required workflow:
1. When an employee loses a position and would become None, open a DisplacementCase with expiry now+X hours.
2. During window, allow displacement claims vs less-senior employees in the same craft within allowed scope.
3. On approval, transfer position assignment and open case for displaced employee (chain).
4. On expiry, apply default action (often place onto extra board position) or mark as pending manual placement.

## 6. Concurrency, Idempotency, Determinism

### 6.1 Global No-Double-Booking
- Use `employee_booking` (or equivalent) to enforce exclusive assignment across overlapping work windows.
- Booking acquisition is atomic (transaction + unique constraint on overlap via application logic; SQL Server can use exclusion-like patterns with range checks in transaction).

### 6.2 Idempotent Commands
- All write commands that can be retried must accept an idempotency key (`request_id`) and store command results.
- `BindEmployeeToSlot` must be idempotent for the same (slot, request_id).

### 6.3 Deterministic Candidate Ordering
- Board candidate iteration is deterministic for a given snapshot:
  - stable sorting keys (policy-defined) + stable tie-breakers (employee_id).
- Skip reasons must be captured as structured codes.
- Decision logs must allow replay.

## 7. Relational Schema (Contractor-Implementable)

### 7.1 IdentityDbContext
Tables (ASP.NET Core Identity with GUID keys):
- `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`
- `invite`:
  - `invite_id` (PK)
  - `parent_id` (FK to ParentRegistry entry)
  - `email`
  - `token_hash`
  - `expires_utc`
  - `redeemed_utc` nullable
  - `redeemed_user_id` nullable
  - audit fields (`created_by_user_id`, `created_utc`)

### 7.2 OperationsDbContext — TenantConfig
`group_type`:
- `group_type_id` PK, `name`, `description`, `flags_json`, audit
`dynamic_group`:
- `group_id` PK, `group_type_id` FK, `name`, `parent_group_id` FK nullable, `path` (materialized path) or closure table, `is_work_area` bool, audit
`group_attribute_definition` / `group_attribute_value`:
- definition per group_type and values per group.

Indexes:
- `dynamic_group(parent_group_id, name)`
- `dynamic_group(path)` if using materialized path

### 7.3 OperationsDbContext — Employees
`employee`:
- `employee_id` PK, `employee_number` unique, `first_name`, `last_name`, `status`, `hire_date`, audit
`employee_contact`:
- `employee_contact_id` PK, `employee_id` FK, `type` (EMAIL/PHONE/ADDRESS), `value`, `is_primary`, `is_verified`, audit
`craft`:
- `craft_id` PK, `code` unique, `name`
`employee_craft` (effective):
- `employee_craft_id` PK, `employee_id` FK, `craft_id` FK, `start_utc`, `end_utc`
`qualification_type`:
- `qualification_type_id` PK, `code` unique, `name`, `craft_id` nullable
`employee_qualification` (effective):
- `employee_qualification_id` PK, `employee_id` FK, `qualification_type_id` FK, `start_utc`, `end_utc`, `evidence_ref` nullable
`craft_roster` (effective):
- `craft_roster_id` PK, `craft_id` FK, `scope_group_id` FK nullable, `start_utc`, `end_utc`
`craft_roster_member` (effective):
- `craft_roster_member_id` PK, `craft_roster_id` FK, `employee_id` FK, `seniority_rank` int, `seniority_date` date, `start_utc`, `end_utc`
`availability_log`:
- `availability_log_id` PK, `employee_id` FK, `state` enum, `reason_code`, `start_utc`, `end_utc` nullable, `entered_by_user_id`, `notes`
`employee_group_membership` (effective):
- `employee_group_membership_id` PK, `employee_id` FK, `group_id` FK, `membership_type` (HOME/ASSIGNED/ELIGIBLE/etc.), `start_utc`, `end_utc`
Notifications:
- `notification` (id, type, payload_json, created_by, created_utc)
- `notification_audience` (notification_id, audience_type USER/GROUP, ref_id)
- `notification_recipient_snapshot` (notification_id, employee_id, delivered_utc, read_utc nullable)

### 7.4 OperationsDbContext — WorkManagement
`assignment_template`:
- `assignment_template_id` PK, `work_area_group_id` FK, `code`, `name`, `recurrence_json`, `is_active`, audit
`work_instance`:
- `work_instance_id` PK, `assignment_template_id` FK nullable (ad-hoc), `work_area_group_id` FK, `start_utc`, `end_utc`, `call_time_utc`, `status`, audit
`position_role`:
- `position_role_id` PK, `craft_id` FK, `code`, `name`
`position_slot`:
- `position_slot_id` PK, `work_instance_id` FK, `position_role_id` FK, `status`, `bound_employee_id` FK nullable, `binding_source` enum, `created_utc`, audit
`slot_requirement`:
- `slot_requirement_id` PK, `position_slot_id` FK, `priority` int, `position_role_id` FK nullable, `qualification_type_id` FK nullable, `notes`
`slot_binding_audit`:
- `slot_binding_audit_id` PK, `position_slot_id` FK, `from_employee_id` nullable, `to_employee_id` nullable, `reason_code`, `source`, `changed_by_user_id`, `changed_utc`, `correlation_id`

### 7.5 OperationsDbContext — Crews
`crew`:
- `crew_id` PK, `crew_type` (REGULAR/RELIEF), `home_group_id` FK, `name`, `is_active`, audit
`crew_position`:
- `crew_position_id` PK, `crew_id` FK, `position_role_id` FK, `display_order`
`crew_incumbency` (effective):
- `crew_incumbency_id` PK, `crew_position_id` FK, `employee_id` FK, `start_utc`, `end_utc`
`crew_attachment_template` (effective):
- `crew_attachment_template_id` PK, `assignment_template_id` FK, `crew_id` FK, `start_utc`, `end_utc`
`crew_attachment_instance` (effective):
- `crew_attachment_instance_id` PK, `work_instance_id` FK, `crew_id` FK, `start_utc`, `end_utc`
`relief_coverage_rule` (effective):
- `relief_coverage_rule_id` PK, `relief_crew_id` FK, `assignment_template_id` FK, `days_of_week_mask`, `start_utc`, `end_utc`

### 7.6 OperationsDbContext — Boards
`extra_board`:
- `extra_board_id` PK, `craft_id` FK, `placed_group_id` FK, `board_kind` (PRIMARY/AUXILIARY), `name`, `is_active`, audit
`board_member` (effective):
- `board_member_id` PK, `extra_board_id` FK, `employee_id` FK, `order_index`, `state_json`, `start_utc`, `end_utc`
`board_cascade_policy`:
- `board_cascade_policy_id` PK, `work_area_group_id` FK, `craft_id` FK, `cascade_mode` (WORKAREA_ONLY/UP_HIERARCHY), `max_levels`, `aux_enabled`, `aux_max_levels`, `selection_strategy`, audit

### 7.7 OperationsDbContext — Dispatching
`dispatch_projection` (optional persisted):
- `dispatch_projection_id` PK, `position_slot_id` FK, `as_of_utc`, `projected_employee_id` FK nullable, `trace_json`, `computed_utc`
`dispatch_decision_log`:
- `dispatch_decision_log_id` PK, `position_slot_id` FK, `as_of_utc`, `selected_employee_id` FK nullable, `decision_json`, `created_utc`
`dispatch_override`:
- `dispatch_override_id` PK, `position_slot_id` FK, `requested_by_user_id`, `approved_by_user_id` nullable, `reason_code`, `status`, `created_utc`, `decided_utc` nullable
`employee_booking`:
- `employee_booking_id` PK, `employee_id` FK, `start_utc`, `end_utc`, `position_slot_id` FK nullable, `created_utc`, `created_by_user_id`

### 7.8 OperationsDbContext — AbsenceVacancy
`absence_request`:
- `absence_request_id` PK, `employee_id` FK, `start_utc`, `end_utc` nullable, `reason_code`, `status` (PENDING/APPROVED/DENIED/CANCELLED), `requested_by_user_id`, `approved_by_user_id` nullable, audit
`vacancy_impact`:
- `vacancy_impact_id` PK, `absence_request_id` FK, `position_slot_id` FK, `impact_start_utc`, `impact_end_utc` nullable, audit

## 8. gRPC Contracts (Service Surface)

One `.proto` per module. REST transcoding annotations live in the same `.proto`.

### TenantConfigService
- CRUD: GroupTypes, Groups, Attributes
- Queries: GetTree, GetAncestors, GetWorkAreas

### EmployeesService
- Employee CRUD
- Set crafts, qualifications, roster memberships
- Availability commands (MarkOff, MarkUp, etc.)
- Notifications (Create, List, MarkRead)

### WorkManagementService
- Template CRUD, GenerateInstances, CreateAdHocInstance, PromoteToTemplate
- Slot CRUD, UpdateRequirements
- Bind/Unbind slot (idempotent)

### CrewsService
- Crew CRUD, Positions CRUD
- Set incumbency (effective-dated)
- Attach to template / override instance
- Relief coverage rule CRUD

### BoardsService
- Board CRUD, Member CRUD (effective-dated)
- Board state query

### DispatchingService
- List vacancies
- Get projections / recompute projections
- Execute call (binding) with optional override request/approval flows
- Get decision logs / audit

### AbsenceVacancyService
- Submit/Approve/Deny/Cancel absence
- Apply vacancy impacts batch

### PoliciesService (Admin)
- Configure per-craft policies: ordering, cascade, calling windows, override, displacement policy keys

## 9. Blazor Server Front End (Technical)

### 9.1 Architecture
- Blazor Server hosted as Crew.Web; communicates with Crew.Api using generated gRPC clients.
- Authentication via OIDC configured per Parent; token/session stored server-side; gRPC calls attach access token.

### 9.2 Page Map (MVP)
- Admin:
  - `/admin/parents` (optional, for superadmin) 
  - `/admin/groups`, `/admin/work-areas`
  - `/admin/crafts`, `/admin/qualifications`, `/admin/rosters`
  - `/admin/crews`, `/admin/boards`, `/admin/policies`
  - `/admin/invites`, `/admin/users`, `/admin/audit`
- Dispatch:
  - `/dispatch/instances`, `/dispatch/vacancies`, `/dispatch/boards`, `/dispatch/decision-logs`
- Employee:
  - `/employee/availability`, `/employee/schedule`, `/employee/notifications`, `/employee/profile`

### 9.3 Authorization Gates
- Each page has RBAC + scope constraints; Dispatch pages additionally require craft permissions.

## 10. Implementation Order (Contractor Plan)
1. Tenancy foundation: Parent registry + resolver middleware + per-Parent auth config.
2. Identity + invitations.
3. OperationsDbContext scaffolding + TenantConfig.
4. Employees core (availability, crafts, quals, rosters, memberships) + Employee pages.
5. WorkManagement (templates/instances/slots) + Dispatch pages baseline.
6. Crews + incumbent prefill + relief coverage.
7. Boards + Policies configuration surfaces.
8. Dispatching: projections + calling-time binding + decision logs + overrides + booking holds.
9. AbsenceVacancy: approvals + vacancy impacts end-to-end.
10. Displacement policy surface + case workflow skeleton (no craft hardcoding).

## 11. Validation & Acceptance
Required automated tests:
- Effective-date overlap validators.
- No-double-booking under concurrency.
- Deterministic calling order and skip reason capture.
- Cross-Parent access rejection.

Operational monitoring:
- Decision log volume, override rate, time-to-fill.
- Booking hold conflicts (contention).

## 12. References (Inputs)
- Crew Service Application Scope.docx
- spec_4_crew_and_assignment_staffing_model.md
- spec_5_boards_and_dispatching_addendum.md
- spec_6_authentication_tenancy_and_invitations.md
- spec_employee_module.md
- spec_employee_module_merged.md
- spec_employee_module_integration_into_dynamic_group_hierarchy.md

## Need Professional Help in Developing Your Architecture?
Please contact me at https://sammuti.com :)