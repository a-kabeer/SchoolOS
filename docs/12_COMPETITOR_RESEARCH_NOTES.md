# 12 — Competitor and Market Research Notes

**Evidence caution:** These notes summarize public product descriptions reviewed during the planning discussion. They are not independent hands-on product tests, audited performance results or legal claims. Feature availability, pricing and positioning can change. Re-check official websites and run demos before using these details in sales or investment materials.

## 1. Competitors originally identified

| Product | Public positioning observed | Product-design implication |
|---|---|---|
| eSchool | Broad school ERP; fees, attendance, academics, HR and bank/reconciliation claims | Do not compete on a generic module checklist; benchmark actual reconciliation workflow |
| Ilmversity | School management and learning platform with administration, finance and academic workflows | Compete on workflow reliability and operational simplicity |
| SIMSIN | Local school ERP and related education products | Study local workflow and product ecosystem |
| GLOWSIMS | Broad ERP with finance, campus reporting and bank/payment integration claims | A basic bank match feature is not enough to differentiate |
| OpenEduCat Community Edition | Open-source/self-hosted school ERP alternative | Managed onboarding, Pakistan-specific workflows, support and simpler operations may distinguish SaaS |
| Skoo | Publicly advertises offline attendance/cash-fee entry and AI-related functions | Offline and AI claims already exist; benchmark sync and error handling |
| Ilma.pk | Publicly advertises offline-first workflows and synchronization | Reliability must be demonstrated, not merely claimed |

## 2. Additional Pakistan-focused AI products observed

Public product pages reviewed during research included claims associated with:

- Muallim — lesson planning, worksheets, quizzes and paper generation.
- Classwell — AI-assisted marking and voice/parent-related features.
- Ilmora — AI assistant, analytics and workflow claims.
- EduPortal — analytics and AI assistant claims.
- AI Campus — AI-oriented education management claims.
- Skoolyte — school management, offline and AI-related claims.

Do not assume all advertised features are generally available, production-ready or independently benchmarked. Request live demos, references and documented workflows.

## 3. AI answer checking products discussed

### Qalam — Smart Exam Creator & Grading App

A likely match for the Saudi exam-scanning app described in the discussion. Public materials describe exam creation and camera/OMR-style objective checking (e.g. MCQ and true/false). A possible Karachi software-company connection was observed through publisher/package clues, but the exact ownership, contracting and development relationship was not conclusively established. Treat that connection as unverified until confirmed by the companies.

### AIDE AI

A Pakistan-founded assessment product associated with handwritten/typed answer evaluation against marking schemes and teacher review. Its Saudi connection discussed was expansion/program participation signals, not proof of live deployment in Saudi schools. Company-reported usage claims should not be represented as audited numbers.

### Product implication

- OMR/objective answer detection and subjective handwritten answer grading are different technical problems.
- Consider partner/API evaluation before building a specialist grading engine from scratch.
- Any vendor must be tested with local languages, handwriting, mathematics working, diagrams and the school's real marking rubrics.
- Teacher approval remains mandatory for final marks.

## 4. Differentiation hypothesis

The product should not claim that “AI”, “offline”, “attendance” or “fee reconciliation” is unique. Competitors already advertise related capabilities.

Potential differentiation to validate:

1. Transparent, auditable payment allocation and exception resolution.
2. Family/sibling combined payment handling.
3. Reliable offline operation with explicit pending/confirmed states and robust retry/conflict behaviour.
4. Finance and academic data connected through useful, permission-safe intelligence.
5. Configurable board/curriculum framework with validated content coverage.
6. Better onboarding, data migration and local support.

## 5. Required competitor validation

For each competitor, test or request a demo of:

- One payment for multiple siblings.
- Partial payment and arrears allocation.
- Unreferenced bank transfer.
- Duplicate statement import.
- Reversed payment and refund history.
- Cash collection while offline, force close and later sync.
- Two devices editing the same attendance record.
- Cross-campus permissions.
- Parent account with multiple children.
- Data export and account termination.
- Actual AI marking workflow, human review and accuracy evidence.

Record product version, test date, evidence and limitations. Do not rely only on marketing pages.
