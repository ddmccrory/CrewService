# Phase 1 — User Access (Invitation Management, End-to-End)

**Branch:** `0.1.2/phase-01-user-access`
**Depends on:** Phase 00B (Context Switcher — provides parent/railroad selection)

## Why First

Before anything else exists, a SystemAdmin invites a ParentAdmin. That ParentAdmin
then bootstraps everything downstream. Without user access, no other API call is
authorized. This phase delivers the complete invitation lifecycle — from creation
through acceptance — across both the API and the Blazor UI.

---

## Existing Infrastructure (Complete — No Changes Needed)

These components were built in earlier work and are fully functional.

### Domain Entities

| Entity | Location | Status |
|--------|----------|--------|
| `Invitation` | `Models/UserAccess/Invitation.cs` | ✅ Complete — Create, Accept, MarkExpired, Revoke, IsValid, GenerateToken |
| `UserParentAssignment` | `Models/UserAccess/UserParentAssignment.cs` | ✅ Complete — Create, UpdateRole, Delete |
| `Roles` | `Models/UserAccess/Roles.cs` | ✅ Complete — SystemAdmin (global), 7 per-parent roles |
| `InvitationStatus` | `Models/UserAccess/InvitationStatus.cs` | ✅ Complete — Pending, Accepted, Expired, Revoked |

### Repositories

| Interface | Location | Status |
|-----------|----------|--------|
| `IInvitationRepository` | `Modules/UserAccess/IUserAccessRepositories.cs` | ✅ Complete — GetByTokenAsync, GetByEmailAsync, GetByParentCtrlNbrAsync, GetPendingByEmailAndParentAsync |
| `IUserParentAssignmentRepository` | `Modules/UserAccess/IUserAccessRepositories.cs` | ✅ Complete — GetByUserIdAsync, GetByParentCtrlNbrAsync, GetByUserAndParentAsync |

### gRPC Proto & Services

| Proto / Service | RPCs | Status |
|-----------------|------|--------|
| `invitation.proto` / `InvitationService` | CreateInvitation, GetInvitation, GetInvitationsByParent, GetInvitationsByEmail, RevokeInvitation, ResendInvitation | ✅ All implemented |
| `user_parent_assignment.proto` / `UserParentAssignmentService` | Get, GetByUser, GetByParent, Create, UpdateRole, Delete | ✅ All implemented |
| `auth.proto` / `AuthService.RegisterUser` | Token-based registration: validates invitation → creates user → accepts invitation → creates UserParentAssignment | ✅ Implemented |

### Unit Tests

| File | Coverage | Status |
|------|----------|--------|
| `InvitationTests.cs` | Create, Accept, Revoke, MarkExpired, validation, domain events | ✅ Exists |
| `UserParentAssignmentTests.cs` | Create, UpdateRole, Delete, repository queries, domain events | ✅ Exists |

### EF Configuration & Migrations

| Component | Status |
|-----------|--------|
| `InvitationConfiguration.cs` | ✅ Complete |
| `UserParentAssignmentConfiguration.cs` | ✅ Complete |
| `AddInvitations` migration | ✅ Applied |
| `AddUserParentAssignments` migration | ✅ Applied |

---

## Phase 1 Work Items

### 1. API: Add `expiration_days` to `CreateInvitationRequest` proto

The `Invitation.Create` domain method already accepts `expirationDays` (default 7),
but the proto doesn't expose it. Add an optional `int32 expiration_days` field so
the UI can pass a custom value.

> **Note:** This default is hardcoded for now. Phase 3 (Tenant Configuration) will
> introduce `ParentSettings` where `InvitationExpirationDays` becomes a configurable
> parent-level setting.

### 2. API: Server-side role authorization on invitation RPCs

Enforce the following permission matrix in `InvitationService`:

| Caller Role | Can Create For | Can View | Can Revoke/Resend |
|---|---|---|---|
| **SystemAdmin** | Any per-parent role | All parents | All |
| **ParentAdmin** | ParentAdmin, RailroadAdmin, CraftManager, CrewManager, Dispatcher, PayrollClerk, ReadOnly | Own parent | Own parent |
| **RailroadAdmin** | CraftManager, CrewManager, Dispatcher, PayrollClerk, ReadOnly | Own parent | Own parent |
| **Below RailroadAdmin** | None | No access | No access |

