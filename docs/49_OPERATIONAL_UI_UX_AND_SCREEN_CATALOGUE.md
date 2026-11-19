# 49 — SchoolOS Operational UI/UX and Screen Catalogue

- **Document ID:** DOC-49
- **Status:** Draft
- **Decision maturity:** Proposed screen catalogue; design approval pending
- **Owner:** Product owner (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)
**Version:** v1.8 draft · **Status:** Screen inventory and interaction baseline; wireframes/design system approval pending  
**Applies to:** Local Data Plane staff, teacher, parent and student experiences  
**Companion:** docs/01_PRODUCT_REQUIREMENTS.md, docs/02_MODULES_AND_WORKFLOWS.md, docs/33_CUSTOMER_PORTAL_SPECIFICATION.md, docs/34_SUPER_ADMIN_CONSOLE_SPECIFICATION.md

## 1. UX goals

SchoolOS is intended for daily school operations in Pakistan, including users with varied digital experience, low-cost Android devices and intermittent internet access.

The interface must prioritize:
1. Fast and accurate fee, admission, attendance and examination workflows.
2. Clear distinction between provisional, pending, confirmed, failed and reversed operations.
3. Permission-aware campus/class context; the user must know which campus/scope they are acting in.
4. Responsive use on desktop, tablet and mobile.
5. English and Urdu support with right-to-left behavior for Urdu.
6. Accessible forms, tables, errors, keyboard navigation and readable contrast.
7. Low bandwidth and predictable behavior when Cloud Control Plane is unavailable.
8. Safe confirmation for financial, academic and destructive actions.

## 2. Main application shells

### 2.1 Local school application

Served by the Local Node on the school network:
- Staff/Admin Shell.
- Teacher Workspace.
- Accountant/Cashier Workspace.
- Parent/Student views only where the approved deployment provides a secure local or remote access route.
- Local node status and sync/license banner.

### 2.2 Cloud Control Plane

Separate audiences and layouts are specified in docs 30–35:
- Public Website/CMS.
- Ephemeral Demo.
- Customer Portal.
- Super Admin Console.
- Support Center.

Do not combine Super Admin navigation with Local school roles in one menu or reuse its auth session.

## 3. Global navigation proposal

The Local school shell uses role-filtered modules. A user sees only modules/actions authorized for their scope.

| Navigation group | Screens |
|---|---|
| Home | Role dashboard, notifications, tasks, quick actions |
| Organization | School profile, campuses, academic years, terms, grade levels, sections, holidays |
| Admissions | Inquiry list, application list, application detail, review queue, admission reports |
| Students & Families | Student list/detail, guardian links, households, enrollment history, transfers, documents |
| Academics | Boards, curricula, subjects, topics, outcomes, class-subject allocation, lesson plans, homework |
| Attendance | Attendance sessions, roster marking, attendance history, staff attendance, leave, correction requests |
| Examinations | Exam sessions, assessments, question bank, paper review, marks entry, moderation, result approval, report cards |
| Fees | Fee heads, fee structures, assignments, invoices, challans, payments, allocations, receipts, concessions, refunds |
| Finance | Chart of accounts, journals, expenses, cashbook, bank accounts, period close |
| Reconciliation | Import batches, bank transactions, match suggestions, unmatched queue, review/close |
| Communication | Templates, announcements, message history, delivery status, preferences |
| Reports | Campus dashboards, class/student/fee/attendance/exam reports, authorized exports |
| Administration | Users, roles, campus scope, devices, audit, local settings |
| Node & Sync | Node health, sync queue, conflicts, last sync, data exchange, license status |
| Help | User guides, contact support, build/version information |

## 4. Role-specific home dashboards

### OrganizationOwner / Headmaster
- Campus selector and All Campuses option where authorized.
- Student count and admission trend.
- Fee billed/collected/outstanding.
- Attendance summary.
- Exam/result completion.
- Campus comparison with freshness timestamp.
- Alerts and approval queue.

### CampusPrincipal
- Assigned campus only.
- Today's attendance and absent summary.
- Pending admissions and approvals.
- Fee collection/outstanding summary if permitted.
- Examination/teacher workflow status.
- Campus notices and operational alerts.

