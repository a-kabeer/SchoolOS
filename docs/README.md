# SchoolOS Documentation Catalogue

This is the categorized index for canonical SchoolOS documentation. The numbered document IDs and file paths are stable; categories are navigation aids, not a new folder hierarchy.

**Authority:** The root [README](../README.md) is the project overview. [AGENTS.md](../AGENTS.md) governs contributor and AI-agent workflow. Product/engineering requirements live in the canonical numbered documents; approved architecture decisions live in [the ADR index](13_ADR_INDEX.md) and `decisions/`; task status lives only in [the machine-readable task registers](../registers/README.md). Repo-local skills are supplemental guidance, not policy.

## 1. Getting started and product foundation

| Document | Purpose |
|---|---|
| [00 — Decisions and Assumptions](00_DECISIONS_AND_ASSUMPTIONS.md) | Product direction, confirmed choices and assumptions |
| [01 — Product Requirements](01_PRODUCT_REQUIREMENTS.md) | Product scope, actors and requirements |
| [02 — Modules and Workflows](02_MODULES_AND_WORKFLOWS.md) | School operations and module responsibilities |
| [11 — Open Questions](11_OPEN_QUESTIONS.md) | Unresolved product and implementation questions |
| [12 — Competitor Research Notes](12_COMPETITOR_RESEARCH_NOTES.md) | Research observations and competitor comparisons |
| [56 — Glossary](56_GLOSSARY.md) | Canonical SchoolOS terminology |

## 2. Core architecture, deployment and data

| Document | Purpose |
|---|---|
| [03 — Modular Monolith Architecture](03_MODULAR_MONOLITH_ARCHITECTURE.md) | Application boundaries and module structure |
| [04 — Database Blueprint](04_DATABASE_BLUEPRINT.md) | Operational database baseline |
| [05 — Offline PWA and Sync](05_OFFLINE_PWA_AND_SYNC.md) | Offline application behavior and synchronization principles |
| [14 — Hybrid Cloud/Local Architecture](14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md) | Cloud Control Plane and Local Data Plane |
| [16 — Local Security, Sync and Operations](16_LOCAL_SECURITY_SYNC_AND_OPERATIONS.md) | Local-node security and operating model |
| [18 — Campus Data Access and Deployment Scenarios](18_CAMPUS_DATA_ACCESS_AND_DEPLOYMENT_SCENARIOS.md) | Multi-campus access and deployment profiles |
| [19 — Technical References](19_TECHNICAL_REFERENCES.md) | Technical source material |
| [36 — Platform Database Blueprint](36_PLATFORM_DATABASE_BLUEPRINT.md) | Cloud platform data model |
| [37 — Platform API Catalog](37_PLATFORM_API_CATALOG.md) | Platform API families |
| [43 — Operational Domain Data Dictionary](43_OPERATIONAL_DOMAIN_DATA_DICTIONARY.md) | Operational entities and fields |
| [44 — PostgreSQL ERD and Migration Specification](44_POSTGRESQL_ERD_AND_MIGRATION_SPEC.md) | Database relationships, constraints and migrations |
| [45 — API and Integration Event Contracts](45_API_AND_INTEGRATION_EVENT_CONTRACTS.md) | API and event contract conventions |
| [62 — SaaS Tenant Implementation Blueprint](62_SAAS_TENANT_IMPLEMENTATION_BLUEPRINT.md) | Proposed tenant, membership, isolation and provisioning foundation |

## 3. AI platform

| Document | Purpose |
|---|---|
| [06 — AI Intelligence Specification](06_AI_INTELLIGENCE_SPEC.md) | AI product capabilities and safeguards |
| [20 — AI Architecture and Hosting](20_AI_ARCHITECTURE_AND_HOSTING.md) | Hybrid AI hosting architecture |
| [21 — AI Provider Gateway and Privacy](21_AI_PROVIDER_GATEWAY_AND_PRIVACY.md) | Provider abstraction and privacy routing |
| [22 — AI RAG and Tool Calling](22_AI_RAG_AND_TOOL_CALLING.md) | Retrieval and authorized tool invocation |
| [23 — AI Training and Governance](23_AI_TRAINING_AND_GOVERNANCE.md) | Training, data governance and human review |
| [24 — AI Pricing and Usage Metering](24_AI_PRICING_AND_USAGE_METERING.md) | AI commercial usage model |
| [25 — AI Implementation Task Plan Summary](25_AI_IMPLEMENTATION_TASK_PLAN.md) | Entry point to AI delivery work |
| [26 — AI Progress Tracker](26_AI_PROGRESS_TRACKER.md) | AI workstream progress summary |

