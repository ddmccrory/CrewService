# CrewService Solution

CrewService is a modular .NET 10 gRPC-based microservice for crew and employee management. The host project groups logical layers and domain modules to simplify deployment, transactionality, and consistent bootstrapping.

## Table of Contents

- [Summary](#summary)
- [Quickstart](#quickstart)
- [Overview](#overview)
- [Repository layout](#repository-layout)
- [UI context — creation mapping](#ui-context---creation-mapping)
- [Orchestration Unit of Work (UoW)](#orchestration-unit-of-work-uow)
- [Sequence diagram](#sequence-diagram)
- [Development notes](#development-notes)
- [Testing](#testing)
- [Contributing](#contributing)
- [License](#license)

---

## Summary

- Targets: `.NET 10`, C# 14  
- Purpose: Provide gRPC endpoints (with JSON transcoding), a clean layered structure for domain-driven design, and a short‑lived orchestration UoW for atomic multi-context flows.

## Quickstart

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)  
- __Visual Studio__ 2026 (recommended)  
- Database (SQL Server or configured EF provider)

Steps:

1. Clone:
   - `git clone https://github.com/ddmccrory/CrewService.git`
2. Restore packages:
   - `dotnet restore`
3. Configure secrets (JWT, connection strings).
4. Run migrations and start:
   - `dotnet run --project CrewService.API`

## Overview

Core goals:

- Expose gRPC endpoints with JSON transcoding for REST compatibility.
- Use a clean, layered organization for domain-driven development.
- Support multiple creation scopes driven by UI context (Users, Employees, Railroad, Railroad Pool).

## Repository layout

Top-level API/host project groups the implementation layers and domain modules so the host can manage cross-cutting concerns consistently.

- `CrewService.API` (host)
  - Application layers / entry points
  - Persistence (EF Core DbContext, migrations)
  - Presentation (gRPC services, JSON transcoding, swagger)
  - Infrastructure (adapters, Identity concrete `User` at `CrewService.Infrastructure\Models\UserAccount\User.cs`)
  - Domain (aggregates, value objects, domain events: `Employee`, `RailroadEmployee`, `RailroadPoolEmployee`, etc.)
  - GrpcService (service registrations and gRPC-Web / transcoding wiring)
- `CrewService.FrontEnd` — Blazor UI and pages
- `docs/` — diagrams and supporting docs
- `tests/` — unit and integration tests

Rationale: grouping Domain, Infrastructure, Persistence, Presentation, and GrpcService under the host simplifies achieving a single transactional boundary and consistent bootstrapping.

## UI context — creation mapping

The active UI determines which entities to create:

- Users UI → create `User` only.  
- Employees UI → create `User` + `Employee`.  
- Railroad UI → create `User` + `Employee` + `RailroadEmployee`.  
- Railroad Pool UI → create `User` + `Employee` + `RailroadEmployee` + `RailroadPoolEmployee`.

The orchestrator should accept a composite payload or an explicit scope flag and create only the required entities.

## Orchestration Unit of Work (UoW)

Purpose:
- Provide a short‑lived orchestration UoW that creates a single shared `DbConnection` + `DbTransaction` and instantiates one or both DbContexts:
  - `UserAccessDbContext` (identity / `User`)
  - `CrewServiceDbContext` (domain entities)

High-level behavior:
- Factory creates and opens a `DbConnection`, begins a `DbTransaction`.
- Builds `DbContextOptions` for requested contexts using the same connection.
- Instantiates contexts and calls `Database.UseTransaction(transaction)` on each enlisted context.
- Provides repository instances bound to those contexts.
- Caller performs operations, then calls `CommitAsync()`; on error, call `RollbackAsync()` then dispose.

DI / Registration:
- Add an `IOrchestrationUnitOfWorkFactory` registered in DI (transient) returning `IOrchestrationUnitOfWork`.
- Keep existing scoped DbContext registrations for non-orchestration flows unchanged.
- Repositories remain one-per-entity under `CrewService.Persistance.Repositories` and accept a context instance via constructor.

Safety rules:
- Use single explicit `DbTransaction` on one opened connection to avoid MSDTC promotions.
- Call `SaveChanges()` on the context that creates the `User` to obtain `User.Id` before creating dependent entities (still inside the same transaction).
- Never pass EF entity instances across DbContexts; pass IDs only.
- Keep transaction lifetime minimal and avoid long-running I/O inside transaction boundaries.

Migrations & schema:
- Ensure `Employee.UserId -> User.Id` FK exists and migrations are coordinated.
- Prefer explicit cascade behavior (recommend `Restrict` for explicit deletes).

Observability & safety:
- Make orchestration idempotent (idempotency key or unique constraints).
- Add correlation IDs to logs for each orchestration.
- Provide integration tests for success and failure paths.

## Sequence diagram

The following mermaid sequence diagram shows the orchestrator flow for the four employee UI contexts:

Users_UI->>Orchestrator: CreateUser(payload: user-only)  
Employees_UI->>Orchestrator: CreateUser(payload: user + employee)  
Railroad_UI->>Orchestrator: CreateUser(payload: user + employee + railroadEmployee)  
RailroadPool_UI->>Orchestrator: CreateUser(payload: user + employee + railroadEmployee + pool)

Orchestrator->>Orchestrator: Validate payload & determine creation scope  
Orchestrator->>DbContext: Begin transaction / UnitOfWork

Orchestrator->>UserManager: Create User (email, password, profile)  
UserManager-->>Orchestrator: Created User.Id

alt Employee requested  
    Orchestrator->>EmployeeRepo: Create Employee (User.Id, employee data)  
    EmployeeRepo-->>Orchestrator: Employee.Id, EmployeeNumber  
    Orchestrator->>UserManager: Update User (mirror EmployeeNumber / PrimaryRole) [optional]  
end

alt RailroadEmployee requested  
    Orchestrator->>RailroadEmployeeRepo: Create RailroadEmployee (Employee.Id, railroad data)  
    RailroadEmployeeRepo-->>Orchestrator: RailroadEmployee.Id  
end

alt RailroadPoolEmployee requested  
    Orchestrator->>RailroadPoolRepo: Create RailroadPoolEmployee (RailroadEmployee.Id, pool data)  
    RailroadPoolRepo-->>Orchestrator: PoolEmployee.Id  
end

Orchestrator->>DbContext: Commit transaction  
DbContext-->>Orchestrator: Commit OK

Orchestrator->>Outbox: Store domain events / integration messages  
Outbox-->>EventBus: Publish events asynchronously  
EventBus-->>Orchestrator: Publish ACK

## Development notes

- Separate `DbContext` classes are used: one for Identity persistence and another for domain persistence.
- Cross-context transaction strategies:
  - Same DB/connection: consolidate contexts or share `DbConnection`/`DbTransaction`.
  - Different DBs: prefer outbox + message broker or a saga for eventual consistency.
- Enforce DB uniqueness (email, employee number) and make create operations idempotent.
- Treat PII as sensitive: encrypt at rest and avoid returning PII in responses.
- Assign roles/claims within the same transactional boundary if they depend on created domain entities; otherwise, perform role assignment as a subsequent idempotent operation.

## Testing

Recommended integration tests:
- user-only
- user → employee
- user → employee → railroad employee
- full path (user → employee → railroad employee → railroad pool)
- simulated failures at each step to verify rollback and idempotency/retry behavior

## Contributing

Fork, create a feature branch, add tests for behavior changes, and open a pull request.

## License

MIT — see `LICENSE` in repository root.

## Notes

- Projects target: `.NET 10`
- Repositories are located under `CrewService.Persistance.Repositories`.
- UoW must support both single‑context and dual‑context flows; factory should accept options (e.g., `needUserContext`, `needCrewContext`).

