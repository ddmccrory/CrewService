# Impl Spec: PTRA Seed Data

## Tenant: Port Terminal Railroad Association

| Field | Value |
|-------|-------|
| Parent Name | Port Terminal Railroad Association |
| Railroad Mark | PTRA |
| Railroad Name | Port Terminal Railroad Association |

This document defines the seed data rows that reproduce the original railroad's
operating rules as tenant configuration. Each section maps SA's hard-coded logic
to configurable entities defined in the implementation specs.

---

## 1. Parent and Railroad

| Entity | Field | Value |
|--------|-------|-------|
| Parent | Name | Port Terminal Railroad Association |
| Railroad | Mark | PTRA |
| Railroad | Name | Port Terminal Railroad Association |
| Railroad | ParentCtrlNbr | → Parent above |

---

## 2. Work Areas (SA Pools → DynamicGroups)

| SA Pool | PoolNumber | DynamicGroup Name | GroupType | IsWorkArea |
|---------|-----------|-------------------|-----------|------------|
| Yard/Engine | 10 | PTRA Yard/Engine | WorkArea | true |
| Yardmaster | 20 | PTRA Yardmaster | WorkArea | true |
| Clerical | 30 | PTRA Clerical | WorkArea | true |
| Mechanical | 40 | PTRA Mechanical | WorkArea | true |
| MoW | 50 | PTRA Maintenance of Way | WorkArea | true |
| Patrolmen | 60 | PTRA Patrolmen | WorkArea | true |

---

## 3. Crafts

| SA Craft | Craft Name | HoursOfService |
|----------|-----------|----------------|
| Engineer | Engineer | true |
| Yardman | Yardman | true |
| Yardmaster | Yardmaster | false |
| Clerical | Clerical | false |
| Mechanical | Mechanical | false |
| MoW | Maintenance of Way | false |
| Patrolmen | Patrolmen | false |

---

## 4. CraftOperationsPolicy (from B02)

| Craft | LateCallThresholdMin | RestStrategy | FixedRestHrs | ConsecDayResetHrs | DeleteConflictingShift | AutoAnnulOffDuty |
|-------|---------------------|-------------|-------------|-------------------|----------------------|-----------------|
| Engineer | 90 | FRA | null | 24 | false | true |
| Yardman | 90 | FRA | null | 24 | false | true |
| Yardmaster | 90 | FixedHours | 8.0 | 24 | false | true |
| Clerical | 90 | FixedHours | 8.0 | 24 | false | true |
| Mechanical | 90 | FixedHours | 8.0 | 24 | true | true |
| MoW | 90 | FixedHours | 8.0 | 24 | false | true |
| Patrolmen | 90 | FixedHours | 8.0 | 24 | false | true |

**Notes**: SA `DeleteConflictingNextShift` is true only for pool 40 (Mechanical).

---

## 5. CraftOperationsPolicy — vacancy/board fields

| Craft | WeeklyHoursCap | VacancyScope | BoardSortStrategy | HelperSearchEnabled |
|-------|---------------|-------------|-------------------|-------------------|
| Engineer | null | Roster | TieUpFirst | true |
| Yardman | null | Roster | TieUpFirst | true |
| Yardmaster | 40 | Roster | BoardOrderFirst | false |
| Clerical | 40 | Roster | BoardOrderFirst | false |
| Mechanical | null | Roster | BoardOrderFirst | false |
| MoW | null | WorkArea | BoardOrderFirst | false |
| Patrolmen | 40 | Roster | BoardOrderFirst | false |

**Notes**: SA pools 20, 30, 60 have 40h/week cap. Pool 50 (MoW) uses WorkArea-level vacancy scope.

---

## 6. RegulatoryStandard — System-Level (NOT per-tenant)

FRA limits are federal law — seeded once, shared by all parents and railroads. These
rows are created in `gap-fra-compliance` Commit 1, not as part of PTRA tenant seeding.

| Code | Description | MaxDuty | MinRest | 8hIn24h | ConsecDay6 | ConsecDay7 | Rest6d | Rest7d | MoCap | DH12hCap | Wreck |
|------|------------|---------|---------|---------|-----------|-----------|--------|--------|-------|---------|-------|
| CFR-228-TRAIN | 49 CFR Part 228 — Train | 720 | 600 | true | 6 | 7 | 2880 | 4320 | 16560 | 1800 | 240 |
| CFR-228-SIGNAL | 49 CFR Part 228 — Signal | 720 | 600 | false | 6 | 7 | 2880 | 4320 | 16560 | 1800 | 240 |
| CFR-228-DISPATCH | 49 CFR Part 228 — Dispatching | 720 | 600 | false | 6 | 7 | 2880 | 4320 | 16560 | 1800 | 240 |

**PTRA-specific**: The only per-tenant decision is which crafts are covered:

| Craft | IsHoursOfServiceCovered | RegulatoryStandard |
|-------|------------------------|-------------------|
| Engineer | true | CFR-228-TRAIN |
| Yardman | true | CFR-228-TRAIN |
| Yardmaster | false | null |
| Clerical | false | null |
| Mechanical | false | null |
| MoW | false | null |
| Patrolmen | false | null |

### RegulatoryQualification — System-Level (NOT per-tenant)

FRA craft certifications are also federal law. Seeded once, shared by all tenants.

| Code | CFR Part | Description |
|------|---------|-------------|
| CFR-240-ENGINEER | 49 CFR Part 240 | Locomotive Engineer Certification |
| CFR-242-CONDUCTOR | 49 CFR Part 242 | Conductor Certification |
| CFR-242-SWITCHMAN | 49 CFR Part 242 | Switchman Certification |

