# 52 — OCI Staging Deployment and Automated QA Runbook

- **Document ID:** DOC-52
- **Status:** Draft
- **Decision maturity:** Proposed staging QA runbook; deployment not implemented
- **Owner:** DevOps/operations lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `91cd4355e68d1b37e1943aeae1db58b4ff34c53f` (documentation refactor base)
**Version:** 1.0  
**Prepared:** 19 November 2026  
**Scope:** SchoolOS cloud-hosted development/pilot staging, continuous deployment, browser regression testing and optional AI-assisted QA.
> This is a staging environment for validating the current SchoolOS web application. It does **not** replace the Hybrid Cloud Control Plane + Local Data Plane architecture or the Local Node deployment requirements in documents 14, 16 and 17. A staging website is not a production Local Node.

## 1. Target delivery path

```text
Feature branch -> Pull request -> CI checks -> Merge into main
                                      |
                                      v
                            GitHub Actions deployment
                                      |
                                      v
                              OCI staging website
                                      |
                                      v
                              GitHub Actions QA
                              /             \
                    Playwright/Chromium   Gemini CLI + MCP
                    deterministic tests   optional AI analysis
                              \             /
                               PASS or FAIL
                                      |
                         GitHub summary / issue evidence
```

The deployment workflow must deploy the exact commit merged into main. QA must start only after deployment succeeds and must test the same commit on the staging URL.

**Core rule:** Playwright is the deterministic QA gate. Gemini is an optional assistant for exploration and failure analysis; it must not replace deterministic assertions or approve a release by itself.

## 2. Existing repository audit — verified baseline

The following was checked against repository branch `main` on 19 November 2026. Re-check this baseline after major merges.

| Area | Current repository observation | Consequence |
|---|---|---|
| Solution | Root `SchoolOS.sln` and `global.json` exist | .NET solution and SDK pin are present |
| Backend | `SchoolOS.Cloud.Api` and `SchoolOS.Local.Api` exist | Two API host projects are present |
| Backend routes | Each host currently exposes a basic `/health` endpoint; OpenAPI is mapped in Development | Process smoke check is possible; full dependency readiness is not verified |
| Frontend | `frontend/schoolos-web`, React + TypeScript + Vite | Frontend project exists |
| Frontend screen | Current `App.tsx` renders a Phase 0 SchoolOS shell | Login and school-management UI are not present in the audited main snapshot |
| Database | `deploy/docker-compose.yml` defines PostgreSQL 17 for local development | This is not a complete staging stack |
| Environment example | `deploy/.env.example` contains local-development database defaults | Never reuse these values for staging or production |
| .NET tests | `tests/UnitTests` and `tests/ArchitectureTests` exist | Add the existing test projects to CI |
| Frontend scripts | Frontend `package.json` has `build` and `lint` scripts | Both can run in CI; no Playwright script is currently defined |
| Playwright | No Playwright E2E project was found | Create a separate QA project |
| App container images | No backend or frontend application Dockerfile was found | Containerization is a prerequisite |
| GitHub Actions | No `.github/workflows` files were found | CI, deployment and QA workflows still need to be added |

### 2.1 Feature-readiness limitation

The audited Cloud API and Local API currently expose only basic health endpoints, and the React application is a Phase 0 shell. The audited main branch does not show a working authentication flow, school creation endpoint/UI or campus-management workflow.

Therefore:

- The first QA iteration can validate the frontend shell, HTTPS, HTTP response and API process health.
- Login, school creation, campus creation and role/permission regression tests remain **Blocked** until those features, authorization rules, stable selectors and test-data setup exist.
- Do not create placeholder tests that claim these workflows passed when the corresponding product features do not exist.
- If the live deployment has functionality newer than audited main, merge that source into this repository and repeat the audit before enabling those tests.

### 2.2 Existing documentation — keep and complement

