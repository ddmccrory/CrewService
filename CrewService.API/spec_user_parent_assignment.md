# SPEC-8 User-Parent Assignment and Role-Based Access

## Background

The system currently ties users to a single parent through the `Employee` entity (`Employee.ClientCtrlNbr`). This creates two limitations:

1. **Non-employee users cannot be associated with a parent.** Users who are supervisors, dispatchers, auditors, or other non-operational roles have no formal relationship to a parent unless an `Employee` record exists for them.
2. **A user cannot belong to multiple parents.** Holding companies and shared-service organizations need users (e.g., regional managers, corporate auditors) to operate across several parents.

This spec introduces a **`UserParentAssignment`** join entity that links an Identity `User` (by `UserId`) to a domain `Parent` (by `ParentCtrlNbr`) with a `Role` attribute, enabling multi-parent, role-aware user access without requiring an employee record.

## Requirements

### Must Have

- **[R-UPA-001] User-to-Parent assignment via join entity**: Any Identity user can be assigned to one or more Parents via a `UserParentAssignment` row. The user does not need an `Employee` record.
- **[R-UPA-002] Role per assignment**: Each assignment carries a `Role` string indicating the user's function for that parent (e.g., `Admin`, `Manager`, `Dispatcher`, `ReadOnly`).
- **[R-UPA-003] Unique constraint**: A `(UserId, ParentCtrlNbr)` pair must be unique — a user cannot be assigned to the same parent twice.
- **[R-UPA-004] Standard entity behavior**: `UserParentAssignment` extends `Entity`, inheriting `CtrlNbr` (PK), audit fields (`CreatedBy`, `ModifiedBy`), and soft-delete support (`IsDeleted`, `DeletedAt`, `DeletedBy`).
- **[R-UPA-005] CRUD via gRPC**: Full create, read (by ID / by user / by parent), update (role), and delete operations exposed through gRPC with REST transcoding.
- **[R-UPA-006] Domain events**: Assignment creation, role update, and deletion each raise a domain event for outbox/event processing.

### Should Have

- **[R-UPA-101] Role validation**: Application-layer validation that `Role` values belong to a known set. Initially enforced as non-empty string; a controlled vocabulary can be added later.
- **[R-UPA-102] JWT role enrichment**: Include the user's parent-specific roles in JWT claims during authentication for downstream authorization decisions.

### Could Have

- **[R-UPA-201] Role hierarchy/permissions matrix**: Define a permissions model where roles map to granular permissions (e.g., `Dispatcher` can mark-off employees, `ReadOnly` can only view).
- **[R-UPA-202] Effective-dated assignments**: Track when an assignment becomes active/inactive for audit and compliance.

### Won't Have (Now)

- **[R-UPA-301] Self-service role changes**: Users cannot modify their own assignments; this is admin-only.

## Method

### 1) Domain Entity: `UserParentAssignment`

**Location:** `CrewService.Domain.Models.UserAccess`

| Property | Type | Notes |
|---|---|---|
| `CtrlNbr` | `ControlNumber` (PK) | Standard entity PK (inherited from `Entity`) |
| `UserId` | `string` | Identity user ID (`AspNetUsers.Id`). Required, max 128. |
| `ParentCtrlNbr` | `ControlNumber` | FK ? `Parent.CtrlNbr`. Required. |
| `Role` | `string` | Role name for this assignment. Required, max 50. |
| Audit fields | inherited from `Entity` | `CreatedBy`, `ModifiedBy`, `DeletedBy`, `IsDeleted`, `DeletedAt` |

**Factory method:** `UserParentAssignment.Create(string userId, long parentCtrlNbr, string role)`
- Validates `userId` and `role` are non-empty.
- Raises `UserParentAssignmentCreatedDomainEvent`.

**Mutation:** `UpdateRole(string role)`
- Updates role if changed.
- Raises `UserParentAssignmentUpdatedDomainEvent`.

**Deletion:** `Delete()`
- Raises `UserParentAssignmentDeletedDomainEvent`.
- Soft-delete is handled by the repository's `Remove()` method.

### 2) Domain Events

**Location:** `CrewService.Domain.DomainEvents.UserAccess`

| Event | Payload |
|---|---|
| `UserParentAssignmentCreatedDomainEvent` | `AggregateCtrlNbr` |
| `UserParentAssignmentUpdatedDomainEvent` | `AggregateCtrlNbr`, `Changes` (role) |
| `UserParentAssignmentDeletedDomainEvent` | `AggregateCtrlNbr`, `DeletedAt` |

### 3) Repository Interface

**Location:** `CrewService.Domain.Modules.UserAccess`

```csharp
public interface IUserParentAssignmentRepository : IRepository<UserParentAssignment>
{
    Task<List<UserParentAssignment>> GetByUserIdAsync(string userId);
    Task<List<UserParentAssignment>> GetByParentCtrlNbrAsync(long parentCtrlNbr);
    Task<UserParentAssignment?> GetByUserAndParentAsync(string userId, long parentCtrlNbr);
}
```

