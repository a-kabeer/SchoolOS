# 11 — Open Questions and Decisions Needed

These questions do not block saving the current product direction. They should be resolved before the related implementation phase.

## A. Product and customer discovery

- [ ] Which school size/band is the first paying customer profile?
- [ ] Is pricing per student, campus, school entity or a hybrid?
- [ ] Will schools operate under one tenant if they have multiple legal entities/brands?
- [ ] What are the top five workflows that pilot schools consider most painful?
- [ ] Which languages are required for the full interface at launch: English, Urdu, Roman Urdu?
- [ ] Which reports and fee challan formats are mandatory for target schools?

## B. Finance and payments

- [ ] Which accounting basis and chart-of-accounts template should be the default?
- [ ] Will a tenant have one consolidated ledger or separate books per legal school entity?
- [ ] What approval limits apply to discounts, waivers, refunds and expenses?
- [ ] Which banks' statement formats should be supported first?
- [ ] Which payment channels are essential for pilot: bank transfer, cash, cheque, Raast, JazzCash, Easypaisa or other?
- [ ] Does each campus have a separate bank account or are accounts shared?
- [ ] How will cash collected offline be reconciled if synchronization is rejected?
- [ ] What are the receipt numbering and fiscal-period rules?

## C. Curriculum and examinations

- [ ] Which boards must be supported first in content, not just system structure?
- [ ] Which official syllabus sources can be used and under what rights?
- [ ] Which Cambridge stages/qualifications are in the initial scope?
- [ ] What grading/report-card formats are required by each board/school?
- [ ] Who approves generated question papers and marking schemes?
- [ ] Which languages and subjects should be used for the first AI marking pilot?

## D. Offline and devices

- [ ] Which target devices and browsers are used by pilot schools?
- [ ] Are shared devices common?
- [ ] How long can devices remain offline?
- [ ] Which student fields may be cached locally?
- [ ] Is provisional offline cash receipt acceptable to school accountants?
- [ ] What is the policy for lost devices and pending local operations?
- [ ] Which conflicts can be automatically resolved and which require review?

## E. AI and privacy

- [ ] Which AI providers and data-processing terms are acceptable?
- [ ] Can any provider retain prompts or use submitted data for model training?
- [ ] What student data may be processed externally?
- [ ] What is the retention period for answer images and AI results?
- [ ] Which languages/subjects are included in initial evaluation datasets?
- [ ] What accuracy and teacher override thresholds are required before AI grading is enabled?
- [ ] Who is authorized to view Early Intervention signals?

## F. Architecture and operations

- [ ] Shared database with tenant-scoped rows confirmed, or alternate tenancy model required?
- [ ] Exact Tenant → School → Campus hierarchy and subscription boundary?
- [ ] RLS context and connection-pooling implementation?
- [ ] Module-by-module migration ownership strategy?
- [ ] File/object storage provider?
- [ ] Backup location, encryption and restore responsibilities?
- [ ] VPS provider, region, monitoring and uptime arrangement?
- [ ] Will a separate AI processing service be justified later, or remain an adapter inside the modular monolith initially?

## G. Legal and commercial review

- [ ] Pakistan-specific privacy/cyber/education legal review.
- [ ] Child/guardian consent and school data-controller responsibilities.
- [ ] Terms of service, privacy policy and data-processing agreements.
- [ ] Cambridge and board content licensing/usage rights.
- [ ] Payment-provider and bank API contractual requirements.
- [ ] Customer data export, retention and termination policy.

## Decision recording rule

For every material decision, record: context, options considered, selected option, consequences, owner and date in an ADR or product decision log.


## H. Hybrid deployment, licensing and local operations