| Existing file | Keep its responsibility | This runbook's relationship |
|---|---|---|
| `docs/07_SECURITY_AND_QUALITY_REQUIREMENTS.md` | Product security, tenant isolation, privacy and quality requirements | Adds a concrete staging QA execution path; does not replace security requirements |
| `docs/09_PROGRESS_TRACKER.md` | Overall product and hybrid workstream tracker | Links to the dedicated staging QA tracker in document 53 |
| `docs/14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md` | Long-term Cloud Control Plane and Local Data Plane design | Staging is explicitly a separate cloud-hosted QA environment |
| `docs/16_LOCAL_SECURITY_SYNC_AND_OPERATIONS.md` | Production Local Node hardening, synchronization, backup and operations | Its network/secret/database principles are reused; Local Node deployment is not replaced |
| `docs/17_HYBRID_DEPLOYMENT_TASK_PLAN.md` | Product-level hybrid deployment work packages H0–H7 | This document handles the separate developer staging pipeline and browser QA |
| `docs/48_HYBRID_DEPLOYMENT_OPERATIONS_AND_RECOVERY_RUNBOOK.md` | Hybrid environment separation, Local Node provisioning, production operations and recovery | This runbook narrows the scope to developer/pilot OCI staging CI/CD and browser QA; it does not replace the production operating procedures. |
| `docs/DEVELOPMENT_ENVIRONMENT_AND_TOOLING.md` | Local Windows developer toolchain and local workflows | Local development remains unchanged |
| `docs/GIT_WORKFLOW_AND_BRANCHING_STRATEGY.md` | Protected main, feature/docs branches, Conventional Commits and PR review | Staging deployment starts after a successful merge into main |

Staging-specific tasks are tracked in `docs/53_STAGING_QA_PROGRESS_TRACKER.md` to avoid duplicating the hybrid product work packages. Document 48 defines the wider environment separation and production Local Node operating model; this runbook supplies the concrete OCI developer/pilot staging CI/CD procedure only.

## 3. Environment boundaries

SchoolOS has a long-term hybrid product architecture. Keep these environments separate:

| Environment | Purpose | Data | Deployment |
|---|---|---|---|
| Developer local | Individual development and debugging | Local synthetic/test data | Existing local commands and development Compose file |
| OCI staging | Shared development/pilot preview and automated QA | Dedicated synthetic QA database | GitHub Actions deploys to OCI |
| Production Local Data Plane | Customer's authoritative school operations | Customer-owned operational data | Future supported Local Node installation/upgrade process |
| Cloud Control Plane production | SchoolOS platform/customer/subscription control plane | Approved platform metadata and projections | Separate production release process |

Never point staging at a production database, storage bucket, school account or authentication tenant. Use synthetic QA data. OCI staging is not a production-ready hybrid Local Node; the Local Node still requires documents 14, 16 and 17 acceptance controls.

## 4. Target repository layout

Add these paths only after confirming Docker build contexts and runtime requirements:

```text
SchoolOS/
├── .github/
│   └── workflows/
│       ├── ci.yml
│       ├── deploy-oci-staging.yml
│       └── live-ai-qa.yml
├── .gemini/
│   └── settings.json
├── deploy/
│   ├── docker-compose.yml              # Existing local PostgreSQL setup; keep
│   └── staging/
│       ├── compose.yaml                # Separate OCI staging stack
│       ├── Caddyfile                   # Or reviewed Nginx configuration
│       └── .env.example                # Placeholders only
├── frontend/schoolos-web/
├── qa/
│   └── e2e/
│       ├── package.json
│       ├── package-lock.json
│       ├── playwright.config.ts
│       └── specs/
│           ├── health.spec.ts
│           ├── landing.spec.ts
│           ├── auth.spec.ts            # Add when authentication exists
│           ├── schools.spec.ts         # Add when school workflow exists
│           ├── campuses.spec.ts        # Add when campus workflow exists
│           └── permissions.spec.ts    # Add when authorization exists
├── src/
├── tests/                              # Existing .NET test projects; keep
└── docs/
```

Keep the Playwright project under `qa/e2e`; do not replace the existing root `tests/` directory, which contains .NET test projects. A project Gemini settings file may contain MCP server configuration but never API keys, passwords, SSH keys or database credentials.

## 5. OCI staging server

### 5.1 Proposed VM settings

These are the requested starting settings, not a guarantee that capacity or free eligibility will be available in every account or region.