### 4) Persistence: EF Configuration

**Location:** `CrewService.Persistance.Configurations`

- PK on `CtrlNbr` with `ControlNumber` value conversion
- `ParentCtrlNbr` with `ControlNumber` value conversion
- Unique composite index on `(UserId, ParentCtrlNbr)`
- Individual indexes on `UserId` and `ParentCtrlNbr`
- Standard audit owned types (`CreatedBy`, `ModifiedBy`, `DeletedBy`)

### 5) DB Schema Addition

```
UserParentAssignments:
  CtrlNbr            BIGINT PK
  UserId             TEXT(128) NOT NULL
  ParentCtrlNbr      BIGINT   NOT NULL
  Role               TEXT(50) NOT NULL
  -- audit fields (CreatedBy, ModifiedBy, DeletedBy, IsDeleted, DeletedAt)

Indexes:
  IX_UserParentAssignments_UserId                     (UserId)
  IX_UserParentAssignments_ParentCtrlNbr              (ParentCtrlNbr)
  IX_UserParentAssignments_UserId_ParentCtrlNbr       UNIQUE (UserId, ParentCtrlNbr)
```

**Migration:** `20260226192247_AddUserParentAssignments`

### 6) gRPC API Surface

**Proto:** `Protos/user_parent_assignment.proto`
**Service:** `UserParentAssignmentSrvc`

| RPC | HTTP | Description |
|---|---|---|
| `GetAssignmentAsync` | `GET /user-parent-assignment/{ctrlNbr}` | Get a single assignment by CtrlNbr |
| `GetAssignmentsByUserAsync` | `GET /user-parent-assignment/user/{userId}` | All assignments for a user |
| `GetAssignmentsByParentAsync` | `GET /user-parent-assignment/parent/{parentCtrlNbr}` | All assignments for a parent |
| `CreateAssignmentAsync` | `POST /user-parent-assignment` | Assign a user to a parent with a role |
| `UpdateAssignmentRoleAsync` | `PUT /user-parent-assignment` | Change the role on an existing assignment |
| `DeleteAssignmentAsync` | `DELETE /user-parent-assignment/{ctrlNbr}` | Soft-delete an assignment |

#### Messages

```proto
message GetAssignmentResponse {
  int64 ctrlNbr = 1;
  string userId = 2;
  int64 parentCtrlNbr = 3;
  string role = 4;
}

message CreateAssignmentRequest {
  string userId = 1;
  int64 parentCtrlNbr = 2;
  string role = 3;
}

message UpdateAssignmentRoleRequest {
  int64 ctrlNbr = 1;
  string role = 2;
}
```

### 7) Validation & Conflict Rules

| Rule | Enforcement |
|---|---|
| `UserId` non-empty | Service layer + domain entity (`ArgumentException.ThrowIfNullOrEmpty`) |
| `Role` non-empty | Service layer + domain entity |
| `ParentCtrlNbr > 0` | Service layer |
| Duplicate `(UserId, ParentCtrlNbr)` | Service layer check + unique DB index |

### 8) Suggested Role Vocabulary

The `Role` field is a free-form string (max 50 chars) for flexibility. The following initial vocabulary is recommended:

| Role | Description |
|---|---|
| `Admin` | Full access to parent configuration, user management, and all operations |
| `Manager` | Can manage employees, crews, and operational settings |
| `Dispatcher` | Can perform dispatch, mark-off, and board management operations |
| `Payroll` | Can access payroll and time entry functions |
| `ReadOnly` | View-only access across all parent data |

Role enforcement should be implemented via authorization policies that inspect the user's assignments for the current parent context.

## Relationship to Existing Entities

- **`User` (Identity)** — Unchanged. `User.PrimaryRoleId` remains for backward compatibility. `UserParentAssignment` provides the authoritative, per-parent role mapping.
- **`Employee`** — Unchanged. `Employee.UserId` and `Employee.ClientCtrlNbr` continue to link employees to their identity and parent. `UserParentAssignment` operates independently — a user can have an assignment without an employee record.
- **`Parent`** — Unchanged. `Parent.Railroads` navigation still works. User assignments are queried via the repository, not a navigation property on `Parent`.

## Integration Notes

### JWT Claims Enrichment (Future)
When a user authenticates, `AuthService.GenerateJwtToken` should be updated to:
1. Query `IUserParentAssignmentRepository.GetByUserIdAsync(userId)`.
2. Include parent-specific role claims (e.g., `parent:{ctrlNbr}:role:{role}`).
3. Authorization policies can then inspect these claims for per-parent access decisions.

### Query Patterns

| Query | Approach |
|---|---|
| "Which parents can this user access?" | `GetByUserIdAsync(userId)` |
| "Who has access to this parent?" | `GetByParentCtrlNbrAsync(parentCtrlNbr)` |
| "What role does this user have for this parent?" | `GetByUserAndParentAsync(userId, parentCtrlNbr)` |
| "Is this user an admin anywhere?" | `GetByUserIdAsync(userId)` then filter `Role == "Admin"` |
