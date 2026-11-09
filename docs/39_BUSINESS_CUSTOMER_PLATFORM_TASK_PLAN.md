# 39 — Business & Customer Platform Implementation Task Plan

**Version:** v1.7 · **Status:** Planning only; every task Not Started

## Delivery principles

- Build within the existing ASP.NET Core Modular Monolith and React + TypeScript architecture; do not introduce microservices by default.
- Keep Cloud Control Plane BCP data separate from campus Local Data Plane operational data.
- Design auth, tenant isolation, audit and data retention before customer-facing launch.
- Prefer a usable end-to-end vertical slice over creating all screens without working backend workflows.
- All APIs, entities and screen lists in this archive are proposed until detailed design and review.

## Work packages and dependencies

| WP | Work package | Scope | Depends on | Priority |
|---|---|---|---|---|
| BCP-0 | Discovery & architecture | Final user journeys, data classification, hostnames, auth audiences, module boundaries, operational model | None | Critical |
| BCP-1 | Identity, permissions & audit foundation | Customer/staff identity, MFA, RBAC/permissions, organization scoping, audit, sessions | BCP-0 | Critical |
| BCP-2 | Public website & CMS | Public pages, SEO, media, draft-review-publish CMS, approved pricing feed | BCP-0, BCP-1 | High |
| BCP-3 | Lead & CRM | Lead capture, pipeline, assignment, activity, follow-up, conversion | BCP-1, BCP-2 | High |
| BCP-4 | Demo platform | Synthetic templates, ephemeral session-bound workspaces, multi-tab/refresh handling, server-side expiry, cleanup and personalized demo request | BCP-1, BCP-3 | High |
| BCP-5 | Registration & onboarding | Verification, application, review, statuses, onboarding checklist, trial gate | BCP-1, BCP-3 | Critical |
| BCP-6 | Super Admin Console | Org directory/360, staff operations, approvals, node/subscription/payment/licence/support management | BCP-1, BCP-5 | Critical |
| BCP-7 | Subscription, billing & licensing integration | Integrate existing commercial and signed licence plan into admin + portal workflows | BCP-6; existing BILL tasks | Critical |
| BCP-8 | Customer Portal | Org/campus/node metadata, billing, licence downloads, team, security, support | BCP-1, BCP-6, BCP-7 | High |
| BCP-9 | Support Center | Help center, ticketing, chat, WhatsApp, customer-approved support access | BCP-1, BCP-6, BCP-8 | High |
| BCP-10 | Notifications & monitoring | Email/in-app/SMS/WhatsApp provider abstraction, node health summaries, alerts, reports | BCP-1, BCP-6 | High |
| BCP-11 | QA, security & production readiness | E2E, tenant isolation, security review, backup/restore, support readiness, legal/policy review | BCP-2 to BCP-10 | Critical |

## Detailed task register

