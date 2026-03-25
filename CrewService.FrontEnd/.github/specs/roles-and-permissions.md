# Roles & Permissions — Feature Spec

## Overview

Replace hardcoded role constants with a database-driven roles and permissions system.
Admins can create custom roles, map roles to features with access levels (None / ReadOnly / FullAccess),
and optionally override permissions per parent company.

## Design Decisions

1. **Custom roles** — Admins can create new roles beyond the built-in system roles.
2. **Per-parent overrides** — A parent company can override the global permission defaults for its own users.
3. **API enforcement** — Permissions are enforced at both the UI (nav menu, page-level) and API (gRPC endpoint) layers.

---

## Domain Model

### AccessLevel (enum)

| Value | Meaning |
|---|---|
| `None` | No access — menu item hidden, endpoint blocked |
| `ReadOnly` | Can view but not create/edit/delete |
| `FullAccess` | Full CRUD access |

### Role (entity)

| Property | Type | Notes |
|---|---|---|
| `CtrlNbr` | `ControlNumber` | PK (auto-generated) |
| `Name` | `string` | Unique, e.g. "SystemAdmin", "TrainmasterSupervisor" |
| `Description` | `string` | Human-readable description |
| `IsSystem` | `bool` | `true` for built-in roles (cannot be deleted/renamed) |
| `Level` | `int` | Hierarchy rank — higher = more authority. Used to prevent assigning roles above caller's level |

System roles seeded at startup (mirrors current `Roles.cs`):

| Name | Level | IsSystem |
|---|---|---|
| SystemAdmin | 100 | true |
| ParentAdmin | 80 | true |
| RailroadAdmin | 60 | true |
| CraftManager | 40 | true |
| CrewManager | 40 | true |
| Dispatcher | 40 | true |
| PayrollClerk | 40 | true |
| Employee | 20 | true |

### Feature (entity)

| Property | Type | Notes |
|---|---|---|
| `Key` | `string` | PK, e.g. `"daily/call-board"`, `"parents"` |
| `DisplayName` | `string` | e.g. "Call Board", "Parents" |
| `Category` | `string` | Grouping for the matrix UI, e.g. "Daily Operations", "Administration" |
| `Route` | `string` | The Blazor page route, e.g. `"/daily/call-board"` |

Seeded from nav menu routes at startup.

### Permission (entity)

| Property | Type | Notes |
|---|---|---|
| `CtrlNbr` | `ControlNumber` | PK |
| `RoleCtrlNbr` | `long` | FK → Role |
| `FeatureKey` | `string` | FK → Feature |
| `AccessLevel` | `AccessLevel` | None / ReadOnly / FullAccess |
| `ParentCtrlNbr` | `long?` | `null` = global default, set = parent-specific override |

**Unique constraint:** `(RoleCtrlNbr, FeatureKey, ParentCtrlNbr)` — one permission per role+feature+parent combo.

### Effective Permission Resolution

For a given user + feature:

1. Get user's role and parent context
2. Look for parent-specific permission: `WHERE RoleCtrlNbr = @role AND FeatureKey = @feature AND ParentCtrlNbr = @parent`
3. Fall back to global default: `WHERE RoleCtrlNbr = @role AND FeatureKey = @feature AND ParentCtrlNbr IS NULL`
4. If no permission exists → `AccessLevel.None`

---

## Branch Plan

### Branch 1: `feature/roles-domain-and-persistence`

**Foundation — domain model, persistence, seed data**

| Commit | Scope |
|---|---|
| Add AccessLevel enum and domain entities | `AccessLevel`, `Role`, `Feature`, `Permission` entities in Domain layer |
| Add repository interfaces | `IRoleRepository`, `IFeatureRepository`, `IPermissionRepository` |
| Add EF Core configuration and migration | Entity configs, FK constraints, unique indexes, migration |
| Add repository implementations | Standard repo pattern matching existing codebase |
| Seed system roles and features in BaselineSeeder | Migrate `Roles.cs` constants → `Role` rows (`IsSystem = true`). Seed `Feature` rows from nav routes. Seed default `Permission` rows matching current hardcoded access |

### Branch 2: `feature/roles-api-endpoints`

**gRPC services for CRUD + permission queries** (depends on Branch 1)

| Commit | Scope |
|---|---|
| Add proto definitions | Request/response messages, service definitions for Role, Feature, Permission |
| Implement RoleService (CRUD) | Create, Update, Delete (block system roles), GetAll, GetByCtrlNbr. Can't delete `IsSystem`, can't create role with Level above caller's |
| Implement FeatureService (read-only) | GetAll (grouped by Category), GetByKey |
| Implement PermissionService | GetMatrix, GetEffectivePermissions (resolves parent overrides), UpdatePermission |

### Branch 3: `feature/roles-management-ui`

**Admin pages** (depends on Branch 2)

| Commit | Scope |
|---|---|
| Add BlazorUI clients | `RoleClient`, `FeatureClient`, `PermissionClient` |
| Add `/admin/roles` page | DataTable lister, modal CRUD, system role badges, Level display. SystemAdmin only |
| Add `/admin/permissions` page | Matrix grid — rows = features (grouped by category), columns = roles. Cell = dropdown (None/ReadOnly/FullAccess). Per-parent toggle for overrides |
| Add nav items and route guards | Wire into Administration section, restrict to admin roles |

### Branch 4: `feature/dynamic-authorization`

**Replace hardcoded checks** (depends on Branch 3)

| Commit | Scope |
|---|---|
| Add PermissionAuthorizationHandler (API) | Custom `IAuthorizationHandler` — resolves role → queries effective permissions → checks feature key |
| Add `[AuthorizeFeature]` attribute | `[AuthorizeFeature("daily/call-board")]` for declarative endpoint/page protection |
| Add UserPermissionService (BlazorUI) | Loads + caches effective permissions on login. Exposes `HasAccess(featureKey)`, `IsReadOnly(featureKey)` |
| Rewrite NavMenu to use permissions | Replace all `AuthorizeView Roles="..."` with permission lookups |
| Add permission enforcement to AppComponentBase | `IsReadOnly` property, `RequireFeature()` redirect. Pages declare their feature key |
| Update pages for ReadOnly mode | Hide create/edit/delete when `IsReadOnly`, disable form submissions |

### Dependency Chain

```
Branch 1 (domain) → Branch 2 (API) → Branch 3 (UI) → Branch 4 (enforcement)
```

Each PR is independently mergeable. After Branch 1, the app works identically (seeded data mirrors hardcoded behavior).
After Branch 3, admins can manage roles/permissions but old hardcoded checks still protect pages.
Branch 4 flips the switch to dynamic authorization.

---

## Open Items

- [ ] Decide if `Feature` needs a `RequiresRailroad` flag (some features are parent-scoped only)
- [ ] Decide if users can have multiple roles (current system: one role per user-parent-railroad tuple)
- [ ] Determine caching strategy for permissions (per-request vs session-scoped with invalidation)