> **Note:** Non-admin role invitations (Dispatcher, PayrollClerk, etc.) will
> eventually be auto-created by a qualification/employee-entry process in Phase 4.
> For now, admins can create them manually.

### 3. UI: Create `InvitationsClient` gRPC client

Blazor gRPC client wrapping the `InvitationSrvc` proto. Methods:

- `CreateAsync(email, parentCtrlNbr, role, expirationDays?)`
- `GetByParentAsync(parentCtrlNbr)`
- `RevokeAsync(ctrlNbr)`
- `ResendAsync(ctrlNbr)`

Follows the existing `BaseGrpcClient<T>` pattern.

### 4. UI: Invitations list page (`admin/invitations`)

- Routed at `/admin/invitations`, gated to `SystemAdmin`, `ParentAdmin`, `RailroadAdmin`
- Filters to the parent selected in the context switcher
- Displays: email, role, status (badge), created date, expires date
- **Pending** rows show Resend and Revoke action buttons
- **Development only:** Token is visible and copyable (for testing the accept flow)
- RailroadAdmin sees the list and can create non-admin invitations
- ParentAdmin/SystemAdmin see the list and can create any invitation

### 5. UI: Create Invitation form

- Inline or modal form on the Invitations list page
- Fields: Email (required), Role (dropdown — filtered by caller's role per the
  authorization matrix), Expiration Days (default 7, editable)
- Parent defaults from the context switcher selection (not editable — switch
  context to invite for a different parent)
- On success: new invitation appears in the list with Pending status

### 6. UI: Refactor `Register.razor` → `AcceptInvitation.razor`

This is an invitation-only application — there is no public registration.

- Rename route from `/Account/Register` to `/Account/AcceptInvitation`
- Token comes from the URL query string (`?token=xyz`), not a paste field
- On load: validate token via API, display email and role (read-only)
- If token is invalid/expired: show error message, no password fields
- If token is valid: show password + confirm password fields
- On submit: calls `AuthService.RegisterUser` → creates user → accepts
  invitation → creates `UserParentAssignment`
- On success: redirect to login page

The invitation link format (used in dev, eventually sent via email):
```
https://localhost:7132/Account/AcceptInvitation?token=<base64-token>
```

### 7. End-to-end verification

Full flow test:
1. Login as SystemAdmin
2. Select a parent in the context switcher
3. Navigate to Admin → Invitations
4. Create invitation for `jane@railroad.com` as `ParentAdmin`
5. Copy the invitation link (visible in dev)
6. Open in incognito / log out
7. Navigate to the invitation link
8. See email + role displayed, set password
9. Submit → account created, invitation accepted, assignment created
10. Login as `jane@railroad.com` → context switcher shows the assigned parent

---

## Authorization Matrix — NavMenu

The "Invitations" menu item under the Admin group remains gated to:
`SystemAdmin, ParentAdmin, RailroadAdmin`

All three roles can view the list and create invitations. The Create form's
role dropdown is filtered server-side based on the caller's role.

---

## Deferred to Future Phases

| Item | Target Phase | Notes |
|------|-------------|-------|
| Settings infrastructure (`ParentSettings`) | Phase 3 — Tenant Configuration | `InvitationExpirationDays` becomes a parent-level setting; hardcoded default of 7 replaced |
| UserParentAssignment management UI | Employees area (TBD) | View/edit/remove role assignments after acceptance |
| Auto-invitation on employee entry | Phase 4 — Employee Foundation | Qualification process controls invitation creation for non-admin roles |
| Email delivery of invitation links | Production readiness (TBD) | Phase 1 uses copy-to-clipboard in dev; production sends email |

---

## Onboarding Story

> A SystemAdmin logs in, selects "Acme Railroad" in the context switcher,
> and navigates to Admin → Invitations. She clicks "Create Invitation",
> enters `jane@railroad.com`, selects the `ParentAdmin` role, and leaves
> expiration at the default 7 days. The invitation appears in the list as
> Pending.
>
> Jane receives the invitation link (copied from the dev UI for now). She
> opens it in her browser and sees her email and role displayed. She sets a
> password, submits, and her account is created. The invitation is marked
> Accepted, and a `UserParentAssignment` links her to Acme Railroad as a
> ParentAdmin.
>
> Jane logs in and sees Acme Railroad in her context switcher. She can now
> proceed to Phase 2 — managing the parent and railroad structure.