| Setting | Proposed value |
|---|---|
| Instance name | `schoolos-pilot-server` |
| Compartment | Default compartment or a dedicated SchoolOS staging compartment |
| Image | Ubuntu 24.04 LTS, ARM64 |
| Shape | `VM.Standard.A1.Flex` |
| OCPU | 2 |
| Memory | 12 GB |
| Boot volume | 50 GB |
| Public IPv4 | Assign if using direct public HTTPS ingress |
| VCN | Dedicated VCN with internet connectivity |
| Access | SSH key only; restrict SSH to trusted administrator IPs |

### 5.2 Always Free checks — zero cost is not guaranteed

Oracle's current Always Free documentation describes an A1 allowance of 1,500 OCPU-hours and 9,000 GB-hours per month, equivalent to a total of 2 OCPUs and 12 GB RAM for an Always Free tenancy. The allowance is shared across the tenancy and compute instances must be in the home region. The default 50 GB boot volume counts against the combined 200 GB Always Free block-volume pool. The current documentation also lists five total volume backups.

Before creating resources:

1. Confirm the selected A1 shape is labelled **Always Free-eligible** in the OCI Console.
2. Confirm total A1 OCPU and memory across the whole tenancy remain within the allowance.
3. Confirm boot volume, public IP, backups, networking and extra services remain within the intended allowance.
4. Review cost estimates and the tenancy Limits, Quotas and Usage page.
5. Confirm the selected Ubuntu image is ARM64/aarch64.
6. If OCI reports out-of-host-capacity, do not create duplicate resources. Check another available availability domain in the home region or retry later.
7. Enable cost/usage monitoring and use compartment quotas where practical.

Oracle also documents that Always Free compute instances may be reclaimed when published idle criteria are met over a seven-day period. Treat Always Free as a pilot/development hosting option, not a production availability guarantee.

Official references:
- https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier_topic-Always_Free_Resources.htm
- https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier.htm
- https://docs.oracle.com/en-us/iaas/Content/Compute/Tasks/launchinginstance.htm

### 5.3 ARM64 image compatibility

OCI A1 uses ARM64. GitHub-hosted Ubuntu runners may use a different CPU architecture. Final application images must target `linux/arm64`, or be built on a compatible ARM64 runner/host.

- Use Docker Buildx and QEMU where necessary.
- Publish ARM64 images to GHCR tagged with the immutable Git commit SHA.
- Do not publish only an AMD64 image and assume it will run on A1.
- Pin base image versions and, after validation, image digests.
- Use the same immutable image tag for deployment and QA evidence.

## 6. OCI network and host security

### 6.1 Inbound access

| Port | Policy | Reason |
|---|---|---|
| TCP 80 | Public only if needed for HTTP-to-HTTPS redirect/certificate validation | Web ingress |
| TCP 443 | Public | HTTPS staging website |
| TCP 22 | Restricted to trusted administrator public IP/CIDR | SSH administration |
| TCP 5432 | No public ingress | PostgreSQL remains private |

Apply restrictions in both OCI Network Security Group/Security List rules and the VM firewall. Do not rely on a single layer.

### 6.2 Host baseline

1. Connect with the private SSH key and a non-root administrator account.
2. Apply supported Ubuntu security updates.
3. Keep SSH password authentication disabled; use key-based authentication.
4. Restrict SSH to a trusted source IP; do not use unrestricted SSH ingress as the normal configuration.
5. Configure UFW with default-deny inbound and only required ingress.
6. Install Docker Engine and the Docker Compose plugin using Docker's official Ubuntu instructions.
7. Restrict Docker daemon and administrator access.
8. Configure time synchronization, disk monitoring and service restart policies.
9. Record OS, architecture, Docker versions and deployment timestamp.

Official Docker guide: https://docs.docker.com/engine/install/ubuntu/

### 6.3 PostgreSQL isolation

- PostgreSQL must be reachable only on the private Compose network.
- Do not publish `5432:5432` to a public host interface.
- Use a unique high-entropy staging database password.
- Do not reuse local-development defaults in `deploy/.env.example`.
- Use a least-privilege application database role, not PostgreSQL superuser.
- Persist data in a named volume or approved storage location.
- Configure backups separately from the live database volume.

