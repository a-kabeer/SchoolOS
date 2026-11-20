# SchoolOS — Project Blueprint

**Document set:** Product, Technical & Business Platform Blueprint v1.7 with the v1.8 implementation-ready baseline and v2.1 documentation reconciliation supplement  
**Prepared:** 9 November 2026  
**Documentation reconciliation:** 19 November 2026  
**Working product name:** SchoolOS (provisional; final brand not selected)  
**Status:** Product direction, hybrid blueprint, and Phase 1 foundation + Student Lifecycle vertical slice (`Identity`, `Organization` including academic structures, `Admissions`, `Students`, durable offline licensing, and React EN/UR/AR UI) are implemented and verified on the repository baseline; detailed discovery, Phase 2+ modules, and open `DEC-*` / commercial decisions remain open.

## What this project is

SchoolOS is a proposed multi-tenant, AI-enabled school operating platform for private schools and multi-campus school groups in Pakistan. Its intended strengths are:

1. Dependable fee collection, accounting controls and bank reconciliation.
2. Practical offline operation for important school workflows.
3. Configurable academic and curriculum support for Pakistani boards and Cambridge pathways.
4. AI tools that assist school finance, learning, examinations, reporting, voice interaction and student support—with human oversight.

## Confirmed decisions

- **Target market:** Private schools and private school chains.
- **Curriculum ambition:** All relevant Pakistani boards plus Cambridge. Board-specific content must be validated and loaded; architecture support does not mean every syllabus is already populated.
- **Offline ambition:** Broad practical offline support, with explicit limits for sensitive financial approvals and bank verification.
- **AI scope:** AI School Auditor, Learning Recovery, Exam Intelligence, Business Intelligence Copilot, Urdu Voice Assistant and Student Early Intervention.
- **AI privacy:** Hybrid; permitted/minimized external API use, sensitive processing via private/local routes by policy.
- **Offline AI:** Optional premium campus-local inference; core school operations never require AI.
- **AI pricing:** Limited included AI allowance + advanced paid add-on; private/local inference priced separately.
- **Architecture:** Modular Monolith, not an initial microservices system.
- **Backend:** Modern ASP.NET Core / C#; target .NET 10 LTS.
- **Frontend:** React + TypeScript.
- **Database:** PostgreSQL; separate operational database per Local Data Plane plus a separate Cloud Control Plane database, each with module-owned schemas.
- **Developer partner:** The user's friend will contribute as a team developer and has ASP.NET Core and React experience.
- **Deployment:** Hybrid Cloud Control Plane + Local Data Plane nodes. The school business application remains a Modular Monolith; each site node runs ASP.NET Core, React and PostgreSQL.
- **Subscription enforcement:** Node-bound signed offline licence leases with BOTH automatic online renewal and manual signed `.lic` file/USB fallback. After the grace period, Restricted Mode preserves historical viewing, reports/export, backup, billing/licence renewal and support while blocking new operational writes. A valid renewal reactivates the existing installation without a software update. Seven-day grace remains a proposal pending commercial approval.
- **Offline operation:** Local school operations run against a Local Node on the LAN; PWA caching is supplementary, not the authoritative offline backend.
- **Synchronization:** Event-based hub-and-spoke with transactional Outbox/Inbox, node identity, idempotency and explicit conflict handling.
- **Topology:** Shared group node or per-campus edge nodes. Every campus that must be independently operable while disconnected needs its own edge node. Also define three deployment profiles: Cloud Centralized, Disconnected Local + Manual Data Exchange, and Hybrid Local + Cloud Sync. The strategic baseline remains Hybrid.

## Suggested reading paths

Read the repository-wide [contributor and AI-agent guide](AGENTS.md) before changing code, tests, configuration or CI. Use [61 — Reference Project Adoption and Integration Plan](docs/61_REFERENCE_PROJECT_ADOPTION_AND_INTEGRATION_PLAN.md) to understand which practices from Education-Portal and eSchool are adopted, adapted or explicitly excluded.

Start with the [categorized documentation catalogue](docs/README.md), which includes the seven-source knowledge map, then follow the path that matches your role. AI coding agents must follow [AGENTS.md](AGENTS.md), load only context and skills relevant to the assigned task, and verify implementation claims against source code and tests. Use document 56 for terminology, document 60 for unresolved architecture/security decisions, and document 62 for the proposed SaaS tenant implementation baseline.

