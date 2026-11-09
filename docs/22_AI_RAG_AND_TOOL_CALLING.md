# 22 — AI Retrieval-Augmented Generation (RAG) and Tool Calling

## 1. Why both patterns are needed

RAG supplies relevant content from school documents and approved knowledge sources. Tool calling retrieves current structured operational facts or invokes a narrowly approved application action. They solve different problems and must share the same tenant, campus and permission policy.

- RAG: “What does the school policy say about fee concessions?”
- Tool call: “How much overdue fee is there at Campus B for this month?”
- Combined: retrieve a policy and compare it with authorized finance facts, with citations and explicit data freshness.

## 2. Initial storage direction

Use PostgreSQL with `pgvector` as the initial candidate for embeddings and vector retrieval, alongside relational metadata. This avoids adding a separate vector database before scale requires it. Validate extension availability, indexing, backup/restore, query performance and per-tenant isolation in the actual deployment environment. A dedicated vector database can be reconsidered only with measured evidence.

Local and cloud RAG are separate trust boundaries:

- Local RAG indexes approved documents in the relevant Local Data Plane and is available without WAN if its dependencies are local.
- Cloud RAG indexes only content the school has approved for cloud replication/processing.
- Do not assume local indexes or embeddings can be copied to cloud by default.

## 3. Document ingestion pipeline

1. Upload into private, access-controlled document storage.
2. Validate file type, size, malware scanning and tenant/campus ownership.
3. Extract text from supported PDF/DOCX and images; OCR is used where necessary.
4. Detect language/script and preserve page/section references.
5. Normalize, segment and chunk content using tested language-aware rules.
6. Attach metadata: tenant, campus, source ID, document type, version, owner, ACL, classification, page/section and effective dates.
7. Generate embeddings using an approved model and privacy route.
8. Store chunks, vectors and source metadata.
9. Index and mark the document version active.
10. On replacement/deletion/permission change, invalidate or re-index affected chunks and enforce deletion/retention rules.

For Urdu and mixed Urdu/English/Roman Urdu, benchmark extraction, chunking, embeddings and retrieval separately; do not assume English-only quality generalizes.

## 4. Retrieval controls

- Apply tenant and campus filters before similarity ranking, not after generation.
- Enforce document ACLs using the authenticated user's current permissions.
- Return source references (document, page/section, version) with answer claims where possible.
- Show “not found” or low-confidence output instead of inventing policy.
- Keep data freshness and document effective dates visible.
- Prevent one tenant's query, cache, embedding or source reference from exposing another tenant's content.
- Treat retrieved text as untrusted data; it cannot override system/developer policy or authorize tool calls.

## 5. Initial read-only tools

Expose typed application services, not raw SQL. Candidate tools:

- `GetFeeCollectionReport`
- `GetAttendanceReport`
- `GetStudentPerformance`
- `GetCampusComparison`
- `GetOutstandingFees`
- `GetExpenseSummary`

Each tool must define required parameters, permissions, scope, validation, freshness, output schema, row/aggregation limits and audit event. The backend derives tenant and user scope; model-provided tenant/campus IDs are hints at most and must be validated against the user's assignments.

## 6. Later draft/action tools

Potential later tools:

- `CreateWorksheet` (create draft content only)
- `DraftParentNotification`
- `CreateFollowUpTask`
- `DraftFeeReminder`
- `PrepareExamPaper`

Actions use authorized module application services. High-impact actions require an explicit human confirmation and normal approvals. AI must not directly create ledger entries, reverse payments, publish marks, finalize exam outcomes or send consequential messages without the existing permission/approval path.

## 7. Tool execution lifecycle

1. Model proposes a tool and arguments.
2. Server checks whether the tool is allow-listed for this feature and user.
3. Server validates typed arguments and derives trusted scope.
4. Server applies authorization and business validation in the owning module.
5. Read-only tool executes or draft/action is queued for human confirmation.
6. Tool result is minimized and returned to model as untrusted data.
7. Tool call/result and user approval are audited.
8. The model explains the result with date range, filters, sources and caveats.

Never grant model-generated code execution, arbitrary URL fetch, unrestricted SQL or direct DB credentials.

## 8. Preliminary AI schema catalogue

Names are logical proposals; final names and ownership need ADR/ERD review.

- `ai.providers`, `ai.models`, `ai.routing_policies`
- `ai.requests`, `ai.conversations`, `ai.messages`, `ai.jobs`, `ai.results`
- `ai.prompts`, `ai.prompt_versions`
- `ai.tools`, `ai.tool_executions`, `ai.approvals`
- `ai.documents`, `ai.document_versions`, `ai.document_chunks`, `ai.embedding_profiles`
- `ai.usage_records`, `ai.credit_ledger`
- `ai.privacy_policies`, `ai.policy_decisions`
- `ai.evaluation_runs`, `ai.feedback`, `ai.model_versions`

Each table should be reviewed for tenant/campus ownership, ACL requirements, retention, auditability and local-versus-cloud placement. Do not blindly duplicate the business module's student/fee records in AI tables; store references and minimum necessary snapshots only.

## 9. Cache and queue rules

- Include tenant, permission scope, document version, policy and model version in cache identity where relevant.
- Never share cached sensitive responses between users unless authorization equivalence is explicitly proven.
- Long tasks use durable jobs, status, retries, expiry and idempotency.
- Do not re-run a billed action blindly after timeout; reconcile job status first.
- Clear or invalidate indexes/caches after permission revocation or source deletion.
