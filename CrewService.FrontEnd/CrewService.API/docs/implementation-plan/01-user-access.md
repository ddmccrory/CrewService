# Phase 1 — User Access (Invitation + UserParentAssignment)

**Branch:** `feature/api-user-access`
**Depends on:** Nothing (this is the entry point for onboarding)

## Why First

Before anything else exists, a SystemAdmin invites a ParentAdmin. That ParentAdmin
then bootstraps everything downstream. Without user access, no other API call is
authorized.

## Domain Entities

| Entity | Location | Status |
|--------|----------|--------|
| `Invitation` | `Models/UserAccess/Invitation.cs` | ✅ Complete |
| `UserParentAssignment` | `Models/UserAccess/UserParentAssignment.cs` | ✅ Complete |
| `Roles` | `Models/UserAccess/Roles.cs` | ✅ Complete |
| `InvitationStatus` | `Models/UserAccess/InvitationStatus.cs` | ✅ Complete |

## Repositories

| Interface | Location | Status |
|-----------|----------|--------|
| `IInvitationRepository` | `Modules/UserAccess/IUserAccessRepositories.cs` | ✅ Defined |
| `IUserParentAssignmentRepository` | `Modules/UserAccess/IUserAccessRepositories.cs` | ✅ Defined |

## gRPC Services

| Service | Location | Status |
|---------|----------|--------|
| `InvitationService` | `Presentation/Services/InvitationService.cs` | ✅ Exists — audit RPCs |
| `UserParentAssignmentService` | `Presentation/Services/UserParentAssignmentService.cs` | ✅ Exists — audit RPCs |
| `AuthService` | `Presentation/Services/AuthService.cs` | ✅ Exists — audit RPCs |
| `AccountService` | `Presentation/Services/AccountService.cs` | ✅ Exists — audit RPCs |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: verify invitation proto covers create/accept/revoke/expire/list` | Compare proto RPCs to `Invitation` domain methods |
| 2 | `audit: verify UserParentAssignment proto covers create/update/delete/list` | Compare proto RPCs to domain methods |
| 3 | `fix: fill any missing RPC implementations` | Wire up any stub RPCs found in audit |
| 4 | `test: add unit tests for invitation lifecycle` | Pending/Accept/Expire/Revoke flows |
| 5 | `test: add unit tests for user-parent assignment CRUD` | Create/UpdateRole/Delete |

## Railroad Setup Story

> A SystemAdmin creates an `Invitation` for `email=jane@railroad.com`,
> `role=ParentAdmin`, targeting a `ParentCtrlNbr`. Jane receives a token,
> calls `AcceptInvitation`, and a `UserParentAssignment` is created linking
> her userId to the Parent with the ParentAdmin role. She can now proceed
> to Phase 2.