| Reader | Recommended path |
|---|---|
| Product owner | 00–02 → 11 → 29 → 51 → 56 → 60–62 |
| Backend developer | 03–05 → 14–18 → 36 → 43–47 → 60–62 |
| Frontend developer | 01–02 → 45–46 → 49 → 56 → relevant module specification |
| DevOps / operations | 14–16 → 48 → 52–53 → 60 → Hybrid and documentation task plans |
| AI developer | 06, 20–26 → ADR-015–018 → AI detailed plan and registers |
| Commercial / BCP | 27–41 → ADR-019–021 → BILL/BCP detailed plans and registers |
| QA / security | 07 → 38 → 46–48 → 51–53 → 60–62 |

### Canonical navigation

- [Categorized documentation catalogue](docs/README.md) · [Project overview](README.md)
- [Product requirements](docs/01_PRODUCT_REQUIREMENTS.md) · [Module workflows](docs/02_MODULES_AND_WORKFLOWS.md)
- [ADR index](docs/13_ADR_INDEX.md) · [Open questions](docs/11_OPEN_QUESTIONS.md) · [Decision closure register](docs/60_ARCHITECTURE_AND_SECURITY_DECISION_CLOSURE_REGISTER.md)
- [Data dictionary](docs/43_OPERATIONAL_DOMAIN_DATA_DICTIONARY.md) · [ERD and migrations](docs/44_POSTGRESQL_ERD_AND_MIGRATION_SPEC.md) · [API/event contracts](docs/45_API_AND_INTEGRATION_EVENT_CONTRACTS.md)
- [Authorization matrix](docs/46_AUTHORIZATION_AND_TENANT_ISOLATION_MATRIX.md) · [Acceptance tests](docs/47_WORKFLOW_ACCEPTANCE_TEST_CATALOGUE.md) · [Operations runbook](docs/48_HYBRID_DEPLOYMENT_OPERATIONS_AND_RECOVERY_RUNBOOK.md)
- [Task Register guide](docs/59_TASK_REGISTER_AND_TRACEABILITY_GUIDE.md) · [Registers](registers/README.md) · [Documentation standards](docs/54_DOCUMENTATION_STANDARDS.md) · [Reference adoption plan](docs/61_REFERENCE_PROJECT_ADOPTION_AND_INTEGRATION_PLAN.md)

## Important status note

This archive captures the product direction, working requirements and proposed hybrid technical blueprint discussed so far. The hybrid subscription, licensing, local-node and sync approach is a design baseline, but it is **not yet a fully implementation-ready specification**. The seven-day grace window is proposed, not commercially approved. The exact shared-node vs per-campus-node topology must be chosen per pilot; independent campus operation during WAN outages requires per-campus nodes. Local remote revocation cannot be immediate while the node is fully disconnected, and software-only enforcement can be defeated by a customer with unrestricted OS root access. The database catalogue is preliminary; detailed field definitions, constraints, API contracts, screen designs, exact workflows, board syllabus data, payment-provider agreements, legal review and pilot acceptance tests must be completed during discovery and design.

Do not treat vendor marketing claims, proposed performance targets, or example data in these documents as independently verified product performance.

## Archive contents

- Product requirements and module catalogue
- Finance and reconciliation workflows
- Modular Monolith architecture for ASP.NET Core and React
- Preliminary PostgreSQL schema/table catalogue
- PWA and offline synchronization design
- Six AI capability specifications and safeguards
- Hybrid AI hosting: cloud APIs, private inference and optional campus-local inference
- Provider Gateway, privacy classification, safe routing/fallback and provider-term review
- Separate local/cloud RAG boundaries, pgvector candidate, citations and allow-listed tool calling
- No shared model training by default; opt-in fine-tuning and human-reviewed evaluation
- AI credit ledger, usage metering, quotas, alerts and paid-add-on packaging
- AI-0 through AI-7 implementation task plan, AI progress tracker and ADR-015 through ADR-018
- Security and non-functional requirements
- Phased development task plan
- Progress tracker, risk register, open questions and ADR template
- Competitor landscape notes
- Hybrid Cloud Control Plane + Local Data Plane deployment design
- Subscription, signed offline licensing and restricted-mode policy
- Local-node security, synchronization, backup, update and operations runbook
- Three campus deployment profiles: disconnected manual exchange, cloud centralized, and hybrid local/cloud sync
- Main-campus Headmaster campus selector and consolidated campus reporting requirements
- Signed/encrypted, schema-versioned campus data packages with dry-run, idempotent imports and freshness indicators
- Hybrid deployment work packages and ADRs through ADR-014
- Business & Customer Platform (BCP): public website/CMS, instant and guided demos, self-registration plus manual review, customer portal, Super Admin Console and ticket/chat/WhatsApp support
- BCP preliminary cloud database entity catalogue, API families, screen/route inventory and security baseline
- BCP-0 through BCP-11 implementation work packages, BCP-001 through BCP-041 task register, separate progress tracker and ADR-020
- Ephemeral session-based demo: temporary synthetic workspace, logout revocation, close/refresh/multi-tab handling, server-side expiry, retryable cleanup, backup/log retention safeguards and ADR-021