The existing `deploy/docker-compose.yml` is for local development. Do not turn it into a public-facing staging stack. Add a separate `deploy/staging/compose.yaml`.

## 7. Staging container design

### 7.1 Required build artefacts

Before deployment automation can work, add and validate:

- A multi-stage .NET 10 Dockerfile for the Cloud API host selected for the staging preview.
- A frontend Dockerfile that builds the React/Vite app and serves generated static files through a reviewed reverse proxy/web server.
- A staging Compose file with application, PostgreSQL and reverse proxy services.
- Health/readiness strategy.
- Explicit database migration strategy.
- Persistent volume and log-retention settings.
- Staging-only configuration with placeholders only.

The audited repository did not contain application Dockerfiles.

### 7.2 Suggested service boundaries

| Service | Exposure | State |
|---|---|---|
| Reverse proxy (Caddy or Nginx) | Public ports 80/443 | TLS configuration and access logs |
| React frontend | Private Compose network behind proxy | Static build artefact |
| SchoolOS Cloud API | Private Compose network behind proxy | Stateless process where practical |
| PostgreSQL | Private Compose network only | Persistent database volume |

This staging target is a cloud-hosted preview. Do not expose the Local Data Plane API or PostgreSQL directly to the public internet. If a separate Local API target is needed later, define a protected service and separate test dataset.

### 7.3 Configuration rules

- Read connection strings and credentials from environment variables or an approved secret provider.
- Never bake secrets into Docker image layers or build arguments.
- Do not commit the real staging `.env` file.
- Restrict server-side environment file permissions.
- Configure ASP.NET Core environment as Staging.
- Keep developer exception pages disabled.
- Configure forwarded headers correctly behind the reverse proxy.
- Run schema migrations as a controlled one-time deploy step, not independently in every app replica.
- Record the deployed commit SHA and image digest in deployment metadata/logs.

### 7.4 Health versus readiness

Existing `/health` endpoints report API process status. They do not currently prove PostgreSQL connectivity, migration completion or business workflow readiness.

Before claiming system readiness, implement a readiness endpoint (for example, `/health/ready`) that checks required dependencies safely. Keep liveness separate from readiness.

Initial deployment verification may use the existing `/health` response, but label it as an application-process smoke check.

## 8. GitHub Actions design

### 8.1 Workflow responsibilities

| Workflow | Trigger | Responsibility | Permission model |
|---|---|---|---|
| `.github/workflows/ci.yml` | PR targeting `main` | Restore, build, test, frontend lint/build; no deploy secrets | Read-only |
| `.github/workflows/deploy-oci-staging.yml` | Push to `main` after merge | Build ARM64 images, publish immutable tags, deploy to OCI and verify health | Contents read; packages write; staging deployment secrets |
| `.github/workflows/live-ai-qa.yml` | Successful staging deployment | Checkout exact deployed commit, run Playwright, save evidence, optionally run Gemini | Contents read; issue write only for reporting job |

QA can alternatively be a downstream job inside the deployment workflow. If separate, validate the branch, source repository and exact head SHA before tests.

### 8.2 PR CI checks

Use versions pinned by the repository and lockfiles. The audited baseline includes SDK pinning in `global.json`, Node.js 22.x LTS in the development guide, and a frontend lockfile.

Minimum checks:

1. Checkout PR commit.
2. Set up .NET SDK using `global.json`.
3. Run `dotnet restore`.
4. Run `dotnet build --no-restore`.
5. Run `dotnet test` (or the reviewed equivalent).
6. Set up Node.js 22.x.
7. Run `npm ci` from `frontend/schoolos-web`.
8. Run `npm run lint`.
9. Run `npm run build`.
10. Publish test output on failure.

The E2E project should run its own `npm ci` and Playwright checks after it is introduced. Keep secrets unavailable to untrusted pull-request code.

### 8.3 Deployment after merge

The deployment workflow should:

