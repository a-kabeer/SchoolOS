# 00 — Decisions and Assumptions

**Status legend**
- **Confirmed:** Explicitly selected in discussion.
- **Recommended:** Proposed architecture or product decision, pending technical/business review.
- **Open:** Must be resolved before implementation of the affected area.

## 1. Confirmed product decisions

| Topic | Decision | Status |
|---|---|---|
| Target customer | Pakistani private schools and private school chains | Confirmed |
| Organization scale | Single-campus schools through multi-campus groups | Confirmed direction |
| Curriculum | Pakistani boards and Cambridge | Confirmed ambition |
| Offline | Broad practical offline operation | Confirmed ambition; exact actions governed by policy |
| AI | All six capabilities listed below | Confirmed scope direction |
| Backend | ASP.NET Core / C# | Confirmed |
| Frontend | React | Confirmed |
| Developer partner | Friend contributes as a team developer; ASP.NET Core and React expertise | Confirmed |
| Architecture | Modular Monolith | Confirmed |
| Database | PostgreSQL | Recommended and accepted as current direction |
| Deployment | Hybrid: SchoolOS Cloud Control Plane + one or more customer Local Data Plane nodes | Confirmed direction; exact topology per customer |
| Local operation | School staff use a Local Node over LAN; internet outage must not stop approved local workflows | Confirmed direction |
| Subscription enforcement | Cloud-issued, node-bound signed offline license lease; both automatic online renewal and manual signed-file/USB fallback | Confirmed policy; grace duration pending commercial approval |
| Expiry behavior | Restricted mode after grace; preserve historical data, read/export, backup and renewal; block new operational writes | Confirmed policy |
| Synchronization | Event-based hub-and-spoke using transactional Outbox/Inbox and idempotent handlers | Recommended design direction |

## 2. Selected AI capabilities

1. AI School Auditor.
2. AI Learning Recovery Engine.
3. AI Examination Intelligence (paper generation, quality review and assisted marking).
4. AI Business Intelligence Copilot.
5. Urdu / Roman Urdu Voice Assistant.
6. Student Early Intervention System.

All six belong in the product vision. They do not all need to ship in the first release. Human review is required for sensitive decisions, including financial changes, marks and student interventions.

## 3. Confirmed AI hosting, privacy and commercial direction (v1.3)

| Topic | Selected direction | Status |
|---|---|---|
| AI privacy | Hybrid: permitted/minimized external API use; sensitive workloads private/local by policy | Confirmed direction |
| AI hosting | Cloud AI, Private AI and optional Campus Local AI | Confirmed direction; detailed service design pending |
| Offline AI | Optional premium local inference; core workflows must not depend on AI | Confirmed direction |
| AI pricing | Limited AI included in standard subscription; advanced AI paid add-on; private/local AI premium | Confirmed direction; prices/quotas open |
| Training on school data | No automatic shared model training; fine-tuning only with explicit opt-in and governance | Confirmed direction |
| RAG and tools | Permission-filtered RAG and allow-listed backend tools; no unrestricted SQL or DB access | Accepted architecture direction |
| Vector storage | PostgreSQL + pgvector as initial candidate | Recommended; validate in target environments |
| Provider | Central backend Provider Gateway; vendor/model not selected | Accepted architecture direction |

**Important:** Provider names, models, data retention schedules, legal terms, included quotas and numeric prices are not finalized. `20`–`26` contain the detailed AI design and task plan. Provider “not used for training” terms must not be confused with zero retention.

## 4. Recommended technology baseline

