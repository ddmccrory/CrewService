# 06 – FRA Compliance Requirements (49 CFR Part 228)

## Source

**49 CFR Part 228** — Hours of Service Recordkeeping and Reporting (2010 Edition)

This document cross-references the **federal regulatory requirements** against both the
**legacy SA implementation** and the **CrewService gap analysis** to ensure nothing is missed.

---

## 1. Employee Classification (§228.5 Definitions)

The CFR defines **three distinct employee types** with different rules:

| Employee Type | CFR Definition | SA Equivalent | SA Coverage | CrewService Status |
|--------------|----------------|---------------|-------------|-------------------|
| **Train employee** | Individual engaged in or connected with the movement of a train, including hostlers | Engineer, Yardman (Pools 10, 20) | ✅ `Craft.HoursofService == true` | 🔴 Not modeled |
| **Signal employee** | Individual engaged in installing, repairing, or maintaining signal systems | Not present in SA | ❌ Not applicable | N/A |
| **Dispatching service employee** | Individual who dispatches, reports, transmits, receives, or delivers train movement orders | Not present in SA | ❌ Not applicable | N/A |

### Gap Finding

SA only tracks train employees. The CFR also defines:
- **Commingled service** — non-covered service not separated by a qualifying off-duty period (different definitions per employee type)
- **Limbo time** — time treated as neither on-duty nor off-duty
- **Deadhead transportation** — travel to/from duty assignments (counts as on-duty TO assignment, does NOT count returning)

**CrewService must model**: Employee type classification per craft to apply the correct rule set.

---

## 2. Hours Limitations — Train Employees (§228.7(a), Appendix A)

These are the **hard statutory limits** that the system must enforce:

| Rule | CFR Source | Limit | SA Implementation | SA Correct? | CrewService |
|------|-----------|-------|-------------------|-------------|-------------|
| **Max consecutive on-duty** | §228.7(a), App A | **12 hours** | `FRARequirements.MaxHours = 12` | ✅ | 🔴 |
| **Min rest after full tour** | App A | **10 consecutive hours** off duty | `FRARequirements.RestHours = 10` | ✅ | 🔴 |
| **Min rest in preceding 24h** | §228.7(a) | **8 consecutive hours** off duty within preceding 24h (before going on duty) | Not explicitly tracked as separate check | ⚠️ Partial | 🔴 |
| **Max consecutive days (6 days, home terminal end)** | §228.19(b)(6) | Cannot initiate on-duty on **7th consecutive day** if 6th ended at home terminal (unless CBA allows) | `FRARequirements.ConsecutiveDays = 6` → auto "SR" mark-off | ⚠️ See below | 🔴 |
| **Mandatory rest after 6 consecutive days** | §228.19(b)(7) | **48 consecutive hours** off at home terminal | SA creates "SR" mark-off but does not enforce 48h minimum | ⚠️ Gap | 🔴 |
| **Max consecutive days (absolute)** | §228.19(b)(8) | Cannot initiate on-duty on **more than 7 consecutive days** (no CBA exception) | SA uses `ConsecutiveDays = 6` as hard stop; 7-day rule not separately tracked | ⚠️ Gap | 🔴 |
| **Mandatory rest after 7 consecutive days** | §228.19(b)(9) | **72 consecutive hours** off at home terminal | Not implemented in SA | 🔴 Gap | 🔴 |
| **Monthly cap** | §228.19(b)(5) | **276 hours** cumulative (covered service + deadhead + other service) per calendar month | Not tracked in SA | 🔴 Gap | 🔴 |
| **Excess deadhead after 12h** | §228.19(b)(10)(iv) | **30 hours** per calendar month of time awaiting/in deadhead after 12 consecutive hours on duty | Not tracked in SA | 🔴 Gap | 🔴 |
| **Penalty rest for excess service** | §228.19(b)(4) | Additional off-duty time equal to the amount by which (TTOD + deadhead to release) exceeds 12h | SA adds penalty rest: `RestTime = 10h + (hours_over_12)` | ✅ | 🔴 |
| **Wreck/relief exception** | App A | Up to **16 hours** (4 extra) during actual emergency | Not implemented in SA | 🔴 Gap | 🔴 |