Related decisions: [ADR-015](decisions/ADR-015-AI-HYBRID-HOSTING-AND-OFFLINE.md), [ADR-016](decisions/ADR-016-AI-PROVIDER-GATEWAY-AND-PRIVACY-ROUTING.md), [ADR-017](decisions/ADR-017-AI-RAG-AND-TOOL-CALLING.md), [ADR-018](decisions/ADR-018-AI-TRAINING-AND-CUSTOMER-DATA-GOVERNANCE.md).

## 4. Commercial, school operations and customer platform

| Document | Purpose |
|---|---|
| [15 — Subscription, Licensing and Offline Enforcement](15_SUBSCRIPTION_LICENSING_AND_OFFLINE_ENFORCEMENT.md) | Subscription, licence and Restricted Mode policy |
| [27 — Commercial Pricing Strategy](27_COMMERCIAL_PRICING_STRATEGY.md) | Commercial pricing assumptions and proposals |
| [28 — Commercial Billing Task Plan Summary](28_COMMERCIAL_BILLING_IMPLEMENTATION_TASK_PLAN.md) | Entry point to billing delivery work |
| [29 — Business & Customer Platform Requirements](29_BUSINESS_CUSTOMER_PLATFORM_REQUIREMENTS.md) | BCP scope and audiences |
| [30 — Public Website and CMS](30_PUBLIC_WEBSITE_CMS_SPECIFICATION.md) | Public site and content management |
| [31 — Demo Platform](31_DEMO_PLATFORM_SPECIFICATION.md) | Demo experience requirements |
| [32 — Registration and Onboarding](32_REGISTRATION_ONBOARDING_SPECIFICATION.md) | Customer registration and approval flow |
| [33 — Customer Portal](33_CUSTOMER_PORTAL_SPECIFICATION.md) | Customer-facing account and subscription portal |
| [34 — Super Admin Console](34_SUPER_ADMIN_CONSOLE_SPECIFICATION.md) | Platform operations interface |
| [35 — Support Center](35_SUPPORT_CENTER_SPECIFICATION.md) | Support workflows and channels |
| [39 — BCP Task Plan Summary](39_BUSINESS_CUSTOMER_PLATFORM_TASK_PLAN.md) | Entry point to BCP delivery work |
| [40 — BCP Progress Tracker](40_BUSINESS_CUSTOMER_PLATFORM_PROGRESS_TRACKER.md) | BCP progress summary |
| [41 — Ephemeral Session-Based Demo](41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md) | Temporary synthetic demo workspace |
| [55 — Guardian and Household Requirements](55_GUARDIAN_AND_HOUSEHOLD_REQUIREMENTS.md) | Guardian, household and payer requirements |

Related decisions: [ADR-019](decisions/ADR-019-ORGANIZATION-SUBSCRIPTION-AND-METERING.md), [ADR-020](decisions/ADR-020-BUSINESS-CUSTOMER-PLATFORM.md), [ADR-021](decisions/ADR-021-EPHEMERAL-SESSION-BASED-DEMO.md).

## 5. Security, quality and operations

| Document | Purpose |
|---|---|
| [07 — Security and Quality Requirements](07_SECURITY_AND_QUALITY_REQUIREMENTS.md) | Security and non-functional baseline |
| [38 — Platform Security Requirements](38_PLATFORM_SECURITY_REQUIREMENTS.md) | Cloud platform security |
| [46 — Authorization and Tenant Isolation Matrix](46_AUTHORIZATION_AND_TENANT_ISOLATION_MATRIX.md) | Role, permission and resource-scope rules |
| [47 — Workflow Acceptance Test Catalogue](47_WORKFLOW_ACCEPTANCE_TEST_CATALOGUE.md) | Requirement-level acceptance scenarios; not test execution evidence |
| [48 — Hybrid Deployment Operations and Recovery Runbook](48_HYBRID_DEPLOYMENT_OPERATIONS_AND_RECOVERY_RUNBOOK.md) | Deployment, recovery and support operations |
| [49 — Operational UI/UX and Screen Catalogue](49_OPERATIONAL_UI_UX_AND_SCREEN_CATALOGUE.md) | Screens, states, responsive behavior and RTL requirements |
| [50 — Data Migration, Import and Export](50_DATA_MIGRATION_IMPORT_EXPORT_SPECIFICATION.md) | Customer data migration and portability |
| [52 — OCI Staging and Automated QA Runbook](52_OCI_STAGING_AND_AUTOMATED_QA_RUNBOOK.md) | Developer/pilot staging and automated QA |
| [53 — Staging QA Progress Tracker](53_STAGING_QA_PROGRESS_TRACKER.md) | Staging workstream progress summary |
| [57 — Offline Sync and Manual Data Exchange](57_OFFLINE_SYNC_AND_MANUAL_DATA_EXCHANGE.md) | Device-to-node and node-to-cloud exchange |

