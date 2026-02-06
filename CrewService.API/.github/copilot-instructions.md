# Copilot Instructions

## General Guidelines
- Avoid including breathing comments (e.g., 'Taking a deep breath') in responses.
- Do not include 'took a deep breath', 'breathing', or similar filler comments in responses.
- Provide explicit assessments and clear recommendations rather than silence or mere agreement; ensure the user knows when you endorse a design.
- Do not generate any code until explicitly instructed to do so.

## Project Structure
- The repository layout for the API project includes the following components: 'GrpcService', 'Domain', 'Infrastructure', 'Application', 'Persistence', and 'Presentation'.
- Each entity (User, Employee, RailroadEmployee, RailroadPoolEmployee) has its own set of API endpoints, and an additional orchestration endpoint will be added alongside per-entity endpoints.
- The Application layer is part of the CrewService.API project and should be documented in README.md as 'Application (use cases, application services, DTOs, commands/queries)'.
- README.md should be concise and serve as a single source of truth, consolidating information and removing duplicated sections. Ensure it includes a dedicated Orchestration UoW section and avoids repeated content.
- DbContext files will remain separate in the CrewService project, with one for Identity and one for the crewservice domain/persistence. Repositories accept DbContext in their constructors, and orchestrations should account for this, with a dedicated Orchestration UoW section included in the README.

## Orchestration Unit of Work
- Focus discussions on the Orchestration Unit of Work (UoW) and proceed with unit-of-work changes.
- Endorse emitting domain events for all CRUD operations and using the Outbox pattern inside the orchestration UoW. 
- Use event types such as UserCreated, UserUpdated, UserDeleted, EmployeeCreated, EmployeeUpdated, EmployeeDeleted, RailroadEmployee events, and optional composite events.
- Include envelope fields in events: eventid, EventType, AggregateType, AggregateId, OccurredAt, CorrelationId, OrchestrationId, IdempotencyKey, EventVersion, with minimal payloads avoiding PII.
- Raise events inside aggregates/repositories, ensuring the UoW translates domain events to Outbox rows persisted in the same transaction.
- Implement a background publisher to publish and mark outbox rows, including CorrelationId and OrchestrationId, with idempotency handling.
- Consider a soft-delete preference, outbox schema, and retention policies.
- Recommended next steps include adding domain event hooks, creating Outbox DDL, wiring UoW CommitAsync to persist outbox, implementing the background publisher, and conducting integration tests.
- Create domain-event batches in the following specific order: 
  1. Employees + Railroads
  2. RailroadEmployee + RailroadPool + RailroadPoolEmployee + RailroadPoolPayrollTier
  3. Address/Phone/Email and their Type entities
  4. Seniority domain (Craft, Roster, Seniority, SeniorityState)
  5. Employment & Parents (EmploymentStatus, EmploymentStatusHistory, EmployeePriorServiceCredit, Parent)
  6. Remaining entities.