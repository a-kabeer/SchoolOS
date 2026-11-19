# 44 — PostgreSQL ERD, Constraints and Migration Specification

- **Document ID:** DOC-44
- **Status:** Draft
- **Decision maturity:** Proposed ERD and migration baseline; approval required
- **Owner:** Technical lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)
**Version:** v1.8 draft · **Status:** Proposed physical-design baseline; approval required before migrations  
**Applies to:** Local Data Plane operational database  
**Companion:** docs/04_DATABASE_BLUEPRINT.md, docs/43_OPERATIONAL_DOMAIN_DATA_DICTIONARY.md

## 1. Database deployment boundaries

SchoolOS has two database ownership boundaries in the strategic hybrid model:

1. **Cloud Control Plane database:** platform identity, customer/organization registry, CRM, registration, demo, subscription, commercial billing, licensing, node registry, support, CMS, sync hub metadata and approved reporting projections. Use docs/36_PLATFORM_DATABASE_BLUEPRINT.md.
2. **Local Data Plane operational database:** school operations including people, admissions, student lifecycle, academics, attendance, examinations, fees, finance, reconciliation, communication, operational audit and local sync queues.

Do not create direct SQL foreign keys from the Cloud Control Plane database to a Local Data Plane database. Cross-plane integration uses authenticated, versioned events/commands and approved read-model projections.

Deployment profiles:
- Hybrid Local + Cloud Sync: each Local Node has its own operational database.
- Disconnected Local + Manual Data Exchange: every independently operating campus has its own Local Node/database; signed data packages are imported as controlled replicas.
- Cloud Centralized: a separate cloud-hosted operational deployment may use shared tenant-scoped rows and RLS after its own threat model and capacity review. This profile must not silently change the hybrid data boundary.

## 2. Logical schema ownership

| PostgreSQL schema | Owning module | Primary data |
|---|---|---|
| identity | Identity & Access | users, roles, permissions, scope assignments, persons |
| organization | Organization | schools, campuses, academic years, terms, grade levels, sections |
| admissions | Admissions | inquiries, applications, documents, reviews |
| students | Students | student/guardian profiles, households, enrollments, transfers |
| academics | Academics | boards, curricula, versions, subjects, topics, outcomes, class-subject allocation |
| attendance | Attendance | sessions, student/staff records, leave, devices |
| examinations | Examinations | exam sessions, assessments, marks, grading, approvals, report-card snapshots |
| fees | Fees | fee heads/structures, invoices, payments, allocations, receipts, concessions/refunds |
| finance | Finance | chart of accounts, journal entries/lines, cash/bank accounts, expenses |
| reconciliation | Reconciliation | import batches, bank rows, match suggestions, confirmed matches |
| communication | Communication | templates, notification requests, delivery attempts, preferences |
| integration | Integration / Sync | outbox, inbox, cursors, conflicts, webhook receipts |
| audit | Audit | append-only audit events, approval history, security events |
| customization | Customization capability | field definitions and typed values where needed |
| platform_local | Local platform adapter | node identity, signed licence snapshot, device registry projection |

A PostgreSQL schema is a namespace and ownership boundary, not a separate microservice or database server. Each module owns its writes and migrations.

## 3. Conceptual ERD — core operational model

The following diagram is logical. It intentionally omits several lookup and audit tables. Cardinalities and physical FK constraints must be reviewed before implementation.

