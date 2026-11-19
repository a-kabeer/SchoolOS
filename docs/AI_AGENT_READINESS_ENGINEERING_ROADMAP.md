# SchoolOS AI-Agent Readiness Audit — Prioritised Engineering Roadmap

- **Document ID:** AUDIT-ROADMAP-001
- **Status:** Proposed engineering sequence — not a task-status register
- **Decision maturity:** Recommendation based on the read-only audit; approval and detailed task mapping pending
- **Owner:** Technical lead (named owner TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `3cc2a02342f60af124e04aeb86563c331b497a1f`
- **Companion audit:** [AI-Agent Readiness & Complete Implementation Gap Audit](AI_AGENT_READINESS_AND_IMPLEMENTATION_GAP_AUDIT.md)

## 1. Purpose and authority

This is a phase/dependency roadmap distilled from the complete gap audit. It is deliberately not a second task register. Detailed checklists stay in canonical workstream plans under `docs/task-plans/`; individual task status stays in the eight CSV partitions under `registers/`; requirement traceability stays in DOC-51 and `registers/requirement-traceability.csv`; decisions stay in DOC-60 and approved ADRs; phase-level delivery sequencing remains DOC-08.

This roadmap does not approve open decisions, alter existing work-package statuses, create new requirements or authorize code changes before required sign-offs.

## 2. P0 — establish the safe foundation and first pilot slice

| Sequence | Workstream | Canonical plan / specifications | Prerequisite / decision gate | Deliverables | Acceptance gate |
|---:|---|---|---|---|---|
| P0.1 | Product, security, finance and topology decisions | DOC-00/01/03/07/11/14/15/18/44–51/60/62; relevant ADRs | Product owner and relevant domain/security/legal/operations owners available | Close or explicitly block DEC-001/002/003/004/005/006/008/010/012/017/020/021/022/023 as applicable; approve first release scope and identity/security model | Owner sign-offs; approved data ownership, legal/privacy scope, ERD/contract decisions and named owners |
| P0.2 | Build/test/module boundary foundation | DOC-08 Phase 1; DOC-03; DOC-52 | P0.1 gates for affected design | Minimal Cloud/Local composition roots, module registration boundaries, build rules, DB integration harness, architecture tests, build/test/security CI | ARC-001, DB-001; clean build/test in CI; prohibited Cloud↔Local dependencies fail tests |
| P0.3 | Persistence and tenant identity | H1, H2; DOC-44; DOC-62 | Tenant hierarchy and DB model decisions | Separate Cloud platform DB and Local operational DB; module-owned schema/migration; tenant, school, campus, node and identity entities; global user/membership | SAAS-001/002/006/007/009; hierarchy constraints, migrations and fail-closed tenant integration tests |
| P0.4 | Authentication, authorization and audit | BCP-1; H2/H5; DOC-38/46 | Identity provider/MFA/local lifecycle approval | Concrete TenantContext, separate authentication audiences, session lifecycle, default-deny permission/scope/resource/state checks, RLS, audit | SEC-001–007, SAAS-003/004/005/014, OPS-003 |
| P0.5 | First persisted school vertical slice | DOC-08 Phases 2–3; DOC-43/45/47 | Persistence, authentication, authorization and audit | Admission → enrollment; fee invoice/payment allocation; initial finance posting contract | ADM/FAM/FEE/FIN P0 acceptance; transaction, idempotency and audit evidence |
| P0.6 | Local Data Plane runtime | H2/H5; DOC-14/16/48 | Core schema, identity and first workflows | Installable Local Node, local PostgreSQL, local auth, HTTPS, files/jobs, health/readiness and backup primitives | HYB-001, SAAS-011; install, disconnected-operation and recovery tests |
| P0.7 | Offline and durable synchronization | H4/H6; DOC-05/14/18/45/47/57 | Local runtime, ownership rules and node identity | Outbox/Inbox, cursors, retry/idempotency, conflict workflow, device management and client queue | OFF-001–008, HYB-002/003, SAAS-012 |
| P0.8 | Billing, entitlement and signed licensing | BILL-0/1; H1/H3; DOC-15/27/36/45/46/60/62 | Tenant/node identity; commercial decisions | Versioned subscription, payment verification, student/node metering, signer/verifier, automatic+manual renewal, Restricted Mode | SAAS-010, LIC-001–004; signed test vectors, tamper/replay/wrong-node tests |
| P0.9 | Multi-campus and manual exchange | H6; DOC-18/45/48/50/57 | Nodes, sync, ownership and selected topology | Node-campus assignment, signed encrypted packages, dry-run/import approval/read-only reporting and freshness | HYB-004–006, SAAS-008/012/013 |
| P0.10 | Backup, restore and pilot safety | H5/H7; DOC-07/16/48/52/60 | Schema/runtime; DEC-017/021/022 | Encrypted DB + document + configuration/key recovery; separate backup custody; alerts; restore procedure and operational ownership | OPS-001/002; recorded restore drill, integrity validation and measured RPO/RTO |

## 3. P1 — public-production requirements

| Sequence | Workstream | Canonical plan / specifications | Dependency | Deliverables | Acceptance gate |
|---:|---|---|---|---|---|
| P1.1 | Admissions, students and guardian relationships | DOC-02/04/43/45/46/47/55; DOC-08 Phase 2 | P0 persistence, auth and authorization | Full admission/enrollment, person matching, guardian/household, academic history and transfer flows | ADM-001–005, FAM-001–003; relationship-scope tests |
| P1.2 | Fees, finance and reconciliation depth | DOC-02/04/43–47; DEC-005/006/007 | P0 vertical slice | Fee schedules, partial/sibling allocations, receipts, double entry, refunds, bank import/matching and close | FEE/FIN/REC P0/P1 tests and reconciliation evidence |
| P1.3 | Academics, attendance and exams | DOC-01/02/43/45–49/55; DOC-08 Phases 4–5 | P0 persistence, authorization and Local Node foundations | Curriculum versioning, attendance/device operations, exam/marks/moderation/result publication | EXM-001–006 and approved offline marks semantics |
| P1.4 | Organization billing, metering and customer lifecycle | BILL detailed plan; DOC-27/28/36/37/45/60/62 | P0 tenant + node + entitlement foundations; commercial approvals | Plan versions, invoices, payment verification, student/node snapshots, credits/refunds, customer exit | BILL/SAAS-010 and DEC-018/020/023 evidence |
| P1.5 | Multi-campus deployment and exchange | H6; DOC-18/45/48/50/57 | Nodes, sync, ownership and selected topology | Node-campus assignment, signed packages, dry-run/import approval/read-only reporting and freshness | HYB-004–006, SAAS-008/012/013 |
| P1.6 | BCP production surfaces | BCP-0…11, DOC-29…41, DOC-38/41/45/46/52 | P0 identity/security/audit; billing and licence state machines | Separate audience auth, registration approval, synthetic demo isolation/cleanup, customer portal, least-privilege admin/support | DEM-001/002, SEC-007, SAAS-014 and BCP acceptance |
| P1.7 | Frontend workflows, accessibility and locale | DOC-01/07/49; UX-001/002 | P0 API/auth/workflow foundations | Role-aware navigation/screens/forms, responsive layout, accessibility, RTL/approved languages and explicit sync states | UX-001/002 and critical Playwright journeys |
| P1.8 | Security/privacy/observability/CI hardening | DOC-07/16/38/48/52/60; relevant skills | P0 foundation | Threat model, secrets lifecycle, retention/deletion, scanning, telemetry, incident/support process, release pipelines | Security scan results, OPS-002/003, restore/support evidence |
| P1.9 | AI provider gateway, RAG and approved tools | AI-0…3, DOC-20…26, DOC-60 | Tenant authorization, data projection, audit, provider/legal decisions, DEC-023 quotas | Provider routing, classification/redaction, secrets, usage ledger, vector/document ingestion, ACL filtering, typed read-only tools and prompt-injection defense | AI-001–003; provider denial, ACL and adversarial tests |
| P1.10 | First AI features | AI-4…6 | P1.9 and data/quality/privacy gates | Auditor/Copilot then learning/exam, voice and intervention in approved increments | AI-004/005; human review, quality/fairness and cost acceptance |

## 4. P2 — later/advanced capabilities

| Sequence | Capability | Dependency and gate |
|---:|---|---|
| P2.1 | Optional private/campus-local AI inference | AI-7, supported hardware/runtime matrix, separate support/patching and local data policy |
| P2.2 | Advanced enterprise identity (SAML/OIDC federation, SCIM and configurable tenant IdP) | Secure baseline authentication/membership/tenant lifecycle and approved commercial tier |
| P2.3 | Dedicated tenant database/deployment and advanced residency placement | DEC-021/022, tested restore and data residency/contract evidence |
| P2.4 | Advanced usage metering, quotas, noise isolation and enterprise reporting | DEC-023 after foundational billing and sync metrics |
| P2.5 | Additional HR/payroll, library, inventory, procurement and transport modules | Explicit product requirement, legal/data review, plan/entitlement and task approval; not assumed in current release scope |

## 5. Completion and governance rules

- Reuse existing H, BILL, BCP, AI, STQA and DOCALIGN plans. Do not duplicate their detailed checklists in DOC-08 or this roadmap.
- Update existing CSV task rows only when current status and evidence satisfy DOC-59. Do not mark work started because it was planned or audited.
- Extend the existing requirement traceability rows with code path, executable test path/name, test run URL/result and reviewer/PR/commit evidence; do not make a parallel tracker.
- Resolve DOC-60 decisions only through owner-approved ADR/decision closure and required validation.
- P0 means a pilot gate, P1 a public-production gate, and P2 a later/advanced capability. External legal, provider or customer verification is a dependency, not an implementation task an agent can invent.
- A task is Done only with acceptance evidence, code review and appropriate tests; a green documentation-only workflow does not certify application readiness.
