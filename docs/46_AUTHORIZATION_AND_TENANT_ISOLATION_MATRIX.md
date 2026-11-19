# 46 — Authorization, Role and Tenant-Isolation Matrix

- **Document ID:** DOC-46
- **Status:** Draft
- **Decision maturity:** Proposed authorization matrix; owner sign-off required
- **Owner:** Security lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)
**Version:** v1.8 draft · **Status:** Proposed authorization baseline; owner sign-off required  
**Companion:** docs/07_SECURITY_AND_QUALITY_REQUIREMENTS.md, docs/38_PLATFORM_SECURITY_REQUIREMENTS.md, docs/43_OPERATIONAL_DOMAIN_DATA_DICTIONARY.md

## 1. Core authorization model

SchoolOS must use **permission + scope + resource relationship + state** for every protected operation.

A role is a named bundle of permissions. A scope limits where those permissions apply. A resource relationship may further restrict access (for example, a parent sees only verified linked children). A state rule decides whether an operation is still allowed (for example, a posted journal cannot be edited).

### 1.1 Scope hierarchy

| Scope | Example | Access meaning |
|---|---|---|
| Platform | SchoolOS company platform | Internal Cloud Control Plane operation only |
| Tenant / Organization | A school group/customer | Organization-owned data |
| School / Legal Entity | A registered school entity | One legal school within a tenant |
| Campus | Campus A | Records assigned to a campus |
| Academic year | 2026–27 | Records for a specific school year |
| Class / Grade | Class 6 | Grade/class records |
| Section | 6-A | One section |
| Assigned resource | Assigned students, subjects, tickets, cases | Only explicitly linked records |
| Self / Household relationship | Own profile or verified child | Personal/guardian relationship access |

Scope is additive only when expressly granted. A CampusPrincipal does not automatically inherit access to every campus. A Teacher does not gain access to all students in a grade just because they teach one section.

## 2. Separate security audiences

The following are separate trust boundaries:

1. Public anonymous visitor.
2. Ephemeral demo user.
3. Local school staff/user.
4. Parent/guardian and student portal user.
5. Customer organization portal user.
6. Internal SchoolOS staff / Super Admin.
7. Time-limited customer-approved support operator.
8. Local Node service identity / Sync Hub node identity.

Do not reuse a browser cookie, access token or authorization audience across these boundaries. Demo sessions cannot be promoted to customer/admin sessions. A customer portal identity cannot call internal Super Admin endpoints.

## 3. Role catalogue — Local Data Plane

| Role | Typical scope | Purpose |
|---|---|---|
| OrganizationOwner | Tenant / approved schools | Organization-wide oversight and user/role delegation |
| Headmaster | Organization or selected schools/campuses | Academic and operational oversight; consolidated reporting |
| CampusPrincipal | One or more assigned campuses | Campus operations and approvals within assigned scope |
| AdmissionsOfficer | Assigned campus/school | Inquiry, application, document and admission workflow |
| Accountant | Assigned campus/school/finance books | Fee ledger, bank import, payment and finance review |
| Cashier | Assigned cash office/campus | Receive authorized cash/bank evidence, issue provisional/confirmed receipt per policy |
| FinanceController | Approved finance scope | Approve adjustments, refunds, journals and period close |
| Teacher | Assigned classes/sections/subjects | Attendance, lesson/homework, assigned student and marks drafts |
| ExamController | Approved exam/campus/year | Exam setup, moderation, result approval/publication |
| HRManager | Assigned school/campus HR scope | Staff records, leave and payroll preparation |
| ParentGuardian | Verified linked children | Child-specific notices, statements and approved requests |
| Student | Own student record | Own timetable, notices, homework and published result |
| LocalSystemAdmin | One Local Node | Local configuration, user/device/node operations; no implicit finance approval |

These are starter roles. Schools may configure custom roles only from approved permission keys and cannot grant platform-internal permissions.

## 4. Role × module permission matrix

Legend:
- **R** = read within assigned scope.
- **W** = create/update normal workflow records.
- **A** = approve/authorize a controlled transition.
- **D** = dashboard/report read.
- **Own** = own record or verified relationship only.
- **—** = no default access.
- **Scope** = always limited by explicit tenant/school/campus/class/resource assignment.

