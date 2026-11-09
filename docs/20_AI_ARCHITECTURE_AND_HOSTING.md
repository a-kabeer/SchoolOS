# 20 — SchoolOS AI Architecture and Hosting Strategy

**Status:** Architecture direction accepted; detailed provider, contract, model and deployment choices remain open.

## 1. Purpose and product boundary

SchoolOS AI is an intelligence and assistance layer over the school's authorized operational data. It is not a separate source of truth and does not own student, finance, attendance, examination or HR records. Those remain owned by their business modules and databases.

The AI layer may explain, summarize, retrieve, recommend, generate drafts and call approved application capabilities. It must not bypass the existing permission model, write directly to business tables, approve financial postings, publish marks or make consequential student decisions without the relevant human workflow.

## 2. Confirmed AI product decisions

| Decision | Selected direction | Meaning |
|---|---|---|
| AI privacy | Hybrid | External APIs may process permitted/minimized data; sensitive workloads use private or local processing by policy. |
| Offline AI | Optional premium add-on | Core school operations must work without AI. Local inference is an optional deployment capability, not a prerequisite. |
| AI pricing | Hybrid | A limited amount of AI is included in the standard subscription; advanced AI is a paid add-on with metered usage/credits. |
| Product capabilities | Six initial capabilities | Auditor, Learning Recovery, Examination Intelligence, Business Copilot, Urdu Voice and Student Early Intervention. |

These are product-direction decisions. They do not select a specific AI vendor, model, GPU, voice service, embedding model or inference framework.

## 3. Three supported AI hosting environments

### 3.1 Cloud AI (default starting point)

- SchoolOS backend routes approved requests to contracted external model APIs through the Provider Gateway.
- API credentials remain server-side; React clients never receive provider secrets.
- Requests are minimized, privacy-classified and scoped to the authenticated tenant/campus/user.
- Central usage, cost, latency, errors and provider health are measured.
- The school must be told which data categories may leave its environment and which provider terms apply.
- When internet or provider service is unavailable, AI jobs may queue or fail gracefully; core school operations continue.

### 3.2 Private AI infrastructure (premium)

- Inference runs on dedicated or logically isolated infrastructure operated for SchoolOS or a customer.
- Useful for schools/groups requiring stronger isolation, custom retention terms or controlled model deployments.
- The contract, threat model, access controls, backups, logging, upgrades and incident responsibilities must be defined before sale.
- “Private” does not automatically mean zero retention or perfect confidentiality; operational access and vendor/subprocessor controls still need review.

### 3.3 Campus Local AI (optional premium)

- A local inference runtime can be deployed beside the campus Local Data Plane.
- Selected requests can run on-premises when WAN connectivity is unavailable, subject to installed model capabilities and available hardware.
- Local RAG may use campus-local approved documents and data.
- Model size, GPU/CPU/RAM, latency, update process, security patching and support requirements must be tested on target customer hardware.
- Local AI is not assumed to be installed on every node. If absent, AI can be unavailable or queued while ordinary school workflows remain available.

**Candidate technologies only:** Ollama can support simple local experimentation; vLLM may be evaluated for higher-throughput GPU serving. These are not committed implementation choices.

## 4. High-level architecture

```text
React + TypeScript clients
        |
        v
ASP.NET Core application / authenticated user context
        |
        +--> Business modules (Finance, Students, Academics, Exams, etc.)
        |        |  approved application services / reporting projections
        |        v
        +--> SchoolOS AI Module
                 |
                 +--> AI Orchestrator and feature workflows
                 +--> Privacy classification and policy decision
                 +--> Tool Calling (allow-listed application functions)
                 +--> Retrieval / RAG (tenant- and ACL-filtered)
                 +--> Usage, credit ledger, audit and evaluation
                 |
                 v
          AI Provider Gateway
             |        |         |
          Cloud API  Private   Campus Local Runtime
```

AI orchestration and business policy remain within the Modular Monolith initially. Model inference may run in a separate process/container/service where required for resource isolation. This does not require splitting the business application into microservices.

## 5. Provider Gateway responsibility

Create a SchoolOS-owned provider abstraction (working name: `SchoolOS.AI.ProviderGateway`). It should:

- Route requests by task, model capability, tenant policy, data classification and hosting availability.
- Select only providers approved for the relevant data class and region/contract.
- Apply tenant budgets, quotas, usage metering and request limits.
- Normalize provider-specific request/response and error handling.
- Support retries and fallback only to providers that satisfy the same privacy, capability and policy requirements.
- Record provider/model/version, policy decision, usage and latency without unnecessarily copying sensitive payloads into logs.
- Support provider health checks, circuit breakers and configurable timeouts.

`Microsoft.Extensions.AI` may be evaluated as a .NET integration abstraction. It does not replace SchoolOS routing, privacy, tenant, billing or audit logic.

## 6. Request lifecycle

1. User authenticates to SchoolOS.
2. Backend derives tenant, campus, role and permissions from trusted server-side identity and assignments.
3. Feature service builds a typed request; user-supplied tenant/campus IDs are never accepted as authority.
4. Policy engine classifies the data and determines allowed hosting routes, redaction and consent/approval requirements.
5. Context is retrieved through permission-checked application services and/or RAG.
6. Gateway selects a compatible provider/runtime and enforces quota and budget.
7. Output is validated, optionally grounded with sources and checked for required human approval.
8. Result is returned or queued; audit, usage and evaluation metadata are recorded.
9. Any consequential action is executed only through the owning module's authorized application service and its normal workflow.

## 7. Failure and offline behaviour

- No AI capability is a dependency for attendance, fee collection, admissions, timetables, exams or other core school operations.
- If cloud AI is unavailable, show a clear unavailable/queued state and preserve the user's original workflow.
- Local AI can satisfy only the capabilities and data policies supported by the locally installed model and retrieval index.
- Do not silently send a sensitive local request to a cloud provider as a fallback.
- Use bounded queues, idempotency keys and cancellation/expiry policies for long-running document or paper jobs.

## 8. Initial rollout recommendation

Start with a controlled cloud API gateway for low-risk, permitted tasks and a small set of read-only Copilot/Auditor capabilities after reporting and access controls are mature. Keep sensitive data local/private by default. Evaluate private hosting and campus local inference after pilot workload, hardware and cost evidence exists. The previously stated overall infrastructure budget of USD 0–5/month is not compatible with assuming dedicated GPU inference; API use should be tightly capped, while local GPU inference remains an optional customer-funded capability.

## 9. Non-goals for the first AI release

- Training a foundation model from school data.
- Giving an LLM direct database credentials or unrestricted SQL access.
- Automatic financial postings, payment reversals or mark publication.
- Automatic student diagnosis, punishment or high-stakes decisions.
- Guaranteeing offline voice or image processing on a node without the relevant local models and hardware.

## 10. Acceptance criteria for architecture readiness

- The same feature can be routed to approved cloud/private/local providers without moving business rules into provider adapters.
- Tenant/campus scope is enforced before context retrieval and tool execution.
- Sensitive requests cannot fall back to a less-private provider.
- Provider secrets never reach browser clients.
- Usage, cost/credits, latency, errors and policy decisions are auditable.
- AI outage does not stop core school workflows.
- Human approval gates are tested for all consequential actions.