1. Trigger only from a push to `main`.
2. Check out the pushed commit SHA.
3. Build and test the same source revision.
4. Build ARM64-compatible application images.
5. Tag each image with the full commit SHA; do not use `latest` as deployment identity.
6. Authenticate to GHCR using workflow `GITHUB_TOKEN` and minimum `packages: write` permission.
7. Publish images.
8. Connect to OCI using a dedicated deployment SSH key.
9. Pull exact commit-tagged images on the VM.
10. Run pre-deploy checks and take a database backup before schema migration when required.
11. Apply approved migrations once, if required.
12. Update/restart staging Compose services.
13. Wait for health/readiness checks.
14. Record deployed commit SHA and image digest.
15. Fail the job if the deployment is unhealthy.

Use a protected GitHub Environment named `staging`. Add concurrency controls so overlapping deployments cannot corrupt staging.

### 8.4 QA trigger and commit integrity

QA must not test whichever revision happens to be newest on main. It must test the exact SHA deployed by the successful deployment run.

For a separate `workflow_run` QA workflow:

- Accept only the named staging deployment workflow.
- Require a successful conclusion.
- Require `head_branch == main`.
- Require source repository to equal the current repository.
- Check out `workflow_run.head_sha`.
- Pass the staging URL through a protected environment variable.
- Do not execute scripts or binaries downloaded from untrusted PR artifacts.

For the simplest security model, a QA job after the deploy job in the same trusted main-branch workflow is also acceptable.

### 8.5 Branch protection and release policy

Follow `docs/GIT_WORKFLOW_AND_BRANCHING_STRATEGY.md`:

- Protect `main`; do not use direct commits or force-pushes.
- Require PR review and CI checks.
- Deploy staging only from merged `main`.
- A failed QA run creates evidence and a bug issue; it does not automatically modify code or merge a PR.
- Production deployment must be a separate, explicitly approved workflow and is outside this runbook.

Official references:
- https://docs.github.com/en/actions
- https://docs.github.com/en/actions/security-for-github-actions/security-guides/automatic-token-authentication
- https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment

## 9. GitHub Environment variables and secrets

Create a GitHub Environment named `staging`. Use environment variables for non-sensitive settings and environment secrets for credentials.

| Name | Type | Purpose | When needed |
|---|---|---|---|
| `STAGING_BASE_URL` | Variable | Staging HTTPS URL | After DNS/TLS |
| `DEPLOY_HOST` | Secret or protected variable | OCI host/IP | Before SSH deployment |
| `DEPLOY_USER` | Secret or protected variable | Restricted deploy account | Before SSH deployment |
| `DEPLOY_SSH_PRIVATE_KEY` | Secret | Dedicated CI private key | Before SSH deployment |
| `DEPLOY_SSH_KNOWN_HOSTS` | Secret | Pre-verified SSH host key line(s) | Before SSH deployment |
| `QA_EMAIL` | Secret | Staging QA account email | Only after authentication exists |
| `QA_PASSWORD` | Secret | Staging QA account password | Only after authentication exists |
| `GEMINI_API_KEY` | Secret | Google AI Studio key for headless Gemini CLI | Optional AI QA phase |

Notes:

- Use automatically provided `GITHUB_TOKEN` for GHCR publishing with required package permission; avoid long-lived personal tokens when not needed.
- Staging DB password/connection string belongs on the OCI host or approved secret manager. Do not pass it to Playwright or store it in GitHub Actions unless a narrowly scoped migration job genuinely needs it.
- Never put secret values in workflow YAML, `.gemini/settings.json`, Dockerfiles, issue bodies, screenshots or logs.
- Never reuse a production administrator account as the QA account.
- Do not enable login/school-create tests until a dedicated least-privilege staging account and deterministic test-data strategy exist.
- If Gemini AI Studio API key authentication is selected, verify its own current quota/pricing. A Google AI Pro subscription is not a guarantee of unlimited or free API usage in a non-interactive CI runner.

## 10. Playwright E2E test framework

### 10.1 Initialise the QA project

From the repository root:

```bash
mkdir -p qa/e2e
cd qa/e2e
npm init -y
npm install --save-dev @playwright/test
npx playwright install chromium
```

