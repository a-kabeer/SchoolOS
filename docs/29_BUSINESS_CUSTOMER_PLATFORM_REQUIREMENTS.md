# 29 — SchoolOS Business & Customer Platform (BCP): Product Requirements

**Version added:** v1.6 · **Date:** 9 November 2026 · **Status:** Requirements baseline; implementation Not Started

## 1. Purpose and boundary

The Business & Customer Platform (BCP) is the commercial, customer-facing and company-operations ecosystem around SchoolOS. It supports discovery, lead capture, product demonstrations, customer registration, onboarding, subscriptions, licence administration and support.

BCP is part of the **Cloud Control Plane**. It does not replace the SchoolOS campus Local Data Plane and must not become an unrestricted back door into a school's student, guardian, attendance, examination or fee records. The campus application remains the operational system of record for school data. The Control Plane keeps only the minimum organization, campus, node, subscription, licence, approved reporting and support metadata needed to operate the service.

## 2. Confirmed choices recorded in this version

| Area | Selected direction | Status |
|---|---|---|
| Platform name | SchoolOS Business & Customer Platform (BCP), working initiative name | Working name |
| Architecture | ASP.NET Core Modular Monolith backend; React + TypeScript frontends; PostgreSQL + EF Core/Npgsql | Existing architecture direction |
| Deployment | BCP hosted in Cloud Control Plane; Local Data Planes remain separate | Accepted direction |
| Demo | Both instant self-service demo and personalized sales demo request | Confirmed |
| Registration | Both self-registration and manual company review/approval | Confirmed |
| Support | Both built-in ticketing/live chat/help center and WhatsApp integration | Confirmed |
| Company operations | Internal Super Admin Console to manage organizations and platform issues online | Confirmed requirement |
| Public website | Complete product information, features, pricing, Try Demo and contact/registration paths | Confirmed requirement |

## 3. Audiences and frontends

BCP has five audience-specific experiences. They may share a backend and design system, but must not share the same privileged session boundary.

1. **Public Marketing Website** — visitors researching product, features, offline architecture, security, pricing and company details.
2. **Demo Platform** — prospects exploring synthetic sample data or requesting a guided session.
3. **Customer Portal** — organization owners and authorized customer contacts managing subscription, billing, licences, nodes, support and team access.
4. **Super Admin Console** — SchoolOS internal staff managing customers, approvals, subscriptions, payments, licences, leads, CMS, support and audit.
5. **Support Center** — customers and support staff using knowledge articles, tickets, live chat and WhatsApp-linked conversations.

Suggested hostnames are examples only; availability and final domain ownership have not been checked: `www.schoolos.com`, `demo.schoolos.com`, `portal.schoolos.com`, `admin.schoolos.com`, `support.schoolos.com`, `api.schoolos.com`.

## 4. Business goals and success measures

- Turn product interest into qualified leads, demos and approved organizations.
- Enable safe self-service discovery without exposing production data.
- Reduce manual work for customer onboarding, subscription, renewal and support.
- Give company staff an organization-wide operational view without unrestricted access to school records.
- Provide transparent customer visibility into node health, licence status, invoices and support cases.
- Maintain auditable, least-privilege administration.

Measures to define during discovery: website-to-lead conversion, demo start/completion, demo-to-proposal and proposal-to-paid conversion, onboarding time, invoice collection time, node heartbeat/sync success, ticket first-response/resolution time, licence renewal success and customer retention.

## 5. Functional requirements

### BCP-FR-001 — Public website and CMS
- Present complete product positioning, modules, target customer types, deployment choices, practical offline model, security approach, AI privacy, pricing structure, FAQs, resources and contact details.
- Offer visible Try Demo, Request a Guided Demo, Register and Contact Sales paths.
- Provide SEO metadata, canonical URLs, sitemap, robots controls, accessible semantic markup, responsive layouts and analytics consent controls.
- Staff with CMS permission can draft, preview, schedule and publish approved pages/articles/FAQs without deploying code.
- Public pricing must distinguish approved prices from indicative or custom pricing. Never publish an unapproved commercial hypothesis as a final tariff.

### BCP-FR-002 — Lead and CRM
- Capture lead source, person/contact details, organization name, city/province, estimated students/campuses, current software, deployment preference, requested modules and consent.
- Maintain pipeline: New → Contacted → Demo Scheduled → Demo Completed → Proposal → Negotiation → Converted / Lost.
- Support ownership, assignments, notes, activity history, tasks/reminders, duplicate detection, search/filter and reporting.
- Record conversion into an organization without losing the lead's history.

### BCP-FR-003 — Demo platform
- Support instant self-service demo and personalized sales demo request.
- Use synthetic sample school data only. Demo credentials/data must never be copied from a real customer.
- Provide demo template selection, session expiry, reset, rate limits, abuse controls and cleanup.
- Demo must not issue production licences, create payable invoices, connect to a real Local Node or mutate production organization data.
- Personalized demo request captures school/contact needs and a preferred appointment window; it creates/updates a CRM lead and appointment request.
- Initial implementation may use a shared resettable demo environment/template. Move to isolated temporary environments if concurrency, privacy or load testing justifies it.

