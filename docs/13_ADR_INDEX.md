# 13 — Architecture Decision Record (ADR) Index

Use the [canonical ADR template](templates/ADR_TEMPLATE.md) to document material design decisions. The root `templates/ADR_TEMPLATE.md` is a deprecated compatibility pointer. Statuses below describe the current planning baseline; cryptographic and operational implementations still require review.

| ADR | Title | Current decision | Status |
|---|---|---|---|
| ADR-001 | Modular Monolith | School business application remains a domain-oriented ASP.NET Core Modular Monolith | Historical summary only — no standalone ADR file in the current repository baseline |
| ADR-002 | Backend Framework | ASP.NET Core / .NET 10 LTS | Historical summary only — no standalone ADR file in the current repository baseline |
| ADR-003 | Frontend | React + TypeScript | Historical summary only — no standalone ADR file in the current repository baseline |
| ADR-004 | Database | PostgreSQL | Historical summary only — no standalone ADR file in the current repository baseline |
| ADR-005 | Multi-tenancy and database boundary | Tenant/site-scoped records; each Local Data Plane has its own operational DB and Cloud Control Plane has a separate platform DB | Historical summary only — no standalone ADR file in the current repository baseline |
| ADR-006 | Offline Model | Local Data Plane server is authoritative for local operations; PWA/IndexedDB is supplementary | Historical summary only — no standalone ADR file in the current repository baseline |
| ADR-007 | Finance Integrity | Double-entry ledger and immutable posted entries | Historical summary only — no standalone ADR file in the current repository baseline |
| ADR-008 | Event Delivery | Transactional Outbox/Inbox and idempotent consumers | Historical summary only — no standalone ADR file in the current repository baseline |
| ADR-009 | AI Provider | Provider abstraction; no vendor lock-in | Historical summary only — no standalone ADR file in the current repository baseline |
| ADR-010 | Deployment | Hybrid Cloud Control Plane + Local Data Plane nodes | Historical summary only — no standalone ADR file in the current repository baseline |
| ADR-011 | Hybrid Topology | Shared group node or per-campus edge nodes based on outage requirement | Accepted direction; per-pilot topology pending |
| ADR-012 | Signed Offline License Lease | Node-bound, time-bounded asymmetric signed licence; automatic online renewal + manual `.lic`/USB fallback; restricted mode after grace | Accepted product policy; format/security review pending |
| ADR-013 | Multi-node Synchronization | Event-based hub-and-spoke through Cloud Sync Hub; no generic financial last-write-wins | Accepted direction; detailed contracts pending |

## Decision-record files

- `decisions/ADR-011-HYBRID-CLOUD-LOCAL.md`
- `decisions/ADR-012-SIGNED-OFFLINE-LICENSE-LEASE.md`
- `decisions/ADR-013-SYNC-HUB-EVENT-BASED.md`

| ADR-014 | Campus Data Access and Disconnected Data Exchange | Accepted direction | Defines disconnected manual import/export, cloud-centralized multi-campus access and hybrid sync profiles | `decisions/ADR-014-CAMPUS-DATA-ACCESS-AND-EXCHANGE.md` |


## AI decision records (v1.3)

| ADR | Decision | Direction | Status |
|---|---|---|---|
| ADR-015 | AI Hybrid Hosting and Optional Offline Inference | Cloud + private + optional campus-local AI | Accepted direction; implementation details pending |
| ADR-016 | AI Provider Gateway and Privacy Routing | Central backend gateway; policy-safe fallback only | Accepted direction; providers pending |
| ADR-017 | AI RAG and Tool Calling | Permission-filtered RAG and allow-listed application tools | Accepted direction; schemas pending |
| ADR-018 | AI Training and Customer Data Governance | No shared model training by default; fine-tuning opt-in only | Accepted direction; legal controls pending |

Decision records: `decisions/ADR-015-AI-HYBRID-HOSTING-AND-OFFLINE.md`, `ADR-016-AI-PROVIDER-GATEWAY-AND-PRIVACY-ROUTING.md`, `ADR-017-AI-RAG-AND-TOOL-CALLING.md`, `ADR-018-AI-TRAINING-AND-CUSTOMER-DATA-GOVERNANCE.md`.


## Historical ADR coverage

ADR-001 through ADR-010 are index-only historical summaries in this repository baseline; standalone decision files are not present. Do not infer that an ADR file exists for these rows.

## Licence policy decision update (v1.4)

ADR-012 records the confirmed renewal and expiry behavior: both automatic online renewal and manual signed-file import are supported; after grace, the node enters Restricted Mode while preserving historical read/export, backup, billing/renewal and support access. A valid renewal reactivates the existing installation without a software update. The seven-day grace duration remains a proposal pending commercial approval.

## Commercial decision records (v1.5)

| ADR | Decision | Direction | Status |
|---|---|---|---|
| ADR-019 | Organization Subscription and Commercial Metering | One organization subscription; organization-wide unique active student metering; first node included; additional nodes potentially charged; paid onboarding; monthly + annual billing; all core modules across tiers | Proposed commercial direction for validation; not final pricing approval |

Decision record: `decisions/ADR-019-ORGANIZATION-SUBSCRIPTION-AND-METERING.md`.


## Business & Customer Platform decision (v1.6)

| ADR | Decision | Direction | Status |
|---|---|---|---|
| ADR-020 | Business & Customer Platform Ecosystem | Five audience experiences in Cloud Control Plane; separate auth boundaries; synthetic demo; gated registration; organization-scoped portal; least-privilege Super Admin and integrated support | Accepted planning direction; detailed design pending |

Decision record: `decisions/ADR-020-BUSINESS-CUSTOMER-PLATFORM.md`.


## Demo platform decision update (v1.7)

| ADR | Decision | Direction | Status |
|---|---|---|---|
| ADR-021 | Ephemeral Session-Based Demo | Temporary session-bound workspace; synthetic-only data; immediate logout revocation; server leases/expiry; retryable cleanup; isolated Demo Runtime | Accepted product direction; storage/timeouts and retention details pending |

Decision record: `decisions/ADR-021-EPHEMERAL-SESSION-BASED-DEMO.md`. Full design: `41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md`.

## Documentation and governance decisions

The canonical documentation governance standard is [document 54](54_DOCUMENTATION_STANDARDS.md). Remaining architecture/security decisions are tracked in [document 60](60_ARCHITECTURE_AND_SECURITY_DECISION_CLOSURE_REGISTER.md).