## 6. Delivery and project controls

| Document | Purpose |
|---|---|
| [08 — Development Roadmap](08_DEVELOPMENT_ROADMAP.md) | Phased delivery roadmap and gates |
| [09 — Progress Tracker](09_PROGRESS_TRACKER.md) | Project-level progress summary |
| [10 — Risk Register](10_RISK_REGISTER.md) | Project risk tracking |
| [17 — Hybrid Deployment Task Plan Summary](17_HYBRID_DEPLOYMENT_TASK_PLAN.md) | Entry point to hybrid delivery work |
| [42 — Documentation Completion and Gap Register](42_DOCUMENTATION_COMPLETION_AND_GAP_REGISTER.md) | Historical baseline and current documentation gap assessment |
| [51 — Requirements Traceability and Documentation Sign-off](51_REQUIREMENTS_TRACEABILITY_AND_DOCUMENTATION_SIGNOFF.md) | Requirement-to-specification/test mapping and approval gate |
| [59 — Task Register and Traceability Guide](59_TASK_REGISTER_AND_TRACEABILITY_GUIDE.md) | Canonical task-status and lineage rules |

Detailed delivery plans: [task-plan catalogue](task-plans/README.md). Canonical task statuses and machine-readable requirement traceability: [register guide](../registers/README.md).

## 7. Governance, decisions and reference adoption

| Document | Purpose |
|---|---|
| [13 — ADR Index](13_ADR_INDEX.md) | Decision record catalogue and historical ADR coverage |
| [54 — Documentation Standards](54_DOCUMENTATION_STANDARDS.md) | Metadata, authority, evidence and change control |
| [58 — Documentation Refactor v2.1 Migration Report](58_DOCUMENTATION_REFACTOR_V2_1_MIGRATION_REPORT.md) | Historical migration and post-merge reconciliation |
| [60 — Architecture and Security Decision Closure Register](60_ARCHITECTURE_AND_SECURITY_DECISION_CLOSURE_REGISTER.md) | Open decision authority and closure evidence |
| [61 — Reference Project Adoption and Integration Plan](61_REFERENCE_PROJECT_ADOPTION_AND_INTEGRATION_PLAN.md) | Adopted/adapted/excluded reference-project practices |

Standalone decision records: [ADR-011](decisions/ADR-011-HYBRID-CLOUD-LOCAL.md), [ADR-012](decisions/ADR-012-SIGNED-OFFLINE-LICENSE-LEASE.md), [ADR-013](decisions/ADR-013-SYNC-HUB-EVENT-BASED.md), [ADR-014](decisions/ADR-014-CAMPUS-DATA-ACCESS-AND-EXCHANGE.md), [ADR-015](decisions/ADR-015-AI-HYBRID-HOSTING-AND-OFFLINE.md), [ADR-016](decisions/ADR-016-AI-PROVIDER-GATEWAY-AND-PRIVACY-ROUTING.md), [ADR-017](decisions/ADR-017-AI-RAG-AND-TOOL-CALLING.md), [ADR-018](decisions/ADR-018-AI-TRAINING-AND-CUSTOMER-DATA-GOVERNANCE.md), [ADR-019](decisions/ADR-019-ORGANIZATION-SUBSCRIPTION-AND-METERING.md), [ADR-020](decisions/ADR-020-BUSINESS-CUSTOMER-PLATFORM.md) and [ADR-021](decisions/ADR-021-EPHEMERAL-SESSION-BASED-DEMO.md). The ADR index, rather than this catalogue, is authoritative for decision status.

## 8. Engineering guides, templates and supplemental skills

- [Development Environment and Tooling](DEVELOPMENT_ENVIRONMENT_AND_TOOLING.md)
- [Git Workflow and Branching Strategy](GIT_WORKFLOW_AND_BRANCHING_STRATEGY.md)
- [Canonical detailed task plans](task-plans/README.md)
- Reusable documentation templates: [ADR](templates/ADR_TEMPLATE.md), [change proposal](templates/CHANGE_PROPOSAL_TEMPLATE.md), [module specification](templates/MODULE_SPEC_TEMPLATE.md) and [runbook](templates/RUNBOOK_TEMPLATE.md).
- [Repository-wide contributor and AI-agent authority](../AGENTS.md)
- [Supplemental repo-local agent skills](../.agents/skills/README.md) — subordinate to AGENTS.md and canonical SchoolOS documents.

## Document lifecycle and evidence

A document's presence in this catalogue means it is discoverable, not approved or implemented. Read its metadata and the relevant decision/traceability record. Accepted direction, approved requirements, implementation status and executed test evidence are separate states. Open decisions remain open until the owner-approved closure evidence is recorded.

Last reviewed: 2026-11-19.