| Layer | Proposed technology |
|---|---|
| Backend runtime/framework | ASP.NET Core on .NET 10 LTS |
| Language | C# |
| Frontend | React + TypeScript |
| Frontend build | Vite (recommended) |
| Database | PostgreSQL |
| ORM | Entity Framework Core 10 |
| PostgreSQL provider | Compatible Npgsql EF Core 10 release; verify latest patch at implementation time |
| Authentication | ASP.NET Core Identity or a carefully selected identity solution |
| Authorization | Policy-based permissions plus tenant/school/campus scope |
| Offline client data | IndexedDB, e.g. through Dexie |
| PWA | Service Worker and Web App Manifest; Workbox or equivalent maintained tooling |
| Background processing | .NET BackgroundService initially; add a durable job framework if requirements justify it |
| Cache / distributed coordination | Redis only when justified |
| Deployment | Hybrid Cloud Control Plane plus Dockerized Linux Local Data Plane nodes |
| AI | Provider abstraction/gateway; no permanent lock-in to one model vendor |

**Version policy:** Use supported stable releases and lock exact package versions in the repository. Re-check .NET, EF Core, Npgsql, React and browser support before implementation and upgrades. Do not copy old patch versions from this planning document.

## 5. Core architectural decisions

- The school business application remains a Modular Monolith. Each Local Data Plane runs the same ASP.NET Core business application package.
- A separate Cloud Control Plane host handles platform-level billing, subscription, license issuance, node registry, releases and sync coordination. This is a separate deployment/trust boundary, not a microservice per school business module.
- Business domains are separated into modules with explicit boundaries.
- Each Local Data Plane has its own operational PostgreSQL database with module-owned schemas. The Cloud Control Plane has a separate, smaller platform database. The cloud does not automatically hold a full live copy of all school records.
- A module owns its tables and business rules; other modules use public contracts/application interfaces or events rather than writing directly to its tables.
- Use synchronous calls for immediate queries/decisions and asynchronous events for follow-up work.
- Use a Transactional Outbox for reliable event publication from committed database transactions.
- Avoid circular module dependencies.
- Keep external providers (AI, SMS, WhatsApp, payments, email, storage) behind replaceable adapters.
- Do not introduce microservices per business module, Kubernetes or Redis as mandatory complexity before a demonstrated need.
- Support one shared group Local Node or one Local Edge Node per campus. A campus that must work independently when disconnected from head office needs its own local node.
- Use versioned business events and explicit data ownership for node synchronization; do not use generic last-write-wins or direct bidirectional database replication for financial data.
- Local nodes initiate outbound authenticated synchronization; do not expose local databases or permanent admin ports to the public internet.

## 6. Multi-tenant model (recommended)

Proposed hierarchy:

`SaaS Platform → Tenant / Customer Organization → School / Legal Entity → Campus`

A tenant may operate one or more school entities and each school may have one or more campuses. The final commercial/legal model may simplify this hierarchy after customer interviews.

Tenant and scope identifiers must be enforced server-side. A client-supplied `tenant_id` is never trusted as proof of access.

## 7. Business priorities

1. Tenant and campus isolation.
2. Fee, ledger and reconciliation integrity.
3. Safe offline synchronization.
4. Usable admissions, student, attendance and academic workflows.
5. AI capabilities built on validated data and review workflows.

## 8. Product assumptions requiring validation

- Initial customer profile may be independent schools and chains with roughly 200–1,500 students per school; this is a hypothesis, not a researched market-size fact.
- Schools will provide anonymised sample fee records and bank statements for workflow testing.
- CSV/XLSX bank statement import is the first reconciliation integration; direct bank or payment-provider APIs come later.
- English and Urdu interfaces are initial priorities; Roman Urdu is a priority for conversational AI, not necessarily the entire interface in release one.
- The local server is the authoritative operational backend during internet outages. PWA caching is supplementary; it is not the offline database/server. Browser sync limitations still matter for client-side drafts and cache refresh.

## 9. Hybrid deployment decisions