Commit `package.json` and `package-lock.json`. In CI use `npm ci` and install only Chromium.

Official reference: https://playwright.dev/docs/ci

### 10.2 Recommended configuration

- Test directory: `specs`.
- Browser: Chromium initially.
- Base URL: supplied by `STAGING_BASE_URL`.
- CI workers: start with one worker for stability.
- Retries: one retry in CI for diagnosis; do not hide repeated failures.
- Trace: retain on first retry or failure.
- Screenshot: capture on failure.
- Video: retain on failure only when useful.
- HTML/JUnit reports: save as workflow artifacts.
- Set explicit test/action timeouts and tune them from observed response times.

Do not hardcode staging URL or QA password in Playwright config.

### 10.3 Tests possible against the audited main branch

| Spec | Readiness | Test intent |
|---|---|---|
| `health.spec.ts` | Ready after API deployment | Verify expected health response |
| `landing.spec.ts` | Ready after frontend deployment | Verify HTTPS navigation and Phase 0 shell |
| `auth.spec.ts` | Blocked | Add after authentication UI/API exists |
| `schools.spec.ts` | Blocked | Add after school list/create workflows exist |
| `campuses.spec.ts` | Blocked | Add after campus workflows exist |
| `permissions.spec.ts` | Blocked | Add after roles and authorization policies exist |

### 10.4 Later authenticated flow

When authentication and school management are implemented:

1. Use a staging-only QA administrator with only the permissions required by the tests.
2. Log in through the public UI; do not bypass authentication by writing session cookies unless a reviewed test-auth mechanism exists.
3. Verify the authenticated user and role.
4. Create a uniquely named synthetic school using a deterministic test suffix.
5. Verify the school appears in list and detail views.
6. Test required-field and invalid-input validation.
7. Verify unauthorized users cannot create or view another tenant's school.
8. Clean test data only through an approved cleanup endpoint or isolated disposable test database.
9. Capture evidence on failure.

Never create real school, student or guardian records in production. Avoid sending real minor/student data to AI services.

### 10.5 Selector and data standards

- Prefer accessible roles and labels.
- Add stable `data-testid` values where a stable user-facing locator is unavailable.
- Avoid generated CSS class selectors.
- Each test owns its data or uses an isolated seeded dataset.
- Make setup and cleanup idempotent.
- Keep tests independent and repeatable.
- Diagnose flaky tests rather than increasing retries indefinitely.

## 11. Gemini CLI and Playwright MCP

### 11.1 Purpose

Gemini CLI is an optional AI-assisted QA layer. It can read Playwright reports, screenshots, traces and sanitized logs; summarize likely failure causes; suggest missing edge cases; inspect the staging browser through MCP; and draft reproducible bug reports.

It cannot replace Playwright assertions, tenant-isolation tests, security review or human release approval.

### 11.2 Install and authenticate

Official Gemini CLI installation:

```bash
npm install -g @google/gemini-cli
gemini --version
```

For a non-interactive CI runner, use a supported headless authentication method such as a Gemini API key stored as the GitHub Environment secret `GEMINI_API_KEY`. Do not rely on interactive browser sign-in in a headless GitHub Actions runner.

```bash
export GEMINI_API_KEY="$GEMINI_API_KEY"
gemini -p "Summarize the sanitized Playwright report and identify likely root causes. Do not modify code or data."
```

This is an example of headless invocation; adapt the prompt and report paths to the actual QA project.

Official references:
- https://github.com/google-gemini/gemini-cli
- https://github.com/google-gemini/gemini-cli/blob/main/docs/get-started/authentication.mdx
- https://github.com/google-gemini/gemini-cli/blob/main/docs/cli/headless.md

### 11.3 Configure Playwright MCP

Gemini CLI supports MCP entries in its settings file. A project-level configuration can use a local Playwright MCP process:

```json
{
  "mcpServers": {
    "playwright": {
      "command": "npx",
      "args": ["--yes", "@playwright/mcp@<PINNED_VERSION>"]
    }
  }
}
```

