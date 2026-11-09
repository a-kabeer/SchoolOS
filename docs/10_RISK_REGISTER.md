# 10 — Risk Register

| ID | Risk | Impact | Likelihood (initial estimate) | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|
| R-01 | Cross-tenant data leakage | Critical | Medium | Server-side scope enforcement, RLS where suitable, automated penetration/access tests | Technical lead | Open |
| R-02 | Duplicate or incorrect fee posting | Critical | Medium | Double-entry rules, idempotency, immutable postings, reconciliation and integration tests | Finance lead | Open |
| R-03 | Bank statement formats vary | High | High | Start with configurable import templates and mapping; collect samples from pilot banks | Product/finance | Open |
| R-04 | Family payment allocation complexity | High | Medium | Validate real cases; support household, split and partial allocations | Finance lead | Open |
| R-05 | Offline sync loses or duplicates operations | Critical | Medium | Unique operation IDs, idempotency, acknowledgements, retry/conflict tests | Technical lead | Open |
| R-06 | Browser PWA background sync is inconsistent | High | High | Foreground/reopen sync, explicit pending state, actual-device testing | Frontend lead | Open |
| R-07 | Curriculum scope is too broad | High | High | Prioritize board inventory, versioned curriculum model, validate content coverage | Academic product lead | Open |
| R-08 | AI paper marks are inaccurate | High | Medium | Rubrics, curated tests, teacher approval, subject/language-specific evaluation | AI/academic lead | Open |
| R-09 | AI risk/auditor outputs create false accusations | High | Medium | Evidence-based neutral language, human review, false-positive tracking | Product/AI lead | Open |
| R-10 | AI provider changes cost or policy | Medium | Medium | Provider abstraction, usage caps, contracts and fallback | Technical lead | Open |
| R-11 | Student/guardian data exposed to AI vendor unnecessarily | Critical | Medium | Data minimization, provider review, retention controls and legal review | Security/privacy owner | Open |
| R-12 | Scope expands before core workflows stabilize | High | High | Phase gates, MVP non-goals and customer validation | Product owner | Open |
| R-13 | Backup cannot be restored | Critical | Low/Medium | Off-host backups and scheduled restore drills | Operations | Open |
| R-14 | Poor adoption by school staff | High | Medium | Role-specific UX, onboarding, training and pilot feedback | Product owner | Open |
| R-15 | Payroll/tax/accounting requirements vary | High | Medium | Local accountant/legal review and configurable policies | Finance product owner | Open |

## Risk review cadence

- Review during weekly project planning.
- Reassess probability and impact after each pilot or major architecture change.
- Every critical/high risk requires an owner and mitigation evidence.
- Record incidents and decisions in the work log and ADRs.


## Hybrid deployment risks (v1.1 additions)

| Risk | Likelihood | Impact | Mitigation / owner |
|---|---|---|---|
| Local server hardware/power failure | Medium | Critical | UPS, independent backups, restore drills, spare/replacement process / Operations |
| Customer LAN compromise exposes local school data | Medium | Critical | Host hardening, firewall, disk encryption, least privilege, incident plan / Security |
| Subscription remains usable past a cloud cancellation while node is disconnected | Medium | High | Signed lease with bounded expiry, managed appliance option, contractual terms; accept no immediate revocation / Product + Engineering |
| Customer with root access tampers with clock or binary | Medium | High | Managed appliance, TPM/secure boot where supported, signed builds, tamper logs, legal terms / Engineering + Legal |
| Multi-node sync creates conflicting student/finance records | Medium | Critical | Aggregate ownership, Outbox/Inbox, version checks, idempotency and human conflict queue / Architecture |
| Group reports are stale while campus node is offline | High | Medium | Last-sync timestamp and stale-data labels / Product |
| Local backup exists only on same server and is lost with hardware | Medium | Critical | Separate local backup target plus encrypted off-site copy / Operations |
| Cloud AI unavailable during internet outage | High | Medium | Queue requests, declare AI unavailable, optional later Local AI Pack / Product |
| Parent portal exposes stale or unauthorized cloud projection | Medium | High | Data minimization, scoped read model, freshness labels and access tests / Security |
| Signed update or DB migration breaks disconnected node | Medium | High | Signed manifests, staged rollout, compatibility matrix and pre-update backup / Release Engineering |
| Local node has not synchronized security revocation | Medium | High | Define maximum offline window, local revocation workflow and customer policy / Security |


## AI-specific risks (v1.3)

| Risk | Impact | Mitigation / control | Owner | Status |
|---|---|---|---|---|
| Sensitive school data sent to an ineligible external provider | High | Classification, minimization, explicit routing policy, deny-by-default tests and audit | Product + Security | Open |
| Provider retention/training terms misunderstood or changed | High | Contract review, provider register, periodic revalidation and customer disclosure | Legal + Security | Open |
| Cross-tenant or cross-campus RAG leakage | Critical | Pre-retrieval ACL filters, tenant-scoped indexes/metadata, isolation tests and audit | Technical lead | Open |
| Prompt injection causes unauthorized tool/action | High | Untrusted-input handling, allow-listed typed tools, backend authorization and human approvals | Technical lead | Open |
| Hallucinated finance or academic conclusions | High | Deterministic reports, evidence/citations, confidence handling and human review | Product + QA | Open |
| AI usage cost exceeds subscription revenue | High | Quotas, credits, budgets, caps, alerts and per-feature margin monitoring | Product + Finance | Open |
| Local inference hardware cannot meet performance/support expectations | Medium/High | Optional premium scope, hardware benchmark and supported-device matrix | Technical lead | Open |
| Bias or false-positive student intervention | High | Human review, fairness evaluation, transparent signals and no automatic punitive action | Academic lead | Open |


