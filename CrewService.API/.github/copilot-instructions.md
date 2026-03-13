# Copilot Instructions

## General Guidelines
- Avoid including breathing comments (e.g., 'Taking a deep breath') in responses.
- Do not include 'took a deep breath', 'breathing', or similar filler comments in responses.
- Provide explicit assessments and clear recommendations rather than silence or mere agreement; ensure the user knows when you endorse a design.
- Do not generate any code until explicitly instructed to do so.
- Ensure that all functionality from the legacy SA system is provided in CrewService while eliminating all legacy defects. Every process should be improved architecturally, but automated processes must produce the same end effect to the user. Port domain knowledge and improve architecture — this is a complete functional replacement with better engineering.
- Avoid hard-coded pool-specific logic from the legacy system; instead, implement a configurable rule system that allows any railroad to define their own rules, rather than reproducing the specific logic of one railroad.
- FRA compliance rules (and other government regulations added later) apply universally to ALL railroads for ALL parents/tenants. They should NOT be redefined per-railroad. FRA policy values are system-level defaults derived from the CFR, not tenant-configurable overrides. The configurable part is only which crafts are covered (HoursOfService = true/false), not the regulatory limits themselves.
- Craft qualifications — specifically Conductors/Switchmen and Engineers — are FRA-regulated and must be system-level policies, not tenant-configurable. Any government-regulated qualification requirements should be modeled as system-level, same as RegulatoryStandard.
- Drug and alcohol compliance (Part 219), certification requirements (Parts 240/242), and all FRA regulatory processes are system-level — they apply to all covered employees across all railroads and all parents. They should NOT be modeled as tenant-configurable policies. The system enforces them universally for any employee flagged as covered.
- Build all backend (CrewService.API) first across all branches, then build frontend (CrewService.FrontEnd/BlazorUI) separately as a follow-on phase. No data migration from SA — this is a greenfield build with seed data only. Frontend specs will be process-oriented and discussed later.
- Use the ControlNumber value object for all CtrlNbr parameters; never use raw long. The codebase uses ControlNumber consistently.
- Always pass CancellationToken explicitly to async methods - never rely on default = default. Repository overrides should only exist when the derived logic actually differs from the base (e.g., adding Includes). Don't override just to repeat the same logic.
- All entities must have explicit EF Configuration files. Do not rely on ApplyConfigurationsFromAssembly to implicitly discover unconfigured entities.
- Use `AuditStamp.Create("SYSTEM")` only in development and seeding contexts. Production entity creation should not hardcode "SYSTEM" as the audit name since the DbContext interceptor overwrites it with the authenticated user anyway.

## Branch Naming Conventions
- Always bump version numbers when creating new branches. Use release-based branch naming with incremented version numbers (e.g., "0.1.2/feature-name") — never reuse the same version number across branches.
- Prefer release-based branch naming with version numbers (e.g., "0.1.1/feature-name" or "release/0.1.1-feature-name") instead of simple feature branch names.

## Project Structure
- **Architecture:** Modular monolith – one process, many modules. Each module owns contracts (proto), domain, application logic, and infrastructure.
- **Layer projects:** GrpcService (host), Domain, Application, Infrastructure, Persistence, Presentation.
- **Module folders:** New modules are organized under `Modules/` subfolders within Domain, Persistence, and Presentation. Legacy entities remain under `Models/`, `Configurations/`, `Repositories/`, and `Services/`.
- **Bounded contexts:** TenantConfig, Employees, UserAccess, WorkManagement, Crews, Boards, Policies, Dispatching, AbsenceVacancy, Payroll, Reporting (planned).
- **Protos organization:** Use a shared `Protos` folder at the repo root for gRPC proto files, referenced by both backend (CrewService.API/CrewService.Presentation) with `GrpcServices="Server"` and frontend (CrewService.FrontEnd/CrewService.BlazorUI) with `GrpcServices="Client"` via relative paths.
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

## Messaging
- CrewService does NOT use MSMQ. MSMQ is a legacy SA technology that is not being ported. The replacement is domain events + outbox pattern (already implemented via OutboxMessage/IOutboxDispatcher) or in-process Channel<T> for pipeline orchestration.