- **Cloud Control Plane:** customer registry, subscription/billing, license signing, node registry, entitlements, signed releases, sync hub and optional cloud reporting/backup catalogues.
- **Local Data Plane:** local React UI, ASP.NET Core Modular Monolith, local PostgreSQL, local identity, files, sync agent, license verifier and backups.
- **Offline licensing:** cloud-signed, time-bounded, node-bound license lease verified locally with a public key. A disconnected node cannot be remotely revoked immediately.
- **Renewal policy (confirmed):** support both automatic renewal when the Local Node has internet and manual signed `.lic` file download/import (including approved USB transfer) when it does not. Automatic renewal must not be required for local operations.
- **Expiry policy (confirmed):** after the configured grace boundary, enter Restricted Mode. Preserve existing data; allow historical viewing, reports/export, backup/restore administration, billing/licence renewal and support; block new operational writes such as admissions, attendance, fee collection, marks and new financial postings. Renewing the signed licence reactivates the existing installation without requiring a software update.
- **Grace policy:** seven calendar days remains the initial proposal, not a commercially approved promise. While connected, a local node should check for license/subscription updates at startup and on a scheduled heartbeat (suggested 12 hours); local operations must not make a live cloud check on every request.
- **Expiry:** after the grace boundary, enter restricted mode; preserve data and allow read/export, backup and renewal.
- **Topology:** both shared group node and per-campus edge nodes are supported. Per-campus nodes are required for independent branch autonomy during WAN outages.
- **AI:** cloud AI features are unavailable/queued during internet outage unless a separately tested Local AI Pack is installed.
- **Remote parent access:** requires an explicit cloud portal/read-model gateway or a customer VPN; do not expose local server ports directly.

## 10. Non-goals for the first release

- Microservices architecture.
- Building a custom large language model from scratch.
- Automatically approving or posting AI-generated financial actions.
- Automatically publishing AI-assigned examination marks.
- Full bank API integration with every Pakistani bank before validating statement import.
- Guaranteeing that every feature works offline.
- Claiming full board/curriculum content coverage before syllabus data is validated and loaded.

## 10. Commercial pricing direction under validation (v1.5)

The owner selected an organization-level hybrid pricing model for analysis: one organization subscription, unique active students counted across campuses, first Local Node included, additional nodes potentially charged, school-owned server hardware, all standard core modules in all packages, monthly student snapshots, paid one-time onboarding, and monthly plus annual billing. The starting numeric preference is Rs. 2,500/month including 200 students, with Rs. 10 per additional student/month.

**These are preferences and an analysis baseline, not final commercial approval.** Rs. 1,000/month for each additional node, Rs. 5,000 standard onboarding and a 10% annual discount are unapproved test hypotheses only. Package price examples are formula-derived illustrations. See `27_COMMERCIAL_PRICING_STRATEGY.md`, `28_COMMERCIAL_BILLING_IMPLEMENTATION_TASK_PLAN.md` and ADR-019.

Important distinction: organization-wide student billing is separate from campus access scope. A campus is not automatically a billable Local Node. Meter separately deployed, registered Local Data Plane installations; an organization that requires independent disconnected operation at each campus generally needs a node at each such campus.


## 11. Business & Customer Platform decisions (v1.6)

| Topic | Decision | Status |
|---|---|---|
| BCP scope | Public Website/CMS, Demo Platform, Customer Portal, Super Admin Console and Support Center in the Cloud Control Plane | Confirmed scope direction |
| Demo | Both instant self-service demo and personalized sales demo request | Confirmed |
| Registration | Both self-registration and manual company review/approval | Confirmed |
| Support | Built-in ticketing/live chat/help center plus WhatsApp integration | Confirmed |
| Public website | Complete product information/pricing with Try Demo, registration and contact paths | Confirmed requirement |
| Admin | Internal Super Admin Console for organization and platform operations | Confirmed requirement |
| Data boundary | BCP is not an unrestricted gateway to campus Local Data Plane student/finance/exam data | Accepted security direction |
| Demo data | Synthetic sample data only; session isolation, expiry and reset required | Accepted security direction |
| Registration gates | Verification, organization review, payment verification, subscription activation, node registration and licence issuance are distinct | Accepted product/security rule |
| Auth boundary | Separate audience/session boundary for customer portal and privileged admin | Accepted security direction |

The working hostname examples, identity provider, exact demo isolation implementation, support provider, SLA, retention rules and final public tariff remain open. See docs 29–40 and ADR-020. All BCP implementation tasks remain Not Started.