### BCP-FR-004 — Registration and onboarding
- Support public self-registration with email/phone verification and manual staff-created/invited onboarding.
- Capture organization profile, legal/business contact details, primary owner, estimated students/campuses, deployment profile and requested services.
- Statuses: Draft, Email Pending, Submitted, Under Review, Trial, Approved, Active, Rejected, Suspended.
- Submission must not automatically grant a production licence. Approval, payment verification, subscription activation and node activation are separate state transitions.
- A trial/demo workspace may be provisioned under explicit limits without granting production data or production licence access.
- Staff actions, rejection reasons and customer-visible status changes must be recorded.

### BCP-FR-005 — Customer Portal
- Display organization profile, campuses, registered Local Nodes, subscription, student capacity snapshot, invoices, payment history, licence details, downloads, announcements, support tickets, team members and security settings according to permissions.
- Support manual signed `.lic` download/import workflow and show automatic renewal status where applicable.
- Display last heartbeat, last successful sync, last backup report (if supplied), installed version and stale-data indicators. A missing heartbeat must not be presented as proof that the node is broken; show its timestamp and status logic.
- Customer roles must be isolated from local school-app roles. A billing contact does not become a student/finance application administrator.

### BCP-FR-006 — Super Admin Console
- Provide organization directory with search, filters, pagination and organization 360 view.
- Filters should include organization name/customer ID, city/province, plan, student capacity, campus/node count, payment status, licence status and last heartbeat/sync.
- Manage leads, demos, registration review, organizations, campuses, nodes, plans, subscriptions, invoices, payments, licences, support, CMS, knowledge base, AI usage, notifications, staff, roles, configuration and audit.
- Verify payments against independently available bank/payment records; a screenshot alone is not sufficient confirmation.
- Admin actions must be permission-gated, reason-coded where appropriate and audited.
- Admin staff do not receive automatic unrestricted access to operational school data. Any exceptional support access requires customer approval, defined scope, expiry and audit.

### BCP-FR-007 — Support Center
- Provide knowledge base, ticket creation, ticket thread, attachment support, ticket status and live chat.
- Integrate WhatsApp through an approved provider/webhook, map inbound message to a verified customer/contact where possible, and preserve channel/message delivery state.
- Ticket categories: Account, Billing, Licence, Installation, Synchronization, Data, Finance, Academic, AI, Feature Request and Other.
- Priorities: Critical, High, Normal, Low. Status flow: Created → Assigned → Investigating → Resolution Proposed → Customer Confirmation → Closed; reopening and escalation rules must be defined.
- Show customer-visible SLA expectations only after staffing and support hours are approved.
- Do not send passwords, licence private keys, student lists or sensitive finance records through WhatsApp.

### BCP-FR-008 — Notifications and announcements
- Support in-app and email by default; SMS/WhatsApp only through configured providers and applicable consent/preferences.
- Events include registration status, approval, invoice, payment receipt/verification, licence issued/expiring, node offline, sync failure, ticket reply, release/update and security alert.
- Use templates, delivery attempts, provider response IDs, retries and opt-out/consent handling.

### BCP-FR-009 — Reporting and monitoring
- Business reports: registered organizations, active subscriptions, new leads, demo conversion, MRR/ARR, outstanding invoices, plan distribution, churn and onboarding revenue.
- Operations reports: campuses/nodes, heartbeat freshness, sync summaries, licence expiry, software versions, backup status summaries and support workload.
- AI usage reports: tenant-level credits/units/costs/budgets/alerts; avoid displaying raw prompts or sensitive source data by default.

## 6. Non-functional and operational requirements

- Tenant and organization scope is derived from authenticated identity/membership server-side, not trusted from client-supplied organization IDs.
- Separate authentication audiences/sessions for public/demo, customer portal and internal admin.
- MFA required for privileged staff; customer MFA policy configurable, with stronger controls for owners/finance/IT contacts.
- Admin, payment, licence, role, support-access and organization-status actions are audited.
- Public signup/demo endpoints are rate-limited and abuse-protected.
- Attachments are size-limited, type-validated and malware-scanned before staff access.
- Webhooks require signature verification, replay protection and idempotency.
- All privileged operations have server-side authorization and resource-level checks.
- Availability, backup RPO/RTO, retention, support hours and SLA targets remain open until operations and commercial capacity are known.

## 7. Out of scope for the first BCP release

- General-purpose remote desktop or unrestricted access to customer servers.
- A public API that exposes full student/fee records to SchoolOS company staff.
- Production access through demo accounts.
- Automated payment acceptance based only on uploaded screenshot evidence.
- Automatic AI training on customer school data.
- Full customer-side ERP/CRM replacement.

## 8. Acceptance criteria for BCP readiness

- Each frontend has a defined audience, route boundary, session model and permission map.
- Signup, approval, payment verification, subscription and licence activation are distinct tested transitions.
- Demo data is synthetic, resettable, isolated from production and expires automatically.
- Organization/campus authorization tests prove no cross-tenant access.
- Admin access and customer-approved support access are logged and time-limited.
- Ticket, chat and WhatsApp webhook flows are idempotent and auditable.
- CMS content can be previewed and published with role separation.
- Public price content cannot accidentally expose draft/test hypotheses.
- Restore, incident response, data export and customer termination processes are documented before launch.