- [ ] Will first pilot use one shared group node or independent local node per campus?
- [ ] Who owns the local server hardware: SchoolOS or school customer?
- [ ] Will SchoolOS offer a managed appliance with restricted OS root access?
- [x] Renewal channels: both automatic online renewal and manual signed `.lic` file import/USB fallback are required.
- [x] Expiry behavior: Restricted Mode after grace; preserve historical read/export, backup, renewal and support; block new operational writes.
- [ ] What offline license lease duration and grace period are commercially acceptable? (Seven calendar days is a proposal only.)
- [ ] What exact operation-by-operation allow/deny matrix is required for Restricted Mode?
- [ ] How long may cloud subscription/user/device revocation remain unapplied to a disconnected node?
- [ ] Which hardware/TPM/secure boot capabilities can be required of pilot devices?
- [ ] Which local backup target and off-site backup destination will be supported?
- [ ] What data is allowed to synchronize to cloud for billing, reporting, backups and parent access?
- [ ] Is remote parent/student access required in release one? If yes, which minimum data projection is acceptable?
- [ ] What is the campus-to-cloud data ownership policy for students/guardians transferred across campuses?
- [ ] Which AI functions must work offline, if any, and what local hardware budget is acceptable?
- [ ] What are customer responsibilities for power, UPS, LAN, internet, physical server security and OS admin?
- [ ] What data export and retention window applies after subscription cancellation?
- [ ] What support hours and local recovery SLA can the business actually provide?

## Decision notes

- Full independent offline operation at each campus requires a local node at each such campus. A shared head-office node only works while branches can reach it over the private network.
- Monthly subscription enforcement on a disconnected node is bounded by the last signed lease. Immediate remote revocation while fully offline is not technically possible.
- Confirmed renewal policy: both automatic online renewal and manual signed `.lic` file import/USB fallback.
- Confirmed expiry policy: Restricted Mode after grace, preserving historical data access, reports/export, backup, renewal and support while blocking new operational writes. Renewal reactivates the existing installation without a software update.
- If the customer has OS root access, software-only licensing can be tampered with. Decide whether to offer a SchoolOS-managed appliance.
- The seven-day grace period is only a proposal until approved commercially.
- Remote parent portal and cloud AI depend on connectivity and should display freshness/availability status.


## AI provider and data governance questions (v1.3)

- Which external AI providers/models meet required quality, contract, region and retention requirements for each data class?
- What exact data classes may be sent externally, and what notices/consents/contract clauses are required?
- What retention schedule applies to prompts, outputs, uploaded documents, embeddings, logs and backups?
- Which provider offers are eligible for zero/modified retention, and under what conditions?
- Which tasks require private/local-only inference?
- What local hardware profiles and model sizes are commercially supportable?
- What is the approved policy for customer opt-in fine-tuning, deletion and dataset provenance?
- What are included AI credits, paid add-on prices, overage behavior and paper-checking units?
- What evaluation thresholds must each language/subject/task meet before release?

## I. Commercial pricing and billing (v1.5)

The following choices are recorded as the current analysis baseline, not final tariff approval: organization-level subscription; organization-wide unique active student count; first Local Node included; additional nodes potentially charged; school provides local hardware; all standard core modules across packages; monthly student snapshots; paid one-time onboarding; monthly and annual billing.

- [ ] Validate Rs. 2,500/month base, 200 included students and Rs. 10 per additional student/month with pilot customers and cost data.
- [ ] Approve final capacity/package bands and whether large organizations receive volume discounts.
- [ ] Set additional active Local Node monthly fee (Rs. 1,000 is only an unapproved test hypothesis).
- [ ] Set onboarding/installation fee and included service scope (Rs. 5,000 is only an unapproved test hypothesis).
- [ ] Decide additional-node installation/setup charge.
- [ ] Decide annual discount (10% is an unapproved test hypothesis).
- [ ] Decide monthly snapshot date, preview period, dispute/correction window and timezone.
- [ ] Decide whether suspended-but-enrolled students are billable.
- [ ] Decide how annual customers are charged for monthly overage: monthly, quarterly or annual true-up.
- [ ] Decide node deactivation, replacement, transfer, pro-rating and billing stop rules.
- [ ] Approve hardware/OS readiness, school vs SchoolOS support responsibilities and recovery SLA.
- [ ] Approve taxes, withholding, payment fees, refunds, credit notes, overdue and cancellation rules.
- [ ] Approve data export/retention and licence behavior after organization subscription termination.