### Key Gap Findings

1. **SA uses `ConsecutiveDays = 6` as a single threshold** but the CFR actually has a **two-tier system**:
   - 6 consecutive days → 48h rest at home terminal (CBA may allow 7th day)
   - 7 consecutive days → 72h rest at home terminal (absolute limit, no exception)

2. **SA does not track the 276-hour monthly cumulative cap**. The CFR requires reporting any violation.

3. **SA does not track deadhead time separately**. The CFR counts deadhead TO an assignment as on-duty, but deadhead RETURNING is neither on-duty nor off-duty — and has its own 30h/month cap after 12h tours.

4. **The 8-hour-in-preceding-24h rule** is distinct from the 10h post-tour rest. SA's rest-for-next check partially covers this but doesn't explicitly validate the 24h lookback window.

---

## 3. Recordkeeping Requirements — Train Employees (§228.11(b))

Each hours-of-duty record **must** contain the following. These map to data fields CrewService must capture:

| # | CFR Requirement (§228.11(b)) | SA Field | SA Coverage | CrewService |
|---|------------------------------|----------|-------------|-------------|
| (1) | Employee identification (initials + surname) | `EmployeeNumber`, `EmployeeName` on FRA record | ✅ | 🔴 |
| (2) | Each covered service position in a duty tour | `AssignmentName` on FRA record | ✅ | 🔴 |
| (3) | Amount of time off duty prior to going on duty | `PreviousRestHours`, `PreviousRestMinutes` | ✅ | 🔴 |
| (4) | Location, date, time reported for first assignment | `OnDutyLocation`, `OnDutyDateTime` on FRA record | ✅ | 🔴 |
| (5) | Location, date, time released from each assignment preceding an interim release | Not explicitly separated from (7) | ⚠️ Partial | 🔴 |
| (6) | Location, date, time reporting after an interim release | Not tracked (no interim release concept) | 🔴 Gap | 🔴 |
| (7) | Location, date, time released from last assignment; also if >12h and has interim release | `OffDutyLocation`, `OffDutyDateTime` on FRA record | ✅ | 🔴 |
| (8) | Beginning/ending location, date, time for **transportation periods** (deadhead) with mode | Not tracked as separate periods | 🔴 Gap | 🔴 |
| (9) | Beginning/ending location, date, time for **other service at behest of railroad** | Not tracked | 🔴 Gap | 🔴 |
| (10) | Identification code of service type for other service | Not tracked | 🔴 Gap | 🔴 |
| (11) | **Total time on duty** for the duty tour | `CoveredServiceTime` on FRA record | ✅ | 🔴 |
| (12) | **Reason** for any service exceeding 12h TTOD | Not captured | 🔴 Gap | 🔴 |
| (13) | Total amount of time by which (TTOD + deadhead to release) exceeds 12h | Not calculated | 🔴 Gap | 🔴 |
| (14) | **Cumulative monthly totals** of: (i) covered service, (ii) deadhead from duty to release, (iii) other service | `MonthlyCoveredServiceTime` exists; others missing | ⚠️ Partial | 🔴 |
| (15) | Cumulative monthly total of deadhead after 12h consecutive | Not tracked | 🔴 Gap | 🔴 |
| (16) | **Number of consecutive days** in which an on-duty period was initiated | `ConsecutiveDays` on on-duty record | ✅ | 🔴 |

### Key Gap Findings

1. **Interim release** — The CFR supports **broken (aggregate) service**: a duty tour interrupted by a qualifying interim release (≥4h at a designated terminal). SA does not model interim releases at all. This means a 12h tour can be split across the day with rest in between.

2. **Deadhead transportation tracking** — The CFR requires recording every transportation segment (mode, start/end location/time). SA tracks `DailyFRADeadheadRecord` but only as a single period, not per-segment.

