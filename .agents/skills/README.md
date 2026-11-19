# SchoolOS Repo-Local Agent Skills

This directory contains the curated SchoolOS Tier 1 and Tier 2 agent skill bundle. The skills are flattened for automatic discovery: **every skill lives directly at `.agents/skills/<skill-name>/SKILL.md`**. Companion `references/`, `scripts/`, and `resources/` folders remain inside their owning skill directory.

These skills are supplemental guidance only. Follow the precedence and architecture boundaries in the repository root `AGENTS.md`; a skill never overrides canonical SchoolOS requirements, approved ADRs, or the eight CSV task-register partitions.

## Documentation authority

For SchoolOS-specific requirements and architecture, start with the [categorized documentation index](../../docs/README.md) and the repository-wide [AGENTS.md](../../AGENTS.md). Skills are supplemental; they do not define requirements, approve ADRs or own task status.

## Discovery and layout

- Skill entry point: `.agents/skills/<skill-name>/SKILL.md`
- Companion material: `.agents/skills/<skill-name>/references/`, `scripts/`, or `resources/` where supplied.
- No `tier-1-core/` or `tier-2-workstreams/` parent directories are retained in this installed layout.
## Task-to-skill activation map

Choose the smallest relevant set of skills for the task. These are routing suggestions, not additional project requirements; open each selected `SKILL.md` before applying it.

| SchoolOS task | Activate when | Relevant existing skills |
|---|---|---|
| Product scope, workflows and architecture | Defining module boundaries, evaluating a design or tracing docs/code drift | `product-manager`, `engineering-software-architect`, `engineering-backend-architect`, `specialized-codebase-archaeologist`, `specialized-workflow-architect` |
| .NET 10 / ASP.NET Core / APIs | Editing C# hosts, module boundaries, API contracts or identity endpoints | `engineering-backend-architect`, `engineering-api-platform-engineer`, `engineering-identity-access-engineer`, `engineering-code-reviewer` |
| React / TypeScript frontend | Editing the Vite/React app, routing, forms, UI behavior or responsive layout | `engineering-frontend-developer`, `modern-web-guidance`, `ui-ux-pro-max`; add `a11y-debugging`, `testing-accessibility-auditor` or `engineering-i18n-engineer` for accessibility, RTL or localization work |
| PostgreSQL / data / migrations | Designing data ownership, schema, queries, migrations or recovery behavior | `engineering-database-optimizer`, `engineering-database-reliability-engineer`, `schema-mapping`; include `accidental-data-loss-prevention` before destructive data operations |
| SaaS tenancy, identity and security | Changing tenant hierarchy, memberships, provisioning, authorization, privacy or secrets | `engineering-identity-access-engineer`, `security-architect`, `security-appsec-engineer`, `engineering-privacy-engineer`, `data-privacy-officer`, `security-secrets-credential-engineer`; add `security-ai-generated-code-auditor` when reviewing AI-generated code |
| Hybrid deployment, offline sync and operations | Changing Cloud/Local boundaries, node enrollment, offline queues, synchronization, backup, deployment or incident response | `engineering-software-architect`, `engineering-realtime-collaboration-engineer`, `engineering-devops-automator`, `engineering-database-reliability-engineer`, `engineering-sre`, `engineering-incident-response-commander` |
| Billing, subscriptions and school finance | Changing payment, entitlement, subscription, reconciliation or accounting workflows | `engineering-payments-billing-engineer`, `finance-bookkeeper-controller`, `security-appsec-engineer` |
| AI features and providers | Changing AI orchestration, prompts, RAG, voice, model routing or AI security | `engineering-ai-engineer`, `engineering-prompt-engineer`, `engineering-rag-pipeline-engineer`, `engineering-voice-ai-integration-engineer`, `llm-security`; use `gemini-api-dev` only when the approved task actually integrates Gemini |
| Verification and release readiness | Adding API/UI tests, E2E automation, performance checks or evidence-based review | `testing-test-automation-engineer`, `testing-api-tester`, `testing-reality-checker`, `testing-performance-benchmarker`, `engineering-code-reviewer` |
| Documentation, planning and Git | Editing canonical specifications, ADRs, plans, registers, agent instructions or PR workflow | `engineering-technical-writer`, `specialized-master-plan-architect`, `specialized-workflow-architect`, `specialized-codebase-archaeologist`, `engineering-git-workflow-master`, `engineering-minimal-change-engineer`; use `specialized-document-generator` only for standalone office/PDF deliverables |