## AI decisions added in v1.3

The AI hosting, privacy and commercial direction is now explicitly recorded: Hybrid AI privacy, optional premium offline/local AI, and limited included AI plus advanced paid AI. The design includes a central Provider Gateway, privacy-aware routing, separate local/cloud RAG boundaries, authorized typed tool calling, no shared model training by default, human review for consequential outputs and usage/credit controls. Provider brands, models, numeric prices, quotas and retention schedules remain open pending evaluation and contract review.

## Commercial pricing direction added in v1.5

The commercial discussion is captured as a **proposed analysis baseline, not final pricing approval**: one organization subscription, organization-wide unique active student count, first Local Node included, additional nodes potentially billed, school-provided local server, all standard core modules across capacity bands, monthly student snapshots, paid one-time onboarding, and monthly plus annual billing. The starting numeric preference is Rs. 2,500/month including 200 students, plus Rs. 10 per additional student/month. Rs. 1,000/month per additional node, Rs. 5,000 standard onboarding and 10% annual discount are unapproved test hypotheses only. See `docs/27_COMMERCIAL_PRICING_STRATEGY.md`, `docs/28_COMMERCIAL_BILLING_IMPLEMENTATION_TASK_PLAN.md` and ADR-019.

## Important status note for AI

All AI workstreams (AI-0 to AI-7) are **Not Started**. The archive documents decisions and a proposed implementation plan; it does not claim that the gateway, RAG, AI features, billing or local inference have been implemented.


## Licence policy decisions added in v1.4

The renewal choice is confirmed as **both** automatic renewal when a Local Node is online and manual signed `.lic` download/import (including approved USB transfer) when it is offline. The expiry choice is confirmed as **Restricted Mode** after grace, not destructive lockout: preserve historical records, read-only viewing, reports/export, backup/restore administration, billing/licence renewal and support; block new operational writes. Both renewal paths use the same signature validation and activation pipeline. A valid renewal restores the existing installation without a software update. Implementation remains Not Started, and the seven-day grace duration remains a proposal pending commercial approval.


## Business & Customer Platform added in v1.6

This version adds the SchoolOS Business & Customer Platform (BCP) in the Cloud Control Plane: Public Marketing Website/CMS, Demo Platform, Customer Portal, Super Admin Console and Support Center. Confirmed choices are both instant and personalized demos; both self-registration and manual review/approval; and both built-in tickets/live chat/help center and WhatsApp integration. Detailed requirements, screen/routes, preliminary entities, API families, security requirements, task plan and progress tracker are documented in files 29–40 and ADR-020. All BCP implementation work remains Not Started.

The BCP is not a gateway to unrestricted campus operational data. Customer portal and company admin surfaces use organization-scoped commercial/node metadata and approved projections. Demo uses synthetic data only. Registration, payment verification, subscription activation, node registration and licence issuance remain separate gates. Pricing values remain proposals until separately approved.


## Ephemeral session-based demo added in v1.7

The instant demo is a temporary session-bound workspace rather than a permanent demo account. Logout revokes access immediately; last-tab close, browser crash, inactivity or absolute lifetime are handled with best-effort close hints plus server-side client leases and expiry sweeps. Refresh and multiple tabs reconnect safely. The workspace contains synthetic data only and is cleaned through an idempotent retryable worker, including associated temporary files/cache/queues/resources. The recommended starting shape is a dedicated Demo Runtime store isolated from production and excluded from long-lived production/customer backups. Final storage isolation, timeouts and residual-data retention remain subject to threat/load testing and approval. All implementation tasks remain Not Started. See document 41 and ADR-021.


## Documentation completion baseline (v1.8)

The documentation-first supplement is in `docs/42` through `docs/60`. Read these after the original product/architecture baseline and before beginning Phase 1 implementation:

- `42_DOCUMENTATION_COMPLETION_AND_GAP_REGISTER.md` — repository evidence, documentation gaps, architecture boundaries and no-code gate.
- `43_OPERATIONAL_DOMAIN_DATA_DICTIONARY.md` — proposed Local Data Plane entities and logical fields.
- `44_POSTGRESQL_ERD_AND_MIGRATION_SPEC.md` — database boundaries, ERD, constraints, RLS and migration controls.
- `45_API_AND_INTEGRATION_EVENT_CONTRACTS.md` — Local/Cloud API families, errors, idempotency and events.
- `46_AUTHORIZATION_AND_TENANT_ISOLATION_MATRIX.md` — roles, resource scopes and Restricted Mode permissions.
- `47_WORKFLOW_ACCEPTANCE_TEST_CATALOGUE.md` — Given/When/Then business, security and operational tests.
- `48_HYBRID_DEPLOYMENT_OPERATIONS_AND_RECOVERY_RUNBOOK.md` — provisioning, backup, restore, sync, upgrade and incident procedures.
- `49_OPERATIONAL_UI_UX_AND_SCREEN_CATALOGUE.md` — role dashboards, screen inventory, states, Urdu/RTL and accessibility.
- `50_DATA_MIGRATION_IMPORT_EXPORT_SPECIFICATION.md` — onboarding imports, dry-run, reconciliation, data exchange and customer exports.
- `51_REQUIREMENTS_TRACEABILITY_AND_DOCUMENTATION_SIGNOFF.md` — requirement-to-test traceability and approval gate.
- `52_OCI_STAGING_AND_AUTOMATED_QA_RUNBOOK.md` — OCI staging deployment, GitHub Actions CI/CD, Playwright and optional Gemini/MCP QA.
- `53_STAGING_QA_PROGRESS_TRACKER.md` — staging task dependencies, blockers and acceptance evidence.

These specifications are drafts until reviewed and approved. Their existence does not imply implementation. Phase 1 coding must remain blocked until the sign-off checklist in document 51 is complete.

The separate developer/pilot OCI staging pipeline is documented in `docs/52_OCI_STAGING_AND_AUTOMATED_QA_RUNBOOK.md` and tracked in the canonical staging-QA task register. It complements document 48 but does not replace production Local Node operations.


## Documentation governance and v2.1 reconciliation

The v2.1 remediation package has been selectively reconciled with the current repository baseline. The existing numbered documentation and merged PR #2 / PR #3 changes remain authoritative; the alternate folder hierarchy from the ZIP was not copied as a competing structure. Use [docs/README.md](docs/README.md) as the detailed catalogue; it complements rather than replaces this project overview.

- [54 — Documentation Standards](docs/54_DOCUMENTATION_STANDARDS.md)
- [55 — Guardian and Household Requirements](docs/55_GUARDIAN_AND_HOUSEHOLD_REQUIREMENTS.md)
- [56 — SchoolOS Glossary](docs/56_GLOSSARY.md)
- [57 — Offline Sync and Manual Data Exchange](docs/57_OFFLINE_SYNC_AND_MANUAL_DATA_EXCHANGE.md)
- [58 — v2.1 Documentation Migration Report](docs/58_DOCUMENTATION_REFACTOR_V2_1_MIGRATION_REPORT.md)
- [59 — Task Register and Traceability Guide](docs/59_TASK_REGISTER_AND_TRACEABILITY_GUIDE.md)

The detailed H0–H7, AI-0–AI-7, BILL, BCP and documentation-governance task plans are in `docs/task-plans/`. The eight CSV partitions under `registers/` are the single task-status source. Documents 09, 26, 40 and 53 are summaries; document 51 and the requirement CSV contain requirement traceability. Reusable ADR, module-specification, runbook and change-proposal templates are in `docs/templates/`.

Document 54 is the adopted repository documentation standard. Document 60 consolidates remaining architecture/security gates without silently approving them. Task status and test evidence remain distinct from design approval and implementation.


## Reference-project adoption (DOC-61)

SchoolOS adopts selected engineering practices from [Education-Portal](https://github.com/ahmedshahjr/Education-Portal) and operational workflow research from [eSchool](https://github.com/a-kabeer/eSchool) without merging repositories or changing SchoolOS's modular-monolith, React, separate Cloud/Local database and offline-first direction. The adopted practices, exclusions, cross-document mapping and open module-entitlement decision are recorded in [DOC-61](docs/61_REFERENCE_PROJECT_ADOPTION_AND_INTEGRATION_PLAN.md). The root [AGENTS.md](AGENTS.md) is the authoritative contributor and AI-agent guide. Reference adoption is a documented direction, not a claim that the listed capabilities are implemented.


## SaaS tenant implementation blueprint (DOC-62)

[DOC-62 — SaaS Tenant Architecture and Implementation Blueprint](docs/62_SAAS_TENANT_IMPLEMENTATION_BLUEPRINT.md) specifies the v1 tenant implementation contract: Organization/Tenant → School → Campus hierarchy, global identity and tenant memberships, server-derived TenantContext, shared Cloud PostgreSQL isolation/RLS, one-tenant Local Nodes, tenant provisioning, node enrollment, subscription entitlements, sync validation, acceptance tests and delivery gates.

DOC-62 is a concrete implementation proposal, not a claim that SaaS tenant code is already implemented. DEC-001, DEC-004, DEC-008 and DEC-020 remain open until approved and supported by code, migrations, tests and operational evidence.
