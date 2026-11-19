# Machine-readable registers

- **Status:** Current migration snapshot for review; not a substitute for canonical product specifications or signed-off trackers.
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `1f84bb022816a91d985c7c0baae77b3a5ca47121`
- **Source package:** SchoolOS Documentation Refactor v2.1 Remediation Draft

## Task registers

| Register | Rows (excluding header) |
|---|---:|
| [Hybrid H0–H3](task-register-hybrid-h0-h3.csv) | 48 |
| [Hybrid H4–H7](task-register-hybrid-h4-h7.csv) | 44 |
| [AI AI-0–AI-3](task-register-ai-ai0-ai3.csv) | 33 |
| [AI AI-4–AI-7](task-register-ai-ai4-ai7.csv) | 38 |
| [Commercial billing](task-register-commercial-billing.csv) | 49 |
| [Business & Customer Platform](task-register-business-customer-platform.csv) | 53 |
| **Total** | **265** |

Each task row has a stable `Task ID`, a `Source ID` and an `ID Type`. These columns preserve the distinction between source work packages, source task IDs and newly allocated checklist tracking IDs. A separate crosswalk file is intentionally not maintained, because lineage is kept alongside each task row.

## Requirements

[Requirement traceability](requirement-traceability.csv) contains the 36 requirement rows exported from the current document 51 sign-off matrix. Acceptance-test references are references from the matrix only; they do not show test implementation or execution.

## Authority and update rules

- Canonical requirements remain in the numbered specifications and requirement sign-off document.
- Project-level phase tracking remains in [document 09](../docs/09_PROGRESS_TRACKER.md).
- OCI staging/automated-QA progress remains in [document 53](../docs/53_STAGING_QA_PROGRESS_TRACKER.md).
- Task CSVs are planning registers. Do not mark work complete without code/PR, test and reviewer evidence.
- When a source task is changed, update its canonical work-package document and corresponding row in the same PR.
