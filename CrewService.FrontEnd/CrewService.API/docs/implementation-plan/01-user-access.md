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

### 2. API: Add `ValidateInvitationToken` RPC

Add a new RPC to `invitation.proto` that the AcceptInvitation page calls on load
to verify a token before showing the registration form.

**Proto definition:**
```protobuf
rpc ValidateInvitationToken (ValidateInvitationTokenRequest) returns (ValidateInvitationTokenReply);

message ValidateInvitationTokenRequest {
  string token = 1;
}

message ValidateInvitationTokenReply {
  bool is_valid = 1;
  string email = 2;
  string role = 3;
  string parent_name = 4;
  string status = 5;           // "Pending", "Expired", "Accepted", "Revoked"
  bool user_already_exists = 6; // true if email already has an account
}
```

**Implementation in `InvitationService`:**
- Look up invitation by token (`IInvitationRepository.GetByTokenAsync`)
- If not found → `is_valid = false`, empty fields
- If found → populate all fields, `is_valid` = `invitation.IsValid()`
- `user_already_exists` check via `UserManager<ApplicationUser>`
- No authentication required — this endpoint is called by unauthenticated users
  clicking the invitation link

### 3. API: Server-side role authorization on invitation RPCs

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

### 4. API: Email service infrastructure

Deliver invitation links via email using **MailKit** and capture them locally
with **smtp4dev** during development.

#### 4a. `IInvitationEmailService` interface

```
CrewService.Application/Modules/UserAccess/IInvitationEmailService.cs
```

```csharp
public interface IInvitationEmailService
{
    Task SendInvitationAsync(string toEmail, string role, string parentName,
                             string acceptUrl, DateTime expiresUtc);
    Task SendReminderAsync(string toEmail, string role, string parentName,
                           string acceptUrl, DateTime expiresUtc);
}
```

#### 4b. `SmtpInvitationEmailService` implementation

```
CrewService.Infrastructure/Services/SmtpInvitationEmailService.cs
```

- Uses **MailKit** (`MailKit` + `MimeKit` NuGet packages) to send via SMTP
- Reads SMTP settings from `IOptions<SmtpSettings>`:
  ```csharp
  public class SmtpSettings
  {
      public string Host { get; set; } = "localhost";
      public int Port { get; set; } = 25;
      public string FromAddress { get; set; } = "noreply@crewservice.local";
      public string FromName { get; set; } = "CrewService";
      public bool UseSsl { get; set; } = false;
      // Username/Password omitted — not needed for smtp4dev
  }
  ```
- Builds a `MimeMessage` with an HTML body (see 4d below)
- Registered as `services.AddTransient<IInvitationEmailService, SmtpInvitationEmailService>()`

#### 4c. SMTP configuration in `appsettings.Development.json`

```json
"SmtpSettings": {
  "Host": "localhost",
  "Port": 25,
  "FromAddress": "noreply@crewservice.local",
  "FromName": "CrewService",
  "UseSsl": false
}
```

#### 4d. HTML email template

Simple, inline-styled HTML email body containing:
- CrewService header
- "You've been invited to join **{ParentName}** as a **{Role}**."
- A prominent **Accept Invitation** button linking to the AcceptInvitation URL
- Expiration notice: "This invitation expires on {ExpiresUtc:MMMM d, yyyy}."
- Plain-text fallback part for email clients that don't render HTML

#### 4e. Integration with `InvitationService`

- **CreateInvitation** RPC: after persisting the invitation, call
  `IInvitationEmailService.SendInvitationAsync(...)` with the generated
  accept URL (`{BaseUrl}/Account/AcceptInvitation?token={token}`)
- **ResendInvitation** RPC: call `IInvitationEmailService.SendReminderAsync(...)`
  with a fresh accept URL (token unchanged, email resent)
- Base URL read from configuration (`AppSettings:BaseUrl` or similar)

#### 4f. smtp4dev setup (development only)

