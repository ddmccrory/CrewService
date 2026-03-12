# CrewService ↔ StrategicApplications Gap Analysis

## Migration Charter

**Goal**: Deliver **100% functional replacement** of the legacy StrategicApplications system in CrewService. Every automated process, user workflow, and operational outcome that exists in SA must exist in CrewService — the end effect to the user is identical.

**Constraint**: **Zero defect carry-forward.** The architectural problems in SA (god objects, hard-coded pool branches, Thread.Sleep concurrency, timestamp PKs, dual DbContexts, four-constant FRA model, MSMQ coupling, inline side effects) are eliminated — not ported. See [07-design-improvements.md](07-design-improvements.md) for the full critique and replacement patterns.

**Principle**: **Port the domain knowledge. Improve the architecture.** SA was built for one specific railroad — its hard-coded rules are that railroad's configuration expressed as code. CrewService does not reproduce those specific branches; it builds a **configurable platform** that can express those rules (and any other railroad's rules) through tenant-level policy and configuration entities. The original railroad's rules become seed data, not application logic.

## Branch / Commit Strategy

All gap-closure work should follow this branch layout:

```
main
 └─ feature/session-context          ← current working branch
      └─ feature/gap-{module}        ← one branch per gap module (see below)
           └─ commits: one per logical unit of work
```

### Recommended Branches (in priority order)

| Branch | Covers | Priority |
|--------|--------|----------|
| `feature/gap-daily-operations` | Daily call sheet generation, shift management, on-duty/off-duty lifecycle | P0 – Core |
| `feature/gap-mark-off-system` | Mark-off/mark-up lifecycle, absence codes, auto-markup, FRA-generated marks | P0 – Core |
| `feature/gap-fra-compliance` | FRA hours-of-service engine per 49 CFR Part 228: duty tour tracking, rest validation, consecutive day tiers, monthly caps, excess service reporting, quick tie-up, electronic recordkeeping program edits | P0 – Core |
| `feature/gap-vacancy-assignment` | Auto-vacancy algorithm, extra board consumption, helper search, pool guards | P0 – Core |
| `feature/gap-payroll-engine` | Payroll record creation, earning codes, approval routing, pay rate calculation | P1 – High |
| `feature/gap-electronic-calling` | AtHoc/notification integration, crew calling, response tracking | P1 – High |
| `feature/gap-background-services` | Timer-based automation, file watchers, MSMQ replacement | P1 – High |
| `feature/gap-roster-board-ops` | Roster boards, hangout processing, board mark-offs, seniority move timers | P2 – Medium |
| `feature/gap-holiday-payroll` | Holiday qualification, holiday payroll record generation, compensation types | P2 – Medium |
| `feature/gap-reporting-exports` | Daily reports, payroll PDF generation, ADP/UKG export/import | P2 – Medium |
| `feature/gap-railroad-information` | Railroad information records, publish timers, read-receipts | P3 – Lower |
| `feature/gap-safety-besafe` | BeSafe safety observation module (exists in SAClassLibrary only) | P3 – Lower |

### Commit Convention

```
gap({module}): {short description}

- Domain entities / value objects
- Repository interfaces
- Configuration / EF mappings
- Application services / use cases
- gRPC proto + presentation service
- Background worker (if applicable)
- Unit tests
```

## Document Index

### Analysis Documents (reference material)

| File | Description |
|------|-------------|
| [01-domain-entity-gaps.md](01-domain-entity-gaps.md) | Entity-level coverage comparison |
| [02-automated-process-gaps.md](02-automated-process-gaps.md) | Timer-based and file-watcher automation gaps |
| [03-business-logic-gaps.md](03-business-logic-gaps.md) | Core algorithm and business rule gaps |
| [04-integration-gaps.md](04-integration-gaps.md) | External system integration gaps |
| [05-module-mapping.md](05-module-mapping.md) | SA concept → CrewService module mapping |
| [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md) | 49 CFR Part 228 — hours of service |
| [07-design-improvements.md](07-design-improvements.md) | What NOT to port — architectural improvements |
| [08-fra-certification-requirements.md](08-fra-certification-requirements.md) | 49 CFR Parts 240 & 242 — certification |
| [09-fra-drug-alcohol-requirements.md](09-fra-drug-alcohol-requirements.md) | 49 CFR Part 219 — drug and alcohol |

### Build Specs (implementation plan — `B##` prefix = build order)

| File | Phase | Branch |
|------|-------|--------|
| [impl/00-plan-overview.md](impl/00-plan-overview.md) | — | **Master plan — start here** |
| [impl/B01-fra-compliance.md](impl/B01-fra-compliance.md) | 0 | `feature/gap-fra-compliance` |
| [impl/B02-daily-operations.md](impl/B02-daily-operations.md) | 1 | `feature/gap-daily-operations` |
| [impl/B03-mark-off-system.md](impl/B03-mark-off-system.md) | 2 | `feature/gap-mark-off-system` |
| [impl/B04-vacancy-assignment.md](impl/B04-vacancy-assignment.md) | 3 | `feature/gap-vacancy-assignment` |
| [impl/B05-payroll-engine.md](impl/B05-payroll-engine.md) | 4 | `feature/gap-payroll-engine` |
| [impl/B06-electronic-calling.md](impl/B06-electronic-calling.md) | 5 | `feature/gap-electronic-calling` |
| [impl/B07-background-services.md](impl/B07-background-services.md) | 6 | `feature/gap-background-services` |
| [impl/B08-roster-board-ops.md](impl/B08-roster-board-ops.md) | 7 | `feature/gap-roster-board-ops` |
| [impl/B09-holiday-payroll.md](impl/B09-holiday-payroll.md) | 8 | `feature/gap-holiday-payroll` |
| [impl/B10-reporting-exports.md](impl/B10-reporting-exports.md) | 9 | `feature/gap-reporting-exports` |
| [impl/B11-railroad-information.md](impl/B11-railroad-information.md) | 10 | `feature/gap-railroad-information` |
| [impl/B12-safety-besafe.md](impl/B12-safety-besafe.md) | 11 | `feature/gap-safety-besafe` |
| [impl/B13-ptra-seed-data.md](impl/B13-ptra-seed-data.md) | 12 | `feature/gap-ptra-seed` |
| [impl/B14-cross-cutting.md](impl/B14-cross-cutting.md) | 0a | `feature/gap-cross-cutting` |
| [impl/B13-ptra-seed-data.md](impl/B13-ptra-seed-data.md) | 12 | `feature/gap-ptra-seed` |
