# 23 — AI Training, Data Governance and Evaluation

## 1. Default training policy

School/student/guardian/staff/finance/examination data is **not automatically used to train shared foundation models**. Retrieval-augmented generation (RAG) is retrieval at request time; it is not model training.

No cross-school training on customer data by default. A provider's “not used for training” setting does not by itself settle retention, abuse-monitoring, access or transfer questions; check current contractual terms.

## 2. Fine-tuning policy (opt-in only)

Fine-tuning may be considered only after a documented use case and quality baseline exist. Required gates:

1. Written customer permission and confirmation of data ownership/authority.
2. Purpose limitation and privacy/legal review.
3. Data minimization, de-identification where appropriate and removal of unnecessary identifiers.
4. Teacher/domain-expert review of labels and expected answers.
5. Versioned dataset with provenance and consent record.
6. Train/test separation and leakage checks.
7. Evaluation for accuracy, bias, language and subject-level failure patterns.
8. Model/version approval, rollback plan and deletion/retention terms.

Teacher feedback may be stored for product evaluation where policy allows, but must not silently become a training dataset. Explicitly separate feedback, evaluation sets and training sets.

## 3. Human accountability

- Teachers remain responsible for final academic decisions and published marks.
- Finance staff remain responsible for ledger, reconciliation and approvals.
- Principals/authorized staff decide interventions and student support.
- AI findings must show evidence and uncertainty; no automated accusations or labels.
- Keep an override/correction path and record reviewer outcome.

## 4. Feature-specific evaluation

### AI Auditor
Measure precision/recall on reviewed examples, false-positive rate, missed anomalies, explanation usefulness and reviewer disposition. Test varied school sizes and transaction patterns. AI may prioritize review but cannot declare fraud.

### Learning Recovery
Evaluate topic/learning-outcome classification, worksheet alignment, difficulty, learning gains and educator usefulness. Total exam score alone is insufficient to infer mastery of individual topics.

### Examination Intelligence
Evaluate paper coverage, mark totals, answer-key correctness, OCR/handwriting recognition and suggested marks by subject, language, question type and image quality. Store original paper/answer evidence, rubric version, AI suggestion and teacher-approved final mark separately.

### Business Copilot
Validate every numeric result against deterministic reports. Responses must disclose date range, filters, scope, calculation and data freshness. No unrestricted SQL.

### Urdu Voice Assistant
Test Urdu script, Roman Urdu, English mixing, accent variation, names, background noise and confirmation behavior. Voice must not become sole authentication for sensitive operations.

### Student Early Intervention
Evaluate false positives, subgroup fairness, explainability and effectiveness of supportive follow-up. No permanent or stigmatizing student labels, automated punishment, exclusion, diagnosis or promotion decision.

## 5. Evaluation operations

- Curated, permissioned and versioned datasets.
- Human-reviewed reference outputs and rubrics.
- Regression suite for prompts, models, tools and retrieval.
- Red-team tests for prompt injection, data leakage and unsafe actions.
- Performance and cost metrics by task type.
- Drift monitoring and rollback for model/prompt releases.
- Documented release gate and owner for each AI capability.

## 6. Governance record

For each AI feature/model, record purpose, allowed users, data classes, hosting routes, prohibited uses, provider terms, retention, training status, evaluation threshold, human approval points, incident owner and customer configuration. Revisit when a provider, model, data source or intended use changes.
