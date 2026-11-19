# 47 — Workflow and Acceptance Test Catalogue

- **Document ID:** DOC-47
- **Status:** Draft
- **Decision maturity:** Proposed acceptance-test catalogue; execution evidence not implied
- **Owner:** QA lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ 898812f89e0ec7f03aff6c3b1408e46782814dfd; reference sources pinned in DOC-61
**Version:** v1.8 draft · **Status:** Proposed acceptance baseline; pilot-specific values and policies require approval  
**Companion:** docs/02_MODULES_AND_WORKFLOWS.md, docs/08_DEVELOPMENT_ROADMAP.md, docs/45_API_AND_INTEGRATION_EVENT_CONTRACTS.md, docs/46_AUTHORIZATION_AND_TENANT_ISOLATION_MATRIX.md

## 1. Purpose

This catalogue turns the product workflow documents into testable acceptance outcomes. Each test should be automated at the lowest useful level (unit, integration, API, security or E2E). Tests involving permissions, finance, sync and licensing require persistence-level evidence, not only UI assertions.

Priority:
- **P0:** Must pass before first operational pilot.
- **P1:** Must pass before public production launch.
- **P2:** Later module or advanced capability.

Test data must be synthetic. Never use real student/guardian records in CI or public demo environments.

## Architecture, schema and identity boundaries

### ARC-001 — Modular Monolith dependency boundaries (P0)

**Given** the solution and module projects.  
**When** architecture tests inspect project references and module dependencies.  
**Then** module dependency rules are enforced, prohibited references are rejected, and violations are reported.

### DB-001 — Cloud/local database separation (P0)

**Given** the Cloud Control Plane and a Local Data Plane deployment.  
**When** database schemas, migrations and connection configuration are reviewed.  
**Then** platform/commercial data and campus operational data use their approved database boundaries, with no cross-database foreign key or shared operational store.

### ID-001 — Person and login identity remain distinct (P0)

**Given** one person who may hold more than one school role and a separate credentialed login identity.  
**When** the person or login is created, linked, disabled or merged.  
**Then** person data and credentials remain separate, and disabling a login does not delete historical school records.

## 2. Admission-to-enrollment

### ADM-001 — Complete approved admission (P0)

**Given** an AdmissionsOfficer with assigned campus scope and an application in Submitted status.  
**When** the officer records review checks and the authorized approver accepts the application.  
**Then** the application moves through the approved state transitions, the decision actor/time/reason are audited, an appropriate Person/Student record is linked or created exactly once, guardian links are recorded, and an enrollment is created for the selected academic year/campus/grade/section.

### ADM-002 — Duplicate person detection (P0)

**Given** an applicant whose person/contact details may already exist.  
**When** a new application is submitted.  
**Then** the system presents authorized possible matches using approved matching rules; it does not silently merge people or create duplicate identity records without review.

### ADM-003 — Section optionality (P0)

**Given** a grade level where sections are not configured.  
**When** a student is enrolled in that grade.  
**Then** enrollment succeeds with nullable section assignment and no fake section is created.

### ADM-004 — Historical promotion (P0)

**Given** a student enrolled in Class 5 during 2025–26.  
**When** the student is promoted to Class 6 in 2026–27.  
**Then** the previous enrollment remains unchanged, a new enrollment is created, and historical reports still show the original campus/class/section.

### ADM-005 — Unauthorized admission decision (P0)

**Given** a user without application approval permission.  
**When** the user calls the accept/reject endpoint directly.  
**Then** the API rejects the request, makes no state change and records a security-relevant denial where policy requires.

## 3. Guardian, family and sibling access

### FAM-001 — Guardian linked to multiple students (P0)

**Given** one verified guardian linked to two siblings in the same household.  
**When** the guardian opens the Parent Portal/local parent view.  
**Then** only the linked students appear and each student's academic/fee information remains individually scoped.

### FAM-002 — Relationship permission separation (P0)

**Given** a person marked as emergency contact but not authorized for financial notices.  
**When** that person attempts to read a fee statement.  
**Then** the request is denied even though the emergency-contact relationship exists.

### FAM-003 — Prevent surname-based family merging (P0)

