# Changelog

## v1.0 — 2026-11-09

- Captured agreed target market: private schools and private school chains in Pakistan.
- Captured curriculum ambition: Pakistani boards plus Cambridge.
- Captured broad offline ambition with online-only controls for authoritative financial approvals/verification.
- Captured all six selected AI capabilities.
- Aligned technical direction with ASP.NET Core / .NET 10, React + TypeScript and PostgreSQL.
- Captured developer partner's ASP.NET Core and React expertise and team contribution.
- Added modular monolith design, preliminary database catalogue, offline sync design, security requirements, roadmap, progress tracker, risk register, open questions and competitor notes.


## v1.1 — 9 November 2026

- Changed deployment direction from cloud-VPS/PWA-first to Hybrid Cloud Control Plane + Local Data Plane.
- Added shared-group-node and per-campus edge-node deployment options.
- Added cloud/local database separation and record ownership rules.
- Added event-based hub-and-spoke synchronization with Outbox/Inbox and idempotency.
- Added signed node-bound offline license leases, proposed seven-day grace and restricted-mode policy.
- Added offline renewal-file process and explicit limitations around remote revocation, clock rollback and customer root access.
- Added local node security, backup, signed update and operational runbook.
- Added hybrid deployment implementation work packages, tracker tasks, risk entries and ADR-011 through ADR-013.
- Clarified that cloud AI is unavailable during internet outages unless a separately tested local AI runtime is deployed.
- Clarified that remote parent access requires a cloud portal projection/gateway or an approved VPN; local services must not be publicly exposed.

## v1.2 — 9 November 2026

- Added Scenario 1: disconnected local applications at each campus with controlled signed/encrypted manual data exchange to the main-campus node.
- Added Headmaster campus-wise and consolidated reporting from imported snapshots, including last-imported freshness indicators.
- Established imported branch data as read-only reporting replicas by default; branch-owned corrections require an explicit correction-command/round-trip workflow.
- Added Scenario 2: cloud-centralized multi-campus deployment with organization-wide Headmaster access and campus-scoped staff permissions.
- Clarified Scenario 3: hybrid local nodes with automatic cloud synchronization; independent operation requires a node at each independently disconnected campus.
- Added campus data-exchange package requirements: stable IDs, signed manifest, recipient encryption, schema versioning, dry-run, deduplication, provenance, audit and recovery.
- Added ADR-014, campus deployment scenario specification, task packages, tracker entries and open questions.


## v1.3 — 9 November 2026

- Confirmed AI privacy direction as hybrid: permitted/minimized external API use, with sensitive workloads private/local by policy.
- Confirmed campus-local/offline AI as an optional premium add-on; core school operations do not depend on AI.
- Confirmed hybrid AI pricing: limited AI allowance in standard subscription, advanced AI paid add-on, private/local AI premium.
- Added three AI hosting environments: cloud API, private AI infrastructure and optional Campus Local AI.
- Added centralized Provider Gateway responsibilities, privacy-aware routing and safe fallback rules.
- Added data classification, minimization, provider retention/training review and privacy-safe audit requirements.
- Added PostgreSQL + pgvector as the initial RAG candidate, with separate local and cloud trust boundaries.
- Added allow-listed typed tool calling through authorized application services; no unrestricted SQL or direct DB access.
- Added no-shared-training-by-default policy, opt-in fine-tuning gates and human-reviewed evaluation.
- Added AI usage metering, credits, budgets, quotas, alerts and exhaustion behavior.
- Added AI-0 through AI-7 task plan, AI progress tracker and ADR-015 through ADR-018.
- Explicitly marked all AI workstreams Not Started; no implementation completion is implied.


## v1.4 — 9 November 2026

- Confirmed both automatic online licence renewal and manual signed `.lic` file import/USB fallback for disconnected Local Nodes.
- Confirmed Restricted Mode after the configured grace period: preserve historical read access, reports/export, backup/restore administration, billing/licence renewal and support; block new operational writes.
- Clarified that licence renewal reactivates the existing installation and does not require a software update.
- Required automatic and manual renewal to share the same signature validation and atomic activation pipeline.
- Updated ADR-012, hybrid deployment work package H3, progress tracker, open questions and licensing risk register.
- Kept the seven-day grace duration as a proposal pending commercial approval; all implementation tasks remain Not Started.

## v1.5 — 9 November 2026

- Added organization-level hybrid commercial pricing analysis with one subscription per organization and organization-wide unique active student metering.
- Recorded preferences for school-provided local server hardware, first Local Node included, additional nodes potentially billed, all core modules across plans, monthly student snapshots, paid onboarding and monthly + annual billing.
- Recorded the starting price preference of Rs. 2,500/month, 200 included students and Rs. 10 per additional student/month.
- Added illustrative Starter/Growth/Professional/Enterprise price bands, node-fee and onboarding hypotheses, annual-discount hypothesis, revenue examples and profitability framework.
- Explicitly marked Rs. 1,000/month additional-node fee, Rs. 5,000 onboarding and 10% annual discount as unapproved test hypotheses.
- Added customer-owned hardware responsibilities, billing risks, pilot validation requirements and open commercial questions.
- Added ADR-019, commercial pricing strategy, commercial billing task plan and BILL-001 through BILL-010 tracker tasks.
- All billing implementation tasks remain Not Started; no final public tariff or profit forecast is approved.


## v1.6 — 9 November 2026

- Added SchoolOS Business & Customer Platform (BCP) as a Cloud Control Plane workstream.
- Defined five audience experiences: Public Website/CMS, Demo Platform, Customer Portal, Super Admin Console and Support Center.
- Recorded both instant self-service demo and personalized sales demo request as selected.
- Recorded both self-registration and manual company review/approval as selected.
- Recorded built-in ticketing/live chat/help center plus WhatsApp integration as selected support direction.
- Added functional requirements, page/route structures, preliminary database entity catalogue and API families.
- Added security requirements for separate auth boundaries, synthetic demos, least-privilege admin access, tenant isolation and time-limited customer-approved support access.
- Added BCP work packages BCP-0 through BCP-11 and tasks BCP-001 through BCP-035.
- Added ADR-020, open questions and risk register entries.
- Explicitly marked all BCP implementation work Not Started; no code, integrations or operational launch is claimed.
- Kept all previously proposed prices and support SLAs unapproved unless separately confirmed.


## v1.7 — 9 November 2026

- Confirmed the instant self-service demo as an ephemeral session-bound workspace, not a permanent demo account.
- Specified immediate access revocation on explicit logout and automatic end/cleanup after last-tab abandonment, inactivity or absolute lifetime.
- Added server-authoritative client leases, heartbeat, reconnect grace, refresh and multi-tab handling; browser close callbacks are best-effort only.
- Recommended a dedicated Demo Runtime store isolated from production and customer/billing data, with immutable synthetic templates and server-derived session scope/RLS.
- Defined idempotent retryable cleanup across database rows, objects/files, cache, queues, search/vector artifacts and temporary runtime resources.
- Added backup exclusion, log minimization, residual-copy retention and cleanup-failure safeguards.
- Added `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md`, ADR-021 and BCP-036 through BCP-041; updated demo specification, task plan, progress trackers, risks, open questions and ADR index.
- Marked all related implementation tasks Not Started. Heartbeat/lease/grace/idle/absolute-lifetime values are initial proposals, not approved customer-facing SLAs; final storage isolation remains pending threat/load/cost testing.
