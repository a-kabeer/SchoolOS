# SchoolOS — Contributor and AI-Agent Guide

This file is the repository-wide source of truth for how people and AI coding assistants contribute to SchoolOS. Read it before changing source code, tests, configuration, dependencies, documentation that changes requirements, or CI/CD.

The numbered documents under `docs/`, approved ADRs, and the canonical task registers remain authoritative for product requirements, architecture, security policy, and implementation status. This guide explains how to work within those decisions; it does not replace them.

## 1. Project invariants

Contributors MUST preserve these approved direction choices unless an approved ADR changes them:

- The school-business application is a **modular monolith**, not an initial microservices system.
- The backend is ASP.NET Core / C# on the .NET 10 target; the frontend is React + TypeScript.
- The strategic deployment model is a Cloud Control Plane plus one or more Local Data Plane nodes.
- Each Local Data Plane owns its operational PostgreSQL database; the Cloud Control Plane owns its separate platform/commercial database.
- Local business workflows must not depend on a live Cloud Control Plane request for routine operation.
- Cross-plane exchange uses authenticated, versioned contracts and the documented synchronization mechanism. The Cloud host must not directly query or update customer operational tables.
- Offline capability is built around the Local Node and its synchronization/lease model. PWA caching is supplementary, not the authoritative offline backend.
- Existing SchoolOS documentation, ADRs and approved task registers take precedence over patterns found in reference repositories.

Do not silently replace these choices with a shared operational database, Angular, microservices, or a cloud-only runtime model.

## 2. Required reading before a change

Read the relevant parts of:

- `README.md` — project status and canonical navigation.
- `docs/54_DOCUMENTATION_STANDARDS.md` — documentation authority and change control.
- `docs/03_MODULAR_MONOLITH_ARCHITECTURE.md` — module and host boundaries.
- `docs/07_SECURITY_AND_QUALITY_REQUIREMENTS.md` — security and quality baseline.
- `docs/46_AUTHORIZATION_AND_TENANT_ISOLATION_MATRIX.md` — permission and scope rules.
- `docs/60_ARCHITECTURE_AND_SECURITY_DECISION_CLOSURE_REGISTER.md` — unresolved decisions.
- The affected module specification, API/event contract, data dictionary, acceptance tests and task-plan/register entries.

For a security, privacy, payment, licensing, sync or AI change, read the matching detailed specification as well. Never infer an unresolved decision from a neighbouring document.

## 3. Security and privacy rules

The following rules are mandatory:

- **SEC-01 — Server-derived scope:** Never trust a tenant, school, campus, class, student or guardian identifier supplied by the browser as proof of authorization. Resolve identity and scope server-side and verify the resource relationship.
- **SEC-02 — Defense in depth:** Apply authorization in the application/service boundary and use database controls such as PostgreSQL RLS where approved. EF Core query filters are not, by themselves, the complete security boundary.
- **SEC-03 — No weakened controls:** Do not remove authorization, validation, antiforgery, tenant filters, audit checks, TLS requirements or security tests to make a feature or build pass. Fix the cause or stop and report the blocker.
- **SEC-04 — No secrets or real personal data:** Never commit passwords, connection strings containing credentials, API keys, signing keys, tokens, private student/guardian/staff information, production exports or identifiable production fixtures.
- **SEC-05 — External data boundaries:** Do not add telemetry destinations, CDNs, AI providers, payment providers, or other external services that receive school data without an approved data-flow, privacy and security review.
- **SEC-06 — Sensitive operations:** Financial postings, refunds, waivers, result publication, role changes, licensing and support access require explicit authorization, state checks and audit evidence.
- **SEC-07 — Offline trust:** Treat queued offline commands as untrusted until validated by the Local Node. Make replay, idempotency, device identity, stale authorization and conflict handling explicit.
- **SEC-08 — Compliance claims:** Security-control mappings do not establish certification or legal compliance. Never claim SchoolOS is SOC 2, ISO 27001, PCI DSS, FedRAMP, GovRAMP or otherwise certified without valid independent evidence and approval.
- **SEC-09 — AI safety:** AI output is advisory unless an approved workflow says otherwise. Do not let model output bypass business validation, authorization, human approval or audit.
- **SEC-10 — Test data:** Use synthetic or appropriately anonymized test data. Never use real minors' records in public CI, screenshots, examples or prompts.

