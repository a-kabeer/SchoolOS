# 42 — Documentation Completion and Repository Gap Register

- **Document ID:** DOC-42
- **Status:** Draft
- **Decision maturity:** Proposed documentation gap baseline; owner sign-off pending
- **Owner:** Technical lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `9d8d27caa65f9bd0a68764671d451495df28df77` (pre-alignment post-PR #7 audit baseline; earlier reviewed refs retained below)
**Version:** v1.8 draft  
**Created:** 18 November 2026  
**Repository reviewed:** `a-kabeer/SchoolOS`  
**Reviewed ref:** `main` at `66e0d35416a7f556ecb2aead4a8580bd3a0ed686`  
**Purpose:** Establish a documentation-first gate before domain implementation.  

## 1. Executive finding

The repository contains a substantial product and architecture blueprint, but the current source tree is still a foundation scaffold.

At the reviewed commit:

- The solution contains Cloud API and Local API hosts, three BuildingBlocks projects, one ArchitectureTests project and one UnitTests project.
- Six C# source files were present: two host `Program.cs` files, `ITenantContext`, `Entity<TId>`, `Result` / `Result<T>`, and telemetry constants.
- The ten school business-module folders and four platform-module folders contain only `.gitkeep` placeholders.
- No EF Core migration files, SQL schema files, or `.github/workflows` CI files were present in the recursive repository tree.
- The React app is a Vite/TypeScript shell; `App.tsx` renders a foundation placeholder.
- A PostgreSQL Docker Compose development service exists, but neither API host currently registers a DbContext, database provider, authentication, authorization, domain endpoints or module services.
- Existing tests cover a shared-kernel result type and one SharedKernel dependency boundary. They do not yet prove tenant isolation, database behavior, API authorization or business workflows.

**Conclusion:** Documentation is ahead of implementation. The next milestone is to complete and approve the specifications, not to begin business-module coding.

## 2. Evidence and status language

Use these labels consistently in every SchoolOS document:

| Label | Meaning |
|---|---|
| **Implemented / Verified** | Executable source, migration, test or operational evidence exists at a cited commit. |
| **Documented / Proposed** | Design exists, but no executable implementation evidence has been verified. |
| **Confirmed Decision** | Product owner explicitly selected the direction. |
| **Open Decision** | A responsible owner must select an option before the related implementation. |
| **External Verification Required** | Depends on vendor contract, regulator, bank, board, legal counsel or current third-party documentation. |
| **Not in Initial Release** | Explicitly outside the initial delivery scope. |

A folder, project name, README statement, checklist item or architecture diagram is not proof that a feature is implemented.

## 3. Repository implementation baseline

| Area | Evidence at reviewed commit | Status |
|---|---|---|
| .NET solution / target framework | `SchoolOS.sln`, `Directory.Build.props`, `global.json` | Foundation present |
| Cloud API host | Minimal host, OpenAPI registration and health endpoint | Scaffold only |
| Local API host | Minimal host, OpenAPI registration and health endpoint | Scaffold only |
| PostgreSQL development service | `deploy/docker-compose.yml`, local loopback port binding | Dev infrastructure only |
| EF Core persistence | Package versions declared centrally, no DbContext or migrations found | Not implemented |
| Tenant context | `ITenantContext` interface only | Abstraction only |
| Authentication / authorization | No implementation found in the reviewed source tree | Not implemented |
| Business modules | Ten module folders contain placeholders only | Not implemented |
| Platform modules | Tenancy, Licensing, Subscriptions and SyncHub folders contain placeholders only | Not implemented |
| Frontend | React/Vite app shell; dependencies include React Query, Router, React Hook Form, Zod and Dexie | Scaffold only |
| Tests | Shared-kernel unit tests and one architecture test | Very limited foundation |
| CI | No GitHub workflow file found in reviewed tree | Not found |
| Database schema / migrations | No SQL schema or EF migration file found | Not implemented |

This is a repository snapshot, not a statement about uncommitted local work or another branch.

## 4. Existing documentation inventory

The current documentation set already records many product decisions and must be preserved rather than rewritten casually.

| Existing source | Main coverage | Remaining documentation work |
|---|---|---|
| 00–02 | Decisions, product requirements, modules and workflows | Turn requirements into traceable IDs and measurable acceptance criteria |
| 03 | Modular Monolith architecture | Add implementation dependency map and approved module contracts |
| 04 | Preliminary Local Data Plane database catalogue | Complete ERD, columns, types, keys, indexes, ownership and migration plan |
| 05 | Offline/PWA and synchronization policy | Complete API envelope, conflict matrix and test cases |
| 06, 20–26 | AI product, privacy, RAG, governance and tasks | Provider-specific terms, retention and evaluation thresholds remain external/open |
| 07 | Security and quality requirements | Complete actor/resource permission matrix, threat model and test evidence |
| 08–11 | Roadmap, progress, risks and open questions | Assign owners and close decisions before affected phase |
| 12 | Competitor notes | Keep marketing claims evidence-qualified |
| 14–18 | Hybrid deployment, licensing, sync, campus scenarios | Complete pilot topology and operating procedures |
| 27–28, ADR-019 | Commercial pricing/billing direction | Numeric tariff, active-student definition, taxes and billing rules remain open |
| 29–41, ADR-020–021 | Business & Customer Platform and ephemeral demo | Complete detailed schema, API DTOs, authorization and retention decisions |
| 36–38 | BCP database, API family and security baseline | Full field dictionary, OpenAPI contract, retention schedule and threat model |

## 5. Architecture conflicts to avoid

### 5.1 Hybrid database boundary

The current strategic baseline is **Cloud Control Plane + Local Data Plane**:

- Cloud Control Plane owns customer/platform metadata: organization registration, subscriptions, commercial billing, licence issuance, node registry, sync coordination, signed releases and approved reporting projections.
- Every deployed Local Data Plane owns its operational PostgreSQL database for school business records.
- Cloud Control Plane must not become an uncontrolled mirror of the full student, guardian, attendance, examination and finance database.
- Tenant, school, campus and node identifiers remain in operational records where relevant for provenance, access scope and transfer.
- A cloud-centralized deployment profile remains documented for customers with reliable internet; it is a separate deployment profile, not permission to silently merge the hybrid databases.

**Do not implement one universal shared database for all hybrid campuses.** The shared-row/RLS approach may be used in the explicitly selected Cloud Centralized profile or within a dedicated data store whose boundary has been approved.

### 5.2 Offline authority

The Local Node is the authoritative operational backend during internet outages. PWA/IndexedDB is supplementary for browser drafts/cache and client-side pending work; it is not a substitute for the Local Node and PostgreSQL.

### 5.3 Pricing

The repository's current commercial starting preference is Rs. 2,500/month including 200 students plus Rs. 10 per additional student/month, organization-level billing, first Local Node included, school-provided local server and monthly/annual billing analysis. The earlier competitor research's example tier table is not automatically the SchoolOS tariff. Other amounts in the repository are explicitly unapproved test hypotheses.

### 5.4 Research-derived shared-database advice

The tenant-customization research's shared database + `tenant_id` + RLS recommendation must be applied only where that topology is selected. It does not supersede the approved hybrid cloud/local database separation.

## 6. Documentation deliverables required before implementation

| ID | Deliverable | Required outcome | Proposed owner | Gate |
|---|---|---|---|---|
| DOC-001 | Product scope and glossary | One canonical list of actors, terms, modules, in/out-of-scope | Product owner | P0 |
| DOC-002 | Requirements traceability | Every requirement has ID, source, priority, owner and acceptance evidence | Product + QA | P0 |
| DOC-003 | Domain ERD | Relationships, cardinalities, ownership and delete rules | Technical lead | P0 |
| DOC-004 | Field-level data dictionary | Names, types, nullability, defaults, constraints, classification | Technical lead | P0 |
| DOC-005 | Database/migration strategy | Module ownership, baseline, rollback, seed/reference data, RLS setup | Backend lead | P0 |
| DOC-006 | API contracts | Routes, DTOs, validation, errors, scopes, idempotency and OpenAPI plan | Backend lead | P0 |
| DOC-007 | Event contracts | Outbox/Inbox envelope, versioning, ownership, retry and replay rules | Backend lead | P0 |
| DOC-008 | Permission matrix | Role × action × tenant/school/campus/class/resource scopes | Security + product | P0 |
| DOC-009 | Workflow specifications | Main success, exception, cancellation, reversal and offline paths | Product + QA | P0 |
| DOC-010 | Acceptance/test catalogue | Given/When/Then tests and release gates | QA lead | P0 |
| DOC-011 | Threat model | Trust boundaries, abuse cases, controls and residual risks | Security lead | P0 |
| DOC-012 | Deployment/runbooks | Install, upgrade, backup, restore, outage, node replacement, support | Operations | Before pilot |
| DOC-013 | External integrations | Bank, payment, SMS, WhatsApp, email and legal verification requirements | Product + integration owner | Before each integration |
| DOC-014 | UI/UX specification | Navigation, screen inventory, responsive/RTL, forms, empty/error states | Product + design | Before UI module |
| DOC-015 | Data migration specification | Source templates, mapping, validation, dry-run, rollback and sign-off | Product + implementation | Before pilot |
| DOC-016 | Operational policies | Retention, export, deletion, incident, RPO/RTO and support ownership | Product + operations | Before pilot |

## 7. Open decisions that must not be silently invented

The following are open unless an approved ADR or later signed decision resolves them:

1. Exact Tenant → School/Legal Entity → Campus cardinality and legal customer identity.
2. UUID versus another primary-key strategy; recommended UUIDs are not yet a signed universal decision.
3. RLS policy context, database roles, ownership and connection-pooling implementation.
4. Exact local node ownership for shared-group and per-campus deployment.
5. Which campus/master-data aggregates can have one writer, and which require a command/approval workflow.
6. Whether Fees and Finance posting share a database transaction or use an outbox-driven posting workflow.
7. Accounting basis, chart-of-accounts template, fiscal year, receipt numbering and approval limits.
8. The first supported bank statement formats and payment-provider contracts.
9. Final board/curriculum content scope and content usage rights.
10. Final auth provider, MFA requirements and identity lifecycle.
11. Data retention/deletion periods, child/guardian notices and Pakistan-specific legal review.
12. Local node hardware baseline, OS support, support responsibilities and backup destination.
13. Licence lease lifetime and grace period (seven days remains a proposal).
14. Cloud replication and remote parent portal data projection.
15. Final demo session timings and isolated storage mechanism.
16. Final commercial pricing/taxes, support SLA and onboarding charges.

Each decision must receive an ADR or approved decision-log entry with context, options, decision, consequences, owner and date.

## 8. Documentation-only delivery sequence

1. Approve glossary, actors, scope and cross-plane ownership.
2. Approve the operational and control-plane ERDs plus field data dictionary.
3. Approve migration, constraint, indexing and RLS strategy.
4. Approve role/resource scope matrix and threat model.
5. Approve API DTOs, error catalogue, event contracts and idempotency rules.
6. Approve end-to-end workflows and Given/When/Then acceptance tests.
7. Approve deployment, backup/restore, monitoring and support runbooks.
8. Complete external verification and record unresolved dependencies.
9. Conduct a documentation consistency review and product-owner/technical-lead sign-off.
10. Only then authorize Phase 1 implementation.

## 9. Documentation Definition of Done

Documentation is complete for a module only when:

- Its purpose, actors, non-goals and dependencies are defined.
- Every persisted entity has a data dictionary entry.
- Relationships, keys, constraints and indexes are described.
- API request/response contracts and authorization are defined.
- State transitions and failure/recovery paths are documented.
- Audit, privacy, retention and export requirements are specified.
- Acceptance tests include positive, negative, duplicate/retry and permission scenarios.
- Open decisions are listed with an owner; no invented assumption is presented as confirmed.
- Related diagrams, ADRs, progress trackers and document links are consistent.
- Product owner and technical lead have approved the version.

## 10. Change control

- Keep this document and companion specifications versioned in Git.
- Use an ADR for architecture/security/data ownership decisions with lasting consequences.
- Update the data dictionary, API catalog, acceptance tests and progress tracker together when a contract changes.
- Mark a decision as superseded rather than deleting its history.
- Never update the tracker to Completed from discussion alone; require implementation evidence for implementation tasks and approval evidence for documentation tasks.

## 11. Related documents

- `docs/00_DECISIONS_AND_ASSUMPTIONS.md`
- `docs/03_MODULAR_MONOLITH_ARCHITECTURE.md`
- `docs/04_DATABASE_BLUEPRINT.md`
- `docs/05_OFFLINE_PWA_AND_SYNC.md`
- `docs/07_SECURITY_AND_QUALITY_REQUIREMENTS.md`
- `docs/08_DEVELOPMENT_ROADMAP.md`
- `docs/14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md`
- `docs/18_CAMPUS_DATA_ACCESS_AND_DEPLOYMENT_SCENARIOS.md`
- `docs/36_PLATFORM_DATABASE_BLUEPRINT.md`
- `docs/37_PLATFORM_API_CATALOG.md`
- `docs/38_PLATFORM_SECURITY_REQUIREMENTS.md`


## 12. Post-merge documentation reconciliation

**Current reconciliation baseline:** `main` at `9d8d27caa65f9bd0a68764671d451495df28df77` (19 November 2026), after PRs #5, #6 and #7. This section supplements, and does not rewrite, the original implementation inventory and reviewed commit recorded above.

### 12.1 Changes incorporated by merged pull requests

| Change | Repository evidence | Documentation disposition |
|---|---|---|
| PR #5 — documentation governance | Canonical task plans, seven task-register partitions, DOC-54, DOC-59, DOC-60 and documentation-quality validator/workflow | Adopted. The flat DOC-00–62 paths remain stable. Remaining count and validator gaps are handled by the documentation alignment workstream. |
| PR #6 — SaaS tenant and reference adoption | DOC-61, DOC-62, TEN-001–003, SAAS-001–014 and cross-document updates | Documented proposal and traceability are present. DEC-001, DEC-004, DEC-008 and DEC-020 remain open; no SaaS implementation is implied. |
| PR #7 — repo-local agent skills | 51 skills under `.agents/skills/`, catalogue README and precedence subsection in `AGENTS.md` | Supplemental engineering guidance only. It does not alter canonical requirements, approved ADRs or task-status authority. |

### 12.2 Current documentation and register state

At the pre-alignment baseline, the seven canonical task-register CSV partitions contained 297 task rows and the requirement traceability CSV contained 37 data rows. The current alignment change adds a dedicated documentation-governance partition with 12 DOCALIGN tasks; on this branch the register model therefore contains eight partitions and 309 task rows, while the requirement count remains 37.

The new [categorized documentation catalogue](README.md) preserves all numbered file paths and provides a single browsable entry point. Detailed implementation plans remain under `docs/task-plans/`; ADR records remain under `docs/decisions/`; templates remain under `docs/templates/`; machine-readable status remains under `registers/`.

### 12.3 Specification existence is not approval or implementation

| Deliverable | Current canonical documents | Documentation state | Remaining gate |
|---|---|---|---|
| Product scope and glossary | 00–02, 56 | Documented | Product-owner review and approval |
| Requirements traceability | 51 and `registers/requirement-traceability.csv` | 37 current requirement rows mapped or explicitly unassigned | Product, technical and QA sign-off |
| Operational data dictionary | 43 | Draft specification exists | Domain-owner review; field-level completeness |
| Domain ERD and migrations | 44 | Draft specification exists | Technical/DBA/security review and migration verification |
| Cloud platform data model | 36 | Draft specification exists | Tenant cardinality and ownership decisions remain open |
| API and event contracts | 37, 45 | Catalogues/specification exist | Contract review and implementation evidence |
| Authorization and tenant isolation | 07, 38, 46, 62 | Requirements and proposed SaaS controls documented | DEC-004/008 approval and executable security tests |
| Workflow acceptance tests | 47 and SaaS cases in 62 | Scenarios documented | Test implementation and execution evidence |
| Hybrid operations and recovery | 16, 48, 52 | Runbooks documented | Pilot-specific operating approval and restore drills |
| UI/UX and screen catalogue | 49 | Catalogue documented | Product/design review and usability/RTL verification |
| Data migration and portability | 50 | Specification documented | Pilot mapping, dry-run and reconciliation evidence |
| SaaS tenant foundation | 61–62 | Adoption plan and implementation proposal documented | DEC-001/004/008/020 plus code, migrations, tests and operational evidence |
| External integrations and legal/privacy | 19, 37, 45, 50, 60 | Dependencies and verification needs identified | Provider contracts and legal/security verification |

The existence of these files establishes documentation coverage only. It does not establish approval, feature completion, test implementation, successful test execution or legal compliance.

### 12.4 Remaining documentation-governance actions

The current alignment work is tracked in [DOCUMENTATION_ALIGNMENT_TASK_PLAN.md](task-plans/DOCUMENTATION_ALIGNMENT_TASK_PLAN.md) and `registers/task-register-documentation-governance.csv`. Its tasks cover the DOC-60 table, current counts, navigation, historical reconciliation, metadata semantics and validator expansion. The plan's task statuses are authoritative only in the CSV partition.

The original document 42 implementation snapshot remains tied to its recorded reviewed commit. Do not reinterpret that snapshot as a new code audit of the post-PR #7 repository.

## 13. Current-state audit after merged PR #8 (19 November 2026)

- **Audit baseline:** `a-kabeer/SchoolOS` `main` at `3cc2a02342f60af124e04aeb86563c331b497a1f`.
- **Audit method:** GitHub repository tree inspection, current documentation and governance-source review, task-register row counting, inspection of all 51 skill entry points, and targeted review of Cloud API, Local API, frontend shell, tests and the documentation-quality workflow.
- **Scope:** Documentation architecture, agent guidance, skills discoverability, traceability and implementation/documentation alignment. No runtime code or schema changes were made by this audit.

### 13.1 Repository and governance findings

| Area | Verified current state | Finding |
|---|---|---|
| Documentation layout | Stable numbered documents DOC-00–DOC-62; focused `decisions/`, `task-plans/` and `templates/` directories | Preserve the existing flat numbered structure and paths; no mass relocation is justified. |
| Navigation | Root `README.md`, categorized `docs/README.md`, ADR index, task-plan index and register guide exist | The catalogue covers the numbered documents, but it needs an explicit seven-source knowledge map for agents and maintainers. |
| Task-status authority | Eight `registers/task-register-*.csv` partitions; 309 data rows in total; 37 requirement-traceability rows | The eight partitions reconcile to the counts manifest and remain one logical task register. Do not create a Markdown task-status tracker. |
| Detailed delivery plans | Five canonical workstream plans under `docs/task-plans/`; numbered documents 17, 25, 28 and 39 are summaries/entry points | Keep detailed execution plans in `docs/task-plans/`; make DOC-08 the phase-level master roadmap and link to the workstream plans. |
| Documentation governance | DOC-54 defines metadata, lifecycle and authority; `scripts/validate-docs.mjs` and `.github/workflows/documentation-quality.yml` exist | Retain the existing quality gate. Its validation must be run against the resulting PR branch; repository-file inspection alone is not a passing test result. |
| Agent instructions | Root `AGENTS.md` is the repository-wide authority and already defines precedence, security and architecture invariants | Add one explicit task-execution protocol and knowledge-source routing here rather than creating a second instruction root. |
| Agent skills | 51 `SKILL.md` entry points under `.agents/skills/`; index identifies 29 Tier 1 and 22 Tier 2 skills | Preserve all 51. Improve task-to-skill activation guidance; do not copy skill bodies into general documentation or assume every skill applies to every task. |
| Decision governance | DOC-60 contains the decision closure register; ADRs remain under `docs/decisions/` | Preserve all Open decisions. A proposal, skill or implementation plan must not close a decision. |

### 13.2 Verified implementation baseline at the audit commit

The current source tree remains a foundation scaffold, consistent with the earlier implementation finding, with the following current-snapshot evidence:

- The .NET solution has Cloud API and Local API hosts. Each host currently registers OpenAPI and exposes a basic `/health` endpoint; the inspected host entry points do not register database, authentication, authorization or business-module services.
- The production C# source files visible in the tree are the two host entry points plus four BuildingBlocks source files (`ITenantContext`, `Entity`, `Result` and telemetry constants). Two test source files cover Shared Kernel behavior and a module-boundary rule; this is not evidence of end-to-end tenant isolation or school workflows.
- The ten school-module directories and four platform-module directories remain placeholders (`.gitkeep` only). No EF Core `DbContext`, migration set or checked-in operational SQL schema appears in the current source tree.
- The React/Vite frontend remains a shell. `frontend/schoolos-web/src/App.tsx` renders the Phase 0 foundation placeholder; no completed school workflow is evidenced by that component.
- The only workflow file in `.github/workflows/` is `documentation-quality.yml`. It validates documentation and registers; it is not a .NET/frontend build, test or deployment pipeline.
- The repository includes a PostgreSQL development Compose service. Its presence is development infrastructure, not proof that either API host is connected to a configured operational database.

**Implementation conclusion:** SchoolOS is still at the documented foundation/scaffold stage in this audited main snapshot. The Modular Monolith, Cloud Control Plane, Local Data Plane, SaaS tenant isolation, identity, business modules, billing/licensing and AI capabilities remain architectural/product specifications or plans unless separate executable evidence is cited. Do not claim those capabilities are implemented or verified.

### 13.3 Refactor actions justified by this audit

1. Publish an explicit seven-source knowledge map using existing authorities: root `README.md` for project overview; DOC-03 with DOC-14 and related architecture specifications for system architecture; DOC-01 and linked domain specifications for functional requirements; DOC-62 with DOC-46 and the relevant data/API contracts for multi-tenancy; DOC-08 with the detailed workstream plans for the master implementation roadmap; the eight CSV partitions with `registers/README.md` for implementation/task status; and root `AGENTS.md` for the agent workflow.
2. Add a concise, ordered agent task-execution protocol to `AGENTS.md`, including scoped context loading, relevant skills, source inspection, implementation limits, tests and evidence-based reporting.
3. Extend the skills catalogue with activation guidance tied to SchoolOS's actual .NET 10, React/TypeScript, PostgreSQL, hybrid deployment, multi-tenancy, security, testing and documentation responsibilities. Preserve every existing skill and its companion files.
4. Clarify DOC-08 as the phase-level master implementation plan; leave task-level status exclusively in the CSV partitions.
5. Update relevant navigation and source-baseline references without renumbering existing documents, changing approved decisions, or rewriting historical snapshots.

### 13.4 Validation boundary

The audit verified repository contents and counted the task-register CSV rows. It did not execute `node scripts/validate-docs.mjs`, .NET builds, frontend builds or application tests. The documentation-quality GitHub Actions workflow is the required automated validation for the PR; report its actual result after GitHub runs it. No runtime code, database schema, API behavior or product policy was changed as part of this audit.
