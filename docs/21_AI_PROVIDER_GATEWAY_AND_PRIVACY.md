# 21 — AI Provider Gateway, Privacy Classification and Security

## 1. Design objectives

The Provider Gateway is the single backend-controlled entry point for model inference, embedding, speech and other AI provider capabilities. Feature modules must not call third-party SDKs directly. This keeps provider changes, privacy policy, metering and reliability controls centralized.

## 2. Provider adapter contract

Define capability-oriented adapters rather than assuming every model supports every task. Example capabilities:

- Text generation and structured output.
- Vision/image understanding.
- Embeddings.
- Speech-to-text and text-to-speech.
- Reranking, where justified.

An adapter should expose supported capabilities, limits, model/version, context window, modality support, region/hosting characteristics, retention/training terms and health status. Provider-specific options remain inside the adapter.

## 3. Routing policy inputs

Routing decisions should consider:

- Tenant's enabled plan and entitlements.
- User permission and campus scope.
- Task type and required capability.
- Data classification and approved transformations/redactions.
- Provider contract, region and retention eligibility.
- Whether external processing is enabled by the tenant.
- Local runtime availability and capability.
- Budget, remaining credits, rate limits and provider health.

Routing must be deterministic and explainable enough to audit. A provider fallback is permitted only if the alternative meets the same or stricter privacy and task requirements. Never use cloud as a silent fallback for a request that policy requires to remain local/private.

## 4. Data classification baseline

| Class | Examples | Default handling |
|---|---|---|
| Public | Public syllabus descriptions, general educational explanations | Cloud API permitted where contract and product policy allow. |
| Internal | Internal timetable, school policies, non-sensitive operational procedures | Cloud API may be allowed with tenant policy and minimum necessary context. |
| Confidential | Fees, staff/student records, attendance, marks, parent/guardian details | Minimize and redact; use cloud only when explicitly permitted by policy/contract. Prefer private/local for sensitive workflows. |
| Highly Sensitive | Child safeguarding information, sensitive assessments or records with heightened harm risk | Default local/private processing; strict access and explicit policy exception required before external transfer. |

Classification is a starting governance scheme, not a legal determination. Final categories, lawful basis, notices, retention and contracts require Pakistan-specific legal/privacy review and customer agreement.

## 5. Minimization and redaction

- Retrieve only records required for the current task.
- Prefer aggregate or pseudonymized context over names, contact details or identifiers.
- Do not transmit full student profiles when a small set of assessment observations is sufficient.
- Mask direct identifiers where task quality permits.
- Treat embeddings as potentially sensitive derived data; protect them with the same tenant/ACL boundaries as the source documents.
- Never put secrets, provider keys, passwords or authentication tokens in prompts.

## 6. Security controls

- Provider credentials stored in a secrets manager or protected server-side secret store; encrypted at rest and rotated.
- Separate credentials per environment and, where practical, per customer/private deployment.
- TLS for all remote inference and sync traffic; mutual TLS for trusted node-to-cloud connectivity where applicable.
- Authorization checked in the backend before every retrieval and tool invocation.
- Strict tenant and campus isolation in logs, queues, caches, vector search and stored outputs.
- Prompt injection treated as untrusted input; retrieved documents and tool outputs cannot alter system policy.
- Allow-list tools and validate their typed arguments server-side.
- Rate limits, token/page/minute caps, spend budgets and abuse detection.
- Audit access to highly sensitive AI workloads; avoid writing raw sensitive prompts/responses to general logs.

## 7. Retention and provider terms

“Not used for model training” must not be interpreted as “zero retention.” Before enabling a provider for a data class, record and review:

- Whether customer inputs/outputs are used for training or improvement.
- Abuse-monitoring and operational retention periods.
- Available zero-retention or modified-retention eligibility and conditions.
- Data processing locations, subprocessors and cross-border transfer implications.
- Encryption, access controls, incident notification and deletion terms.
- Contractual ownership and permitted use of prompts, outputs and uploaded documents.

These provider terms can change. Verify current official terms and applicable contracts before implementation and before each material provider change; this archive does not certify current provider retention policies.

## 8. Logging and audit

Record metadata such as request ID, tenant, authorized campus scope, user/service identity, feature, classification, policy decision, provider/model/version, timestamps, token/page/minute usage, cost/credits, status, error category and source references. Store payload content only when there is a defined purpose, access policy and retention schedule. Sensitive content should be excluded or separately protected.

## 9. Human oversight and output handling

- Auditor findings are review suggestions, not fraud conclusions.
- AI marks are recommendations, not official marks.
- Learning and intervention recommendations require educator review.
- Draft messages must be approved before sending where they affect parents/students.
- Voice is not a substitute for strong authentication or confirmation.
- Any model-generated action must pass the owning module's authorization, validation and audit workflow.

## 10. Operational tests

Test cross-tenant retrieval attempts, wrong-campus access, prompt injection, malicious uploaded documents, provider outage, fallback policy, rate-limit exhaustion, key rotation, log redaction, deletion/retention jobs and data export. Include automated tests proving sensitive requests never reach a disallowed provider.
