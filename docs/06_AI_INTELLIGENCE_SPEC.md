# 06 — AI Intelligence Specification

## 1. AI architecture principles

- AI functions live behind the SchoolOS AI module and provider gateway.
- Provider-specific SDKs are isolated in adapters.
- AI reads data through authorized application/reporting interfaces, not unrestricted database access.
- Every request includes tenant/scope and permission checks.
- Long tasks run asynchronously and expose job status.
- Prompts and model configuration are versioned.
- AI output is stored with model/provider/version metadata and evidence references.
- Human review is required for sensitive financial and academic outcomes.
- Track usage, latency, cost, error and quality.
- Do not send unnecessary student/guardian sensitive data to external providers.

## 2. AI School Auditor

### Purpose
Identify unusual or inconsistent financial and operational activity for authorized review.

### Initial checks
- Potential duplicate payment/reference/import.
- Unmatched bank transaction.
- Cash closing variance.
- Discounts/waivers outside configured policy patterns.
- Repeated payment reversals or adjustments.
- Unusual changes to protected student/fee records.
- Repeated marks corrections or attendance amendments.
- Unusual user activity, subject to privacy and policy review.

### Output requirements
Each finding includes:
- Finding category and severity.
- A clear explanation.
- Source records and date range.
- Why it was flagged.
- Confidence/limitations, where meaningful.
- Recommended human review step.
- Reviewer outcome and false-positive feedback.

### Restrictions
AI cannot automatically accuse a person of fraud, reverse payments, alter the ledger, suspend a user or modify student records.

## 3. AI Learning Recovery Engine

### Purpose
Use question-level and topic/outcome-level performance to recommend targeted learning support.

### Required data
- Assessment questions and marks.
- Topic and learning-outcome mapping.
- Attempts and date context.
- Approved teacher observations where relevant.

### Functions
- Identify potential topic-level gaps.
- Recommend practice questions/worksheets.
- Adjust difficulty gradually.
- Suggest teacher-led revision.
- Compare re-assessment with previous performance.
- Provide explainable progress summaries.

### Restriction
Do not infer precise topic mastery from total exam marks alone. Recommendations are provisional and teacher-reviewable.

## 4. AI Examination Intelligence

### A. Paper generation and quality assurance
Inputs:
- Board/curriculum and version
- Grade/qualification
- Subject and topics
- Learning outcomes
- Marks and duration
- Question types
- Difficulty distribution
- Language and formatting rules

Checks:
- Total marks and section totals
- Duplicate/near-duplicate questions
- Syllabus coverage
- Learning-outcome coverage
- Answer-key consistency
- Marking scheme completeness
- Difficulty distribution
- Language/terminology consistency

### B. Objective paper checking
Use OMR/image processing where appropriate. Validate alignment, erasures, multiple marks and uncertain detections. Route low-confidence sheets to a human.

### C. Subjective/handwritten answer assistance
Potential pipeline:
1. Image quality and page detection.
2. OCR/handwriting recognition.
3. Answer segmentation.
4. Rubric/mark-scheme retrieval.
5. Evidence-based suggested marks and rationale.
6. Teacher review and correction.
7. Final approved mark stored by Examination module.

### Safeguards
- Original answer image remains traceable.
- Mark-scheme/rubric version is recorded.
- AI suggestion and teacher-approved mark are stored separately.
- Teacher can override with reason where policy requires.
- No AI-only final publication.
- Evaluate handwriting, Urdu/English, mathematics working, diagrams and varied writing styles separately.

## 5. AI Business Intelligence Copilot

### Example questions
- Which campus has the highest overdue fees?
- Compare current and previous month collections.
- Show students whose attendance and academic results have both declined.
- Summarize expenses for a date range.
- Estimate upcoming fee collections.

### Design
- Use an approved semantic reporting layer and predefined query tools.
- Do not give the LLM unrestricted SQL/database access.
- Enforce tenant/campus scope before data retrieval.
- Return calculation logic, filters, data freshness and source references.
- Label forecasts as estimates and show assumptions.
- Log tool calls and sensitive access.

## 6. Urdu / Roman Urdu Voice Assistant

### Capability pipeline
Speech input → speech-to-text → language/intent handling → permission check → structured query/action → confirmation (if required) → application service → spoken response.

### Example requests
- Parent asks remaining fee balance.
- Teacher prepares attendance entry for a class.
- Principal requests campus collection summary.

### Safeguards
- Confirmation before consequential actions.
- No voice-only authorization for sensitive operations.
- Disambiguate student/class names.
- Respect guardian-to-child and staff-to-campus scope.
- Keep audit records of commands and confirmed actions.
- Test Urdu, Roman Urdu, English mixing, accents, background noise and names.

## 7. Student Early Intervention

### Signals
- Attendance trends.
- Subject/topic-level results.
- Homework completion.
- Assessment progression.
- Teacher observations.

### Output
Recommend a human review or supportive intervention, not a permanent student label.

### Safeguards
- No automatic punishment, exclusion, promotion or diagnosis.
- Explain contributing signals.
- Limit sensitive access to authorized staff.
- Evaluate false positives and subgroup fairness.
- Allow staff to correct context and record intervention outcome.
- Avoid using fee hardship as a general academic-risk signal without an explicit lawful, ethical and policy-reviewed purpose.

## 8. Shared AI data model (preliminary)

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

Store references and minimum necessary content. Avoid duplicating sensitive student records in AI logs. Encrypt provider credentials/secrets and restrict administration.

## 9. AI release order

1. AI School Auditor and Business Copilot after Finance, reporting and access controls are stable.
2. Learning Recovery after question-level/topic mapping is populated.
3. Exam generation and marking after rubrics, paper workflows and teacher approval exist.
4. Voice assistant after permissions and action confirmation flows are stable.
5. Early Intervention after data quality, fairness evaluation and school support policies are approved.

All six are in product scope; this is an implementation order, not removal from scope.

## 10. AI evaluation requirements

- Curated test datasets with appropriate permissions.
- Human-reviewed reference answers/marks.
- Accuracy and error analysis by subject/language/question type.
- False-positive rates for auditor and intervention signals.
- Teacher override/correction rate.
- Prompt/model regression tests.
- Latency and cost per operation.
- Provider outage and fallback tests.
- Data retention and training-use contract review.


## 11. Detailed AI architecture documents (v1.3)

The six feature descriptions above are retained. Their platform architecture and governance are expanded in:

- `20_AI_ARCHITECTURE_AND_HOSTING.md` — hosting modes and request lifecycle.
- `21_AI_PROVIDER_GATEWAY_AND_PRIVACY.md` — provider routing, classification, security and retention.
- `22_AI_RAG_AND_TOOL_CALLING.md` — document retrieval and approved application tools.
- `23_AI_TRAINING_AND_GOVERNANCE.md` — training policy, human accountability and evaluation.
- `24_AI_PRICING_AND_USAGE_METERING.md` — credits, quotas and premium packaging.
- `25_AI_IMPLEMENTATION_TASK_PLAN.md` and `26_AI_PROGRESS_TRACKER.md` — delivery plan and status.
