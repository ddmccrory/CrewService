# Copilot Instructions

## General Guidelines
- Avoid including breathing comments (e.g., 'Taking a deep breath') in responses.
- Do not include 'took a deep breath', 'breathing', or similar filler comments in responses.
- Provide explicit assessments and clear recommendations rather than silence or mere agreement; ensure the user knows when you endorse a design.

## Project Structure
- The repository layout for the API project includes the following components: 'GrpcService', 'Domain', 'Infrastructure', 'Application', 'Persistence', and 'Presentation'.
- Each entity (User, Employee, RailroadEmployee, RailroadPoolEmployee) has its own set of API endpoints, and an additional orchestration endpoint will be added alongside per-entity endpoints.
- The Application layer is part of the CrewService.API project and should be documented in README.md as 'Application (use cases, application services, DTOs, commands/queries)'.
- README.md should be concise and serve as a single source of truth, consolidating information and removing duplicated sections. Ensure it includes a dedicated Orchestration UoW section and avoids repeated content.
- DbContext files will remain separate in the CrewService project, with one for Identity and one for the crewservice domain/persistence. Repositories accept DbContext in their constructors, and orchestrations should account for this, with a dedicated Orchestration UoW section included in the README.

## Orchestration Unit of Work
- Focus discussions on the Orchestration Unit of Work (UoW) and proceed with unit-of-work changes.