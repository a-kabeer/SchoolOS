# SchoolOS Repo-Local Agent Skills

This directory contains the curated SchoolOS Tier 1 and Tier 2 agent skill bundle. The skills are flattened for automatic discovery: **every skill lives directly at `.agents/skills/<skill-name>/SKILL.md`**. Companion `references/`, `scripts/`, and `resources/` folders remain inside their owning skill directory.

These skills are supplemental guidance only. Follow the precedence and architecture boundaries in the repository root `AGENTS.md`; a skill never overrides canonical SchoolOS requirements, approved ADRs, or the seven CSV task-register partitions.

## Discovery and layout

- Skill entry point: `.agents/skills/<skill-name>/SKILL.md`
- Companion material: `.agents/skills/<skill-name>/references/`, `scripts/`, or `resources/` where supplied.
- No `tier-1-core/` or `tier-2-workstreams/` parent directories are retained in this installed layout.

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