## Licensing policy risks (v1.4)

| Risk | Impact | Mitigation / owner | Status |
|---|---|---|---|
| Automatic renewal fails silently while node is online | High | Renewal health status, retries/backoff, admin warnings and manual-file fallback / Engineering | Open |
| Manual licence file is copied, tampered with or replayed | High | Asymmetric signature, node binding, sequence checks, atomic import and audit / Security | Open |
| Restricted Mode blocks a critical recovery or safeguarding workflow | Critical | Operation-by-operation allow/deny matrix, emergency recovery policy and role-based tests / Product + QA | Open |
| Renewal activates but queued offline operations are posted incorrectly | High | Quarantine/review, idempotency and business validation before posting / Engineering + Finance | Open |

## Commercial pricing and billing risks (v1.5)

| Risk | Impact | Mitigation / owner | Status |
|---|---|---|---|
| Offline campuses produce stale organization student counts | High | Snapshot freshness indicator, reconciliation window and auditable adjustments / Product + Finance | Open |
| Student is counted twice after campus transfer | High | Organization-level stable identity and deduplication tests / Engineering | Open |
| Node fee is perceived as arbitrary or campus tax | Medium | Define billable node clearly and document ongoing node services / Commercial | Open |
| Customer-owned hardware is underpowered or unsupported | High | Publish hardware/OS matrix and perform readiness assessment / Operations | Open |
| Fixed onboarding fee does not cover migration/support effort | High | Scope standard setup, time-track delivery and quote migration separately / Operations | Open |
| Annual discount and overage policy reduce contribution | Medium | Pilot discount sensitivity and model monthly/quarterly/annual true-ups / Finance | Open |
| Customer disputes monthly count | High | Preview, fixed snapshot date, campus breakdown and credit-note workflow / Product + Finance | Open |
| Optional AI or messaging costs exceed subscription revenue | High | Separate usage credits, budgets and add-ons / AI + Finance | Open |
| Student/node count is changed without authorization | High | Server-side calculations, entitlements, immutable snapshots and audit / Engineering | Open |


## Business & Customer Platform risks (v1.6)

| Risk | Impact | Mitigation / owner | Status |
|---|---|---|---|
| Public/demo/portal/admin session boundary is weak | Critical | Separate audiences/cookies, MFA for staff, auth threat model and privilege-boundary tests / Security | Open |
| BCP becomes an unintended route into campus student/finance records | Critical | Strict Cloud/Local data boundary, minimized projections, customer-approved access grants and audit / Architecture | Open |
| Demo session exposes another demo or production data | Critical | Synthetic-only datasets, strong session isolation, expiry/reset and penetration tests / Engineering | Open |
| Self-registration grants production access prematurely | High | Separate verification, organization review, payment, subscription, node and licence gates / Product + Engineering | Open |
| Admin employee has excessive privileges | Critical | Least privilege, separation of duties, MFA, step-up approval and admin audit / Security | Open |
| WhatsApp webhook spoofing/replay/duplicate events | High | Signature verification, timestamp validation, idempotency and sender identity checks / Engineering | Open |
| Sensitive data shared through WhatsApp/chat attachments | High | Channel policy, warnings, attachment scanning, minimization and staff training / Support + Security | Open |
| Support access to Local Node persists beyond customer approval | Critical | Explicit grants, default read-only, scope restriction, automatic expiry and audit / Security | Open |
| Public site publishes draft/unapproved tariff | Medium/High | Published-price snapshot and CMS permissions/review workflow / Commercial + CMS | Open |
| Support SLA promises exceed available staffing | High | Approve hours/targets only after capacity modelling / Operations | Open |
| Lead/demo/analytics collection exceeds privacy needs | Medium/High | Data minimization, consent and retention schedule / Product + Legal | Open |
| Admin export or reports leak cross-organization metadata | High | Permission checks, filtered exports, audit and tenant-isolation testing / Engineering | Open |


## Ephemeral demo-specific risks (v1.7)

| Risk | Impact | Mitigation / owner | Status |
|---|---|---|---|
| Browser close signal is missed or falsely interpreted during refresh/multi-tab use | High | Server-side client leases, reconnect grace, inactivity and absolute expiry; never rely on unload callback alone / Engineering | Open |
| Demo cleanup partially fails and temporary data remains | High | Idempotent cleanup worker, retry policy, dead-letter alert, session revocation before cleanup and resource inventory / Engineering + Operations | Open |
| Deleted demo records remain in physical backups/snapshots or logs | High | Separate disposable Demo Runtime store, exclude from long-lived backup sets, minimize logs and document residual-copy expiry / Operations + Security | Open |
| Session-scoped authorization is bypassed across demo workspaces | Critical | Server-derived session scope, PostgreSQL RLS, separate demo store, penetration and concurrency tests / Security | Open |
| Demo runtime or background job triggers a real integration | High | Deny-by-default integration policy, no production secrets, disabled payment/message/licence/sync adapters and negative tests / Engineering | Open |