~~~mermaid
erDiagram
    TENANT ||--o{ SCHOOL : owns
    SCHOOL ||--o{ CAMPUS : contains
    SCHOOL ||--o{ ACADEMIC_YEAR : defines
    ACADEMIC_YEAR ||--o{ TERM : contains
    CAMPUS ||--o{ SECTION : offers
    GRADE_LEVEL ||--o{ SECTION : groups

    PERSON ||--o{ USER_PERSON_LINK : authenticates_as
    PERSON ||--o| STUDENT_PROFILE : may_be
    PERSON ||--o| GUARDIAN_PROFILE : may_be
    PERSON ||--o| EMPLOYEE : may_be
    HOUSEHOLD ||--o{ HOUSEHOLD_MEMBER : contains
    PERSON ||--o{ HOUSEHOLD_MEMBER : joins
    STUDENT_PROFILE ||--o{ STUDENT_GUARDIAN : linked_to
    GUARDIAN_PROFILE ||--o{ STUDENT_GUARDIAN : guardian_for

    STUDENT_PROFILE ||--o{ ENROLLMENT : has
    ACADEMIC_YEAR ||--o{ ENROLLMENT : for
    CAMPUS ||--o{ ENROLLMENT : at
    GRADE_LEVEL ||--o{ ENROLLMENT : grade
    SECTION o|--o{ ENROLLMENT : optional_section

    STUDENT_PROFILE ||--o{ APPLICATION : applies
    APPLICATION ||--o{ APPLICATION_DOCUMENT : includes
    APPLICATION ||--o{ APPLICATION_REVIEW : reviewed_by

    CURRICULUM ||--o{ CURRICULUM_VERSION : versions
    CURRICULUM_VERSION ||--o{ CURRICULUM_SUBJECT : includes
    SUBJECT ||--o{ CURRICULUM_SUBJECT : mapped
    SUBJECT ||--o{ TOPIC : has
    TOPIC ||--o{ LEARNING_OUTCOME : defines
    ENROLLMENT ||--o{ ATTENDANCE_RECORD : receives
    ATTENDANCE_SESSION ||--o{ ATTENDANCE_RECORD : contains

    EXAM_SESSION ||--o{ ASSESSMENT : contains
    ASSESSMENT ||--o{ MARK : records
    ENROLLMENT ||--o{ MARK : receives
    GRADING_SCHEME ||--o{ GRADE_BAND : defines
    EXAM_SESSION ||--o{ RESULT_APPROVAL : requires
    ENROLLMENT ||--o{ REPORT_CARD : receives

    HOUSEHOLD ||--o{ INVOICE : billed_to
    INVOICE ||--|{ INVOICE_ITEM : contains
    PAYMENT ||--o{ PAYMENT_ALLOCATION : split_into
    INVOICE ||--o{ PAYMENT_ALLOCATION : settled_by
    PAYMENT ||--o| RECEIPT : produces
    PAYMENT ||--o{ REFUND : may_be_refunded

    CHART_ACCOUNT ||--o{ JOURNAL_LINE : posts
    JOURNAL_ENTRY ||--|{ JOURNAL_LINE : contains
    BANK_ACCOUNT ||--o{ IMPORT_BATCH : receives
    IMPORT_BATCH ||--o{ BANK_TRANSACTION : contains
    BANK_TRANSACTION ||--o{ MATCH_SUGGESTION : proposes
    BANK_TRANSACTION ||--o{ CONFIRMED_MATCH : resolved_by
    PAYMENT_ALLOCATION ||--o{ CONFIRMED_MATCH : matched_to

    USER ||--o{ USER_ROLE : assigned
    ROLE ||--o{ USER_ROLE : grants
    USER ||--o{ USER_SCOPE_ASSIGNMENT : scoped
    ROLE ||--o{ ROLE_PERMISSION : has
    PERMISSION ||--o{ ROLE_PERMISSION : included
~~~

Diagram notation note: this is a conceptual ERD; optional and polymorphic relationships are not fully representable in a single Mermaid diagram. The approved physical ERD must show every table, key, nullable relationship and ownership rule.

## 4. Key relationships and invariants

### 4.1 Tenant, school, campus and node

- Tenant is the SaaS customer organization.
- School is a school/legal entity owned by a tenant.
- Campus is a physical/operational location belonging to a school.
- Local Node is a deployment identity assigned to a site/campus or an approved shared group scope.
- A node may be replaced or deactivated; retain node history and do not reuse node IDs.
- Every operational record must have enough tenant/school/campus provenance for authorization, reporting and data exchange.
- The exact Tenant → School → Campus cardinality and node-to-campus assignment model remain open until approved through ADR.

### 4.2 People and roles

A Person is not the same thing as a User account. A Person can be a student, guardian, employee or more than one of these. A User is a credentialed identity. StudentGuardian and HouseholdMember are explicit relationship tables. Do not infer family links from surname, address or matching phone number.

### 4.3 Enrollment

Enrollment is the historical relationship between a student and an academic year/class/section/campus. Promotion and transfer create new records or controlled transition history. Never overwrite last year's class assignment.

### 4.4 Fees and finance

- Invoice has one or more invoice lines.
- Payment has zero or more allocations; unallocated amounts remain visible as advance/unapplied funds under an approved policy.
- A payment can allocate to multiple siblings' invoices when payer/guardian authorization permits it.
- A posted journal has one or more journal lines and must balance by currency and accounting scope.
- A bank transaction is not automatically a student payment.
- Reversal/refund is a new linked financial operation, not an edit/delete of the original posting.

### 4.5 Academic records

- Curriculum and grading rules are versioned.
- Marks reference the assessment and the student's enrollment.
- Published report cards are immutable snapshots with revision history.
- A corrected published result requires a reason, authority and new revision.

## 5. Key constraints

The final DDL must implement at least these rules where applicable:

| Domain | Constraint / invariant |
|---|---|
| Tenant | IDs and unique business codes are scoped to the correct tenant/school |
| Campus | Campus belongs to one school; campus code unique in its parent scope |
| Academic year | End date >= start date; one current year per approved scope |
| Section | Unique campus + year + grade + section code |
| Student | Admission/student number unique within its agreed school scope |
| Guardian links | No duplicate active student/guardian relationship |
| Enrollment | No overlapping active enrollment unless explicitly permitted |
| Attendance | Unique mark per defined daily/period session and enrollment |
| Marks | Score in allowed range; absence/exemption mutually consistent with score |
| Fee invoice | Invoice number unique within approved issuer/financial-year scope |
| Payment | Sum of active allocations <= available payment balance |
| Invoice | Allocations cannot exceed outstanding balance except approved advance/overpayment rules |
| Journal | Posted debit total = posted credit total by currency/scope |
| Journal immutability | Posted lines cannot be edited or deleted through ordinary application roles |
| Bank import | Duplicate file/row/event detection is idempotent |
| Sync | Unique event ID per originating node/stream and consumer processing scope |
| Audit | Application roles cannot update/delete existing audit rows |
| Custom field | One valid typed value per record/field; definition type must match value column |

Use explicit CHECK, UNIQUE, FOREIGN KEY and exclusion constraints where they can enforce the rule correctly. Application validation is still required for workflow and authorization logic.

## 6. Indexing baseline

The final index plan must be derived from actual queries and EXPLAIN plans. Initial candidates:

- Tenant/school/campus scoped indexes on high-volume operational tables.
- Unique indexes for school codes, campus codes, admission numbers and invoice/receipt numbers.
- Attendance: campus/date/session and enrollment/date/period.
- Enrollment: student/academic-year/status and campus/class/section/year.
- Fees: household/status/due-date, invoice/student or payer scope, payment reference and allocation foreign keys.
- Reconciliation: bank-account/statement-date, normalized reference, source row key and import batch.
- Exams: exam/assessment/enrollment and published result lookup.
- Sync: event ID, state/next-attempt, node cursor and aggregate version.
- Audit: tenant/campus/resource/time and actor/time; consider partitioning only after measured volume.

Do not add indexes to every column. Indexes add write and storage overhead. Include index rationale in migration PRs.

## 7. Tenant isolation and PostgreSQL RLS

### 7.1 Application authorization remains mandatory

- Derive tenant and scope from authenticated server-side identity/membership.
- Never accept a body/query tenantId as proof of access.
- Apply object-level authorization to reads, writes, exports, files and reports.
- Filter and authorize before pagination/count aggregation.
- A Local Node deployment still validates tenant/campus scope even if one tenant is expected.

### 7.2 RLS design proposal

RLS may provide defense in depth. Before enabling it, approve:
- Which tables are tenant-scoped.
- Which tables are platform/local-global.
- Which service roles can bypass RLS (prefer none for request-serving application role).
- How the request tenant/scope is set transaction-locally.
- How background jobs, migrations and sync workers receive validated scope.
- How connection pooling prevents context leakage.

Illustrative pattern only:

~~~sql
ALTER TABLE students.student_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE students.student_profiles FORCE ROW LEVEL SECURITY;

CREATE POLICY student_tenant_scope
ON students.student_profiles
USING (
  tenant_id = current_setting('app.tenant_id', true)::uuid
)
WITH CHECK (
  tenant_id = current_setting('app.tenant_id', true)::uuid
);
~~~

Set request context inside a transaction using transaction-local settings (for example SET LOCAL / set_config(..., true)) and clear it automatically at transaction end. Do not rely on session-scoped connection settings with a pool. The final implementation must test no tenant context leaks across pooled connections.

RLS owner/bypass rules need explicit configuration. Table owners and superusers can bypass normal RLS unless configured otherwise; production requests must use a restricted application role.

### 7.3 Required isolation tests

- Tenant A cannot read/write Tenant B records by direct ID.
- Campus A staff cannot access Campus B data without explicit scope.
- Teacher cannot access non-assigned section/student.
- Parent cannot access a sibling or unrelated student's records unless a verified guardian link grants access.
- Export, attachments, report counts, search and background jobs enforce the same scope.
- Pooled connection reused for another tenant has no stale context.
- Sync/import worker rejects wrong-tenant/wrong-campus package data.

## 8. Module-owned migration strategy

### 8.1 Ownership

Each module owns its tables and migration history. The initial deployment may package migrations in the same application release, but the owning module must remain explicit.

Recommended ownership map:
- Identity migrations: identity
- Organization migrations: organization
- Student/admission migrations: students, admissions
- Academic/attendance/exam migrations: each respective schema
- Fees/Finance/Reconciliation migrations: each respective schema
- Integration/Audit/Customization migrations: respective schema
- Cloud Control Plane migrations: separate project/database and separate release pipeline

### 8.2 Migration requirements

Every migration must:
1. Have a stable identifier and descriptive name.
2. Be forward-only in production unless a reviewed rollback is safe.
3. Include a tested backup and recovery approach for destructive changes.
4. Avoid table locks beyond the accepted maintenance window; use expand/contract for large tables.
5. Add nullable/backfill/validate/not-null in safe stages when existing rows exist.
6. Create indexes concurrently where supported and necessary; account for EF transactional migration behavior.
7. Preserve financial, audit, academic and synchronization history.
8. Include reference/seed data versioning and deterministic IDs where needed.
9. Be tested on an empty database and a representative upgraded database.
10. Record migration checksum/version and deployment result.
11. Include a documented rollback or forward-fix plan.
12. Never run schema migrations automatically from every API instance concurrently without a single controlled migration lock/runner.

### 8.3 EF Core recommendation

- Use EF Core/Npgsql compatible with the selected .NET target.
- A single controlled startup/deployment migration runner may invoke module migration assemblies in dependency order, or each module may use its own DbContext/migration assembly.
- Avoid a single giant DbContext that exposes all module tables to every feature.
- Cross-module reads/writes must use module contracts; direct writes to another module's tables are prohibited.
- A physical FK across schemas may be considered where referential integrity is essential and migration ownership is agreed. This is an open technical decision; avoid accidental coupling through undocumented FK creation.

### 8.4 Migration acceptance tests

- New database can be created from zero in a clean environment.
- Supported prior database upgrades without data loss.
- Migration failure leaves a recoverable state.
- No two hosts race to apply migrations.
- Backup restore followed by migration is tested.
- Reference data seeding is idempotent.
- Rollback/forward-fix procedure is rehearsed.

## 9. Backup, restore and database maintenance

- Define RPO/RTO per customer deployment tier; current 15-minute RPO / 4-hour RTO are proposed targets, not measured guarantees.
- Back up database and document/object storage consistently.
- Encrypt backup files and protect keys separately.
- Store at least one off-host/off-site copy where the customer agreement permits.
- Document local backup when internet is down and later off-site transfer.
- Test full restore to replacement hardware, including licence/node reactivation.
- Monitor disk growth, WAL, backup age, failed jobs, connection pool and long transactions.
- Do not expose PostgreSQL directly to the public internet.

## 10. Data exchange and cross-node imports

Campus export packages are not SQL dumps. They must include stable IDs, source node, source campus, tenant, event/package IDs, schema/module version, export watermark, record counts, checksums and signed manifest; sensitive payloads are encrypted for the intended receiver.

Imports require signature/recipient/tenant/schema verification, dry-run, duplicate detection, dependency/reference validation, approval, bounded transactional/batch processing, audit and an idempotent receipt. Imported branch data is read-only reporting replica by default; corrections use an explicit command/round-trip workflow.

## 11. Approval checklist before first migration

- [ ] Tenant/school/campus/node topology approved.
- [ ] Logical ERD reviewed by product owner and technical lead.
- [ ] Field dictionary reviewed; data classification applied.
- [ ] PK/FK/unique/check/exclusion constraints approved.
- [ ] Index plan approved against expected access patterns.
- [ ] RLS context and DB-role design tested with pooling.
- [ ] Module migration ownership approved.
- [ ] Seed/reference-data strategy approved.
- [ ] Migration lock, backup and rollback/forward-fix plan approved.
- [ ] Backup/restore acceptance tests defined.

## 12. SaaS tenant implementation companion

Use [DOC-62](62_SAAS_TENANT_IMPLEMENTATION_BLUEPRINT.md) as the end-to-end implementation companion for the v1 SaaS tenant boundary.

Physical schema and migration work must preserve these invariants:

- Cloud Organizations.Id is the canonical TenantId. Cloud OrganizationId and Local TenantId are the same UUID, not separate identity domains.
- Organization/Tenant → School → Campus is the proposed v1 hierarchy. A Campus must not reference a School in another tenant.
- A Local Node binds to one Tenant and may be assigned multiple campuses only when all assignments belong to that Tenant.
- Every tenant-owned table has a non-null tenant key directly or an enforced same-tenant parent relationship. Prefer direct tenant keys for high-volume and security-sensitive tables.
- Add composite unique keys and composite foreign keys to prevent cross-tenant references.
- Cloud runtime roles are restricted and must not own tables, be superusers or have BYPASSRLS. Schema migration credentials are separate.
- Set app.tenant_id transaction-locally inside every tenant-scoped database unit of work. Never depend on session-level state on a pooled connection.
- Missing tenant context fails closed. RLS, application authorization and resource-scope checks all remain required.

DOC-62 provides the recommended entity catalogue additions, RLS policy pattern, rollout gates and SAAS acceptance criteria. This does not replace the physical ERD approval checklist in this document. DEC-001 and DEC-004 remain open until owner approval and executable integration/security evidence are linked.
