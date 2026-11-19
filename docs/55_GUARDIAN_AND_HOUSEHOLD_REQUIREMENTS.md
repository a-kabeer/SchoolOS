# Guardian and Household Requirements

- **Document ID:** PRD-STU-REL-001
- **Status:** Current
- **Decision maturity:** Confirmed relationship-cardinality requirement; detailed authority rules open
- **Owner:** Product owner (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `91cd4355e68d1b37e1943aeae1db58b4ff34c53f` (documentation refactor base)

## 1. Requirements

| ID | Requirement | Source evidence | Status |
|---|---|---|---|
| STU-REQ-001 | Maintain student identity and enrollment history without unnecessary duplicate people/records | `docs/01_PRODUCT_REQUIREMENTS.md`, Admissions and students section | Source requirement retained |
| STU-REQ-002 | One student may be associated with multiple guardian records | `docs/01_PRODUCT_REQUIREMENTS.md`, explicitly says “Multiple guardians per student” | Confirmed cardinality requirement |
| STU-REQ-003 | One guardian and/or household may relate to multiple students/siblings in the same family | `docs/01_PRODUCT_REQUIREMENTS.md`; `docs/02_MODULES_AND_WORKFLOWS.md` one-payment-for-multiple-siblings workflow | Confirmed cardinality requirement |
| STU-REQ-004 | Relationship type, primary contact, authority and communication scope must be represented according to approved rules | Required to implement the above safely; exact rules are not specified by source | Open design requirement; do not assume defaults |

## 2. Cardinality rule

The product MUST support a many-to-many relationship between student records and guardian records. A household/family grouping may associate multiple students/siblings and guardians. The exact entity structure (for example, an explicit Household entity or a family grouping abstraction) remains a design choice until reviewed against data ownership, privacy and school workflows.

The data model MUST NOT assume:

- exactly one guardian per student;
- exactly one student per guardian;
- that every guardian automatically has equal authority;
- that a household payer can view every child's records;
- that communications to one guardian automatically authorize disclosure to every other guardian.

## 3. Open decisions — no defaults approved

1. Which relationship types are supported (parent, legal guardian, emergency contact, authorized pickup, payer, other)?
2. Can a student have guardians from more than one household, and can a guardian belong to more than one household?
3. How are primary contact and preferred language/channel selected when there are multiple guardians?
4. Which guardian may view attendance, marks, health/safeguarding data, fee statements and notices?
5. How are custody restrictions, court orders, consent withdrawal and emergency contacts recorded and enforced?
6. Can a guardian make payments across siblings without receiving access to all sibling academic or personal data?
7. What happens to guardian/household links when a student transfers, withdraws or ages out?

## 4. Acceptance criteria for downstream implementation

- A student can be linked to at least two guardians without duplicate student records.
- A guardian can be linked to at least two student/sibling records.
- The UI displays each relationship distinctly and does not overwrite another guardian link.
- Authorization tests prove that payment/payer access does not automatically grant unrelated academic or personal-data access.
- Relationship updates and authority changes have an audit trail.
- Bulk import/export preserves stable identities and relationship provenance.

**Test IDs:** Suggested traceability IDs are listed in `../registers/requirement-traceability.csv`; test names are proposals until the QA test catalogue is approved.

## 5. Task linkage gap

The restored work-package plans in `docs/task-plans/` contain no specific student/guardian relationship implementation task. This requirement is therefore marked `Unassigned — new student/admissions work package required` in the traceability register. Do not attach it to an unrelated BCP task simply to make the mapping appear complete.


## 6. Canonical model and authorization references

- The proposed logical fields for `students.households`, `students.guardian_profiles`, `students.student_guardians` and `students.household_members` are described in [43 — Operational Domain Data Dictionary](43_OPERATIONAL_DOMAIN_DATA_DICTIONARY.md).
- Parent/guardian access is based on verified relationships and scoped permissions, as described in [46 — Authorization and Tenant Isolation Matrix](46_AUTHORIZATION_AND_TENANT_ISOLATION_MATRIX.md).
- This document is the focused requirement source for guardian/household cardinality and unresolved family-authority rules. It does not approve custody, consent, fee-payer or communication policy.