3. **Other service / commingled service tracking** — The CFR requires tracking any non-covered service at the behest of the railroad. SA tracks `DailyFRACommingleRecord` but only as a single period. The CFR requires service type identification codes.

4. **Excess service reason** — When TTOD exceeds 12h, the CFR requires a reason. SA does not capture this.

5. **Monthly cumulative totals** — SA tracks `MonthlyCoveredServiceTime` and `MonthlyCommingledTime` and `MonthlyDeadheadTime` on the FRA record, but does **not** track the (ii) deadhead-from-duty-to-release or (iii) other-service subtotals separately, nor the 30h/month deadhead-after-12h cap.

---

## 4. Electronic Recordkeeping System Requirements (Subpart D, §228.201–§228.207)

Since CrewService is a greenfield electronic system, it **must** comply with Subpart D if used for FRA recordkeeping:

| # | CFR Requirement | Description | CrewService Implication |
|---|----------------|-------------|------------------------|
| §228.201(1) | System meets all Subpart D requirements | Overall compliance | Architecture must be designed with these rules |
| §228.201(2) | Records contain all §228.11 information | All 16 data fields for train employees | See §3 above |
| §228.201(3) | Sufficient monitoring indicators for accuracy | Data validation, anomaly detection | Need validation rules / program edits |
| §228.201(4) | Employee training on system use | Training program documentation | Out of scope for software; document requirement |
| §228.201(5) | IT security program for integrity | Unauthorized access prevention | Auth/authz already planned (SPEC-6) |
| §228.201(6) | FRA can prohibit/revoke electronic system | Must maintain ability to revert to manual | Compliance consideration |
| §228.203(a) | Identify each individual who entered data | Audit trail per field | `AuditStamp` value object exists; ensure per-field granularity |
| §228.203(c)(1) | **Calculate TTOD** using data entered by employee | System must auto-calculate total time on duty | Application service requirement |
| §228.203(c)(2) | **Identify input errors** via program edits | Validation filters at data entry time | Domain validation rules |
| §228.203(c)(3) | **Chronological order** for outstanding records | Records must be completed in order | Sequencing constraint on FRA record entry |
| §228.203(c)(4) | **Reconciliation** when system-generated prior time off differs from employee-reported | Conflict resolution workflow | New requirement — not in SA |
| §228.203(c)(5) | **Require explanation** if TTOD exceeds statutory max | Excess service reason capture | New field — not in SA |
| §228.203(c)(6) | **Quick tie-up** when at or within 3 minutes of max | Minimal data entry for emergency tie-up | New workflow — not in SA |
| §228.203(c)(7) | Certified final release not >3 min in future, not in past | Time validation against server clock | Validation rule |
| §228.203(c)(8) | Auto-modify for **daylight savings time** transitions | DST-safe calculations | Use UTC internally (already planned) |
| §228.203(c)(9) | Full record required at end of duty tour unless quick tie-up mandated | Completeness enforcement | Workflow rule |
| §228.203(c)(10) | Disallow quick tie-up when time remains for full record | Quick tie-up gating | Workflow rule |
| §228.203(c)(11) | Disallow manipulation that precludes compliance | Tamper prevention | Security + validation |
| §228.203(d) | **Search capabilities** by: employee, train/job, origin, release location, territory, excess service records, >12h tours | Query/search API | Read model / reporting requirement |
| §228.205 | FRA/State inspector access to records | Read-only access for regulators | Authorization role for inspectors |
| §228.207 | Employee training requirements for electronic system | Documented training program | Out of scope for software |

### Key Gap Findings

1. **Quick tie-up** — The CFR mandates a streamlined data entry process when an employee is at or within 3 minutes of their 12h max. This is a distinct UI/API workflow that neither SA nor CrewService implements.

2. **Prior time off reconciliation** — When the system calculates prior rest differently than the employee reports, the system must require reconciliation. This is a new conflict-resolution workflow.

3. **Chronological completion** — Outstanding (delayed) records must be completed in order. This requires sequencing enforcement in the FRA record entry flow.

4. **Search capabilities** — The system must support searching FRA records by 7 specific criteria within date ranges. This is a reporting/read-model requirement.

