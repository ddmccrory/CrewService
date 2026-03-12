# 09 – FRA Drug & Alcohol Compliance (49 CFR Part 219)

## Purpose

This document specifies the system-level regulatory requirements from 49 CFR Part 219
(Control of Alcohol and Drug Use) that CrewService must support. These are **federal law** —
they apply to all railroads, all tenants. Part 219 is directly cross-referenced by
Parts 240 and 242 for certification eligibility determinations.

---

## 1. Prohibitions — Subpart B

### §219.101 — Alcohol and Drug Use Prohibited

| Rule | Requirement |
|------|------------|
| §219.101(a)(1) | No regulated employee may use or possess alcohol or any controlled substance while on duty and subject to performing regulated service |
| §219.101(a)(2)(i) | No regulated employee may report for or remain on duty while under the influence of or impaired by alcohol |
| §219.101(a)(2)(ii) | No regulated employee may be on duty with ≥0.04 alcohol concentration (breath or blood) |
| §219.101(a)(2)(iii) | No regulated employee may be on duty under the influence of any controlled substance |
| §219.101(a)(3) | No alcohol use within **4 hours** of reporting for service, or after receiving notice to report (whichever is less) |

### Alcohol Thresholds

| Threshold | Consequence | CFR |
|-----------|------------|-----|
| < 0.02 | Negative — not evidence of misuse; railroad may NOT use as basis for action | §219.101(a)(5) |
| 0.02 – 0.039 | Positive — removed from service for minimum **8 hours** or until next shift; NOT a §219.101 violation; CANNOT be used to decertify under Parts 240/242 | §219.101(a)(4) |
| ≥ 0.04 | Violation of §219.101 — immediate removal; triggers certification review under Parts 240/242 | §219.101(a)(2)(ii) |

### §219.102 — Alcohol Concentration BAC ≥ 0.04

A regulated employee who has a BAC ≥ 0.04 while on duty violates §219.102. This is the
threshold that triggers certification eligibility review under §240.119/§242.115.

---

## 2. Responsive Action — §219.104

| Step | Requirement | CFR |
|------|------------|-----|
| 1 | **Immediate removal** from regulated service upon violation of §219.101/§219.102, or refusal to test | §219.104(a)(1-2) |
| 2 | Written notice of reason for removal (verbal initially OK, written ASAP) | §219.104(b) |
| 3 | Notice must inform employee they cannot perform DOT safety-sensitive duties until return-to-duty process complete | §219.104(b) |
| 4 | If employee denies, hearing within **10 calendar days** of suspension (or per CBA) | §219.104(c)(1-2) |
| 5 | Hearing before presiding officer other than the charging official | §219.104(c)(1) |

### Refusal to Test

A refusal to provide a breath or body fluid specimen is treated the same as a positive
result for purposes of responsive action (§219.104(a)(2)) and certification eligibility
(§240.119/§242.115).

---

## 3. Testing Types

| Test Type | Subpart | When | Authority |
|-----------|---------|------|-----------|
| Post-accident toxicological | C (§219.201) | After qualifying accident/incident; within **4 hours** for alcohol, ASAP for drugs | Mandatory |
| Reasonable suspicion | D (§219.301) | Observable signs of impairment during duty | Mandatory |
| Reasonable cause | E (§219.401) | After qualifying events (elective by railroad) | Optional Federal authority |
| Random | G (§219.601) | Random selection from safety-sensitive pool | Mandatory |
| Pre-employment | F (§219.501) | Before first regulated service | Mandatory |
| Return-to-duty | 49 CFR Part 40 | Before returning after violation | Mandatory |
| Follow-up | 49 CFR Part 40 | After return-to-duty, per SAP schedule | Mandatory |

### Random Testing Rates — §219.601/§219.607

| Substance | Minimum Annual Rate | CFR |
|-----------|-------------------|-----|
| Drugs | **50%** of covered employees (may be reduced to 25% by FRA Administrator) | §219.601 |
| Alcohol | **10%** of covered employees (may be raised to 25% or 50% by Administrator) | §219.601 |

### Post-Accident Testing Criteria — §219.201

