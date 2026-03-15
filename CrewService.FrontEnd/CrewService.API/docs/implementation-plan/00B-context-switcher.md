# Phase 0B — Context Switcher (Parent → Railroad)

**Branch:** `feature/ui-context-switcher`
**Depends on:** Phase 0A (menu shell exists)

## Why Second

Every page below Dashboard needs to know *which Parent and Railroad* the user is
working with. Without a global context, every page would need its own picker,
leading to inconsistent UX and duplicated state logic. Building this immediately
after the menu shell means every placeholder page can display the selected context
even before real data flows through.

## Key Design Decisions

1. **Two-level context only: Parent → Railroad.**
   Work areas are *not* part of the global context. Small railroads like PTRA
   operate across all work areas simultaneously — work areas exist for compensation
   (travel pay), not operational silos. Work-area filtering belongs on individual
   page toolbars, not the global switcher.

2. **Auto-selection for single-assignment users.**
   If a user has exactly one `UserParentAssignment`, the Parent is auto-selected
   and the picker is hidden. If that Parent has exactly one Railroad, that is also
   auto-selected. The user never sees an empty "choose a parent" screen.

3. **SystemAdmin sees all; role-scoped users see their assignments.**
   The parent list is fetched from the user's `UserParentAssignment` records,
   except for SystemAdmin who gets the full `GetAllParents` list.

4. **Selection persisted in `ProtectedSessionStorage`.**
   Survives Blazor Server reconnects and page refreshes. Cleared on logout.

## Architecture

```
┌─────────────────────────────────────────────────┐
│ MainLayout.razor                                │
│  ┌─────────────────────────────────────────┐    │
│  │ <CascadingValue Value="appContext">     │    │
│  │   ┌──────────────┐  ┌───────────────┐   │    │
│  │   │ NavMenu      │  │ @Body         │   │    │
│  │   │ (reads ctx)  │  │ (reads ctx)   │   │    │
│  │   └──────────────┘  └───────────────┘   │    │
│  └─────────────────────────────────────────┘    │
│  ┌──────────────────────────┐                   │
│  │ ContextSwitcher component│ ← top bar         │
│  │ [Parent ▼] [Railroad ▼] │                    │
│  └──────────────────────────┘                   │
└─────────────────────────────────────────────────┘
         │ reads/writes
         ▼
   AppContextService (scoped)
         │ persists to
         ▼
   ProtectedSessionStorage
```

## AppContextService

```csharp
// Services/AppContextService.cs — scoped per circuit
public class AppContextService
{
    public long? SelectedParentCtrlNbr { get; private set; }
    public string? SelectedParentName { get; private set; }
    public long? SelectedRailroadCtrlNbr { get; private set; }
    public string? SelectedRailroadName { get; private set; }

    public event Action? OnContextChanged;

    public void SetParent(long ctrlNbr, string name) { ... }
    public void SetRailroad(long ctrlNbr, string name) { ... }
    public void Clear() { ... }

    public bool HasParent => SelectedParentCtrlNbr.HasValue;
    public bool HasRailroad => SelectedRailroadCtrlNbr.HasValue;
    public bool IsFullySelected => HasParent && HasRailroad;
}
```

**Behavior:**
- `SetParent` clears the selected railroad (forces re-selection).
- `SetRailroad` only works when a parent is already selected.
- `OnContextChanged` fires on every state change so components can react.

## ContextSwitcher Component

```
Components/Layout/ContextSwitcher.razor
```

- Injected into `MainLayout.razor` top bar, between the title and the username.
- Two `<select>` dropdowns: Parent, then Railroad (disabled until parent selected).
- On parent change → fetches railroads for that parent via `ParentsClient.GetByCtrlNbrAsync`.
- On railroad change → calls `AppContextService.SetRailroad`.
- Only visible when user is authenticated.
- Hidden entirely for unauthenticated users.

## Consuming Context in Pages

Pages receive the context via `[CascadingParameter]`:

```razor
@code {
    [CascadingParameter]
    public AppContextService AppContext { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (!AppContext.IsFullySelected) return; // show "select a railroad" message
        await LoadData(AppContext.SelectedRailroadCtrlNbr!.Value);
    }
}
```

## Files Created / Modified

| File | Action |
|------|--------|
| `Services/AppContextService.cs` | **New** — scoped state service |
| `Components/Layout/ContextSwitcher.razor` | **New** — top-bar Parent/Railroad dropdowns |
| `Components/Layout/MainLayout.razor` | **Modify** — add `CascadingValue` + `ContextSwitcher` |
| `Components/Layout/MainLayout.razor.css` | **Modify** — styling for context switcher |
| `Components/Pages/Placeholder.razor` | **Modify** — display selected context or "please select" |
| `Program.cs` | **Modify** — register `AppContextService` as scoped |

## Data Flow

1. **On login** → `ContextSwitcher.OnInitializedAsync`:
   - Fetch user's `UserParentAssignment` records (or all parents for SystemAdmin).
   - If exactly one parent → auto-select.
   - Try restoring from `ProtectedSessionStorage`.

2. **Parent selected** → `ContextSwitcher`:
   - Calls `ParentsClient.GetByCtrlNbrAsync(ctrlNbr)` to get railroads.
   - If exactly one railroad → auto-select.
   - Calls `AppContextService.SetParent(...)`.

3. **Railroad selected** → `ContextSwitcher`:
   - Calls `AppContextService.SetRailroad(...)`.
   - `OnContextChanged` fires → all subscribed components reload.

4. **On logout** → `AppContextService.Clear()` + clear `ProtectedSessionStorage`.

## Existing Clients Used

| Client | Method | Purpose |
|--------|--------|---------|
| `ParentsClient` | `GetAllAsync()` | SystemAdmin parent list |
| `ParentsClient` | `GetByCtrlNbrAsync(ctrlNbr)` | Railroad list for selected parent |
| `TenantConfigClient` | `GetWorkAreasAsync()` | *Not used in switcher* — future page-level filter |

> **Note:** `UserParentAssignment` lookup will be stubbed until Phase 1 completes
> the API audit. Initially, all authenticated users see all parents (SystemAdmin behavior).
> Phase 1 will add role-scoped filtering.

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `feat(ui): add AppContextService for parent/railroad state` | Service class + DI registration |
| 2 | `feat(ui): add ContextSwitcher component` | Dropdowns in top bar |
| 3 | `feat(ui): wire CascadingValue in MainLayout` | CascadingValue wrapping Body |
| 4 | `feat(ui): persist context to ProtectedSessionStorage` | Save/restore on refresh |
| 5 | `feat(ui): auto-select single parent/railroad` | Skip picker when only one option |
| 6 | `feat(ui): show selected context in placeholder pages` | Placeholder displays "Railroad: PTRA" |
