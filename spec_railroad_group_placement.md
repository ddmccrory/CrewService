# SPEC-7 Railroad Placement in Dynamic Group Hierarchy

## Background

The platform's organizational structure is driven by tenant-defined Dynamic Groups. Railroads are first-class domain entities with identity attributes (`RailroadMark`, `Name`) and serve as the operational boundary for employee participation, availability, and board eligibility.

Today, `Railroad.ParentCtrlNbr` ties each railroad directly to a `Parent` — a flat, one-level relationship. This is sufficient for simple tenants (one parent, one railroad) but cannot represent holding companies with railroads organized under regions, divisions, or other intermediate organizational layers.

This spec introduces a **join table** (`RailroadGroupPlacement`) that positions a railroad entity as a **leaf attachment** on any node in the Dynamic Group tree, enabling arbitrary organizational depth without changing the railroad's domain identity.

## Requirements

### Must Have

- **[R-RGP-001] Railroad leaf attachment via join table**: A railroad can be placed under any `DynamicGroup` node via a `RailroadGroupPlacement` row. The railroad is always a leaf — it does not sprout its own subtree. The group node it attaches to **can** have child groups (subdivisions, work areas, etc.).
- **[R-RGP-002] Parent isolation preserved**: `Railroad.ParentCtrlNbr` remains required and is the hard tenant-isolation boundary. `RailroadGroupPlacement` is purely organizational/navigational.
- **[R-RGP-003] No placement = direct child of Parent**: If a railroad has no `RailroadGroupPlacement` rows, it is treated as a direct child of the Parent in tree rendering and queries. This preserves backward compatibility.
- **[R-RGP-004] Arbitrary nesting depth**: The group node a railroad attaches to can exist at any depth in the Dynamic Group tree (Parent ? Division ? Region ? Railroad attachment).
- **[R-RGP-005] Railroad as WorkArea (simple setup)**: In the simplest tenant configuration, a railroad's group node can itself be marked `IsWorkArea = true`. The railroad is both the railroad and the work area. Employees reference this group as their `HomeWorkArea` with `RailroadId` set to the same railroad.
- **[R-RGP-006] Railroad group can have child groups**: A group node that has a railroad attached is a normal interior node — it can have child groups (subdivisions, terminals, work areas). "Leaf" describes the **railroad entity's role** (it is an attachment, not a tree node), not the group node.
- **[R-RGP-007] Multiple placements allowed**: A railroad may appear under more than one group (e.g., a shared-track railroad relevant to two regions). Each placement is a separate row in `RailroadGroupPlacement`.

### Should Have

- **[R-RGP-101] GroupType validation**: Optionally validate that `RailroadGroupPlacement.GroupCtrlNbr` references a group whose `GroupType` is appropriate for railroad attachment (e.g., `"Railroad"`, `"Operating Company"`). This is a business rule enforced in the application layer, not a schema constraint.

### Won't Have (Now)

- **[R-RGP-301] Effective-dated placements**: Railroad organizational moves are infrequent (corporate restructuring). Soft-delete + audit fields provide the trail. Effective dating can be added later if needed.

## Method

### 1) Domain Entity: `RailroadGroupPlacement`

**Location:** `CrewService.Domain.Modules.TenantConfig`

A simple entity linking a `Railroad` (by `CtrlNbr`) to a `DynamicGroup` (by `CtrlNbr`).

| Property | Type | Notes |
|---|---|---|
| `CtrlNbr` | `ControlNumber` (PK) | Standard entity PK |
| `RailroadCtrlNbr` | `ControlNumber` | FK ? `Railroad.CtrlNbr`. Required. |
| `GroupCtrlNbr` | `ControlNumber` | FK ? `DynamicGroup.CtrlNbr`. Required. |
| Audit fields | inherited from `Entity` | `CreatedBy`, `ModifiedBy`, `DeletedBy` |

**Invariants:**
- A `(RailroadCtrlNbr, GroupCtrlNbr)` pair must be unique (no duplicate placements).
- `GroupCtrlNbr` must reference an existing, non-deleted `DynamicGroup`.
- `RailroadCtrlNbr` must reference an existing, non-deleted `Railroad`.

### 2) Persistence: EF Configuration

**Location:** `CrewService.Persistance.Modules.TenantConfig`

- PK on `CtrlNbr`
- Unique index on `(RailroadCtrlNbr, GroupCtrlNbr)` to prevent duplicate placements
- Index on `GroupCtrlNbr` for "find all railroads under a group" queries
- Index on `RailroadCtrlNbr` for "find all placements for a railroad" queries
- Standard `ControlNumber` value conversions and audit owned types

### 3) Tree Rendering Impact

When building the organizational tree for UI or queries:

1. Load the `DynamicGroup` tree as today.
2. Join `RailroadGroupPlacement` to attach railroad entities to their group nodes.
3. Railroads with **no placement rows** render as direct children of the Parent (root level).
4. Railroads are always leaf decorations — they appear as children of their group node but have no children of their own.

**Example trees:**

**Simple (railroad = work area):**
```
Parent: "Acme Shortline"
 ??? DynamicGroup: "Acme Railroad" (GroupType="Railroad", IsWorkArea=true)
       ? Railroad "ACME" attached via RailroadGroupPlacement
```

