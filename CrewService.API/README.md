# Crew Service Platform

A dynamic railroad crew management and dispatch platform built as a **modular monolith** with gRPC-first APIs and REST transcoding. Designed for highly variable organizational structures, agreement-driven staffing rules, and real-world dispatch operations.

## Table of Contents

- [Summary](#summary)
- [Quickstart](#quickstart)
- [Architecture overview](#architecture-overview)
- [Deployables](#deployables)
- [Domain modules](#domain-modules)
- [Repository layout](#repository-layout)
- [Data strategy](#data-strategy)
- [Orchestration Unit of Work (UoW)](#orchestration-unit-of-work-uow)
- [Domain events & Outbox](#domain-events--outbox)
- [gRPC contract strategy](#grpc-contract-strategy)
- [Implementation order](#implementation-order)
- [Development notes](#development-notes)
- [Testing](#testing)
- [Contributing](#contributing)
- [License](#license)

---

## Summary

- **Targets:** .NET 10, C# 14
- **Architecture:** Modular monolith — one process, many modules
- **API surface:** Canonical gRPC + REST/JSON via transcoding (same `.proto` contracts)
- **Front end:** Blazor Server (Crew.Web) calling Crew.Api via generated gRPC clients
- **Datastores:** SQLite (dev), SQL Server (prod); one physical database per Parent tenant
- **Tenancy:** Parent-based multi-tenant with per-Parent DB isolation

## Quickstart

**Prerequisites:**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2026 (recommended)
- Database (SQLite for dev, SQL Server for prod)

**Steps:**

1. Clone: `git clone https://github.com/ddmccrory/CrewService.git`
2. Restore packages: `dotnet restore`
3. Configure secrets (JWT, connection strings via User Secrets)
4. Run migrations and start: `dotnet run --project CrewService.API/CrewService.GrpcService`

## Architecture overview

**Fundamental principles:**

- Organizational structure is **dynamic and tenant-defined** (Dynamic Groups)
- Operational behavior is **policy-driven and agreement-aware**
- No operational logic depends on hard-coded hierarchy levels
- Modules integrate via **in-process application interfaces** and **domain events** — never direct cross-module DbContext access

## Deployables

| Deployable | Description |
|---|---|
| **Crew.Api** (`CrewService.GrpcService`) | ASP.NET Core host: gRPC endpoints (canonical) + REST/JSON transcoding (secondary) |
| **Crew.Web** (planned) | Blazor Server app calling Crew.Api via generated gRPC clients |

## Domain modules

Each module owns its contracts (proto), application logic, domain rules, and infrastructure:

| Module | Bounded Context | Status |
|---|---|---|
| **TenantConfig** | GroupTypes, DynamicGroups (hierarchical tree), GroupAttributeDefinitions, GroupAttributeValues, WorkArea designation | Scaffolded |
| **Employees** | Employee, Addresses, PhoneNumbers, EmailAddresses, contact types (AddressType, PhoneNumberType, EmailAddressType), EmploymentStatus, EmploymentStatusHistory, EmployeePriorServiceCredits, Craft, Roster, Seniority, SeniorityState, Parent, Railroad, PayrollTier | Existing (legacy structure) |
| **WorkManagement** | AssignmentTemplates, WorkInstances, PositionRoles, PositionSlots, SlotRequirements | Scaffolded |
| **Crews** | Regular/Relief Crews, CrewPositions, CrewIncumbency, CrewAttachmentTemplates, CrewAttachmentInstances, ReliefCoverageRules | Scaffolded |
| **Boards** | ExtraBoards (primary/auxiliary), BoardMembers, BoardCascadePolicies | Scaffolded |
| **Policies** | CraftDisplacementPolicy, DisplacementCases, DisplacementClaims, BulletinPolicy, SeniorityMovePolicy, SeniorityMoves, auto-placement on extra board | Scaffolded |
| **Bulletins** | PositionVacancies (structural, discriminated target for crew/board), Bulletins (bid posting with computed window), BulletinBids (priority ranking, seniority capture), award/forced assignment, auto-withdrawal of lower-priority bids | Scaffolded |
| **Dispatching** | DispatchProjections, DispatchDecisionLogs, DispatchOverrides, EmployeeBookings | Scaffolded |
| **AbsenceVacancy** | AbsenceRequests, VacancyImpacts on PositionSlots | Scaffolded |
| **Payroll** | TimeEntries, PayrollRuns, PayrollRecords, approval/locking | Scaffolded |
| **Auth/Account** | JWT authentication, user accounts, registration, role management | Existing (legacy protos) |
| **Reporting** | Read models, dashboards (optional, deferred) | Planned |

**Boundary rule:** Modules do not call each other's EF Core DbContext directly. They integrate via in-process application interfaces (clean) or domain events (cleaner for future extraction).

## Repository layout

```
CrewService/
├── CrewService.API/
│   ├── CrewService.GrpcService/            # Host entry point, DI composition root
│   ├── CrewService.Domain/                 # Aggregates, value objects, domain events
│   │   ├── Primitives/                     # Entity base class (soft delete, domain event raising)
│   │   ├── ValueObjects/                   # ControlNumber, AuditStamp, Name
│   │   ├── Interfaces/                     # IRepository, IOrchestrationUnitOfWork, IDomainEvent, ICurrentUserService
│   │   ├── Exceptions/                     # DomainException, NotFoundException, ConflictException, ForbiddenException, ValidationException
│   │   ├── Outbox/                         # OutboxMessage, OutboxMessageStatus
│   │   ├── DomainEvents/                   # DomainEvent base + legacy per-entity events (Employees, ContactTypes, Employment, Seniority, Railroads, Parents)
│   │   ├── Models/                         # Legacy entity models
│   │   │   ├── Employees/                  # Employee, Address, PhoneNumber, EmailAddress, EmployeePriorServiceCredit
│   │   │   ├── ContactTypes/               # AddressType, PhoneNumberType, EmailAddressType
│   │   │   ├── Employment/                 # EmploymentStatus, EmploymentStatusHistory
│   │   │   ├── Seniority/                  # Craft, Roster, Seniority, SeniorityState
│   │   │   ├── Parents/                    # Parent
│   │   │   └── Railroads/                  # Railroad, PayrollTier
│   │   └── Modules/                        # New modular domain entities
│   │       ├── TenantConfig/
│   │       ├── WorkManagement/
│   │       ├── Crews/
│   │       ├── Boards/
│   │       ├── Policies/
│   │       ├── Bulletins/
│   │       ├── Dispatching/
│   │       ├── AbsenceVacancy/
│   │       └── Payroll/
│   ├── CrewService.Application/            # Use cases, application services, DTOs (placeholder)
│   ├── CrewService.Infrastructure/         # Cross-cutting concerns
│   │   ├── Exceptions/                     # GrpcExceptionInterceptor, GlobalExceptionHandler
│   │   ├── Models/UserAccount/             # Identity User model
│   │   └── Outbox/                         # OutboxDispatcher (Channel), OutboxPublisherService, NoOpMessagePublisher, Options
│   ├── CrewService.Persistance/            # EF Core DbContexts, migrations, repositories
│   │   ├── Data/                           # OperationsDbContext (CrewServiceDbContext), IdentityDbContext (UserAccessDbContext)
│   │   ├── Configurations/                 # Legacy EF configurations
│   │   ├── Repositories/                   # Legacy repositories
│   │   ├── UnitOfWork/                     # OrchestrationUnitOfWork, Factory
│   │   └── Modules/                        # New modular configurations + repositories
│   │       ├── TenantConfig/
│   │       ├── WorkManagement/
│   │       ├── Crews/
│   │       ├── Boards/
│   │       ├── Policies/
│   │       ├── Bulletins/
│   │       ├── Dispatching/
│   │       ├── AbsenceVacancy/
│   │       └── Payroll/
│   ├── CrewService.Presentation/           # gRPC service implementations + protos
│   │   ├── Protos/                         # Legacy per-entity proto files + common.proto
│   │   │   └── modules/                    # New per-module proto files
│   │   └── Services/                       # Legacy gRPC services (Auth, Account, Employee, ContactTypes, Employment, Seniority, Railroad, PayrollTier, Parent)
│   │       └── Modules/                    # New per-module gRPC services (TenantConfig, WorkManagement, Crews, Boards, Policies, Bulletins, Dispatching, AbsenceVacancy, Payroll)
│   └── CrewService.UnitTests/
├── docs/                                   # Diagrams, specs, scope documents
└── tests/                                  # Integration tests
```

## Data strategy

- **Single physical database** per Parent tenant per environment
- **Two DbContexts** per Parent DB:
  - `IdentityDbContext` (`UserAccessDbContext`) — ASP.NET Core Identity + invitations
  - `OperationsDbContext` (`CrewServiceDbContext`) — all operational domain tables
- Separate EF migrations history tables per context
- Operations tables partitioned by **module ownership** (schema prefix in SQL Server, naming prefix in SQLite)
- Effective-dated records use `[start_utc, end_utc)` semantics (end nullable for open-ended)

## Orchestration Unit of Work (UoW)

**Purpose:** Short-lived orchestration UoW creating a single shared `DbConnection` + `DbTransaction` across both DbContexts.

| Component | Location | Description |
|---|---|---|
| `IOrchestrationUnitOfWork` | Domain/Interfaces | Interface: repositories + commit/rollback |
| `IOrchestrationUnitOfWorkFactory` | Domain/Interfaces | Factory for creating UoW instances |
| `OrchestrationUnitOfWork` | Persistance/UnitOfWork | Concrete implementation |
| `OrchestrationUnitOfWorkFactory` | Persistance/UnitOfWork | Creates dedicated connection/transaction per UoW |

**Safety rules:**

- Single explicit `DbTransaction` on one opened connection (no MSDTC)
- `SaveChanges()` to obtain IDs before creating dependent entities (within same transaction)
- Never pass EF entity instances across DbContexts; pass IDs only
- Keep transaction lifetime minimal
- Make orchestration idempotent (idempotency key or unique constraints)
- Add correlation IDs for tracing

## Domain events & Outbox

**Domain Event envelope** (`DomainEvent` base record):

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

**Outbox flow:**

1. UoW collects domain events from aggregates
2. On `CommitAsync()`, events → `OutboxMessage` rows in the same transaction
3. `OutboxDispatcher` signals background publisher via Channel
4. `OutboxPublisherService` publishes immediately (channel) or polls (fallback)

## gRPC contract strategy

One `.proto` per module with REST transcoding annotations in the same proto:

**Module protos** (`Protos/modules/`):

| Proto | Module |
|---|---|
| `modules/tenant_config.proto` | TenantConfig |
| `modules/work_management.proto` | WorkManagement |
| `modules/crews.proto` | Crews |
| `modules/boards.proto` | Boards |
| `modules/policies.proto` | Policies (displacement, bulletin policy, seniority move policy, seniority moves) |
| `modules/bulletins.proto` | Bulletins (vacancies, bulletins, bids) |
| `modules/dispatching.proto` | Dispatching |
| `modules/absence_vacancy.proto` | AbsenceVacancy |
| `modules/payroll.proto` | Payroll |

**Legacy per-entity protos** (`Protos/`) — will consolidate into module protos:

| Proto | Entity/Feature |
|---|---|
| `auth.proto` | JWT authentication (login, token refresh) |
| `account.proto` | User registration, account management |
| `employee.proto` | Employee CRUD |
| `address_type.proto` | Address type lookups |
| `phone_number_type.proto` | Phone number type lookups |
| `email_address_type.proto` | Email address type lookups |
| `employment_status.proto` | Employment status lookups |
| `employment_status_history.proto` | Employment status history |
| `prior_service_credit.proto` | Employee prior service credits |
| `craft.proto` | Crafts |
| `roster.proto` | Rosters |
| `seniority.proto` | Seniority records |
| `seniority_state.proto` | Seniority states |
| `parent.proto` | Parent tenants |
| `railroad.proto` | Railroads |
| `payroll_tier.proto` | Payroll tiers |
| `common.proto` | Shared messages (DeleteResponse, etc.) |

## Implementation order

Aligned with SPEC-0 §10:

1. **TenantConfig** — group tree + types + attributes + WorkArea designation
2. **Employees refactor** — restructure existing entities into module, add availability/qualifications/memberships
3. **WorkManagement** — templates, work instances, position roles, position slots, slot requirements
4. **Crews** — regular/relief crews, positions, incumbency, attachment to work, relief coverage rules
5. **Boards + Policies** — board definitions, cascade config, ordering strategies, displacement policies/cases/claims
6. **Bulletins + Policies** — structural vacancies, bulletin posting/bidding/award, bulletin policy, seniority move policy, seniority moves, forced assignment, auto-withdrawal of lower-priority bids
7. **Dispatching** — projections, calling-time binding, decision logs, overrides, employee bookings
8. **AbsenceVacancy** — absence requests, approvals, vacancy impacts on position slots
9. **Payroll** — time entry, payroll runs, payroll records, approval/locking
10. **Crew.Web** — Blazor Server app with gRPC client facades per module
11. **Tenancy + Auth** — Parent registry, tenant resolution, per-Parent OIDC/internal auth, invitations

## Development notes

- **Separate DbContexts:** `UserAccessDbContext` (Identity) and `CrewServiceDbContext` (operations)
- **Cross-context transactions:** Same DB/connection via shared `DbConnection`/`DbTransaction` in UoW
- **Uniqueness:** Enforce DB uniqueness; make create operations idempotent
- **PII:** Treat as sensitive; encrypt at rest, avoid returning in responses or event payloads
- **Module boundaries:** No cross-module EF navigation; integrate via app interfaces + domain events

**Configuration (appsettings.json / User Secrets):**

```json
{
  "ConnectionStrings": {
    "SQLiteConnection": "Data Source=crewservice.db"
  },
  "Jwt": {
    "Key": "<your-secret-key>",
    "Issuer": "<issuer>",
    "Audience": "<audience>"
  },
  "OutboxPublisher": {
    "Enabled": true,
    "PollingInterval": "00:00:30",
    "BatchSize": 100,
    "RetentionDays": 7
  }
}
```

## Testing

- Effective-date overlap validators
- No-double-booking under concurrency
- Deterministic calling order and skip reason capture
- Cross-Parent access rejection
- Orchestration success and failure paths
- Outbox publishing and message delivery

## Contributing

Fork, create a release-based feature branch (e.g., `0.2.0/feature-name`), add tests, and open a pull request.

## License

MIT — see `LICENSE` in repository root.