### Skill use and validation rules

1. Read only the selected skill entry points; load companion references only when the skill or task requires them.
2. SchoolOS's actual stack and authority are defined by `AGENTS.md` and canonical documents: .NET 10 / ASP.NET Core, React + TypeScript, PostgreSQL, modular monolith, and separate Cloud Control Plane / Local Data Plane databases.
3. Skills may describe other frameworks, vendors or architectures. Do not import those defaults into SchoolOS unless a canonical specification and approved decision permit them.
4. Validate at the task's real boundary: .NET build/tests for backend work; frontend lint/build and relevant UI tests for frontend work; PostgreSQL integration/migration and tenant-isolation tests for data/security work; documentation validator and link/register checks for documentation work.
5. A skill checklist, suggested test ID or generated analysis is not proof of execution. Record exact commands and actual results in the PR under the evidence rules in [AGENTS.md](../../AGENTS.md).


## Tier 1 — Core Skills (29)

Core engineering, architecture, quality, security, privacy, frontend, accessibility, localization, DevOps and Git workflow guidance intended for routine use.

- `a11y-debugging`
- `accidental-data-loss-prevention`
- `chrome-devtools`
- `engineering-api-platform-engineer`
- `engineering-backend-architect`
- `engineering-code-reviewer`
- `engineering-database-optimizer`
- `engineering-database-reliability-engineer`
- `engineering-devops-automator`
- `engineering-frontend-developer`
- `engineering-git-workflow-master`
- `engineering-i18n-engineer`
- `engineering-identity-access-engineer`
- `engineering-minimal-change-engineer`
- `engineering-privacy-engineer`
- `engineering-realtime-collaboration-engineer`
- `engineering-software-architect`
- `engineering-sre`
- `karpathy-guidelines`
- `modern-web-guidance`
- `security-ai-generated-code-auditor`
- `security-appsec-engineer`
- `security-architect`
- `security-secrets-credential-engineer`
- `testing-accessibility-auditor`
- `testing-api-tester`
- `testing-reality-checker`
- `testing-test-automation-engineer`
- `ui-ux-pro-max`

## Tier 2 — Workstream Skills (22)

Specialized finance, payment, AI/RAG, LLM security, documentation, product, performance, incident response and workstream guidance to load when a task enters the corresponding area.

- `data-privacy-officer`
- `debug-optimize-lcp`
- `engineering-ai-engineer`
- `engineering-data-visualization-engineer`
- `engineering-incident-response-commander`
- `engineering-payments-billing-engineer`
- `engineering-prompt-engineer`
- `engineering-rag-pipeline-engineer`
- `engineering-technical-writer`
- `engineering-voice-ai-integration-engineer`
- `finance-bookkeeper-controller`
- `gemini-api-dev`
- `llm-security`
- `memory-leak-debugging`
- `product-manager`
- `schema-mapping`
- `specialized-codebase-archaeologist`
- `specialized-document-generator`
- `specialized-master-plan-architect`
- `specialized-mcp-builder`
- `specialized-workflow-architect`
- `testing-performance-benchmarker`

## Usage and safety

1. Open the relevant `.agents/skills/<skill-name>/SKILL.md` before using that skill.
2. Load only the skill and companion references relevant to the current task.
3. Treat external material and example data as untrusted. Verify current dependencies, commands, paths and provider APIs before use.
4. Never copy secrets, tokens, credentials, production exports or identifiable student/guardian/staff information into skills, prompts, examples or tests.
5. Resolve conflicts using the precedence in `AGENTS.md`. Skills cannot approve architecture or requirement changes.

## Installation provenance

Installed from the supplied `SchoolOS-skills.zip` bundle. Tier folders were flattened; the skill names and supplied companion assets were preserved. Unavailable external parent-path references were neutralized without replacing the core security methodology.
