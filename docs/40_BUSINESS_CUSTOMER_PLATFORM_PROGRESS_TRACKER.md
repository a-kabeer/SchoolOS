# 40 — Business & Customer Platform Progress Tracker

**Version:** v1.7 · **Last updated:** 9 November 2026 · **Overall status:** Planning / Not Started

## Status definitions

`Not Started` · `In Progress` · `Blocked` · `In Review` · `Completed`

No implementation evidence has been supplied for the BCP work packages. Therefore all tasks are initialized as **Not Started**.

## Work package status

| Work package | Description | Status | Owner | Evidence / notes |
|---|---|---|---|---|
| BCP-0 | Discovery & architecture | Not Started | Product + technical lead | Requirements baseline captured; design approval pending |
| BCP-1 | Identity, permissions & audit foundation | Not Started | Technical lead | Permission matrix and threat model pending |
| BCP-2 | Public website & CMS | Not Started | Frontend + content | |
| BCP-3 | Lead & CRM | Not Started | Product + backend | |
| BCP-4 | Demo platform | Not Started | Backend + QA | Ephemeral session-bound workspace direction accepted; storage mechanism and TTL values pending design/testing |
| BCP-5 | Registration & onboarding | Not Started | Backend + operations | |
| BCP-6 | Super Admin Console | Not Started | Frontend + backend | |
| BCP-7 | Subscription, billing & licensing integration | Not Started | Backend + finance | Uses existing BILL plan; no implementation claimed |
| BCP-8 | Customer Portal | Not Started | Frontend + backend | |
| BCP-9 | Support Center | Not Started | Backend + support | WhatsApp provider not selected |
| BCP-10 | Notifications & monitoring | Not Started | Backend + operations | Providers/SLAs open |
| BCP-11 | QA, security & production readiness | Not Started | QA + security + operations | |

## Task-level tracker

Use `docs/39_BUSINESS_CUSTOMER_PLATFORM_TASK_PLAN.md` as the task register. Update status only with evidence: PR/commit, migration, test result, reviewed UX or operational artifact. A discussion or design note alone does not mark an implementation task complete.

## Decision confirmations captured

- Instant self-service demo **and** personalized sales demo request: confirmed.
- Instant demo should be ephemeral and session-bound: logout revokes immediately; closing the last tab, inactivity or absolute lifetime ends the session and triggers cleanup. Refresh/multiple tabs must reconnect safely.
- Demo data must be synthetic, isolated from production and removed through retryable cleanup; browser close is best-effort and server-side leases/expiry sweeps are mandatory.
- Self-registration **and** manual company review/approval: confirmed.
- Built-in ticketing/live chat/help center **and** WhatsApp integration: confirmed.
- Public product/pricing website and internal Super Admin Console: required.

## Open dependencies

- Final hostnames/domain and hosting topology.
- BCP identity provider/session strategy and MFA enforcement.
- Customer/staff role-permission matrix and separation of duties.
- Approved production pricing and payment method/provider.
- Demo storage isolation, session timeouts/concurrency, client lease and cleanup completion targets.
- WhatsApp Business provider and template/consent policy.
- Support hours, SLAs, staffing and escalation rules.
- Data retention, deletion, export and privacy/legal requirements.
- Customer support remote-access policy and implementation mechanism.

## Work log

| Date | Contributor | Completed | In progress | Blockers | Decisions / links |
|---|---|---|---|---|---|
| 2026-11-09 | Product planning | v1.6 BCP requirements and task plan documented | Discovery | Detailed design and approval | ADR-020 |


## Demo session lifecycle work (v1.7)

The product direction is captured in `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md` and ADR-021. BCP-013, BCP-014 and BCP-036–BCP-041 remain **Not Started**. Proposed test values are heartbeat 30s, client lease 90s, last-client reconnect grace 60s, inactivity 15m, absolute lifetime 2h and cleanup sweep 1m; none is a final SLA.