| Module / action | Owner | Headmaster | Principal | Admissions | Accountant | Cashier | Finance Ctrl | Teacher | Exam Ctrl | HR | Parent | Student |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Organization setup | R/W | R | R/W campus settings | — | — | — | — | — | — | — | — | — |
| User/role delegation | A/W | A within delegated scope | W within campus | — | — | — | — | — | — | — | — | — |
| Admissions inquiries | R/D | R/D | R/D | R/W | — | — | — | — | — | — | Own request | — |
| Applications/documents | R | R | R/A | R/W/A per policy | — | — | — | Limited assigned review | — | — | Submit own/child |
| Student profile | R | R | R/W | R/W limited | Limited billing fields | — | — | R assigned students | R assigned exam students | — | Own | Own |
| Guardian/family links | R | R | R/A | W with verification | Billing relationship R | — | — | Minimum contact R | — | Own verified links | Own verified links |
| Enrollment/promotion | A | A | A/W campus | W request | — | — | — | — | — | — | Own child status R | Own status R |
| Fee setup | R | R | R/W if delegated | — | R/W | — | A | — | — | — | — | — |
| Invoice/challan | R | R | R | — | R/W | R | R/A where policy | — | — | — | Own child R | Own R if enabled |
| Cash collection | R | R | R | — | R | W | A policy | — | — | — | Own receipt R | Own receipt R |
| Payment allocation | R | R | R | — | W | Limited W | A exception | — | — | — | Own allocation R | Own R |
| Concession/waiver | R | A limited | W request/A limited | — | W request | — | A | — | — | — | Own request | — |
| Refund/reversal | R | Request | Request | — | W request | — | A | — | — | — | Own status R | — |
| Journal posting | R | D limited | D campus | — | W draft | — | A/W post | — | — | — | — | — |
| Bank statement import | R | D | D campus | — | W | — | A session close | — | — | — | — | — |
| Bank match confirmation | R | D | D campus | — | W with limits | — | A exception | — | — | — | — | — |
| Attendance | D | R/D | R/D/W corrections | — | — | — | — | W assigned classes | R | W staff attendance if assigned | Own child R | Own R |
| Lesson/homework | D | R/D | R | — | — | — | — | W assigned classes | R | — | Own child R | Own R/W submission |
| Exam setup | R | A | A campus | — | — | — | — | R assigned | R/W | — | — | — |
| Marks entry | R | R | R | — | — | — | — | W assigned drafts | R/W | — | — | — |
| Marks moderation | R | A per policy | A campus | — | — | — | — | — | W/A | — | — | — |
| Result publication | R | A | A only if delegated | — | — | — | — | — | W/A | — | Published own child R | Published own R |
| HR/leave | R | R | R campus | — | — | — | — | Own leave | — | W/A | — | — |
| Payroll | D limited | D limited | — | — | R | — | A/W | Own payslip R | — | W | — | — |
| Communication templates | R | R | R/W campus | W admission templates | W fee templates | — | A sensitive notices | W class notices | W exam notices | W HR notices | Preference R/W own | Preference R/W own |
| Audit logs | R authorized summary | R scope-limited | R campus | Own workflow history | Finance audit R | Own transaction audit | Finance audit R/W no delete | Own action history | Exam audit | HR audit | Own access history | Own access history |
| Exports | A org scope | R/A delegated | R campus | Limited admissions | Finance scoped | Receipt only | Finance scoped | Assigned class only | Exam scope | HR scope | Own linked child only | Own record only |

This table is an initial proposal, not a blanket permission grant. Product owner and technical lead must review each permission key and scope before implementation.

## 5. Separation of duties

The following maker/checker rules are mandatory unless an approved low-risk exception is documented:

- Cashier records collection; Accountant or FinanceController reviews exceptions/allocations according to school policy.
- A user cannot approve their own refund, waiver or high-value concession request.
- Journal preparer and journal approver must be separate for controlled accounts/thresholds.
- Bank match suggestions are not final until authorized confirmation.
- Marks entry, moderation and final result publication have distinct permission/state boundaries.
- Licence issuance/revocation and payment verification require privileged platform roles and audit.
- Support staff cannot self-approve access to customer operational records.
- Role changes and privilege escalation require reason, actor, scope, effective time and audit.

Approval thresholds and whether small schools may combine roles remain a product decision; the system must support configuration without bypassing audit.

## 6. Parent / guardian relationship access

Parent access is derived from verified StudentGuardian and household links, not from a user-submitted student ID.

The access service must separately evaluate:
- Can view academic notices?
- Can view fee statements?
- Can pay or request a payment allocation?
- Can update contact details?
- Can submit leave/absence information?
- Has legal custody or collection authority?
- Is the primary communication recipient?

A person can be an emergency contact without being entitled to fees/marks. A payer is not automatically the legal guardian. Relationship permissions need explicit policy and effective dates.

## 7. Platform roles — Cloud Control Plane

| Role | Scope | Key capabilities |
|---|---|---|
| PlatformSuperAdmin | Entire SchoolOS platform | Platform configuration and exceptional approvals; MFA mandatory |
| PlatformOperations | Nodes/releases/health | Node health, release rollout, diagnostics; no default access to school student records |
| PlatformFinance | Commercial invoices/payments | Verify commercial payments, billing corrections and credit notes |
| LicenseOperator | Licence issue/renew/revoke | Signed lease lifecycle through protected signing service; dual control for sensitive actions |
| SalesAgent | Leads/demo/appointments | CRM and sales workflow; no campus operational records |
| CustomerSupport | Tickets/help center | Customer-visible support workflow; no default operational data access |
| CMSPublisher | Public website content | Draft/review/publish public content; no customer operations |
| SecurityAuditor | Platform security/audit | Read security/audit evidence only; no routine mutation |

The signing private key is held by a protected signing service/KMS/HSM boundary where feasible; no human role retrieves or exports it.

## 8. Customer-approved support access

