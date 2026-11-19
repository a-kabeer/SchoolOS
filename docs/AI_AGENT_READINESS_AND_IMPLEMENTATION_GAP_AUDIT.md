# SchoolOS AI-Agent Readiness & Complete Implementation Gap Audit

- **Document ID:** AUDIT-READINESS-001
- **Status:** Current audit report — owner acceptance pending
- **Decision maturity:** Evidence-based implementation assessment; does not approve open product or architecture decisions
- **Owner:** Technical lead (named audit reviewer TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `3cc2a02342f60af124e04aeb86563c331b497a1f`
- **Audit branch:** `audit/ai-agent-readiness-gap-audit`
- **Audit mode:** Read-only repository inspection; no runtime, schema, dependency or infrastructure implementation changes

## 1. Executive assessment

**Overall assessment: NOT READY for product implementation beyond controlled foundation slices; NOT READY for a customer pilot or SaaS production.**

At the audited main commit the repository is a Phase 0 scaffold, not an implementation of the SchoolOS product objectives. The source tree has two minimal ASP.NET Core API hosts, three BuildingBlocks projects, an interface-only tenant-context abstraction, a single React shell, two unit tests, one architecture test and one documentation-only GitHub Actions workflow. The ten school business-module directories and four platform-module directories contain only placeholder files. No DbContext, EF Core migration set, operational SQL schema, authentication/authorization pipeline, provisioning service, business API endpoint, Local Node sync worker, licence verifier or deployed offline workflow was found in the tracked tree.

This is an **implementation coverage audit**, not a penetration test, runtime test run, legal review or deployment assessment. Findings are limited to code, configuration, tests, GitHub workflow files and canonical documents visible at the exact baseline. “Not found” means not present in the inspected repository tree; it does not assert that no uncommitted or external system exists.

### Readiness by major objective

| Objective | Assessment | Repository evidence |
|---|---|---|
| .NET Modular Monolith | **Foundation only** | Solution, two hosts, SharedKernel, Security and Observability building blocks; no implemented business modules or module registration |
| React application | **Shell only** | One placeholder `App.tsx`; no routes, feature pages, API client, auth state, or workflow UI |
| SaaS tenant model | **Not implemented** | `ITenantContext` interface only; no concrete context, middleware, identity, membership or tenant data services |
| Organization → School → Campus | **Not implemented** | Data dictionary/ERD describe the proposal; no entities, persistence, API or UI |
| Authentication | **Not implemented in inspected code** | No Identity/provider registration, login endpoints, token/session handling, password/MFA lifecycle |
| Authorization and tenant isolation | **Not implemented / not verifiable** | No auth middleware/policies, no scoped query service, no DB roles/RLS/migrations, no security tests |
| Cloud Control Plane | **Health shell only** | Cloud API currently exposes OpenAPI in Development and `/health`; no platform services |
| Local Data Plane | **Health shell only** | Local API currently exposes OpenAPI in Development and `/health`; no operational services |
| PostgreSQL persistence | **Development container only** | Compose starts PostgreSQL; hosts do not register a DbContext or connection |
| Billing/subscriptions | **Not implemented** | Planning documents only; no catalogue, invoice/payment, metering or state machine |
| Licensing | **Not implemented** | No signing service, key custody, signed lease format, verifier, .lic import or Restricted Mode gate |
| Offline and synchronization | **Not implemented** | No PWA service worker/IndexedDB queue, Local Node sync agent, Outbox/Inbox or conflict handling |
| School workflows | **Not implemented** | Admissions, students, fees, finance, reconciliation, academics, attendance and exams are placeholders/specifications |
| AI platform | **Not implemented** | No gateway, provider adapters, RAG, tools, evaluation, usage metering or human-review workflow |
| Testing and CI | **Very limited** | Two unit tests, one architecture test, docs validator workflow only; no application test/build pipeline |
| Operations and recovery | **Documented only** | Compose dev DB; no application images, Local Node installer, deployment pipeline or restore automation |

## 2. Audit scope and method

### 2.1 Exact repository snapshot

- Repository: `a-kabeer/SchoolOS`
- Default branch: `main`
- Commit SHA: `3cc2a02342f60af124e04aeb86563c331b497a1f`
- Repository tree inspected: 316 entries; recursive response was not truncated.
- Existing PR #9 was open and not merged at audit start. Its changes were **not** assumed to exist on `main` and were not included in the audited source baseline.

### 2.2 Required sources read

The audit read the root instructions and project context, including `AGENTS.md`, `README.md`, `docs/README.md`, `docs/54_DOCUMENTATION_STANDARDS.md`, `docs/GIT_WORKFLOW_AND_BRANCHING_STRATEGY.md`, `docs/59_TASK_REGISTER_AND_TRACEABILITY_GUIDE.md`, the ADR index, product requirements, module workflows, architecture, security, roadmap, hybrid deployment, licensing, campus deployment scenarios, Cloud platform security, data dictionary, ERD, API/event contracts, authorization matrix, acceptance-test catalogue, operations runbook, migration specification, sign-off matrix, risk register, open questions, DOC-60, DOC-61 and DOC-62.

Relevant detailed delivery plans read: AI, Business & Customer Platform, commercial billing and hybrid deployment plans. The task-plan index and register guide were also inspected.

### 2.3 Relevant repo-local skills selected

The following existing `.agents/skills/` entry points were verified present and used as audit lenses (not as project policy):

- Architecture/review: `engineering-code-reviewer`, `engineering-backend-architect`, `engineering-software-architect`, `specialized-codebase-archaeologist`.
- Identity/security/privacy: `engineering-identity-access-engineer`, `security-architect`, `security-appsec-engineer`, `security-ai-generated-code-auditor`, `engineering-privacy-engineer`.
- Database/operations: `engineering-database-optimizer`, `engineering-database-reliability-engineer`, `engineering-realtime-collaboration-engineer`, `engineering-devops-automator`, `engineering-sre`.
- Verification/planning: `testing-api-tester`, `testing-test-automation-engineer`, `testing-reality-checker`, `specialized-master-plan-architect`, `engineering-technical-writer`, `engineering-git-workflow-master`, `engineering-minimal-change-engineer`.

### 2.4 Inspection limitations

The GitHub connector allowed direct inspection of tracked source, project files and documentation but did not provide a working local clone/build environment. Therefore this audit did **not** run `dotnet build`, `dotnet test`, `npm ci`, `npm run build`, `npm run lint`, Docker Compose, PostgreSQL integration tests, security scans, dependency vulnerability scans or a runtime penetration test. No GitHub Actions application build/test workflow exists in the inspected tree. These commands must be executed in a proper checkout before implementation or release decisions.

No environment secrets, external infrastructure, cloud deployment, managed identity provider, payment provider, DNS/TLS setup or production database could be inspected. No claim is made about their state.

## 3. Verified implementation inventory

### 3.1 Backend / .NET

| Path | What exists | Audit interpretation |
|---|---|---|
| `src/Hosts/SchoolOS.Cloud.Api/Program.cs` | API explorer, OpenAPI registration, Development-only OpenAPI endpoint, HTTPS redirection and static `/health` response | Cloud platform host scaffold; no application services, controllers/minimal business routes, auth, persistence or module registrations |
| `src/Hosts/SchoolOS.Local.Api/Program.cs` | Same host setup with Local Data Plane health response | Local host scaffold; no operational API, local auth, persistence, workers or module registrations |
| `src/BuildingBlocks/Security/ITenantContext.cs` | Interface exposes nullable TenantId, CampusId, UserId, IsAuthenticated and HasScope | Contract-only abstraction; no concrete implementation, scoped registration, middleware, principal validation or consumer found |
| `src/BuildingBlocks/SharedKernel/Entity.cs` | Base entity ID and basic created/updated metadata | Primitive only; no domain aggregate or persistence mapping |
| `src/BuildingBlocks/SharedKernel/Result.cs` | Success/failure result wrappers | Implemented primitive |
| `src/BuildingBlocks/Observability/TelemetryConstants.cs` | Service name, correlation header and TenantId/CampusId keys | Constants only; no telemetry pipeline or propagation |
| Project files | SharedKernel, Security, Observability and two Web API hosts | No module projects or application/infrastructure projects in current tree |

**Architecture finding:** The two-host arrangement is represented by separate host projects, but the intended Cloud Control Plane and Local Data Plane responsibilities are not implemented. There is no demonstrated boundary enforcement beyond project-reference scaffolding. The Cloud/Local separation must be tested architecturally before modules are added.

### 3.2 Frontend

| Path | What exists | Audit interpretation |
|---|---|---|
| `frontend/schoolos-web/src/App.tsx` | Static SchoolOS Phase 0 card | No product screens or navigation |
| `src/main.tsx` | React root render | Standard app bootstrap only |
| `src/index.css` | Tailwind directives and font stack | Minimal styling |
| `package.json` | React 19, React Router, TanStack Query/Table, Dexie, React Hook Form, Zod, Vite, TypeScript | Dependencies are present but not evidence of features |
| `vite.config.ts` | React plugin, port 5173, ES2022 build target, source maps | Development build config only |

No route tree, login/logout flow, protected route, server session handling, tenant/campus selector, organization dashboard, workflow form, API client, offline service worker registration, Dexie schema, queued command manager, conflict UI, accessibility or RTL implementation was found in tracked `src/`.

### 3.3 Database and persistence

- `deploy/docker-compose.yml` defines one local PostgreSQL 17 Alpine container with a loopback-bound port, local volume and `pg_isready` health check.
- `deploy/.env.example` documents development-only environment variables and placeholder credentials.
- EF Core/Npgsql versions are centrally declared in `Directory.Packages.props`, but no DbContext, DbContext registration, entity configuration, migration, SQL schema, RLS policy, runtime DB role or seed/provisioning script was found.
- Neither API host has a connection string in its `appsettings.json` or registers persistence services.
- The Compose database is therefore a development dependency scaffold, not proof that SchoolOS connects to or persists application data.

### 3.4 Tests

| Test source | Current coverage |
|---|---|
| `tests/UnitTests/SharedKernelTests.cs` | Two tests for Result success/failure |
| `tests/ArchitectureTests/ModuleBoundaryTests.cs` | One rule: SharedKernel assembly must not depend on the two host namespaces |

No API integration test project, PostgreSQL/Testcontainers test, authentication test, tenant-isolation test, RLS/pool test, security test project, Local Node sync test, licensing test, frontend test, Playwright project, end-to-end test or migration/restore test was found.

### 3.5 Infrastructure / CI

- Only `.github/workflows/documentation-quality.yml` exists. It runs `node scripts/validate-docs.mjs` for documentation/register changes.
- No .NET/frontend build/test workflow, security scanning workflow, container build/publish workflow, staging deployment workflow or Playwright workflow was found.
- The Docker Compose file only starts PostgreSQL. No application Dockerfiles, production Compose/Helm/Terraform definitions, Local Node install/bootstrap/update agent or secrets/key-management configuration was found in the inspected tree.
- Current files are adequate to begin local developer experimentation but not a pilot/production release.

## 4. Requirement-to-code-to-test traceability

The current machine-readable register contains 37 requirements. Its evidence-status column explicitly says the task/test references are reference-only and execution has not been verified. The following table maps the most critical requirement families to actual source evidence inspected.

| Requirement IDs | Requirement family | Code evidence on audited main | Test evidence on audited main | Audit result |
|---|---|---|---|---|
| PLAT-001–003 | Modular Monolith, two planes, separate DBs | Host projects + building blocks only; no module implementation/DbContext | One SharedKernel boundary rule; no DB/plane integration test | Partial foundation; core architecture behavior unimplemented |
| TEN-001 | Tenant → School → Campus; one-tenant Local Node | No domain entity or persistence implementation | No hierarchy/tenant boundary tests | Not implemented; proposed v1 hierarchy awaits DEC-001 |
| TEN-002 / ID-002 / SEC-001 | Server-derived TenantContext; permission + scope + relationship | `ITenantContext` interface only; no concrete resolver/middleware/auth/policy | No IDOR/cross-tenant/campus/resource tests | Not implemented |
| TEN-003 / BCP-001 | Tenant lifecycle, membership, provisioning, node enrollment | No Cloud platform domain/services or DB schema | No SAAS-001/002/003/008/009 | Not implemented |
| ID-001 | Separate Person and credentialed User | No identity entities or authentication provider | ID-001 catalogue only; no test source | Not implemented; requirement has no mapped task |
| ADM-001 / STU-001/002 | Admission, household/guardian links, enrollment history | No modules or endpoints | No ADM/FAM tests | Not implemented; task mappings Unassigned |
| FEE-001–004 / FIN-001/002 | Fee plans, invoices, allocations, double-entry, immutability | No Fees/Finance modules, DB or posting services | No FEE/FIN tests | Not implemented; most mapped task fields Unassigned |
| REC-001/002 | Bank statement import and reconciliation | No Reconciliation module/import parser | No REC tests | Not implemented |
| ACA-001 / EXM-001/002 | Curriculum and exam/result integrity | No Academics/Examinations modules | No EXM tests | Not implemented; board content rights remain external/open |
| ATT-001/002 / FEE-004 | Offline attendance, provisional receipt, idempotent sync | No offline client store, Local API operations, device registry, queue or sync host | No OFF/HYB/FEE offline tests | Not implemented |
| HYB-001/002 | Local operation during cloud outage; Outbox/Inbox | Local API health only; no persistence/workers | No HYB/OFF tests | Not implemented |
| LIC-001/002 | Automatic + manual signed .lic renewal, Restricted Mode | No signing/verifying code, lease claims, import or authorization overlay | No LIC tests | Not implemented; operation matrix/grace details remain open |
| SEC-002/003/004 | Separate auth audiences, MFA/step-up, sensitive-action audit | No auth pipeline or audit store | No SEC-007/OPS-003 | Not implemented; policy/provider approvals required |
| BCP-002 | Ephemeral synthetic demo | No demo host/runtime/store/session manager | No DEM-001/002 | Not implemented; isolation/TTL/backups remain open |
| AI-001 | Privacy-aware gateway and RAG | No AI module/gateway/provider adapter/vector store/job worker | No AI-001–005 tests | Not implemented; AI plan explicitly Not Started |
| MIG-001 | Validated repeatable migration/export | No importer/exporter application service or package verifier | No MIG tests | Not implemented; plan remains draft and pilot-specific decisions open |
| UX-001 | Role-aware responsive UI and Urdu RTL | Single static page; no UI workflows or locale layer | No UX tests | Not implemented; launch language scope open |
| OPS-001–003 | Restore, backup encryption/secrets, audit completeness | No app backup/restore/audit mechanisms | No restore/security/audit tests | Not implemented |

**Important:** Acceptance-test IDs in DOC-47 and SAAS-001–014 are specifications, not actual test files or successful test executions. They must be implemented and run at appropriate unit, database integration, API/security, browser/E2E and operational levels.

## 5. SaaS multi-tenancy deep-dive

### 5.1 Organization / school / campus hierarchy

The documented proposal is Tenant/Organization → one or more Schools/legal entities → one or more Campuses. A Local Node is bound to one tenant and can serve multiple campuses within that tenant; a tenant can operate multiple nodes; a Local Node must not host unrelated tenants in v1. The proposal is captured in DOC-62 and DOC-44, with final product/legal approval still open under DEC-001.

**Code result:** No Organization, School, Campus, Tenant, Node, NodeCampusAssignment, Membership or Invitation domain type/table/service exists in the tracked `src/` tree. The proposal is not implemented.

### 5.2 Tenant context and request resolution

The only relevant code is `ITenantContext`, a nullable interface. It does not identify the authenticated principal source, active tenant membership, host/route selection, scope assignment, failure state, node-bound tenant, or data transaction context. Neither host registers it or installs authentication/authorization middleware.

**Code result:** TenantContext is not operational. No evidence that every request derives and validates scope server-side or that missing/mismatched scope fails closed.

### 5.3 Database isolation

DOC-44/DOC-62 propose tenant-owned rows, composite same-tenant foreign keys, restricted Cloud PostgreSQL roles, transaction-local tenant context and RLS as defense in depth; each Local Node database is single-tenant. These are proposed controls with DEC-004 open.

**Code result:** No database schema, RLS policy, runtime database role, transaction-local setting, connection-pool guard, interceptor, EF query guard or integration test. No isolation guarantee can be claimed.

### 5.4 Authentication / authorization

The docs specify global user identity with explicit tenant memberships, separate Cloud/platform/local/demo/parent/student/support audiences, default-deny access and `permission + scope + resource relationship + state`. DEC-008 remains open for identity provider/MFA/local identity lifecycle.

**Code result:** No credential store, login, password handling, token validation, session lifecycle, refresh/revocation, MFA, policy registration, authorization handler, object-level guard or audit path in hosts. `IsAuthenticated` in the interface is not an implementation.

### 5.5 Provisioning and node enrollment

DOC-62/BCP/HYB plans describe onboarding, tenant creation, invitations/memberships, payment/subscription gates, node registration, one-time enrollment tokens and tenant-bound node identity/certificates.

**Code result:** No endpoint, application service, table, worker, certificate issuance/rotation, node status or membership workflow is implemented.

**Conclusion:** SchoolOS is not yet a verifiable SaaS application. The organization/campus hierarchy exists as a detailed proposal, not as an enforceable multi-tenant implementation.

## 6. Subscription, billing and licensing audit

### 6.1 Subscription and billing

The documents describe organization-level subscriptions, configurable plans/prices, active-student metering across campuses, node entitlements, monthly/annual invoices, payment verification, credit notes/refunds, tax settings, customer lifecycle and billing audit. Commercial numbers remain proposals; DEC-018 and DEC-020/023-related policy details remain open.

**Code result:** No subscription, plan, price version, invoice, payment, payment reconciliation, capacity snapshot, entitlement, node metering or billing API/model exists in the inspected implementation. The BILL task plan is planning-only.

### 6.2 Signed licence lease

The accepted product policy is both automatic online renewal and manual signed `.lic`/USB fallback, with Restricted Mode after approved grace, preserving historical read/export, backup, billing/renewal and support. A valid renewal should reactivate without a software update. Seven-day grace is not approved; cryptographic format/key custody and operation-by-operation allow/deny remain open.

**Code result:** No asymmetric signer, key store, issuer/audience validation, key rotation, node binding, sequence/replay validation, local verifier, durable entitlement snapshot, manual file importer, time rollback handling or Restricted Mode authorization guard exists in the inspected source.

**Conclusion:** Subscription state, entitlement state, user authorization and local lease state must become distinct implemented gates; none is yet evidenced in code.

## 7. Offline, Local Node and synchronization audit

### Required architecture

The Local Data Plane is the authoritative local runtime. PWA cache is supplementary. Approved local workflows must not require Cloud availability. Synchronization is event-based hub-and-spoke with transactional Outbox/Inbox, unique node identity, authenticated outbound sync, idempotency, cursors, conflict review, explicit ownership and visible freshness. Signed/encrypted manual campus data exchange is a separate supported profile.

### Code result

- No service worker or PWA manifest in the tracked frontend tree.
- Dexie is declared as a dependency, but no database schema or offline queue implementation exists in `src/`.
- No offline command envelope, device registration/revocation, client operation ID, pending/synced/conflict state or reconnect/replay behavior.
- No Local API business endpoint, background worker, Outbox/Inbox table, Sync Agent/Hub, cursor or conflict resolution.
- No mTLS node identity, signed event envelope or campus package import/export code.
- No local authentication or authorization service usable during WAN outage.
- Compose provides a local PostgreSQL container but not a Local Node installation/runtime package.

**Conclusion:** Offline operation and sync are not implemented. The presence of Dexie in package dependencies and a local PostgreSQL Compose service is not offline capability.

## 8. Security, privacy and operational gaps

| Area | Verified state | Priority |
|---|---|---|
| Authentication and session security | No auth registration or session/token pipeline in API hosts | P0 |
| Default-deny authorization | No authorization services/policies or endpoint protection visible | P0 |
| Tenant and campus isolation | No tenant middleware/context implementation or DB/RLS policy | P0 |
| Secrets and signing-key custody | No secrets/KMS/HSM integration or licence signer | P0 before licensing |
| Audit of finance/roles/licence/support/export | Telemetry constants only; no audit persistence/event pipeline | P0 |
| Node identity and sync transport | No certificate/device registration/mTLS | P0 before hybrid pilot |
| Local backup and tested restore | No backup/restore runtime; runbook is documentation only | P0 before pilot |
| Supply-chain/dependency scanning | No SCA/SAST/secret-scan workflow in tracked workflows | P1 |
| Privacy data maps/retention/deletion | Requirements documented; no enforcement jobs or consent gates | P1, with P0 gates before processing child data |
| Rate limits/abuse controls | No API endpoint/security middleware | P1 |
| Demo isolation and cleanup | No runtime; threat/TTL/storage decisions open | P1 before demo launch |
| Logs/traces/metrics | Telemetry constants only; no OpenTelemetry setup | P1 |
| Incident response/restore evidence | Runbook specified but no operational evidence | P0 before customer pilot |

No concrete exploitable runtime vulnerability was established because the repository contains almost no authenticated application surface. The appropriate finding is **critical security control absence / security assurance not established**, not a claim that a specific cross-tenant exploit was reproduced.

## 9. Testing and CI gap

| Layer | Present | Missing before pilot |
|---|---|---|
| Unit | 2 SharedKernel Result tests | Domain/application tests for business invariants |
| Architecture | 1 SharedKernel host-dependency rule | Cloud/Local dependency boundaries, module encapsulation, cycles, data ownership and prohibited project references |
| DB integration | None | PostgreSQL/Testcontainers, migrations, constraints, transaction/outbox, RLS and pooled connection tests |
| Identity/security | None | Authentication, membership, tenant switch, authorization, IDOR, role/scope, MFA/step-up and export tests |
| API/functional | None | Contract/API tests for onboarding, admission, fees, finance, attendance, exams, billing, licences and support |
| Offline/sync | None | Restart, long outage, idempotency, out-of-order delivery, conflict, revocation, wrong-node/tenant and clock tests |
| Frontend | None | Component/route/form tests, accessibility and locale/RTL tests |
| E2E | None | Playwright critical workflows and multi-tenant/multi-campus journeys |
| Performance/reliability | None | Load/capacity, node outage, backup/restore, migration and recovery drills |
| CI | Docs validator only | .NET build/test, frontend lint/build/test, container build, security scans and E2E pipelines |

P0 acceptance tests already specified include ARC-001, DB-001, SEC-001–006, HYB-001–003, LIC-001–004, OPS-001–003, SAAS-001–012 and relevant core admission/fee/finance/exam tests. The catalogue currently contains no corresponding executable test project.

## 10. Prioritised implementation gap register

Priority definitions align to DOC-47: **P0** must pass before first operational pilot; **P1** before public production; **P2** later/advanced capability. A documented open decision or external review may block a task even when its code dependencies are understood.

### P0 — Foundation, data integrity and pilot blockers

| Gap | Verified evidence | Requirement / acceptance links | Primary existing plan | Exit outcome |
|---|---|---|---|---|
| P0-01 — Close pre-implementation approval gate | DOC-51 explicitly blocks Phase 1 until sign-off; DOC-60 has open P0 decisions | PLAT-001–003, TEN-001–003; ARC-001, DB-001, SAAS-001/002 | DOC-08 Phase 0; DOC-60 closure process | Product/security/finance/operations/QA sign-off or explicit owner-approved blockers |
| P0-02 — Build solution/module skeleton and enforce architecture boundaries | Host and BuildingBlock projects only; module folders placeholders | PLAT-001; ARC-001, DB-001 | DOC-08 Phase 1; H2 | Buildable modular boundaries, module composition and Cloud/Local architecture tests |
| P0-03 — Implement organization/tenant hierarchy and lifecycle | No tenant/org/school/campus entities or persistence | TEN-001, TEN-003; SAAS-001/002/008/009 | H1, H6; BCP-0/5/6 | Tenant → School → Campus model and one-tenant Local Node with database constraints |
| P0-04 — Implement identity and server-derived TenantContext | Interface only; no authentication or middleware | TEN-002, ID-001/002, SEC-001; SEC-001–007, SAAS-003–005/014 | BCP-1; H2; DOC-62 sections 4–5 | Validated principal, explicit membership, separate audiences, fail-closed scope resolution and object-level checks |
| P0-05 — Implement PostgreSQL persistence and tenant isolation | No DbContext/migrations/schema/RLS/runtime DB roles | PLAT-003, TEN-002; DB-001, SEC-005, SAAS-006/007 | H1/H2; DOC-44, DOC-62 sections 6–8 | Migrations, constraints, restricted DB roles, approved RLS and pooled-context isolation tests |
| P0-06 — Implement core audit and sensitive-operation controls | No audit store/pipeline | SEC-004; OPS-003; SAAS-014 | BCP-1/6; H5 | Append-only audit for privileged, access, finance, export, licence, node and support actions |
| P0-07 — Implement first operational vertical slice and domain foundation | No business module endpoints; no operational workflow | ADM-001, STU-001/002, FEE-001/002, FIN-001/002; ADM/FEE/FIN tests | DOC-08 Phases 2–3; existing product/module specs | Approved admission-to-enrollment and fee/finance workflows with transactional persistence and tests |
| P0-08 — Implement Local Node packaging and local auth/runtime | Local API health only; no packaging/install scripts/local auth | PLAT-002/003, HYB-001, ATT-001; DB-001, HYB-001, SAAS-011 | H2; H5 | Installable Local Node with PostgreSQL, persistent storage, local HTTPS/auth and WAN-independent operation |
| P0-09 — Implement transactional sync foundation | No Outbox/Inbox/Sync Hub/Agent/device identity | ATT-002, HYB-001/002; OFF-003–007, HYB-002/003, SAAS-012 | H4; H6 | Durable Outbox/Inbox, node authentication, idempotency, acknowledgement, retry, conflict review and wrong-scope rejection |
| P0-10 — Implement signed lease and Restricted Mode | No signer/verifier/lease state/restricted gate | LIC-001/002; LIC-001–004, SAAS-010/011 | H3; BILL-1 | Key custody/rotation, node-bound signed leases, automatic and manual renewal, sequence/replay checks, clock handling and approved allow/deny matrix |
| P0-11 — Implement data safety and recovery | No backup/restore implementation | OPS-001/002; OPS-001/002 | H5; DOC-48 | Encrypted backups include DB/files/keys/config; restore drills verify integrity and measured RPO/RTO |
| P0-12 — Establish executable CI and test foundation | No build/test CI; only two unit + one architecture test | All P0 acceptance IDs | DOC-52 as staging companion; create CI from approved plan | .NET/frontend builds, unit/integration/security/architecture/API tests and required merge-blocking checks |
| P0-13 — Implement first release finance/reconciliation integrity | No fees, ledger, bank import or reconciliation modules | FEE-001–004, FIN-001/002, REC-001/002; FEE/FIN/REC tests | DOC-08 Phase 3; BILL scope where applicable | Balanced journals, immutable postings, allocations, idempotency, source-provenanced imports and maker/checker approvals |

### P1 — Public-production requirements

| Gap | Verified evidence | Requirement / acceptance links | Existing plan | Exit outcome |
|---|---|---|---|---|
| P1-01 — Full admission/student/guardian/campus workflows | No implementation; mappings mostly Unassigned | ADM-001, STU-001/002, ID-001; ADM/FAM/ID tests | DOC-08 Phase 2; BCP where commercial-facing | Complete approved workflows and relationship-safe access |
| P1-02 — Academic, attendance and exam workflows | No modules/UI/storage | ACA-001, ATT-001, EXM-001/002; EXM/OFF tests | DOC-08 Phases 4–5 | Versioned academic structure, local attendance, marks approval and immutable result publication |
| P1-03 — Complete billing, metering and customer lifecycle | No billing services/data | BCP-001, BILL, TEN-003; SAAS-010, BCP plan tests | BILL-0…6; BCP-5…8 | Organization subscription, plan versions, student/node metering, payment verification and independent entitlement gates |
| P1-04 — Campus topology, manual exchange and multi-node lifecycle | No node registry/data packages/reporting projections | HYB-002; HYB-004–006, SAAS-008/012 | H6; DOC-18/50/57 | Approved node/campus ownership and signed encrypted dry-run/apply import/export |
| P1-05 — Production demo, portal, admin and support experiences | No BCP frontend/backend modules | BCP-001/002, SEC-002/003/004; DEM tests, SAAS-014 | BCP-0…11 | Separate auth audiences, synthetic demo isolation/expiry/cleanup, customer portal, least-privilege admin/support |
| P1-06 — RAG/provider gateway and AI governance | AI plan explicitly Not Started; no code | AI-001; AI-001–005 | AI-0…3 first; AI-4…7 later | Data classification, approved provider routing, secret custody, permission-filtered retrieval/tools, evals, audit, human review and quotas |
| P1-07 — Responsive UI, accessibility and locale/RTL | Static page; no product UI | UX-001; UX-001/002 | DOC-08 frontend phases; DOC-49 | Role-aware usable workflows, accessibility and approved language/RTL scope |
| P1-08 — Migration, export/offboarding and portability | No importer/exporter | MIG-001; MIG-001–005, SAAS-013 | BILL-5, H6; DOC-50 | Dry-run, idempotent batch, source provenance, financial reconciliation, customer acceptance and scoped export |
| P1-09 — Security/privacy operations and threat model | No operational security controls or security CI | SEC-001–004, BCP-002; SEC-007, OPS-002/003, DEM | BCP-1/11, H5/H7 | Threat model, MFA, data maps, retention/deletion, security scans, rate limits, incident response and restore evidence |
| P1-10 — Observability and deployment automation | Telemetry constants only; docs workflow only | PLAT-002/003, HYB-001; OPS tests | H5/H7; DOC-52 | Health/readiness, structured logs, metrics/traces, alerts, signed artifacts, staged updates and CI/CD |
| P1-11 — Demo privacy, expiry and cleanup | No isolated demo runtime | BCP-002; DEM-001/002, DEC-013 | BCP-4; DOC-41 | Server-authoritative session state, isolation, logout revocation, retryable deletion and backup exclusions |
| P1-12 — AI release features | No AI service/UI/workers | AI-001; AI acceptance tests | AI-4…6 | Finance auditor/copilot, learning/exam intelligence and voice/intervention only after foundation, privacy and evaluation gates |

### P2 — Later/advanced capabilities

| Gap | Reason for P2 | Existing reference |
|---|---|---|
| P2-01 — Premium private/campus-local AI inference | Optional premium capability; core operations must not depend on AI | AI-7; DOC-20 |
| P2-02 — Advanced AI model fine-tuning and deeper personalization | Requires data governance/opt-in, evaluation and privacy approval first | DOC-23; AI-7 |
| P2-03 — Advanced school modules such as HR/payroll, inventory, library, procurement and transport | Later-release/non-goal candidates; require explicit requirements/rights/legal and entitlement decisions | DOC-01/02/43 and risk register |
| P2-04 — Enterprise isolation options and advanced residency placement | Tenant-specific database/deployment options depend on DEC-021/022 and measured demand | DOC-62; DEC-021/022 |
| P2-05 — Advanced multi-node analytics and usage/fair-use optimisation | Depends on stable synchronization, tenant metering and reporting contracts | DOC-14/18/27/36/62; DEC-023 |
| P2-06 — Advanced SSO/SCIM/enterprise identity | Add after first secure authentication/membership foundation; not a substitute for basic authentication/authorization | DOC-62; `engineering-identity-access-engineer` |

## 11. Ordered engineering roadmap and dependencies

This roadmap orders existing canonical work; it does not create a competing task register. Detailed checklists remain in the canonical workstream plans and individual task status remains in the eight CSV partitions.

| Order | Stage | Reuse existing work | Entry dependency | Deliverables | Acceptance / exit gate |
|---:|---|---|---|---|---|
| 0 | Product/security/architecture closure | DOC-08 Phase 0; DOC-51; DOC-60; relevant ADRs | Audit accepted by owners | Close or explicitly block DEC-001/002/003/004/005/006/008/010/012/017/020/021/022/023 as applicable; approve first release scope and identity/security model | Owner sign-offs; approved data ownership, legal/privacy scope, ERD/contract decisions and named owners |
| 1 | Build/test/module boundary foundation | DOC-08 Phase 1; DOC-03; DOC-52 | Stage 0 gates for affected design | Minimal Cloud/Local composition roots, module registration boundaries, build rules, DB integration harness, architecture tests, build/test/security CI | ARC-001, DB-001; clean build/test in CI; prohibited Cloud↔Local dependencies fail tests |
| 2 | Persistence and tenant identity | H1, H2; DOC-44; DOC-62 | Tenant hierarchy and DB model decisions | Separate Cloud platform DB and Local operational DB; module-owned schema/migration; tenant, school, campus, node and identity entities; global user/membership | SAAS-001/002/006/007/009; hierarchy constraints, migrations and fail-closed tenant integration tests |
| 3 | Authentication, authorization and audit | BCP-1; H2/H5; DOC-38/46 | Identity provider/MFA/local lifecycle approval | Concrete TenantContext, separate authentication audiences, session lifecycle, default-deny permission/scope/resource/state checks, RLS, audit | SEC-001–007, SAAS-003/004/005/014, OPS-003 |
| 4 | First persisted school vertical slice | DOC-08 Phases 2–3; DOC-43/45/47 | Persistence, auth, authorization and audit | Admission → enrollment; fee invoice/payment allocation; initial finance posting contract | ADM/FAM/FEE/FIN P0 acceptance; transaction, idempotency and audit evidence |
| 5 | Local Data Plane runtime | H2/H5; DOC-14/16/48 | Core schema, identity and first workflows | Installable Local Node, local PostgreSQL, local auth, HTTPS, files/jobs, health/readiness and backup primitives | HYB-001, SAAS-011; install, disconnected-operation and recovery tests |
| 6 | Offline and durable synchronization | H4/H6; DOC-05/14/18/45/47/57 | Local runtime, ownership rules and node identity | Outbox/Inbox, cursors, retry/idempotency, conflict workflow, device management and client queue | OFF-001–008, HYB-002/003, SAAS-012 |
| 7 | Billing, entitlement and signed licensing | BILL-0/1; H1/H3; DOC-15/27/36/45/46/60/62 | Tenant/node identity; commercial decisions | Versioned subscription, payment verification, student/node metering, signer/verifier, automatic+manual renewal, Restricted Mode | SAAS-010, LIC-001–004; signed test vectors, tamper/replay/wrong-node tests |
| 8 | Multi-campus and manual exchange | H6; DOC-18/45/48/50/57 | Nodes, sync, ownership and selected topology | Node-campus assignment, signed encrypted packages, dry-run/import approval/read-only reporting and freshness | HYB-004–006, SAAS-008/012/013 |
| 9 | Business & Customer Platform | BCP-0…11; DOC-29…41 | Identity, authorization, audit, billing/licence boundaries | Website/CMS, CRM, onboarding, synthetic demo, admin, portal and support | DEM-001/002, SEC-007, SAAS-014 and BCP acceptance tests |
| 10 | AI platform foundations/features | AI-0…6; DOC-20…26 | Stable tenant authorization, data projections, audit, provider/legal approvals, quotas | Provider Gateway, RAG, approved tools, evaluations, Auditor/Copilot then later learning/exam/voice | AI-001–005 and adversarial/provider/privacy/cost tests |
| 11 | Production hardening and pilot | H5/H7; DOC-48/52; DOC-50 | Required P0 and selected P1 deliverables | Monitoring, incident/support, signed updates, restore, migration, performance, security and customer pilot | OPS-001/002/003, PERF, MIG, security acceptance; measured RPO/RTO and customer sign-off |

**Dependency rule:** Do not start downstream work on a placeholder dependency. If an open decision blocks the design, obtain owner approval or record an explicit blocker before implementation. This sequence does not change current task status.

## 12. Decision and traceability gaps

The current machine-readable register has 37 requirements. It is useful but not yet a full requirement-to-code-to-test matrix: task references often point to future plan items and the evidence fields say reference-only. Requirements currently Unassigned at task level include PLAT-001, ID-001, ADM-001, STU-001/002, FEE-001/002/003, FIN-001/002, REC-001/002, ACA-001, EXM-001/002 and UX-001.

Extend the existing traceability process—not a new tracker—to record verified code path(s), executable test path/test name, test run URL/result and reviewer/PR/commit evidence for each existing requirement ID. Mark missing implementation/test mapping explicitly; do not invent requirements to disguise gaps.

## 13. Open decisions to keep visible

| Decision | Current state | Audit implication |
|---|---|---|
| DEC-001 tenant/legal hierarchy | Open | DOC-62 hierarchy is a proposal, not approved data model |
| DEC-002 primary keys/external IDs | Open | No final ID/schema contract |
| DEC-003 pilot shared-node vs per-campus topology | Open per pilot | Cannot select deployment shape without customer outage analysis |
| DEC-004 RLS/context/DB roles/pooling | Open | No production isolation approach approved/tested |
| DEC-005 fees-to-finance posting boundary | Open | Finance slice needs approved transactional contract |
| DEC-006 accounting policy/CoA/fiscal year/approval | Open | No production finance posting without owner approval |
| DEC-007 bank/payment providers | External verification | Provider integration cannot be assumed |
| DEC-008 identity/MFA/local lifecycle | Open | Identity provider/security flow must be approved |
| DEC-009 minors/legal/privacy retention | External verification | Qualified Pakistan-specific legal/privacy review required |
| DEC-010 Restricted Mode operation matrix | Open | Restricted Mode cannot ship until operation allow/deny behavior is approved |
| DEC-011 cloud data projections | Open | Parent/reporting projections need purpose/field/retention approval |
| DEC-012 offline marks/cash semantics | Open | Offline approval/posting rules need owner decisions |
| DEC-013 demo isolation/TTL/backup retention | Open | Demo runtime is blocked pending threat/load/retention decisions |
| DEC-014 curriculum/content rights | External verification | Board content must be rights-verified |
| DEC-015 migration/opening balances | Open per pilot | Customer-specific migration scope and method required |
| DEC-016 language and RTL launch scope | Open | English/Urdu/Roman Urdu scope and acceptance need approval |
| DEC-017 backup/RPO/RTO/key custody | Open per tier | Published reliability commitments require measured restore evidence |
| DEC-018 tariff/tax/refund/exit policy | Open/external verification | Pricing/tax/legal approval required |
| DEC-019 support/SLA/node replacement | Open | Operational ownership and support access need decision |
| DEC-020 module/entitlement/lease semantics | Open | Core/paid, dependencies and signed lease claims need approval |
| DEC-021 tenant-specific Cloud restore | Open | Shared DB tenant-selective recovery must be designed/tested |
| DEC-022 residency/hosting placement | Open/external verification | Region, subprocessors, backup and support boundaries require review |
| DEC-023 usage quotas/fair use/overages | Open | Metering dimensions and enforcement need decision |

## 14. Technical debt and implementation risks

These are not claims of production incidents; they are risks established by the code/documentation gap:

1. **Placeholder architecture risk:** Empty module folders and a thin host scaffold may create false confidence about modular separation. Define and test dependency boundaries as modules are added.
2. **Tenant contract risk:** `ITenantContext` exposes nullable scope values but does not itself specify fail-closed behavior. Protected access must reject missing or ambiguous scope.
3. **No database contract in code:** EF/Npgsql package references without a DbContext/migrations/test harness leave data ownership and RLS purely documentary.
4. **No application CI:** Regressions in backend/frontend can merge without an executable build/test workflow based on the current tree.
5. **No dependency/security pipeline:** No SCA, SAST, secret scan or container scan workflow was found.
6. **No operational deployment artifact:** Dev-only PostgreSQL Compose is not a supported Local Node package.
7. **No frontend test/offline foundation:** Dexie being installed does not establish offline semantics; there is no persisted queue, service worker or failure-state UI.
8. **Production source maps need a release decision:** Vite config has `sourcemap: true`; confirm whether production artifacts should ship source maps and where private maps are retained. This is a hardening review item, not a demonstrated vulnerability.
9. **Development DB defaults:** Compose/env example contain predictable development credentials. They are clearly marked local development defaults and DB binds to loopback; production configuration must refuse defaults and use protected secrets.
10. **Documentation-to-task gaps:** Many core functional requirements map to Unassigned tasks; use and reconcile existing workstream plans rather than adding duplicated task lists.

## 15. Audit conclusions and handoff

- SchoolOS currently has a **small technical foundation**, not the approved SchoolOS SaaS/school-operating product.
- The most serious gaps are foundational: authentication, tenant hierarchy/isolation, database schema/persistence, authorization, audit, local operational runtime, node identity, synchronization, signed licence enforcement and P0 tests.
- Billing, BCP, school workflows and AI are not ready to be implemented safely until shared identity, data ownership and authorization foundations exist.
- Do not market tenant isolation, offline operation, signed licensing, billing, backup/restore, AI/RAG or business workflow functionality as implemented.
- This report does not change architecture, approve the proposed DOC-62 tenant baseline, select an identity/payment/provider, close DEC items or update task status. Decisions remain with their documented owners.

### Evidence references

- Repository baseline: `3cc2a02342f60af124e04aeb86563c331b497a1f`
- Backend host: `src/Hosts/SchoolOS.Cloud.Api/Program.cs`, `src/Hosts/SchoolOS.Local.Api/Program.cs`
- Security primitive: `src/BuildingBlocks/Security/ITenantContext.cs`
- Tests: `tests/UnitTests/SharedKernelTests.cs`, `tests/ArchitectureTests/ModuleBoundaryTests.cs`
- Frontend: `frontend/schoolos-web/src/App.tsx`, `frontend/schoolos-web/src/main.tsx`
- Infrastructure/CI: `deploy/docker-compose.yml`, `deploy/.env.example`, `.github/workflows/documentation-quality.yml`
- Requirements: DOC-01/02/03/07/14/15/18/38/43–51/60/62
- Existing plans: AI, BCP, Commercial Billing and Hybrid Deployment under `docs/task-plans/`
- Registers: `registers/requirement-traceability.csv` and eight `registers/task-register-*.csv` partitions

**Validation limitation:** Repository content inspection and CSV parsing/counts were performed. Application builds, tests, scanners, PostgreSQL, Docker and deployment runtime were not executed. Those limitations must remain visible until respective evidence is produced.
