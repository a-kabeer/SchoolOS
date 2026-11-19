# 53 — Staging Deployment and Automated QA Progress Tracker

- **Canonical runbook:** [52 — OCI Staging Deployment and Automated QA](52_OCI_STAGING_AND_AUTOMATED_QA_RUNBOOK.md)
- **Canonical task register:** [Staging QA task register](../registers/task-register-staging-qa.csv)
- **Scope:** OCI developer/pilot staging only; production Local Node operations remain in document 48.

## Overall status

Repository audit is recorded as completed. Staging deployment, CI/CD, Playwright and optional AI-assisted QA are not yet implemented. Detailed status for all 32 STQA tasks is maintained only in the canonical CSV register.

## Current blockers

1. The audited main-branch React app is still the Phase 0 shell.
2. The audited Cloud API and Local API expose basic health endpoints; dependency-aware readiness is not established.
3. Login, school creation, campus management and role/permission workflows are not present in the audited baseline.
4. Application Dockerfiles, staging Compose, CI/deployment workflows and the Playwright E2E project are prerequisites.
5. OCI tenancy, region, capacity, hostname and protected deployment credentials require environment-owner verification.

Do not create placeholder tests that claim unavailable product workflows have passed.

## Workstream map

| Phase | Workstream | Task IDs |
|---|---|---|
| 0 | Audit and scope | STQA-001–STQA-003 |
| 1 | App readiness and containerization | STQA-004–STQA-008 |
| 2 | OCI infrastructure and host security | STQA-009–STQA-013 |
| 3 | Staging environment | STQA-014–STQA-015 |
| 4 | GitHub CI/CD | STQA-016–STQA-019 |
| 5 | Playwright and reporting | STQA-020–STQA-026 |
| 6 | Optional Gemini/MCP analysis | STQA-027–STQA-029 |
| 7 | Operations and recovery | STQA-030–STQA-031 |
| 8 | End-to-end acceptance | STQA-032 |

## Completion rules

- The CSV register is the only task-level status source.
- Mark a task complete only with acceptance evidence, PR/commit and reviewer.
- A workflow file existing is not proof the workflow works.
- A green build is not proof that login, school creation, authorization or tenant isolation works.
- Gemini analysis is optional and never overrides deterministic Playwright results.
- Keep this tracker summary-only; do not copy task statuses here.
