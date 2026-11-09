# 25 — AI Implementation Task Plan

**Overall status:** Not Started. This is a planned work breakdown, not evidence of implementation.

## Dependencies before AI feature rollout

- Tenant/campus identity and server-side authorization are stable.
- Core business modules expose audited application services and trusted reporting projections.
- Data quality and ownership are sufficiently mature for the selected use case.
- Hybrid Local Data Plane and Cloud Control Plane boundaries are defined.
- Customer privacy notices, provider agreements and retention policy are reviewed.

## AI-0 — Discovery, privacy and provider evaluation

- [ ] Define detailed acceptance criteria for all six AI features.
- [ ] Create feature-to-data-class matrix and approved hosting routes.
- [ ] Identify provider candidates, contract terms, region, retention and training settings.
- [ ] Build cost/quality benchmark set for Urdu, Roman Urdu and English tasks.
- [ ] Decide data retention, customer controls and deletion workflow.
- [ ] Define human approval and incident response policy.
- [ ] Complete architecture and privacy review.

**Exit:** Approved AI requirements, data-flow diagram, provider shortlist and privacy policy.

## AI-1 — Provider Gateway and usage foundation

- [ ] Implement provider capability/adapter interfaces.
- [ ] Implement tenant-aware gateway and deterministic routing policy.
- [ ] Add server-side secrets storage and key rotation.
- [ ] Add budgets, quotas, rate limits and usage/credit ledger.
- [ ] Add model/prompt versioning and background job status.
- [ ] Add provider timeout, retry, circuit breaker and outage behavior.
- [ ] Add privacy-safe audit metadata and observability.
- [ ] Test that prohibited data classes cannot reach disallowed providers.

**Exit:** Gateway operates in test environment with tenant isolation, usage metering and privacy-route regression tests.

## AI-2 — Secure tool calling

- [ ] Define typed allow-listed tool contracts.
- [ ] Implement server-side tenant/campus scope derivation.
- [ ] Connect first read-only finance/attendance/reporting tools.
- [ ] Add tool argument validation, row limits and freshness metadata.
- [ ] Add tool execution audit and prompt-injection tests.
- [ ] Add approval flow for draft/action tools (not automatic consequential writes).

**Exit:** AI can answer selected report questions only through authorized application services.

## AI-3 — RAG foundation

- [ ] Validate PostgreSQL + pgvector in cloud and local environments.
- [ ] Implement private document ingestion and malware/file validation.
- [ ] Add PDF/DOCX extraction and OCR path for image documents.
- [ ] Add language-aware chunking and source-page references.
- [ ] Add embeddings, tenant/campus ACL filters and citations.
- [ ] Implement document versioning, deletion and re-indexing.
- [ ] Separate cloud-approved and local-only indexes.
- [ ] Evaluate Urdu/English/Roman Urdu retrieval quality.

**Exit:** Permission-filtered answers cite the correct source and cannot cross tenant/campus boundaries.

## AI-4 — First product release: Auditor and Business Copilot

- [ ] Implement deterministic finance anomaly rules.
- [ ] Add read-only reporting tools and explanation layer.
- [ ] Add finding severity, evidence and reviewer disposition.
- [ ] Show calculation, date range, scope and data freshness in Copilot.
- [ ] Add feedback, false-positive tracking and evaluation dashboard.
- [ ] Pilot with controlled, anonymized or explicitly approved data.

**Exit:** Human-reviewed results meet agreed quality thresholds and do not write finance records.

## AI-5 — Learning and Examination Intelligence

- [ ] Implement topic/learning-outcome performance dataset.
- [ ] Add curriculum-aware worksheet generation and teacher review.
- [ ] Add exam paper blueprint and coverage validation.
- [ ] Add objective OMR/image checking pilot.
- [ ] Evaluate OCR/handwriting and rubric-based subjective marking.
- [ ] Store AI suggestions separately from approved official marks.
- [ ] Add subject/language-specific evaluation and moderation.

**Exit:** Teachers can inspect, correct and approve outputs; no AI-only final mark publication.

## AI-6 — Voice and Early Intervention

- [ ] Benchmark Urdu, Roman Urdu and mixed-language speech services.
- [ ] Build voice assistant read-only query flow.
- [ ] Add confirmation and normal permission checks for actions.
- [ ] Build early-intervention signals with explanation and human review.
- [ ] Conduct false-positive, fairness and safety reviews.
- [ ] Test noise, accent, names, accessibility and device compatibility.

**Exit:** Voice and intervention suggestions meet agreed safety/quality thresholds and remain human-supervised.

## AI-7 — Private and Campus Local AI

- [ ] Define premium private-hosting service and operational responsibilities.
- [ ] Evaluate local model runtime and hardware profiles.
- [ ] Implement local inference endpoint behind the same gateway contract.
- [ ] Implement local RAG and approved local document indexing.
- [ ] Test WAN outage and cloud-fallback prohibition for local-only data.
- [ ] Define model update, vulnerability patching and rollback process.
- [ ] Measure cost, latency, support load and local capacity.

**Exit:** Optional local inference works on a supported hardware profile and does not affect core offline school operations.

## Definition of Done for any AI task

- [ ] Code and automated tests completed.
- [ ] Tenant/campus authorization and data classification tested.
- [ ] Quality and adversarial tests pass.
- [ ] Human approval requirements are implemented.
- [ ] Usage, cost and audit events are observable.
- [ ] Privacy/retention/provider terms are reviewed.
- [ ] Documentation, rollback and support instructions are updated.
- [ ] Product owner and technical lead accept the result.