[smtp4dev](https://github.com/rnwood/smtp4dev) is a local fake SMTP server
that captures outbound emails in a web UI — no emails leave the machine.

**Install (one-time):**
```bash
dotnet tool install -g Rnwood.Smtp4dev
```

**Run (before testing invitation flow):**
```bash
smtp4dev
```

- SMTP listener: `localhost:25` (receives MailKit messages)
- Web UI: `http://localhost:5000` (view captured emails, click links)

> **Note:** smtp4dev's default web UI port (5000) may conflict with Kestrel.
> If needed, start with `smtp4dev --urls http://localhost:5080` to use a
> different port.

### 5. UI: Create `InvitationsClient` gRPC client

Blazor gRPC client wrapping the `InvitationSrvc` proto. Methods:

- `CreateAsync(email, parentCtrlNbr, role, expirationDays?)`
- `GetByParentAsync(parentCtrlNbr)`
- `RevokeAsync(ctrlNbr)`
- `ResendAsync(ctrlNbr)`
- `ValidateTokenAsync(token)` — unauthenticated call (no auth header)

Follows the existing `BaseGrpcClient<T>` pattern. `ValidateTokenAsync`
uses the same no-auth pattern as `AuthClient.RegisterUserAsync`.

### 6. UI: Invitations list page (`admin/invitations`)

- Routed at `/admin/invitations`, gated to `SystemAdmin`, `ParentAdmin`, `RailroadAdmin`
- Filters to the parent selected in the context switcher
- Displays: email, role, status (badge), created date, expires date
- **Pending** rows show Resend and Revoke action buttons
- RailroadAdmin sees the list and can create non-admin invitations
- ParentAdmin/SystemAdmin see the list and can create any invitation

### 7. UI: Create Invitation form

- Inline or modal form on the Invitations list page
- Fields: Email (required), Role (dropdown — filtered by caller's role per the
  authorization matrix), Expiration Days (default 7, editable)
- Parent defaults from the context switcher selection (not editable — switch
  context to invite for a different parent)
- On success: invitation email is sent via SMTP, new invitation appears in
  the list with Pending status

### 8. UI: Refactor `Register.razor` → `AcceptInvitation.razor`

This is an invitation-only application — there is no public registration.

- Rename route from `/Account/Register` to `/Account/AcceptInvitation`
- Token comes from the URL query string (`?token=xyz`), populated automatically
  when the user clicks the link in the invitation email
- On load: call `ValidateInvitationToken` RPC, display email, role, and parent
  name (read-only)
- If token is invalid/expired: show error message with status reason, no
  password fields
- If `user_already_exists` is true: show message explaining the user already
  has an account and the new role assignment was applied — redirect to login
- If token is valid and user is new: show password + confirm password fields
- On submit: calls `AuthService.RegisterUser` → creates user → accepts
  invitation → creates `UserParentAssignment`
- On success: redirect to login page

The invitation link format (sent via email, captured by smtp4dev in dev):
```
https://localhost:7132/Account/AcceptInvitation?token=<base64-token>
```

### 9. End-to-end verification (with smtp4dev)

Full flow test using smtp4dev to capture invitation emails:

1. Start smtp4dev (`smtp4dev` or `smtp4dev --urls http://localhost:5080`)
2. Login as SystemAdmin
3. Select a parent in the context switcher
4. Navigate to Admin → Invitations
5. Create invitation for `jane@railroad.com` as `ParentAdmin`
6. Open smtp4dev web UI (`http://localhost:5000` or `:5080`)
7. Find the captured email → verify HTML content, role, parent name, expiration
8. Click the **Accept Invitation** link in the email
9. See email, role, and parent name displayed (read-only), set password
10. Submit → account created, invitation accepted, assignment created
11. Login as `jane@railroad.com` → context switcher shows the assigned parent

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
| Production SMTP configuration | Production readiness (TBD) | Replace smtp4dev settings with real SMTP provider (SendGrid, SES, etc.) |

---

## Onboarding Story

> A SystemAdmin logs in, selects "Acme Railroad" in the context switcher,
> and navigates to Admin → Invitations. She clicks "Create Invitation",
> enters `jane@railroad.com`, selects the `ParentAdmin` role, and leaves
> expiration at the default 7 days. The system sends an invitation email
> and the invitation appears in the list as Pending.
>
> In development, the email is captured by smtp4dev. The SystemAdmin opens
> the smtp4dev web UI and sees the HTML email with a prominent
> "Accept Invitation" button. In production, Jane would receive this email
> in her inbox.
>
> Jane clicks the Accept Invitation link. The AcceptInvitation page loads,
> validates her token, and displays her email, assigned role (ParentAdmin),
> and parent name (Acme Railroad) — all read-only. She sets a password,
> submits, and her account is created. The invitation is marked Accepted,
> and a `UserParentAssignment` links her to Acme Railroad as a ParentAdmin.
>
> Jane logs in and sees Acme Railroad in her context switcher. She can now
> proceed to Phase 2 — managing the parent and railroad structure.
