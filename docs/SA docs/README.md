# StrategicApplications Modular System Spec

This folder contains a modularized version of `docs/SystemSpec.md`.

## Organization

1. `spec-01-architecture-primary-keys-entity-reference.md`
   - Solution architecture, contexts, key generation, entity catalog, and related late-discovered details.
2. `spec-02-operations-boards-markoff-vacancy-shifts.md`
   - Operations workflows including daily crew position, vacancy, FRA, mark-off, boards, shifts, and related late-discovered details.
3. `spec-03-payroll-earnings-rates-compensation.md`
   - Payroll approvals, processing, holidays, compensation, rates, and related late-discovered details.
4. `spec-04-reference-services-integrations-crosscutting.md`
   - Services, utilities, configuration, controllers overview, integrations, and cross-cutting reconciliations.
5. `spec-05-strict-full-sweep-tracker.md`
   - Strict full sweep tracker increments and reconciliations.

## Data preservation approach

- Content was transferred by contiguous line ranges from `docs/SystemSpec.md` to avoid loss.
- Duplicate/overlapping findings remain where originally documented to preserve traceability.
- "Gap" entries are treated as later-discovered details and are kept within their pertinent modular spec documents.

## Source file retained

- `docs/SystemSpec.md` remains unchanged as the monolithic source reference.