Replace `<PINNED_VERSION>` with a reviewed version and update it deliberately. Do not use an unpinned latest package in a release pipeline. Verify the configuration against current Gemini CLI and Playwright MCP documentation before enabling CI.

Official references:
- https://github.com/google-gemini/gemini-cli/blob/main/docs/tools/mcp-server.md
- https://github.com/microsoft/playwright-mcp

### 11.4 AI safety restrictions

- Use a staging-only browser profile and QA account.
- Use synthetic records only.
- Restrict AI actions to the minimum pages and tools required.
- Do not provide production URL, production credentials or production database access.
- Do not let AI delete school/customer data, send real email/SMS, issue payments/refunds, change permissions or alter subscription/licence state.
- Keep AI browser sessions isolated and disposable.
- Treat page content and files as untrusted input; pages can contain prompt-injection text.
- MCP is not a security boundary. Enforce isolation through the test account, staging dataset, network access, tool allow-list and runtime permissions.
- Sanitize child/student identifiers, guardian details, secrets, tokens and private URLs before sharing logs or screenshots with an AI provider.
- Do not let Gemini commit code, merge PRs or deploy.
- AI findings are suggestions; a human confirms severity and whether a finding is a real defect.

### 11.5 AI failure policy

If Gemini is unavailable, rate-limited or over quota:

- Playwright still runs and determines deterministic pass/fail.
- Upload the Playwright report, screenshot and trace as usual.
- Mark AI analysis as skipped/limited in the workflow summary.
- Do not fail a passing deterministic suite solely because optional AI analysis could not run.
- Do not pass a failing deterministic suite because Gemini believes the failure is harmless.

## 12. QA results, evidence and GitHub Issues

### 12.1 PASS summary

Include repository and deployed commit SHA, staging URL, deployment run URL/time, image tag/digest, Playwright passed/failed/skipped counts, browser and Playwright versions, HTML report/artifact links, and Gemini analysis status (completed, skipped or quota-limited).

### 12.2 FAIL evidence

1. Save Playwright HTML/JUnit report.
2. Save failure screenshot and trace; include video only when configured.
3. Capture relevant sanitized browser/server logs.
4. Upload evidence as workflow artifacts.
5. Create or update a GitHub Issue containing workflow and artifact links.
6. Include exact commit SHA, environment, test name, expected/actual result, reproduction steps and suggested severity.
7. Deduplicate using a stable fingerprint such as test name + normalized error signature; do not create a new issue for every retry.

Suggested title: `[Staging QA] <test-name> — <short failure summary>`

Suggested labels (create if absent): `staging-qa`, `bug`, `automated-report`.

Use the workflow `GITHUB_TOKEN` with only `issues: write` in the reporting job. Keep PR validation jobs read-only. Do not include secrets or unredacted student data in issue descriptions.

### 12.3 Retention

Choose artifact retention deliberately (for example, 14 days for ordinary QA runs). Failure artifacts may contain sensitive UI content; review redaction and repository access before making them available to collaborators.

## 13. Backup, recovery and rollback

Staging data is disposable, but still protect configuration and test evidence:

- Use a separate staging PostgreSQL database and persistent volume.
- Do not restore production data into staging.
- Use synthetic seed data.
- Back up before schema migrations or destructive staging operations.
- Store backups outside the live database volume.
- Restrict backup access.
- Test restoration to a clean staging database before accepting the backup process.

### 13.1 Application rollback

- Keep the last known-good image tag/digest.
- On failed deployment, redeploy the last known-good application image.
- Verify API health and browser smoke test after rollback.
- Do not automatically reverse migrations unless a safe down path has been tested.
- Prefer backward-compatible expand/contract migrations.
- If data migration is incompatible, restore from a verified pre-deploy backup and record the data-loss window.

An image rollback does not automatically roll back a database schema.

## 14. End-to-end acceptance gate

