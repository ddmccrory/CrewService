# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- Do not pause for confirmation; continue uninterrupted batched updates to SystemSpec until completion. However, DO ask for confirmation before reverting changes.

## Code Style
- Use specific formatting rules
- Follow naming conventions

## Project-Specific Rules
- Custom requirement A
- Custom requirement B
- Keep the CrewService.BlazorUI.Client WASM project as scaffolding for future use, as it will support the addition of live boards and roster updates requiring real-time client-side interactivity (likely via SignalR/WebAssembly). SignalR/real-time push for invitation status updates is deferred to the broader SignalR buildout planned for this project.
- CRUD/management pages should use `@rendermode InteractiveServer` with `AuthenticationStateProvider` for claims — not SSR with form POSTs. SSR is reserved for document-oriented pages like Login, AcceptInvitation, and Account management. The project goal is to take advantage of Blazor interactivity.
- All admin roles (SystemAdmin, ParentAdmin, RailroadAdmin) should have the same access to features like Administration, Invitations, and admin-only profile fields. The only restriction is they cannot assign roles above their own level in the hierarchy.
- Use buttons (sized evenly) over links on list pages.
- Create dialogs should use the Modal component pattern from the Invitations page (Modal with BodyContent/Footer render fragments, form id linking for submit). This pattern should be followed throughout the entire project. User strongly prefers modal dialogs over inline forms for CRUD operations on management pages.