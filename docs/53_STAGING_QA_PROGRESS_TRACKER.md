# 53 — Staging Deployment and Automated QA Progress Tracker

**Version:** 1.0  
**Prepared:** 19 November 2026  
**Companion runbook:** [52 — OCI Staging Deployment and Automated QA Runbook](52_OCI_STAGING_AND_AUTOMATED_QA_RUNBOOK.md)  
**Scope:** OCI development/pilot staging and automated QA only. Hybrid production Local Node tasks remain in document 17.

## 1. Status definitions

- **Not Started** — no implementation work has been verified.
- **In Progress** — work has started; acceptance evidence is incomplete.
- **Blocked** — a prerequisite is missing; record the dependency.
- **In Review** — implementation exists and awaits verification/review.
- **Completed** — acceptance criteria passed and evidence is linked.

## 2. Repository baseline checkpoint

| Item | Audited state on 19 November 2026 |
|---|---|
| Repository | `a-kabeer/SchoolOS` |
| Audited branch | `main` |
| .NET solution and API hosts | Present |
| React/Vite frontend | Present; Phase 0 shell |
| Local PostgreSQL Compose | Present; development-only configuration |
| .NET unit/architecture test projects | Present |
| Application Dockerfiles | Not found |
| Staging Compose stack | Not found |
| GitHub Actions workflows | Not found |
| Playwright E2E project | Not found |
| Login and school creation flows in audited main | Not found; feature tests are Blocked pending implementation |

The repository audit is complete. This does not mean deployment, CI/CD, a QA runner or AI integration has been completed.

## 3. Task tracker

