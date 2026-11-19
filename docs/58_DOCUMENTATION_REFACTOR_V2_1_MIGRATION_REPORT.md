# 58 — Documentation Refactor v2.1 Migration Report

- **Document ID:** GOV-MIGRATION-001
- **Status:** Current
- **Decision maturity:** Not applicable — migration report
- **Owner:** Technical lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)

## 1. Migration objective

Reconcile the uploaded v2.1 remediation package with the current repository documentation baseline without replacing the existing flat `docs/00–53` structure or reintroducing content already merged through PR #2 and PR #3. This is a documentation-only change.

## 2. Verified repository baseline

- PR #1 (Phase 0 foundation) is merged.
- PR #2 (documentation baseline, documents 42–51) is merged.
- PR #3 (OCI staging/automated QA, documents 52–53) is merged.
- The current `main` commit at migration start was `1f84bb022816a91d985c7c0baae77b3a5ca47121`.
- Existing ADR-011 through ADR-021 are retained under `docs/decisions/`; document 13 remains the repository ADR index.

The ZIP's pinned source commit `66e0d35416a7f556ecb2aead4a8580bd3a0ed686` predates PR #2 and PR #3. Its alternate proposed folder hierarchy and migration map were not adopted as a replacement.

## 3. Adopted assets

| Source asset | Repository destination | Disposition |
|---|---|---|
| Documentation standards | `docs/54_DOCUMENTATION_STANDARDS.md` | Adopted, adjusted for current flat structure and gradual metadata compliance |
| Guardian/household requirements | `docs/55_GUARDIAN_AND_HOUSEHOLD_REQUIREMENTS.md` | Adopted as focused companion to docs 43 and 46 |
| Glossary shell | `docs/56_GLOSSARY.md` | Replaced with concise definitions aligned to current docs; no product policy added |
| Offline sync/data exchange specification | `docs/57_OFFLINE_SYNC_AND_MANUAL_DATA_EXCHANGE.md` | Adopted as detailed companion to doc 05; unresolved semantics remain open |
| Four restored task plans | `docs/task-plans/` | Adopted; source package/work-package and task IDs preserved |
| Consolidated task registers | Six workstream CSVs under `registers/task-register-*.csv` | Adopted; 265 source/work-package/tracking rows partitioned by workstream; `ID Type` and `Source ID` preserve lineage without a separate crosswalk file |
| Requirement traceability | `registers/requirement-traceability.csv` | Derived from the current document 51 matrix (36 requirement rows); acceptance-test references are references only, not evidence of execution |
| Task counts | `registers/task-register-counts.json` | Adopted and annotated with current repository baseline and scope |
| ADR, module, runbook and change templates | `docs/templates/` | Adopted as reusable templates, not approved decisions |

## 4. Deliberately not imported

- The ZIP's proposed `docs/00-overview`, `01-product`, `02-architecture`, `03-platform`, `04-ai`, `05-security-operations`, `06-delivery` and `07-reference` hierarchy was not copied wholesale because it duplicates or relocates current numbered documents 00–53.
- Its alternate `docs/INDEX.md` and ADR index were not added as parallel indexes; the root README and `docs/13_ADR_INDEX.md` remain the navigation/decision indexes.
- Its decision register was not imported because current ADR-011–021 records and document 13 already exist.
- Its proposed document register and source-path migration register were not imported because their destinations describe the alternate hierarchy that this migration intentionally does not adopt.
- The ZIP's branch-baseline reconciliation was not copied as current fact: it describes a 9–11 November snapshot and must be re-audited before any branch status is asserted.
- No application code, database schema, migrations, deployment configuration, secrets, workflows or runtime behavior were changed.

## 5. Important open decisions preserved

- Guardian relationship authority, custody, payer rights and communication scope remain open; multiple guardians per student and multiple students/siblings per household remain supported requirements.
- Whether marks can be finalized by a reachable Local Node during WAN loss remains open.
- Whether a Local Node-accepted offline cash receipt is campus-final or remains provisional pending cloud acknowledgement remains open.
- Acceptance-test references carried from document 51 are traceability pointers only; test execution was not verified by this migration.

