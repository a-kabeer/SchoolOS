# 43 — Operational Domain Data Dictionary

- **Document ID:** DOC-43
- **Status:** Draft
- **Decision maturity:** Proposed logical data dictionary; DDL approval pending
- **Owner:** Technical lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ 898812f89e0ec7f03aff6c3b1408e46782814dfd; reference sources pinned in DOC-61
**Version:** v1.8 draft · **Status:** Logical data dictionary for review, not an approved migration script  
**Database boundary:** Local Data Plane operational PostgreSQL database  
**Companion:** `docs/04_DATABASE_BLUEPRINT.md`, `docs/36_PLATFORM_DATABASE_BLUEPRINT.md`

## 1. Scope and boundary

This dictionary describes the proposed operational data model for one SchoolOS Local Data Plane. It does not define the Cloud Control Plane commercial database. Cloud-side BCP entities remain in `docs/36_PLATFORM_DATABASE_BLUEPRINT.md`.

- Local PostgreSQL is authoritative for the operational records owned by that Local Node.
- Cloud Control Plane stores platform/commercial metadata and only approved, purpose-limited projections.
- A Local Node may serve one campus or multiple campuses according to the deployment profile.
- `TenantId`, `SchoolId`, `CampusId` and `OriginNodeId` are retained where applicable even if a first pilot has only one value.
- The physical schema, final names, UUID strategy and exact RLS strategy require technical-lead approval before migrations.

## 2. Data-type conventions

| Logical type | PostgreSQL recommendation | Rule |
|---|---|---|
| Identifier | `uuid` | Recommended globally unique identifiers; final universal choice remains open |
| Short text | `varchar(n)` | Use explicit length for bounded values |
| Long text | `text` | Do not use as a substitute for structured relations |
| Money | `numeric(14,2)` + currency code | Never use `real`, `double precision` or C# `float` for money |
| General decimal | `numeric(p,s)` | Choose precision per business rule |
| Date only | `date` | Birthday, school day, due date |
| Instant | `timestamptz` | Store UTC instants; render in school timezone |
| Boolean | `boolean` | True/false |
| Controlled status | `text` + CHECK or lookup FK | Avoid undocumented free-text status values |
| JSON document | `jsonb` | For schema-governed flexible attributes, not core relational facts |
| Version | `bigint` | Aggregate version / monotonic sequence where required |
| Hash | `bytea` or normalized hex text | Define algorithm and canonicalization |
| File reference | UUID + private object key | Do not store public unrestricted file paths |

All timestamps that represent an actual moment use UTC at rest. A local school-day value is a `date` and must not be derived from a UTC timestamp without applying the school's configured timezone.

## 3. Common column rules

Common columns are added only where appropriate; do not mechanically add every column to every table.

| Column | Type | Nullable | Purpose |
|---|---|---:|---|
| `id` | uuid | No | Primary identity |
| `tenant_id` | uuid | Usually no | Customer scope and provenance |
| `school_id` | uuid | Depends | Legal school/entity scope |
| `campus_id` | uuid | Depends | Campus scope |
| `origin_node_id` | uuid | Depends | Node that originated the record/event |
| `created_at_utc` | timestamptz | No | Server creation time |
| `created_by` | uuid/text | Depends | Authenticated actor |
| `updated_at_utc` | timestamptz | Yes | Last update time |
| `updated_by` | uuid/text | Yes | Actor who last updated |
| `row_version` | bigint | For mutable records | Optimistic concurrency |
| `status` | controlled text/FK | Depends | State-machine state |

Use restrictive deletion for historical, financial, audit and synchronization records. Deactivation/status transitions are preferred over physical deletion. Personal-data deletion/anonymization behavior must follow an approved retention policy.

## 4. Identity and people

### 4.1 `identity.persons`

Represents one natural person regardless of school role.

| Field | Type | Rule / meaning |
|---|---|---|
| `id` | uuid | PK |
| `tenant_id` | uuid | Tenant ownership |
| `first_name` | varchar(100) | Required |
| `middle_name` | varchar(100) | Optional |
| `last_name` | varchar(100) | Required according to school policy |
| `display_name` | varchar(250) | Optional derived or preferred display |
| `date_of_birth` | date | Optional; access restricted |
| `gender_code` | text/FK | Optional; controlled reference |
| `primary_phone` | varchar(30) | Normalize internationally |
| `primary_email` | varchar(254) | Normalize for matching |
| `preferred_language` | text | e.g. en, ur; allowed values configured |
| `is_active` | boolean | Default true |

