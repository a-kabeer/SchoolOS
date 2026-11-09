# SchoolOS — Project Blueprint

**Document set:** Product, Technical & Business Platform Blueprint v1.7  
**Prepared:** 9 November 2026  
**Working product name:** SchoolOS (provisional; final brand not selected)  
**Status:** Product direction and BCP scope captured; detailed discovery and implementation decisions remain open. No implementation is implied.

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

## Suggested reading order

1. `docs/00_DECISIONS_AND_ASSUMPTIONS.md`
2. `docs/01_PRODUCT_REQUIREMENTS.md`
3. `docs/02_MODULES_AND_WORKFLOWS.md`
4. `docs/03_MODULAR_MONOLITH_ARCHITECTURE.md`
5. `docs/04_DATABASE_BLUEPRINT.md`
6. `docs/05_OFFLINE_PWA_AND_SYNC.md`
7. `docs/06_AI_INTELLIGENCE_SPEC.md`
8. `docs/07_SECURITY_AND_QUALITY_REQUIREMENTS.md`
9. `docs/08_DEVELOPMENT_ROADMAP.md`
10. `docs/09_PROGRESS_TRACKER.md`, `docs/10_RISK_REGISTER.md` and `docs/11_OPEN_QUESTIONS.md`
11. `docs/14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md`
12. `docs/15_SUBSCRIPTION_LICENSING_AND_OFFLINE_ENFORCEMENT.md`
13. `docs/16_LOCAL_SECURITY_SYNC_AND_OPERATIONS.md`
14. `docs/17_HYBRID_DEPLOYMENT_TASK_PLAN.md`
15. `docs/18_CAMPUS_DATA_ACCESS_AND_DEPLOYMENT_SCENARIOS.md`
16. `docs/decisions/ADR-014-CAMPUS-DATA-ACCESS-AND-EXCHANGE.md`
17. `docs/decisions/ADR-011-HYBRID-CLOUD-LOCAL.md`, `ADR-012-SIGNED-OFFLINE-LICENSE-LEASE.md`, `ADR-013-SYNC-HUB-EVENT-BASED.md`
18. `docs/19_TECHNICAL_REFERENCES.md`
19. `docs/20_AI_ARCHITECTURE_AND_HOSTING.md`
20. `docs/21_AI_PROVIDER_GATEWAY_AND_PRIVACY.md`
21. `docs/22_AI_RAG_AND_TOOL_CALLING.md`
22. `docs/23_AI_TRAINING_AND_GOVERNANCE.md`
23. `docs/24_AI_PRICING_AND_USAGE_METERING.md`
24. `docs/25_AI_IMPLEMENTATION_TASK_PLAN.md`
25. `docs/26_AI_PROGRESS_TRACKER.md`
26. `docs/decisions/ADR-015` through `ADR-018`
27. `docs/27_COMMERCIAL_PRICING_STRATEGY.md` and `docs/28_COMMERCIAL_BILLING_IMPLEMENTATION_TASK_PLAN.md`
28. `docs/decisions/ADR-019-ORGANIZATION-SUBSCRIPTION-AND-METERING.md`
29. `docs/29_BUSINESS_CUSTOMER_PLATFORM_REQUIREMENTS.md`
30. `docs/30_PUBLIC_WEBSITE_CMS_SPECIFICATION.md`
31. `docs/31_DEMO_PLATFORM_SPECIFICATION.md`
32. `docs/32_REGISTRATION_ONBOARDING_SPECIFICATION.md`
33. `docs/33_CUSTOMER_PORTAL_SPECIFICATION.md`
34. `docs/34_SUPER_ADMIN_CONSOLE_SPECIFICATION.md`
35. `docs/35_SUPPORT_CENTER_SPECIFICATION.md`
36. `docs/36_PLATFORM_DATABASE_BLUEPRINT.md`
37. `docs/37_PLATFORM_API_CATALOG.md`
38. `docs/38_PLATFORM_SECURITY_REQUIREMENTS.md`
39. `docs/39_BUSINESS_CUSTOMER_PLATFORM_TASK_PLAN.md`
40. `docs/40_BUSINESS_CUSTOMER_PLATFORM_PROGRESS_TRACKER.md`
41. `docs/decisions/ADR-020-BUSINESS-CUSTOMER-PLATFORM.md`
42. `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md` and `docs/decisions/ADR-021-EPHEMERAL-SESSION-BASED-DEMO.md`

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


## Commercial pricing analysis added in v1.5

The archive now records the selected pricing direction and implementation plan. The customer preference is organization-wide billing, school-owned hardware, first Local Node included, paid onboarding, all core modules in every package, monthly student snapshots and monthly/annual billing. Numeric prices beyond the initial Rs. 2,500/200/Rs. 10 starting preference remain proposals requiring pilot validation. No billing implementation is claimed.

## Licence policy decisions added in v1.4

The renewal choice is confirmed as **both** automatic renewal when a Local Node is online and manual signed `.lic` download/import (including approved USB transfer) when it is offline. The expiry choice is confirmed as **Restricted Mode** after grace, not destructive lockout: preserve historical records, read-only viewing, reports/export, backup/restore administration, billing/licence renewal and support; block new operational writes. Both renewal paths use the same signature validation and activation pipeline. A valid renewal restores the existing installation without a software update. Implementation remains Not Started, and the seven-day grace duration remains a proposal pending commercial approval.


## Business & Customer Platform added in v1.6

This version adds the SchoolOS Business & Customer Platform (BCP) in the Cloud Control Plane: Public Marketing Website/CMS, Demo Platform, Customer Portal, Super Admin Console and Support Center. Confirmed choices are both instant and personalized demos; both self-registration and manual review/approval; and both built-in tickets/live chat/help center and WhatsApp integration. Detailed requirements, screen/routes, preliminary entities, API families, security requirements, task plan and progress tracker are documented in files 29–40 and ADR-020. All BCP implementation work remains Not Started.

The BCP is not a gateway to unrestricted campus operational data. Customer portal and company admin surfaces use organization-scoped commercial/node metadata and approved projections. Demo uses synthetic data only. Registration, payment verification, subscription activation, node registration and licence issuance remain separate gates. Pricing values remain proposals until separately approved.


## Ephemeral session-based demo added in v1.7

The instant demo is a temporary session-bound workspace rather than a permanent demo account. Logout revokes access immediately; last-tab close, browser crash, inactivity or absolute lifetime are handled with best-effort close hints plus server-side client leases and expiry sweeps. Refresh and multiple tabs reconnect safely. The workspace contains synthetic data only and is cleaned through an idempotent retryable worker, including associated temporary files/cache/queues/resources. The recommended starting shape is a dedicated Demo Runtime store isolated from production and excluded from long-lived production/customer backups. Final storage isolation, timeouts and residual-data retention remain subject to threat/load testing and approval. All implementation tasks remain Not Started. See document 41 and ADR-021.