**Holding company with subdivisions:**
```
Parent: "Continental Rail Holdings"
 ??? DynamicGroup: "Northeast Region" (GroupType="Region")
 ?    ??? DynamicGroup: "NE Freight" (GroupType="Railroad", IsWorkArea=false)
 ?          ? Railroad "NEF" attached via RailroadGroupPlacement
 ?          ??? DynamicGroup: "Hudson Subdivision" (GroupType="Subdivision")
 ?          ?    ??? DynamicGroup: "Albany Terminal" (IsWorkArea=true)
 ?          ?    ??? DynamicGroup: "Poughkeepsie Yard" (IsWorkArea=true)
 ?          ??? DynamicGroup: "Mohawk Subdivision" (GroupType="Subdivision")
 ?               ??? DynamicGroup: "Syracuse Terminal" (IsWorkArea=true)
 ??? DynamicGroup: "Southeast Region" (GroupType="Region")
      ??? DynamicGroup: "SE Passenger" (GroupType="Railroad", IsWorkArea=false)
            ? Railroad "SEP" attached via RailroadGroupPlacement
            ??? DynamicGroup: "Atlanta Terminal" (IsWorkArea=true)
```

### 4) Query Patterns

| Query | Approach |
|---|---|
| "All railroads for this parent" | Existing: `Railroad` where `ParentCtrlNbr = X` |
| "All railroads under a group subtree" | Join `RailroadGroupPlacement` ? `DynamicGroup.Path LIKE '{groupPath}%'` |
| "Which group is this railroad in?" | `RailroadGroupPlacement` where `RailroadCtrlNbr = X` |
| "All work areas for a railroad" | Find railroad's group via placement, then descendants where `IsWorkArea = true` |
| "Railroads with no group (direct children of Parent)" | `Railroad` LEFT JOIN `RailroadGroupPlacement` WHERE placement is NULL |

### 5) Integration with EmployeeGroupMembership

No changes to `EmployeeGroupMembership` (SPEC-3). The existing design already separates:
- `GroupId` ? the `DynamicGroup` node (which may or may not have a railroad attached)
- `RailroadId` ? the `Railroad` entity (optional, for Home Railroad affinity)

In the "railroad is the work area" scenario:
- `GroupId` ? the group node (has `IsWorkArea=true` and a railroad attached via placement)
- `RailroadId` ? the same railroad from the placement

### 6) gRPC API Surface

Add to the existing `TenantConfigSrvc` in `tenant_config.proto`:

```proto
  // Railroad Group Placements
  rpc PlaceRailroadInGroup(PlaceRailroadInGroupRequest) returns (RailroadGroupPlacementResponse);
  rpc RemoveRailroadFromGroup(RemoveRailroadFromGroupRequest) returns (common.DeleteResponse);
  rpc GetRailroadPlacements(GetRailroadPlacementsRequest) returns (GetRailroadPlacementsResponse);
  rpc GetRailroadsInGroup(GetRailroadsInGroupRequest) returns (GetRailroadsInGroupResponse);
```

#### Messages

```proto
message PlaceRailroadInGroupRequest {
  int64 railroad_ctrl_nbr = 1;
  int64 group_ctrl_nbr = 2;
}

message RailroadGroupPlacementResponse {
  int64 ctrl_nbr = 1;
  int64 railroad_ctrl_nbr = 2;
  int64 group_ctrl_nbr = 3;
}

message RemoveRailroadFromGroupRequest {
  int64 ctrl_nbr = 1;  // placement CtrlNbr
}

message GetRailroadPlacementsRequest {
  int64 railroad_ctrl_nbr = 1;
}

message GetRailroadPlacementsResponse {
  repeated RailroadGroupPlacementResponse placements = 1;
}

message GetRailroadsInGroupRequest {
  int64 group_ctrl_nbr = 1;
  bool include_descendants = 2;  // walk subtree
}

message GetRailroadsInGroupResponse {
  repeated RailroadGroupPlacementResponse placements = 1;
}
```

### 7) Domain Events

| Event | Payload |
|---|---|
| `RailroadPlacedInGroupDomainEvent` | `RailroadCtrlNbr`, `GroupCtrlNbr`, `PlacementCtrlNbr` |
| `RailroadRemovedFromGroupDomainEvent` | `RailroadCtrlNbr`, `GroupCtrlNbr`, `PlacementCtrlNbr` |

### 8) DB Schema Addition (OperationsDbContext — TenantConfig)

```sql
railroad_group_placement:
  ctrl_nbr          BIGINT PK
  railroad_ctrl_nbr BIGINT FK ? railroad(ctrl_nbr)    NOT NULL
  group_ctrl_nbr    BIGINT FK ? dynamic_group(ctrl_nbr) NOT NULL
  -- audit fields (created_by, modified_by, deleted_by)

Indexes:
  UQ_RGP_Railroad_Group  UNIQUE (railroad_ctrl_nbr, group_ctrl_nbr)
  IX_RGP_Group           (group_ctrl_nbr)
  IX_RGP_Railroad        (railroad_ctrl_nbr)
```

## Relationship to Existing Entities

- **`Railroad`** — unchanged. `ParentCtrlNbr` remains required for tenant isolation. Railroad keeps its domain attributes (`RailroadMark`, `Name`). No FK to `DynamicGroup` is added to the entity itself.
- **`DynamicGroup`** — unchanged. Group nodes that have railroads attached behave identically to other groups; they can be interior nodes with children.
- **`Parent`** — unchanged. The `Parent.Railroads` navigation still works for flat listing. Tree-aware queries use `RailroadGroupPlacement`.
- **`EmployeeGroupMembership`** — unchanged. References `GroupId` and optional `RailroadId` independently.

## Migration Path

1. Create `RailroadGroupPlacement` entity, configuration, and repository in the TenantConfig module.
2. Add the EF migration for the new table + indexes.
3. Add gRPC endpoints to `TenantConfigSrvc`.
4. Update tree-rendering queries to include railroad attachments.
5. Existing railroads with no placements continue to work as direct children of the Parent — no data migration required.