- [ ] PR CI builds and tests the exact PR revision.
- [ ] Main is protected and requires documented reviews/checks.
- [ ] Merge into main triggers staging deployment.
- [ ] OCI VM is in the intended home region and uses approved resource allocation.
- [ ] Images are ARM64-compatible and tagged with the commit SHA.
- [ ] Staging has its own database and synthetic data.
- [ ] PostgreSQL is not publicly reachable.
- [ ] HTTPS is valid and HTTP redirects as expected.
- [ ] Deployed commit SHA matches the workflow SHA.
- [ ] Health/readiness semantics are documented.
- [ ] Playwright runs only after deployment succeeds.
- [ ] Playwright tests the exact deployed commit.
- [ ] Failure screenshots, traces and reports are retained.
- [ ] GitHub issue creation is restricted, deduplicated and redacted.
- [ ] Gemini analysis is optional, scoped and cannot approve deployment.
- [ ] Backup and restore have been exercised.
- [ ] OCI quotas, costs, artifact storage and AI API usage are reviewed.

Login and school-creation acceptance criteria remain conditional on those application features being implemented.

## 15. Implementation sequence

Follow `docs/53_STAGING_QA_PROGRESS_TRACKER.md` in order. The sequence is split into readiness, infrastructure, deployment, deterministic QA, AI QA and operational acceptance.

Complete one task, attach evidence, verify the result and then update its status. Do not mark a task Completed merely because a command was issued.

## 16. Troubleshooting

| Symptom | First checks | Safe response |
|---|---|---|
| OCI A1 capacity error | Home region, availability domain, tenancy capacity | Retry later or check another availability domain; do not create duplicate paid resources |
| VM unreachable | Public IP, VCN route, NSG/Security List, UFW, SSH source IP | Use OCI Console/serial access; do not permanently open SSH globally |
| ARM image fails | Image platform, buildx target, native dependencies | Rebuild/publish `linux/arm64` and verify image manifest |
| Compose fails | Compose validation, env file, image tags, volume ownership | Inspect logs/config; do not delete DB volumes |
| API healthy, app unusable | Frontend/API proxy, readiness, DB connectivity | Distinguish process health from full readiness |
| Login/school tests fail due missing routes | Product feature state and deployed SHA | Keep feature tests Blocked until UI/API exist |
| Playwright browser missing | CI install step and browser version | Run documented Chromium installation |
| SSH deployment fails | Key format, username, verified host key, permissions | Rotate/reissue the dedicated deploy key if exposure is suspected |
| Issue creation fails | Job token permissions and repository settings | Grant only reporting job required write permission |
| Gemini rate-limited | API key auth and current quota | Mark AI analysis skipped; Playwright remains authoritative |
| Disk usage grows | Docker images, logs, reports, PostgreSQL volume | Prune only confirmed unused artefacts; never remove live DB volume |

## 17. Official references

- OCI Always Free resources: https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier_topic-Always_Free_Resources.htm
- OCI Free Tier: https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier.htm
- OCI instance creation: https://docs.oracle.com/en-us/iaas/Content/Compute/Tasks/launchinginstance.htm
- Docker Engine on Ubuntu: https://docs.docker.com/engine/install/ubuntu/
- Docker Compose: https://docs.docker.com/compose/
- GitHub Actions: https://docs.github.com/en/actions
- GitHub Environments: https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment
- GitHub Actions token permissions: https://docs.github.com/en/actions/security-for-github-actions/security-guides/automatic-token-authentication
- Playwright CI: https://playwright.dev/docs/ci
- Playwright MCP: https://github.com/microsoft/playwright-mcp
- Gemini CLI installation: https://github.com/google-gemini/gemini-cli/blob/main/docs/get-started/installation.mdx
- Gemini CLI authentication: https://github.com/google-gemini/gemini-cli/blob/main/docs/get-started/authentication.mdx
- Gemini CLI headless mode: https://github.com/google-gemini/gemini-cli/blob/main/docs/cli/headless.md
- Gemini CLI MCP setup: https://github.com/google-gemini/gemini-cli/blob/main/docs/tools/mcp-server.md

**Maintenance note:** OCI limits, Always Free eligibility, region capacity, GitHub Actions quotas, Gemini authentication/API quotas, Node.js versions, Playwright releases and MCP configuration can change. Re-check official documentation and the OCI Console at implementation time. This is an implementation guide, not a claim that these services are already deployed.