CNIC, B-Form and other government identifiers must be kept in a separately classified identifier table with access restrictions, uniqueness scope and encryption/tokenization policy approved before implementation. Do not use a national identifier as the public student ID.

### 4.2 `identity.users`

Login identity; do not duplicate the password/credential storage in person or student tables.

| Field | Type | Rule / meaning |
|---|---|---|
| `id` | uuid | PK |
| `tenant_id` | uuid | Tenant scope for local school users |
| `person_id` | uuid | FK to persons where user maps to a person |
| `username_normalized` | varchar(256) | Unique within approved identity boundary |
| `email_normalized` | varchar(254) | Optional; unique policy to be approved |
| `identity_subject` | varchar(300) | External identity subject if delegated |
| `is_enabled` | boolean | Account enablement |
| `locked_until_utc` | timestamptz | Optional lockout |
| `last_login_at_utc` | timestamptz | Optional |

Credentials, password hashes, MFA factors and refresh/session tokens belong to ASP.NET Identity or the selected identity provider's approved storage model. Never store plaintext or reversible passwords.

### 4.3 `identity.roles`, `identity.permissions`, `identity.user_roles`, `identity.user_scope_assignments`

- Role: stable role code, display name, description, built-in/custom flag and active status.
- Permission: stable key such as `students.student.read`, module, action and description.
- UserRole: user, role, effective-from/effective-to and assigning actor.
- UserScopeAssignment: user/role, scope type (tenant, school, campus, class, section, assigned-resource), scope ID, effective dates and assigning actor.

A role is not itself a scope. The authorization decision combines permission + scope + resource relationship.

## 5. Organization and academic calendar

### 5.1 `organization.schools`

| Field | Type | Rule / meaning |
|---|---|---|
| `id` | uuid | PK |
| `tenant_id` | uuid | Owning customer |
| `school_code` | varchar(40) | Unique within tenant |
| `legal_name` | varchar(200) | Required |
| `display_name` | varchar(200) | Required |
| `timezone_id` | varchar(80) | IANA timezone |
| `default_currency` | char(3) | Initially PKR; currency code explicit |
| `is_active` | boolean | Status |

### 5.2 `organization.campuses`

| Field | Type | Rule / meaning |
|---|---|---|
| `id` | uuid | PK |
| `tenant_id` | uuid | Tenant scope |
| `school_id` | uuid | FK to schools |
| `campus_code` | varchar(40) | Unique within school |
| `name` | varchar(200) | Required |
| `address_json` | jsonb | Validated address structure; final fields to approve |
| `timezone_id` | varchar(80) | Defaults from school, may override if needed |
| `is_active` | boolean | Active campus |

### 5.3 `organization.academic_years` and `organization.terms`

AcademicYear fields: `id`, `tenant_id`, `school_id`, `code`, `name`, `start_date`, `end_date`, `is_current`, `status`.

Term fields: `id`, `academic_year_id`, `parent_term_id` (optional), `code`, `name`, `start_date`, `end_date`, `sequence`, `is_grade_reporting_period`, `status`.

Constraints: end date must be on/after start date; only one current academic year per approved school scope; terms must remain within parent academic-year bounds unless an approved exception exists. Academic history is never overwritten when the current year changes.

### 5.4 `organization.grade_levels` / `organization.sections`

GradeLevel: `id`, `school_id`, `code`, `name`, `sequence`, `active`.

Section: `id`, `campus_id`, `academic_year_id`, `grade_level_id`, `code`, `name`, `capacity`, `active`.

Unique business key: campus + academic year + grade level + section code. A school may configure no sections for a grade; do not create fake section values for those cases.

## 6. Admissions and student lifecycle

### 6.1 `admissions.inquiries`

Fields: `id`, tenant/school/campus IDs, inquiry number, applicant name or provisional person link, guardian contact, requested academic year, grade, source channel, owner, status, received-at, next-follow-up-at, created metadata.

Inquiry is a lead-like admission record and is not yet an enrolled student.

### 6.2 `admissions.applications`

Fields: `id`, inquiry ID, applicant person ID (when verified), requested academic year, grade level, campus, application number, form version, status, submitted-at, decision-at, decision-by, decision-reason.

