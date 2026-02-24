# Copilot Instructions

## General Guidelines
- Avoid including breathing comments (e.g., 'Taking a deep breath') in responses.
- Do not include 'took a deep breath', 'breathing', or similar filler comments in responses.
- Provide explicit assessments and clear recommendations rather than silence or mere agreement; ensure the user knows when you endorse a design.
- Do not generate any code until explicitly instructed to do so.

## Branch Naming Conventions
- Prefer release-based branch naming with version numbers (e.g., "0.1.1/feature-name" or "release/0.1.1-feature-name") instead of simple feature branch names.

## Project Structure
- **Architecture:** Modular monolith — one process, many modules. Each module owns contracts (proto), domain, application logic, and infrastructure.
- **Layer projects:** GrpcService (host), Domain, Application, Infrastructure, Persistance, Presentation.
- **Module folders:** New modules are organized under `Modules/` subfolders within Domain, Persistance, and Presentation. Legacy entities remain under `Models/`, `Configurations/`, `Repositories/`, and `Services/`.
- **Bounded contexts:** TenantConfig, Employees, WorkManagement, Crews, Boards, Policies, Dispatching, AbsenceVacancy, Payroll, Reporting (planned).
- **One `.proto` per module** under `Protos/modules/`; legacy per-entity protos remain under `Protos/`.
- **Two DbContexts:** IdentityDbContext (UserAccessDbContext) for Identity; OperationsDbContext (CrewServiceDbContext) for all operational domain tables. Repositories accept DbContext in their constructors.
- **Module boundary rule:** Modules do not call each other's EF repositories or DbContext directly. Integrate via in-process application interfaces and domain events.
- README.md is the single source of truth for architecture and layout.

## Orchestration Unit of Work
- Short-lived orchestration UoW shares a single DbConnection + DbTransaction across both DbContexts.
- Emit domain events for all CRUD operations; use the Outbox pattern inside the UoW.
- Include envelope fields: EventId, EventType, AggregateType, AggregateId, OccurredAt, CorrelationId, OrchestrationId, IdempotencyKey, EventVersion, with minimal payloads avoiding PII.
- Raise events inside aggregates; UoW translates domain events to Outbox rows in the same transaction.
- Background publisher publishes and marks outbox rows with CorrelationId and OrchestrationId.
- Soft-delete preference, outbox schema, and retention policies apply.