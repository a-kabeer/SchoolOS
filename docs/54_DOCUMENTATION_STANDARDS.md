# Documentation Standards
- **Document ID:** GOV-DOCS-001
- **Status:** Current repository standard
- **Decision maturity:** Accepted project documentation standard for this repository
- **Owner:** Technical lead (accountable role; named assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `1f84bb022816a91d985c7c0baae77b3a5ca47121`; reconciled against the current repository documentation baseline and the uploaded v2.1 remediation draft

**Applies to:** All canonical product, engineering, security, AI, operations, commercial, delivery and architecture-decision documents. Templates, indexes, generated manifests, CSV/JSON registers and changelogs have their own metadata rules below.

## 1. Required metadata for canonical documents

New and materially revised canonical Markdown documents MUST begin immediately after their H1 title with all six metadata fields below. Existing documents are brought into compliance during substantive edits; this migration does not perform a broad metadata-only rewrite.

```markdown
# Document Title

- **Document ID:** DOC-AREA-NNN
- **Status:** Draft | Current | Superseded
- **Decision maturity:** Confirmed | Accepted direction | Proposed | Open | Not applicable
- **Owner:** Accountable role; name only when assigned
- **Last reviewed:** YYYY-MM-DD
- **Source baseline:** Repository/ref + full commit SHA, or approved source/ADR reference
```

The field `Owner` identifies one accountable role. When the accountable role has not been established, write `Unassigned (accountable owner TBD)`; do not guess a person. A list of contributing authors may be retained in an additional `Owners` field, but it does not replace `Owner`.

`Source baseline` MUST identify a reproducible source. For this migration, use the current audited base `a-kabeer/SchoolOS main @ 1f84bb022816a91d985c7c0baae77b3a5ca47121` and add source paths where useful. Retain older pinned baselines in historical records when they are required to reproduce an earlier audit. A product-version label such as “Blueprint v1.7” by itself is insufficient. For branch-specific content, name the branch and full SHA and explicitly label it unmerged/WIP. If a canonical document is intentionally source-independent, state `Not applicable — rationale: ...`; do not omit the field.

## 2. ADR-specific metadata

An ADR is a canonical document and MUST contain the same six metadata fields. ADRs also retain their decision-specific `Date`, optional `Owners` attribution, `Context`, `Decision`, `Consequences`, `Alternatives considered` and `Verification` sections.

Example ADR header:

```markdown
# ADR-XXX — Decision title

- **Document ID:** ADR-XXX
- **Status:** Proposed | Accepted | Superseded | Rejected
- **Decision maturity:** Proposed | Accepted direction | Superseded | Rejected
- **Owner:** Technical lead (or named accountable owner)
- **Last reviewed:** YYYY-MM-DD
- **Source baseline:** Repository/ref + full commit SHA
- **Date:** YYYY-MM-DD
- **Owners:** Contributing decision participants (optional attribution)
```

`Status` expresses the lifecycle of the decision record; `Decision maturity` explains whether the direction is proposed, accepted or superseded. Do not infer implementation completion from either field. Existing ADR `Owners` lists are preserved as attribution while `Owner` supplies accountability.

Historical ADR-001 through ADR-010 are index-only summaries in the inspected main baseline. Do not fabricate standalone records or claim missing metadata for files that do not exist. Their index rows must label them `Historical summary only — no source ADR file in pinned baseline`.

## 3. Exceptions: non-canonical and generated artefacts

The following are not canonical Markdown specification documents and are exempt from the seven-line opening header, but MUST be discoverable through the root README, the relevant numbered-document references, or a register guide:

- `docs/templates/**` — contain metadata placeholders and usage instructions, not current requirements.
- `README.md` and `docs/13_ADR_INDEX.md` — navigation indexes; they are exempt from the canonical specification header but must link to the current documents.
- `registers/*.csv`, `registers/*.json` — use stable column/property names and include source baseline in the register guide or adjacent metadata.
- Root `README.md`, `CHANGELOG.md`, `MIGRATION_MAP.md`, `REFACTOR_CHECKLIST.md`, `REFACTOR_REVIEW_NOTES.md` and `FILE_MANIFEST.json` — package artefacts; they identify the package version and pinned baseline in their content.

## 4. Normative language

- **MUST / MUST NOT:** mandatory requirement or prohibition.
- **SHOULD / SHOULD NOT:** strong recommendation; deviations require a reason.
- **MAY:** optional behaviour.
- **PROPOSED:** recommendation awaiting approval.
- **ILLUSTRATIVE:** example only; not a contract or commitment.

Avoid using `will` where the statement is only an intention.

## 5. Separate decision maturity from implementation status

A decision can be accepted while implementation remains `Not Started`. Every feature specification must state both when relevant. Never mark implementation complete because a design, roadmap or ADR exists.

## 6. Canonical ownership and duplication

- Requirements live in product/module specifications and the requirement traceability register.
- Architecture choices live in ADRs and architecture documents.
- API and schema contracts live in their respective canonical specifications.
- Task-level implementation status lives in seven canonical workstream-partitioned CSV files named `registers/task-register-*.csv`. Together they form one logical register; `registers/README.md` and `registers/task-register-counts.json` define membership and counts.
- Progress trackers (documents 09, 26, 40 and 53) are summary views only and MUST NOT duplicate task-level status.
- Risks live in the risk register.
- Unresolved decisions live in `docs/11_OPEN_QUESTIONS.md` and any scoped open-decision register.
- Runbooks describe executable operational procedures.

Other documents should link to the canonical source and summarize only what readers need to understand the local context.

## 7. Evidence and traceability

For claims about implementation, include a repository path, commit/PR, test name/result, migration, release artifact or operational record. The current main baseline is not evidence that a planned feature is implemented. For requirements, use stable IDs such as `STU-REQ-002`, `OFF-REQ-006` or `BCP-REQ-002`. For work packages/tasks, link exact source task IDs or clearly label newly assigned register tracking IDs. For architecture decisions, cite ADR IDs. For risks, cite risk IDs.

Distinguish:

- **Verified fact:** directly supported by code/configuration/tests or an approved decision.
- **Inference:** reasoned interpretation; label it.
- **Recommendation:** proposed future behaviour; label it.

The traceability register must identify missing task/test evidence as `Unassigned`, `Not yet authored` or `Not run`; never use a fabricated test result or task link to make a row appear complete. See [Task Register and Traceability Guide](59_TASK_REGISTER_AND_TRACEABILITY_GUIDE.md), [Decision Closure Register](60_ARCHITECTURE_AND_SECURITY_DECISION_CLOSURE_REGISTER.md) and the [register README](../registers/README.md).

## 8. Links, diagrams and examples

- Use relative Markdown links within the repository.
- Keep diagrams source-controlled (Mermaid or text) and readable without rendering.
- Mark sample IDs, domains, secrets, prices and thresholds as examples.
- Never place real credentials, student records, private keys, tokens or production exports in documentation.
- Every architecture diagram must name trust boundaries and data ownership, not only components.

## 9. Change control

A documentation-only change uses `docs:` Conventional Commit scope. A material architecture, security, data ownership, licensing, commercial or AI governance change requires an ADR or a reviewed amendment to the existing ADR. Update the index and cross-references in the same change.

## 10. Quality checklist

- [ ] Title and all required metadata are present.
- [ ] The document has one clear purpose and audience.
- [ ] Status and decision maturity are explicit and not conflated.
- [ ] Source baseline is reproducible or an explicit not-applicable rationale exists.
- [ ] Requirements and tasks have unique IDs where traceability matters.
- [ ] Proposed values are labelled as proposals.
- [ ] Implementation claims link to code/tests/evidence.
- [ ] No conflicting duplicate rule is introduced.
- [ ] Relative links and heading anchors resolve.
- [ ] Task IDs are unique across partitions.
- [ ] Requirements map to task IDs or explicitly state Unassigned.
- [ ] Acceptance-test references resolve to document 47.
- [ ] Progress trackers do not duplicate task statuses.
- [ ] GitHub Actions documentation validation passes.
- [ ] Sensitive information is absent.
- [ ] Root README, `docs/13_ADR_INDEX.md`, relevant source references and task crosswalk are updated where necessary.