Status transitions must be controlled (Draft → Submitted → UnderReview → Accepted/Rejected/Waitlisted/Withdrawn). Accepted applications do not automatically create duplicate Person records.

### 6.3 `admissions.application_documents` and `admissions.application_reviews`

Document metadata: application ID, private file object ID, document type, original name, content type, size, checksum, scan status, uploaded-by and uploaded-at.

Review: application ID, reviewer, review step, outcome, comments, decision-at. Sensitive document downloads require resource-level authorization and audit.

### 6.4 `students.student_profiles`

| Field | Type | Rule / meaning |
|---|---|---|
| `id` | uuid | PK |
| `person_id` | uuid | FK to identity.persons |
| `tenant_id` | uuid | Scope |
| `student_number` | varchar(40) | Stable school-facing identifier |
| `admission_number` | varchar(40) | Unique within approved school scope |
| `admission_date` | date | Required after admission |
| `profile_status` | text | Controlled |
| `custom_data_json` | jsonb | Schema-governed custom attributes only |

### 6.5 `students.households`, `students.guardian_profiles`, `students.student_guardians`

Household fields: ID, tenant ID, household code, display name, billing contact person ID, active status.

GuardianProfile fields: ID, person ID, tenant ID, occupation (optional), employer (optional), preferred contact channel.

StudentGuardian fields: student ID, guardian person ID, relationship code, is-primary-contact, is-emergency-contact, has-legal-custody, can-collect-student, receives-academic-notices, receives-financial-notices, effective dates.

A guardian may link to multiple students. A student may have multiple guardians. Do not assume that the payer, legal guardian, emergency contact and notification recipient are always the same person.

### 6.6 `students.household_members`

Fields: household ID, person/student profile reference, relationship code, is-billing-member, effective-from/to. Enforce no duplicate active membership for the same household/person. Family/sibling fee views derive from approved household membership; they must not merge unrelated students by matching surname or phone alone.

### 6.7 `students.enrollments`

Fields: ID, tenant/school/campus IDs, student profile ID, academic year ID, grade level ID, section ID (nullable where sections are not configured), roll number, start date, end date, enrollment status, previous enrollment ID (optional), created metadata.

Unique rule: a student cannot have overlapping active enrollments in the same school/year unless an explicit approved transfer/dual-enrollment rule exists. Promotion creates a new historical enrollment; it does not overwrite the prior year.

### 6.8 `students.student_status_history`, `students.transfers`, `students.withdrawals`

Keep reason, effective date, actor, approval, source/target campus and linked enrollment. Historical records must remain traceable.

## 7. Academics and curriculum

### 7.1 Core tables

- `academics.boards`: code, name, jurisdiction, active status.
- `academics.curricula`: board ID, qualification/pathway, name, language, active status.
- `academics.curriculum_versions`: curriculum ID, version, effective dates, source reference, approval status.
- `academics.subjects`: code, name, language/aliases and active status.
- `academics.curriculum_subjects`: curriculum version, grade/qualification, subject and assessment configuration.
- `academics.topics`: subject/version, parent topic, code, title, sequence.
- `academics.learning_outcomes`: topic/version, stable code, description, assessment indicators.
- `academics.class_subjects`: academic year, class/grade, section (optional), subject, teacher allocation and effective dates.

Official syllabus content and usage rights require external verification before loading. Architecture support does not mean every board's content is populated.

### 7.2 Lesson plans and homework

LessonPlan: teacher, class/section, subject, curriculum version, topic/outcomes, lesson date, objectives, resources, status and review metadata.

Homework: class/section, subject, teacher, title, instructions, due date, attachments, publication status.

Submission: homework ID, student ID, submitted-at, content/file reference, status, feedback, marked-by, marked-at.

## 8. Attendance

### 8.1 `attendance.sessions`

Fields: ID, tenant/school/campus, attendance date (date), session type (daily/period/staff), grade/section or period, opened-by, opened-at, closed-by, closed-at, status.

### 8.2 `attendance.records`

Fields: ID, session ID, student enrollment ID, attendance code, reason code, comment, source (web/mobile/biometric/import), device ID, client operation ID, client occurred-at, server recorded-at, recorded-by, row version.

Unique rule depends on attendance mode: one daily mark per enrollment/date, or one period mark per enrollment/date/period. Define the mode before creating the unique index.

### 8.3 `attendance.staff_records`, `attendance.leave_requests`, `attendance.devices`

