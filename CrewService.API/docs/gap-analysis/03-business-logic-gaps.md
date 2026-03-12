# 03 – Business Logic Gaps

## Overview

The legacy SA system embeds complex, pool-specific business logic directly in entity methods and utility classes.
CrewService has the structural modules but is missing the **core algorithmic engines** that drive daily operations.

This document categorizes the missing business logic by domain area.

---

## 1. Daily Call Sheet Generation Pipeline

**SA Location**: `SADailyCallSheetService` (6 sub-services) + `Global.asax` timers  
**CrewService Module**: WorkManagement (partially), Dispatching (partially)  
**Status**: 🔴 Core pipeline not implemented

### What SA Does

A 6-stage MSMQ pipeline creates daily operational records:

1. **Calculate next shift** — circular shift sequencing (1→2→3→1), date increment on wrap
2. **Create `DailyAssignmentShift`** — container for one shift's worth of work
3. **Create `DailyAssignment`** — per-assignment records filtered by effective dates, on-duty days, shift, and abolishment status
4. **Create `DailyCrewPosition`** — per-position records within each assignment
5. **Create `DailyCrewPositionOnDutyRecord`** — place assigned employees on duty (see §2)
6. **Link mark-off records** — connect existing mark-offs to the new on-duty records

### What CrewService Has

- `AssignmentTemplate` with `RecurrenceJson` — can express *when* work runs
- `WorkInstance` with status lifecycle — can represent a generated day's work
- No concept of **shift-level containers** or the **multi-stage generation pipeline**
- No circular shift sequencing logic
- No per-position daily record generation

### Gap Items (branch: `feature/gap-daily-operations`)

| Commit Scope | Description |
|-------------|-------------|
| Domain: `ShiftInstance` entity | Shift-level container linking a WorkInstance to a specific shift period |
| Domain: `PositionSlotInstance` entity | Daily materialized position slot with status flags (annulled, DoNotFill, skipped, moved) |
| Application: `CallSheetGenerationService` | Orchestrates the generation pipeline: template → work instance → shift → position slots |
| Application: shift sequencing logic | Circular shift advancement with date-wrap rules (configurable, not hard-coded to 3 shifts) |
| Application: assignment filtering | Effective date, on-duty day, abolishment, shift matching filters |
| Worker: `DailyCallSheetWorker` | `BackgroundService` replacing the SA timer + MSMQ pipeline |

---

## 2. On-Duty / Off-Duty Lifecycle

**SA Location**: `DailyCrewPosition.CreateDailyCrewPositionOnDutyRecord()`, `CreateDailyCrewPositionOffDutyRecord()`  
**CrewService Module**: Dispatching (`EmployeeBooking`), WorkManagement  
**Status**: 🔴 Not implemented

### What SA Does (On-Duty — 17 steps)

1. Create on-duty record with position/employee binding
2. Resolve assigned vs. unassigned position status
3. Handle late calls (+90 min adjustment)
4. Calculate previous rest hours/minutes from last off-duty
5. Set job code (pool-specific format: varies by pool 10/20/30/40/50/60)
6. Determine payroll earning code (pool-specific overtime/ST rules — 20+ condition branches)
7. Calculate consecutive days (reset if rest ≥ 24h)
8. Calculate ST days worked / total days worked (pay-period boundary resets on day 1 and 16)
9. Create AFE billing record
10. Update employee previous-rest information
11. If position annulled → auto-create off-duty record
12. FRA checks: rest-for-next-on-duty, unavailable record creation, NN mark-off
13. Pool 40 special: delete conflicting next-shift on-duty records
14. Update mark-off linkage

### What SA Does (Off-Duty — craft-specific)

- Craft-specific rest calculations (Clerical, Yardmaster, Engineer/Yardman, default)
- Consecutive-day rested datetime
- Post-save: previous rest update, FRA compliance check, hangout notification, FIFO board reposition, payroll tier update, Teams notification

### What CrewService Has

- `EmployeeBooking` — simple start/end booking with optional position slot link
- No on-duty/off-duty state machine, no rest calculations, no FRA integration

### Gap Items (branch: `feature/gap-daily-operations`)

