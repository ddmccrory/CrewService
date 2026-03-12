# 07 – Design Critique: What NOT to Port from SA

## Purpose

The gap analysis documents (01–06) catalog what SA does and what CrewService is missing.
This document takes the opposite view: **what SA gets wrong** and where CrewService should
deliberately improve rather than replicate.

Per the [Migration Charter](README.md): the goal is **100% functional replacement with zero defect carry-forward**. Every process below must produce the same end effect to the user while eliminating the underlying architectural problem.

---

## 1. Architecture: God-Object Anti-Patterns

### SA Problem

SA's `Global.asax.cs` is a **monolithic orchestrator** holding:
- 17 timer dictionaries (per-pool)
- 6 file watchers
- Active user session cache
- Processing guard booleans
- All timer handler methods

It's a single class responsible for the entire runtime lifecycle of the application. Similarly, `ApplicationUtilities` is a static class mixing control-number generation, Teams messaging, vacancy processing, IP checks, and transaction scope creation.

`DailyCrewPosition` itself is another god object — an entity with 30+ computed properties and 10+ methods that reach into the database, create child records, send Teams messages, and trigger vacancy reassignment — all from within the entity class.

### CrewService Improvement

CrewService already avoids this via clean architecture:
- Domain entities are **behavior-rich but side-effect-free** (they raise domain events, they don't call databases)
- Side effects live in application services and domain event handlers
- Infrastructure concerns are injected, not statically referenced

**Do not replicate**: SA's pattern of entities that `new` up DbContexts, call `SaveChanges()` internally, or send HTTP requests. Every SA entity method that touches the database should become an application-layer orchestration in CrewService.

---

## 2. Single-Railroad Hard-Coding → Multi-Tenant Configurable Rules

### SA Problem

SA was built for **one specific railroad**. Its 122 `PoolNumber.Equals(` branches, 20+ earning-code conditions, craft-name string switches, and hard-coded location IDs (11, 13, 14) are not general-purpose logic — they are **one tenant's operating rules baked into the application code**.

This means:
- Onboarding a second railroad would require forking or heavily modifying the codebase
- Any rule change for the existing railroad requires a code deployment
- The "logic" isn't really logic — it's **configuration disguised as code**

### CrewService Improvement

CrewService doesn't need to reproduce SA's specific branches. It needs to **support the same kinds of rules** as tenant-configurable behavior so any railroad can define their own.

CrewService already has the foundation — `CraftDisplacementPolicy`, `BulletinPolicy`, `SeniorityMovePolicy`, `BoardCascadePolicy`, and the `GroupAttributeDefinition/Value` system — but these only cover a fraction of the configurable behaviors.

**What's needed**: Extend the policy/configuration surface to cover all behavioral variants SA hard-coded for one railroad:

| SA Hard-Code (one railroad's rule) | Configurable Policy Field | Type |
|-------------------------------------|--------------------------|------|
| Earning code format (pool 30/60 vs default) | `JobCodeFormat` | string template |
| Meal deduction skip (pool 50 OT) | `DeductMealOnOvertime` | bool |
| 40h/week availability cap (pools 20, 30) | `WeeklyHoursCap` | decimal? (null = no cap) |
| Roster vs pool-level vacancy (pool 50) | `VacancyScope` | enum (Roster / WorkArea) |
| Shift-overlap delete (pool 40) | `DeleteConflictingNextShift` | bool |
| Rest calculation strategy | `RestCalculationStrategy` | enum (FRA / FixedHours / CraftConfigured) |
| Helper search enabled | `HelperSearchEnabled` | bool |
| Board ordering strategy | `BoardSortStrategy` | enum (TieUpFirst / BoardOrderFirst / FIFO) |

**This is the single highest-value architectural improvement over SA.** It turns one railroad's hard-coded rules into a configurable platform that any railroad can use — each tenant defines their own policies without code changes. The existing railroad's rules become **seed data**, not application logic.

---

## 3. Concurrency: Thread.Sleep and In-Memory Guards → Proper Coordination

### SA Problem

SA uses **48 `Thread.Sleep()` calls** and **in-memory boolean dictionaries** for concurrency:

- `PoolInProgress[poolCtrlNbr]` — prevents concurrent vacancy updates per pool
- `CallSheetInProgress[poolCtrlNbr]` — prevents concurrent call sheet generation
- `HolidayRecordsProcessing` / `VacancyRecordsProcessing` / `StatusRecordsProcessing` — boolean flags for file watchers
- Mark-off creation **polls** `CallSheetInProgress` in a `while` loop with `Thread.Sleep(1000)`
- File watchers use `Thread.Sleep(5000)` to wait for file copies to complete
- `CreateNewControlNumber()` calls `Thread.Sleep(1)` to space out timestamp-based PKs

These are **process-local** — they fail in multi-instance deployment, after app pool recycles, and create hidden coupling between unrelated operations.

### CrewService Improvement

- **Distributed locks** (database advisory lock or Redis) for per-work-area processing guards
- **Outbox pattern** (already present) replaces the need for mark-off creation to wait for call sheet completion — they become eventually consistent via domain events
- **`ControlNumber` value object** already uses a proper generation strategy — no `Thread.Sleep(1)` needed
- File processing should use **idempotent consumers** with a processed-file ledger, not sleep-and-hope

**Do not replicate**: Any `Thread.Sleep` for coordination. Any `static Dictionary<long, bool>` as a concurrency guard.

---

## 4. Data Model: Denormalization and Duplicate Contexts

### SA Problem

**Two DbContexts pointing at the same database with duplicated models:**
- `StrategicApplicationsContext` (204 DbSets, Identity-based) — used by the web app
- `SAClassLibraryContext` (215 DbSets, plain EF6) — used by Windows Services
- Both map to the same tables but with **independent entity classes** and different fluent configurations
- This means a bug fix or schema change must be applied in two places

**Pervasive denormalization for display:**
- `EmployeeNumber` is stored on `MarkOffRecord`, `DailyCrewPositionOnDutyRecord`, `DailyCrewPositionElectronicCallRecord`, and the FRA record — any employee number change requires updating all of these
- `CreatedByName` on `MarkOffRecord` opens a **new DbContext** inside a computed property to resolve a username to a display name
- FRA records denormalize `EmployeeName`, `AssignmentName`, `OnDutyLocation`, `OffDutyLocation` as strings

### CrewService Improvement

- **Single `OperationsDbContext`** shared by all consumers (already the design)
- Denormalized display fields belong in **read models / projections**, not in the write-side entities
- The gRPC presentation layer already returns composed DTOs — it can resolve display names at read time without storing them
- For FRA reporting, use a **read-side snapshot** populated by domain events rather than denormalizing into the write model

**Do not replicate**: Dual-context duplication. Computed properties that open new DbContexts. Denormalized string copies of data that lives elsewhere.

---

## 5. Primary Keys: Timestamp-Based ControlNumbers

### SA Problem

Every entity uses `ControlNumberBase` which generates PKs via:
```
Thread.Sleep(1);
return Convert.ToInt64(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
```

Issues:
- **Collision risk**: Two servers at the same millisecond produce the same key
- **Thread.Sleep(1)** is required to prevent duplicates within the same process
- **Leaks creation time**: Anyone reading a PK can derive when the record was created
- **Not monotonically increasing in clustered indexes**: millisecond granularity plus clock skew creates index fragmentation
- **Int64 overflow**: format `yyyyMMddHHmmssfff` produces values like `20260228153045123` — safe for now but tightly coupled to the date format

### CrewService Improvement

CrewService already has a `ControlNumber` value object. The generation strategy should use:
- **Database-generated sequential IDs** (SQL Server `IDENTITY` or `SEQUENCE`) for clustered index performance, OR
- **UUID v7** (time-ordered) if distributed generation is needed without database round-trips
- Audit timestamps stored as separate `CreatedAtUtc` / `ModifiedAtUtc` columns (already present via `AuditStamp`)

**Do not replicate**: Timestamp-as-PK generation. Thread.Sleep for uniqueness spacing.

---

## 6. FRA Compliance: Constants-Only vs. Full Regulatory Model

### SA Problem

SA's entire FRA implementation is a single static class with four constants:
```
MaxHours = 12
RestHours = 10
ConsecutiveDays = 6
ConsecutiveDayHours = 24
```

As documented in [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md), this covers roughly **40% of 49 CFR Part 228**. Critical missing areas:

- Two-tier consecutive day rest (48h/72h) — SA only has one tier
- 276h monthly cumulative cap — not tracked
- 30h deadhead-after-12h monthly cap — not tracked
- Interim releases (broken/aggregate service) — not modeled
- Quick tie-up workflow — not implemented
- 6 of 10 reportable violation types — not detected

This isn't a matter of "port what SA has." **SA is non-compliant with the CFR.** The gap is between the regulation and reality, not between SA and CrewService.

### CrewService Improvement

Build the FRA module directly from the CFR, not from SA's partial implementation:
- Model the duty tour as a **first-class aggregate** with segments, not as a flat record
- Implement all **10 excess service violation types** as a rule engine
- Track **monthly accumulators** as their own entity, updated on each tie-up
- Implement the **electronic recordkeeping program edits** (§228.203(c)) as domain validation rules
- Treat SA's implementation as a **behavioral reference for the 40% it does cover**, but the CFR as the authoritative spec for the remaining 60%

**Do not replicate**: SA's "four constants" approach to FRA compliance. Build to the regulation.

---

## 7. Vacancy Algorithm: Tightly-Coupled Monolith → Composable Pipeline

### SA Problem

`ApplicationUtilities.UpdateDailyCrewPositionVacancies()` is a ~500-line method that:
1. Queries all open positions for a pool
2. Loops through extra board members
3. Applies 8+ skip rules inline (worked cap, on-duty, availability, rest, mark-off, qualification)
4. Has special-case logic for foreman helper search with hard-coded location IDs (11, 13, 14)
5. Has a separate code path for pool 50 (MoW) that queries at pool-level instead of roster-level
6. Modifies board order, creates on-duty records, and sends Teams notifications — all inside the same method

This is untestable in isolation. Each skip rule is interleaved with database queries and side effects. Changing one rule risks breaking others.

### CrewService Improvement

CrewService already has the right entities — `DispatchProjection`, `DispatchDecisionLog`, `BoardCascadePolicy` — but needs a composable design:

**Pipeline approach:**
```
VacancyResolutionEngine
  ├── Step 1: CollectOpenPositions (query)
  ├── Step 2: NoBidBulletinHandler (pre-phase)
  ├── Step 3: RankBoardMembers (sort by board policy)
  └── Step 4: For each candidate:
       ├── ISkipRule[] (composable, testable)
       │   ├── WorkedCapRule
       │   ├── AlreadyOnDutyRule
       │   ├── AvailabilityRule
       │   ├── RestRule (delegates to FRA module)
       │   ├── MarkOffRule
       │   └── QualificationRule
       ├── IAssignmentStrategy (Foreman/Helper vs standard)
       └── Log decision to DispatchDecisionLog
```

Each `ISkipRule` is a single-responsibility class that can be unit tested with known inputs. The foreman helper search becomes an `IAssignmentStrategy` implementation rather than an inline special case. Pool-level vs. roster-level query scope is driven by `VacancyScope` on the craft policy (see §2).

**Do not replicate**: 500-line monolithic method. Inline skip rules. Hard-coded location IDs.

---

## 8. On-Duty Record Creation: 17-Step Inline Procedure → Domain Events

### SA Problem

`DailyCrewPosition.CreateDailyCrewPositionOnDutyRecord()` performs 17 steps in sequence inside the entity, mixing:
- Record creation (core)
- Payroll earning code resolution (payroll concern)
- AFE billing record creation (billing concern)
- FRA rest-for-next check (compliance concern)
- Mark-off linkage (absence concern)
- Teams notification (integration concern)
- Pool 40 conflicting-record deletion (pool-specific concern)

Every concern is coupled to every other. Adding FRA monthly cap tracking (which SA doesn't do) would mean modifying this already-fragile method.

### CrewService Improvement

The on-duty placement becomes a **domain command** that raises events:

```
PlaceOnDuty(employee, positionSlot, onDutyTime)
  → validates business rules
  → creates OnDutyRecord
  → raises OnDutyRecordCreated domain event

Handlers (separate, composable, independently testable):
  OnDutyRecordCreated →
    ├── PayrollEarningHandler (creates earning record)
    ├── AfeBillingHandler (creates billing record)
    ├── FraComplianceHandler (runs rest/consecutive/monthly checks)
    ├── MarkOffLinkageHandler (links existing mark-offs)
    ├── ShiftConflictHandler (handles overlap rules per craft policy)
    └── NotificationHandler (sends Teams/operational messages)
```

Each handler is a focused class. FRA compliance is a separate bounded context that **reacts** to on-duty events rather than being wired into the creation path. Adding the monthly cap tracker is just another handler — zero changes to the core placement logic.

**Do not replicate**: 17-step inline procedure mixing 6 concerns in one entity method.

---

## 9. MSMQ Pipeline → In-Process Orchestration or Saga

### SA Problem

The daily call sheet pipeline uses **5 MSMQ queues** across separate Windows Service sub-services:
1. Web app or timer creates a message in queue 1
2. Sub-service 1 reads, creates records, posts to queue 2
3. Sub-service 2 reads, creates records, posts to queue 3
4. ...through 5 stages

MSMQ is deprecated. The pipeline is fragile — if one sub-service crashes, messages pile up. There's no built-in retry, no dead-letter monitoring, no visibility into pipeline state. Debugging requires checking Windows Event Logs on the service host.

But the underlying idea — **staged record generation** — is sound. The pipeline exists because each stage depends on the previous stage's output.

### CrewService Improvement

The staged dependency is real, but MSMQ is the wrong mechanism. Options:

**Option A — Synchronous orchestrator (simplest, recommended for v1):**
`CallSheetGenerationService` calls each stage in sequence within a single unit of work. If stage 3 fails, the transaction rolls back stages 1–2 as well. No message queue needed — the dependency chain is sequential by nature.

**Option B — Domain event chain (when stages need independent failure):**
Each stage raises an event consumed by the next stage's handler. The outbox ensures delivery. Each stage is its own transaction — partial progress is possible and resumable.

**Option C — Saga pattern (when stages span services or tenants):**
A `CallSheetSaga` tracks state across stages. Useful if generation ever needs to span multiple services or handle per-stage compensation.

For a call sheet pipeline that runs for a single work area: **Option A** is correct. The MSMQ design was solving a deployment constraint (separate Windows Services), not a domain modeling problem.

**Do not replicate**: MSMQ. Multi-process pipeline for what is inherently a sequential single-tenant operation.

---

## 10. Payroll Earning Code Resolution: One Railroad's Branches → Configurable Rule Engine

### SA Problem

`GetPayrollEarningCode()` is a ~200-line method with 20+ conditional branches determining the earning code based on: pool number, off-day flag, worked-double flag, holiday flag, same-shift flag, unassigned status, consecutive days, and more.

These branches encode **one specific railroad's earning rules**. They are correct for that railroad, but they are not generalizable logic — they are configuration expressed as code. A second railroad would have completely different earning rules, and there is no way to support that without forking the method.

### CrewService Improvement

Model earning code rules as **data-driven configuration**, not code branches:

```
EarningCodeRule
  ├── Priority (int) — evaluated in order, first match wins
  ├── Conditions (JSON or structured)
  │   ├── IsOffDay: true/false/null (null = don't care)
  │   ├── IsWorkedDouble: true/false/null
  │   ├── IsHoliday: true/false/null
  │   ├── IsSameShift: true/false/null
  │   ├── IsUnassigned: true/false/null
  │   ├── ConsecutiveDaysMin: int?
  │   └── CraftId: guid? (null = all crafts)
  └── ResultCode: string (the earning code to assign)
```

The resolution becomes: load rules for the work area, evaluate in priority order, return first match. The original railroad's rules become **seed data** for that tenant. New railroads define their own rules through the same mechanism — zero code changes.

**Do not replicate**: Hard-coded earning code branches. Build a configurable rule matcher that can express any railroad's rules.

---

## Summary: Port vs. Improve Decision Matrix

| SA Behavior | Port As-Is? | Improve? | Approach |
|------------|-------------|----------|----------|
| Core domain concepts (what off-day means, how rest resets consecutive days) | ✅ Support | — | These are domain truths; the system must be able to express them |
| One railroad's specific rules (pool branches, earning codes, location IDs) | ❌ Don't hard-code | ✅ | Capture as **seed data / tenant config** — not application code |
| Timer scheduling per work area | — | ✅ | Replace `System.Timers.Timer` + `Global.asax` with `BackgroundService` + DB-driven schedule |
| MSMQ 5-queue pipeline | ❌ Don't port | ✅ | Synchronous orchestrator or domain event chain |
| 48 Thread.Sleep calls | ❌ Don't port | ✅ | Distributed locks, outbox, idempotent consumers |
| Timestamp-based PKs | ❌ Don't port | ✅ | Sequential DB IDs or UUID v7 |
| Dual DbContext (web + services) | ❌ Don't port | ✅ | Single shared `OperationsDbContext` (already done) |
| Entity methods with side effects | ❌ Don't port | ✅ | Domain events + application-layer handlers |
| FRA four-constant model | ❌ Don't port | ✅ | Full CFR Part 228 compliance model |
| 17-step on-duty creation | — | ✅ | Domain command + event-driven handler chain |
| Earning code determination | — | ✅ | Data-driven rule engine (one railroad's rules become seed data) |
| Vacancy algorithm | — | ✅ | Composable skip-rule pipeline with configurable scope/strategy |
| On-duty/off-duty data requirements | ✅ Support | — | The *data captured* is correct; the *code structure* needs decomposition |
| Mark-off code reference data | ✅ Support | — | The code flags are domain truth; model as configurable reference entities |
| FRA regulations (12h max, 10h rest, penalty rest, + the 60% SA missed) | ✅ Support | ✅ | Build to the CFR, not to SA's partial implementation |
| AtHoc integration protocol | ✅ Support | — | The OAuth2 + polling pattern is correct; extract behind `ICrewNotificationProvider` |

### Guiding Principle

**Port the domain knowledge. Improve the architecture. Replace 100% of user-facing functionality.**

SA was built for one railroad. CrewService is a **platform** that must support that railroad's rules — and any future railroad's rules — through configuration, not code. The original railroad's behavior is preserved as tenant seed data. The system's value is that it can express the *kinds* of rules railroads need, not that it hard-codes any one railroad's specific rules.

What changes is *how* those rules are structured: static classes, god objects, magic numbers, Thread.Sleep, dual contexts, and inline side effects are defects, not features. They are eliminated in the migration — not optional improvements for later.

---

## Cross-References

- Gap items these improvements apply to: [03-business-logic-gaps.md](03-business-logic-gaps.md)
- FRA regulatory baseline: [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md)
- Automated process improvements: [02-automated-process-gaps.md](02-automated-process-gaps.md)
- Integration improvements: [04-integration-gaps.md](04-integration-gaps.md)
