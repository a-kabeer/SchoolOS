# 50 — Data Migration, Import/Export and Customer Exit Specification

- **Document ID:** DOC-50
- **Status:** Draft
- **Decision maturity:** Proposed migration/import/export policy; school templates pending
- **Owner:** Product owner (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ 898812f89e0ec7f03aff6c3b1408e46782814dfd; reference sources pinned in DOC-61
**Version:** v1.8 draft · **Status:** Migration contract baseline; school-specific mapping templates pending discovery  
**Applies to:** Initial school onboarding, ongoing CSV/XLSX imports, campus exchange and customer data export  
**Companion:** docs/18_CAMPUS_DATA_ACCESS_AND_DEPLOYMENT_SCENARIOS.md, docs/43_OPERATIONAL_DOMAIN_DATA_DICTIONARY.md, docs/44_POSTGRESQL_ERD_AND_MIGRATION_SPEC.md

## 1. Objectives

SchoolOS must make it practical for a school to move from Excel, desktop software or another School ERP without losing critical records or financial history.

The migration/import process must be:
- Explicitly mapped and repeatable.
- Validated before writing data.
- Idempotent for the same approved import package/batch.
- Auditable with source, actor, time, errors and row counts.
- Safe for sensitive child/guardian and financial data.
- Recoverable when a batch fails.
- Clear about imported historical facts versus new operational records.

A migration is not complete because the file upload succeeds. It is complete only after school-side reconciliation and signed acceptance.

## 2. Data migration stages

1. **Discovery:** identify source system, file types, owners and data-quality limitations.
2. **Scope approval:** select which entities/history are migrating and what is excluded.
3. **Source backup:** preserve original files/database export and record checksums.
4. **Mapping:** map source columns/codes to SchoolOS fields and reference data.
5. **Dry-run:** parse, validate, detect duplicates and produce an error report; write no live business records.
6. **Correction:** school corrects source data or approves documented exceptions.
7. **Test import:** import synthetic/anonymized sample or isolated staging copy.
8. **Reconciliation:** compare counts, fee totals, outstanding balances and sampled student histories.
9. **Production import:** approved, versioned batch with durable import log.
10. **Post-import verification:** sample, aggregate and relationship checks.
11. **Acceptance:** customer signs the migration report.
12. **Archive/retention:** source backups retained or securely deleted under approved agreement and policy.

## 3. Initial supported source formats

Initial preferred sources:
- CSV UTF-8 with documented delimiter and header row.
- XLSX workbook with named sheets and header rows.
- Approved vendor export format where its schema is documented.

Avoid arbitrary macros, password-protected workbooks without a secure transfer process, embedded executable content and raw database dumps from an untrusted source.

Before accepting a format, identify:
- Encoding and delimiter.
- Date format and timezone assumptions.
- Decimal/thousands separators.
- Whether first row is header.
- Sheet names and row counts.
- Blank rows/merged cells/formulas.
- Hidden columns/sheets.
- Source record key and duplicate strategy.

## 4. Recommended import templates

Templates below are proposed onboarding contracts. Exact required/optional columns must be tailored to the school and approved before import.

### 4.1 Students

| SchoolOS field | Example source column | Required | Validation |
|---|---|---:|---|
| admissionNumber | Admission No | Yes | Unique in school scope |
| firstName | First Name | Yes | Non-empty |
| middleName | Middle Name | No | Text |
| lastName | Last Name | School policy | Text |
| dateOfBirth | DOB | No/conditional | Parseable date, not future unless approved |
| genderCode | Gender | Optional | Map to approved code list |
| admissionDate | Admission Date | Required for admitted student | Valid date |
| campusCode | Campus | Required if multi-campus | Must map to active campus |
| gradeCode | Class/Grade | Required for current enrollment | Must map to approved grade |
| sectionCode | Section | Conditional | Required only where sections are configured |
| academicYearCode | Session/Year | Required | Must map to known academic year |
| studentStatus | Status | Required | Map to approved state |

### 4.2 Guardians and household relationships

| Field | Required | Validation |
|---|---:|---|
| guardianSourceKey | Recommended | Stable source identity |
| guardianName | Yes | Person name |
| phone | At least one verified contact route per policy | Normalize country code |
| email | No | Valid normalized email if supplied |
| relationship | Yes | Map to approved relationship code |
| studentAdmissionNumber | Yes | Must match imported student |
| isPrimaryContact | No | Boolean |
| receivesAcademicNotices | No | Boolean |
| receivesFinancialNotices | No | Boolean |
| householdSourceKey | Recommended | Groups verified siblings |
| legalCustody / collectionAuthority | Conditional | Explicitly sourced and approved; never inferred |

A shared phone number does not automatically prove all students are siblings or that the contact has legal custody.

### 4.3 Staff

Include source key, staff number, person name, contact, designation, campus, joining date, employment status and assigned role/scope only when verified. Do not import user passwords from an old software system; users must establish new credentials.

### 4.4 Academic master data

Include academic years, grade levels, sections, subjects, board/curriculum references and teacher allocations as separate mapped tables. Validate campus/year/grade/section and board relationships before student enrollment import.

### 4.5 Fees and outstanding balances

Possible import datasets:
- Fee heads.
- Effective fee structures.
- Student fee assignments.
- Open invoice balances / opening receivables.
- Unallocated credits/advances.
- Historical payments/receipts where source quality and legal retention support them.

**Financial migration rule:** Do not silently invent a detailed invoice/payment history from a single opening balance. Choose one approved method:
1. Full transaction history import with source IDs and reconciliation; or
2. Opening balance conversion with cut-off date, opening balance account and signed school approval.

The chosen method must preserve source provenance and explain how old receipts, arrears, concessions and refunds are represented.

### 4.6 Attendance, marks and academic history

Import history only when source data, definitions and approvals are clear. Each imported mark/result must identify academic year, exam/assessment, max marks, obtained marks, absence/exemption state and grading scheme/source version. Historical records must not be displayed as SchoolOS-created/verified assessments if they were imported from another system.

## 5. Reference-data mapping

Prepare mapping tables for:
- Campus codes.
- Academic years and terms.
- Grade/class names.
- Section names.
- Board/curriculum and subjects.
- Attendance codes.
- Fee heads and payment channels.
- Staff designations and roles.
- Student/guardian relationship types.
- Gender/status/other school-defined code lists.

Never create a new master value silently because a source value did not match. Present unmapped values for school review.

## 6. Duplicate detection and identity matching

Use layered matching:
1. Explicit source system ID (best, if stable).
2. Admission/student number within the correct school scope.
3. Verified national identifier only if lawful, necessary and protected.
4. Person name + DOB + guardian/contact as a possible-match signal, not automatic proof.

Rules:
- Do not merge records only by name or phone.
- Do not use national identifiers as public keys.
- Store source-system ID and import batch reference for traceability.
- Possible matches enter a review queue.
- Record merge decisions with before/after references and approver.
- Re-run of an identical import must not create duplicates.

## 7. Validation categories

### 7.1 File-level errors
- Unsupported extension/content type.
- File too large.
- Encoding failure.
- Missing required sheet/header.
- Invalid workbook structure.
- Malware scan failure where scanning is enabled.

### 7.2 Row-level errors
- Missing mandatory value.
- Invalid date/number.
- Unknown campus/class/section.
- Duplicate business key.
- Broken guardian/student relationship.
- Negative amount where prohibited.
- Marks exceed maximum.
- Invalid attendance/status code.
- Invalid date range.
- Invalid academic-year reference.

### 7.3 Cross-row/business errors
- Same admission number repeated.
- Student assigned to inactive campus.
- Section does not belong to selected grade/year/campus.
- Guardian link refers to missing student.
- Household links unrelated or ambiguous people without approval.
- Opening balance totals do not match source report.
- Invoice/payment/receipt references inconsistent.
- Duplicate bank transaction/payment source reference.

## 8. Dry-run report format

Every dry-run returns:
- Import batch ID and template/version.
- Source file name/checksum.
- Importing user and timestamp.
- Source sheet and row count.
- Valid rows.
- Rows with warnings.
- Rejected rows.
- Potential duplicates.
- Unmapped reference values.
- Creates/updates/skips summary.
- Financial totals by currency and period where relevant.
- Relationship integrity result.
- Sampled warning/error details with source row number.
- Required actions before approval.

A dry-run never mutates production business data.

## 9. Import execution contract

1. Authorized operator selects a validated dry-run batch.
2. School confirms mapping and approved exceptions.
3. An authorized approver signs off where policy requires.
4. The import worker obtains a durable batch lock.
5. Records are processed in dependency order (reference/master data before dependent records).
6. Each source record stores source system, source key, import batch and imported-at metadata.
7. The importer uses deterministic/idempotent keys so retry cannot duplicate data.
8. Failure produces a durable row/error log and safe resume/rollback plan.
9. Finance batches use special reconciliation/approval gates; do not mix unreviewed opening balances with ordinary posted receipts.
10. Batch totals and relationship checks are recalculated after import.
11. The system issues a final import report for customer sign-off.

## 10. Financial migration reconciliation

Before a finance migration is accepted, compare:
- Opening cash/bank balances.
- Total accounts receivable/open student balances by campus/class/fee head.
- Total outstanding invoice balances.
- Unallocated/advance payments.
- Concessions and waivers.
- Refunds and reversed payments.
- Source statement/import period totals.
- SchoolOS control-account totals after import.

Every difference must be explained, corrected or explicitly accepted in writing. Never force balances to match by directly editing derived totals.

## 11. Rollback and recovery

- Dry-run is always reversible because it does not write operational records.
- Production import batch uses a transaction/batch journal and idempotency key.
- For non-financial incomplete batches, use an approved rollback or compensating cleanup that does not violate foreign keys/history.
- For posted finance records, use reversal/adjustment entries; do not delete posted records to roll back.
- For partially imported large batches, use a deterministic checkpoint and resume strategy.
- Retain batch logs and source references according to policy.
- Provide an operator-readable recovery report.

## 12. Export and portability

Customers must be able to export their authorized data in documented portable formats.

Recommended exports:
- Students and enrollment history.
- Guardians/households and relationship permissions.
- Staff and role/scope assignments, excluding credentials.
- Academic master data and result history.
- Attendance history.
- Fee invoices, invoice lines, payments, allocations, receipts, concessions and refunds.
- Finance journals, journal lines and approved chart of accounts.
- Bank import batches, transactions and confirmed matches.
- Documents/attachments where the customer has authorization.
- Audit history where legally/operationally appropriate.
- Custom field definitions and values, with schema version.
- Import/export manifests and checksums.

Exports must:
- Apply the same permission scope as the UI/API.
- Clearly identify date range, timezone, campus and academic year.
- Include a data dictionary and schema version.
- Preserve IDs/relationships so another system can reconstruct associations.
- Include private attachments in a secure archive or controlled download set where approved.
- Avoid including passwords, private keys, session tokens or provider secrets.
- Be encrypted for large/sensitive transfer and expire under policy.
- Record requester, approver, purpose, scope and time.

## 13. Campus Data Exchange package

For manual disconnected-campus exchange, package is not an arbitrary CSV folder or database dump.

Required manifest fields:
- Package ID.
- Tenant/school/campus/source node.
- Intended receiving node.
- Schema and module versions.
- Export type (incremental/full snapshot/date range).
- Export watermark/cursor.
- Generated-at UTC.
- Record counts by domain.
- File list, checksums and sizes.
- Event IDs and dependency references.
- Signing key ID and signature.
- Recipient encryption key ID.
- Import compatibility requirements.

Import workflow: verify signature/recipient → verify tenant/scope → compatibility check → checksum → duplicate check → dry-run → review → approve → apply → acknowledge. Imported branch-owned records are read-only at head office by default.

## 14. Privacy and data handling

- Use anonymized samples for mapping and testing where practical.
- Transfer sensitive files through an approved encrypted channel.
- Restrict access to source files to named migration operators.
- Never email unencrypted child/guardian/financial exports.
- Remove temporary copies after approved retention period.
- Log access to sensitive exports.
- Apply Pakistan-specific legal/privacy review before production migration.
- Obtain school authority and guardian-related approvals where required by policy.

## 15. Customer migration acceptance form

The school signs that:
- Source scope/date range was agreed.
- Record counts were reviewed.
- Student/guardian relationships were sampled.
- Active enrollments and class/section mapping were checked.
- Fee opening balances and payment totals were reconciled.
- Historical marks/attendance scope and limitations were explained.
- Known source-data defects were documented.
- Exceptions and exclusions were accepted.
- Final import batch ID, timestamp and report were retained.

## 16. Open decisions

- Initial templates and exact mandatory fields by source type.
- Whether legacy passwords are always discarded (recommended) or any migration exception exists.
- Historical attendance and result migration depth.
- Opening-balance versus full transaction import per pilot.
- National identifier data classification, encryption and retention.
- File/object storage provider and malware scanning solution.
- Export archive encryption and expiry method.
- Customer data export window after cancellation.
- Manual exchange package size limit and removable-media policy.


## 17. Legacy ERP discovery and adapter rules

The eSchool reverse-engineering work is a reference for the depth of source-system discovery that may be needed. It is not permission to copy legacy application code or to assume that observed screens reveal all server-side business rules.

For each legacy ERP or school database, the migration discovery record should include:

- Customer authorization, source owner, permitted access method and extraction date.
- Observed modules, roles, workflows, report outputs and user-facing state transitions.
- Source tables/entities, primary/business keys, relationships, constraints and indexes where legitimately available.
- API/endpoint inventory and request/response shapes where authorized and documented.
- Confirmed facts, inferred behavior, unknown backend logic and gaps requiring customer/vendor clarification.
- Source technology/version, encoding, timezone, date/currency formats and known data-quality limitations.
- Mapping version, source record key, target SchoolOS entity, transformation, validation, exception and reconciliation rule.

Prefer a documented export/API or customer-authorized database extract. Use an adapter/transform layer to map source data into the approved SchoolOS import contract; do not couple the operational domain model to legacy table names or endpoint behavior.

Never place source credentials, real student/guardian data, vendor-owned code, private screenshots or raw production database dumps in this repository. Keep source extracts in an approved restricted migration workspace and use synthetic/anonymized fixtures for tests.

The existing dry-run, idempotency, financial reconciliation and customer acceptance requirements in sections 8–15 remain mandatory. DOC-61 records the eSchool source evidence and the limit on what is adopted.