**PTRA-specific**: Which crafts require which qualifications:

| Craft | Required Qualification |
|-------|-----------------------|
| Engineer | CFR-240-ENGINEER |
| Yardman | CFR-242-CONDUCTOR |

---

## 7. ShiftDefinitions (from B02)

| Work Area | ShiftCode | DisplayName | DefaultStart | DefaultEnd | Order |
|-----------|----------|-------------|-------------|-----------|-------|
| PTRA Yard/Engine | 1 | First Shift | 07:00 | 15:00 | 1 |
| PTRA Yard/Engine | 2 | Second Shift | 15:00 | 23:00 | 2 |
| PTRA Yard/Engine | 3 | Third Shift | 23:00 | 07:00 | 3 |
| PTRA Yardmaster | 1 | Day Shift | 07:00 | 15:00 | 1 |
| PTRA Yardmaster | 2 | Afternoon Shift | 15:00 | 23:00 | 2 |
| PTRA Yardmaster | 3 | Night Shift | 23:00 | 07:00 | 3 |
| PTRA Clerical | 1 | Day Shift | 07:00 | 15:00 | 1 |
| PTRA Clerical | 2 | Afternoon Shift | 15:00 | 23:00 | 2 |
| PTRA Mechanical | 1 | Day Shift | 07:00 | 15:00 | 1 |
| PTRA Mechanical | 2 | Afternoon Shift | 15:00 | 23:00 | 2 |
| PTRA MoW | 1 | Day Shift | 07:00 | 15:00 | 1 |
| PTRA Patrolmen | 1 | Day Shift | 07:00 | 15:00 | 1 |

**Notes**: Exact shift times should be validated against SA data. 3-shift coverage is typical for 24/7 operations (Yard/Engine, Yardmaster).

---

## 8. AbsenceCodes (from B03)

Core mark-off codes from SA's `MarkOffCode` table. System-only codes (SR, NR, NN) are created by FRA compliance.

| Code | Description | Excused | Compensated | Approval | SystemOnly | AutoMarkUpHrs |
|------|------------|---------|-------------|----------|-----------|--------------|
| V1 | Vacation Week 1 | true | true | true | false | 168 |
| V2 | Vacation Week 2 | true | true | true | false | 168 |
| PD | Personal Day | true | true | true | false | 24 |
| CD | Company Day | true | false | false | false | 24 |
| SD | Sick Day | true | false | false | false | 24 |
| SR | Safety Rest (FRA) | true | false | false | true | 48 |
| NR | Not Rested (FRA) | true | false | false | true | null |
| NN | Not Notified (FRA) | true | false | false | true | null |
| AW | AWOL | false | false | false | false | null |
| SU | Suspended | false | false | false | false | null |
| FD | Funeral Day | true | true | true | false | 24 |
| JD | Jury Duty | true | true | false | false | null |

**Notes**: Full code list should be validated against SA `MarkOffCode` table. AutoMarkUpHrs for SR is set to 48h per CFR 6-day rest requirement.

---

## 9. EarningCodeRules (from B05)

Representative rules extracted from SA's `GetPayrollEarningCode()` branches. Priority order — first match wins.

| Priority | IsOffDay | IsHoliday | IsWorkedDouble | IsUnassigned | CraftFilter | ResultCode |
|----------|---------|-----------|---------------|-------------|-------------|-----------|
| 1 | true | true | null | null | null | HO |
| 2 | true | false | true | null | null | OT-DBL |
| 3 | true | false | false | null | null | OT |
| 4 | false | true | null | null | null | HP |
| 5 | false | false | null | true | null | UA |
| 6 | false | false | null | false | null | ST |

**Notes**: This is a simplified representation. The full rule set has 20+ rows and must be extracted from SA source. Each work area gets its own rule set.

---

## 10. Seeding Implementation

### Approach

Use EF Core `HasData()` in a dedicated `PtraSeedConfiguration` class per entity type,
applied conditionally via a `--seed ptra` flag or environment variable.

### Seeding Order (respects FK dependencies)

**System-level (all tenants — seeded once):**
1. `RegulatoryStandard` — CFR-228-TRAIN, CFR-228-SIGNAL, CFR-228-DISPATCH
2. `RegulatoryQualification` — CFR-240-ENGINEER, CFR-242-CONDUCTOR, CFR-242-SWITCHMAN

**PTRA tenant-level:**
3. `Parent` — Port Terminal Railroad Association
4. `Railroad` — PTRA (→ Parent)
5. `DynamicGroup` / `GroupType` — 6 work areas (→ Railroad)
6. `Craft` — 7 crafts with `IsHoursOfServiceCovered` + `RegulatoryStandardCtrlNbr`
7. `CraftRegulatoryQualification` — Engineer → CFR-240, Yardman → CFR-242
8. `RailroadGroupPlacement` — crafts → work areas
9. `CraftOperationsPolicy` — per craft (→ Craft)
10. `ShiftDefinition` — per work area (→ DynamicGroup)
11. `AbsenceCode` — 12+ codes
12. `AbsenceCodeCraftOverride` — craft-specific overrides (→ AbsenceCode, Craft)
13. `EarningCodeRule` — per work area (→ DynamicGroup)

### Data Extraction

Rows marked "validate against SA" require extraction from the live SA database.
The SA `StrategicApplicationsContext` can be queried to produce exact seed values.