**Given** two unrelated students share a surname or phone number.  
**When** family matching is performed.  
**Then** the system does not combine them without an explicit verified household/guardian relationship.

## 4. Fees, partial payment and sibling allocation

### FEE-001 — Generate invoice from approved fee structure (P0)

**Given** an active enrollment and a fee structure effective for the relevant campus/class/year.  
**When** an authorized user generates an invoice batch.  
**Then** invoice lines use the correct effective fee heads/amounts, invoice totals reconcile, due dates are correct, and duplicate generation is prevented by the documented batch/idempotency rule.

### FEE-002 — Partial payment (P0)

**Given** an invoice of PKR 10,000 with no payment.  
**When** an authorized cashier records PKR 4,000 and allocates it to the invoice.  
**Then** the payment and allocation are persisted, remaining outstanding is PKR 6,000, the receipt state is correct, and a retry with the same idempotency key does not create a second payment.

### FEE-003 — Sibling consolidated payment (P0)

**Given** two siblings with verified household membership and separate invoices.  
**When** their guardian makes one PKR 30,000 payment and an authorized accountant allocates PKR 15,000 to each child's invoice.  
**Then** one source payment remains traceable, both allocations are recorded, total allocations equal PKR 30,000, both child ledgers update correctly, and household reporting shows the correct combined balance.

### FEE-004 — Allocation cannot exceed payment (P0)

**Given** an unallocated payment balance of PKR 5,000.  
**When** a user attempts to allocate PKR 5,500.  
**Then** the operation fails with a stable business-rule error and no partial allocation is posted.

### FEE-005 — Invoice cannot be over-allocated (P0)

**Given** an invoice has PKR 2,000 outstanding.  
**When** a payment allocation of PKR 2,500 is requested.  
**Then** the operation is rejected unless an explicitly approved overpayment/advance-credit policy is active; the system does not silently write a negative outstanding amount.

### FEE-006 — Provisional offline cash receipt (P0)

**Given** an authorized cashier on a registered Local Node with cloud internet unavailable.  
**When** the cashier records a permitted cash collection offline.  
**Then** the receipt has a unique local identifier and clearly displays Provisional/Pending Sync; it is not labelled bank-confirmed or centrally confirmed before server validation.

### FEE-007 — Rejected provisional receipt reconciliation (P1)

**Given** an offline provisional receipt later fails server validation or duplicates an existing transaction.  
**When** the Local Node processes the queued event.  
**Then** the record becomes an exception requiring authorized review; no duplicate ledger posting occurs and the original receipt evidence remains auditable.

### FEE-008 — Refund maker/checker (P0)

**Given** a user requests a refund.  
**When** the same user attempts to approve it where separation of duties is required.  
**Then** approval is denied and the request remains pending for a different authorized approver.

## 5. Finance and accounting controls

### FIN-001 — Balanced journal posting (P0)

**Given** a journal draft with debit total equal to credit total for the same currency/accounting scope.  
**When** an authorized approver posts it.  
**Then** the journal becomes immutable and all lines, posting actor/time and source references are retained.

### FIN-002 — Reject unbalanced journal (P0)

**Given** a journal with total debit different from total credit.  
**When** posting is attempted.  
**Then** posting is rejected atomically and no ledger balance changes.

### FIN-003 — Posted journal immutability (P0)

**Given** a posted journal.  
**When** an ordinary user attempts to update/delete a line.  
**Then** the operation is denied; correction requires a linked reversal/adjustment with reason and authorization.

### FIN-004 — Currency precision (P0)

**Given** financial values with fractional currency amounts.  
**When** amounts are stored/calculated/reported.  
**Then** fixed-precision decimal rules are used, currency is explicit, and no binary floating-point rounding is used for monetary totals.

### FIN-005 — Restricted financial period (P1)

**Given** a closed accounting period.  
**When** a user attempts a posting dated into that period.  
**Then** posting is rejected unless an authorized reopen process records approver, reason and audit history.

## 6. Bank statement reconciliation

### REC-001 — Import supported bank statement (P0)

**Given** an approved bank statement format with valid headers and rows.  
**When** an Accountant uploads the statement.  
**Then** the parser version, source file hash, account, period, row count and import actor are saved and a batch summary is returned.

