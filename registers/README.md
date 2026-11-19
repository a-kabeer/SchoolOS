# SchoolOS Machine-Readable Registers

- **Status:** Current canonical register guide
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `9d8d27caa65f9bd0a68764671d451495df28df77` (pre-alignment audit baseline)

## Task registers — one logical source of truth

The eight CSV files below are partitions of the same canonical task register. Task IDs are unique across all partitions. Update the applicable CSV row to change task status; progress trackers are summary views only.

| Register | Workstream | Data rows |
|---|---|---:|
| [Hybrid H0–H3](task-register-hybrid-h0-h3.csv) | Hybrid deployment H0–H3 | 48 |
| [Hybrid H4–H7](task-register-hybrid-h4-h7.csv) | Hybrid deployment H4–H7 | 44 |
| [AI AI-0–AI-3](task-register-ai-ai0-ai3.csv) | AI AI-0–AI-3 | 33 |
| [AI AI-4–AI-7](task-register-ai-ai4-ai7.csv) | AI AI-4–AI-7 | 38 |
| [Commercial billing](task-register-commercial-billing.csv) | BILL | 49 |
| [Business & Customer Platform](task-register-business-customer-platform.csv) | BCP | 53 |
| [Staging QA](task-register-staging-qa.csv) | STQA | 32 |
| [Documentation governance](task-register-documentation-governance.csv) | DOCALIGN | 12 |
| **Total** | **All workstreams** | **309** |

## Requirements traceability

[Requirement traceability](requirement-traceability.csv) contains 37 requirements with implementation task IDs or an Unassigned marker, exact acceptance-test IDs, and evidence status. References are not proof of implementation or test execution.

## Authority

- Task-level status: the eight CSV partitions above.
- Requirements and sign-off: document 51 and the requirement CSV.
- Project/workstream summaries: documents 09, 26, 40 and 53.
- Detailed plans: `docs/task-plans/`, including the [documentation alignment plan](../docs/task-plans/DOCUMENTATION_ALIGNMENT_TASK_PLAN.md).
- Open decision status: document 60.

Update the canonical plan and matching CSV row in the same PR. Do not duplicate task statuses in progress trackers.