### Accountant / FinanceController
- Today's collection.
- Cash/bank channel split.
- Unallocated payments.
- Failed/reversed/provisional receipts.
- Unmatched statement transactions.
- Pending waivers/refunds/journals.
- Daily cash close and reconciliation status.

### Cashier
- Payment lookup.
- Collect payment.
- Allocation selection within permission.
- Print/share receipt.
- Provisional/pending sync status.
- Cash drawer/session close.

### Teacher
- Assigned classes/sections.
- Today's timetable.
- Attendance to complete.
- Lesson/homework tasks.
- Marks entry deadlines.
- Pending sync/conflict queue.

### Parent/Guardian
- Verified linked children selector.
- Attendance and notices.
- Timetable/homework.
- Published results only.
- Fee statement and receipts according to relationship permissions.
- Notification preferences and approved requests.

A guardian should not see a child merely because they know the admission number. The backend relationship check is required.

## 5. Critical screen inventory

| Screen ID | Screen | Primary user | Key actions |
|---|---|---|---|
| UI-ORG-001 | Organization overview | Owner | View school/campus profile |
| UI-ORG-002 | Campus selector | Owner/Headmaster | Select authorized campus or All |
| UI-ORG-003 | Academic year setup | Principal/Admin | Create/activate/close year |
| UI-ADM-001 | Inquiry register | Admissions | Search/filter/create inquiry |
| UI-ADM-002 | Application detail | Admissions | Documents, review, decision |
| UI-STU-001 | Student list | Authorized staff | Search/filter/export authorized rows |
| UI-STU-002 | Student profile | Authorized staff | Tabs: personal, family, enrollment, attendance, academic, fee |
| UI-STU-003 | Family/guardian manager | Admissions/Admin | Verify links and relationship permissions |
| UI-STU-004 | Enrollment history | Principal/Admin | Promote/transfer/withdraw with history |
| UI-ATT-001 | Attendance roster | Teacher | Present/absent/late/excused and submit |
| UI-ATT-002 | Attendance correction | Teacher/Principal | Request/approve correction with reason |
| UI-EXM-001 | Exam setup | ExamController | Create exam and assessments |
| UI-EXM-002 | Marks entry grid | Teacher/ExamController | Draft, save, validate, submit |
| UI-EXM-003 | Moderation/approval | ExamController | Review and approve |
| UI-EXM-004 | Result publication | Authorized approver | Preview, publish snapshot, revision |
| UI-FEE-001 | Fee setup | Accountant | Fee heads and structures |
| UI-FEE-002 | Invoice generation | Accountant | Preview, exceptions, generate |
| UI-FEE-003 | Challan detail | Accountant/Parent | View/print/share payment voucher |
| UI-FEE-004 | Collect payment | Cashier | Record payment and allocate |
| UI-FEE-005 | Household ledger | Accountant | Sibling balances and payment allocation |
| UI-FEE-006 | Concession/refund workflow | Finance | Request, approve, reverse |
| UI-FIN-001 | Journal entry | Accountant | Draft journal |
| UI-FIN-002 | Journal approval | FinanceController | Validate and post |
| UI-REC-001 | Statement import | Accountant | Upload, map columns, preview |
| UI-REC-002 | Match review | Accountant | Accept/reject suggestions |
| UI-REC-003 | Reconciliation close | FinanceController | Review totals/exceptions, close |
| UI-NODE-001 | Local node status | Admin | Health, storage, backup, license |
| UI-NODE-002 | Sync queue | Admin | Pending, retry, conflict review |
| UI-NODE-003 | Campus data exchange | Admin | Export, import, dry-run, approve |
| UI-ADM-SEC-001 | Users and permissions | Admin | Assign roles and bounded scopes |
| UI-AUD-001 | Audit history | Authorized auditor | Search event history and export |

## 6. Standard screen states

Every list/detail/form must define:

- Loading.
- Empty state with useful next action.
- Validation error.
- Permission denied.
- Resource not found/not visible.
- Network disconnected.
- Local Node unavailable.
- Cloud disconnected.
- Pending Sync.
- Syncing.
- Synced/Acknowledged.
- Conflict / Needs Review.
- Provisional / Unconfirmed financial state.
- Success confirmation.
- Provider/dependency failure.
- Stale data / last updated timestamp.
- Restricted Mode / action blocked.

