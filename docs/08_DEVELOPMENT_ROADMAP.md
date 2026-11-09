# 08 — Phase-Wise Development Task Plan

**Planning rule:** Do not set calendar duration until team capacity, product scope, integrations and acceptance criteria have been assessed. Each phase must finish with working software, tests, documentation and a demo.

## Phase 0 — Product discovery and design

- [ ] Interview 15–20 private schools/school-chain stakeholders.
- [ ] Interview owners, principals, accountants, cashiers and teachers separately.
- [ ] Observe real fee collection and bank reconciliation.
- [ ] Collect anonymised sample fee registers, bank statement formats and receipt examples.
- [ ] Validate family/sibling payment workflows.
- [ ] Inventory selected Pakistani boards and Cambridge pathways.
- [ ] Validate student, parent, staff and campus permissions.
- [ ] Map current and desired workflows.
- [ ] Complete PRD and non-goals.
- [ ] Complete user/permission matrix.
- [ ] Complete initial ERD and data dictionary.
- [ ] Create architecture decision records.
- [ ] Create clickable wireframes for critical workflows.
- [ ] Confirm hosting, backup and legal-review approach.

**Exit criteria:** Product scope, key workflows and architectural decisions are reviewed and approved by product owner and technical lead.

## Phase 1 — Platform and tenancy foundation

- [ ] Create ASP.NET Core solution and React/TypeScript application.
- [ ] Configure PostgreSQL and EF Core/Npgsql.
- [ ] Define module dependency rules and architecture tests.
- [ ] Implement tenant, school and campus model.
- [ ] Implement user authentication and account lifecycle.
- [ ] Implement roles, permissions and scoped assignments.
- [ ] Implement tenant/campus authorization middleware or application context.
- [ ] Add PostgreSQL RLS strategy if adopted.
- [ ] Add audit foundation.
- [ ] Add API error/validation standards and OpenAPI.
- [ ] Add structured logs, correlation IDs and health checks.
- [ ] Add Docker development environment.
- [ ] Add CI build, test and security scanning.
- [ ] Add migration and seed-data strategy.

**Exit criteria:** Two test tenants and multiple campuses are isolated in automated API and database tests.

## Phase 2 — Admissions and student lifecycle

- [ ] Admission inquiry management.
- [ ] Application forms and validation.
- [ ] Document upload with private storage.
- [ ] Assessment/interview and approval workflows.
- [ ] Student/guardian/household entities.
- [ ] Duplicate student detection.
- [ ] Enrollment and class/section assignment.
- [ ] Sibling relationship management.
- [ ] Campus/school transfer workflow.
- [ ] Student withdrawal/archive workflow.
- [ ] Student and guardian access foundations.
- [ ] Historical record tests.

**Exit criteria:** Complete admission-to-enrollment workflow works for multiple campuses without duplicate or leaked records.

## Phase 3 — Fees, finance and reconciliation

- [ ] Fee categories and structures.
- [ ] Student fee assignments.
- [ ] Invoice and invoice-line generation.
- [ ] Instalments and partial payments.
- [ ] Discounts, waivers and scholarships with approval rules.
- [ ] Household/sibling payment allocations.
- [ ] Cash/bank/cheque payment methods.
- [ ] Receipt/challan generation and unique identifiers.
- [ ] Chart of accounts.
- [ ] Double-entry ledger and balanced posting.
- [ ] Expenses, vendors and cashbook.
- [ ] Accounting periods and closing controls.
- [ ] Bank account configuration.
- [ ] CSV/XLSX statement templates and import batches.
- [ ] Duplicate import/transaction detection.
- [ ] Match suggestion engine.
- [ ] Unmatched payment queue.
- [ ] Manual match approval and allocation.
- [ ] Reversal and refund workflows.
- [ ] Transactional outbox and notification events.
- [ ] Daily cash closing and campus/group reports.
- [ ] Finance integration and audit tests.

**Exit criteria:** A realistic fee cycle is completed and independently reviewed: invoice → payment → allocation → balanced ledger → statement match → report.

## Phase 4 — Academic foundation and offline workflows