## 6. Validation performed

- ZIP archive integrity checked with `unzip -t`.
- All 71 entries in the ZIP SHA-256 manifest verified with zero mismatches.
- 62 Markdown files inventoried; 265 register rows checked for unique task/work-package IDs across six workstream CSVs; 36 current document 51 requirement rows exported.
- Task-register source paths remapped to the imported plan locations; detailed work-package plans are stored under `docs/task-plans/`.
- New/updated Markdown relative links and CSV/JSON registers are validated as part of this PR.
- No claim is made that the SchoolOS application features or planned delivery tasks are implemented.

## 7. Review required

Product owner and technical lead should review unresolved decisions in document 60, including guardian authority, offline workflow semantics, legal/privacy controls and commercial policies. This migration report does not approve those product decisions.


## 8. Follow-up governance refactor

The follow-up refactor consolidates duplicate task-plan content, makes seven CSV partitions the task-level status authority, adds requirement-to-task mappings and exact test IDs, standardizes canonical metadata and ADR templates, shortens role-based navigation, and adds GitHub Actions validation. It does not mark tasks or product features implemented. Decision closure and product/technical sign-off remain explicit gates.


## 9. Follow-up reconciliation after PRs #5–#7

This section records the subsequent repository changes; it does not alter the original v2.1 migration results in sections 1–8.

| Merged PR | Follow-up outcome | Relationship to the original migration |
|---|---|---|
| [PR #5 — Documentation governance](https://github.com/a-kabeer/SchoolOS/pull/5) | Consolidated task-plan summaries, established seven canonical task-register partitions, clarified task-status authority, added DOC-60 and documentation validation | Follow-up governance work after this report's original six-partition/265-row snapshot |
| [PR #6 — SaaS tenant and reference adoption](https://github.com/a-kabeer/SchoolOS/pull/6) | Added DOC-61 and DOC-62, SaaS requirements TEN-001–003, SaaS acceptance references and related platform-specification alignment | Added the SaaS tenant proposal and adoption traceability without closing its open decisions |
| [PR #7 — Repo-local agent skills](https://github.com/a-kabeer/SchoolOS/pull/7) | Added 51 skills under `.agents/skills/` and precedence guidance in `AGENTS.md` | Did not change `docs/`, `registers/` or the documentation-quality workflow; the PR description explicitly states that the documentation validator was not run for that diff |

### 9.1 Reconciled counts

At the PR #7 base commit `f614eae6d63cf9d1f4a7d12bfd1dc0e32c0f5102` (post-PR #6 and pre-PR #7), the canonical registers contained 297 task rows across seven CSV partitions and 37 requirement rows. The prior 265 task rows and 36 requirement rows recorded in section 3 remain the correct result of the original v2.1 migration and are not overwritten.

PR #7 then merged as `9d8d27caa65f9bd0a68764671d451495df28df77` without changing `docs/` or `registers/`, so the seven-partition/297-task/37-requirement documentation baseline remained unchanged. The follow-up documentation-arrangement work adds 12 DOCALIGN tasks in an eighth partition. Its branch therefore contains 309 task rows across eight partitions and still 37 requirement rows. The counts JSON and validator are updated together to derive/verify these values from the actual CSV files.

### 9.2 Current navigation and governance

The categorized [documentation catalogue](README.md) is added without moving or renumbering DOC-00–62. The existing focused indexes remain: [ADR index](13_ADR_INDEX.md), [task-plan index](task-plans/README.md), and [register guide](../registers/README.md). The repo-local skill catalogue remains subordinate to `AGENTS.md`, canonical documents and task registers.

The new [Documentation Arrangement and Alignment Task Plan](task-plans/DOCUMENTATION_ALIGNMENT_TASK_PLAN.md) and [DOCALIGN task register](../registers/task-register-documentation-governance.csv) track remaining review and sign-off work. A documentation file or successful structural validator does not approve product policy or prove feature implementation.