Do not display an empty list when data failed to load; show an explicit error.

## 7. Form design rules

- Place labels above fields; placeholders are examples, not the only label.
- Mark required fields and explain validation next to the field.
- Use controlled dropdowns for school-defined values such as class, section, board and fee head.
- Keep admission number, student number, invoice number and receipt number read-only after issuance unless a documented correction process exists.
- Group related fields into logical sections.
- Preserve entered draft values after validation failure.
- Show an unsaved-change warning where navigation could lose work.
- Use confirmation dialogs for payment posting, refund, waiver, result publication, deletion/deactivation and node/license actions.
- Show the amount/currency and allocation target before final financial confirmation.
- Use inline help for unfamiliar finance/exam terms.
- Avoid asking the user to enter tenant/campus IDs manually; derive scope from the session and show the active scope.

## 8. Table and search behavior

- Apply permission filters on the server before pagination.
- Include useful search/filter fields: admission no, student name, class, section, academic year, status, date and campus where authorized.
- Keep table columns configurable but ensure sensitive fields are hidden by default.
- Show totals only for the authorized result set.
- Exports must use the same authorization and filters.
- For long lists use server-side pagination/search.
- Provide clear filter reset and no-results state.
- Show the data freshness timestamp for imported/synchronized snapshots.

## 9. Financial UI rules

- Currency must be visible (initial default PKR).
- Show billed, paid, allocated, unallocated and outstanding as distinct values.
- A bank transaction suggestion is not a confirmed payment.
- A provisional offline receipt must carry a visible Provisional/Pending Sync label.
- A receipt is not deleted after posting; corrections are reversals/refunds/adjustments.
- Before posting, display the final ledger impact and approvals.
- Show original and reversal records together.
- Prevent double submission with disabled submit state and idempotency support.

## 10. Academic UI rules

- Differentiate draft marks from moderated, approved and published results.
- Show maximum marks and allowed range beside every marks field.
- Absence and exemption must use explicit status controls, not magic negative numbers.
- Published result is read-only; correction produces a new revision.
- Show board/qualification/grading-scheme version in exam/result configuration.
- Student and parent views show published results only unless policy explicitly grants another view.

## 11. Offline and sync UI rules

- Header displays Local Node connectivity and cloud connectivity separately.
- Each offline operation has a local operation ID and status.
- Pending Sync is not equivalent to Synced.
- On conflict, show the conflicting record, version, source device and safe resolution choices.
- Do not silently discard an operation on logout or session expiry; follow approved recovery policy.
- When local storage is near quota, warn before accepting more offline drafts.
- Show last cached dataset refresh time and allow user to refresh when connected.

## 12. Urdu, RTL and accessibility

- Use proper Urdu script and an approved Nastaleeq-capable font fallback stack for the interface/documents where licensed and technically supported.
- Set direction per language context; do not force every mixed-content component to RTL.
- Numerals, phone numbers, currency codes and identifiers must remain correctly ordered in RTL layouts.
- Test mixed Urdu/English labels, numbers, punctuation, dates and validation messages.
- Provide an RTL-compatible table/grid strategy.
- Keyboard focus and dialog order must remain logical in both LTR and RTL.
- Inputs need persistent labels, accessible error announcements and visible focus.
- Never rely on color alone to convey Present/Absent, Pending/Confirmed or success/failure.

Full language scope (English, Urdu and whether Roman Urdu is a complete UI locale at release one) remains an Open Decision. Roman Urdu is currently a conversational AI priority, not automatically a translated application interface.

## 13. Design system deliverables before UI implementation

- [ ] Approved navigation and role menus.
- [ ] Responsive breakpoints and layout grid.
- [ ] Typography and Urdu font licensing review.
- [ ] Color tokens and contrast checks.
- [ ] Button, input, select, table, modal, toast and badge components.
- [ ] Error/empty/loading/offline state components.
- [ ] Date/time/currency/number formatting rules.
- [ ] RTL interaction and visual QA.
- [ ] Accessibility checklist.
- [ ] Clickable wireframes for admission, fee payment, reconciliation, attendance and result publication.
- [ ] Parent/guardian multi-child view.
- [ ] Campus selector and freshness indicators.