Support access is not a permanent role. It is a time-limited grant with:
- Customer approver and named support agent.
- Purpose and ticket reference.
- Node/campus scope.
- Start and expiry time.
- Default read-only permission.
- Explicit elevated access only when justified and approved.
- Visible active-session banner/notification where appropriate.
- Complete access and action audit.
- Automatic expiry and revocation.
- Break-glass process that is separately approved, time-limited and post-reviewed.

No generic permanent remote shell or direct public database access by default.

## 9. Authorization evaluation order

Every request must follow this order:

1. Authenticate the correct audience/session.
2. Resolve active user and tenant membership from server-side identity.
3. Resolve role and effective permission assignments.
4. Resolve permitted school/campus/class/resource scope.
5. Load resource through a scope-aware query.
6. Verify relationship and workflow state.
7. Apply business rule and separation-of-duties checks.
8. Execute transaction and record audit.
9. Return only fields the caller may see.

Do not fetch an unrestricted object by ID and then trust the frontend to hide fields.

## 10. Restricted Mode authorization overlay

When a Local Node enters licence Restricted Mode after the approved grace boundary:

Allowed categories (subject to role):
- Historical read-only viewing.
- Reports and authorized data export.
- Backup/restore administration.
- Billing/licence status and renewal.
- Support and diagnostics.
- Manual signed licence import and validation.

Blocked categories:
- New admissions/enrollments.
- New attendance writes/corrections unless explicitly allowed by final matrix.
- Fee collection and new receipt creation.
- New marks and result publication.
- New financial postings, refunds and waivers.
- Other new operational writes.

The exact operation-by-operation list is open and must be approved before release. Do not infer that all HTTP GET endpoints are safe or all POST endpoints are writes; classify each use case.

## 11. Security implementation requirements

- Default deny.
- Resource-based authorization on all sensitive endpoints.
- Server-derived tenant and scope.
- PostgreSQL RLS as defense in depth where selected.
- Non-owner restricted database role for normal requests.
- MFA for privileged Cloud Control Plane roles.
- Step-up authentication for high-impact actions.
- Session/device revocation and refresh rotation.
- Audit protected changes, sensitive reads and exports.
- Rate-limit login, registration, demo, uploads and webhooks.
- Mask child/national-ID/financial data in grids and logs.
- Do not expose credentials, licence secrets or support tokens.

## 12. Required authorization tests

- Cross-tenant IDOR attempts return no data.
- Cross-campus access denied by API and database safeguards.
- Teacher assigned to 6-A cannot access 6-B.
- Parent linked to Student A cannot access Student B by changing route ID.
- Emergency contact without financial permission cannot read fee ledger.
- Cashier cannot self-approve restricted refund/waiver.
- Finance user cannot approve own high-value journal where maker/checker is required.
- Expired support grant stops access immediately.
- Revoked device cannot submit queued events.
- Demo token cannot call customer/admin endpoint.
- Customer portal user cannot access another organization's invoice/node/license.
- Super Admin operations require MFA and audited reason.
- Restricted Mode blocks each explicitly prohibited mutation while allowing approved read/export/backup/renewal operations.

## 13. Approval checklist

- [ ] Role catalogue approved.
- [ ] Permission-key naming convention approved.
- [ ] Resource-scope hierarchy approved.
- [ ] Parent/guardian relationship policy approved.
- [ ] Maker/checker and approval thresholds approved.
- [ ] Platform role separation approved.
- [ ] Support-access grant lifecycle approved.
- [ ] Restricted Mode operation matrix approved.
- [ ] RLS/database-role strategy approved.
- [ ] Authorization test suite approved.


## 14. Guardian and household requirement companion

Use [55 — Guardian and Household Requirements](55_GUARDIAN_AND_HOUSEHOLD_REQUIREMENTS.md) together with the guardian/family-link permissions in this matrix. Access is derived from verified relationships plus explicit permission and scope; payer, emergency-contact or household membership alone must not be treated as blanket access.


## 9. Module entitlement is separate from user authorization

A module being visible or included in a menu is not an authorization decision. SchoolOS must evaluate distinct gates for a protected capability:

1. The module is available and compatible on the Local Node.
2. The organization's subscription entitlement permits the module.
3. The signed, node-bound local licence/lease permits the module under the approved lease policy.
4. The authenticated user has the required permission.
5. The user has the correct tenant/school/campus/class/resource relationship.
6. The record state and Restricted Mode policy allow the requested operation.

The Cloud Control Plane owns the commercial catalogue and subscription state. The Local Data Plane validates its signed entitlement snapshot/lease locally; routine local requests must not require a live Cloud call. User permissions and relationship scope remain independently enforced while offline.

A module entitlement cannot grant a user privileges. A user role cannot enable an unlicensed module. Hiding a module in the frontend is not enforcement: APIs and application services must reject unauthorized operations. Disabling an entitlement must preserve the customer's data and historical records.

The module catalogue fields, core/paid split, dependency semantics, compatibility and signed lease claims remain subject to DEC-020 in DOC-60. The underlying tenant/school/campus cardinality and RLS/DB-role implementation remain subject to DEC-001 and DEC-004; this section does not close those decisions. See DOC-61.