## J. Business & Customer Platform (v1.6)

The product choices for demo, registration and support channels are confirmed. The following implementation and operating details remain open:

- [ ] What final company domain/hostnames and DNS ownership will be used?
- [ ] Will public website use server-side rendering/SEO framework or a React SPA with a rendering layer?
- [ ] Which identity provider/auth strategy will serve staff and customer portal users?
- [ ] What MFA policy, session duration, device/session revocation and account recovery rules apply?
- [ ] Approve the detailed staff/customer role-permission matrix and separation-of-duties requirements.
- [ ] Benchmark dedicated Demo Runtime + session-scoped RLS against per-session schema/database or isolated runtime; approve the final mechanism after threat/load/cost testing.
- [ ] Approve final heartbeat/client lease/reconnect grace, inactivity timeout, absolute lifetime, concurrency limit and cleanup completion target. (Initial proposals: 30s heartbeat, 90s lease, 60s reconnect grace, 15m inactivity, 2h absolute lifetime and 1m cleanup sweep.)
- [ ] Confirm backup/snapshot exclusions and maximum residual-data lifetime for disposable demo data.
- [ ] Decide whether first-release demo needs file uploads; current recommendation is to disable arbitrary uploads.
- [ ] Define minimal retention for non-content demo lifecycle/security events.
- [ ] What verification is required for organization registration (business proof/contact verification/duplicate checks)?
- [ ] Which cases require mandatory manual review, and what is the expected review turnaround?
- [ ] What trial duration, limits and allowed features apply?
- [ ] Which payment methods/provider and bank verification evidence policy will be used?
- [ ] Which WhatsApp Business provider, number ownership, template policy and consent process will be used?
- [ ] What are support hours, staffing model, priority definitions, SLA targets and escalation paths?
- [ ] Which live chat implementation/provider is preferred, and how will transcripts be retained?
- [ ] What customer data may SchoolOS support staff see during approved technical support?
- [ ] What is the exact approval and emergency access model for remote Local Node diagnostics?
- [ ] What are retention/deletion periods for leads, demo sessions, registrations, support tickets, chat, WhatsApp messages and attachments?
- [ ] What privacy notices, terms, data processing agreement, cookie/analytics consent and customer export rights are required?
- [ ] Which customer/node metrics may be exposed in portal and admin dashboards, and how fresh must they be?
- [ ] Which public plan prices and add-on rates are approved for publishing? Existing numerical prices remain analysis proposals, not final tariff approval.
- [ ] Which hosting, backup, monitoring, RPO/RTO and incident response commitments can the business operationally support?


## K. Ephemeral demo session design (v1.7)

The following product direction is accepted; the implementation details below remain open:

- [x] Instant demo is temporary and session-bound, not a permanent demo account.
- [x] Explicit logout revokes access immediately and initiates high-priority workspace cleanup.
- [x] Closing the last tab/window should end the demo automatically; because browser close notifications are unreliable, server-side client leases, reconnect grace, inactivity/absolute expiry and cleanup sweeps are required.
- [x] Refresh and multiple tabs must continue one valid demo session; closing one tab must not destroy another active tab's workspace.
- [x] Demo records are synthetic only, isolated from production and removed with temporary resources.
- [ ] Approve the final Demo Runtime isolation model and RLS strategy after threat/load testing.
- [ ] Approve timeout, concurrency, cleanup target and residual-data retention values; current values are test proposals, not customer-facing SLAs.
- [ ] Confirm that disposable demo data is excluded from long-lived production/customer backups and define handling for immutable infrastructure snapshots.
- [ ] Confirm first-release upload policy and the retention period for minimal operational lifecycle events.

**Decision note:** Browser `pagehide`/`sendBeacon` is only a best-effort hint. Server-side session state is authoritative. Revoke access immediately on logout; physical deletion runs asynchronously through an idempotent retryable cleanup worker. Do not claim immediate erasure from immutable backups unless the backup design actually supports it. See document 41 and ADR-021.