StaffRecord: employee ID, attendance date, in/out timestamps, source, device and correction approval.

LeaveRequest: employee/student subject, date range, leave type, reason, attachment reference, status and approver.

Device: device ID, node ID, assigned user/scope, public-key or credential reference, status, enrolled-at, revoked-at and last-seen-at.

Device credentials are secret material and must never be exposed in API responses.

## 9. Examinations and results

### 9.1 `examinations.exam_sessions`

Academic year, term, campus, exam code/name, start/end dates, status, owner and approval metadata.

### 9.2 `examinations.assessments` / `examinations.exam_subjects`

Assessment: exam session, subject, grade/section, max marks, weightage, assessment date, duration, status.

ExamSubject: exam session, subject, syllabus/curriculum version, paper configuration, grading scheme.

### 9.3 `examinations.marks`

Assessment ID, student enrollment ID, obtained marks (numeric), absent flag, exemption flag, entered-by, entered-at, row version, moderation state. Exactly one of obtained marks/absence/exemption states is valid according to a documented rule. Values cannot exceed maximum marks unless an explicit policy supports bonus marks.

### 9.4 `examinations.grading_schemes`, `examinations.grade_bands`

GradingScheme: board/pathway, version, effective dates, maximum score basis, rounding policy.

GradeBand: scheme ID, lower bound, upper bound, grade label, GPA value (optional), remarks/descriptor and sequence.

No board grading rule should be hard-coded in UI logic. Published results retain the grading-scheme version used.

### 9.5 `examinations.result_approvals` and `examinations.report_cards`

Approval: exam/grade scope, approver, outcome, reason, approved-at and version.

ReportCard: student enrollment, exam/term, immutable published snapshot, grading version, published-by/at, snapshot hash and revision number. Corrections create a new revision with explicit reason; never silently overwrite a published result.

## 10. Fees and finance

### 10.1 Fees entities

| Table | Key fields |
|---|---|
| `fees.fee_heads` | id, tenant/school, code, name, category, taxable/configuration code, active |
| `fees.fee_structures` | id, school/campus, academic year, grade, effective dates, name, status |
| `fees.fee_structure_items` | structure ID, fee head ID, amount numeric(14,2), frequency, due-day, optional flag |
| `fees.student_fee_assignments` | student/enrollment, structure, effective dates, approved overrides |
| `fees.invoices` | invoice number, payer/household, issue date, due date, currency, gross/discount/tax/net totals, status |
| `fees.invoice_items` | invoice ID, fee head, description, quantity, unit amount, discount/tax/net amount |
| `fees.payments` | payment number, payer, channel, amount, currency, received-at, source reference, status |
| `fees.payment_allocations` | payment ID, invoice ID, amount, allocation status, allocated-at, actor |
| `fees.receipts` | receipt number, payment ID, provisional/confirmed state, issued-at, printable snapshot reference |
| `fees.concessions` | student/household, rule, amount/percent, validity, request, approval and reason |
| `fees.adjustments` | invoice/payment, adjustment type, amount, reason, approval and reversal link |
| `fees.refunds` | payment/allocation, amount, method, reason, approval, external reference and status |

Money uses `numeric(14,2)` or a reviewed precision and explicit currency. Amounts are non-negative for normal charge/payment rows; signed accounting values belong in journal lines. Invoice numbers and receipt numbers have issuer/year uniqueness rules approved per school.

### 10.2 Fee invariants

- A payment can be allocated across multiple invoices/siblings only when payer authorization and household relationships permit it.
- An invoice may be settled by multiple partial payments.
- Sum of active allocations cannot exceed payment amount.
- Sum of invoice allocations cannot exceed its outstanding amount except documented overpayment/advance-credit policy.
- A provisional offline receipt is not labelled confirmed until server validation.
- Posted financial transactions are immutable; corrections require reversal/adjustment with reason and actor.
- Do not infer student allocation solely from bank payer name or amount.

### 10.3 Finance entities

- `finance.chart_of_accounts`: code, name, account type, parent account, active state.
- `finance.journal_entries`: journal number, date, period, source type/ID, narration, status, posted-by/at, reversal-of ID.
- `finance.journal_entry_lines`: journal ID, account ID, debit, credit, currency, campus, student/party reference where appropriate.
- `finance.bank_accounts`: institution, masked account reference, currency, campus owner, active status.
- `finance.cash_accounts`: cashier/campus, currency, active status.
- `finance.expenses`: expense number, vendor, account, amount, tax fields, payment status and approval.