### REC-002 — Duplicate file import (P0)

**Given** a bank statement file already imported for the same bank/account/period under the duplicate policy.  
**When** the same file is uploaded again.  
**Then** the system detects the duplicate and does not create duplicate bank transactions.

### REC-003 — Match suggestion is not final settlement (P0)

**Given** a bank credit resembles a fee invoice/payment.  
**When** the matching engine suggests a candidate.  
**Then** the suggestion is visibly Pending Review and does not allocate a fee or post accounting entries until an authorized user confirms.

### REC-004 — Partial bank match (P1)

**Given** a statement transaction covering multiple invoice allocations or only part of a payment.  
**When** the Accountant confirms a valid split.  
**Then** source amount equals confirmed allocation total plus explicitly recorded unmatched remainder; every allocation references the same original bank transaction.

### REC-005 — Unmatched transaction workflow (P0)

**Given** a bank row with no reliable candidate.  
**When** matching completes.  
**Then** it remains in an unmatched queue with an exception reason; the system does not guess a student.

### REC-006 — Reconciliation close gate (P1)

**Given** a reconciliation session containing matched and unmatched rows.  
**When** the Accountant attempts to close it.  
**Then** the system requires all rows to be accounted for under an approved close policy and records totals, reviewer and exceptions.

## 7. Attendance and offline synchronization

### OFF-001 — Online attendance (P0)

**Given** a teacher assigned to Class 6-A.  
**When** the teacher records daily attendance.  
**Then** only assigned students can be marked, session/date constraints are enforced, and the operation is audited.

### OFF-002 — Offline capture (P0)

**Given** the Local Node is available on school LAN but the internet connection to cloud is down.  
**When** the teacher submits attendance.  
**Then** the Local Node persists the record and the user receives a local server acknowledgement; no cloud round trip is required.

### OFF-003 — Browser pending operation (P0)

**Given** a browser draft/operation is queued locally and the server has not acknowledged it.  
**When** the user views sync status.  
**Then** it displays Pending Sync, not Synced.

### OFF-004 — Duplicate operation retry (P0)

**Given** the same logical attendance operation is submitted three times with the same operation ID.  
**When** the API processes the retries.  
**Then** one business effect is persisted and the same acknowledgement/result is returned.

### OFF-005 — Conflicting attendance edits (P0)

**Given** two authorized devices edit the same attendance record from the same base version.  
**When** both changes synchronize.  
**Then** the configured concurrency/conflict rule is applied; the system does not silently use last-write-wins where review is required.

### OFF-006 — Rejected sync visibility (P0)

**Given** an offline operation is rejected due to invalid scope or state.  
**When** the client receives the response.  
**Then** it displays a reviewable error, preserves the operation evidence and does not mark it Synced.

### OFF-007 — Pending queue after restart (P0)

**Given** operations are queued and the browser/device closes unexpectedly.  
**When** the app restarts and the Local Node is reachable.  
**Then** pending work can be resumed/reviewed and no queued operation disappears silently.

### OFF-008 — Device revocation (P1)

**Given** a lost device has been revoked.  
**When** it reconnects and submits queued operations.  
**Then** the server rejects the revoked device identity and logs the attempt.

## 8. Examination and result integrity

### EXM-001 — Marks validation (P0)

**Given** an assessment with maximum marks 50.  
**When** a teacher enters a mark of 55 without an approved bonus policy.  
**Then** the mark is rejected with a validation error.

### EXM-002 — Absence/exemption consistency (P0)

**Given** a student is marked absent or exempt.  
**When** the marks payload also includes an incompatible numeric score under the approved rule.  
**Then** validation rejects the inconsistent state.

### EXM-003 — Draft marks versus published result (P0)

**Given** marks are entered but not approved.  
**When** a parent requests the report card.  
**Then** unpublished results are not exposed.

### EXM-004 — Publish immutable result snapshot (P0)

**Given** all required assessments and approvals are complete.  
**When** ExamController publishes results.  
**Then** a snapshot is saved with grading-scheme version, publisher, time, revision and integrity hash; later mark edits cannot silently change it.

### EXM-005 — Correct published result (P1)

