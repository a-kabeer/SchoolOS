# 04 — Preliminary PostgreSQL Database Blueprint

**Status:** Logical table catalogue only. Detailed columns, datatypes, constraints, indexes, cardinalities and migration scripts are not yet approved.

> **Hybrid database topology update (v1.1):** The earlier “one PostgreSQL database initially” statement is superseded for hybrid deployment. Each Local Data Plane has its own operational PostgreSQL database; the Cloud Control Plane has a separate platform database for tenants, subscriptions, license leases, node registry, sync envelopes and optional projections. The cloud must not become an uncontrolled full copy of all local student/finance data. See `14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md` and `15_SUBSCRIPTION_LICENSING_AND_OFFLINE_ENFORCEMENT.md`.

## 1. Database strategy

Use one PostgreSQL operational database per Local Data Plane, with logical schemas owned by business modules. Use a separate Cloud Control Plane database for platform metadata. This keeps each customer/site operational data boundary explicit while preserving local transactional consistency.

Suggested schemas:

```text
platform
identity
organization
admissions
students
fees
finance
reconciliation
academics
attendance
examinations
hr
communication
ai
reporting
audit
integration
```

## 2. Multi-tenant hierarchy

Proposed model:

`Tenant → School / Legal Entity → Campus`

A school chain may contain one or more school entities and multiple campuses. This hierarchy must be validated with real customers because some customers may prefer a simpler tenant-to-campus structure.

### Tenant data rules

- Every tenant-owned business record includes `tenant_id`.
- Include `school_id` and/or `campus_id` when the record is scoped at those levels.
- Platform-global configuration must be separated from tenant records.
- Server-side authorization establishes the active tenant/scope.
- PostgreSQL RLS may provide a second layer of row filtering.
- Database roles, table ownership, connection pooling and RLS bypass behaviour require careful design and automated tests.
- Never rely only on React filtering or a client-provided tenant ID.

## 3. Common record metadata

Where appropriate:

- `id` — stable primary identifier (UUID recommended, final strategy to be set)
- `tenant_id`
- `school_id` / `campus_id` when applicable
- `created_at`, `updated_at`
- `created_by`, `updated_by`
- `row_version` or concurrency token where needed
- `status` with controlled state transitions

Do not add every field to every table mechanically. Platform-global, immutable event and child tables may require a different shape.

## 4. Table catalogue

### 4.1 Platform (local projection only)

The local operational database may keep a minimal local projection of tenant/node identity and entitlement state. Full subscription and billing records are owned by the Cloud Control Plane. See document 15 for its cloud-side schema catalogue.

- `platform_local.node_identity`
- `platform_local.license_snapshots`
- `platform_local.license_validation_events`
- `platform_local.entitlement_cache`
- `platform_local.device_registrations`

### 4.2 Identity

- `identity.users`
- `identity.roles`
- `identity.permissions`
- `identity.user_roles`
- `identity.role_permissions`
- `identity.user_scope_assignments`
- `identity.guardian_user_links`
- `identity.user_sessions` (only if not fully delegated to the identity framework)

### 4.3 Organization

- `organization.schools`
- `organization.campuses`
- `organization.academic_years`
- `organization.terms`
- `organization.departments`
- `organization.settings`
- `organization.holidays`

### 4.4 Admissions

- `admissions.inquiries`
- `admissions.applications`
- `admissions.application_documents`
- `admissions.assessments`
- `admissions.application_reviews`

### 4.5 Students

- `students.students`
- `students.guardians`
- `students.households`
- `students.student_guardians`
- `students.enrollments`
- `students.student_documents`
- `students.transfers`
- `students.withdrawals`
- `students.student_status_history`

### 4.6 Fees

- `fees.fee_categories`
- `fees.fee_structures`
- `fees.fee_structure_items`
- `fees.student_fee_assignments`
- `fees.invoices`
- `fees.invoice_items`
- `fees.installment_plans`
- `fees.payments`
- `fees.payment_allocations`
- `fees.receipts`
- `fees.discounts`
- `fees.waivers`
- `fees.adjustments`
- `fees.refunds`
- `fees.payment_reversals`

### 4.7 Finance

- `finance.chart_of_accounts`
- `finance.journal_entries`
- `finance.journal_entry_lines`
- `finance.bank_accounts`
- `finance.cash_accounts`
- `finance.expenses`
- `finance.vendors`
- `finance.purchase_records`
- `finance.fiscal_periods`
- `finance.reconciliation_control_accounts`

### 4.8 Reconciliation

- `reconciliation.import_batches`
- `reconciliation.bank_transactions`
- `reconciliation.match_suggestions`
- `reconciliation.confirmed_matches`
- `reconciliation.reconciliation_sessions`
- `reconciliation.unmatched_reasons`

### 4.9 Academics

- `academics.education_boards`
- `academics.curricula`
- `academics.curriculum_versions`
- `academics.grade_levels`
- `academics.subjects`
- `academics.curriculum_subjects`
- `academics.topics`
- `academics.learning_outcomes`
- `academics.topic_outcomes`
- `academics.class_subjects`
- `academics.lesson_plans`
- `academics.homework`
- `academics.homework_submissions`
- `academics.learning_resources`

### 4.10 Attendance

- `attendance.sessions`
- `attendance.records`
- `attendance.staff_records`
- `attendance.leave_requests`
- `attendance.devices`
- `attendance.sync_batches`
- `attendance.correction_requests`
- `attendance.sync_operations` (or a shared sync schema if multiple modules use it)

### 4.11 Examinations

