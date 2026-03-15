# Implementation Plan — UI Shell First, Then Railroad Setup Order

## Guiding Principle

Build the **UI shell first** — menu structure, context switcher, placeholder pages —
so the application feels navigable before any domain functionality is wired up.
Then implement API functionality in the same order a new railroad would be onboarded:
**invite admin → configure tenant → define crafts/rosters → build crews/jobs → enable operations.**

Each phase is a feature branch off `develop`. Each commit within a branch is atomic and buildable.

---

## Phases Overview

| Phase | Branch | What It Delivers | Depends On |
|-------|--------|------------------|------------|
| **0A** | `feature/ui-menu-shell` | Task-oriented sidebar menu with role-gated groups + placeholder pages | Nothing |
| **0B** | `feature/ui-context-switcher` | `AppContextService`, Parent→Railroad selector in top bar, `CascadingValue` propagation | Phase 0A |
| 1 | `feature/api-user-access` | Invitation + UserParentAssignment CRUD | Phase 0B |
| 2 | `feature/api-parent-railroad` | Parent + Railroad CRUD | Phase 1 (admin exists) |
| 3 | `feature/api-tenant-config` | GroupType, DynamicGroup, attributes, placements | Phase 2 |
| 4 | `feature/api-employee-foundation` | ContactTypes, EmploymentStatus, Employee CRUD | Phase 2 |
| 5 | `feature/api-seniority` | Craft, Roster, Seniority, SeniorityState | Phases 3 + 4 |
| 6 | `feature/api-crews-work` | Crews, positions, incumbencies, AssignmentTemplate, WorkInstance | Phases 3 + 5 |
| 7 | `feature/api-boards-bulletins` | ExtraBoard, BoardMember, cascade policy, bulletins/bids | Phases 5 + 6 |
| 8 | `feature/api-daily-ops` | ShiftDefinition/Instance, PositionSlotInstance, dispatching | Phases 6 + 7 |
| 9 | `feature/api-policies-payroll` | CraftOperationsPolicy, PayRate, EarningCodeRule, holidays | Phases 5 + 8 |
| 10 | `feature/api-absence-markoff` | AbsenceCode, AbsenceRequest, CompensationBalance, mark-off | Phase 9 |
| 11 | `feature/api-vacancy-assignment` | VacancyResolutionEngine, skip rules, dispatch overrides | Phases 7 + 10 |
| 12 | `feature/api-notifications` | NotificationRequest/Response, provider config, Teams webhook | Phase 8 |
| 13 | `feature/api-fra-compliance` | RegulatoryStandard, certifications, drug/alcohol, duty tours | Phases 5 + 8 |
| 14 | `feature/api-safety-info` | SafetyObservation, RailroadInformation (independent) | Phase 2 |
| 15 | `feature/api-reporting-exports` | PayrollExportBatch, PayrollImportRecord, formatters | Phase 9 |
| 16 | `feature/api-background-services` | All BackgroundService workers | Phases 8–13 |

---

## Phase 0A — Menu Shell (see `00A-menu-shell.md`)

Build the sidebar navigation with task-oriented collapsible groups, role-gated visibility,
and stub pages so every menu item has a landing page that says "Coming Soon."

**Menu groups:**
- Daily Operations — Dispatcher, CrewManager
- Crew Staffing — CrewManager, RailroadAdmin
- Employee Management — RailroadAdmin, CraftManager, ParentAdmin
- Payroll — PayrollClerk, RailroadAdmin
- Compliance — RailroadAdmin, CraftManager
- Information — all authenticated
- Administration — SystemAdmin, ParentAdmin, RailroadAdmin

## Phase 0B — Context Switcher (see `00B-context-switcher.md`)

Build a scoped `AppContextService` with `SelectedParent` / `SelectedRailroad`,
a dropdown selector in the `MainLayout` top bar, and a `CascadingValue` that
propagates context to all child pages.

**Key design decisions:**
- Context level is **Parent → Railroad** only (not work area).
- Work areas exist for compensation, not operational silos — filtering by work area
  happens on individual page toolbars, not the global context switcher.
- Single-parent users auto-select and skip the parent picker.
- Selection persisted to `ProtectedSessionStorage` so it survives page refreshes.

---

## Per-Phase Vertical Slice Pattern (Phases 1–16)

Every API phase follows the same implementation pattern across the clean architecture layers:

```
1. Domain      — Entity already exists; verify/enhance if needed
2. Proto       — Define or verify gRPC service + messages
3. Persistance — EF config + repository implementation
4. Presentation — gRPC service implementation
5. Tests       — Unit tests for domain logic + service layer
```

---

## What Already Exists (Status Baseline)

**Fully implemented gRPC services (Presentation layer):**
- ParentService, RailroadService, InvitationService, UserParentAssignmentService
- EmployeeService, CraftService, RosterService, SeniorityService
- AddressTypeService, EmailAddressTypeService, PhoneNumberTypeService
- EmploymentStatusService, EmploymentStatusHistoryService
- PayrollTierService, PriorServiceCreditService, SeniorityStateService
- AccountService, AuthService

**Module gRPC services (Presentation/Services/Modules/):**
- TenantConfigService, CrewsService, BoardsService, BulletinsService
- WorkManagementService, DailyOperationsService, DispatchingService
- MarkOffService, PoliciesService, PayrollService, PayrollEngineService
- HolidayManagementService, HolidayPayrollService
- VacancyAssignmentService, ElectronicCallingService
- FraComplianceService, SafetyService, RailroadInfoService
- RosterBoardService, ReportingExportsService, BackgroundServicesService

**Domain entities:** All defined across `Models/` (legacy) and `Modules/` (new).
**Persistence:** EF configurations and repositories exist for all entities.

**Blazor UI (existing):**
- `NavMenu.razor` — flat menu with Dashboard, Parents, Group Types, Login/Logout
- `MainLayout.razor` — top bar with title + username link (no context switcher)
- `AppThemeService` — Bootswatch theme switcher (singleton)
- `ParentsClient`, `RailroadsClient`, `TenantConfigClient` — gRPC clients ready
- `BaseGrpcClient<T>` — auth interceptor pattern for all clients
- CRUD pages: Parents, ParentDetail, GroupTypes, GroupTypeDetail, GroupDetail
- Account pages: Login, Register, Profile, Addresses, PhoneNumbers, EmailAddresses, Theme

> **Phases 0A/0B build the navigational skeleton. Phases 1–16 audit each API service
> for completeness — verify every proto RPC has a working implementation, every
> repository method is called, and every domain operation is exposed through the API.
> As each API phase completes, its placeholder page is replaced with real functionality.**