**Given** a published result contains an approved error.  
**When** an authorized correction is approved.  
**Then** a new revision is created with reason and approval history; the previous revision remains traceable.

### EXM-006 — Cross-board grading (P1)

**Given** two curricula use different grading schemes.  
**When** results are calculated.  
**Then** the applicable scheme version is selected by school/qualification/effective date; no UI hard-coded grade mapping is used.

## 9. Tenant, campus and relationship security

### SEC-001 — Cross-tenant resource IDOR (P0)

**Given** a user in Tenant A knows a Tenant B resource UUID.  
**When** the user calls the API directly.  
**Then** no protected data is returned and no mutation occurs.

### SEC-002 — Campus scope (P0)

**Given** a Cashier assigned to Campus A.  
**When** the cashier requests Campus B fee records by changing a query parameter.  
**Then** the request is denied or safely hidden according to anti-enumeration policy.

### SEC-003 — Teacher class scope (P0)

**Given** a teacher assigned only to Section 6-A.  
**When** the teacher requests Section 6-B attendance or marks.  
**Then** the server rejects the access.

### SEC-004 — Parent child scope (P0)

**Given** a guardian linked to Student A only.  
**When** the guardian changes a route ID to Student B.  
**Then** Student B data is not returned.

### SEC-005 — RLS connection-pool isolation (P0)

**Given** a pooled DB connection previously served Tenant A.  
**When** it is reused for Tenant B.  
**Then** transaction-local scope is reset and Tenant A context cannot leak.

### SEC-006 — Export scope (P0)

**Given** a user permitted to export one campus.  
**When** the user requests an organization-wide export.  
**Then** only authorized campus rows are included and counts/files do not leak unauthorized data.

## 10. Hybrid deployment, licence and data exchange

### HYB-001 — Local operation during cloud outage (P0)

**Given** a valid Local Node with an active signed lease and working LAN.  
**When** Cloud Control Plane becomes unreachable.  
**Then** approved local authentication and school operations continue; cloud-only functions visibly report unavailable/stale state.

### HYB-002 — Outbox after committed transaction (P0)

**Given** a local business operation emits an integration event.  
**When** the database transaction commits.  
**Then** business data and Outbox event are both committed; neither can persist without the other.

### HYB-003 — Duplicate cloud event delivery (P0)

**Given** a node event is delivered more than once.  
**When** the consumer processes it.  
**Then** one business effect occurs and duplicate delivery is acknowledged idempotently.

### HYB-004 — Manual signed data package import (P1)

**Given** a package from a registered campus node.  
**When** an authorized operator uploads it.  
**Then** signature, recipient, tenant, schema version, checksum and duplicate package ID are verified before any data is applied.

### HYB-005 — Dry-run and import approval (P1)

**Given** a valid package containing creates, updates, rejects and conflicts.  
**When** dry-run is requested.  
**Then** the system displays counts and record-level errors without changing operational data; a separate authorized approval is required to apply it.

### HYB-006 — Imported branch data is read-only (P1)

**Given** branch-owned records imported into the head-office reporting replica.  
**When** a head-office user attempts a direct edit.  
**Then** the write is blocked; any approved correction uses the defined command/round-trip workflow.

### LIC-001 — Valid automatic lease renewal (P0)

**Given** a connected Local Node and a verified paid entitlement.  
**When** the node obtains a newly signed lease.  
**Then** signature, node binding, tenant, sequence and validity are checked before atomic activation.

### LIC-002 — Manual .lic renewal (P0)

**Given** a disconnected node with a legitimate renewal file.  
**When** an authorized administrator imports the file.  
**Then** the same validator/activation pipeline as automatic renewal is used; no software update is required.

### LIC-003 — Reject tampered/wrong-node/replay file (P0)

**Given** a modified, wrong-node, expired or old-sequence licence file.  
**When** imported.  
**Then** it is rejected, the active valid licence state is not corrupted, and the attempt is audited.

### LIC-004 — Restricted Mode after approved grace (P0)

**Given** an expired lease beyond the approved grace boundary.  
**When** a user attempts blocked operational writes.  
**Then** they are denied while approved historical read/export, backup, renewal and support actions remain available.

### DEM-001 — Demo session isolation (P1)

