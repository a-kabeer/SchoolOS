# Task Register and Traceability Guide

- **Document ID:** DEL-REG-001
- **Status:** Current register operating guidance
- **Decision maturity:** Proposed operating standard for the delivery baseline
- **Owner:** Delivery lead (named assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9`; task detail restored from the uploaded v2.1 remediation draft and mapped to the current repository

## Canonical registers — one logical task register

- [Hybrid deployment H0–H3](../registers/task-register-hybrid-h0-h3.csv) and [hybrid deployment H4–H7](../registers/task-register-hybrid-h4-h7.csv).
- [AI AI-0–AI-3](../registers/task-register-ai-ai0-ai3.csv) and [AI AI-4–AI-7](../registers/task-register-ai-ai4-ai7.csv).
- [Commercial billing](../registers/task-register-commercial-billing.csv).
- [Business & Customer Platform](../registers/task-register-business-customer-platform.csv).
- [Staging QA](../registers/task-register-staging-qa.csv).
- [Requirement traceability](../registers/requirement-traceability.csv) exports the 36 current rows from document 51. Acceptance-test references are pointers only; they do not assert that tests exist or have passed.
- [Register counts and metadata](../registers/task-register-counts.json).
- [`registers/task-register-counts.json`](../registers/task-register-counts.json) reports the register row totals and ID categories.

The detailed plans are under [`docs/task-plans/`](task-plans/). Existing numbered summaries (documents 17, 25, 28 and 39) remain in place and are not replaced.

## Scope and exclusions

The seven CSV files are partitions of one logical task register covering hybrid deployment, AI, commercial billing, BCP and staging QA. Documents 09, 26, 40 and 53 are summary views only; they do not hold task-level status. Documentation review/sign-off remains in documents 42 and 51. Open decision closure is tracked in document 60.

The registers are the sole task-level status source, but rows are not proof that application features are implemented. All statuses are planning/not-started labels inherited from the source. Never infer completion from inclusion in a register.

## ID preservation rules

1. Exact source task IDs remain unchanged: BILL-001…BILL-605 and BCP-001…BCP-041; BCP-0…BCP-11 work-package IDs also remain unchanged.
2. The hybrid source plan uses work-package IDs H0…H7 but does not assign a unique ID to each checklist item. `H0-01`, `H0-02`, etc. are new register tracking IDs. The `Source ID` column preserves H0…H7.
3. The AI plan similarly uses AI-0…AI-7 package IDs without unique checklist-item IDs. `AI-0-01`, etc. are new tracking IDs, not historical source IDs.
4. The `ID Type` and `Source ID` fields in each register preserve source package/task lineage. Work-package rows are distinguished from source task IDs and newly allocated checklist tracking IDs. Never renumber or reuse a source ID.

## Status and ownership discipline

- A task may become `In Progress` only when implementation work is evidenced.
- A task may become complete/verified only with a PR or commit, relevant tests and reviewer acceptance.
- If a requirement has no appropriate task, record `Unassigned` and create a separately approved planning item; do not attach it to an unrelated task.
- Preserve unknown owners, priorities and dependencies as `Unassigned` or `Not specified in source`; do not invent them.
- Acceptance-test references in the requirement register are traceability labels only. They are not test names, execution evidence or passing results.

## Updating rows

For each task update, retain the stable ID and update status, owner, dependencies, acceptance criteria, test evidence and PR/commit together. Do not duplicate task rows in progress trackers. Use one row per source task. Do not overwrite source wording with a short title. Add new tasks only with an explicit origin and approval reference.
