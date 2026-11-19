# 51 — Requirements Traceability and Documentation Sign-off

- **Document ID:** DOC-51
- **Status:** Draft
- **Decision maturity:** Proposed traceability and sign-off baseline; review pending
- **Owner:** Product owner (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `898812f89e0ec7f03aff6c3b1408e46782814dfd` (base for documentation governance refactor)
**Version:** v1.8 draft · **Status:** Review pending  
**Purpose:** Link SchoolOS requirements to specifications, data/API contracts and acceptance tests before implementation.

## 1. Status vocabulary

- **Draft:** Written but not reviewed.
- **In Review:** Review by owner is active.
- **Approved:** Required owners signed off.
- **Open Decision:** Decision required before affected implementation.
- **External Verification:** Requires regulator, provider, bank, legal counsel or board evidence.
- **Implemented:** Code, migration and tests prove completion at a cited commit.
- **Deferred:** Explicitly outside the target release.

A specification approved for implementation does not mean the feature is implemented.

## 2. Requirements traceability matrix

| ID | Requirement | Existing source | Current specification | Implementation task IDs | Acceptance tests | Requirement status | Evidence status |
|---|---|---|---|---|---|
| PLAT-001 | .NET Modular Monolith | 00, 03 | 42 | Unassigned (Unassigned — no clearly matching task) | ARC-001 | Documented | Reference only; test execution not verified |
| PLAT-002 | Cloud Control Plane + Local Data Plane | 14, ADR-011 | 44, 45, 48 | H1-01; H2-01 (Mapped — confirm scope) | HYB-001 | Confirmed direction | Reference only; test execution not verified |
| PLAT-003 | Separate platform and operational DBs | 14, 36 | 44 | H1-01; H2-03 (Mapped — confirm scope) | DB-001; SEC-001 | Confirmed direction | Reference only; test execution not verified |
| TEN-001 | Tenant → School → Campus and single-tenant Local Node hierarchy | 00, 04, 14, 36 | 14, 36, 43, 44, 62 | H1-02; H6-01; H6-02; BCP-005; BCP-021 (Mapped — confirm scope) | SEC-001; SEC-002; SAAS-002; SAAS-009 | Proposed v1 baseline; DEC-001 approval pending | Reference only; implementation and test execution not verified |
| TEN-002 | Server-derived TenantContext, authorization and tenant isolation | 07, 38 | 44, 45, 46, 62 | BCP-007; H7-08 (Mapped — confirm scope) | SEC-001; SEC-002; SEC-003; SEC-004; SEC-005; SEC-006; SAAS-003; SAAS-004; SAAS-005; SAAS-006; SAAS-007; SAAS-014 | Documented proposal; DEC-004 approval and implementation evidence pending | Reference only; implementation and test execution not verified |
| TEN-003 | SaaS tenant provisioning, membership lifecycle, node enrollment and entitlement binding | 14, 15, 29, 36 | 14, 36, 45, 46, 47, 48, 62 | BCP-005; BCP-006; BCP-017; BCP-018; BCP-019; BCP-021; BCP-024; H1-02 (Mapped — confirm scope) | SAAS-001; SAAS-003; SAAS-008; SAAS-009; SAAS-010; SAAS-011; SAAS-012; SAAS-013 | Proposed v1 baseline; DEC-001/008/020 approval pending | Reference only; implementation and test execution not verified |
| ID-001 | Separate Person from User credentials | 04 | 43 | Unassigned (Unassigned — no clearly matching task) | ID-001 | Proposed | Reference only; test execution not verified |
| ID-002 | Permission + scope + resource relationship | 07, 38 | 46 | BCP-007; H7-08 (Mapped — confirm scope) | SEC-001; SEC-002; SEC-003; SEC-004; SEC-005; SEC-006 | Documented | Reference only; test execution not verified |
| ADM-001 | Admission-to-enrollment workflow | 02, 08 | 43, 45, 47 | Unassigned (Unassigned — no clearly matching task) | ADM-001; ADM-002; ADM-003; ADM-004; ADM-005 | Documented | Reference only; test execution not verified |
| STU-001 | Family/guardian/sibling links | 02, 04 | 43, 46 | Unassigned (Unassigned — no clearly matching task) | FAM-001; FAM-002; FAM-003 | Documented | Reference only; test execution not verified |
| STU-002 | Preserve enrollment history | 02, 04 | 43, 44 | Unassigned (Unassigned — no clearly matching task) | ADM-004 | Documented | Reference only; test execution not verified |
| FEE-001 | Fee heads, fee structures and invoices | 02, 04 | 43, 45 | Unassigned (Unassigned — no clearly matching task) | FEE-001 | Documented | Reference only; test execution not verified |
| FEE-002 | Partial payments and payment allocations | 02, 04 | 43, 45 | Unassigned (Unassigned — no clearly matching task) | FEE-002; FEE-004; FEE-005 | Documented | Reference only; test execution not verified |
| FEE-003 | Sibling consolidated payment | 02, 04 | 43, 45 | Unassigned (Unassigned — no clearly matching task) | FEE-003 | Documented | Reference only; test execution not verified |
| FEE-004 | Provisional offline cash receipt | 05, 14 | 45, 48 | H4-02; H4-06 (Mapped — confirm scope) | FEE-006; FEE-007 | Documented | Reference only; test execution not verified |
| FIN-001 | Double-entry, balanced journals | 04, 07 | 43, 44 | Unassigned (Unassigned — no clearly matching task) | FIN-001; FIN-002 | Documented | Reference only; test execution not verified |
| FIN-002 | Posted journal immutability | 02, 04 | 43, 46 | Unassigned (Unassigned — no clearly matching task) | FIN-003 | Documented | Reference only; test execution not verified |
| REC-001 | Bank statement import with provenance | 02, 04 | 43, 45, 50 | Unassigned (Unassigned — no clearly matching task) | REC-001; REC-002 | Documented | Reference only; test execution not verified |
| REC-002 | Match suggestion is not settlement | 02, 12 | 43, 45 | Unassigned (Unassigned — no clearly matching task) | REC-003; REC-004; REC-005; REC-006 | Documented | Reference only; test execution not verified |
| ACA-001 | Versioned board/curriculum/grading | 01, 04 | 43 | Unassigned (Unassigned — no clearly matching task) | EXM-006 | External content rights open | Reference only; test execution not verified |
| ATT-001 | Offline attendance on Local Node | 05, 14 | 45, 48 | H2-05; H4-02; H4-05 (Mapped — confirm scope) | OFF-001; OFF-002 | Confirmed direction | Reference only; test execution not verified |
| ATT-002 | Idempotent sync and conflict review | 05, ADR-013 | 45 | H4-02; H4-06; H4-08; H4-12 (Mapped — confirm scope) | OFF-003; OFF-004; OFF-005; OFF-006; OFF-007; OFF-008; HYB-002; HYB-003 | Documented | Reference only; test execution not verified |
| EXM-001 | Marks limits and absence/exemption rules | 02, 04 | 43, 45 | Unassigned (Unassigned — no clearly matching task) | EXM-001; EXM-002 | Documented | Reference only; test execution not verified |
| EXM-002 | Approval and immutable published results | 02, 04 | 43, 45, 46 | Unassigned (Unassigned — no clearly matching task) | EXM-003; EXM-004; EXM-005 | Documented | Reference only; test execution not verified |
| SEC-001 | Default-deny/resource authorization | 07, 38 | 46 | BCP-007; H7-08 (Mapped — confirm scope) | SEC-001; SEC-002; SEC-003; SEC-004; SEC-005; SEC-006 | Documented | Reference only; test execution not verified |
| SEC-002 | Separate public/demo/portal/admin identities | 38, 41 | 45, 46 | BCP-003; BCP-006 (Mapped — confirm scope) | DEM-001; DEM-002 | Documented | Reference only; test execution not verified |
| SEC-003 | MFA/step-up for privileged actions | 38 | 46 | BCP-004; BCP-006; BCP-022 (Mapped — confirm scope) | SEC-007 | Policy approval pending | Reference only; test execution not verified |
| SEC-004 | Audit sensitive actions and exports | 07, 38 | 43, 46 | BCP-007; BCP-022 (Mapped — confirm scope) | OPS-003 | Documented | Reference only; test execution not verified |
| HYB-001 | Local operation during cloud outage | 14, 05 | 45, 48 | H2-05; H2-06; H7-03 (Mapped — confirm scope) | HYB-001 | Confirmed direction | Reference only; test execution not verified |
| LIC-001 | Automatic and manual signed .lic renewal | 15, ADR-012 | 45, 48 | H3-08; H3-09; H3-10 (Mapped — confirm scope) | LIC-001; LIC-002; LIC-003 | Policy confirmed | Reference only; test execution not verified |
| LIC-002 | Restricted Mode preserves read/export/backup/renewal | 15, 11 | 46, 48 | H3-11; H3-13 (Mapped — confirm scope) | LIC-004 | Operation matrix open | Reference only; test execution not verified |
| HYB-002 | Signed/encrypted campus data exchange | 18, ADR-014 | 45, 48, 50 | H4-02; H4-03; H4-06; H6-05 (Mapped — confirm scope) | HYB-004; HYB-005; HYB-006 | Documented | Reference only; test execution not verified |
| BCP-001 | Control Plane owns BCP and commercial data | 29, 36 | 42, 44, 45 | BCP-001; BCP-002 (Mapped — confirm scope) | SEC-001; SEC-002; SEC-003; SEC-004; SEC-005; SEC-006 | Documented | Reference only; test execution not verified |
| BCP-002 | Ephemeral synthetic demo | 41, ADR-021 | 45, 46 | BCP-013; BCP-014; BCP-036; BCP-037; BCP-038; BCP-039; BCP-040; BCP-041 (Mapped — confirm scope) | DEM-001; DEM-002 | TTL/storage open | Reference only; test execution not verified |
| AI-001 | Privacy-aware AI provider gateway and RAG | 20–26 | Existing AI docs | AI-1-02; AI-1-08; AI-3-05; AI-3-08 (Mapped — confirm scope) | AI-001; AI-002; AI-003; AI-004; AI-005 | Implementation Not Started | Reference only; test execution not verified |
| MIG-001 | Validated, repeatable school data migration | 08, 18 | 43, 50 | BILL-502; H6-08 (Mapped — confirm scope) | MIG-001; MIG-002; MIG-003; MIG-004; MIG-005 | Draft | Reference only; test execution not verified |
| UX-001 | Role-aware responsive UI and Urdu RTL | 01, 07 | 49 | Unassigned (Unassigned — no clearly matching task) | UX-001; UX-002 | Language scope open | Reference only; test execution not verified |

## 3. Task and test traceability rules

The machine-readable `registers/requirement-traceability.csv` is the canonical row-level register. Every requirement has exact acceptance-test IDs and either existing implementation task IDs or an explicit Unassigned marker. Mapped task IDs must exist in the seven task-register partitions. References are not evidence of implementation or test execution; PR/commit, test-run and reviewer links are required before completion.

## 4. New v1.8 documentation inventory

| Document | What it adds | Review owners | Status |
|---|---|---|---|
| 42 — Documentation Completion & Gap Register | Verified repository baseline, documentation gaps, architecture conflict notes and no-code gate | Product owner, technical lead | Draft |
| 43 — Operational Domain Data Dictionary | Operational entities, logical fields, types, constraints and data handling | Technical lead, domain owners | Draft |
| 44 — PostgreSQL ERD & Migration Specification | Cloud/local DB boundary, ERD, constraints, indexes, RLS and migration rules | Technical lead, DBA/security | Draft |
| 45 — API & Integration Event Contracts | Local/Cloud API families, DTO/error conventions, idempotency and event envelope | Backend lead, security | Draft |
| 46 — Authorization & Tenant Isolation Matrix | Roles, scopes, separation of duties, guardian access and Restricted Mode overlay | Product owner, security | Draft |
| 47 — Workflow Acceptance Test Catalogue | Given/When/Then cases for admissions, finance, reconciliation, offline, exams, security and operations | Product owner, QA | Draft |
| 48 — Hybrid Deployment Operations & Recovery Runbook | Provisioning, monitoring, backup, restore, upgrade, outage and support procedures | Operations, technical lead | Draft |
| 49 — Operational UI/UX & Screen Catalogue | Navigation, dashboards, screens, states, forms, RTL and accessibility | Product owner, design/QA | Draft |
| 50 — Data Migration & Import/Export Specification | Templates, mappings, validation, reconciliation, dry-run and portability | Product owner, migration/finance | Draft |
| 51 — Requirements Traceability & Sign-off | Requirement-to-spec/test mapping and sign-off process | Product owner, technical lead, QA | Draft |
| 62 — SaaS Tenant Architecture and Implementation Blueprint | Tenant hierarchy, identity/membership, TenantContext, Cloud RLS, Local Node binding, provisioning, entitlement, sync and SaaS acceptance gates | Product owner, technical lead, security, QA | Draft |

Existing docs 00–41 remain the source decision and requirements baseline. The new documents supplement them; they do not silently supersede ADRs or mark implementation tasks complete.

## 5. Open decisions requiring explicit approval

| Decision | Owner | Affected documents |
|---|---|---|
| Exact Tenant → School → Campus cardinality | Product owner | 00, 04, 42–44 |
| UUID vs alternate key strategy | Technical lead | 43–45 |
| Pilot: shared group node or per-campus nodes | Product + pilot customer | 14, 18, 48 |
| Fees-to-Finance transaction boundary | Backend + finance lead | 02, 04, 43–45 |
| RLS context, DB roles and connection pooling | Backend + security | 07, 38, 44, 46 |
| National identifier classification, encryption and retention | Security + legal | 07, 38, 43, 50 |
| Accounting basis, chart of accounts, fiscal year and approval limits | Finance owner | 04, 08, 43, 46, 47 |
| First bank formats and payment channels | Product/integration owner | 11, 45, 47, 50 |
| First board/curriculum content and usage rights | Academic owner + legal | 01, 04, 43, 47 |
| Authentication provider, MFA and local identity lifecycle | Technical + security | 00, 07, 38, 45, 46 |
| Backup frequency, retention, RPO/RTO and support SLA | Operations/commercial | 07, 16, 48 |
| Licence lease duration and grace | Product/commercial | 15, 46, 48 |
| Restricted Mode operation-by-operation matrix | Product + technical + finance | 15, 46, 47 |
| Parent remote projection and freshness | Product + security | 14, 18, 36, 45 |
| Demo TTL, client lease and isolated storage | Product + security + operations | 31, 36, 38, 41, 45 |
| Migration source templates and financial opening method | Migration + finance | 43, 47, 50 |
| English/Urdu/Roman Urdu interface launch scope | Product owner | 01, 49 |
| Final tariff, tax, cancellation and export policy | Commercial + legal | 27, 28, 36, 50 |
| Tenant-specific restore/recovery from shared Cloud PostgreSQL | Operations + DBA/technical + security | 07, 14, 36, 44, 48, 50, 62 |
| Tenant data residency, hosting region, subprocessors and dedicated isolation placement | Security + legal + operations + commercial | 07, 14, 36, 38, 48, 62 |
| Tenant usage metering, quotas, API/sync/AI limits and fair-use/overage policy | Product + commercial + technical/operations | 27, 28, 36, 37, 45, 46, 48, 62 |

Each decision must be recorded in an ADR or approved decision log with context, options, selected outcome, consequences, owner and date.

## 6. Required review and sign-off

1. Product owner approves scope, actors, business rules, workflow and non-goals.
2. Technical lead approves module boundaries, DB design, migrations, API/event contracts and deployment topology.
3. Security lead approves trust boundaries, authorization, RLS, secrets, retention and incident procedures.
4. Finance/domain owner approves fee allocation, accounting, bank reconciliation and approval rules.
5. QA lead approves acceptance coverage, test data and release gates.
6. Operations lead approves provisioning, backup/restore, upgrade, outage and support runbooks.
7. External verification is collected for legal, payment/bank, messaging provider, regulator and curriculum/content requirements.
8. Every unresolved item is closed or explicitly accepted as an implementation blocker with an owner.
9. Product owner and technical lead approve the documentation baseline version.
10. Only after approval may Phase 1 implementation be authorized.

## 7. Final documentation gate

- [ ] Requirements have stable IDs, priority and source.
- [ ] Every core module has reviewed workflows and acceptance tests.
- [ ] Cloud Control Plane and Local Data Plane ERDs are reviewed separately.
- [ ] Field-level dictionary and data classification are reviewed.
- [ ] Migration ownership, constraints, indexes, RLS and recovery are approved.
- [ ] API DTOs, errors, idempotency and event contracts are reviewed.
- [ ] Role/scope matrix and separation of duties are approved.
- [ ] Threat model and authorization tests are approved.
- [ ] UI screen inventory, states, RTL and accessibility are approved.
- [ ] Migration/import/export and finance reconciliation process is approved.
- [ ] Deployment and recovery runbooks are approved per pilot topology.
- [ ] Open decisions are resolved or explicitly marked as blockers.
- [ ] External dependencies are supported by current evidence/contracts.
- [ ] Document links, versions and status labels are consistent.
- [ ] Product, technical, security and QA sign-off is recorded.

**No coding gate:** Phase 1 remains blocked until this checklist is signed off. Approval of a specification does not automatically authorize unrelated phases.


## 8. Supplemental machine-readable requirement register

The root `registers/requirement-traceability.csv` exports the 37 requirement rows in this document 51 matrix into a machine-readable supplement. Its acceptance-test references are copied from this matrix and remain references only; they do not prove that the corresponding test exists or has passed. This CSV is not a substitute for this sign-off matrix.