**Given** two independent ephemeral demo sessions.  
**When** each modifies synthetic records.  
**Then** neither session can read or change the other's data, including via guessed IDs, exports, background jobs or cache keys.

### DEM-002 — Demo logout/reconnect/cleanup (P1)

**Given** a demo with multiple tabs.  
**When** one tab closes, another refreshes, then the user explicitly logs out.  
**Then** refresh/multi-tab continuity works before logout; logout revokes access immediately and starts idempotent cleanup; old tokens cannot read the workspace.

## 11. Privileged access, AI safety and migration

### SEC-007 — Privileged action requires MFA/step-up (P0)

**Given** a staff account assigned a privileged role.  
**When** the user performs a configured high-impact action without satisfying required MFA/step-up.  
**Then** the action is denied, no state changes occur, and the denial is auditable without logging secrets.

### AI-001 — Provider route respects data classification (P1)

**Given** an AI request with a classified payload and tenant routing policy.  
**When** the Provider Gateway selects a provider/runtime.  
**Then** only a route approved for that data class, region and contract is selected.

### AI-002 — RAG enforces tenant, campus and document ACLs (P1)

**Given** documents belonging to different tenants, campuses and user ACLs.  
**When** a user performs retrieval.  
**Then** filtering occurs before model context construction and no unauthorized chunk, citation or cache entry is exposed.

### AI-003 — Tool calls use allow-listed application services (P1)

**Given** a model-proposed tool call with typed arguments.  
**When** the server validates and executes it.  
**Then** only an allow-listed tool runs through its owning application service; arbitrary SQL, code execution and client-supplied tenant authority are rejected.

### AI-004 — Consequential AI actions require human approval (P1)

**Given** an AI-generated mark, financial recommendation or consequential notification draft.  
**When** the model proposes an action.  
**Then** no official posting, mark publication or message delivery occurs without the owning module's normal human approval workflow.

### AI-005 — AI feature release has evaluated quality evidence (P1)

**Given** a versioned AI feature/model/prompt and an approved evaluation set.  
**When** a release candidate is evaluated.  
**Then** quality, safety, language coverage, false-positive and cost results are recorded, and a failed gate blocks release.

### MIG-001 — Import dry-run does not change live records (P1)

**Given** a school migration file with valid and invalid rows.  
**When** an authorized operator runs validation/dry-run.  
**Then** row errors, duplicates, unmapped values and totals are reported without modifying operational records.

### MIG-002 — Repeating an approved import is idempotent (P1)

**Given** a previously applied import batch and stable source keys.  
**When** the same logical batch is retried.  
**Then** no duplicate student, relationship, invoice, payment or event is created.

### MIG-003 — Financial migration reconciles to source evidence (P1)

**Given** source balances, payment history and an approved opening-balance policy.  
**When** finance migration is reconciled.  
**Then** invoice, payment, allocation, receivable and control totals reconcile or have approved exceptions.

### MIG-004 — Failed import can resume or recover safely (P1)

**Given** an import that fails after a partial batch.  
**When** the operator resumes or invokes the approved recovery path.  
**Then** checkpoints and idempotency prevent duplicate effects; posted financial history uses reversal/adjustment entries.

### MIG-005 — Customer export preserves relationships and scope (P1)

**Given** a user authorized for a defined school/campus scope.  
**When** a portability export is requested.  
**Then** only authorized records are included, IDs and relationships are preserved, a versioned manifest is supplied, and secrets are excluded.

## 12. Backups, privacy, accessibility and performance

### OPS-001 — Restore drill (P0 before pilot)

**Given** an encrypted backup of database and private files.  
**When** restored onto replacement hardware using the runbook.  
**Then** data integrity checks pass, Node identity/licence recovery follows policy, and the measured RPO/RTO are recorded.

### OPS-002 — Backup encryption and secrets (P0)

**Given** generated backup artifacts.  
**When** stored or transferred.  
**Then** encryption and key separation requirements are met; no production secrets are committed to the repository.

### OPS-003 — Audit completeness (P0)

**Given** an authorized sensitive operation.  
**When** committed.  
**Then** actor, scope, resource, action, timestamp, reason, correlation and outcome are recorded without logging credentials or unnecessary child information.