## 4. Architecture and code rules

- Keep business rules inside the owning module. Do not put domain logic in API controllers, React components, background workers or the Shared Kernel.
- Use module-owned Domain, Application, Infrastructure and Contracts boundaries. These may be folders or separate projects as justified by module maturity; do not create empty projects solely to imitate a template.
- Other modules consume published contracts or events, not another module's private entities, DbContext or tables.
- Keep the Shared Kernel small and stable. A concept belongs there only when it is genuinely shared and has no single domain owner.
- Keep Cloud Control Plane and Local Data Plane dependencies explicit. Shared code must not imply shared databases or shared trust.
- Use explicit DTOs/contracts at API and integration boundaries. Do not expose EF Core entities directly.
- Use decimal/numeric types with explicit currency for money. Never use binary floating point for financial amounts.
- Make state transitions explicit. Posted financial records are corrected through approved reversal/adjustment workflows, not destructive edits.
- Use idempotency keys and database constraints for retried or concurrent commands.
- Prefer simple, maintainable solutions. Do not add dependencies, abstractions, services or modules without a demonstrated requirement.

## 5. Testing and evidence

Choose the lowest test level that can prove the behavior, and add higher-level tests where boundaries matter:

- Unit tests for domain invariants and application decisions.
- Integration tests for EF Core mappings, PostgreSQL constraints, transactions, migrations and RLS/context behavior.
- Architecture tests for module dependency rules and Cloud/Local separation.
- Security tests for cross-tenant, cross-campus, relationship, export and privilege boundaries.
- API/functional tests for business workflows and state transitions.
- UI/E2E tests for critical user journeys and accessibility/RTL behavior.

For every code/config/dependency change:

1. Run the relevant formatting, analyzers and build.
2. Run affected tests, then the full required test suite when practical.
3. Run frontend checks when the frontend is changed.
4. Report exact commands and actual results. Never claim a test passed if it was not run.
5. Record evidence in the PR and link the acceptance-test IDs.

A passing unit test does not prove tenant isolation, financial integrity, offline durability or production readiness.

## 6. Documentation and decision control

- Do not invent answers for items marked Open in document 60 or the open-question register.
- Material architecture, security, data ownership, licensing, commercial or AI-governance changes require an ADR or approved amendment to the existing ADR.
- Update affected canonical specifications, acceptance tests, requirement mappings and navigation in the same change.
- Keep implementation status in the seven canonical CSV task-register partitions. Do not create a competing Markdown status tracker.
- A design document, scaffold, placeholder directory or roadmap does not prove implementation.
- Clearly label proposed, accepted, implemented and verified states separately.

## 7. Git and review workflow

- Work on a focused feature or documentation branch; do not commit directly to `main`.
- Keep pull requests small enough to review and include purpose, scope, affected docs/contracts, tests/evidence, migration impact, security/privacy impact and rollback considerations.
- AI-generated changes receive the same CI and human review as any other change.
- AI assistants and automated agents MUST NOT approve or merge their own pull requests, bypass branch protection, force-push shared branches, or change repository security settings.
- Do not close a review finding by suppressing its test or analyzer without an approved rationale.

## 8. Reference-project use

The Education-Portal and eSchool repositories are learning and evidence sources, not upstream dependencies or specifications for SchoolOS.

- Reuse general engineering practices from Education-Portal only where they fit SchoolOS's .NET/React modular-monolith and hybrid topology.
- Use eSchool's functional inventory as a workflow discovery checklist, not as a requirement to clone every module or route.
- Do not copy eSchool's legacy PHP/jQuery implementation, sample secrets, development-only database fallback, screenshots or proprietary material into SchoolOS.
- Every adopted capability must be reconciled with SchoolOS product requirements, data ownership, authorization, acceptance tests and the task registers.

See [DOC-61 — Reference Project Adoption and Integration Plan](docs/61_REFERENCE_PROJECT_ADOPTION_AND_INTEGRATION_PLAN.md) for the controlled adoption crosswalk.
