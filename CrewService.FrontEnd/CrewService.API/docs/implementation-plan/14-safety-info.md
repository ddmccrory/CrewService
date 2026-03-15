# Phase 14 — Safety & Railroad Information (Independent)

**Branch:** `feature/api-safety-info`
**Depends on:** Phase 2 (railroad exists — otherwise independent)

## Domain Entities — Safety

| Entity | Source |
|--------|--------|
| `SafetyObservation` | `Modules/Safety/SafetyObservation.cs` |
| `SafetyObservationResolution` | `Modules/Safety/SafetyObservationResolution.cs` |
| `SafetyReferenceData` | `Modules/Safety/SafetyReferenceData.cs` |

## Domain Entities — Railroad Information

| Entity | Source |
|--------|--------|
| `RailroadInformation` | `Modules/RailroadInfo/RailroadInformation.cs` |
| `RailroadInformationReadReceipt` | `Modules/RailroadInfo/RailroadInformationReadReceipt.cs` |

## gRPC Services

| Service | Status |
|---------|--------|
| `SafetyService` | ✅ Exists — audit |
| `RailroadInfoService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: safety reference data RPCs` | Categories, types |
| 2 | `audit: safety observation + resolution RPCs` | Report/Resolve/Query |
| 3 | `audit: railroad information CRUD + read receipt RPCs` | Post/Read/Acknowledge |
| 4 | `fix: fill missing RPCs` | Wire stubs |
| 5 | `test: safety observation lifecycle` | Report → resolve |
| 6 | `test: railroad information distribution` | Post → read receipt |
