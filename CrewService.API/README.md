# Crew Service Platform — API

The Crew Service Platform is a comprehensive railroad crew management and dispatch system built as a **modular monolith** targeting **.NET 10**. It exposes a gRPC-first API surface with REST/JSON transcoding, enabling both native gRPC clients (Blazor front end) and standard HTTP consumers to interact with the same endpoints.

The platform manages the complete lifecycle of railroad crew operations:

- **Organizational hierarchy** — Dynamic, tenant-defined group structures. Group Types define the tiers (e.g. Region, Subdivision, WorkArea). Dynamic Groups form a parent-child tree. Groups flagged as work areas are the operational level where crafts, rosters, boards, crews, and policies are defined. Railroads are placed at any group level via Railroad Placements, allowing flexible organizational modeling. Custom attributes attach arbitrary metadata to groups of a given type
- **Employee management** — Full employee records: demographics (gender, race, marital status, birth date), employment data (employment date, status, employee number), operational flags (allow FMLA, call for overtime, process payroll, tie up off property), and identification (SSN and driver's license encrypted at rest). Each employee has typed addresses, phone numbers (with calling order for electronic calling), and email addresses. Prior service credit, employment status history, and seniority entries complete the employee profile
- **Crafts** — The central organizing unit of the platform. Each work area defines its crafts (e.g. Engineer, Conductor, Clerical). A craft carries agreement-driven configuration that governs all downstream behavior: mark-off/mark-up hours, required rest hours, unpaid meal period minutes, Hours of Service applicability, vacation rules, and payroll processing flags. Policies, boards, bulletins, displacement, dispatching, and payroll all key off the craft
- **Rosters** — A roster belongs to a specific craft + railroad combination and represents a named seniority list (e.g. "Engineer Roster" for CSX at Jax Yard). Rosters carry purpose flags — `Training` (student roster), `ExtraBoard` (extra board roster), and `OvertimeBoard` (overtime roster). Employees are placed on rosters via seniority entries
- **Seniority** — A seniority entry places an employee on a roster with a rank, roster date, seniority state (active, furloughed, on leave, etc.), and training eligibility flag. The `LastActiveRoster` flag marks the employee's current roster. Seniority rank drives bulletin bid priority, displacement order, extra board calling order, and seniority move eligibility
- **Work management** — Assignment templates define reusable job definitions per work area and craft (e.g. "Q101 Jacksonville-Waycross"). Work instances are specific occurrences on a date with start/end/call times. Position slots define the staffing need per instance (e.g. 1 Engineer, 1 Conductor). Slots are filled by binding employees. Position roles define the types of positions a craft supports
- **Crew composition** — Regular crews are assigned to specific assignment templates; relief crews fill in on rest days. Each crew has ordered positions (linked to position roles like Engineer, Conductor). Incumbencies assign employees to crew positions for date ranges. Crew attachment templates link crews to the assignments they work. Relief coverage rules define which templates a relief crew covers and on which days of the week
- **Extra boards & cascade policies** — Extra boards are seniority-ordered pools of available employees per craft + work area, typed as PRIMARY or AUXILIARY. Board members are ordered by seniority rank. Cascade policies define how unfilled positions escalate up the group hierarchy by craft — specifying the search strategy (e.g. UP_HIERARCHY), maximum levels to search, and ordering method (e.g. SENIORITY)
- **Bulletins & vacancy** — Position vacancies are scoped to a craft and can target crew positions, board positions, or position slots. When bulletined, a bid window opens (duration governed by the craft's BulletinPolicy). Employees bid with a priority preference; bids are ranked by seniority. The highest-seniority bidder is awarded the position. Lower-priority bids are auto-withdrawn
- **Dispatching** — Projections show upcoming unfilled slots across work areas. Dispatchers execute crew calls to assign employees (typically the next available on the extra board by seniority). Every call attempt is recorded in decision logs. Override requests allow out-of-order assignments and require approval. Employee bookings reserve employees for future slots
- **Daily operations** — The call sheet is the central daily view for a work area showing all assignments, slots, and employee statuses. PlaceOnDuty/TieUp record the on-duty/off-duty lifecycle (feeding FRA compliance tracking and payroll time entry generation). Annul cancels a position slot for the day
- **Mark-off & absence** — Employees submit absence requests with an absence code, date range, and optional position slot. Requests go through approval workflow (governed by craft's ApproveAllMarkOffs flag and MarkOffHours window). Approved absences deduct from compensation balances. When an absence vacates a position, the system flags it for vacancy resolution
- **Policies** — Per-craft rules driven by labor agreement terms. Displacement policies govern how employees are displaced from positions based on seniority rank (window hours, ordering strategy, extra board fallback). Bulletin policies control vacancy posting duration. Seniority move policies define when employees can exercise seniority to move between rosters within a craft
- **Payroll** — Time entries are created from tie-up records (or manually) with employee, date, type (regular/overtime), and hours. Payroll runs aggregate entries for a pay period and progress through OPEN → LOCKED → APPROVED. The payroll engine resolves earning codes based on craft/tier/work rules, runs trial calculations for preview, and locks final results. Holiday pay qualification evaluates worked-day-before/after rules per employee
- **FRA compliance** — Federal Railroad Administration Hours of Service tracking. Duty tours are automatically generated from on-duty/tie-up records, tracking total on-duty time, rest time, and compliance status (12-hour on-duty limit with 10-hour mandatory rest). Employee certifications track FRA-required credentials (e.g. locomotive engineer certification) with issue/expiration dates. Only crafts with `HoursOfService = true` are subject to FRA tracking
- **Safety** — BeSafe-style observation reporting. Employees or supervisors report safety observations at a work area, categorized by safety category (e.g. track hazard, equipment defect). Corrective actions are assigned to observations. Once all actions are complete, the observation is resolved. Dashboard views show open and resolved observations per work area
- **Railroad information** — Operational notices (e.g. track conditions, speed restrictions) with a full lifecycle: DRAFT → PUBLISHED → CLOSED (or CANCELLED). Published notices are visible to employees at a work area. Employees acknowledge reading via read receipts; management can verify compliance by querying receipt status
- **Reporting** — Payroll export generates batches from locked payroll runs for external payroll systems. Payroll import ingests adjustments and corrections. Daily report generation produces operational summaries per work area (staffing levels, absences, overtime, vacancy counts)
- **Electronic calling** — Automated crew call notifications sent to employee phone numbers in calling order (respecting the DialOne prefix flag from employee phone configuration). Status polling tracks delivery state: sent, delivered, acknowledged, no answer, declined
- **Background services** — Configurable worker schedules for periodic tasks: outbox message publishing, vacancy resolution polling, payroll deadline reminders, and FRA compliance checks. Workers can be enabled/disabled and rescheduled without redeployment. Execution logs record each run's status and errors

The system supports **multi-tenant isolation** (multiple Parent corporations coexist in the same database, logically isolated by Parent scoping), **invite-only user access** with per-parent role-based authorization

## Table of Contents

- [Summary](#summary)
- [Quickstart](#quickstart)
- [Architecture Overview](#architecture-overview)
- [Solution Structure](#solution-structure)
- [Domain Modules](#domain-modules)
- [Repository Layout](#repository-layout)
- [Data Strategy](#data-strategy)
- [Orchestration Unit of Work](#orchestration-unit-of-work)
- [Domain Events & Outbox](#domain-events--outbox)
- [API Endpoint Reference](#api-endpoint-reference)
  - [Authentication & Account](#authentication--account)
  - [Tenant Configuration](#tenant-configuration)
  - [Organization](#organization)
  - [Employee Management](#employee-management)
  - [Crafts, Seniority & Rosters](#crafts-seniority--rosters)
    - [Crafts](#crafts) — Central organizing unit per work area; agreement-driven configuration
    - [Rosters](#rosters) — Named seniority lists per craft + railroad
    - [Seniority](#seniority) — Employee placement on a roster with rank, date, and state
    - [Seniority States](#seniority-states) — Reference data classifying roster standing
    - [Payroll Tiers](#payroll-tiers) — Pay rate brackets per work area
  - [Work Management](#work-management)
  - [Crews](#crews)
  - [Boards](#boards)
  - [Policies](#policies)
  - [Bulletins & Vacancy](#bulletins--vacancy)
  - [Dispatching](#dispatching)
  - [Daily Operations](#daily-operations)
  - [Mark-Off & Absence](#mark-off--absence)
  - [Vacancy Assignment](#vacancy-assignment)
  - [Payroll](#payroll)
  - [Payroll Engine](#payroll-engine)
  - [Holiday Management](#holiday-management)
  - [Holiday Payroll](#holiday-payroll)
  - [Reporting & Exports](#reporting--exports)
  - [Electronic Calling](#electronic-calling)
  - [Background Services](#background-services)
  - [Roster Board](#roster-board)
  - [FRA Compliance](#fra-compliance)
  - [Railroad Information](#railroad-information)
  - [Safety](#safety)
- [Field Encryption](#field-encryption)
- [Soft Delete & Global Query Filters](#soft-delete--global-query-filters)
- [Roles & Authorization](#roles--authorization)
- [Development Notes](#development-notes)
  - [Testing with Swagger](#testing-with-swagger)
- [Testing](#testing)
- [Spec Sheets](#spec-sheets)
- [License](#license)

---

## Summary

| Attribute | Value |
|---|---|
| **Runtime** | .NET 10, C# 14 |
| **Architecture** | Modular monolith — single process, many domain modules |
| **API Surface** | Canonical gRPC + REST/JSON via gRPC-JSON transcoding (same `.proto` contracts) |
| **Front End** | Blazor Server (`CrewService.FrontEnd`) calling the API via generated gRPC clients |
| **Datastores** | SQLite (dev), SQL Server or PostgreSQL (prod); all Parent tenants share the same physical database |
| **Tenancy** | Parent-based multi-tenant with logical isolation (all Parents in one database, scoped by Parent reference) |
| **Auth** | JWT Bearer tokens; role-based authorization per parent tenant |
| **Swagger** | Available at `/swagger` in Development environment (gRPC transcoding endpoints) |

## Quickstart

**Prerequisites:**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2026 (recommended)
- Database engine (SQLite for dev, SQL Server or PostgreSQL for production)

**Steps:**

```bash
git clone https://github.com/ddmccrory/CrewService.git
cd CrewService
dotnet restore
```

Configure user secrets for the `CrewService.GrpcService` project:

```bash
dotnet user-secrets set "Jwt:Key" "<your-256-bit-secret>"
dotnet user-secrets set "Encryption:Key" "<base64-encoded-AES-256-key>"
```

Run the API (auto-migrates and seeds in Development):

```bash
dotnet run --project CrewService.API/CrewService.GrpcService
```

Browse Swagger UI at `https://localhost:<port>/swagger`.

**Default admin credentials (dev seed):** `admin@crewservice.dev` / `Admin@123`

## Architecture Overview

**Fundamental principles:**

- Organizational structure is **dynamic and tenant-defined** via the Dynamic Group hierarchy (regions, subdivisions, work areas — all configurable)
- Operational behavior is **policy-driven and agreement-aware** — each craft at each work area carries its own agreement-driven configuration (rest hours, mark-off/mark-up hours, meal periods, HOS applicability, vacation rules), and craft-level policies govern displacement windows, bulletin posting durations, and seniority move eligibility. Rosters organize employees by seniority rank within a craft + railroad, and that rank drives bid priority, displacement order, and calling order
- No operational logic depends on hard-coded hierarchy levels
- Modules integrate via **in-process application interfaces** and **domain events** — never direct cross-module `DbContext` access

**Deployables:**

| Deployable | Description |
|---|---|
| **CrewService.GrpcService** | ASP.NET Core host — gRPC endpoints (canonical) + REST/JSON transcoding (secondary), Swagger in dev |
| **CrewService.FrontEnd** | Blazor Server app calling the API via generated gRPC-Web clients |

## Solution Structure

The solution follows Clean Architecture with a modular domain layer:

| Project | Layer | Responsibility |
|---|---|---|
| `CrewService.GrpcService` | Host | Entry point, DI composition root, middleware pipeline, `DevDataSeeder` |
| `CrewService.Presentation` | Presentation | gRPC service implementations, proto definitions |
| `CrewService.Application` | Application | Use cases, application services, DTOs |
| `CrewService.Domain` | Domain | Aggregates, entities, value objects, domain events, repository interfaces |
| `CrewService.Infrastructure` | Infrastructure | Cross-cutting: exception interceptors, Identity `User` model, outbox dispatcher |
| `CrewService.Persistance` | Persistence | EF Core `DbContext`s, migrations, repository implementations, encryption, UoW |
| `CrewService.UnitTests` | Tests | xUnit tests with in-memory SQLite fixtures |

## Domain Modules

Each module owns its entities, repository interfaces, domain events, proto contracts, and gRPC service implementation:

| Module | Bounded Context | Key Entities |
|---|---|---|
| **TenantConfig** | Tenant-defined organizational hierarchy. Group Types define tiers (e.g. Region, Subdivision, WorkArea). Dynamic Groups form a parent-child tree. Groups flagged as work areas are the operational level where crafts, rosters, and crews are defined. Railroads are placed at any group level. Custom attributes attach arbitrary metadata to groups. | GroupType, DynamicGroup, RailroadGroupPlacement, GroupAttributeDefinition, GroupAttributeValue |
| **Employees** | Full employee lifecycle: demographics, employment data, operational flags, identification (SSN/DL encrypted at rest), nested addresses/phones/emails (typed by contact type), prior service credit, employment status tracking with history | Employee, Address, PhoneNumber, EmailAddress, EmploymentStatus, EmploymentStatusHistory, PriorServiceCredit |
| **Contact Types** | Reference data for contact information | AddressType, PhoneNumberType, EmailAddressType |
| **Crafts** | Central organizing unit per work area (e.g. Engineer, Conductor, Clerical). Carries agreement-driven configuration: mark-off/mark-up hours, required rest hours, meal periods, HOS applicability, vacation rules, payroll flags. All policies, boards, bulletins, displacement, and dispatching key off the craft | Craft |
| **Rosters** | Named seniority lists per craft + railroad (e.g. "Engineer Roster" for CSX at Jax Yard). Carry purpose flags: Training, ExtraBoard, OvertimeBoard. Employees are placed on rosters via seniority entries | Roster |
| **Seniority** | Places an employee on a roster with a rank, roster date, seniority state, and training eligibility. Rank drives bulletin bid priority, displacement order, extra board calling order, and seniority move eligibility | Seniority, SeniorityState |
| **Organization** | Parent = top-level corporate tenant (logical isolation within a shared database — all operational data is scoped by Parent). Railroad = rail carrier under a parent (identified by RR mark). PayrollTier = pay rate brackets scoped to a work area. | Parent, Railroad, PayrollTier |
| **UserAccess** | Invite-only user access. Invitations link email + Parent + role. UserParentAssignment links a registered user to a Parent with a role; a user can have assignments to multiple Parents. JWT claims are built from these assignments at login. | UserParentAssignment, Invitation, Roles |
| **WorkManagement** | AssignmentTemplate = reusable job definition per work area + craft. WorkInstance = specific occurrence on a date. PositionSlot = staffing need on an instance (filled by binding employees). PositionRole = type of position a craft supports (e.g. Engineer, Conductor). | AssignmentTemplate, WorkInstance, PositionRole, PositionSlot |
| **Crews** | Regular/relief crews per work area + craft. CrewPositions define ordered slots (linked to PositionRoles). Incumbency assigns an employee for a date range. Attachment templates link crews to assignment templates. Relief coverage rules define which templates a relief crew covers and on which days (bitmask). | Crew, CrewPosition, CrewIncumbency, CrewAttachmentTemplate, ReliefCoverageRule |
| **Boards** | Extra boards per craft + work area (PRIMARY/AUXILIARY), board membership with seniority-ordered positions, cascade policies that define how unfilled positions escalate up the group hierarchy by craft | ExtraBoard, BoardMember, BoardCascadePolicy |
| **Policies** | Per-craft displacement rules (window hours, seniority-based order, extra board fallback), bulletin posting rules (duration per craft), and seniority move rules (eligibility window, roster-date-based order) | CraftDisplacementPolicy, BulletinPolicy, SeniorityMovePolicy |
| **Bulletins** | Position vacancies per craft, bulletin posting with bid windows (duration governed by craft-level BulletinPolicy), employee bidding ranked by seniority, award to highest-seniority bidder | PositionVacancy, Bulletin, BulletinBid |
| **Dispatching** | Projections show upcoming unfilled slots. ExecuteCall dispatches employees by seniority/cascade order. Decision logs record every call attempt and outcome. Overrides allow out-of-order assignments (require approval). Bookings reserve employees for future slots. | DispatchProjection, DispatchDecisionLog, DispatchOverride, EmployeeBooking |
| **DailyOperations** | Call sheet = daily view of all assignments/slots/employees for a work area. PlaceOnDuty/TieUp record the on-duty/off-duty lifecycle (feeds FRA tracking and payroll). AnnulPosition cancels a slot for the day. | OnDutyRecord, OffDutyRecord, DailyPositionSlot |
| **MarkOff** | Absence request workflow: employee submits with absence code + date range, manager approves/declines. Compensation balance tracks available leave. Absence codes are reference data per work area. Craft’s MarkOffHours and ApproveAllMarkOffs govern the window and auto-approval. | AbsenceRequest, CompensationBalance, AbsenceCode |
| **AbsenceVacancy** | When an approved absence vacates a position slot, the system flags the slot as vacant and feeds it into the Vacancy Assignment engine for automatic resolution | AbsenceRequest (absence-vacancy flow) |
| **VacancyAssignment** | Automated engine that evaluates all open vacancies for a work area by craft, applies displacement and cascade policies, and assigns the highest-priority available employee to each slot. Resolution runs are logged for audit. | VacancyResolutionRun |
| **Payroll** | TimeEntry = hours worked by employee on a date (from tie-ups or manual). PayrollRun = aggregation for a pay period (OPEN → LOCKED → APPROVED). PayrollRecord = per-employee result in a run. | TimeEntry, PayrollRun, PayrollRecord |
| **PayrollEngine** | Calculation engine: resolves earning codes based on craft/tier/work rules, runs trial calculations for preview, locks final results, and supports per-earning approval for exceptions | EarningCodeResult, PayrollRunStatus |
| **HolidayManagement** | US federal holiday catalog with observed-date computation. GenerateHolidaysForYear creates concrete holiday records per work area + year, used by Holiday Payroll for qualification evaluation. | Holiday |
| **HolidayPayroll** | Evaluates employee qualification for holiday pay based on worked-day-before/after rules, absence codes, and seniority state | HolidayQualification |
| **ReportingExports** | Payroll export to external systems, import of adjustments/corrections, daily operational report generation with staffing/absence/overtime summaries | PayrollExportBatch, DailyReport |
| **ElectronicCalling** | Sends automated crew call notifications to employee phone numbers (in calling order per employee phone config), tracks delivery status (sent, delivered, acknowledged, declined) | NotificationRequest |
| **BackgroundServices** | Configurable worker schedules (enable/disable, cron expression) for outbox publishing, vacancy polling, payroll reminders, FRA checks. Execution logs record each run’s status and errors. | WorkerSchedule, ExecutionLog |
| **RosterBoard** | Visual display of a roster's seniority-ordered employee positions. Supports hangout (temporarily removing an employee from the active board) and restore operations | RosterBoardPosition |
| **FraCompliance** | FRA Hours of Service duty tours, employee certifications | FraDutyTour, EmployeeCertification |
| **RailroadInfo** | Operational notices, publish/close lifecycle, read receipts | RailroadInformation, ReadReceipt |
| **Safety** | Safety observations, corrective actions, resolution workflow | SafetyObservation, SafetyAction, SafetyCategory |

**Boundary rule:** Modules never call each other's EF Core `DbContext` directly. Cross-module integration uses in-process application interfaces or domain events.

## Repository Layout

```
CrewService/
├── CrewService.API/
│   ├── CrewService.GrpcService/         # Host: entry point, DI root, DevDataSeeder
│   ├── CrewService.Presentation/        # gRPC service implementations
│   │   └── Services/
│   │       ├── AuthService.cs           # Login, register, token refresh
│   │       ├── AccountService.cs        # Profile, theme management
│   │       ├── EmployeeService.cs       # Employee CRUD + addresses/phones/emails
│   │       ├── ParentService.cs         # Parent tenant CRUD
│   │       ├── RailroadService.cs       # Railroad CRUD
│   │       ├── InvitationService.cs     # Invite-only user onboarding
│   │       ├── UserParentAssignmentService.cs
│   │       ├── CraftService.cs, RosterService.cs, SeniorityService.cs, SeniorityStateService.cs
│   │       ├── AddressTypeService.cs, PhoneNumberTypeService.cs, EmailAddressTypeService.cs
│   │       ├── EmploymentStatusService.cs, EmploymentStatusHistoryService.cs
│   │       ├── PayrollTierService.cs, PriorServiceCreditService.cs
│   │       └── Modules/
│   │           ├── TenantConfigService.cs
│   │           ├── WorkManagementService.cs
│   │           ├── CrewsService.cs
│   │           ├── BoardsService.cs
│   │           ├── PoliciesService.cs
│   │           ├── BulletinsService.cs
│   │           ├── DispatchingService.cs
│   │           ├── DailyOperationsService.cs
│   │           ├── MarkOffService.cs
│   │           ├── AbsenceVacancyService.cs
│   │           ├── VacancyAssignmentService.cs
│   │           ├── PayrollService.cs
│   │           ├── PayrollEngineService.cs
│   │           ├── HolidayManagementService.cs
│   │           ├── HolidayPayrollService.cs
│   │           ├── ReportingExportsService.cs
│   │           ├── ElectronicCallingService.cs
│   │           ├── BackgroundServicesService.cs
│   │           ├── RosterBoardService.cs
│   │           ├── FraComplianceService.cs
│   │           ├── RailroadInfoService.cs
│   │           └── SafetyService.cs
│   ├── CrewService.Domain/
│   │   ├── Primitives/                  # Entity base (soft delete, domain event raising)
│   │   ├── ValueObjects/                # ControlNumber, AuditStamp, Name
│   │   ├── Interfaces/                  # IRepository, IOrchestrationUnitOfWork, IDomainEvent
│   │   ├── Exceptions/                  # DomainException, NotFoundException, ConflictException
│   │   ├── Outbox/                      # OutboxMessage, OutboxMessageStatus
│   │   ├── DomainEvents/                # Per-entity domain events
│   │   ├── Models/                      # Legacy entities (Employees, ContactTypes, Employment, Seniority, Parents, Railroads, UserAccess)
│   │   └── Modules/                     # Modular domain entities
│   │       ├── TenantConfig/            # GroupType, DynamicGroup, RailroadGroupPlacement, Attributes
│   │       ├── UserAccess/              # Invitation, Roles
│   │       ├── Employees/               # Employee module interfaces
│   │       ├── WorkManagement/          # AssignmentTemplate, WorkInstance, PositionRole, PositionSlot
│   │       ├── Crews/                   # Crew, CrewPosition, CrewIncumbency, etc.
│   │       ├── Boards/                  # ExtraBoard, BoardMember, BoardCascadePolicy
│   │       ├── Policies/                # CraftDisplacementPolicy, BulletinPolicy, SeniorityMovePolicy
│   │       ├── Bulletins/               # PositionVacancy, Bulletin, BulletinBid
│   │       ├── Dispatching/             # DispatchProjection, DispatchDecisionLog, EmployeeBooking
│   │       ├── AbsenceVacancy/          # AbsenceRequest
│   │       ├── Payroll/                 # TimeEntry, PayrollRun, PayrollRecord
│   │       ├── FraCompliance/           # FraDutyTour
│   │       ├── HolidayManagement/       # Holiday
│   │       ├── RailroadInfo/            # RailroadInformation, ReadReceipt
│   │       ├── Safety/                  # SafetyObservation, SafetyAction, SafetyCategory
│   │       ├── Notifications/           # NotificationRequest
│   │       └── Infrastructure/          # WorkerSchedule, ExecutionLog
│   ├── CrewService.Application/         # Use cases, application services
│   ├── CrewService.Infrastructure/      # Exception interceptors, Identity User, Outbox
│   ├── CrewService.Persistance/         # DbContexts, migrations, repositories, encryption
│   │   ├── Data/                        # CrewServiceDbContext, UserAccessDbContext
│   │   ├── Configurations/              # EF Core entity configurations
│   │   ├── Encryption/                  # AesFieldEncryptor, EncryptedStringConverter
│   │   ├── Repositories/                # Repository implementations
│   │   ├── Services/                    # CurrentUserService
│   │   ├── UnitOfWork/                  # OrchestrationUnitOfWork + Factory
│   │   └── Modules/                     # Per-module configurations + repositories
│   └── CrewService.UnitTests/           # xUnit tests
├── Protos/                              # Shared proto definitions
│   ├── common.proto                     # Shared messages (DeleteResponse)
│   ├── google/api/                      # HTTP annotation imports
│   ├── *.proto                          # Core entity protos (auth, employee, craft, etc.)
│   └── modules/                         # Module protos (tenant_config, crews, safety, etc.)
├── docs/                                # Specifications and gap analysis
└── CrewService.FrontEnd/                # Blazor Server front end
```

## Data Strategy

- **Single shared database** for all Parent tenants per environment — logical tenant isolation via Parent scoping on all operational entities
- **Two DbContexts** sharing the same database and connection:
  - `UserAccessDbContext` — ASP.NET Core Identity tables + invitations
  - `CrewServiceDbContext` — all operational domain tables
- Separate EF migration history tables per context
- Operations tables partitioned by module ownership
- Effective-dated records use `[start_utc, end_utc)` semantics (end nullable = open-ended)

## Orchestration Unit of Work

Short-lived orchestration UoW creating a single shared `DbConnection` + `DbTransaction` across both DbContexts:

| Component | Location | Description |
|---|---|---|
| `IOrchestrationUnitOfWork` | Domain/Interfaces | Interface: repositories + commit/rollback |
| `IOrchestrationUnitOfWorkFactory` | Domain/Interfaces | Factory for creating UoW instances |
| `OrchestrationUnitOfWork` | Persistance/UnitOfWork | Concrete implementation |
| `OrchestrationUnitOfWorkFactory` | Persistance/UnitOfWork | Creates dedicated connection/transaction per UoW |

**Safety rules:** Single explicit `DbTransaction` on one opened connection (no MSDTC). `SaveChanges()` to obtain IDs before creating dependent entities within the same transaction. Never pass EF entity instances across DbContexts — pass IDs only.

## Domain Events & Outbox

Domain events are collected from aggregates during `CommitAsync()`, persisted as `OutboxMessage` rows in the same transaction, and published asynchronously via `OutboxPublisherService` (Channel-based with polling fallback).

| Field | Type | Description |
|---|---|---|
| `EventId` | `Guid` | Unique event identifier |
| `EventType` | `string` | Concrete event type name |
| `AggregateType` | `string` | Aggregate root type |
| `AggregateId` | `long` | Aggregate identifier |
| `OccurredAt` | `DateTime` | UTC timestamp |
| `CorrelationId` | `string?` | Request correlation |
| `OrchestrationId` | `string?` | Groups related events |
| `IdempotencyKey` | `string?` | Deduplication key |
| `EventVersion` | `int` | Schema version (default: 1) |
| `PayloadJson` | `string?` | Minimal JSON payload (camelCase, no PII) |

## API Endpoint Reference

All endpoints are defined as gRPC services with REST/JSON transcoding via `google.api.http` annotations. Both native gRPC and HTTP/JSON are available on the same port. Swagger UI is available at `/swagger` in development.

> **Authentication:** All endpoints except `AuthSrvc` require a valid JWT Bearer token in the `Authorization` header. Tokens are obtained via `/login` and refreshed via `/refresh_token`.

---

### Authentication & Account

**Service:** `AuthSrvc` (proto: `auth.proto`) — *No authorization required*

Handles user authentication via JWT Bearer tokens. Users must first be invited (via `InvitationSrvc`) before they can register. On login, the API returns a JWT containing the user's identity and role claims (built from `UserParentAssignment` rows and the global `PrimaryRoleId`). The JWT is short-lived; use the refresh token to obtain a new pair without re-authenticating. All other API endpoints require this JWT in the `Authorization: Bearer <token>` header.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `AuthenticateUser` | `POST` | `/login` | Authenticate with username/password, returns JWT + refresh token |
| `RegisterUser` | `POST` | `/register` | Register a new user via invitation token |
| `RefreshJwtToken` | `POST` | `/refresh_token` | Exchange expired JWT + refresh token for new tokens |

**Service:** `AccountSrvc` (proto: `account.proto`)

Manages the authenticated user's own profile (first/middle/last name) and UI theme preferences (theme name + light/dark mode). These endpoints operate on the currently logged-in user — no `ctrl_nbr` is needed; the identity is extracted from the JWT.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetProfile` | `GET` | `/account/manage/profile` | Get current user's profile (name fields) |
| `ModifyProfile` | `POST` | `/account/manage` | Update current user's name fields |
| `ModifyTheme` | `POST` | `/account/manage/theme` | Update current user's UI theme preference |

---

### Tenant Configuration

**Service:** `TenantConfigSrvc` (proto: `modules/tenant_config.proto`)

Manages the dynamic group hierarchy that defines each tenant's organizational structure. The hierarchy is fully configurable — there are no hard-coded levels. A typical setup has three tiers: **Region → Subdivision → Work Area**, but tenants can define any structure via Group Types. Groups flagged as **work areas** are the operational level where crafts, rosters, boards, crews, and policies are defined. Railroads are placed at any group level via Railroad Placements, allowing a single railroad to appear at a region level (broad) or a work area level (narrow). Custom Attribute Definitions let tenants attach arbitrary metadata to groups of a given type.

**Group Types**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllGroupTypes` | `GET` | `/v1/tenant-config/group-types` | List all group types |
| `GetGroupType` | `GET` | `/v1/tenant-config/group-types/{ctrl_nbr}` | Get a single group type |
| `CreateGroupType` | `POST` | `/v1/tenant-config/group-types` | Create a group type (e.g. Region, Subdivision, WorkArea) |
| `UpdateGroupType` | `PUT` | `/v1/tenant-config/group-types/{ctrl_nbr}` | Update a group type |
| `DeleteGroupType` | `DELETE` | `/v1/tenant-config/group-types/{ctrl_nbr}` | Soft-delete a group type |

**Dynamic Groups**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllGroups` | `GET` | `/v1/tenant-config/groups` | List all dynamic groups |
| `GetGroup` | `GET` | `/v1/tenant-config/groups/{ctrl_nbr}` | Get a single group |
| `CreateGroup` | `POST` | `/v1/tenant-config/groups` | Create a group under a parent group |
| `UpdateGroup` | `PUT` | `/v1/tenant-config/groups/{ctrl_nbr}` | Update a group |
| `DeleteGroup` | `DELETE` | `/v1/tenant-config/groups/{ctrl_nbr}` | Soft-delete a group |
| `GetGroupTree` | `GET` | `/v1/tenant-config/groups/tree` | Get full hierarchical group tree |
| `GetWorkAreas` | `GET` | `/v1/tenant-config/work-areas` | List all groups flagged as work areas |
| `GetAncestors` | `GET` | `/v1/tenant-config/groups/{ctrl_nbr}/ancestors` | Get ancestor chain for a group |

**Railroad Placements**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `PlaceRailroadInGroup` | `POST` | `/v1/tenant-config/railroad-placements` | Place a railroad at a specific group level |
| `RemoveRailroadFromGroup` | `DELETE` | `/v1/tenant-config/railroad-placements/{ctrl_nbr}` | Remove a railroad placement |
| `GetRailroadPlacements` | `GET` | `/v1/tenant-config/railroad-placements/by-railroad/{railroad_ctrl_nbr}` | Get all placements for a railroad |
| `GetRailroadsInGroup` | `GET` | `/v1/tenant-config/railroad-placements/by-group/{group_ctrl_nbr}` | Get all railroads placed in a group |

**Custom Attributes**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAttributeDefinitions` | `GET` | `/v1/tenant-config/attribute-definitions/by-group-type/{group_type_ctrl_nbr}` | List attribute definitions for a group type |
| `GetAttributeDefinition` | `GET` | `/v1/tenant-config/attribute-definitions/{ctrl_nbr}` | Get a single attribute definition |
| `CreateAttributeDefinition` | `POST` | `/v1/tenant-config/attribute-definitions` | Define a custom attribute on a group type |
| `UpdateAttributeDefinition` | `PUT` | `/v1/tenant-config/attribute-definitions/{ctrl_nbr}` | Update an attribute definition |
| `DeleteAttributeDefinition` | `DELETE` | `/v1/tenant-config/attribute-definitions/{ctrl_nbr}` | Delete an attribute definition |
| `GetAttributeValues` | `GET` | `/v1/tenant-config/attribute-values/by-group/{group_ctrl_nbr}` | Get attribute values for a group |
| `SetAttributeValue` | `POST` | `/v1/tenant-config/attribute-values` | Set an attribute value on a group |
| `DeleteAttributeValue` | `DELETE` | `/v1/tenant-config/attribute-values/{ctrl_nbr}` | Delete an attribute value |

---

### Organization

**Service:** `ParentSrvc` (proto: `parent.proto`)

A Parent represents a top-level corporate tenant (e.g. "CSX Corporation", "Port Terminal Railroad Association"). Multiple Parents coexist in the same physical database, with all operational data (employees, crafts, rosters, crews, payroll, etc.) logically scoped to a Parent. The Parent is the root of the tenancy model — users are assigned to Parents via UserParentAssignment, and all downstream entities trace back to a Parent through the group hierarchy.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllParentsAsync` | `GET` | `/parent` | List all parent tenants |
| `GetParentAsync` | `GET` | `/parent/{ctrlNbr}` | Get a parent by control number (includes railroads) |
| `CreateParentAsync` | `POST` | `/parent` | Create a new parent tenant |
| `UpdateParentAsync` | `PUT` | `/parent` | Update a parent tenant |
| `DeleteParentAsync` | `DELETE` | `/parent/{ctrlNbr}` | Delete a parent tenant |

**Service:** `RailroadSrvc` (proto: `railroad.proto`)

A Railroad belongs to a Parent and represents a rail carrier identified by a unique railroad mark (e.g. "CSXT", "PTRA"). Railroads are placed into the dynamic group hierarchy via Railroad Placements (see Tenant Config). Rosters are scoped to a craft + railroad combination.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllRailroadsAsync` | `GET` | `/railroad` | List all railroads |
| `GetRailroadAsync` | `GET` | `/railroad/{ctrlNbr}` | Get a single railroad |
| `GetAllParentRailroadsAsync` | `GET` | `/parentrailroads/{parent_ctrlNbr}` | List railroads for a parent |
| `CreateRailroadAsync` | `POST` | `/railroad` | Create a railroad under a parent |
| `UpdateRailroadAsync` | `PUT` | `/railroad` | Update a railroad |
| `DeleteRailroadAsync` | `DELETE` | `/railroad/{ctrlNbr}` | Delete a railroad |

**Service:** `InvitationSrvc` (proto: `invitation.proto`)

The platform uses an **invite-only** access model. An admin creates an invitation by specifying an email address, target Parent, and role. The system generates a unique token and (in production) sends an email. The recipient uses the token to register via `POST /register`. Invitations can be revoked before acceptance or resent if the email was missed. This flow ensures no user can self-register without being explicitly invited by an authorized admin.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `CreateInvitation` | `POST` | `/invitation` | Create an invitation (email + parent + role) |
| `GetInvitation` | `GET` | `/invitation/{ctrl_nbr}` | Get a single invitation |
| `GetInvitationsByParent` | `GET` | `/invitation/parent/{parent_ctrl_nbr}` | List invitations for a parent |
| `GetInvitationsByEmail` | `GET` | `/invitation/email/{email}` | List invitations for an email |
| `RevokeInvitation` | `DELETE` | `/invitation/{ctrl_nbr}` | Revoke a pending invitation |
| `ResendInvitation` | `POST` | `/invitation/{ctrl_nbr}/resend` | Resend an invitation |

**Service:** `UserParentAssignmentSrvc` (proto: `user_parent_assignment.proto`)

Links a registered user to a Parent with a specific role (e.g. ParentAdmin, Dispatcher, PayrollClerk). A user can have assignments to multiple Parents, each with a different role. These assignments are used to build JWT claims at login time. Users without any assignment (and without the global `SystemAdmin` role) are blocked from accessing the platform.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAssignmentAsync` | `GET` | `/user-parent-assignment/{ctrlNbr}` | Get a single assignment |
| `GetAssignmentsByUserAsync` | `GET` | `/user-parent-assignment/user/{userId}` | List assignments for a user |
| `GetAssignmentsByParentAsync` | `GET` | `/user-parent-assignment/parent/{parentCtrlNbr}` | List assignments for a parent |
| `CreateAssignmentAsync` | `POST` | `/user-parent-assignment` | Create a user-parent assignment with role |
| `UpdateAssignmentRoleAsync` | `PUT` | `/user-parent-assignment` | Update the role on an existing assignment |
| `DeleteAssignmentAsync` | `DELETE` | `/user-parent-assignment/{ctrlNbr}` | Delete an assignment |

---

### Employee Management

**Service:** `EmployeeSrvc` (proto: `employee.proto`)

Full employee lifecycle with nested contact information. An employee record contains demographic data (gender, race, marital status, birth date), employment data (employment date, status, employee number), operational flags (allow FMLA mark-off, call for overtime, process payroll, tie up off property), and identification (SSN — encrypted at rest, driver's license). Each employee has zero or more addresses (typed by AddressType), phone numbers (typed by PhoneNumberType, with calling order and dial-one flags for electronic calling), and email addresses (typed by EmailAddressType). Employees are placed on rosters via seniority entries, assigned to crew positions via incumbencies, and referenced by nearly every operational module.

**Employee CRUD**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllEmployeesAsync` | `GET` | `/v1/employees` | List employees (paginated: `page_number`, `page_size`) |
| `GetEmployeeAsync` | `GET` | `/v1/employees/{ctrl_nbr}` | Get employee by control number (includes addresses, phones, emails) |
| `GetEmployeeByNumberAsync` | `GET` | `/v1/employees/by-number/{employee_number}` | Get employee by employee number |
| `CreateEmployeeAsync` | `POST` | `/v1/employees` | Create an employee (SSN encrypted at rest) |
| `UpdateEmployeeAsync` | `PUT` | `/v1/employees/{ctrl_nbr}` | Update employee fields |
| `DeleteEmployeeAsync` | `DELETE` | `/v1/employees/{ctrl_nbr}` | Soft-delete an employee |

**Addresses**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `AddAddressAsync` | `POST` | `/v1/employees/{employee_ctrl_nbr}/addresses` | Add an address to an employee |
| `UpdateAddressAsync` | `PUT` | `/v1/employees/{employee_ctrl_nbr}/addresses/{ctrl_nbr}` | Update an address |
| `DeleteAddressAsync` | `DELETE` | `/v1/employees/{employee_ctrl_nbr}/addresses/{ctrl_nbr}` | Remove an address |

**Phone Numbers**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `AddPhoneNumberAsync` | `POST` | `/v1/employees/{employee_ctrl_nbr}/phones` | Add a phone number |
| `UpdatePhoneNumberAsync` | `PUT` | `/v1/employees/{employee_ctrl_nbr}/phones/{ctrl_nbr}` | Update a phone number |
| `DeletePhoneNumberAsync` | `DELETE` | `/v1/employees/{employee_ctrl_nbr}/phones/{ctrl_nbr}` | Remove a phone number |

**Email Addresses**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `AddEmailAddressAsync` | `POST` | `/v1/employees/{employee_ctrl_nbr}/emails` | Add an email address |
| `UpdateEmailAddressAsync` | `PUT` | `/v1/employees/{employee_ctrl_nbr}/emails/{ctrl_nbr}` | Update an email address |
| `DeleteEmailAddressAsync` | `DELETE` | `/v1/employees/{employee_ctrl_nbr}/emails/{ctrl_nbr}` | Remove an email address |

**Service:** `PriorServiceCreditSrvc` (proto: `prior_service_credit.proto`)

Manages prior service credit for employees who have previous railroad employment. Prior service credit affects seniority date calculations, vacation accrual, and payroll tier eligibility. Each employee has at most one prior service credit record.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAsync` | `GET` | `/v1/employees/{employee_ctrl_nbr}/prior-service-credit` | Get prior service credit for an employee |
| `CreateAsync` | `POST` | `/v1/employees/{employee_ctrl_nbr}/prior-service-credit` | Create prior service credit |
| `UpdateAsync` | `PUT` | `/v1/employees/{employee_ctrl_nbr}/prior-service-credit` | Update prior service credit |
| `DeleteAsync` | `DELETE` | `/v1/employees/{employee_ctrl_nbr}/prior-service-credit` | Delete prior service credit |

**Service:** `EmploymentStatusSrvc` (proto: `employment_status.proto`)

Reference data defining the possible employment statuses (e.g. Active, Furloughed, Terminated, On Leave, Retired). Each employee references a current employment status, and changes are tracked in the employment status history.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllAsync` | `GET` | `/v1/employment-statuses` | List all employment statuses |
| `GetAsync` | `GET` | `/v1/employment-statuses/{ctrl_nbr}` | Get a single status |
| `CreateAsync` | `POST` | `/v1/employment-statuses` | Create an employment status |
| `UpdateAsync` | `PUT` | `/v1/employment-statuses/{ctrl_nbr}` | Update a status |
| `DeleteAsync` | `DELETE` | `/v1/employment-statuses/{ctrl_nbr}` | Delete a status |

**Service:** `EmploymentStatusHistorySrvc` (proto: `employment_status_history.proto`)

Tracks the complete history of employment status changes for each employee, including the effective date and the status transitioned to. Provides an audit trail of status transitions (e.g. Active → Furloughed → Active).

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllByEmployeeAsync` | `GET` | `/v1/employees/{employee_ctrl_nbr}/status-history` | List status history for an employee |
| `GetAsync` | `GET` | `/v1/employment-status-history/{ctrl_nbr}` | Get a single history entry |
| `CreateAsync` | `POST` | `/v1/employees/{employee_ctrl_nbr}/status-history` | Create a status history entry |
| `DeleteAsync` | `DELETE` | `/v1/employment-status-history/{ctrl_nbr}` | Delete a history entry |

**Contact Type Reference Data:**

| Service | Proto | Routes (CRUD) |
|---|---|---|
| `AddressTypeSrvc` | `address_type.proto` | `/v1/address-types`, `/v1/address-types/{ctrl_nbr}` |
| `PhoneNumberTypeSrvc` | `phone_number_type.proto` | `/v1/phone-number-types`, `/v1/phone-number-types/{ctrl_nbr}` |
| `EmailAddressTypeSrvc` | `email_address_type.proto` | `/v1/email-address-types`, `/v1/email-address-types/{ctrl_nbr}` |

Each contact type service supports: `GetAll`, `Get`, `Create`, `Update`, `Delete`.

---

### Crafts, Seniority & Rosters

This section covers the three-level hierarchy that underpins all operational behavior: **Craft → Roster → Seniority**.

#### Crafts

**Service:** `CraftSrvc` (proto: `craft.proto`)

Crafts are the central organizing unit of the platform. Each work area defines its crafts (e.g. Engineer, Conductor, Clerical), and each craft carries agreement-driven configuration: mark-off/mark-up hours, required rest hours, unpaid meal periods, HOS applicability, vacation rules, and payroll processing flags. Policies, boards, bulletins, displacement, and dispatching all key off the craft.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllAsync` | `GET` | `/v1/crafts` | List crafts (filter by `dynamic_group_ctrl_nbr`, paginated) |
| `GetAsync` | `GET` | `/v1/crafts/{ctrl_nbr}` | Get a single craft with all configuration |
| `CreateAsync` | `POST` | `/v1/crafts` | Create a craft at a work area |
| `UpdateAsync` | `PUT` | `/v1/crafts/{ctrl_nbr}` | Update craft configuration (mark-off hours, rest hours, HOS, meal periods, etc.) |
| `DeleteAsync` | `DELETE` | `/v1/crafts/{ctrl_nbr}` | Soft-delete a craft |

#### Rosters

**Service:** `RosterSrvc` (proto: `roster.proto`)

A roster belongs to a specific craft and railroad combination. Each roster represents a named seniority list (e.g. "Engineer Roster" for CSX at Jax Yard). Rosters carry flags that indicate their purpose: `Training` (student roster), `ExtraBoard` (extra board roster), and `OvertimeBoard` (overtime roster). Employees are placed on rosters via seniority entries.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllAsync` | `GET` | `/v1/rosters` | List all rosters |
| `GetAsync` | `GET` | `/v1/rosters/{ctrl_nbr}` | Get a single roster |
| `CreateAsync` | `POST` | `/v1/rosters` | Create a roster for a craft + railroad |
| `UpdateAsync` | `PUT` | `/v1/rosters/{ctrl_nbr}` | Update a roster |
| `DeleteAsync` | `DELETE` | `/v1/rosters/{ctrl_nbr}` | Soft-delete a roster |

#### Seniority

**Service:** `SenioritySrvc` (proto: `seniority.proto`)

A seniority entry places an employee on a specific roster with a rank, roster date, state, and training eligibility flag. The `LastActiveRoster` flag marks the roster the employee is currently active on. Seniority rank drives bulletin bid priority, displacement order, and extra board calling order.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllAsync` | `GET` | `/v1/seniority` | List seniority records |
| `GetAsync` | `GET` | `/v1/seniority/{ctrl_nbr}` | Get a single seniority record |
| `CreateAsync` | `POST` | `/v1/seniority` | Create a seniority entry (employee + roster + rank + date) |
| `UpdateAsync` | `PUT` | `/v1/seniority/{ctrl_nbr}` | Update a seniority record |
| `DeleteAsync` | `DELETE` | `/v1/seniority/{ctrl_nbr}` | Soft-delete a seniority record |

#### Seniority States

**Service:** `SeniorityStateSrvc` (proto: `seniority_state.proto`)

Seniority states are reference data that classify an employee's current standing on a roster (e.g. active, furloughed, on leave). Each seniority entry references a state.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllAsync` | `GET` | `/v1/seniority-states` | List all seniority states |
| `GetAsync` | `GET` | `/v1/seniority-states/{ctrl_nbr}` | Get a single state |
| `CreateAsync` | `POST` | `/v1/seniority-states` | Create a seniority state |
| `UpdateAsync` | `PUT` | `/v1/seniority-states/{ctrl_nbr}` | Update a state |
| `DeleteAsync` | `DELETE` | `/v1/seniority-states/{ctrl_nbr}` | Soft-delete a state |

#### Payroll Tiers

**Service:** `PayrollTierSrvc` (proto: `payroll_tier.proto`)

Payroll tiers define pay rate brackets scoped to a work area (e.g. tier 1 = 7 years at rate 100, tier 2 = 14 years at rate 150).

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllAsync` | `GET` | `/v1/payroll-tiers` | List payroll tiers |
| `GetAsync` | `GET` | `/v1/payroll-tiers/{ctrl_nbr}` | Get a single tier |
| `CreateAsync` | `POST` | `/v1/payroll-tiers` | Create a payroll tier |
| `UpdateAsync` | `PUT` | `/v1/payroll-tiers/{ctrl_nbr}` | Update a tier |
| `DeleteAsync` | `DELETE` | `/v1/payroll-tiers/{ctrl_nbr}` | Soft-delete a tier |

---

### Work Management

**Service:** `WorkManagementSrvc` (proto: `modules/work_management.proto`)

Manages the layered structure of railroad work. An **Assignment Template** is a reusable job definition scoped to a work area and craft (e.g. "Q101 Jacksonville-Waycross" — a through-freight assignment running daily). A **Work Instance** is a specific occurrence of a template on a given date with start/end/call times. Each work instance has **Position Slots** that define the staffing need (e.g. 1 Engineer, 1 Conductor). Slots are filled by binding employees. **Position Roles** define the types of positions a craft supports (e.g. Engineer, Conductor, Brakeman). Crews are attached to templates and inherit their position structure.

**Assignment Templates**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllTemplates` | `GET` | `/v1/work-management/templates` | List assignment templates |
| `GetTemplate` | `GET` | `/v1/work-management/templates/{ctrl_nbr}` | Get a single template |
| `CreateTemplate` | `POST` | `/v1/work-management/templates` | Create a template at a work area |
| `UpdateTemplate` | `PUT` | `/v1/work-management/templates/{ctrl_nbr}` | Update a template |
| `DeleteTemplate` | `DELETE` | `/v1/work-management/templates/{ctrl_nbr}` | Soft-delete a template |

**Work Instances**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetWorkInstances` | `GET` | `/v1/work-management/instances` | List work instances |
| `CreateWorkInstance` | `POST` | `/v1/work-management/instances` | Create a work instance (start/end/call times) |

**Position Slots**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetPositionSlots` | `GET` | `/v1/work-management/instances/{work_instance_ctrl_nbr}/slots` | List slots for a work instance |
| `CreatePositionSlot` | `POST` | `/v1/work-management/slots` | Create a position slot on a work instance |
| `BindSlot` | `POST` | `/v1/work-management/slots/{ctrl_nbr}/bind` | Bind an employee to a slot |
| `UnbindSlot` | `POST` | `/v1/work-management/slots/{ctrl_nbr}/unbind` | Unbind an employee from a slot |

**Position Roles**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetPositionRoles` | `GET` | `/v1/work-management/roles/{craft_ctrl_nbr}` | List position roles for a craft |
| `CreatePositionRole` | `POST` | `/v1/work-management/roles` | Create a position role (e.g. Engineer, Conductor) |

---

### Crews

**Service:** `CrewsSrvc` (proto: `modules/crews.proto`)

Manages regular and relief crews and their staffing structure. A **Crew** is scoped to a work area and craft, typed as REGULAR (assigned to a specific set of templates) or EXTRA (a pool). Each crew has ordered **Crew Positions** (linked to a position role, e.g. Engineer slot #1). An **Incumbency** assigns an employee to a crew position for an effective date range. **Crew Attachment Templates** link a crew to the assignment templates it works. **Relief Coverage Rules** define which templates a relief crew covers and on which days (day-of-week bitmask), so the system knows which relief crew fills in when a regular crew's rest day falls on a template's operating day.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllCrews` | `GET` | `/v1/crews` | List all crews |
| `GetCrew` | `GET` | `/v1/crews/{ctrl_nbr}` | Get a single crew |
| `CreateCrew` | `POST` | `/v1/crews` | Create a crew (REGULAR or EXTRA) at a work area |
| `UpdateCrew` | `PUT` | `/v1/crews/{ctrl_nbr}` | Update a crew |
| `DeleteCrew` | `DELETE` | `/v1/crews/{ctrl_nbr}` | Soft-delete a crew |
| `GetCrewPositions` | `GET` | `/v1/crews/{crew_ctrl_nbr}/positions` | List positions in a crew |
| `CreateCrewPosition` | `POST` | `/v1/crews/positions` | Add a position to a crew (role + order) |
| `GetCrewIncumbencies` | `GET` | `/v1/crews/positions/{crew_position_ctrl_nbr}/incumbencies` | List incumbencies for a position |
| `CreateCrewIncumbency` | `POST` | `/v1/crews/incumbencies` | Assign an employee to a crew position |
| `GetCrewAttachmentTemplates` | `GET` | `/v1/crews/{crew_ctrl_nbr}/attachment-templates` | List template attachments for a crew |
| `CreateCrewAttachmentTemplate` | `POST` | `/v1/crews/attachment-templates` | Attach a crew to an assignment template |
| `GetReliefCoverageRules` | `GET` | `/v1/crews/{relief_crew_ctrl_nbr}/relief-rules` | List relief rules for a crew |
| `CreateReliefCoverageRule` | `POST` | `/v1/crews/relief-rules` | Define a relief coverage rule (crew + template + day mask) |

---

### Boards

**Service:** `BoardsSrvc` (proto: `modules/boards.proto`)

Manages extra boards, board membership, and cascade policies. Each extra board is scoped to a craft + work area (e.g. "Jax Engineer Extra Board") and typed as PRIMARY or AUXILIARY. Board members are employees ordered by seniority rank. Cascade policies define how unfilled positions escalate up the group hierarchy by craft — specifying the search strategy (e.g. UP_HIERARCHY), maximum levels to search, and ordering method (e.g. SENIORITY).

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetAllBoards` | `GET` | `/v1/boards` | List all extra boards |
| `GetBoard` | `GET` | `/v1/boards/{ctrl_nbr}` | Get a single board |
| `CreateBoard` | `POST` | `/v1/boards` | Create an extra board (PRIMARY or AUXILIARY) |
| `UpdateBoard` | `PUT` | `/v1/boards/{ctrl_nbr}` | Update a board |
| `DeleteBoard` | `DELETE` | `/v1/boards/{ctrl_nbr}` | Soft-delete a board |
| `GetBoardMembers` | `GET` | `/v1/boards/{extra_board_ctrl_nbr}/members` | List members of a board |
| `CreateBoardMember` | `POST` | `/v1/boards/members` | Add an employee to a board (with position) |
| `GetCascadePolicy` | `GET` | `/v1/boards/cascade-policy/{work_area_group_ctrl_nbr}/{craft_ctrl_nbr}` | Get cascade policy for a work area + craft |
| `UpsertCascadePolicy` | `PUT` | `/v1/boards/cascade-policy` | Create or update a cascade policy |

---

### Policies

**Service:** `PoliciesSrvc` (proto: `modules/policies.proto`)

Each policy is scoped to a specific craft (identified by `craft_ctrl_nbr`). Displacement policies govern how employees are displaced from positions based on seniority rank — specifying the window (hours), ordering strategy (e.g. roster date), and fallback target (e.g. extra board). Bulletin policies control how long a vacancy is posted for bidding. Seniority move policies define when and how employees can exercise seniority to move between rosters within a craft.

**Displacement Policy**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetDisplacementPolicy` | `GET` | `/v1/policies/displacement/{craft_ctrl_nbr}` | Get displacement policy for a craft |
| `UpsertDisplacementPolicy` | `PUT` | `/v1/policies/displacement/{craft_ctrl_nbr}` | Create or update displacement policy (window hours, order, fallback) |

**Bulletin Policy**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetBulletinPolicy` | `GET` | `/v1/policies/bulletin/{craft_ctrl_nbr}` | Get bulletin policy for a craft |
| `UpsertBulletinPolicy` | `PUT` | `/v1/policies/bulletin/{craft_ctrl_nbr}` | Create or update bulletin policy (posting duration) |

**Seniority Move Policy**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetSeniorityMovePolicy` | `GET` | `/v1/policies/seniority-move/{craft_ctrl_nbr}` | Get seniority move policy for a craft |
| `UpsertSeniorityMovePolicy` | `PUT` | `/v1/policies/seniority-move/{craft_ctrl_nbr}` | Create or update seniority move policy |
| `ExerciseSeniorityMove` | `POST` | `/v1/policies/seniority-move/exercise` | Exercise a seniority move for an employee |
| `GetSeniorityMovesByEmployee` | `GET` | `/v1/policies/seniority-moves/employee/{employee_ctrl_nbr}` | List seniority moves for an employee |

---

### Bulletins & Vacancy

**Service:** `BulletinsSrvc` (proto: `modules/bulletins.proto`)

Manages position vacancies, bulletin postings, and employee bids. Vacancies are scoped to a craft and can target crew positions, board positions, or position slots. When a vacancy is bulletined, a bid window opens (duration governed by the craft's BulletinPolicy). Employees bid on bulletins with a priority preference; bids are ranked by seniority. The highest-seniority bidder is awarded the position.

**Vacancies**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetOpenVacancies` | `GET` | `/v1/bulletins/vacancies/open` | List all open vacancies |
| `GetVacanciesByCraft` | `GET` | `/v1/bulletins/vacancies/craft/{craft_ctrl_nbr}` | List vacancies for a craft |
| `GetVacancy` | `GET` | `/v1/bulletins/vacancies/{ctrl_nbr}` | Get a single vacancy |
| `AbolishVacancy` | `PUT` | `/v1/bulletins/vacancies/{ctrl_nbr}/abolish` | Abolish (cancel) a vacancy |

**Bulletins**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetPostedBulletins` | `GET` | `/v1/bulletins/posted` | List all posted bulletins |
| `GetPostedBulletinsByCraft` | `GET` | `/v1/bulletins/posted/craft/{craft_ctrl_nbr}` | List posted bulletins for a craft |
| `GetBulletin` | `GET` | `/v1/bulletins/{ctrl_nbr}` | Get a single bulletin |

**Bids**

| Method | HTTP | Route | Description |
|---|---|---|---|
| `SubmitBid` | `POST` | `/v1/bulletins/bids` | Submit a bid on a bulletin |
| `WithdrawBid` | `PUT` | `/v1/bulletins/bids/{ctrl_nbr}/withdraw` | Withdraw a bid |
| `GetBidsByBulletin` | `GET` | `/v1/bulletins/{bulletin_ctrl_nbr}/bids` | List bids for a bulletin |
| `GetBidsByEmployee` | `GET` | `/v1/bulletins/bids/employee/{employee_ctrl_nbr}` | List bids by an employee |

---

### Dispatching

**Service:** `DispatchingSrvc` (proto: `modules/dispatching.proto`)

Handles the real-time staffing of position slots. **Projections** show upcoming unfilled slots across work areas, giving dispatchers a forward-looking view of staffing needs. **ExecuteCall** dispatches an employee to a slot (typically the next employee on the extra board per craft seniority and cascade policy). Every call attempt is recorded in a **Decision Log** with the reasoning (e.g. employee called, accepted, declined, skipped for rest). **Overrides** allow a dispatcher to request an out-of-order assignment (e.g. calling a lower-seniority employee); overrides require approval. **Employee Bookings** reserve an employee for a future slot before the actual call is made.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetProjections` | `GET` | `/v1/dispatching/projections` | Get current staffing projections for open slots |
| `GetDecisionLogs` | `GET` | `/v1/dispatching/decision-logs/{position_slot_ctrl_nbr}` | Get dispatch decision logs for a slot |
| `ExecuteCall` | `POST` | `/v1/dispatching/call` | Execute a crew call (dispatch an employee to a slot) |
| `RequestOverride` | `POST` | `/v1/dispatching/overrides` | Request a dispatch override |
| `ApproveOverride` | `POST` | `/v1/dispatching/overrides/{ctrl_nbr}/approve` | Approve a pending override |
| `GetEmployeeBookings` | `GET` | `/v1/dispatching/bookings/{employee_ctrl_nbr}` | List bookings for an employee |
| `CreateEmployeeBooking` | `POST` | `/v1/dispatching/bookings` | Create an employee booking |

---

### Daily Operations

**Service:** `DailyOperationsSrvc` (proto: `modules/daily_operations.proto`)

The day-to-day operational lifecycle of crew assignments. The **Call Sheet** is the central daily view for a work area — showing all assignments, their position slots, which employees are on duty, who is called and en route, and which slots remain open. **PlaceOnDuty** records an employee reporting for work on a position slot (start of duty). **TieUp** records the employee going off duty at the end of their tour (captures off-duty time, location, total hours worked). **AnnulPosition** cancels a position slot for the day (e.g. train annulled, assignment not needed). The on-duty/tie-up lifecycle feeds FRA Hours of Service tracking and payroll time entry generation.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetCallSheet` | `GET` | `/v1/daily-operations/call-sheet/{work_area_group_ctrl_nbr}` | Get the daily call sheet for a work area |
| `PlaceOnDuty` | `POST` | `/v1/daily-operations/on-duty` | Place an employee on duty |
| `TieUp` | `POST` | `/v1/daily-operations/tie-up` | Tie up (end duty) for an employee |
| `AnnulPosition` | `POST` | `/v1/daily-operations/annul` | Annul a daily position slot |

---

### Mark-Off & Absence

**Service:** `MarkOffSrvc` (proto: `modules/mark_off.proto`)

Employee mark-off (absence) requests, approval workflow, and compensation balance tracking. When an employee needs time off, they submit an absence request specifying the absence code (e.g. personal leave, vacation, sick), start/end times, and optionally the position slot they're vacating. The request goes through an approval workflow — a craft manager or crew manager approves or declines. Approved absences update the employee's **Compensation Balance** (deducting from available leave days/hours). **Absence Codes** are reference data scoped to a work area that define the available reasons for absence and whether they require approval. The craft's `MarkOffHours` and `ApproveAllMarkOffs` configuration governs the mark-off window and auto-approval behavior.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `CreateAbsenceRequest` | `POST` | `/v1/mark-off/requests` | Submit a mark-off/absence request |
| `ApproveAbsence` | `POST` | `/v1/mark-off/approve` | Approve a pending absence |
| `DeclineAbsence` | `POST` | `/v1/mark-off/decline` | Decline a pending absence |
| `GetCompensationBalance` | `GET` | `/v1/mark-off/compensation-balance/{employee_ctrl_nbr}` | Get compensation (leave) balance for an employee |
| `GetAbsenceCodes` | `GET` | `/v1/mark-off/absence-codes/{work_area_group_ctrl_nbr}` | List absence codes for a work area |

**Service:** `AbsenceVacancySrvc` (proto: `modules/absence_vacancy.proto`)

Absence request workflow with vacancy impact tracking. When an absence is approved for an employee who holds a position on a crew or board, the system identifies which position slots are impacted and flags them as vacant. This feeds into the Vacancy Assignment engine, which automatically evaluates vacant slots and assigns replacements per craft-level displacement and cascade policies.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `SubmitAbsenceRequest` | `POST` | `/v1/absence-vacancy/requests` | Submit an absence request |
| `GetPendingRequests` | `GET` | `/v1/absence-vacancy/requests/pending` | List pending absence requests |
| `GetEmployeeRequests` | `GET` | `/v1/absence-vacancy/requests/employee/{employee_ctrl_nbr}` | List requests by an employee |
| `ApproveRequest` | `POST` | `/v1/absence-vacancy/requests/{ctrl_nbr}/approve` | Approve an absence request |
| `DenyRequest` | `POST` | `/v1/absence-vacancy/requests/{ctrl_nbr}/deny` | Deny an absence request |
| `CancelRequest` | `POST` | `/v1/absence-vacancy/requests/{ctrl_nbr}/cancel` | Cancel an absence request |

---

### Vacancy Assignment

**Service:** `VacancyAssignmentSrvc` (proto: `modules/vacancy_assignment.proto`)

Automated resolution engine for filling vacant position slots. When triggered for a work area, the engine evaluates all open vacancies by craft, applies the craft's displacement policy (seniority-based order, window hours) and board cascade policy (escalation strategy, hierarchy search), and assigns the highest-priority available employee to each slot. Each resolution run is logged with the decisions made, employees evaluated, and outcomes. Runs can be triggered manually or by background services on a schedule.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `TriggerResolution` | `POST` | `/v1/vacancy-assignment/trigger` | Trigger vacancy resolution for a work area |
| `GetResolutionRuns` | `GET` | `/v1/vacancy-assignment/runs/{work_area_group_ctrl_nbr}` | List resolution run history |

---

### Payroll

**Service:** `PayrollSrvc` (proto: `modules/payroll.proto`)

Manages the payroll processing lifecycle. **Time Entries** are created from tie-up records (or manually) and represent hours worked by an employee on a specific date, with a type (regular, overtime, etc.) and hour count. **Payroll Runs** are created for a pay period, aggregate time entries into **Payroll Records** (one per employee per run), and progress through a lifecycle: OPEN (editable) → LOCKED (no further edits, ready for processing) → APPROVED. The PayrollEngine module handles the calculation logic; this module handles the data model and lifecycle.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetTimeEntries` | `GET` | `/v1/payroll/time-entries` | List time entries |
| `CreateTimeEntry` | `POST` | `/v1/payroll/time-entries` | Create a time entry (employee, date, type, hours) |
| `GetPayrollRun` | `GET` | `/v1/payroll/runs/{pay_period}` | Get a payroll run by pay period |
| `CreatePayrollRun` | `POST` | `/v1/payroll/runs` | Create a new payroll run |
| `LockPayrollRun` | `POST` | `/v1/payroll/runs/{ctrl_nbr}/lock` | Lock a payroll run (prevents further edits) |
| `GetPayrollRecords` | `GET` | `/v1/payroll/runs/{payroll_run_ctrl_nbr}/records` | List payroll records for a run |

---

### Payroll Engine

**Service:** `PayrollEngineSrvc` (proto: `modules/payroll_engine.proto`)

The calculation engine that sits on top of the Payroll data model. **ResolveEarningCode** determines the earning code for a time entry based on the employee's craft configuration, payroll tier, and work rules (e.g. regular vs. overtime vs. penalty). **CalculateTrial** runs a trial payroll calculation across all time entries in a run without committing results — used for preview and validation. **LockFinal** commits the final payroll results and locks the run. **ApproveEarning** allows per-earning approval for exception handling.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `ResolveEarningCode` | `POST` | `/v1/payroll-engine/resolve` | Resolve earning code for a time entry |
| `CalculateTrial` | `POST` | `/v1/payroll-engine/trial` | Run a trial payroll calculation |
| `LockFinal` | `POST` | `/v1/payroll-engine/lock` | Lock final payroll results |
| `ApproveEarning` | `POST` | `/v1/payroll-engine/approve` | Approve an individual earning |

---

### Holiday Management

**Service:** `HolidayManagementSrvc` (proto: `modules/holiday_management.proto`)

Manages the US federal holiday catalog and generates observable holiday records per work area per year. The catalog contains holiday definitions (e.g. "New Year's Day", "Independence Day") with their rules for observed dates. **GenerateHolidaysForYear** creates concrete holiday records for a work area and year, computing the observed date for each holiday. These records are used by the Holiday Payroll module to evaluate employee qualification for holiday pay.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetUsHolidayCatalog` | `GET` | `/v1/holiday-management/catalog` | List the US holiday catalog |
| `GenerateHolidaysForYear` | `POST` | `/v1/holiday-management/generate` | Generate holiday records for a specific year |

---

### Holiday Payroll

**Service:** `HolidayPayrollSrvc` (proto: `modules/holiday_payroll.proto`)

Evaluates whether an employee qualifies for holiday pay based on railroad work rules. Qualification typically depends on whether the employee worked the day before and the day after the holiday, any absence codes on those days, and the employee's seniority state. **EvaluateQualification** takes a holiday, employee, and work-day context and returns qualified/disqualified with a reason. **GetHolidays** lists the generated holidays for a work area so the front end can display the holiday calendar.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `EvaluateQualification` | `POST` | `/v1/holiday-payroll/evaluate` | Evaluate holiday pay qualification for an employee |
| `GetHolidays` | `GET` | `/v1/holiday-payroll/holidays/{work_area_group_ctrl_nbr}` | List holidays for a work area |

---

### Reporting & Exports

**Service:** `ReportingExportsSrvc` (proto: `modules/reporting_exports.proto`)

Handles payroll data exchange and operational report generation. **ExportPayroll** generates an export batch from a locked payroll run in a format suitable for external payroll systems. **GetExportBatches** retrieves previous exports for audit. **ImportPayroll** ingests payroll data from external systems (e.g. adjustments, corrections). **GenerateDailyReport** produces a daily operational summary for a work area, including staffing levels, absences, overtime, and position vacancy counts.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `ExportPayroll` | `POST` | `/v1/reporting/payroll-export` | Export payroll data for a run |
| `GetExportBatches` | `GET` | `/v1/reporting/payroll-export/{payroll_run_ctrl_nbr}` | List export batches for a payroll run |
| `ImportPayroll` | `POST` | `/v1/reporting/payroll-import` | Import payroll data |
| `GenerateDailyReport` | `POST` | `/v1/reporting/daily-report` | Generate a daily operational report |

---

### Electronic Calling

**Service:** `ElectronicCallingSrvc` (proto: `modules/electronic_calling.proto`)

Sends automated crew call notifications to employees and tracks delivery status. When a dispatcher executes a crew call, the system sends a notification to the employee's phone numbers (in calling order, respecting the `DialOne` flag). **SendCrewCall** initiates the notification. **PollCallStatus** checks the current delivery state (sent, delivered, acknowledged, no answer, declined). The employee's phone number configuration (calling order, dial-one prefix) from the Employee module drives the notification routing.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `SendCrewCall` | `POST` | `/v1/electronic-calling/send` | Send a crew call notification to an employee |
| `PollCallStatus` | `GET` | `/v1/electronic-calling/poll/{request_ctrl_nbr}` | Poll notification delivery status |

---

### Background Services

**Service:** `BackgroundServicesSrvc` (proto: `modules/background_services.proto`)

View and manage the platform's background worker schedules and their execution history. Background workers handle periodic tasks such as outbox message publishing, vacancy resolution polling, payroll deadline reminders, and FRA compliance checks. Each worker has a **schedule** (enabled/disabled, optional cron expression, next fire time). **Execution Logs** record each run's start time, duration, status (success/failure), and any error details. Administrators can enable/disable workers and adjust their schedules without redeploying.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetWorkerSchedules` | `GET` | `/v1/background-services/schedules` | List all worker schedules |
| `UpdateSchedule` | `PUT` | `/v1/background-services/schedules/{ctrl_nbr}` | Update a worker schedule |
| `GetExecutionLogs` | `GET` | `/v1/background-services/logs/{worker_schedule_ctrl_nbr}` | List execution logs for a worker schedule |

---

### Roster Board

**Service:** `RosterBoardSrvc` (proto: `modules/roster_board.proto`)

The roster board is the operational view of a roster — showing all employees in seniority-ranked order with their current status, position assignments, and availability. Dispatchers and crew managers use the roster board to visualize staffing. Hangout temporarily removes an employee from the active board (e.g. for training or temporary reassignment); restore returns them.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `GetRosterBoard` | `GET` | `/v1/roster-board/{ctrl_nbr}` | Get the roster board for a roster (employees in seniority order with status) |
| `HangoutPosition` | `POST` | `/v1/roster-board/hangout` | Hang out a position (temporarily remove from active board) |
| `RestorePosition` | `POST` | `/v1/roster-board/restore` | Restore a hung-out position to the active board |

---

### FRA Compliance

**Service:** `FraComplianceSrvc` (proto: `modules/fra_compliance.proto`)

Federal Railroad Administration Hours of Service compliance tracking. The FRA mandates limits on how long train crews can be on duty before mandatory rest (typically 12 hours on-duty with 10 hours mandatory rest). **Duty Tours** are automatically generated from on-duty/tie-up records in Daily Operations and track total on-duty time, rest time, and compliance status. **SearchDutyTours** provides filtered queries by employee, date range, and compliance status. **Employee Certifications** track FRA-required certifications (e.g. locomotive engineer certification) with issue/expiration dates. Crafts with `HoursOfService = true` are subject to FRA tracking.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `SearchDutyTours` | `GET` | `/api/fra/duty-tours` | Search FRA duty tours (by employee, date range, etc.) |
| `GetDutyTour` | `GET` | `/api/fra/duty-tours/{ctrl_nbr}` | Get a single duty tour record |
| `GetEmployeeCertifications` | `GET` | `/api/fra/certifications/{employee_ctrl_nbr}` | List certifications for an employee |

---

### Railroad Information

**Service:** `RailroadInfoSrvc` (proto: `modules/railroad_info.proto`)

Operational notices and information bulletins with a full lifecycle and read receipt tracking. Railroad information notices (e.g. track conditions, speed restrictions, operational changes) are created in DRAFT state, published to make them visible to employees at a work area, and eventually closed when no longer relevant. Notices can also be cancelled if issued in error. Employees acknowledge reading a notice via **AcknowledgeRead**, which creates a timestamped **Read Receipt**. Management can query read receipts to verify compliance (e.g. all engineers at Jax Yard have read the speed restriction notice).

| Method | HTTP | Route | Description |
|---|---|---|---|
| `CreateInformation` | `POST` | `/v1/railroad-info` | Create a new information notice (draft) |
| `GetInformation` | `GET` | `/v1/railroad-info/{ctrl_nbr}` | Get a single notice |
| `GetByWorkArea` | `GET` | `/v1/railroad-info/work-area/{work_area_group_ctrl_nbr}` | List notices for a work area |
| `PublishInformation` | `POST` | `/v1/railroad-info/{ctrl_nbr}/publish` | Publish a draft notice |
| `CloseInformation` | `POST` | `/v1/railroad-info/{ctrl_nbr}/close` | Close an active notice |
| `CancelInformation` | `POST` | `/v1/railroad-info/{ctrl_nbr}/cancel` | Cancel a notice |
| `AcknowledgeRead` | `POST` | `/v1/railroad-info/{information_ctrl_nbr}/read` | Acknowledge reading a notice |
| `GetReadReceipts` | `GET` | `/v1/railroad-info/{information_ctrl_nbr}/read-receipts` | List read receipts for a notice |

---

### Safety

**Service:** `SafetySrvc` (proto: `modules/safety.proto`)

Safety observation reporting, corrective action tracking, and resolution workflow (BeSafe-style). Employees or supervisors report a **Safety Observation** at a work area, categorized by a **Safety Category** (e.g. track hazard, equipment defect, procedure violation). Each observation can have one or more **Corrective Actions** assigned (describing what must be done to remediate). Once all actions are complete, the observation is **resolved** (closed). **GetByWorkArea** provides a dashboard view of all open and resolved observations for a work area. **GetCategories** and **CreateCategory** manage the reference data for observation classification.

| Method | HTTP | Route | Description |
|---|---|---|---|
| `CreateObservation` | `POST` | `/v1/safety/observations` | Report a safety observation |
| `GetObservation` | `GET` | `/v1/safety/observations/{ctrl_nbr}` | Get a single observation |
| `GetByWorkArea` | `GET` | `/v1/safety/observations/work-area/{work_area_group_ctrl_nbr}` | List observations for a work area |
| `AddAction` | `POST` | `/v1/safety/observations/{observation_ctrl_nbr}/actions` | Add a corrective action to an observation |
| `ResolveObservation` | `POST` | `/v1/safety/observations/{observation_ctrl_nbr}/resolve` | Resolve (close) an observation |
| `GetCategories` | `GET` | `/v1/safety/categories/{work_area_group_ctrl_nbr}` | List safety categories for a work area |
| `CreateCategory` | `POST` | `/v1/safety/categories` | Create a safety category |

---

## Field Encryption

Sensitive PII fields are encrypted at rest using AES-256 with deterministic IV derivation:

| Component | Description |
|---|---|
| `IFieldEncryptor` | Domain interface for encrypt/decrypt operations |
| `AesFieldEncryptor` | AES-256 implementation, reads key from `Encryption:Key` configuration |
| `EncryptedStringConverter` | EF Core `ValueConverter<string, string>` wrapping `IFieldEncryptor` |

**Encrypted fields:**

| Entity | Field |
|---|---|
| `Employee` | `SocialSecurityNumber` |
| `Employee` | `DriversLicenseNumber` |

Encryption is applied transparently via EF Core value converters. No application code calls encrypt/decrypt manually.

## Soft Delete & Global Query Filters

All domain entities inherit from `Entity`, which provides soft-delete support:

| Property | Type | Description |
|---|---|---|
| `IsDeleted` | `bool` | Marks the entity as logically deleted |
| `DeletedAt` | `DateTime?` | UTC timestamp of deletion |
| `DeletedBy` | `AuditStamp?` | Who performed the deletion |

`CrewServiceDbContext.OnModelCreating` applies a global query filter (`WHERE IsDeleted = false`) to every entity that inherits from `Entity`. Use `IgnoreQueryFilters()` for audit queries that need deleted records.

## Roles & Authorization

**Constants:** `CrewService.Domain.Models.UserAccess.Roles`

### Global Role (`User.PrimaryRoleId`)

| Role | Description |
|---|---|
| `SystemAdmin` | Full platform access across all parents; bypasses parent scoping |

### Per-Parent Roles (`UserParentAssignment.Role`)

| Role | Description |
|---|---|
| `ParentAdmin` | Full access within parent, including user/role management |
| `RailroadAdmin` | Full operational access; no user management |
| `CraftManager` | Manages craft configuration (agreement-driven parameters), rosters (seniority lists per craft + railroad), seniority entries (employee rank/date on rosters), displacement policies, and bulletin policies |
| `CrewManager` | Crew staffing, bulletins, absence approvals |
| `Dispatcher` | Dispatch, boards, mark-offs |
| `PayrollClerk` | Time entry, payroll processing |
| `ReadOnly` | View-only across all operational modules |

**Key rules:**

- No `UserParentAssignment` + no `SystemAdmin` on `PrimaryRoleId` = **blocked** (no access)
- `SystemAdmin` is global and does not require per-parent assignment rows
- JWT claims are built from real assignments at authentication time
- Dev bootstrap: `admin@crewservice.dev` / `Admin@123` seeded as `SystemAdmin`

## Development Notes

- **Auto-migration:** In Development, `MigrateDatabasesAsync()` runs automatically at startup for both `DbContext`s
- **Dev seeding:** `DevDataSeeder.SeedAsync()` runs after migrations, idempotently seeding all 14 data sections (group hierarchy, employees, crafts, rosters, seniority, work management, crews, boards, bulletins, dispatching, policies, payroll, safety, FRA compliance)
- **gRPC-Web:** All services are mapped with `.EnableGrpcWeb()` for Blazor client compatibility
- **Swagger:** gRPC transcoding endpoints are browsable at `/swagger` in development
- **Module boundaries:** No cross-module EF navigation; integrate via application interfaces + domain events
- **PII:** Encrypted at rest (AES-256); avoid returning in responses or event payloads

**Configuration (User Secrets):**

```json
{
  "ConnectionStrings": {
    "SQLiteConnection": "Data Source=crewservice.db"
  },
  "Jwt": {
    "Key": "<your-256-bit-secret>",
    "Issuer": "<issuer>",
    "Audience": "<audience>"
  },
  "Encryption": {
    "Key": "<base64-encoded-AES-256-key>"
  },
  "OutboxPublisher": {
    "Enabled": true,
    "PollingInterval": "00:00:30",
    "BatchSize": 100,
    "RetentionDays": 7
  }
}
```

### Testing with Swagger

The API ships with gRPC-JSON transcoding and Swagger UI, so every gRPC endpoint is testable as a standard REST/JSON call from the browser.

**1. Open Swagger UI**

Launch the API in Development and navigate to:

```
https://localhost:<port>/swagger
```

All gRPC services appear as REST endpoints grouped by proto package. Request and response bodies are plain JSON — field names use the `snake_case` convention from the proto definitions.

**2. Authenticate**

Most endpoints require a JWT Bearer token. To obtain one:

1. Expand **`POST /login`** under the `AuthSrvc` section
2. Execute with one of the seeded credentials:

   | Account | Username | Password | Role |
   |---|---|---|---|
   | System Admin | `admin@crewservice.dev` | `Admin@123` | `SystemAdmin` (global) |
   | Seeded employees | `james.smith1@csx.example.com` | `Seed@123` | `ReadOnly` (first 6 employees are upgraded to distinct roles) |

3. Copy the `token` value from the response

**3. Authorize**

1. Click the **🔒 Authorize** button at the top of the Swagger page
2. In the **Value** field enter: `Bearer <paste-your-token-here>`
3. Click **Authorize**, then **Close**

All subsequent requests will include the JWT in the `Authorization` header.

**4. Call endpoints**

- **GET endpoints** — fill in path and query parameters, click **Try it out → Execute**
- **POST/PUT endpoints** — edit the JSON request body, click **Execute**
- **DELETE endpoints** — provide the `ctrl_nbr`, click **Execute**

**5. Common `ctrl_nbr` values**

The `DevDataSeeder` creates entities with auto-generated `ControlNumber` values (timestamp-based `long`). To discover them:

1. Call `GET /parent` to list parents → note the `ctrlNbr` for "CSX Corporation"
2. Call `GET /v1/tenant-config/groups` to list groups → note `ctrl_nbr` for "Jax Yard"
3. Call `GET /v1/employees` to list employees → note `ctrl_nbr` values for subsequent calls
4. Call `GET /v1/crafts` to list crafts, `GET /v1/rosters` for rosters, etc.

> **Tip:** Swagger shows the full JSON schema for every request/response. Hover over model names to expand nested structures.

## Testing

**Framework:** xUnit + Microsoft.NET.Test.Sdk + coverlet (code coverage)

**Test Fixtures** (`CrewService.UnitTests/Fixtures/`):

| Fixture | Purpose |
|---|---|
| `TestDbContextFactory` | Creates in-memory SQLite `CrewServiceDbContext` instances for isolated tests |
| `TestCurrentUserService` | Stub `ICurrentUserService` returning a deterministic test user |
| `TestFieldEncryptor` | No-op `IFieldEncryptor` for tests that don't need real encryption |

## Spec Sheets

Feature specifications are stored in `docs/` at the repository root:

| Spec | Topic |
|---|---|
| `SPEC-0_Crew_Service_Platform_Technical_Spec_FINAL.md` | Platform-level technical specification |
| `spec_4_crew_and_assignment_staffing_model.md` | Crew and assignment staffing model |
| `spec_5_boards_and_dispatching_addendum.md` | Boards and dispatching addendum |
| `spec_6_authentication_tenancy_and_invitations.md` | Authentication, tenancy, and invite-only access |
| `spec_employee_module.md` | Employee module design |
| `spec_employee_module_merged.md` | Employee module merged spec |
| `spec_employee_module_integration_into_dynamic_group_hierarchy.md` | Employee integration into dynamic group hierarchy |
| `spec_railroad_group_placement.md` | Railroad placement in dynamic group hierarchy |
| `spec_user_parent_assignment.md` | User-to-parent assignment with role-based access |

Additional gap analysis and implementation plans are in `CrewService.API/docs/gap-analysis/`.

## License

MIT — see `LICENSE` in repository root.