### UX-001 — Keyboard/accessibility (P1)

**Given** a user operating the core forms with keyboard and screen reader.  
**When** navigating fields, validation and dialogs.  
**Then** controls have labels, focus order, visible focus and accessible error summaries.

### UX-002 — Urdu RTL (P1)

**Given** Urdu language selected.  
**When** user views forms, navigation, tables, validation and generated documents.  
**Then** right-to-left direction, mixed Urdu/English/numeric content, fonts and alignment are usable and remain readable.

### PERF-001 — Performance baseline (P1)

**Given** the approved pilot dataset and target hardware.  
**When** representative admissions, attendance, fee search and reports are load-tested.  
**Then** p95 latency, throughput, DB load and cold-start are measured and compared with approved targets. Proposed targets are not treated as achieved until measurements exist.

### PERF-002 — Result-day workload (P1)

**Given** representative exam/results peak load.  
**When** report cards are generated and parents read results.  
**Then** queues, database capacity and application responsiveness remain within approved operational thresholds.

## 13. Release acceptance gates

### First pilot (minimum P0)
- No unresolved critical/high tenant isolation finding.
- Admission-to-enrollment, fees/payment, balanced journal, statement import/match, attendance and result publication pass their P0 tests.
- Local operation continues during cloud outage within the documented license policy.
- Backup/restore drill passes.
- Audit and support access controls operate.
- No duplicate financial or sync side effects under retry/replay tests.

### Public production (P0 + P1)
- Security, performance, accessibility and RTL tests pass.
- Parent/guardian access is relationship-scoped.
- Data export, retention and termination workflows are approved.
- External payment/WhatsApp/bank integrations have provider contract and end-to-end evidence.
- Operations/support staffing and escalation are ready.
- Legal/privacy review is completed.

## 14. Evidence recording

For each accepted test, keep:
- Test ID and specification version.
- Build/commit SHA.
- Environment and database migration version.
- Synthetic test data version.
- Execution timestamp.
- Pass/fail and logs/artifact references.
- Linked bug/PR.
- Reviewer/approver.

A passing test on a developer laptop is not by itself production readiness evidence.


## 15. Reference-adoption acceptance scenarios

These scenarios supplement the existing workflow catalogue. They are proposed acceptance requirements, not evidence that the scenarios have been implemented or executed. They use synthetic data.

### MOD-001 — Entitlement and user authorization are independent (P0)

**Given** a Local Node has a valid signed entitlement for a module, but the signed-in user lacks the relevant permission.  
**When** the user calls the module API directly.  
**Then** the request is denied, no data changes, and the denial is auditable according to policy.

### MOD-002 — User role cannot enable an unentitled module (P0)

**Given** a user has a role containing a permission for a module, but the node's verified subscription/licence entitlement does not include that module.  
**When** the user accesses the module API while Cloud is unavailable.  
**Then** the Local Node denies the operation using its verified local entitlement state; no live Cloud call is required and no customer data is deleted.

### MOD-003 — Module dependency and compatibility gate (P1)

**Given** a module depends on another module or requires a newer compatible contract/schema version.  
**When** the catalogue/lease configuration is activated with the dependency missing or incompatible.  
**Then** activation is rejected or the capability is safely unavailable with an explicit diagnostic; no silent partial operation is allowed.

### FEE-009 — Concurrent or retried cashier payment is idempotent (P0)

**Given** the same payment instruction is submitted twice because of a double click, retry or concurrent cashier request using the same idempotency/source key.  
**When** the Local Node processes the requests.  
**Then** only one payment/receipt and one corresponding accounting effect are created; subsequent attempts return the existing result or a safe duplicate response.

### FIN-006 — Fee receipt is traceable to finance posting (P0)

**Given** a fee payment is accepted and reaches its approved confirmed/posted state.  
**When** the finance integration completes.  
**Then** the receipt, payment allocation, source operation and balanced journal can be traced to each other; retry cannot create a second journal; any failure is visible and recoverable without an unsafe dual-write.

The exact transaction/posting boundary remains governed by DEC-005. Do not mark FIN-006 passed until the chosen contract and persistence-level tests are approved.

Link these scenarios to the canonical task-register IDs before implementation. Do not create a separate Markdown task-status tracker.

