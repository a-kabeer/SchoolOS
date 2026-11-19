# 51 — Requirements Traceability and Documentation Sign-off

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

| ID | Requirement | Existing source | v1.8 specification | Acceptance tests | Status |
|---|---|---|---|---|---|
| PLAT-001 | .NET Modular Monolith | 00, 03 | 42 | Architecture tests | Documented |
| PLAT-002 | Cloud Control Plane + Local Data Plane | 14, ADR-011 | 44, 45, 48 | HYB-001 | Confirmed direction |
| PLAT-003 | Separate platform and operational DBs | 14, 36 | 44 | SEC-001, HYB-001 | Confirmed direction |
| TEN-001 | Tenant → School → Campus hierarchy | 00, 04 | 43, 44 | SEC-001, SEC-002 | Open exact cardinality |
| TEN-002 | Server-derived scope and tenant isolation | 07, 38 | 45, 46 | SEC-001 to SEC-006 | Documented |
| ID-001 | Separate Person from User credentials | 04 | 43 | ADM-002 | Proposed |
| ID-002 | Permission + scope + resource relationship | 07, 38 | 46 | SEC-001 to SEC-006 | Documented |
| ADM-001 | Admission-to-enrollment workflow | 02, 08 | 43, 45, 47 | ADM-001 to ADM-005 | Documented |
| STU-001 | Family/guardian/sibling links | 02, 04 | 43, 46 | FAM-001 to FAM-003 | Documented |
| STU-002 | Preserve enrollment history | 02, 04 | 43, 44 | ADM-004 | Documented |
| FEE-001 | Fee heads, fee structures and invoices | 02, 04 | 43, 45 | FEE-001 | Documented |
| FEE-002 | Partial payments and payment allocations | 02, 04 | 43, 45 | FEE-002, FEE-004, FEE-005 | Documented |
| FEE-003 | Sibling consolidated payment | 02, 04 | 43, 45 | FEE-003 | Documented |
| FEE-004 | Provisional offline cash receipt | 05, 14 | 45, 48 | FEE-006, FEE-007 | Documented |
| FIN-001 | Double-entry, balanced journals | 04, 07 | 43, 44 | FIN-001, FIN-002 | Documented |
| FIN-002 | Posted journal immutability | 02, 04 | 43, 46 | FIN-003 | Documented |
| REC-001 | Bank statement import with provenance | 02, 04 | 43, 45, 50 | REC-001, REC-002 | Documented |
| REC-002 | Match suggestion is not settlement | 02, 12 | 43, 45 | REC-003 to REC-006 | Documented |
| ACA-001 | Versioned board/curriculum/grading | 01, 04 | 43 | EXM-006 | External content rights open |
| ATT-001 | Offline attendance on Local Node | 05, 14 | 45, 48 | OFF-001, OFF-002 | Confirmed direction |
| ATT-002 | Idempotent sync and conflict review | 05, ADR-013 | 45 | OFF-003 to OFF-008, HYB-002, HYB-003 | Documented |
| EXM-001 | Marks limits and absence/exemption rules | 02, 04 | 43, 45 | EXM-001, EXM-002 | Documented |
| EXM-002 | Approval and immutable published results | 02, 04 | 43, 45, 46 | EXM-003 to EXM-005 | Documented |
| SEC-001 | Default-deny/resource authorization | 07, 38 | 46 | SEC-001 to SEC-006 | Documented |
| SEC-002 | Separate public/demo/portal/admin identities | 38, 41 | 45, 46 | DEM-001, DEM-002 | Documented |
| SEC-003 | MFA/step-up for privileged actions | 38 | 46 | Privileged-action tests | Policy approval pending |
| SEC-004 | Audit sensitive actions and exports | 07, 38 | 43, 46 | OPS-003 | Documented |
| HYB-001 | Local operation during cloud outage | 14, 05 | 45, 48 | HYB-001 | Confirmed direction |
| LIC-001 | Automatic and manual signed .lic renewal | 15, ADR-012 | 45, 48 | LIC-001 to LIC-003 | Policy confirmed |
| LIC-002 | Restricted Mode preserves read/export/backup/renewal | 15, 11 | 46, 48 | LIC-004 | Operation matrix open |
| HYB-002 | Signed/encrypted campus data exchange | 18, ADR-014 | 45, 48, 50 | HYB-004 to HYB-006 | Documented |
| BCP-001 | Control Plane owns BCP and commercial data | 29, 36 | 42, 44, 45 | SEC tests | Documented |
| BCP-002 | Ephemeral synthetic demo | 41, ADR-021 | 45, 46 | DEM-001, DEM-002 | TTL/storage open |
| AI-001 | Privacy-aware AI provider gateway and RAG | 20–26 | Existing AI docs | AI evaluation/security plan | Implementation Not Started |
| MIG-001 | Validated, repeatable school data migration | 08, 18 | 43, 50 | Migration acceptance suite | Draft |
| UX-001 | Role-aware responsive UI and Urdu RTL | 01, 07 | 49 | UX-001, UX-002 | Language scope open |

## 3. New v1.8 documentation inventory

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

Existing docs 00–41 remain the source decision and requirements baseline. The new documents supplement them; they do not silently supersede ADRs or mark implementation tasks complete.

## 4. Open decisions requiring explicit approval

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

Each decision must be recorded in an ADR or approved decision log with context, options, selected outcome, consequences, owner and date.

## 5. Required review and sign-off

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

## 6. Final documentation gate

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


## 7. Supplemental machine-readable requirement register

The root `registers/requirement-traceability.csv` exports the 36 requirement rows in this document 51 matrix into a machine-readable supplement. Its acceptance-test references are copied from this matrix and remain references only; they do not prove that the corresponding test exists or has passed. This CSV is not a substitute for this sign-off matrix.
