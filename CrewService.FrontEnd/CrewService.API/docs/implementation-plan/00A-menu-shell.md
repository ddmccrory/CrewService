# Phase 0A — Menu Shell & Placeholder Pages

**Branch:** `feature/ui-menu-shell`
**Depends on:** Nothing (this is the first thing built)

## Why First

Before wiring any domain functionality, the application needs a navigable skeleton.
Users and stakeholders can see every planned feature, understand the grouping,
and validate that role-based visibility works. Every menu item lands on a stub page
so nothing is a dead link.

## Design Decisions

- **Task-oriented groups** — menu items grouped by what the user is trying to do,
  not by system module. Cross-cutting features appear in the group where the user
  most naturally looks for them.
- **Collapsible groups** — each group is a collapsible section in the sidebar.
  Empty groups (no items visible for the current role) are hidden entirely.
- **Role gating** — each group and item specifies which roles can see it.
  Uses `AuthorizeView` with `Roles` attribute.
- **Existing pages preserved** — Dashboard, Parents, ParentDetail, GroupTypes,
  GroupTypeDetail, GroupDetail pages keep their current routes. They move under
  the appropriate menu group.
- **Remove Register link** — this is an invitation-only application. The
  `Register` NavLink in the `NotAuthorized` block of `NavMenu.razor` must be
  removed. Unauthenticated users see only Login.

## Menu Structure

### Daily Operations
*Roles: Dispatcher, CrewManager, RailroadAdmin, SystemAdmin*

| Item | Route | Roles |
|------|-------|-------|
| Call Board | `/daily/call-board` | Dispatcher, CrewManager |
| Assignments | `/daily/assignments` | Dispatcher, CrewManager |
| Mark-Offs | `/daily/mark-offs` | Dispatcher, CrewManager |
| On-Duty / Off-Duty | `/daily/duty-status` | Dispatcher |
| Vacancy Resolution | `/daily/vacancy-resolution` | Dispatcher |

### Crew Staffing
*Roles: CrewManager, RailroadAdmin, SystemAdmin*

| Item | Route | Roles |
|------|-------|-------|
| Crews | `/staffing/crews` | CrewManager, RailroadAdmin |
| Extra Boards | `/staffing/extra-boards` | CrewManager, RailroadAdmin |
| Bulletins & Bids | `/staffing/bulletins` | CrewManager, RailroadAdmin |
| Roster Boards | `/staffing/roster-boards` | CrewManager, RailroadAdmin |

### Employee Management
*Roles: RailroadAdmin, CraftManager, ParentAdmin, SystemAdmin*

| Item | Route | Roles |
|------|-------|-------|
| Employees | `/employees` | RailroadAdmin, CraftManager, ParentAdmin |
| Seniority Rosters | `/employees/seniority` | RailroadAdmin, CraftManager |
| Crafts | `/employees/crafts` | RailroadAdmin, CraftManager |
| Prior Service Credits | `/employees/prior-service` | RailroadAdmin |
| Invitations | `/admin/invitations` | ParentAdmin, RailroadAdmin |

### Payroll
*Roles: PayrollClerk, RailroadAdmin, SystemAdmin*

| Item | Route | Roles |
|------|-------|-------|
| Payroll Dashboard | `/payroll` | PayrollClerk, RailroadAdmin |
| Pay Rates | `/payroll/rates` | PayrollClerk, RailroadAdmin |
| Earning Codes | `/payroll/earning-codes` | PayrollClerk, RailroadAdmin |
| Holidays | `/payroll/holidays` | PayrollClerk, RailroadAdmin |
| Export / Import | `/payroll/export` | PayrollClerk, RailroadAdmin |

### Compliance
*Roles: RailroadAdmin, CraftManager, SystemAdmin*

| Item | Route | Roles |
|------|-------|-------|
| FRA Compliance | `/compliance/fra` | RailroadAdmin, CraftManager |
| Safety Observations | `/compliance/safety` | RailroadAdmin, CraftManager |
| Absence Codes | `/compliance/absence-codes` | RailroadAdmin |
| Policies | `/compliance/policies` | RailroadAdmin |

### Information
*Roles: all authenticated users*

| Item | Route | Roles |
|------|-------|-------|
| Railroad Info | `/info/railroad` | (all authenticated) |
| Reports | `/info/reports` | (all authenticated) |

### Administration
*Roles: SystemAdmin, ParentAdmin, RailroadAdmin*

| Item | Route | Roles |
|------|-------|-------|
| Parents | `/parents` | SystemAdmin, ParentAdmin, RailroadAdmin |
| Group Types | `/config/group-types` | SystemAdmin, ParentAdmin |
| User Assignments | `/admin/users` | ParentAdmin, RailroadAdmin |
| Notification Config | `/admin/notifications` | RailroadAdmin |
| Background Jobs | `/admin/jobs` | SystemAdmin |

## Files Created / Modified

| File | Action |
|------|--------|
| `Components/Layout/NavMenu.razor` | Rewrite — collapsible groups with role gating |
| `Components/Layout/NavMenu.razor.css` | Update — styling for collapsible groups |
| `Components/Pages/Placeholder.razor` | New — generic "Coming Soon" page with `@page` for every stub route |
| `Components/Pages/Daily/*.razor` | New — one stub per Daily Operations item (or use Placeholder with route params) |
| `Components/Pages/Staffing/*.razor` | New — one stub per Crew Staffing item |
| `Components/Pages/Employees/*.razor` | New — stubs (existing Employees page moves here later) |
| `Components/Pages/Payroll/*.razor` | New — one stub per Payroll item |
| `Components/Pages/Compliance/*.razor` | New — one stub per Compliance item |
| `Components/Pages/Info/*.razor` | New — one stub per Information item |
| `Components/Pages/Admin/*.razor` | New — stubs for admin items not already built |

## Implementation Approach

**Option chosen: single `Placeholder.razor` with multiple `@page` directives.**
This avoids creating 25+ nearly identical files. Each route gets an `@page` directive
on the same component. The component reads `NavigationManager.Uri` to display the
page name. When real functionality is built, the `@page` directive is removed from
Placeholder and a dedicated page file is created.

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `feat(ui): add collapsible sidebar menu groups` | Rewrite NavMenu.razor with task-oriented groups |
| 2 | `fix(ui): remove Register link — invitation-only app` | Remove Register NavLink from NotAuthorized block |
| 3 | `feat(ui): add role-based menu visibility` | AuthorizeView wrappers on each group/item |
| 4 | `feat(ui): add placeholder page for all stub routes` | Single Placeholder.razor with route mapping |
| 5 | `style(ui): update sidebar CSS for collapsible groups` | NavMenu.razor.css updates |
| 6 | `refactor(ui): move existing pages under new menu structure` | Update NavLink hrefs for Parents, GroupTypes |