## 16. SaaS tenant foundation acceptance scenarios

The detailed implementation contract and full scenario descriptions are maintained in [DOC-62](62_SAAS_TENANT_IMPLEMENTATION_BLUEPRINT.md). These scenarios are required for the SaaS tenant foundation and must be added to the canonical requirement-to-task-to-test traceability workflow. They are not considered passed until reproducible automated evidence is linked.

### SAAS-001 — Registration provisions one tenant idempotently (P0)

**Given** an approved, verified registration and a stable idempotency key.  
**When** the provisioning command is retried after a timeout or worker restart.  
**Then** exactly one Organization, School, Campus, owner membership and subscription exist.

### SAAS-002 — Organization hierarchy cannot cross tenants (P0)

**Given** two tenants with separate Schools and Campuses.  
**When** an API or SQL write attaches one tenant's Campus to another tenant's School or Node.  
**Then** application validation and composite foreign keys reject the relation.

### SAAS-003 — Tenant switch requires an active membership (P0)

**Given** a user belongs to Tenant A but not Tenant B.  
**When** the user attempts to switch to Tenant B.  
**Then** no Tenant B session or data is returned.

### SAAS-004 — Route/host and authenticated tenant mismatch is denied (P0)

**Given** a valid Tenant A session.  
**When** the caller changes the route or host to Tenant B.  
**Then** the request is denied before tenant-owned data is read.

### SAAS-005 — Missing tenant context fails closed (P0)

**Given** a tenant-owned table and the restricted Cloud runtime role.  
**When** a query or write runs without transaction-local tenant context.  
**Then** no tenant rows are readable and writes are rejected.

### SAAS-006 — RLS and connection-pool context do not leak (P0)

**Given** pooled connections are used for Tenant A and then Tenant B.  
**When** a prior transaction commits, rolls back or throws.  
**Then** the next tenant transaction sees only its own rows.

### SAAS-007 — Tenant-owned foreign keys are tenant-consistent (P0)

**Given** globally unique IDs belonging to different tenants.  
**When** a cross-tenant parent/child relationship is attempted.  
**Then** application validation and database constraints reject it.

### SAAS-008 — Node enrollment is tenant-bound and single-use (P0)

**Given** an enrollment challenge issued for Tenant A.  
**When** it is expired, replayed, used without proof of key possession or presented for Tenant B.  
**Then** enrollment and lease issuance are denied.

### SAAS-009 — A Local Node cannot host a second tenant (P0)

**Given** a Local Node already enrolled for Tenant A.  
**When** configuration or an API attempts to rebind it to Tenant B.  
**Then** readiness/enrollment fails until the old node is formally decommissioned and re-enrolled.

### SAAS-010 — Entitlement and user authorization are independent (P0)

**Given** either the tenant lacks a module entitlement or the user lacks its permission.  
**When** the module API is called.  
**Then** access is denied regardless of frontend visibility.

### SAAS-011 — Local operations survive Cloud outage within valid lease (P0)

**Given** a node has a valid signed lease and synchronized local membership.  
**When** Cloud connectivity is lost.  
**Then** approved local operations continue and synchronization is queued locally.

### SAAS-012 — Sync rejects wrong-tenant or wrong-campus node events (P0)

**Given** a node credential is bound to Tenant A and Campus A1.  
**When** it submits an event for Tenant B or unassigned Campus B1.  
**Then** Cloud rejects the event before routing or projecting its payload.

### SAAS-013 — Tenant export and offboarding remain tenant-scoped (P1)

**Given** an authorized tenant owner requests export and closure.  
**When** export and decommission workflows execute.  
**Then** only that tenant's data is exported, credentials are revoked and retention/legal-hold rules are applied.

### SAAS-014 — Platform support cannot become permanent tenant access (P0)

**Given** a customer-approved, time-limited support grant.  
**When** the grant expires or is revoked.  
**Then** the support agent can no longer access tenant records through the grant or a cached session.

Existing SEC-001 and SEC-005 remain required security tests; SAAS-004 through SAAS-007 deepen the explicit SaaS context and database acceptance contract. Existing LIC and HYB scenarios remain mandatory for licence and offline behavior.