| Commit Scope | Description |
|-------------|-------------|
| Domain: `OnDutyRecord` entity | Core on-duty tracking: employee, position, times, earning code, consecutive days, rest |
| Domain: `OffDutyRecord` entity | Tie-up record: off-duty time, rest time, rested/available datetimes, release reason |
| Domain: `OnDutyStatus` value object | Status booleans: IsCalled, IsOnDuty, IsTiedUp, IsOpen, IsRestricted, etc. |
| Application: on-duty placement service | The 17-step workflow as an application service with craft-aware strategy |
| Application: off-duty (tie-up) service | Craft-specific rest calculation with strategy pattern replacing pool-number switch |
| Application: payroll earning code resolver | Pool/craft-specific earning code determination (extract from hard-coded branches) |

---

## 3. Vacancy Assignment Algorithm

**SA Location**: `ApplicationUtilities.UpdateDailyCrewPositionVacancies()`  
**CrewService Module**: Dispatching (partially), Boards (partially), AbsenceVacancy (partially)  
**Status**: 🔴 Core algorithm not implemented

### What SA Does

Two-phase algorithm running per roster (or per pool for MoW):

**Phase 1 — No-Bid Bulletin Handling**: Force-assign junior employees to no-bid positions before general vacancy processing.

**Phase 2 — Extra Board Assignment**: While vacancies remain, iterate extra board members in TieUpOrder/BoardOrder, applying:
- 12-day worked cap
- Already on-duty / already-assigned skip
- Availability check (available by end of calling window)
- Rest check (rested by end of calling window)
- Mark-off / mark-up time check
- Qualification check (`IsQualified` against position requirements + effective date)
- Foreman vacancy special: helper search with swap logic and cross-assignment eligible helper check (hard-coded location search order: 11, 13, 14)

### What CrewService Has

- `DispatchProjection` — projection record but no algorithm to compute it
- `DispatchDecisionLog` — audit trail entity but no decision engine
- `BoardMember` with `OrderIndex` and `StateJson` — board state but no consumption logic
- `VacancyImpact` — links absence to position but no fill logic
- `BoardCascadePolicy` — cascade config but no cascade resolution algorithm

### Gap Items (branch: `feature/gap-vacancy-assignment`)

| Commit Scope | Description |
|-------------|-------------|
| Application: `VacancyResolutionEngine` | Core vacancy-fill loop: query open positions, rank XB members, apply skip rules, assign |
| Application: qualification checker | `IsQualified(employee, position, asOfDate)` against requirements with effective dates |
| Application: no-bid bulletin handler | Pre-phase force-assignment by reverse seniority |
| Application: helper search logic | Foreman swap + cross-assignment eligible-helper search (configurable, not hard-coded locations) |
| Application: board cascade resolver | `ResolveBoards(workArea, craft, kind)` implementing `BoardCascadePolicy` |
| Domain: concurrency guard | Per-work-area processing lock replacing `PoolInProgress` dictionary |

---

## 4. FRA Hours-of-Service Compliance

**SA Location**: `FRARequirements` static class, `DailyCrewPositionOnDutyFRARecord`  
**CrewService Module**: None  
**Status**: 🔴 Not implemented  
**Full CFR cross-reference**: See [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md)

### What SA Does

Enforces 49 CFR Part 228 for crafts where `Craft.HoursofService == true`:

- **Constants**: MaxHours=12, RestHours=10, ConsecutiveDays=6, ConsecutiveDayHours=24
- **Rest calculation**: Base 10h + penalty rest for hours beyond 12h on duty
- **Consecutive day tracking**: ≥6 consecutive days → mandatory "SR" (Safety Rest) mark-off
- **Rest-for-next check**: If `RestedDateTime > nextOnDutyDateTime` → "NR" (Not Rested) mark-off
- **Not-Notified check**: Open notifications + not rested by calling time → "NN" mark-off
- **FRA form entity**: Per-on-duty compliance record with covered service time, commingled time, deadhead time, certification
- **Auto-generated mark-offs**: SR, NR, NN codes created automatically with Teams notification

### What SA Is Missing (per CFR review)

- **Two-tier consecutive day rest** (48h after 6 days, 72h after 7 days) — SA only uses 6-day threshold
- **276h monthly cumulative cap** — not tracked
- **30h/month deadhead-after-12h cap** — not tracked
- **Interim release (broken/aggregate service)** — not modeled
- **Per-segment deadhead and other-service tracking** — single records only
- **Quick tie-up workflow** (mandatory when ≤3 min from 12h max)
- **Prior time off reconciliation** (system vs. employee-reported)
- **Excess service reason capture** (required when TTOD >12h)
- **6 of 10 reportable violation types** not detected

### What CrewService Has

