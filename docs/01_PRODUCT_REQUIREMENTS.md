# 01 — Product Requirements Document (PRD)

**Product:** SchoolOS (working name)  
**Version:** 1.0 planning baseline  
**Market:** Pakistan; private schools and school chains  
**Product type:** Hybrid multi-tenant School SaaS with Cloud Control Plane + Local Data Plane nodes and PWA-capable local web UI

## 1. Product vision

Build a dependable school operating platform that brings financial, academic and administrative operations into one system. Its initial competitive focus is reliable fee reconciliation and controlled offline work. AI should make school data more useful and reduce repetitive work without removing human accountability.

## 2. Target users

- School group owner / organization owner
- Chain administrator
- Finance director / group accountant
- Campus principal
- Campus administrator
- Accountant
- Cashier
- Admission officer
- Examination controller
- Teacher
- HR manager
- Parent / guardian
- Student
- SaaS platform operator

## 3. Customer problems to solve

- Fee records, cash collections, bank deposits and accounting records do not always reconcile easily.
- One payment may cover multiple children or invoices.
- Unreferenced or misreferenced transfers need manual investigation.
- Campus-level and consolidated reporting can be inconsistent.
- Internet interruptions disrupt attendance and other daily workflows.
- Student data and academic performance are stored in disconnected systems or spreadsheets.
- School leaders need trusted summaries rather than manually combining spreadsheets.
- Teachers need help preparing assessments and identifying learning gaps.

These are hypotheses to validate through structured school interviews and demonstrations of current workflows.

## 4. Product principles

1. **Financial correctness before visual dashboards.** Every receipt and posting must be traceable.
2. **Offline with explicit state.** Pending local records must never be presented as server-confirmed transactions.
3. **Human accountability.** AI suggests, explains and assists; authorized staff approve sensitive actions.
4. **Tenant isolation by design.** One school's information must never be visible to another tenant.
5. **Configurable curriculum.** Board and syllabus differences are represented as data and versioned configurations, not hardcoded assumptions.
6. **Role-specific experiences.** Each user sees the tools needed for their responsibilities.
7. **Auditable changes.** Important financial, academic and access changes must retain history.
8. **Incremental delivery.** Release a stable core before adding every optional ERP module.

## 5. Release strategy

### Release 1 — SchoolOS Core

- Multi-tenant organization and campus administration
- Identity, users, roles and permissions
- Admissions and student/guardian records
- Academic year, class, section and subject setup
- Fee structures, invoices, receipts and allocations
- Core finance ledger and expense records
- Bank statement import and reconciliation queue
- Attendance and essential offline workflows
- Examination basics and result workflow
- Basic reporting and audit history
- Initial AI Auditor and Business Copilot capabilities only after data and permissions are ready

### Release 2 — SchoolOS Intelligence

- Learning Recovery Engine
- AI examination generation, quality checking and assisted marking
- Urdu/Roman Urdu voice assistant
- Student Early Intervention
- Advanced finance and operational analytics

### Release 3 — SchoolOS Enterprise

- HR and payroll
- Transport
- Library
- Inventory and procurement
- Hostel and visitor management where demanded
- Advanced school-chain controls, integrations and analytics

## 6. Functional scope by area

### 6.1 Platform and organization

- SaaS customer/tenant registration and lifecycle
- Organization profile and school entities
- Multiple campuses per school
- Academic years, terms and configurable calendars
- User invitations, account status and access revocation
- Role/permission administration
- Campus and school scope assignments
- Subscription plans and usage limits (commercial policy to be finalized)
- Global audit and operational monitoring

### 6.2 Admissions and students

- Admission inquiries and lead sources
- Application forms and document upload
- Admission assessment/interview workflow
- Review, approval, rejection and wait-list states
- Student admission number and enrollment
- Student profile and history
- Guardian and household/family records
- Multiple guardians per student and siblings in one household
- Class, section and campus allocation
- Campus/school transfers
- Student withdrawal and archived history

### 6.3 Fees and finance

- Configurable fee categories and fee schedules
- Class-wide and student-specific fee assignments
- Tuition, admission, annual, exam, transport and custom charges
- Monthly, term and ad hoc invoices
- Installments, partial payments, discounts and scholarships
- Waivers and penalties with approval rules
- Household/sibling balances and combined payment allocations
- Cash, cheque, bank transfer and online payment records
- Receipt and challan generation
- Chart of accounts and double-entry ledger
- Cashbook, bank accounts, expenses, vendors and accounting periods
- Daily closing and campus/consolidated reporting
- Reversals and refunds through controlled workflows

### 6.4 Reconciliation

- Import bank statements from supported CSV/XLSX templates
- Preserve original import batch and source rows
- Detect duplicate imports and duplicate bank references
- Suggest matches using amount, reference, date, payer and contextual details
- Support one bank transaction allocated to multiple invoices/students
- Support partial allocations and split/combined payments
- Route uncertain transactions to an unmatched queue
- Require authorized confirmation before final allocation/posting
- Track reversals, discrepancies and reconciliation sessions
- Produce auditable reconciliation reports

### 6.5 Academics and curriculum

- Configurable education boards and curriculum frameworks
- Curriculum and syllabus versioning
- Grade levels, classes, sections, subjects and topics
- Learning outcomes / SLOs and competency mapping
- Class-subject and teacher allocations
- Lesson planning, class diary and homework
- Learning material and resource management
- Board-specific grading and assessment configuration
- Cambridge pathway support through configurable subject/qualification structures