- [ ] Board and curriculum configuration.
- [ ] Curriculum versioning.
- [ ] Grade, subject, topic and learning-outcome structures.
- [ ] Class/section and teacher allocation.
- [ ] Timetable.
- [ ] Student attendance.
- [ ] Staff attendance and leave.
- [ ] PWA manifest and service worker.
- [ ] IndexedDB schema and local migrations.
- [ ] Device registration and user scope.
- [ ] Offline operation envelope and sync queue.
- [ ] Server idempotency and acknowledgement API.
- [ ] Conflict detection and review UI.
- [ ] Offline attendance.
- [ ] Offline diary/lesson-plan drafts.
- [ ] Offline marks drafts.
- [ ] Provisional offline cash receipt workflow.
- [ ] Local cache cleanup and device revocation.
- [ ] Network loss, force-close and retry tests.

**Exit criteria:** Offline operations survive defined device/network failure tests and are acknowledged or visibly flagged after sync.

## Phase 5 — Examinations and assessment

- [ ] Question bank and tagging.
- [ ] Curriculum/topic/outcome mapping.
- [ ] Exam schedule and setup.
- [ ] Exam blueprint and weighting.
- [ ] Paper creation and review.
- [ ] Answer keys and rubrics.
- [ ] Marks entry and import (where suitable).
- [ ] Validation and moderation.
- [ ] Approval and publication controls.
- [ ] Report cards and transcripts.
- [ ] Recheck workflow.
- [ ] Historical grading/curriculum version tests.

**Exit criteria:** Exam-to-published-result workflow is fully auditable and role-protected.

## Phase 6 — Portals and communication

- [ ] Role-specific principal dashboard.
- [ ] Accountant/cashier workspace.
- [ ] Teacher workspace.
- [ ] Parent portal.
- [ ] Student portal.
- [ ] Announcements and notices.
- [ ] Notification template system.
- [ ] SMS provider adapter.
- [ ] WhatsApp provider adapter.
- [ ] Email adapter.
- [ ] Delivery status and retry handling.
- [ ] Parent-child access controls.
- [ ] Language and RTL review.

**Exit criteria:** Parents see only their linked children and message delivery status is traceable.

## Phase 7 — First AI release

- [ ] AI provider abstraction and credential management.
- [ ] Tenant-aware AI authorization context.
- [ ] Prompt/model versioning.
- [ ] AI background jobs and status API.
- [ ] AI usage/cost tracking.
- [ ] Structured result and evidence schema.
- [ ] AI Auditor initial financial checks.
- [ ] Business Copilot approved reporting tools.
- [ ] Human review and feedback workflow.
- [ ] AI evaluation set and regression tests.
- [ ] Provider outage/fallback tests.

**Exit criteria:** AI results are grounded in permitted data, include evidence and cannot bypass normal business permissions.

## Phase 8 — Advanced AI release

- [ ] Learning Recovery topic analysis.
- [ ] Personalized worksheet generation.
- [ ] Exam paper generator enhancements.
- [ ] Question quality and curriculum checks.
- [ ] OMR objective answer checking.
- [ ] Handwritten answer processing pilot.
- [ ] Rubric-based suggested marks.
- [ ] Teacher correction and approval.
- [ ] Urdu/Roman Urdu speech recognition evaluation.
- [ ] Voice Assistant read-only queries.
- [ ] Voice actions with confirmation.
- [ ] Student Early Intervention signals.
- [ ] Fairness and false-positive evaluation.

**Exit criteria:** Each AI capability passes a defined domain test set and human-review policy.

## Phase 9 — Advanced school operations

- [ ] HR and employee lifecycle.
- [ ] Payroll and approvals.
- [ ] Transport routes and allocation.
- [ ] Library circulation.
- [ ] Inventory and procurement.
- [ ] Visitor management.
- [ ] Complaints and feedback.
- [ ] School events/calendar.
- [ ] Advanced organization/group reporting.

**Exit criteria:** Only modules validated by customer demand are released.

## Hybrid deployment work packages (must be integrated with the phases above)

The selected hybrid model adds the work packages in `17_HYBRID_DEPLOYMENT_TASK_PLAN.md`. They are not optional post-launch tasks. At minimum, Cloud Control Plane, Local Data Plane packaging, signed license leases, secure synchronization, local backups, signed updates and node/site monitoring must be implemented and tested before a hybrid customer is considered production-ready.