---

## 5. Excess Service Reporting (§228.19)

The railroad **must report to FRA** each of these violations. The system must detect and flag them:

| # | Violation | CFR Reference | Must Report | SA Detects? | CrewService |
|---|-----------|--------------|-------------|-------------|-------------|
| 1 | On duty >12 consecutive hours | §228.19(b)(1) | Yes | ✅ `IsRestricted` property | 🔴 |
| 2 | Continues on duty without 10h off in preceding 24h | §228.19(b)(2) | Yes | ⚠️ Partial (rest-for-next check) | 🔴 |
| 3 | Returns to duty without 10h off in preceding 24h | §228.19(b)(3) | Yes | ⚠️ Partial (NR mark-off) | 🔴 |
| 4 | Returns without additional rest for excess service | §228.19(b)(4) | Yes | ✅ Penalty rest calculation | 🔴 |
| 5 | Exceeds 276h cumulative monthly | §228.19(b)(5) | Yes | 🔴 Not tracked | 🔴 |
| 6 | On-duty 7th consecutive day (home terminal, no CBA) | §228.19(b)(6) | Yes | ⚠️ Uses 6-day limit only | 🔴 |
| 7 | Returns after 6 days without 48h off at home terminal | §228.19(b)(7) | Yes | 🔴 Not enforced | 🔴 |
| 8 | On-duty >7 consecutive days (absolute) | §228.19(b)(8) | Yes | 🔴 Not tracked | 🔴 |
| 9 | Returns after 7 days without 72h off at home terminal | §228.19(b)(9) | Yes | 🔴 Not tracked | 🔴 |
| 10 | Exceeds 30h/month deadhead after 12h tour | §228.19(b)(10)(iv) | Yes | 🔴 Not tracked | 🔴 |

### SA has 4 of 10 violation detections; CrewService has 0 of 10.

---

## 6. Revised FRA Gap Items for CrewService

Based on this CFR review, the gap items in `03-business-logic-gaps.md` §4 need expansion.
The following replaces the original 5-row gap table:

### Domain Entities (branch: `feature/gap-fra-compliance`)

| Entity | Purpose | CFR Source |
|--------|---------|------------|
| `FraEmployeeType` enum | Train / Signal / Dispatching classification per craft | §228.5 |
| `FraDutyTour` | Root entity for a complete duty tour (may span multiple assignments) | §228.11(b) |
| `FraDutyTourSegment` | Each covered-service assignment within a tour | §228.11(b)(2),(4)–(7) |
| `FraInterimRelease` | Qualifying break ≥4h at designated terminal within a tour | App A, broken service |
| `FraTransportationSegment` | Deadhead/transportation period with mode, start/end location/time | §228.11(b)(8) |
| `FraOtherServiceSegment` | Non-covered service at behest of railroad, with service type code | §228.11(b)(9),(10) |
| `FraCommingledService` | Non-covered service not separated by qualifying off-duty (existing `DailyFRACommingleRecord` concept) | §228.5, commingled service |
| `FraExcessServiceReport` | Reportable violation record with violation type, explanation, and FRA report status | §228.19 |
| `FraMonthlyAccumulator` | Per-employee monthly running totals: covered service, deadhead, other service, deadhead-after-12h | §228.11(b)(14),(15) |

### Domain Value Objects

| Value Object | Purpose | CFR Source |
|-------------|---------|------------|
| `RestRequirement` | Computed rest hours (base 10h + penalty), rested datetime | App A |
| `ConsecutiveDayState` | Current count, home-terminal flag, required rest tier (48h vs 72h) | §228.19(b)(6)–(9) |
| `DutyTourTotals` | Computed TTOD, excess amount, deadhead-to-release time | §228.11(b)(11),(13) |

### Application Services

