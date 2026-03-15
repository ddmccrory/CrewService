# Phase 7 — Boards & Bulletins

**Branch:** `feature/api-boards-bulletins`
**Depends on:** Phase 5 (crafts) + Phase 6 (crews/positions for vacancies)

## Why Seventh

Extra boards hold available employees for vacancy filling. When a crew position
vacates, a PositionVacancy is created, a Bulletin is posted, bids are collected
by seniority, and the position is awarded. This is the displacement engine.

## Domain Entities

| Entity | Source | Status |
|--------|--------|--------|
| `ExtraBoard` | `Modules/Boards/BoardEntities.cs` | ✅ |
| `BoardMember` | `Modules/Boards/BoardEntities.cs` | ✅ |
| `BoardCascadePolicy` | `Modules/Boards/BoardEntities.cs` | ✅ |
| `RosterBoard*` | `Modules/Boards/RosterBoardEntities.cs` | ✅ |
| `PositionVacancy` | `Modules/Bulletins/BulletinEntities.cs` | ✅ |
| `Bulletin` | `Modules/Bulletins/BulletinEntities.cs` | ✅ |
| `BulletinBid` | `Modules/Bulletins/BulletinEntities.cs` | ✅ |

## gRPC Services

| Service | Status |
|---------|--------|
| `BoardsService` | ✅ Exists — audit |
| `BulletinsService` | ✅ Exists — audit |
| `RosterBoardService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: extra board CRUD + board member management` | Create/Add/AdvanceState |
| 2 | `audit: board cascade policy RPCs` | Create/GetByWorkArea |
| 3 | `audit: position vacancy lifecycle RPCs` | Create/MarkBulletined/Fill/Abolish |
| 4 | `audit: bulletin + bid lifecycle RPCs` | Post/Close/Award/ForceAssign/Complete/Cancel + bid CRUD |
| 5 | `audit: roster board operations` | Board positions, daily status |
| 6 | `fix: fill missing RPCs` | Wire stubs |
| 7 | `test: vacancy → bulletin → bid → award flow` | End-to-end bulletin lifecycle |

## Railroad Setup Story

> CraftManager creates ExtraBoard "Yard Extra" for the Engineer craft,
> placed in the "Yard" group. Adds BoardMembers in seniority order.
> Sets BoardCascadePolicy for how vacancies cascade up the group hierarchy.
> When a CrewPosition vacates, a PositionVacancy is created, a Bulletin is
> posted with a bid window, bids are ranked by seniority, and the winner
> is awarded the position.
