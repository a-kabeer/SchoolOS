# 24 — AI Pricing, Entitlements and Usage Metering

**Status:** Pricing architecture selected; numeric prices, included quotas and credit weights are not finalized.

## 1. Confirmed commercial direction

Use hybrid AI pricing:

- A limited amount of useful AI is included in the standard SchoolOS subscription.
- Advanced AI features and higher usage are sold as a paid add-on.
- Dedicated/private AI infrastructure and optional campus-local inference are premium deployment options with separate infrastructure/support costs.

Do not promise unlimited paper checking or unlimited voice/image processing inside the base subscription.

## 2. Product packaging proposal

### Standard subscription (limited AI allowance)
Potentially include basic Copilot questions, policy Q&A and limited report explanations/worksheet generation. Exact included features and quota must be tested against unit economics and customer value.

### AI Pro add-on
Potentially include advanced Auditor, Learning Recovery, advanced exam-paper generation, a defined paper-checking allowance, advanced analytics and higher monthly credits.

### Private/Local AI premium
Dedicated inference, local runtime deployment, local model updates, hardware sizing, support and maintenance. Price separately because workload and hardware costs vary materially.

These are packaging recommendations, not approved rate cards.

## 3. Metering dimensions

| Workload | Candidate usage unit | Notes |
|---|---|---|
| Text generation/Copilot | Input and output tokens or calibrated credits | Record model-specific token usage and provider charges. |
| Document analysis/RAG | Pages processed, extracted text/tokens, embeddings | Separate one-time indexing from repeated inference. |
| Paper checking | Pages/images plus processing complexity | Calibrate by subject, handwriting quality and retries; teacher review remains included in workflow. |
| Voice | Audio minutes and text/model usage | Track STT, LLM and TTS components separately. |
| Embeddings | Tokens processed and vector storage | Account for re-indexing and retained versions. |
| Local inference | Allocated hardware, operations and support tier | Metering may be subscription/site-based rather than per token. |

A credit system can simplify customer presentation, but retain underlying provider units and actual cost for financial control.

## 4. Usage controls

- Per-tenant monthly budgets and feature entitlements.
- Per-user/per-feature rate limits where appropriate.
- Admin-configurable spend caps.
- Alerts at proposed 50%, 80% and 100% usage thresholds (thresholds configurable).
- No surprise overage charges; require an explicit opt-in for overages or pause/limit AI at cap.
- Maintain an append-only credit/usage ledger with adjustments and refunds traceable.
- Show estimated usage before expensive batch jobs where feasible.
- Prevent duplicate billing on retries through request/job idempotency.

## 5. Credit exhaustion behaviour

When an AI entitlement or credit allowance is exhausted, pause or restrict only AI features according to plan policy. Do not block admissions, attendance, fee collection, financial records, examination workflow or access to existing school data. Present a clear reason and an admin path to upgrade/top up.

## 6. Cost management

- Use cheaper/smaller models for suitable low-risk tasks only after quality evaluation.
- Set maximum input/output sizes, page counts, audio duration and concurrency.
- Cache only where privacy and freshness rules permit.
- Batch embeddings and document processing safely.
- Track gross margin by tenant, feature, provider and model.
- Evaluate actual usage in the pilot before final quotas/pricing.

## 7. Open commercial decisions

- Included credits by plan and reset schedule.
- AI Pro price and whether billing is per school, campus or student band.
- Whether paper-checking allowance is per page, per script or weighted credit.
- Overage purchase flow and local payment methods.
- Private AI setup, minimum term, support SLA and hardware ownership.
- Treatment of unused credits and failed/abandoned jobs.