- Nothing directly. No FRA entities, no rest calculator, no compliance checks.
- However, `EmployeeBooking` (Dispatching module) tracks start/end UTC per employee — a foundation for duty tour tracking.
- The `Craft` entity exists and can carry an FRA employee-type classification.
- The outbox / domain-event infrastructure can drive auto mark-off side effects.

### Gap Items (branch: `feature/gap-fra-compliance`)

| Commit Scope | Description |
|-------------|-------------|
| Domain: `FraDutyTour` + segments | Root duty tour entity with covered-service, interim-release, transportation, and other-service segments |
| Domain: `FraExcessServiceReport` | Reportable violation record covering all 10 CFR §228.19 violation types |
| Domain: `FraMonthlyAccumulator` | Per-employee monthly running totals (276h cap, 30h deadhead cap) |
| Domain: `ConsecutiveDayState` VO | Two-tier tracking (6→48h, 7→72h) with home-terminal awareness |
| Domain: `RestRequirement` VO | Computed rest hours (base 10h + penalty), rested datetime, 8h-in-24h check |
| Application: `FraDutyTourCalculator` | Calculate TTOD from segments including commingled and deadhead |
| Application: `FraRestValidator` | 10h post-tour, 8h-in-24h, penalty rest validation |
| Application: `FraConsecutiveDayTracker` | Two-tier consecutive day enforcement with 48h/72h rest |
| Application: `FraMonthlyCapTracker` | 276h monthly cap + 30h deadhead-after-12h monthly cap |
| Application: `FraExcessServiceDetector` | Detect all 10 reportable violations per §228.19 |
| Application: `FraQuickTieUpService` | Emergency tie-up workflow when at/within 3 min of max |
| Application: `FraPriorTimeOffReconciler` | System vs. employee prior-rest conflict resolution |
| Application: craft-awareness | Configurable per craft via `FraEmployeeType` (not just hard-coded Engineer/Yardman) |
| Presentation: FRA record search | Query by 7 CFR-mandated criteria (§228.203(d)) |

---

## 5. Mark-Off / Mark-Up System

**SA Location**: `MarkOffRecord`, `MarkUpRecord`, `MarkOffCode`, `MarkOffRequestRecord`, + 10 related entities  
**CrewService Module**: AbsenceVacancy (`AbsenceRequest`)  
**Status**: ⚠️ Heavily simplified — missing most business logic

### What SA Does

- **20+ mark-off codes** with per-code behavior: approval required, excused, compensated, auto-markup, holiday-exempt, system-only
- **Craft-specific overrides**: `CraftMarkOffCode` can override auto-markup hours per craft
- **Auto-markup**: Hard-coded durations (V1=168h, V2=336h, VD/CD/PD/SD=24h) with craft override
- **Request workflow**: Mark-off requests → approval → wait-list → fulfillment, with vacation week alignment logic (Pool 10 uses Jan 1 day-of-week alignment)
- **On-duty impact**: Mark-off during active on-duty creates off-duty record, triggers payroll review, creates manual tie-up notification
- **Extra board impact**: Marked-off XB employees pushed to back of board (`TieUpOrder + 10 years`)
- **Compensation balance tracking**: Running balance of compensated hours; auto-remove unused requests when balance ≤ 0
- **Interface file generation**: Creates files for external system sync on add/change/delete
- **Concurrency**: Waits for `CallSheetInProgress` to clear before modifying XB positions

### What CrewService Has

- `AbsenceRequest` — has status lifecycle (PENDING→APPROVED→DENIED→CANCELLED→COMPLETED) and reason code
- `VacancyImpact` — links absence to a position slot with impact window
- Missing: code reference data, auto-markup engine, request-to-mark-off linking, compensation tracking, XB board manipulation

### Gap Items (branch: `feature/gap-mark-off-system`)

| Commit Scope | Description |
|-------------|-------------|
| Domain: `AbsenceCode` reference entity | Code, description, flags (excused, compensated, auto-markup, system-only, approval, holiday-exempt) |
| Domain: `AbsenceCodeCraftOverride` | Per-craft override of auto-markup hours |
| Domain: `AbsenceApproval` entity | Approval officer assignment, approval/decline tracking |
| Domain: `AbsenceMarkUp` entity | Mark-up datetime, markup buffer hours |
| Application: auto-markup engine | Duration calculation with craft override, auto mark-up scheduling |
| Application: request-to-absence linking | Vacation week alignment, date matching, wait-list processing |
| Application: compensation balance tracker | Running balance per employee per compensation type |
| Application: board-impact side effects | XB repositioning on mark-off/mark-up, roster board timer reset |

---

## 6. Payroll Engine