| Service | Purpose | CFR Source |
|---------|---------|------------|
| `FraDutyTourCalculator` | Calculate TTOD from segments, including commingled and deadhead time | §228.203(c)(1) |
| `FraRestValidator` | Validate 10h post-tour rest, 8h-in-24h rule, penalty rest for excess | §228.7(a), App A |
| `FraConsecutiveDayTracker` | Track consecutive days with two-tier rest enforcement (48h/72h) | §228.19(b)(6)–(9) |
| `FraMonthlyCapTracker` | Track 276h monthly cap and 30h deadhead-after-12h cap | §228.19(b)(5),(10) |
| `FraExcessServiceDetector` | Detect all 10 reportable violations, create excess service reports | §228.19(b)(1)–(10) |
| `FraPriorTimeOffReconciler` | Compare system-calculated vs. employee-reported prior rest; require resolution | §228.203(c)(4) |
| `FraQuickTieUpService` | Streamlined tie-up when at/within 3 min of 12h max | §228.203(c)(6) |
| `FraRecordSearchService` | Query FRA records by 7 CFR-mandated search criteria | §228.203(d) |

### Validation Rules (Program Edits per §228.203(c))

| Rule | Description |
|------|-------------|
| TTOD auto-calculation | System must calculate, not allow manual entry of TTOD |
| Input error detection | Program edits flag anomalies at data entry |
| Chronological enforcement | Outstanding records must be completed in order |
| Prior time off reconciliation | Flag when system prior-rest ≠ employee-reported prior-rest |
| Excess service explanation | Require text explanation when TTOD > 12h |
| Quick tie-up gating | Force quick tie-up when ≤3 min from max; block when not needed |
| Final release time validation | Not >3 min in future, not in past vs. server clock |
| DST transition handling | Auto-correct calculations spanning DST changes |

---

## 7. Summary: SA vs. CFR Coverage

| Area | CFR Requirements | SA Covers | SA Gaps |
|------|-----------------|-----------|---------|
| Max on-duty hours | 12h consecutive | ✅ | — |
| Post-tour rest | 10h minimum | ✅ | — |
| Penalty rest for excess | Extra rest = excess hours | ✅ | — |
| 8h-in-24h rule | Pre-duty rest validation | ⚠️ Implicit only | Explicit 24h lookback |
| Consecutive days (6 → 48h) | Two-tier with home terminal | ⚠️ 6-day only | 48h enforcement, home terminal check |
| Consecutive days (7 → 72h) | Absolute limit | 🔴 | Full gap |
| 276h monthly cap | Cumulative tracking | 🔴 | Full gap |
| 30h deadhead-after-12h cap | Monthly tracking | 🔴 | Full gap |
| Interim releases | Broken/aggregate service | 🔴 | Full gap |
| Deadhead tracking (per-segment) | Mode, locations, times | ⚠️ Single record | Per-segment tracking |
| Other service / commingled (per-segment) | Type codes, times | ⚠️ Single record | Per-segment with type codes |
| Quick tie-up workflow | Emergency streamlined entry | 🔴 | Full gap |
| Prior time off reconciliation | System vs. employee conflict | 🔴 | Full gap |
| Excess service explanation | Reason text when >12h | 🔴 | Full gap |
| Excess service reporting (10 types) | Detect and report to FRA | ⚠️ 4 of 10 | 6 violation types missing |
| Electronic record search (7 criteria) | FRA inspector query access | 🔴 | Full gap |
| Wreck/relief exception (16h) | Emergency extension | 🔴 | Full gap |

**Bottom line**: SA implements the core hours/rest limits correctly but is missing approximately **60% of the CFR's recordkeeping, reporting, and compliance requirements**. CrewService should be built to full CFR compliance from the start.

---

## Cross-References

- FRA gap items in business logic context: [03-business-logic-gaps.md §4](03-business-logic-gaps.md)
- FRA entities in domain entity gaps: [01-domain-entity-gaps.md](01-domain-entity-gaps.md)
- Background workers for FRA compliance: [02-automated-process-gaps.md](02-automated-process-gaps.md)
- FRA excess service reporting as integration: [04-integration-gaps.md](04-integration-gaps.md)
- Branch plan: `feature/gap-fra-compliance` in [README.md](README.md)