Journal constraint: each posted journal must balance by currency and approved accounting scope. A posting service should validate and commit the journal atomically. Posted entries are never edited in place.

## 11. Bank reconciliation

### 11.1 Tables

- `reconciliation.import_batches`: bank account, source filename, file hash, source period, imported-by/at, row count, duplicate status, parser version and batch status.
- `reconciliation.bank_transactions`: batch, source row key, bank reference, transaction date, value date, description, debit, credit, currency, normalized payer/reference, source payload hash.
- `reconciliation.match_suggestions`: bank transaction, candidate payment/invoice, confidence/rule, explanation and generated-at.
- `reconciliation.confirmed_matches`: bank transaction, payment/allocation, matched amount, reviewer, decision, reason and timestamp.
- `reconciliation.sessions`: bank account, statement period, opening/closing balances, imported totals, unmatched totals, closed-by/at and status.

Unique duplicate protection: source file hash/bank-account/period policy plus provider/bank reference where reliable. Duplicate imports must be visible and safely rejected or treated idempotently.

### 11.2 Reconciliation invariant

A bank credit is evidence of a bank transaction; it is not automatically an allocated student fee payment. A confirmed match must retain source row, reviewer, linked payment/allocation and any exception reason.

## 12. Communication, sync and audit

### 12.1 Communication

- `communication.templates`: channel, language, template key, content/version, provider category, approval state.
- `communication.notifications`: recipient/person, channel, template version, business reference, consent/purpose, queued-at and status.
- `communication.delivery_attempts`: notification ID, provider, provider message ID, attempt number, sent-at, delivery status, cost/currency and error code.
- `communication.message_preferences`: person, channel, purpose, consent/opt-out status, effective dates and source.

### 12.2 Synchronization

- `integration.outbox_messages`: event ID, aggregate type/ID/version, event type/schema version, tenant/school/campus/node IDs, occurred-at, payload reference, delivery state and retry metadata.
- `integration.inbox_messages`: source node, event ID, event type/version, received-at, processed-at, outcome and error code.
- `integration.sync_cursors`: peer/node, stream, last acknowledged sequence and updated-at.
- `integration.sync_conflicts`: aggregate, base/current/incoming version, source node, conflict type, state, assigned reviewer and resolution reference.

### 12.3 Audit

`audit.audit_events` records actor, action, resource type/ID, tenant/school/campus/node, server timestamp, correlation ID, source/device, reason, outcome and appropriately minimized before/after data. Never log passwords, auth tokens, private keys, raw secrets or unnecessary child data. Audit records are append-only to normal application roles.

## 13. Custom-field model

The customization research describes typed EAV, flex columns, dynamic columns and JSON. For SchoolOS, the proposed default is:

- Core business attributes remain real relational columns.
- Tenant-defined form-style attributes use a validated `jsonb` payload on the owning entity where appropriate.
- Custom attributes that require frequent filtering, sorting, unique constraints or cross-record reporting use typed definitions and value rows.
- Do not dynamically `ALTER TABLE` a shared operational table for each school.
- Do not store arbitrary unvalidated JSON keys directly from the client.

Proposed metadata:
- `customization.field_definitions`: tenant, entity type, stable field key, label, data type, required flag, options, validation rules, searchable flag, status and sort order.
- `customization.field_values`: tenant, entity type/record ID, field definition, typed value columns (text/number/date/boolean), updated-by/at.

Typed EAV values require a constraint that exactly one matching typed-value column is populated according to the field definition. The final physical constraint and query-index strategy must be approved. Custom-field changes should be versioned; deleting a definition must not silently erase historical values.

## 14. Cloud Control Plane boundary

Use `docs/36_PLATFORM_DATABASE_BLUEPRINT.md` for platform-side identity, CRM, registration, demo, subscription, commercial billing, licensing, portal, support, CMS, notification, reporting and audit entities.

The Cloud Control Plane must not store full student profiles, guardian records, fee invoices, marks or attendance by default. If an approved parent/reporting projection is required, define each field, purpose, refresh interval, retention period, customer authorization and access scope in a separate projection contract.

## 15. Required approval before DDL

Before generating EF migrations, approve:

- Entity names and module ownership.
- PK/FK strategy and cross-schema relationship policy.
- Nullability/defaults and state-machine values.
- Unique/check/exclusion constraints.
- Indexes and expected query patterns.
- Tenant/campus scope on each table.
- Data classification and retention.
- RLS policy and connection-pooling behavior.
- Immutable/soft-delete behavior.
- Migration, rollback and seed/reference-data strategy.

This document is a logical dictionary; it is not yet a substitute for reviewed DDL or generated EF Core migrations.


## 16. Guardian and household requirements companion

The focused guardian/household cardinality, open policy decisions and downstream acceptance criteria are maintained in [55 — Guardian and Household Requirements](55_GUARDIAN_AND_HOUSEHOLD_REQUIREMENTS.md). This data dictionary remains the proposed logical model reference; it does not approve legal custody, payer authority or communication permissions.


## 17. Reference-informed domain coverage review

The eSchool functional inventory is a discovery checklist, not a schema specification and not a mandate to reproduce its module or route count. The SchoolOS logical entities already described in this document remain authoritative subject to the approvals in section 15.

When reviewing domain coverage, verify the end-to-end lifecycle and invariants across:

- Student/person identity, guardian/household relationships, admissions, enrollment history, promotion, transfer and withdrawal.
- Fee structures, assignments, invoices/challans, payments, allocations, receipts, concessions, refunds and arrears.
- Finance accounts, journals, journal lines, approvals, posting, cash/bank movements, period locks and reversal history.
- Attendance sessions/records, staff attendance, correction requests, source/device identifiers and offline operation IDs.
- Examination sessions, assessments, marks, grading, moderation, result approval, publication and correction history.
- Role/scope assignments, audit events, communication preferences and permitted parent/teacher/student projections.

Do not create new tables merely because a legacy screen or route exists. Each entity must have one owning module, a documented business purpose, tenant/school/campus scope, retention/classification, constraints, API/event use and acceptance tests.

HR/payroll, library, inventory and transport are candidate capabilities discovered in eSchool; they are not silently added to the approved operational schema by this cross-check. They require product requirements, statutory/legal review where applicable, module ownership, pricing/entitlement decisions and task-register approval first. Pakistani payroll calculations must be verified against current qualified sources before implementation.

DOC-61 records the reference scope and non-adoption boundaries. DOC-44 remains the authority for physical PostgreSQL design and migration approval.

## 18. SaaS tenant and operational ownership contract

[DOC-62](62_SAAS_TENANT_IMPLEMENTATION_BLUEPRINT.md) defines the v1 implementation contract for tenant ownership and request context. Apply these rules when converting this logical dictionary into physical entities:

| Field | Required meaning |
|---|---|
| TenantId | Canonical customer Organization UUID; required on every tenant-owned operational aggregate |
| SchoolId | Owning legal/operating School; required where the entity is school-owned |
| CampusId | Owning Campus; required where the workflow or record is campus-scoped |
| OriginNodeId | Local Node that originated the record/event; required where the entity is locally created or synchronized |
| CreatedBy / UpdatedBy | Authenticated user or service actor; never a client-supplied display name |
| RowVersion / AggregateVersion | Concurrency/version marker where the aggregate supports concurrent change or synchronization |

Rules:

- TenantId on a Local Data Plane row is the same immutable UUID as organizations.Organizations.Id in the Cloud Control Plane. Do not create two distinct IDs for Tenant and Organization.
- Every operational module entity must be classified as tenant-owned, school-owned, campus-owned, resource-assigned or platform-global. Unclassified ownership is a schema review blocker.
- Operational records must not have a null TenantId. Truly global catalogues belong in an explicit global schema/table classification; do not use NULL to mean both global and unknown tenant.
- Every CampusId/SchoolId relationship must be validated against the same TenantId. Physical schema should use composite tenant-aware foreign keys where tables are relationally linked.
- Query filters are not authorization. The application derives tenant and campus scope from validated server-side identity; RLS provides database defense in depth.
- TenantId is immutable after creation. Tenant transfer is a controlled, audited migration.
- File metadata, audit events, imports, exports, notifications, background jobs and sync envelopes retain tenant provenance and repeat authorization at execution time.
- Local Nodes remain single-tenant in v1, even if the node serves multiple campuses of the same organization.

The field names in existing domain tables remain subject to DOC-44's approved physical ERD and DEC-002 key strategy. This section defines the ownership contract; it does not claim that migrations or RLS policies are implemented.
