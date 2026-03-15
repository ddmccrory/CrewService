# Phase 12 — Notifications & Electronic Calling

**Branch:** `feature/api-notifications`
**Depends on:** Phase 8 (shift instances trigger notifications)

## Domain Entities

| Entity | Source |
|--------|--------|
| `NotificationEntities` | `Modules/Notifications/NotificationEntities.cs` |
| `NotificationProviderConfig` | `Modules/Notifications/NotificationProviderConfig.cs` |
| `TeamsWebhookConfig` | `Modules/TenantConfig/TeamsWebhookConfig.cs` |

## Application Layer

| Component | Source |
|-----------|--------|
| `ICrewNotificationProvider` | `Application/ElectronicCalling/` |
| Provider implementations | `Application/ElectronicCalling/Providers/` |

## gRPC Services

| Service | Status |
|---------|--------|
| `ElectronicCallingService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: notification provider config RPCs` | CRUD for provider setup |
| 2 | `audit: notification request/response RPCs` | Send/Query notifications |
| 3 | `audit: teams webhook config RPCs` | CRUD per work area |
| 4 | `fix: fill missing RPCs` | Wire stubs |
| 5 | `test: notification lifecycle` | Configure → send → track response |