### H0 — Deployment discovery
- [ ] Select shared group node or per-campus edge topology for each pilot.
- [ ] Define managed appliance versus customer-managed server.
- [ ] Define hardware, network, UPS, OS patching and support responsibilities.
- [ ] Define offline lease/grace and offline renewal policy.
- [ ] Define cloud replication, backup and parent remote portal scope.

### H1 — Cloud Control Plane
- [ ] Tenant registry, subscriptions, billing and payment verification.
- [ ] Node registry, entitlements, license issuance and signed release catalogue.
- [ ] Sync hub and node health reporting.

### H2 — Local Data Plane
- [ ] Package ASP.NET Core + React + PostgreSQL for local Linux deployment.
- [ ] Local authentication, local HTTPS, persistent volumes and local health checks.

### H3 — Subscription and license enforcement
- [ ] Signed node-bound offline leases.
- [ ] Local validation, grace/restricted modes, offline renewal import and time rollback handling.

### H4 — Synchronization
- [ ] Transactional Outbox/Inbox, mTLS node identity, versioned events, idempotency and conflict review.

### H5 — Operations
- [ ] Local/off-site backups, restore, signed updates, monitoring and support access controls.

### H6 — Multi-campus topology
- [ ] Multi-node site registry, record ownership, campus event routing and consolidated reporting freshness.

### H7 — Hybrid pilot hardening
- [ ] Internet/power outage, node replacement, lease expiry, sync and security drills.

## Phase 10 — Pilot, hardening and launch

- [ ] Select 3–5 pilot schools.
- [ ] Prepare data migration templates.
- [ ] Train school staff and define support channels.
- [ ] Measure reconciliation accuracy and resolution time.
- [ ] Test daily and month-end closing.
- [ ] Test offline reliability on actual low-cost devices.
- [ ] Conduct tenant-isolation and role-permission tests.
- [ ] Conduct load/performance testing.
- [ ] Conduct backup/restore drill.
- [ ] Review AI quality and provider data terms.
- [ ] Complete legal/privacy/security review.
- [ ] Complete incident response and rollback runbooks.
- [ ] Complete production deployment and monitoring.
- [ ] Record pilot feedback and release decision.

**Exit criteria:** No unresolved critical security, financial-integrity or data-loss defects; pilot customers approve operational readiness.

## Definition of Done (applies to every task)

A task is not Done merely because code was written. It must have:

- Acceptance criteria satisfied.
- Relevant automated tests passing.
- Authorization and tenant-scope tests where applicable.
- Code review completed.
- Documentation/API contract updated.
- Audit and error-handling behaviour reviewed.
- Progress tracker and work log updated.
- No unresolved critical defect.


## Business & Customer Platform roadmap (v1.6)

The BCP is an additional Cloud Control Plane delivery stream. It is tracked in `39_BUSINESS_CUSTOMER_PLATFORM_TASK_PLAN.md` and `40_BUSINESS_CUSTOMER_PLATFORM_PROGRESS_TRACKER.md`. All phases below are **Not Started**.

- **BCP-0 — Discovery & architecture:** confirm journeys, hostnames, data classifications, module boundaries, auth/session audiences and operational model.
- **BCP-1 — Identity, permissions & audit:** staff/customer identity, MFA, tenant-scoped authorization, admin separation of duties and audit.
- **BCP-2/3 — Website, CMS and CRM:** public product/pricing pages, content workflows, lead capture and sales pipeline.
- **BCP-4 — Demo:** synthetic data templates, isolated sessions, reset/expiry and personalized demo appointments.
- **BCP-5/6 — Registration and Super Admin:** verified self-registration, assisted registration, manual approval and organization operations.
- **BCP-7/8 — Billing/licensing integration and Customer Portal:** integrate the existing subscription/licence policy; expose authorized commercial/node metadata to customers.
- **BCP-9/10 — Support, notifications and monitoring:** built-in tickets/chat/help center, WhatsApp, consent-aware notifications and node health read models.
- **BCP-11 — QA and production readiness:** tenant isolation, security review, restore, support readiness, privacy/legal and launch gates.

**Dependency rule:** do not release admin or portal workflows before BCP-1 authorization boundaries and audit are ready. Do not activate production customers from registration alone; approval, payment, subscription, node and licence gates remain distinct.