| Task ID | Task | Priority | Status | Acceptance evidence |
|---|---|---|---|---|
| BCP-001 | Confirm BCP scope, actors, customer journeys and explicit non-goals | Critical | Not Started | Approved requirements and process maps |
| BCP-002 | Define Cloud Control Plane database boundary and module ownership | Critical | Not Started | Reviewed data-flow and context map |
| BCP-003 | Define public/demo/portal/admin auth audiences and session boundaries | Critical | Not Started | Auth threat model and tested design |
| BCP-004 | Approve staff/customer role-permission matrix and separation of duties | Critical | Not Started | Permission matrix signed off |
| BCP-005 | Produce BCP ERD, data dictionary, classification and retention schedule | Critical | Not Started | Reviewed ERD and data dictionary |
| BCP-006 | Implement platform identity, MFA, session management and account recovery | Critical | Not Started | Automated identity/security tests |
| BCP-007 | Implement resource-level organization/campus authorization and audit | Critical | Not Started | Tenant isolation and audit test suite |
| BCP-008 | Design website information architecture, responsive UI and accessibility | High | Not Started | Approved sitemap and UX acceptance |
| BCP-009 | Implement CMS pages, revisions, review, preview and publishing | High | Not Started | CMS workflow tests |
| BCP-010 | Implement public product/features/pricing/resource pages | High | Not Started | Public pages and SEO checks |
| BCP-011 | Build lead capture, duplicate detection and CRM pipeline | High | Not Started | Lead-to-conversion workflow tests |
| BCP-012 | Implement CRM assignment, activities, tasks/reminders and reports | Medium | Not Started | Sales workflow acceptance |
| BCP-013 | Build synthetic demo data templates and environment isolation | Critical | Not Started | Isolation and data provenance tests |
| BCP-014 | Implement demo session creation, session-bound expiry, reset and idempotent cleanup | High | Not Started | Logout/close/crash/idle/max-lifetime/refresh/multi-tab and cleanup E2E tests |
| BCP-015 | Disable real payments, notifications and licence issuance in demo | Critical | Not Started | Negative integration tests |
| BCP-016 | Implement personalized demo requests and sales appointment workflow | High | Not Started | Lead + appointment E2E test |
| BCP-017 | Implement self-registration and email/phone verification | Critical | Not Started | Registration verification tests |
| BCP-018 | Implement organization review, manual approval/rejection and reasons | Critical | Not Started | Staff/customer state-transition tests |
| BCP-019 | Implement onboarding checklist and trial/production separation | Critical | Not Started | No production licence before gates test |
| BCP-020 | Build admin organization directory and Organization 360 | Critical | Not Started | Search/filter/detail authorization tests |
| BCP-021 | Build admin campus/node registry and operational summaries | High | Not Started | Node lifecycle tests |
| BCP-022 | Build admin roles, staff management and audit views | Critical | Not Started | Privilege-escalation tests |
| BCP-023 | Integrate subscription, billing and payment verification workflows | Critical | Not Started | Billing reconciliation and audit tests |
| BCP-024 | Integrate signed licence issue/renew/revoke/manual download flows | Critical | Not Started | Signature, node binding and replay tests |
| BCP-025 | Build customer portal organization, campus, node and subscription screens | High | Not Started | Role-scoped portal E2E tests |
| BCP-026 | Build customer portal invoice, payment, licence and downloads screens | High | Not Started | Cross-org access and file authorization tests |
| BCP-027 | Build portal team, notifications and security settings | Medium | Not Started | Invitation, revoke and MFA tests |
| BCP-028 | Implement support ticket lifecycle, assignments, attachments and history | High | Not Started | Ticket E2E + attachment scan tests |
| BCP-029 | Implement live chat with durable message storage | Medium | Not Started | Reconnect and message persistence tests |
| BCP-030 | Implement WhatsApp webhook integration with signature/idempotency | High | Not Started | Replay, spoofing and duplicate-delivery tests |
| BCP-031 | Implement customer-approved time-limited support access | Critical | Not Started | Approval, expiry, scope and audit tests |
| BCP-032 | Implement knowledge base publishing and search | Medium | Not Started | Article workflow and access tests |
| BCP-033 | Implement notifications, preferences, consent and delivery logs | High | Not Started | Retry, opt-out and delivery tests |
| BCP-034 | Implement business and node operations dashboards/read models | Medium | Not Started | Reconciliation of dashboard aggregates |
| BCP-035 | Complete security review, penetration testing and launch checklist | Critical | Not Started | No unresolved critical/high findings |
| BCP-036 | Design demo-specific auth audience, session/client lease state machine, refresh and multi-tab behavior | Critical | Not Started | Reviewed lifecycle/state model and threat model |
| BCP-037 | Provision a fresh ephemeral workspace from immutable synthetic template in a dedicated Demo Runtime store | Critical | Not Started | Workspace isolation, clean-seed and no-production-data tests |
| BCP-038 | Implement server-authoritative heartbeat, disconnect hints, inactivity/absolute expiry and explicit logout revocation | Critical | Not Started | Stale-token, close/crash, refresh, multi-tab and expiry tests |
| BCP-039 | Implement idempotent high-priority cleanup worker for DB, objects, cache, queues, indexes and runtime resources | Critical | Not Started | Retry, partial-failure, idempotency and cleanup-health evidence |
| BCP-040 | Define demo backup exclusion, log minimization, residual-copy retention and deletion verification | Critical | Not Started | Backup/restore review and demonstrable deletion-window evidence |
| BCP-041 | Complete demo isolation/lifecycle penetration, concurrency and failure-injection test suite | Critical | Not Started | No cross-session access; all lifecycle and cleanup tests pass |

## Recommended sequencing

1. Complete BCP-0 and BCP-1 before releasing any customer portal or admin function.
2. Start public site/CMS and CRM in parallel after the data/auth contracts are stable.
3. Build demo after synthetic-data and session isolation design is accepted.
4. Build registration/onboarding before admin approval screens; complete admin workflows alongside it.
5. Integrate billing/licensing using existing BILL-001…BILL-010 and ADR-019 as source design; do not duplicate commercial rules.
6. Build portal and support on top of stable organization, subscription, licence and authorization services.
7. Complete security, data-retention, restore and operational readiness before public production launch.

## Definition of Done

Code merged; migrations reviewed; automated unit/integration/E2E tests passed; tenant/campus authorization tests passed; audit events verified; accessibility/UI acceptance completed; error/loading/empty states covered; operational logs/metrics/alerts added; docs and runbooks updated; security review passed; no unresolved critical defect.


## Ephemeral session-based demo requirements (v1.7)

See `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md` and ADR-021. The session-bound temporary workspace direction is accepted, but all BCP-013/014 and BCP-036–BCP-041 implementation tasks remain **Not Started**. Browser close events are best-effort only; backend leases, expiry sweeps, immediate logout revocation and retryable cleanup are mandatory. Proposed timeout values and final storage-isolation mechanism are not final commitments.