**Coverage caution:** A configurable framework is not the same as supplying every board's complete verified syllabus. Each board, curriculum version, subject, grading rule and content source must be validated and loaded with appropriate rights.

### 6.6 Attendance and staff

- Daily and period-wise student attendance
- Teacher/staff attendance
- Late arrival and early departure
- Leave requests and approval
- Attendance correction workflow
- Parent absence notifications
- Attendance summaries and trend reports
- Device-level offline capture and sync audit

### 6.7 Examinations

- Exam sessions and schedules
- Question bank and tagging
- Exam blueprints and topic/learning-outcome coverage
- Paper creation and approval
- Answer keys and marking rubrics
- Marks entry and verification
- Grading scales and result calculation
- Moderation, rechecking and approval
- Report cards and transcripts
- Result publication and notification

### 6.8 Communication

- Notices and announcements
- Email, SMS and WhatsApp provider integration
- Fee and attendance notifications
- Examination/result notifications
- Message templates and language variants
- Delivery attempts and status tracking
- Parent-teacher communication (scope and safeguards to be defined)

### 6.9 Advanced operations (later release)

- HR, recruitment, staff records and payroll
- Transport routes, vehicles, drivers and student allocation
- Library circulation and fines
- Inventory, procurement and stock movement
- Hostel management
- Visitor management
- Complaints and feedback
- Events and school calendar

## 7. User experience requirements

- Responsive desktop, tablet and mobile layouts
- Role-specific dashboards and navigation
- English and Urdu interface support, including right-to-left layouts
- PKR as initial default currency
- Configurable date, academic year and grading formats
- Printable fee challans, receipts and reports
- Clear sync states: Local Draft, Pending Sync, Synced, Conflict, Failed/Needs Review
- Clear distinction between imported bank transaction, matched payment and confirmed fee allocation
- Accessible forms, validation messages and keyboard-friendly administration

## 8. Business rules that must be specified in detail

- Family payment allocation and cross-child permissions
- Fee invoice generation, due dates and late penalty policy
- Discount, scholarship and waiver approval thresholds
- Cash collection and receipt finalization
- Payment reversals and refunds
- Accounting period closing and reopening
- Student transfers between campuses or school entities
- Attendance correction deadlines and approvals
- Examination marks lock, recheck and publication
- Data retention and student withdrawal handling
- Curriculum version changes and historical result interpretation

## 9. Product success metrics

Proposed pilot measures:

- No duplicate financial postings
- 95%+ automatic matching on eligible, clearly identifiable imported bank transactions (target to validate, not guaranteed)
- Every unmatched bank transaction remains traceable and assigned for review
- All accepted offline operations either synchronize or remain visible as explicit exceptions
- Zero unauthorized cross-tenant access in security tests
- All published results have an approval history
- Reduced time for daily cash closing and month-end fee reconciliation
- Teacher correction rate and marking agreement measured for AI-assisted grading
- AI Auditor findings include evidence and measurable false-positive rates

## 10. Out of scope for initial launch

- Microservices architecture
- A custom foundation model
- Fully automated decisions about student promotion, discipline, financial fraud or final marks
- Every possible bank's direct API connection
- Full-featured offline bank verification and refunds
- Claiming all board syllabus content is included before validation


## Hybrid deployment requirements (v1.1)

- The product must support a SchoolOS-operated Cloud Control Plane and one or more customer Local Data Plane nodes.
- Local nodes run the school-business Modular Monolith, React UI, PostgreSQL and local authentication.
- Staff must be able to access approved local workflows over the school LAN while the public internet/cloud is unavailable.
- The product must support a shared group node or per-campus edge nodes; independent offline operation at a campus requires a local node at that campus.
- Monthly subscription entitlements must be represented by a signed, time-bounded, node-bound offline license lease.
- Subscription expiry must not delete school data; restricted mode must retain read/export, backup and renewal paths.
- Local/cloud sync must use explicit business events, ownership rules, idempotency and conflict handling; no generic last-write-wins financial sync.
- Remote parent/student access requires a separately designed Cloud Portal Gateway/read model or secure VPN access. A local server must not be publicly exposed.
- Cloud-based AI features may become unavailable/queued during internet outages. Offline AI requires a separately installed and validated local runtime.

See documents 14–17 for the deployment, subscription, security and implementation requirements.


## 11. AI hosting, privacy and commercial requirements

- Support cloud inference for permitted/minimized data, private AI infrastructure for higher-isolation needs, and optional premium campus-local inference.
- AI must remain optional to core school operations. A provider or internet outage must not prevent ordinary admissions, attendance, finance or academic work.
- Apply privacy classification and backend authorization before data retrieval or provider routing. Sensitive workloads default to private/local handling unless a documented policy exception permits external processing.
- All model access is mediated by a backend Provider Gateway. Provider keys are never exposed to React clients.
- RAG must respect tenant, campus and document ACLs and provide source references. Operational data is accessed through allow-listed application tools, not unrestricted SQL.
- School/customer data is not automatically used to train shared foundation models. Fine-tuning requires explicit opt-in and governed datasets.
- Base subscription may include a limited AI allowance; advanced AI is a paid add-on. Private/local AI is a premium option. Numeric quotas and prices require cost and pilot validation.
- AI usage must be metered and controlled with tenant budgets, quotas, alerts and no-surprise overage behavior. Credit exhaustion may restrict AI only; it must not lock core school workflows.
- Consequential finance, examination, communication and student-support actions remain under existing permissions and human approval.

See `20_AI_ARCHITECTURE_AND_HOSTING.md` through `26_AI_PROGRESS_TRACKER.md` for detailed requirements and implementation tasks.
