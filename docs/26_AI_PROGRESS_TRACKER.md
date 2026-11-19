# 26 — AI Progress Tracker

- **Document ID:** DOC-26
- **Status:** Current
- **Decision maturity:** Not applicable — progress tracker
- **Owner:** Delivery lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)
- **Canonical task plan:** [AI Implementation Plan](task-plans/AI_IMPLEMENTATION_TASK_PLAN.md)
- **Canonical task registers:** [AI-0–AI-3](../registers/task-register-ai-ai0-ai3.csv) and [AI-4–AI-7](../registers/task-register-ai-ai4-ai7.csv)
- **Related decisions:** ADR-015 through ADR-018

## Overall status

**Planning / Not Started.** This tracker records product decisions and dependencies. Task-level status exists only in the canonical AI CSV partitions.

## Decision status

| Area | Current direction | Decision state |
|---|---|---|
| Hosting | Cloud AI, Private AI and optional Campus Local AI | Accepted direction |
| Privacy | Minimized external API use; sensitive workloads private/local by policy | Accepted direction |
| Offline inference | Optional premium capability | Accepted direction |
| Pricing | Limited included AI, advanced paid add-on and premium private/local hosting | Accepted direction |
| Provider/vendor | Not selected | Open |
| Models, embeddings, speech-to-text and text-to-speech | Not selected | Open |
| Final data classification and legal wording | Draft baseline | Open / external review |
| Numeric quotas and prices | Not set | Open |

## Delivery dependencies

AI release depends on tenant/campus authorization, reliable business data and reporting projections, approved provider terms, privacy classification, retention/deletion policy, Urdu/English/Roman Urdu evaluation sets, human approval controls and cost limits.

## Change control

Update decision maturity only when the relevant ADR and affected specification change. Update task status only in the canonical AI CSV register with PR/commit, test and reviewer evidence.
