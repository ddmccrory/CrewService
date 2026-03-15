# Phase 3 — Tenant Configuration (Dynamic Groups)

**Branch:** `feature/api-tenant-config`
**Depends on:** Phase 2 (Railroad must exist to place in groups)

## Why Third

Dynamic groups define the organizational hierarchy — divisions, subdivisions,
work areas. A Railroad is placed into groups. Work areas are where crews,
crafts, shifts, and boards are assigned. This is the structural backbone.

## Domain Entities

| Entity | Location | Status |
|--------|----------|--------|
| `GroupType` | `Modules/TenantConfig/GroupType.cs` | ✅ Complete |
| `DynamicGroup` | `Modules/TenantConfig/DynamicGroup.cs` | ✅ Complete |
| `GroupAttributeDefinition` | `Modules/TenantConfig/GroupAttributeDefinition.cs` | ✅ Complete |
| `GroupAttributeValue` | `Modules/TenantConfig/GroupAttributeValue.cs` | ✅ Complete |
| `RailroadGroupPlacement` | `Modules/TenantConfig/RailroadGroupPlacement.cs` | ✅ Complete |
| `TeamsWebhookConfig` | `Modules/TenantConfig/TeamsWebhookConfig.cs` | ✅ Complete |

## Repositories

| Interface | Location | Status |
|-----------|----------|--------|
| `IGroupTypeRepository` | `Modules/TenantConfig/ITenantConfigRepositories.cs` | ✅ Defined |
| `IDynamicGroupRepository` | `Modules/TenantConfig/ITenantConfigRepositories.cs` | ✅ Defined |
| `IGroupAttributeDefinitionRepository` | `Modules/TenantConfig/ITenantConfigRepositories.cs` | ✅ Defined |
| `IGroupAttributeValueRepository` | `Modules/TenantConfig/ITenantConfigRepositories.cs` | ✅ Defined |
| `IRailroadGroupPlacementRepository` | `Modules/TenantConfig/ITenantConfigRepositories.cs` | ✅ Defined |

## gRPC Service

| Service | Location | Status |
|---------|----------|--------|
| `TenantConfigService` | `Presentation/Services/Modules/TenantConfigService.cs` | ✅ Exists — audit all RPCs |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: verify GroupType CRUD RPCs` | Create/Get/GetAll/Update/Delete |
| 2 | `audit: verify DynamicGroup CRUD + tree queries` | Create/Get/GetAll/GetByType/GetChildren/Update/Delete |
| 3 | `audit: verify attribute definition + value RPCs` | CRUD for both, GetByGroup |
| 4 | `audit: verify RailroadGroupPlacement RPCs` | Place/Remove/GetByRailroad/GetByGroup |
| 5 | `fix: fill missing RPCs` | Wire stubs |
| 6 | `test: group hierarchy and placement tests` | Build tree → place railroad → query |

## Railroad Setup Story

> Jane creates GroupTypes: "Division", "Subdivision", "Work Area" (isWorkArea=true).
> She creates DynamicGroups: Division "Gulf" → Subdivision "Tampa" → Work Area "Yard".
> She places Railroad "PTRA" into the "Yard" work area via RailroadGroupPlacement.
> Optionally she adds attributes (e.g., timezone, milepost range) to groups.
