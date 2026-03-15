# Phase 13 — FRA Compliance

**Branch:** `feature/api-fra-compliance`
**Depends on:** Phase 5 (crafts) + Phase 8 (duty tours reference shift data)

## Domain Entities

| Entity | Source |
|--------|--------|
| `RegulatoryStandard` | `Modules/FraCompliance/RegulatoryStandard.cs` |
| `RegulatoryQualification` | `Modules/FraCompliance/RegulatoryQualification.cs` |
| `CraftRegulatoryQualification` | `Modules/FraCompliance/CraftRegulatoryQualification.cs` |
| `EmployeeCertification` | `Modules/FraCompliance/EmployeeCertification.cs` |
| `CertificationEligibilityCheck` | `Modules/FraCompliance/CertificationEligibilityCheck.cs` |
| `CertificationRevocationRecord` | `Modules/FraCompliance/CertificationRevocationRecord.cs` |
| `DrugAlcoholTestRecord` | `Modules/FraCompliance/DrugAlcoholTestRecord.cs` |
| `DrugAlcoholAction` | `Modules/FraCompliance/DrugAlcoholAction.cs` |
| `VoluntaryReferral` | `Modules/FraCompliance/VoluntaryReferral.cs` |
| `FraDutyTour` | `Modules/FraCompliance/FraDutyTour.cs` |
| `FraDutyTourSegment` | `Modules/FraCompliance/FraDutyTourSegment.cs` |
| `FraTransportationSegment` | `Modules/FraCompliance/FraTransportationSegment.cs` |
| `FraOtherServiceSegment` | `Modules/FraCompliance/FraOtherServiceSegment.cs` |
| `FraMonthlyAccumulator` | `Modules/FraCompliance/FraMonthlyAccumulator.cs` |
| `FraExcessServiceReport` | `Modules/FraCompliance/FraExcessServiceReport.cs` |

## gRPC Service

| Service | Status |
|---------|--------|
| `FraComplianceService` | ✅ Exists — audit (largest service) |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: regulatory standard + qualification RPCs` | System-level reference data |
| 2 | `audit: craft regulatory qualification RPCs` | Per-craft qual requirements |
| 3 | `audit: employee certification lifecycle RPCs` | Issue/Revoke/Renew/Check |
| 4 | `audit: drug & alcohol test + action + referral RPCs` | Test recording, actions |
| 5 | `audit: duty tour + segment RPCs` | Part 228 tracking |
| 6 | `audit: monthly accumulator + excess service RPCs` | HOS calculations |
| 7 | `fix: fill missing RPCs` | Wire stubs |
| 8 | `test: certification lifecycle` | Issue → eligibility check → revocation |
| 9 | `test: duty tour HOS calculations` | Tour → segments → accumulator |
