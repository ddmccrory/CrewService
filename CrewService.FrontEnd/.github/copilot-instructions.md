# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- Do not pause for confirmation; continue uninterrupted batched updates to SystemSpec until completion. However, DO ask for confirmation before reverting changes.
- Prefer proper fixes over workarounds/hacks. When the correct solution is known, implement it rather than building interim solutions.
- Fix warnings and technical debt immediately rather than deferring them.
- Work in smaller increments rather than large batch changes. When making multiple edits, apply them incrementally and verify each step.
- Fully discuss and agree on design before any code is written. Do not start coding until explicitly told to proceed.
- Launch wizards from existing list pages via buttons rather than separate pages, and prefer atomic API endpoints (single transaction) over sequential client-side calls for multi-step wizard operations.

## Code Style
- Use specific formatting rules
- Follow naming conventions
- All ControlNumber references in the codebase should use the ControlNumber value object type, not raw long. This applies to all domain entities, value objects, and domain method signatures. When comparing ControlNumber values, use the ControlNumber type directly rather than extracting .Value to long.
- All checkboxes in the project must use Bootstrap slide switch style: wrap in `<div class="form-check form-switch">` with `class="form-check-input"` on the input/InputCheckbox. Never use plain checkboxes.
- Code columns should always be uppercase in display (using `.ToUpperInvariant()`), should be the first column in DataTable definitions, and should be the default sort column.
- Never use raw `<select>` elements for dropdowns. Use `SelectInput` (inside EditForm) or `FilterSelect` (standalone filter dropdowns outside forms) from Components/Shared/. Both auto-select when there is exactly one item, hiding the placeholder. This applies to all pages including filter dropdowns on list/management pages.

## Project-Specific Rules
- Custom requirement A
- Custom requirement B
- Keep the CrewService.BlazorUI.Client WASM project as scaffolding for future use, as it will support the addition of live boards and roster updates requiring real-time client-side interactivity (likely via SignalR/WebAssembly). SignalR/real-time push for invitation status updates is deferred to the broader SignalR buildout planned for this project.
- CRUD/management pages should use `@rendermode InteractiveServer` with `AuthenticationStateProvider` for claims — not SSR with form POSTs. SSR is reserved for document-oriented pages like Login, AcceptInvitation, and Account management. The project goal is to take advantage of Blazor interactivity.
- All admin roles (SystemAdmin, ParentAdmin, RailroadAdmin) should have the same access to features like Administration, Invitations, and admin-only profile fields. The only restriction is they cannot assign roles above their own level in the hierarchy.
- Use buttons (sized evenly) over links on list pages.
- Create dialogs should use the Modal component pattern from the Invitations page (Modal with BodyContent/Footer render fragments, form id linking for submit). This pattern should be followed throughout the entire project. User strongly prefers modal dialogs over inline forms for CRUD operations on management pages. All modals in the project should use static backdrop (clicking outside the modal should not close it).
- Edit and Delete buttons belong on lister/table pages (in row action columns), NOT on detail page headers. Detail pages are read-only views; editing and deleting an item is done from the list page that contains it.
- System group types (Railroad, Assignment) should be created per-parent, not globally. Each parent gets its own Railroad and Assignment types when created.
- Time display format (12-hour vs 24-hour) may become a configurable parent/railroad-level setting in the future. Keep time formatting logic centralized so it can be easily swapped to a system setting later.
- Memory: AssignmentSchedule default behavior: No schedule (no AssignmentSchedule row) means the assignment runs on NO days, not every day. An assignment must have an explicitly set OperatingDaysMask to be included in call sheet generation.
- In the call sheet "Add Assignment → From Template" feature, only load assignments where `IsExtra = true` (Extra Board Assignments) that have an `AssignmentSchedule` matching the same shift definition as the current shift instance.
- All .proto files should be in the shared Protos/ folder at the repo root (C:\Projects\CrewService\Protos\) by design, so both the API and FrontEnd solutions reference the same proto files. The CrewService.API/Protos/ copies should not exist.
- **Dropdown Behavior**: The Crew Work Area dropdown should show ONLY work areas (isWorkArea=true). The Assignment Group dropdown should show the work area AND its descendants. For new assignments, the user must select the location (don't pre-seed with work area). For existing assignments, it should be pre-populated. FilterSelect auto-selects when there's only one option.
- When a board member is called to fill a vacancy, they are placed back on the board at the normal tie-up time (8 or 12 hours after calling time). If they tie up late, their position is re-adjusted. If they tie up early, they are NOT re-adjusted by default — but some railroads may use the early tie-up time. This tie-up time handling is configurable per railroad.
- Memory: All seeder data (DevDataSeeder) should always match the app creation process so we always get the same results. The seeder must create data the same way the application creates data — using the same patterns (UoW, auto-creation of related entities, etc.).
- **Board Types**: Force Assign is a process (employee forced off current position onto a no-bid bulletin position), not a board type. Bulletined Positions are open positions posted for bidding, not a board type. Every board must have a specific operational purpose. Board types are: ExtraBoard, Hangout, ExtendedAbsence, Training, Overtime.