| Task ID | Phase | Task | Priority | Status | Dependencies | Acceptance evidence |
|---|---|---|---|---|---|---|
| STQA-001 | 0 — Audit | Inspect repository structure, docs, Docker config, tests and app readiness | Critical | Completed | None | Main tree and relevant files reviewed; findings in document 52 |
| STQA-002 | 0 — Scope | Confirm cloud-hosted preview staging is separate from production Local Nodes | Critical | Not Started | Product owner confirmation | Environment boundary recorded |
| STQA-003 | 0 — Readiness | Confirm staging Cloud API/frontend scope and smoke-test endpoints | Critical | In Progress | STQA-001, STQA-002 | Accepted service diagram and endpoint list |
| STQA-004 | 1 — App readiness | Implement or identify working authentication, school create/list/detail and role permissions | Critical | Blocked | Product feature implementation | Verified UI/API workflows and tests |
| STQA-005 | 1 — Containerization | Add and verify ARM64-compatible .NET Cloud API Dockerfile | Critical | Not Started | STQA-003 | Image builds and starts on ARM64 |
| STQA-006 | 1 — Containerization | Add frontend Dockerfile and reviewed static-serving configuration | High | Not Started | STQA-003 | Image builds and frontend loads |
| STQA-007 | 1 — Containerization | Add separate staging Compose stack; keep local Compose unchanged | Critical | Not Started | STQA-005, STQA-006 | Compose config validates; services start |
| STQA-008 | 1 — Readiness | Implement dependency-aware readiness checks and migration status | High | Not Started | STQA-005, STQA-007 | Liveness/readiness tests pass |
| STQA-009 | 2 — OCI | Verify OCI account, home region, quota, free eligibility and cost boundaries | Critical | Not Started | STQA-002 | OCI Console evidence, no secrets |
| STQA-010 | 2 — OCI | Provision Ubuntu 24.04 ARM64 A1 Flex VM with approved allocation | Critical | Not Started | STQA-009 | Instance running; shape/architecture/limits recorded |
| STQA-011 | 2 — Security | Configure SSH key access, restricted ingress and least-privilege deploy user | Critical | Not Started | STQA-010 | SSH verification and hardening record |
| STQA-012 | 2 — Security | Configure OCI network rules/UFW; keep PostgreSQL private | Critical | Not Started | STQA-010 | Network checks show only approved access |
| STQA-013 | 2 — Host | Install Docker Engine/Compose and apply host baseline | High | Not Started | STQA-010, STQA-011 | Version checks and service status |
| STQA-014 | 3 — Staging | Configure staging-only secrets, PostgreSQL volume and synthetic seed data | Critical | Not Started | STQA-007, STQA-012, STQA-013 | Isolated staging DB; no production data |
| STQA-015 | 3 — Staging | Configure DNS, reverse proxy and HTTPS | High | Not Started | STQA-010, STQA-012 | Valid HTTPS certificate and redirect |
| STQA-016 | 4 — GitHub | Create protected staging Environment and configure deployment variables/secrets | Critical | Not Started | STQA-011, STQA-015 | Secret names configured; values never logged |
| STQA-017 | 4 — CI | Add PR CI for .NET restore/build/test and frontend lint/build | Critical | Not Started | STQA-001 | Successful PR check; branch protection uses checks |
| STQA-018 | 4 — Deploy | Add main deployment workflow and publish immutable ARM64 image tags | Critical | Not Started | STQA-005, STQA-006, STQA-013, STQA-016, STQA-017 | Workflow deploys image tagged with exact commit SHA |
| STQA-019 | 4 — Deploy | Deploy staging Compose stack, apply migration/backup gate and verify health | Critical | Not Started | STQA-007, STQA-008, STQA-014, STQA-015, STQA-018 | Deployed SHA matches workflow SHA; readiness evidence |
| STQA-020 | 5 — Playwright | Create isolated Playwright E2E project under qa/e2e | High | Not Started | STQA-001 | Lockfile, config and Chromium installation committed |
| STQA-021 | 5 — Playwright | Add landing-page and current API health smoke tests | Critical | Not Started | STQA-019, STQA-020 | Repeatable passing CI report |
| STQA-022 | 5 — Playwright | Add authenticated login/logout tests | Critical | Blocked | STQA-004, STQA-016, STQA-020 | QA-only account and test evidence |
| STQA-023 | 5 — Playwright | Add school create/list/detail and validation tests | Critical | Blocked | STQA-004, STQA-016, STQA-020 | Unique synthetic school verified and cleaned |
| STQA-024 | 5 — Playwright | Add campus and role/permission regression tests | High | Blocked | STQA-004, STQA-020 | Cross-tenant/campus denial tests pass |
| STQA-025 | 5 — Reports | Publish HTML/JUnit reports, screenshots, traces and failure artifacts | High | Not Started | STQA-020, STQA-021 | Failed test produces retained evidence |
| STQA-026 | 5 — Reports | Create/update deduplicated GitHub issues on QA failure | Medium | Not Started | STQA-025 | Sanitized reproduction details and artifact links |
| STQA-027 | 6 — Gemini | Install/authenticate Gemini CLI in headless CI with protected API key | Medium | Not Started | STQA-016, STQA-025 | Sanitized report analysis within configured quota |
| STQA-028 | 6 — Gemini | Configure pinned Playwright MCP version and staging-only scope | Medium | Not Started | STQA-020, STQA-027 | MCP verified; destructive actions not authorized |
| STQA-029 | 6 — Gemini | Add optional AI exploratory QA/failure summary step | Medium | Not Started | STQA-025, STQA-027, STQA-028 | AI skip/quota failure does not override Playwright |
| STQA-030 | 7 — Operations | Add staging DB backup, artifact retention and restore drill | High | Not Started | STQA-014, STQA-019 | Restore to clean staging DB verified |
| STQA-031 | 7 — Operations | Add immutable-image rollback and migration safeguards | Critical | Not Started | STQA-018, STQA-019, STQA-030 | Failed deployment rollback tested |
| STQA-032 | 8 — Acceptance | Complete merge-to-main → deploy → QA → report end-to-end flow | Critical | Not Started | All applicable tasks | Acceptance checklist in document 52 complete |

## 4. Current blockers

1. The audited main-branch React app is still the Phase 0 shell.
2. The audited Cloud API and Local API expose basic health endpoints; readiness is not a database check.
3. The audited main branch does not contain login or school-creation workflows.
4. No application Dockerfiles, staging Compose stack, GitHub Actions workflows or Playwright E2E project were found.
5. OCI account, home region, capacity, hostname and credentials must be verified during implementation.

Do not bypass these blockers by writing tests that simulate success without exercising real product behaviour.

## 5. Work log

| Date | Task ID | Change/command | Result | Evidence link | Blocker / next action |
|---|---|---|---|---|---|
| 2026-11-19 | STQA-001 | Audited main tree, README, deployment plan, security/quality docs, local Compose, API hosts, frontend and tests | Completed | See document 52, Section 2 | Continue STQA-002 and STQA-003 |

## 6. Completion rules

- Update this tracker only after collecting acceptance evidence.
- Use **In Review** when implementation exists but is not independently verified.
- A workflow file existing is not proof the workflow works.
- A green build is not proof authentication, authorization, school creation or tenant isolation works.
- A Gemini summary is not proof deterministic tests passed.
- Keep this tracker aligned with `docs/09_PROGRESS_TRACKER.md`; the overall product tracker remains the project-level view.