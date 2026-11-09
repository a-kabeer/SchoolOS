# 26 — AI Progress Tracker

**Baseline date:** 9 November 2026  
**Overall AI status:** Planning / Not Started. No AI implementation completion is implied by this tracker.

## Decision status

| Area | Current direction | Status |
|---|---|---|
| Hosting | Hybrid: Cloud AI + Private AI + optional Campus Local AI | Accepted direction |
| Privacy | Hybrid: permitted/minimized external API use; sensitive workloads private/local | Accepted direction |
| Offline inference | Optional premium capability | Accepted direction |
| Pricing | Limited AI included + advanced paid add-on; private/local premium | Accepted direction |
| Provider/vendor | Not selected | Open |
| Models/embeddings/STT/TTS | Not selected | Open |
| Final data classification/legal wording | Draft baseline only | Open |
| Numeric quotas and prices | Not set | Open |

## Workstream tracker

| Workstream | Status | Dependency | Completion evidence |
|---|---|---|---|
| AI-0 Discovery and privacy | Not Started | Product discovery | Approved requirements, data-flow and provider assessment |
| AI-1 Provider Gateway | Not Started | AI-0 | Gateway tests, routing policy, usage ledger and privacy tests |
| AI-2 Tool Calling | Not Started | AI-1 + business APIs | Read-only tool tests and authorization audit |
| AI-3 RAG | Not Started | AI-1 + document storage | ACL, citation, deletion and retrieval evaluation |
| AI-4 Auditor + Copilot | Not Started | Finance/reporting maturity | Human-reviewed pilot and quality metrics |
| AI-5 Learning + Examination | Not Started | Curriculum/exam data quality | Teacher acceptance, rubric and subject evaluation |
| AI-6 Voice + Intervention | Not Started | Identity, data and safety policy | Language/fairness/safety evaluation |
| AI-7 Private/Local AI | Not Started | Gateway and hybrid node | Supported hardware benchmark and offline tests |

## Weekly record

| Date | Contributor | Completed | In progress | Blockers / decisions | Evidence |
|---|---|---|---|---|---|
| | | | | | |

## Change control

Any change to AI hosting, privacy class, external provider, data retention, training use, credit metering or human approval policy should be recorded in the relevant ADR and this tracker. A feature is not `Completed` until code, tests, privacy review, documentation and acceptance evidence are all present.