**SA Location**: `PayrollRecord`, `PayrollEarningRecord`, `PayrollUtilities`, `ProcessPayrollController`  
**CrewService Module**: Payroll (`TimeEntry`, `PayrollRun`, `PayrollRecord`)  
**Status**: ⚠️ Structural entities exist but all calculation/approval logic missing

### What SA Does

- **Earning code determination**: 20+ condition branches based on pool, off-day, worked-double, holiday, same-shift, unassigned status
- **Job code formatting**: Pool-specific format (pools 30/60 vs. 50 vs. default use different `{PositionCode}{AssignmentNumber}` ordering)
- **Three-tier approval routing**: Default officer → payroll-code-specific officer → position-based officer, with 12+ earning codes routed to code-specific roles
- **Self-approval prevention**: Officer matching check
- **Pay rate calculation**: Position pay rates, engineer-specific pay rates, payroll tier percentages
- **Period processing**: Trial and final payroll processing per semi-monthly period (1st–15th, 16th–end)
- **Holiday payroll**: Qualification rules, holiday earning record generation
- **ADP/UKG export/import**: CSV generation for payroll systems, paid-amount import from file watchers
- **PDF generation**: iText-based payroll reports and earning statements

### What CrewService Has

- `TimeEntry` — hours by type with adjustment support
- `PayrollRun` — period lifecycle (DRAFT→LOCKED) with versioning
- `PayrollRecord` — earnings type + amount + hours
- Missing: earning code logic, approval routing, pay rate calculation, period processing, export/import

### Gap Items (branch: `feature/gap-payroll-engine`)

| Commit Scope | Description |
|-------------|-------------|
| Domain: `EarningCode` reference entity | Code, description, overtime flag, approval required, compensation type |
| Domain: `PayRate` / `PositionPayRate` entities | Rate tables, craft-specific rates, engineer rates, tier percentages |
| Domain: `EarningApproval` entity | Required/completed/declined approval with officer resolution |
| Application: earning code resolver | Craft/policy-driven code determination (replacing pool-number switches) |
| Application: approval routing service | Three-tier officer resolution with role-based lookup |
| Application: period processing service | Trial/final payroll run, semi-monthly boundary logic |
| Application: payroll export service | CSV generation for ADP/UKG formats |

---

## 7. Single-Railroad Rules → Multi-Tenant Configuration Strategy

### The Problem

SA was built for **one specific railroad**. Its 122 `PoolNumber.Equals(` call sites hard-code that railroad's operating rules as application logic. Each pool has unique rules for:

- Job code format
- Payroll earning code determination
- Vacancy assignment behavior
- Extra board scheduling (40h/week cap for pools 20, 30)
- Call sheet timing
- Rest calculations (Pool 50/MoW skips meal deduction for overtime)
- Bulletin processing
- Mark-off handling

These rules are correct for the original railroad, but they are **configuration, not generalizable logic**. CrewService does not need to reproduce these specific branches — it needs to **support the same kinds of rules** as tenant-configurable behavior.

### CrewService Approach

Replace hard-coded single-railroad branching with **configurable craft-level and work-area-level policies**. The original railroad's rules become seed data for that tenant:

| SA Hard-Code (one railroad) | CrewService Configurable Policy |
|---------------|------------------------|
| `PoolNumber == 10` (Yard/Engine) | Craft policies on Engineer/Yardman crafts |
| `PoolNumber == 20` (Yardmaster) | Craft policy with 40h/week availability rule |
| `PoolNumber == 30` (Clerical) | Craft policy with 40h/week availability rule |
| `PoolNumber == 40` (Mechanical) | Craft policy with shift-overlap delete rule |
| `PoolNumber == 50` (MoW) | Craft policy with pool-level (not roster-level) vacancy + no meal deduction on OT |
| `PoolNumber == 60` (Patrolmen) | Craft policy similar to Clerical |

Each policy is a **configuration record in the database**, not a code branch. The `GroupAttributeDefinition` / `GroupAttributeValue` system in TenantConfig can store these per work-area group.

---

## Cross-References

- Domain entities needed: [01-domain-entity-gaps.md](01-domain-entity-gaps.md)
- Background workers to host this logic: [02-automated-process-gaps.md](02-automated-process-gaps.md)
- External integrations triggered by this logic: [04-integration-gaps.md](04-integration-gaps.md)
- SA concept → module mapping: [05-module-mapping.md](05-module-mapping.md)
- Full 49 CFR Part 228 requirements for §4: [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md)