Testing is mandatory when an accident/incident involves:
- Fatality
- Release for medical treatment away from scene
- Railroad-reportable damage above current threshold
- Specific qualifying events per §219.201(a)(1-4)

---

## 4. Voluntary Referral and Return-to-Duty — Subpart F / §219.403

### §219.403 — Voluntary Referral Policy

Each railroad must maintain a voluntary referral policy that:
- Allows employees to self-refer for substance abuse counseling/treatment
- Treats the referral as **confidential** except for certification ineligibility
- Employee must be evaluated by a Substance Abuse Professional (SAP)
- Employee may not perform regulated service until SAP determines fitness

### Return-to-Duty Process (49 CFR Part 40)

| Step | Requirement |
|------|------------|
| 1 | SAP evaluation and recommended treatment |
| 2 | Completion of recommended treatment |
| 3 | SAP follow-up evaluation confirming compliance |
| 4 | Return-to-duty test — must be **negative** for drugs, **< 0.02** for alcohol |
| 5 | Follow-up testing schedule set by SAP: minimum **6 tests in first 12 months** |
| 6 | Follow-up testing may continue for up to **60 months** |

---

## 5. Certification Impact Cross-Reference

### Engineer Certification — §240.119

| Scenario | Ineligibility Period | CFR |
|----------|---------------------|-----|
| First violation of §219.101 or §219.102 | During evaluation + primary treatment | §240.119(e) |
| Second violation (within 60 months) | **2 years** | §240.119(e) |
| Third+ violation (within 60 months) | **Permanent** | §240.119(e) |
| Refusal to test | Treated as single violation | §240.119 |
| Voluntary referral (no incident) | Confidential; not a violation per se | §219.403 |

### Conductor Certification — §242.115

Same structure as §240.119 — identical ineligibility periods apply.

---

## 6. CrewService Data Model Requirements

All entities below are **system-level compliance records** — they are not tenant-configurable.
The system automatically enforces Part 219 for every employee flagged as covered
(i.e., performing regulated service). No railroad opts in or out of these requirements;
the only per-railroad decision is which employees perform regulated service.

### `DrugAlcoholTestRecord` — FraCompliance module (new)

| Field | Type | Notes |
|-------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| TestType | string | "PostAccident", "ReasonableSuspicion", "ReasonableCause", "Random", "PreEmployment", "ReturnToDuty", "FollowUp" |
| TestDate | DateTime | |
| AlcoholResult | decimal? | BAC concentration |
| DrugResult | string? | "Negative", "Positive", "Refused" |
| SubstancesDetected | string? | JSON array if positive |
| IsViolation | bool | True if ≥0.04 alcohol or positive drug |
| FederalAuthority | bool | True = FRA test; False = company authority |

### `DrugAlcoholAction` — FraCompliance module (new)

| Field | Type | Notes |
|-------|------|-------|
| CtrlNbr | ControlNumber | PK |
| TestRecordCtrlNbr | ControlNumber | FK → DrugAlcoholTestRecord |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| ActionType | string | "RemovedFromService", "HearingScheduled", "SAPReferral", "ReturnToDuty", "FollowUpScheduled" |
| ActionDate | DateTime | |
| Notes | string? | |

### `VoluntaryReferral` — FraCompliance module (new)

| Field | Type | Notes |
|-------|------|-------|
| CtrlNbr | ControlNumber | PK |
| EmployeeCtrlNbr | ControlNumber | FK → Employee |
| ReferralDate | DateTime | |
| SapEvaluationDate | DateTime? | |
| TreatmentCompletedDate | DateTime? | |
| ReturnToDutyTestDate | DateTime? | |
| ReturnToDutyResult | string? | |
| FollowUpTestsRequired | int | Min 6 in first 12 months |
| FollowUpEndDate | DateTime? | Max 60 months |
| Status | string | "Referred", "InTreatment", "ReturnToDuty", "FollowUp", "Completed" |

---

## Cross-References

- Hours of service: [06-fra-compliance-requirements.md](06-fra-compliance-requirements.md)
- Certification (Parts 240/242): [08-fra-certification-requirements.md](08-fra-certification-requirements.md)
- Implementation spec: [impl/B01-fra-compliance.md](impl/B01-fra-compliance.md)
- Source regulation: `docs/49 CFR Part 219.html`
