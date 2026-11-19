# SchoolOS Documentation Arrangement & Alignment — Task Plan

- **Document ID:** GOV-ALIGN-PLAN-001
- **Status:** Current — execution tracked in the canonical register
- **Decision maturity:** Not applicable — delivery plan; no product or architecture decisions are approved here
- **Owner:** Technical lead (named assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `docs/complete-ai-agent-docs-refactor` @ `3cc2a02342f60af124e04aeb86563c331b497a1f` (unmerged documentation update; WIP)

## 1. Purpose and authority

This plan governs the documentation arrangement and alignment workstream following PRs #5–#8. It preserves all existing numbered document IDs and paths, approved product/architecture decisions, the existing skill bundle and the principle that task-level implementation status has one canonical machine-readable source. The post-PR #8 agent-first knowledge/workflow improvements extend this plan; they do not create a new competing plan.

**Task status authority:** `registers/task-register-documentation-governance.csv`. This plan describes scope and acceptance criteria only. Update the CSV row in the same pull request whenever a task status changes. Tasks remain `In Progress` until this change is merged and the final validation evidence is available.

## 2. Re-audit baseline

Audited `main` before this change: `9d8d27caa65f9bd0a68764671d451495df28df77`.

| Merged PR | Verified scope | Arrangement impact |
|---|---|---|
| [PR #5 — documentation governance](https://github.com/a-kabeer/SchoolOS/pull/5) | Consolidated plan summaries, seven task-register partitions, documentation standard and validator | Preserve the flat numbered documents; reconcile remaining count/validation gaps |
| [PR #6 — SaaS tenant and reference adoption](https://github.com/a-kabeer/SchoolOS/pull/6) | DOC-61/DOC-62, SaaS tenant blueprint and related traceability | Surface the new reference and tenant documents in navigation; keep proposed decisions open |
| [PR #7 — repo-local skills](https://github.com/a-kabeer/SchoolOS/pull/7) | 51 skills and precedence guidance in AGENTS.md | Preserve the skills catalogue as supplemental; skills cannot override canonical requirements or task registers |\n| [PR #8 — documentation arrangement](https://github.com/a-kabeer/SchoolOS/pull/8) | Updated documentation index, register alignment, metadata/validator governance and task-plan organization | Re-audit the merged main state; do not assume the earlier branch snapshot remains current |

The pre-change repository had 297 task rows in seven CSV partitions and 37 actual requirement rows. DOC-59 and the register counts JSON still said 36. DEC-021–023 were outside the primary decision table. There was no `docs/README.md`. The validator did not reconcile count metadata, enforce unique canonical document IDs, or scan all key navigation files.

### 2.1 Post-PR #8 audit baseline

The post-merge implementation audit is recorded in [DOC-42 §13](../42_DOCUMENTATION_COMPLETION_AND_GAP_REGISTER.md). It inspected `main` at `3cc2a02342f60af124e04aeb86563c331b497a1f`. The repository now has 309 task rows in eight CSV partitions, 37 requirement rows and 51 repo-local skill entry points. The numbered `docs/00–62` structure, flat skill-discovery layout and canonical workstream plans are valid and remain in place.

The audit justifies targeted improvements rather than a mass move: publish the seven-source knowledge map, add one ordered agent execution protocol to `AGENTS.md`, map the existing skills to task activation conditions, clarify DOC-08 as the phase-level master implementation plan, and keep DOC-09/26/40/53 summary-only. The source remains a foundation scaffold; this documentation audit does not authorize runtime or schema changes.

Historical DOC-58 figures (265 tasks / 36 requirements) describe its earlier migration snapshot and must remain unchanged as historical evidence. DOC-42's original implementation inventory is also a historical snapshot and must be supplemented, not rewritten as if it were performed against the current commit.

## 3. Target arrangement — no mass relocation

```text
SchoolOS/
├── README.md                         # Project overview and reader entry point
├── AGENTS.md                         # Repository-wide contributor/agent authority
├── .agents/skills/README.md          # Supplemental skill catalogue
├── docs/
│   ├── README.md                     # Categorized canonical documentation index
│   ├── 00–62 numbered documents      # Preserve every existing ID and path
│   ├── decisions/                    # ADR records
│   ├── task-plans/                   # Canonical detailed delivery plans
│   └── templates/                    # Reusable document templates
├── registers/                        # Canonical task and requirement registers
├── scripts/                          # Documentation validation
└── .github/workflows/                # Documentation quality CI
```

The root README remains the project entry point. `docs/README.md` is the detailed catalogue. `docs/13_ADR_INDEX.md`, `docs/task-plans/README.md` and `registers/README.md` retain their focused index responsibilities. AGENTS.md remains authoritative; repo-local skills remain subordinate. Do not add a competing index or task-status tracker.

## 4. Canonical task plan

The following IDs are registered in the eighth task-register partition. All are tracked as `In Progress` until merge and final validation.

| ID | Priority | Task | Primary scope | Dependencies | Acceptance criteria |
|---|---|---|---|---|---|
| DOCALIGN-001 | P0 | Establish documentation-governance workstream | New CSV partition; register guides; counts; authority references | None | Add the eighth partition with the same ten-column schema; register DOCALIGN-001–012; update every “seven partitions” authority reference; validate 309 unique task rows. |
| DOCALIGN-002 | P0 | Normalize DOC-60 decision closure table | DOC-60 | DOCALIGN-001 | DEC-001–023 appear once in one six-column primary table. Keep the DOC-62 proposal comparison separate. Preserve all decision states as open/external verification. |
| DOCALIGN-003 | P0 | Reconcile requirement counts | DOC-59; register README; counts JSON | DOCALIGN-001 | All current summaries report 37 requirement rows, calculated from the CSV. Preserve DOC-58's historical 36-row figure. |
| DOCALIGN-004 | P1 | Add categorized documentation index | New docs/README.md; root README; DOC-54; validator | DOCALIGN-001 | Every numbered document and supporting index/folder is discoverable; links resolve; no file is moved or renumbered. |
| DOCALIGN-005 | P1 | Reconcile DOC-42 to post-merge state | DOC-42 | DOCALIGN-002/003 | Add a dated current reconciliation; separate existence, review, approval, implementation and test evidence; retain the older repository snapshot. |
| DOCALIGN-006 | P1 | Complete DOC-58 migration history | DOC-58 | DOCALIGN-003/005 | Add a follow-up for PRs #5–#7 and current counts (297 previous tasks / 37 previous requirements); preserve original 265/36 migration figures. |
| DOCALIGN-007 | P1 | Align README and focused indexes | Root README; DOC-13; task-plan and register indexes | DOCALIGN-004 | Root README links docs index; reading paths include DOC-61/62 where relevant; remove repeated commercial narrative; cross-links stay canonical. |
| DOCALIGN-008 | P1 | Standardize source baseline and lifecycle semantics | DOC-54; touched docs and register metadata; AGENTS.md; skills guide | DOCALIGN-005/006 | Define baseline as the reproducible source used for authoring/review; preserve historical baselines; distinguish document status, decision maturity and implementation evidence. |
| DOCALIGN-009 | P1 | Expand automated validation | Validator and GitHub Actions workflow | DOCALIGN-003/004/008 | Validate all eight partitions, per-file and total counts, 37 requirements, unique canonical Document IDs, links in key navigation files, ADR coverage and DOC-60 table structure. Fail CI on violations. |
| DOCALIGN-010 | P1 | Align repo-local skill navigation and activation guidance | docs index; skills guide; AGENTS.md | DOCALIGN-004 | Link one skills catalogue as supplemental guidance; map existing skill names to task triggers for architecture, .NET, React, PostgreSQL, SaaS tenancy, security, testing and documentation; retain all 51 skills and PR #7 precedence. |
| DOCALIGN-011 | P1 | Reconcile SaaS traceability | DOC-51, requirement CSV, DOC-47 and DOC-62 | DOCALIGN-003/009 | TEN-001–003 and SAAS-001–014 references remain consistent. Do not imply tests ran or close DEC-001/004/008/020 through documentation edits. |
| DOCALIGN-012 | P0 | Complete quality review and sign-off gate | All touched files, post-PR #8 audit, agent workflow, skills index and GitHub Actions | DOCALIGN-002–011 | DOC-42 records the post-PR #8 audit; seven-source map and ordered execution protocol are discoverable; skill activation guidance preserves all 51 skills; validator passes on the PR branch; links/counts reconcile; no duplicate authority, runtime/schema or unresolved product policy changed. |

## 5. Execution sequence

### Phase A — Tracking and structural correctness
1. DOCALIGN-001 establishes the eighth partition and the single task-status authority.
2. DOCALIGN-002 repairs the primary decision table.
3. DOCALIGN-003 reconciles current task/requirement counts.

**Exit gate:** One primary six-column decision table; eight task partitions; 309 unique task rows; 37 current requirements; historical counts preserved.

### Phase B — Navigation and historical reconciliation
4. DOCALIGN-004 creates the categorized catalogue.
5. DOCALIGN-005 and DOCALIGN-006 append current-state reconciliation to DOC-42 and DOC-58.
6. DOCALIGN-007 and DOCALIGN-008 align the README, indexes, metadata and lifecycle conventions.

Publish the seven-source map in the catalogue, clarify DOC-08 as the master roadmap, retain focused indexes and preserve historical/current audit baselines.\n\n**Exit gate:** Every canonical document is discoverable; historical/current baselines are distinct; agents can locate the primary authority for each knowledge area.

### Phase C — Automated enforcement and final review
7. DOCALIGN-009 expands the validator and CI path filters.
8. DOCALIGN-010 and DOCALIGN-011 align task-scoped skill activation and SaaS traceability.\n9. Add the ordered agent execution protocol to AGENTS.md and align it with the seven-source map.
9. DOCALIGN-012 closes only after the PR validation and owner review.

**Exit gate:** The validator enforces the same rules described by DOC-54; no open decision is silently approved.

## 6. Non-goals and safeguards

- Do not move or renumber DOC-00 through DOC-62.
- Do not create another ADR index, document index, task tracker or status table.
- Do not rewrite historical audit/migration evidence as current.
- Do not mark draft specifications as approved merely because they exist.
- Do not close DEC-001, DEC-004, DEC-008, DEC-010, DEC-020 or any other open decision through formatting/navigation work.
- Do not claim SaaS tenancy, RLS, provisioning, authentication or Local Node licensing is implemented without code, migration, test and operational evidence.
- Do not let `.agents/skills/` change SchoolOS architecture or documentation authority.

## 7. Definition of done

- Canonical indexes are clear and non-competing; links resolve.
- Current counts match actual source registers (8 partitions, 309 task rows, 37 requirement rows).
- Historical DOC-58 and DOC-42 snapshots remain traceable.
- DOC-60 has one consistent DEC-001–023 primary table and retains open states.
- The canonical task register remains the only task-status authority.
- DOC-54 and the validator enforce the same governance rules.
- Documentation-quality CI passes after the changes.
- Product owner and technical lead sign off the resulting documentation baseline.

## 8. References

- [SchoolOS main](https://github.com/a-kabeer/SchoolOS)
- [PR #5 — Documentation governance](https://github.com/a-kabeer/SchoolOS/pull/5)
- [PR #6 — SaaS tenant and reference adoption](https://github.com/a-kabeer/SchoolOS/pull/6)
- [PR #7 — Repo-local agent skills](https://github.com/a-kabeer/SchoolOS/pull/7)
- [DOC-54 — Documentation Standards](../54_DOCUMENTATION_STANDARDS.md)
- [DOC-59 — Task Register and Traceability Guide](../59_TASK_REGISTER_AND_TRACEABILITY_GUIDE.md)
- [DOC-60 — Architecture and Security Decision Closure Register](../60_ARCHITECTURE_AND_SECURITY_DECISION_CLOSURE_REGISTER.md)
