# 61 — Reference Project Adoption and Integration Plan

- **Document ID:** DOC-61
- **Status:** Current
- **Decision maturity:** Accepted adoption direction; implementation, module packaging and selected domain policies remain subject to their existing gates
- **Owner:** Product owner (accountable role; named assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS main @ 898812f89e0ec7f03aff6c3b1408e46782814dfd`; `ahmedshahjr/Education-Portal claude/project-thread-kqn559 @ e656701a3ad8e31337e3b8f74920843b1c8dd455`; `a-kabeer/eSchool master @ a38a8e4081157da61628ffa539e1168266f0034d`

## 1. Purpose

This document records which practices and domain findings SchoolOS will adopt from Education-Portal and eSchool, how they are adapted to SchoolOS, and what must not be copied. It is an integration plan for SchoolOS's existing architecture, not a proposal to merge the repositories or replace SchoolOS's existing specifications.

The purpose is to improve engineering discipline and operational completeness while preserving the current SchoolOS direction:

- Modular Monolith for school-business capabilities.
- ASP.NET Core / C# and React + TypeScript.
- Separate Cloud Control Plane and Local Data Plane trust and database boundaries.
- Local-first operational workflows with authenticated synchronization.
- Signed, node-bound offline licensing and Restricted Mode as already specified.
- Human-reviewed AI capabilities, not AI-dependent core operations.

This document does not claim that the practices listed here are already implemented in code.

## 2. Reference evidence and limits

### 2.1 Education-Portal

Reviewed repository: [ahmedshahjr/Education-Portal](https://github.com/ahmedshahjr/Education-Portal), branch `claude/project-thread-kqn559`, pinned at `e656701a3ad8e31337e3b8f74920843b1c8dd455`.

Relevant evidence:

- [AGENTS.md](https://github.com/ahmedshahjr/Education-Portal/blob/e656701a3ad8e31337e3b8f74920843b1c8dd455/AGENTS.md) defines one authoritative contributor/AI-agent guide, engineering rules, security rules and change discipline.
- [Full-stack solution](https://github.com/ahmedshahjr/Education-Portal/tree/e656701a3ad8e31337e3b8f74920843b1c8dd455/Education%20Portal%20Full%20Stack%20App) uses a recognizable Domain / Application / Infrastructure / Web structure with separate unit, functional, integration and acceptance test projects.
- [Architecture decision records](https://github.com/ahmedshahjr/Education-Portal/tree/e656701a3ad8e31337e3b8f74920843b1c8dd455/Education%20Portal%20Full%20Stack%20App/docs/decisions) record security, observability, module catalogue, plans, subscriptions and module enforcement decisions.
- The current solution direction documented in AGENTS.md is .NET 10, PostgreSQL, Clean Architecture and Angular. SchoolOS adopts the general engineering practices, not the Angular frontend or its database topology.
- Its tenant model is a master tenant → school → branch hierarchy with one shared database. That hierarchy and storage model are not automatically applicable to SchoolOS.

### 2.2 eSchool

Reviewed repository: [a-kabeer/eSchool](https://github.com/a-kabeer/eSchool), branch `master`, pinned at `a38a8e4081157da61628ffa539e1168266f0034d`.

Relevant evidence:

- [Reverse-engineering index](https://github.com/a-kabeer/eSchool/tree/a38a8e4081157da61628ffa539e1168266f0034d/docs/reverse-engineering) includes environment/access findings, module inventory, functional specifications, roles/permissions, API inventory, technology analysis, backend gaps, logical database design, modernization recommendations and a functional test matrix.
- [Rebuild blueprint](https://github.com/a-kabeer/eSchool/blob/a38a8e4081157da61628ffa539e1168266f0034d/docs/reverse-engineering/10-rebuild-technical-blueprint.md) documents a proposed modern .NET/React/PostgreSQL rebuild.
- [Frontend routes and workspaces](https://github.com/a-kabeer/eSchool/blob/a38a8e4081157da61628ffa539e1168266f0034d/src/eSchool.Client/src/App.tsx) illustrate the breadth of student, admissions, fee counter, ledger, attendance, exams, payroll, library, inventory and parent/teacher views.
- [API bootstrap](https://github.com/a-kabeer/eSchool/blob/a38a8e4081157da61628ffa539e1168266f0034d/src/eSchool.WebAPI/Program.cs) illustrates background workers, bounded channels, SignalR and tenant-context concepts. These are candidates to assess, not patterns to copy without production hardening.
- The repository's Docker Compose contains development credentials in plaintext and the API has an in-memory fallback when no connection string is configured. Neither is a production pattern to copy.

The reported module/route counts and test pass rate in eSchool documentation are findings from that audit, not a requirement for SchoolOS and not independent proof that a rebuilt system is production-ready.

## 3. SchoolOS decisions that remain invariant

Reference adoption MUST NOT silently change these SchoolOS choices:

1. Keep the modular-monolith business application. Do not split modules into independently deployed microservices for the initial product.
2. Keep React + TypeScript; do not replace it with Education-Portal's Angular frontend.
3. Keep distinct Cloud Control Plane and Local Data Plane databases and trust boundaries. Do not copy Education-Portal's single shared operational database for all schools.
4. Keep Local Data Plane as the authority for its operational records. The Cloud Control Plane receives only approved, purpose-limited projections and synchronization messages.
5. Do not make routine school operations depend on online Cloud Control Plane availability.
6. Do not change the existing signed licence lease, manual `.lic` renewal, grace-period proposal or Restricted Mode policy through this adoption document.
7. Do not silently close open decisions in DOC-60, particularly tenant/school/campus cardinality, PostgreSQL RLS implementation, fees-to-finance posting boundary, accounting policy, data retention, offline receipt semantics or module entitlement rules.
8. Keep AI optional for core operations and preserve the existing privacy, human-review and usage-metering boundaries.

## 4. Practices adopted from Education-Portal

### 4.1 One authoritative contributor and AI-agent guide

**Adopt.** Root `AGENTS.md` is introduced as the single repository-wide instruction source for human developers and AI coding assistants. Tool-specific instruction files, if added later, must be short pointers rather than competing copies.

The guide defines mandatory rules for tenant scope, privacy, secrets, security controls, AI safety, test evidence, documentation authority and pull-request workflow. Its existence does not mean every control is already automated; automation must be tracked and evidenced separately.

### 4.2 Clean boundaries inside each SchoolOS module

**Adopt with adaptation.** Use Domain, Application, Infrastructure and Contracts as logical boundaries inside a module. These can initially be folders within a module project. Split into separate projects only when dependency enforcement, ownership, build performance or module maturity justifies it.

Rules:

- Domain holds entities, value objects, invariants and domain policies; it must not depend on ASP.NET Core, EF Core, React or a host.
- Application holds use cases, authorization requirements, orchestration and ports/interfaces.
- Infrastructure implements persistence, external providers, messaging and file storage.
- Contracts contains the module's intentionally published APIs/events/DTOs.
- Hosts compose modules and expose transport endpoints; controllers/endpoints must not own business rules.
- Another module must not read or write a module's private DbContext, tables or entities.
- Keep BuildingBlocks/SharedKernel small; avoid turning them into a shared domain model.

Do not copy framework-specific choices such as MediatR placement or Aspire as mandatory runtime architecture without a SchoolOS ADR and a concrete need.

### 4.3 Central module catalogue and entitlement enforcement

**Adopt the capability; exact classifications remain open.** SchoolOS should have a central catalogue for module metadata and dependencies, aligned with the existing Subscriptions and Licensing platform modules.

The catalogue should eventually describe:

- Stable module code and display metadata.
- Core or paid/optional classification.
- Dependencies and incompatibilities.
- Availability/retirement status.
- Module contract/schema compatibility.
- Plan and subscription entitlement mapping.
- Local Node feature/entitlement representation.
- Audit history for catalogue and entitlement changes.

Expected enforcement layers:

1. Cloud Control Plane owns the commercial catalogue, plans, subscription state and issuance of signed node-bound entitlements.
2. The signed local licence/entitlement snapshot carries only the claims the Local Node needs to operate within its permitted lease.
3. Local Data Plane validates the signature, node binding, time/lease state, module compatibility and local configuration without contacting Cloud for every request.
4. Application authorization independently checks the signed-in user's permissions, campus/resource scope and record relationship.
5. Restricted Mode policy remains a separate gate that controls allowed operations after expiry/grace.

A role must not grant access to a module that the subscription/licence does not enable. A valid subscription must not grant an individual user permissions they do not have. Disabling an entitlement must preserve existing customer data; it must not delete or corrupt it.

Do not implement a module as paid/core, set its price, or add a module dependency solely because a reference project does so. Those choices require product/commercial approval and, where architecture or licence semantics change, an ADR. Track the unresolved entitlement design in DOC-60 DEC-020.

### 4.4 Tenant isolation and secure scope resolution

**Adopt the security practices; adapt the topology.**

- Derive organization, school, campus and node context on the server from authenticated identity, configured node identity and verified membership.
- Reject unknown or mismatched tenant/host/node contexts; never fall back to a default customer.
- Combine permission, scope, resource relationship and record state.
- Apply application-level authorization, module guards and PostgreSQL controls such as RLS where the approved design requires them.
- Treat RLS as defense in depth; test transaction-local context and pooled-connection isolation.
- Keep platform staff, customer users, parents/students, demo users, support operators and node identities in separate authorization audiences.

Education-Portal's host-to-school resolution and global query filters are useful implementation references, but SchoolOS must preserve its tenant → school/legal entity → campus model pending DEC-001 and its Local Node/database boundary. The exact RLS/DB role design remains open under DEC-004.

### 4.5 Security-by-default contribution workflow

**Adopt.** Every code/config/dependency/CI change must follow root `AGENTS.md`, include appropriate security/privacy review, run tests and be reviewed through a pull request.

CI automation should be introduced incrementally for secret scanning, dependency/vulnerability checks, formatting/analyzers, build/test, documentation validation and selected security checks. Until a workflow exists and a run is evidenced, document it as planned—not as an active control.

Education-Portal's compliance mappings may be used as a control-discovery reference. SchoolOS must not claim certification or legal compliance on the strength of a checklist or workflow.

### 4.6 Test layering and observability

**Adopt.**

- Keep unit and architecture tests and expand with PostgreSQL integration, API/functional, security and UI/E2E tests as modules become implemented.
- Use real PostgreSQL in persistence integration tests; do not let an in-memory provider stand in for PostgreSQL constraints, transactions, RLS, locking or migrations.
- Test Cloud/Local separation, tenant/campus isolation, idempotency, financial posting and offline replay at the relevant integration boundaries.
- Use structured logs, correlation/trace IDs, health/readiness endpoints and OpenTelemetry-compatible traces/metrics where approved.
- Keep personal and child data out of logs and telemetry; review export destinations and retention.

Aspire may be evaluated for local orchestration/test composition, but it is not required to run the product or to preserve the two-plane deployment.

## 5. Functional capabilities adopted from eSchool

eSchool is used as a functional discovery and validation source. SchoolOS's own product/module requirements remain the source of truth.

### 5.1 Core school workflows to validate

The following workflows should be covered by existing SchoolOS module specifications and acceptance tests:

- **Student lifecycle:** inquiry, application, admission decision, person/student profile, guardian links, enrollment, promotion, transfer, withdrawal and historical record preservation.
- **Fee operations:** fee heads/structures, student assignment, invoice/challan generation, partial payment, concessions, payer/household relationships, collection, allocation, receipt, refund/reversal and arrears.
- **Finance:** chart of accounts, journal preparation/approval/posting, balanced debit/credit, general ledger, trial balance, period close, cash closing and reconciliation.
- **Attendance:** daily/period attendance, staff attendance, corrections, absence notifications and offline capture/synchronization.
- **Examinations:** exam setup, assessments, marks entry, validation, moderation, approval, publication and correction history.
- **People and permissions:** staff/teacher/guardian/student roles, campus/class/section scope, relationship access, maker/checker separation and audit.
- **Reporting and portals:** role-specific reports and appropriately scoped parent/teacher/student views.

Many of these are already present in DOC-43, DOC-46, DOC-47 and DOC-49. Adoption means cross-checking coverage and strengthening tests—not creating duplicate entities, routes or specifications.

### 5.2 Financial controls and cashier workflow

**Adopt and enforce through existing Finance/Fees specifications.**

- Use decimal money values with an explicit currency.
- Require each posted journal to balance.
- Preserve payment, receipt, allocation and journal relationships.
- Make posting/retry idempotent; enforce unique provider/source references where available.
- Treat a bank match suggestion as a proposal until authorized confirmation.
- Do not delete posted finance records; use approved reversal/refund/adjustment workflows.
- Apply period locks, approval limits and maker/checker separation.
- Preserve source, actor, timestamp, approval and audit trail.

Fees owns the fee-facing workflow and receipt lifecycle. Finance owns the general ledger and accounting invariants. The exact Fees-to-Finance transaction boundary and posting method remain open under DEC-005; do not implement an unsafe dual-write. Use the approved local transaction and/or Outbox pattern once the decision is closed.

The cashier UX should support fast student/challan lookup, due amount visibility, payment method and allocation review, clear confirmation, receipt reprint/history and duplicate-submit protection. Barcode/QR scanning can be an optional lookup input after hardware and operational evaluation; the workflow must remain usable without a scanner. During offline operation, receipt status must clearly distinguish provisional/pending from confirmed/posted according to DOC-12 and the approved finance policy.

### 5.3 HR/payroll and other discovered modules

eSchool's inventory includes broader capabilities such as HR/payroll, library, inventory and transport. These are **candidate product capabilities**, not automatically accepted SchoolOS scope.

Before creating a module or schema, the product owner must confirm target customers, business workflows, legal/statutory requirements, data ownership, priority, dependency, pricing/entitlement and acceptance tests. Pakistani payroll tax and statutory calculations require current qualified review; do not copy unverified formulas from a legacy audit.

### 5.4 Functional inventory is not a route mandate

The eSchool count of operational modules, submodules/routes or procedural endpoints is a discovery metric for that legacy product. SchoolOS must implement coherent domain capabilities and user journeys, not reproduce the same number of routes or every legacy screen.

## 6. Legacy migration practices adopted from eSchool

**Adopt as a source-discovery method, not as code reuse.**

For each legacy source system:

1. Record the customer authorization and permitted extraction method.
2. Inventory modules, workflows, roles, reports, file formats, APIs, database entities and known backend gaps.
3. Separate directly observed behavior from inferred behavior and unknown behavior.
4. Map source entities and keys to SchoolOS's approved DOC-43/DOC-44 model.
5. Profile and validate source data; detect duplicates and broken relationships.
6. Run a non-mutating dry run with row counts, warnings, rejected rows and financial totals.
7. Import into an isolated test/staging copy and reconcile student/guardian relationships and academic history.
8. Reconcile fee receivables, payments, unallocated amounts, cash/bank balances and control accounts.
9. Obtain customer review/approval before production import.
10. Preserve source identifiers, mapping version, batch ID, checksums, actor, timestamps and recovery evidence.

Do not copy legacy application code, credentials, proprietary screenshots/assets, undocumented business assumptions or database dumps into SchoolOS. Only use customer-authorized data and extraction paths. DOC-50 remains the migration contract and DEC-015 remains the pilot-specific scope/opening-balance gate.

## 7. Integrated SchoolOS module-access model

The effective ability to use a protected capability is the intersection of independent controls:

```text
Module available in this node
    AND subscription entitlement is valid
    AND signed node licence/lease permits it
    AND module version/schema is compatible
    AND user permission is granted
    AND tenant/campus/resource relationship is valid
    AND operation is allowed by record state and Restricted Mode
```

Cloud commercial status is not a per-request dependency for Local Node operations. The Local Node evaluates its verified, node-bound signed entitlement snapshot/lease within the already approved lease policy. User authorization remains local and independent. Expiry/Restricted Mode behavior is governed by DOC-15 and DOC-46, not by hiding modules in the UI alone.

The exact catalogue fields, core/paid split, dependency rules, compatibility policy and lease claims must be closed through DEC-020 before production implementation.

## 8. Cross-document integration map

| Adoption area | Canonical SchoolOS location | Required treatment |
|---|---|---|
| Contributor and AI-agent rules | Root `AGENTS.md`, DOC-54 | One authoritative rule set; tool files are pointers |
| Module-internal Clean Architecture | DOC-03 | Logical layers inside each module; projects only when justified |
| Tenant and node security | DOC-07, DOC-46 | Server-derived scope, defense in depth and separate trust boundaries |
| Catalogue and entitlements | DOC-15, DOC-27, DOC-36, DOC-37, DOC-46, DOC-60 | Cloud catalogue + signed local entitlements; DEC-020 open |
| School domain and data | DOC-02, DOC-43, DOC-44 | eSchool as a coverage cross-check; no unapproved schema expansion |
| Fees and finance | DOC-02, DOC-04, DOC-43–47 | Preserve balanced posting, immutable ledger and approval boundaries |
| Functional acceptance | DOC-47 | Add module/fee/finance scenarios; use synthetic test data |
| Cashier and operational UX | DOC-49 | Fast lookup and receipt workflow; scanner optional |
| Legacy migration | DOC-50 | Authorized source inventory, mapping, dry-run and financial reconciliation |
| Observability and operations | DOC-07, DOC-48, DOC-52 | Structured telemetry and automated checks, with real evidence |
| Documentation and task status | DOC-54, DOC-59, registers | Do not create a parallel task-status source |

## 9. Recommended delivery order

This is an ordering recommendation, not an implementation status register. Approved task IDs and status must be added to the existing canonical CSV task-register partitions before implementation begins.

1. **Engineering guardrails:** use root AGENTS.md; define module contribution, security, PR and test practices.
2. **Foundation vertical slices:** implement real Identity, Organization/Tenant context and Students/Admissions workflows with authorization and database integration tests.
3. **Finance vertical slice:** complete Fee invoice → payment/receipt/allocation → Finance posting/reconciliation with idempotency and audit.
4. **Module catalogue:** approve DEC-020; implement catalogue and entitlement evaluation without making Cloud availability a per-request dependency.
5. **Operational breadth:** attendance, exams, communication and role-specific reporting.
6. **Migration pilot:** test one authorized legacy source through dry-run, reconciliation and signed acceptance.
7. **Candidate modules:** prioritize HR/payroll, library, inventory and transport only after product and legal discovery.
8. **Observability and release gates:** progressively automate secret/dependency scans, architecture/security tests, build/test and operational telemetry.

## 10. Definition of done for an adoption

An adopted practice is not complete because this document references it. Before calling it implemented:

- Its requirement is present in the owning canonical specification.
- Its decision dependencies are approved and open decisions are not bypassed.
- Its task is in the canonical task register with an owner, dependencies and acceptance criteria.
- Code/configuration exists in the intended module/host boundary.
- Unit and relevant integration/security/acceptance tests pass and are linked.
- Documentation and navigation are updated.
- Operational/security consequences and rollback are reviewed.
- The implementation status is supported by a commit, PR, test run or operational evidence.

Until then, it remains a documented adoption direction.

## 11. Non-adoptions and explicit exclusions

SchoolOS will not copy:

- Education-Portal's Angular frontend or one-shared-operational-database topology.
- A master-tenant/school/branch cardinality without resolving SchoolOS DEC-001.
- Microservices or a mandatory Aspire runtime merely because a reference repository uses them.
- Legacy PHP/jQuery code or its screen count as the implementation plan.
- Hardcoded Compose credentials, production use of an in-memory database fallback, or secrets in repository configuration.
- Every legacy feature/module without product prioritization.
- Compliance claims based on control mappings, checklists or automated scans alone.

## 12. Review note

This is a controlled adoption baseline. It documents source-inspired engineering and domain practices while retaining SchoolOS's existing product decisions and open-decision gates. Any later implementation or architecture change must be reviewed against DOC-54 and, where required, recorded in an ADR.