- `examinations.exam_sessions`
- `examinations.exam_subjects`
- `examinations.question_banks`
- `examinations.questions`
- `examinations.exam_blueprints`
- `examinations.papers`
- `examinations.answer_keys`
- `examinations.marking_rubrics`
- `examinations.student_attempts`
- `examinations.marks`
- `examinations.result_approvals`
- `examinations.report_cards`
- `examinations.recheck_requests`

### 4.12 HR (later phase)

- `hr.employees`
- `hr.employee_documents`
- `hr.contracts`
- `hr.leave_balances`
- `hr.payroll_runs`
- `hr.payroll_items`

### 4.13 Communication

- `communication.templates`
- `communication.notifications`
- `communication.delivery_attempts`
- `communication.announcements`
- `communication.conversations`
- `communication.message_preferences`

### 4.14 AI

- `ai.provider_configs`
- `ai.prompt_versions`
- `ai.ai_requests`
- `ai.ai_jobs`
- `ai.ai_results`
- `ai.ai_reviews`
- `ai.ai_feedback`
- `ai.evaluation_runs`
- `ai.usage_records`
- `ai.risk_signals`

### 4.15 Audit and integration

- `audit.audit_events`
- `audit.approval_history`
- `audit.security_events`
- `integration.outbox_messages`
- `integration.inbox_messages` (if needed for consumer idempotency)
- `integration.webhook_receipts`

## 5. Critical relationships

- Tenant has many schools; school has many campuses.
- School has academic years and terms.
- Student can have multiple guardian links; guardian can link to multiple students.
- Household groups related guardians/students for family fee handling.
- Student has enrollment history; do not overwrite historical campus/class assignments.
- Fee structure has items; invoices have line items.
- Payment has one or more allocations; allocation references an invoice or a controlled receivable.
- Bank transaction may produce one or more confirmed allocations, within amount limits.
- Journal entry has multiple debit/credit lines and must balance.
- Curriculum has versions; versions contain subjects/topics/outcomes.
- Questions can map to topics and learning outcomes.
- Exam attempts connect students to papers/questions/answers; marks connect to rubric and approval history.
- AI outputs should point to source records or approved evidence references without duplicating unnecessary sensitive content.

## 6. Finance invariants

1. Use decimal/numeric types for money, never binary floating point.
2. Store currency explicitly; PKR is the initial default.
3. Every posted journal entry balances debits and credits.
4. Posted financial records are immutable; corrections use reversals/adjustments.
5. Payment allocations cannot exceed available payment amount.
6. A bank statement row is not automatically a student fee payment.
7. Import batches and source rows must remain traceable.
8. Financial operations require idempotency keys where requests may be retried.
9. Accounting period lock/reopen rules need authorization and audit.
10. Rounding and currency precision rules must be formally defined.

## 7. Curriculum and assessment data rules

- Board/curriculum definitions must be versioned.
- Grade/qualification mappings should be configurable.
- Subjects can vary by board, grade and qualification.
- Learning outcomes need stable identifiers within a version.
- Examination questions should carry curriculum/topic/outcome references when applicable.
- Historical results retain the curriculum/grading version used at the time.
- Cambridge and board content must only be loaded from authorized/appropriate sources.

## 8. Database decisions still open

- UUID versus another primary-key strategy.
- Tenant → School → Campus exact cardinality and billing boundary.
- Shared schema with `tenant_id` versus schema-per-tenant (recommended: shared schemas and tenant-scoped rows initially).
- RLS context-setting strategy with connection pooling.
- Whether Fees and Finance share a controlled transaction boundary or use an outbox-driven posting workflow.
- Document storage provider and metadata schema.
- Data retention, anonymization and deletion rules.
- Reporting projection strategy and eventual read replica needs.

## 9. Required database deliverables before Phase 2/3 coding

- ERD with cardinalities.
- Field-level data dictionary.
- Unique/check/foreign-key constraints.
- Index plan for common queries.
- Tenant and campus isolation policy per table.
- EF Core migration ownership strategy.
- Seed/reference data plan.
- Backup and restoration strategy.
- Finance transaction examples with journal postings.


## 7. Cloud Control Plane database catalogue

Do not mix the platform billing/licensing database with each Local Node's operational database. The Cloud Control Plane requires its own database (schema catalogue, not final ERD):

- `platform.tenants`
- `platform.subscription_plans`
- `platform.plan_entitlements`
- `platform.subscriptions`
- `platform.subscription_items`
- `platform.subscription_invoices`
- `platform.subscription_payments`
- `platform.subscription_events`
- `platform.license_nodes`
- `platform.license_leases`
- `platform.license_signing_keys` (metadata only; private keys live in a protected key store/HSM where feasible)
- `platform.node_activations`
- `platform.node_revocations`
- `platform.release_manifests`
- `platform.update_rollouts`
- `sync.cloud_inbox_events`
- `sync.cloud_outbox_events`
- `sync.node_cursors`
- `sync.delivery_attempts`
- `backup.backup_catalogue`
- Optional reporting projections / parent portal read models with explicit data minimization and retention.

## 8. Local synchronization schema

Each Local Node should have a dedicated shared synchronization schema:

- `sync.outbox_events`
- `sync.inbox_events`
- `sync.cursors`
- `sync.conflicts`
- `sync.delivery_attempts`
- `sync.schema_versions`

Events should include origin node, tenant/site scope, aggregate ID/version, event ID, event type/schema version and UTC event time. Financial entries must be immutable and idempotent; do not use last-write-wins for journals.
